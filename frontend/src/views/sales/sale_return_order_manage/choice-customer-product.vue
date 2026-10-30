<template>
    <Loading v-show="isLoading" />
    <div class="kingzon-page-content">
        <div class="kingzon-page-panel">
            <SearchPanel :searchParams="searchParams" :searchParamsHidden="searchParamsHidden" @search="search" />
        </div>
        <KingZonDataGrid ref="dataGrid" class="kingzon-page-fill" :data="productList" id="saleReturnCustomerProductGrid"
            idField="goodsId" :isCheckBoxSelection="true" :initSelectedRows="selectedProducts" selectionMode="null"
            :leftGroupRowWidth="620" :columnResizing="true" :columnMoving="true" emptyMsg="暂无可直接退货的客户产品"
            @seletedRowsChanged="selectedRowsChanged" @pageChange="getData" @sortChange="getData">
            <template #left>
                    <GridColumn align="center" width="40"><template #body="scope">{{ scope.rowIndex + 1
                            }}</template></GridColumn>
                    <GridColumn field="productNo" title="产品编码" width="100" :sortable="true"
                        :overflowTextEllipsis="true" />
                    <GridColumn field="customerMaterialNo" title="客户产品图号" width="140" :sortable="true"
                        :overflowTextEllipsis="true" />
                    <GridColumn field="customerMaterialName" title="客户产品名称" width="140" :sortable="true"
                        :overflowTextEllipsis="true" />
                    <GridColumn field="productName" title="产品名称" width="150" :sortable="true"
                        :overflowTextEllipsis="true" />
            </template>
            <template #center>
                    <GridColumn field="specification" title="规格型号" width="150" :sortable="true"
                        :overflowTextEllipsis="true" />
                    <GridColumn field="technicalCode" title="技术型号" width="100" :sortable="true"
                        :overflowTextEllipsis="true" />
                    <GridColumn field="unit" title="主单位" width="80" :overflowTextEllipsis="true" />
                    <GridColumn field="deliveryCount" title="累计送货" width="130" align="right">
                        <template #body="scope">
                            <UnitQuantityShow :quantity="scope.row.deliveryCount" :unitList="scope.row.unitInfos" />
                        </template>
                    </GridColumn>
                    <GridColumn field="returnCount" title="累计退货" width="130" align="right">
                        <template #body="scope">
                            <UnitQuantityShow :quantity="scope.row.returnCount" :unitList="scope.row.unitInfos" />
                        </template>
                    </GridColumn>
                    <GridColumn title="可退货数量" width="130" align="right">
                        <template #body="scope">
                            <UnitQuantityShow :quantity="availableQuantity(scope.row)"
                                :unitList="scope.row.unitInfos" />
                        </template>
                    </GridColumn>
                    <GridColumn field="deliveryTime" title="送货通知时间" width="120">
                        <template #body="scope">{{ $utils.formatDateTime(scope.row.deliveryTime, 'yyyy-MM-dd') }}</template>
                    </GridColumn>
                    <GridColumn field="outboundTime" title="送货出库时间" width="120">
                        <template #body="scope">{{ $utils.formatDateTime(scope.row.outboundTime, 'yyyy-MM-dd') }}</template>
                    </GridColumn>
                    <GridColumn field="price" title="单价" width="100" align="right">
                        <template #body="scope">
                            {{ (scope.row.price ?? 0).toFixed(4) }}
                        </template>
                    </GridColumn>
            </template>
        </KingZonDataGrid>
        <div class="kingzon-btn-group kingzon-page-footer">
            <LinkButton @click="$emit('cancel')">取消</LinkButton>
            <LinkButton btnCls="c8" @click="submit">确定</LinkButton>
        </div>
    </div>
</template>

<script lang="ts">
import { defineComponent, type PropType } from 'vue'
import { DataType, FilterType } from '@/types/enums'
import { Filter, type FilterTmp, Search } from '@/types/types'
import { CustomerDeliveryProductDTO, InventoryUnitDto, SaleGoodsUnitDto } from '@/types/sale'
import UnitQuantityShow from '../delivery_order_manage/inventory/unit-quantity-show.vue'

