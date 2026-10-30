<template>
  <Loading v-show="isLoading"></Loading>
  <div class="kingzon-page-content">
    <KingZonShowForm :formData="formData" :columnWidth="'25%'"></KingZonShowForm>
    <SearchPanel :searchParams="searchParams" @search="search" :showIcon="false">
    </SearchPanel>
    <KingZonDataGrid id="operationDetailDataGrid" :showDownLoad="true" :data="records" :pagination="true"
      class="kingzon-page-fill" ref="dataGrid" :exportFileName="'完工记录'" @pageChange="getDataAndRefresh"
      @sortChange="getDataAndRefresh" :showFooter="true" :footerData="footerData">
      <GridColumnGroup align="left" :frozen="true" width="150">
        <GridHeaderRow>
          <GridColumn align="center" cellCss="datagrid-td-rownumber" width="40">
            <template #body="scope">
              {{ scope.rowIndex + 1 }}
            </template>
          </GridColumn>
          <GridColumn field="recordNo" title="报工记录" width="110" align="left" :sortable="true"
            :overflowTextEllipsis="true">
          </GridColumn>
        </GridHeaderRow>
      </GridColumnGroup>
      <GridColumnGroup :columnMoving="true" align="center">
        <GridHeaderRow>
          <GridColumn :rowspan="2" field="operateUserName" title="报工人" width="70" align="left"
            :overflowTextEllipsis="true">
          </GridColumn>
          <GridColumn :colspan="2" title="生产单位" align="center" width="120">
          </GridColumn>
          <GridColumn :colspan="2" title="收入计价单位" align="center" width="120">
          </GridColumn>

          <GridColumn :colspan="2" title="产品单位" align="center" width="120">
          </GridColumn>
          <GridColumn :colspan="2" title="成品主单位" align="center" width="120">
          </GridColumn>
          <GridColumn field="price" :rowspan="2" title="加工单价" align="right" width="80" :sortable="true">
          </GridColumn>
          <GridColumn field="totalPrice" :rowspan="2" title="合计收入" align="right" width="100" :sortable="true">
          </GridColumn>
          <GridColumn :rowspan="2" field="createUserName" title="创建人" width="70" align="left"
            :overflowTextEllipsis="true">
          </GridColumn>
          <GridColumn :rowspan="2" field="createTime" title="报工时间" align="left" width="150" :sortable="true">
            <template #body="scope">
              {{ $utils.formatDateTime(scope.row.createTime, 'yyyy-MM-dd HH:mm:ss') }}
            </template>
          </GridColumn>
          <GridColumn field="remark" :rowspan="2" title="备注" align="left" width="150" :overflowTextEllipsis="true">
          </GridColumn>
        </GridHeaderRow>
        <GridHeaderRow>
          <!-- 生产单位 -->
          <GridColumn field="operateQuantity" title="报工数量" align="right" width="90" :sortable="true">
          </GridColumn>
          <GridColumn field="dispatchUnitInfo.productionUnitName" title="单位" align="left" width="100">
          </GridColumn>

          <!-- 计价单位 -->
          <GridColumn field="incomeQuantity" title="报工数量" align="right" width="90" :sortable="true">
          </GridColumn>
          <GridColumn field="dispatchUnitInfo.incomeUnitName" title="单位" align="left" width="80">
          </GridColumn>
          <!-- 产品单位 -->
          <GridColumn field="baseQuantity" title="报工数量" align="right" width="90" :sortable="true">
          </GridColumn>
          <GridColumn field="dispatchUnitInfo.baseUnitName" title="单位" align="left" width="80">
          </GridColumn>
          <!-- 主计量单位 -->
          <GridColumn field="mainQuantity" title="报工数量" align="right" width="90" :sortable="true">
          </GridColumn>
          <GridColumn field="dispatchUnitInfo.mainUnitName" title="单位" align="left" width="80">
          </GridColumn>

        </GridHeaderRow>
      </GridColumnGroup>
    </KingZonDataGrid>
  </div>
</template>

<script lang="ts">

import { DataType, FilterType, OperateType } from '@/types/enums';
import type { GetDispatchOrderDto } from '@/types/production';
import { Filter, Search, ShowFormData, type FilterTmp } from '@/types/types';
import { defineComponent } from 'vue';

