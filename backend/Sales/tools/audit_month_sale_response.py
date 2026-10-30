"""Read-only comparison of a month-report JSON response with legacy DB tables."""
import argparse
from collections import defaultdict
from datetime import datetime, timedelta
from decimal import Decimal, ROUND_HALF_UP
import getpass
import json
from pathlib import Path

import pymysql


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--response', required=True)
    parser.add_argument('--host', required=True)
    parser.add_argument('--port', type=int, default=3306)
    parser.add_argument('--user', required=True)
    parser.add_argument('--database', required=True)
    parser.add_argument('--pending-status', type=int, default=0)
    args = parser.parse_args()
    response = json.loads(Path(args.response).read_text(encoding='utf-8-sig'), parse_float=Decimal)
    start = datetime.fromisoformat(response['startDate'])
    end = datetime.fromisoformat(response['endDate']) + timedelta(days=1)
    last_start, last_end = start.replace(year=start.year - 1), end.replace(year=end.year - 1)
    january_start = start.replace(month=1)
    january_end = january_start.replace(month=2)
    weeks = response['weeks']
    dates = [(datetime.fromisoformat(week['startDate']), datetime.fromisoformat(week['endDate']) + timedelta(days=1)) for week in weeks]
    assert dates[0][0] == start and dates[-1][1] == end
    assert all(dates[index][0] == dates[index - 1][1] for index in range(1, len(dates)))
    assert all(week['weekNo'] == index + 1 and week['days'] == (dates[index][1] - dates[index][0]).days for index, week in enumerate(weeks))
    differences = []
    checks = 0

    def compare(path, actual, expected):
        nonlocal checks
        checks += 1
        if actual != expected:
            differences.append({'field': path, 'response': actual, 'database': expected})

    fields = ['orderAmount', 'deliveryAmount', 'returnAmount', 'netDeliveryAmount',
        'januaryNetDeliveryAmount', 'lastYearOrderAmount', 'yearOnYearRate', 'deliveryShareRate']
    amount_fields = fields[:6]

    def blank():
        return {**{field: Decimal(0) for field in amount_fields}, 'orderWeeks': [Decimal(0)] * len(weeks),
            'deliveryWeeks': [Decimal(0)] * len(weeks)}

    def rounded(value):
        return value.quantize(Decimal('0.000001'), rounding=ROUND_HALF_UP)

    def complete(row, total):
        row['netDeliveryAmount'] = row['deliveryAmount'] - row['returnAmount']
        baseline = row['lastYearOrderAmount']
        row['yearOnYearRate'] = None if baseline == 0 else rounded((row['deliveryAmount'] - baseline) / abs(baseline))
        row['deliveryShareRate'] = None if total == 0 else rounded(row['deliveryAmount'] / total)

    customers = {}
    connection = pymysql.connect(host=args.host, port=args.port, user=args.user,
        password=getpass.getpass('Database password: '), database=args.database,
        charset='utf8mb4', connect_timeout=8, read_timeout=30, autocommit=False,
        cursorclass=pymysql.cursors.DictCursor)
    sql_record = []
    try:
        with connection.cursor() as cursor:
            cursor.execute('SET SESSION TRANSACTION ISOLATION LEVEL REPEATABLE READ')
            cursor.execute('SET SESSION TRANSACTION READ ONLY')
            cursor.execute('START TRANSACTION WITH CONSISTENT SNAPSHOT, READ ONLY')
            order_parts = ['SUM(CASE WHEN o.CreateTime >= %s AND o.CreateTime < %s THEN o.OrderTotalPrice ELSE 0 END) AS orderAmount',
                'SUM(CASE WHEN o.CreateTime >= %s AND o.CreateTime < %s THEN o.OrderTotalPrice ELSE 0 END) AS lastYearOrderAmount']
            order_params = [start, end, last_start, last_end]
            for index, (week_start, week_end) in enumerate(dates):
                order_parts.append(f'SUM(CASE WHEN o.CreateTime >= %s AND o.CreateTime < %s THEN o.OrderTotalPrice ELSE 0 END) AS orderWeek{index + 1}')
                order_params.extend([week_start, week_end])
            order_sql = '''SELECT p.Id AS customerId,p.CategoryId AS categoryId,p.CustomerName AS customerName,
                COALESCE(NULLIF(c.Name,''),'未分类') AS categoryName,''' + ','.join(order_parts) + '''
                FROM SaleOrder o JOIN Base_Partner p ON p.Id=o.CustomerId
                LEFT JOIN Base_Category c ON c.Id=p.CategoryId
                WHERE p.IsDeleted=0 AND o.Status<>%s
                AND ((o.CreateTime >= %s AND o.CreateTime < %s) OR (o.CreateTime >= %s AND o.CreateTime < %s))
                GROUP BY p.Id,p.CategoryId,p.CustomerName,c.Name'''
            order_params.extend([args.pending_status, start, end, last_start, last_end])
            cursor.execute(order_sql, order_params)
            for source in cursor.fetchall():
                row = blank()
                row.update({key: source[key] for key in ['customerId', 'categoryId', 'customerName', 'categoryName', 'orderAmount', 'lastYearOrderAmount']})
                row['orderWeeks'] = [source[f'orderWeek{index + 1}'] for index in range(len(weeks))]
                customers[source['customerId']] = row
            sql_record.append({'sql': order_sql, 'params': order_params})
            delivery_parts = ['SUM(CASE WHEN o.DeliveryTime >= %s AND o.DeliveryTime < %s THEN d.SalePrice*d.DeliveryQuantity ELSE 0 END) AS deliveryAmount',
                'SUM(CASE WHEN o.DeliveryTime >= %s AND o.DeliveryTime < %s THEN d.SalePrice*d.ReturnQuantity ELSE 0 END) AS returnAmount',
                'SUM(CASE WHEN o.DeliveryTime >= %s AND o.DeliveryTime < %s THEN d.SalePrice*(d.DeliveryQuantity-d.ReturnQuantity) ELSE 0 END) AS januaryNetDeliveryAmount']
            delivery_params = [start, end, start, end, january_start, january_end]
            for index, (week_start, week_end) in enumerate(dates):
                delivery_parts.append(f'SUM(CASE WHEN o.DeliveryTime >= %s AND o.DeliveryTime < %s THEN d.SalePrice*d.DeliveryQuantity ELSE 0 END) AS deliveryWeek{index + 1}')
                delivery_params.extend([week_start, week_end])
            delivery_sql = '''SELECT p.Id AS customerId,p.CategoryId AS categoryId,p.CustomerName AS customerName,
                COALESCE(NULLIF(c.Name,''),'未分类') AS categoryName,''' + ','.join(delivery_parts) + '''
                FROM DeliveryOrderDetail d JOIN DeliveryOrder o ON o.Id=d.DeliveryOrderId
                JOIN Base_Partner p ON p.Id=o.CustomerId LEFT JOIN Base_Category c ON c.Id=p.CategoryId
                WHERE p.IsDeleted=0 AND d.IsDeleted=0 AND d.IsOutboundInventory=1
                AND ((o.DeliveryTime >= %s AND o.DeliveryTime < %s) OR (o.DeliveryTime >= %s AND o.DeliveryTime < %s))
                GROUP BY p.Id,p.CategoryId,p.CustomerName,c.Name'''
            delivery_params.extend([start, end, january_start, january_end])
            cursor.execute(delivery_sql, delivery_params)
            for source in cursor.fetchall():
                row = customers.setdefault(source['customerId'], blank())
                row.update({key: source[key] for key in ['customerId', 'categoryId', 'customerName', 'categoryName', 'deliveryAmount', 'returnAmount', 'januaryNetDeliveryAmount']})
                row['deliveryWeeks'] = [source[f'deliveryWeek{index + 1}'] for index in range(len(weeks))]
            sql_record.append({'sql': delivery_sql, 'params': delivery_params})
            cursor.execute('SELECT COUNT(*) AS n FROM SaleOrder WHERE Status=%s AND CreateTime >= %s AND CreateTime < %s',
                (args.pending_status, last_start, end))
            pending_count = cursor.fetchone()['n']
    finally:
        connection.rollback()
        connection.close()
    db_categories = defaultdict(list)
    for row in customers.values():
        db_categories[row['categoryId']].append(row)
    total = blank()
    categories = {}

    def add(target, source):
        for field in amount_fields:
            if field != 'netDeliveryAmount':
                target[field] += source[field]
        for name in ['orderWeeks', 'deliveryWeeks']:
            target[name] = [left + right for left, right in zip(target[name], source[name])]

    for key, rows in db_categories.items():
        category = blank()
        category['categoryName'] = rows[0]['categoryName']
        for row in rows:
            add(category, row)
            add(total, row)
        categories[key] = category
    complete(total, total['deliveryAmount'])
    for row in [*customers.values(), *categories.values()]:
        complete(row, total['deliveryAmount'])

    def compare_stat(path, api, db):
        for field in fields:
            compare(path + '.' + field, api.get(field), db[field])
        for name in ['orderWeeks', 'deliveryWeeks']:
            values = {week['weekNo']: week['amount'] for week in api[name]}
            compare(path + '.' + name + '.weekNumbers', sorted(values), list(range(1, len(weeks) + 1)))
            for index, amount in enumerate(db[name]):
                compare(f'{path}.{name}[{index + 1}].amount', values.get(index + 1), amount)
            compare(path + '.' + name + '.sum', sum(values.values()), api['orderAmount' if name == 'orderWeeks' else 'deliveryAmount'])

    compare_stat('total', response, total)
    compare('categories.ids', sorted(category['categoryId'] for category in response['categories']), sorted(categories))
    seen = set()
    for api_category in response['categories']:
        category_id = api_category['categoryId']
        path = f'category[{category_id}]'
        if category_id not in categories:
            continue
        compare_stat(path, api_category, categories[category_id])
        compare(path + '.name', api_category['categoryName'], categories[category_id]['categoryName'])
        expected_ids = sorted(row['customerId'] for row in db_categories[category_id])
        compare(path + '.customerIds', sorted(row['customerId'] for row in api_category['customers']), expected_ids)
        compare(path + '.ranks', sorted(row['rank'] for row in api_category['customers']), list(range(1, len(expected_ids) + 1)))
        ranked = sorted(api_category['customers'], key=lambda row: row['rank'])
        compare(path + '.rankingAmounts', [row['januaryNetDeliveryAmount'] for row in ranked],
            sorted((row['januaryNetDeliveryAmount'] for row in db_categories[category_id]), reverse=True))
        for api in api_category['customers']:
            customer_id = api['customerId']
            compare(f'customer[{customer_id}].duplicate', customer_id in seen, False)
            seen.add(customer_id)
            if customer_id not in customers:
                continue
            db = customers[customer_id]
            compare_stat(f'customer[{customer_id}]', api, db)
            compare(f'customer[{customer_id}].name', api['customerName'], db['customerName'])
            compare(f'customer[{customer_id}].categoryId', category_id, db['categoryId'])
    compare('customers.ids', sorted(seen), sorted(customers))
    output = Path(__file__).resolve().parents[1] / 'data' / ('audit-' + datetime.now().strftime('%Y%m%d-%H%M%S'))
    output.mkdir(parents=True, exist_ok=False)
    audit = {'database': args.database, 'audited_at': datetime.now().isoformat(), 'period_start': start,
        'period_end_exclusive': end, 'checks': checks, 'difference_count': len(differences),
        'customer_count': len(customers), 'category_count': len(categories),
        'pending_status': args.pending_status, 'pending_orders_in_relevant_span': pending_count,
        'totals': {field: {'response': response.get(field), 'database': total[field]} for field in fields},
        'differences': differences, 'queries': sql_record,
        'ranking_scope': 'Contiguous ranks and January amount descending; name ordering within ties depends on host culture.'}
    (output / 'audit.json').write_text(json.dumps(audit, ensure_ascii=False, indent=2, default=str), encoding='utf-8')
    lines = ['# 月报数据核对', '', f'- 数据库：{args.database}',
        f'- 账期：{start:%Y-%m-%d} 至 {(end - timedelta(days=1)):%Y-%m-%d}',
        f'- 客户：{len(customers)}；分类：{len(categories)}；核对项：{checks}；差异：{len(differences)}',
        '- 只读一致性快照查询，未修改数据库。', '', '| 项目 | 接口值 | 数据库值 |', '| --- | ---: | ---: |']
    labels = ['订单总额', '送货总额', '退货总额', '净发货额', '1月净发货额', '去年同期销售额', '同比（小数）', '送货占比（小数）']
    for label, field in zip(labels, fields):
        lines.append(f'| {label} | {response.get(field)} | {total[field]} |')
    if differences:
        lines += ['', '## 差异', '']
        lines += [f"- {item['field']}：接口 {item['response']}，数据库 {item['database']}" for item in differences]
    (output / '核对结果.md').write_text('\n'.join(lines) + '\n', encoding='utf-8')
    print(json.dumps({'checks': checks, 'difference_count': len(differences), 'customers': len(customers),
        'categories': len(categories), 'totals': audit['totals'], 'differences': differences[:20],
        'report': str(output / '核对结果.md'), 'audit': str(output / 'audit.json')}, ensure_ascii=True, default=str))


if __name__ == '__main__':
    main()
