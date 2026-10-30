<template>
    <div class="kingzon-page-content craft-workspace-page">
        <ProcessOrderHeader ref="processOrderHeader" v-model="craftOrder" mode="edit" :craftOrderId="craftOrderId"
            v-model:activeVersionId="activeVersionId" :routes="routeItems" @update:routes="updateRoutes"
            @beforeSave="normalizeRoutes" v-model:selectedProcessKey="selectedProcessKey" @saved="onHeaderSaved"
            @versionSaved="onVersionSaved" @loaded="onHeaderLoaded" @editabilityChange="canEditCurrentVersion = $event"
            @cancel="$emit('cancel')" />
        <div v-if="craftOrder.id" class="workspace-shell">
            <section ref="workspace" class="workspace" :style="{
                gridTemplateColumns: `${leftWidth}% 8px minmax(0, 1fr) 8px ${rightWidth}%`
            }">
                <ProcessTree :craftOrderId="craftOrder.id" :activeVersionId="activeVersionId" :routes="routeItems"
                    :selectedProcessKey="selectedProcessKey" @selectProcess="onTreeProcessSelect" />
                <div class="resizer" title="拖动调整左侧宽度" @pointerdown="startResize('left', $event)"></div>
                <ProcessEditor v-if="selectedProcess" ref="processEditor" :process="selectedProcess"
                    :key="`${activeVersionId}:${selectedProcess.uiKey}`" :readonly="!canEditCurrentVersion"
                    :craftOrderInfo="craftOrder" :machineOptions="machineOptions" :machineLoading="machineLoading"
                    :inboundRequired="selectedInboundRequired" @updateProcess="updateProcess"
                    v-model:selectTabIndex="selectTabIndex" @updateMaterial="updateMaterial"
                    @resourcesAdded="clearLibrarySelection" @materialSelectionChange="selectedMaterial = $event"
                    @createCraftVersion="$emit('createCraftVersion', $event)" />
                <div v-else class="empty-editor">{{ activeVersionId ? '请在上方新增或选择工序' : '请先新增工艺版本' }}</div>
                <div class="resizer" title="拖动调整资料库宽度" @pointerdown="startResize('right', $event)"></div>
                <ResourceLibrary ref="resourceLibrary" :craftId="selectedProcess?.craftId"
                    :selectTabIndex="selectTabIndex" @machine-options="onMachineOptions" @select-machines="addMachines"
                    @drag-machines="showMachines" :disabled="!selectedProcess || !canEditCurrentVersion"
                    :allowOutput="!!(selectedProcess?.isInbound || selectedProcess?.isOutsource)"
                    :hasMaterialSelection="!!selectedMaterial" @select-resources="addResources" />
            </section>
        </div>
    </div>
</template>

<script lang="ts">
import { defineComponent } from 'vue'
import { type ProcessEditorRef, type MachineOptions, type MachineSelection, type HeaderSaved, type ProcessSelection, type ResourceSelection, type ResourceLibraryRef, type MaterialChange, CraftOrderDto, type CraftProcessDto, type CraftProcessMaterialDto, type CraftMachineResource } from '@/types/craft'
import ProcessOrderHeader from './component/craft-order-header.vue'
import ProcessTree from './component/craft-tree.vue'
import ProcessEditor from './component/craft-editor.vue'
import ResourceLibrary from './component/resource-library.vue'

