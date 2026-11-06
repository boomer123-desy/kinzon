<template>
    <Drawer v-if="visible" ref="drawer" title="生产排产甘特图" region="east" :drawerStyle="{ width: '97vw', height: '100vh', zIndex: 9100 }"
        :closable="true" :maskClosable="false" @collapse="close">
        <div class="kingzon-page-content" style="display: flex; flex-direction: column; height: 100%; min-height: 0; padding: 8px; box-sizing: border-box">
            <div style="display: flex; align-items: center; justify-content: space-between; gap: 8px; flex-wrap: wrap; flex-shrink: 0"><strong>{{ selectedNode?.name || '全部半成品' }} · {{ orderCount }} 张排产单</strong>
                <div style="display: flex; align-items: center; flex-wrap: wrap; gap: 8px"><label><CheckBox v-model="operationView" />显示工序</label>
                    <LinkButton v-if="targetId" plain @click="targetId = ''">查看全部</LinkButton>
                    <LinkButton plain @click="changeWindow(-1)">上一月</LinkButton><LinkButton plain @click="monthOffset = 0">计划月份</LinkButton><LinkButton plain @click="changeWindow(1)">下一月</LinkButton>
                    <LinkButton plain @click="zoom(-1)">缩小</LinkButton><LinkButton plain @click="zoom(1)">放大</LinkButton>
                    <LinkButton plain @click="setExpanded(true)">全部展开</LinkButton><LinkButton plain @click="setExpanded(false)">全部收起</LinkButton>
                </div></div>
            <div style="display: flex; align-items: center; justify-content: space-between; gap: 8px; flex-wrap: wrap; margin: 8px 0"><span>{{ monthLabel }} · 默认两个月 · 产品 → 半成品树 → 排产单 → 工序</span>
                <span>拖动条形改期，拖动两端调整时间；Ctrl + 滚轮缩放；底部滚动条移动时间轴</span></div>
            <div v-if="!chartData.tasks.length" style="flex: 1; padding: 24px; text-align: center">当前范围暂无有效排产单</div>
            <SvarChart v-else ref="chart" style="flex: 1; min-height: 0; min-width: 0" :tasks="chartData.tasks" :links="chartData.links" :start="range.start" :end="range.end"
                @change="onTimeChange" @select="selectedTaskId = $event" />
            <div style="display: flex; align-items: center; justify-content: space-between; gap: 8px; flex-shrink: 0; padding-top: 8px"><span v-if="selectedTask" class="kingzon-textEllipsis" style="max-width: 75%">{{ selectedTask.text }} · {{ selectedTask.statusText }} · {{ selectedTask.details }}</span>
                <span v-else>选择树节点查看状态；在制排产不能直接改期</span>
                <div style="display: flex; align-items: center; flex-wrap: wrap; gap: 8px"><LinkButton v-if="canCancelSelected" plain @click="$emit('cancelOrder', selectedTask!.orderId!)">撤销排产</LinkButton><LinkButton @click="close">关闭</LinkButton></div>
            </div>
        </div>
    </Drawer>
</template>
<script lang="ts">
import { ProductionScheduleStatus } from '@/types/enums'
import { defineAsyncComponent, defineComponent, type PropType } from 'vue'
import { flattenTree, timelineData, timelineRange } from '../schedule'
import type { ScheduleNodeDto } from '@/types/schedule'
import type { ScheduleTimeChangeDto, TimelineTaskDto } from '@/types/schedule'
import { ScheduleProductDto, type PlanContextDto } from '@/types/schedule'
const SvarChart = defineAsyncComponent(() => import('./svar-chart.vue'))
interface ChartRef { zoom(direction: number): void; setExpanded(expanded: boolean): void; sync(): void }
export default defineComponent({
    name: 'ScheduleGantt', components: { SvarChart }, props: {
        nodes: { type: Array as PropType<ScheduleNodeDto[]>, required: true }, planStart: { type: String, required: true },
        planEnd: { type: String, required: true }, product: { type: Object as PropType<ScheduleProductDto>, default: () => new ScheduleProductDto({ name: '生产计划' }) }, revision: { type: Number, required: true }, quantity: { type: Number, default: 100 }
    }, emits: { cancelOrder: (id: string) => !!id, reschedule: (change: ScheduleTimeChangeDto) => !!change.orderId },
    data() { return { visible: false, targetId: '', operationView: true, monthOffset: 0, selectedTaskId: '' } },
    computed: {
        context(): PlanContextDto { return { quantity: this.quantity, start: this.planStart, end: this.planEnd, revision: this.revision, product: this.product } },
        selectedNode(): ScheduleNodeDto | undefined { return flattenTree(this.nodes).find(node => node.id === this.targetId) },
        chartData() { return timelineData(this.nodes, this.context, this.targetId, this.operationView) },
        orderCount(): number { return new Set(this.chartData.tasks.filter(task => task.orderId).map(task => task.orderId)).size },
        range(): { start: Date; end: Date } { return timelineRange(this.context, this.chartData.tasks, this.monthOffset) },
        monthLabel(): string { const end = new Date(this.range.end); end.setDate(end.getDate() - 1); return `${this.range.start.getFullYear()}年${this.range.start.getMonth() + 1}月 — ${end.getFullYear()}年${end.getMonth() + 1}月` },
        selectedTask(): TimelineTaskDto | undefined { return this.chartData.tasks.find(task => task.id === this.selectedTaskId) },
        canCancelSelected(): boolean { return !!this.selectedTask?.orderId && flattenTree(this.nodes).flatMap(node => node.orders).some(order => order.id === this.selectedTask?.orderId && order.status !== ProductionScheduleStatus.在制 && order.status !== ProductionScheduleStatus.已撤销) }
    },
    methods: {
        open(targetId = '') { this.targetId = targetId; this.monthOffset = 0; this.selectedTaskId = ''; this.visible = true; this.$nextTick(() => (this.$refs.drawer as EasyUI.Drawer | undefined)?.expand()) },
        close() { this.visible = false; this.selectedTaskId = '' },
        changeWindow(delta: number) { this.monthOffset += delta },
        zoom(direction: number) { (this.$refs.chart as ChartRef | undefined)?.zoom(direction) },
        setExpanded(expanded: boolean) { (this.$refs.chart as ChartRef | undefined)?.setExpanded(expanded) },
        onTimeChange(change: ScheduleTimeChangeDto) { this.$emit('reschedule', change); this.$nextTick(() => (this.$refs.chart as ChartRef | undefined)?.sync()) }
    }
})
</script>
