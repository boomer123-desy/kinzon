<template>
    <Loading v-show="isLoading" />
    <div class="kingzon-page" style="width: 1200px; height: 680px">
        <div class="kingzon-page-content">
            <div class="kingzon-page-panel">
                <SearchPanel :searchParams="searchParams" :searchParamsHidden="searchParamsHidden" @search="search" />
            </div>

            <div class="kingzon-page-fill" style="display: flex">
                <KingZonDataGrid ref="dataGrid" style="width: 100%; flex-grow: 1" :data="deliveryOrderList"
                    selectionMode="null" :columnResizing="true" emptyMsg="暂无可退货的送货明细" @pageChange="getData"
                    @sortChange="getData">
                    <GridColumnGroup :frozen="true" align="left" width="540">
                        <GridHeaderRow>
                            <GridColumn field="selected" align="center" width="45">
                                <template #body="scope">
                                    <CheckBox v-model="scope.row.selected" :disabled="scope.row.actualDeliveryQuantity -
                                        scope.row.returnQuantity == 0"
                                        @checkedChange="onCheckedChange($event, scope.row)" />
                                </template>
                            </GridColumn>
                            <GridColumn align="center" width="40">
                                <template #body="scope">{{ scope.rowIndex + 1 }}</template>
                            </GridColumn>
                            <GridColumn field="deliveryOrderNo" title="送货单号" align="left" width="140" :sortable="true"
                                :overflowTextEllipsis="true" />
                            <GridColumn field="productNo" title="产品编码" align="left" width="100" :sortable="true"
                                :overflowTextEllipsis="true" />
                            <GridColumn field="productCustomerMaterialNo" title="客户产品图号" align="left" width="130"
                                :overflowTextEllipsis="true" />
                            <GridColumn field="productName" title="产品名称" align="left" width="120" :sortable="true"
                                :overflowTextEllipsis="true" />
                        </GridHeaderRow>
                    </GridColumnGroup>
                    <GridColumnGroup :columnMoving="true" align="center">
                        <GridHeaderRow>
                            <GridColumn field="saleOrderNo" title="销售单号" align="left" width="140" :sortable="true"
                                :overflowTextEllipsis="true" />
                            <GridColumn field="productSpecification" title="规格型号" align="left" width="120"
                                :sortable="true" :overflowTextEllipsis="true" />
                            <GridColumn field="productMaterialGrade" title="材质牌号" align="left" width="100"
                                :sortable="true" :overflowTextEllipsis="true" />
                            <GridColumn field="productMaterialSpecification" title="原材料规格" align="left" width="120"
                                :sortable="true" :overflowTextEllipsis="true" />
                            <GridColumn field="productUnit" title="主单位" align="left" width="60"
                                :overflowTextEllipsis="true" />
                            <GridColumn field="deliveryQuantity" title="送货数量" align="right" width="150"
                                :sortable="true">
                                <template #body="scope">
                                    <UnitQuantityShow :quantity="scope.row.deliveryQuantity"
                                        :unitInfos="scope.row.unitInfos" />
                                </template>
                            </GridColumn>
                            <GridColumn field="actualDeliveryQuantity" title="实际送货数量" align="right" width="110"
                                :sortable="true">
                                <template #body="scope">
                                    <UnitQuantityShow :quantity="scope.row.actualDeliveryQuantity"
                                        :unitInfos="scope.row.unitInfos" />
                                </template>
                            </GridColumn>
                            <GridColumn field="returnQuantity" title="已退货数量" align="right" width="90" :sortable="true">
                                <template #body="scope">
                                    <UnitQuantityShow :quantity="scope.row.returnQuantity"
                                        :unitInfos="scope.row.unitInfos" />
                                </template>
                            </GridColumn>
                            <GridColumn field="availableReturnQuantity" title="可退货数量" align="right" width="150">
                                <template #body="scope">
                                    <UnitQuantityShow :quantity="scope.row.actualDeliveryQuantity -
                                        scope.row.returnQuantity" :unitInfos="scope.row.unitInfos" />
                                </template>
                            </GridColumn>
                            <GridColumn field="saleOrderDetailPrice" title="销售单价" align="right" width="100"
                                :sortable="true">
                                <template #body="scope">{{ scope.row.saleOrderDetailPrice.toFixed(4) }}</template>
                            </GridColumn>
                            <GridColumn field="deliveryTime" title="预计送货日期" align="left" width="120" :sortable="true">
                                <template #body="scope">{{ $utils.formatDateTime(scope.row.deliveryTime, 'yyyy-MM-dd')
                                    }}</template>
                            </GridColumn>
                        </GridHeaderRow>
                    </GridColumnGroup>
                </KingZonDataGrid>
            </div>

            <div class="kingzon-btn-group kingzon-page-footer">
                <LinkButton @click="$emit('cancel')">取消</LinkButton>
                <LinkButton btnCls="c8" @click="submit">确定</LinkButton>
            </div>
        </div>
    </div>
