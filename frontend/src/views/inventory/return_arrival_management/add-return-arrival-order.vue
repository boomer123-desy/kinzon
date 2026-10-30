<template>
    <Loading v-show="isLoading" />
    <div class="kingzon-page-content" style="padding: 10px">
        <Form :model="form" class="kingzon-page-form" labelAlign="right" :labelWidth="80">
            <FormField label="退货单号">{{ orderNoText }}</FormField>
            <FormField label="到货日期"><DateBox v-model="form.arrivalTime" format="yyyy-MM-dd" :panelStyle="{ zIndex: 100001 }" style="width: 180px" /></FormField>
            <FormField label="到货备注" style="flex-basis: 100%"><TextBox v-model="form.remark" :multiline="true" style="width: 100%; height: 44px" /></FormField>
        </Form>
        <KingZonDataGrid class="kingzon-page-fill" :data="details" :pagination="false" selectionMode="null" emptyMsg="没有待到货明细">
            <GridColumnGroup :frozen="true" align="left" width="420"><GridHeaderRow>
                <GridColumn field="productNo" title="产品编码" width="120" /><GridColumn field="productName" title="产品名称" width="160" :overflowTextEllipsis="true" />
                <GridColumn field="customerMaterialNo" title="客户产品图号" width="140" :overflowTextEllipsis="true" />
            </GridHeaderRow></GridColumnGroup>
            <GridColumnGroup><GridHeaderRow>
                <GridColumn field="productSpecification" title="规格型号" width="120" :overflowTextEllipsis="true" />
                <GridColumn field="technicalCode" title="技术型号" width="110" :overflowTextEllipsis="true" />
                <GridColumn field="remainingQuantity" title="待到货数量" align="right" width="120"><template #body="scope"><UnitFormQuantityShow :quantity="scope.row.remainingQuantity" :unitList="scope.row.unitInfos" :unitId="scope.row.returnUnitConversionId" /></template></GridColumn>
                <GridColumn field="arrivalQuantity" title="本次到货数量" align="right" width="230"><template #body="scope"><NumberBox v-model="scope.row.arrivalQuantity" :min="0" :max="getMax(scope.row)" :precision="4" :spinners="false" style="width: 115px" /><ComboBox v-model="scope.row.arrivalUnitConversionId" :data="getUnitOptions(scope.row)" @valueChange="onUnitChange(scope.row)" style="width: 100px; margin-left: 4px" /></template></GridColumn>
                <GridColumn v-if="isInbound" field="positionName" title="入库库位" width="210"><template #body="scope"><TextBox v-model="scope.row.positionName" :disabled="true" style="width: 165px" /><font-awesome-icon :icon="['fas', 'warehouse']" class="kingzon-pointer" style="margin-left: 6px; color: var(--color-blue)" @click="selectPosition(scope.row)" /></template></GridColumn>
            </GridHeaderRow></GridColumnGroup>
        </KingZonDataGrid>
        <div class="kingzon-btn-group kingzon-page-footer"><LinkButton @click="$emit('cancel')">取消</LinkButton><LinkButton btnCls="c8" @click="submit">{{ isInbound ? '到货并入库' : '确认到货' }}</LinkButton></div>
    </div>
    <Dialog ref="positionDialog" title="选择库位" :modal="true" closed :draggable="true" :dialogStyle="{ height: '85%', width: '85%' }">
        <SelectPosition :dataType="selectedRow.dataType" :dataId="selectedRow.dataId" :dataStatus="InventoryDataStatus.None" :positionId="selectedRow.positionId" @close="closePosition" @confirm="confirmPosition" />
    </Dialog>
</template>
<script lang="ts">
import { defineComponent, type PropType } from 'vue'
import type { SelectOption } from '@/types/types'
import { UnitConversionHelper } from '@/utils/UnitConversionHelper'
import { InventoryDataStatus, InventoryDataType } from '@/types/enums'
import type { ReturnArrivalDetailDTO } from '@/types/inventory'
import UnitFormQuantityShow from './unit-form-quantity-show.vue'
import SelectPosition from '@/views/inventory_management/select-position.vue'

