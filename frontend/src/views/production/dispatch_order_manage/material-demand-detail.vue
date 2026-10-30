<template>
  <Loading v-show="isLoading"></Loading>
  <div class="kingzon-page-content">
    <KingZonShowForm :formData="formData" :columnWidth="'25%'"></KingZonShowForm>
    <div class="kingzon-btn-group" style="margin: 5px">
      <LinkButton iconCls="icon-add" @click="addOtherMaterial()">补料</LinkButton>
    </div>
    <KingZonDataGrid id="materialDemandDetailDataGrid" :showDownLoad="true" :data="records" :pagination="false"
      class="kingzon-page-fill" ref="dataGrid" :exportFileName="'领料需求详情'" @pageChange="getDataAndRefresh"
      @sortChange="getDataAndRefresh" :dblclickToEdit="true" editMode="cell" :sorts="[{ field: 'order', order: 'asc' }]"
      :rowCss="(row) => { return !row.id ? { background: 'var(--color-green-add)' } : {} }">
      <GridColumnGroup align="left" :frozen="true" width="500">
        <GridHeaderRow>
          <GridColumn align="center" cellCss="datagrid-td-rownumber" width="40" field="order" :sortable="true">
            <template #body="scope">
              {{ scope.rowIndex + 1 }}
            </template>
          </GridColumn>
          <GridColumn field="productTechnicalCode" title="技术型号" width="100" :overflowTextEllipsis="true"
            :sortable="true">
          </GridColumn>
          <GridColumn field="productNo" title="编码" width="120" align="left" :overflowTextEllipsis="true"
            :sortable="true">
          </GridColumn>
          <GridColumn field="productName" title="名称" width="150" align="left" :overflowTextEllipsis="true">
          </GridColumn>
          <GridColumn field="materialDemandType" title="需求类型" width="90" align="center" :sortable="true">
            <template #body="scope">
              <span v-if="scope.row.materialDemandType === MaterialDemandType.补料需求"
                class="kingzon-tag kingzon-tag-warning">{{ MaterialDemandType[MaterialDemandType.补料需求] }}</span>
              <span v-else class="kingzon-tag">{{ MaterialDemandType[MaterialDemandType.领料需求] }}</span>
            </template>
          </GridColumn>
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
          <GridColumn field="materialGrade" title="材质牌号" width="120" :overflowTextEllipsis="true" :rowspan="2">
          </GridColumn>
          <GridColumn field="productPackageWay" title="包装方式" align="left" width="100" :overflowTextEllipsis="true"
            :rowspan="2">
          </GridColumn>
          <GridColumn field="productCategory" title="物料类别" width="120" :overflowTextEllipsis="true" :rowspan="2">
          </GridColumn>
          <GridColumn field="remark" title="物料备注" width="120" :overflowTextEllipsis="true" :rowspan="2">
          </GridColumn>
          <GridColumn :colspan="4" title="物料单位" align="center">
          </GridColumn>
        </GridHeaderRow>
        <GridHeaderRow>
          <GridColumn field="baseDemandQuantity" title="需求数量" width="90" align="right" :sortable="true">
            <template #body="scope">
              {{ $utils.formatThousandSeparator(scope.row.baseDemandQuantity) }}
            </template>
          </GridColumn>
          <GridColumn field="baseReceiveQuantity" title="领用数量" width="90" align="right" :sortable="true">
            <template #body="scope">
              {{ $utils.formatThousandSeparator(scope.row.baseReceiveQuantity) }}
            </template>
          </GridColumn>
          <GridColumn field="baseReturnQuantity" title="退料数量" width="90" align="right" :sortable="true">
            <template #body="scope">
              {{ $utils.formatThousandSeparator(scope.row.baseReturnQuantity) }}
            </template>
          </GridColumn>
          <GridColumn field="productUnit" title="单位" width="50" align="left">
          </GridColumn>
        </GridHeaderRow>
      </GridColumnGroup>
      <GridColumnGroup align="right" :frozen="true" width="730">
        <GridHeaderRow>
          <GridColumn field="demandQuantity" title="当前需求" width="100" align="right" :editable="true" :sortable="true"
            :cellCss="getFormulaCellCss">
            <template #body="scope">
              {{ $utils.formatThousandSeparator(scope.row.demandQuantity ?? 0) }}
            </template>
            <template #edit="scope">
              <NumberBox v-model="scope.row.demandQuantity" :spinners="false" :min="0" :precision="6"
                :disabled="scope.row.receiveQuantity > 0 || scope.row.returnQuantity > 0 || scope.row.isDemand == false"
                @valueChange="submitModifyInfo(scope.row)">
              </NumberBox>
            </template>
          </GridColumn>
          <GridColumn field="unit" title="需求单位" width="80" align="left" :editable="true" :cellCss="getFormulaCellCss">
            <template #body="scope">
              {{scope.row.unitList ? scope.row.unitList.find(s => s.value ==
                scope.row.unitConversionId)?.text : ''}}
            </template>
            <template #edit="scope">
              <ComboBox v-model="scope.row.unitConversionId" :data="scope.row.unitList"
                @valueChange="submitModifyInfo(scope.row)"
                :disabled="scope.row.receiveQuantity > 0 || scope.row.returnQuantity > 0 || scope.row.isDemand == false">
              </ComboBox>
            </template>
          </GridColumn>
          <GridColumn field="detailRemark" title="需求备注" width="150" align="left" :editable="true"
            :cellCss="getFormulaCellCss">
            <template #edit="scope">
              <TextBox v-model="scope.row.detailRemark"
                :disabled="scope.row.receiveQuantity > 0 || scope.row.returnQuantity > 0 || scope.row.isDemand == false"
                @blur="submitModifyInfo(scope.row)">
              </TextBox>
            </template>
          </GridColumn>
          <GridColumn field="supplierId" title="供应商" width="150" align="left" :editable="true"
            :cellCss="getFormulaCellCss">
            <template #body="scope">
              {{scope.row.supplierId ? supplierList.find(s => s.value == scope.row.supplierId)?.text : ''}}
            </template>
            <template #edit="scope">
              <ComboBox v-model="scope.row.supplierId" :data="supplierList" @valueChange="submitModifyInfo(scope.row)"
                :disabled="scope.row.receiveQuantity > 0 || scope.row.returnQuantity > 0 || scope.row.isDemand == false">
                <Addon>
                  <span v-if="scope.row.supplierId" class="textbox-icon icon-clear" title="Clear value"
                    @click="scope.row.supplierId = null"></span>
                </Addon>
              </ComboBox>
            </template>
          </GridColumn>
          <GridColumn field="dataStatus" title="物料状态" width="100" align="left" :editable="true"
            :cellCss="getFormulaCellCss">
            <template #body="scope">
              {{ scope.row.dataStatus ? InventoryDataStatus[scope.row.dataStatus] : '' }}
            </template>
            <template #edit="scope">
              <ComboBox v-model="scope.row.dataStatus" :data="dataStatusList" @valueChange="submitModifyInfo(scope.row)"
                :disabled="scope.row.receiveQuantity > 0 || scope.row.returnQuantity > 0 || scope.row.isDemand == false">
                <Addon>
                  <span v-if="scope.row.dataStatus" class="textbox-icon icon-clear" title="Clear value"
                    @click="scope.row.dataStatus = null"></span>
                </Addon>
              </ComboBox>
            </template>
          </GridColumn>
          <GridColumn field="isDemand" title="领用状态" width="90" align="center" :sortable="true">
            <template #body="scope">
              <SwitchButton @click="changeDemandStatus(scope.row)" style="width: 75px; height: 24px;"
                v-model="scope.row.isDemand" onText="领用" offText="不领用">
              </SwitchButton>
            </template>
          </GridColumn>
          <GridColumn field="op" title="操作" width="100" align="center">
            <template #body="scope">
              <ButtonGroup style="height: 24px">
                <template v-if="!scope.row.id">
                  <!-- 补料需求 -->
                  <LinkButton @click="cancelNewMaterial(scope.row)">取消</LinkButton>
                </template>
                <template v-else>
                  <!-- 领料需求 -->
                  <LinkButton v-if="!scope.row.receiveQuantity && !scope.row.returnQuantity"
                    @click="deleteRow(scope.row)">删除</LinkButton>
                  <LinkButton @click="addMaterial(scope.row)">补料</LinkButton>
                </template>
              </ButtonGroup>
            </template>
          </GridColumn>
        </GridHeaderRow>
      </GridColumnGroup>
    </KingZonDataGrid>
    <div class="kingzon-page-footer kingzon-btn-group">
      <LinkButton @click="onCloseClick">关闭</LinkButton>
      <LinkButton btnCls="c8" @click="submitAll">提交</LinkButton>
      <!-- <LinkButton v-if="!isAllDataCommit" btnCls="c8" @click="confirmNewMaterial">提交补料信息</LinkButton> -->
    </div>

    <!-- 选择物料 -->
    <Drawer v-if="isShowMaterialDrawer" @collapse="isShowMaterialDrawer = false;" ref="materialDrawer" title="选择补料物料"
      :drawerStyle="{ width: '90%' }" region="east" :maskClosable="false" :closable="true">
      <SelectMaterial :selectList="materialSelectList" :furmulaMaterialIds="materialFormulaIds"
        @close="materialDrawer.collapse();" @next="handleMaterialSubmit" />
    </Drawer>
  </div>
