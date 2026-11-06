<template>
    <div class="schedule-order-panel" :class="{ 'schedule-root-panel': quantityReadOnly }" style="display: flex; flex-direction: column; flex: 1; min-height: 0; overflow: hidden">
        <div style="display: flex; align-items: baseline; flex-wrap: wrap; gap: 4px 8px; min-width: 0; padding: 6px 8px 0">
            <strong style="overflow: hidden; text-overflow: ellipsis; white-space: nowrap" :title="node.name">{{ node.name }}</strong>
            <span class="kingzon-text-grey" style="overflow-wrap: anywhere">{{ node.code }}</span>
            <span v-if="quantityReadOnly && selectedRow" class="kingzon-tag">{{ selectedRow.statusText || statusText(selectedRow.status) }}</span>
        </div>
        <div class="schedule-order-heading">
            <strong>{{ quantityReadOnly ? '顶层排产单' : '排产单' }} <small v-if="!quantityReadOnly">{{ dataList.length }} 张</small></strong>
            <div style="display: flex; align-items: center; gap: 4px; flex-wrap: wrap; justify-content: flex-end">
                <LinkButton plain :disabled="loading" @click="$emit('refresh')">刷新排产单</LinkButton>
                <LinkButton plain @click="$emit('openGantt', node.id)">查看甘特图</LinkButton>
                <LinkButton v-if="selectedRow?.draft && !quantityReadOnly" plain :disabled="busy" @click="$emit('cancelDraft', selectedRow.id)">关闭未保存排产单
                </LinkButton>
                <LinkButton v-if="!quantityReadOnly || !dataList.length" iconCls="icon-add" :disabled="!shortage || !canAdd || busy" @click="$emit('add')">新增排产单</LinkButton>
            </div>
        </div>
        <p v-if="error" style="color: var(--color-red); padding: 0 8px">{{ error }}</p>
        <div v-if="addDisabledReason && !quantityReadOnly" class="kingzon-text-grey" style="padding: 4px 8px">{{ addDisabledReason }}</div>
        <div v-if="!shortage" class="kingzon-text-grey" style="padding: 4px 8px">当前需求已满足</div>
        <div v-if="dataList.length" style="position: relative; flex: 1; min-height: 0; min-width: 0; overflow: hidden">
            <Tabs ref="tabs" :border="!quantityReadOnly" :selectedIndex="selectedIndex" :scrollable="true"
                style="position: absolute; inset: 0; width: 100%; height: 100%" @tabSelect="selectTab">
                <TabPanel v-for="row in dataList" :key="row.id" :title="tabTitle(row)"
                    :panelStyle="{ minHeight: '0', overflow: 'hidden' }"
                    :bodyStyle="{ display: 'flex', flexDirection: 'column', padding: '0', minHeight: '0', overflow: 'hidden' }">
                    <CreateSchedule v-if="row.draft" :key="row.id" :craftVersionId="node.serverMaterial?.craftVersionId ?? craftVersionId"
                        :allowProcessEdit="allowProcessEdit" :saving="savingDraftId === row.id"
                        :initialQuantity="initialQuantity" :quantityReadOnly="quantityReadOnly"
                        style="flex: 1; height: 0; min-height: 0; overflow: hidden"
                        @submit="$emit('submitDraft', row.id, $event)" @cancel="$emit('cancelDraft', row.id)" />
                    <ScheduleSavedDetail v-else-if="savedOrderById[row.id]" :order="savedOrderById[row.id]" :busy="busy"
                        @start="$emit('startOrder', $event)" @delete="$emit('deleteOrder', $event)" />
                    <div v-else-if="orderById[row.id]" style="height: 100%; min-height: 0; overflow: auto; padding: 8px; box-sizing: border-box">
                        <div style="display: flex; align-items: center; justify-content: space-between; gap: 8px">
                            <strong>{{ orderById[row.id].id }} · 排产详情</strong>
                            <LinkButton
                                v-if="orderById[row.id].status !== ProductionScheduleStatus.在制 && orderById[row.id].status !== ProductionScheduleStatus.已撤销"
                                plain :disabled="readOnly" @click="$emit('cancelOrder', orderById[row.id].id)">撤销排产</LinkButton>
                        </div>
                        <p>数量 {{ orderById[row.id].quantity }} {{ node.unit }} · 需求版本 R{{ orderById[row.id].snapshot?.revision ?? '—' }}</p>
                        <p>{{ formatDate(orderById[row.id].start) }} 至 {{ formatDate(orderById[row.id].end) }}</p>
                        <p v-if="orderById[row.id].details?.remark">备注：{{ orderById[row.id].details.remark }}</p>
                        <KingZonDataGrid v-if="orderById[row.id].details?.processes.length" :id="`productionScheduleProcessGrid-${row.id}`"
                            idField="code" :data="orderById[row.id].details.processes" :pagination="false" :lazy="false"
                            :virtualScroll="false" :showFieldFilter="false" :showDownLoad="false" :columnMoving="false"
                            :fitColumns="true" selectionMode="null"
                            :style="{ height: `${Math.min(260, 65 + orderById[row.id].details.processes.length * 34)}px` }">
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
                </TabPanel>
            </Tabs>
        </div>
        <div v-else class="kingzon-text-grey" style="padding: 8px; text-align: center">暂无排产单，点击右上角新增</div>
    </div>