export default defineComponent({
    name: 'CreateCraft',
    emits: ['submit', 'createCraftVersion', 'cancel'],
    components: { ProcessOrderHeader, ProcessTree, ProcessEditor, ResourceLibrary },
    props: { craftOrderId: { type: Number, required: true } },
    data() {
        return {
            craftOrder: new CraftOrderDto(),
            activeVersionId: undefined as number | undefined,
            routeItems: [] as CraftProcessDto[],
            selectedProcessKey: '',
            selectedMaterial: null as { row: CraftProcessMaterialDto; main: CraftProcessMaterialDto } | null,
            canEditCurrentVersion: false,
            selectTabIndex: 0,
            machineOptions: [] as CraftMachineResource[],
            machineLoading: false,
            leftWidth: 24,
            rightWidth: 26,
            stopResize: null as (() => void) | null
        }
    },
    computed: {
        selectedInboundRequired(): boolean {
            const active = this.routeItems.filter(item => !item.isDeleted)
            const index = this.selectedProcess ? active.indexOf(this.selectedProcess) : -1
            return index >= 0 && !!active[index + 1]?.isOutsource
        },
        processTypeState(): string {
            return JSON.stringify([this.canEditCurrentVersion, this.routeItems.map(item =>
                [item.code, item.isOutsource, item.isInbound, item.isDeleted])])
        },
        selectedProcess(): CraftProcessDto | null {
            return this.routeItems.find(item => item.uiKey === this.selectedProcessKey) || this.routeItems[0] || null
        }
    },
    watch: {
        processTypeState: {
            immediate: true,
            handler() { if (this.canEditCurrentVersion) this.normalizeRoutes() }
        },
        selectedProcess() { this.selectedMaterial = null },
        activeVersionId() { this.selectedMaterial = null }
    },
    beforeUnmount() { this.stopResize?.() },
    methods: {
        updateRoutes(routes: CraftProcessDto[]) {
            this.routeItems = routes
            if (this.canEditCurrentVersion) this.normalizeRoutes()
        },
        // 路线数据由页面持有：先强制外协前工序入库，再清理非入库工序的产出料。
        normalizeRoutes() {
            const active = this.routeItems.filter(process => !process.isDeleted)
            for (let index = 1; index < active.length; index++) {
                if (active[index].isOutsource) active[index - 1].isInbound = true
            }
            for (const process of active) {
                if (!process.isInbound && !process.isOutsource && process.materials?.some(row => Number(row.ioType) === 2)) {
                    const outputIds = new Set(process.materials.filter(row => Number(row.ioType) === 2)
                        .map(row => row.id).filter(Boolean))
                    process.materials = process.materials.filter(row => Number(row.ioType) !== 2
                        && !outputIds.has(row.insteadSourceId))
                }
            }
        },
        updateProcess(changes: Partial<CraftProcessDto>) {
            const process = this.selectedProcess
            if (!this.canEditCurrentVersion || !process) return
            Object.assign(process, changes)
            this.normalizeRoutes()
            if (changes.materials) this.updateMaterialSort()
        },
        updateMaterial({ row, changes }: MaterialChange) {
            const process = this.selectedProcess
            if (!this.canEditCurrentVersion || !process) return
            const materials = process.materials ?? []
            if (!materials.some(main => main === row || main.substitutes?.includes(row))) return
            Object.assign(row, changes)
            if (changes.substitutes) this.updateMaterialSort()
        },
        updateMaterialSort() {
            for (const [index, main] of (this.selectedProcess?.materials ?? []).entries()) {
                main.sortNo = index + 1
                main.substitutes?.forEach((row, childIndex) => { row.sortNo = childIndex + 1 })
            }
        },
        clearLibrarySelection() { (this.$refs.resourceLibrary as ResourceLibraryRef | undefined)?.clearSelection() },
        showMachines() { (this.$refs.processEditor as ProcessEditorRef | undefined)?.showMachines() },
        onMachineOptions({ craftId, rows, loading }: MachineOptions) {
            if (Number(craftId) !== Number(this.selectedProcess?.craftId || 0)) return
            this.machineOptions = rows
            this.machineLoading = loading
        },
        async addMachines(payload: MachineSelection) {
            if (!this.selectedProcess || !this.canEditCurrentVersion) return
            const count = await (this.$refs.processEditor as ProcessEditorRef | undefined)?.addMachines(payload)
            if (count) this.clearLibrarySelection()
        },
        onHeaderLoaded(order: CraftOrderDto) {
            this.craftOrder = order
            this.activeVersionId = undefined
            this.routeItems = []
            this.selectedProcessKey = ''
        },
        onHeaderSaved({ craftOrder }: HeaderSaved) {
            this.craftOrder = craftOrder
            this.$kingzon_messager.showInfo('工艺单名称已保存')
            this.$emit('submit', craftOrder)
        },
        onVersionSaved() {
            this.$kingzon_messager.showInfo('版本和工序保存成功')
            this.$emit('submit', this.craftOrder)
        },
        async onTreeProcessSelect(process: ProcessSelection | null) {
            if (!process) return
            const versionId = this.activeVersionId
            const key = process.key ?? this.routeItems.find(item => item.code === process.code)?.uiKey ?? ''
            this.selectedProcessKey = key
            await this.$nextTick()
            if (versionId !== this.activeVersionId || this.selectedProcessKey !== key) return
            const editor = (this.$refs.processEditor as ProcessEditorRef | undefined)
            editor?.showTab(process.tab ?? (process.material ? 1 : 0))
            if (process.material) await editor?.focusMaterial(process.material)
        },
        async addResources(payload: ResourceSelection) {
            if (!this.selectedProcess || !this.canEditCurrentVersion) return
            const count = await (this.$refs.processEditor as ProcessEditorRef | undefined)?.addResources(payload)
            if (count) {
                this.clearLibrarySelection()
                const label = { input: '领用主料', output: '入库物料', substitute: '替料' }[payload.target]
                this.$kingzon_messager.showInfo(`已加入 ${count} 条${label}`)
            }
        },
        startResize(side: 'left' | 'right', event: PointerEvent) {
            this.stopResize?.()
            event.preventDefault()
            const origin = event.clientX
            const initial = side === 'left' ? this.leftWidth : this.rightWidth
            const total = (this.$refs.workspace as HTMLElement | undefined)?.clientWidth || window.innerWidth
            const onMove = (move: PointerEvent) => {
                const delta = ((move.clientX - origin) / total) * 100
                if (side === 'left') this.leftWidth = Math.min(34, Math.max(18, initial + delta))
                else this.rightWidth = Math.min(34, Math.max(20, initial - delta))
            }
            const onUp = () => {
                window.removeEventListener('pointermove', onMove)
                window.removeEventListener('pointerup', onUp)
                window.removeEventListener('pointercancel', onUp)
                this.stopResize = null
            }
            this.stopResize = onUp
            window.addEventListener('pointermove', onMove)
            window.addEventListener('pointerup', onUp)
            window.addEventListener('pointercancel', onUp)
        }
    }
})
</script>

<style scoped>
.craft-workspace-page {
    display: flex;
    flex-direction: column;
    height: 100%;
    min-height: 0;
    min-width: 0;
    overflow: auto;
}

.workspace-shell {
    flex: 1 1 0;
    min-height: 360px;
    display: flex;
}

.workspace {
    display: grid;
    width: 100%;
    min-height: 0;
    min-width: 0;
}

.resizer {
    position: relative;
    cursor: col-resize;
    user-select: none;
    touch-action: none;
}

.resizer::before {
    position: absolute;
    top: 0;
    bottom: 0;
    left: 3px;
    width: 2px;
    content: '';
    background: var(--color-grey-bg);
}

.resizer:hover::before {
    background: var(--color-blue);
}

.empty-editor {
    display: flex;
    align-items: center;
    justify-content: center;
    min-width: 0;
    color: #777;
    font-size: 13px;
    background: #fff;
    border: 1px solid var(--color-grey-bg);
}
</style>