</template>
<script lang="ts">

import { ProductIdDto } from '@/types/common';
import { MaterialDemandType, InventoryDataStatus, ProductType, InventoryDataType } from '@/types/enums';
import { InventoryUnitOutboundDto, type GetDispatchOrderDto, type GetProductListByProductionDto } from '@/types/production';
import { Filter, Search, ShowFormData, type SelectOption } from '@/types/types';
import { defineComponent } from 'vue';
import SelectMaterial from './select-material.vue';
import { cloneDeep } from 'lodash';
import { InventoryUnitShowDto, UnitConversionHelper } from '@kingzon/base-pc'

export default defineComponent({
  components: {
    SelectMaterial
  },
  props: {
    selectRow: {
      type: Object as () => GetDispatchOrderDto,
      required: true,
      default: {} as any
    },
  },
  emits: ['close'],
  computed: {
    ProductType() {
      return ProductType;
    },
    InventoryDataStatus() {
      return InventoryDataStatus;
    },
    MaterialDemandType() {
      return MaterialDemandType;
    },
    isAllDataCommit() {
      return !this.records.some(o => !o.id)
    },
    maxOrder() {
      return Math.max(0, ...this.records.map(row => Number(row.order)).filter(Number.isFinite))
    }
  },
  data() {
    return {
      isLoading: false,
      searchParams: [] as Search[],
      filterList: [] as Filter[],
      formData: [] as ShowFormData[],
      records: [] as any[],
      unitList: [] as ProductIdDto[],
      unitConversitions: [] as any[],
      supplierList: [] as SelectOption[],
      dataStatusList: [] as SelectOption[],
      //
      isShowMaterialDrawer: false,
      materialDrawer: {} as EasyUI.Drawer,
      materialSelectList: [] as GetProductListByProductionDto[],
      materialFormulaIds: [],
      changedMaterialList: [] as any[],
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
      new ShowFormData('领料配方', this.selectRow?.formulaName ?? '')] as ShowFormData[];
    this.dataStatusList = this.$utils.getEnumKeyValueArray(InventoryDataStatus).filter(s => Number(s.value)).map(s => {
      return {
        value: Number(s.value),
        text: s.text
      }
    }) as SelectOption[];
  },
  async mounted() {
    try {
      this.isLoading = true;
      await this.getSupplierList()
      await this.getDataAndRefresh();
    } catch (err) {
      this.$kingzon_messager.showError(err);
    } finally {
      this.isLoading = false;
    }
  },
  methods: {
    async getSupplierList() {
      const res = await this.$http.get("/api/base/Supplier/GetAllSupplier") as any;
      this.supplierList = res.data.map(x => {
        return {
          value: x.id,
          text: x.customerName
        }
      }) as SelectOption[];
    },
    async getDataAndRefresh() {
      await this.getData();
      await this.getTotalUnitList();
      await this.getUnitConversition();
    },
    async getData() {
      const res = await this.$http.get("/api/production/DispatchOrderManage/GetMaterialDemandByProduction", {
        id: this.selectRow?.id,
      }, this.$refs.dataGrid);
      this.records = res.data as any[];
      this.records.forEach(item => {
        item.dataStatus = item.dataStatus == 0 ? undefined : item.dataStatus;
      })
    },
    /**
     * 获取当前所有物料的单位
     */
    async getTotalUnitList() {
      var productIdList = this.records.map(x => {
        return {
          idField: (x.productId + (x.specificationId ? ('_' + x.specificationId) : '')).toString(),
          productId: x.productId,
          specificationId: x.specificationId,
        }
      });
      const res = await this.$http.post('/api/sale/SaleOrderManage/GetProductUnitList', productIdList) as any;
      this.unitList = res.data as ProductIdDto[];
      //设置每条记录的单位列表
      this.records.forEach(x => {
        x.unitList = this.getProductUnits(x);
        x.unitList.forEach(u => {
          var item = x.convUnitInfo.find(s => s.id == Number(u.value));
          u.remainQuantity = item ? item.outboundQuantity : 0;
          u.ratio = item ? item.ratio : u.ratio;
        })
      })
    },

    async getUnitConversition() {
      var postData = {
        productIds: this.records.map(x => x.productId),
        specificationIds: this.records.filter(x => x.specificationId).map(x => x.specificationId),
        processPriceIds: [],
      };
      var res = await this.$http.post('/api/sale/SaleMonthPlan/GetUnitConversionListByTargetIds', postData) as any;
      this.unitConversitions = res.data as any[];
    },

    getProductUnits(row) {
      var that = this;
      var idField = (row.productId + (row.specificationId ? ('_' + row.specificationId) : '')).toString();
      var list = that.unitList.find(x => x.idField == idField);
      var options = list ? (list.unitList ?? []).map(x => {
        return {
          value: Number(x.code),
          text: x.text + (x.extension ? '【' + x.extension + '】' : ''),
          code: x.packageId,
          extension: x.unitId.toString(),
          isMainUnit: x.isMainUnit,
          ratio: x.value
        }
      }) as SelectOption[] : [];
      return options;
    },

    async deleteRow(row) {
      try {
        await this.$kingzon_messager.showConfim("是否确定删除该物料需求");
        this.isLoading = true;
        const res = await this.$http.get("/api/production/DispatchOrderManage/DeleteMaterialDemand", { id: row.id }) as any
        this.$kingzon_messager.showInfo(res.data);
        await this.getDataAndRefresh();
      } catch (error) {
        this.$kingzon_messager.showError(error);
      } finally {
        this.isLoading = false;
      }
    },

    async changeDemandStatus(row) {
      try {
        this.isLoading = true;
        const res = await this.$http.get("/api/production/DispatchOrderManage/ChangeDemandStatus", {
          id: row.id,
          isDeleted: !row.isDemand
        }) as any;
        this.$kingzon_messager.showInfo(res.data);
        await this.getDataAndRefresh();
      } catch (err) {
        this.$kingzon_messager.showError(err);
      } finally {
        this.isLoading = false;
      }
    },
    setConvUnitInfo(row): boolean {
      if (!row.demandQuantity) {
        return false
      }

      var unit = row.unitList.find(s => s.value == row.unitConversionId);
      if (!unit) {
        this.$kingzon_messager.showError("需求单位不正确");
        return false;
      }

      const unitInfoList = row.unitList.map(u => {
        return new InventoryUnitShowDto({
          id: Number(u.value),
          unitId: Number(u.extension),
          unitName: u.text,
          ratio: u.ratio,
          remark: undefined,
          remainQuantity: u.remainQuantity,
        })
      }) as InventoryUnitShowDto[];

      const helper = new UnitConversionHelper(row.unitConversionId, row.demandQuantity, unitInfoList);

      //当前单位的需求数量
      var unitInfo = helper.getConversionUnitWithQuantity();
      const convUnitInfoList = [] as InventoryUnitOutboundDto[]
      convUnitInfoList.push(Object.assign({}, unitInfo?.unit, { outboundQuantity: Number(row.demandQuantity) ?? 0 }));

      const mainUnitInfo = helper.getMainUnitWithQuantity();

      //如果不是主单位，还需要添加主单位的出库数量
      if (row.unitConversionId != -1) {
        convUnitInfoList.push(Object.assign({}, mainUnitInfo?.unit, { outboundQuantity: Number(mainUnitInfo?.quantity) ?? 0 }));
      }

      //如果是规格单位，还需要添加对应产品主单位的数量
      if (row.specificationId) {
        var productMainUnitConvId = row.unitList.find(o => o.isMainUnit)?.value;
        if (row.unitConversionId != productMainUnitConvId) {
          var productMainUnitInfo = helper.convertToUnit(productMainUnitConvId);
          convUnitInfoList.push(Object.assign({}, productMainUnitInfo?.unit, { outboundQuantity: Number(productMainUnitInfo?.quantity) ?? 0 }));
        }
      }

      //单位Id
      row.unitId = unitInfo?.unit.unitId ?? 0;
      //主单位需求数量
      row.baseDemandQuantity = mainUnitInfo?.quantity ?? 0;
      //单位转换信息
      row.convUnitInfo = convUnitInfoList;
      return true;
    },
    submitModifyInfo(row) {
      this.changedMaterialList.push(row);
    },
    async confirmModifyInfo(row: any, refresh = true) {
      if (!row.id) {
        return;
      }

      if ((row.demandQuantity ?? 0) <= 0) {
        this.$kingzon_messager.showError(row.productName + "请输入正确的需求用量");
        return
      }

      try {
        if (!this.setConvUnitInfo(row)) {
          this.$kingzon_messager.showError(row.productName + "多单位计算错误");
          return;
        }
        // console.log('submitModifyInfo', row.convUnitInfo);
        this.isLoading = true;
        var postData = {
          id: row.id,
          demandQuantity: row.demandQuantity,
          baseDemandQuantity: row.baseDemandQuantity,
          unitConversionId: row.unitConversionId,
          unitId: row.unitId,
          remark: row.detailRemark,
          convUnitInfo: row.convUnitInfo,
          supplierId: row.supplierId > 0 ? row.supplierId : undefined,
          dataStatus: row.dataStatus ?? InventoryDataStatus.None,
        };
        var res = await this.$http.post("/api/production/DispatchOrderManage/ChangeMaterialDemand", postData) as any;
        var newRecords = this.records.filter(x => !x.id && x != row);
        if (refresh) {
          await this.getDataAndRefresh();
          this.records.push(...newRecords);
        }
        this.$kingzon_messager.showInfo(res.data);
      } catch (err) {
        this.$kingzon_messager.showError(err);
      } finally {
        this.isLoading = false;
      }
    },

    getFormulaCellCss(row) {
      if (row.isDemand) {
        return 'kingzon-cell-editable';
      }
      return '';
    },
    //新增其他材料
    addOtherMaterial() {
      this.isShowMaterialDrawer = true;
      this.$nextTick(() => {
        this.materialDrawer = this.$refs.materialDrawer as EasyUI.Drawer;
        this.materialDrawer.expand();
      });
    },
    _buildNewMaterialRow(source): any {
      var newRow = cloneDeep(source)
      newRow.id = 0
      newRow.order = this.maxOrder + 1
      newRow.materialDemandType = MaterialDemandType.补料需求
      newRow.demandQuantity = 0
      newRow.receiveQuantity = 0
      newRow.baseReceiveQuantity = 0
      newRow.baseDemandQuantity = 0
      newRow.returnQuantity = 0
      newRow.baseReturnQuantity = 0
      newRow.isDemand = true
      newRow.supplierId = source.supplierId ?? null
      newRow.dataStatus = source.dataStatus ?? null
      newRow.unitList = source.unitList ? [...source.unitList] : []
      newRow.convUnitInfo = source.convUnitInfo ? [...source.convUnitInfo] : []
      newRow.unitConversionId = -1
      return newRow
    },
    // 已有需求行追加一单同物料
    addMaterial(row) {
      const newRow = this._buildNewMaterialRow(row);
      // 补料统一追加到已有需求之后。
      this.records.push(newRow);
    },
    async handleMaterialSubmit(selectedData) {
      // console.log('handleMaterialSubmit', selectedData);
      if (!selectedData || selectedData.length === 0) {
        return this.$kingzon_messager.showError("请选择物料");
      }
      try {
        selectedData.forEach(e => {
          const newRow = this._buildNewMaterialRow(e);
          this.records.push(newRow);
        });

        this.materialDrawer.collapse();
        this.isLoading = true;
        await this.getTotalUnitList();
        await this.getUnitConversition();
        // this.records.forEach(item => {
        //   if (!item.id) {
        //     this.submitModifyInfo(item);
        //   }
        // })
      } catch (err) {
        this.$kingzon_messager.showError(err);
      } finally {
        this.isLoading = false;
      }
    },
    cancelNewMaterial(row) {
      // 从列表中移除未保存的新增行
      this.records = this.records.filter(x => x !== row);
    },
    async confirmNewMaterial(newRows) {
      try {
        this.isLoading = true;
        var postData = [] as any[];

        if (newRows.some(row => row.demandQuantity <= 0)) {
          return this.$kingzon_messager.showError("请输入正确的需求量")
        }

        newRows.forEach(row => {
          this.setConvUnitInfo(row);
          postData.push({
            dispatchOrderId: this.selectRow?.id,
            dataId: row.specificationId ?? row.productId,
            dataType: row.specificationId ? InventoryDataType.Specification : InventoryDataType.Product,
            demandQuantity: row.demandQuantity,
            baseDemandQuantity: row.baseDemandQuantity,
            unitId: row.unitId,
            unitConversionId: row.unitConversionId ?? null,
            supplierId: row.supplierId ?? null,
            dataStatus: row.dataStatus ?? InventoryDataStatus.None,
            detailRemark: row.detailRemark ?? null,
            convUnitInfo: row.convUnitInfo ?? [],
            materialDemandType: row.materialDemandType,
            order: row.order
          });
        });

        const res = await this.$http.post("/api/production/DispatchOrderManage/AddMaterialDemand", postData) as any;
        await this.getDataAndRefresh();
        this.$kingzon_messager.showInfo(res.data);
      } catch (err) {
        this.$kingzon_messager.showError(err);
      } finally {
        this.isLoading = false;
      }
    },
    async onCloseClick() {
      if (!this.isAllDataCommit) {
        try {
          await this.$kingzon_messager.showConfim("还有未保存的数据，是否放弃更改")
          this.$emit('close');
        } catch (e) {
          return
        }
      }
      this.$emit('close');
    },
    //提交所有更改
    async submitAll() {
      //新增的需求
      var newRows = this.records.filter(x => !x.id);
      if (newRows.length > 0) {
        await this.confirmNewMaterial(newRows);
      }
      //已有的需求修改
      var modifiedRows = this.changedMaterialList.filter(x => x.id);
      if (modifiedRows.length > 0) {
        for (var i = 0; i < modifiedRows.length; i++) {
          var row = modifiedRows[i];
          await this.confirmModifyInfo(row, false);
        }
      }

      //刷新数据
      await this.getDataAndRefresh();
    }
  },
});
</script>

<style scoped></style>
