<template>
    <TreeGrid ref="treeGrid" style="flex: 1; min-height: 0; height: 0; overflow: hidden" :data="dataList"
        idField="id" treeField="name" :columnResizing="true" :frozenWidth="220"
        :selection="selection" selectionMode="single" @rowExpand="$emit('expand', $event)"
        @rowCollapse="$emit('collapse', $event)" @selectionChange="$emit('select', $event)">
        <GridColumn field="name" title="结构 / 名称" width="220" :frozen="true">
            <template #body="{ row }">
                <span :title="requirements[row.id]?.source">{{ row.name }}</span>
            </template>
        </GridColumn>
        <GridColumn field="code" title="编码" width="100" align="left">
            <template #body="{ row }"><span :title="quantityTip(row.source, 'code')">{{
                cellValue(row.source, 'code')
                    }}</span></template>
        </GridColumn>
        <GridColumn field="specification" title="规格 / 型号" width="110" align="left">
            <template #body="{ row }">
                <span :title="quantityTip(row.source, 'specification')">{{ cellValue(row.source,
                    'specification') }}</span>
            </template>
        </GridColumn>
        <GridColumn field="unit" title="单位" width="55" align="center">
            <template #body="{ row }"><span :title="quantityTip(row.source, 'unit')">{{
                cellValue(row.source, 'unit')
                    }}</span></template>
        </GridColumn>
        <GridColumn field="gross" title="总需求量" width="85" align="right">
            <template #body="{ row }"><span :title="quantityTip(row.source, 'gross')">{{
                cellValue(row.source, 'gross')
                    }}</span></template>
        </GridColumn>
        <GridColumn field="stock" title="库存总量" width="85" align="right">
            <template #body="{ row }"><span :title="quantityTip(row.source, 'stock')">{{
                cellValue(row.source, 'stock') }}</span></template>
        </GridColumn>
        <GridColumn field="occupied" title="占用量" width="75" align="right">
            <template #body="{ row }"><span :title="quantityTip(row.source, 'occupied')">{{
                cellValue(row.source, 'occupied') }}</span></template>
        </GridColumn>
        <GridColumn field="transit" title="在途量" width="75" align="right">
            <template #body="{ row }"><span :title="quantityTip(row.source, 'transit')">{{
                cellValue(row.source, 'transit')
                    }}</span></template>
        </GridColumn>
        <GridColumn field="inProduction" title="在制量" width="75" align="right">
            <template #body="{ row }">
                <span :title="quantityTip(row.source, 'inProduction')">{{
                    cellValue(row.source, 'inProduction')
                    }}</span>
            </template>
        </GridColumn>
        <GridColumn field="scheduledTotal" title="已排产量" width="90" align="right">
            <template #body="{ row }">
                <span v-if="row.kind == 'semi'" :title="quantityTip(row.source, 'scheduledTotal')">{{
                    cellValue(row.source, 'scheduledTotal')
                    }}</span>
                <span v-else>-</span>
            </template>
        </GridColumn>
        <GridColumn field="net" title="缺量" width="75" align="right">
            <template #body="{ row }"><span :style="{ color: shortage(row.source) > 0 ? 'var(--color-red)' : undefined }"
                    :title="quantityTip(row.source, 'net')">{{ cellValue(row.source, 'net')
                    }}</span></template>
        </GridColumn>
        <GridColumn field="status" title="状态" width="95" align="center">
            <template #body="scope">
                <span class="kingzon-tag" :class="statusClass(scope.row.source)">{{ requirements[scope.row.id].status }}</span>
            </template>
        </GridColumn>
        <GridColumn field="operation" title="操作" width="95" align="center">
            <template #body="scope">
                <LinkButton v-if="scope.row.kind === 'semi'" plain @click.stop="$emit('select', scope.row)">排产详情</LinkButton>
                <LinkButton v-else plain @click.stop="$emit('material', scope.row.source)">物料详情</LinkButton>
            </template>
        </GridColumn>
    </TreeGrid>
</template>

<script lang="ts">
import { defineComponent, type PropType } from 'vue'
import type { ScheduleNodeDto, ScheduleTreeRowDto, MaterialRequirementDto, PlanContextDto } from '@/types/production-schedule'
import { materialCellValue, materialQuantityTip, materialStatusClass } from '../schedule'

export default defineComponent({
    name: 'ScheduleMaterialTree',
    props: {
        dataList: { type: Array as PropType<ScheduleTreeRowDto[]>, required: true },
        selection: { type: Object as PropType<ScheduleTreeRowDto | null>, default: null },
        requirements: { type: Object as PropType<Record<string, MaterialRequirementDto>>, required: true },
        context: { type: Object as PropType<PlanContextDto>, required: true }
    },
    emits: ['select', 'expand', 'collapse', 'material'],
    methods: {
        cellValue(node: ScheduleNodeDto, key: string): string | number {
            return materialCellValue(node, key, this.requirements, this.context)
        },
        quantityTip(node: ScheduleNodeDto, key: string): string {
            return materialQuantityTip(node, key, this.requirements)
        },
        statusClass(node: ScheduleNodeDto): string {
            return materialStatusClass(node, this.requirements)
        },
        shortage(node: ScheduleNodeDto): number {
            return this.requirements[node.id]?.net ?? 0
        }
    }
})
</script>
