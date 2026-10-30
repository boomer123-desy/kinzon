<template>
    <Loading v-show="isLoading" />
    <div class="kingzon-page-content">
        <Form ref="form" :model="returnOrder" :rules="rules" class="kingzon-page-form" labelAlign="right" :labelWidth="80">
            <FormField name="customerId" label="客户:" style="width:35%">
                <ComboBox v-model="returnOrder.customerId" :data="customerOptions" textField="text"
                    valueField="value" />
            </FormField>
            <FormField name="returnTime" label="退货日期:" style="width:20%">
                <DateBox v-model="returnOrder.returnTime" format="yyyy-MM-dd"
                    :panelStyle="{ width: '192px', height: '250px !important', maxHeight: '250px !important' }" />
            </FormField>
            <FormField name="remark" label="备注:" style="flex-basis: 100%;margin-bottom: 5px">
                <TextBox v-model="returnOrder.remark" :multiline="true" style=" height: 40px" />
            </FormField>
        </Form>

        <div class="kingzon-btn-group" style="margin: 5px">
            <LinkButton btnCls="c8" @click="chooseProducts">选择产品</LinkButton>
            <LinkButton @click="clearDetails" :disabled="details.length === 0">清空</LinkButton>
            <KingZonFileButton @onFileSelect="onFileSelect" :multiple="true">文件上传</KingZonFileButton>
            <FileUploadList v-if="files.length" v-model:uploadFileList="files" style="width: 100%;" />
        </div>

        <KingZonDataGrid id="saleReturnProductDraftGrid" ref="detailsGrid" class="kingzon-page-fill" :pagination="false" :data="details"
            selectionMode="null" :columnResizing="true" :columnMoving="true" :dblclickToEdit="true" editMode="cell"
            emptyMsg="请选择客户产品">
            <GridColumnGroup :frozen="true" align="left" width="500">
                <GridHeaderRow>
                    <GridColumn align="center" width="40"><template #body="scope">{{ scope.rowIndex + 1 }}</template>
                    </GridColumn>
                    <GridColumn field="productNo" title="产品编码" width="90" />
                    <GridColumn field="productName" title="产品名称" width="140" />
                    <GridColumn field="specification" title="规格型号" width="140" />
                    <GridColumn field="technicalCode" title="技术型号" width="110" />
                </GridHeaderRow>
            </GridColumnGroup>
            <GridColumnGroup>
                <GridHeaderRow>
                    <GridColumn field="customerMaterialNo" title="客户产品图号" width="110" />
                    <GridColumn field="customerMaterialName" title="客户产品名称" width="140" />
                    <GridColumn field="unit" title="单位" width="60" />
                    <GridColumn field="deliveryCount" title="累计送货" align="right" width="150">
                        <template #body="scope">
                            <UnitQuantityShow :quantity="scope.row.deliveryCount" :unitList="scope.row.unitInfos" />
                        </template>
                    </GridColumn>
                    <GridColumn field="returnCount" title="累计退货" align="right" width="150">
                        <template #body="scope">
                            <UnitQuantityShow :quantity="scope.row.returnCount" :unitList="scope.row.unitInfos" />
                        </template>
                    </GridColumn>
                    <GridColumn field="availableQuantity" title="可退货数量" align="right" width="150">
                        <template #body="scope">
                            <UnitQuantityShow :quantity="availableQuantity(scope.row)" :unitList="scope.row.unitInfos" />
                        </template>
                    </GridColumn>
                    <GridColumn field="deliveryTime" title="送货通知时间" width="120">
                        <template #body="scope">{{ $utils.formatDateTime(scope.row.deliveryTime, 'yyyy-MM-dd') }}</template>
                    </GridColumn>
                    <GridColumn field="outboundTime" title="送货出库时间" width="120">
                        <template #body="scope">{{ $utils.formatDateTime(scope.row.outboundTime, 'yyyy-MM-dd') }}</template>
                    </GridColumn>

                    <GridColumn field="showPrice" title="单价" align="right" width="100" :editable="true" cellCss="kingzon-cell-editable">
                        <template #body="scope">
                            {{ (scope.row.showPrice ?? 0).toFixed(6) }}
                        </template>
                        <template #edit="scope">
                            <NumberBox v-model="scope.row.showPrice" :min="0" :precision="4" :spinners="false"
                                @valueChange="onPriceChange(scope.row)" />
                        </template>
                    </GridColumn>
                    <GridColumn field="totalPrice" title="总价" align="right" width="110">
                        <template #body="scope">
                            {{ ((scope.row.showPrice ?? 0) * scope.row.returnQuantity).toFixed(4) }}
                        </template>
                    </GridColumn>

                    <GridColumn field="reason" title="退货原因" :editable="true" cellCss="kingzon-cell-editable"
                        width="140">
                        <template #edit="scope">
                            <TextBox v-model="scope.row.reason"
                                :disabled="scope.row.isComplete || scope.row.inventoryQuantity > 0" />
                        </template>
                    </GridColumn>
                    <GridColumn field="remark" title="明细备注" :editable="true" cellCss="kingzon-cell-editable"
                        width="140">
                        <template #edit="scope">
                            <TextBox v-model="scope.row.remark"
                                :disabled="scope.row.isComplete || scope.row.inventoryQuantity > 0" />
                        </template>
                    </GridColumn>
                </GridHeaderRow>
            </GridColumnGroup>
            <GridColumnGroup :frozen="true" align="right" width="60">
                <GridHeaderRow>
                    <GridColumn field="saleReturnType" title="退货类型" :editable="true" cellCss="kingzon-cell-editable"
                        width="100">
                        <template #body="scope">
                            {{ SaleReturnType[scope.row.saleReturnType] }}
                        </template>
                        <template #edit="scope">
                            <ComboBox v-model="scope.row.saleReturnType" :data="saleReturnTypeList"
                                :disabled="scope.row.isComplete || scope.row.inventoryQuantity > 0" />
                        </template>
                    </GridColumn>
                    <GridColumn field="returnQuantity" title="本次退货数量" :editable="true" cellCss="kingzon-cell-editable"
                        align="right" width="160">
                        <template #body="scope">
                            {{ scope.row.returnQuantity }}{{getUnitOptions(scope.row).find(it => it.value ==
                                scope.row.returnUnitId)?.text}}
                        </template>
                        <template #edit="scope">
                            <NumberBox style="flex:1.5" v-model="scope.row.returnQuantity" :min="0" :spinners="false"
                                :precision="4" :max="getMax(scope.row)"
                                :disabled="scope.row.isComplete || scope.row.inventoryQuantity > 0">
                            </NumberBox>
                            <ComboBox style="flex:1" v-model="scope.row.returnUnitId" :data="getUnitOptions(scope.row)"
                                @valueChange="onUnitChange(scope.row)"
                                :disabled="scope.row.isComplete || scope.row.inventoryQuantity > 0">
                            </ComboBox>
                        </template>
                    </GridColumn>
                    <GridColumn title="操作" width="60" align="center">
                        <template #body="scope">
                            <LinkButton @click="remove(scope.row.goodsId)">删除</LinkButton>
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

    <Dialog ref="productDialog" title="选择客户产品" :modal="true" :draggable="true" :resizable="true" closed
        :dialogStyle="{ width: '80%', height: '85%' }">
        <ChoiceCustomerProduct :customerId="returnOrder.customerId!" :selectedRows="details"
            @cancel="productDialog.close()" @submit="selectProducts" />
    </Dialog>
    <Dialog ref="fileUploadDialog" title="选择文件" :modal="true" :draggable="true" :resizable="true" closed
        :dialogStyle="{ width: '600px', maxHeight: '600px' }">
        <FileUpload :uploadFileList="uploadFileList" :upload="false" @close="fileUploadDialog.close()"
            @submit="uploadFileSubmit" />
    </Dialog>
