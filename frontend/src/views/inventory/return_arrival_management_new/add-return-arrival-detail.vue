<template>
    <div class="kingzon-page-content" style="min-width: 480px; padding: 16px">
        <Form :model="form" class="kingzon-page-form" labelAlign="right" :labelWidth="85">
            <FormField label="退货单号" style="flex-basis: 100%">{{ row.saleReturnOrderNo }}</FormField>
            <FormField label="产品" style="flex-basis: 100%">{{ row.productNo }} - {{ row.productName }}</FormField>
            <FormField label="待到货数量" style="flex-basis: 100%">{{ row.remainingQuantity }}{{ row.productUnit }}</FormField>
            <FormField label="到货日期" style="flex-basis: 100%"><DateBox v-model="form.arrivalTime" format="yyyy-MM-dd" style="width: 180px" /></FormField>
            <FormField label="到货数量" style="flex-basis: 100%">
                <NumberBox v-model="form.arrivalQuantity" :min="0" :max="maxQuantity" :precision="4" :spinners="false" style="width: 180px" />
                <ComboBox v-model="form.arrivalUnitConversionId" :data="unitOptions" @valueChange="onUnitChange" style="width: 120px; margin-left: 6px" />
            </FormField>
            <FormField label="备注" style="flex-basis: 100%"><TextBox v-model="form.remark" :multiline="true" style="width: 350px; height: 55px" /></FormField>
        </Form>
        <div class="kingzon-btn-group kingzon-page-footer"><LinkButton @click="$emit('cancel')">取消</LinkButton><LinkButton btnCls="c8" @click="submit">保存</LinkButton></div>
    </div>
</template>

<script lang="ts">
import { defineComponent, type PropType } from 'vue'
import type { SelectOption } from '@/types/types'
import { UnitConversionHelper } from '@/utils/UnitConversionHelper'
import { PostReturnArrivalRecordDTO, type ReturnArrivalDetailDTO } from '../../../types/fragments/inventory'

export default defineComponent({
    name: 'addReturnArrivalDetail',
    emits: ['cancel', 'submit'],
    props: { row: { type: Object as PropType<ReturnArrivalDetailDTO>, required: true } },
    data() { return { isLoading: false, form: new PostReturnArrivalRecordDTO() } },
    computed: {
        unitOptions(): SelectOption[] {
            return [
                { value: -1, text: this.row.productUnit },
                ...this.row.unitInfos.map(unit => ({ value: unit.id, text: unit.unitName })),
            ]
        },
        maxQuantity(): number {
            return new UnitConversionHelper(-1, this.row.remainingQuantity, this.row.unitInfos)
                .convertToUnit(this.form.arrivalUnitConversionId)?.quantity ?? 0
        },
        baseArrivalQuantity(): number {
            return new UnitConversionHelper(this.form.arrivalUnitConversionId, Number(this.form.arrivalQuantity || 0), this.row.unitInfos)
                .convertToUnit(-1)?.quantity ?? 0
        },
    },
    created() { this.form = this.createRecord(this.row) },
    methods: {
        createRecord(row: ReturnArrivalDetailDTO) {
            return new PostReturnArrivalRecordDTO({ saleReturnOrderDetailId: row.id, arrivalUnitConversionId: -1 })
        },
        onUnitChange() { this.form.arrivalQuantity = Math.min(this.form.arrivalQuantity, this.maxQuantity) },
        submit() {
            if (!this.form.arrivalQuantity || this.form.arrivalQuantity <= 0 || this.baseArrivalQuantity > this.row.remainingQuantity) return this.$kingzon_messager.showError('请填写正确的到货数量')
            this.isLoading = true
            this.$http.post('api/inventory/ReturnArrival/SaveReturnArrivalRecords', [this.form])
                .then((res: any) => { this.$kingzon_messager.showInfo(res.data || '到货记录新增成功'); this.$emit('submit') })
                .catch((err: unknown) => this.$kingzon_messager.showError(err || '到货记录新增失败'))
                .finally(() => { this.isLoading = false })
        },
    },
})
</script>
