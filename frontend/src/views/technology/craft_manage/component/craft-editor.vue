<template>
    <section class="panel process-editor-panel">
        <div class="panel-header editor-heading">
            <strong>当前工序：{{ process.code }} {{ process.name }}</strong>
            <span class="editor-switch-tip">点击上方工序路线切换</span>
        </div>
        <Tabs ref="tabs" class="process-tabs" @tabSelect="onTabSelect">
            <TabPanel title="工序信息">
                <CraftProcessInfo :process="process" :readonly="readonly" :inboundRequired="inboundRequired"
                    @updateProcess="$emit('updateProcess', $event)" />
            </TabPanel>
            <TabPanel title="物料信息">
                <CraftInfo ref="craftInfo" :process="process" :readonly="readonly" :craftOrderInfo="craftOrderInfo"
                    @updateProcess="$emit('updateProcess', $event)" @updateMaterial="$emit('updateMaterial', $event)"
                    @resourcesAdded="$emit('resourcesAdded')" @createCraftVersion="$emit('createCraftVersion', $event)"
                    @materialSelectionChange="$emit('materialSelectionChange', $event)" />
            </TabPanel>
            <TabPanel title="加工设备">
                <CraftMachine ref="craftMachine" :process="process" :readonly="readonly"
                    :machineOptions="machineOptions" :loading="machineLoading"
                    @updateProcess="$emit('updateProcess', $event)" @resourcesAdded="$emit('resourcesAdded')" />
            </TabPanel>
            <TabPanel title="工序文件">
                <CraftFile :process="process" :readonly="readonly" />
            </TabPanel>
        </Tabs>
    </section>
</template>

<script lang="ts">
import type { TabsRef, MaterialEditorRef, MachineEditorRef, ResourceSelection, MachineSelection, CraftProcessMaterialDto, CraftOrderDto, CraftProcessDto, CraftMachineResource } from '@/types/craft'
import { defineComponent, type PropType } from 'vue'
import CraftInfo from './craft-info.vue'
import CraftProcessInfo from './craft-process-info.vue'
import CraftMachine from './craft-machine.vue'
import CraftFile from './craft-file.vue'

export default defineComponent({
    name: 'ProcessEditor',
    emits: ['createCraftVersion', 'materialSelectionChange', 'resourcesAdded', 'updateProcess', 'updateMaterial', 'update:selectTabIndex'],
    components: { CraftInfo, CraftProcessInfo, CraftMachine, CraftFile },
    props: {
        process: { type: Object as PropType<CraftProcessDto>, required: true },
        readonly: Boolean,
        inboundRequired: Boolean,
        machineLoading: Boolean,
        machineOptions: { type: Array as PropType<CraftMachineResource[]>, default: () => [] },
        craftOrderInfo: {
            type: Object as PropType<CraftOrderDto>,
            default: () => ({})
        }
    },
    methods: {
        showTab(index: number) {
            (this.$refs.tabs as TabsRef | undefined)?.select(index)
            this.$emit('update:selectTabIndex', index)
        },
        async focusMaterial(material: CraftProcessMaterialDto) {
            const process = this.process
            this.showTab(1)
            await this.$nextTick()
            if (this.process !== process) return false
            return (this.$refs.craftInfo as MaterialEditorRef | undefined)?.focusMaterial(material) ?? false
        },
        showMachines() { (this.$refs.tabs as TabsRef | undefined)?.select(2) },
        async addMachines(payload: MachineSelection) {
            if (this.readonly) return 0
            this.showMachines()
            await this.$nextTick()
            return (this.$refs.craftMachine as MachineEditorRef | undefined)?.insertMachines(payload) ?? 0
        },
        async addResources({ resources, target }: ResourceSelection) {
            if (this.readonly) return 0
            this.showTab(1)
            await this.$nextTick()
            return (this.$refs.craftInfo as MaterialEditorRef | undefined)?.insertResources(resources, target) ?? 0
        },
        onTabSelect(tab) {
            const tabIndexMap: Record<string, number> = {
                '工序信息': 0,
                '物料信息': 1,
                '加工设备': 2,
                '工序文件': 3
            }
            const index = tabIndexMap[tab.title]
            if (index === undefined) return
            this.$emit('update:selectTabIndex', index)
        }
    }
})
</script>

<style scoped>
.process-editor-panel {
    position: relative;

    display: flex;
    flex-direction: column;

    width: 100%;
    height: 100%;
    min-width: 0;
    min-height: 0;

    overflow: hidden;

    background: #fff;
    border: 1px solid var(--color-grey-bg);
    border-radius: 0;
    box-sizing: border-box;
}

.panel-header {
    display: flex;
    flex: 0 0 auto;
    align-items: center;
    justify-content: space-between;

    height: 34px;
    padding: 0 14px;

    font-size: 13px;

    background-color: var(--color-grey-bg);
    border-bottom: 1px solid var(--color-grey-bg);
    box-sizing: border-box;
}

.editor-heading {
    height: 34px;
    padding: 0 14px;
}

.editor-heading strong {
    font-size: 13px;
}

.editor-switch-tip {
    color: #98a2b3;
    font-size: 11px;
}

.process-info-panel {
    flex: 0 0 auto;
    border-bottom: 1px solid var(--color-grey-bg);
}

.process-tabs {
    flex: 1;
    min-height: 0;
    overflow: hidden;
}

/* Tabs整体 */
:deep(.process-tabs.tabs-container),
:deep(.process-tabs .tabs-container) {
    display: flex;
    flex-direction: column;

    height: 100% !important;
    min-height: 0;
}

/* Tab标题区域不能参与撑高 */
:deep(.process-tabs .tabs-header) {
    flex: 0 0 auto;
}

/* Tab内容区域占剩余空间 */
:deep(.process-tabs .tabs-panels) {
    flex: 1;

    height: 0 !important;
    min-height: 0;

    overflow: hidden;
}

/* 每一个TabPanel */
:deep(.process-tabs .tabs-panels > .panel) {
    height: 100% !important;
    min-height: 0;
}

/* TabPanel真正的body */
:deep(.process-tabs .tabs-panels > .panel > .panel-body) {
    height: 100% !important;
    min-height: 0;

    padding: 0;

    overflow: auto;
    box-sizing: border-box;
}
</style>
