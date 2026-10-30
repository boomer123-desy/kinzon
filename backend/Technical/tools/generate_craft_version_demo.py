"""Generate a complete, isolated craft-version fixture in test_standard.

The default run validates inside a transaction and rolls back. Use --apply to
commit. Supply CRAFT_DB_PASSWORD at runtime; credentials are never saved.
"""
import argparse
from collections import Counter, defaultdict
from datetime import datetime
from decimal import Decimal
import getpass
import json
import os
from pathlib import Path
import re

import pymysql


STATES = {0: '暂存', 2: '已发布', 3: '已废弃'}
PRODUCTS = {
    'A': ('总装成品', 2, 146, 45),
    'B': ('壳体半成品', 1, 146, 60),
    'C': ('内芯半成品', 1, 146, 60),
    'D': ('独立切换成品', 2, 146, 45),
    'E': ('单版本草稿', 1, 146, 60),
    'F': ('规格产出成品', 2, 147, 45),
    'RAW': ('领用主料', 0, 145, 135),
    'ALT': ('领用替料', 0, 145, 135),
    'PACK': ('包装材料', 0, 146, 135),
}
# (state, main, process count); no new records use the skipped review state.
VERSIONS = {
    'A': [(3, False, 3), (2, True, 4), (0, False, 5),
          (2, False, 4), (3, False, 3), (0, False, 6)],
    'B': [(2, False, 3), (2, True, 4), (0, False, 5), (3, False, 3)],
    'C': [(2, True, 3), (0, False, 4), (3, False, 3)],
    'D': [(3, False, 3), (2, True, 4), (0, False, 5), (2, False, 4)],
    'E': [(0, True, 1)],
    'F': [(3, False, 3), (2, True, 4), (0, False, 5)],
}
ENTITY_TABLES = [
    'Base_Product', 'Base_ProductSpecification', 'Base_UnitConversion',
    'Base_Craft', 'Base_Machine', 'Technical_CraftOrder',
    'Production_MBom', 'Production_MBom_Version', 'WF_WorkflowTemplate',
    'Technical_CraftOrderVersion', 'WF_NodeTemplate',
    'Technical_NodeSettingTemplate', 'Technical_NodeSettingTemplate_Machine_RL',
    'Technical_NodeSettingTemplate_Material_RL', 'Production_MBom_Detail',
]
RELATION_TABLES = ['Base_Machine_Craft_RL', 'WF_NodeTemplateLink']


def encode(value):
    return json.dumps(value, ensure_ascii=False, indent=2,
                      default=lambda x: str(x) if isinstance(x, Decimal) else x.isoformat())


def require(condition, message):
    if not condition:
        raise ValueError(message)


