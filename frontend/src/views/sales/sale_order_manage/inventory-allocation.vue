<template>
    <div style="height:100%;width: 550px;display: flex; flex-direction: column;">
        <div v-if="currentDeliveryOrderDetail"
            style="padding : 8px 0 2px;display: flex;flex-direction: row;align-items: start;justify-content: space-between;">
            <div style="display: flex;flex-direction: column; ">
                <div v-if="type != 'delivery'">
                    <span style="display: inline-block; width: 90px;text-align: right;font-weight: bold;">送货量：</span>
                    <NumberBox style="width:160px" v-model="currentDeliveryOrderDetail.inputUnitQuantity" :precision="4"
                        :min="0" :spinners="false"
                        :inputStyle="!isCanCommit ? { backgroundColor: 'var(--color-red-border)' } : {}"
                        @valueChange="onTotalStateChange" @focus="isTotalChange = true">
                    </NumberBox>
                    <ComboBox style="width: 90px;" :data="allUnitInfoList"
                        v-model="currentDeliveryOrderDetail.inputUnitConversionId" :editable="false" valueField="id"
                        textField="unitName" @selectionChange="onTotalStateChange" @focus="isTotalChange = true">
                    </ComboBox>
                </div>
                <div v-if="type == 'delivery'">
                    <span style="display: inline-block; width: 90px;text-align: right;font-weight: bold;">送货量：</span>
                    <span>{{ currentDeliveryOrderDetail.inputUnitQuantity }}</span>
                    <span>{{currentDeliveryOrderDetail.unitConversionList.find(it => it.id ==
                        currentDeliveryOrderDetail?.inputUnitConversionId)?.unitName }}</span>
                </div>
                <div v-if="currentDeliveryOrderDetail.isAllocate && detailOutboundQtyByMain != totalRecordOutboundQtyByMain"
                    style="font-size: 12px;color:red;text-align: center;">送货量与合计占用不等</div>
                <div style="margin : 5px 0;">
                    <span style="display: inline-block; width: 90px;text-align: right;">指定库存：</span>
                    <span>
                        <CheckBox :multiple="false" v-model="currentDeliveryOrderDetail.isAllocate"
                            @click="isTotalChange = true; onTotalStateChange()"></CheckBox>
                    </span>
                </div>
            </div>
            <div style="flex:1;display: flex;flex-direction: row;flex-wrap: wrap;padding-right: 10px;gap: 0 8px;">
                <div v-for="o in unitHelper4OutboundWithAllUnit.getOtherUnitsWithQuantity()" :key="o.unit.id"
                    class="kingzon-text-subinfo" style="text-align: right;">
                    {{ $utils.decimalPlaces4(o.quantity) + ' ' + o.unitName }}
                </div>
            </div>
        </div>
        <Panel title="占用详情" style="flex: 1; ">
            <div v-if="currentDeliveryOrderDetail && currentDeliveryOrderDetail.inventoryRecordList?.length == 0"
                style="text-align: center; height: 135px; padding-top: 45px; ">
                ⚠️暂无可用库存，请等待库存补货
            </div>
            <DataList
                v-else-if="currentDeliveryOrderDetail && currentDeliveryOrderDetail.inventoryRecordList?.length > 0"
                :data="currentDeliveryOrderDetail.inventoryRecordList" ref="dataList">
                <template #default="scope">
                    <div style="border-bottom: 1px solid var(--color-grey-border);" :style="{
                        backgroundColor: scope.row.occupyInputQuantity > 0 && (unitHelper4Inventory(scope.row).getMainUnitWithQuantity()?.quantity ?? 0) > getRowAvailableQuantity(scope.row).toNumber() ? 'var(--color-red-border)'
                            : (unitHelper4Inventory(scope.row).getMainUnitWithQuantity()?.quantity ?? 0) > getRowAvailableQuantity(scope.row).toNumber() ? 'var(--color-grey-border)'
                                : scope.row.occupyInputQuantity > 0 ? 'var(--color-green-border)' : '',
                        borderLeft: scope.row.occupyInputQuantity > 0 ? '10px solid var(--color-green-tag)' : '',
                        padding: scope.row.occupyInputQuantity > 0 ? '2px 0' : '2px 0 2px 10px'
                    }">
                        <!-- <div class="content-item">
                            <div>库房位置：</div>
                            <div style="flex:1">
                                {{ scope.row.areaName + "-" + scope.row.locationName + "-" + scope.row.positionName }}
                            </div>
                        </div> -->
                        <div style="display: flex;flex-direction: row;justify-content: space-between;">
                            <div class="content-item" style="flex:1">
                                <div>库存编码：</div>
                                <div> {{ scope.row.inventoryRecordNo }}</div>
                                <div v-if="scope.row.assignSaleOrderDetailId == scope.row.saleOrderDetailId"
                                    class="kingzon-pointer" style="margin-left: 5px;" title="订单库存">
                                    <font-awesome-icon :icon="['fas', 'dolly-flatbed']"
                                        style="color:var(--color-blue); " />
                                </div>
                            </div>
                            <!-- <div class="content-item" style="flex:1">
                                <div>入库时间：</div>
                                <div> {{ $utils.formatDateTime(scope.row.createTime, 'yyyy-MM-dd HH:mm:ss') }}
                                </div>
                            </div> -->

                        </div>
                        <!-- 批次号 来源单号等 -->
                        <!-- <div style="display: flex;flex-direction: row;">
                            <div class="content-item" style="flex:1">
                                <div>库存来源：</div>
                                <div>{{ InventorySourceType[scope.row.inventorySourceType] }}</div>
                            </div>
                            <div class="content-item" style="flex:1">
                                <div>来源单号：</div>
                                <div> {{ scope.row.inventorySourceNo }}</div>
                            </div>
                        </div> -->
                        <div style="display: flex;flex-direction: row;flex-wrap: wrap;">
                            <div v-for="para in scope.row.inventoryCustomAttributeList" :key="para.systemParaId"
                                class="content-item" style="width: 50%;">
                                <div>{{ para.name }}：</div>
                                <div>{{ para.value }}</div>
                            </div>
                        </div>
                        <div class="content-item">
                            <div>库存余量：</div>
                            <div>
                                {{ scope.row.originRemainingQuantity }}{{
                                    unitHelper4InventoryWithMainQty(scope.row, undefined,
                                        'originRemainingQuantity').getConversionUnitWithQuantity()?.unitName
                                }}
                                {{
                                    unitHelper4InventoryWithMainQty(scope.row, undefined,
                                        'originRemainingQuantity').getOtherUnitsWithQuantity()
                                        .map(o => `${$utils.decimalPlaces4(o.quantity)}${o.unitName}`)
                                        .join(' ')
                                }}
                            </div>
                        </div>

                        <div>
                            <div style="display: flex;flex-direction: row;justify-content: space-between;">
                                <div class="content-item" style="flex:1">
                                    <div>可用库存：</div>
                                    {{ $utils.decimalPlaces4(getRowAvailableQuantity(scope.row)) }}{{
                                        unitHelper4InventoryWithMainQty(scope.row,
                                            getRowAvailableQuantity(scope.row).toNumber())
                                            .getConversionUnitWithQuantity()?.unitName
                                    }}
                                    {{
                                        unitHelper4InventoryWithMainQty(scope.row,
                                            getRowAvailableQuantity(scope.row).toNumber())
                                            .getOtherUnitsWithQuantity()
                                            .map(o => `${$utils.decimalPlaces4(o.quantity)}${o.unitName}`)
                                            .join(' ')
                                    }}
                                </div>
                                <div class="content-item" style="flex:1">
                                    <div>已占用量：</div>
                                    <div>
                                        {{ $utils.decimalPlaces4(getRowOccupiedQuantity(scope.row)) }}{{
                                            unitHelper4InventoryWithMainQty(scope.row,
                                                getRowOccupiedQuantity(scope.row).toNumber())
                                                .getConversionUnitWithQuantity()?.unitName
                                        }}
                                        {{
                                            unitHelper4InventoryWithMainQty(scope.row,
                                                getRowOccupiedQuantity(scope.row).toNumber())
                                                .getOtherUnitsWithQuantity()
                                                .map(o => `${$utils.decimalPlaces4(o.quantity)}${o.unitName}`)
                                                .join(' ')
                                        }}
                                    </div>
                                </div>
                            </div>
                            <div class="content-item">
                                <div>占用量：</div>
                                <div
                                    v-if="(unitHelper4Inventory(scope.row).getMainUnitWithQuantity()?.quantity ?? 0) <= getRowAvailableQuantity(scope.row).toNumber() ||
                                        (getRowAvailableQuantity(scope.row).toNumber() <= 0 && !scope.row.occupyInputQuantity)">
                                    <NumberBox style="width:160px" v-model="scope.row.occupyInputQuantity"
                                        :precision="4" :min="0" :spinners="false"
                                        :disabled="getRowAvailableQuantity(scope.row).toNumber() <= 0 || !currentDeliveryOrderDetail?.isAllocate"
                                        :max="Math.max(0, unitHelper4InventoryWithMainQty(scope.row, getRowAvailableQuantity(scope.row).toNumber())
                                            .convertToUnit(scope.row.occupyInputUnitConversionId)?.quantity ?? 0)"
                                        @valueChange="onRecordStateChange(scope.row)" @focus="isTotalChange = false">
                                        <Addon>
                                            <span v-if="scope.row.occupyInputQuantity" class="textbox-icon icon-clear"
                                                @click="isTotalChange = false; scope.row.occupyInputQuantity = 0"></span>
                                        </Addon>
                                    </NumberBox>
                                    <ComboBox style="width: 90px;"
                                        :disabled="getRowAvailableQuantity(scope.row).toNumber() <= 0 || !currentDeliveryOrderDetail?.isAllocate"
                                        :data="scope.row.inventoryUnitsInfoList"
                                        v-model="scope.row.occupyInputUnitConversionId" :editable="false"
                                        valueField="id" textField="unitName" @focus="isTotalChange = false"
                                        @valueChange="onRecordStateChange(scope.row)">
                                    </ComboBox>
                                </div>
                                <div v-else>
                                    <div>
                                        <NumberBox style="width:160px" v-model="scope.row.occupyInputQuantity"
                                            :precision="4" :min="0" :spinners="false"
                                            :disabled="!currentDeliveryOrderDetail?.isAllocate"
                                            @valueChange="onRecordStateChange(scope.row)"
                                            @focus="isTotalChange = false">
                                            <Addon>
                                                <span v-if="scope.row.occupyInputQuantity"
                                                    class="textbox-icon icon-clear"
                                                    @click="isTotalChange = false; scope.row.occupyInputQuantity = 0"></span>
                                            </Addon>
                                        </NumberBox>
                                        <ComboBox style="width: 90px;"
                                            :disabled="!currentDeliveryOrderDetail?.isAllocate"
                                            :data="scope.row.inventoryUnitsInfoList"
                                            v-model="scope.row.occupyInputUnitConversionId" :editable="false"
                                            valueField="id" textField="unitName" @focus="isTotalChange = false"
                                            @valueChange="onRecordStateChange(scope.row)">
                                        </ComboBox>
                                    </div>
                                    <div style="font-size: 12px;color:var( --color-red);">
                                        可用库存不足
                                    </div>
                                </div>
                                <div
                                    style="width:100%;display: flex;flex-direction: row;flex-wrap: wrap;padding: 2px 5px 0;gap: 0 8px;">
                                    <div v-for="o in unitHelper4InventoryWithAllUnit(scope.row.occupyInputUnitConversionId, scope.row.occupyInputQuantity).getOtherUnitsWithQuantity()"
                                        :key="o.unit.id" class="kingzon-text-subinfo" style="text-align: right;">
                                        {{ $utils.decimalPlaces4(o.quantity) + ' ' + o.unitName }}
                                    </div>
                                </div>
                            </div>
                        </div>
                    </div>
                </template>
            </DataList>
        </Panel>
        <div
            style="text-align: left;padding: 2px 10px 0;display: flex;flex-direction: column;overflow:hidden;white-space:pre">
            <span style="font-weight: bold;display: flex;flex-direction: row;justify-content: space-between;">
                <div style="font-weight: bold;display: flex;flex-direction: row; flex: 1"><span
                        style="width:90px;font-weight: bold;text-align: right;flex-shrink: 0;">总库存余量：</span>{{
                            summaryRecordRemainQty }}</div>
                <div style="font-weight: bold;display: flex;flex-direction: row; flex: 1">
                    <span style="width:90px;font-weight: bold;text-align: right;flex-shrink: 0;">总可用库存：</span>{{
                        summaryRecordAvailableQty }}
                </div>
            </span>
            <span style="font-weight: bold;display: flex;flex-direction: row;white-space: normal">
                <span style="width:90px;font-weight: bold;text-align: right;flex-shrink: 0;">合计占用：</span>
                {{ summaryRecordOutboundQty }}
            </span>
        </div>
        <div class="kingzon-btn-group kingzon-page-footer">
            <LinkButton @click="resetAllocateInventory()">
                重置
            </LinkButton>
            <LinkButton class="c8" :disabled="!isCanCommit" @click="isCanCommit && saveAllocateInventory()">
                确定
            </LinkButton>
        </div>
    </div>
