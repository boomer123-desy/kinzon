<template>
  <Loading v-show="isLoading"></Loading>
  <div class="kingzon-page-content">
    <div class="kingzon-page-fill" style="display:flex;flex-direction:row;width:100%; overflow-y: unset;">
      <div style="flex:1;width:0;display:flex;flex-direction:column;height:100%;padding-right: 2px;margin-right: 2px;">
        <Form ref="form" :model="postData" labelPosition="before" :labelWidth="100" labelAlign="right" :rules="rules"
          :messages="messages" errorType="tooltip" class="kingzon-page-form kingzon-form-thin">
          <FormField name="customerId" label="客户" style="width: 50%">
            <ComboBox v-model="postData.customerId" textField="customerName" valueField="id" :data="allCustomers"
              @selectionChange="onCustomerChange" :disabled="!isCanChangeCustomer">
            </ComboBox>
          </FormField>
          <div style="width: 50%"></div>
          <FormField name="customerAddress" label="收货人信息" style="width: 50%">
            <div style="display: flex">
              <ComboBox :data="addressList" v-model="selectAddressIndex" textField="displayText" valueField="index"
                @selectionChange="onAddressChange"></ComboBox>
              <LinkButton @click="openAddCustomerAddressPage" style="margin-left: 5px;  height: 30px">
                <font-awesome-icon :icon="['fas', 'plus']" />
              </LinkButton>
            </div>
          </FormField>
          <FormField name="estimatedDeliveryTime" label="预计送达时间" style="width: 50%">
            <DateBox v-model="postData.estimatedDeliveryTime" format="yyyy-MM-dd"
              :panelStyle="{ width: '192px', height: '250px !important', maxHeight: '250px !important' }"
              style="width: 100%;">
              <Addon>
                <span v-if="postData.estimatedDeliveryTime" class="textbox-icon icon-clear"
                  @click="postData.estimatedDeliveryTime = undefined"></span>
              </Addon>
            </DateBox>
          </FormField>
          <FormField name="deliveryMode" label="送货方式" style="width: 25%">
            <ComboBox v-model="postData.deliveryMode" :data="modeSelection">
            </ComboBox>
          </FormField>
          <FormField v-show="postData.deliveryMode == DeliveryModeType.物流" name="logisticsId" label="物流公司"
            style="width: 25%">
            <ComboBox v-model="postData.logisticsId" :data="logisticsSelection" @selectionChange="getDefTransitPrice">
            </ComboBox>
          </FormField>
          <FormField v-show="postData.deliveryMode == DeliveryModeType.物流" name="transitPrice" label="运费含税单价"
            style="width:25%">
            <NumberBox v-model="postData.transitPrice" :min='0' :precision="2" :spinners="false"
              :value="defTransitPrice">
              <Addon class="kingzon-input-addon">
                <span>{{ '元/Kg' }}</span>
              </Addon>
            </NumberBox>
          </FormField>
          <FormField v-show="postData.deliveryMode == DeliveryModeType.物流" name="costCategoryId" label="费用类别"
            style="width: 25%">
            <ComboBox v-model="postData.costCategoryId" :data="costCategoryList">
            </ComboBox>
          </FormField>
          <FormField name="remark" label="备注" style="width:100%">
            <TextBox v-model="postData.remark"></TextBox>
          </FormField>
          <FormField v-for="(item, index) in customAttributeList" :key="item.systemPara.id"
            :name="item.systemPara.id.toString()" :label="item.systemPara.dataValue" style="width: 50%">
            <ComboBox :data="item.selection" valueField="value" v-model="postData.customAttributeList[index].value"
              @selectionChange="onCustomerAttributeSelectionChange($event, index)">
            </ComboBox>
          </FormField>
          <FormField name="isDirectOutbound" label="出库检验" style="width:100%">
            <CheckBox v-model="postData.isDirectOutbound"></CheckBox>
          </FormField>
          <FormField name="isInventoryTransfer" label="库存调拨" style="width: 50%">
            <div style="display: flex; flex-direction: row;align-items: center;">
              <CheckBox v-model="postData.isInventoryTransfer"></CheckBox>
              <ComboBox style="margin-left: 5px;flex:1;" v-model="transferAreaId" :data="customerAreas"
                v-show="postData.isInventoryTransfer">
              </ComboBox>
            </div>
          </FormField>
          <FormField v-show="postData.isInventoryTransfer" name="transferPositionName" label="库位库区" style="width: 50%">
            <div style="margin-left: 5px;flex:1;display: flex;align-items: center;">
              <TextBox class="box" :disabled="true" v-model="transferPositionName" style="flex:1;"></TextBox>
              <font-awesome-icon :icon="['fas', 'warehouse']" style="margin:0 5px;color:var(--color-blue)"
                class="kingzon-pointer" @click="clickPositon()" />
            </div>
          </FormField>

        </Form>
        <div style="display: flex;flex-direction: row;align-items: center;padding-right: 50px;">
          <span style="font-weight: bold;font-size: 15px;margin-left: 10px;margin-bottom: 10px;">
            发货明细
          </span>
          <LinkButton class="c1" style="margin-left: 10px; margin-bottom: 5px; height: 25px" title="新增销售单"
            @click="openAddSaleOrderDetailDrawer">
            <font-awesome-icon :icon="['fas', 'plus']" />
          </LinkButton>
          <LinkButton style="margin-left: 10px; margin-bottom: 5px; height: 25px" title="清空"
            @click="clearDeliveryOrderDetailList">
            <font-awesome-icon :icon="['fas', 'minus']" />
          </LinkButton>
          <div style="flex:1"></div>
          <div v-if="postData.deliveryMode == DeliveryModeType.物流" style="display: flex;">
            <div>理论总重：{{ getTotalWeight }}kg | 单价：{{ getTransitPrice }}元/kg
            </div>
            <div style="font-weight: 600;margin-left: 20px;margin-right: 5px;">预估含税运费：{{ getTransitCost
            }}元
            </div>
          </div>
          <div v-else>
            <div style="font-weight: 600;margin-left: 20px;margin-right: 5px;">理论总重：{{ getTotalWeight }}kg</div>
          </div>
        </div>
        <KingZonDataGrid id="deliveryDetailDataGrid" ref="deliveryDetailDataGrid" :data="deliveryOrderDetailList"
          :pagination="false" selectionMode="single" @rowSelect="onDataGridRowSelect" style="flex:1;height:0;">
          <GridColumnGroup :frozen="true" align="left" width="160">
            <GridHeaderRow>
              <GridColumn width="40" align="center" field="rowIndex">
                <template #body="scope">
                  {{ scope.rowIndex + 1 }}
                </template>
              </GridColumn>
              <GridColumn field="saleOrderNo" title="销售订单编号" width="120"></GridColumn>
            </GridHeaderRow>
          </GridColumnGroup>
          <GridColumnGroup>
            <GridHeaderRow>
              <GridColumn field="productNo" title="产品编码" width="80"></GridColumn>
              <GridColumn field="productName" title="产品名称" width="100" :overflowTextEllipsis="true"></GridColumn>
              <GridColumn field="specification" title="规格型号" width="100" :overflowTextEllipsis="true"></GridColumn>
              <GridColumn field="technicalCode" title="技术型号" width="80" :overflowTextEllipsis="true"></GridColumn>
              <GridColumn field="packageId" title="外包装方案" width="150" cellCss="kingzon-cell-editable">
                <template #body="scope">
                  <ComboBox v-model="scope.row.packageId" style="width: 100%" :data="getPackageList(scope.row)"
                    valueField="id" textField="packageSpecification" :panelStyle="{ minWidth: '150px' }"
                    @selectionChange="packageChange(scope.row)" @click.stop>
                  </ComboBox>
                  <!-- <span>{{ scope.row.packageId }}</span> -->
                  <!--新增时 默认读取销售订单的外包装方案  如果没有 读取goods的外包装方案，
                   可选下拉列表为使用后端方法  api/sale/SaleOrderManage/GetProductPackageList
                   最终该字段 记录在送货订单明细上，编辑时 读取送货订单明细的外包装方案
                    -->
                </template>
              </GridColumn>
              <GridColumn field="packageNum" title="外包装数量" width="80" :overflowTextEllipsis="true">
                <template #body="scope">
                  <div v-if="scope.row.inputUnitQuantity > 0 && scope.row.packageId > 0 && scope.row.packageNum > 0"
                    :title="scope.row.packageNum + scope.row.packageUnitName + ' ' + scope.row.palletNum + '托盘'">
                    {{ scope.row.packageNum + scope.row.packageUnitName }}<br>
                    {{ scope.row.palletNum + '托盘' }}
                  </div>
                </template>
              </GridColumn>
              <GridColumn field="totalWeight" title="理论重量(Kg)" width="90" :overflowTextEllipsis="true">
              </GridColumn>
              <GridColumn field="deliveryTime" title="客户交期" align="left" width="90" :overflowTextEllipsis="true">
                <template #body="scope">
                  {{ $utils.formatDateTime(scope.row.deliveryTime, "yyyy-MM-dd") }}
                </template>
              </GridColumn>
              <GridColumn field="saleCustomerOrderNo" title="客户订单号" width="100" :overflowTextEllipsis="true">
              </GridColumn>
              <GridColumn field="saleCustomerMaterialNo" title="客户产品图号" width="100" :overflowTextEllipsis="true">
              </GridColumn>
              <GridColumn field="saleRemark" title="下单备注" width="100" :overflowTextEllipsis="true"></GridColumn>
              <GridColumn field="placeOrderQuantity" title="下单数量" align="right" width="120">
                <template #body="scope">
                  <div> {{ scope.row.placeOrderQuantity }}
                    {{ scope.row.salesUnit }}</div>
                  <div style="display: flex;flex-direction: row;justify-content: right;">
                    <div style="font-style: italic;color: var(--color-grey);font-size: 12px;margin-left: 5px;"
                      v-for="unit in scope.row.unitConversionList" :key="unit.unitId">
                      {{ unit.placeOrderQuantity }}{{ unit.unitName }}</div>
                  </div>
                </template>
              </GridColumn>
              <GridColumn field="deliveryQuantity" title="已送货数量" width="120" align="right">
                <template #body="scope">
                  <div> {{ scope.row.actualDeliveryQuantity }}
                    {{ scope.row.salesUnit }}</div>
                  <div style="color: var(--color-grey);font-size: 12px;">
                    {{ scope.row.actualDeliveryQtyByMainUnit }}{{ scope.row.productUnit }}
                  </div>
                </template>
              </GridColumn>
              <GridColumn field="todoQuantity" title="已通知送货待出库" width="120" align="right">
                <template #body="scope">
                  <div> {{ scope.row.todoQuantity }}
                    {{ scope.row.salesUnit }}</div>
                  <div style="color: var(--color-grey);font-size: 12px;">
                    {{ scope.row.todoQtyByMainUnit }}{{ scope.row.productUnit }}
                  </div>
                </template>
              </GridColumn>
            </GridHeaderRow>
          </GridColumnGroup>
          <GridColumnGroup :frozen="true" align="right" width="450">
            <GridHeaderRow>
              <GridColumn field="pendingDeliveryQuantity" title="差欠数量" width="120" align="right">
                <template #body="scope">
                  <div style="display: flex; flex-direction: column;justify-content: center;height:100%">
                    <div>
                      <span>
                        {{ $utils.formatThousandSeparator(getPendingDeliveryQuantity(scope.row), 4) }}
                      </span>
                      <span>{{ scope.row.salesUnit }}</span>
                    </div>

                    <span style="display: inline; color: #ff0f0f; font-size: 12px; margin-left: 5px">
                      (限总量:{{ $utils.formatThousandSeparator(getMaxPendingDeliveryQty(scope.row), 4) }}{{
                        scope.row.salesUnit }})
                    </span>
                  </div>
                </template>
              </GridColumn>
              <GridColumn field="orderInventory" title="订单库存余量" align="right" width="120">
                <template #body="scope">
                  <div>{{ $utils.formatThousandSeparator(getOrderInventoryRemain(scope.row), 4) }}{{ scope.row.salesUnit
                  }}</div>
                  <div style="color:var(--color-yellow); font-size: 12px; margin-left: 5px">(总量:{{
                    $utils.formatThousandSeparator(getOrderInventory(scope.row), 4) }}{{ scope.row.salesUnit }})</div>
                </template>
              </GridColumn>
              <GridColumn field="inputUnitQuantity" title="本次送货量" width="120" align="right">
                <template #body="scope">
                  <div>
                    {{ $utils.formatThousandSeparator(scope.row.inputUnitQuantity, 4) }}
                    {{ new UnitConversionHelper(scope.row.inputUnitConversionId,
                      scope.row.inputUnitQuantity, scope.row.unitInfo).getConversionUnitWithQuantity()?.unitName }}
                  </div>
                  <div class="kingzon-text-subinfo">
                    <span style="display: inline-block; padding-right:2px;"
                      v-for="item in (new UnitConversionHelper(scope.row.inputUnitConversionId, scope.row.inputUnitQuantity, scope.row.unitInfo)).getOtherUnitsWithQuantity()"
                      :key=item.unit.id>
                      {{ item.quantity }}{{ item?.unitName }}
                    </span>
                  </div>
                </template>
              </GridColumn>
              <GridColumn field="confirm" title="确认" align="center" width="40">
                <template #body="scope">
                  <font-awesome-icon v-if="scope.row.isAlreadyOutbound" :icon="['fas', 'check-square']"
                    style="color:var(--color-green-tag); " size="lg" />
                </template>
              </GridColumn>
              <GridColumn field="operate" title="操作" align="center" width="60">
                <template #body="scope">
                  <LinkButton @click="deleteDeliveryOrderDetail(scope.row, scope.rowIndex)">删除
                  </LinkButton>
                </template>
              </GridColumn>
            </GridHeaderRow>
          </GridColumnGroup>
        </KingZonDataGrid>
      </div>
      <div>
        <!-- 分配库存的操作页面 -->
        <Panel title="确认送货数量" :collapsible="false" style="height:100%;width:100%;">
          <InventoryAllocation :type="type" :deliveryOrderDetail="currentSelectedRow" @save="onAllocateSave"
            @reset="onAllocateReset">
          </InventoryAllocation>
        </Panel>
      </div>
    </div>
    <div class="kingzon-btn-group kingzon-page-footer">
      <LinkButton @click="cancel">取消 </LinkButton>
      <LinkButton v-if="type == 'create'" @click="createDeliveryOrderNotice" class="c8">确定</LinkButton>
      <LinkButton v-if="type == 'edit'" @click="onEditSaveClick" class="c8">确定</LinkButton>
    </div>
  </div>
  <!-- 添加销售订单明细 -->
  <Drawer ref="addSaleOrderDetailDrawer" v-if="isAddSaleOrderDetailDrawerShow"
    @collapse="isAddSaleOrderDetailDrawerShow = false" region="east" :drawerStyle="{ width: '95%' }" title="选择销售明细"
    :closable="true">
    <AddSaleOrderDetailPage :customerId="postData.customerId ?? 0" :deliveryOrderId="deliveryOrderId"
      :initSelectedRows="selectedSaleOrderDetailRows" @confirm="onDeliveryOrderDetailAdded"
      @cancle="addSaleOrderDetailDrawer.collapse()">
    </AddSaleOrderDetailPage>
  </Drawer>
  <!-- 新增客户地址的弹窗 -->
  <Dialog ref="addCustomerAdressDialog" :closed="true" :modal="true" :title="'新增收货地址'"
    :dialogStyle="{ height: '80%', width: '600px' }">
    <AddAddress @submit="addSuccess" @close="addCancel" :addressList="addressList" :customerId="postData.customerId">
    </AddAddress>
  </Dialog>
  <!-- 选择库位库区 -->
  <Dialog ref="selectPosition" title="选择库位" :modal="true" closed :draggable="true"
    :dialogStyle="{ height: '85%', width: '85%' }">
    <SelectPosition :dataType="InventoryDataType.Product" :dataId="0" :areaId="transferAreaId"
      :positionId="postData.transferPositionId" :purposes="[WarehousePurpose.客户寄售仓]"
      @close="selectPositionDialog.close()" @confirm="selectPositionConfirm">
    </SelectPosition>
  </Dialog>
