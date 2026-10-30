<template>
    <Loading v-show="isLoading" />
    <div class="kingzon-page-content">
        <div style="flex: 1; min-height: 0; display: flex; flex-direction: row">
            <div style="flex: 1; width: 0; min-width: 0; display: flex; flex-direction: column">
                <KingZonDataGrid id="returnArrivalBatchInboundGrid" ref="dataGrid" class="kingzon-page-fill"
                    style="height: 100%; width: 100%; flex-grow: 1" :data="recordList" :pagination="false"
                    selectionMode="single" :showDownLoad="false" :columnResizing="true" :columnMoving="true"
                    emptyMsg="没有待入库的到货记录" @rowClick="rowClick">
                    <GridColumnGroup :frozen="true" align="left" width="560">
                        <GridHeaderRow>
                        <GridColumn field="index" title="序号" align="center" width="45"><template #body="scope">{{ scope.rowIndex + 1 }}</template></GridColumn>
                        <GridColumn field="arrivalRecordNo" title="到货编号" width="120" />
                        <GridColumn field="saleReturnOrderNo" title="退货单号" width="145" />
                        <GridColumn field="productNo" title="产品编码" width="110" />
                        <GridColumn field="productName" title="产品名称" width="140" :overflowTextEllipsis="true" />
                        </GridHeaderRow>
                    </GridColumnGroup>
                    <GridColumnGroup align="left">
                        <GridHeaderRow>
                        <GridColumn field="customerMaterialNo" title="客户产品图号" width="130" :overflowTextEllipsis="true" />
                        <GridColumn field="customerMaterialName" title="客户产品名称" width="140" :overflowTextEllipsis="true" />
                        <GridColumn field="productSpecification" title="规格型号" width="120" :overflowTextEllipsis="true" />
                        <GridColumn field="technicalCode" title="技术型号" width="110" :overflowTextEllipsis="true" />
                        <GridColumn field="remainingQuantity" title="待入库量" width="120" align="right"><template #body="scope"><UnitQuantityShow :quantity="scope.row.remainingQuantity" :unitList="scope.row.unitInfos" :unitId="scope.row.arrivalUnitConversionId" /></template></GridColumn>
                        <GridColumn field="inboundQuantityText" title="已填写入库量" width="130" align="right" cellCss="kingzon-cell-editable" />
                        <GridColumn field="positionInfo" title="库位库区" width="160" cellCss="kingzon-cell-editable" :overflowTextEllipsis="true" />
                        <GridColumn field="inboundRemark" title="入库备注" width="140" cellCss="kingzon-cell-editable" :overflowTextEllipsis="true" />
                        </GridHeaderRow>
                    </GridColumnGroup>
                </KingZonDataGrid>
            </div>
            <Panel style="width: 500px; flex-shrink: 0">
                <template #header><div style="line-height: 21px">入库信息填写</div></template>
                <Inbound v-if="selectedRow" ref="inbound" :key="selectedRow.id" :dataType="selectedRow.dataType"
                    :dataId="selectedRow.dataId" :dataStatus="InventoryDataStatus.None" displayMode="small"
                    :inboundQuantity="selectedRow.remainingQuantity" :inboundUnitList="getInboundUnitList(selectedRow)"
                    :inboundList="cloneDeep(selectedRow.inboundList)" :showCost="false" :showExtend="false"
                    :showPrice="false" closeText="清空" @confirm="inboundConfirm" @close="clearInbound">
                    <template #showForm><KingZonShowForm :formData="formData" columnWidth="50%" /></template>
                </Inbound>
                <div v-else class="kingzon-detail-text" style="padding: 16px">请选择左侧到货记录填写入库信息</div>
            </Panel>
        </div>
        <div class="kingzon-btn-group kingzon-page-footer">
            <LinkButton @click="$emit('cancel')">取消</LinkButton>
            <LinkButton btnCls="c8" @click="confirm">确定入库</LinkButton>
        </div>
    </div>
</template>

<script lang="ts">
import { defineComponent, type PropType } from 'vue'
import { cloneDeep } from 'lodash'
import { InventoryDataStatus, InventoryDataType, ShowFormDataType } from '@/types/enums'
import type { ShowFormData } from '@/types/types'
import { ReturnArrivalRecordDTO } from '@/types/inventory'
import Inbound from './inbound.vue'
import UnitQuantityShow from './unit-form-quantity-show.vue'

class ReturnArrivalRecordBatchRow extends ReturnArrivalRecordDTO {
    remainingQuantity = 0
    dataType: InventoryDataType = InventoryDataType.Product
    dataId = 0
    inboundList: any[] = []
    inboundQuantityText = ''
    positionInfo = ''
    inboundRemark = ''

    constructor(init?: Partial<ReturnArrivalRecordBatchRow>) { super(init) }
}

