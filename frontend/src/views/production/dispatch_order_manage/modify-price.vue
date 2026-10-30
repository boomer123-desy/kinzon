<template>
  <Loading v-show="isLoading"></Loading>
  <div class="kingzon-page-content">
    <KingZonShowForm :formData="formData" :columnWidth="'25%'"></KingZonShowForm>
    <div style="width: 100%;display: flex;justify-content: center;height: 30px;margin: 5px 0;">
      <Label style="width: auto;">更新加工单价:</Label>
      <NumberBox style="width: 400px;" v-model="price" :spinners="true" :min="0" @valueChange="modifyPrice()"
        :precision="4">
        <Addon class="kingzon-input-addon">
          最新单价：{{ newPrice }}元/{{ selectRow?.dispatchUnitInfo?.incomeUnitName }}
        </Addon>
      </NumberBox>
      <div style="display: flex;align-items: center;" v-if="records.length">
        <label style="width: auto; margin: 0 10px;">统一更新</label>
        <CheckBox v-model="isBulkModify" @click="modifyPrice()">
        </CheckBox>
      </div>
    </div>
    <KingZonDataGrid id="modifyPriceDataGrid" :showDownLoad="true" :data="records" :pagination="false"
      class="kingzon-page-fill" ref="dataGrid" :exportFileName="'完工记录'" @pageChange="getDataAndRefresh"
      @sortChange="getDataAndRefresh" :showFooter="true" :footerData="footerData" :dblclickToEdit='true'
      editMode="cell">
      <GridColumnGroup align="left" :frozen="true" width="150">
        <GridHeaderRow>
          <GridColumn align="center" cellCss="datagrid-td-rownumber" width="40">
            <template #body="scope">
              {{ scope.rowIndex + 1 }}
            </template>
          </GridColumn>
          <GridColumn field="recordNo" title="报工编号" width="110" align="left" :overflowTextEllipsis="true"
            :sortable="true">
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

          <GridColumn field="price" :rowspan="2" title="加工单价" align="right" width="100" :sortable="true"
            :editable="true" :cellCss="'kingzon-cell-editable'">
            <template #edit="scope">
              <NumberBox v-model="scope.row.price" :spinners="true" :min="0" :precision="4">
              </NumberBox>
            </template>
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
    <div class="kingzon-btn-group kingzon-page-footer">
      <LinkButton @click="$emit('close')">取消</LinkButton>
      <LinkButton btnCls="c8" @click="submit">确定</LinkButton>
    </div>
  </div>
</template>

<script lang="ts">

import { DataType, FilterType, OperateType } from '@/types/enums';
import type { GetDispatchOrderDto } from '@/types/production';
import { Filter, ShowFormData } from '@/types/types';
import { defineComponent } from 'vue';

export default defineComponent({
  props: {
    selectRow: {
      type: Object as () => GetDispatchOrderDto,
      required: true,
      default: {} as any
    },
    newPrice: {
      type: Number,
      default: 0
    }
  },
  emits: ['close', 'submit'],
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
      formData: [] as ShowFormData[],
      records: [] as any[],
      price: undefined as number | undefined,
      isBulkModify: false,
    };
  },
  created() {
    this.price = this.selectRow?.price as number;
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
    this.getDataAndRefresh();
  },
  methods: {

    getData() {
      var filterList = [
        new Filter('operateType', OperateType.出单, FilterType.Equals, DataType.SelectNum),
        new Filter('dispatchOrderId', this.selectRow?.id, FilterType.Equals, DataType.SelectNum),
      ];
      var postData = {
        isPage: false,
        filterList: filterList
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


    modifyPrice() {
      if (this.isBulkModify) {
        this.records.forEach(x => {
          x.price = (this.price ?? 0);
          x.totalPrice = Number(((this.price ?? 0) * (x.incomeQuantity ?? 0)).toFixed(4));
        })
      }
    },



    submit() {
      this.$kingzon_messager.showConfim("是否确认更新加工单价")
        .then(() => {
          var postData = {
            id: this.selectRow?.id,
            price: this.price ?? 0,
            records: this.records.map(x => {
              return {
                id: x.id,
                price: x.price
              }
            })
          };
          this.isLoading = true;
          this.$http.post('/api/production/DispatchOrderManage/ModifyDispatchOrderPrice', postData)
            .then((res: any) => {
              this.$kingzon_messager.showInfo(res.data);
              this.$emit('submit');
            })
            .catch((err) => {
              this.$kingzon_messager.showError(err);
            })
            .finally(() => {
              this.isLoading = false;
            })
        })

    }

  },
});
</script>

<style scoped></style>