class Fixture:
    def __init__(self, cursor, batch):
        self.q = cursor
        self.batch = batch
        now = datetime.now().replace(microsecond=0)
        self.audit = dict(EnterpriseId=0, CreateUserNo=batch, UpdateUserNo=batch,
                          CreateUserName='工艺版本测试', UpdateUserName='工艺版本测试',
                          CreateTime=now, UpdateTime=now)
        self.ids = {table: [] for table in ENTITY_TABLES}
        self.relations = {table: [] for table in RELATION_TABLES}
        self.products = {}
        self.orders = {}
        self.versions = {}
        self.units = {}

    def all(self, sql, params=()):
        self.q.execute(sql, params)
        return self.q.fetchall()

    def insert(self, table, **values):
        require(table in self.ids or table in self.relations, 'Unapproved table')
        if table in self.ids:
            values = {**self.audit, **values}
        fields = ','.join('`' + name + '`' for name in values)
        self.q.execute(f'INSERT INTO `{table}` ({fields}) VALUES ('
                       + ','.join(['%s'] * len(values)) + ')', tuple(values.values()))
        if table in self.ids:
            result = self.q.lastrowid
            self.ids[table].append(result)
            return result
        self.relations[table].append(values)
        return None

    def update(self, table, row_id, **values):
        # Updates may only target rows inserted by this transaction.
        require(row_id in self.ids[table], 'Cannot update an existing business row')
        self.q.execute(f'UPDATE `{table}` SET '
                       + ','.join(f'`{key}`=%s' for key in values)
                       + ' WHERE Id=%s AND CreateUserNo=%s',
                       (*values.values(), row_id, self.batch))

    def preflight(self):
        tables = ENTITY_TABLES + RELATION_TABLES
        engines = self.all('SELECT TABLE_NAME, ENGINE FROM information_schema.TABLES '
                          'WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME IN ('
                          + ','.join(['%s'] * len(tables)) + ')', tables)
        require(len(engines) == len(tables)
                and all(row['ENGINE'] == 'InnoDB' for row in engines),
                'All target tables must exist and support transactions')
        triggers = self.all('SELECT EVENT_OBJECT_TABLE FROM information_schema.TRIGGERS '
                            'WHERE TRIGGER_SCHEMA=DATABASE()')
        require(not any(row['EVENT_OBJECT_TABLE'] in tables for row in triggers),
                'Inspect trigger side effects before seeding')
        require(not self.all('SELECT Id FROM Technical_CraftOrder WHERE Code LIKE %s',
                             (self.batch + '%',)), 'This batch already exists')
        require(not self.all('SELECT Id FROM Base_Product WHERE ProductNo LIKE %s',
                             (self.batch + '%',)), 'This product batch already exists')
        self.units = {row['Id']: row['Name'] for row in self.all(
            'SELECT Id,Name FROM Base_Unit WHERE Id IN (141,145,146,147) '
            'AND EnterpriseId=0 AND IsDeleted=0')}
        require(len(self.units) == 4, 'Missing required units')
        categories = self.all('SELECT Id FROM Base_Category WHERE Id IN (45,60,135,141) '
                              'AND EnterpriseId=0 AND IsDeleted=0')
        require(len(categories) == 4, 'Missing required categories')
        node_types = self.all('SELECT Id,NodeTypeEnum FROM WF_NodeType '
                              'WHERE WorkflowTypeId=1 AND EnterpriseId=0 AND Id IN (1,2,3)')
        require({x['Id']: x['NodeTypeEnum'] for x in node_types} == {1: 1, 2: 10, 3: 99},
                'Unexpected craft workflow node types')

    def snapshot(self, unit_id, quantity=1, conversion_id=0, ratio=1):
        return dict(Id=conversion_id, UnitId=unit_id, UnitName=self.units[unit_id],
                    Ratio=float(ratio), Quantity=float(quantity))

    def create_catalog(self):
        for key, (name, product_type, unit, category) in PRODUCTS.items():
            self.products[key] = self.insert('Base_Product',
                ProductNo=f'{self.batch}-P-{key}', ProductName=f'版本测试-{name}',
                ProductType=product_type, BaseUnitId=unit, ProductCategoryId=category,
                Discriminator='Base_Product', ReviewStatus=2, IsDeleted=0,
                NoSuffix=f'CV-{key}', Remark=f'{self.batch} 独立测试资料')
        self.spec_id = self.insert('Base_ProductSpecification', ProductId=self.products['F'],
            Specification='5米/段', Name='版本测试规格', NoSuffix='CV-F-5M',
            Code=f'{self.batch}-SPEC-F', SpecificationUnitId=141,
            ProductCategoryId=45, IsDeleted=0, Remark=self.batch)
        self.spec_conversion_id = self.insert('Base_UnitConversion',
            SourceId=self.spec_id, TargetId=self.products['F'], ConversionType=12,
            FromUnitId=141, ToUnitId=147, Ratio=5)
        self.crafts = {}
        for key, name in [('CUT', '下料'), ('FORM', '加工'), ('OUT', '外协'), ('QC', '检验包装')]:
            self.crafts[key] = self.insert('Base_Craft', Name=f'版本测试-{name}',
                Code=f'{self.batch}-{key}', Remark=self.batch)
        self.machines = defaultdict(list)
        for key, label in [('CUT', '下料机'), ('FORM', '加工机一'),
                           ('FORM', '加工机二'), ('QC', '检验台')]:
            machine = self.insert('Base_Machine', Code=f'{self.batch}-M{len(self.ids["Base_Machine"]) + 1}',
                Name=f'版本测试-{label}', Model='CV-DEMO', Capacity=100,
                IsStop=0, IsDeleted=0, CategoryId=141, MachineType=0, Remark=self.batch)
            self.insert('Base_Machine_Craft_RL', MachineId=machine, CraftId=self.crafts[key])
            self.machines[key].append(machine)

    def create_versions(self):
        for key, definitions in VERSIONS.items():
            name = f'版本测试-{PRODUCTS[key][0]}'
            spec = self.spec_id if key == 'F' else None
            craft_id = self.insert('Technical_CraftOrder', Code=f'{self.batch}-{key}',
                Name=name, ProductId=self.products[key], SpecificationId=spec,
                Description=f'{self.batch} 多版本测试专用；工序/物料用量随版本变化', IsDeleted=0)
            bom_id = self.insert('Production_MBom', ProductId=self.products[key],
                ProductSpecificationId=spec, Status=1 if key == 'E' else 2, IsDeleted=0)
            self.orders[key] = dict(id=craft_id, code=f'{self.batch}-{key}', name=name,
                                    product_id=self.products[key], bom_id=bom_id, versions=[])
            for number, (status, main, count) in enumerate(definitions, 1):
                remark = f'{self.batch} {key}-V{number}；{count}道工序；用于版本切换对照'
                if key == 'E':
                    remark += '；故意未勾选末工序报工，测试发布校验'
                workflow_id = self.insert('WF_WorkflowTemplate', Name=f'{name}-V{number}', WorkflowTypeId=1)
                units = [self.snapshot(141 if key == 'F' else PRODUCTS[key][2], conversion_id=-1)]
                if key == 'F':
                    units.append(self.snapshot(147, quantity=5,
                                               conversion_id=self.spec_conversion_id, ratio=Decimal('0.2')))
                bom_version = self.insert('Production_MBom_Version', MBomId=bom_id,
                    Version=f'V{number}', IsMain=main, Status=1 if status == 0 else status,
                    TotalQuantity=1, UnitConversionId=-1, UnitInfo=encode(units),
                    Remark=remark, IsDeleted=0)
                version_id = self.insert('Technical_CraftOrderVersion', CraftOrderId=craft_id,
                    SequenceNo=number, Version=f'V{number}', IsMain=main, Status=status,
                    Remark=remark, IsDeleted=0, WorkflowTemplateId=workflow_id, BomVersionId=bom_version)
                value = dict(id=version_id, number=number, status=status, state=STATES[status],
                    main=main, process_count=count, workflow_id=workflow_id, bom_version_id=bom_version,
                    nodes=[], material_ids=[], key=key)
                self.versions[key, number] = value
                self.orders[key]['versions'].append(value)

    def workflow_node(self, workflow_id, name, kind, position):
        return self.insert('WF_NodeTemplate', Name=name, Type={1: 'start', 2: '节点', 3: 'end'}[kind],
            Left=20 + position * 160, Top=100, Width=120 if kind == 2 else 25,
            Height=50 if kind == 2 else 25, NodeTypeId=kind, DataId=0, WorkflowTemplateId=workflow_id)

    def material(self, version, node, key, quantity, substitute=None, ref=None, output=False):
        is_spec = key == 'F' and output
        unit = 141 if is_spec else PRODUCTS[key][2]
        material_id = self.insert('Technical_NodeSettingTemplate_Material_RL',
            NodeSettingTemplateId=node, DataType=2 if is_spec else 1,
            DataId=self.spec_id if is_spec else self.products[key],
            IOType=2 if output else 1, InsteadSourceId=substitute, Quantity=quantity,
            UnitId=unit, UnitConversionId=0, UnitListInfo=encode([self.snapshot(unit, quantity)]),
            LossRate=0 if output else Decimal('0.015'), SortNo=len(version['material_ids']),
            RefCraftOrderVersionId=self.versions[ref]['id'] if ref else None,
            Remark=f'{self.batch} {"产出" if output else "替料" if substitute else "领用"}')
        version['material_ids'].append(material_id)
        return material_id

    def create_routes(self):
        for (key, number), version in self.versions.items():
            count = version['process_count']
            # V3 / V4 of A and D include outsourcing, preceded by an inbound process.
            outsource = 2 if key in ('A', 'D') and number in (3, 4) else -1
            workflow = version['workflow_id']
            chain = [self.workflow_node(workflow, '开始', 1, 0)]
            for index in range(count):
                craft = ('QC' if index == count - 1 else 'CUT' if index == 0
                         else 'OUT' if index == outsource else 'FORM')
                label = {'CUT': '下料', 'FORM': '加工', 'OUT': '外协处理', 'QC': '检验入库'}[craft]
                name = f'{key}-V{number}-{index + 1:02d} {label}'
                wf_node = self.workflow_node(workflow, name, 2, index + 1)
                chain.append(wf_node)
                inbound = index == count - 1 or index == outsource - 1
                node = self.insert('Technical_NodeSettingTemplate', CraftOrderVersionId=version['id'],
                    Sequence=index + 1, Code=f'{key}-V{number}-N{index + 1:02d}', Name=name,
                    IsOutsource=index == outsource, IsInbound=inbound,
                    IsReport=key != 'E', WorkHours=Decimal(number + index + 1) / 10,
                    Description=f'{self.batch} 用于核对版本工序差异', IsDeleted=0,
                    NodeTemplateId=wf_node, CraftId=self.crafts[craft])
                version['nodes'].append(node)
                for machine_index, machine in enumerate(self.machines[craft]):
                    self.insert('Technical_NodeSettingTemplate_Machine_RL', MachineId=machine,
                        NodeSettingTemplateId=node, IsDeleted=0,
                        WorkHours=Decimal(number + index + machine_index + 1) / 10)
                if inbound:
                    self.material(version, node, key, 1, output=True)
            chain.append(self.workflow_node(workflow, '结束', 3, count + 1))
            for previous, following in zip(chain, chain[1:]):
                self.insert('WF_NodeTemplateLink', PreviousId=previous, NextId=following,
                            WorkflowTemplateId=workflow)
            first, last = version['nodes'][0], version['nodes'][-1]
            main = self.material(version, first, 'RAW', Decimal('1.20') + Decimal(number) / 10)
            self.material(version, first, 'ALT', Decimal('1.35') + Decimal(number) / 10, substitute=main)
            if count > 1:
                # Repeated material across processes tests BOM aggregation without counting outputs.
                self.material(version, version['nodes'][1], 'RAW', Decimal('0.05') * number)
            self.material(version, last, 'PACK', Decimal('0.10') * number)
            if key == 'A':
                # Saved V1 references stay V1 even while B's default main is V2.
                self.material(version, first, 'B', 1, ref=('B', 1 if number in (1, 4) else 2))
                self.material(version, first, 'C', 2, ref=('C', 1))
            if key == 'B':
                self.material(version, first, 'C', 1, ref=('C', 1))

    def aggregate(self):
        rows = self.all('''SELECT m.*, n.CraftOrderVersionId, v.BomVersionId AS RefBomVersionId
            FROM Technical_NodeSettingTemplate_Material_RL m
            JOIN Technical_NodeSettingTemplate n ON n.Id=m.NodeSettingTemplateId
            LEFT JOIN Technical_CraftOrderVersion v ON v.Id=m.RefCraftOrderVersionId
            WHERE m.CreateUserNo=%s AND m.IOType=1 ORDER BY m.Id''', (self.batch,))
        totals = defaultdict(lambda: defaultdict(lambda: dict(quantity=Decimal(0), substitutes=defaultdict(Decimal))))
        mains = {}
        for row in rows:
            if row['InsteadSourceId']:
                continue
            identity = (4, row['RefBomVersionId']) if row['RefCraftOrderVersionId'] else (1, row['DataId'])
            group = totals[row['CraftOrderVersionId']][identity]
            group['quantity'] += row['Quantity']
            group['unit'] = row['UnitId']
            mains[row['Id']] = group
        for row in rows:
            if row['InsteadSourceId']:
                group = mains[row['InsteadSourceId']]
                group['substitutes'][row['DataId'], row['UnitId']] += row['Quantity']
        return totals

    def create_bom_details(self):
        totals = self.aggregate()
        for key, order in self.orders.items():
            main_version = next(v for v in order['versions'] if v['main'])
            baseline = totals[main_version['id']]
            for version in order['versions']:
                position = 0
                for (detail_type, detail_id), total in totals[version['id']].items():
                    quantity = total['quantity'].quantize(Decimal('0.0001'))
                    baseline_row = baseline.get((detail_type, detail_id))
                    diff = quantity - baseline_row['quantity'] if baseline_row else None
                    main_id = self.insert('Production_MBom_Detail', DetailType=detail_type, DetailId=detail_id,
                        Quantity=quantity, UnitConversionId=0,
                        UnitInfo=encode([self.snapshot(total['unit'], quantity)]), Ratio=quantity,
                        UpperTolerance=0, LowerTolerance=0, QuantityDiff2MainVersion=diff,
                        RatioDiff2MainVersion=diff, IsSubstitute=0, MainDetailId=None, Order=position,
                        VersionId=version['bom_version_id'], MBomId=order['bom_id'])
                    position += 1
                    for (sub_id, unit), amount in total['substitutes'].items():
                        self.insert('Production_MBom_Detail', DetailType=1, DetailId=sub_id,
                            Quantity=amount, UnitConversionId=0, UnitInfo=encode([self.snapshot(unit, amount)]),
                            Ratio=amount, UpperTolerance=0, LowerTolerance=0,
                            IsSubstitute=1, MainDetailId=main_id, Order=position,
                            VersionId=version['bom_version_id'], MBomId=order['bom_id'])
                        position += 1

    def validate(self):
        checks = []
        def check(condition, name):
            require(condition, name)
            checks.append(name)

        for table, ids in self.ids.items():
            rows = self.all(f'SELECT Id FROM `{table}` WHERE CreateUserNo=%s', (self.batch,))
            check({r['Id'] for r in rows} == set(ids), f'{table}: 插入ID及批次一致')
        versions = self.all('''SELECT v.*, b.IsMain AS BomMain,b.Status AS BomStatus,
            b.MBomId,b.UnitInfo,b.TotalQuantity,b.UnitConversionId,
            o.ProductId,o.SpecificationId,bh.ProductId AS BomProductId,
            bh.ProductSpecificationId AS BomSpecificationId
            FROM Technical_CraftOrderVersion v JOIN Technical_CraftOrder o ON o.Id=v.CraftOrderId
            JOIN Production_MBom_Version b ON b.Id=v.BomVersionId
            JOIN Production_MBom bh ON bh.Id=b.MBomId WHERE v.CreateUserNo=%s''', (self.batch,))
        check(len(versions) == 21 and len({v['BomVersionId'] for v in versions}) == 21,
              '21个工艺版本与21个BOM版本一一对应')
        check(Counter(v['Status'] for v in versions) == {0: 7, 2: 8, 3: 6}, '状态分布：暂存7/发布8/废弃6')
        for order in self.orders.values():
            children = [v for v in versions if v['CraftOrderId'] == order['id']]
            check(sum(v['IsMain'] for v in children) == 1, order['code'] + ': 唯一主版本')
        check(all(v['IsMain'] == v['BomMain'] and (1 if v['Status'] == 0 else v['Status']) == v['BomStatus']
                  and v['ProductId'] == v['BomProductId'] and v['SpecificationId'] == v['BomSpecificationId']
                  for v in versions), 'BOM与工艺状态、主版本、产品规格匹配')
        for version in versions:
            units = json.loads(version['UnitInfo'])
            check(version['TotalQuantity'] == 1 and version['UnitConversionId'] == -1
                  and all(Decimal(str(u['Ratio'])) * Decimal(str(u['Quantity'])) == 1 for u in units),
                  f"V{version['Id']}: BOM单位比例有效")
        nodes = self.all('SELECT * FROM Technical_NodeSettingTemplate WHERE CreateUserNo=%s', (self.batch,))
        for version in self.versions.values():
            route = sorted((n for n in nodes if n['CraftOrderVersionId'] == version['id']), key=lambda n: n['Sequence'])
            check(len(route) == version['process_count'] and [n['Sequence'] for n in route] == list(range(1, len(route) + 1)),
                  f"{version['key']}-V{version['number']}: 工序顺序完整")
            check(all(not n['IsOutsource'] or i > 0 and route[i-1]['IsInbound'] for i, n in enumerate(route)),
                  f"{version['key']}-V{version['number']}: 外协前入库")
            if version['key'] != 'E':
                check(route[-1]['IsReport'] and (route[-1]['IsInbound'] or route[-1]['IsOutsource']),
                      f"{version['key']}-V{version['number']}: 末工序满足发布条件")
            wf_nodes = self.all('SELECT Id,NodeTypeId FROM WF_NodeTemplate WHERE WorkflowTemplateId=%s', (version['workflow_id'],))
            links = self.all('SELECT PreviousId,NextId FROM WF_NodeTemplateLink WHERE WorkflowTemplateId=%s', (version['workflow_id'],))
            next_nodes = {l['PreviousId']: l['NextId'] for l in links}
            current = next(n['Id'] for n in wf_nodes if n['NodeTypeId'] == 1)
            visited = []
            while current not in visited:
                visited.append(current)
                if current not in next_nodes:
                    break
                current = next_nodes[current]
            check(len(visited) == len(wf_nodes) == len(route) + 2 and len(links) == len(wf_nodes) - 1
                  and next(n['NodeTypeId'] for n in wf_nodes if n['Id'] == visited[-1]) == 3,
                  f"{version['key']}-V{version['number']}: 工作流起止节点及连接完整")
        bad_machines = self.all('''SELECT m.Id FROM Technical_NodeSettingTemplate_Machine_RL m
            JOIN Technical_NodeSettingTemplate n ON n.Id=m.NodeSettingTemplateId
            LEFT JOIN Base_Machine_Craft_RL r ON r.MachineId=m.MachineId AND r.CraftId=n.CraftId
            WHERE m.CreateUserNo=%s AND (r.MachineId IS NULL OR m.WorkHours<=0)''', (self.batch,))
        check(not bad_machines, '设备属于对应加工类别，设备工时有效')
        references = self.all('''SELECT m.Id,v.Id AS RefId,v.Status,o.ProductId,m.DataId,n.CraftOrderVersionId
            FROM Technical_NodeSettingTemplate_Material_RL m
            JOIN Technical_NodeSettingTemplate n ON n.Id=m.NodeSettingTemplateId
            JOIN Technical_CraftOrderVersion v ON v.Id=m.RefCraftOrderVersionId
            JOIN Technical_CraftOrder o ON o.Id=v.CraftOrderId WHERE m.CreateUserNo=%s''', (self.batch,))
        check(len(references) == 16 and all(r['Status'] == 2 and r['DataId'] == r['ProductId'] for r in references),
              '16个工艺引用均为同产品的已发布版本')
        graph = defaultdict(set)
        for r in references:
            graph[r['CraftOrderVersionId']].add(r['RefId'])
        def visit(node, path):
            require(node not in path, 'Circular version reference')
            for child in graph[node]:
                visit(child, path | {node})
        for version in versions:
            visit(version['Id'], set())
        checks.append('多级工艺引用无循环')
        totals = self.aggregate()
        bom_rows = self.all('SELECT * FROM Production_MBom_Detail WHERE CreateUserNo=%s', (self.batch,))
        for version in self.versions.values():
            details = [d for d in bom_rows if d['VersionId'] == version['bom_version_id']]
            mains = [d for d in details if not d['IsSubstitute']]
            expected = totals[version['id']]
            check(len(mains) == len(expected) and all(
                d['Quantity'] == expected[d['DetailType'], d['DetailId']]['quantity'] and d['Ratio'] == d['Quantity']
                for d in mains), f"{version['key']}-V{version['number']}: BOM只汇总领用物料")
            for main in mains:
                actual_subs = {d['DetailId']: d['Quantity'] for d in details if d['MainDetailId'] == main['Id'] and d['IsSubstitute']}
                expected_subs = {product: amount for (product, unit), amount
                                 in expected[main['DetailType'], main['DetailId']]['substitutes'].items()}
                check(actual_subs == expected_subs, f"BOM明细{main['Id']}: 替料关联及用量匹配")
        return checks

    def cleanup_sql(self):
        # Kept as a review-only rollback script. Never disables FK enforcement.
        lines = ['-- 仅对应本批测试数据；测试后产生新引用时应先人工核对。',
                 '-- 默认 ROLLBACK；确认查询与删除范围后才能改为 COMMIT。', 'START TRANSACTION;']
        def remove(table, extra=''):
            ids = ','.join(map(str, self.ids[table]))
            lines.append(f"DELETE FROM `{table}` WHERE Id IN ({ids}) AND CreateUserNo='{self.batch}'{extra};")
        material = 'Technical_NodeSettingTemplate_Material_RL'
        remove(material, ' AND InsteadSourceId IS NOT NULL')
        remove(material)
        remove('Technical_NodeSettingTemplate_Machine_RL')
        remove('Technical_NodeSettingTemplate')
        remove('Production_MBom_Detail', ' AND IsSubstitute=1')
        remove('Production_MBom_Detail')
        remove('Technical_CraftOrderVersion')
        remove('Production_MBom_Version')
        remove('Production_MBom')
        remove('Technical_CraftOrder')
        wfids = ','.join(map(str, self.ids['WF_WorkflowTemplate']))
        lines.append(f'DELETE FROM WF_NodeTemplateLink WHERE WorkflowTemplateId IN ({wfids});')
        remove('WF_NodeTemplate')
        remove('WF_WorkflowTemplate')
        machineids = ','.join(map(str, self.ids['Base_Machine']))
        craftids = ','.join(map(str, self.ids['Base_Craft']))
        lines.append(f'DELETE FROM Base_Machine_Craft_RL WHERE MachineId IN ({machineids}) AND CraftId IN ({craftids});')
        for table in ['Base_Machine', 'Base_Craft', 'Base_UnitConversion', 'Base_ProductSpecification', 'Base_Product']:
            remove(table)
        lines.append('ROLLBACK;')
        return '\n'.join(lines) + '\n'


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--host', required=True)
    parser.add_argument('--port', type=int, default=8306)
    parser.add_argument('--user', required=True)
    parser.add_argument('--database', required=True)
    parser.add_argument('--batch', default='KZ-CV-' + datetime.now().strftime('%Y%m%d'))
    parser.add_argument('--apply', action='store_true')
    args = parser.parse_args()
    require(args.database == 'test_standard', 'This fixture is limited to test_standard')
    require(bool(re.fullmatch(r'KZ-CV-[0-9A-Z-]{1,18}', args.batch)), 'Invalid batch name')
    output = Path(__file__).resolve().parents[1] / 'data' / args.batch
    manifest_path = output / ('manifest.json' if args.apply else 'dry-run.json')
    require(not (output / 'manifest.json').exists(), 'A committed batch manifest already exists')
    password = os.environ.get('CRAFT_DB_PASSWORD') or getpass.getpass('Database password: ')
    connection = pymysql.connect(host=args.host, port=args.port, user=args.user,
        password=password, database=args.database, charset='utf8mb4',
        autocommit=False, cursorclass=pymysql.cursors.DictCursor,
        connect_timeout=10, read_timeout=45, write_timeout=30)
    try:
        with connection.cursor() as cursor:
            fixture = Fixture(cursor, args.batch)
            fixture.preflight()
            fixture.create_catalog()
            fixture.create_versions()
            fixture.create_routes()
            fixture.create_bom_details()
            checks = fixture.validate()
            manifest = dict(batch=args.batch, database=args.database, committed=False,
                created_at=datetime.now(), ids=fixture.ids, relations=fixture.relations,
                counts={**{t: len(ids) for t, ids in fixture.ids.items()},
                        **{t: len(rows) for t, rows in fixture.relations.items()}},
                orders=fixture.orders, checks=checks)
            output.mkdir(parents=True, exist_ok=True)
            # Persist IDs before commit so even an interrupted run remains traceable.
            manifest_path.write_text(encode(manifest), encoding='utf-8')
            if args.apply:
                (output / 'cleanup-review.sql').write_text(fixture.cleanup_sql(), encoding='utf-8')
                connection.commit()
                manifest['committed'] = True
                manifest_path.write_text(encode(manifest), encoding='utf-8')
            else:
                connection.rollback()
            print(encode(dict(batch=args.batch, committed=manifest['committed'],
                counts=manifest['counts'], passed_checks=len(checks), manifest=str(manifest_path))))
    except Exception:
        connection.rollback()
        raise
    finally:
        connection.close()


if __name__ == '__main__':
    main()
