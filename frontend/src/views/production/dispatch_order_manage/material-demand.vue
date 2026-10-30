<template>
  <Loading v-if="isLoading"></Loading>
  <div class="kingzon-page-content">
    <KingZonShowForm :formData="formData" :columnWidth="'25%'"></KingZonShowForm>
    <Form ref="form" :model="submitData" :rules="rules" :messages="messages" labelAlign="right" :labelWidth="100"
      style="width:100%;justify-content: center;" class="kingzon-page-form" @validate="errors = $event">
      <FormField name="formulaVersionId" label="配方:" style="width:500px;">
        <ComboBox v-model="submitData.formulaVersionId" :data="formulas" @selectionChange="getFormulaById">
          <Addon>
            <span v-if="submitData.formulaVersionId" class="textbox-icon icon-clear" @click="cleanFormula"></span>
          </Addon>
        </ComboBox>
      </FormField>
      <FormField name="formulaUnitConversionId" label="配方单位:" style="width:300px;">
        <ComboBox v-model="submitData.formulaUnitConversionId" :data="formulaUnits"
          @selectionChange="getConvTotalQuantity">
        </ComboBox>
      </FormField>
    </Form>
    <div class="kingzon-btn-group" style="margin-top: -30px;margin-bottom: 5px;margin-left: 5px;">
      <LinkButton @click="showSeleteDrawer">选择其他物料</LinkButton>
    </div>
    <TreeGrid class="kingzon-page-fill" :data="materialDetails" treeField="detailType" :selectionMode="'none'" id="id"
      :dblclickToEdit="true" editMode="cell" :pagination="false" ref="treeGrid" :columnResizing="true" :lazy="true">
      <GridColumnGroup :frozen="true" align="left" width="470">
        <GridHeaderRow>
          <GridColumn field="detailType" title="类型" width="200" align="left">
            <template #body="scope">
              <div class="kingzon-textEllipsis" :title="!scope.row.detailType ? '其他物料' : scope.row.detailType == FormulaDetailType.Formula ? '配方【' + scope.row.detailDataNo + '-' + scope.row.detailDataName + '】' :
                ProductType[scope.row.detailProductType]">
                {{ !scope.row.detailType && scope.row.children.length ? "其他物料" : scope.row.detailType ==
                  FormulaDetailType.Formula ? '配方【' + scope.row.detailDataNo + '-' + scope.row.detailDataName + '】' :
                  ProductType[scope.row.detailProductType] }}
              </div>
            </template>
          </GridColumn>
          <GridColumn field="detailDataNo" title="物料编码" width="120" align="left">
            <template #body="scope">
              <div class="kingzon-textEllipsis" :title="scope.row.detailType == FormulaDetailType.Formula ? scope.row.formulaProductInfo?.productNo :
                scope.row.detailDataNo">
                {{ scope.row.detailType == FormulaDetailType.Formula ? scope.row.formulaProductInfo?.productNo :
                  scope.row.detailDataNo }}
              </div>
            </template>
          </GridColumn>
          <GridColumn field="detailDataName" title="物料名称" width="150" align="left">
            <template #body="scope">
              <div class="kingzon-textEllipsis" :title="scope.row.detailType == FormulaDetailType.Formula ? scope.row.formulaProductInfo?.productName :
                scope.row.detailDataName">
                {{ scope.row.detailType == FormulaDetailType.Formula ? scope.row.formulaProductInfo?.productName :
                  scope.row.detailDataName }}
              </div>
            </template>
          </GridColumn>
        </GridHeaderRow>
      </GridColumnGroup>
      <GridColumnGroup align="center" :columnMoving="true">
        <GridHeaderRow>
          <GridColumn field="detailDataTechCode" title="技术型号" width="100" align="left" :rowspan="2">
            <template #body="scope">
              <div class="kingzon-textEllipsis" :title="scope.row.detailType == FormulaDetailType.Formula ? scope.row.formulaProductInfo?.productTechCode :
                scope.row.detailDataTechCode">
                {{ scope.row.detailType == FormulaDetailType.Formula ? scope.row.formulaProductInfo?.productTechCode :
                  scope.row.detailDataTechCode }}
              </div>
            </template>
          </GridColumn>
          <GridColumn field="detailDataCategoryName" title="物料类别" width="100" align="left" :rowspan="2">
            <template #body="scope">
              <div class="kingzon-textEllipsis" :title="scope.row.detailType == FormulaDetailType.Formula ? scope.row.formulaProductInfo?.productCategoryName :
                scope.row.detailDataCategoryName">
                {{ scope.row.detailType == FormulaDetailType.Formula ? scope.row.formulaProductInfo?.productCategoryName
                  :
                  scope.row.detailDataCategoryName }}
              </div>
            </template>
          </GridColumn>
          <GridColumn field="detailDataSpecification" title="规格型号" width="100" align="left" :rowspan="2">
            <template #body="scope">
              <div class="kingzon-textEllipsis" :title="scope.row.detailType == FormulaDetailType.Formula ? scope.row.formulaProductInfo?.productSpecification :
                scope.row.detailDataSpecification">
                {{ scope.row.detailType == FormulaDetailType.Formula ?
                  scope.row.formulaProductInfo?.productSpecification
                  :
                  scope.row.detailDataSpecification }}
              </div>
            </template>
          </GridColumn>
          <GridColumn field="detailDataMaterialGrade" title="材质牌号" width="100" align="left" :rowspan="2">
            <template #body="scope">
              <div class="kingzon-textEllipsis" :title="scope.row.detailType == FormulaDetailType.Formula ? scope.row.formulaProductInfo?.productMaterialGrade :
                scope.row.detailDataMaterialGrade">
                {{ scope.row.detailType == FormulaDetailType.Formula ?
                  scope.row.formulaProductInfo?.productMaterialGrade
                  :
                  scope.row.detailDataMaterialGrade }}
              </div>
            </template>
          </GridColumn>
          <!-- 二级表头 -->
          <GridColumn title="配方用量" width="100" align="center" :overflowTextEllipsis="true" :colspan="3">
          </GridColumn>
          <!-- 二级表头 -->
          <!-- <GridColumn title="偏差(kg)" width="100" align="center" :overflowTextEllipsis="true" :colspan="2">
          </GridColumn> -->
          <!-- 库存 -->
          <GridColumn title="库存" width="100" align="center" :overflowTextEllipsis="true" :colspan="3">
          </GridColumn>
        </GridHeaderRow>
        <GridHeaderRow>
          <GridColumn field="curQuantity" title="用量(kg)" width="120" align="right">
          </GridColumn>
          <GridColumn field="curRatio" title="比例(%)" width="100" align="right">
          </GridColumn>
          <GridColumn field="curBaseQuantity" title="底数(kg)" width="100" align="right">
          </GridColumn>
          <!-- <GridColumn field="lowerTolerance" title="下偏差" width="100" align="right">
          </GridColumn>
          <GridColumn field="upperTolerance" title="上偏差" width="100" align="right">
          </GridColumn> -->

          <GridColumn field="totalStockQuantity" title="总库存" width="110" align="right">
            <template #body="scope">
              <div style="display: flex;flex-direction: column;">
                <div style="z-index: 999;">
                  <span
                    :style="{ background: scope.row.abnormalQuantity ? 'white' : 'transparent', padding: scope.row.abnormalQuantity ? '0 0 0 2px' : '0' }">
                    {{ $utils.formatThousandSeparator(scope.row.totalStockQuantity) }}
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
          <GridColumn field="detailDataUnit" title="单位" width="40" align="left" :overflowTextEllipsis="true">
          </GridColumn>
        </GridHeaderRow>
      </GridColumnGroup>
      <GridColumnGroup align="right" :frozen="true" width="800">
        <GridHeaderRow>
          <GridColumn title="当前配方" width="100" align="center" :overflowTextEllipsis="true" :colspan="2">
          </GridColumn>
          <GridColumn title="当前需求" width="100" align="center" :overflowTextEllipsis="true" :colspan="2">
          </GridColumn>
          <GridColumn field="supplierId" title="供应商" width="130" align="right" :editable="true" :cellCss="getCellCss"
            :rowspan="2">
            <template #body="scope">
              {{scope.row.supplierId ? supplierList.find(s => s.value == scope.row.supplierId)?.text : ''}}
            </template>
            <template #edit="scope">
              <ComboBox v-model="scope.row.supplierId" :data="supplierList"
                :disabled="scope.row.isDemand == false || (!scope.row.detailType && scope.row.children.length > 0)">
                <Addon>
                  <span v-if="scope.row.supplierId" class="textbox-icon icon-clear" title="Clear value"
                    @click="scope.row.supplierId = null"></span>
                </Addon>
              </ComboBox>
            </template>
          </GridColumn>
          <GridColumn field="dataStatus" title="物料状态" width="100" align="right" :editable="true" :rowspan="2"
            :cellCss="getCellCss">
            <template #body="scope">
              {{ scope.row.dataStatus ? InventoryDataStatus[scope.row.dataStatus] : '' }}
            </template>
            <template #edit="scope">
              <ComboBox v-model="scope.row.dataStatus" :data="dataStatusList"
                :disabled="scope.row.isDemand == false || (!scope.row.detailType && scope.row.children.length > 0)">
                <Addon>
                  <span v-if="scope.row.dataStatus" class="textbox-icon icon-clear" title="Clear value"
                    @click="scope.row.dataStatus = null"></span>
                </Addon>
              </ComboBox>
            </template>
          </GridColumn>
          <GridColumn field="detailRemark" title="需求备注" width="120" align="left" :rowspan="2" :cellCss="getCellCss"
            :editable="true">
            <template #edit="scope">
              <TextBox v-model="scope.row.detailRemark"
                :disabled="scope.row.isDemand == false || (!scope.row.detailType && scope.row.children.length > 0)">
              </TextBox>
            </template>
          </GridColumn>
          <GridColumn field="op" title="操作" width="90" align="center" :rowspan="2">
            <template #body="scope">
              <ButtonGroup style="height: 24px">
                <SwitchButton v-if="scope.row.detailType" @click="changeDemandStatus(scope.row)"
                  style="width: 75px; height: 24px;" v-model="scope.row.isDemand" onText="领用" offText="不领用">
                </SwitchButton>
                <LinkButton v-if="!scope.row.detailType" @click="deleteRow(scope.row)">删除</LinkButton>
              </ButtonGroup>
            </template>
          </GridColumn>
        </GridHeaderRow>
        <GridHeaderRow>
          <!-- 当前配方 -->
          <GridColumn field="formulaQuantity" title="用量" width="100" align="right" :editable="true"
            :cellCss="getFormulaCellCss">
            <template #edit="scope">
              <NumberBox v-model="scope.row.formulaQuantity" :spinners="false" :min="0" :precision="6"
                :disabled="scope.row.isDemand == false || !scope.row.detailType"
                @valueChange="setDemandQuantity(scope.row)">
              </NumberBox>
            </template>
          </GridColumn>
          <GridColumn field="formulaConversionId" title="单位" width="80" align="left" :editable="true"
            :cellCss="getFormulaCellCss">
            <template #body="scope">
              {{scope.row.kgUnitList ? scope.row.kgUnitList.find(s => s.value == scope.row.formulaConversionId)?.text :
                ''}}
            </template>
            <template #edit="scope">
              <ComboBox v-model="scope.row.formulaConversionId" :data="scope.row.kgUnitList"
                @valueChange="changeFormulaUnit($event, scope.row)"
                :disabled="scope.row.isDemand == false || !scope.row.detailType">
              </ComboBox>
            </template>
          </GridColumn>
          <!-- 当前需求 -->
          <GridColumn field="demandQuantity" title="用量" width="100" align="right" :editable="true"
            :cellCss="getCellCss">
            <template #edit="scope">
              <NumberBox v-model="scope.row.demandQuantity" :spinners="false" :min="0" :precision="6"
                @valueChange="setConvInfos(scope.row)"
                :disabled="scope.row.isDemand == false || (!scope.row.detailType && scope.row.children.length > 0)">
              </NumberBox>
            </template>
          </GridColumn>
          <GridColumn field="unit" title="单位" width="80" align="left" :editable="true" :cellCss="getCellCss">
            <template #body="scope">
              {{scope.row.unitList ? scope.row.unitList.find(s => s.value == scope.row.unitConversionId)?.text : ''}}
            </template>
            <template #edit="scope">
              <ComboBox v-model="scope.row.unitConversionId" :data="scope.row.unitList"
                @valueChange="changeDemandUnit($event, scope.row)"
                :disabled="scope.row.isDemand == false || (!scope.row.detailType && scope.row.children.length > 0)">
              </ComboBox>
            </template>
          </GridColumn>
        </GridHeaderRow>
      </GridColumnGroup>
    </TreeGrid>
    <div class="kingzon-btn-group kingzon-page-footer">
      <LinkButton @click="$emit('close')">取消</LinkButton>
      <LinkButton btnCls="c8" @click="submit">确定</LinkButton>
    </div>
  </div>

  <!-- 选择物料 -->
  <Drawer v-if="isShowSeleteDrawer" @collapse="isShowSeleteDrawer = false;" ref="seleteMaterial" title="选择其他物料"
    @expand="$utils.updateModal()" :drawerStyle="{ width: '95%' }" region="east" :maskClosable="false" :closable="true">
    <SelectMaterial :selectList="otherMaterials" @close="seleteMaterialDrawer.collapse()" @next="getOtherMaterials"
      :furmulaMaterialIds="furmulaMaterialIds">
    </SelectMaterial>
  </Drawer>
