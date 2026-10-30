<template>
    <Loading v-show="isLoading" />
    <div class="kingzon-btn-group" style="margin: 5px">
        <LinkButton iconCls="icon-add" @click="openBatchArrivalDrawer" :disable="selectedRows.length == 0">
            批量到货</LinkButton>
    </div>
    <KingZonDataGrid ref="dataGrid" class="kingzon-page-fill" :data="detailList" :showDownLoad="true"
        exportFileName="销售退货到货" emptyMsg="没有符合条件的数据" :isCheckBoxSelection="true"
        @seletedRowsChanged="onCheckBoxClicked" idField="id" :cellSpan="cellSpan"
        :sorts="[{ field: 'saleReturnOrderNo', order: 'desc' }]" @pageChange="getData" @sortChange="getData"
        @rowContextMenu="rowContextMenu($event)">
        <GridColumnGroup :frozen="true" align="left" width="530">
            <GridHeaderRow>
                <GridColumn field="index" align="center" width="40"><template #body="scope">{{
                    scope.rowIndex + 1 }}</template>
                </GridColumn>
                <GridColumn field="saleReturnOrderNo" title="退货单号" width="145" :sortable="true" />
                <GridColumn field="productNo" title="产品编码" width="115" :sortable="true" />
                <GridColumn field="productName" title="产品名称" width="150" :sortable="true"
                    :overflowTextEllipsis="true" />
            </GridHeaderRow>
        </GridColumnGroup>
        <GridColumnGroup align="center">
            <GridHeaderRow>
                <GridColumn field="customerMaterialNo" title="客户产品图号" width="120" :sortable="true"
                    :overflowTextEllipsis="true" />
                <GridColumn field="technicalCode" title="技术型号" width="100" :sortable="true"
                    :overflowTextEllipsis="true" />
                <GridColumn field="productCategory" title="产品类别" width="110" :sortable="true"
                    :overflowTextEllipsis="true" />
                <GridColumn field="productSpecification" title="规格型号" width="120" :sortable="true"
                    :overflowTextEllipsis="true" />
                <GridColumn field="materialGrade" title="材质牌号" width="100" :sortable="true"
                    :overflowTextEllipsis="true" />
                <GridColumn field="materialSpecification" title="原材料规格" width="120" :sortable="true"
                    :overflowTextEllipsis="true" />
                <GridColumn field="productRemark" title="产品备注" width="140" :overflowTextEllipsis="true" />
                <GridColumn field="customerCode" title="客户编码" width="105" :sortable="true" />
                <GridColumn field="customerName" title="客户名称" width="150" :sortable="true"
                    :overflowTextEllipsis="true" />
                <GridColumn field="saleReturnTime" title="退货日期" width="110"><template #body="scope">{{
                    $utils.formatDateTime(scope.row.saleReturnTime, 'yyyy-MM-dd')
                        }}</template>
                </GridColumn>
                <GridColumn field="returnQuantity" title="退货数量" align="right" width="110">
                    <template #body="scope">
                        <UnitQuantityShow :quantity="scope.row.returnQuantity" :unitList="scope.row.unitInfos"
                            :unitId="scope.row.returnUnitConversionId">
                        </UnitQuantityShow>
                    </template>
                </GridColumn>
                <GridColumn field="arrivalQuantity" title="累计到货" align="right" width="110">
                    <template #body="scope">
                        <UnitQuantityShow :quantity="scope.row.arrivalQuantity" :unitList="scope.row.unitInfos"
                            :unitId="scope.row.returnUnitConversionId">
                        </UnitQuantityShow>
                    </template>
                </GridColumn>
                <GridColumn field="remainingQuantity" title="待到货数量" align="right" width="120">
                    <template #body="scope">
                        <UnitQuantityShow :quantity="scope.row.remainingQuantity" :unitList="scope.row.unitInfos"
                            :unitId="scope.row.returnUnitConversionId">
                        </UnitQuantityShow>
                    </template>
                </GridColumn>
                <GridColumn field="reason" title="退货原因" width="160" :overflowTextEllipsis="true" />
                <GridColumn field="remark" title="明细备注" width="160" :overflowTextEllipsis="true" />
            </GridHeaderRow>
        </GridColumnGroup>
        <GridColumnGroup align="right" :frozen="true">
            <GridHeaderRow>
                <GridColumn field="options" title="操作" align="center" width="110">
                    <template #body="scope">
                        <LinkButton v-if="!scope.row.isComplete" @click="openOrderDrawer(scope.row)">
                            确认到货 </LinkButton>
                    </template>
                </GridColumn>
            </GridHeaderRow>
        </GridColumnGroup>
    </KingZonDataGrid>
    <Drawer v-if="showDetailDrawer" ref="detailDrawer" region="east" :drawerStyle="{ width: '540px' }" :closable="true"
        :maskClosable="false" @collapse="showDetailDrawer = false">
        <template #header>
            <div class="f-full" style="line-height: 30px">退货明细到货登记</div>
        </template>
        <AddReturnArrivalDetail :row="contextMenuRow" @cancel="closeDetailDrawer" @submit="saveSuccess" />
    </Drawer>
    <Drawer v-if="showOrderDrawer" ref="orderDrawer" region="east" :drawerStyle="{ width: '850px' }" :closable="true"
        :maskClosable="false" @collapse="showOrderDrawer = false">
        <template #header>
            <div class="f-full" style="line-height: 30px">退货订单到货登记</div>
        </template>
        <AddReturnArrivalOrder :rows="orderRows" @cancel="closeOrderDrawer" @submit="saveSuccess" />
    </Drawer>
    <Drawer v-if="showBatchArrivalDrawer" ref="batchArrivalDrawer" region="east"
        :drawerStyle="{ width: '90%' }" :closable="true" :maskClosable="false"
        @collapse="showBatchArrivalDrawer = false">
        <template #header>
            <div class="f-full" style="line-height: 30px">批量到货登记</div>
        </template>
        <AddReturnArrivalOrder :rows="batchArrivalRows" @cancel="closeBatchArrivalDrawer" @submit="saveSuccess" />
    </Drawer>
    <Menu v-if="contextMenuVisible" :inline="true" :style="contextMenuStyle" class="detail-menu-wrapper">
        <MenuItem v-for="item in contextMenuItems" :key="String(item.value)" :text="item.text"
            @click="onContextMenuClick(item)" />
    </Menu>