type ArrivalRow = ReturnArrivalDetailDTO & { arrivalQuantity: number, arrivalUnitConversionId: number, positionId: number, positionName: string, dataType: InventoryDataType, dataId: number }
export default defineComponent({
    name: 'addReturnArrivalOrder', emits: ['cancel', 'submit'], components: { UnitFormQuantityShow, SelectPosition },
    props: { rows: { type: Array as PropType<ReturnArrivalDetailDTO[]>, required: true }, isInbound: { type: Boolean, default: false } },
    data() { return { isLoading: false, form: { arrivalTime: new Date(), remark: '' }, details: [] as ArrivalRow[], selectedRow: {} as ArrivalRow } },
    computed: {
        InventoryDataStatus() { return InventoryDataStatus },
        orderNoText(): string { const nos = [...new Set(this.rows.map(row => row.saleReturnOrderNo).filter(Boolean))]; return nos.length <= 1 ? (nos[0] ?? '') : `多单（${nos.length} 单）` },
    },
    created() { this.details = this.rows.filter(row => !row.isComplete).map(row => ({ ...row, arrivalQuantity: 0, arrivalUnitConversionId: -1, positionId: 0, positionName: '', dataType: (row as any).dataType ?? InventoryDataType.Product, dataId: (row as any).dataId ?? row.productId })) as ArrivalRow[] },
    methods: {
        getUnitOptions(row: ArrivalRow): SelectOption[] { return [{ value: -1, text: row.productUnit || '主单位' }, ...row.unitInfos.map(unit => ({ value: unit.id, text: unit.unitName }))] },
        getMax(row: ArrivalRow): number { return new UnitConversionHelper(-1, row.remainingQuantity, row.unitInfos).convertToUnit(row.arrivalUnitConversionId)?.quantity ?? 0 },
        getBaseQuantity(row: ArrivalRow): number { return new UnitConversionHelper(row.arrivalUnitConversionId, Number(row.arrivalQuantity || 0), row.unitInfos).convertToUnit(-1)?.quantity ?? 0 },
        onUnitChange(row: ArrivalRow) { row.arrivalQuantity = Math.min(Number(row.arrivalQuantity || 0), this.getMax(row)) },
        selectPosition(row: ArrivalRow) { this.selectedRow = row; (this.$refs.positionDialog as EasyUI.Dialog)?.open() },
        closePosition() { (this.$refs.positionDialog as EasyUI.Dialog)?.close() },
        confirmPosition(position: any) { this.selectedRow.positionId = position.id; this.selectedRow.positionName = position.fullName ?? ''; this.closePosition() },
        getUnitQtyList(row: ArrivalRow) { const base = this.getBaseQuantity(row); return row.unitInfos.filter(unit => Number(unit.ratio) > 0).map(unit => ({ id: unit.id, unitId: unit.unitId, unitName: unit.unitName, ratio: unit.ratio, conversionType: unit.conversionType ?? null, quantity: base / Number(unit.ratio) })) },
        submit() {
            const selected = this.details.filter(row => Number(row.arrivalQuantity) > 0)
            if (!selected.length || selected.some(row => this.getBaseQuantity(row) > row.remainingQuantity)) return this.$kingzon_messager.showError('请至少填写一条正确的到货数量')
            if (this.isInbound && selected.some(row => !row.positionId)) return this.$kingzon_messager.showError('到货并入库时必须选择每条明细的库位')
            const records = selected.map(row => ({ saleReturnOrderDetailId: row.id, arrivalQuantity: row.arrivalQuantity, arrivalUnitConversionId: row.arrivalUnitConversionId, arrivalTime: this.form.arrivalTime, remark: this.form.remark, ...(this.isInbound ? { inventoryRecordList: [{ positionId: row.positionId, remark: this.form.remark, unitQtyList: this.getUnitQtyList(row) }] } : {}) }))
            this.isLoading = true
            this.$http.post(this.isInbound ? 'api/inventory/ReturnArrival/SaveReturnArrivalAndWarehousing' : 'api/inventory/ReturnArrival/SaveReturnArrivalRecords', this.isInbound ? { arrivalRecords: records } : records)
                .then((res: any) => { this.$kingzon_messager.showInfo(res.data || (this.isInbound ? '到货并入库成功' : '到货记录新增成功')); this.$emit('submit') })
                .catch((err: unknown) => this.$kingzon_messager.showError(err || '到货保存失败'))
                .finally(() => { this.isLoading = false })
        },
    },
})
</script>
