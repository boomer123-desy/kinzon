<template>
    <div class="schedule-order-panel" style="display: flex; flex-direction: column; min-height: 0; overflow: hidden"
        :style="{ flex: selectedOrder ? '1 1 0' : '0 0 auto' }">
        <div class="schedule-order-heading">
            <div class="schedule-order-identity">
                <strong :title="node.name">{{ node.name }}</strong>
                <span class="kingzon-text-grey" :title="`${node.code} · ${node.specification}`">{{ node.code }} · {{ node.specification }}</span>
            </div>
            <LinkButton plain style="flex-shrink: 0" @click="$emit('openGantt', node.id)">查看甘特图</LinkButton>
        </div>
        <div class="schedule-order-heading">
            <strong>排产单列表 <small>{{ node.orders.length }} 张</small></strong>
            <div style="display: flex; align-items: center; gap: 4px; flex-shrink: 0">
                <LinkButton plain @click="listExpanded = !listExpanded">{{ listExpanded ? '收起' : '展开' }}</LinkButton>
                <LinkButton iconCls="icon-add" :disabled="readOnly || hasDraft || !shortage" @click="$emit('add')">新增排产单</LinkButton>
            </div>
        </div>
        <KingZonDataGrid v-if="listExpanded && dataList.length" ref="dataGrid" id="productionScheduleOrderGrid"
            idField="id" :data="dataList" :style="{ height: `${listHeight}px`, flexShrink: 0 }"
            :showFieldFilter="false" :showDownLoad="false" :pagination="false" :fitColumns="true" :rowHeight="58"
            :lazy="false" :virtualScroll="false" :columnMoving="false" :multiSort="false" selectionMode="single"
            :selection="selectedRow" @selectionChange="$emit('select', $event)">
            <GridColumnGroup>
                <GridHeaderRow>
                    <GridColumn field="id" title="排产单 / 数量" :width="108">
                        <template #body="scope">
                            <div class="schedule-order-cell" :title="scope.row.id">
                                <div class="kingzon-textEllipsis">{{ scope.rowIndex + 1 }}. {{ scope.row.draft ? '新增（未保存）' : scope.row.id }}</div>
                                <div class="kingzon-text-grey">{{ scope.row.draft ? '待配置' : `${scope.row.quantity} ${node.unit}` }}</div>
                            </div>
                        </template>
                    </GridColumn>
                    <GridColumn field="start" title="计划时间" :width="160">
                        <template #body="scope">
                            <div v-if="!scope.row.draft" class="schedule-order-cell">
                                <div>开始 {{ formatDate(scope.row.start) }}</div>
                                <div>完成 {{ formatDate(scope.row.end) }}</div>
                            </div>
                            <span v-else class="kingzon-text-grey">请填写排产信息</span>
                        </template>
                    </GridColumn>
                    <GridColumn field="status" title="状态" :width="70" align="center">
                        <template #body="scope">
                            <LinkButton v-if="scope.row.draft" plain @click.stop="$emit('cancelDraft', scope.row.id)">取消</LinkButton>
                            <span v-else class="kingzon-tag"
                                :class="{ 'kingzon-tag-info': scope.row.status === ProductionScheduleStatus.已撤销 }">
                                {{ statusText(scope.row.status) }}
                            </span>
                        </template>
                    </GridColumn>
                </GridHeaderRow>
            </GridColumnGroup>
        </KingZonDataGrid>
        <div v-if="!listExpanded && selectedRow" class="kingzon-text-grey" style="padding: 4px 0; overflow-wrap: anywhere">
            当前选中：{{ selectedRow.draft ? '新增（未保存）' : selectedRow.id }}
        </div>
        <div v-if="listExpanded && !dataList.length" class="kingzon-text-grey" style="padding: 8px; text-align: center">
            {{ readOnly ? '排产单查询与保存接口尚未接入' : '暂无排产单，点击右上角新增' }}
        </div>
        <div v-if="!shortage" class="kingzon-text-grey" style="padding: 4px 0">当前需求已满足</div>
        <div v-if="selectedOrder" style="flex: 1; min-height: 0; overflow: auto; padding-top: 8px">
            <div style="display: flex; align-items: center; justify-content: space-between; gap: 8px">
                <strong>{{ selectedOrder.id }} · 排产详情</strong>
                <LinkButton
                    v-if="selectedOrder.status !== ProductionScheduleStatus.在制 && selectedOrder.status !== ProductionScheduleStatus.已撤销"
                    plain :disabled="readOnly" @click="$emit('cancelOrder', selectedOrder.id)">撤销排产</LinkButton>
            </div>
            <p>数量 {{ selectedOrder.quantity }} {{ node.unit }} · 需求版本 R{{ selectedOrder.snapshot?.revision ?? '—' }}</p>
            <p>{{ formatDate(selectedOrder.start) }} 至 {{ formatDate(selectedOrder.end) }}</p>
            <p v-if="selectedOrder.details?.remark">备注：{{ selectedOrder.details.remark }}</p>
            <KingZonDataGrid v-if="selectedOrder.details?.processes.length" id="productionScheduleProcessGrid"
                idField="code" :data="selectedOrder.details.processes" :pagination="false" :lazy="false"
                :virtualScroll="false" :showFieldFilter="false" :showDownLoad="false" :columnMoving="false" :fitColumns="true"
                selectionMode="null"
                :style="{ height: `${Math.min(260, 65 + selectedOrder.details.processes.length * 34)}px` }">
                <GridColumn field="name" title="工序" :width="130">
                    <template #body="scope">{{ scope.row.code }} {{ scope.row.name }}</template>
                </GridColumn>
                <GridColumn field="start" title="开始" :width="145">
                    <template #body="scope">{{ formatDate(scope.row.start) }}</template>
                </GridColumn>
                <GridColumn field="end" title="完成" :width="145">
                    <template #body="scope">{{ formatDate(scope.row.end) }}</template>
                </GridColumn>
                <GridColumn field="supplier" title="资源 / 供应商" :width="170">
                    <template #body="scope">
                        {{scope.row.outsource ? scope.row.supplier : scope.row.dispatches.map(row => row.machine + '/' +
                            row.operator).join('、')}}
                    </template>
                </GridColumn>
            </KingZonDataGrid>
            <p v-else class="kingzon-text-grey">该历史排产单未设置工序明细</p>
        </div>
    </div>