</template>

<script lang="ts">
import { defineComponent, type PropType } from 'vue'
import { type FilterTmp, Search } from '@/types/types'
import { DataType, FilterType } from '@/types/enums'
import { DeliveryOrderDetailDTO, SaleReturnOrderFilterListDTO } from '@/types/sale'
import { UnitConversionHelper } from '@/utils/UnitConversionHelper';
import UnitQuantityShow from '@/components/unit_quantity_show/index.vue'
export default defineComponent({
    name: 'deliverOrderDetail',
    props: {
        choiceCustomerId: { type: Number, required: true },
        selectedRows: { type: Array as PropType<DeliveryOrderDetailDTO[]>, default: () => [] },
    },
    components: { UnitQuantityShow },
    emits: ['cancel', 'submit'],
    data() {
        return {
            isLoading: false,
            searchParams: [] as Search[],
            searchParamsHidden: [] as Search[],
            deliveryOrderList: [] as DeliveryOrderDetailDTO[],
            selectedDeliveryRows: [] as DeliveryOrderDetailDTO[],
            filterInfo: new SaleReturnOrderFilterListDTO({ customerId: this.choiceCustomerId }),
        }
    },
    watch: {
        choiceCustomerId(customerId: number) {
            this.filterInfo.customerId = customerId
            this.selectedDeliveryRows = []
            this.getData()
        },
    },
    computed: {
        UnitConversionHelper() {
            return UnitConversionHelper
        },
    },
    created() {
        this.selectedDeliveryRows = [...this.selectedRows]
        this.addSearchParam()
        this.getData()
    },
    methods: {
        addSearchParam() {
            this.searchParams.push(
                new Search('deliveryOrderNo', '送货单号', 'deliveryOrder', FilterType.Contains, DataType.Text),
                new Search('productNo', '产品编码', 'product', FilterType.Contains, DataType.Text),
                new Search('productName', '产品名称', 'product', FilterType.Contains, DataType.Text),
                new Search('saleOrder.orderNo', '销售单号', 'saleOrderDetail', FilterType.Contains, DataType.Text),
            )
            this.searchParamsHidden.push(
                new Search('customerMaterialNo', '客户产品图号', 'saleOrderDetail', FilterType.Contains, DataType.Text),
                new Search('materialGrade', '材质牌号', 'product', FilterType.Contains, DataType.Text),
                new Search('materialSpecification', '原材料规格', 'product', FilterType.Contains, DataType.Text),
                new Search('estimatedDeliveryTime', '预计送货日期', 'deliveryOrder', FilterType.GreaterThanOrEqual, DataType.DateTime, undefined, undefined, '开始时间'),
                new Search('estimatedDeliveryTime', undefined, 'deliveryOrder', FilterType.LessThanOrEqual, DataType.DateTime, undefined, undefined, '结束时间'),
            )
        },
        search(params: FilterTmp[]) {
            this.filterInfo.deliveryOrderFilterList = params.find(item => item.table === 'deliveryOrder')?.filter ?? []
            this.filterInfo.deliveryOrderDetailFilterList = params.find(item => item.table === 'deliveryOrderDetail')?.filter ?? []
            this.filterInfo.saleOrderDetailFilterList = params.find(item => item.table === 'saleOrderDetail')?.filter ?? []
            this.filterInfo.productFilterList = params.find(item => item.table === 'product')?.filter ?? []
            this.getData()
        },
        getData() {
            if (!this.filterInfo.customerId) {
                this.deliveryOrderList = []
                return
            }
            this.isLoading = true
            this.$http.post('api/sale/SaleReturnOrderManage/GetDeliveryOrder', this.filterInfo, this.$refs.dataGrid)
                .then((res: any) => {
                    this.deliveryOrderList = res.data as DeliveryOrderDetailDTO[]
                    this.deliveryOrderList.forEach(row => {
                        row.selected = this.selectedDeliveryRows.some(selected => selected.id === row.id)
                    })
                })
                .catch((err: unknown) => this.$kingzon_messager.showError(err || '网络错误'))
                .finally(() => { this.isLoading = false })
        },
        onCheckedChange(checked: boolean, row: DeliveryOrderDetailDTO) {
            const index = this.selectedDeliveryRows.findIndex(item => item.id === row.id)
            if (checked && index < 0) {
                this.selectedDeliveryRows.push({ ...row })
            }
            if (!checked && index >= 0) {
                this.selectedDeliveryRows.splice(index, 1)
            }
        },
        submit() {
            if (this.selectedDeliveryRows.length === 0) {
                this.$kingzon_messager.showError('请选择送货明细')
                return
            }
            this.$emit('submit', this.selectedDeliveryRows)
        },
    },
})
</script>
