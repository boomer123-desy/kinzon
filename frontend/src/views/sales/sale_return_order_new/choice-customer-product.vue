<template>
    <Loading v-show="isLoading" />
    <div class="kingzon-page" style="width: 1200px; height: 680px">
        <div class="kingzon-page-content">
            <div class="kingzon-page-panel">
                <SearchPanel :searchParams="searchParams" :searchParamsHidden="searchParamsHidden" @search="search" />
            </div>
            <KingZonDataGrid ref="dataGrid" class="kingzon-page-fill" :data="productList" selectionMode="null"
                :columnResizing="true" :columnMoving="true" emptyMsg="暂无可直接退货的客户产品"
                @pageChange="getData" @sortChange="getData">
                <GridColumnGroup :frozen="true" align="left" width="540"><GridHeaderRow>
                    <GridColumn field="selected" width="45" align="center"><template #body="scope"><CheckBox v-model="scope.row.selected" :disabled="availableQuantity(scope.row) <= 0" @checkedChange="onCheckedChange($event, scope.row)" /></template></GridColumn>
                    <GridColumn align="center" width="40"><template #body="scope">{{ scope.rowIndex + 1 }}</template></GridColumn>
                    <GridColumn field="productNo" title="产品编码" width="120" :sortable="true" :overflowTextEllipsis="true" />
                    <GridColumn field="customerMaterialNo" title="客户产品图号" width="140" :sortable="true" :overflowTextEllipsis="true" />
                    <GridColumn field="productName" title="产品名称" width="150" :sortable="true" :overflowTextEllipsis="true" />
                </GridHeaderRow></GridColumnGroup>
                <GridColumnGroup :columnMoving="true" align="center"><GridHeaderRow>
                    <GridColumn field="specification" title="规格型号" width="150" :sortable="true" :overflowTextEllipsis="true" />
                    <GridColumn field="unit" title="主单位" width="80" :overflowTextEllipsis="true" />
                    <GridColumn field="deliveryCount" title="累计送货" width="130" align="right"><template #body="scope"><UnitQuantityShow :quantity="scope.row.deliveryCount" :unitInfos="scope.row.unitInfos" /></template></GridColumn>
                    <GridColumn field="returnCount" title="累计退货" width="130" align="right"><template #body="scope"><UnitQuantityShow :quantity="scope.row.returnCount" :unitInfos="scope.row.unitInfos" /></template></GridColumn>
                    <GridColumn title="可退货数量" width="130" align="right"><template #body="scope"><UnitQuantityShow :quantity="availableQuantity(scope.row)" :unitInfos="scope.row.unitInfos" /></template></GridColumn>
                    <GridColumn field="price" title="销售单价" width="100" align="right"><template #body="scope">{{ (scope.row.price ?? 0).toFixed(4) }}</template></GridColumn>
                </GridHeaderRow></GridColumnGroup>
            </KingZonDataGrid>
            <div class="kingzon-btn-group kingzon-page-footer"><LinkButton @click="$emit('cancel')">取消</LinkButton><LinkButton btnCls="c8" @click="submit">确定</LinkButton></div>
        </div>
    </div>
</template>

<script lang="ts">
import { defineComponent, type PropType } from 'vue'
import { DataType, FilterType } from '@/types/enums'
import { type FilterTmp, Search } from '@/types/types'
import { CustomerDeliveryProductDTO } from '@/types/sale'
import UnitQuantityShow from '@/components/unit_quantity_show/index.vue'

export default defineComponent({
    name: 'choiceCustomerProduct', components: { UnitQuantityShow }, emits: ['cancel', 'submit'],
    props: { customerId: { type: Number, required: true }, selectedRows: { type: Array as PropType<CustomerDeliveryProductDTO[]>, default: () => [] } },
    data() { return { isLoading: false, searchParams: [] as Search[], searchParamsHidden: [] as Search[], productList: [] as CustomerDeliveryProductDTO[], selectedProducts: [] as CustomerDeliveryProductDTO[], filterInfo: { customerId: this.customerId, productFilterList: [], customerMaterialNoFilterList: [] } } },
    watch: { customerId(value: number) { this.filterInfo.customerId = value; this.selectedProducts = []; this.getData() } },
    created() { this.selectedProducts = [...this.selectedRows]; this.addSearchParams(); this.getData() },
    methods: {
        addSearchParams() { this.searchParams.push(new Search('productNo', '产品编码', 'product', FilterType.Contains, DataType.Text), new Search('productName', '产品名称', 'product', FilterType.Contains, DataType.Text), new Search('customerMaterialNo', '客户产品图号', 'customerMaterialNo', FilterType.Contains, DataType.Text)); this.searchParamsHidden.push(new Search('specification', '规格型号', 'product', FilterType.Contains, DataType.Text), new Search('materialGrade', '材质牌号', 'product', FilterType.Contains, DataType.Text), new Search('materialSpecification', '原材料规格', 'product', FilterType.Contains, DataType.Text)) },
        search(params: FilterTmp[]) { this.filterInfo.productFilterList = params.find(x => x.table === 'product')?.filter ?? []; this.filterInfo.customerMaterialNoFilterList = params.find(x => x.table === 'customerMaterialNo')?.filter ?? []; this.getData() },
        getData() { if (!this.filterInfo.customerId) { this.productList = []; return }; this.isLoading = true; this.$http.post('api/sale/SaleReturnOrderManage/GetCustomerDeliveryProducts', this.filterInfo, this.$refs.dataGrid).then((res: any) => { this.productList = (res.data ?? []).map((row: CustomerDeliveryProductDTO) => new CustomerDeliveryProductDTO({ ...row, unitInfos: row.unitInfos ?? [], selected: this.selectedProducts.some(item => item.goodsId === row.goodsId) })) }).catch((err: unknown) => this.$kingzon_messager.showError(err || '客户产品获取失败')).finally(() => { this.isLoading = false }) },
        availableQuantity(row: CustomerDeliveryProductDTO) { return row.deliveryCount - row.returnCount },
        onCheckedChange(checked: boolean, row: CustomerDeliveryProductDTO) { const index = this.selectedProducts.findIndex(item => item.goodsId === row.goodsId); if (checked && index < 0) this.selectedProducts.push({ ...row }); if (!checked && index >= 0) this.selectedProducts.splice(index, 1) },
        submit() { if (!this.selectedProducts.length) return this.$kingzon_messager.showError('请选择客户产品'); this.$emit('submit', this.selectedProducts) },
    },
})
</script>
