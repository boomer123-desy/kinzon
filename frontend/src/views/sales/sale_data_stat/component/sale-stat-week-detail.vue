<template>
    <Loading v-show="isLoading"></Loading>
    <div class="kingzon-page-content" style="height: 100%; min-height: 0;">
        <div class="kingzon-btn-group" style="margin: 5px;">
            <LinkButton @click="getData" :disabled="isLoading">刷新</LinkButton>
        </div>
        <KingZonDataGrid ref="dataGrid" :cellSpan="cellSpan" :footerData="footerData" :showFooter="true" class="kingzon-page-fill" :data="result.rows" :id="'monthSaleWeekDetail-' + query.detailType"
            idField="id" :pagination="false" :lazy="false" :virtualScroll="true" :columnMoving="false"
            :showFieldFilter="false" :showDownLoad="true" :exportFileName="query.title" selectionMode="null" emptyMsg="该周暂无相关明细">
            <GridColumnGroup :frozen="true" align="left" :width="520">
                <GridHeaderRow>
                    <GridColumn field="index" title="编号" align="center" :width="180" cellCss="datagrid-td-rownumber">
                        <template #body="scope">{{ scope.rowIndex + 1 }}</template>
                        <template #footer="scope">{{ scope.row.index }}</template>
                    </GridColumn>
                    <GridColumn field="orderNo" :title="isSale ? '销售单号' : '送货单号'" halign="center" :width="190" :overflowTextEllipsis="true"></GridColumn>
                    <GridColumn field="customerName" title="客户" halign="center" :width="150" :overflowTextEllipsis="true"></GridColumn>
                </GridHeaderRow>
            </GridColumnGroup>
            <GridColumnGroup align="center">
                <GridHeaderRow>
                    <GridColumn field="dateTime" :title="isSale ? '订单创建时间' : '送货时间'" halign="center" :width="140">
                        <template #body="scope">{{ $utils.formatDateTime(scope.row.dateTime, 'yyyy-MM-dd HH:mm') }}</template>
                    </GridColumn>
                    <GridColumn v-if="!isSale" field="saleOrderNo" title="销售单号" halign="center" :width="180" :overflowTextEllipsis="true"></GridColumn>
                    <GridColumn v-if="!isSale" field="detailNo" title="送货通知编号" halign="center" :width="170" :overflowTextEllipsis="true"></GridColumn>
                    <GridColumn field="customerOrderNo" title="客户订单号" halign="center" :width="150" :overflowTextEllipsis="true"></GridColumn>
                    <GridColumn field="productNo" title="产品编码" halign="center" :width="120" :overflowTextEllipsis="true"></GridColumn>
                    <GridColumn field="productName" title="产品名称" halign="center" :width="160" :overflowTextEllipsis="true"></GridColumn>
                    <GridColumn field="customerMaterialNo" title="客户产品图号" halign="center" :width="140" :overflowTextEllipsis="true"></GridColumn>
                    <GridColumn field="specification" title="规格" halign="center" :width="130" :overflowTextEllipsis="true"></GridColumn>
                    <GridColumn field="version" title="版本" halign="center" :width="70"></GridColumn>
                    <GridColumn field="unit" title="单位" halign="center" :width="65"></GridColumn>
                    <GridColumn field="quantity" :title="isSale ? '下单数量' : '送货数量'" align="right" halign="center" :width="100"></GridColumn>
                    <GridColumn field="price" title="不含税单价" align="right" halign="center" :width="100">
                        <template #body="scope">{{ amountText(scope.row.price, 4) }}</template>
                    </GridColumn>
                    <GridColumn field="priceHaveTax" title="含税单价" align="right" halign="center" :width="100">
                        <template #body="scope">{{ amountText(scope.row.priceHaveTax, 4) }}</template>
                    </GridColumn>
                    <GridColumn field="totalPrice" :title="isSale ? '明细金额（不含税）' : '送货金额（不含税）'" align="right" halign="center" :width="120">
                        <template #body="scope">{{ amountText(scope.row.totalPrice) }}</template>
                        <template #footer="scope">{{ amountText(scope.row.totalPrice) }}</template>
                    </GridColumn>
                    <GridColumn field="totalPriceHaveTax" :title="isSale ? '明细金额（含税）' : '送货金额（含税）'" align="right" halign="center" :width="140">
                        <template #body="scope">{{ amountText(scope.row.totalPriceHaveTax) }}</template>
                        <template #footer="scope">{{ amountText(scope.row.totalPriceHaveTax) }}</template>
                    </GridColumn>
                    <GridColumn v-if="isSale" field="orderTotalPrice" title="订单总额（不含税）" align="right" halign="center" :width="140">
                        <template #body="scope">{{ amountText(scope.row.orderTotalPrice) }}</template>
                        <template #footer="scope">{{ amountText(scope.row.orderTotalPrice) }}</template>
                    </GridColumn>
                    <GridColumn v-if="isSale" field="orderTotalPriceHaveTax" title="订单总额（含税）" align="right" halign="center" :width="150">
                        <template #body="scope">{{ amountText(scope.row.orderTotalPriceHaveTax) }}</template>
                        <template #footer="scope">{{ amountText(scope.row.orderTotalPriceHaveTax) }}</template>
                    </GridColumn>
                    <GridColumn v-if="isSale" field="moldCost" title="模具费" align="right" halign="center" :width="100">
                        <template #body="scope">{{ amountText(scope.row.moldCost) }}</template>
                    </GridColumn>
                    <GridColumn v-if="!isSale" field="returnQuantity" title="退货数量" align="right" halign="center" :width="100"></GridColumn>
                    <GridColumn v-if="!isSale" field="returnAmount" title="退货金额（不含税）" align="right" halign="center" :width="110">
                        <template #body="scope"><span style="color: var(--color-red);">{{ amountText(scope.row.returnAmount) }}</span></template>
                        <template #footer="scope"><span style="color: var(--color-red);">{{ amountText(scope.row.returnAmount) }}</span></template>
                    </GridColumn>
                    <GridColumn v-if="!isSale" field="returnAmountHaveTax" title="退货金额（含税）" align="right" halign="center" :width="140">
                        <template #body="scope"><span style="color: var(--color-red);">{{ amountText(scope.row.returnAmountHaveTax) }}</span></template>
                        <template #footer="scope"><span style="color: var(--color-red);">{{ amountText(scope.row.returnAmountHaveTax) }}</span></template>
                    </GridColumn>
                    <GridColumn field="remark" title="备注" halign="center" :width="200" :overflowTextEllipsis="true"></GridColumn>
                </GridHeaderRow>
            </GridColumnGroup>
        </KingZonDataGrid>
        <div class="kingzon-page-footer kingzon-btn-group"><LinkButton @click="$emit('close')">关闭</LinkButton></div>
    </div>