</template>

<script lang="ts">
import { BaseCustomer, CustomerAddressDto, SystemParaTreeDto } from '@/types/common'
import { DataType, DisplayType, FilterType, PartnerType, SystemParaType, CostCategoryFeature, DeliveryModeType, UnitConversionType } from '@/types/enums'
import { GetSaleOrderDetailForDeliveryDto, GetDeliveryOrderDto, CreateOrUpdateDeliveryOrderPostDto, DeliveryOrderDetailListDto, CreateOrUpdateDeliveryDetailPostDto, CreateOrUpdateDeliveryInventoryPostDto, InventoryUnitOutboundDto, GetCostCategoryByCostCategoryFeatureDto, ProductIdDto, GetPackageUnitToTargetUnitRatioDto } from '@/types/sale'
import { defineComponent } from 'vue'
import AddSaleOrderDetailPage from './add-sale-order-detail-page.vue'
import AddAddress from '@/views/customer_manage/add-address.vue'
import { CustomAttribute, Filter, ShowFormData, type SelectOption } from '@/types/types'
import InventoryAllocation, { OccupyInfoDto } from './inventory-allocation.vue'
import { UnitConversionHelper } from '@/utils/UnitConversionHelper.js'
import Decimal from 'decimal.js'
import SelectPosition from '../inventory/select-position.vue'
import { InventoryDataType, WarehousePurpose, WarehouseOwnerType, StorageLocation_PositionDTO } from '@/views/delivery_order_manage/inventory/inventory.js'
import { cloneDeep, get } from 'lodash';

