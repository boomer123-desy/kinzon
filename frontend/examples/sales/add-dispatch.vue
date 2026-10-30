<template>
  <Loading v-show="isLoading"></Loading>
  <div class="kingzon-page-content">
    <div class="kingzon-btn-group" style="margin: 5px;display: flex;justify-content: left;">
      <LinkButton @click="selectProducts">选择产品</LinkButton>
      <LinkButton @click="clear">清空</LinkButton>
      <div style="width: calc(100% - 200px); display: flex;justify-content: center;">
        <Label style="width: auto;">统一派工时间:</Label>
        <DateBox v-model="formData.dispatchDate" format="yyyy-MM-dd" @valueChange="setAllDispatchDate"
          :panelStyle="{ width: '192px', height: '250px !important', maxHeight: '250px !important' }"
          style="width: 300px;">
        </DateBox>
        <Label style="width: auto;margin-left: 10px;">统一预计完工时间:</Label>
        <DateBox v-model="formData.planCompleteDate" format="yyyy-MM-dd" @valueChange="setAllCompleteDate"
          :panelStyle="{ width: '192px', height: '250px !important', maxHeight: '250px !important' }"
          style="width: 300px;">
        </DateBox>
        <Label style="width: auto;margin-left: 10px;">统一派工部门:</Label>
        <ComboBox v-model="formData.departmentId" :data="getDepartments(undefined)" style="width: 300px;"
          @selectionChange="setAllDepartments">
        </ComboBox>
      </div>
    </div>
    <KingZonDataGrid id="addDispatchDataGrid" ref="dataGrid" class="kingzon-page-fill" :data="orderList"
      :showDownLoad="true" :exportFileName="'派工数据'" :pagination="false" :dblclickToEdit='true' editMode="cell"
      :rowCss="getRowCss">
      <GridColumnGroup :frozen="true" width="430" align="left">
        <GridHeaderRow>
          <GridColumn align="center" width="40">
            <template #body="scope">
              {{ scope.rowIndex + 1 }}
            </template>
          </GridColumn>
          <GridColumn field="productTechnicalCode" title="技术型号" width="100" :overflowTextEllipsis="true">
          </GridColumn>
          <GridColumn field="productNo" title="产品编码" width="120">
          </GridColumn>
          <GridColumn field="productName" title="产品名称" width="170" :overflowTextEllipsis="true"></GridColumn>
        </GridHeaderRow>
      </GridColumnGroup>
      <GridColumnGroup :columnMoving="true" align="center">
        <GridHeaderRow>
          <GridColumn field="productSpecification" title="规格型号" width="120" :overflowTextEllipsis="true" :rowspan="2">
          </GridColumn>
          <GridColumn field="productType" title="类型" width="70" :rowspan="2">
            <template #body="scope">
              {{ ProductType[scope.row.productType] }}
            </template>
          </GridColumn>
          <GridColumn field="productPackageWay" title="包装方式" align="left" width="100" :overflowTextEllipsis="true"
            :rowspan="2">
          </GridColumn>
          <GridColumn field="productCategory" title="产品类别" width="120" :overflowTextEllipsis="true" :rowspan="2">
          </GridColumn>
          <GridColumn field="remark" title="产品备注" width="120" :overflowTextEllipsis="true" :rowspan="2"></GridColumn>
          <GridColumn field="packageId" title="外包装方案" width="120" :rowspan="2" :cellCss="'kingzon-cell-editable'"
            :editable="true">
            <template #body="scope">
              {{scope.row.pacakgeList && scope.row.pacakgeList.find(x => x.value == scope.row.packageId) ?
                scope.row.pacakgeList.find(x => x.value == scope.row.packageId)?.text : ''}}
            </template>
            <template #edit="scope">
              <ComboBox v-model="scope.row.packageId" :data="scope.row.pacakgeList"
                @selectionChange="onChangePackage($event, scope.row)">
                <Addon>
                  <span v-if="scope.row.packageId" class="textbox-icon icon-clear"
                    @click="scope.row.packageId = undefined"></span>
                </Addon>
              </ComboBox>
            </template>
          </GridColumn>
          <GridColumn field="dispatchDate" title="派工时间" width="120" :editable="true" :rowspan="2"
            :cellCss="'kingzon-cell-editable'">
            <template #body="scope">
              {{ scope.row.dispatchDate ? $utils.formatDateTime(scope.row.dispatchDate, "yyyy-MM-dd") : '' }}
            </template>
            <template #edit="scope">
              <DateBox v-model="scope.row.dispatchDate" format="yyyy-MM-dd" @valueChange="onChangeDispatchDate"
                :panelStyle="{ width: '192px', height: '250px !important', maxHeight: '250px !important' }"
                style="width: 100%;">
              </DateBox>
            </template>
          </GridColumn>
          <GridColumn field="planCompleteDate" title="预计完工时间" width="120" :editable="true" :rowspan="2"
            :cellCss="'kingzon-cell-editable'">
            <template #body="scope">
              {{ scope.row.planCompleteDate ? $utils.formatDateTime(scope.row.planCompleteDate, "yyyy-MM-dd") : '' }}
            </template>
            <template #edit="scope">
              <DateBox v-model="scope.row.planCompleteDate" format="yyyy-MM-dd" @valueChange="onChangePlanCompleteDate"
                :panelStyle="{ width: '192px', height: '250px !important', maxHeight: '250px !important' }"
                style="width: 100%;">
              </DateBox>
            </template>
          </GridColumn>
          <GridColumn field="departmentId" title="派工部门" width="100" :overflowTextEllipsis="true" :editable="true"
            :rowspan="2" :cellCss="'kingzon-cell-editable'">
            <template #body="scope">
              {{departmentList.find(x => x.value == scope.row.departmentId) ? departmentList.find(x => x.value ==
                scope.row.departmentId)?.text : ''}}
            </template>
            <template #edit="scope">
              <ComboBox v-model="scope.row.departmentId" :data="scope.row.departmentList"
                @selectionChange="onChangeDepartment($event, scope.row)">
              </ComboBox>
            </template>
          </GridColumn>
          <GridColumn field="waitProductionList" title="待生产数量" width="100" align="right" :rowspan="2">
            <template #body="scope">
              <div v-for="(item, index) in scope.row.waitProductionList" :key="index">
                {{ item.quantity }}{{ item.unitName.replace(/^"|"$/g, '') }}
              </div>
              <div v-if="scope.row.waitProductionList.length <= 0">
                /
              </div>
            </template>
          </GridColumn>
          <GridColumn :colspan="2" title="生产单位" align="center">
          </GridColumn>
          <GridColumn :colspan="2" title="收入计价单位" align="center">
          </GridColumn>
          <GridColumn :colspan="2" title="产品单位" align="center">
          </GridColumn>
          <GridColumn :colspan="2" title="成品主单位" align="center">
          </GridColumn>
          <GridColumn v-for="para in productionParaList" :key="para.systemPara.id"
            :field="para.systemPara.id.toString()" :title="para.systemPara.dataValue" width="100" :rowspan="2"
            :editable="true" :cellCss="'kingzon-cell-editable'">
            <template #body="scope">
              <div class="kingzon-textEllipsis"
                :title="scope.row.productionAttributeList?.find(attr => attr.systemParaId === para.systemPara.id)?.value">
                {{scope.row.productionAttributeList?.find(attr => attr.systemParaId === para.systemPara.id)?.value}}
              </div>
            </template>
            <template #edit="scope">
              <TextBox
                :value="scope.row.productionAttributeList.find(attr => attr.systemParaId === para.systemPara.id).value"
                style="width: 100%;"
                @valueChange="scope.row.productionAttributeList.find(attr => attr.systemParaId === para.systemPara.id).value = $event.currentValue">
              </TextBox>
            </template>
          </GridColumn>
          <GridColumn field="orderRemark" title="派工备注" width="150" :overflowTextEllipsis="true" :rowspan="2"
            :editable="true" :cellCss="'kingzon-cell-editable'">
          </GridColumn>
        </GridHeaderRow>
        <GridHeaderRow>
          <!-- 生产单位 -->
          <GridColumn field="dispatchQuantity" title="派工数量" align="right" width="90" :editable="true"
            :cellCss="'kingzon-cell-editable'">
            <template #edit="scope">
              <NumberBox v-model="scope.row.dispatchQuantity" :spinners="true" :min="0" :precision="4"
                @valueChange="onChangeDispatchQuantity(scope.row)">
              </NumberBox>
            </template>
          </GridColumn>
          <GridColumn field="dispatchUnitInfo.productionUnitName" title="单位" align="left" :editable="true" width="100"
            :cellCss="'kingzon-cell-editable'">
            <template #edit="scope">
              <ComboBox v-model="scope.row.dispatchUnitInfo.unitConversionId" :data="scope.row.unitList"
                @valueChange="onChangeProductUnit($event, scope.row)">
              </ComboBox>
            </template>
          </GridColumn>

          <!-- 计价单位 -->
          <GridColumn field="incomeQuantity" title="派工数量" align="right" width="90" :overflowTextEllipsis="true">
          </GridColumn>
          <GridColumn field="dispatchUnitInfo.incomeUnitName" title="单位" align="left" width="80"
            :overflowTextEllipsis="true">
          </GridColumn>
          <!-- 产品单位 -->
          <GridColumn field="baseQuantity" title="派工数量" align="right" width="90" :overflowTextEllipsis="true">
          </GridColumn>
          <GridColumn field="dispatchUnitInfo.baseUnitName" title="单位" align="left" width="80"
            :overflowTextEllipsis="true">
          </GridColumn>
          <!-- 主计量单位 -->
          <GridColumn field="mainQuantity" title="派工数量" align="right" width="90" :overflowTextEllipsis="true">
          </GridColumn>
          <GridColumn field="dispatchUnitInfo.mainUnitName" title="单位" align="left" width="80"
            :overflowTextEllipsis="true">
          </GridColumn>
        </GridHeaderRow>
      </GridColumnGroup>
      <GridColumnGroup :frozen="true" align="right" width="630">
        <GridHeaderRow>
          <GridColumn field="materialDemands" title="领料需求" width="280">
            <template #body="scope">
              <div v-if="scope.row.materialDemands" class="kingzon-textEllipsis"
                :title="scope.row.materialDemands.materials.map(item => `【${ProductType[item.detailProductType ?? ProductType.成品]}】 ${item.detailType == FormulaDetailType.Formula ? item.formulaProductInfo?.productNo + '-' + item.formulaProductInfo?.productName : item.detailDataNo + '-' + item.detailDataName}：${item.demandQuantity}${item.unitList.find(x => x.value == item.unitConversionId)?.text}`).join('\n')">
                <div :style="{ color: item.detailType ? '' : 'var(--color-yellow)' }"
                  v-for="(item, index) in scope.row.materialDemands.materials" :key="index">
                  【{{ ProductType[item.detailProductType ?? ProductType.成品] }}】 <span
                    v-if="item.detailType == FormulaDetailType.Formula">{{ item.formulaProductInfo?.productNo }}-{{
                      item.formulaProductInfo?.productName }}</span><span v-else>{{ item.detailDataNo }}-{{
                      item.detailDataName }}</span>：{{
                      item.demandQuantity }}{{item.unitList.find(x =>
                    x.value == item.unitConversionId)?.text}}
                </div>
              </div>
            </template>
          </GridColumn>
          <GridColumn field="saleOrders" title="销售单" width="130">
            <template #body="scope">
              <div v-if="scope.row.saleOrders && scope.row.saleOrders.length > 0"
                :title="scope.row.saleOrders.map(item => `${item.orderNo}${item.customerName}：${item.dispatchQuantity}${item.dispatchUnit}`).join('\n')">
                <div v-for="(item, index) in scope.row.saleOrders" :key="index" style="white-space: break-spaces;">
                  {{ item.orderNo }}{{ item.customerName }}：{{ item.dispatchQuantity }}{{ item.dispatchUnit }}
                </div>
              </div>
            </template>
          </GridColumn>
          <GridColumn title="操作" align="center" width="220">
            <template #body="scope">
              <ButtonGroup style="height: 24px">
                <LinkButton @click="materialDemand(scope.row, scope.rowIndex)">领料需求</LinkButton>
                <LinkButton @click="selectSale(scope.row, scope.rowIndex)">销售单</LinkButton>
                <LinkButton @click="copyOrder(scope.row, scope.rowIndex)">复制</LinkButton>
                <LinkButton @click="deleteOrder(scope.rowIndex)">删除</LinkButton>
              </ButtonGroup>
            </template>
          </GridColumn>
        </GridHeaderRow>
      </GridColumnGroup>
    </KingZonDataGrid>
    <div class="kingzon-btn-group kingzon-page-footer">
      <LinkButton @click="$emit('close')">取消</LinkButton>
      <LinkButton btnCls="c8" @click="submit">确定</LinkButton>
    </div>
  </div>

  <!-- 物料需求 -->
  <Drawer @expand="$utils.updateModal()" v-if="isShowMaterialDrawer" @collapse="isShowMaterialDrawer = false;"
    ref="materialDemand" :title="'领料需求'" :drawerStyle="{ width: '98%' }" region="east" :maskClosable="false"
    :closable="true">
    <MaterialDemand :currentRow="currentRow" @close="materailDemandDrawer.collapse()" @submit="getMaterialDemands">
    </MaterialDemand>
  </Drawer>

  <!-- 销售单 -->
  <Drawer @expand="$utils.updateModal()" v-if="isShowSaleOrderDrawer" @collapse="isShowSaleOrderDrawer = false;"
    ref="saleOrder" :title="'工单关联销售单'" :drawerStyle="{ width: '98%' }" region="east" :maskClosable="false"
    :closable="true">
    <SelectSaleOrder :currentRow="currentRow" :selectedRows="currentRow.saleOrders" @close="saleOrderDrawer.collapse()"
      @submit="saleOrderSubmit">
    </SelectSaleOrder>
  </Drawer>

