<template>
    <Dialog ref="dialog" :title="processName + ' · 工序物料'" :modal="true" :closed="true"
        :dialogStyle="{ width: '1200px', maxWidth: '96vw', height: '70vh' }" @close="$emit('close')">
        <div class="kingzon-page-content" style="min-height: 0; padding: 5px; box-sizing: border-box">
            <div style="display: flex; flex-wrap: wrap; align-items: center; gap: 12px; padding: 5px 0">
                <span>已配置 {{ rows.length }} 项物料</span><span>齐套 {{ readyCount }} 项</span>
                <span>缺料 {{ shortageCount }} 项</span><span>未查询 {{ uncheckedCount }} 项</span>
                <LinkButton :disabled="checkingStock || saving" @click="$emit('checkStock')">{{ checkingStock ? '正在校验库存…' : '校验库存' }}</LinkButton>
                <span class="kingzon-text-grey">按全部工序共用库存计算，不占用库存</span>
            </div>
            <div style="display: flex; flex: 1; gap: 8px; min-height: 0; overflow: auto">
                <KingZonDataGrid id="scheduleMaterials" :data="rows" :pagination="false" :lazy="false" :virtualScroll="false"
                    :showFieldFilter="false" :showDownLoad="false" style="flex: 1; min-width: 550px; height: 100%">
                    <GridColumnGroup>
                        <GridHeaderRow>
                            <GridColumn field="code" title="编码" width="105" />
                            <GridColumn field="name" title="物料名称" width="135" />
                            <GridColumn field="quantity" title="需求数量" width="115">
                                <template #body="{ row }"><NumberBox :modelValue="row.quantity" :min="0" :precision="6" :disabled="saving"
                                    style="width: 100px" @update:modelValue="changeQuantity(row, $event)" /></template>
                            </GridColumn>
                            <GridColumn field="unitId" title="单位" width="95">
                                <template #body="{ row }"><ComboBox :modelValue="`${row.unitConversionId ?? 0}:${row.unitId}`" :data="unitOptions(row)"
                                    valueField="value" textField="unitName" :editable="false" :disabled="saving" style="width: 85px"
                                    @selectionChange="changeUnit(row, $event)" /></template>
                            </GridColumn>
                            <GridColumn field="allocated" title="库存满足" width="90" align="right">
                                <template #body="{ row }">{{ stockRow(row)?.allocated ?? '未查询' }}</template>
                            </GridColumn>
                            <GridColumn field="shortage" title="缺料" width="90" align="right">
                                <template #body="{ row }">{{ stockRow(row)?.shortage ?? '未查询' }}</template>
                            </GridColumn>
                            <GridColumn title="操作" width="60"><template #body="{ row }">
                                <LinkButton plain :disabled="saving" @click="removeMaterial(row)">删除</LinkButton>
                            </template></GridColumn>
                        </GridHeaderRow>
                    </GridColumnGroup>
                </KingZonDataGrid>
                <ResourceLibrary :selectTabIndex="1" :craftId="craftId" :allowOutput="false" :disabled="saving"
                    style="width: 380px; flex-shrink: 0" @select-resources="addMaterials" />
            </div>
            <p v-if="stockError" style="color: var(--color-red)">{{ stockError }}</p>
            <p v-if="error" style="color: var(--color-red)">{{ error }}</p>
            <div class="kingzon-btn-group kingzon-page-footer">
                <LinkButton :disabled="saving" @click="cancel">取消</LinkButton>
                <LinkButton :disabled="saving" @click="confirm">确认物料配置</LinkButton>
            </div>
        </div>
    </Dialog>
</template>

<script lang="ts">
import { defineComponent, type PropType } from 'vue'
import ResourceLibrary from '@/views/technology/craft_manage/component/resource-library.vue'
import type { MaterialResource } from '@/types/craft'
import { ScheduleMaterialEditDto, ScheduleUnitOptionDto, type ScheduleMaterialAvailabilityDto } from '@/types/production-schedule'

