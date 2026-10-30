<template>
    <div class="svar-chart" style="width: 100%; height: 100%; overflow: hidden"><Willow :fonts="false">
        <Gantt :tasks="inputTasks" :links="links" :columns="columns" :start="start" :end="end" :init="init"
            :gridWidth="420" :cellHeight="38" :scaleHeight="28" :cellWidth="28" :scales="scales" :zoom="zoomConfig" :taskTemplate="taskTemplate" durationUnit="hour" lengthUnit="hour" />
    </Willow></div>
</template>
<script lang="ts">
import { defineComponent, h, markRaw, type PropType } from 'vue'
import { Gantt, Willow, type IApi } from '@svar-ui/vue-gantt'
import { localDateTime } from '../schedule'
import type { TimelineTaskDto, TimelineDataDto, ScheduleTimeChangeDto } from '@/types/production-schedule'
const TimelineBar = defineComponent({
    inheritAttrs: false, props: { data: { type: Object as PropType<TimelineTaskDto>, required: true } },
    setup(props) { return () => h('div', { class: 'timeline-bar',
        title: `${props.data.text} · ${props.data.statusText} · ${localDateTime(props.data.start).replace('T', ' ')} 至 ${localDateTime(props.data.end).replace('T', ' ')} · ${props.data.details ?? ''}` }, props.data.text) }
})
export default defineComponent({
    name: 'SvarChart', components: { Gantt, Willow }, props: {
        tasks: { type: Array as PropType<TimelineTaskDto[]>, required: true }, links: { type: Array as PropType<TimelineDataDto['links']>, required: true },
        start: { type: Date, required: true }, end: { type: Date, required: true }
    }, emits: { change: (change: ScheduleTimeChangeDto) => !!change.orderId, select: (id: string) => !!id },
    data() {
        const month = { unit: 'month', step: 1, format: '%Y年%m月' }
        const day = { unit: 'day', step: 1, format: '%d' }
        return { api: null as IApi | null, syncing: false, taskTemplate: markRaw(TimelineBar),
            columns: [{ id: 'text', header: '半成品 / 排产单 / 工序', width: 300, flexgrow: 1 }, { id: 'quantity', header: '数量', width: 80, align: 'right' }],
            scales: [month, day], zoomConfig: { level: 1, levels: [
                { minCellWidth: 50, maxCellWidth: 180, scales: [month, { unit: 'week', step: 1, format: '%m-%d' }] },
                { minCellWidth: 16, maxCellWidth: 80, scales: [month, day] },
                { minCellWidth: 16, maxCellWidth: 80, scales: [{ unit: 'day', step: 1, format: '%m月%d日' }, { unit: 'hour', step: 6, format: '%H:%i' }] },
                { minCellWidth: 24, maxCellWidth: 100, scales: [{ unit: 'day', step: 1, format: '%m月%d日' }, { unit: 'hour', step: 1, format: '%H:%i' }] }
            ] } }
    },
    computed: {
        // Give the library independent objects; it never owns the parent's business data.
        inputTasks(): TimelineTaskDto[] { return this.tasks.map(task => ({ ...task, start: new Date(task.start), end: new Date(task.end) })) }
    },
    methods: {
        init(api: IApi) {
            this.api = markRaw(api)
            const editable = (id: string | number) => { const task = this.tasks.find(task => task.id === id); return !!task?.orderId && !task.locked }
            api.intercept('drag-task', event => editable(event.id))
            api.intercept('update-task', event => this.syncing || (editable(event.id) && ('start' in event.task || 'end' in event.task)))
            for (const action of ['add-task', 'delete-task', 'move-task', 'indent-task', 'copy-task', 'add-link', 'delete-link', 'update-link', 'show-editor']) api.intercept(action, () => false)
            api.on('select-task', event => this.$emit('select', String(event.id)))
            api.on('update-task', event => {
                if (this.syncing || event.inProgress) return
                // SVAR applies event.diff at the active scale before this listener runs.
                const task = api.getTask(event.id)
                const source = this.tasks.find(task => task.id === event.id)
                if (source?.orderId && task.start && task.end) this.$emit('change', { orderId: source.orderId, processCode: source.processCode,
                    start: localDateTime(task.start), end: localDateTime(task.end) })
            })
        },
        zoom(direction: number) { void this.api?.exec('zoom-scale', { dir: direction }) },
        setExpanded(expanded: boolean) { for (const task of this.tasks) if (this.tasks.some(child => child.parent === task.id)) void this.api?.exec('open-task', { id: task.id, mode: expanded }) },
        async sync() {
            if (!this.api) return
            this.syncing = true
            try {
                // Restore canonical dates after either accepted or rejected drag, without resetting scroll or tree expansion.
                for (const task of this.tasks) await this.api.exec('update-task', { id: task.id, task: { start: new Date(task.start), end: new Date(task.end),
                    duration: (task.end.getTime() - task.start.getTime()) / 3600000 }, eventSource: 'sync' })
            } finally { this.syncing = false }
        }
    }
})
</script>
<style scoped>
.svar-chart :deep(.wx-link), .svar-chart :deep(.wx-progress-marker) { display: none; }
.svar-chart :deep(.wx-toggle-icon::before) { font-family: sans-serif; content: '▾'; font-size: 15px; }
.svar-chart :deep(.wx-toggle-icon.wxi-menu-right::before) { content: '▸'; }
.svar-chart :deep(.timeline-bar) { position: absolute; inset: 0; padding: 3px 6px; box-sizing: border-box; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
</style>