</template>

<script lang="ts">
import { ProductionScheduleStatus } from '@/types/enums'
import { scheduleStatusText } from '../schedule'
import { defineComponent, type PropType } from 'vue'
import type { ScheduleNodeDto, ScheduleOrderDto, ScheduleListRowDto } from '@/types/schedule'

export default defineComponent({
    name: 'ScheduleOrderList',
    props: {
        node: { type: Object as PropType<ScheduleNodeDto>, required: true },
        dataList: { type: Array as PropType<ScheduleListRowDto[]>, required: true },
        selectedRow: { type: Object as PropType<ScheduleListRowDto | null>, default: null },
        selectedOrder: { type: Object as PropType<ScheduleOrderDto>, default: undefined },
        hasDraft: { type: Boolean, default: false },
        readOnly: { type: Boolean, default: false },
        shortage: { type: Number, default: 0 }
    },
    emits: ['add', 'select', 'cancelDraft', 'cancelOrder', 'openGantt'],
    data() { return { listExpanded: true } },
    watch: {
        'node.id'() { this.listExpanded = true }
    },
    computed: {
        listHeight(): number { return 34 + Math.min(3, this.dataList.length) * 58 },
        ProductionScheduleStatus() { return ProductionScheduleStatus }
    },
    methods: {
        statusText(value: ProductionScheduleStatus): string {
            return scheduleStatusText(value)
        },
        formatDate(value: string): string {
            return this.$utils.formatDateTime(value, 'yyyy-MM-dd HH:mm')
        }
    }
})
</script>

<style scoped>
.schedule-order-panel { padding: 6px 8px; border: 1px solid #e5e7eb; background: #fff; box-sizing: border-box; }
.schedule-order-heading { display: flex; align-items: center; justify-content: space-between; gap: 6px; padding: 3px 0; }
.schedule-order-identity { flex: 1; display: flex; flex-direction: column; gap: 2px; min-width: 0; }
.schedule-order-identity > * { overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
.schedule-order-cell { min-width: 0; line-height: 18px; font-size: 12px; white-space: normal; overflow-wrap: anywhere; }
</style>
