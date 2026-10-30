<template>
    <Loading v-show="isLoading"></Loading>
    <div class="kingzon-page-content">
        <div class="kingzon-btn-group" style="margin: 4px;">
            <template v-if="operate == OperateStatus.库存">
                <span v-if="areaInfo && areaInfo.lastCheckTime" style="margin-right: 10px;">最近盘库时间{{
                    $utils.formatDateTime(areaInfo.lastCheckTime,
                        "yyyy-MM-dd HH:mm:ss") }}</span>
                <LinkButton class="c8" @click="inventoryCheck()">生成盘库记录</LinkButton>
                <LinkButton class="c8" @click="addNewStock()">新物资入库</LinkButton>
            </template>
            <ButtonGroup style="flex: 1; display: flex; justify-content: center; align-items: center;">
                <LinkButton :class="{ c8: operate == OperateStatus.库存 }"
                    @click="operateStatusChange(OperateStatus.库存)"> 库存物资</LinkButton>
                <LinkButton :class="{ c8: operate == OperateStatus.库存记录 }"
                    @click="operateStatusChange(OperateStatus.库存记录)"> 库存记录</LinkButton>
            </ButtonGroup>
        </div>
        <KingZonDataGrid :id="'grid' + areaId" v-if="areaInfo && operate == OperateStatus.库存" :data="recordList"
            class="kingzon-page-fill" ref="dataGrid" :showDownLoad="true" :exportFileName="areaInfo.name + '-库存明细'"
            @pageChange="refreshTable(showZeroStock)" @sortChange="refreshTable(showZeroStock)" :cellSpan="getCellSpan"
            :footerData="footerData" :showFooter="true" style="overflow-y: unset;">
            <GridColumnGroup align="left" :frozen="true" width="540" :columnMoving="true">
                <GridHeaderRow>
                    <GridColumn align="center" cellCss="datagrid-td-rownumber" width="40" field="index">
                        <template #body="scope">
                            {{ scope.rowIndex + 1 }}
                        </template>
                    </GridColumn>
                    <GridColumn field="lastCheckTime" title="最近盘点时间" width="150" align="left" :sortable="true">
                        <template #body="scope">
                            {{ scope.row.lastCheckTime ? $utils.formatDateTime(scope.row.lastCheckTime,
                                "yyyy-MM-dd HH:mm:ss") : "/" }}
                        </template>
                    </GridColumn>
                    <GridColumn field="materialNo" title="物料编码" width="100" align="left" :overflowTextEllipsis="true">
                    </GridColumn>
                    <GridColumn field="materialName" title="物料名称" width="150" align="left" :overflowTextEllipsis="true">
                    </GridColumn>
                    <GridColumn field="materialTechnicalCode" title="技术型号" width="100" align="left"
                        :overflowTextEllipsis="true">
                    </GridColumn>
                </GridHeaderRow>
            </GridColumnGroup>
            <GridColumnGroup :columnMoving="true" align="center">
                <GridHeaderRow>
                    <GridColumn field="materialCategory" title="物料类别" width="100" align="left" :rowspan="2"
                        :overflowTextEllipsis="true">
                    </GridColumn>
                    <GridColumn field="dataStatus" title="状态" width="85" align="left" :overflowTextEllipsis="true"
                        :rowspan="2">
                        <template #body="scope">
                            {{ scope.row.dataStatus ? InventoryDataStatus[scope.row.dataStatus] : "" }}
                        </template>
                    </GridColumn>
                    <GridColumn field="materialSpecification" title="规格型号" width="150" align="left"
                        :overflowTextEllipsis="true" :rowspan="2">
                    </GridColumn>
                    <GridColumn field="materialGrade" title="材质牌号" width="120" align="left" :overflowTextEllipsis="true"
                        :rowspan="2">
                    </GridColumn>
                    <GridColumn v-if="areaInfo.showCustomer" field="materialNoCustomerName" title="客户" width="120"
                        align="left" :overflowTextEllipsis="true" :rowspan="2">
                    </GridColumn>
                    <GridColumn field="safetyStock" title="安全库存" width="120" align="right" :rowspan="2">
                    </GridColumn>
                    <GridColumn field="supplierName" title="供应商" width="120" align="left" :overflowTextEllipsis="true"
                        :rowspan="2">
                    </GridColumn>
                    <GridColumn field="totalStock" title="总库存" width="100" align="right" :rowspan="2">
                        <template #body="scope">
                            {{ $utils.formatThousandSeparator(scope.row.totalStock) }}
                        </template>
                    </GridColumn>
                    <GridColumn field="materialUnit" title="单位" width="60" align="left" :overflowTextEllipsis="true"
                        :rowspan="2">
                    </GridColumn>
                    <!-- 异常/正常  本库库存 -->
                    <GridColumn field="stockQuantity" title="本库库存" width="130" align="right" :sortable="true"
                        :rowspan="2">
                        <template #body="scope">
                            <div style="display: flex;flex-direction: column;" :title="summaryText(scope.row)">
                                <div style="z-index: 999;">
                                    <span class="kingzon-detail-text" @click="showDetailList(scope.row)"
                                        :style="{ background: scope.row.areaAbnormalStock ? 'white' : 'transparent', padding: scope.row.areaAbnormalStock ? '0 0 0 2px' : '0' }">
                                        {{ $utils.formatThousandSeparator(scope.row.areaStock) }}
                                    </span>
                                </div>
                                <span v-if="scope.row.areaAbnormalStock" style=" margin-top:-18px;display: flex;flex-direction: column;font-size:
                                    11px;text-align: left;">
                                    <span style="color: var(--color-red);">
                                        异{{ $utils.formatThousandSeparator(scope.row.areaAbnormalStock) }}
                                    </span>
                                    <span style="color: var(--color-green);">
                                        正{{ $utils.formatThousandSeparator(scope.row.areaNormalStock) }}
                                    </span>
                                </span>
                            </div>
                        </template>
                        <template #footer="scope">
                            <div style="display: flex;flex-direction: column;" :title="footerSummaryText()">
                                <div style="z-index: 999;">
                                    <span
                                        :style="{ background: scope.row.areaAbnormalStock ? 'white' : 'transparent', padding: scope.row.areaAbnormalStock ? '0 0 0 2px' : '0' }">
                                        {{ $utils.formatThousandSeparator(scope.row.areaStock) }}
                                    </span>
                                </div>
                                <span style=" margin-top:-18px;display: flex;flex-direction: column;font-size:
                                    11px;text-align: left;">
                                    <span style="color: var(--color-red);">
                                        异{{ $utils.formatThousandSeparator(scope.row.areaAbnormalStock) }}
                                    </span>
                                </span>
                            </div>
                        </template>
                    </GridColumn>
                    <GridColumn v-if="areaInfo.showMoney" field="inventoryPrice" title="本库金额" width="100" align="right"
                        :sortable="true" :rowspan="2">
                        <template #body="scope">
                            {{ $utils.formatThousandSeparator(scope.row.areaAmount) }}
                        </template>
                    </GridColumn>
                    <!-- 二级表头 -->
                    <GridColumn title="期初库存" :width="areaInfo.showMoney ? 300 : 100" align="center"
                        :colspan="areaInfo.showMoney ? 3 : 1">
                    </GridColumn>
                    <GridColumn title="本期入库" :width="areaInfo.showMoney ? 300 : 100" align="center"
                        :colspan="areaInfo.showMoney ? 3 : 1">
                    </GridColumn>
                    <GridColumn title="本期出库" :width="areaInfo.showMoney ? 300 : 100" align="center"
                        :colspan="areaInfo.showMoney ? 3 : 1">
                    </GridColumn>
                    <GridColumn title="期末转结" :width="areaInfo.showMoney ? 300 : 100" align="center"
                        :colspan="areaInfo.showMoney ? 3 : 1">
                    </GridColumn>
                    <GridColumn field="lastOperationTime" title="最近操作时间" width="150" align="left" :rowspan="2"
                        :sortable="true">
                        <template #body="scope">
                            {{ $utils.formatDateTime(scope.row.lastOperationTime, "yyyy-MM-dd HH:mm:ss") }}
                        </template>
                    </GridColumn>
                </GridHeaderRow>
                <GridHeaderRow>
                    <GridColumn field="beginningStockQuantity" title="数量" width="100" align="right">
                    </GridColumn>
                    <GridColumn v-if="areaInfo.showMoney" field="beginningStockPrice" title="单价" width="100"
                        align="right">
                    </GridColumn>
                    <GridColumn v-if="areaInfo.showMoney" field="beginningStockAmount" title="金额" width="100"
                        align="right">
                    </GridColumn>
                    <GridColumn field="currentInStockQuantity" title="数量" width="100" align="right">
                        <template #body="scope">
                            <div class="kingzon-detail-text" @click="showCurrentInList(scope.row)">{{
                                scope.row.currentInStockQuantity }}</div>
                        </template>
                    </GridColumn>
                    <GridColumn v-if="areaInfo.showMoney" field="currentInStockPrice" title="单价" width="100"
                        align="right">
                    </GridColumn>
                    <GridColumn v-if="areaInfo.showMoney" field="currentInStockAmount" title="金额" width="100"
                        align="right">
                    </GridColumn>
                    <GridColumn field="currentOutStockQuantity" title="数量" width="100" align="right">
                        <template #body="scope">
                            <div class="kingzon-detail-text" @click="showCurrentOutList(scope.row)">{{
                                scope.row.currentOutStockQuantity }}</div>
                        </template>
                    </GridColumn>
                    <GridColumn v-if="areaInfo.showMoney" field="currentOutStockPrice" title="单价" width="100"
                        align="right">
                    </GridColumn>
                    <GridColumn v-if="areaInfo.showMoney" field="currentOutStockAmount" title="金额" width="100"
                        align="right">
                    </GridColumn>
                    <GridColumn field="endingStockQuantity" title="数量" width="100" align="right">
                    </GridColumn>
                    <GridColumn v-if="areaInfo.showMoney" field="endingStockPrice" title="单价" width="100" align="right">
                    </GridColumn>
                    <GridColumn v-if="areaInfo.showMoney" field="endingStockAmount" title="金额" width="100"
                        align="right">
                    </GridColumn>
                </GridHeaderRow>
            </GridColumnGroup>
            <GridColumnGroup :frozen="true" align="right" width="130">
                <GridHeaderRow>
                    <GridColumn title="操作" width="130px" align="center">
                        <template #body="scope">
                            <ButtonGroup style="height:24px">
                                <LinkButton @click="materialInbound(scope.row)">入库</LinkButton>
                                <LinkButton @click="materialOutbound(scope.row)">出库</LinkButton>
                                <MenuButton text="..." :plain="true">
                                    <Menu>
                                        <MenuItem text="盘点" @click="materialCheck(scope.row)"></MenuItem>
                                        <MenuItem text="移库" @click="materialMove(scope.row)"></MenuItem>
                                        <MenuItem text="出入库记录" @click="materialInventoryRecord(scope.row)"></MenuItem>
                                    </Menu>
                                </MenuButton>
                            </ButtonGroup>
                        </template>
                    </GridColumn>
                </GridHeaderRow>
            </GridColumnGroup>
        </KingZonDataGrid>
        <!-- 库存记录：出入库记录（复用 material-inventory-datagrid） -->
        <MaterialInventoryDataGrid v-else-if="areaInfo" ref="materialRecordGrid"
            :dataGridId="'recordGrid' + areaId" :recordList="inventoryRecordList" :areaInfo="areaInfo"
            :systemParaList="systemParaList" :showMaterialInfo="true" :showInventoryType="true"
            :showQuantity="true" :showUsedQuantity="true" :showRemainQuantity="true" :showSelection="false"
            :showPage="true" :rightGroupRowWidth="0" :showAbnormalReport="false" @refreshTable="getInventoryRecord">
        </MaterialInventoryDataGrid>
    </div>

    <Dialog ref="MaterialInventory"
        :title="(selectedRow.materialNo ?? '') + '-' + (selectedRow.materialName ?? '') + ' 库存详情'" :modal="true" closed
        :draggable="true" :dialogStyle="{ height: '85%', width: '85%' }">
        <MaterialInventoryList :areaId="areaId" :dataType="selectedRow.dataType" :dataId="selectedRow.dataId"
            :supplierId="selectedRow.supplierId" :dataStatus="selectedRow.dataStatus"></MaterialInventoryList>
    </Dialog>

    <Dialog ref="Inbound" :title="(selectedRow.materialNo ?? '') + '-' + (selectedRow.materialName ?? '') + ' 直接入库'"
        :modal="true" closed :draggable="true" :dialogStyle="{ height: '85%', width: '85%' }">
        <Inbound :areaId="areaId" :dataType="selectedRow.dataType" :dataId="selectedRow.dataId"
            :supplierId="selectedRow.supplierId" :dataStatus="selectedRow.dataStatus" :showCost="true"
            @close="inboundDialog.close()" @confirm="inboundConfirm"></Inbound>
    </Dialog>
    <Dialog ref="Outbound" :title="(selectedRow.materialNo ?? '') + '-' + (selectedRow.materialName ?? '') + ' 直接出库'"
        :modal="true" closed :draggable="true" :dialogStyle="{ height: '85%', width: '85%' }">
        <Outbound :areaId="areaId" :dataType="selectedRow.dataType" :dataId="selectedRow.dataId"
            :supplierId="selectedRow.supplierId" :dataStatus="selectedRow.dataStatus" label="领用"
            @close="outboundDialog.close()" @confirm="outboundConfirm"></Outbound>
    </Dialog>


    <!-- 物料移库 -->
    <Drawer v-if="isMoveMaterial" ref="moveInventory"
        :title="(selectedRow.materialNo ?? '') + '-' + (selectedRow.materialName ?? '') + ' 移库'"
        :drawerStyle="{ width: '75%' }" region="east" :closable="true" :maskClosable="false"
        @collapse="isMoveMaterial = false; refreshTable(showZeroStock)">
        <MoveMaterial :areaId="areaId" :dataType="selectedRow.dataType" :dataId="selectedRow.dataId"
            :supplierId="selectedRow.supplierId" :dataStatus="selectedRow.dataStatus">
        </MoveMaterial>
    </Drawer>

    <!-- 新增物料入库 -->
    <Drawer v-if="isSelectMaterial" ref="selectMaterialDrawer"
        :title="(areaInfo?.code ? areaInfo.code + areaInfo.name : areaInfo?.name) + ' - 选择物资'"
        :drawerStyle="{ width: '95%' }" region="east" :closable="true" :maskClosable="false"
        @collapse="isSelectMaterial = false">
        <SelectMaterial :areaId="areaId" @close="selectMaterialDrawer.collapse()" @confirm="selectMaterialConfirm">
        </SelectMaterial>
    </Drawer>
    <Drawer v-if="isMaterialInbound" ref="materialInboundDrawer"
        :title="(areaInfo?.code ? areaInfo.code + areaInfo.name : areaInfo?.name) + ' - 新增物资入库'"
        :drawerStyle="{ width: '95%' }" region="east" :closable="true" :maskClosable="false"
        @collapse="isMaterialInbound = false">
        <MaterialInbound :areaId="areaId" :materialList="selectMaterialList" @close="materialInboundDrawer.collapse()"
            @confirm="materialInboundConfirm">
        </MaterialInbound>
    </Drawer>
    <!-- 出入库流水 -->
    <Drawer ref="InventoryRecordDrawer" v-if="isInventoryRecord"
        :title="(selectedRow.materialNo ?? '') + '-' + (selectedRow.materialName ?? '') + ' 出入库明细'"
        :drawerStyle="{ width: '75%' }" region="east" :closable="true" :maskClosable="false"
        @collapse="isInventoryRecord = false; refreshTable(showZeroStock)">
        <InventoryRecord ref="InventoryRecord" :areaId="areaId" :dataType="selectedRow.dataType"
            :dataId="selectedRow.dataId" :supplierId="selectedRow.supplierId" :dataStatus="selectedRow.dataStatus">
        </InventoryRecord>
    </Drawer>
    <!-- 本期出入库 -->
    <Dialog ref="CurrentInDialog"
        :title="(selectedRow.materialNo ?? '') + '-' + (selectedRow.materialName ?? '') + ' 本期入库'" :modal="true" closed
        :draggable="true" :dialogStyle="{ height: '85%', width: '85%' }">
        <CurrentInventoryRecord :areaId="areaId" :dataType="selectedRow.dataType" :dataId="selectedRow.dataId"
            :supplierId="selectedRow.supplierId" :dataStatus="selectedRow.dataStatus" :inventoryType="InventoryType.入库">
        </CurrentInventoryRecord>
    </Dialog>
    <Dialog ref="CurrentOutDialog"
        :title="(selectedRow.materialNo ?? '') + '-' + (selectedRow.materialName ?? '') + ' 本期出库'" :modal="true" closed
        :draggable="true" :dialogStyle="{ height: '85%', width: '85%' }">
        <CurrentInventoryRecord :areaId="areaId" :dataType="selectedRow.dataType" :dataId="selectedRow.dataId"
            :supplierId="selectedRow.supplierId" :dataStatus="selectedRow.dataStatus" :inventoryType="InventoryType.出库">
        </CurrentInventoryRecord>
    </Dialog>
    <!-- 盘库 -->
    <Drawer ref="InventoryCheckDrawer" v-if="isInventoryCheck"
        :title="areaInfo?.code ? areaInfo.code + areaInfo.name : areaInfo?.name + ' 盘库'" :drawerStyle="{ width: '95%' }"
        region="east" :closable="true" :maskClosable="false"
        @collapse="isInventoryCheck = false; refreshTable(showZeroStock)">
        <InventoryCheck ref="InventoryCheckRecord" :areaId="areaId" @close="inventoryCheckDrawer.collapse()"
            @confirm="inventoryCheckConfirm">
        </InventoryCheck>
    </Drawer>
    <!-- 盘点 -->
    <Dialog ref="CreateCheckDialog" @close="refreshTable(showZeroStock)"
        :title="(selectedRow.materialNo ?? '') + '-' + (selectedRow.materialName ?? '') + ' 盘点'" :modal="true" closed
        :draggable="true" :resizable="true" :dialogStyle="{ height: '90%', width: '85%' }">
        <CreateCheck :areaId="areaId" :dataType="selectedRow.dataType" :dataId="selectedRow.dataId"
            :supplierId="selectedRow.supplierId" :dataStatus="selectedRow.dataStatus">
        </CreateCheck>
    </Dialog>
