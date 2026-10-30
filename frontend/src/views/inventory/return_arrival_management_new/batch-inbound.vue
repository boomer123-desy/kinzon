<template>
    <Loading v-show="isLoading"></Loading>
    <div class="kingzon-page-content">
        <KingZonDataGrid class="kingzon-page-fill" :data="recordList" :pagination="false" style="margin-top: 20px;"
            :dblclickToEdit="true" editMode="cell">
            <GridColumnGroup :frozen="true" align="left" width="520">
                <GridHeaderRow>
                    <GridColumn align="center" field="rowIndex" width="40">
                        <template #body="scope">
                            {{ scope.rowIndex + 1 }}
                        </template>
                    </GridColumn>
                    <GridColumn field="arrivalRecordNo" title="到货编号" align="left" :sortable="true" width="130">
                    </GridColumn>
                    <GridColumn field="saleReturnOrderNo" title="退货单号" align="left" :sortable="true" width="145">
                    </GridColumn>
                    <GridColumn field="productNo" title="产品编码" width="110" align="left" :sortable="true"
                        :overflowTextEllipsis="true">
                    </GridColumn>
                    <GridColumn field="productName" title="产品名称" width="120" align="left" :overflowTextEllipsis="true">
                    </GridColumn>
                </GridHeaderRow>
            </GridColumnGroup>
            <GridColumnGroup>
                <GridHeaderRow>
                    <GridColumn field="customerName" title="客户名称" width="140" align="left" :overflowTextEllipsis="true">
                    </GridColumn>
                    <GridColumn field="baseArrivalQuantity" title="到货量" width="120" align="right" :sortable="true">
                        <template #body="scope">
                            <UnitQuantityShow :quantity="scope.row.baseArrivalQuantity" :unitList="scope.row.unitInfos"
                                :unitId="scope.row.arrivalUnitConversionId" :isChange="false">
                            </UnitQuantityShow>
                        </template>
                    </GridColumn>
                    <GridColumn field="arrivalTime" title="到货时间" width="110" align="left" :sortable="true">
                        <template #body="scope">
                            {{ $utils.formatDateTime(scope.row.arrivalTime, 'yyyy-MM-dd') }}
                        </template>
                    </GridColumn>
                </GridHeaderRow>
            </GridColumnGroup>
            <GridColumnGroup :frozen="true" align="right" width="440">
                <GridHeaderRow>
                    <GridColumn field="remainingQuantity" title="待入库量" align="right" width="140" :sortable="true">
                        <template #body="scope">
                            <UnitQuantityShow :quantity="scope.row.remainingQuantity" :unitList="scope.row.unitInfos"
                                :unitId="scope.row.arrivalUnitConversionId">
                            </UnitQuantityShow>
                        </template>
                    </GridColumn>
                    <GridColumn field="positionName" title="库位" align="right" width="180" :sortable="true">
                        <template #body="scope">
                            <div style="display: flex;align-items: center;">
                                <TextBox v-model="scope.row.positionName" class="box" :disabled="true"></TextBox>
                                <font-awesome-icon :icon="['fas', 'warehouse']"
                                    style="margin:0 5px;color:var(--color-blue)" class="kingzon-pointer"
                                    @click="clickPositon(scope.row)" />
                            </div>
                        </template>
                    </GridColumn>
                    <GridColumn field="remark" title="入库备注" align="right" width="120" :sortable="true" :editable="true"
                        cellCss="kingzon-cell-editable">
                        <template #edit="scope">
                            <TextBox v-model="scope.row.remark"></TextBox>
                        </template>
                    </GridColumn>
                </GridHeaderRow>
            </GridColumnGroup>
        </KingZonDataGrid>
        <div class="kingzon-btn-group kingzon-page-footer">
            <LinkButton @click="close">取消</LinkButton>
            <LinkButton btnCls="c8" @click="confirm">确定</LinkButton>
        </div>
    </div>
    <Dialog ref="SelectPosition" title="选择库位" :modal="true" closed :draggable="true"
        :dialogStyle="{ height: '85%', width: '85%' }">
        <SelectPosition :dataType="selectedRow.dataType" :dataId="selectedRow.dataId"
            :dataStatus="selectedRow.dataStatus" :positionId="selectedRow.positionId"
            @close="selectPositionDialog.close()" @confirm="selectPositionConfirm">
        </SelectPosition>
    </Dialog>
</template>

<script lang="ts">
import { InventoryDataStatus, InventoryDataType } from '@/types/enums';
import { StorageLocation_PositionDTO } from '@/types/inventory';
import { defineComponent, type PropType } from 'vue';
import UnitQuantityShow from '@/views/arrival_record_management/unit-quantity-show.vue';
import SelectPosition from '@/views/inventory_management/select-position.vue'
import { ReturnArrivalRecordDTO } from '../../../types/fragments/inventory';

