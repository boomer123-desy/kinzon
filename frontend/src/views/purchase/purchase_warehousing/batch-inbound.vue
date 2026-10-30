<template>
    <Loading v-show="isLoading"></Loading>
    <div class="kingzon-page-content">
        <KingZonDataGrid class="kingzon-page-fill" :data="recordList" :pagination="false" style="margin-top: 20px;"
            :dblclickToEdit="true" editMode="cell">
            <GridColumnGroup :frozen="true" align="left" width="460">
                <GridHeaderRow>
                    <GridColumn align="center" field="rowIndex" width="40">
                        <template #body="scope">
                            {{ scope.rowIndex + 1 }}
                        </template>
                    </GridColumn>
                    <GridColumn field="arrivalRecordNo" title="到货编号" align="left" :sortable="true" width="120">
                        <template #body="scope">
                            <KingZonSymbol :rowText="scope.row.arrivalRecordNo" :symbolText="'让'"
                                :color="'var(--color-yellow)'" :showSymbol="scope.row.concessionQuantity > 0">
                            </KingZonSymbol>
                        </template>
                    </GridColumn>
                    <GridColumn field="productNo" title="物料编码" width="100" align="left" :sortable="true"
                        :overflowTextEllipsis="true">
                    </GridColumn>
                    <GridColumn field="productName" title="物料名称" width="100" align="left" :overflowTextEllipsis="true">
                    </GridColumn>
                    <GridColumn field="productSpecification" align="left" width="100" title="规格型号"
                        :overflowTextEllipsis="true">
                    </GridColumn>
                </GridHeaderRow>
            </GridColumnGroup>
            <GridColumnGroup>
                <GridHeaderRow>
                    <GridColumn field="supplierName" title="供应商" width="100" align="left" :sortable="true"
                        :overflowTextEllipsis="true">
                    </GridColumn>
                    <GridColumn field="arrivalQuantity" title="到货量" width="100" align="right" :sortable="true"
                        :overflowTextEllipsis="true">
                        <template #body="scope">
                            <UnitQuantityShow :quantity="scope.row.arrivalQuantity" :unitList="scope.row.unitInfoList"
                                :isChange="false">
                            </UnitQuantityShow>
                        </template>
                    </GridColumn>
                    <GridColumn field="arrivalTime" title="到货时间" width="100" align="left" :sortable="true"
                        :overflowTextEllipsis="true">
                    </GridColumn>
                    <GridColumn v-for="(para, index) in additionalParam" :key="index"
                        :field="para.systemPara.id.toString()" :title="para.systemPara.dataValue" align="left"
                        width="100">
                    </GridColumn>
                </GridHeaderRow>
            </GridColumnGroup>
            <GridColumnGroup :frozen="true" align="right" width="420">
                <GridHeaderRow>
                    <GridColumn field="receivableQuantity" title="入库量" align="right" width="120" :sortable="true">
                        <template #body="scope">
                            <UnitQuantityShow :quantity="scope.row.receivableQuantity"
                                :unitList="scope.row.unitInfoList">
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
import { DataType, FilterType, InventoryDataStatus, InventoryDataType, InventorySourceType, SystemParaType } from '@/types/enums';
import { ArrivalInspectionRecordDTO, InventoryUnitShowDto, StorageLocation_PositionDTO } from '@/types/inventory';
import { CustomAttribute, Filter } from '@/types/types';
import { defineComponent, type PropType } from 'vue';
import UnitQuantityShow from '@/views/arrival_record_management/unit-quantity-show.vue';
import type { SystemParaTreeDto } from '@/types/common';
import SelectPosition from '@/views/inventory_management/select-position.vue'
import { UnitConversionHelper } from '@/utils/UnitConversionHelper';
class ArrivalInspectionRecordDTOHasPositionName extends ArrivalInspectionRecordDTO {
    positionName: string | undefined = undefined
    supplierId: number | undefined = undefined
    arrivalUnitConversionId: number | undefined = undefined
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
            recordList: [] as ArrivalInspectionRecordDTOHasPositionName[],
            additionalParam: [] as SystemParaTreeDto[],
            selectPositionDialog: {} as EasyUI.Dialog,
            selectedRow: {} as ArrivalInspectionRecordDTOHasPositionName
        }
    },
    methods: {
        async init() {
            try {
                this.isLoading = true;
                await this.getAdditionalParam();
                await this.getData()
                await this.getDeafaultPosition()
            } catch (error) {
                this.$kingzon_messager.showError(error)
            } finally {
                this.isLoading = false
            }
        },
        async getAdditionalParam() {
            var types = [SystemParaType.外购物料信息] as SystemParaType[];
            var result = await this.$http.post('/api/base/SystemPara/GetAdditionalParamByTypes', types)
            this.additionalParam = result.data as SystemParaTreeDto[];
        },
        async getData() {
            if (!this.arrivalRecordIds?.length) {
                this.$kingzon_messager.showError("未选择任何到货记录")
                return
            }
            const filterList = [new Filter('arrivalRecordId', this.arrivalRecordIds, FilterType.IntArrayContains, DataType.multiSelectNum)]
            const postData = {
                filterList: filterList,
                isPaged: false
            }

            const result = await this.$http.post("/api/inventory/PurchasingWarehousing/GetArrivalInspectionInventoryRecords", postData)
            this.recordList = result.data as ArrivalInspectionRecordDTOHasPositionName[]

            console.log("获取到的数据", this.recordList)
            this.recordList.forEach((item) => {
                item.dataType = !item.specificationId
                    ? InventoryDataType.Product
                    : InventoryDataType.Specification;
                item.dataId = !item.specificationId
                    ? item.productId
                    : item.specificationId;

                this.additionalParam.forEach((para: SystemParaTreeDto) => {
                    item[para.systemPara.id.toString()] = item.customAttributeList.find((p: CustomAttribute) => p.systemParaId == para.systemPara.id)?.value;
                });
            });
        },

        async getDeafaultPosition() {
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
                        // supplierId: this.supplierId,
                        dataStatus: item.dataStatus ?? InventoryDataStatus.None,
                        inboundQuantity: item.arrivalQuantity ?? 0
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

        clickPositon(row: ArrivalInspectionRecordDTOHasPositionName) {
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
                    this.$kingzon_messager.showError(`物料[${missPosition.productNo}]未选择库位`)
                    return;
                }
                var error = [] as string[]
                for (var item of this.recordList) {
                    try {
                        var postData = this._createPostData(item)
                        await this.$http.post('/api/inventory/PurchasingWarehousing/PostPurchaseWarehousingInventoryRecord', postData)
                    } catch (e) {
                        error.push(`${item.arrivalRecordNo},入库失败,失败原因：${e}`)
                    }
                }
                if (error.length > 0) {
                    this.$kingzon_messager.showError(error.join('\r\n'))
                }

                this.$emit('success')
            } catch (e) {
                this.$kingzon_messager.showError(e)
            } finally {
                this.isLoading = false
            }
        },
        _createPostData(item: ArrivalInspectionRecordDTOHasPositionName) {
            var postData = {
                arrivalRecordId: item.arrivalRecordId,
                totalQuantity: item.receivableQuantity,
                inventoryRecordList: [{
                    remark: item.remark ?? '',
                    positionId: item.positionId,
                    customAttributeList: item.customAttributeList,
                    unitQtyList: (item.unitInfoList || []).map((o: any) => {
                        var helper = new UnitConversionHelper(-1, item.receivableQuantity, item.unitInfoList as InventoryUnitShowDto[])
                        var data = {
                            id: o.id,
                            unitId: o.unitId,
                            unitName: o.unitName,
                            ratio: o.ratio,
                            remark: o.remark ?? null,
                            conversionType: o.conversionType ?? null,
                            quantity: helper.convertToUnit(o.id)?.quantity ?? 0,
                            price: o.price ?? null,
                            materialCost: o.materialCost ?? null,
                            processingCost: o.processingCost ?? null
                        }
                        return data
                    })
                }]
            }
            return postData;
        }
    }
})
</script>