export default defineComponent({
    name: 'returnArrivalBatchInbound',
    emits: ['cancel', 'success'],
    components: { Inbound, UnitQuantityShow },
    props: { arrivalRecordIds: { type: Array as PropType<number[]>, default: () => [] } },
    data() {
        return {
            isLoading: false,
            recordList: [] as ReturnArrivalRecordBatchRow[],
            selectedRow: undefined as ReturnArrivalRecordBatchRow | undefined,
            formData: [] as ShowFormData[],
        }
    },
    computed: {
        InventoryDataStatus() { return InventoryDataStatus },
        cloneDeep() { return cloneDeep },
    },
    mounted() { this.getData() },
    methods: {
        async getData() {
            if (!this.arrivalRecordIds.length) return this.$kingzon_messager.showError('未选择任何到货记录')
            try {
                this.isLoading = true
                const res = await this.$http.post('api/inventory/ReturnArrival/GetReturnArrivalInventoryRecords', { arrivalRecordIds: this.arrivalRecordIds })
                this.recordList = (res.data ?? []).map((row: any) => {
                    const batchRow = new ReturnArrivalRecordBatchRow(row)
                    // 子类字段初始化会覆盖基类构造时的同名赋值，因此必须在实例创建后回填。
                    batchRow.dataType = row.dataType ?? InventoryDataType.Product
                    batchRow.dataId = row.dataId ?? row.productId
                    return batchRow
                })
                this.selectedRow = this.recordList[0]
                this.setFormData()
                this.$nextTick(() => (this.$refs.dataGrid as any)?.selectRow?.(this.selectedRow))
            } catch (err) {
                this.$kingzon_messager.showError(err)
            } finally {
                this.isLoading = false
            }
        },
        rowClick(row: ReturnArrivalRecordBatchRow) {
            this.selectedRow = row
            this.setFormData()
        },
        setFormData() {
            const row = this.selectedRow
            this.formData = row ? [
                { label: '到货编号', value: row.arrivalRecordNo ?? '', type: ShowFormDataType.文本, validate: true },
                { label: '退货单号', value: row.saleReturnOrderNo ?? '', type: ShowFormDataType.文本, validate: true },
                { label: '客户产品图号', value: row.customerMaterialNo ?? '', type: ShowFormDataType.文本, validate: true },
                { label: '客户产品名称', value: (row as any).customerMaterialName ?? '', type: ShowFormDataType.文本, validate: true },
                { label: '产品编码', value: row.productNo ?? '', type: ShowFormDataType.文本, validate: true },
                { label: '产品名称', value: row.productName ?? '', type: ShowFormDataType.文本, validate: true },
                { label: '规格型号', value: row.productSpecification ?? '', type: ShowFormDataType.文本, validate: true },
                { label: '技术型号', value: row.technicalCode ?? '', type: ShowFormDataType.文本, validate: true },
            ] as ShowFormData[] : []
        },
        getInboundUnitList(row: ReturnArrivalRecordBatchRow) {
            return (row.unitInfos ?? []).filter((unit: any) => Number(unit.ratio) > 0).map((unit: any) => ({
                ...unit, quantity: Number(row.remainingQuantity) / Number(unit.ratio),
            }))
        },
        inboundConfirm(info: any) {
            if (!this.selectedRow) return
            const total = (info.dataList ?? []).reduce((sum: number, item: any) => {
                const baseUnit = (item.unitQtyList ?? []).find((unit: any) => unit.id === -1)
                return sum + Number(baseUnit?.quantity ?? 0)
            }, 0)
            if (total <= 0 || total > Number(this.selectedRow.remainingQuantity)) return this.$kingzon_messager.showError('入库数量必须大于 0，且不能超过待入库量')
            this.selectedRow.inboundList = cloneDeep(info.dataList ?? [])
            this.selectedRow.inboundQuantityText = `${total}${this.selectedRow.productUnit ?? ''}`
            this.selectedRow.positionInfo = this.selectedRow.inboundList.map((item: any) => item.positionName).filter(Boolean).join('；')
            this.selectedRow.inboundRemark = this.selectedRow.inboundList.map((item: any) => item.remark).filter(Boolean).join('；')
            const index = this.recordList.indexOf(this.selectedRow)
            this.selectedRow = this.recordList[index + 1]
            this.setFormData()
            this.$nextTick(() => (this.$refs.dataGrid as any)?.selectRow?.(this.selectedRow))
        },
        clearInbound() {
            if (!this.selectedRow) return
            this.selectedRow.inboundList = []
            this.selectedRow.inboundQuantityText = ''
            this.selectedRow.positionInfo = ''
            this.selectedRow.inboundRemark = ''
            ;(this.$refs.inbound as any)?.clearData?.()
        },
        async confirm() {
            const unfilled = this.recordList.find(row => !row.inboundList?.length)
            if (unfilled) return this.$kingzon_messager.showError(`到货记录[${unfilled.arrivalRecordNo}]尚未填写入库信息`)
            try {
                this.isLoading = true
                const arrivalRecords = this.recordList.map(row => ({
                    arrivalRecordId: row.id,
                    inventoryRecordList: row.inboundList.map((item: any) => ({ unitQtyList: item.unitQtyList, positionId: item.positionId, remark: item.remark ?? '' })),
                }))
                const res = await this.$http.post('api/inventory/ReturnArrival/PostReturnArrivalBatchWarehousing', { arrivalRecords })
                this.$kingzon_messager.showInfo(res.data || '批量入库成功')
                this.$emit('success')
            } catch (err) {
                this.$kingzon_messager.showError(err)
            } finally {
                this.isLoading = false
            }
        },
    },
})
</script>