class ReturnArrivalRecordBatchRow extends ReturnArrivalRecordDTO {
    remainingQuantity: number = 0
    dataType: InventoryDataType = InventoryDataType.Product
    dataId: number = 0
    dataStatus: InventoryDataStatus = InventoryDataStatus.None
    positionId: number = 0
    positionName: string = ''

    constructor(init?: Partial<ReturnArrivalRecordBatchRow>) {
        super(init)
    }
}

export default defineComponent({
    emits: ['cancel', 'success'],
    props: {
        arrivalRecordIds: {
            type: Array as PropType<number[]>,
            default: () => []
        },
    },
    components: {
        UnitQuantityShow, SelectPosition
    },
    mounted() {
        this.selectPositionDialog = this.$refs.SelectPosition as EasyUI.Dialog;
        this.init()
    },
    data() {
        return {
            isLoading: false,
            recordList: [] as ReturnArrivalRecordBatchRow[],
            selectPositionDialog: {} as EasyUI.Dialog,
            selectedRow: {} as ReturnArrivalRecordBatchRow
        }
    },
    methods: {
        async init() {
            try {
                this.isLoading = true;
                await this.getData()
                await this.getDefaultPosition()
            } catch (error) {
                this.$kingzon_messager.showError(error)
            } finally {
                this.isLoading = false
            }
        },
        async getData() {
            if (!this.arrivalRecordIds?.length) {
                this.$kingzon_messager.showError("未选择任何到货记录")
                return
            }
            const result = await this.$http.post("/api/inventory/ReturnArrival/GetReturnArrivalInventoryRecords", {
                arrivalRecordIds: this.arrivalRecordIds
            })
            this.recordList = (result.data ?? []) as ReturnArrivalRecordBatchRow[]
            this.recordList.forEach((item) => {
                item.dataType = item.dataType ?? InventoryDataType.Product;
                item.dataId = item.dataId ?? item.productId;
                item.dataStatus = InventoryDataStatus.None;
            })
        },

        async getDefaultPosition() {
            if (!this.recordList) {
                return;
            }
            for (var item of this.recordList) {
                if (item.positionId) {
                    continue;
                }
                try {
                    const res = await this.$http.post('/api/inventory/InventoryManagement/GetInboundDefPosition', {
                        dataType: item.dataType,
                        dataId: item.dataId,
                        dataStatus: item.dataStatus ?? InventoryDataStatus.None,
                        inboundQuantity: item.remainingQuantity ?? 0
                    })
                    var data = res.data as any
                    if (data && data.positionId && data.positionId !== 0) {
                        item.positionName = data.positionInfo
                        item.positionId = data.positionId
                    }
                } catch (error) {
                    this.$kingzon_messager.showError(error)
                }
            }
        },

        clickPositon(row: ReturnArrivalRecordBatchRow) {
            this.selectedRow = row;
            this.selectPositionDialog.open();
        },
        selectPositionConfirm(position: StorageLocation_PositionDTO) {
            this.selectedRow.positionId = position.id;
            this.selectedRow.positionName = position.fullName;
            this.selectPositionDialog.close();
        },
        close() {
            this.$emit('cancel')
        },
        async confirm() {
            try {
                this.isLoading = true;
                const missPosition = this.recordList.find(o => !o.positionId)
                if (missPosition) {
                    this.$kingzon_messager.showError(`到货记录[${missPosition.arrivalRecordNo}]未选择库位`)
                    return;
                }
                const postData = {
                    arrivalRecords: this.recordList.map(item => this._createPostData(item))
                }
                await this.$http.post('/api/inventory/ReturnArrival/PostReturnArrivalBatchWarehousing', postData)
                this.$kingzon_messager.showInfo('批量入库成功')
                this.$emit('success')
            } catch (e) {
                this.$kingzon_messager.showError(e)
            } finally {
                this.isLoading = false
            }
        },
        _createPostData(item: ReturnArrivalRecordBatchRow) {
            const remainingQuantity = Number(item.remainingQuantity ?? 0)
            const mainUnit = {
                id: -1,
                unitId: item.productUnitId ?? 0,
                unitName: item.productUnit ?? '',
                ratio: 1,
                conversionType: null,
                quantity: remainingQuantity
            }
            const otherUnits = (item.unitInfos || [])
                .filter((o: any) => Number(o.ratio) > 0)
                .map((o: any) => ({
                    id: o.id,
                    unitId: o.unitId,
                    unitName: o.unitName,
                    ratio: o.ratio,
                    conversionType: o.conversionType ?? null,
                    quantity: remainingQuantity / Number(o.ratio)
                }))
            return {
                arrivalRecordId: item.id,
                inventoryRecordList: [{
                    unitQtyList: [mainUnit, ...otherUnits],
                    positionId: item.positionId,
                    remark: item.remark ?? '',
                }]
            }
        }
    }
})
</script>
