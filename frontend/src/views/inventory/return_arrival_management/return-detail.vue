<template>
    <Loading v-show="isLoading" />
    <div class="kingzon-btn-group" style="margin: 5px">
        <LinkButton iconCls="icon-add" @click="openBatchArrival(false)" :disable="selectedRows.length === 0">批量到货</LinkButton>
        <LinkButton iconCls="icon-save" @click="openBatchArrival(true)" :disable="selectedRows.length === 0">批量到货并入库</LinkButton>
    </div>
    <KingZonDataGrid ref="dataGrid" class="kingzon-page-fill" :data="detailList" :showDownLoad="true" exportFileName="销售退货待到货" emptyMsg="没有符合条件的数据"
        :isCheckBoxSelection="true" idField="id" :cellSpan="cellSpan" :sorts="[{ field: 'saleReturnOrderNo', order: 'desc' }]" @pageChange="getData" @sortChange="getData" @seletedRowsChanged="selectedRows = $event">
        <template #left>
            <GridColumn field="index" align="center" width="42"><template #body="scope">{{ scope.rowIndex + 1 }}</template></GridColumn>
            <GridColumn field="saleReturnOrderNo" title="退货单号" width="145" :sortable="true" />
            <GridColumn field="productNo" title="产品编码" width="115" :sortable="true" />
            <GridColumn field="productName" title="产品名称" width="150" :sortable="true" :overflowTextEllipsis="true" />
        </template>
        <template #center>
            <GridColumn field="customerMaterialNo" title="客户产品图号" width="130" :overflowTextEllipsis="true" />
            <GridColumn field="customerMaterialName" title="客户产品名称" width="140" :overflowTextEllipsis="true" />
            <GridColumn field="productSpecification" title="规格型号" width="120" :sortable="true" :overflowTextEllipsis="true" />
            <GridColumn field="technicalCode" title="技术型号" width="110" :sortable="true" :overflowTextEllipsis="true" />
            <GridColumn field="customerName" title="退货客户" width="150" :overflowTextEllipsis="true" />
            <GridColumn field="saleReturnTime" title="退货日期" width="110"><template #body="scope">{{ $utils.formatDateTime(scope.row.saleReturnTime, 'yyyy-MM-dd') }}</template></GridColumn>
            <GridColumn field="returnQuantity" title="退货数量" align="right" width="110"><template #body="scope"><UnitQuantityShow :quantity="scope.row.returnQuantity" :unitList="scope.row.unitInfos" :unitId="scope.row.returnUnitConversionId" /></template></GridColumn>
            <GridColumn field="arrivalQuantity" title="累计到货" align="right" width="110"><template #body="scope"><UnitQuantityShow :quantity="scope.row.arrivalQuantity" :unitList="scope.row.unitInfos" :unitId="scope.row.returnUnitConversionId" /></template></GridColumn>
            <GridColumn field="remainingQuantity" title="待到货量" align="right" width="110"><template #body="scope"><UnitQuantityShow :quantity="scope.row.remainingQuantity" :unitList="scope.row.unitInfos" :unitId="scope.row.returnUnitConversionId" /></template></GridColumn>
            <GridColumn field="reason" title="退货原因" width="140" :overflowTextEllipsis="true" />
            <GridColumn field="remark" title="明细备注" width="140" :overflowTextEllipsis="true" />
            <GridColumn field="saleReturnOrderRemark" title="退货单备注" width="160" :overflowTextEllipsis="true" />
        </template>
        <template #right><GridColumn field="operation" title="操作" align="center" width="165"><template #body="scope"><LinkButton @click="openOrderArrival(scope.row, false)">确认到货</LinkButton><LinkButton @click="openOrderArrival(scope.row, true)">到货并入库</LinkButton></template></GridColumn></template>
    </KingZonDataGrid>
    <Drawer v-if="showArrivalDrawer" ref="arrivalDrawer" region="east" :drawerStyle="{ width: '92%' }" :closable="true" :maskClosable="false" @collapse="showArrivalDrawer = false">
        <template #header><div class="f-full" style="line-height: 30px">{{ arrivalInbound ? '退货到货并入库' : '退货到货登记' }}</div></template>
        <AddReturnArrivalOrder :rows="arrivalRows" :isInbound="arrivalInbound" @cancel="closeDrawer" @submit="saveSuccess" />
    </Drawer>
</template>
<script lang="ts">
import { defineComponent, type PropType } from 'vue'
import type { Filter } from '@/types/types'
import { ReturnArrivalDetailDTO } from '@/types/inventory'
import AddReturnArrivalOrder from './add-return-arrival-order.vue'
import UnitQuantityShow from './unit-form-quantity-show.vue'

export interface ReturnArrivalFilterDTO { isComplete: boolean; saleReturnOrderFilterList: Filter[]; saleReturnOrderDetailFilterList: Filter[]; productFilterList: Filter[]; customerFilterList: Filter[] }
export default defineComponent({
    name: 'returnDetail', components: { AddReturnArrivalOrder, UnitQuantityShow }, emits: ['changed'],
    props: { filter: { type: Object as PropType<ReturnArrivalFilterDTO>, required: true } },
    data() { return { isLoading: false, detailList: [] as ReturnArrivalDetailDTO[], selectedRows: [] as ReturnArrivalDetailDTO[], showArrivalDrawer: false, arrivalRows: [] as ReturnArrivalDetailDTO[], arrivalInbound: false, mergeColumns: ['saleReturnOrderNo', 'customerName', 'saleReturnTime', 'saleReturnOrderRemark', 'operation'] } },
    mounted() { this.getData() },
    methods: {
        getData() { this.isLoading = true; this.$http.post('api/inventory/ReturnArrival/GetReturnArrivalDetails', this.filter, this.$refs.dataGrid as any).then((res: any) => { this.detailList = res.data ?? []; this.selectedRows = [] }).catch((err: unknown) => this.$kingzon_messager.showError(err || '退货到货查询失败')).finally(() => { this.isLoading = false }) },
        openOrderArrival(row: ReturnArrivalDetailDTO, isInbound: boolean) { this.isLoading = true; this.$http.get('api/inventory/ReturnArrival/GetReturnArrivalOrderDetails', { SaleReturnOrderId: row.saleReturnOrderId }).then((res: any) => { this.arrivalRows = res.data ?? []; this.arrivalInbound = isInbound; this.showArrivalDrawer = true; this.$nextTick(() => (this.$refs.arrivalDrawer as EasyUI.Drawer)?.expand()) }).catch((err: unknown) => this.$kingzon_messager.showError(err || '退货单明细查询失败')).finally(() => { this.isLoading = false }) },
        openBatchArrival(isInbound: boolean) { if (!this.selectedRows.length) return this.$kingzon_messager.showError('请选择待到货明细'); this.arrivalRows = this.selectedRows.map(row => new ReturnArrivalDetailDTO(row)); this.arrivalInbound = isInbound; this.showArrivalDrawer = true; this.$nextTick(() => (this.$refs.arrivalDrawer as EasyUI.Drawer)?.expand()) },
        closeDrawer() { (this.$refs.arrivalDrawer as EasyUI.Drawer)?.collapse() },
        saveSuccess() { this.closeDrawer(); this.getData(); this.$emit('changed') },
        cellSpan(data: any) { return this.mergeColumns.includes(data.column.field) ? this.$utils.getCellSpan(data.row, data.rowIndex, this.detailList, 'saleReturnOrderNo') : { rowspan: 1, colspan: 1 } },
    },
})
</script>