export default defineComponent({
  props: {
    selectRow: {
      type: Object as () => GetDispatchOrderDto,
      required: true,
      default: {} as any
    },
    operateType: {
      type: Number as () => OperateType,
      default: OperateType.接单
    }
  },
  computed: {
    footerData() {
      if (this.records) {
        return [{
          index: '合计',
          totalPrice: this.records.reduce((sum, row) => sum + (row.totalPrice ?? 0), 0).toFixed(4),
          operateQuantity: this.records.reduce((sum, row) => sum + (row.operateQuantity ?? 0), 0).toFixed(4),
          baseQuantity: this.records.reduce((sum, row) => sum + (row.baseQuantity ?? 0), 0).toFixed(4),
          incomeQuantity: this.records.reduce((sum, row) => sum + (row.incomeQuantity ?? 0), 0).toFixed(4),
        }]
      }
      return [{
        index: '合计'
      }]
    },
  },
  data() {
    return {
      isLoading: false,
      searchParams: [] as Search[],
      filterList: [] as Filter[],
      formData: [] as ShowFormData[],
      records: [] as any[],
    };
  },
  created() {
    this.formData = [
      new ShowFormData('技术型号', this.selectRow?.technicalCode ?? ""),
      new ShowFormData('产品编码', this.selectRow?.productNo ?? ""),
      new ShowFormData('产品名称', this.selectRow?.productName ?? ""),
      new ShowFormData('规格型号', this.selectRow?.specification ?? ""),
      new ShowFormData('外包装方案', this.selectRow?.packageSpecification ?? ''),
      new ShowFormData('派工部门', this.selectRow?.departmentName ?? ''),
      new ShowFormData('派工数量', this.selectRow?.dispatchQuantity + (this.selectRow?.dispatchUnitInfo?.productionUnitName ?? '')),
      // new ShowFormData('派工数量', '生产单位：' + this.selectRow?.dispatchQuantity + (this.selectRow?.dispatchUnitInfo?.productionUnitName ?? '') + '; 产品单位：' +
      //   this.selectRow?.baseQuantity + (this.selectRow?.dispatchUnitInfo?.baseUnitName ?? '') + '; 加工计价单位：' +
      //   this.selectRow?.incomeQuantity + (this.selectRow?.dispatchUnitInfo?.incomeUnitName ?? '')),
      new ShowFormData('加工单价', (this.selectRow?.price ?? 0) + '元/' + (this.selectRow?.dispatchUnitInfo?.incomeUnitName ?? ''))] as ShowFormData[];
  },
  mounted() {
    this.init();
  },
  methods: {

    init() {
      this.addSearchParam();
      this.getDataAndRefresh();
    },


    getData() {
      var filters = [
        new Filter('operateType', this.operateType, FilterType.Equals, DataType.SelectNum),
        new Filter('dispatchOrderId', this.selectRow?.id, FilterType.Equals, DataType.SelectNum),
      ];
      this.filterList.push(...filters);
      var postData = {
        isPage: true,
        filterList: this.filterList
      };
      return this.$http.post("/api/production/OperationRecordManage/GetOperationRecordList", postData, this.$refs.dataGrid);
    },


    getDataAndRefresh() {
      this.isLoading = true;
      this.getData()
        .then(res => {
          this.records = res.data as any[];
        })
        .catch(err => {
          this.$kingzon_messager.showError(err);
        })
        .finally(() => {
          this.isLoading = false;
        })
    },


    //查询面板
    addSearchParam(): void {
      this.searchParams.push(new Search('recordNo', '报工编号'));
      this.searchParams.push(new Search('operateUserName', '报工人'));
      this.searchParams.push(new Search('createUserName', '创建人'));
      this.searchParams.push(new Search('createTime', "报工时间", "default", FilterType.GreaterThanOrEqual, DataType.DateTime, undefined, undefined, '开始时间'));
      this.searchParams.push(new Search('createTime', "", "default", FilterType.LessThanOrEqual, DataType.DateTime, undefined, undefined, '结束时间'));
    },

    //查询方法
    search(params: FilterTmp[]) {
      this.filterList = params.find((item) => item.table == 'default')?.filter ?? [] //单表查询
      this.getDataAndRefresh();
    },
  },
});
</script>

<style scoped></style>