</template>
<script lang="ts">
import { defineComponent } from 'vue'
import { Filter, Search, type FilterTmp, type SelectOption } from '@/types/types'
import { ReturnArrivalDetailDTO } from '@/types/inventory.js'
import AddReturnArrivalDetail from './add-return-arrival-detail.vue'
import AddReturnArrivalOrder from './add-return-arrival-order.vue'
import UnitQuantityShow from '../arrival_record_management/unit-quantity-show.vue'


/** 退货到货查询条件。 */
export class ReturnArrivalFilterDTO {
    isComplete = false
    saleReturnOrderFilterList: Filter[] = []
    saleReturnOrderDetailFilterList: Filter[] = []
    productFilterList: Filter[] = []
}
export default defineComponent({
    name: 'returnDetail',
    components: { AddReturnArrivalDetail, AddReturnArrivalOrder, UnitQuantityShow },
    props: {
        filter: {
            type: ReturnArrivalFilterDTO,
            default: {}
        }
    },
    data() {
        return {
            isLoading: false,
            detailList: [] as ReturnArrivalDetailDTO[],
            contextMenuVisible: false,
            contextMenuStyle: { left: '0px', top: '0px' } as any,
            contextMenuItems: [] as SelectOption[],
            contextMenuRow: new ReturnArrivalDetailDTO(),
            showDetailDrawer: false,
            showOrderDrawer: false,
            orderRows: [] as ReturnArrivalDetailDTO[],
            showBatchArrivalDrawer: false,
            batchArrivalRows: [] as ReturnArrivalDetailDTO[],
            selectedRows: [] as ReturnArrivalDetailDTO[],
            cellSpanColumns: ['saleReturnOrderNo', 'customerCode', 'customerName', 'saleReturnTime', 'options']
        }
    },
    created() {
        this.getData()
    },
    mounted() {
        document.addEventListener('pointerdown', this.onDocumentPointerDown)
    },
    beforeUnmount() {
        document.removeEventListener('pointerdown', this.onDocumentPointerDown)
    },
    methods: {
        getData() {
            this.isLoading = true
            const dataGrid = this.$refs.dataGrid as any
            this.$http
                .post(
                    'api/inventory/ReturnArrival/GetReturnArrivalDetails',
                    this.filter,
                    dataGrid
                )
                .then((res: any) => {
                    this.detailList = res.data ?? []
                })
                .catch((err: unknown) => this.$kingzon_messager.showError(err || '退货到货查询失败'))
                .finally(() => {
                    this.isLoading = false
                })
        },
        rowContextMenu(event: any) {
            event.originalEvent?.preventDefault()
            this.contextMenuRow = new ReturnArrivalDetailDTO(event.row)
            this.contextMenuItems = this.contextMenuRow.isComplete
                ? [{ text: '已完成，无可用操作' }]
                : [
                    { text: '明细到货登记', value: 'detailArrival' }
                ]
            this.contextMenuStyle = {
                left: `${event.originalEvent?.clientX ?? 0}px`,
                top: `${event.originalEvent?.clientY ?? 0}px`
            }
            this.contextMenuVisible = true
        },
        onContextMenuClick(item: SelectOption) {
            this.contextMenuVisible = false
            if (item.value === 'detailArrival') this.openDetailDrawer()
        },
        openDetailDrawer() {
            this.showDetailDrawer = true
            this.$nextTick(() => (this.$refs.detailDrawer as EasyUI.Drawer).expand())
        },
        openOrderDrawer(row: ReturnArrivalDetailDTO) {
            this.isLoading = true
            this.$http
                .get('api/inventory/ReturnArrival/GetReturnArrivalOrderDetails', {
                    SaleReturnOrderId: row.saleReturnOrderId
                })
                .then((res: any) => {
                    this.orderRows = res.data ?? []
                    this.showOrderDrawer = true
                    this.$nextTick(() => (this.$refs.orderDrawer as EasyUI.Drawer).expand())
                })
                .catch((err: unknown) => this.$kingzon_messager.showError(err || '退货订单明细查询失败'))
                .finally(() => {
                    this.isLoading = false
                })
        },
        closeDetailDrawer() {
            (this.$refs.detailDrawer as EasyUI.Drawer).collapse()
        },
        closeOrderDrawer() {
            (this.$refs.orderDrawer as EasyUI.Drawer).collapse()
        },
        onCheckBoxClicked(list: ReturnArrivalDetailDTO[]) {
            this.selectedRows = list ?? []
        },
        openBatchArrivalDrawer() {
            if (this.selectedRows.length == 0) {
                this.$kingzon_messager.showError("请选择一条记录")
                return
            }
            this.batchArrivalRows = this.selectedRows.map(row => new ReturnArrivalDetailDTO(row))
            this.showBatchArrivalDrawer = true
            this.$nextTick(() => (this.$refs.batchArrivalDrawer as EasyUI.Drawer).expand())
        },
        closeBatchArrivalDrawer() {
            this.showBatchArrivalDrawer = false
            ;(this.$refs.batchArrivalDrawer as EasyUI.Drawer)?.collapse()
        },
        saveSuccess() {
            if (this.showDetailDrawer) this.closeDetailDrawer()
            if (this.showOrderDrawer) this.closeOrderDrawer()
            if (this.showBatchArrivalDrawer) this.closeBatchArrivalDrawer()
            this.getData()
        },
        onDocumentPointerDown(event: PointerEvent) {
            if (!(event.target as Element | null)?.closest('.detail_menu'))
                this.contextMenuVisible = false
        },
        cellSpan(data: any) {
            if (this.cellSpanColumns.includes(data.column.field)) {
                var rowIndex = data.rowIndex;
                return this.$utils.getCellSpan(data.row, rowIndex, this.detailList, 'saleReturnOrderNo');
            }
            return { rowspan: 1, colspan: 1 };
        }
    }
})
</script>
<style>
.detail-menu-wrapper {
    position: fixed;
    z-index: 99999;
}
</style>