</template>

<script lang="ts">
import { type ProductIdDto } from '@/types/common';
import { DataType, FilterType, FormulaDetailType, InventoryDataStatus, ProductType, UnitConversionType } from '@/types/enums';
import { Filter, FormulaStatus, ShowFormData, type SelectOption } from '@/types/types';
import { defineComponent } from 'vue';
import SelectMaterial from './select-material.vue';
import { cloneDeep } from 'lodash';
import { type FormulaDetailDto, type GetProductListByProductionDto, InventoryUnitOutboundDto } from '@/types/production.js';
import Decimal from 'decimal.js';

export default defineComponent({
  props: {
    currentRow: {
      type: Object as () => any,
      required: true,
      default: {} as any
    },
  },
  components: {
    SelectMaterial
  },
  emits: ['close', 'submit'],
  computed: {
    FormulaDetailType() {
      return FormulaDetailType;
    },
    ProductType() {
      return ProductType;
    },
    InventoryDataStatus() {
      return InventoryDataStatus;
    }
  },
  data() {
    return {
      isLoading: false,
      formData: [] as ShowFormData[],
      formulas: [] as SelectOption[],
      submitData: {
        formulaVersionId: undefined,
        formulaUnitConversionId: undefined,
      } as any,
      rules: {},
      messages: {},
      errors: {},
      materialDetails: [] as FormulaDetailDto[],
      formulaVersion: {} as SelectOption,
      unitConversitions: [] as any[],
      unitList: [] as ProductIdDto[],
      materials: [] as FormulaDetailDto[],
      isShowSeleteDrawer: false,
      seleteMaterialDrawer: {} as EasyUI.Drawer,
      otherMaterials: [] as GetProductListByProductionDto[],
      formulaUnits: [] as SelectOption[],
      formulaUnitInfo: {} as SelectOption,
      convTotalQuantity: 0,
      convRatio: 0,
      furmulaMaterialIds: [] as number[],
      isInit: false,
      supplierList: [] as SelectOption[],
      dataStatusList: [] as SelectOption[],
    };
  },
  created() {
    this.isInit = true;
    this.formData = [
      new ShowFormData('技术型号', this.currentRow?.productTechnicalCode ?? ""),
      new ShowFormData('产品编码', this.currentRow?.productNo ?? ""),
      new ShowFormData('产品名称', this.currentRow?.productName ?? ""),
      new ShowFormData('规格型号', this.currentRow?.specification ?? ""),
      new ShowFormData('包装方式', this.currentRow?.productPackageWay ?? ''),
      new ShowFormData('派工部门', this.currentRow?.departmentName ?? ''),
      new ShowFormData('派工数量', this.currentRow?.dispatchQuantity),
      new ShowFormData('派工单位', this.currentRow?.dispatchUnitInfo?.productionUnitName ?? '')] as ShowFormData[];
    this.dataStatusList = this.$utils.getEnumKeyValueArray(InventoryDataStatus).filter(s => Number(s.value)).map(s => {
      return {
        value: Number(s.value),
        text: s.text
      }
    }) as SelectOption[];
  },
  mounted() {
    this.init();
  },
  methods: {
    init() {
      this.isLoading = true;
      this.getSupplierList()
        .then((res: any) => {
          this.supplierList = res.data.map(x => {
            return {
              value: x.id,
              text: x.customerName
            }
          }) as SelectOption[];
          this.getDataAndRefresh();
        }).catch(err => {
          this.$kingzon_messager.showError(err);
          this.isLoading = false;
        })
    },

    getData() {
      var filters = [new Filter('formula.productId', this.currentRow?.productId, FilterType.Equals, DataType.SelectNum),
        // new Filter('status', FormulaStatus.已生效, FilterType.Equals, DataType.SelectNum)
      ];
      return this.$http.post("/api/technical/FormulaManage/GetFormulaOptions", filters);
    },

    getSupplierList() {
      return this.$http.get("/api/base/Supplier/GetAllSupplier");
    },

    getTotalUnitList() {
      var productIdList = this.materials.map(x => {
        var productId = x.detailType != FormulaDetailType.Formula ? x.detailId : x.formulaProductInfo?.productId;
        return {
          idField: (productId + (x.specificationId ? ('_' + x.specificationId) : '')).toString(),
          productId: productId,
          specificationId: x.specificationId,
        }
      });
      productIdList.push({
        idField: this.currentRow.productId.toString(),
        productId: this.currentRow.productId,
        specificationId: undefined,
      })
      return this.$http.post('/api/sale/SaleOrderManage/GetProductUnitList', productIdList)
    },

    getUnitConversition() {
      var postData = {
        productIds: this.materials.map(x => x.detailType != FormulaDetailType.Formula ? x.detailId : x.formulaProductInfo?.productId).filter(x => x !== undefined),
        specificationIds: this.materials.filter(s => s.detailType != FormulaDetailType.Formula && s.specificationId).map(x => x.specificationId),
      };
      postData.productIds.push(this.currentRow.productId);
      if (this.currentRow.specificationId) {
        postData.specificationIds.push(this.currentRow.specificationId);
      }
      return this.$http.post('/api/sale/SaleMonthPlan/GetUnitConversionListByTargetIds', postData);
    },

    getProductUnits(row) {
      var that = this;
      var productId = row.detailType != FormulaDetailType.Formula ? row.detailId : row.formulaProductInfo?.productId;
      var idField = (productId + (row.specificationId ? ('_' + row.specificationId) : '')).toString();
      var list = that.unitList.find(x => x.idField == idField);
      var options = list ? (list.unitList ?? []).map(x => {
        return {
          value: Number(x.code),
          text: x.text + (x.extension ? '【' + x.extension + '】' : ''),
          code: x.packageId,
          extension: x.unitId.toString()
        }
      }) as SelectOption[] : [];
      return options;
    },

    getBaseRatio(x, toUnitId, fromUnitId, id) {
      if (id < 0 || !id) {
        return 1;
      }
      var productId = x.detailType != FormulaDetailType.Formula ? x.detailId : x.formulaProductInfo?.productId;
      var baseToMain = this.unitConversitions.find(u => u.sourceId == x.specificationId && u.conversionType == UnitConversionType.SpecToProduct);
      var baseToMainRatio = baseToMain ? baseToMain.ratio : 1;
      var ratioInfos = x.specificationId ? this.unitConversitions.filter(u => (u.targetId == x.specificationId &&
        (u.conversionType == UnitConversionType.ProductSpec ||
          u.conversionType == UnitConversionType.PackageToProductSpec) ||
        (u.targetId == productId && u.conversionType == UnitConversionType.PackageToProduct)) ||
        (u.sourceId == x.specificationId && u.conversionType == UnitConversionType.SpecToProduct)) :
        this.unitConversitions.filter(u => u.targetId == productId && (u.conversionType == UnitConversionType.Product ||
          u.conversionType == UnitConversionType.PackageToProduct));
      var mainConv = ratioInfos.find(r => r.toUnitId == toUnitId && r.fromUnitId == fromUnitId && r.id == id);
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

    getDataAndRefresh() {
      this.isLoading = true;
      this.getData()
        .then(res => {
          this.formulas = res.data as SelectOption[];
          this.formulas.forEach(s => {
            s.value = Number(s.value);
          })
          //如果当前初始化，则默认选中第一个配方版本
          if (this.formulas.length > 0 && !this.currentRow.materialDemands && !this.currentRow.specificationId) {
            var info = this.formulas.find(s => Number(s.value) == this.currentRow?.formulaVersionId);
            this.submitData.formulaVersionId = info ? info.value : this.formulas[0].value;
            this.formulaVersion = info ? info : this.formulas[0];
            this.$nextTick(() => {
              this.isInit = false;
              this.getFormulaById();
            })
          }
          //如果当前行有配方版本id，获取明细单位
          if (this.currentRow.materialDemands && this.currentRow.materialDemands.formulaVersionId) {
            this.submitData.formulaVersionId = this.currentRow.materialDemands.formulaVersionId;
          }
          if (this.currentRow.materialDemands && this.currentRow.materialDemands.materialDetails.length) {
            this.materialDetails = cloneDeep(this.currentRow.materialDemands.materialDetails);
            return this.getMaterialUnitInfos();
          }
          if (!this.submitData.formulaVersionId || (this.currentRow.materialDemands && this.currentRow.materialDemands.materialDetails.length == 0)) {
            this.isInit = false;
            this.isLoading = false;
          }
        })
        .catch(err => {
          this.$kingzon_messager.showError(err);
          this.isLoading = false;
        })
    },

    getMaterialUnitInfos() {
      this.materials = [];
      this.materialDetails.forEach(s => {
        this.getTotalMaterials(s);
      })
      this.isLoading = true;
      this.getTotalUnitList()
        .then((res) => {
          this.unitList = res.data as ProductIdDto[];
          return this.getUnitConversition();
        })
        .then(res => {
          this.unitConversitions = res.data as any;
          this.materialDetails.forEach(s => {
            this.setUnitList(s);
          })
          //配方单位kg
          this.initCurrentRowUnit();
        })
        .catch(err => {
          this.$kingzon_messager.showError(err);
        })
        .finally(() => {
          this.isLoading = false;
          this.isInit = false;
        })
    },

    getFormulaById() {
      this.formulaVersion = this.formulas.find(s => s.value == this.submitData.formulaVersionId) as SelectOption;
      //console.log('getFormulaById', this.formulaVersion, this.submitData.formulaVersionId, this.isInit);
      if (!this.formulaVersion || !this.formulaVersion.code || !this.submitData.formulaVersionId || this.isInit) {
        this.isLoading = false;
        return;
      }
      this.isLoading = true;
      this.$http.get("/api/technical/FormulaManage/GetFormulaById", {
        formulaId: Number(this.formulaVersion.code),
        formulaVersionId: Number(this.formulaVersion.value)
      })
        .then((res: any) => {
          this.materialDetails = res.data.versionList.length ? res.data.versionList[0].detailList : [];
          this.materials = [];
          this.materialDetails.forEach(s => {
            s.isDemand = true;
            this.getTotalMaterials(s);
          })
          return this.getTotalUnitList()
        })
        .then((res) => {
          this.unitList = res.data as ProductIdDto[];
          return this.getUnitConversition();
        })
        .then(res => {
          this.unitConversitions = res.data as any;
          this.materialDetails.forEach(s => {
            this.setUnitList(s);
          })
          //配方单位kg
          this.initCurrentRowUnit();
        })
        .catch(err => {
          this.$kingzon_messager.showError(err);
        })
        .finally(() => {
          this.isLoading = false;
        })
    },

    initCurrentRowUnit() {
      this.formulaUnits = this.currentRow.unitList.filter(s => s.text.toLowerCase().includes("kg")) as SelectOption[];
      if (this.currentRow.specificationId) {
        var productUnits = this.getProductUnits({
          detailId: this.currentRow.productId,
          specificationId: null,
          detailType: FormulaDetailType.Product
        }).filter(s => s.text.toLowerCase().includes("kg") && (s.value as number) > 0) as SelectOption[];
        productUnits.forEach(s => {
          s.text = s.text + '【成品次单位】';
        })
        this.formulaUnits = [...this.formulaUnits, ...productUnits];
        this.formulaUnits = Array.from(
          new Map(this.formulaUnits.map(item => [item.value, item])).values()
        ) as SelectOption[];
      }
      if (!this.submitData.formulaUnitConversionId && this.submitData.formulaVersionId) {
        var info = (this.currentRow?.dispatchUnitInfo.productionUnitName ?? '').toLowerCase().includes("kg") ?
          this.formulaUnits.find((u: SelectOption) => u.value == this.currentRow?.dispatchUnitInfo.unitConversionId) :
          this.formulaUnits.find((u: SelectOption) => u.text.toLowerCase().includes("kg"));
        this.formulaUnitInfo = info as SelectOption;
        this.submitData.formulaUnitConversionId = info ? info.value : undefined;
      }

    },

    setUnitList(row) {
      row.unitList = this.getProductUnits(row);
      row.kgUnitList = row.unitList.filter(s => s.text.toLowerCase().includes("kg"));
      this.setInitUnit(row);
      row.children.forEach(s => {
        s.unitList = this.getProductUnits(s);
        s.kgUnitList = s.unitList.filter(s => s.text.toLowerCase().includes("kg"));
        this.setInitUnit(s);
        if (s.children && s.children.length) {
          this.setUnitList(s);
        }
      });
    },

    setInitUnit(row) {
      if (row.formulaUnitId || !row.isDemand || row.unitId) {
        return;
      }
      var info = row.detailDataUnit.toLowerCase().includes('kg') ?
        row.unitList.find((u: SelectOption) => Number(u.extension) == row.detailDataUnitId) :
        row.unitList.find((u: SelectOption) => u.text.toLowerCase().includes("kg"))
      //是否存在kg单位，如果存在则默认选中kg单位，如果不存在则默认选中物料单位
      if (info) {
        row.unitId = Number(info.extension);
        row.unitConversionId = Number(info.value);
        row.formulaConversionId = row.unitConversionId;
        row.formulaUnitId = row.unitId;
      }
      if (!row.unitId && row.unitList.length == 1) {
        row.unitId = Number(row.unitList[0].extension);
        row.unitConversionId = Number(row.unitList[0].value);
      }
    },

    getTotalMaterials(row) {
      this.materials.push(row);
      row.children.forEach(s => {
        this.materials.push(s);
        if (s.children && s.children.length) {
          this.getTotalMaterials(s);
        }
      });
    },

    changeDemandUnit(info, row) {
      var event = row.unitList.find(s => s.value == info.currentValue);
      row.unitId = event ? Number(event.extension) : row.unitId;
      if (row.detailType) {
        this.setDemandQuantity(row);
      }
      if (!row.detailType) {
        this.setConvInfos(row);
      }
    },

    changeFormulaUnit(info, row) {
      var event = row.unitList.find(s => s.value == info.currentValue);
      row.formulaUnitId = event ? Number(event.extension) : row.formulaUnitId;
      this.setDemandQuantity(row);
    },

    setDemandQuantity(row) {
      //demandRatio * 基础单位 = 需求单位
      row.demandRatio = this.getBaseRatio(row, row.detailDataUnitId, row.unitId, row.unitConversionId);

      // formulaRatio * 基础单位 = 配方单位
      var formulaRatio = this.getBaseRatio(row, row.detailDataUnitId, row.formulaUnitId, row.formulaConversionId);

      // convRatio * 配方单位 = 需求单位
      row.convRatio = row.demandRatio !== 0 ? formulaRatio / row.demandRatio : 0;
      row.demandQuantity = Number(((row.formulaQuantity ?? 0) * row.convRatio).toFixed(6));
      this.setConvInfos(row);
    },

    setConvInfos(row) {
      row.demandRatio = this.getBaseRatio(row, row.detailDataUnitId, row.unitId, row.unitConversionId);
      row.baseDemandQuantity = Number(((row.demandQuantity ?? 0) * row.demandRatio).toFixed(6));
      //console.log('row.baseDemandQuantity', row, row.baseDemandQuantity, row.demandQuantity, row.demandRatio);
      //获取需求单位与主单位相关的转换关系
      row.convUnitInfo = [];
      var baseUnit = row.unitList.find(s => Number(s.value) == row.unitConversionId);
      if (baseUnit) {
        var demandConv = this.unitConversitions.find(s => s.id == row.unitConversionId);
        //需求单位选项
        row.convUnitInfo.push(new InventoryUnitOutboundDto({
          id: row.unitConversionId,
          unitId: row.unitId,
          unitName: baseUnit.text,
          ratio: row.demandRatio,
          remark: undefined,
          conversionType: demandConv ? demandConv.conversionType : UnitConversionType.Product,
          outboundQuantity: row.demandQuantity,
        }));
        //未选择需求单位时，规格单位选项
        if (row.unitConversionId != -1) {
          row.convUnitInfo.push(new InventoryUnitOutboundDto({
            id: -1,
            unitId: row.detailDataUnitId,
            unitName: row.detailDataUnit,
            ratio: 1,
            remark: undefined,
            conversionType: UnitConversionType.Product,
            outboundQuantity: row.baseDemandQuantity,
          }));
        }
      }
      var productId = row.detailType != FormulaDetailType.Formula ? row.detailId : row.formulaProductInfo?.productId;
      //需求单位与成品主单位
      if (row.specificationId && row.mainUnitId && row.detailDataUnitId) {
        // mainRatio*成品主单位 = 规格主单位
        var mainRatioInfo = this.unitConversitions.find(s => s.sourceId == row.specificationId && s.targetId == productId && s.toUnitId == row.mainUnitId && s.fromUnitId == row.detailDataUnitId && s.conversionType == UnitConversionType.SpecToProduct);
        //需求单位与成品主单位不一致时，添加成品主单位选项
        if (mainRatioInfo && row.unitConversionId != mainRatioInfo.id) {
          var ratio = mainRatioInfo.ratio != 0 ? 1 / mainRatioInfo.ratio : 0;
          row.convUnitInfo.push(new InventoryUnitOutboundDto({
            id: mainRatioInfo.id,
            unitId: row.mainUnitId,
            unitName: row.mainUnitName,
            ratio: ratio,
            remark: undefined,
            conversionType: UnitConversionType.SpecToProduct,
            outboundQuantity: row.demandQuantity ? row.demandQuantity * ratio : 0,
          }));
        }
      }
      // console.log('setConvInfos', row, row.convUnitInfo);
    },

    deleteRow(row) {
      if (row.parent) {
        row.parent.children = row.parent.children.filter(s => s.id != row.id);
      }
      if (row.parent && row.parent.children.length) {
        return;
      }
      if ((row.parent && row.parent.children.length <= 0) || !row.parent) {
        this.materialDetails = this.materialDetails.filter(s => s.detailType);
      }
    },

    changeDemandStatus(row) {
      if (row.isDemand == false) {
        row.formulaQuantity = undefined;
        row.demandQuantity = undefined;
        row.unitConversionId = undefined;
        row.unitId = undefined;
        row.formulaConversionId = undefined;
        row.formulaUnitId = undefined;
        row.detailRemark = undefined;
        row.supplierId = undefined;
        row.convUnitInfo = [];
      }
      if (row.isDemand == true && row.detailType) {
        this.setInitUnit(row);
        this.setMaterialDemandQuantityByRow(row);
      }
    },

    showSeleteDrawer() {
      this.materials = [];
      this.materialDetails.forEach(s => {
        this.getTotalMaterials(s);
      })
      var list = (this.materialDetails.find(s => !s.detailType) ? this.materialDetails.find(s => !s.detailType)?.children : []) as FormulaDetailDto[];
      this.otherMaterials = list.map(s => {
        return {
          id: s.id,
          productId: s.detailId,
          productType: s.detailProductType,
          productNo: s.detailDataNo,
          productName: s.detailDataName,
          productSpecification: s.detailDataSpecification,
          materialSpecification: s.detailDataSpecification,
          materialGrade: s.detailDataMaterialGrade,
          productCategory: s.detailDataCategoryName,
          productTechnicalCode: s.detailDataTechCode,
          productUnit: s.detailDataUnit,
          productUnitId: s.detailDataUnitId,
          specificationId: s.specificationId,
          occupyInventory: s.occupyInventory,
          inventoryQuantity: s.inventoryQuantity,
          abnormalQuantity: s.abnormalQuantity
        }
      }) as GetProductListByProductionDto[];
      this.furmulaMaterialIds = this.materials.map(x => x.detailType != FormulaDetailType.Formula ? x.detailId : x.formulaProductInfo?.productId).filter(x => x !== undefined) as number[];
      this.isShowSeleteDrawer = true;
      this.$nextTick(() => {
        this.seleteMaterialDrawer = this.$refs.seleteMaterial as EasyUI.Drawer;
        this.seleteMaterialDrawer.expand();
      })
    },

    getOtherMaterials(list) {
      this.otherMaterials = list;
      var newMaterials = list.map(s => {
        return {
          id: s.id,
          detailType: undefined,//其他物料
          detailId: s.productId,
          detailProductType: s.productType,
          detailDataNo: s.productNo,
          detailDataName: s.productName,
          detailDataSpecification: s.productSpecification ?? s.materialSpecification,
          detailDataMaterialGrade: s.materialGrade,
          detailDataCategoryName: s.productCategory,
          detailDataTechCode: s.productTechnicalCode,
          detailDataUnit: s.productUnit,
          detailDataUnitId: s.productUnitId,
          mainUnitId: s.productMainUnitId,
          mainUnitName: s.productMainUnit,
          specificationId: s.specificationId,
          demandQuantity: undefined,
          occupyInventory: s.occupyInventory,
          inventoryQuantity: s.inventoryQuantity,
          abnormalQuantity: s.abnormalQuantity,
          totalStockQuantity: Number((s.occupyInventory + s.inventoryQuantity).toFixed(6)),
          children: [],
          unitList: [],
          kgUnitList: [],
          isDemand: true,
        }
      });
      var addList = [] as any[];
      this.seleteMaterialDrawer.collapse();
      var otherParent = this.materialDetails.find(s => !s.detailType) as FormulaDetailDto;
      if (!otherParent) {
        otherParent = {
          detailType: undefined,
          children: []
        } as any;
        this.materialDetails.push(otherParent);
      }
      newMaterials.forEach(s => {
        var info = otherParent.children.find(x => x.id == s.id);
        //如果不存在则添加到其他物料下
        if (!info) {
          otherParent.children.push(s);
          addList.push(s);
        }
      })
      otherParent.children.forEach(s => {
        var data = newMaterials.find(x => x.id == s.id);
        if (!data) {
          otherParent.children = otherParent.children.filter(x => x.id != s.id);
        }
      })
      //如果有新增物料，新增获取单位和转换关系
      if (addList.length) {
        this.materials = [];
        this.materialDetails.forEach(s => {
          this.getTotalMaterials(s);
        })
        this.isLoading = true;
        this.getTotalUnitList()
          .then((res) => {
            this.unitList = res.data as ProductIdDto[];
            return this.getUnitConversition();
          })
          .then(res => {
            this.unitConversitions = res.data as any;
            this.materials.filter(s => !s.detailType && !s.unitId).forEach(s => {
              s.unitList = this.getProductUnits(s);
              var selfUnit = s.unitList.find(x => Number(x.extension) == s.detailDataUnitId);
              s.unitId = selfUnit ? Number(selfUnit.extension) : undefined;
              s.unitConversionId = selfUnit ? Number(selfUnit.value) : undefined;
            })
          })
          .catch(err => {
            this.$kingzon_messager.showError(err);
          })
          .finally(() => {
            this.isLoading = false;
          })
      }
    },

    //获取派工量转换的总用量
    getConvTotalQuantity(event) {
      this.formulaUnitInfo = event;
      //console.log('getConvTotalQuantity', event, this.currentRow, this.formulaUnitInfo);
      if (!event || this.isInit) {
        return;
      }
      //console.log('getConvTotalQuantity', event, this.currentRow, this.formulaUnitInfo);
      var conversion = this.unitConversitions.find(s => s.id == event.value);
      //console.log(this.unitConversitions, this.formulaUnitInfo);
      if (!conversion && this.formulaUnitInfo) {
        conversion = {
          id: this.formulaUnitInfo.value,
          sourceId: this.currentRow.specificationId ?? this.currentRow.productId,
          targetId: this.currentRow.productId,
          fromUnitId: Number(this.formulaUnitInfo.extension),
          toUnitId: this.currentRow.dispatchUnitInfo.baseUnitId,
          ratio: 1,
          conversionType: UnitConversionType.Product
        };
      }
      //productionRatio * 基础单位 = 派工单位
      var productionRatio = this.getBaseRatio(this.currentRow, this.currentRow.dispatchUnitInfo.baseUnitId, this.currentRow.dispatchUnitInfo.productionUnitId, this.currentRow.dispatchUnitInfo.unitConversionId);
      //console.log(productionRatio, 'productionRatio');
      // formulaRatio * 基础单位 = 配方单位
      var formulaRatio = 0;
      //配方单位为成品次单位/成品包装单位,派工产品为规格产品，需多次转换
      if ((conversion.conversionType == UnitConversionType.Product || conversion.conversionType == UnitConversionType.PackageToProduct) && this.currentRow.specificationId) {
        var row = cloneDeep(this.currentRow);
        row.specificationId = null;
        //配方单位和成品主单位转换
        var formulaToMain = this.getBaseRatio(row, row.productMainUnitId, Number(this.formulaUnitInfo.extension), this.formulaUnitInfo.value);
        //成品主单位和规格成品主单位转换
        var mainToSpec = this.unitConversitions.find(s => s.sourceId == this.currentRow.specificationId && s.targetId == this.currentRow.productId && s.conversionType == UnitConversionType.SpecToProduct);
        var mainToSpecRatio = mainToSpec ? mainToSpec.ratio : 0;
        formulaRatio = mainToSpecRatio != 0 ? formulaToMain / mainToSpecRatio : 0;
      }
      else {
        formulaRatio = this.getBaseRatio(this.currentRow, this.currentRow.dispatchUnitInfo.baseUnitId, Number(this.formulaUnitInfo.extension), this.formulaUnitInfo.value);
      }

      // convRatio * 配方单位 = 派工单位
      this.convRatio = formulaRatio !== 0 ? productionRatio / formulaRatio : 0;
      this.convTotalQuantity = Number((this.currentRow.dispatchQuantity * this.convRatio).toFixed(6));
      //console.log(formulaRatio, 'formulaRatio', this.convTotalQuantity);
      this.setMaterialDemandQuantity();
    },

    //按照配方单位计算需求用量
    setMaterialDemandQuantity() {
      this.materials = [];
      this.materialDetails.forEach(s => {
        this.getTotalMaterials(s);
      })
      //更新配方物料和同产品物料
      this.materials.filter(s => s.detailType).forEach(s => {
        this.setMaterialDemandQuantityByRow(s);
      })
      //更新其他物料
      this.materials.filter(s => !s.detailType && s.isDemand && s.children.length > 0).forEach(s => {
        this.setConvInfos(s);
      })
    },

    setMaterialDemandQuantityByRow(row) {
      //存在配方单位，根据配方单位转换
      var formualUnit = row.kgUnitList.find(u => u.value == row.formulaConversionId) as SelectOption;
      if (formualUnit) {
        var formulaRatio = this.getBaseRatio(row, row.detailDataUnitId, Number(formualUnit.extension), formualUnit.value);//配方单位 针对主单位 转换率
        var demandRatio = this.getBaseRatio(row, row.detailDataUnitId, Number(row.unitId), row.unitConversionId);  //需求单位 针对主单位 转换率
        var formulaDemandRatio = demandRatio !== 0 ? formulaRatio / demandRatio : 0;   //配方单位与需求单位转换率
        // row.formulaQuantity = Number((this.convTotalQuantity * row.totalRatio * row.formulaRatio).toFixed(6));

        var scale = Decimal.div(this.convTotalQuantity, row.curBaseQuantity);
        var formulaQuantity = Decimal.mul(scale, row.curQuantity);
        row.formulaQuantity = this.$utils.decimalPlaces4(Decimal.mul(formulaQuantity, formulaDemandRatio), 6).toNumber();
        row.formulaRatio = formulaDemandRatio;
        this.setDemandQuantity(row);
      }
      if (!formualUnit) {
        this.setConvInfos(row);
      }
    },

    getFormulaCellCss(row) {
      if (row.isDemand && row.detailType) {
        return 'kingzon-cell-editable';
      }
      return '';
    },

    getCellCss(row) {
      if (row.isDemand) {
        return 'kingzon-cell-editable';
      }
      return '';
    },

    cleanFormula() {
      this.materialDetails = [];
      this.materials = [];
      this.submitData.formulaVersionId = undefined;
      this.submitData.formulaUnitConversionId = undefined;
      this.formulaVersion = {} as SelectOption;
      this.otherMaterials = [];
      this.convTotalQuantity = 0;
    },

    submit() {
      this.materials = [];
      this.materialDetails.forEach(s => {
        this.getTotalMaterials(s);
      })
      var noDemand = this.materials.find(s => (s.detailType || (!s.detailType && s.children.length <= 0)) && s.isDemand && (!s.demandQuantity || !s.unitId));
      if (noDemand) {
        var str = noDemand.detailType == FormulaDetailType.Formula ? noDemand.formulaProductInfo?.productNo + '-' + noDemand.formulaProductInfo?.productName : noDemand.detailDataNo + '-' + noDemand.detailDataName;
        this.$kingzon_messager.showError('请输入【' + ProductType[noDemand.detailProductType ?? ProductType.成品] + '】' + str + '需求用量或单位');
        return;
      }
      this.$emit('submit', {
        formulaVersionId: this.submitData.formulaVersionId,
        formulaUnitConversionId: this.submitData.formulaUnitConversionId,
        formulaUnitInfo: this.formulaUnitInfo,
        materialDetails: this.materialDetails,
        formulaQuantity: this.convTotalQuantity,
        convRatio: this.convRatio,
        unitConversitions: this.unitConversitions,
        materials: this.materials.filter(s => (s.detailType || (!s.detailType && s.children.length <= 0)) && s.isDemand)
      });
    },

  },
});
</script>

<style scoped>
:deep(.switchbutton-off) {
  font-size: 12px
}

:deep(.switchbutton-on) {
  font-size: 12px
}
</style>
