<template>
    <Loading v-show="isLoading" />
    <div class="kingzon-page-content" style="min-width: 520px; padding: 16px">
        <Form :model="form" class="kingzon-page-form" labelAlign="right" :labelWidth="85">
            <FormField label="退货单号" style="flex-basis: 100%">{{ row.saleReturnOrderNo }}</FormField>
            <FormField label="产品" style="flex-basis: 100%">{{ row.productNo }} - {{ row.productName }}</FormField>
            <FormField label="退货类型" style="flex: 0 0 50%">
                <ComboBox v-model="form.saleReturnType" :data="saleReturnTypeList" style="width: 130px" />
            </FormField>
            <FormField label="退货数量" style="flex-basis: 100%">
                <NumberBox v-model="form.returnQuantity" :min="0" :max="maxQuantity" :precision="4" :spinners="false" style="width: 160px" />
                <ComboBox v-model="form.returnUnitId" :data="unitOptions" style="width: 120px; margin-left: 6px" />
            </FormField>
            <FormField label="单价" style="flex: 0 0 50%">{{ unitPrice.toFixed(4) }} 元/{{ selectedUnitName }}</FormField>
            <FormField label="总价" style="flex: 0 0 50%">{{ totalPrice.toFixed(4) }} 元</FormField>
            <FormField label="退货原因" style="flex-basis: 100%">
                <TextBox v-model="form.reason" style="width: 390px" />
            </FormField>
            <FormField label="明细备注" style="flex-basis: 100%">
                <TextBox v-model="form.remark" :multiline="true" style="width: 390px; height: 45px" />
            </FormField>
        </Form>
        <div class="kingzon-btn-group kingzon-page-footer">
            <LinkButton @click="$emit('cancel')">取消</LinkButton>
            <LinkButton btnCls="c8" @click="submit">保存</LinkButton>
        </div>
    </div>
</template>

<script lang="ts">
import { defineComponent, type PropType } from 'vue'
import { SaleReturnType } from '@/types/enums'
import type { SelectOption } from '@/types/types'
import type { SaleReturnOrderDetailViewDTO } from '@/types/sale'
import { UnitConversionHelper } from '@/utils/UnitConversionHelper'

export default defineComponent({
    name: 'editSaleReturnOrder',
    emits: ['cancel', 'submit'],
    props: {
        row: { type: Object as PropType<SaleReturnOrderDetailViewDTO>, required: true },
    },
    data() {
        return {
            isLoading: false,
            saleReturnTypeList: [] as SelectOption[],
            unitOptions: [] as SelectOption[],
            form: {
                id: 0,
                returnQuantity: 0,
                returnUnitId: -1,
                unitInfos: [] as any[],
                saleReturnType: SaleReturnType.成品,
                reason: '',
                remark: '',
            },
        }
    },
    computed: {
        selectedUnit() {
            return (this.form.unitInfos ?? []).find((item: any) => item.id === this.form.returnUnitId)
        },
        selectedUnitName(): string {
            return this.selectedUnit?.unitName ?? this.row.productUnit
        },
        conversionRatio(): number {
            return this.selectedUnit?.ratio ?? 1
        },
        unitPrice(): number {
            return Number(this.row.price ?? 0) / this.conversionRatio
        },
        baseQuantity(): number {
            return new UnitConversionHelper(this.form.returnUnitId, Number(this.form.returnQuantity || 0), this.form.unitInfos)
                .convertToUnit(-1)?.quantity ?? 0
        },
        totalPrice(): number {
            return Number(this.row.price ?? 0) * this.baseQuantity
        },
        maxQuantity(): number | undefined {
            if (!this.row.deliveryOrderNo) return undefined
            const currentUnitId = this.row.inputUnitConversionId ?? -1
            const actualDeliveryBase = new UnitConversionHelper(currentUnitId, this.row.actualDeliveryQuantity, this.form.unitInfos)
                .convertToUnit(-1)?.quantity ?? 0
            const returnedBase = new UnitConversionHelper(currentUnitId, this.row.deliveryOrderReturnQuantity, this.form.unitInfos)
                .convertToUnit(-1)?.quantity ?? 0
            const availableBase = actualDeliveryBase - returnedBase + this.row.returnQuantity
            return new UnitConversionHelper(-1, availableBase, this.form.unitInfos)
                .convertToUnit(this.form.returnUnitId)?.quantity
        },
    },
    created() {
        this.saleReturnTypeList = this.$utils.getEnumKeyValueArray(SaleReturnType)
        this.form = {
            id: this.row.id,
            returnQuantity: this.row.inputReturnQuantity ?? this.row.returnQuantity,
            returnUnitId: this.row.inputUnitConversionId ?? -1,
            unitInfos: this.row.unitInfos ?? [],
            saleReturnType: this.row.saleReturnType,
            reason: this.row.reason ?? '',
            remark: this.row.remark ?? '',
        }
        this.unitOptions = [
            { value: -1, text: this.row.productUnit },
            ...this.form.unitInfos.map((item: any) => ({ value: item.id, text: item.unitName })),
        ]
    },
    methods: {
        submit() {
            if (!this.form.returnQuantity || this.form.returnQuantity <= 0 || (this.maxQuantity !== undefined && this.form.returnQuantity > this.maxQuantity)) {
                this.$kingzon_messager.showError('请填写正确的退货数量')
                return
            }
            this.isLoading = true
            this.$http.post('api/sale/SaleReturnOrderManage/UpdateSaleReturnOrderDetail', this.form)
                .then((res: any) => {
                    this.$kingzon_messager.showInfo(res.data || '保存成功')
                    this.$emit('submit')
                })
                .catch((err: unknown) => this.$kingzon_messager.showError(err || '保存失败'))
                .finally(() => { this.isLoading = false })
        },
    },
})
</script>
