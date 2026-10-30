<template>
    <div class="kingzon-page-content" style="min-width: 760px; padding: 16px">
        <Form :model="form" class="kingzon-page-form" labelAlign="right" :labelWidth="85">
            <FormField label="退货单号">{{ orderNoText }}</FormField>
            <FormField label="到货日期"><DateBox v-model="form.arrivalTime" format="yyyy-MM-dd" style="width: 180px" /></FormField>
            <FormField label="备注" style="flex-basis: 100%"><TextBox v-model="form.remark" :multiline="true" style="width: 450px; height: 45px" /></FormField>
        </Form>
        <KingZonDataGrid class="kingzon-page-fill" :data="details" :pagination="false" selectionMode="null" emptyMsg="没有待到货明细">
            <GridColumnGroup><GridHeaderRow>
                <GridColumn field="productNo" title="产品编码" width="120" /><GridColumn field="productName" title="产品名称" width="160" />
                <GridColumn field="remainingQuantity" title="待到货数量" align="right" width="120"><template #body="scope">{{ scope.row.remainingQuantity }}{{ scope.row.productUnit }}</template></GridColumn>
                <GridColumn field="arrivalQuantity" title="本次到货数量" align="right" width="230"><template #body="scope"><NumberBox v-model="scope.row.arrivalQuantity" :min="0" :max="getMax(scope.row)" :precision="4" :spinners="false" style="width: 115px" /><ComboBox v-model="scope.row.arrivalUnitConversionId" :data="getUnitOptions(scope.row)" @valueChange="onUnitChange(scope.row)" style="width: 100px; margin-left: 4px" /></template></GridColumn>
            </GridHeaderRow></GridColumnGroup>
        </KingZonDataGrid>
        <div class="kingzon-btn-group kingzon-page-footer"><LinkButton @click="$emit('cancel')">取消</LinkButton><LinkButton btnCls="c8" @click="submit">保存</LinkButton></div>
    </div>
</template>

<script lang="ts">
import { defineComponent, type PropType } from 'vue'
import type { SelectOption } from '@/types/types'
import { UnitConversionHelper } from '@/utils/UnitConversionHelper'
import { PostReturnArrivalRecordDTO, type ReturnArrivalDetailDTO } from '../inventory'

export default defineComponent({
    name: 'addReturnArrivalOrder',
    emits: ['cancel', 'submit'],
    props: { rows: { type: Array as PropType<ReturnArrivalDetailDTO[]>, required: true } },
    data() {
        return { isLoading: false, form: { arrivalTime: new Date(), remark: '' }, details: [] as Array<ReturnArrivalDetailDTO & { arrivalQuantity: number, arrivalUnitConversionId: number }> }
    },
    computed: {
        orderNoText(): string {
            const orderNos = [...new Set(this.rows.map(row => row.saleReturnOrderNo).filter(Boolean))]
            return orderNos.length <= 1 ? (orderNos[0] ?? '') : `多单（${orderNos.length} 单）`
        }
    },
    created() { this.details = this.rows.filter(row => !row.isComplete).map(row => ({ ...row, arrivalQuantity: 0, arrivalUnitConversionId: -1 })) },
    methods: {
        getUnitOptions(row: ReturnArrivalDetailDTO): SelectOption[] {
            return [{ value: -1, text: row.productUnit }, ...row.unitInfos.map(unit => ({ value: unit.id, text: unit.unitName }))]
        },
        getMax(row: ReturnArrivalDetailDTO & { arrivalUnitConversionId: number }): number {
            return new UnitConversionHelper(-1, row.remainingQuantity, row.unitInfos).convertToUnit(row.arrivalUnitConversionId)?.quantity ?? 0
        },
        getBaseQuantity(row: ReturnArrivalDetailDTO & { arrivalQuantity: number, arrivalUnitConversionId: number }): number {
            return new UnitConversionHelper(row.arrivalUnitConversionId, Number(row.arrivalQuantity || 0), row.unitInfos).convertToUnit(-1)?.quantity ?? 0
        },
        onUnitChange(row: ReturnArrivalDetailDTO & { arrivalQuantity: number, arrivalUnitConversionId: number }) { row.arrivalQuantity = Math.min(row.arrivalQuantity, this.getMax(row)) },
        submit() {
            const selected = this.details.filter(row => row.arrivalQuantity > 0)
            if (!selected.length || selected.some(row => this.getBaseQuantity(row) > row.remainingQuantity)) return this.$kingzon_messager.showError('请至少填写一条正确的到货数量')
            const records = selected.map(row => new PostReturnArrivalRecordDTO({ saleReturnOrderDetailId: row.id, arrivalQuantity: row.arrivalQuantity, arrivalUnitConversionId: row.arrivalUnitConversionId, arrivalTime: this.form.arrivalTime, remark: this.form.remark }))
            this.isLoading = true
            this.$http.post('api/inventory/ReturnArrival/SaveReturnArrivalRecords', records)
                .then((res: any) => { this.$kingzon_messager.showInfo(res.data || '到货记录新增成功'); this.$emit('submit') })
                .catch((err: unknown) => this.$kingzon_messager.showError(err || '到货记录新增失败'))
                .finally(() => { this.isLoading = false })
        },
    },
})
</script>
