<template>
    <Loading v-show="isLoading" />
    <div class="kingzon-btn-group" style="margin: 5px" v-if="!isCompleted">
        <LinkButton iconCls="icon-add" @click="batchInbound" :disable="selectedRecordList.length == 0">批量入库</LinkButton>
    </div>
    <div class="kingzon-btn-group" style="margin: 5px" v-else>
        <LinkButton iconCls="icon-remove" @click="deleteSelectedInventory" :disable="selectedRecordList.length == 0">批量删除</LinkButton>
    </div>
    <KingZonDataGrid style="height: 100%;width:100%; flex-grow: 1;" ref="dataGrid" class="kingzon-page-fill"
        :data="arrivalRecordList" :showDownLoad="true" exportFileName="销售退货到货记录" emptyMsg="没有符合条件的数据"
        :sorts="[{ field: 'arrivalTime', order: 'desc' }]" @pageChange="getData" @sortChange="getData"
        :columnResizing="true" :isCheckBoxSelection="true" @seletedRowsChanged="onCheckBoxClicked" idField="id"
        :leftGroupRowWidth="530" :cellSpan="isCompleted ? completedCellSpan : undefined">
        <template #left>
            <GridColumn field="index" align="center" width="40">
                <template #body="scope">
                    {{ scope.rowIndex + 1 }}
                </template>
            </GridColumn>
            <GridColumn field="arrivalRecordNo" title="到货记录号" width="110" />
            <GridColumn v-if="isCompleted" field="recordNo" title="入库单号" width="120" />
            <GridColumn field="saleReturnOrderNo" title="退货单号" width="140" />
            <GridColumn field="productNo" title="产品编码" width="100" />
        </template>
        <template #center>
            <GridColumn field="productName" title="产品名称" width="150" :overflowTextEllipsis="true" />
            <GridColumn field="productSpecification" title="规格型号" width="120" :sortable="true"
                :overflowTextEllipsis="true" />
            <GridColumn field="technicalCode" title="技术型号" width="100" :sortable="true" :overflowTextEllipsis="true" />


            <GridColumn field="customer" title="客户" width="150" :overflowTextEllipsis="true">
                <template #body="scope">
                    <span :title="scope.row.customerCode + '-' + scope.row.customerName">
                        {{ scope.row.customerCode + "-" + scope.row.customerName }}
                    </span>
                </template>
            </GridColumn>
            <GridColumn field="customerMaterialNo" title="客户产品图号" width="120" :sortable="true"
                :overflowTextEllipsis="true" />
            <GridColumn field="arrivalTime" title="到货日期" width="110" :sortable="true"><template #body="scope">{{
                $utils.formatDateTime(scope.row.arrivalTime, 'yyyy-MM-dd') }}</template></GridColumn>
            <GridColumn field="baseArrivalQuantity" title="到货数量" align="right" width="120"><template #body="scope">
                    <UnitQuantityShow :quantity="scope.row.baseArrivalQuantity" :unitList="scope.row.unitInfos"
                        :unitId="scope.row.arrivalUnitConversionId" />
                </template>
            </GridColumn>
            <GridColumn v-if="!isCompleted" field="inventoryQuantity" title="已入库数量" align="right" width="120">
                <template #body="scope">
                    <span :class="scope.row.inventoryQuantity ? 'kingzon-detail-text' : ''">
                        <UnitQuantityShow :quantity="scope.row.inventoryQuantity" :unitList="scope.row.unitInfos"
                            :unitId="scope.row.arrivalUnitConversionId" @click="inventoryDetail(scope.row)" />
                    </span>
                </template>
            </GridColumn>
            <GridColumn v-if="!isCompleted" field="remainingQuantity" title="待入库量" align="right" width="120">
                <template #body="scope"><UnitQuantityShow :quantity="getRemainingQuantity(scope.row)" :unitList="scope.row.unitInfos"
                    :unitId="scope.row.arrivalUnitConversionId" /></template>
            </GridColumn>
            <GridColumn v-if="isCompleted" field="quantity" title="本次入库量" align="right" width="120">
                <template #body="scope"><UnitQuantityShow :quantity="scope.row.quantity" :unitList="scope.row.unitInfos" :unitId="-1" /></template>
            </GridColumn>
            <GridColumn v-if="isCompleted" field="positionName" title="库位" width="120" :overflowTextEllipsis="true" />
            <GridColumn field="remark" title="到货备注" width="150" :overflowTextEllipsis="true" />
            <GridColumn field="productCategory" title="产品类别" width="110" :sortable="true"
                :overflowTextEllipsis="true" />
            <GridColumn field="productRemark" title="产品备注" width="150" :overflowTextEllipsis="true" />
        </template>
        <template #right v-if="!isCompleted">
            <GridColumn field="operation" title="操作" align="center" width="60"><template #body="scope">
                    <LinkButton v-if="getRemainingQuantity(scope.row) > 0" @click="warehousing(scope.row)">入库
                    </LinkButton>
                </template>
            </GridColumn>
        </template>
        <template #right v-else>
            <GridColumn field="operation" title="操作" align="center" width="60"><template #body="scope">
                <LinkButton @click="deleteInventory(scope.row)">删除</LinkButton>
            </template></GridColumn>
        </template>
    </KingZonDataGrid>
    <Dialog ref="inboundDialog" :title="selectedArrival.arrivalRecordNo + '-退货入库'" :modal="true" closed
        :draggable="true" :dialogStyle="{ height: '85%', width: '85%' }">
        <Inbound :dataType="getInboundDataType()" :dataId="getInboundDataId()"
            :inboundUnitList="inboundUnitList" :inboundQuantity="getRemainingQuantity(selectedArrival)"
            :showCost="false" :showExtend="false" :showPrice="false" @close="closeInboundDialog"
            @confirm="submitInbound">
            <template #showForm><KingZonShowForm :formData="inboundInfoData" :columnWidth="'25%'" /></template>
        </Inbound>
    </Dialog>
    <Dialog ref="batchInboundDialog" title="销售退货批量入库" :modal="true" closed :draggable="true"
        :dialogStyle="{ height: '85%', width: '85%' }">
        <BatchInbound v-if="showBatchInbound" :arrivalRecordIds="batchArrivalRecordIds"
            @cancel="closeBatchInboundDialog" @success="batchInboundSuccess" />
    </Dialog>
    <Dialog ref="inventoryDetailDialog" :title="selectedArrivalDetail.arrivalRecordNo + '-入库详情'" :modal="true" closed
        :draggable="true" :dialogStyle="{ height: '90%', width: '85%' }">
        <SourceInventory :sourceId="selectedArrivalDetail.id" :sourceType="InventorySourceType.退货入库"
            ref="sourceInventory" :inventoryType="InventoryType.入库" :typeLable="'入库'" :isShowPrice="false"
            :formData="infoData" :rightWidth="60" :isShowPurchaseExt="false">
            <template #showForm>
                <KingZonShowForm :formData="infoData" :columnWidth="'25%'">
                </KingZonShowForm>
            </template>
            <template #right>
                <GridHeaderRow>
                    <GridColumn field="act" title="操作" align="center" width="60">
                        <template #body="scope">
                            <ButtonGroup style="height: 24px">
                                <LinkButton @click="deleteRecord(scope.row)">删除</LinkButton>
                            </ButtonGroup>
                        </template>
                    </GridColumn>
                </GridHeaderRow>
            </template>
        </SourceInventory>
    </Dialog>
