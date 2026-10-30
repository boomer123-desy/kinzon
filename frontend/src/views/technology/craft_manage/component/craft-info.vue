<template>
    <div class="editor-content">
        <div class="material-guide">{{ selectionLabel }}<span>拖到行上/下沿插入，中间加替料；双击数量可修改。</span></div>
        <section class="data-section input-section">
            <div class="section-title">
                <h3>领用主料</h3><span>{{ inputMaterials.length }} 条主料</span>
            </div>
            <CraftMaterialTable ref="inputTable" class="input-table" :rows="inputMaterials" :ioType="1"
                :selectedRow="selection?.row" :readonly="readonly" :versionOptions="versionOptionsByProduct"
                @updateMaterial="$emit('updateMaterial', $event)" @select="selectMaterial" @remove="removeMaterial"
                @loadVersions="loadPublishedVersions($event, true)" @createCraftVersion="createCraftVersion"
                @dropResources="dropResources" />
        </section>
        <section v-if="showOutput" class="data-section output-section">
            <div class="section-title">
                <h3>入库物料</h3><span>{{ outputMaterials.length }} 条主料</span>
            </div>
            <CraftMaterialTable ref="outputTable" class="output-table" :rows="outputMaterials" :ioType="2"
                :selectedRow="selection?.row" :readonly="readonly" :versionOptions="versionOptionsByProduct"
                @updateMaterial="$emit('updateMaterial', $event)" @select="selectMaterial" @remove="removeMaterial"
                @loadVersions="loadPublishedVersions($event, true)" @createCraftVersion="createCraftVersion"
                @dropResources="dropResources" />
        </section>
    </div>
</template>

<script lang="ts">
import { InventoryDataType } from '@/types/common'
import { CraftProcessMaterialDto, CraftVersionOptionDto, type CraftOrderDto, type CraftProcessDto, type CraftResourceTarget, type MaterialTableRef, type ResourceDrop, type MaterialResource, type MaterialPlacement } from '@/types/craft'
import { defineComponent, toRaw, type PropType } from 'vue'
import CraftMaterialTable from './craft-material-table.vue'
import { SystemParaType } from '@/types/enums'

type MaterialSelection = { row: CraftProcessMaterialDto; main: CraftProcessMaterialDto }

