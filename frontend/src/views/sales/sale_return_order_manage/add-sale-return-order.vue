<template>
    <Loading v-show="isLoading" />
    <div class="kingzon-page-content">
        <Form ref="form" :model="returnOrder" :rules="rules" class="kingzon-page-form" labelAlign="right" :labelWidth="80">
            <FormField name="customerId" label="退货客户:" style="width:35%">
                <ComboBox v-model="returnOrder.customerId" :data="customerOptions" textField="text"
                    valueField="value" />
            </FormField>
            <FormField name="returnTime" label="退货日期:" style="width:20%">
                <DateBox v-model="returnOrder.returnTime" format="yyyy-MM-dd"
                    :panelStyle="{ width: '192px', height: '250px !important', maxHeight: '250px !important' }" />
            </FormField>
            <FormField name="remark" label="退货备注:" style="width: 100%;margin-bottom: 5px;">
                <TextBox v-model="returnOrder.remark" :multiline="true" style="height: 40px" />
            </FormField>
        </Form>

        <div class="kingzon-btn-group" style=" margin: 5px">
            <LinkButton btnCls="c8" @click="chooseDeliveryOrder">选择送货</LinkButton>
            <LinkButton @click="clearDetails" :disabled="details.length === 0">清空</LinkButton>
            <KingZonFileButton @onFileSelect="onFileSelect" :multiple="true">文件上传</KingZonFileButton>
            <FileUploadList v-if="files.length" v-model:uploadFileList="files" style="width: 100%;" />
        </div>

        <KingZonDataGrid id="saleReturnDeliveryDraftGrid" ref="detailsGrid" class="kingzon-page-fill" :pagination="false" :data="details"
            selectionMode="null" :columnResizing="true" :columnMoving="true" emptyMsg="请选择送货明细" :dblclickToEdit="true"
            editMode="cell" style="overflow-y: unset;">
            <GridColumnGroup :frozen="true" width="500">
                <GridHeaderRow>
                    <GridColumn align="center" width="40">
                        <template #body="scope">{{ scope.rowIndex + 1 }}</template>
                    </GridColumn>
                    <GridColumn field="deliveryOrderDetail.deliveryOrderNo" title="送货单号" width="120" :overflowTextEllipsis="true" />
                    <GridColumn field="deliveryOrderDetail.productNo" title="产品编码" width="90" :overflowTextEllipsis="true" />
                    <GridColumn field="deliveryOrderDetail.productName" title="产品名称" width="120" :overflowTextEllipsis="true" />
                    <GridColumn field="deliveryOrderDetail.productSpecification" title="规格型号" width="120" :overflowTextEllipsis="true" />
                    <GridColumn field="deliveryOrderDetail.productTechnicalCode" title="技术型号" width="100" :overflowTextEllipsis="true" />
                </GridHeaderRow>
            </GridColumnGroup>
            <GridColumnGroup>
                <GridHeaderRow>
                    <GridColumn field="deliveryOrderDetail.saleOrderNo" title="销售单号" width="120"
                        :overflowTextEllipsis="true">
                    </GridColumn>
                    <GridColumn field="deliveryOrderDetail.productCustomerMaterialNo" title="客户产品图号" width="110"
                        :overflowTextEllipsis="true">
                    </GridColumn>

                    <GridColumn field="deliveryOrderDetail.deliveryQuantity" title="送货数量" align="right" width="110">
                        <template #body="scope">
                            <UnitQuantityShow :quantity="scope.row.deliveryOrderDetail.deliveryQuantity"
                                :unitList="scope.row.deliveryOrderDetail.unitInfos" />
                        </template>
                    </GridColumn>
                    <GridColumn field="deliveryOrderDetail.returnQuantity" title="已退货数量" align="right" width="110">
                        <template #body="scope">
                            <UnitQuantityShow :quantity="scope.row.deliveryOrderDetail.returnQuantity"
                                :unitList="scope.row.deliveryOrderDetail.unitInfos" />
                        </template>
                    </GridColumn>
                    <GridColumn field="availableQuantity" title="可退货数量" align="right" width="110">
                        <template #body="scope">
                            <UnitQuantityShow :quantity="availableQuantity(scope.row)"
                                :unitList="scope.row.deliveryOrderDetail.unitInfos" />
                        </template>
                    </GridColumn>

                    <GridColumn field="inputPrice" title="单价" align="right" width="100" :editable="true" cellCss="kingzon-cell-editable">
                        <template #body="scope">
                            {{ scope.row.inputPrice.toFixed(4) }}
                        </template>
                        <template #edit="scope">
                            <NumberBox v-model="scope.row.inputPrice" :min="0" :precision="4" :spinners="false"
                                @valueChange="onPriceChange(scope.row)" />
                        </template>
                    </GridColumn>
                    <GridColumn field="totalPrice" title="总价" align="right" width="100">
                        <template #body="scope">{{ getTotalPrice(scope.row) }}</template>
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
                    <GridColumn field="deliveryOrderDetail.deliveryTime" title="送货通知时间" width="110">
                        <template #body="scope">
                            {{ $utils.formatDateTime(scope.row.deliveryOrderDetail.deliveryTime, 'yyyy-MM-dd') }}
                        </template>
                    </GridColumn>
                    <GridColumn field="deliveryOrderDetail.outboundTime" title="送货出库时间" width="110">
                        <template #body="scope">{{ $utils.formatDateTime(scope.row.deliveryOrderDetail.outboundTime, 'yyyy-MM-dd') }}</template>
                    </GridColumn>

                </GridHeaderRow>
            </GridColumnGroup>
            <GridColumnGroup :frozen="true" align="right" width="50">
                <GridHeaderRow>
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
                    <GridColumn field="returnQuantity" title="本次退货数量" align="right" width="160" :editable="true"
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
                    <GridColumn field="remove" title="操作" align="center" width="50">
                        <template #body="scope">
                            <LinkButton @click="removeDetail(scope.row.deliveryOrderDetail.id)">删除</LinkButton>
                        </template>
                    </GridColumn>
                </GridHeaderRow>
            </GridColumnGroup>
        </KingZonDataGrid>

        <div style="margin: 8px 20px 4px; text-align: right">合计：{{ totalPrice.toFixed(4) }} 元</div>
        <div class="kingzon-btn-group kingzon-page-footer">
            <LinkButton @click="$emit('cancel')">取消</LinkButton>
            <LinkButton btnCls="c8" @click="submit">保存</LinkButton>
        </div>
    </div>

    <Dialog ref="deliveryDialog" title="选择送货明细" :modal="true" :draggable="true" :resizable="true" closed
        :dialogStyle="{ width: '80%', height: '85%' }">
        <DeliverOrderDetail :choiceCustomerId="returnOrder.customerId!"
            :selectedRows="details.map(item => item.deliveryOrderDetail)" @cancel="closeDeliveryDialog"
            @submit="selectDeliveryOrderDetails" />
    </Dialog>
    <Dialog ref="fileUploadDialog" title="选择文件" :modal="true" :draggable="true" :resizable="true" closed
        :dialogStyle="{ width: '600px', maxHeight: '600px' }">
        <FileUpload :uploadFileList="uploadFileList" :upload="false" @close="fileUploadDialog.close()"
            @submit="uploadFileSubmit" />
    </Dialog>