interface DialogRef { open(): void; close(): void }
export default defineComponent({
    name: 'ScheduleMaterialDialog', components: { ResourceLibrary },
    props: {
        modelValue: { type: Array as PropType<ScheduleMaterialEditDto[]>, default: () => [] },
        availability: { type: Object as PropType<Record<string, ScheduleMaterialAvailabilityDto>>, default: () => ({}) },
        processKey: { type: String, required: true }, processName: { type: String, default: '' },
        craftId: { type: Number, default: 0 }, quantity: { type: Number, default: 0 }, checkingStock: Boolean, saving: Boolean, stockError: { type: String, default: '' }
    },
    emits: ['update:modelValue', 'preview', 'close', 'checkStock'],
    data() { return { rows: [] as ScheduleMaterialEditDto[], nextKey: 1, error: '' } },
    computed: {
        readyCount(): number { return this.rows.filter(row => this.stockRow(row)?.shortage === 0).length },
        shortageCount(): number { return this.rows.filter(row => (this.stockRow(row)?.shortage ?? 0) > 0).length },
        uncheckedCount(): number { return this.rows.filter(row => this.stockRow(row)?.shortage == null).length }
    },
    methods: {
        open() {
            this.error = ''
            this.rows = this.modelValue.map(row => new ScheduleMaterialEditDto(JSON.parse(JSON.stringify(row))))
            this.preview(); (this.$refs.dialog as DialogRef).open()
        },
        cancel() { (this.$refs.dialog as DialogRef).close(); this.$emit('close') },
        preview() { this.error = ''; this.$emit('preview', this.rows) },
        stockRow(row: ScheduleMaterialEditDto) { return this.availability[`${this.processKey}/${row.key}`] },
        unitOptions(material: ScheduleMaterialEditDto) {
            return material.unitOptions.map(unit => ({ ...unit, value: `${unit.id}:${unit.unitId}` }))
        },
        changeQuantity(material: ScheduleMaterialEditDto, value: number | null) {
            if (this.saving) return
            material.quantity = Number(value) || 0
            if (this.quantity > 0) material.quantityPerUnit = material.quantity / this.quantity
            this.preview()
        },
        changeUnit(material: ScheduleMaterialEditDto, option: ScheduleUnitOptionDto | null) {
            if (this.saving || !option || option.ratio <= 0) return
            const old = material.unitOptions.find(unit => unit.id === (material.unitConversionId ?? 0) && unit.unitId === material.unitId)
            if (!old || old.ratio <= 0) return
            material.quantityPerUnit = material.quantityPerUnit * old.ratio / option.ratio
            material.quantity = Number((material.quantityPerUnit * this.quantity).toFixed(6))
            material.unitId = option.unitId; material.unitConversionId = option.id; this.preview()
        },
        removeMaterial(material: ScheduleMaterialEditDto) {
            if (this.saving) return
            this.rows = this.rows.filter(row => row !== material); this.preview()
        },
        addMaterials({ resources }: { resources: MaterialResource[] }) {
            if (this.saving) return
            for (const resource of resources) {
                const id = Number(resource.dataId), type = Number(resource.dataType), unitId = Number(resource.unitId)
                if (!(id > 0) || !(unitId > 0) || !Number.isFinite(type) || this.rows.some(row => row.dataId === id && row.dataType === type)) continue
                const unit = new ScheduleUnitOptionDto({ id: 0, unitId, unitName: resource.unitName ?? '', ratio: 1 })
                let key = ''
                do { key = `${this.processKey}-added-material-${this.nextKey++}` } while (this.rows.some(row => row.key === key))
                this.rows.push(new ScheduleMaterialEditDto({
                    key, dataId: id, dataType: type, code: resource.materialNo ?? resource.code ?? '',
                    name: resource.materialName ?? resource.name ?? '', quantityPerUnit: 1, quantity: this.quantity,
                    unitId, unitConversionId: 0, unitOptions: [unit]
                }))
            }
            this.preview()
        },
        confirm() {
            if (this.saving) return
            const keys = new Set<string>()
            for (const row of this.rows) {
                const key = `${row.dataType}:${row.dataId}`
                const units = row.unitOptions.filter(unit => unit.id === (row.unitConversionId ?? 0) && unit.unitId === row.unitId)
                if (keys.has(key) || !Number.isFinite(row.quantity) || row.quantity < 0 || row.quantity >= 1e12
                    || Number(row.quantity.toFixed(6)) !== row.quantity || units.length !== 1 || !Number.isFinite(units[0].ratio) || units[0].ratio <= 0) {
                    this.error = '请检查重复物料、需求数量及单位换算'; return
                }
                keys.add(key)
            }
            this.$emit('update:modelValue', this.rows.map(row => new ScheduleMaterialEditDto(JSON.parse(JSON.stringify(row)))))
            this.cancel()
        }
    }
})
</script>
