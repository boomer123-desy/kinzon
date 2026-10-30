<template>
    <Loading v-show="isLoading" />
    <div class="kingzon-page-content" style="min-width: 430px; padding: 16px">
        <Form :model="form" class="kingzon-page-form" labelAlign="right" :labelWidth="90">
            <FormField label="退货单号" style="flex-basis: 100%">{{ row.saleReturnOrderNo }}</FormField>
            <FormField label="客户" style="flex-basis: 100%">{{ row.customerCode }} - {{ row.customerName }}</FormField>
            <FormField label="退货日期" style="flex-basis: 100%">
                <DateBox v-model="form.returnTime" format="yyyy-MM-dd" style="width: 180px" />
            </FormField>
            <FormField label="退货单备注" style="flex-basis: 100%">
                <TextBox v-model="form.remark" :multiline="true" style="width: 360px; height: 55px" />
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
import type { SaleReturnOrderDetailViewDTO } from '@/types/sale'

export default defineComponent({
    name: 'editSaleReturnOrderHeader',
    emits: ['cancel', 'submit'],
    props: { row: { type: Object as PropType<SaleReturnOrderDetailViewDTO>, required: true } },
    data() {
        return {
            isLoading: false,
            form: { id: 0, returnTime: new Date(), remark: '' },
        }
    },
    created() {
        this.form = {
            id: this.row.saleReturnOrderId,
            returnTime: this.row.saleReturnOrderReturnTime ? new Date(this.row.saleReturnOrderReturnTime) : new Date(),
            remark: this.row.saleReturnOrderRemark ?? '',
        }
    },
    methods: {
        submit() {
            this.isLoading = true
            this.$http.post('api/sale/SaleReturnOrderManage/UpdateSaleReturnOrder', this.form)
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
