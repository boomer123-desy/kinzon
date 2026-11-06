<template>
    <Panel class="schedule-process-card" :class="{ 'schedule-process-compact': compact }" :bodyStyle="{ padding: expanded ? '6px' : '0' }" style="margin-bottom: 6px">
        <template #header>
            <div class="schedule-process-heading" @dragover.prevent @drop.prevent.stop="$emit('dropProcess', process.key)">
                <span :draggable="canEdit" title="拖动调整工序顺序" class="schedule-process-drag"
                    @dragstart="startDrag" @dragend="$emit('dragEnd')">↕</span>
                <div class="schedule-process-title-row">
                    <LinkButton plain :disabled="saving" class="schedule-process-title"
                        :title="`${process.code || index + 1} · ${process.name || '新工序'}`" @click="$emit('toggle')">
                        {{ process.code || index + 1 }} · {{ process.name || '新工序' }}
                    </LinkButton>
                    <span v-if="compact" class="schedule-process-type">{{ process.isOutsource ? '外协' : '内部生产' }}{{ process.isInbound ? ' · 入库' : '' }}</span>
                </div>
                <div class="schedule-process-tools">
                    <MenuButton text="更多" :plain="true" :disabled="!canEdit">
                        <Menu>
                            <MenuItem text="上移" :disabled="!canEdit || index === 0" @click="moveProcess(-1)" />
                            <MenuItem text="下移" :disabled="!canEdit || index === count - 1" @click="moveProcess(1)" />
                            <MenuItem text="复制" :disabled="!canEdit" @click="copyProcess" />
                            <MenuItem text="删除" :disabled="!canEdit" @click="removeProcess" />
                        </Menu>
                    </MenuButton>
                    <LinkButton plain :disabled="saving" @click.stop="$emit('toggle')">{{ expanded ? '收起' : '展开' }}</LinkButton>
                </div>
                <div class="schedule-process-summary">
                    <span v-if="!compact">{{ process.isOutsource ? '外协' : '内部生产' }}{{ process.isInbound ? ' · 入库' : '' }}</span>
                    <span>设备 {{ process.machines.length }} 台</span><span>人员 {{ userCount }} 人 / {{ allocationCount }} 条分配</span>
                    <span>物料 {{ process.materials.length }} 项</span><span>{{ compact ? '派工工时' : '工时' }} {{ humanHours }} h</span><span v-if="compact">参考工时 —</span>
                </div>
            </div>
        </template>
        <div v-if="expanded">
            <div style="display: flex; flex-wrap: wrap; gap: 6px 16px; padding: 4px">
                <span>加工工艺：{{ craftName }}</span>
                <span>是否报工：{{ process.isReportWork ? '是' : '否' }}</span>
            </div>
            <div v-if="!process.isOutsource || process.machines.length" class="schedule-process-section">
                <ScheduleMachine :modelValue="process.machines" :quantity="quantity" :machineOptions="machineOptions"
                    :disabled="saving" @update:modelValue="$emit('updateProcess', { machines: $event })" />
            </div>
            <div class="schedule-process-section">
                <div style="display: flex; justify-content: space-between; align-items: center; gap: 8px">
                    <strong>工序物料 <small class="kingzon-text-grey">{{ process.materials.length }} 项</small></strong>
                    <LinkButton :disabled="saving" @click="$emit('materials')">配置物料</LinkButton>
                </div>
            </div>
        </div>
    </Panel>
</template>

<script lang="ts">
import { defineComponent, type PropType } from 'vue'
import ScheduleMachine from './schedule-machine.vue'
import type { ScheduleProcessEditDto, ScheduleMachineOptionDto } from '@/types/schedule'

