<template>
    <div>
        <Loading v-show="isLoading"></Loading>
        <div v-if="theme == 'Formula'" style="display: flex;flex-direction: column;height: 100%;">
            <div
                style="text-align: center;background-color: var(--color-grey-bg);height: 34px;line-height: 34px;font-weight: 700;">
                查找资料库
            </div>
            <SearchPanel :searchParams="searchParams" :searchParamsHidden="searchParamsHidden" @search="search"
                style="margin: 0 5px;">
            </SearchPanel>
            <Tabs :plain="true" style="flex: 1;" ref="tabs" @tabSelect="tabSelect">
                <TabPanel title="物料" data-theme="RowMaterial" style="height: 400px;">
                    <SearchTable :dataList="dataList['RowMaterial']" :theme="'RowMaterial'" ref="dataGrid_RowMaterial"
                        @refreshTable="refreshTable" @choose="chooseData"></SearchTable>
                </TabPanel>
                <TabPanel title="成品" data-theme="Product">
                    <!-- 成品如果没有配方 那么即物料 -->
                    <!-- 成品如果有配方 那么 加入的是配方，输入配方的用量，配方明细 不能在本页面编辑-->
                    <SearchTable :dataList="dataList['Product']" :theme="'Product'" ref="dataGrid_Product"
                        @refreshTable="refreshTable" @choose="chooseData"></SearchTable>
                </TabPanel>
                <TabPanel title="半成品" data-theme="SemiProduct">
                    <SearchTable :dataList="dataList['SemiProduct']" :theme="'SemiProduct'" ref="dataGrid_SemiProduct"
                        @refreshTable="refreshTable" @choose="chooseData"></SearchTable>
                </TabPanel>
                <TabPanel title="配方" data-theme="Formula">
                    <SearchTable :dataList="dataList['Formula']" :theme="'Formula'" ref="dataGrid_Formula"
                        @refreshTable="refreshTable" @choose="chooseData"></SearchTable>
                </TabPanel>
            </Tabs>
        </div>
        <div v-if="theme == 'Inspection'">
            <div
                style="text-align: center;background-color: var(--color-grey-bg);height: 34px;line-height: 34px;font-weight: 700;">
                查询检验标准库，开发中！
            </div>
        </div>
    </div>
</template>

<script lang="ts">
import { defineComponent, type PropType } from 'vue';
import { type SelectOption, Search, Filter, type FilterTmp, type UploadFile } from '@/types/types';
import { DataType, FilterType, ProductType } from '@/types/enums';
import SearchTable from './formula-search-table.vue'


export default defineComponent({
    name: 'FormulaSearch',
    emits: ["chooseData"],
    components: { SearchTable },
    props: {
        theme: {
            type: String as PropType<"Formula" | "Inspection">,
            default: "Formula"
        }
    },
    data() {
        return {
            isLoading: false,
            searchParams: [] as Search[],
            searchParamsHidden: [] as Search[],
            filterList: [] as Filter[],
            selectedTab: "RowMaterial" as "RowMaterial" | "Product" | "SemiProduct" | "Formula",
            dataList: {}
        }
    },
    created() {
        this.init();
    },
    mounted() {
    },
    methods: {
        init() {
            this.addSearchParam();
        },
        //查询面板
        addSearchParam(): void {
            this.searchParams.push(new Search('code', '编码'));
            this.searchParams.push(new Search('name', '名称'));
            this.searchParamsHidden.push(new Search('techCode', '技术型号'));
            this.searchParamsHidden.push(new Search('version', '版本号'));
            this.searchParamsHidden.push(new Search('specification', '规格型号'));
        },
        getData() {
            var datagrid = this.$refs['dataGrid_' + this.selectedTab] as any;

            switch (this.selectedTab) {
                case 'RowMaterial':
                    return this.getMaterialData(datagrid.$refs.dataGrid);
                case 'Product':
                    return this.getProductData(datagrid.$refs.dataGrid);
                case 'SemiProduct':
                    return this.getSemiProductData(datagrid.$refs.dataGrid);
                case 'Formula':
                    return this.getFormulaData(datagrid.$refs.dataGrid);
                default:
                    return Promise.resolve();
            }
        },
        getMaterialData(dataGrid) {
            var filterList = this.filterList.filter(item => item.key != 'version')
            return this.$http.post('/api/technical/FormulaManage/GetRawMaterialResourceList', filterList, dataGrid)
        },
        getProductData(dataGrid) {
            var filterList = this.filterList.filter(item => item.key != 'version' && item.key != 'specification')
            return this.$http.post('/api/technical/FormulaManage/GetProductResourceList?productType=' + ProductType.成品, filterList, dataGrid)
        },
        getSemiProductData(dataGrid) {
            var filterList = this.filterList.filter(item => item.key != 'version' && item.key != 'specification')
            return this.$http.post('/api/technical/FormulaManage/GetProductResourceList?productType=' + ProductType.半成品, filterList, dataGrid)
        },
        getFormulaData(dataGrid) {
            var filterList = this.filterList.filter(item => item.key != 'techCode' && item.key != 'specification')
            return this.$http.post('/api/technical/FormulaManage/GetFormulaResourceList', filterList, dataGrid)
        },
        search(params: FilterTmp[]) {
            this.filterList = params.find((item) => item.table == 'default')?.filter ?? [] //单表查询
            this.refreshTable();
        },
        refreshTable() {
            this.isLoading = true;
            this.getData()
                .then((res: any) => {
                    this.dataList[this.selectedTab] = res.data;
                })
                .catch((err) => {
                    this.$kingzon_messager.showError(err);
                })
                .finally(() => {
                    this.isLoading = false;
                })
        },
        tabSelect($event) {
            this.selectedTab = $event.$el.dataset.theme;
        },
        chooseData(row) {
            this.$emit("chooseData", row);
        }
    }
})
</script>