</template>

<script lang="ts">
import { defineComponent } from 'vue'
import { FileBase, FileStorageSourceType, SaleReturnType } from '@/types/enums'
import type { SelectOption, UploadFile } from '@/types/types'
import { CustomerDeliveryProductDTO, PostSaleReturnOrderDTO, PostSaleReturnOrderDetailDTO, ProductReturnDetailDraft } from '@/types/sale'
import ChoiceCustomerProduct from './choice-customer-product.vue'
import { UnitConversionHelper } from '@/utils/UnitConversionHelper.js'
import UnitQuantityShow from '../delivery_order_manage/inventory/unit-quantity-show.vue'

export default defineComponent({
    name: 'addSaleReturnProduct',
    emits: ['cancel', 'submit'],
    components: { ChoiceCustomerProduct, UnitQuantityShow },
    data: () => ({
        isLoading: false,
        productDialog: {} as EasyUI.Dialog,
        fileUploadDialog: {} as EasyUI.Dialog,
        uploadFileList: [] as UploadFile[],
        files: [] as UploadFile[],
        rules: { customerId: 'required', returnTime: 'required' },
        customerOptions: [] as SelectOption[],
        saleReturnTypeList: [] as SelectOption[],
        returnOrder: new PostSaleReturnOrderDTO(),
        details: [] as ProductReturnDetailDraft[]
    }),
    computed: {
        SaleReturnType: () => SaleReturnType,
        totalPrice(): number {
            return this.details.reduce(
                (total, item) => total + item.showPrice * Number(item.returnQuantity || 0),
                0
            )
        }
    },
    watch: {
        'returnOrder.customerId'(value: number, oldValue: number) {
            if (oldValue && value !== oldValue) this.details = []
        }
    },
    created() {
        this.saleReturnTypeList = this.$utils.getEnumKeyValueArray(SaleReturnType)
        this.getCustomers()
    },
    mounted() {
        this.productDialog = this.$refs.productDialog as EasyUI.Dialog
        this.fileUploadDialog = this.$refs.fileUploadDialog as EasyUI.Dialog
    },
    methods: {
        getCustomers() {
            this.$http
                .post('api/sale/SaleReturnOrderManage/GetCustomers', [])
                .then((res: any) => {
                    this.customerOptions = (res.data ?? []).map((x: any) => ({
                        value: x.id,
                        text: `${x.customerCode}-${x.customerName}`
                    }))
                })
                .catch((err: unknown) => this.$kingzon_messager.showError(err || '客户数据获取失败'))
        },
        availableQuantity(row: ProductReturnDetailDraft) {
            return row.deliveryCount - row.returnCount
        },
        chooseProducts() {
            if (!this.returnOrder.customerId)
                return this.$kingzon_messager.showError("请先选择客户");
            this.productDialog.open();
        },
        getMax(detail: ProductReturnDetailDraft) {
            const quantity = detail.deliveryCount - detail.returnCount
            return new UnitConversionHelper(-1,
                quantity,
                detail.unitInfos
            ).convertToUnit(detail.returnUnitId ?? -1)?.quantity ?? 0
        },
        onUnitChange(detail: ProductReturnDetailDraft) {
            var max = this.getMax(detail)
            detail.returnQuantity = detail.returnQuantity > max ? max : detail.returnQuantity
            var unit = detail.unitInfos.find(it => it.id == detail.returnUnitId)
            detail.showPrice = detail.price * (unit?.ratio ?? 1)
        },
        onPriceChange(detail: ProductReturnDetailDraft) {
            const unit = detail.unitInfos.find(it => it.id == detail.returnUnitId)
            detail.price = Number(detail.showPrice || 0) / (unit?.ratio ?? 1)
        },
        getUnitOptions(row: ProductReturnDetailDraft): SelectOption[] {
            return row.unitInfos.map(it => {
                return {
                    value: it.id,
                    text: it.unitName
                }
            })
        },
        selectProducts(rows: CustomerDeliveryProductDTO[]) {
            const old = new Map(this.details.map((x) => [x.goodsId, x]));
            this.details = rows.map(
                (x) =>
                    old.get(x.goodsId) ??
                    new ProductReturnDetailDraft({
                        ...x,
                        showPrice: x.price,
                        returnQuantity: 0,
                        returnUnitId: -1,
                        saleReturnType: SaleReturnType.正常品,
                        remark: ''
                    }),
            );
            this.productDialog.close();
        },
        remove(goodsId: number) {
            this.$messager.confirm({ title: '提示', msg: '确定删除这条退货明细吗？', result: (ok: boolean) => {
                if (ok) this.details = this.details.filter((x) => x.goodsId !== goodsId)
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
            if (!this.returnOrder.customerId) return this.$kingzon_messager.showError('请先选择客户')
            if (!this.details.length) return this.$kingzon_messager.showError('请选择产品')
            if (
                this.details.some(
                    (x) =>
                        !x.returnQuantity ||
                        x.returnQuantity <= 0 ||
                        x.returnQuantity > this.getMax(x)
                )
            )
                return this.$kingzon_messager.showError('请填写正确的退货数量')
            const data = new PostSaleReturnOrderDTO({
                customerId: this.returnOrder.customerId,
                returnTime: this.returnOrder.returnTime,
                remark: this.returnOrder.remark,
                details: this.details.map(
                    (x) =>
                        new PostSaleReturnOrderDetailDTO({
                            goodsId: x.goodsId,
                            returnUnitId: x.returnUnitId,
                            returnQuantity: x.returnQuantity,
                            saleReturnType: Number(x.saleReturnType),
                            unitInfos: x.unitInfos,
                            price: x.price,
                            reason: x.reason,
                            remark: x.remark
                        })
                )
            })
            this.isLoading = true
            this.$http
                .post('api/sale/SaleReturnOrderManage/PostSaleReturnOrder', data)
                .then((res: any) => {
                    this.$kingzon_messager.showInfo('保存成功')
                    if (res.data && this.files.length) {
                        this.$utils.uploadFile(res.data, this.files, FileStorageSourceType.销售退货文件, FileBase.SaleReturnOrder)
                    }
                    this.$emit('submit', res.data)
                })
                .catch((err: unknown) => this.$kingzon_messager.showError(err || '保存失败'))
                .finally(() => {
                    this.isLoading = false
                })
        }
    }
})
</script>
<style scoped>
/* css样式 */
:deep(.datagrid-editable>.f-column) {
    display: flex;
    flex-direction: row;
}
</style>
