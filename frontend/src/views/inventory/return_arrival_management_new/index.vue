<template>
    <div class="kingzon-page">
        <div class="kingzon-page-title"><span>销售退货到货</span></div>
        <div class="kingzon-page-body">
            <!-- 未到货按退货单明细查询。 -->
            <SearchPanel v-show="activeTab === '未到货'" :searchParams="detailSearchParams"
                :searchParamsHidden="detailSearchParamsHidden" @search="searchDetails" />
            <!-- 未入库、已入库均按到货记录查询，使用同一套条件。 -->
            <SearchPanel v-show="activeTab !== '未到货'" :searchParams="arrivalSearchParams"
                :searchParamsHidden="arrivalSearchParamsHidden" @search="searchArrivalRecords" />
            <Tabs :plain="true" style="height: 100%" @tabSelect="tabSelect">
                <TabPanel title="未到货">
                    <div class="kingzon-page-content">
                        <ReturnDetail ref="returnDetail" :filter="detailFilter" />
                    </div>
                </TabPanel>
                <TabPanel title="未入库">
                    <div class="kingzon-page-content">
                        <ArrivalDetail ref="pendingArrivalDetail" :filter="arrivalFilter" :isCompleted="false"
                            :isActive="activeTab === '未入库'" />
                    </div>
                </TabPanel>
                <TabPanel title="已入库">
                    <div class="kingzon-page-content">
                        <ArrivalDetail ref="completedArrivalDetail" :filter="arrivalFilter" :isCompleted="true"
                            :isActive="activeTab === '已入库'" />
                    </div>
                </TabPanel>
            </Tabs>
        </div>
    </div>
</template>

<script lang="ts">
import { defineComponent } from 'vue'
import { DataType, FilterType } from '@/types/enums'
import { Filter, Search, type FilterTmp } from '@/types/types'
import ReturnDetail from './return-detail.vue'
import ArrivalDetail from './arrival-detail.vue'

export class ReturnArrivalDetailFilterDTO {
    isComplete = false
    saleReturnOrderFilterList: Filter[] = []
    saleReturnOrderDetailFilterList: Filter[] = []
    productFilterList: Filter[] = []
    customerFilterList: Filter[] = []
}

export class ReturnArrivalRecordFilterDTO {
    arrivalRecordFilterList: Filter[] = []
    saleReturnOrderFilterList: Filter[] = []
    saleReturnOrderDetailFilterList: Filter[] = []
    productFilterList: Filter[] = []
    customerFilterList: Filter[] = []
}

export default defineComponent({
    name: 'returnArrivalManagement',
    components: { ReturnDetail, ArrivalDetail },
    data() {
        return {
            activeTab: '未到货',
            detailSearchParams: [] as Search[],
            detailSearchParamsHidden: [] as Search[],
            arrivalSearchParams: [] as Search[],
            arrivalSearchParamsHidden: [] as Search[],
            detailFilter: new ReturnArrivalDetailFilterDTO(),
            arrivalFilter: new ReturnArrivalRecordFilterDTO(),
        }
    },
    created() {
        this.addDetailSearchParams()
        this.addArrivalSearchParams()
    },
    methods: {
        tabSelect(tabPanel: EasyUI.TabPanel) {
            this.activeTab = tabPanel.title
            this.$nextTick(() => this.refreshActiveTab())
        },
        refreshActiveTab() {
            const refs = this.$refs as Record<string, any>
            if (this.activeTab === '未到货') refs.returnDetail?.getData()
            if (this.activeTab === '未入库') refs.pendingArrivalDetail?.getData()
            if (this.activeTab === '已入库') refs.completedArrivalDetail?.getData()
        },
        addDetailSearchParams() {
            this.detailSearchParams.push(
                new Search('orderNo', '退货单号', 'saleReturnOrder', FilterType.Contains, DataType.Text),
                new Search('productNo', '产品编码', 'product', FilterType.Contains, DataType.Text),
                new Search('productName', '产品名称', 'product', FilterType.Contains, DataType.Text)
            )
            this.detailSearchParamsHidden.push(
                new Search('customerCode', '客户编码', 'customer', FilterType.Contains, DataType.Text),
                new Search('returnTime', '退货日期', 'saleReturnOrder', FilterType.GreaterThanOrEqual, DataType.DateTime, undefined, undefined, '开始时间'),
                new Search('returnTime', undefined, 'saleReturnOrder', FilterType.LessThanOrEqual, DataType.DateTime, undefined, undefined, '结束时间'),
                new Search('reason', '退货原因', 'saleReturnOrderDetail', FilterType.Contains, DataType.Text),
                new Search('remark', '明细备注', 'saleReturnOrderDetail', FilterType.Contains, DataType.Text)
            )
        },
        addArrivalSearchParams() {
            this.arrivalSearchParams.push(
                new Search('arrivalRecordNo', '到货记录号', 'arrivalRecord', FilterType.Contains, DataType.Text),
                new Search('orderNo', '退货单号', 'saleReturnOrder', FilterType.Contains, DataType.Text),
                new Search('productNo', '产品编码', 'product', FilterType.Contains, DataType.Text),
                new Search('productName', '产品名称', 'product', FilterType.Contains, DataType.Text)
            )
            this.arrivalSearchParamsHidden.push(
                new Search('customerCode', '客户编码', 'customer', FilterType.Contains, DataType.Text),
                new Search('arrivalTime', '到货日期', 'arrivalRecord', FilterType.GreaterThanOrEqual, DataType.DateTime, undefined, undefined, '开始时间'),
                new Search('arrivalTime', undefined, 'arrivalRecord', FilterType.LessThanOrEqual, DataType.DateTime, undefined, undefined, '结束时间'),
                new Search('remark', '到货备注', 'arrivalRecord', FilterType.Contains, DataType.Text),
                new Search('reason', '退货原因', 'saleReturnOrderDetail', FilterType.Contains, DataType.Text)
            )
        },
        searchDetails(params: FilterTmp[]) {
            this.detailFilter.saleReturnOrderFilterList = params.find(item => item.table === 'saleReturnOrder')?.filter ?? []
            this.detailFilter.saleReturnOrderDetailFilterList = params.find(item => item.table === 'saleReturnOrderDetail')?.filter ?? []
            this.detailFilter.productFilterList = params.find(item => item.table === 'product')?.filter ?? []
            this.detailFilter.customerFilterList = params.find(item => item.table === 'customer')?.filter ?? []
            this.$nextTick(() => (this.$refs.returnDetail as any)?.getData())
        },
        searchArrivalRecords(params: FilterTmp[]) {
            this.arrivalFilter.arrivalRecordFilterList = params.find(item => item.table === 'arrivalRecord')?.filter ?? []
            this.arrivalFilter.saleReturnOrderFilterList = params.find(item => item.table === 'saleReturnOrder')?.filter ?? []
            this.arrivalFilter.saleReturnOrderDetailFilterList = params.find(item => item.table === 'saleReturnOrderDetail')?.filter ?? []
            this.arrivalFilter.productFilterList = params.find(item => item.table === 'product')?.filter ?? []
            this.arrivalFilter.customerFilterList = params.find(item => item.table === 'customer')?.filter ?? []
            this.$nextTick(() => this.refreshActiveTab())
        },
    }
})
</script>