</template>

<script lang="ts">
import { defineComponent, type PropType } from 'vue'
import type { Filter } from '@/types/types'
import { InventoryDataType, InventorySourceType, InventoryType, ShowFormDataType } from '@/types/enums'
import type { ShowFormData } from '@/types/types'
import { ReturnArrivalRecordDTO } from '@/types/inventory'
import Inbound from './inbound.vue'
import SourceInventory from '@/views/distribute_material/source-inventory-detail.vue'
import UnitQuantityShow from '../arrival_record_management/unit-quantity-show.vue'
import BatchInbound from './batch-inbound.vue'

export interface ReturnArrivalRecordFilterDTO {
    arrivalRecordFilterList: Filter[]
    saleReturnOrderFilterList: Filter[]
    saleReturnOrderDetailFilterList: Filter[]
    productFilterList: Filter[]
    customerFilterList: Filter[]
}

export default defineComponent({
    name: 'arrivalDetail',
    emits: ['changed'],
    components: { UnitQuantityShow, Inbound, BatchInbound, SourceInventory },
    props: {
        filter: { type: Object as PropType<ReturnArrivalRecordFilterDTO>, required: true },
        isCompleted: { type: Boolean, required: true },
        isActive: { type: Boolean, default: false },
    },
    data() {
        return {
            isLoading: false,
            arrivalRecordList: [] as ReturnArrivalRecordDTO[],
            selectedArrival: new ReturnArrivalRecordDTO(),
            inboundUnitList: [] as any[],
            showInbound: false,
            inboundDialog: {} as EasyUI.Dialog,
            selectedRecordList: [] as ReturnArrivalRecordDTO[],
            showBatchInbound: false,
            batchArrivalRecordIds: [] as number[],
            inventoryDetailDialog: {} as EasyUI.Dialog,
            selectedArrivalDetail: new ReturnArrivalRecordDTO(),
            infoData: [] as ShowFormData[],
            inboundInfoData: [] as ShowFormData[],
        }
    },
    computed: {
        InventoryDataType() {
            return InventoryDataType
        },
        InventorySourceType() {
            return InventorySourceType
        },
        InventoryType() {
            return InventoryType
        },
    },
    mounted() {
        if (this.isActive) this.getData()
        this.inventoryDetailDialog = this.$refs.inventoryDetailDialog as EasyUI.Dialog
    },
    watch: {
        isActive(active: boolean) {
            if (active) this.getData()
        },
    },
    methods: {
        getRemainingQuantity(row: ReturnArrivalRecordDTO): number {
            return Math.max(0, Number(row.baseArrivalQuantity || 0) - Number(row.inventoryQuantity || 0))
        },
        getInboundDataType(): InventoryDataType {
            return (this.selectedArrival as any).dataType ?? InventoryDataType.Product
        },
        getInboundDataId(): number {
            return (this.selectedArrival as any).dataId ?? this.selectedArrival.productId
        },
        warehousing(row: ReturnArrivalRecordDTO) {
            this.selectedArrival = new ReturnArrivalRecordDTO(row)
            const remainingQuantity = this.getRemainingQuantity(row)
            const unitList = row.unitInfos
                .filter(unit => Number(unit.ratio) > 0)
                .map(unit => ({ ...unit, quantity: remainingQuantity / Number(unit.ratio) }))
            this.inboundUnitList = unitList
            this.initInboundFormInfo(this.selectedArrival)
            this.showInbound = true
            this.$nextTick(() => {
                (this.$refs.inboundDialog as EasyUI.Dialog)?.open()
            })
        },
        closeInboundDialog() {
            (this.$refs.inboundDialog as EasyUI.Dialog)?.close()
        },
        submitInbound(info: any) {
            const totalQuantity = (info.dataList ?? []).reduce((total: number, item: any) => {
                const baseUnit = (item.unitQtyList ?? []).find((unit: any) => unit.id === -1)
                return total + Number(baseUnit?.quantity ?? 0)
            }, 0)
            if (totalQuantity <= 0 || totalQuantity > this.getRemainingQuantity(this.selectedArrival)) {
                this.$kingzon_messager.showError('入库数量必须大于 0，且不能大于待入库量')
                return
            }
            this.isLoading = true
            this.$http.post('api/inventory/ReturnArrival/PostReturnArrivalWarehousing', {
                arrivalRecordId: this.selectedArrival.id,
                inventoryRecordList: (info.dataList ?? []).map((item: any) => ({
                    unitQtyList: item.unitQtyList,
                    positionId: item.positionId,
                    remark: item.remark ?? '',
                })),
            })
                .then((res: any) => {
                    this.$kingzon_messager.showInfo(res.data || '入库成功')
                    this.closeInboundDialog()
                    this.getData()
                    this.$emit('changed')
                })
                .catch((err: unknown) => this.$kingzon_messager.showError(err || '退货入库失败'))
                .finally(() => { this.isLoading = false })
        },
        getData() {
            if (!this.isActive) return
            this.isLoading = true
            const url = this.isCompleted
                ? 'api/inventory/ReturnArrival/GetCompletedReturnArrivalInventoryRecords'
                : 'api/inventory/ReturnArrival/GetReturnArrivalRecords'
            const postData = this.isCompleted ? this.filter : { ...this.filter, isCompleted: false }
            this.$http.post(
                url,
                postData,
                this.$refs.dataGrid as any
            )
                .then((res: any) => { this.arrivalRecordList = res.data ?? [] })
                .catch((err: unknown) => this.$kingzon_messager.showError(err || '退货到货记录查询失败'))
                .finally(() => { this.isLoading = false })
        },
        onCheckBoxClicked(list) {
            this.selectedRecordList = list;
        },
        batchInbound() {
            if (this.selectedRecordList.length == 0) {
                this.$kingzon_messager.showError("请选择一条记录")
                return
            }
            this.batchArrivalRecordIds = this.selectedRecordList.map(o => o.id)
            this.showBatchInbound = true
            this.$nextTick(() => {
                (this.$refs.batchInboundDialog as EasyUI.Dialog)?.open()
            })
        },
        closeBatchInboundDialog() {
            this.showBatchInbound = false
                ; (this.$refs.batchInboundDialog as EasyUI.Dialog)?.close()
        },
        batchInboundSuccess() {
            this.closeBatchInboundDialog()
            this.getData()
            this.$emit('changed')
        },
        inventoryDetail(row: ReturnArrivalRecordDTO) {
            if (Number(row.inventoryQuantity || 0) <= 0) {
                this.$kingzon_messager.showInfo("暂无入库记录")
                return
            }
            this.selectedArrivalDetail = new ReturnArrivalRecordDTO(row)
            this.initFormInfo(this.selectedArrivalDetail)
            this.$nextTick(() => {
                (this.$refs.inventoryDetailDialog as EasyUI.Dialog)?.open()
            })
        },
        initFormInfo(row: ReturnArrivalRecordDTO) {
            this.infoData = [
                { label: '退货单号', value: row.saleReturnOrderNo ?? '', type: ShowFormDataType.文本, validate: true },
                { label: '客户', value: `${row.customerCode ?? ''}-${row.customerName ?? ''}`, type: ShowFormDataType.文本, validate: true },
                { label: '客户产品图号', value: row.customerMaterialNo ?? '', type: ShowFormDataType.文本, validate: true },
                { label: '客户产品名称', value: row.customerMaterialName ?? '', type: ShowFormDataType.文本, validate: true },
                { label: '产品编码', value: row.productNo ?? '', type: ShowFormDataType.文本, validate: true },
                { label: '产品名称', value: row.productName ?? '', type: ShowFormDataType.文本, validate: true },
                { label: '规格型号', value: row.productSpecification ?? '', type: ShowFormDataType.文本, validate: true },
                { label: '技术型号', value: row.technicalCode ?? '', type: ShowFormDataType.文本, validate: true },
            ] as ShowFormData[]
            // 将最后一项的宽度设置撑满一行
            var lastOne = this.infoData.at(-1)
            if (lastOne) {
                lastOne.columnWidth = (this.infoData.length % 4) > 0 ? (5 - this.infoData.length % 4) * 25 + "%" : "25%"
            }
        },
        initInboundFormInfo(row: ReturnArrivalRecordDTO) {
            this.inboundInfoData = [
                { label: '退货单号', value: row.saleReturnOrderNo ?? '', type: ShowFormDataType.文本, validate: true },
                { label: '客户', value: `${row.customerCode ?? ''}-${row.customerName ?? ''}`, type: ShowFormDataType.文本, validate: true },
                { label: '客户产品图号', value: row.customerMaterialNo ?? '', type: ShowFormDataType.文本, validate: true },
                { label: '客户产品名称', value: (row as any).customerMaterialName ?? '', type: ShowFormDataType.文本, validate: true },
                { label: '产品编码', value: row.productNo ?? '', type: ShowFormDataType.文本, validate: true },
                { label: '产品名称', value: row.productName ?? '', type: ShowFormDataType.文本, validate: true },
                { label: '规格型号', value: row.productSpecification ?? '', type: ShowFormDataType.文本, validate: true },
                { label: '技术型号', value: row.technicalCode ?? '', type: ShowFormDataType.文本, validate: true },
            ] as ShowFormData[]
        },
        completedCellSpan(data: any) {
            const merged = ['arrivalRecordNo', 'saleReturnOrderNo', 'productNo', 'productName', 'customer', 'customerMaterialNo', 'arrivalTime']
            return merged.includes(data.column.field)
                ? this.$utils.getCellSpan(data.row, data.rowIndex, this.arrivalRecordList, 'arrivalRecordId')
                : { rowspan: 1, colspan: 1 }
        },
        deleteInventory(row: any) {
            this.$kingzon_messager.showConfim('是否确认删除该入库记录？').then(() => this.deleteInventoryByIds([row.id]))
        },
        deleteSelectedInventory() {
            if (!this.selectedRecordList.length) return this.$kingzon_messager.showError('请选择要删除的入库记录')
            this.$kingzon_messager.showConfim(`是否确认删除选中的 ${this.selectedRecordList.length} 条入库记录？`)
                .then(() => this.deleteInventoryByIds(this.selectedRecordList.map((row: any) => row.id)))
        },
        deleteInventoryByIds(ids: number[]) {
            this.isLoading = true
            this.$http.post('api/inventory/ReturnArrival/DeleteReturnArrivalInventoryRecords', { ids })
                .then((res: any) => { this.$kingzon_messager.showInfo(res.data || '删除成功'); this.getData(); this.$emit('changed') })
                .catch((err: unknown) => this.$kingzon_messager.showError(err || '删除失败'))
                .finally(() => { this.isLoading = false })
        },
        deleteRecord(row: any): void {
            this.$kingzon_messager.showConfim("是否确认删除该入库记录？")
                .then(() => {
                    this.isLoading = true
                    return this.$http.get('/api/inventory/ReturnArrival/DeleteReturnArrivalInventoryRecord', { id: row.id })
                        .then((res: any) => {
                            this.$kingzon_messager.showInfo(res.data || '删除成功')
                            // 本地同步到货记录已入库数量并刷新头部信息
                            this.selectedArrivalDetail.inventoryQuantity = Math.max(0,
                                Number(this.selectedArrivalDetail.inventoryQuantity || 0) - Number(row.quantity || 0))
                            this.initFormInfo(this.selectedArrivalDetail)
                            // 刷新入库明细与列表
                            var source = this.$refs.sourceInventory as any
                            if (source) source.getDataAndRefresh()
                            this.getData()
                        })
                        .catch((err: unknown) => {
                            this.$kingzon_messager.showError(err || '删除失败')
                            this.isLoading = false
                        })
                })
        }
    }
})
</script>

<style scoped>
.inbound-summary {
    display: flex;
    gap: 24px;
    padding: 10px 16px;
    flex-wrap: wrap;
}
</style>