export default defineComponent({
    name: 'ScheduleProcessCard', components: { ScheduleMachine },
    props: {
        process: { type: Object as PropType<ScheduleProcessEditDto>, required: true },
        crafts: { type: Array as PropType<Array<{ id: number; name: string }>>, default: () => [] },
        machineOptions: { type: Array as PropType<ScheduleMachineOptionDto[]>, default: () => [] },
        index: { type: Number, required: true }, count: { type: Number, required: true },
        quantity: { type: Number, required: true }, editable: Boolean, saving: Boolean, expanded: Boolean, compact: Boolean
    },
    emits: ['copy', 'toggle', 'move', 'remove', 'materials', 'updateProcess', 'dragStart', 'dragEnd', 'dropProcess'],
    computed: {
        craftName(): string { return this.crafts.find(craft => craft.id === this.process.craftId)?.name ?? '—' },
        canEdit(): boolean { return this.editable && !this.saving },
        userCount(): number { return new Set(this.process.machines.flatMap(machine => machine.users.map(user => user.userId))).size },
        allocationCount(): number { return this.process.machines.reduce((sum, machine) => sum + machine.users.length, 0) },
        humanHours(): number { return Number(this.process.machines.reduce((sum, machine) => sum + (machine.users.length ? machine.users.reduce((n, user) => n + (Number(user.hours) || 0), 0) : (Number(machine.hours) || 0)), 0).toFixed(4)) }
    },
    methods: {
        copyProcess() { if (this.canEdit) this.$emit('copy') },
        removeProcess() { if (this.canEdit) this.$emit('remove') },
        moveProcess(step: number) {
            if (this.canEdit && this.index + step >= 0 && this.index + step < this.count) this.$emit('move', step)
        },
        startDrag(event: DragEvent) {
            if (!this.canEdit) { event.preventDefault(); return }
            event.dataTransfer?.setData('application/x-schedule-process', this.process.key)
            this.$emit('dragStart', this.process.key)
        }
    }
})
</script>

<style scoped>
.schedule-process-card :deep(.panel-header), .schedule-process-card :deep(.panel-body) { background: #fff; border-color: #e5e7eb; }
.schedule-process-heading { display: grid; grid-template-columns: 18px minmax(0, 1fr) auto; align-items: start; gap: 2px 4px; width: 100%; min-width: 0; padding: 2px 0; box-sizing: border-box; }
.schedule-process-drag { flex-shrink: 0; cursor: grab; padding: 5px 3px; }
.schedule-process-title-row { display: flex; min-width: 0; }
.schedule-process-title { min-width: 0; max-width: 100%; }
.schedule-process-title :deep(.l-btn-left) { display: block; min-width: 0; max-width: 100%; }
.schedule-process-title :deep(.l-btn-text) { display: block; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
.schedule-process-summary { grid-column: 2 / -1; display: flex; flex-wrap: wrap; gap: 2px 8px; padding: 2px 4px; font-size: 12px; font-weight: normal; color: #777; line-height: 18px; overflow-wrap: anywhere; }
.schedule-process-tools { display: flex; align-items: center; flex-shrink: 0; gap: 2px; }
.schedule-process-section { border-top: 1px solid #e5e7eb; margin-top: 6px; padding-top: 6px; min-width: 0; }
.schedule-process-compact { border: 1px solid #dce1e7; border-radius: 4px; overflow: hidden; margin-bottom: 8px !important; }
.schedule-process-compact :deep(.panel-header) { border: 0; padding: 6px 8px; }
.schedule-process-compact :deep(.panel-body) { border: 0; }
.schedule-process-compact .schedule-process-heading { min-height: 44px; align-items: center; padding: 0; }
.schedule-process-compact .schedule-process-title-row { align-items: center; gap: 4px; }
.schedule-process-type { flex-shrink: 0; font-weight: normal; font-size: 12px; color: #777; }
.schedule-process-compact .schedule-process-summary { flex-wrap: nowrap; overflow: hidden; white-space: nowrap; padding: 0 4px; gap: 8px; }
.schedule-process-compact .schedule-process-summary span { flex-shrink: 0; }
</style>
