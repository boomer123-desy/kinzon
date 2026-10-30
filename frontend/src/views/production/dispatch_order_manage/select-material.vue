<template>
  <Loading v-show="isLoading"></Loading>
  <div class="kingzon-page">
    <div class="kingzon-page-body" style="flex-direction:row;flex-wrap: wrap;">
      <Panel style="width: 24%;height: 100%;">
        <template #header>
          <div style="line-height:21px;text-align: center; ">已选物料</div>
        </template>
        <div v-for="(item, index) in getSeletions" :key="index" style="display: flex;justify-content:left;">
          <div style="width: calc(100% - 35px);"
            :title="'【' + ProductType[item.productType] + '】' + item.productNo + item.productName"
            class='kingzon-textEllipsis'>
            【{{ ProductType[item.productType] }}】 {{ item.productNo + '-' + item.productName }}</div>
          <div style="width: 30px;">
            <LinkButton iconCls="icon-clear" style="height: 24px;" :plain="true" @click="deleteOption(item)">
            </LinkButton>
          </div>
        </div>
      </Panel>
      <Tabs :plain="true" style="height: 100%;width: 76%;" ref="tabs" @tabSelect="tabSelect">
        <TabPanel v-for="(item, index) in productTypeList" :key="index" :title="item.text">
          <div class="kingzon-page-content" style="width: 100%;">
            <SearchPanel :searchParams="searchParams" :searchParamsHidden="searchParamsHidden" @search="search"
              style="margin: 5px;">
            </SearchPanel>
            <KingZonDataGrid :id="`selectMaterialDataGrid_${index}`" class="kingzon-page-fill"
              :ref="'dataGrid' + item.value" emptyMsg="没有符合条件的数据" :data="data[item.value as ProductType]"
              :columnResizing="true" @pageChange="getDataAndRefresh" :isCheckBoxSelection="true"
              :leftGroupRowWidth="460" :rightGroupRowWidth="260" @sortChange="getDataAndRefresh"
              :sorts="[{ field: 'productTechnicalCode', order: 'asc' }]" :idField="'id'"
              @seletedRowsChanged="onCheckBoxClicked" :virtualScroll="true" :virtualPageSize="20"
              :initSelectedRows="selectedProduct">
              <template #left>
                <GridColumn field="index" align="center" width="40px">
                  <template #body="scope">
                    <span>{{ scope.rowIndex + 1 }}</span>
                  </template>
                </GridColumn>
                <GridColumn field="productTechnicalCode" title="技术型号" width="100" :overflowTextEllipsis="true"
                  :sortable="true">
                </GridColumn>
                <GridColumn field="productNo" title="编码" width="120" :sortable="true">
                </GridColumn>
                <GridColumn field="productName" title="名称" width="150" :overflowTextEllipsis="true"></GridColumn>
              </template>
              <template #center>
                <GridColumn :field="item.value != ProductType.原材料 ? 'productSpecification' : 'materialSpecification'"
                  title="规格型号" width="120" :overflowTextEllipsis="true">
                </GridColumn>
                <GridColumn field="materialGrade" title="材质牌号" width="120" v-if="item.value == ProductType.原材料"
                  :overflowTextEllipsis="true">
                </GridColumn>
                <GridColumn field="productPackageWay" title="包装方式" align="left" width="100"
                  v-if="item.value == ProductType.成品" :overflowTextEllipsis="true">
                </GridColumn>
                <GridColumn field="productCategory" title="类别" width="120" :overflowTextEllipsis="true">
                </GridColumn>
                <GridColumn field="remark" title="物料备注" width="120" :overflowTextEllipsis="true"></GridColumn>
                <GridColumn v-for="para in paraList" :key="para.systemPara.id" :field="para.systemPara.id.toString()"
                  :title="para.systemPara.dataValue" width="100">
                  <template #body="scope">
                    <span v-if="scope.row.customAttributeList" class="kingzon-textEllipsis"
                      :title="scope.row.customAttributeList.find(s => s.systemParaId === para.systemPara.id)?.value">
                      {{scope.row.customAttributeList.find(s => s.systemParaId === para.systemPara.id)?.value}}</span>
                  </template>
                </GridColumn>
              </template>
              <template #right>
                <GridColumn field="productUnit" title="单位" width="50" :overflowTextEllipsis="true"></GridColumn>
                <GridColumn field="totalStockQuantity" title="总库存" width="110" align="right">
                  <template #body="scope">
                    <div style="display: flex;flex-direction: column;">
                      <div style="z-index: 999;">
                        <span
                          :style="{ background: scope.row.abnormalQuantity ? 'white' : 'transparent', padding: scope.row.abnormalQuantity ? '0 0 0 2px' : '0' }">
                          {{ $utils.formatThousandSeparator(Number((scope.row.abnormalQuantity +
                            scope.row.inventoryQuantity).toFixed(2)))
                          }}
                        </span>
                      </div>
                      <span v-if="scope.row.abnormalQuantity" style=" margin-top:-18px;display: flex;flex-direction: column;font-size:
                                    11px;text-align: left;">
                        <span style="color: var(--color-red);">
                          异{{ $utils.formatThousandSeparator(scope.row.abnormalQuantity) }}
                        </span>
                        <span style="color: var(--color-green);">
                          正{{ $utils.formatThousandSeparator(scope.row.inventoryQuantity) }}
                        </span>
                      </span>
                    </div>
                  </template>
                </GridColumn>
                <GridColumn field="occupyInventory" title="占用库存" width="100" align="right">
                </GridColumn>
              </template>
            </KingZonDataGrid>
          </div>
        </TabPanel>
      </Tabs>
    </div>
    <div class="kingzon-btn-group kingzon-page-footer">
      <LinkButton @click="$emit('close')">取消</LinkButton>
      <LinkButton btnCls="c8" @click="nextStep">确定</LinkButton>
    </div>
  </div>
