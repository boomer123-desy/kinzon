<template>
    <Loading v-if="isLoading"></Loading>
    <div class="kingzon-page">
        <div class="kingzon-page-title">
            <span>客户仓库管理</span>
        </div>
        <div class="kingzon-page-body">
            <SearchPanel :searchParams="searchParams" :searchParamsHidden="searchParamsHidden" @search="search">
            </SearchPanel>
            <div class="kingzon-page-content">
                <Tabs :scrollable="true" :plain="true" style="height:100%" ref="tabs" @tabSelect="tabSelect">
                    <TabPanel v-for="(item, index) in areaTabList" :key="item.value"
                        :title="(item.code ? item.code + '-' : '') + item.text" :data-area-id="item.value">
                        <div v-if="item.value == areaId" class="kingzon-page-content">
                            <div class="kingzon-btn-group thin" style="text-align: center; margin: 5px;height:24px;">
                                <ButtonGroup>
                                    <LinkButton :class="{ c8: operate == OperateStatus.库存 }"
                                        @click="operateStatusChange(OperateStatus.库存)"> 库存物资</LinkButton>
                                    <LinkButton :class="{ c8: operate == OperateStatus.入库 }"
                                        @click="operateStatusChange(OperateStatus.入库)"> 入库记录</LinkButton>
                                    <LinkButton :class="{ c8: operate == OperateStatus.出库 }"
                                        @click=operateStatusChange(OperateStatus.出库)> 出库记录</LinkButton>
                                </ButtonGroup>
                            </div>
                            <InventoryGrid class="kingzon-page-fill" :ref="'dataGrid' + index"
                                :dataGridId="`dataGridId_inventory_${index}`"
                                v-if="areaInfo && operate == OperateStatus.库存" :recordList="recordList"
                                :areaInfo="areaInfo" :showSelection="false" :showPage="true" :showMaterialInfo="true"
                                @refreshTable="refreshTable" :rightGroupRowWidth="180" :systemParaList="systemParaList">
                                <template #operate="scope">
                                    <ButtonGroup>
                                        <LinkButton @click="outbound(scope.row)">出库</LinkButton>
                                        <LinkButton @click="materialCheck(scope.row)">盘点</LinkButton>
                                        <LinkButton @click="$kingzon_messager.showInfo('功能开发中')">库存调拨</LinkButton>
                                    </ButtonGroup>
                                </template>
                            </InventoryGrid>
                            <InventoryGrid class="kingzon-page-fill" :ref="'dataGrid' + index"
                                :dataGridId="`dataGridId_inbound_${index}`" @refreshTable="refreshTable"
                                v-if="areaInfo && operate == OperateStatus.入库" :recordList="recordList"
                                :areaInfo="areaInfo" :showSelection="false" :showPage="true" :showMaterialInfo="true"
                                :rightGroupRowWidth="0" :systemParaList="systemParaList" :showAbnormalReport="false"
                                :showQuantity="true" :showUsedQuantity="true">
                            </InventoryGrid>
                            <InventoryGrid class="kingzon-page-fill" :ref="'dataGrid' + index"
                                :dataGridId="`dataGridId_outbound_${index}`" @refreshTable="refreshTable"
                                v-if="areaInfo && operate == OperateStatus.出库" :recordList="recordList"
                                :areaInfo="areaInfo" :showSelection="false" :showPage="true" :showMaterialInfo="true"
                                :showRemainQuantity="false" :rightGroupRowWidth="0" :showUsedQuantity="true">
                            </InventoryGrid>
                        </div>
                    </TabPanel>
                </Tabs>
            </div>
        </div>
    </div>

    <!-- 出库 -->
    <Dialog ref="Outbound" :title="(selectedRow.materialNo ?? '') + '-' + (selectedRow.materialName ?? '') + ' 直接出库'"
        :modal="true" closed :draggable="true" :dialogStyle="{ height: '85%', width: '85%' }">
        <Outbound :areaId="Number(areaId)" :dataType="selectedRow.dataType" :dataId="selectedRow.dataId"
            :supplierId="selectedRow.supplierId" :dataStatus="selectedRow.dataStatus" label="领用"
            :onlyOutboundList="true" :outboundList="[selectedRow]" @close="outboundDialog.close()"
            @confirm="outboundConfirm"></Outbound>
    </Dialog>

    <!-- 盘点 -->
    <Dialog ref="CreateCheckDialog"
        :title="(selectedRow.materialNo ?? '') + '-' + (selectedRow.materialName ?? '') + ' 盘点'" :modal="true" closed
        :draggable="true" :resizable="true" :dialogStyle="{ height: '90%', width: '85%' }">
        <CreateCheck :areaId="Number(areaId)" :dataType="selectedRow.dataType" :dataId="selectedRow.dataId"
            :onlyCheckList="true" :checkList="checkselectedRowList" :supplierId="selectedRow.supplierId"
            :dataStatus="selectedRow.dataStatus" @checkSuccess="inventoryCheckSuccess">
        </CreateCheck>
    </Dialog>
