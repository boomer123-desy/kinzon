<template>
    <Loading v-show="isLoading" />
    <div class="kingzon-page-content" style="min-width: 800px; min-height: 500px">
        <Form :model="returnOrder" class="kingzon-page-form" labelAlign="right" :labelWidth="70">
            <FormField name="customerId" label="客户:"><ComboBox v-model="returnOrder.customerId" :data="customerOptions" textField="text" valueField="value" style="width: 220px" /></FormField>
            <FormField name="returnTime" label="退货日期:"><DateBox v-model="returnOrder.returnTime" format="yyyy-MM-dd" style="width: 180px" /></FormField>
            <FormField name="remark" label="备注:" style="flex-basis: 100%"><TextBox v-model="returnOrder.remark" :multiline="true" style="width: 450px; height: 40px" /></FormField>
        </Form>

        <div class="kingzon-btn-group" style="margin: 5px"><LinkButton btnCls="c8" @click="chooseProducts">选择产品</LinkButton><LinkButton @click="details = []" :disabled="details.length === 0">清空</LinkButton></div>
        <KingZonDataGrid ref="detailsGrid" class="kingzon-page-fill" :pagination="false" :data="details" selectionMode="null" :columnResizing="true" :columnMoving="true" :dblclickToEdit="true" editMode="cell" emptyMsg="请选择客户产品">
            <GridColumnGroup :frozen="true" width="560"><GridHeaderRow>
                <GridColumn align="center" width="40"><template #body="scope">{{ scope.rowIndex + 1 }}</template></GridColumn>
                <GridColumn field="productNo" title="产品编码" width="110" /><GridColumn field="customerMaterialNo" title="客户产品图号" width="140" /><GridColumn field="productName" title="产品名称" width="140" /><GridColumn field="specification" title="规格型号" width="130" />
            </GridHeaderRow></GridColumnGroup>
            <GridColumnGroup :columnMoving="true" align="center"><GridHeaderRow>
                <GridColumn field="deliveryCount" title="累计送货" align="right" width="120"><template #body="scope"><UnitQuantityShow :quantity="scope.row.deliveryCount" :unitInfos="scope.row.unitInfos" /></template></GridColumn>
                <GridColumn field="returnCount" title="累计退货" align="right" width="120"><template #body="scope"><UnitQuantityShow :quantity="scope.row.returnCount" :unitInfos="scope.row.unitInfos" /></template></GridColumn>
                <GridColumn title="可退货数量" align="right" width="120"><template #body="scope"><UnitQuantityShow :quantity="availableQuantity(scope.row)" :unitInfos="scope.row.unitInfos" /></template></GridColumn>
                <GridColumn field="returnQuantity" title="本次退货数量" :editable="true" cellCss="kingzon-cell-editable" align="right" width="180"><template #body="scope">{{ scope.row.returnQuantity }}{{ getUnitOptions(scope.row).find(x => x.value === scope.row.returnUnitId)?.text }}</template><template #edit="scope"><NumberBox style="flex: 1.5" v-model="scope.row.returnQuantity" :min="0" :max="getMax(scope.row)" :precision="4" :spinners="false" /><ComboBox style="flex: 1" v-model="scope.row.returnUnitId" :data="getUnitOptions(scope.row)" @valueChange="onUnitChange(scope.row)" /></template></GridColumn>
                <GridColumn field="price" title="单价" align="right" width="100"><template #body="scope">{{ (scope.row.price ?? 0).toFixed(4) }}</template></GridColumn>
                <GridColumn title="总价" align="right" width="110"><template #body="scope">{{ getTotalPrice(scope.row) }}</template></GridColumn>
                <GridColumn field="saleReturnType" title="退货类型" :editable="true" cellCss="kingzon-cell-editable" width="100"><template #body="scope">{{ SaleReturnType[scope.row.saleReturnType] }}</template><template #edit="scope"><ComboBox v-model="scope.row.saleReturnType" :data="saleReturnTypeList" /></template></GridColumn>
                <GridColumn field="reason" title="退货原因" :editable="true" cellCss="kingzon-cell-editable" width="140"><template #edit="scope"><TextBox v-model="scope.row.reason" /></template></GridColumn>
                <GridColumn field="remark" title="明细备注" :editable="true" cellCss="kingzon-cell-editable" width="140"><template #edit="scope"><TextBox v-model="scope.row.remark" /></template></GridColumn>
            </GridHeaderRow></GridColumnGroup>
            <GridColumnGroup :frozen="true" align="right" width="60"><GridHeaderRow><GridColumn title="操作" width="60"><template #body="scope"><LinkButton @click="remove(scope.row.goodsId)">删除</LinkButton></template></GridColumn></GridHeaderRow></GridColumnGroup>
        </KingZonDataGrid>
        <div style="margin: 8px 20px; text-align: right">合计：{{ totalPrice.toFixed(4) }} 元</div>
        <div class="kingzon-btn-group kingzon-page-footer"><LinkButton @click="$emit('cancel')">取消</LinkButton><LinkButton btnCls="c8" @click="submit">保存</LinkButton></div>
    </div>

    <Dialog ref="productDialog" title="选择客户产品" :modal="true" :draggable="true" :resizable="true" closed>
        <ChoiceCustomerProduct :customerId="returnOrder.customerId" :selectedRows="details" @cancel="productDialog.close()" @submit="selectProducts" />
    </Dialog>
