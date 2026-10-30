<template>
    <Panel class="schedule-process-card" :bodyStyle="{ padding: expanded ? '6px' : '0' }" style="margin-bottom: 6px">
        <template #header>
            <div class="schedule-process-heading" @dragover.prevent @drop.prevent.stop="$emit('dropProcess', process.key)">
                <span :draggable="canEdit" :title="editable ? '拖动调整工序顺序' : '工序调整由系统参数控制'"
                    class="schedule-process-drag" @dragstart="startDrag" @dragend="$emit('dragEnd')">↕</span>
                <div class="schedule-process-title-row">
                    <LinkButton plain :disabled="saving" class="schedule-process-title"
                        :title="`${process.code || index + 1} · ${process.name || '新工序'}`" @click="$emit('toggle')">
                        {{ process.code || index + 1 }} · {{ process.name || '新工序' }}
                    </LinkButton>
                </div>
                <div class="schedule-process-tools">
                    <MenuButton text="更多" :plain="true" :disabled="!canEdit">
                        <Menu>
                            <MenuItem text="上移" :disabled="!canEdit || index === 0" @click="processAction('up')" />
                            <MenuItem text="下移" :disabled="!canEdit || index === count - 1" @click="processAction('down')" />
                            <MenuItem text="复制" :disabled="!canEdit" @click="processAction('copy')" />
                            <MenuItem text="删除" :disabled="!canEdit" @click="processAction('remove')" />
                        </Menu>
                    </MenuButton>
                    <LinkButton plain :disabled="saving" @click.stop="$emit('toggle')">{{ expanded ? '收起' : '展开' }}</LinkButton>
                </div>
                <div class="schedule-process-summary">
                    <span>{{ process.isOutsource ? '外协' : '内部生产' }}{{ process.isInbound ? ' · 入库' : '' }}</span>
                    <span>设备 {{ process.machines.length }} 台</span><span>人员 {{ userCount }} 人 / {{ allocationCount }} 条分配</span>
                    <span>物料 {{ process.materials.length }} 项</span><span>人工 {{ humanHours }} 人时</span>
                </div>
            </div>
        </template>
        <div v-if="expanded">
            <Form :model="process" :labelWidth="68" labelAlign="right" class="schedule-process-form">
                <FormField name="craftId" label="加工工艺:" style="min-width: 0; margin: 0">
                    <ComboBox :modelValue="process.craftId" :data="crafts" valueField="id" textField="name"
                        :editable="false" :disabled="!canEdit" style="width: 100%; height: 28px"
                        @update:modelValue="update({ craftId: Number($event) || 0 })" />
                </FormField>
                <div class="schedule-process-flags">
                    <label><CheckBox :modelValue="process.isInbound" :disabled="!canEdit"
                        @update:modelValue="update({ isInbound: $event })" /> 入库</label>
                    <label><CheckBox :modelValue="process.isOutsource" :disabled="!canEdit"
                        @update:modelValue="update({ isOutsource: $event })" /> 外协</label>
                    <label><CheckBox :modelValue="process.isReportWork" :disabled="!canEdit"
                        @update:modelValue="update({ isReportWork: $event })" /> 是否报工</label>
                </div>
            </Form>
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
        quantity: { type: Number, required: true }, editable: Boolean, saving: Boolean, expanded: Boolean
    },
    emits: ['toggle', 'move', 'copy', 'remove', 'materials', 'updateProcess', 'dragStart', 'dragEnd', 'dropProcess'],
    computed: {
        canEdit(): boolean { return this.editable && !this.saving },
        userCount(): number { return new Set(this.process.machines.flatMap(machine => machine.users.map(user => user.userId))).size },
        allocationCount(): number { return this.process.machines.reduce((sum, machine) => sum + machine.users.length, 0) },
        humanHours(): number { return Number(this.process.machines.reduce((sum, machine) => sum + machine.users.reduce((n, user) => n + (Number(user.hours) || 0), 0), 0).toFixed(4)) }
    },
    methods: {
        processAction(action: 'up' | 'down' | 'copy' | 'remove') {
            if (!this.canEdit) return
            if (action === 'up' && this.index > 0) this.$emit('move', -1)
            else if (action === 'down' && this.index < this.count - 1) this.$emit('move', 1)
            else if (action === 'copy') this.$emit('copy')
            else if (action === 'remove') this.$emit('remove')
        },
        update(changes: Partial<ScheduleProcessEditDto>) { if (this.canEdit) this.$emit('updateProcess', changes) },
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
.schedule-process-form { display: flex; flex-direction: column; gap: 6px; }
.schedule-process-flags { display: flex; flex-wrap: wrap; gap: 6px 12px; padding-left: 4px; }
.schedule-process-flags label { display: flex; align-items: center; gap: 4px; }
.schedule-process-section { border-top: 1px solid #e5e7eb; margin-top: 6px; padding-top: 6px; min-width: 0; }
</style>
