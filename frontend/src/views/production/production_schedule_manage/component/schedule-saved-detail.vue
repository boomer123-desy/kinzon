<template>
    <div style="height: 100%; overflow: auto; padding: 8px; box-sizing: border-box">
        <Panel :title="order.orderNo + ' · ' + order.statusText" :bodyStyle="{ padding: '8px' }">
            <div class="kingzon-btn-group" style="padding-bottom: 6px">
                <LinkButton v-if="order.canStart" :disabled="busy" @click="$emit('start', order.id)">开始排产</LinkButton>
                <LinkButton v-if="order.canDelete" :disabled="busy" @click="$emit('delete', order.id)">删除排产单</LinkButton>
            </div>
            <div style="display: flex; flex-wrap: wrap; gap: 8px 24px">
                <span>排产数量：{{ order.quantity }} {{ unitName }}</span>
                <span>派工部门：{{ order.departmentName || '—' }}</span>
                <span>加急：{{ order.urgent ? '是' : '否' }}</span>
                <span>计划开始：{{ formatDate(order.start) }}</span>
                <span>计划完成：{{ formatDate(order.end) }}</span>
            </div>
            <div style="padding-top: 8px; white-space: pre-wrap; overflow-wrap: anywhere">备注：{{ order.remark || '—' }}</div>
        </Panel>
        <Panel v-for="process in order.processes" :key="process.id" :title="`${process.order} · ${process.name}`"
            :collapsible="true" :bodyStyle="{ padding: '6px' }" style="margin-top: 8px">
            <div style="display: flex; flex-wrap: wrap; gap: 6px 16px; padding-bottom: 6px">
                <span>工艺：{{ process.craftName }}</span><span>物料：{{ process.materialCount }} 项</span>
                <span>接单：{{ process.reciveQuantity }}</span><span>完工：{{ process.completeQuantity }}</span>
                <span>预计工时：{{ process.planWorkHours }} h</span><span>实际工时：{{ process.actualWorkHours }} h</span>
            </div>
            <KingZonDataGrid :id="`schedule-resources-${process.id}`" :data="process.machines" idField="id"
                :pagination="false" :lazy="false" :virtualScroll="false" :showDownLoad="false" :showFieldFilter="false"
                :fitColumns="true" :style="{ height: `${Math.min(260, 60 + process.machines.length * 32)}px` }">
                <GridColumnGroup><GridHeaderRow>
                    <GridColumn field="machineName" title="设备" width="150" :overflowTextEllipsis="true" />
                    <GridColumn field="userName" title="人员" width="90" />
                    <GridColumn field="quantity" title="分配数量" width="85" align="right" />
                    <GridColumn field="hours" title="预计工时" width="85" align="right" />
                    <GridColumn field="actualWorkHours" title="实际工时" width="85" align="right" />
                </GridHeaderRow></GridColumnGroup>
            </KingZonDataGrid>
            <KingZonDataGrid v-if="process.materials?.length" :id="`schedule-materials-${process.id}`" :data="process.materials" idField="id"
                :pagination="false" :lazy="false" :virtualScroll="false" :showDownLoad="false" :showFieldFilter="false"
                :fitColumns="true" :style="{ height: `${Math.min(260, 60 + process.materials.length * 32)}px`, marginTop: '6px' }">
                <GridColumnGroup><GridHeaderRow>
                    <GridColumn field="code" title="物料编码" width="100" />
                    <GridColumn field="name" title="领用物料" width="150" :overflowTextEllipsis="true" />
                    <GridColumn field="demandQuantity" title="需求数量" width="80" align="right" />
                    <GridColumn field="unitName" title="单位" width="60" />
                    <GridColumn field="baseReceiveQuantity" title="已领（主单位）" width="100" align="right" />
                </GridHeaderRow></GridColumnGroup>
            </KingZonDataGrid>
        </Panel>
        <p v-if="!order.processes.length" class="kingzon-text-grey">该排产单暂无工序记录</p>
    </div>
</template>
<script lang="ts">
import { defineComponent, type PropType } from 'vue'
import type { SavedScheduleDto } from '@/types/schedule'
export default defineComponent({
    name: 'ScheduleSavedDetail',
    props: { order: { type: Object as PropType<SavedScheduleDto>, required: true }, busy: Boolean },
    emits: ['start', 'delete'],
    computed: {
        unitName(): string {
            try { return JSON.parse(this.order.dispatchUnit || '{}').productionUnitName || '' } catch { return '' }
        }
    },
    methods: {
        formatDate(value: string | null): string { return value ? this.$utils.formatDateTime(value, 'yyyy-MM-dd HH:mm') : '—' }
    }
})
</script>