</template>

<script lang="ts">
import { defineComponent } from 'vue';
import { MaterialStockDto, AreaInfoDto, InboundDto, OutboundDto, InventoryUnitOutboundDto, AvailableMaterialDto, InventoryRecordDto } from '@/types/inventory';
import Decimal from 'decimal.js';
import MaterialInventoryList from './material-inventoryList.vue';
import { InventoryDataStatus, InventoryType } from "@/types/enums";
import type { Para } from '@/types/common';
import Inbound from './inbound.vue';
import Outbound from './outbound.vue';
import SelectMaterial from './select-material.vue';
import MaterialInbound from './material-inbound.vue';
import InventoryRecord from './inventoryRecord.vue';
import CurrentInventoryRecord from './current-inventoryRecord.vue';
import InventoryCheck from './check/check-warehouse.vue';
import CreateCheck from './check/create-check.vue';
// import InventoryCheck from './check-inventory.vue';
import MoveMaterial from './move-material.vue';
import MaterialInventoryDataGrid from '../customer_Inventory_Manage/material-inventory-datagrid.vue';

enum OperateStatus {
    库存 = 0,
    库存记录 = 1
}

export default defineComponent({
    name: 'InventoryDataGrid',
    components: {
        MaterialInventoryList, Inbound, Outbound, SelectMaterial, MaterialInbound, InventoryRecord, CurrentInventoryRecord, InventoryCheck, CreateCheck, MoveMaterial, MaterialInventoryDataGrid
    },
    props: {
        materialfilterList: {
            type: Array,
            default: () => []
        },
        inventoryfilterList: {
            type: Array,
            default: () => []
        },
        customerfilterList: {
            type: Array,
            default: () => []
        },
        checkFilterList: {
            type: Array,
            default: () => []
        },
        sourcefilterList: {
            type: Array,
            default: () => []
        },
        areaId: {
            type: Number,
            default: 0
        },
        selectedAreaId: {
            type: Number,
            default: 0
        }
    },
    computed: {
        footerData() {
            return this.recordList ? [{
                index: '合计',
                totalStock: this.handleFooterData("totalStock"),
                areaStock: this.handleFooterData("areaStock"),
                areaAbnormalStock: this.handleFooterData("areaAbnormalStock"),
                areaNormalStock: this.handleFooterData("areaNormalStock"),
                areaAmount: this.handleFooterData("areaAmount"),
                beginningStockQuantity: this.handleFooterData("beginningStockQuantity"),
                beginningStockAmount: this.handleFooterData("beginningStockAmount"),
                currentInStockQuantity: this.handleFooterData("currentInStockQuantity"),
                currentInStockAmount: this.handleFooterData("currentInStockAmount"),
                currentOutStockQuantity: this.handleFooterData("currentOutStockQuantity"),
                currentOutStockAmount: this.handleFooterData("currentOutStockAmount"),
                endingStockQuantity: this.handleFooterData("endingStockQuantity"),
                endingStockAmount: this.handleFooterData("endingStockAmount")
            }] : [{ index: '合计' }]
        },
        InventoryDataStatus() {
            return InventoryDataStatus;
        },
        InventoryType() {
            return InventoryType;
        },
        OperateStatus() {
            return OperateStatus
        },
    },
    data() {
        return {
            isLoading: false,
            recordList: [] as MaterialStockDto[],
            areaInfo: undefined as AreaInfoDto | undefined,
            showZeroStock: false,
            // 库存记录（出入库）
            operate: OperateStatus.库存 as OperateStatus,
            inventoryRecordList: [] as InventoryRecordDto[],
            systemParaList: [] as Para[],
            materialInventoryDialog: {} as EasyUI.Dialog,
            inboundDialog: {} as EasyUI.Dialog,
            selectedRow: {} as MaterialStockDto,
            outboundDialog: {} as EasyUI.Dialog,
            isSelectMaterial: false,
            selectMaterialDrawer: {} as EasyUI.Drawer,
            isMaterialInbound: false,
            materialInboundDrawer: {} as EasyUI.Drawer,
            selectMaterialList: [] as AvailableMaterialDto[], //新物资入库，选中的物资
            isInventoryRecord: false,
            inventoryRecordDrawer: {} as EasyUI.Drawer,
            currentInDialog: {} as EasyUI.Dialog,
            currentOutDialog: {} as EasyUI.Dialog,
            isInventoryCheck: false,
            inventoryCheckDrawer: {} as EasyUI.Drawer,
            createCheckDialog: {} as EasyUI.Dialog,

            moveInventoryDialog: {} as EasyUI.Dialog,
            isMoveMaterial: false,
            moveInventoryDrawer: {} as EasyUI.Drawer
        }
    },
    created() {
        this.init();
    },
    mounted() {
        this.materialInventoryDialog = this.$refs.MaterialInventory as EasyUI.Dialog;
        this.inboundDialog = this.$refs.Inbound as EasyUI.Dialog;
        this.outboundDialog = this.$refs.Outbound as EasyUI.Dialog;
        this.currentInDialog = this.$refs.CurrentInDialog as EasyUI.Dialog;
        this.currentOutDialog = this.$refs.CurrentOutDialog as EasyUI.Dialog;
        this.createCheckDialog = this.$refs.CreateCheckDialog as EasyUI.Dialog;
    },
    methods: {
        init() {
            this.isLoading = true;
            this.getAreaData()
                .then(res => {
                    this.areaInfo = res.data as AreaInfoDto;
                    return this.$nextTick();
                })
                .then(res => {
                    if (this.areaId == this.selectedAreaId) {
                        return this.getData();
                    }
                })
                .then(res => {
                    if (res && res.data) {
                        var data = res.data as any;
                        this.recordList = data;
                    }
                })
                .catch((err) => {
                    this.$kingzon_messager.showError(err);
                }).finally(() => {
                    this.isLoading = false;
                });
        },
        getAreaData() {
            return this.$http.get("/api/inventory/InventoryManagement/GetAreaInfo", {
                areaId: this.areaId,
            });
        },
        getData() {
            var materialFilters = this.materialfilterList;
            if (this.areaInfo?.showCustomer) {
                materialFilters = materialFilters.concat(this.customerfilterList);
            }
            return this.$http.post("/api/inventory/InventoryManagement/GetAreaStockList", {
                areaId: this.areaId,
                materialFilters: materialFilters,
                inventoryFilters: this.inventoryfilterList,
                checkFilters: this.checkFilterList,
                showZeroStock: this.showZeroStock
            }, this.$refs.dataGrid);
        },
        refreshTable(showZeroStock: boolean = false) {
            if (this.operate != OperateStatus.库存) {
                return this.getInventoryRecord();
            }
            this.showZeroStock = showZeroStock;
            this.isLoading = true;
            return this.getData()
                .then(res => {
                    var data = res.data as any;
                    this.recordList = data;
                    // console.log("refreshTable", this.recordList, this.areaInfo);
                })
                .catch((err) => {
                    this.$kingzon_messager.showError(err);
                }).finally(() => {
                    this.isLoading = false;
                });
        },
        //库存物资 / 库存记录 切换
        operateStatusChange(status: OperateStatus) {
            this.operate = status;
            this.$emit('operateChange', status);
        },
        //获取库存记录（出入库同时显示）
        async getInventoryRecord() {
            if (!this.areaInfo) {
                var resArea = await this.$http.get("/api/inventory/InventoryManagement/GetAreaInfo", { areaId: this.areaId });
                this.areaInfo = resArea.data as AreaInfoDto;
            }
            await this.$nextTick();
            this.isLoading = true;
            try {
                var response = await this.$http.post("/api/inventory/InventoryRecordManagement/GetInventoryRecordByPage", {
                    areaId: this.areaId,
                    inventoryRecordFilters: this.inventoryfilterList,
                    inventoryRecordExtFilters: [],
                    materialFilters: this.materialfilterList,
                    sourceFilters: this.sourcefilterList,
                }, this.getRecordDataGrid());
                this.inventoryRecordList = (response.data as any).records as InventoryRecordDto[];
                this.systemParaList = (response.data as any).extList as Para[];
            } catch (err) {
                this.$kingzon_messager.showError(err);
            } finally {
                this.isLoading = false;
            }
        },
        //取库存记录 datagrid（material-inventory-datagrid 内部 KingZonDataGrid，用于分页）
        getRecordDataGrid() {
            var ref = this.$refs.materialRecordGrid as any;
            if (!ref) return undefined;
            var grid = Array.isArray(ref) ? ref[0] : ref;
            return grid?.dataGrid as EasyUI.DataGrid;
        },
        getCellSpan(data) {
            var cellSpanColumns = ["materialNo", "materialName", "materialTechnicalCode", "materialCategory", "materialSpecification", "materialGrade", "materialNoCustomerName", "safetyStock"];
            if (cellSpanColumns.includes(data.column.field)) {
                var rowIndex = data.rowIndex
                return this.$utils.getCellSpanMultiKey([["dataType"], ["dataId"]], data.row, rowIndex, this.recordList);
            }
            return { rowspan: 1, colspan: 1 };
        },
        handleFooterData(key) {
            var decimalTotal = this.recordList.reduce((sum, record) => Decimal.add(sum, record[key]), new Decimal(0));
            //最多保留6位小数
            if (decimalTotal.decimalPlaces() > 6) {
                decimalTotal = decimalTotal.toDecimalPlaces(6, Decimal.ROUND_HALF_UP);
            }
            return this.$utils.formatThousandSeparator(decimalTotal.toNumber());
        },
        showDetailList(row: MaterialStockDto) {
            this.selectedRow = row;
            this.materialInventoryDialog.open();
        },
        handleUnitData(key: string, record: MaterialStockDto, grouped?: Map<number, { unitName: string; quantity: Decimal; abnormalQty: Decimal; normalQty: Decimal }>) {
            if (grouped == null) {
                grouped = new Map<number, { unitName: string; quantity: Decimal; abnormalQty: Decimal; normalQty: Decimal }>();
            }
            for (const unit of record.inventoryUnitList ?? []) {
                const current = grouped.get(unit[key]);

                if (current) {
                    current.quantity = current.quantity.add(unit.quantity);
                    current.abnormalQty = current.abnormalQty.add(unit.abnormalQuantity ?? 0);
                    current.normalQty = current.normalQty.add(unit.normalQuantity ?? 0);
                } else {
                    grouped.set(unit[key], {
                        unitName: unit.unitName,
                        quantity: new Decimal(unit.quantity),
                        abnormalQty: new Decimal(unit.abnormalQuantity ?? 0),
                        normalQty: new Decimal(unit.normalQuantity ?? 0)
                    });
                }
            }
            return grouped;
        },

        handleFooterUnitData() {
            const grouped = new Map<number, {
                unitName: string,
                quantity: Decimal,
                abnormalQty: Decimal,
                normalQty: Decimal
            }>();

            for (const record of this.recordList) {
                this.handleUnitData('unitId', record, grouped);
            }

            return [...grouped.entries()].map(([unitKey, { unitName, quantity, abnormalQty, normalQty }]) => ({
                unitName,
                quantity: this.$utils.formatThousandSeparator(
                    quantity.toDecimalPlaces(6, Decimal.ROUND_HALF_UP).toNumber()
                ),
                abnormalQty: this.$utils.formatThousandSeparator(
                    abnormalQty.toDecimalPlaces(6, Decimal.ROUND_HALF_UP).toNumber()
                ),
                normalQty: this.$utils.formatThousandSeparator(
                    normalQty.toDecimalPlaces(6, Decimal.ROUND_HALF_UP).toNumber()
                )
            }));
        },
        footerSummaryText() {
            return this.handleFooterUnitData().map(u => `${u.quantity} ${u.unitName} ${u.abnormalQty && u.abnormalQty !== '0' ? `【正${u.normalQty} 异${u.abnormalQty}】` : ''}`).join('\n');
        },
        summaryText(row) {
            var data = [...this.handleUnitData('id', row).entries()].map(([unitKey, { unitName, quantity, abnormalQty, normalQty }]) => ({
                unitName,
                quantity: this.$utils.formatThousandSeparator(
                    quantity.toDecimalPlaces(6, Decimal.ROUND_HALF_UP).toNumber()
                ),
                abnormalQty: this.$utils.formatThousandSeparator(
                    abnormalQty.toDecimalPlaces(6, Decimal.ROUND_HALF_UP).toNumber()
                ),
                normalQty: this.$utils.formatThousandSeparator(
                    normalQty.toDecimalPlaces(6, Decimal.ROUND_HALF_UP).toNumber()
                )
            }));
            return data.map(u => `${u.quantity} ${u.unitName} ${u.abnormalQty && u.abnormalQty !== '0' ? `【正${u.normalQty} 异${u.abnormalQty}】` : ''}`).join('\n');
        },
        materialInbound(row) {
            this.selectedRow = row;
            this.inboundDialog.open();
        },
        async inboundConfirm(data: { dataList: InboundDto[], dataStatus?: InventoryDataStatus, supplierId?: number }) {
            //提交后端
            this.isLoading = true;
            await this.$http.post("/api/inventory/InventoryManagement/Inbound", {
                dataType: this.selectedRow.dataType,
                dataId: this.selectedRow.dataId,
                supplierId: this.selectedRow.supplierId,
                dataStatus: this.selectedRow.dataStatus,
                dataList: data.dataList
            }).then(res => {
                this.$kingzon_messager.showInfo(res.data as any as string);
                this.inboundDialog.close();
            }).catch((err) => {
                this.$kingzon_messager.showError(err);
            }).finally(() => {
                this.isLoading = false;
            });
            this.refreshTable();
        },
        materialOutbound(row) {
            this.selectedRow = row;
            this.outboundDialog.open();
        },
        async outboundConfirm(data: {
            dataList: OutboundDto[], userId: number, deptId: number, addCost: boolean,
            costCategoryId?: number, remark?: string, totalOutboundUnitList: Array<InventoryUnitOutboundDto>,
            totalOutboundQuantity: number, outboundUnitConversionId: number
        }) {
            //提交后端
            this.isLoading = true;
            await this.$http.post("/api/inventory/InventoryManagement/Outbound", {
                dataType: this.selectedRow.dataType,
                dataId: this.selectedRow.dataId,
                userId: data.userId,
                deptId: data.deptId,
                addCost: data.addCost,
                costCategoryId: data.costCategoryId,
                remark: data.remark,
                totalUnitList: data.totalOutboundUnitList,
                totalOutboundQuantity: data.totalOutboundQuantity,
                outboundUnitConversionId: data.outboundUnitConversionId,
                dataList: data.dataList
            }).then(res => {
                this.$kingzon_messager.showInfo(res.data as any as string);
                this.outboundDialog.close();
            }).catch((err) => {
                this.$kingzon_messager.showError(err);
            }).finally(() => {
                this.isLoading = false;
            });
            this.refreshTable();
        },
        addNewStock() {
            this.isSelectMaterial = true;
            this.$nextTick(() => {
                this.selectMaterialDrawer = this.$refs.selectMaterialDrawer as EasyUI.Drawer;
                this.selectMaterialDrawer.expand();
            });
        },
        selectMaterialConfirm(materialList: AvailableMaterialDto[]) {
            this.selectMaterialDrawer.collapse();
            this.selectMaterialList = materialList;

            this.isMaterialInbound = true;
            this.$nextTick(() => {
                this.materialInboundDrawer = this.$refs.materialInboundDrawer as EasyUI.Drawer;
                this.materialInboundDrawer.expand();
            });
        },
        materialInboundConfirm() {
            this.materialInboundDrawer.collapse();
            this.selectMaterialList = [];
            this.refreshTable();
        },
        materialInventoryRecord(row) {
            this.selectedRow = row;
            this.isInventoryRecord = true;
            this.$nextTick(() => {
                this.inventoryRecordDrawer = this.$refs.InventoryRecordDrawer as EasyUI.Drawer;
                console.log("inventoryRecordDrawer", this.inventoryRecordDrawer);
                this.inventoryRecordDrawer.expand();
            });
        },
        showCurrentInList(row) {
            this.selectedRow = row;
            this.currentInDialog.open();
        },
        showCurrentOutList(row) {
            this.selectedRow = row;
            this.currentOutDialog.open();
        },
        inventoryCheck() {
            this.isInventoryCheck = true;
            this.$nextTick(() => {
                this.inventoryCheckDrawer = this.$refs.InventoryCheckDrawer as EasyUI.Drawer;
                this.inventoryCheckDrawer.expand();
            });
        },
        inventoryCheckConfirm() {
            this.inventoryCheckDrawer.collapse();
            this.refreshTable();
        },
        materialCheck(row) {
            this.selectedRow = row;
            this.createCheckDialog.open();
        },
        materialMove(row) {
            this.selectedRow = row;
            this.isMoveMaterial = true;
            this.$nextTick(() => {
                this.moveInventoryDrawer = this.$refs.moveInventory as EasyUI.Drawer;
                this.moveInventoryDrawer.expand();
            })
        },
        materialMoveClose() {
            this.moveInventoryDrawer.collapse()
        },
        doing() {
            this.$kingzon_messager.showInfo("功能开发中");
        }
    }
})
</script>
<style>
:deep(.thin .l-btn-text) {
    line-height: 24px !important;
}

.thin {}
</style>