</template>

<script lang="ts">
import { defineComponent } from 'vue';
import { Filter, Search, type FilterTmp, type SelectOption } from '@/types/types';
import { CategoryType, DataType, FilterType, ProductType, ReviewStatus, SystemParaType } from '@/types/enums';
import { cloneDeep } from 'lodash';
import type { HttpResponseType } from '@/types/httpResponse';
import type { SystemParaTreeDto } from '@/types/common';
import type { GetProductListByProductionDto } from '@/types/production';

export default defineComponent({
  props: ["selectList", "furmulaMaterialIds"],
  emits: ['close', 'next'],
  components: {},
  data() {
    return {
      isLoading: false,
      searchParams: [] as Search[],
      searchParamsHidden: [] as Search[],
      data: {} as { [key in ProductType]: GetProductListByProductionDto[]; },
      selectedProduct: [] as GetProductListByProductionDto[],
      categoryOptions: [] as SelectOption[],
      packageOptions: [] as SelectOption[],
      filterList: [] as Filter[],
      isShowUnPlan: true,
      productTypeList: [] as SelectOption[],
      selectedTab: 0 as ProductType,
      paraList: [] as SystemParaTreeDto[],
    }
  },
  computed: {
    ProductType() {
      return ProductType;
    },

    getSeletions() {
      var list = this.selectedProduct.filter(x => x.id);
      return [...new Map(list.map(item => [item.id, item])).values()];
    },
  },
  created() {
    this.selectedProduct = cloneDeep(this.selectList);
    this.productTypeList = this.$utils.getEnumKeyValueArray(ProductType)
    this.productTypeList.forEach(s => {
      s.value = Number(s.value);
    });
    this.selectedTab = this.productTypeList[0].value as ProductType;
  },
  mounted() {
    this.init();
  },
  methods: {
    init() {
      this.isLoading = true;
      this.getPackageWayList()
        .then((res: any) => {
          this.packageOptions = res.data;
          return this.getProductCategoryList()
        }).then((res: any) => {
          this.categoryOptions = res.data.map((item: any) => {
            return { value: item.id, text: item.name } as SelectOption;
          });
          return this.getCustomParas();
        }).then((res: any) => {
          this.paraList = res.data;
          return this.getData()
        }).then((res: any) => {
          this.data[this.selectedTab] = res.data as GetProductListByProductionDto[];
          this.addSearchParam();
        })
        .catch((err) => {
          this.$kingzon_messager.showError(err);
        }).finally(() => {
          this.isLoading = false;
        });
    },

    getData() {
      this.filterList = this.filterList.filter(x => x.key != "isDeleted" && x.key != 'productType' && x.key != 'reviewStatus' && x.key != 'productId');
      this.filterList.push(new Filter('isDeleted', false, FilterType.Equals, DataType.SelectBoolean));
      this.filterList.push(new Filter('productType', Number(this.selectedTab), FilterType.Equals, DataType.SelectNum));
      this.filterList.push(new Filter('reviewStatus', ReviewStatus.评审通过, FilterType.Equals, DataType.SelectNum));
      var postData = {
        filterList: this.filterList,
        productIds: this.furmulaMaterialIds
      }
      return this.$http.post('/api/production/DispatchOrderManage/GetProductListByProduction', postData, this.$refs['dataGrid' + this.selectedTab]);
    },

    getDataAndRefresh(): void {
      this.isLoading = true;
      this.getData()
        .then((res: any) => {
          this.data[this.selectedTab] = res.data as GetProductListByProductionDto[];
        })
        .catch((err) => {
          this.$kingzon_messager.showError(err);
        }).finally(() => {
          this.isLoading = false;
        });
    },

    getProductCategoryList(): Promise<HttpResponseType> {
      var type = this.selectedTab == ProductType.原材料 ? CategoryType.原材料类别 : CategoryType.产品类别;
      return this.$http.post('/api/base/Category/GetChildrenCategoryListByType', [type]);
    },


    getPackageWayList(): Promise<HttpResponseType> {
      return this.$http.get('/api/base/Product/GetAllPackageWay');
    },


    getCustomParas() {
      var type = this.selectedTab == ProductType.原材料 ? SystemParaType.物料附加参数 : SystemParaType.产品附加参数;
      return this.$http.get('/api/base/SystemPara/GetAdditionalParam', { type: type });
    },

    //构建查询面板内容
    addSearchParam(): void {
      this.searchParams = [];
      this.searchParamsHidden = [];
      this.searchParams.push(new Search('productTechnicalCode', '技术型号', 'default'));
      this.searchParams.push(new Search('productNo', '编码', 'default'));
      this.searchParams.push(new Search('productName', '名称', 'default'));
      this.searchParamsHidden.push(new Search(this.selectedTab == ProductType.原材料 ? 'materialSpecification' : 'productSpecification', '规格型号', 'default'));
      if (this.selectedTab == ProductType.原材料) {
        this.searchParamsHidden.push(new Search('materialGrade', '材质牌号', 'default'));
      }
      if (this.selectedTab == ProductType.成品) {
        this.searchParamsHidden.push(new Search('packageWayId', '包装方式', 'default', FilterType.Equals, DataType.SelectNum, "", this.packageOptions));
      }
      this.searchParamsHidden.push(new Search('productCategoryId', '类别', 'default', FilterType.Equals, DataType.SelectNum, "", this.categoryOptions));
      this.searchParamsHidden.push(new Search('remark', '备注', 'default'));
    },

    search(params: FilterTmp[]) {
      this.filterList = params.find((item) => item.table == 'default')?.filter ?? [];
      this.getDataAndRefresh();
    },

    onCheckBoxClicked(rows) {
      rows.forEach((row: GetProductListByProductionDto) => {
        var index = this.selectedProduct.findIndex((item: GetProductListByProductionDto) => item.id === row.id);
        if (index === -1) {
          this.selectedProduct.push(row);
        }
      });
      //console.log('selectedProduct', rows, this.selectedProduct);
      this.selectedProduct.filter(s => Number(s.productType) === Number(this.selectedTab)).forEach((row: GetProductListByProductionDto) => {
        var index = rows.findIndex((item: GetProductListByProductionDto) => item.id === row.id);
        if (index === -1) {
          this.selectedProduct = this.selectedProduct.filter((item: GetProductListByProductionDto) => item.id !== row.id);
        }
      });
    },

    nextStep() {
      if (this.selectedProduct.length === 0) {
        this.$kingzon_messager.showError('请选择物料');
        return;
      }
      this.$emit('next', this.selectedProduct);
    },

    deleteOption(item: GetProductListByProductionDto) {
      this.selectedProduct = this.selectedProduct.filter((i: GetProductListByProductionDto) => i.id !== item.id);
      var table = this.$refs['dataGrid' + this.selectedTab] as EasyUI.DataGrid;
      table = table[0] ?? table;
      table.selectedRows = table.selectedRows.filter((i: GetProductListByProductionDto) => i.id !== item.id);
      var entity = this.data[this.selectedTab].find((i: GetProductListByProductionDto) => i.id === item.id);
      if (entity) {
        entity.selected = false; // 确保从数据源中移除选中状态
      }
    },


    tabSelect(tab) {
      this.selectedTab = ProductType[tab.title as string] as ProductType;
      this.filterList = [];
      this.$nextTick(() => {
        this.init();
      })
    }
  }
})
</script>