</template>
<script lang="ts">
import { cloneDeep } from 'lodash';
import { defineComponent, } from 'vue';
import { UnitConversionHelper } from '@/utils/UnitConversionHelper';
import { InventorySourceType } from '@/types/enums';
import Decimal from 'decimal.js';
import type { DeliveryOrderDetailListDto, InventoryRecordOccupyDetailDto } from '@/types/sale';
import { InventoryUnitShowDto } from '@/views/sale_order_manage/inventory/inventory';
import { de } from 'date-fns/locale';

export class OccupyInfoDto {
    occupyInputQuantity: number = 0;
    occupyInputUnitConversionId: number = 0;
    inventoryOccupyDetails: InventoryOccupyDto[] = [];
    occupyInputQtyBySalesUnit: number = 0;
    occupyInputQtyByMainUnit: number = 0;
    isAllocate: boolean = false
    unitInfo: InventoryUnitShowDto[] = []
}

export class InventoryOccupyDto {
    inventoryRecordId: number = 0;
    occupyInputQuantity: number = 0;
    occupyInputUnitConversionId: number = 0;
    originOccupyQuantity: number = 0; //总的占用量
    detailOccupyQuantityByMain: number = 0; //本次占用量(主单位)
}

export default defineComponent({
    name: 'DeliveryOutboundAllot',
    emits: ['save', 'reset'], // 保存,重置
    props: {
        deliveryOrderDetail: {
            type: Object as () => DeliveryOrderDetailListDto,
            default: {} as DeliveryOrderDetailListDto,
        },
        type: {
            type: String,
            required: true,
            validator: (value: string) => ['create', 'edit', 'delivery'].includes(value),
        }
    },
    data() {
        return {
            isLoading: false,
            currentDeliveryOrderDetail: undefined as DeliveryOrderDetailListDto | undefined,
            dataList: undefined as EasyUI.DataList | undefined,
            isTotalChange: false,
            isInit: false
        }
    },
    computed: {
        //获取所有的单位
        allUnitInfoList(): InventoryUnitShowDto[] {
            if (!this.currentDeliveryOrderDetail) {
                return []
            }

            //销售单位
            var saleUnitInfo = new InventoryUnitShowDto()
            saleUnitInfo.id = this.currentDeliveryOrderDetail.salesUnitConversionId ?? 0
            saleUnitInfo.unitName = this.currentDeliveryOrderDetail.salesUnit ?? ''
            saleUnitInfo.ratio = this.currentDeliveryOrderDetail.saleToBaseUnitRatio ?? 0
            saleUnitInfo.unitId = this.currentDeliveryOrderDetail.salesUnitId ?? 0
            saleUnitInfo.remainQuantity = this.currentDeliveryOrderDetail.placeOrderQuantity ?? 0

            var saleUnitInfoList = [saleUnitInfo];
            //销售主单位
            if (this.currentDeliveryOrderDetail.salesUnitConversionId != -1) {
                var mainUnitInfo = this.currentDeliveryOrderDetail.unitConversionList?.find(o => o.id == -1)
                if (mainUnitInfo) {
                    saleUnitInfoList.push({
                        id: -1,
                        unitName: mainUnitInfo.unitName,
                        ratio: 1,
                        unitId: mainUnitInfo.unitId,
                        remainQuantity: mainUnitInfo.placeOrderQuantity ?? 0
                    } as InventoryUnitShowDto)
                }
            }
            var saleMainUnit = saleUnitInfoList.find(o => o.id == -1);
            if (!saleMainUnit) {
                return saleUnitInfoList;
            }
            //送货通知上的单位
            var unitInfoList = this.handleUnitList(this.currentDeliveryOrderDetail?.unitInfo ?? [], saleMainUnit)

            //库存记录上的单位
            var inventoryUnitsInfoList = [] as InventoryUnitShowDto[];
            this.currentDeliveryOrderDetail?.inventoryRecordList?.forEach(item => {
                var recordUnitInfoList = this.handleUnitList(item.inventoryUnitsInfoList ?? [], saleMainUnit!)
                inventoryUnitsInfoList = inventoryUnitsInfoList.concat(recordUnitInfoList)
            })

            //合并单位列表
            var allUnitInfoList = [...saleUnitInfoList, ...unitInfoList, ...inventoryUnitsInfoList]

            //去重单位列表
            var unitMap = new Map<number, InventoryUnitShowDto>();
            allUnitInfoList.forEach(item => {
                if (!unitMap.has(item.id)) {
                    unitMap.set(item.id, item)
                }
            })
            return Array.from(unitMap.values());
        },
        unitHelper4Notice() {
            return new UnitConversionHelper(
                this.currentDeliveryOrderDetail?.inputUnitConversionId ?? 0,
                this.currentDeliveryOrderDetail?.inputUnitQuantity ?? 0,
                this.currentDeliveryOrderDetail?.unitInfo ?? []
            )
        },
        unitHelper4OutboundWithAllUnit() {
            return new UnitConversionHelper(
                this.currentDeliveryOrderDetail?.inputUnitConversionId ?? 0,
                this.currentDeliveryOrderDetail?.inputUnitQuantity ?? 0,
                this.allUnitInfoList
            )
        },
        totalRecordOutboundQtyByMain() {
            var totalOutboundQuantity = this.currentDeliveryOrderDetail?.inventoryRecordList?.reduce((sum, o) => {
                return Decimal.add(sum, this.unitHelper4InventoryWithAllUnit(o.occupyInputUnitConversionId, o.occupyInputQuantity ?? 0).getMainUnitWithQuantity()?.quantity ?? 0);
            }, new Decimal(0));
            return totalOutboundQuantity?.toNumber() ?? 0;
        },
        detailOutboundQtyByMain() {
            var result = this.unitHelper4OutboundWithAllUnit.getMainUnitWithQuantity()?.quantity ?? 0
            return result
        },
        isCanCommit() {
            //确保每条库存 记录的占用量不大于可用库存量
            var isAllRecordOutboundQtyValid = this.currentDeliveryOrderDetail?.inventoryRecordList?.every(o => {
                var avaliableQuantity = this.unitHelper4InventoryWithMainQty(o, this.getRowAvailableQuantity(o).toNumber())
                    .convertToUnit(o.occupyInputUnitConversionId ?? 0)?.quantity ?? 0
                return (o.occupyInputQuantity ?? 0) <= Math.max(avaliableQuantity, 0)
            }) ?? true
            return !this.currentDeliveryOrderDetail?.isAllocate ||
                (this.currentDeliveryOrderDetail?.isAllocate
                    && this.detailOutboundQtyByMain == this.totalRecordOutboundQtyByMain
                    && isAllRecordOutboundQtyValid
                )
        },
        InventorySourceType() {
            return InventorySourceType
        },
        summaryRecordOutboundQty() {
            return this.allUnitInfoList.map(e => {
                //计算每个单位的总送货量
                var totalOutboundQuantity = this.currentDeliveryOrderDetail?.inventoryRecordList?.reduce((sum, o) => {
                    var helper = this.unitHelper4InventoryWithAllUnit(o.occupyInputUnitConversionId ?? 0, o.occupyInputQuantity ?? 0)
                    return Decimal.add(sum, helper.convertToUnit(e.id)?.quantity ?? 0);
                }, new Decimal(0));
                return `${this.$utils.decimalPlaces4(totalOutboundQuantity)}${e.unitName}`
            }).join(' ')
        },
        summaryRecordRemainQty() {
            return this.allUnitInfoList.map(e => {
                var totalRemainQuantity = this.currentDeliveryOrderDetail?.inventoryRecordList?.reduce((sum, o) => {
                    var helper = this.unitHelper4InventoryWithAllUnit(-1, o.originRemainingQuantity ?? 0)
                    return Decimal.add(sum, helper.convertToUnit(e.id)?.quantity ?? 0);
                }, new Decimal(0));
                return `${this.$utils.decimalPlaces4(totalRemainQuantity)}${e.unitName}`
            }).join('\n')
        },
        summaryRecordAvailableQty() {
            return this.allUnitInfoList.map(e => {
                var totalAvailableQuantity = this.currentDeliveryOrderDetail?.inventoryRecordList?.reduce((sum, o) => {
                    var helper = this.unitHelper4InventoryWithAllUnit(-1, this.getRowAvailableQuantity(o).toNumber() ?? 0)
                    return Decimal.add(sum, helper.convertToUnit(e.id)?.quantity ?? 0);
                }, new Decimal(0));
                return `${this.$utils.decimalPlaces4(totalAvailableQuantity)}${e.unitName}`
            }).join('\n')
        }
    },
    watch: {
        deliveryOrderDetail: {
            handler(newVal) {
                if (!newVal) {
                    return;
                }
                this.isTotalChange = false
                this.currentDeliveryOrderDetail = cloneDeep(newVal)
                this._initData()
            },
            deep: true,
        }
    },
    created() {
    },
    mounted() {
        this.dataList = (this.$refs.dataList as any) as EasyUI.DataList
    },
    methods: {
        _initData() {
            //这里必须先处理currentDeliveryOrderDetail，然后再做其它操作
            this.isInit = true;
            this._dealCurrentDeliveryOrderDetail()
            setTimeout(() => {
                this.isInit = false;
            }, 0)
        },
        //处理currentDeliveryOrderDetail
        _dealCurrentDeliveryOrderDetail() {
            this.isTotalChange = false;
        },
        unitHelper4Inventory(row: InventoryRecordOccupyDetailDto) {
            var helper = new UnitConversionHelper(
                row.occupyInputUnitConversionId ?? 0,
                row.occupyInputQuantity ?? 0,
                row.inventoryUnitsInfoList
            );
            return helper
        },
        unitHelper4InventoryWithMainQty(row, quantity?: number, key = "originRemainingQuantity") {
            var helper = new UnitConversionHelper(
                -1,
                quantity ?? row[key] ?? 0,
                row.inventoryUnitsInfoList
            );
            return helper
        },
        unitHelper4InventoryWithAllUnit(unitConversionId = -1, quantity: number) {
            var helper = new UnitConversionHelper(
                unitConversionId,
                quantity,
                this.allUnitInfoList
            );
            return helper
        },
        unitHelper4OutboundWithMainQty(quantity: number) {
            var helper = new UnitConversionHelper(
                -1,
                quantity,
                this.allUnitInfoList
            );
            return helper
        },
        getRowAvailableQuantity(row) {
            return Decimal.sub(row.originRemainingQuantity, this.getRowOccupiedQuantity(row))
        },
        getRowOccupiedQuantity(row) {
            return Decimal.sub(row.originOccupyQuantity, row.deliveryDetailOccupyQuantity)
        },
        onTotalStateChange() {
            if (!this.isTotalChange) {
                return
            }
            var totalQuantity = this.currentDeliveryOrderDetail?.inputUnitQuantity ?? 0
            var unitConversionId = this.currentDeliveryOrderDetail?.inputUnitConversionId ?? 0
            var inventoryRecordList = this.currentDeliveryOrderDetail?.inventoryRecordList ?? []

            for (var record of inventoryRecordList) {
                var unit = record.inventoryUnitsInfoList.find(o => o.id == unitConversionId)
                if (!unit || !this.currentDeliveryOrderDetail?.isAllocate) {
                    record.occupyInputUnitConversionId = unit?.id ?? undefined
                    record.occupyInputQuantity = undefined
                    continue
                }

                //第一次进来需要指定默认单位
                if (this.isInit) {
                    if (record.occupyInputUnitConversionId == undefined) {
                        record.occupyInputUnitConversionId = unit?.id ?? undefined
                    }
                    continue
                }

                record.occupyInputUnitConversionId = unitConversionId
                //计算可用库存量
                var canUsedQuantity = this.getRowAvailableQuantity(record).toNumber()
                var convertUnit = this.unitHelper4InventoryWithMainQty(record, canUsedQuantity).convertToUnit(unitConversionId)
                canUsedQuantity = convertUnit?.quantity ?? 0

                if (canUsedQuantity <= 0 || totalQuantity <= 0) {
                    record.occupyInputQuantity = undefined
                    continue
                }

                if (totalQuantity > canUsedQuantity) {
                    record.occupyInputQuantity = canUsedQuantity
                    totalQuantity = Decimal.sub(totalQuantity, canUsedQuantity).toNumber()
                } else {
                    record.occupyInputQuantity = totalQuantity
                    totalQuantity = 0
                }
            }
        },
        //父级修改下单/送货量后，按新送货量重新分配占用
        reAllocateByDeliveryQuantity() {
            if (!this.currentDeliveryOrderDetail?.isAllocate || this.isInit) {
                return;
            }
            this.isTotalChange = true;
            this.onTotalStateChange();
            this.isTotalChange = false;
        },
        async onRecordStateChange(row: InventoryRecordOccupyDetailDto) {
            if (this.isInit) {
                return
            }
            var maxQuantity = this.unitHelper4InventoryWithMainQty(row, this.getRowAvailableQuantity(row).toNumber()).convertToUnit(row.occupyInputUnitConversionId ?? 0)?.quantity ?? 0
            if (row.occupyInputQuantity && row.occupyInputQuantity > maxQuantity) {
                await this.$nextTick();
                row.occupyInputQuantity = Math.max(0, maxQuantity)
            }
            if (this.isTotalChange) {
                return
            }
            var occupyQuantity = this.unitHelper4OutboundWithMainQty(this.totalRecordOutboundQtyByMain).convertToUnit(this.currentDeliveryOrderDetail?.inputUnitConversionId ?? 0)?.quantity
            this.currentDeliveryOrderDetail && (this.currentDeliveryOrderDetail.inputUnitQuantity = occupyQuantity ?? 0)
        },
        //保存分配
        saveAllocateInventory() {
            var usedConcersionMap = new Map<number, InventoryRecordOccupyDetailDto>(this.currentDeliveryOrderDetail?.inventoryRecordList?.filter(o => o.occupyInputQuantity && o.occupyInputQuantity > 0)
                .map(o => [o.occupyInputUnitConversionId ?? 0, o]));
            var helper = this.unitHelper4OutboundWithAllUnit;
            var unitInfo = helper.getAllUnitWithQuantity().map(x => {
                x.unit.quantity = x.quantity
                return x.unit
            }).filter(o => usedConcersionMap.has(o.id) || o.id == this.currentDeliveryOrderDetail?.inputUnitConversionId || o.id == -1);
            var result = {
                occupyInputQuantity: this.currentDeliveryOrderDetail?.inputUnitQuantity,
                occupyInputUnitConversionId: this.currentDeliveryOrderDetail?.inputUnitConversionId,
                occupyInputQtyBySalesUnit: helper.convertToUnit(this.currentDeliveryOrderDetail?.salesUnitConversionId ?? 0)?.quantity ?? 0,
                occupyInputQtyByMainUnit: helper.getMainUnitWithQuantity()?.quantity ?? 0,
                unitInfo: unitInfo,
                inventoryOccupyDetails: this.currentDeliveryOrderDetail?.inventoryRecordList
                    .map(item => {
                        var outboundMainUnit = this.unitHelper4Inventory(item).getMainUnitWithQuantity()
                        return {
                            inventoryRecordId: item.inventoryRecordId,
                            occupyInputQuantity: item.occupyInputQuantity, //选中单位
                            occupyInputUnitConversionId: item.occupyInputUnitConversionId,
                            originOccupyQuantity: Decimal.add(this.getRowOccupiedQuantity(item), outboundMainUnit?.quantity ?? 0).toNumber(),
                            detailOccupyQuantityByMain: outboundMainUnit?.quantity ?? 0, //主单位
                        }
                    }),
                isAllocate: this.currentDeliveryOrderDetail?.isAllocate
            } as OccupyInfoDto
            this.$emit('save', result)
        },
        //重置
        resetAllocateInventory() {
            this.$emit('reset')
        },
        handleUnitList(unitInfoList: InventoryUnitShowDto[], saleMainUnit: InventoryUnitShowDto) {
            if (unitInfoList.length > 0) {
                var deliveryMainUnit = unitInfoList.find(o => o.id == -1);
                if (deliveryMainUnit) {
                    var deliveryOtherUnit = unitInfoList.filter(o => o.id != -1);
                    deliveryOtherUnit.forEach(o => {
                        var scale = o.remainQuantity != 0 ? (deliveryMainUnit?.remainQuantity ?? 0) / o.remainQuantity : 0
                        o.remainQuantity = scale != 0 ? (saleMainUnit.remainQuantity ?? 0) / scale : 0
                    })
                    deliveryMainUnit.remainQuantity = saleMainUnit.remainQuantity
                    return unitInfoList
                }
            }
            return []
        }
    }
})
</script>
<style scoped>
.content-item {
    display: flex;
    word-break: keep-all;
}

.content-item> :first-child {
    text-align: right;
    width: 90px;
    flex-shrink: 0;
}
</style>