</template>

<script lang="ts">
import { defineComponent } from 'vue'
import { SaleReturnType } from '@/types/enums'
import type { SelectOption } from '@/types/types'
import { CustomerDeliveryProductDTO, PostSaleReturnOrderDTO, PostSaleReturnOrderDetailDTO, ProductReturnDetailDraft } from '@/types/sale'
import { UnitConversionHelper } from '@/utils/UnitConversionHelper'
import UnitQuantityShow from '@/components/unit_quantity_show/index.vue'
import ChoiceCustomerProduct from './choice-customer-product.vue'

export default defineComponent({
    name: 'addSaleReturnProduct', components: { ChoiceCustomerProduct, UnitQuantityShow }, emits: ['cancel', 'submit'],
    data: () => ({ isLoading: false, productDialog: {} as EasyUI.Dialog, customerOptions: [] as SelectOption[], saleReturnTypeList: [] as SelectOption[], returnOrder: new PostSaleReturnOrderDTO(), details: [] as ProductReturnDetailDraft[] }),
    computed: { SaleReturnType: () => SaleReturnType, totalPrice(): number { return this.details.reduce((total, item) => total + Number(this.getBaseQuantity(item)) * (item.price ?? 0), 0) } },
    watch: { 'returnOrder.customerId'(value: number, oldValue: number) { if (oldValue && value !== oldValue) this.details = [] } },
    created() { this.saleReturnTypeList = this.$utils.getEnumKeyValueArray(SaleReturnType); this.getCustomers() },
    mounted() { this.productDialog = this.$refs.productDialog as EasyUI.Dialog },
    methods: {
        getCustomers() { this.$http.post('api/sale/SaleReturnOrderManage/GetCustomers', []).then((res: any) => { this.customerOptions = (res.data ?? []).map((x: any) => ({ value: x.id, text: `${x.customerCode}-${x.customerName}` })) }).catch((err: unknown) => this.$kingzon_messager.showError(err || '客户数据获取失败')) },
        availableQuantity(row: ProductReturnDetailDraft) { return row.deliveryCount - row.returnCount },
        getBaseQuantity(row: ProductReturnDetailDraft) { return new UnitConversionHelper(row.returnUnitId ?? -1, Number(row.returnQuantity || 0), row.unitInfos).convertToUnit(-1)?.quantity ?? 0 },
        getMax(row: ProductReturnDetailDraft) { return new UnitConversionHelper(-1, this.availableQuantity(row), row.unitInfos).convertToUnit(row.returnUnitId ?? -1)?.quantity ?? 0 },
        getUnitOptions(row: ProductReturnDetailDraft): SelectOption[] { return [{ value: -1, text: row.unit }, ...row.unitInfos.map(x => ({ value: x.id, text: x.unitName }))] },
        onUnitChange(row: ProductReturnDetailDraft) { row.returnQuantity = Math.min(Number(row.returnQuantity || 0), this.getMax(row)) },
        getTotalPrice(row: ProductReturnDetailDraft) { return ((row.price ?? 0) * this.getBaseQuantity(row)).toFixed(4) },
        chooseProducts() { if (!this.returnOrder.customerId) return this.$kingzon_messager.showError('请先选择客户'); this.productDialog.open() },
        selectProducts(rows: CustomerDeliveryProductDTO[]) { const old = new Map(this.details.map(x => [x.goodsId, x])); this.details = rows.map(x => old.get(x.goodsId) ?? new ProductReturnDetailDraft({ ...x, returnQuantity: 0, returnUnitId: -1, saleReturnType: SaleReturnType.成品 })); this.productDialog.close() },
        remove(goodsId: number) { this.details = this.details.filter(x => x.goodsId !== goodsId) },
        submit() { if (!this.returnOrder.customerId) return this.$kingzon_messager.showError('请先选择客户'); if (!this.details.length) return this.$kingzon_messager.showError('请选择产品'); if (this.details.some(x => !x.returnQuantity || x.returnQuantity <= 0 || x.returnQuantity > this.getMax(x))) return this.$kingzon_messager.showError('请填写正确的退货数量'); const data = new PostSaleReturnOrderDTO({ customerId: this.returnOrder.customerId, returnTime: this.returnOrder.returnTime, remark: this.returnOrder.remark, details: this.details.map(x => new PostSaleReturnOrderDetailDTO({ goodsId: x.goodsId, returnQuantity: x.returnQuantity, returnUnitId: x.returnUnitId, unitInfos: x.unitInfos, saleReturnType: Number(x.saleReturnType), reason: x.reason, remark: x.remark })) }); this.isLoading = true; this.$http.post('api/sale/SaleReturnOrderManage/PostSaleReturnOrder', data).then((res: any) => { this.$kingzon_messager.showInfo('保存成功'); this.$emit('submit', res.data) }).catch((err: unknown) => this.$kingzon_messager.showError(err || '保存失败')).finally(() => { this.isLoading = false }) },
    },
})
</script>

<style scoped>
:deep(.datagrid-editable > .f-column) { display: flex; flex-direction: row; }
</style>
