<template>
    <Loading v-show="isLoading"></Loading>
    <div class="kingzon-page">
        <div class="kingzon-page-title" style="display: flex; justify-content: space-between; align-items: center;">
            <span>订单及销售发货月报表</span>
        </div>
        <div class="kingzon-page-body">
            <div style="display: flex;">
                <SearchPanel :searchParams="searchParams" :showIcon="false" @search="search" @reset="reset">
                </SearchPanel>
                <div class="kingzon-btn-group">
                    <LinkButton :disabled="isLoading || isExporting || report.year < 2" @click="exportExcel">
                        {{ isExporting ? '导出中…' : '导出 Excel' }}
                    </LinkButton>
                </div>
            </div>
            <div class="kingzon-page-content">
                <div style="padding: 5px; text-align: center; font-weight: bold;">{{ reportTitle }}</div>
                <div style="display: flex;">
                    <div style="padding: 0 5px 5px;flex: 1;">
                        账期：{{ dateText(report.startDate) }} 至 {{ dateText(report.endDate) }}；
                        分类内按本年1月净发货额从大到小排名。同比＝（今年发货额－去年同期销售额）÷去年同期销售额；去年销售额为零时显示“—”。
                    </div>
                    <div style="display: flex; align-items: center; gap: 8px;">
                        <RadioButton inputId="saleStatWithoutTax" name="saleStatTax" :value="false"
                            :modelValue="includeTax" :disabled="isLoading || isExporting" @update:modelValue="toggleTax" />
                        <Label for="saleStatWithoutTax">不含税</Label>
                        <RadioButton inputId="saleStatWithTax" name="saleStatTax" :value="true"
                            :modelValue="includeTax" :disabled="isLoading || isExporting" @update:modelValue="toggleTax" />
                        <Label for="saleStatWithTax">含税</Label>
                    </div>
                </div>
                <KingZonDataGrid v-if="!isLoading" :key="gridKey"
                    :id="'monthSaleDataStat-weekAmount-' + report.weeks.length" ref="dataGrid" class="kingzon-page-fill"
                    :data="tableRows" idField="id" :pagination="false" :lazy="false" :columnMoving="false"
                    :multiSort="false" :showFieldFilter="false" :showDownLoad="false" selectionMode="null"
                    :exportFileName="reportTitle" emptyMsg="本账期及比较期暂无数据" :rowCss="rowCss" :footerData="footerData"
                    :showFooter="true">
                    <GridColumnGroup :frozen="true" align="left" :width="300">
                        <GridHeaderRow>
                            <GridColumn field="rank" title="" align="center" :width="50"></GridColumn>
                            <GridColumn field="customerName" title="客户" :width="150" :overflowTextEllipsis="true">
                            </GridColumn>
                        </GridHeaderRow>
                    </GridColumnGroup>
                    <GridColumnGroup align="center" :columnMoving="false">
                        <GridHeaderRow>
                            <template v-for="week in report.weeks" :key="'order-header-' + week.weekNo">
                                <GridColumn halign="center" :title="weekTitle(week.weekNo) + '订单'" :colspan="1">
                                </GridColumn>
                            </template>
                            <GridColumn field="orderAmount" title="订单合计" align="right" :width="120" :rowspan="2">
                                <template #body="scope">{{ amountText(scope.row.orderAmount) }}</template>
                                <template #footer="scope">{{ amountText(scope.row.orderAmount) }}</template>
                            </GridColumn>
                            <template v-for="week in report.weeks" :key="'delivery-header-' + week.weekNo">
                                <GridColumn halign="center" :title="weekTitle(week.weekNo) + '发货'" :colspan="1">
                                </GridColumn>
                            </template>
                            <GridColumn field="returnAmount" title="退货" align="right" :width="110" :rowspan="2"
                                :headerStyle="{ color: 'var(--color-red)' }">
                                <template #body="scope"><span class="kingzon-detail-text"
                                        style="color: var(--color-red);" @click="showReturnDetails(scope.row)">{{
                                            amountText(scope.row.returnAmount) }}</span></template>
                                <template #footer="scope"><span class="kingzon-detail-text"
                                        style="color: var(--color-red);" @click="showReturnDetails(scope.row)">{{
                                            amountText(scope.row.returnAmount) }}</span></template>
                            </GridColumn>
                            <GridColumn field="deliveryAmount" :title="currentMonthTitle + '发货额'" align="right"
                                :width="140" :rowspan="2">
                                <template #body="scope">{{ amountText(scope.row.deliveryAmount) }}</template>
                                <template #footer="scope">{{ amountText(scope.row.deliveryAmount) }}</template>
                            </GridColumn>
                            <GridColumn field="lastYearOrderAmount" :title="lastYearTitle + '销售额'" align="right"
                                :width="140" :rowspan="2">
                                <template #body="scope">{{ amountText(scope.row.lastYearOrderAmount) }}</template>
                                <template #footer="scope">{{ amountText(scope.row.lastYearOrderAmount)
                                    }}</template>
                            </GridColumn>
                            <GridColumn field="yearOnYearRate" title="同比%" align="right" :width="90" :rowspan="2">
                                <template #footer="scope">
                                    <span :style="{ color: rateColor(scope.row.yearOnYearRate) }">{{
                                        percentText(scope.row.yearOnYearRate) }}</span>
                                </template>
                                <template #body="scope">
                                    <div style="display: flex; align-items: center; width: 100%;"
                                        :style="{ color: rateColor(scope.row.yearOnYearRate) }">
                                        <span style="width: 12px; flex-shrink: 0; text-align: left;">
                                            <font-awesome-icon
                                                v-if="scope.row.yearOnYearRate != null && scope.row.yearOnYearRate !== 0"
                                                :icon="['fas', scope.row.yearOnYearRate < 0 ? 'arrow-down' : 'arrow-up']"
                                                :aria-label="scope.row.yearOnYearRate < 0 ? '同比下降' : '同比上升'" />
                                        </span>
                                        <span style="flex: 1; min-width: 0; margin-left: 4px; text-align: right;">{{
                                            percentText(scope.row.yearOnYearRate) }}</span>
                                    </div>
                                </template>
                            </GridColumn>
                            <GridColumn field="deliveryShareRate" title="客户送货占比%" align="right" :width="130"
                                :rowspan="2">
                                <template #body="scope">{{ percentText(scope.row.deliveryShareRate) }}</template>
                                <template #footer="scope">{{ percentText(scope.row.deliveryShareRate) }}</template>
                            </GridColumn>
                        </GridHeaderRow>
                        <GridHeaderRow>
                            <template v-for="week in report.weeks" :key="'order-' + week.weekNo">
                                <GridColumn halign="center" :field="'orderWeek' + week.weekNo" :title="weekRange(week)"
                                    align="right" :width="120">
                                    <template #body="scope">
                                        <span
                                            :class="scope.row.weekAmounts[scope.column.field] != 0 ? 'kingzon-detail-text' : ''"
                                            @click="showWeekDetails(scope.row, scope.column.field)">{{
                                                amountText(scope.row.weekAmounts[scope.column.field]) }}</span>
                                    </template>
                                    <template #footer="scope">
                                        <span class="kingzon-detail-text"
                                            @click="showWeekDetails(scope.row, scope.column.field)">{{
                                                amountText(scope.row.weekAmounts[scope.column.field]) }}</span>
                                    </template>
                                </GridColumn>
                            </template>
                            <template v-for="week in report.weeks" :key="'delivery-' + week.weekNo">
                                <GridColumn halign="center" :field="'deliveryWeek' + week.weekNo"
                                    :title="weekRange(week)" align="right" :width="120">
                                    <template #body="scope">
                                        <span
                                            :class="scope.row.weekAmounts[scope.column.field] != 0 ? 'kingzon-detail-text' : ''"
                                            @click="showWeekDetails(scope.row, scope.column.field)">{{
                                                amountText(scope.row.weekAmounts[scope.column.field]) }}</span>
                                    </template>
                                    <template #footer="scope">
                                        <span class="kingzon-detail-text"
                                            @click="showWeekDetails(scope.row, scope.column.field)">{{
                                                amountText(scope.row.weekAmounts[scope.column.field]) }}</span>
                                    </template>
                                </GridColumn>
                            </template>
                        </GridHeaderRow>
                    </GridColumnGroup>
                </KingZonDataGrid>
                <div v-else class="kingzon-page-fill" style="padding: 20px; text-align: center;">{{ isLoading ?
                    '正在查询月报表…' : '请选择月份查询'
                }}</div>
            </div>
        </div>
    </div>
    <Dialog ref="weekDetailDialog" :title="weekDetailTitle" :modal="true" closed :draggable="true" :resizable="true"
        :dialogStyle="{ width: '90%', height: '85%' }">
        <SaleStatWeekDetail :key="weekDetailKey" :query="weekDetailQuery" @close="closeWeekDetails">
        </SaleStatWeekDetail>
    </Dialog>