</template>

<script lang="ts">
import type { ProductIdDto, SystemParaTreeDto } from '@/types/common';
import { FormulaDetailType, InventoryDataStatus, InventoryDataType, ProductType, UnitConversionType, SystemParaType } from '@/types/enums';
import type { PostDispatchOrderDto } from '@/types/production';
import type { SelectOption } from '@/types/types';
import { cloneDeep } from 'lodash';
import { defineComponent } from 'vue';
import MaterialDemand from './material-demand.vue';
import SelectSaleOrder from './select-saleOrder.vue';
import Decimal from 'decimal.js';

export default defineComponent({
  props: ['selectList', 'selectdDepartment'],
  emits: ['close', 'submit', 'select'],
  data() {
    return {
      isLoading: false,
      orderList: [] as PostDispatchOrderDto[],
      unitList: [] as ProductIdDto[],
      departmentList: [] as any[],
      unitConversitions: [] as any[],
      formData: {
        departmentId: undefined,
        dispatchDate: new Date(),
        planCompleteDate: undefined,
      },
      isModifyDepartment: false,
      packageInfos: [] as ProductIdDto[],
      isModifyQuantity: false,
      isShowMaterialDrawer: false,
      materailDemandDrawer: {} as EasyUI.Drawer,
      currentRow: {} as PostDispatchOrderDto,
      currentRowIndex: 0,
      productionParaList: [] as Array<SystemParaTreeDto>,//扩展参数
      isShowSaleOrderDrawer: false,
      saleOrderDrawer: {} as EasyUI.Drawer,
    };
  },
  components: {
    MaterialDemand, SelectSaleOrder
  },
  computed: {
    ProductType() {
      return ProductType;
    },

    FormulaDetailType() {
      return FormulaDetailType;
    },

  },
  mounted() {

  },
  watch: {
    selectList: {
      handler() {
        this.init();
      },
      deep: true,
    }
  },
  created() {
    this.init();
  },
  methods: {
    init() {
      this.orderList = cloneDeep(this.selectList);
      this.isLoading = true;
      this.getDepartmentList()
        .then((res: any) => {
          this.departmentList = res.data;
          return this.getTotalUnitList();
        })
        .then((res) => {
          this.unitList = res.data as ProductIdDto[];
          return this.getProductPackageList();
        })
        .then((res) => {
          this.packageInfos = res.data as ProductIdDto[];
          return this.getExtendParamData()
        }).then((res) => {
          this.productionParaList = res.data as Array<SystemParaTreeDto>;
          return this.getUnitConversition();
        })
        .then(res => {
          this.unitConversitions = res.data as any;
          for (var row of this.orderList) {
            row.unitList = this.getProductUnits(row);
            row.pacakgeList = this.getPackageList(row);
            row.departmentList = this.getDepartments(row);
            var mainUnit = row.unitList.find((u: any) => u.isMainUnit);
            if (row.dispatchUnitInfo) {
              row.dispatchUnitInfo.mainUnitConversionId = mainUnit ? Number(mainUnit.value) : -1;
            }
            //如果只有一个单位，并且没有设置生产单位，则默认选择第一个单位
            if (row.dispatchUnitInfo && row.unitList.length == 1 && (row.dispatchUnitInfo?.unitConversionId == null || row.dispatchUnitInfo.unitConversionId == undefined)) {
              //console.log('row.unitList[0].value', row.unitList[0].value);
              row.dispatchUnitInfo.unitConversionId = row.unitList[0].value as number;
              this.changeProductUnit(row.unitList[0], row);
            }
            //如果只有一个部门，并且没有设置部门，则默认选择第一个部门
            var departmentList = row.departmentList;
            //console.log('departmentList', departmentList);
            if (departmentList.length == 1 && (row.departmentId == null || row.departmentId == undefined)) {
              var entity = this.selectdDepartment ? departmentList.find(s => s.value == this.selectdDepartment.value) : undefined;
              row.departmentId = entity ? entity.value as number : departmentList[0].value as number;
              this.onChangeDepartment(departmentList[0], row);
            }
            //扩展参数赋值
            row.productionAttributeList = this.productionParaList.map(x => {
              var entity = row.productionAttributeList?.find(s => s.systemParaId == x.systemPara.id);
              return {
                systemParaId: x.systemPara.id,
                value: entity ? entity.value : undefined
              }
            })
          }
        })
        .catch(err => {
          this.$kingzon_messager.showError(err);
        })
        .finally(() => {
          this.isLoading = false;
        })

    },

    getDepartmentList() {
      var products = this.orderList.map(x => {
        return {
          productId: x.productId,
          specificationId: x.specificationId,
        }
      })
      return this.$http.post('/api/production/DispatchOrderManage/GetDepartmentsByPrcessPrice', products);
    },


    getProductPackageList() {
      var productIdList = this.orderList.map(x => {
        return {
          idField: (x.productId + (x.specificationId ? ('_' + x.specificationId) : '')).toString(),
          productId: x.productId,
          specificationId: x.specificationId,
          isFilterGoods: false,
        }
      });
      return this.$http.post('/api/sale/SaleOrderManage/GetProductPackageList', productIdList)
    },


    getUnitConversition() {
      var postData = {
        productIds: this.orderList.map(x => x.productId),
        specificationIds: this.orderList.filter(x => x.specificationId).map(x => x.specificationId),
        processPriceIds: this.departmentList.map(x => x.id),
      };
      return this.$http.post('/api/sale/SaleMonthPlan/GetUnitConversionListByTargetIds', postData);
    },

    getTotalUnitList() {
      var productIdList = this.orderList.map(x => {
        return {
          idField: (x.productId + (x.specificationId ? ('_' + x.specificationId) : '')).toString(),
          productId: x.productId,
          specificationId: x.specificationId,
        }
      });
      return this.$http.post('/api/sale/SaleOrderManage/GetProductUnitList', productIdList)
    },


    getProductUnits(row) {
      var that = this;
      var idField = (row.productId + (row.specificationId ? ('_' + row.specificationId) : '')).toString();
      var list = that.unitList.find(x => x.idField == idField);
      var options = list ? (list.unitList ?? []).map((x) => {
        return {
          value: Number(x.code),
          text: x.text + (x.extension ? '【' + x.extension + '】' : ''),
          code: x.packageId,
          extension: x.unitId.toString(),
          isMainUnit: x.isMainUnit
        }
      }) as SelectOption[] : [];
      return options;
    },

    getPackageList(row) {
      var that = this;
      var idField = (row.productId + (row.specificationId ? ('_' + row.specificationId) : '') + (row.goodsId ? ('_' + row.goodsId) : '')).toString();
      var list = that.packageInfos.find(x => x.idField == idField) as ProductIdDto;
      var options = list ? (list.packageList ?? []).map(x => {
        return {
          value: x.id,
          text: x.packageSpecification,
          code: x.packageUnitId
        }
      }) as SelectOption[] : [];
      return options;
    },

    getDepartments(row) {
      var that = this;
      var list = row ? that.departmentList.filter(x => x.productId == row.productId && x.specificationId == row.specificationId) : that.departmentList;
      return [...new Map(list.map(item => [item.value, item])).values()];
    },

    setAllDispatchDate() {
      if (!this.formData.dispatchDate) {
        return;
      }
      this.orderList.forEach(x => {
        x.dispatchDate = new Date(this.formData.dispatchDate);
      })
    },


    setAllCompleteDate() {
      if (!this.formData.planCompleteDate) {
        return;
      }
      this.orderList.forEach(x => {
        x.planCompleteDate = this.formData.planCompleteDate ? new Date(this.formData.planCompleteDate) : undefined;
      })
    },

    onChangeDispatchDate() {
      var other = this.orderList.find(x => x.dispatchDate != this.formData.dispatchDate);
      if (this.formData.dispatchDate && other) {
        this.formData.dispatchDate = undefined as any;
      }
    },

    onChangePlanCompleteDate() {
      var other = this.orderList.find(x => x.planCompleteDate != this.formData.planCompleteDate);
      if (this.formData.planCompleteDate && other) {
        this.formData.planCompleteDate = undefined as any;
      }
    },


    setAllDepartments(event) {
      if (this.isModifyDepartment || !event) {
        return;
      }
      this.orderList.forEach(s => {
        var department = this.getDepartments(s).find(d => d.value == event.value);
        s.departmentId = department ? department.value : undefined;
        this.onChangeDepartment(department, s);
      })
    },

    //更新部门 =》更新计价单位
    onChangeDepartment(event, row) {
      this.isModifyDepartment = true;
      row.price = event ? event.price : 0;
      row.dispatchUnitInfo.incomeUnitId = event ? event.unitId : undefined;
      row.dispatchUnitInfo.incomeUnitName = event ? event.unitName : undefined;
      row.processPriceId = event ? event.id : undefined;
      row.dispatchUnitInfo.incomeUnitConversionId = event ? event.priceUnitConversionId : undefined;
      row.dispatchUnitInfo.incomeRatio = this.getIncomeRatio(row);
      row.incomeQuantity = Number(((row.dispatchUnitInfo.incomeRatio ?? 0) * (row.dispatchQuantity ?? 0)).toFixed(4));
      var other = this.orderList.find(x => x.departmentId != this.formData.departmentId);
      this.$nextTick(() => {
        if (this.formData.departmentId && other) {
          this.formData.departmentId = undefined;
        }
        this.isModifyDepartment = false;
      })
    },


    onChangeDispatchQuantity(row) {
      if (this.isModifyQuantity) {
        return;
      }
      this.isModifyQuantity = true;
      row.incomeQuantity = Number(((row.dispatchUnitInfo.incomeRatio ?? 0) * (row.dispatchQuantity ?? 0)).toFixed(4));
      row.baseQuantity = Number(((row.dispatchUnitInfo.baseRatio ?? 0) * (row.dispatchQuantity ?? 0)).toFixed(4));
      row.mainQuantity = Number(((row.dispatchUnitInfo.mainRatio ?? 0) * (row.dispatchQuantity ?? 0)).toFixed(4));

      //更新物料需求
      if (row.materialDemands && row.materialDemands.materials && row.materialDemands.materials.length > 0) {
        //转换为配方单位数量
        row.materialDemands.formulaQuantity = Number((row.dispatchQuantity * row.materialDemands.convRatio).toFixed(6));
        row.materialDemands.materials.filter(s => s.detailType && s.formulaUnitId).forEach(item => {
          this.handleMaterialDemandQty(item, row);
        })
      }

      this.$nextTick(() => {
        this.isModifyQuantity = false;
      })
    },


    //更新生产单位 =》更新主计量比例 、 更新产品单位比例 、更新计价单位比例
    onChangeProductUnit(info, row) {
      var event = row.unitList.find(x => x.value == info.currentValue);
      this.changeProductUnit(event, row);

      //更新物料需求
      if (row.materialDemands && row.materialDemands.formulaUnitInfo) {
        //console.log('row.materialDemands.formulaUnitInfo', row.materialDemands.formulaUnitInfo);
        this.getConvTotalQuantity(row.materialDemands.formulaUnitInfo, row.materialDemands.unitConversitions, row);
      }
    },


    //获取派工量转换的总用量
    getConvTotalQuantity(formulaUnitInfo, unitConversitions, row) {
      if (!formulaUnitInfo) {
        return;
      }
      //console.log('formulaUnitInfo', formulaUnitInfo, 'unitConversitions', unitConversitions);
      var conversion = unitConversitions.find(s => s.id == formulaUnitInfo.value);
      //console.log('conversion', conversion);
      if (!conversion && formulaUnitInfo) {
        conversion = {
          id: formulaUnitInfo.value,
          sourceId: row.specificationId ?? row.productId,
          targetId: row.productId,
          fromUnitId: Number(formulaUnitInfo.extension),
          toUnitId: row.dispatchUnitInfo.baseUnitId,
          ratio: 1,
          conversionType: UnitConversionType.Product
        };
      }
      //productionRatio * 基础单位 = 派工单位
      var productionRatio = this.getBaseRatio(row, row.dispatchUnitInfo.baseUnitId, row.dispatchUnitInfo.productionUnitId, row.dispatchUnitInfo.unitConversionId);

      // formulaRatio * 基础单位 = 配方单位
      var formulaRatio = 0;
      //配方单位为成品次单位/成品包装单位,派工产品为规格产品，需多次转换
      if ((conversion.conversionType == UnitConversionType.Product || conversion.conversionType == UnitConversionType.PackageToProduct) && row.specificationId) {
        var item = cloneDeep(row);
        item.specificationId = null;
        //配方单位和成品主单位转换
        var formulaToMain = this.getBaseRatio(item, item.productMainUnitId, Number(formulaUnitInfo.extension), formulaUnitInfo.value, unitConversitions);
        //成品主单位和规格成品主单位转换
        var mainToSpec = unitConversitions.find(s => s.sourceId == item.specificationId && s.targetId == item.productId && s.conversionType == UnitConversionType.SpecToProduct);
        var mainToSpecRatio = mainToSpec ? mainToSpec.ratio : 0;
        formulaRatio = mainToSpecRatio != 0 ? formulaToMain / mainToSpecRatio : 0;
      }
      else {
        formulaRatio = this.getBaseRatio(row, row.dispatchUnitInfo.baseUnitId, Number(formulaUnitInfo.extension), formulaUnitInfo.value);
      }

      // convRatio * 配方单位 = 派工单位
      row.materialDemands.convRatio = formulaRatio !== 0 ? productionRatio / formulaRatio : 0;
      row.materialDemands.formulaQuantity = Number((row.dispatchQuantity * row.materialDemands.convRatio).toFixed(6));

      //更新每行物料需求
      row.materialDemands.materials.filter(s => s.detailType && s.formulaUnitId).forEach(item => {
        this.handleMaterialDemandQty(item, row);
      })
    },
    handleMaterialDemandQty(item, row) {
      var scale = Decimal.div(row.materialDemands.formulaQuantity, item.curBaseQuantity);
      var formulaQuantity = Decimal.mul(scale, item.curQuantity);
      //配方单位数量
      item.formulaQuantity = this.$utils.decimalPlaces4(Decimal.mul(formulaQuantity, item.formulaRatio), 6).toNumber();
      //需求数量 = 配方单位数量 * 转换比例
      item.demandQuantity = Number((item.formulaQuantity * item.convRatio).toFixed(6));
      //基础需求数量 = 需求数量 * 基础比例
      item.baseDemandQuantity = Number((item.demandQuantity * item.demandRatio).toFixed(6));
    },
    getBaseRatio(x, toUnitId, fromUnitId, id, unitConversitions?: any[]) {
      if (id < 0 || !id) {
        return 1;
      }
      var baseToMain = (unitConversitions || this.unitConversitions).find(u => u.sourceId == x.specificationId && u.conversionType == UnitConversionType.SpecToProduct);
      var baseToMainRatio = baseToMain ? baseToMain.ratio : 1;
      var ratioInfos = x.specificationId ? (unitConversitions || this.unitConversitions).filter(u => (u.targetId == x.specificationId &&
        (u.conversionType == UnitConversionType.ProductSpec ||
          u.conversionType == UnitConversionType.PackageToProductSpec) ||
        (u.targetId == x.productId && u.conversionType == UnitConversionType.PackageToProduct)) ||
        (u.sourceId == x.specificationId && u.conversionType == UnitConversionType.SpecToProduct)) :
        (unitConversitions || this.unitConversitions).filter(u => u.targetId == x.productId && (u.conversionType == UnitConversionType.Product ||
          u.conversionType == UnitConversionType.PackageToProduct));

      var mainConv = ratioInfos.find(r => r.toUnitId == toUnitId && r.fromUnitId == fromUnitId && r.id == id);
      //console.log(ratioInfos,baseToMainRatio,mainConv,'getBaseRatio')

      // if (mainConv && mainConv.conversionType == UnitConversionType.SpecToProduct) {
      //   return mainConv.ratio ? 1 / mainConv.ratio : 0;
      // }
      //生产单位为成品包装单位
      if (mainConv && x.specificationId && mainConv.conversionType == UnitConversionType.PackageToProduct) {
        return baseToMainRatio ? mainConv.ratio / baseToMainRatio : 0;
      }
      if (mainConv) {
        return mainConv.ratio;
      }
      //生产单位为成品主单位
      var revConv = ratioInfos.find(r => r.toUnitId == fromUnitId && r.fromUnitId == toUnitId && r.id == id);
      if (revConv && revConv.conversionType == UnitConversionType.SpecToProduct) {
        return revConv.ratio ? 1 / revConv.ratio : 0;
      }
      return 0;
    },


    getIncomeRatio(x) {
      if (!x.dispatchUnitInfo.incomeUnitId) {
        return 0;
      }
      if (x.dispatchUnitInfo.incomeUnitConversionId == x.dispatchUnitInfo.unitConversionId) {
        return 1;
      }
      var incomeToBaseRatio = this.getBaseRatio(x, x.dispatchUnitInfo.baseUnitId, x.dispatchUnitInfo.incomeUnitId, x.dispatchUnitInfo.incomeUnitConversionId);
      //console.log(incomeToBaseRatio, 'incomeToBaseRatio')
      return incomeToBaseRatio ? (x.dispatchUnitInfo.baseRatio ?? 0) / incomeToBaseRatio : 0;
    },


    changeProductUnit(event, row) {
      row.dispatchUnitInfo.productionUnitId = event ? Number(event.extension) : undefined;
      row.dispatchUnitInfo.productionUnitName = event ? event.text : undefined;
      if (!event) {
        row.dispatchUnitInfo.incomeRatio = 0;
        row.dispatchUnitInfo.baseRatio = 0;
        row.dispatchUnitInfo.mainRatio = 0;
      }

      var baseToMain = this.unitConversitions.find(u => u.sourceId == row.specificationId && u.conversionType == UnitConversionType.SpecToProduct);
      var baseToMainRatio = baseToMain ? baseToMain.ratio : 1;
      row.dispatchUnitInfo.baseRatio = this.getBaseRatio(row, row.dispatchUnitInfo.baseUnitId, row.dispatchUnitInfo.productionUnitId, row.dispatchUnitInfo.unitConversionId);
      row.dispatchUnitInfo.incomeRatio = this.getIncomeRatio(row);
      row.dispatchUnitInfo.mainRatio = row.dispatchUnitInfo.baseRatio * baseToMainRatio;

      row.incomeQuantity = Number(((row.dispatchUnitInfo.incomeRatio ?? 0) * (row.dispatchQuantity ?? 0)).toFixed(4));
      row.baseQuantity = Number(((row.dispatchUnitInfo.baseRatio ?? 0) * (row.dispatchQuantity ?? 0)).toFixed(4));
      row.mainQuantity = Number(((row.dispatchUnitInfo.mainRatio ?? 0) * (row.dispatchQuantity ?? 0)).toFixed(4));
    },




    //更新包装规格 =》更新生产单位
    onChangePackage(event, row) {
      if (this.isModifyQuantity || !event) {
        return;
      }
      this.isModifyQuantity = true;
      var unit = this.getProductUnits(row).find(x => x.code == event.value && Number(x.extension) == event.code);
      row.dispatchUnitInfo.unitConversionId = unit ? unit.value : undefined;
      this.$nextTick(() => {
        this.isModifyQuantity = false;
        this.onChangeProductUnit(unit, row);
      })
    },


    copyOrder(row, index) {
      var newRow = cloneDeep(row);
      this.orderList.splice(index + 1, 0, newRow);
    },


    deleteOrder(index) {
      this.orderList.splice(index, 1);
    },


    getRowCss(row) {
      var departmentList = this.getDepartments(row);
      if (departmentList.length <= 0) {
        return {
          'background-color': 'var(--color-yellow-bg-secondary)'
        }
      }
      return '';
    },


    selectProducts() {
      this.$emit('select', this.orderList);
    },


    clear() {
      this.orderList = [];
      this.$nextTick(() => {
        this.formData.dispatchDate = new Date();
        this.formData.departmentId = undefined;
        this.formData.planCompleteDate = undefined;
      })

    },

    materialDemand(row, index) {
      if (!row.dispatchQuantity || !row.dispatchUnitInfo.productionUnitId) {
        return this.$kingzon_messager.showError("请输入派工数量或单位");
      }
      this.currentRow = {
        ...row,
        departmentName: this.departmentList.find(x => x.value == row.departmentId)?.text,
        detailId: row.productId,
        materialDemands: row.materialDemands,
      };
      this.currentRowIndex = index;
      this.isShowMaterialDrawer = true;
      this.$nextTick(() => {
        this.materailDemandDrawer = this.$refs.materialDemand as EasyUI.Drawer;
        this.materailDemandDrawer.expand();
      })
    },

    getMaterialDemands(demands) {
      var currentRow = this.orderList[this.currentRowIndex];
      if (!currentRow) {
        return;
      }
      currentRow.materialDemands = demands;
      //console.log(demands);
      this.materailDemandDrawer.collapse();
    },



    submit() {
      if (this.orderList.length <= 0) {
        return this.$kingzon_messager.showError("请选择产品");
      }
      var nullDepartment = this.orderList.findIndex(x => !x.departmentId);
      if (nullDepartment >= 0) {
        return this.$kingzon_messager.showError("请输入第" + (nullDepartment + 1) + '条记录的派工部门');
      }
      var nullDisQuantity = this.orderList.findIndex(x => !x.dispatchQuantity);
      if (nullDisQuantity >= 0) {
        return this.$kingzon_messager.showError("请输入第" + (nullDisQuantity + 1) + '条记录的生产单位的派工数量');
      }
      var nullDisUnit = this.orderList.findIndex(x => !x.dispatchUnitInfo?.productionUnitId);
      if (nullDisUnit >= 0) {
        return this.$kingzon_messager.showError("请选择第" + (nullDisUnit + 1) + '条记录的生产单位');
      }
      var nullIncomeUnit = this.orderList.findIndex(x => !x.dispatchUnitInfo?.incomeUnitId || !x.incomeQuantity);
      if (nullIncomeUnit >= 0) {
        return this.$kingzon_messager.showError("请在【加工单价管理】功能输入第" + (nullIncomeUnit + 1) + '条记录的收入计价单位');
      }
      var postData = cloneDeep(this.orderList);
      postData.forEach(x => {
        var mainUnit = x.unitList.find((u: any) => u.isMainUnit);
        if (x.dispatchUnitInfo) {
          x.dispatchUnitInfo.mainUnitConversionId = mainUnit ? Number(mainUnit.value) : -1;
        }
        x.dispatchUnit = JSON.stringify(x.dispatchUnitInfo);
        x.materialDemands = x.materialDemands ? {
          formulaVersionId: x.materialDemands.formulaVersionId,
          formulaUnitId: x.materialDemands.formulaUnitInfo ? Number(x.materialDemands.formulaUnitInfo.extension) : undefined,
          formulaConversionId: x.materialDemands.formulaUnitInfo ? Number(x.materialDemands.formulaUnitInfo.value) : undefined,
          formulaQuantity: x.materialDemands.formulaQuantity,
          convRatio: x.materialDemands.convRatio,
          materials: x.materialDemands.materials.map(m => {
            return {
              dataStatus: m.dataStatus ?? InventoryDataStatus.None,
              dataId: m.specificationId ? m.specificationId : m.detailType != FormulaDetailType.Formula ? m.detailId : m.formulaProductInfo?.productId,
              dataType: m.detailProductType == ProductType.成品 ? m.specificationId ? InventoryDataType.Specification : InventoryDataType.Product : InventoryDataType.Product,
              demandQuantity: m.demandQuantity,
              baseDemandQuantity: m.baseDemandQuantity,
              unitId: m.unitId,
              unitConversionId: m.unitConversionId,
              detailRemark: m.detailRemark,
              convUnitInfo: m.convUnitInfo,
              supplierId: m.supplierId > 0 ? m.supplierId : undefined,
              //配方信息
              formulaQuantity: m.formulaQuantity,
              formulaUnitId: m.formulaUnitId,
              formulaConversionId: m.formulaConversionId,
              convRatio: m.convRatio, // 派工量
            }
          })
        } : undefined;
      })

      var noDemandIndex = this.orderList.findIndex(x => !x.materialDemands || x.materialDemands.materials.length <= 0);
      if (noDemandIndex >= 0) {
        this.$kingzon_messager.showConfim("第" + (noDemandIndex + 1) + "条记录没有领料需求，是否继续提交")
          .then(() => {
            return this.submitData(postData);
          })
      }
      else {
        return this.submitData(postData);
      }
    },


    submitData(postData) {
      this.isLoading = true;
      this.$http.post("/api/production/DispatchOrderManage/AddDispatchOrderList", postData)
        .then((res: any) => {
          this.$kingzon_messager.showInfo(res.data);
          this.$emit("submit");
        })
        .catch(err => {
          this.$kingzon_messager.showError(err);
        })
        .finally(() => {
          this.isLoading = false;
        })
    },
    getExtendParamData() {
      return this.$http.post('/api/base/SystemPara/GetAdditionalParamByTypes', [SystemParaType.生产物料信息]);
    },
    selectSale(row, index) {
      if (!row.dispatchQuantity || !row.dispatchUnitInfo.productionUnitId) {
        return this.$kingzon_messager.showError("请输入派工数量或单位");
      }
      this.currentRow = row;
      this.currentRowIndex = index;
      this.isShowSaleOrderDrawer = true;
      this.$nextTick(() => {
        this.saleOrderDrawer = this.$refs.saleOrder as EasyUI.Drawer;
        this.saleOrderDrawer.expand();
      })
    },
    saleOrderSubmit(saleOrders) {
      if (!this.currentRow) {
        return;
      }
      this.currentRow.saleOrders = saleOrders;
      this.saleOrderDrawer.collapse();
    }
  },
});
</script>

<style scoped></style>