</template>

<script lang="ts">
import CreateSchedule from './create-schedule.vue'
import ScheduleSavedDetail from './schedule-saved-detail.vue'
import { ProductionScheduleStatus } from '@/types/enums'
import { scheduleStatusText } from '../schedule'
import { defineComponent, type PropType } from 'vue'
import type { SavedScheduleDto, ScheduleNodeDto, ScheduleOrderDto, ScheduleListRowDto } from '@/types/schedule'

export default defineComponent({
    name: 'ScheduleOrderList',
    components: { CreateSchedule, ScheduleSavedDetail },
    props: {
        craftVersionId: { type: Number, default: 0 },
        allowProcessEdit: { type: Boolean, default: true },
        savingDraftId: { type: String, default: '' },
        canAdd: { type: Boolean, default: true },
        addDisabledReason: { type: String, default: '' },
        initialQuantity: { type: Number, default: 1 },
        quantityReadOnly: Boolean,
        savedOrders: { type: Array as PropType<SavedScheduleDto[]>, default: () => [] },
        loading: Boolean, busy: Boolean, error: { type: String, default: '' },
        node: { type: Object as PropType<ScheduleNodeDto>, required: true },
        dataList: { type: Array as PropType<ScheduleListRowDto[]>, required: true },
        selectedRow: { type: Object as PropType<ScheduleListRowDto | null>, default: null },
        readOnly: { type: Boolean, default: false },
        shortage: { type: Number, default: 0 }
    },
    emits: ['add', 'select', 'submitDraft', 'cancelDraft', 'cancelOrder', 'openGantt', 'refresh', 'startOrder', 'deleteOrder'],
    watch: {
        selectedIndex() { this.syncSelectedTab() },
        dataList() { this.syncSelectedTab() }
    },
    computed: {
        savedOrderById(): Record<string, SavedScheduleDto> {
            return Object.fromEntries(this.savedOrders.map(order => [`saved-${order.id}`, order]))
        },
        orderById(): Record<string, ScheduleOrderDto> {
            return Object.fromEntries(this.node.orders.map(order => [order.id, order]))
        },
        selectedIndex(): number { return Math.max(0, this.dataList.findIndex(row => row.id === this.selectedRow?.id)) },
        ProductionScheduleStatus() { return ProductionScheduleStatus }
    },
    methods: {
        tabTitle(row: ScheduleListRowDto): string {
            return `${row.draft ? '新增 ' + row.id.replace('draft-', '') : row.title || row.id} · ${row.statusText || this.statusText(row.status)}`
        },
        syncSelectedTab() {
            this.$nextTick(() => {
                (this.$refs.tabs as { select(index: number): void } | undefined)?.select(this.selectedIndex)
            })
        },
        selectTab(tab: { title: string }) {
            const row = this.dataList.find(item => this.tabTitle(item) === tab.title)
            if (row && row.id !== this.selectedRow?.id) this.$emit('select', row)
        },
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
.schedule-order-panel {
    margin: 0 8px 6px;
    padding: 0;
    border: 1px solid #dce1e7;
    border-radius: 4px;
    background: #fff;
    box-sizing: border-box;
}

.schedule-order-heading {
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: 6px;
    flex-wrap: wrap;
    padding: 6px 8px;
}
.schedule-root-panel .schedule-order-heading { padding: 6px 10px 10px; border-bottom: 1px solid #dce1e7; }
.schedule-root-panel { min-width: 0; }
.schedule-root-panel :deep(.tabs-header) { display: none; }
.schedule-root-panel :deep(.tabs-panels) { border: 0; }
</style>
