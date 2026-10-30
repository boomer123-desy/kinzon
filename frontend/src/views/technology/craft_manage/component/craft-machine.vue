<template>
    <div class="machine-selector" :class="{ 'machine-drop-active': dragActive }"
        @dragover.stop="onDragOver" @dragleave="onDragLeave" @drop.stop="onDrop">
        <div class="machine-tip">
            <span>{{ !process.craftId ? '请先在“工序信息修改”中选择加工工艺。' : '从右侧资料库的“机器”页签点击加入或拖入此表；随版本一并保存。' }}</span>
            <span>已选 {{ selectedMachines.length }} 台</span>
        </div>
        <div v-if="dragActive" class="machine-drop-hint">松开：加入当前工序的机器（自动跳过重复项）</div>
        <DataGrid class="machine-grid" :data="selectedMachines" :pagination="false" :columnResizing="true"
            selectionMode="single" :rowHeight="30" :rowCss="rowClass">
            <GridColumn field="code" title="编码" width="110" />
            <GridColumn field="name" title="机器名称" width="160" />
            <GridColumn field="model" title="型号" width="110" />
            <GridColumn field="categoryName" title="类别" width="100" />
            <GridColumn field="theoreticalWorkHours" title="理论工时(h)" width="125">
                <template #body="{ row }">
                    <NumberBox :modelValue="row.theoreticalWorkHours" :min="0" :max="99999999.9999" :precision="4" :disabled="readonly"
                        @update:modelValue="setWorkHours(row.id, $event)" style="width: 100%" />
                </template>
            </GridColumn>
            <GridColumn title="状态" width="145">
                <template #body="{ row }">{{ loading ? '正在核对…' : row.available ? '可用' : '当前工艺不可用，请移除' }}</template>
            </GridColumn>
            <GridColumn title="操作" width="55" align="center">
                <template #body="{ row }">
                    <LinkButton class="danger-button" iconCls="icon-remove" :plain="true" :disabled="readonly" title="移除机器" aria-label="移除机器"
                        @click.stop="removeMachine(row.id)" />
                </template>
            </GridColumn>
        </DataGrid>
        <div v-if="!selectedMachines.length" class="empty-tip">尚未选择机器，可从右侧拖入。</div>
    </div>
</template>

<script lang="ts">
import type { CraftProcessDto, CraftMachineResource, MachineSelection } from '@/types/craft'
import { defineComponent, type PropType } from 'vue'

export default defineComponent({
    name: 'CraftMachine',
    emits: ['resourcesAdded', 'updateProcess'],
    props: {
        readonly: Boolean,
        loading: Boolean,
        machineOptions: { type: Array as PropType<CraftMachineResource[]>, default: () => [] },
        process: { type: Object as PropType<CraftProcessDto>, required: true }
    },
    data() { return { dragActive: false } },
    computed: {
        selectedMachines() {
            return [...new Set((this.process.machineIds ?? []).map(Number))].map(id => {
                const machine = this.machineOptions.find(item => item.id === id && item.craftId === Number(this.process.craftId))
                return { id, code: '', name: `机器 #${id}`, ...machine, theoreticalWorkHours: this.process.machineSettings?.find(item => item.machineId === id)?.theoreticalWorkHours ?? 0, available: !!machine }
            })
        }
    },
    mounted() {
        window.addEventListener('dragend', this.clearDrag)
        window.addEventListener('drop', this.clearDrag, true)
    },
    beforeUnmount() {
        window.removeEventListener('dragend', this.clearDrag)
        window.removeEventListener('drop', this.clearDrag, true)
    },
    watch: {
        process() { this.clearDrag() },
        'process.craftId'() { this.clearDrag() },
        readonly() { this.clearDrag() }
    },
    methods: {
        setWorkHours(id: number, value: number | null) {
            if (this.readonly || !(this.process.machineIds ?? []).includes(id)) return
            const settings = [...(this.process.machineSettings ?? [])]
            const index = settings.findIndex(item => item.machineId === id)
            const setting = { machineId: id, theoreticalWorkHours: value ?? 0 }
            if (index >= 0) settings[index] = setting
            else settings.push(setting)
            this.$emit('updateProcess', { machineSettings: settings })
        },
        clearDrag() { this.dragActive = false },
        rowClass(row: { available: boolean }) { return this.dragActive ? 'machine-drop-row' : !this.loading && !row.available ? 'machine-invalid-row' : '' },
        insertMachines(payload: MachineSelection | null | undefined): number {
            if (this.readonly || this.loading || !this.process.craftId || Number(payload?.craftId) !== Number(this.process.craftId)
                || !Array.isArray(payload?.machines)) return 0
            const ids = [...new Set((this.process.machineIds ?? []).map(Number))]
            const allowed = new Set(this.machineOptions.filter(row => row.craftId === Number(this.process.craftId)).map(row => row.id))
            let count = 0
            for (const row of payload.machines) {
                const id = Number(row?.id)
                if (!allowed.has(id) || ids.includes(id)) continue
                ids.push(id)
                count++
            }
            if (count) this.$emit('updateProcess', { machineIds: ids })
            this.$kingzon_messager.showInfo(count ? `已加入 ${count} 台机器` : '未添加机器：已存在或不属于当前工艺')
            return count
        },
        removeMachine(id: number) {
            if (this.readonly) return
            this.$emit('updateProcess', {
                machineIds: (this.process.machineIds ?? []).filter(value => Number(value) !== id),
                machineSettings: (this.process.machineSettings ?? []).filter(item => item.machineId !== id)
            })
        },
        acceptsDrag(event: DragEvent) {
            return !this.readonly && !this.loading && !!this.process.craftId
                && Array.from(event.dataTransfer?.types ?? []).includes('application/x-craft-machines')
        },
        onDragOver(event: DragEvent) {
            this.dragActive = this.acceptsDrag(event)
            if (!this.dragActive) return
            event.preventDefault()
            event.dataTransfer!.dropEffect = 'copy'
        },
        onDragLeave(event: DragEvent) {
            if (!(event.currentTarget as HTMLElement).contains(event.relatedTarget as Node)) this.clearDrag()
        },
        onDrop(event: DragEvent) {
            this.clearDrag()
            if (!this.acceptsDrag(event)) return
            event.preventDefault()
            let payload
            try { payload = JSON.parse(event.dataTransfer!.getData('application/x-craft-machines')) } catch { return }
            if (this.insertMachines(payload)) this.$emit('resourcesAdded')
        }
    }
})
</script>

<style scoped>
.machine-selector { position: relative; display: flex; flex-direction: column; height: 100%; min-height: 0; padding: 8px; box-sizing: border-box; }
.machine-tip { display: flex; justify-content: space-between; gap: 10px; padding-bottom: 8px; color: #64748b; font-size: 12px; }
.machine-grid { flex: 1; min-height: 160px; }
.empty-tip { padding: 6px; color: #777; font-size: 12px; }
.machine-drop-active { outline: 2px solid #2563eb; outline-offset: -2px; background: #eff6ff; }
.machine-drop-hint { position: absolute; z-index: 10; top: 32px; right: 12px; background: #1d4ed8; color: #fff; padding: 5px 8px; font-size: 12px; pointer-events: none; }
.machine-selector :deep(.machine-drop-row > td) { background: #dbeafe !important; }
.machine-selector :deep(.machine-invalid-row > td) { color: #b45309; background: #fffbeb; }
</style>