</template>

<script lang="ts">
import { defineComponent } from 'vue'
import { DataType, FilterType } from '@/types/enums'
import { Search, type FilterTmp } from '@/types/types'
import {
    MonthSaleDataStatDto,
    MonthSaleTableRowDto,
    FinanceWeekDto,
    SaleStatRowType,
    MonthSaleWeekDetailQueryDto,
    SaleStatDetailType
} from '@/types/sales'
import SaleStatWeekDetail from './component/sale-stat-week-detail.vue'

export default defineComponent({
    name: 'saleDataStat',
    components: { SaleStatWeekDetail },
    data() {
        const now = new Date()
        return {
            year: now.getFullYear(),
            month: now.getMonth() + 1,
            customerName: '',
            includeTax: false,
            monthOptions: Array.from({ length: 12 }, (_, index) => ({ value: index + 1, text: `${index + 1}月` })),
            searchParams: [] as Search[],
            report: new MonthSaleDataStatDto(),
            tableRows: [] as MonthSaleTableRowDto[],
            isLoading: false,
            isExporting: false,
            gridKey: 0,
            footerData: [] as MonthSaleTableRowDto[],
            reportCustomerName: '',
            weekDetailKey: 0,
            weekDetailQuery: new MonthSaleWeekDetailQueryDto()
        }
    },
    computed: {
        reportTitle(): string {
            return !this.isLoading ? `${this.report.year}年${this.report.month}月订单及销售发货月报表（${this.report.includeTax ? '含税' : '不含税'}）` : '订单及销售发货月报表'
        },
        weekDetailTitle(): string {
            return `${this.weekDetailQuery.title}（${this.weekDetailQuery.includeTax ? '含税' : '不含税'}） ${this.dateText(this.weekDetailQuery.startDate)} 至 ${this.dateText(this.weekDetailQuery.endDate)}`
        },
        currentMonthTitle(): string {
            return `${this.report.year}年${this.report.month}月`
        },
        lastYearTitle(): string {
            return `${this.report.year - 1}年${this.report.month}月`
        },
        previousMonthTitle(): string {
            return this.report.month === 1 ? `${this.report.year - 1}年12月` : `${this.report.year}年${this.report.month - 1}月`
        }
    },
    created() {
        this.init()
    },
    methods: {
        init() {
            this.addSearchParam()
            this.getData()
        },
        addSearchParam() {
            this.searchParams = [
                new Search('year', '开账年份', 'monthSaleStat', FilterType.Equals, DataType.Integer, this.year, undefined, undefined, '180px'),
                new Search('month', '开账月份', 'monthSaleStat', FilterType.Equals, DataType.SelectNum, this.month, this.monthOptions, undefined, '180px'),
                new Search('customerName', '客户', 'monthSaleStat', FilterType.Contains, DataType.Text, undefined, undefined, '客户名称', '240px')
            ]
        },
        search(params: FilterTmp[]) {
            if (this.isLoading) return
            const filters = params.find(item => item.table === 'monthSaleStat')?.filter ?? []
            const now = new Date()
            this.year = Number(filters.find(item => item.key === 'year')?.value ?? now.getFullYear())
            this.month = Number(filters.find(item => item.key === 'month')?.value ?? now.getMonth() + 1)
            this.customerName = String(filters.find(item => item.key === 'customerName')?.value ?? '').trim()
            return this.getData()
        },
        getMonthDataStat(): Promise<HttpResponseType> {
            return this.$http.get('api/sales/SaleDataStat/GetMonthDataStat', {
                month: `${this.year}-${String(this.month).padStart(2, '0')}-01`,
                customerName: this.customerName.trim() || undefined,
                includeTax: this.includeTax
            })
        },
        exportExcel() {
            if (this.isLoading || this.isExporting || this.report.year < 2) return
            const fileName = `${this.report.year}年${this.report.month}月订单及销售发货月报表.xlsx`
            this.isExporting = true
            // getFile 沿用包装标准的 POST 文件下载封装，条件取当前已加载报表。
            return this.$http.getFile('/api/sales/SaleDataStat/ExportMonthDataStat', {
                month: `${this.report.year}-${String(this.report.month).padStart(2, '0')}-01`,
                customerName: this.reportCustomerName || undefined,
                includeTax: this.report.includeTax
            }).then(res => {
                if (!res.url) throw new Error('文件无法下载')
                const link = document.createElement('a')
                link.download = res.name || fileName
                link.href = res.url
                document.body.appendChild(link)
                try {
                    link.click()
                } finally {
                    document.body.removeChild(link)
                }
            }).catch(err => {
                this.$kingzon_messager.showError(err || '月报表导出失败')
            }).finally(() => {
                this.isExporting = false
            })
        },
        toggleTax(value?: boolean) {
            if (this.isLoading || this.isExporting) return
            const selected = value ?? !this.report.includeTax
            if (selected === this.report.includeTax) return
            this.includeTax = selected
            return this.getData()
        },
        getData() {
            if (this.isLoading) return
            if (!Number.isInteger(this.year) || this.year < 2 || this.year > 9998 || !Number.isInteger(this.month) || this.month < 1 || this.month > 12) {
                this.$kingzon_messager.showError('请选择有效的开账月份')
                return
            }
            this.isLoading = true
            return this.getMonthDataStat().then(res => {
                const report = new MonthSaleDataStatDto(res.data as any)
                const rows: MonthSaleTableRowDto[] = []
                for (const category of report.categories) {
                    for (const customer of category.customers) {
                        rows.push(new MonthSaleTableRowDto({
                            ...customer,
                            id: `customer-${customer.customerId}`,
                            rowType: SaleStatRowType.客户,
                            categoryName: category.categoryName,
                            categoryId: category.categoryId
                        }))
                    }
                    rows.push(new MonthSaleTableRowDto({
                        ...category,
                        id: `category-${category.categoryId ?? 'none'}`,
                        rowType: SaleStatRowType.小计,
                        rank: null,
                        customerName: `${category.categoryName}小计`
                    }))
                }
                const footerRows = rows.length > 0 ? [new MonthSaleTableRowDto({
                    ...report, id: 'total', rowType: SaleStatRowType.总计, rank: null, customerName: '总计'
                })] : []
                for (const row of [...rows, ...footerRows]) {
                    for (const week of row.orderWeeks) row.weekAmounts[`orderWeek${week.weekNo}`] = week.amount
                    for (const week of row.deliveryWeeks) row.weekAmounts[`deliveryWeek${week.weekNo}`] = week.amount
                }
                this.report = report
                this.includeTax = report.includeTax
                this.tableRows = rows
                this.footerData = footerRows
                this.reportCustomerName = this.customerName.trim()
                // 月份变化可能产生不同周数，重新初始化多级表头及导出列。
                this.gridKey++
            }).catch(err => {
                this.includeTax = this.report.includeTax
                this.$kingzon_messager.showError(err || '月报表查询失败')
            }).finally(() => {
                this.isLoading = false
            })
        },
        reset() {
            if (this.isLoading) return
            const now = new Date()
            this.year = now.getFullYear()
            this.month = now.getMonth() + 1
            this.customerName = ''
            this.addSearchParam()
            return this.getData()
        },
        showWeekDetails(row: MonthSaleTableRowDto, field: string) {
            if (row.weekAmounts[field] == 0) {
                return
            }
            const match = /^(order|delivery)Week(\d+)$/.exec(field)
            if (!match) return
            const week = this.report.weeks.find(item => item.weekNo === Number(match[2]))
            if (!week) return
            const detailType = match[1] === 'order' ? SaleStatDetailType.销售 : SaleStatDetailType.送货
            this.weekDetailQuery = new MonthSaleWeekDetailQueryDto({
                month: `${this.report.year}-${String(this.report.month).padStart(2, '0')}-01`,
                weekNo: week.weekNo,
                customerId: row.rowType === SaleStatRowType.客户 ? row.customerId : null,
                filterCategory: row.rowType === SaleStatRowType.小计,
                categoryId: row.categoryId,
                customerName: this.reportCustomerName,
                detailType,
                includeTax: this.report.includeTax,
                title: `${row.customerName} - ${this.weekTitle(week.weekNo)}${detailType === SaleStatDetailType.销售 ? '销售' : '送货'}明细`,
                expectedAmount: row.weekAmounts[field] ?? 0,
                startDate: week.startDate, endDate: week.endDate
            })
            this.weekDetailKey++
            this.$nextTick(() => { (this.$refs.weekDetailDialog as EasyUI.Dialog)?.open() })
        },
        showReturnDetails(row: MonthSaleTableRowDto) {
            this.weekDetailQuery = new MonthSaleWeekDetailQueryDto({
                month: `${this.report.year}-${String(this.report.month).padStart(2, '0')}-01`,
                weekNo: 0,
                customerId: row.rowType === SaleStatRowType.客户 ? row.customerId : null,
                filterCategory: row.rowType === SaleStatRowType.小计,
                categoryId: row.categoryId,
                customerName: this.reportCustomerName,
                detailType: SaleStatDetailType.退货,
                includeTax: this.report.includeTax,
                title: `${row.customerName} - 退货送货单明细`,
                expectedAmount: row.returnAmount,
                startDate: this.report.startDate, endDate: this.report.endDate
            })
            this.weekDetailKey++
            this.$nextTick(() => { (this.$refs.weekDetailDialog as EasyUI.Dialog)?.open() })
        },
        closeWeekDetails() {
            (this.$refs.weekDetailDialog as EasyUI.Dialog)?.close()
        },
        rowCss(row: MonthSaleTableRowDto) {
            if (row.rowType === SaleStatRowType.小计) return 'sale-stat-subtotal'
            return row.rowType === SaleStatRowType.客户 ? {} : { fontWeight: 'bold' }
        },
        amountText(value?: number): string {
            return value == null || value === 0 ? '-' : value.toLocaleString('zh-CN', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
        },
        rateText(value: number | null): string {
            if (value === null) return '—'
            return `${value > 0 ? '↑ ' : value < 0 ? '↓ ' : ''}${(value * 100).toFixed(2)}%`
        },
        percentText(value: number | null): string {
            return value == null ? '—' : `${(value * 100).toFixed(2)}%`
        },
        dateText(value: string): string {
            return value ? value.slice(0, 10) : '—'
        },
        rateColor(value: number | null): string | undefined {
            return value == null || value === 0 ? undefined : value < 0 ? 'var(--color-red)' : 'var(--color-green-tag)'
        },
        weekTitle(weekNo: number): string {
            return `第${['一', '二', '三', '四', '五', '六'][weekNo - 1] ?? weekNo}周`
        },
        weekRange(week: FinanceWeekDto): string {
            if (week.startDate.slice(5, 10) == week.endDate.slice(5, 10)) {
                return week.startDate.slice(5, 10)
            }
            return `${week.startDate.slice(5, 10)}至${week.endDate.slice(5, 10)}`
        }
    }
})
</script>

<style scoped>
:deep(.sale-stat-subtotal .datagrid-cell),
:deep(.sale-stat-subtotal .datagrid-cell *) {
    font-weight: 700 !important;
    font-style: italic !important;
}
</style>
