<template>
    <Loading v-show="isLoading"></Loading>
    <div class="kingzon-page">
        <div class="kingzon-page-title">
            <span>库存管理</span>
        </div>
        <div class="kingzon-page-body">
            <SearchPanel :key="'search-' + operate" :searchParams="searchParams" :searchParamsHidden="searchParamsHidden"
                @search="search">
                <template v-if="operate == OperateStatus.库存" #customer="scope">
                    <CheckBox style="margin-left: 20px" v-model="isOnlyShowStock" @click="refreshTable()">
                    </CheckBox>
                    <span style="margin-left: 5px">仅显示在库物资</span>
                </template>
            </SearchPanel>
            <div class="kingzon-page-content">
                <Tabs :scrollable="true" :plain="true" style="height:100%" ref="tabs" @tabSelect="tabSelect">
                    <TabPanel v-for="(item, index) in tabList" :key="index"
                        :title="(item.code ? item.code + '-' : '') + item.text" :data-theme="item.value">
                        <DataGrid :ref="'dataGrid_' + item.value" :areaId="Number(item.value)"
                            :inventoryfilterList="inventoryfilterList" :materialfilterList="materialfilterList"
                            :customerfilterList="customerfilterList" :checkFilterList="checkFilterList"
                            :sourcefilterList="sourcefilterList" :selectedAreaId="Number(selectedTabId)"
                            @operateChange="onOperateChange">
                        </DataGrid>
                    </TabPanel>
                </Tabs>
            </div>
        </div>
    </div>
</template>

<script lang="ts">
import { defineComponent } from 'vue';
import { type SelectOption, Search, Filter, type FilterTmp, type UploadFile } from '@/types/types';
import { DataType, FilterType, WarehousePurpose, InventorySourceType, InventoryDataStatus } from '@/types/enums';
import DataGrid from './dataGrid.vue';

enum OperateStatus {
    库存 = 0,
    库存记录 = 1
}