export default defineComponent({
    name: 'CraftInfo',
    components: { CraftMaterialTable },
    emits: ['createCraftVersion', 'materialSelectionChange', 'resourcesAdded', 'updateProcess', 'updateMaterial'],
    props: {
        process: { type: Object as PropType<CraftProcessDto>, required: true },
        craftOrderInfo: { type: Object as PropType<CraftOrderDto>, default: () => ({}) },
        readonly: Boolean
    },
    data() {
        return {
            selection: null as MaterialSelection | null,
            versionOptionsByProduct: {} as Record<number, CraftVersionOptionDto[]>,
            versionRequests: {} as Record<number, Promise<CraftVersionOptionDto[]> | undefined>,
            defaultLoss: 0 as number | undefined
        }
    },
    computed: {
        inputMaterials(): CraftProcessMaterialDto[] {
            return (this.process.materials ?? []).filter(item => !item.insteadSourceId && Number(item.ioType) !== 2)
        },
        outputMaterials(): CraftProcessMaterialDto[] {
            return (this.process.materials ?? []).filter(item => !item.insteadSourceId && Number(item.ioType) === 2)
        },
        showOutput(): boolean { return !!(this.process.isInbound || this.process.isOutsource) },
        selectionLabel(): string {
            return this.selection ? `当前主料：${this.selection.main.materialName || this.selection.main.materialNo}` : '选中主料后，可从右侧加入替料。'
        }
    },
    mounted() {
        this.loadExistingVersionOptions()
        this.getDefaultLoss()
    },
    watch: {
        process() {
            this.selectMaterial(null)
            this.loadExistingVersionOptions()
        },
        showOutput(value) {
            if (!value && Number(this.selection?.main.ioType) === 2) this.selectMaterial(null)
        }
    },
    methods: {
        async focusMaterial(material: CraftProcessMaterialDto) {
            if (!this.process.materials?.includes(material) || material.insteadSourceId) return false
            if (Number(material.ioType) === 2 && !this.showOutput) {
                this.$kingzon_messager.showInfo('该入库物料所在区域仅在入库或外协工序中显示')
                return false
            }
            this.selectMaterial({ row: material, main: material })
            await this.$nextTick()
            const table = (this.$refs[Number(material.ioType) === 2 ? 'outputTable' : 'inputTable'] as MaterialTableRef | undefined)
            return table?.focusMaterial(material) ?? false
        },
        selectMaterial(selection: MaterialSelection | null) {
            this.selection = selection
            this.$emit('materialSelectionChange', selection)
        },
        canSelectCraftVersion(row: CraftProcessMaterialDto) {
            return ['成品', '半成品'].includes(row.materialType ?? '') && Number(row.ioType) !== 2
        },
        loadExistingVersionOptions() {
            for (const main of this.process.materials ?? []) {
                for (const row of [main, ...(main.substitutes ?? [])]) this.loadPublishedVersions(row)
            }
        },
        async loadPublishedVersions(row: CraftProcessMaterialDto, force = false, selectMain = false) {
            const productId = Number(row.dataId)
            if (!this.canSelectCraftVersion(row) || !productId) return
            const process = this.process
            try {
                if (force || !Object.prototype.hasOwnProperty.call(this.versionOptionsByProduct, productId)) {
                    if (!this.versionRequests[productId]) {
                        this.versionRequests[productId] = this.$http.get('/api/technical/CraftOrderManage/GetPublishedCraftVersionOptions', { productId })
                            .then(result => {
                                const options = (result?.data ?? []).map((item: CraftVersionOptionDto) => new CraftVersionOptionDto({
                                    id: Number(item.id), text: item.text, isMain: item.isMain
                                }))
                                this.versionOptionsByProduct[productId] = options
                                return options
                            })
                            .finally(() => { delete this.versionRequests[productId] })
                    }
                    await this.versionRequests[productId]
                }
                // 仅新加入的领用料默认选主版本；已保存或手工选择的引用保持原值。
                if (!selectMain || this.readonly || this.process !== process) return
                const currentRow = this.process.materials.flatMap(main => [main, ...(main.substitutes ?? [])])
                    .find(item => toRaw(item) === toRaw(row))
                if (!currentRow || currentRow.refCraftOrderVersionId) return
                const mainVersion = this.versionOptionsByProduct[productId]?.find(item => item.isMain)
                if (mainVersion) this.$emit('updateMaterial', { row: currentRow, changes: { refCraftOrderVersionId: mainVersion.id } })
            } catch (error) {
                this.$kingzon_messager.showError(error)
            }
        },
        createCraftVersion(row: CraftProcessMaterialDto) {
            if (!this.readonly) this.$emit('createCraftVersion', { ...row })
        },
        dropResources({ resources, target, ...placement }: ResourceDrop) {
            const count = this.insertResources(resources, target, placement)
            if (count) this.$emit('resourcesAdded')
            if (count) this.$kingzon_messager.showInfo(`已加入 ${count} 条${target === 'substitute' ? '替料' : target === 'output' ? '入库物料' : '领用主料'}`)
        },
        insertResources(resources: MaterialResource[], target: CraftResourceTarget = 'input', placement?: MaterialPlacement): number {
            if (this.readonly || !resources?.length) return 0
            if (!['input', 'output', 'substitute'].includes(target)) return 0
            if (target === 'output' && !this.showOutput) {
                this.$kingzon_messager.showInfo('当前工序未勾选入库或外协，不能加入入库物料')
                return 0
            }
            const main = placement ? placement.main : this.selection?.main
            if (target === 'substitute' && (!main || !this.process.materials?.includes(main)
                || (Number(main.ioType) === 2 && !this.showOutput))) {
                this.$kingzon_messager.showInfo('请先在领用主料或入库物料中选中一条主料')
                return 0
            }
            const ioType = target === 'substitute' ? Number(main!.ioType) : target === 'output' ? 2 : 1
            const rows = target === 'substitute' ? (main!.substitutes ?? [])
                : (ioType === 2 ? this.outputMaterials : this.inputMaterials)
            // 拖拽落点必须仍属于当前工序；空白处追加不受原选中行影响。
            if (placement && ((placement.position !== 'append' && !placement.row)
                || (placement.row && !rows.includes(placement.row)))) return 0
            const keys = new Set(rows.map(row => `${row.dataType}:${row.dataId}`))
            if (target === 'substitute') keys.add(`${main!.dataType}:${main!.dataId}`)
            const additions: CraftProcessMaterialDto[] = []
            for (const resource of resources) {
                const dataId = Number(resource.dataId)
                const dataType = Number(resource.dataType)
                const unitId = Number(resource.unitId)
                if (!(dataId > 0) || !(unitId > 0) || resource.dataType == null || !Number.isFinite(dataType)) continue
                const key = `${dataType}:${dataId}`
                if (keys.has(key)) continue
                // 当前工艺产品可作为入库物料，但不能成为自身领用料/领用替料。
                const ownId = dataType === InventoryDataType.Product ? this.craftOrderInfo.productId : this.craftOrderInfo.specificationId
                if (ioType !== 2 && dataId === ownId) continue
                keys.add(key)
                additions.push(new CraftProcessMaterialDto({
                    dataId, dataType, ioType, unitId,
                    materialNo: resource.materialNo ?? resource.code ?? '',
                    materialName: resource.materialName ?? resource.name ?? '',
                    materialType: resource.materialType ?? '物料',
                    unitName: resource.unitName ?? '', remark: resource.remark,
                    quantity: 1, lossRate: this.defaultLoss ?? 0,
                    insteadSourceId: target === 'substitute' ? main!.id || undefined : undefined,
                    substitutes: []
                }))
            }
            if (!additions.length) {
                this.$kingzon_messager.showInfo('未添加物料：已存在、缺少基本单位或不能领用当前工艺产品')
                return 0
            }
            if (target === 'substitute') {
                const anchorIndex = placement?.row ? rows.indexOf(placement.row) : -1
                const position = anchorIndex >= 0 ? anchorIndex + (placement?.position === 'before' ? 0 : 1) : rows.length
                this.$emit('updateMaterial', {
                    row: main, changes: {
                        substitutes: [...rows.slice(0, position), ...additions, ...rows.slice(position)]
                    }
                })
                this.$nextTick(() => (this.$refs[ioType === 2 ? 'outputTable' : 'inputTable'] as MaterialTableRef | undefined)?.expandMaterial(main!))
            } else {
                const all = this.process.materials ?? []
                const selectedIndex = main && Number(main.ioType) === ioType ? all.indexOf(main) : -1
                const lastRow = rows[rows.length - 1]
                const anchorIndex = placement?.row ? all.indexOf(placement.row) : -1
                const position = placement
                    ? (anchorIndex >= 0 ? anchorIndex + (placement.position === 'before' ? 0 : 1) : lastRow ? all.indexOf(lastRow) + 1 : all.length)
                    : selectedIndex >= 0 ? selectedIndex + 1 : lastRow ? all.indexOf(lastRow) + 1 : all.length
                this.$emit('updateProcess', { materials: [...all.slice(0, position), ...additions, ...all.slice(position)] })
                this.selectMaterial({ row: additions[0], main: additions[0] })
            }
            this.$nextTick(() => additions.forEach(row => this.loadPublishedVersions(row, false, true)))
            return additions.length
        },
        removeMaterial({ row, main }: MaterialSelection) {
            if (this.readonly) return
            if (row === main) {
                this.$emit('updateProcess', { materials: this.process.materials.filter(item => item !== main) })
            } else {
                this.$emit('updateMaterial', {
                    row: main, changes: {
                        substitutes: (main.substitutes ?? []).filter(item => item !== row)
                    }
                })
            }
            if (this.selection?.row === row || (row === main && this.selection?.main === main)) this.selectMaterial(null)
        },
        async getDefaultLoss() {
            try {
                const result = await this.$http.post("/api/base/SystemPara/GetAdditionalParamByTypes",
                    [SystemParaType.耗材偏差值]
                )
                this.defaultLoss = Number(result.data?.[0]?.systemPara?.dataValue ?? 0)
            } catch (error) {
                this.$kingzon_messager.showError(error)
            }
        }
    }
})
</script>

<style scoped>
.editor-content {
    display: flex;
    flex-direction: column;
    gap: 6px;
    width: 100%;
    height: 100%;
    min-height: 0;
    padding: 6px 8px;
    box-sizing: border-box;
    overflow: auto;
}

.material-guide {
    display: flex;
    flex-wrap: wrap;
    gap: 4px 12px;
    flex-shrink: 0;
    font-size: 11px;
    color: #64748b;
}

.material-guide>span {
    margin-left: auto;
    color: #888;
}

.data-section {
    display: flex;
    flex-direction: column;
    min-width: 0;
}

.input-section {
    flex: 1 0 200px;
    min-height: 200px;
}

.section-title {
    display: flex;
    align-items: center;
    gap: 8px;
    flex-shrink: 0;
    height: 26px;
}

.section-title h3 {
    margin: 0;
    font-size: 13px;
    color: var(--color-blue);
}

.section-title>span {
    font-size: 11px;
    color: #888;
}

.input-table {
    flex: 1 1 0;
}

.output-section {
    flex: 0 0 156px;
}

.output-table {
    height: 130px;
}
</style>