</template>

<script lang="ts">
import { defineComponent } from 'vue'
import { SaleReturnType } from '@/types/enums'
import { FileBase, FileStorageSourceType } from '@/types/enums'
import type { SelectOption, UploadFile } from '@/types/types'
import DeliverOrderDetail from './deliver-order-detail.vue'
import { DeliveryOrderDetailDTO, PostSaleReturnOrderDetailDTO, PostSaleReturnOrderDTO, SaleReturnOrderDetailDraft } from '@/types/sale.js'
import { UnitConversionHelper } from '@/utils/UnitConversionHelper.js'
import UnitQuantityShow from '../delivery_order_manage/inventory/unit-quantity-show.vue'


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
            fileUploadDialog: {} as EasyUI.Dialog,
            uploadFileList: [] as UploadFile[],
            files: [] as UploadFile[],
            rules: { customerId: 'required', returnTime: 'required' },
            returnOrder: new PostSaleReturnOrderDTO(),
            details: [] as SaleReturnOrderDetailDraft[],
        }
    },
    computed: {
        SaleReturnType() {
            return SaleReturnType
        },
        totalPrice(): number {
            return this.details.reduce((total, detail) => total + Number(detail.inputPrice || 0) * Number(detail.returnQuantity || 0), 0)
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
        this.fileUploadDialog = this.$refs.fileUploadDialog as EasyUI.Dialog
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
                price: row.saleOrderDetailPrice,
                inputPrice: row.saleOrderDetailPrice,
                saleReturnType: SaleReturnType.正常品,
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
            var unit = detail.deliveryOrderDetail.unitInfos.find(it => it.id == detail.returnUnitId)
            detail.inputPrice = (detail.price ?? 0) * (unit?.ratio ?? 1)
        },
        onPriceChange(detail: SaleReturnOrderDetailDraft) {
            const unit = detail.deliveryOrderDetail.unitInfos.find(it => it.id == detail.returnUnitId)
            detail.price = Number(detail.inputPrice || 0) / (unit?.ratio ?? 1)
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
            return row.deliveryOrderDetail.unitInfos.map(it => {
                return {
                    value: it.id,
                    text: it.unitName
                }
            })
        },
        getTotalPrice(row: SaleReturnOrderDetailDraft) {
            return ((row.inputPrice ?? 1) * row.returnQuantity).toFixed(4)
        },
        removeDetail(deliveryOrderDetailId: number) {
            this.$messager.confirm({ title: '提示', msg: '确定删除这条退货明细吗？', result: (ok: boolean) => {
                if (ok) this.details = this.details.filter(detail => detail.deliveryOrderDetail.id !== deliveryOrderDetailId)
            }})
        },
        clearDetails() {
            this.$messager.confirm({ title: '提示', msg: '确定清空全部退货明细吗？', result: (ok: boolean) => {
                if (ok) this.details = []
            }})
        },
        onFileSelect(files: UploadFile[]) {
            this.uploadFileList = files
            this.fileUploadDialog.open()
        },
        uploadFileSubmit(files: UploadFile[]) {
            this.files.push(...files)
            this.fileUploadDialog.close()
        },
        submit() {
            if (!(this.$refs.form as EasyUI.Form).valid) return
            if (!this.returnOrder.customerId) {
                this.$kingzon_messager.showError('请先选择退货客户')
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
                        price: detail.price,
                        unitInfos: detail.deliveryOrderDetail.unitInfos,
                        reason: detail.reason,
                        remark: detail.remark,
                    })
                }),
            })

            this.$http.post('api/sale/SaleReturnOrderManage/PostSaleReturnOrder', postData)
                .then((res: any) => {
                    this.$kingzon_messager.showInfo('保存成功')
                    if (res.data && this.files.length) {
                        this.$utils.uploadFile(res.data, this.files, FileStorageSourceType.销售退货文件, FileBase.SaleReturnOrder)
                    }
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