export default defineComponent({
    name: 'choiceCustomerProduct',
    components: { UnitQuantityShow },
    emits: ['cancel', 'submit'],
    props: {
        customerId: { type: Number, required: true },
        selectedRows: { type: Array as PropType<CustomerDeliveryProductDTO[]>, default: () => [] }
    },
    data() {
        return {
            isLoading: false,
            searchParams: [] as Search[],
            searchParamsHidden: [] as Search[],
            productList: [] as CustomerDeliveryProductDTO[],
            selectedProducts: [] as CustomerDeliveryProductDTO[],
            filterInfo: {
                customerId: this.customerId,
                productFilterList: [] as Filter[],
                customerMaterialNoFilterList: [] as Filter[]
            }
        }
    },
    watch: {
        customerId(value: number) {
            this.filterInfo.customerId = value
            this.selectedProducts = []
            this.getData()
        }
    },
    created() {
        this.selectedProducts = [...this.selectedRows]
        this.addSearchParams()
    },
    mounted() {
        this.getData()
    },
    methods: {
        addSearchParams() {
            this.searchParams.push(
                new Search('productNo', '产品编码', 'product', FilterType.Contains, DataType.Text),
                new Search('productName', '产品名称', 'product', FilterType.Contains, DataType.Text),
                new Search(
                    'customerMaterialNo',
                    '客户产品图号',
                    'customerMaterialNo',
                    FilterType.Contains,
                    DataType.Text
                )
            )
            this.searchParamsHidden.push(
                new Search('specification', '规格型号', 'product', FilterType.Contains, DataType.Text),
                new Search('technicalCode', '技术型号', 'product', FilterType.Contains, DataType.Text),
            )
        },
        search(params: FilterTmp[]) {
            this.filterInfo.productFilterList = params.find((x) => x.table === 'product')?.filter ?? []
            this.filterInfo.customerMaterialNoFilterList =
                params.find((x) => x.table === 'customerMaterialNo')?.filter ?? []
            this.getData()
        },
        getData() {
            if (!this.filterInfo.customerId) {
                this.productList = []
                return
            }
            this.isLoading = true
            this.$http
                .post(
                    'api/sale/SaleReturnOrderManage/GetCustomerDeliveryProducts',
                    this.filterInfo,
                    this.$refs.dataGrid
                )
                .then((res: any) => {
                    this.productList = (res.data ?? []).map(
                        (row: CustomerDeliveryProductDTO) =>
                            new CustomerDeliveryProductDTO({
                                ...row,
                                unitInfos: row.unitInfos ?? [],
                                checkBoxDisabled: this.availableQuantity(row) <= 0
                            })
                    )
                    return this.getProductUnitList()
                })
                .then((res: any) => {
                    var unitMap = new Map(res.data.map(it => {
                        return [Number(it.idField), it.unitList]
                    }))
                    this.productList = this.productList.map(it => {
                        var units = (unitMap.get(it.goodsId) ?? []) as SaleGoodsUnitDto[]
                        it.unitInfos = units.map(row => {
                            return new InventoryUnitDto({
                                id: Number(row.code),
                                unitId: row.unitId,
                                unitName: row.text,
                                ratio: row.value
                            })
                        })
                        var saleUnit = it.unitInfos.find(row => row.id == it.salesUnitConversionId)
                        it.price = it.salePrice / (saleUnit?.ratio ?? 1)
                        return it
                    })
                })
                .catch((err: unknown) => this.$kingzon_messager.showError(err || '客户产品获取失败'))
                .finally(() => {
                    this.isLoading = false
                })
        },
        getProductUnitList() {
            var productIdList = this.productList.map(it => {
                return {
                    idField: it.goodsId + '',
                    productId: it.productId,
                    specificationId: it.specificationId
                }
            })
            return this.$http.post('/api/sale/SaleOrderManage/GetProductUnitList', productIdList)
        },
        availableQuantity(row: CustomerDeliveryProductDTO) {
            return row.deliveryCount - row.returnCount
        },
        selectedRowsChanged(rows: CustomerDeliveryProductDTO[]) {
            this.selectedProducts = rows.map(row => new CustomerDeliveryProductDTO(row))
        },
        submit() {
            if (!this.selectedProducts.length) return this.$kingzon_messager.showError('请选择客户产品')
            this.$emit('submit', this.selectedProducts)
        }
    }
})
</script>
