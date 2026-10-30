<template>
    <Loading v-show="isLoading" />
    <div class="kingzon-page-content">
        <div class="kingzon-page-panel">
            <SearchPanel :searchParams="searchParams" :searchParamsHidden="searchParamsHidden" @search="search" />
        </div>
        <div class="kingzon-page-fill" style="display: flex">
            <KingZonDataGrid ref="dataGrid" style="width: 100%; flex-grow: 1" :data="deliveryOrderList"
                id="saleReturnDeliveryDetailGrid" idField="id" :isCheckBoxSelection="true"
                :leftGroupRowWidth="210" :initSelectedRows="selectedDeliveryRows" selectionMode="null" :columnResizing="true"
                emptyMsg="暂无可退货的送货明细" @seletedRowsChanged="selectedRowsChanged" @pageChange="getData"
                @sortChange="getData">
                <template #left>
                    <GridColumn align="center" width="40">
                        <template #body="scope">{{ scope.rowIndex + 1 }}</template>
                    </GridColumn>
                    <GridColumn field="deliveryOrderNo" title="送货单号" align="left" width="120" :sortable="true"
                        :overflowTextEllipsis="true" />
                </template>
                <template #center>
                        <GridColumn field="saleOrderNo" title="销售单号" align="left" width="120" :sortable="true"
                            :overflowTextEllipsis="true" :rowspan="2" />
                        <GridColumn field="historyCustomerMaterialNo" title="客户产品图号" align="left" width="110"
                            :overflowTextEllipsis="true" :rowspan="2" />
                        <GridColumn field="historyCustomerMaterialName" title="客户产品名称" align="left" width="120"
                            :overflowTextEllipsis="true" :rowspan="2" />
                        <GridColumn title="销售单历史产品" align="center" :colspan="4" width="430" />
                        <GridColumn title="产品当前系统资料" align="center" :colspan="4" width="440" />
                        <GridColumn field="deliveryQuantity" title="送货通知量" align="right" width="110" :sortable="true" :rowspan="2">
                            <template #body="scope">
                                <UnitQuantityShow :quantity="scope.row.deliveryQuantity"
                                    :unitList="scope.row.unitInfos" />
                            </template>
                        </GridColumn>
                        <GridColumn field="actualDeliveryQuantity" title="送货出库量" align="right" width="110"
                            :sortable="true" :rowspan="2">
                            <template #body="scope">
                                <UnitQuantityShow :quantity="scope.row.actualDeliveryQuantity"
                                    :unitList="scope.row.unitInfos" />
                            </template>
                        </GridColumn>
                        <GridColumn field="returnQuantity" title="已退货数量" align="right" width="110" :sortable="true" :rowspan="2">
                            <template #body="scope">
                                <UnitQuantityShow :quantity="scope.row.returnQuantity"
                                    :unitList="scope.row.unitInfos" />
                            </template>
                        </GridColumn>
                        <GridColumn field="availableReturnQuantity" title="可退货数量" align="right" width="110" :rowspan="2">
                            <template #body="scope">
                                <UnitQuantityShow :quantity="scope.row.actualDeliveryQuantity - scope.row.returnQuantity" :unitList="scope.row.unitInfos" />
                            </template>
                        </GridColumn>
                        <GridColumn field="saleOrderDetailPrice" title="销售单价" align="right" width="100" :sortable="true" :rowspan="2">
                            <template #body="scope">{{ scope.row.saleOrderDetailPrice.toFixed(4) }}</template>
                        </GridColumn>
                        <GridColumn field="deliveryTime" title="送货通知时间" align="left" width="120" :sortable="true" :rowspan="2">
                            <template #body="scope">{{ $utils.formatDateTime(scope.row.deliveryTime, 'yyyy-MM-dd') }}</template>
                        </GridColumn>
                        <GridColumn field="outboundTime" title="送货出库时间" align="left" width="120" :sortable="true" :rowspan="2">
                            <template #body="scope">{{ $utils.formatDateTime(scope.row.outboundTime, 'yyyy-MM-dd') }}</template>
                        </GridColumn>
                </template>
                <template #multiCenterHeaderRow>
                    <GridHeaderRow>
                        <GridColumn field="historyProductNo" title="产品编码" align="left" width="100" :overflowTextEllipsis="true" />
                        <GridColumn field="historyProductName" title="产品名称" align="left" width="120" :overflowTextEllipsis="true" />
                        <GridColumn field="historyProductSpecification" title="规格型号" align="left" width="110" :overflowTextEllipsis="true" />
                        <GridColumn field="historyProductTechnicalCode" title="技术型号" align="left" width="100" :overflowTextEllipsis="true" />
                        <GridColumn field="productNo" title="产品编码" align="left" width="100" :sortable="true" :overflowTextEllipsis="true" />
                        <GridColumn field="productName" title="产品名称" align="left" width="120" :sortable="true" :overflowTextEllipsis="true" />
                        <GridColumn field="productSpecification" title="规格型号" align="left" width="120" :sortable="true" :overflowTextEllipsis="true" />
                        <GridColumn field="productTechnicalCode" title="技术型号" align="left" width="100" :sortable="true" :overflowTextEllipsis="true" />
                    </GridHeaderRow>
                </template>
            </KingZonDataGrid>
        </div>

        <div class="kingzon-btn-group kingzon-page-footer">
            <LinkButton @click="$emit('cancel')">取消</LinkButton>
            <LinkButton btnCls="c8" @click="submit">确定</LinkButton>
        </div>
    </div>
</template>

<script lang="ts">
import { defineComponent, type PropType } from 'vue'
import { type FilterTmp, Search } from '@/types/types'
import { DataType, FilterType } from '@/types/enums'
import { DeliveryOrderDetailDTO, SaleReturnOrderFilterListDTO } from '@/types/sale'
import { UnitConversionHelper } from '@/utils/UnitConversionHelper';
import UnitQuantityShow from '../delivery_order_manage/inventory/unit-quantity-show.vue';


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
    },
    mounted() {
        this.getData()
    },
    methods: {
        addSearchParam() {
            this.searchParams.push(
                new Search('deliveryOrderNo', '送货单号', 'deliveryOrder', FilterType.Contains, DataType.Text),
                new Search('productNo', '产品编码', 'product', FilterType.Contains, DataType.Text),
                new Search('productName', '产品名称', 'product', FilterType.Contains, DataType.Text),
                new Search('saleOrder.orderNo', '销售单号', 'saleOrderDetail', FilterType.Contains, DataType.Text),
                new Search('technicalCode', '技术型号', 'product', FilterType.Contains, DataType.Text),
            )
            this.searchParamsHidden.push(
                new Search('customerMaterialNo', '客户产品图号', 'saleOrderDetail', FilterType.Contains, DataType.Text),
                new Search('materialSpecification', '规格型号', 'product', FilterType.Contains, DataType.Text),
                new Search('outboundTime', '送货出库时间', 'deliveryOrderDetail', FilterType.GreaterThanOrEqual, DataType.DateTime, undefined, undefined, '开始时间'),
                new Search('outboundTime', undefined, 'deliveryOrderDetail', FilterType.LessThanOrEqual, DataType.DateTime, undefined, undefined, '结束时间'),
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
                        row.checkBoxDisabled = row.actualDeliveryQuantity - row.returnQuantity <= 0
                    })
                })
                .catch((err: unknown) => this.$kingzon_messager.showError(err || '网络错误'))
                .finally(() => { this.isLoading = false })
        },
        selectedRowsChanged(rows: DeliveryOrderDetailDTO[]) {
            this.selectedDeliveryRows = rows.map(row => new DeliveryOrderDetailDTO(row))
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