class SystemParaTreeDtoExtension extends SystemParaTreeDto {
  selection: Array<SelectOption> = []
}
export default defineComponent({
  name: 'LaunchDeliveryOrder',
  emits: ['cancel', 'success'],
  components: {
    AddSaleOrderDetailPage,
    AddAddress,
    InventoryAllocation,
    SelectPosition,
  },
  props: {
    type: {
      type: String,
      default: 'create',
      validator: (value: string) => ['create', 'edit'].includes(value)
    },
    //新增的时候，可以传入销售订单详情的Ids
    saleOrderDetailIds: {
      type: Array,
      default: () => [] as number[]
    },
    customerId: {
      type: Number,
      required: false
    },
    isCanChangeCustomer: {
      type: Boolean,
      default: true
    },
    //type=edit 时，需要传入 送货通知单的Id
    deliveryOrderId: {
      type: Number,
      required: false
    }
  },
  data() {
    return {
      isLoading: false,
      deliveryOrder: {
        customer: ""
      },
      addressList: [] as CustomerAddressDto[],
      deliveryOrderDetailList: [] as DeliveryOrderDetailListDto[],
      customAttributeList: [] as SystemParaTreeDtoExtension[],
      maximumDeliveryQtySystemParaList: [] as SystemParaTreeDto[],
      maximumDeliveryLimitQty: 0 as number,
      allCustomers: [] as BaseCustomer[],
      selectAddressIndex: undefined as number | undefined,
      postData: new CreateOrUpdateDeliveryOrderPostDto(),
      isAddSaleOrderDetailDrawerShow: false,
      addSaleOrderDetailDrawer: {} as EasyUI.Drawer,
      addCustomerAddressDialog: {} as EasyUI.Dialog,
      form: undefined as EasyUI.Form | undefined,
      rules: {
        customerId: {
          required: true,
        }
      },
      messages: {
        customer: {
          required: "*请先选择客户"
        }
      },
      currentSelectedRow: undefined as DeliveryOrderDetailListDto | undefined,
      currentSelectedIndex: -1 as number,
      showFormData: [] as ShowFormData[],
      deliveryDetailDataGrid: undefined as EasyUI.DataGrid | undefined,
      selectPositionDialog: {} as EasyUI.Dialog,
      customerAreas: [] as any,
      transferAreaId: undefined as number | undefined,
      transferPositionName: undefined as string | undefined,
      isInit: false,
      costCategoryList: [] as SelectOption[],
      modeSelection: [] as SelectOption[],
      logisticsSelection: [] as SelectOption[],
      defTransitPrice: 0 as number, //系统获取的运费单价
      province: null as string | null,
      city: null as string | null,
      county: null as string | null,
      deliveryOrderPackageList: [] as ProductIdDto[],  // 所有销售订单的所有包装方案
      originLogisticsCompanyId: 0 as number,  // 编辑时获取的原始数据
      originTransitPrice: 0 as number,  // 编辑时获取的原始数据
    }
  },
  computed: {
    selectedSaleOrderDetailRows(): Array<{ id: number }> {
      var rows = this.deliveryOrderDetailList.map(item => {
        return { id: item.saleOrderDetailId };
      });
      return rows
    },
    PartnerType() {
      return PartnerType
    },
    FilterType() {
      return FilterType
    },
    UnitConversionHelper() {
      return UnitConversionHelper
    },
    InventoryDataType() {
      return InventoryDataType
    },
    WarehousePurpose() {
      return WarehousePurpose
    },
    getTransitPrice() {
      return this.postData.transitPrice == null ? this.defTransitPrice : this.postData.transitPrice; //如果用户未填入运费单价，则使用系统获取的默认运费单价(用户可以填0)
    },
    getLogisticsId() {
      return this.postData.logisticsId ?? this.getSelectedAddress?.logisticsCompanyId; //如果用户未选择物流公司，则使用客户地址的默认物流公司
    },
    getSelectedAddress() {
      return this.addressList.find(o => o.index == this.selectAddressIndex)
    },
    getTotalWeight() {
      const sum = this.deliveryOrderDetailList.reduce(
        (acc, x) => acc.plus(x.totalWeight ?? 0),
        new Decimal(0)
      ).toNumber()
      return sum;
    },
    //预估运费
    getTransitCost() {
      return Decimal.mul(this.getTransitPrice ?? 0, this.getTotalWeight ?? 0).toDecimalPlaces(2).toNumber()
    },
    DeliveryModeType() {
      return DeliveryModeType;
    },
    addressLogisticsCompanyId() {
      return this.getSelectedAddress?.logisticsCompanyId
    }
  },
  watch: {
    transferAreaId: {
      handler: async function (val) {
        if (val) {
          this.isLoading = true
          try {
            await this.getDefPosition()
          } catch (error) {
            this.$kingzon_messager.showError(error)
          } finally {
            this.isLoading = false
          }
        } else {
          this.transferPositionName = ""
          this.postData.transferPositionId = undefined
        }
      },
      deep: true
    },
    getTotalWeight: {  // 重量变化要重新获取重量区间的单价
      handler: async function () {
        await this.getDefTransitPrice()
      },
      deep: true
    }
  },
  async mounted() {
    this.deliveryDetailDataGrid = this.$refs.deliveryDetailDataGrid as EasyUI.DataGrid
    this.addCustomerAddressDialog = this.$refs.addCustomerAdressDialog as EasyUI.Dialog
    this.form = this.$refs.form as EasyUI.Form
    this.selectPositionDialog = this.$refs.selectPosition as EasyUI.Dialog
    this.modeSelection = this.$utils.getEnumKeyValueArray(DeliveryModeType)
    this.isInit = true
    this.isLoading = true
    try {
      await this.init()
    } catch (error) {
      this.$kingzon_messager.showError(error)
    } finally {
      this.isLoading = false
      this.isInit = false
    }
  },

  methods: {
    async init() {
      await this.getAllCustomer();
      this.postData.customerId = this.customerId;

      if (this.customerId) {
        await this.getCustomerAddressById(this.customerId)
      }

      await this.getDeliveryMethodSystemPara()
      await this.getMaximumDeliveryQtySystemPara()
      await this.getAreaData()

      await this.getCostCategoryList()
      await this.getLogisticsList()

      if (this.type == 'edit') {
        if (!this.deliveryOrderId) {
          this.$kingzon_messager.showError("送货单数据错误，请刷新重试")
          return;
        }

        await this.getDeliveryOrderInfo(this.deliveryOrderId)
      }

      if (this.type == 'create' && this.saleOrderDetailIds.length > 0) {
        await this.getChoosedSaleOrderDeliveryInfo(this.saleOrderDetailIds)
      }

    },
    //通过Id获取客户对应的地址信息
    async getCustomerAddressById(customerId?: number) {
      if (!customerId) {
        this.addressList = []
        this.selectAddressIndex = undefined
        return
      }
      const result = await this.$http.get('/api/base/Customer/GetCustomerAddressList', {
        customerId: customerId
      })
      if (result.data) {
        const addressList = result.data as CustomerAddressDto[]
        this.addressList = addressList.map((x) => ({
          ...x,
          displayText: (x.contact ? x.contact + '-' : "") + (x.phone ? x.phone + '-' : "") + x.address + (x.remark ? '【' + x.remark + '】' : '')
        }))
      }
    },
    //预计送达时间,物流公司，运费含税单价要变
    async onAddressChange(address: CustomerAddressDto) {
      if (this.isInit) {
        return
      }
      if (!address) {
        this.postData.estimatedDeliveryTime = undefined
        return
      }
      this.postData.customerAddress = address;
      const logisticsCycle = address.logisticsCycle || 0
      const date = new Date()
      date.setDate(date.getDate() + logisticsCycle)
      this.postData.estimatedDeliveryTime = date
      this.postData.logisticsId = address.logisticsCompanyId
      await this.getDefTransitPrice()
    },
    //获取所有的送货拓展参数
    async getDeliveryMethodSystemPara() {
      const result = await this.$http.get('/api/base/SystemPara/GetAdditionalParam', {
        type: SystemParaType.送货扩展参数
      })
      this.customAttributeList = result.data as SystemParaTreeDtoExtension[];
      this.customAttributeList.forEach((item, index) => {
        this.postData.customAttributeList[index] = new CustomAttribute();
        this.postData.customAttributeList[index].systemParaId = item.systemPara.id
        if (item.systemPara.displayType == DisplayType.Select) {
          item.selection = item.childrenSystemPara.map(s => {
            return {
              value: s.systemPara.id,
              text: s.systemPara.dataValue,
            } as SelectOption
          })
        }
      })
    },
    //获取最大送货数量限制的附加参数
    async getMaximumDeliveryQtySystemPara() {
      const result = await this.$http.get('/api/base/SystemPara/GetAdditionalParam', {
        type: SystemParaType.最大送货量参数
      })
      this.maximumDeliveryQtySystemParaList = result.data as SystemParaTreeDto[]
      this.maximumDeliveryLimitQty = Number(
        this.maximumDeliveryQtySystemParaList[0]?.systemPara.dataValue
      )
    },
    //获取所有的客户
    async getAllCustomer() {
      try {
        const result = await this.$http.get('/api/base/Customer/GetAllCustomer')
        this.allCustomers = result.data as BaseCustomer[];
        this.postData.customerId = this.customerId;
      } catch (error) {
        this.$kingzon_messager.showError(error)
      }
    },

    //新增的时候获取所有的送货明细数据
    async getChoosedSaleOrderDeliveryInfo(saleOrderDetailIds) {
      try {
        this.isLoading = true
        const result = await this.$http.post(
          '/api/inventory/DeliveryOrder/GetSaleOrderDetailForDeliveryByIds',
          saleOrderDetailIds
        )
        const data = result.data as DeliveryOrderDetailListDto[]
        if (data) {
          this.initDeliveryOrderDetailList(data)
          data.forEach(item => {
            this.deliveryOrderDetailList.push(item)
          })
          await this.getDeliveryOrderPackageList(this.deliveryOrderDetailList)
          if (this.deliveryOrderDetailList.length > 0 && !this.currentSelectedRow) {
            this.currentSelectedRow = this.deliveryOrderDetailList[0]
            this.currentSelectedIndex = 0
            //选中行   默认选中第一行
            this.deliveryDetailDataGrid?.dataGrid.selectRow(this.currentSelectedRow)
          }
        }
      } catch (error) {
        this.$kingzon_messager.showError(error)
      } finally {
        this.isLoading = false
      }
    },
    //添加选择的销售订单明细
    openAddSaleOrderDetailDrawer() {
      if (!this.postData.customerId) {
        this.$kingzon_messager.showErrorTip("请先选择客户")
        return
      }
      this.isAddSaleOrderDetailDrawerShow = true
      this.$nextTick(() => {
        this.addSaleOrderDetailDrawer = this.$refs.addSaleOrderDetailDrawer as EasyUI.Drawer
        this.addSaleOrderDetailDrawer.expand()
      })
    },
    //处理用户再次勾选传过来的值
    onDeliveryOrderDetailAdded(tempSelectedIds: number[]) {
      this.addSaleOrderDetailDrawer.collapse()
      var currentSaleOrderDetailIds = this.deliveryOrderDetailList.map(x => x.saleOrderDetailId)
      const existsIds = new Set(currentSaleOrderDetailIds)
      const newIds = new Set(tempSelectedIds)

      const addIds = tempSelectedIds.filter(o => !existsIds.has(o))
      const deleteIds = currentSaleOrderDetailIds.filter(o => !newIds.has(o))

      deleteIds.forEach(item => {
        var index = this.deliveryOrderDetailList.findIndex(o => o.saleOrderDetailId == item)
        if (index >= 0) {
          this.deliveryOrderDetailList.splice(index, 1);
        }
      })

      if (addIds.length > 0) {
        this.getChoosedSaleOrderDeliveryInfo(addIds)
      }
    },
    //打开添加客户地址的弹窗
    openAddCustomerAddressPage() {
      this.addCustomerAddressDialog.open()
    },
    cancel() {
      this.$emit('cancel')
    },
    addCancel() {
      this.addCustomerAddressDialog.close()
    },
    async addSuccess(newAddressIndex) {
      this.addCustomerAddressDialog.close()

      this.isLoading = true
      try {
        await this.getCustomerAddressById(this.postData.customerId)
        var newAddress = this.addressList.find(o => o.index == newAddressIndex)
        if (newAddressIndex && !this.selectAddressIndex && newAddress) {
          this.selectAddressIndex = newAddressIndex //如果之前没有选择地址，则默认选择新增的地址
        }
      } catch (error) {
        this.$kingzon_messager.showError(error)
      } finally {
        this.isLoading = false
      }

    },
    onCustomerAttributeSelectionChange(event, index) {
      this.postData.customAttributeList[index].value = event.text
    },
    //获取订单的基础信息
    async getDeliveryOrderInfo(deliveryOrderId: Number) {
      try {
        this.isLoading = true
        const result = await this.$http.get('/api/inventory/DeliveryOrder/GetDeliveryOrderInfo', {
          orderId: deliveryOrderId
        })
        const data = result.data as GetDeliveryOrderDto
        this.deliveryOrderDetailList = data.deliveryOrderDetailList as DeliveryOrderDetailListDto[]
        await this.getDeliveryOrderPackageList(this.deliveryOrderDetailList)
        this.postData.id = data.id
        this.postData.remark = data.remark
        this.postData.isDirectOutbound = data.isDirectOutbound
        this.postData.isInventoryTransfer = data.isInventoryTransfer
        this.postData.customerId = data.customerId
        this.postData.estimatedDeliveryTime = data.estimatedDeliveryTime ? new Date(data.estimatedDeliveryTime) : undefined
        this.originLogisticsCompanyId = data.logisticsId ?? 0
        this.originTransitPrice = data.transitPrice ?? 0
        if (data.transferAreaId) {
          this.transferAreaId = data.transferAreaId
          this.postData.transferPositionId = data.transferPositionId
          this.transferPositionName = data.transferPositionName
        }
        //获取客户的地址信息（内部设置selectAddressIndex会触发onAddressChange，编辑模式已被屏蔽）
        await this.getCustomerAddressById(data.customerId)
        if (data.customerAddress) {
          var index = this.addressList.findIndex(o => o.index == data.customerAddress?.index)
          if (index < 0) {
            this.addressList.push(data.customerAddress)
          }
          this.selectAddressIndex = data.customerAddress.index
        }
        //必须在selectAddressIndex触发的联动之后赋值，保证后端保存的值不会被地址默认值覆盖
        if (data.customAttributesList && data.customAttributesList.length > 0) {
          data.customAttributesList.forEach((attr) => {
            const index = this.postData.customAttributeList.findIndex((item) => item.systemParaId === attr.systemParaId)
            if (index >= 0) {
              this.postData.customAttributeList[index].value = attr.value
            }
          })
        }
        this.postData.deliveryMode = data.deliveryMode
        this.postData.logisticsId = data.logisticsId
        this.postData.transitPrice = data.transitPrice
        this.postData.costCategoryId = data.costCategoryId
        this.initDeliveryOrderDetailList(this.deliveryOrderDetailList) //放在地址获取之后 
        if (this.deliveryOrderDetailList.length > 0) {
          this.currentSelectedRow = this.deliveryOrderDetailList[0]
          this.deliveryDetailDataGrid?.dataGrid.selectRow(this.currentSelectedRow)
        }
      } catch (error) {
        this.$kingzon_messager.showError(error)
      } finally {
        this.isLoading = false
        this.getDeliveryOrderDetailListWeight()
      }
    },
    async onCustomerChange(item: BaseCustomer) {
      if (this.isInit) {
        return
      }
      // console.log("onCustomerChange", item)
      if (this.type == 'edit') {
        return;
      }
      this.isLoading = true
      try {
        this.deliveryOrderDetailList = []
        this.currentSelectedRow = undefined
        this.selectAddressIndex = undefined
        if (item && item.id) {
          await this.getCustomerAddressById(item.id)
        }
      } catch (error) {
        this.$kingzon_messager.showError(error)
      } finally {
        this.isLoading = false
      }
      var areaId = item && item.id && this.customerAreas.find(x => x.ownerType == WarehouseOwnerType.客户 && x.ownerId == item.id)?.value
      this.transferAreaId = areaId ? Number(areaId) : undefined
    },
    //获取待通知的送货数量
    getPendingDeliveryQuantity(row: DeliveryOrderDetailListDto): number {
      //下单数量-已通知送货数量
      const pendingQty = Decimal.sub(row.placeOrderQuantity || 0, row.actualDeliveryQuantity || 0).sub(row.todoQuantity || 0).toNumber()
      return pendingQty
    },
    getMaxPendingDeliveryQty(row: DeliveryOrderDetailListDto): number {
      //下单数量-已通知送货数量
      var maxDeliveryQty = Decimal.mul(row.placeOrderQuantity || 0, (1 + this.maximumDeliveryLimitQty))
      const pendingQty = Decimal.sub(maxDeliveryQty, row.actualDeliveryQuantity || 0).sub(row.todoQuantity || 0).toNumber()
      return pendingQty
    },
    getOrderInventory(row: DeliveryOrderDetailListDto): number {
      return Decimal.add(row.planDirectDliveryQuantity || 0, row.totalInputProductionQty || 0).add(row.purchaseInventoryQuantity || 0).add(row.productionInventoryQuantity || 0).toNumber();
    },
    getOrderInventoryRemain(row: DeliveryOrderDetailListDto): number {
      return Decimal.sub(this.getOrderInventory(row), row.actualDeliveryQuantity || 0).sub(row.todoQuantity || 0).toNumber();
    },
    //初始化送货明细列表
    initDeliveryOrderDetailList(rows: DeliveryOrderDetailListDto[]): void {
      rows.forEach((item) => {
        //如果送货单没有地址  
        if (!this.selectAddressIndex && item.saleCustomerAddress) {
          this.selectAddressIndex = item.saleCustomerAddress.index
        }

        item.isAlreadyOutbound = item.inputUnitQuantity > 0 //判断本次送货量是否大于0
          && !(item.inventoryRecordList?.some(record => (record.occupyInputQuantity ?? 0) > 0 && record.originRemainingQuantity < record.originOccupyQuantity) ?? false) //判断 可用库存 是否充足
        item.isAllocate = item.inventoryRecordList && item.inventoryRecordList.length > 0
          && (item.occupyQuantityByMain ?? 0) > 0 //判断 占用量是否大于0 大于0则表示已占用库存
        item.inventoryRecordList.forEach((record) => {
          if (record.deliveryDetailOutboundOccupyQuantity > 0) {
            record.deliveryDetailOccupyQuantity += record.deliveryDetailOutboundOccupyQuantity  //销售送货占用 + 库房出库占用
          }
        })
        item.inputUnitConversionId = item.inputUnitConversionId ?? item.salesUnitConversionId //如果没有选择过单位，则默认使用销售单位
        item.occupyInputQtyByMainUnit = item.deliveryQtyByMainUnit
        item.occupyInputQtyBySalesUnit = item.deliveryQuantity
        item.packageId = item.packageId ? item.packageId : 0  // 将null初始化为0（初始化期间不要触发下拉框联动）
        item.backupInfo = {
          inputUnitConversionId: item.inputUnitConversionId,
          inputUnitQuantity: item.inputUnitQuantity,
          isAllocate: item.isAllocate,
          isAlreadyOutbound: item.isAlreadyOutbound,
          inventoryRecordList: item.inventoryRecordList.filter(record => (record.occupyInputQuantity ?? 0) > 0).map(record => {
            var tmp = Object.assign({}, record)
            tmp.deliveryDetailOccupyQuantity = record.deliveryDetailOccupyQuantity
            return tmp
          })
        }
      })
    },
    onDataGridRowSelect(row: DeliveryOrderDetailListDto) {
      this.currentSelectedRow = row
      this.currentSelectedIndex = this.deliveryOrderDetailList.findIndex(o => row === o)
    },
    async onAllocateSave(value: OccupyInfoDto) {
      if (!this.currentSelectedRow) {
        return
      }
      if (this.currentSelectedRow.packageId == null) {
        this.$kingzon_messager.showWarning('未选择外包装方案')
        return
      }
      //做最大值验证
      if (value.occupyInputQtyBySalesUnit > this.getMaxPendingDeliveryQty(this.currentSelectedRow)) {
        this.$kingzon_messager.showError(`${this.currentSelectedRow.saleOrderNo}【${this.currentSelectedRow.productNo}】本次送货量${value.occupyInputQtyBySalesUnit}，超过了差欠数${this.getMaxPendingDeliveryQty(this.currentSelectedRow)}，请修改！`)
        return
      }
      this.currentSelectedRow.unitInfo = value.unitInfo
      this.currentSelectedRow.occupyInputQtyBySalesUnit = value.occupyInputQtyBySalesUnit
      this.currentSelectedRow.occupyInputQtyByMainUnit = value.occupyInputQtyByMainUnit
      this.currentSelectedRow.inputUnitConversionId = value.occupyInputUnitConversionId
      this.currentSelectedRow.inputUnitQuantity = value.occupyInputQuantity
      this.currentSelectedRow.isAllocate = value.isAllocate
      this.currentSelectedRow.isAlreadyOutbound = this.currentSelectedRow.inputUnitQuantity > 0 ? true : false;
      this.currentSelectedRow.inventoryRecordList.forEach((record) => {
        var tmp = value.inventoryOccupyDetails.find(item => item.inventoryRecordId === record.inventoryRecordId)
        if (tmp) {
          record.occupyInputUnitConversionId = tmp.occupyInputUnitConversionId
          record.occupyInputQuantity = tmp.occupyInputQuantity
          record.originOccupyQuantity = tmp.originOccupyQuantity
          record.deliveryDetailOccupyQuantity = tmp.detailOccupyQuantityByMain
        } else {
          record.occupyInputUnitConversionId = undefined
          record.occupyInputQuantity = undefined
        }
      })

      //需要改掉其它明细中，相应库存记录的总的占用量
      this._changeOtherOriginOccupyQuantity()
      //送货量/单位变了，重新计算明细重量（影响总重与预估运费）
      await this.getDeliveryOrderDetailListWeight(this.currentSelectedRow)

      //自动选中下一行 
      if (this.currentSelectedIndex < this.deliveryOrderDetailList.length - 1) {
        this.currentSelectedRow = this.deliveryOrderDetailList[++this.currentSelectedIndex]
        this.deliveryDetailDataGrid?.dataGrid.selectRow(this.currentSelectedRow)
      }
    },
    async onAllocateReset() {
      if (!this.currentSelectedRow) {
        return
      }
      this.currentSelectedRow.inputUnitConversionId = this.currentSelectedRow.backupInfo?.inputUnitConversionId ?? undefined
      this.currentSelectedRow.inputUnitQuantity = this.currentSelectedRow.backupInfo.inputUnitQuantity
      var inventoryMap = new Map<number, any>(this.currentSelectedRow.backupInfo.inventoryRecordList.map(item => [item.inventoryRecordId, item]));
      this.currentSelectedRow.isAlreadyOutbound = this.currentSelectedRow.backupInfo?.isAlreadyOutbound ?? false
      this.currentSelectedRow.isAllocate = this.currentSelectedRow.backupInfo?.isAllocate ?? false
      this.currentSelectedRow.inventoryRecordList.forEach((record) => {
        var tmp = inventoryMap.get(record.inventoryRecordId)
        if (tmp) {
          record.occupyInputUnitConversionId = tmp.occupyInputUnitConversionId
          record.occupyInputQuantity = tmp.occupyInputQuantity
          var diffOccupyQuantity = Decimal.sub(tmp.deliveryDetailOccupyQuantity ?? 0, record.deliveryDetailOccupyQuantity ?? 0)
          record.deliveryDetailOccupyQuantity = tmp.deliveryDetailOccupyQuantity
          record.originOccupyQuantity = Decimal.add(record.originOccupyQuantity ?? 0, diffOccupyQuantity).toNumber()
        } else {
          record.occupyInputUnitConversionId = undefined
          record.occupyInputQuantity = undefined
          record.deliveryDetailOccupyQuantity = 0
          record.originOccupyQuantity = 0
        }
      })
      //???需要测试
      this.currentSelectedRow = cloneDeep(this.currentSelectedRow)

      //需要改掉其它明细中，相应库存记录的总的占用量
      this._changeOtherOriginOccupyQuantity()
      //重置后送货量变了，重新计算明细重量
      await this.getDeliveryOrderDetailListWeight(this.currentSelectedRow)
    },
    _changeOtherOriginOccupyQuantity() {
      this.deliveryOrderDetailList.forEach((item) => {
        if (item.deliveryOrderDetailId === this.currentSelectedRow?.deliveryOrderDetailId) {
          return
        }
        var inventoryMap = new Map<number, any>(this.currentSelectedRow?.inventoryRecordList?.map(item => [item.inventoryRecordId, item]));
        item.inventoryRecordList.forEach((record) => {
          var tmp = inventoryMap.get(record.inventoryRecordId)
          if (tmp) {
            record.originOccupyQuantity = tmp.originOccupyQuantity
          }
        })
      })
    },
    selectPositionConfirm(position: StorageLocation_PositionDTO) {
      this.postData.transferPositionId = position.id;
      this.transferPositionName = position.fullName;
      this.selectPositionDialog.close();
    },
    async getAreaData() {
      var result = await this.$http.get('/api/inventory/InventoryManagement/GetUserStorageAreaList', {
        purpose: WarehousePurpose.客户寄售仓
      })
      this.customerAreas = result.data;
    },
    clickPositon() {
      if (!this.transferAreaId) {
        this.$kingzon_messager.showErrorTip("请先选择库房")
        return
      }
      this.selectPositionDialog.open()
    },
    async getDefPosition() {
      var result = await this.$http.post('/api/inventory/InventoryManagement/GetInboundDefPosition',
        {
          dataType: InventoryDataType.Product,
          areaId: this.transferAreaId,
          purposes: [WarehousePurpose.客户寄售仓],
          filterCategory: false
        })
      var data = result.data as any;
      this.postData.transferPositionId = data.positionId
      this.transferPositionName = data.positionInfo;
    },
    clearDeliveryOrderDetailList() {
      if (this.deliveryOrderDetailList.length <= 0) {
        return;
      }
      this.$kingzon_messager.showConfim("是否清空发货明细·？")
        .then(async () => {
          this.currentSelectedRow = undefined
          this.currentSelectedIndex = -1
          this.deliveryOrderDetailList = []
        })

    },
    deleteDeliveryOrderDetail(row: DeliveryOrderDetailListDto, rowIndex: number) {
      this.$kingzon_messager.showConfim("是否删除" + row.saleOrderNo + "的发货明细？")
        .then(async () => {
          this.deliveryOrderDetailList.splice(rowIndex, 1)
          if (rowIndex === this.currentSelectedIndex) {
            this.currentSelectedRow = undefined
            this.currentSelectedIndex = -1

          }
        })
    },
    checkData(): boolean {
      if (!this.deliveryOrderDetailList || this.deliveryOrderDetailList.length === 0) {
        this.$kingzon_messager.showWarning("请至少添加一条明细数据!")
        return false
      }
      if (this.postData.isInventoryTransfer && !this.postData.transferPositionId) {
        this.$kingzon_messager.showWarning("请选择调拨库房库区库位!")
        return false
      }

      //做最大值验证
      for (const row of this.deliveryOrderDetailList) {
        if (!row.inputUnitQuantity) {
          this.$kingzon_messager.showError(`${row.saleOrderNo}【${row.productNo}】请填写本次送货量！`)
          return false
        }
        //单位必填
        if (!row.inputUnitConversionId) {
          this.$kingzon_messager.showError(`${row.saleOrderNo}【${row.productNo}】请填写本次送货量的单位！`)
          return false
        }
        var maxAllowedQuantity = this.getMaxPendingDeliveryQty(row)
        if (row.inputUnitQuantity > maxAllowedQuantity) {
          this.$kingzon_messager.showError(`${row.saleOrderNo}【${row.productNo}】本次送货量${row.inputUnitQuantity}，超过了差欠数${maxAllowedQuantity}，请修改！`)
          return false
        }
      }

      var unconfirmeData = this.deliveryOrderDetailList.filter(x => !x.isAlreadyOutbound);
      if (unconfirmeData.length > 0) {
        this.$kingzon_messager.showError("存在‘未确认’送货明细，请确认。")
        return false;
      }

      return true;
    },
    createPostData() {
      const detailList = this.deliveryOrderDetailList.map((row) => {
        return {
          deliveryOrderDetailId: row.deliveryOrderDetailId,
          isAllocate: row.isAllocate,
          saleOrderDetailId: row.saleOrderDetailId,
          saleOrderNo: row.saleOrderNo,
          salesUnit: row.salesUnit,
          goodsId: row.goodsId,
          productId: row.productId,
          specificationId: row.specificationId,
          productNo: row.productNo,
          productUnitId: row.productUnitId,
          packageId: row.packageId ? row.packageId : null,
          palletNum: row.packageNum,
          deliveryQuantity: row.occupyInputQtyByMainUnit, //主单位
          inputQuantityBySale: row.occupyInputQtyBySalesUnit, //销售单位
          inputQuantity: row.inputUnitQuantity, //选中的单位
          inputUnitConversionId: row.inputUnitConversionId,
          unitInfo: row.unitInfo.map((unit) => {
            return {
              id: unit.id,
              unitId: unit.unitId,
              unitName: unit.unitName,
              ratio: unit.ratio,
              remark: unit.remark,
              conversionType: unit.conversionType,
              outboundQuantity: unit.quantity,
            } as InventoryUnitOutboundDto
          }),
          inventoryRecordList: row.inventoryRecordList.filter(record => record.occupyInputQuantity).map((record) => {
            var helper = new UnitConversionHelper(record.occupyInputUnitConversionId ?? 0, record.occupyInputQuantity ?? 0, record.inventoryUnitsInfoList)
            return {
              inventoryRecordId: record.inventoryRecordId,
              inventoryRecordNo: record.inventoryRecordNo,
              positionId: record.positionId,
              occupyQuantity: record.deliveryDetailOccupyQuantity, //主单位
              inputQuantity: record.occupyInputQuantity, //选中单位
              inputUnitConversionId: record.occupyInputUnitConversionId,
              unitInfo: helper.getAllUnitWithQuantity().map((x) => {
                var unit = x.unit;
                return {
                  id: unit.id,
                  unitId: unit.unitId,
                  unitName: unit.unitName,
                  ratio: unit.ratio,
                  remark: unit.remark,
                  conversionType: unit.conversionType,
                  outboundQuantity: x.quantity,
                } as InventoryUnitOutboundDto
              })
            } as CreateOrUpdateDeliveryInventoryPostDto
          })
        } as CreateOrUpdateDeliveryDetailPostDto;
      })
      //给postData赋值       
      this.postData.deliveryOrderDetailList = detailList
      const selectCustomerAddress = this.addressList.find(item => item.index === this.selectAddressIndex)
      this.postData.customerAddress = selectCustomerAddress
      this.postData.transferPositionId = this.postData.isInventoryTransfer ? this.postData.transferPositionId : undefined
      this.postData.transitCost = this.getTransitCost
      this.postData.totalWeight = this.getTotalWeight
      this.postData.transitPrice = this.getTransitPrice
      this.postData.deliveryMode = this.postData.deliveryMode != null ? Number(this.postData.deliveryMode) as DeliveryModeType : undefined
    },
    //生成送货通知
    async createDeliveryOrderNotice() {
      if (!this.form?.valid) {
        this.$kingzon_messager.showWarning("请选择客户")
        return;
      }
      if (!this.checkData()) {
        return
      }
      this.createPostData();
      //给后端发送组装好的数据  
      try {
        this.isLoading = true
        var res = await this.$http.post('/api/inventory/DeliveryOrder/CreateDeliveryOrder', this.postData) as any
        this.$kingzon_messager.showInfo(res.data)
        this.$emit('success')
      } catch (error) {
        this.$kingzon_messager.showError(error)
      } finally {
        this.isLoading = false
      }
    },
    //修改送货通知
    async onEditSaveClick() {
      if (!this.form?.valid) {
        this.$kingzon_messager.showWarning("请选择客户")
        return;
      }
      if (!this.checkData()) {
        return
      }
      this.createPostData();
      try {
        this.isLoading = true
        var res = await this.$http.post('/api/inventory/DeliveryOrder/UpdateDeliveryOrder', this.postData) as any
        this.$kingzon_messager.showInfo(res.data)
        this.$emit('success')
      } catch (error) {
        this.$kingzon_messager.showError(error)
      } finally {
        this.isLoading = false
      }
    },
    async getCostCategoryList() {
      var res = await this.$http.get('/api/base/Category/GetCostCategoryByCostCategoryFeature', { feature: CostCategoryFeature.运费 })
      var data = res.data as GetCostCategoryByCostCategoryFeatureDto[]
      this.costCategoryList = data.map(x => ({
        value: x.costCategoryId,
        text: x.name,
      } as SelectOption))
      //设置默认费用类别
      if (this.costCategoryList.length > 0) {
        this.postData.costCategoryId = Number(this.costCategoryList[0].value)
      }
    },
    async getLogisticsList() {
      const result = await this.$http.get('/api/base/SystemPara/GetAdditionalParam', {
        type: SystemParaType.物流公司
      })
      var data = result.data as SystemParaTreeDto[]
      this.logisticsSelection = data.map(x => ({
        value: x.systemPara.id,
        text: x.systemPara.dataValue,
      } as SelectOption))
    },

    // 物流公司变化，总重量变化，地址变化会重新获取默认单价
    // 优先级：地址记录的运费 < Base_LogisticsPrice物流单价表根据省市县和重量查出的运费 < 送货订单记录的运费(新增的时候没有，编辑的时候才考虑)
    async getDefTransitPrice() {
      if (!this.getLogisticsId) {
        this.defTransitPrice = 0
        return
      }

      this.defTransitPrice = 0  // 显示的默认单价
      var addressPrice = 0  // 地址记录的单价
      var logisticsPrice = 0  // 按照物流公司+地址+重量查出来的单价
      var deliveryOrderPrice = this.originTransitPrice ?? 0  // 编辑时的原始单价

      const address = this.addressList.find(o => o.index == this.selectAddressIndex)
      this.province = address?.province as any
      this.city = address?.city as any
      this.county = address?.county as any
      addressPrice = address?.logisticsPrice ?? 0
      if (this.getTotalWeight == 0) {  // 重量等于0，不考虑从后端按照重量区间获取单价，只考虑地址单价和编辑时的初始单价
        if (this.postData.logisticsId == this.originLogisticsCompanyId && deliveryOrderPrice) {
          this.defTransitPrice = deliveryOrderPrice
          return
        }
        if (this.postData.logisticsId == address?.logisticsCompanyId && addressPrice) {
          this.defTransitPrice = addressPrice
          return
        }
        return
      }
      this.isLoading = true
      try {
        var result = await this.$http.post('/api/base/LogisticsPrice/GetLogisticsPriceByLocationAndWeight', {
          logisticsId: this.getLogisticsId,
          province: this.province,
          city: this.city,
          county: this.county,
          weight: this.getTotalWeight
        })
        var data = result.data as any
        if (data) {
          logisticsPrice = data.priceHaveTax as number
        }

        //当送货订单有默认运费时取默认运费，没有时按照重量区间获取，当未获取到物流单价 取地址上的单价
        if (deliveryOrderPrice && this.postData.logisticsId == this.originLogisticsCompanyId) {
          this.defTransitPrice = deliveryOrderPrice
        } else if (logisticsPrice) {
          this.defTransitPrice = logisticsPrice
        } else if (addressPrice && this.postData.logisticsId == address?.logisticsCompanyId) {
          this.defTransitPrice = addressPrice
        }
      } catch (error) {
        this.$kingzon_messager.showError(error)
      } finally {
        this.isLoading = false
      }
    },
    async getDeliveryOrderPackageList(data) {
      var postData = [] as ProductIdDto[]
      const map = new Map<string, ProductIdDto>();
      data.forEach(x => {
        const idField = `${x.productId}-${x.specificationId}`;
        if (!map.has(idField)) {
          map.set(idField, {
            idField,
            productId: x.productId,
            specificationId: x.specificationId,
          });
        }
      });
      postData = Array.from(map.values())

      this.isLoading = true
      try {
        const res = await this.$http.post('/api/sale/SaleOrderManage/GetProductPackageList', postData)
        const data = res.data as ProductIdDto[]
        this.deliveryOrderPackageList = data
      } catch (er) {
        this.$kingzon_messager.showError(er)
      } finally {
        this.isLoading = false
      }
    },
    getPackageList(row) {
      const idField = `${row.productId}-${row.specificationId}`
      const matched = this.deliveryOrderPackageList?.find(x => x.idField == idField)
      const list = matched?.packageList ?? [] // 返回数组，且永远兜底空数组
      return [
        { packageSpecification: '无外包装', id: 0 },
        ...list
      ]
    },
    async getPackageUnitToDeliveryOrderUnitRatio(postData): Promise<GetPackageUnitToTargetUnitRatioDto | undefined> {
      try {
        var res = await this.$http.post('/api/sale/SaleOrderManage/GetPackageUnitToTargetUnitRatio', postData)
        var data = res.data as GetPackageUnitToTargetUnitRatioDto
        return data
      } catch (er) {
        this.$kingzon_messager.showInfo(er as string)
        return undefined
      }
    },
    async getDeliveryOrderDetailListWeight(row: DeliveryOrderDetailListDto | undefined = undefined) {
      this.isLoading = true
      try {
        for (const x of this.deliveryOrderDetailList) {
          if (row && row.deliveryOrderDetailId != x.deliveryOrderDetailId && row != x) {
            continue
          }
          x.totalWeight = 0
          const packageId = x.packageId ?? 0
          //未填本次送货量 时无法计算重量 跳过
          if (!x.inputUnitQuantity) {
            continue
          }
          // 未选包装方案时尝试转换单位为Kg计算重量
          if (!packageId) {
            this.getWeightWithNoPackage(x)
            continue
          }

          const packageInfo = this.getPackageList(x).find(o => o.id == packageId) as any
          if (!packageInfo) {
            continue
          }
          const packageUnitId = packageInfo.packageUnitId
          const packageWeight = packageInfo.packageWegiht ?? 0
          const palletPackageRatio = packageInfo.palletPackageRatio ?? 0
          const palletWeight = packageInfo.palletWegiht ?? 0
          const mainUnitId = x.productUnitId  // 主单位的UnitId 
          if (!packageUnitId || !mainUnitId) {
            continue
          }
          const postData = new GetPackageUnitToTargetUnitRatioDto({
            packageId: packageId,
            targetId: x.specificationId ? x.specificationId : x.productId,
            unitConversionType: x.specificationId ? UnitConversionType.PackageToProductSpec : UnitConversionType.PackageToProduct,
            packageUnitId: packageUnitId,
            targetUnitId: mainUnitId
          })
          const data = await this.getPackageUnitToDeliveryOrderUnitRatio(postData) as GetPackageUnitToTargetUnitRatioDto  // 1 packageUnit = ratio * 1 unit : 1箱 = 10盘
          if (!data) {
            x.packageNum = 0
            x.packageUnitName = ''
            x.palletNum = 0
            continue
          }
          var unitRatio = x.unitInfo?.find(o => o.id == x.inputUnitConversionId)?.ratio  // 次单位对主单位的ratio，例：5，1盘 = 5段
          var ratio = new Decimal(data.ratio) //包装单位 对 主单位的转换  例：ratio = 10，1箱 = 10 段
          ratio = Decimal(ratio).div(unitRatio ?? 1)  // 包装单位 对 次单位的转换  例：1箱 = 10段 = 2盘，ratio = 2 = 10 / 5
          x.packageUnitName = data.packageUnitName
          const packageNum = new Decimal(x.inputUnitQuantity).div(ratio).ceil().toNumber()  // 多少箱，向上取整
          x.packageNum = packageNum
          const totalPackageWeight = new Decimal(packageNum).times(packageWeight).toDecimalPlaces(4).toNumber()
          let totalPalletWeight = 0
          if (palletPackageRatio > 0) {
            const palletNum = new Decimal(packageNum).div(palletPackageRatio).ceil().toNumber()  // 多少托盘，向上取整
            x.palletNum = palletNum
            totalPalletWeight = new Decimal(palletNum).times(palletWeight).toDecimalPlaces(4).toNumber()
          }
          x.totalWeight = Decimal.add(totalPackageWeight, totalPalletWeight).toNumber()
        }
      } finally {
        this.isLoading = false
      }
    },
    async packageChange(row: DeliveryOrderDetailListDto) {
      if (this.isInit) {
        return
      }
      await this.getDeliveryOrderDetailListWeight(row)
    },
    getWeightWithNoPackage(deliveryOrderDetail) {
      var kgUnitInfo = deliveryOrderDetail.unitInfo.find(x => x.unitName?.toLowerCase() == 'kg')
      if (!kgUnitInfo) {
        return
      }
      var kg = kgUnitInfo.quantity
      deliveryOrderDetail.totalWeight = kg
    },
  }
})
</script>
