<template>
    <Loading v-show="isLoading" />
    <div class="kingzon-page-content" style="min-width: 650px; min-height: 500px">
        <Form :model="returnOrder" class="kingzon-page-form" labelAlign="right" :labelWidth="70"
            style="justify-content: flex-start">
            <FormField name="customerId" label="客户:" style="flex: 0 0 35%">
                <ComboBox v-model="returnOrder.customerId" :data="customerOptions" textField="text" valueField="value"
                    style="width: 220px" />
            </FormField>
            <FormField name="returnTime" label="退货日期:" style="flex: 0 0 35%">
                <DateBox v-model="returnOrder.returnTime" format="yyyy-MM-dd" style="width: 180px" />
            </FormField>
            <FormField name="remark" label="备注:" style="flex-basis: 100%">
                <TextBox v-model="returnOrder.remark" :multiline="true" style="width: 450px; height: 40px" />
            </FormField>
        </Form>

        <div class="kingzon-btn-group" style="text-align: left; margin: 5px">
            <LinkButton btnCls="c8" @click="chooseDeliveryOrder">选择送货</LinkButton>
            <LinkButton @click="clearDetails" :disabled="details.length === 0">清空</LinkButton>
        </div>

        <KingZonDataGrid ref="detailsGrid" class="kingzon-page-fill" :pagination="false" :data="details"
            selectionMode="null" :columnResizing="true" :columnMoving="true" emptyMsg="请选择送货明细" :dblclickToEdit="true"
            editMode="cell" style="overflow-y: unset;">
            <GridColumnGroup :frozen="true" width="500">
                <GridHeaderRow>
                    <GridColumn align="center" width="40">
                        <template #body="scope">{{ scope.rowIndex + 1 }}</template>
                    </GridColumn>
                    <GridColumn field="deliveryOrderDetail.deliveryOrderNo" title="送货单号" width="140"
                        :overflowTextEllipsis="true">
                        <template #body="scope">{{ scope.row.deliveryOrderDetail.deliveryOrderNo }}</template>
                    </GridColumn>
                    <GridColumn field="deliveryOrderDetail.productNo" title="产品编码" width="100"
                        :overflowTextEllipsis="true">
                        <template #body="scope">{{ scope.row.deliveryOrderDetail.productNo }}</template>
                    </GridColumn>
                    <GridColumn field="deliveryOrderDetail.productCustomerMaterialNo" title="客户产品图号" width="130"
                        :overflowTextEllipsis="true">
                        <template #body="scope">{{ scope.row.deliveryOrderDetail.productCustomerMaterialNo }}</template>
                    </GridColumn>
                    <GridColumn field="deliveryOrderDetail.productName" title="产品名称" width="120"
                        :overflowTextEllipsis="true">
                        <template #body="scope">{{ scope.row.deliveryOrderDetail.productName }}</template>
                    </GridColumn>
                </GridHeaderRow>
            </GridColumnGroup>
            <GridColumnGroup>
                <GridHeaderRow>
                    <GridColumn field="deliveryOrderDetail.productSpecification" title="规格型号" width="120"
                        :overflowTextEllipsis="true">
                        <template #body="scope">{{ scope.row.deliveryOrderDetail.productSpecification }}</template>
                    </GridColumn>
                    <GridColumn field="deliveryTime" title="送货时间" width="100">
                        <template #body="scope">
                            {{ $utils.formatDateTime(scope.row.deliveryOrderDetail.deliveryTime, 'yyyy-MM-dd') }}
                        </template>
                    </GridColumn>
                    <GridColumn field="deliveryOrderDetail.deliveryQuantity" title="送货数量" align="right" width="90">
                        <template #body="scope">
                            <UnitQuantityShow :quantity="scope.row.deliveryOrderDetail.actualDeliveryQuantity"
                                :unitInfos="scope.row.deliveryOrderDetail.unitInfos" />
                        </template>
                    </GridColumn>
                    <GridColumn field="deliveryOrderDetail.returnQuantity" title="已退货数量" align="right" width="90">
                        <template #body="scope">
                            <UnitQuantityShow :quantity="scope.row.deliveryOrderDetail.returnQuantity"
                                :unitInfos="scope.row.deliveryOrderDetail.unitInfos" />
                        </template>
                    </GridColumn>
                    <GridColumn field="availableQuantity" title="可退货数量" align="right" width="100">
                        <template #body="scope">
                            <UnitQuantityShow :quantity="availableQuantity(scope.row)"
                                :unitInfos="scope.row.deliveryOrderDetail.unitInfos" />
                        </template>
                    </GridColumn>
                    <GridColumn field="returnQuantity" title="本次退货数量" align="right" width="180" :editable="true"
                        cellCss="kingzon-cell-editable">
                        <template #body="scope">
                            {{ scope.row.returnQuantity }}{{getUnitOptions(scope.row).find(it => it.value ==
                                scope.row.returnUnitId)?.text}}
                        </template>
                        <template #edit="scope">
                            <NumberBox style="flex:1.5" v-model="scope.row.returnQuantity" :min="0" :spinners="false"
                                :precision="4" :max="getMax(scope.row)">
                            </NumberBox>
                            <ComboBox style="flex:1" v-model="scope.row.returnUnitId" :data="getUnitOptions(scope.row)"
                                @valueChange="onUnitChange(scope.row)">
                            </ComboBox>
                        </template>
                    </GridColumn>
                    <GridColumn field="deliveryOrderDetail.saleOrderDetailPrice" title="单价" align="right" width="100">
                        <template #body="scope">{{ scope.row.deliveryOrderDetail.saleOrderDetailPrice.toFixed(4)
                        }}</template>
                    </GridColumn>
                    <GridColumn field="totalPrice" title="总价" align="right" width="100">
                        <template #body="scope">{{ getTotalPrice(scope.row) }}</template>
                    </GridColumn>
                    <GridColumn field="saleReturnType" title="退货类型" width="100" :editable="true"
                        cellCss="kingzon-cell-editable">
                        <template #body="scope">
                            {{ SaleReturnType[scope.row.saleReturnType] }}
                        </template>
                        <template #edit="scope">
                            <ComboBox v-model="scope.row.saleReturnType" :data="saleReturnTypeList"
                                style="width: 85px" />
                        </template>
                    </GridColumn>
                    <GridColumn field="reason" title="退货原因" width="150" :editable="true"
                        cellCss="kingzon-cell-editable">
                        <template #edit="scope">
                            <TextBox v-model="scope.row.reason" style="width: 135px" />
                        </template>
                    </GridColumn>
                    <GridColumn field="remark" title="明细备注" width="150" :editable="true"
                        cellCss="kingzon-cell-editable">
                        <template #edit="scope">
                            <TextBox v-model="scope.row.remark" style="width: 135px" />
                        </template>
                    </GridColumn>
                </GridHeaderRow>
            </GridColumnGroup>
            <GridColumnGroup :frozen="true" align="right" width="60">
                <GridHeaderRow>
                    <GridColumn field="remove" title="操作" align="center" width="65">
                        <template #body="scope">
                            <LinkButton @click="removeDetail(scope.row.deliveryOrderDetail.id)">删除</LinkButton>
                        </template>
                    </GridColumn>
                </GridHeaderRow>
            </GridColumnGroup>
        </KingZonDataGrid>

        <div style="margin: 8px 20px; text-align: right">合计：{{ totalPrice.toFixed(4) }} 元</div>
        <div class="kingzon-btn-group kingzon-page-footer">
            <LinkButton @click="$emit('cancel')">取消</LinkButton>
            <LinkButton btnCls="c8" @click="submit">保存</LinkButton>
        </div>
    </div>

    <Dialog ref="deliveryDialog" title="选择送货明细" :modal="true" :draggable="true" :resizable="true" closed>
        <DeliverOrderDetail :choiceCustomerId="returnOrder.customerId!"
            :selectedRows="details.map(item => item.deliveryOrderDetail)" @cancel="closeDeliveryDialog"
            @submit="selectDeliveryOrderDetails" />
    </Dialog>