export default defineComponent({
    name: 'InventoryManagement',
    components: {
        DataGrid
    },
    computed: {
        OperateStatus() {
            return OperateStatus
        }
    },
    data() {
        return {
            isLoading: false,
            searchParams: [] as Search[],
            searchParamsHidden: [] as Search[],
            isOnlyShowStock: true,
            tabList: [] as SelectOption[],
            selectedTabId: undefined as string | undefined,
            materialfilterList: [] as Filter[],
            inventoryfilterList: [] as Filter[],
            customerfilterList: [] as Filter[],
            checkFilterList: [] as Filter[],
            sourcefilterList: [] as Filter[],
            operate: OperateStatus.库存 as OperateStatus,
            customerOptions: [] as SelectOption[],
            categoryOptions: [] as SelectOption[],
        }
    },
    created() {
        this.init();
    },
    methods: {
        init() {
            this.isLoading = true;
            this.getCategoryData()
                .then(res => {
                    var data = res.data as any;
                    this.categoryOptions = data.map((x => {
                        return { value: x.id, text: x.name } as SelectOption;
                    }));
                    return this.getSupplierData()
                })
                .then(res => {
                    var data = res.data as any;
                    this.customerOptions = data.map((x => {
                        return { value: x.id, text: x.customerName } as SelectOption;
                    }));
                    this.buildSearchParam();
                    return this.getAreaData();
                })
                .then(res => {
                    var data = res.data as any;
                    this.tabList = data;
                    if (this.tabList.length > 0) {
                        this.selectedTabId = this.tabList[0].value as string || undefined;
                    }
                })
                .catch((err) => {
                    this.$kingzon_messager.showError(err);
                }).finally(() => {
                    this.isLoading = false;
                });

        },
        //构建查询面板（根据 库存物资/库存记录 切换）
        buildSearchParam(): void {
            this.searchParams = [];
            this.searchParamsHidden = [];
            if (this.operate == OperateStatus.库存) {
                this.addStockSearchParam();
            } else {
                this.addRecordSearchParam();
            }
        },
        //库存物资查询面板
        addStockSearchParam(): void {
            this.searchParams.push(new Search('materialNo', '物料编码'));
            this.searchParams.push(new Search('materialName', '物料名称'));
            this.searchParams.push(new Search('categoryId', '物料类别', 'default', FilterType.IntArrayContains, DataType.multiSelectNum, undefined, this.categoryOptions));
            this.searchParams.push(new Search('technicalCode', '技术型号'));
            this.searchParamsHidden.push(new Search('lastCheckTime', '最近盘点时间', 'check', FilterType.GreaterThanOrEqual, DataType.DateTime, undefined, undefined, "开始日期"));
            this.searchParamsHidden.push(new Search('lastCheckTime', '', 'check', FilterType.LessThanOrEqual, DataType.DateTime, undefined, undefined, "结束日期"));

            this.searchParamsHidden.push(new Search('specification', '规格型号'));
            this.searchParamsHidden.push(new Search('materialGrade', '材质牌号'));

            this.searchParamsHidden.push(new Search('supplierId', '供应商', 'inventory', FilterType.IntArrayContains, DataType.multiSelectNum, undefined, this.customerOptions, undefined, '300px'));
            this.searchParamsHidden.push(new Search('customerName', '客户名称', 'customer'));
            this.searchParamsHidden.push(new Search('customerMaterialNo', '客户产品图号', 'customer'));
        },
        //库存记录查询面板（出入库记录）
        addRecordSearchParam(): void {
            this.searchParams.push(new Search('materialNo', '物料编码', 'material'));
            this.searchParams.push(new Search('materialName', '物料名称', 'material'));
            this.searchParams.push(new Search('technicalCode', '技术型号', 'material'));
            this.searchParams.push(new Search('specification', '规格型号', 'material'));
            this.searchParams.push(new Search('supplierId', '供应商', 'inventoryRecord', FilterType.IntArrayContains, DataType.multiSelectNum, undefined, this.customerOptions, undefined, '250px'));

            var sourceOptions = this.$utils.getEnumKeyValueArray(InventorySourceType);
            this.searchParamsHidden.push(new Search('sourceType', '库存来源', 'inventoryRecord', FilterType.IntArrayContains, DataType.multiSelectNum, undefined, sourceOptions));
            this.searchParamsHidden.push(new Search('sourceNo', '来源单号', 'source'));
            this.searchParamsHidden.push(new Search('createTime', '出入库时间', 'inventoryRecord', FilterType.GreaterThanOrEqual, DataType.DateTime, undefined, undefined, "开始日期"));
            this.searchParamsHidden.push(new Search('createTime', '', 'inventoryRecord', FilterType.LessThanOrEqual, DataType.DateTime, undefined, undefined, "结束日期"));
            this.searchParamsHidden.push(new Search('recordNo', '库存编码', 'inventoryRecord'));
            this.searchParamsHidden.push(new Search('createUserName', '操作员', 'inventoryRecord'));
            var statusOptions = this.$utils.getEnumKeyValueArray(InventoryDataStatus).filter(x => x.value != InventoryDataStatus.None);
            this.searchParamsHidden.push(new Search('dataStatus', '物料状态', 'inventoryRecord', FilterType.Equals, DataType.SelectNum, undefined, statusOptions));
            this.searchParamsHidden.push(new Search('materialGrade', '材质牌号', 'material'));
            this.searchParamsHidden.push(new Search('categoryId', '物料类别', 'material', FilterType.IntArrayContains, DataType.multiSelectNum, undefined, this.categoryOptions));
        },
        //库存物资 / 库存记录 切换（子组件 dataGrid 触发）
        onOperateChange(status: OperateStatus) {
            this.operate = status;
            this.materialfilterList = [];
            this.inventoryfilterList = [];
            this.customerfilterList = [];
            this.checkFilterList = [];
            this.sourcefilterList = [];
            this.buildSearchParam();
            this.refreshTable();
        },
        getAreaData() {
            return this.$http.get("/api/inventory/InventoryManagement/GetUserStorageAreaList", { purpose: WarehousePurpose.普通仓 })
        },
        getSupplierData() {
            return this.$http.get("/api/base/Supplier/GetAllSupplier")
        },
        getCategoryData() {
            return this.$http.get("/api/inventory/InventoryManagement/GetAllMaterialCategory")
        },
        search(params: FilterTmp[]) {
            if (this.operate == OperateStatus.库存) {
                this.materialfilterList = params.find((item) => item.table == 'default')?.filter ?? []
                this.inventoryfilterList = params.find((item) => item.table == 'inventory')?.filter ?? [];
                this.customerfilterList = params.find((item) => item.table == 'customer')?.filter ?? [];
                this.checkFilterList = params.find((item) => item.table == 'check')?.filter ?? [];
            } else {
                //库存记录（出入库）
                this.materialfilterList = params.find((item) => item.table == 'material')?.filter ?? [];
                this.inventoryfilterList = params.find((item) => item.table == 'inventoryRecord')?.filter ?? [];
                this.sourcefilterList = params.find((item) => item.table == 'source')?.filter ?? [];
            }
            this.refreshTable();
        },
        refreshTable() {
            this.$nextTick(() => {
                var dataGrid = this.$refs['dataGrid_' + this.selectedTabId] as InstanceType<typeof DataGrid>;
                if (Array.isArray(dataGrid)) {
                    dataGrid[0].refreshTable(!this.isOnlyShowStock);
                } else {
                    dataGrid.refreshTable(!this.isOnlyShowStock);
                }
            })
        },
        tabSelect($event) {
            this.selectedTabId = $event.$el.dataset.theme;
            //刷新 当前显示table的数据
            var dataGrid = this.$refs['dataGrid_' + this.selectedTabId] as InstanceType<typeof DataGrid>;
            this.$nextTick(() => {
                if (Array.isArray(dataGrid)) {
                    dataGrid[0].refreshTable(!this.isOnlyShowStock);
                } else {
                    dataGrid.refreshTable(!this.isOnlyShowStock);
                }
            })

        },
    }
})

</script>