</template>

<script lang="ts">
import { defineComponent, type PropType } from 'vue'
import { MonthSaleWeekDetailQueryDto, MonthSaleWeekDetailResultDto, SaleStatDetailType } from '@/types/sales'

export default defineComponent({
    name: 'saleStatWeekDetail',
    emits: ['close'],
    props: {
        query: { type: Object as PropType<MonthSaleWeekDetailQueryDto>, required: true }
    },
    data() {
        return { isLoading: false, result: new MonthSaleWeekDetailResultDto() }
    },
    computed: {
        isSale(): boolean { return this.query.detailType === SaleStatDetailType.销售 },
        footerData() {
            const sum = (field: 'quantity' | 'totalPrice' | 'totalPriceHaveTax' | 'moldCost' | 'returnQuantity' | 'returnAmount' | 'returnAmountHaveTax') =>
                this.result.rows.reduce((total, row) => total + (row[field] ?? 0), 0)
            const orders = [...new Map(this.result.rows.map(row => [row.orderId, row])).values()]
            return [{
                index: `${this.result.orderCount}张单据 / ${this.result.detailCount}条明细`,
                quantity: sum('quantity'),
                totalPrice: sum('totalPrice'),
                totalPriceHaveTax: sum('totalPriceHaveTax'),
                orderTotalPrice: this.isSale ? orders.reduce((total, row) => total + (row.orderTotalPrice ?? 0), 0) : null,
                orderTotalPriceHaveTax: this.isSale ? orders.reduce((total, row) => total + (row.orderTotalPriceHaveTax ?? 0), 0) : null,
                moldCost: sum('moldCost'),
                returnQuantity: sum('returnQuantity'),
                returnAmount: sum('returnAmount'),
                returnAmountHaveTax: sum('returnAmountHaveTax')
            }]
        }
    },
    created() { this.getData() },
    methods: {
        cellSpan(data: any): any {
            const fields = ['orderNo', 'customerName', 'dateTime']
            if (this.isSale) fields.push('customerOrderNo', 'orderTotalPrice', 'orderTotalPriceHaveTax')
            if (fields.includes(data.column.field))
                return this.$utils.getCellSpan(data.row, data.rowIndex, (this.$refs.dataGrid as any)?.frozenRows ?? this.result.rows, 'orderId')
            return { rowspan: 1, colspan: 1 }
        },
        getWeekDetails(): Promise<HttpResponseType> {
            const action = this.isSale ? 'GetWeekSaleOrderDetails' : 'GetWeekDeliveryDetails'
            return this.$http.get(`api/sales/SaleDataStat/${action}`, {
                month: this.query.month, weekNo: this.query.weekNo, customerId: this.query.customerId ?? undefined,
                filterCategory: this.query.filterCategory, categoryId: this.query.categoryId ?? undefined,
                customerName: this.query.customerName || undefined,
                includeTax: this.query.includeTax,
                onlyReturns: this.query.detailType === SaleStatDetailType.退货
            })
        },
        getData() {
            if (this.isLoading) return
            this.isLoading = true
            return this.getWeekDetails().then(res => {
                this.result = new MonthSaleWeekDetailResultDto(res.data)
            }).catch(err => {
                this.$kingzon_messager.showError(err || '周明细查询失败')
            }).finally(() => { this.isLoading = false })
        },
        amountText(value: number | null, digits = 2): string {
            return value == null ? '—' : value.toLocaleString('zh-CN', { minimumFractionDigits: digits, maximumFractionDigits: digits })
        }
    }
})
</script>