</template>

<script lang="ts">
import { defineComponent } from 'vue'
import { SaleReturnType } from '@/types/enums'
import type { SelectOption } from '@/types/types'
import DeliverOrderDetail from './deliver-order-detail.vue'
import { DeliveryOrderDetailDTO, PostSaleReturnOrderDetailDTO, PostSaleReturnOrderDTO, SaleReturnOrderDetailDraft } from '@/types/sale.js'
import { UnitConversionHelper } from '@/utils/UnitConversionHelper.js'
import UnitQuantityShow from '@/components/unit_quantity_show/index.vue'


export default defineComponent({
    name: 'addSaleReturnOrder',
    components: { DeliverOrderDetail, UnitQuantityShow },
    emits: ['cancel', 'submit'],
    data() {
        return {
            isLoading: false,
            customerOptions: [] as SelectOption[],
            saleReturnTypeList: [] as SelectOption[],
            deliveryDialog: {} as EasyUI.Dialog,
            returnOrder: new PostSaleReturnOrderDTO(),
            details: [] as SaleReturnOrderDetailDraft[],
        }
    },
    computed: {
        SaleReturnType() {
            return SaleReturnType
        },
        totalPrice(): number {
            return this.details.reduce((total, detail) => {
                const quantity = new UnitConversionHelper(detail.returnUnitId ?? -1, Number(detail.returnQuantity || 0), detail.deliveryOrderDetail.unitInfos).convertToUnit(-1)?.quantity ?? 0
                return total + detail.deliveryOrderDetail.saleOrderDetailPrice * quantity
            }, 0)
        },
        UnitConversionHelper() {
            return UnitConversionHelper
        }
    },
    watch: {
        'returnOrder.customerId'(customerId: number, previousCustomerId: number) {
            if (previousCustomerId && customerId !== previousCustomerId) {
                this.details = []
            }
        },
    },
    created() {
        this.saleReturnTypeList = this.$utils.getEnumKeyValueArray(SaleReturnType)
        this.getCustomers()
    },
    mounted() {
        this.deliveryDialog = this.$refs.deliveryDialog as EasyUI.Dialog
    },
    methods: {
        getCustomers() {
            this.$http.post('api/sale/SaleReturnOrderManage/GetCustomers', [])
                .then((res: any) => {
                    this.customerOptions = (res.data ?? []).map((customer: any) => ({
                        value: customer.id,
                        text: `${customer.customerCode ?? customer.code ?? ''}-${customer.customerName ?? customer.name ?? ''}`,
                    }))
                })
                .catch((err: unknown) => this.$kingzon_messager.showError(err || '客户数据获取失败'))
        },
        chooseDeliveryOrder() {
            if (!this.returnOrder.customerId) {
                this.$kingzon_messager.showError('请先选择客户')
                return
            }
            this.deliveryDialog.open()
        },
        closeDeliveryDialog() {
            this.deliveryDialog.close()
        },
        selectDeliveryOrderDetails(rows: DeliveryOrderDetailDTO[]) {
            const existing = new Map(this.details.map(detail => [detail.deliveryOrderDetail.id, detail]))
            this.details = rows.map(row => existing.get(row.id) ?? new SaleReturnOrderDetailDraft({
                deliveryOrderDetail: row,
                returnQuantity: 0,
                returnUnitId: -1,
                saleReturnType: SaleReturnType.成品,
                reason: '',
                remark: '',
            }))
            this.closeDeliveryDialog()
        },
        availableQuantity(detail: SaleReturnOrderDetailDraft): number {
            const quantity =
                detail.deliveryOrderDetail.actualDeliveryQuantity -
                detail.deliveryOrderDetail.returnQuantity
            return quantity
        },
        onUnitChange(detail: SaleReturnOrderDetailDraft) {
            var max = this.getMax(detail)
            detail.returnQuantity = detail.returnQuantity > max ? max : detail.returnQuantity
        },
        getMax(detail: SaleReturnOrderDetailDraft) {
            const quantity =
                detail.deliveryOrderDetail.actualDeliveryQuantity -
                detail.deliveryOrderDetail.returnQuantity
            return new UnitConversionHelper(-1,
                quantity,
                detail.deliveryOrderDetail.unitInfos
            ).convertToUnit(detail.returnUnitId ?? -1)?.quantity ?? 0
        },
        getUnitOptions(row: SaleReturnOrderDetailDraft): SelectOption[] {
            return [
                { value: -1, text: row.deliveryOrderDetail.productUnit },
                ...row.deliveryOrderDetail.unitInfos.map(it => {
                return {
                    value: it.id,
                    text: it.unitName
                }
                }),
            ]
        },
        getTotalPrice(row: SaleReturnOrderDetailDraft) {
            var unit = new UnitConversionHelper(row.returnUnitId ?? -1, row.returnQuantity, row.deliveryOrderDetail.unitInfos).convertToUnit(-1)
            return (row.deliveryOrderDetail.saleOrderDetailPrice * (unit?.quantity ?? 0)).toFixed(4)
        },
        removeDetail(deliveryOrderDetailId: number) {
            this.details = this.details.filter(detail => detail.deliveryOrderDetail.id !== deliveryOrderDetailId)
        },
        clearDetails() {
            this.details = []
        },
        submit() {
            if (!this.returnOrder.customerId) {
                this.$kingzon_messager.showError('请先选择客户')
                return
            }
            if (this.details.length === 0) {
                this.$kingzon_messager.showError('请选择送货明细')
                return
            }
            if (this.details.some(detail => !detail.returnQuantity || detail.returnQuantity <= 0 || detail.returnQuantity > this.getMax(detail))) {
                this.$kingzon_messager.showError('请填写正确的本次退货数量')
                return
            }

            this.isLoading = true
            const postData = new PostSaleReturnOrderDTO({
                customerId: this.returnOrder.customerId,
                returnTime: this.returnOrder.returnTime,
                remark: this.returnOrder.remark,
                details: this.details.map(detail => {
                    return new PostSaleReturnOrderDetailDTO({
                        deliveryOrderDetailId: detail.deliveryOrderDetail.id,
                        returnQuantity: detail.returnQuantity,
                        saleReturnType: Number(detail.saleReturnType),
                        returnUnitId: detail.returnUnitId,
                        unitInfos: detail.deliveryOrderDetail.unitInfos,
                        reason: detail.reason,
                        remark: detail.remark,
                    })
                }),
            })

            this.$http.post('api/sale/SaleReturnOrderManage/PostSaleReturnOrder', postData)
                .then((res: any) => {
                    this.$kingzon_messager.showInfo('保存成功')
                    this.$emit('submit', res.data)
                })
                .catch((err: unknown) => this.$kingzon_messager.showError(err || '保存失败'))
                .finally(() => { this.isLoading = false })
        },
    },
})
</script>
<style scoped>
/* css样式 */
:deep(.datagrid-editable>.f-column) {
    display: flex;
    flex-direction: row;
}

.field {
    display: flex;
    flex-direction: row;
    align-items: center;
    flex: 1;
    min-width: 0;
}

.field .label {
    width: 80px;
    text-align: right;
}

.field .box {
    flex: 1;
}
</style>