</template>
<script lang="ts">
import { DataType, FilterType, InventoryDataStatus, InventorySourceType, InventoryType, SystemParaType } from '@/types/enums';
import { Search, type Filter, type FilterTmp, type SelectOption } from '@/types/types';
import { defineComponent } from 'vue';
import { AreaInfoDto, InventoryCheckDto, InventoryRecordDto, InventoryUnitCheckDto, InventoryUnitOutboundDto, OutboundDto } from '@/types/inventory.js';
import type { Para, SystemParaTreeDto } from '@/types/common.js';
import InventoryGrid from '../inventory_management/material-inventory-datagrid.vue';
import Outbound from '../inventory_management/outbound.vue';
import CreateCheck from '../inventory_management/check/create-check.vue';


enum OperateStatus {
    库存 = 0,
    入库 = 1,
    出库 = 2
}

export default defineComponent({
    components: {
        InventoryGrid, Outbound, CreateCheck
    },
    data() {
        return {
            isLoading: false as boolean,
            searchParams: [] as Search[], //出库查询
            searchParamsHidden: [] as Search[], //出库隐藏查询

            areaTabList: [] as SelectOption[],  //所有的库房，可出库的feature = 1

            areaInfo: undefined as AreaInfoDto | undefined,  //库房详情 传InventoryGrid组件里面用
            recordList: [] as InventoryRecordDto[], //库存记录 
            areaId: 0 as number, //库房Id
            operate: OperateStatus.库存 as OperateStatus, //选中的库存类型
            systemParaList: [] as Array<Para>,//扩展参数列

            materialFilters: [] as Filter[],  //物料类型过滤
            inventoryRecordFilters: [] as Filter[],  //库存过滤
            inventoryRecordExtFilters: [] as Filter[],  //库存生产信息过滤
            sourceFilters: [] as Filter[],  // 来源过滤
            // extFilterList: [] as Filter[],  //库存生产信息过滤


            customerParaList: [] as SystemParaTreeDto[],  //客户拓展信息
            categoryOptions: [] as SelectOption[],  //取物料类别信息
            supplierOptions: [] as SelectOption[],  //供应商信息

            selectedRow: {} as OutboundDto,

            outboundDialog: {} as EasyUI.Dialog,  // 出库窗口
            createCheckDialog: {} as EasyUI.Dialog,  //盘点窗口
            checkselectedRowList: [] as InventoryCheckDto[],
        }
    },
    computed: {
        OperateStatus() {
            return OperateStatus
        }
    },
    async mounted() {
        try {
            this.isLoading = true;
            this.outboundDialog = this.$refs.Outbound as EasyUI.Dialog;
            this.createCheckDialog = this.$refs.CreateCheckDialog as EasyUI.Dialog;
            await this.getAllMaterialCategory()
            await this.getSupplierData()
            await this.getCustomerParamData()
            await this.getAllCustomerWareHouse()
            await this.getAreaData()
            this.addSearchParam()
            await this.getData()
        } catch (error) {
            this.$kingzon_messager.showError(error)
        } finally {
            this.isLoading = false;
        }
    },
    methods: {
        // 获取物料类别信息
        async getAllMaterialCategory() {
            var reponse = await this.$http.get("/api/inventory/InventoryManagement/GetAllMaterialCategory")
            this.categoryOptions = (reponse.data as any[]).map(o => {
                return { value: o.id, text: o.name } as SelectOption
            })
        },
        //获取所有供应商
        async getSupplierData() {
            var response = await this.$http.get("/api/base/Supplier/GetAllSupplier");
            this.supplierOptions = (response.data as any[]).map(o => {
                return { value: o.id, text: o.customerName } as SelectOption
            })
        },
        //获取客户参数数据
        async getCustomerParamData() {
            var types = [SystemParaType.外购物料信息, SystemParaType.生产物料信息];
            var response = await this.$http.post('/api/base/SystemPara/GetAdditionalParamByTypes', types);
            this.customerParaList = response.data as SystemParaTreeDto[];
        },
        //出库查询面板
        addSearchParam(): void {
            this.searchParams.push(new Search('materialNo', '物料编码', 'material'));
            this.searchParams.push(new Search('materialName', '物料名称', 'material'));
            this.searchParams.push(new Search('technicalCode', '技术型号', 'material'));
            this.searchParams.push(new Search('specification', '规格型号', 'material'));
            this.searchParams.push(new Search('supplierId', '供应商', 'inventoryRecord', FilterType.IntArrayContains, DataType.multiSelectNum, undefined, this.supplierOptions, undefined, '250px'));

            var options = this.$utils.getEnumKeyValueArray(InventorySourceType);  //枚举转换成下拉选项数组
            this.searchParamsHidden.push(new Search('sourceType', '库存来源', 'inventoryRecord', FilterType.IntArrayContains, DataType.multiSelectNum, undefined, options));
            this.searchParamsHidden.push(new Search('sourceNo', '来源单号', 'source'));

            this.searchParamsHidden.push(new Search('createTime', '入库时间', 'inventoryRecord', FilterType.GreaterThanOrEqual, DataType.DateTime, undefined, undefined, "开始日期"));
            this.searchParamsHidden.push(new Search('createTime', '', 'inventoryRecord', FilterType.LessThanOrEqual, DataType.DateTime, undefined, undefined, "结束日期"));
            this.searchParamsHidden.push(new Search('recordNo', '库存编码', 'inventoryRecord'));
            this.searchParamsHidden.push(new Search('createUserName', '操作员', 'inventoryRecord'));
            var statusOptions = this.$utils.getEnumKeyValueArray(InventoryDataStatus).filter(x => x.value != InventoryDataStatus.None)
            this.searchParamsHidden.push(new Search('dataStatus', '物料状态', 'inventoryRecord', FilterType.Equals, DataType.SelectNum, undefined, statusOptions));
            this.searchParamsHidden.push(new Search('materialGrade', '材质牌号', 'material'));
            this.searchParamsHidden.push(new Search('categoryId', '物料类别', 'material', FilterType.IntArrayContains, DataType.multiSelectNum, undefined, this.categoryOptions));
            this.$utils.additionalParam2SearchParam(this.customerParaList, this.searchParams, 'inventoryRecordExt');
        },

        //库存，入库，出库按钮切换
        async operateStatusChange(status: OperateStatus) {
            try {
                this.isLoading = true
                this.operate = status
                await this.getData()
            } catch (error) {
                this.$kingzon_messager.showError(error)
            } finally {
                this.isLoading = false
            }
        },
        //查询实现
        async search(params: FilterTmp[]) {
            try {
                this.isLoading = true;
                this.inventoryRecordFilters = params.find((item) => item.table == 'inventoryRecord')?.filter ?? []; //ok
                this.inventoryRecordExtFilters = params.find((item) => item.table == 'inventoryRecordExt')?.filter ?? [];
                this.materialFilters = params.find((item) => item.table == 'material')?.filter ?? [];
                this.sourceFilters = params.find((item) => item.table == 'source')?.filter ?? [];
                await this.getData();
            } catch (error) {
                this.$kingzon_messager.showError(error)
            } finally {
                this.isLoading = false
            }
        },
        //获取数据
        async getData() {
            await this.$nextTick()
            this.recordList = []
            switch (this.operate) {
                case OperateStatus.库存:
                    await this.getRecord()
                    break;
                case OperateStatus.入库:
                case OperateStatus.出库:
                    await this.getInOrOutRecord()
                    break;
            }
        },
        // 获取可发货的库房，所有客户仓库
        async getAllCustomerWareHouse() {
            var response = await this.$http.get(
                "/api/inventory/CustomerInventoryManage/GetUserAndOrdinaryStorageAreaList"
            )
            this.areaTabList = response.data as SelectOption[]
            this.areaId = Number(this.areaTabList[0].value)

        },
        //根据库区Id获取库区消息
        async getAreaData() {
            var res = await this.$http.get("/api/inventory/InventoryManagement/GetAreaInfo", {
                areaId: this.areaId,
            });
            this.areaInfo = res.data as AreaInfoDto

        },
        //切换Tab
        async tabSelect($event) {
            this.isLoading = true
            try {
                this.areaId = $event.$el.dataset.areaId;
                await this.getAreaData()
                await this.getData()
            } catch (error) {
                this.$kingzon_messager.showError(error)
            } finally {
                this.isLoading = false
            }

        },
        //分页
        async refreshTable() {
            this.isLoading = true
            try {
                await this.getData()
            } catch (error) {
                this.$kingzon_messager.showError(error)
            } finally {
                this.isLoading = false
            }
        },
        getDataGrid() {
            var dataGridName = `dataGrid${this.areaTabList.findIndex(o => Number(o.value) == this.areaId)}`
            var dataGrid = this.$refs[dataGridName] as any[]
            return dataGrid[0].dataGrid as EasyUI.DataGrid;
        },
        //获取页面库存信息
        async getRecord() {
            if (!this.areaId) {
                return
            }
            var inventoryTypeFilter = this.inventoryRecordFilters.findIndex(o => o.key == 'inventoryType')
            if (inventoryTypeFilter >= 0) {
                this.inventoryRecordFilters.splice(inventoryTypeFilter, 1)
            }
            var response = await this.$http.post("/api/inventory/CustomerInventoryManage/GetPositionInventoryWithAreaId", {
                areaIds: [this.areaId],
                inventoryFilterList: this.inventoryRecordFilters,
                extFilterList: this.inventoryRecordExtFilters,
                materialFilterList: this.materialFilters,
                sourceFilterList: this.sourceFilters,
            }, this.getDataGrid());
            this.recordList = (response.data as any).records as InventoryRecordDto[]
            this.systemParaList = (response.data as any).systemParaData;
        },
        //获取入库或出库信息
        async getInOrOutRecord() {
            if (!this.areaId) {
                return
            }

            var inventoryTypeFilter = this.inventoryRecordFilters.find(o => o.key == 'inventoryType')
            if (!inventoryTypeFilter) {
                this.inventoryRecordFilters.push({
                    key: 'inventoryType',
                    value: this.operate == OperateStatus.入库 ? InventoryType.入库 : InventoryType.出库,
                    filterType: FilterType.Equals,
                    dataType: DataType.Integer
                })
            } else {
                inventoryTypeFilter.value = this.operate == OperateStatus.入库 ? InventoryType.入库 : InventoryType.出库
            }
            var response = await this.$http.post("/api/inventory/InventoryRecordManagement/GetInventoryRecordByPage", {
                areaId: this.areaId,
                inventoryRecordFilters: this.inventoryRecordFilters,
                inventoryRecordExtFilters: this.inventoryRecordExtFilters,
                materialFilters: this.materialFilters,
                sourceFilters: this.sourceFilters,
            }, this.getDataGrid());
            this.recordList = (response.data as any).records as InventoryRecordDto[]
            this.systemParaList = (response.data as any).extList as Para[]
        },
        //出库
        outbound(row: OutboundDto) {
            this.selectedRow = row;
            this.selectedRow.outboundUnitList = row.inventoryUnitList!.map(o => {
                return {
                    id: o.id,
                    unitId: o.unitId,
                    unitName: o.unitName,
                    ratio: o.ratio,
                    remark: '',
                    conversionType: o.conversionType,
                    outboundQuantity: 0
                } as InventoryUnitOutboundDto;
            })
            this.selectedRow.outboundUnitConversionId = -1
            this.selectedRow.outboundUnit = row.inventoryUnitList!.find(o => o.id == -1)!.unitName
            this.outboundDialog.open();
        },
        //出库确认提交后端
        async outboundConfirm(data: {
            dataList: OutboundDto[], userId: number, deptId: number, addCost: boolean,
            costCategoryId?: number, remark?: string, totalOutboundUnitList: Array<InventoryUnitOutboundDto>,
            totalOutboundQuantity: number, outboundUnitConversionId: number
        }) {

            try {
                //提交后端
                this.isLoading = true;
                var entity = {
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
                }
                var response = await this.$http.post("/api/inventory/InventoryManagement/Outbound", entity)
                this.$kingzon_messager.showInfo(response.data as any as string);
                this.outboundDialog.close();
            } catch (error) {
                this.$kingzon_messager.showError(error);
            } finally {
                this.isLoading = false
                await this.getData()
            }
        },
        //盘点
        materialCheck(row) {
            this.selectedRow = row;
            var item = row as InventoryCheckDto
            item.checkUnitConversionId = -1;
            item.checkUnit = row.inventoryUnitList!.find(o => o.id == -1)!.unitName
            item.checkUnitList = row.inventoryUnitList?.map(x => new InventoryUnitCheckDto(x));
            item.checkUnitListExMain = row.inventoryUnitListExMain?.map(x => new InventoryUnitCheckDto(x));
            item.checkQuantity = row.remainQuantity;
            item.pAndLQuantity = 0;
            this.checkselectedRowList = [item]
            this.createCheckDialog.open();
        },
        //盘点确定
        async inventoryCheckSuccess() {
            this.createCheckDialog.close();
            await this.getData()
        },

    }
})
</script>
<style scoped>
:deep(.thin .l-btn-text) {
    line-height: 24px !important;
}
</style>