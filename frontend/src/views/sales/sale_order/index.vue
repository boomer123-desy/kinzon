<template>
  <Loading v-show="isLoading"></Loading>
  <div class="kingzon-page">
    <div class="kingzon-page-title">
      <span>{{ isCheck == "false" ? '销售订单管理' : '销售订单评审' }}</span>
    </div>

    <div class="kingzon-page-body">
      <SearchPanel :searchParams="searchParams" :searchParamsHidden="searchParamsHidden" @search="search">
      </SearchPanel>

      <Tabs :plain="true" style="height: 100%" ref="tabs" @tabSelect="tabSelect">
        <TabPanel v-for="(item, index) in tabList" :key="index" :title="item.text">
          <div class="kingzon-page-content">
            <div class="kingzon-btn-group" style="margin:5px">
              <LinkButton v-if="isCheck == 'false'" @click="multiComplete()"
                style="margin-left: 4px;margin-top:2px;margin-bottom:2px">
                {{ selectedTab == SaleOrderStatus.进行中 ? '批量完成' : '批量取消' }}
              </LinkButton>
              <LinkButton v-if="selectedTab == SaleOrderStatus.进行中 && isCheck == 'false'" @click="multiDelete()"
                style="margin-left: 4px;margin-top:2px;margin-bottom:2px">
                批量删除
              </LinkButton>
              <span style="margin-right:10px;" v-if="selectIds.length">已选中：{{ selectIds.length }}
                条</span>
              <LinkButton @click="clean()" v-if="selectIds.length">清空
              </LinkButton>
              <LinkButton class="c8" @click="importOrderClick()" v-if="isCheck == 'false'">导入订单
              </LinkButton>
              <LinkButton class="c8" @click="onClickAdd()" v-if="isCheck == 'false'">新增订单
              </LinkButton>
              <LinkButton v-if="isCheck == 'true' && selectedTab == SaleOrderStatus.进行中" @click="multiReview()"
                style="margin-left: 4px;margin-top:2px;margin-bottom:2px">
                批量评审通过
              </LinkButton>
            </div>
            <KingZonDataGrid :showDownLoad="true" :exportFileName="'销售订单'" class="kingzon-page-fill" :isDownLoad="true"
              :downloadLeft="50" :ref="'dataGrid' + item.value" :data="saleOrderDetailList[item.value as number]"
              :columnResizing="true" :columnMoving="true" :multiSort="true" selectionMode="null"
              @pageChange="getSaleOrderDetailList" @sortChange="getSaleOrderDetailList" emptyMsg="没有符合条件的数据"
              :cellSpan="cellSpan" :virtualScroll="true" :virtualPageSize="20" :footerData="footerData"
              :showFooter="true" :leftGroupRowWidth="680" :rowCss="getRowCss"
              :rightGroupRowWidth="item.value != SaleOrderStatus.已完成 && isCheck == 'false' ? 310 : 190" idField="id"
              :isCheckBoxSelection="true" @seletedRowsChanged="seletedRowsChanged"
              @rowContextMenu="rowContextMenu($event)">

              <template #left>
                <GridColumn cellCss="datagrid-td-rownumber" field="index" title="" align="center" width="40">
                  <template #body="scope">
                    <span>{{ scope.rowIndex + 1 }}</span>
                  </template>
                </GridColumn>
                <GridColumn field="customerOrderNo" title="客户订单号" align="left" :sortable="true" width="85"
                  :overflowTextEllipsis="true">
                </GridColumn>
                <GridColumn field="customerName" title="客户名称" align="left" width="120" :overflowTextEllipsis="true">
                </GridColumn>
                <GridColumn field="productCustomerMaterialNo" title="客户产品图号" width="100" :overflowTextEllipsis="true">
                </GridColumn>
                <GridColumn field="productNo" title="产品编码" align="left" :sortable="true" width="85">
                  <template #body="scope">
                    <KingZonSymbol :rowText="scope.row.productNo" :symbolText="'急'" :color="'var(--color-red)'"
                      :showSymbol="scope.row.isUrgent">
                    </KingZonSymbol>
                  </template>
                </GridColumn>
                <GridColumn field="productName" title="产品名称" width="100" :overflowTextEllipsis="true">
                </GridColumn>
                <GridColumn field="productSpecification" title="规格型号" width="100" :overflowTextEllipsis="true">
                </GridColumn>
              </template>

              <template #center>
                <GridColumn field="placeOrderQuantity" title="下单数量" align="right" width="80" :sortable="true">
                </GridColumn>
                <GridColumn field="makingQuantity" title="产线数量" align="right" width="80" :sortable="true">
                  <template #body="scope">
                    <span class="kingzon-detail-text" @click="showProductionDetail(scope.row)">{{
                      scope.row.makingQuantity
                    }}</span>
                  </template>
                </GridColumn>
                <GridColumn field="productUnPackageQuantity" title="未拣选库存" align="right" width="90" :sortable="true">
                </GridColumn>
                <GridColumn field="pickQuantity" title="待包装" width="90" :sortable="true" align="right">
                </GridColumn>
                <GridColumn field="packagingQuantity" title="待入库" width="100" :sortable="true" align="right">
                </GridColumn>
                <GridColumn field="productPackageQuantity" title="成品库存" align="right" width="80" :sortable="true">
                </GridColumn>
                <GridColumn field="doingQuantity" title="在制数量" width="90" align="right">
                  <template #body="scope">
                    {{
                      scope.row.makingQuantity + scope.row.productUnPackageQuantity + scope.row.pickQuantity +
                      scope.row.packagingQuantity }}
                  </template>
                </GridColumn>
                <GridColumn field="unDeliveryQuantity" title="差欠数量" align="right" width="80">
                  <template #body="scope">
                    {{ scope.row.placeOrderQuantity - scope.row.deliveryQuantity }}
                  </template>
                </GridColumn>
                <GridColumn field="totalDiffQuantity" title="总差欠数量" align="right" width="90">
                </GridColumn>
                <GridColumn field="deliveryNoticeQuantity" title="送货通知" align="right" width="90" :sortable="true">
                </GridColumn>
                <GridColumn field="deliveryQuantity" title="已送货数量" align="right" width="90" :sortable="true">
                </GridColumn>
                <GridColumn field="deliveryRate" title="交付率" align="right" width="65" :sortable="true">
                  <template #body="scope">
                    {{ (scope.row.deliveryRate * 100).toFixed(2) }}%
                  </template>
                </GridColumn>
                <GridColumn field="deliveryTime" title="客户交期" align="left" width="90" :sortable="true">
                  <template #body="scope">
                    {{ $utils.formatDateTime(scope.row.deliveryTime, "yyyy-MM-dd") }}
                  </template>
                </GridColumn>
                <GridColumn field="remark" title="备注" align="left" width="200" :overflowTextEllipsis="true">
                </GridColumn>
                <GridColumn field="moldCost" title="模具费" align="right" width="80">
                  <template #body="scope">{{ scope.row.moldCost.toFixed(4) }}</template>
                </GridColumn>
                <GridColumn field="taxRate" title="税率%" align="right" width="80">
                  <template #body="scope">{{ scope.row.taxRate?.toFixed(4) }}</template>
                </GridColumn>
                <GridColumn field="price" title="不含税单价" align="right" width="80">
                  <template #body="scope">{{ scope.row.price.toFixed(4) }}</template>
                </GridColumn>
                <GridColumn field="priceHaveTax" title="含税单价" align="right" width="80">
                  <template #body="scope">{{ scope.row.priceHaveTax?.toFixed(4) }}</template>
                </GridColumn>
                <GridColumn field="totalPrice" title="不含税总价" align="right" :sortable="true" width="100">
                  <template #body="scope">{{ scope.row.totalPrice.toFixed(4) }}</template>
                </GridColumn>
                <GridColumn field="totalPriceHaveTax" title="含税总价" align="right" :sortable="true" width="100">
                  <template #body="scope">{{ scope.row.totalPriceHaveTax?.toFixed(4) }}</template>
                </GridColumn>
                <GridColumn field="orderTotalPrice" title="合计金额（不含税）" :sortable="true" align="right" width="150">
                  <template #body="scope">{{ scope.row.orderTotalPrice.toFixed(4) }}</template>
                </GridColumn>
                <GridColumn field="orderTotalPriceHaveTax" title="合计金额（含税）" :sortable="true" align="right" width="135">
                  <template #body="scope">{{ scope.row.orderTotalPriceHaveTax?.toFixed(4) }}</template>
                </GridColumn>
                <GridColumn field="productUnit" title="单位" align="left" width="40">
                  <template #body>
                    {{ "件" }}
                  </template>
                </GridColumn>
                <GridColumn field="productMaterialGrade" title="材质牌号" width="100" :overflowTextEllipsis="true">
                </GridColumn>
                <GridColumn field="productMaterialSpecification" title="原材料规格" width="100" :overflowTextEllipsis="true">
                </GridColumn>
                <GridColumn field="customerAddress" title="客户地址" align="left" width="180" :overflowTextEllipsis="true">
                </GridColumn>
                <GridColumn field="receiveName" title="接收人" align="left" width="70" :overflowTextEllipsis="true">
                </GridColumn>
                <GridColumn field="receiveOrderTime" title="接收日期" align="left" :sortable="true" width="90">
                  <template #body="scope">
                    {{ $utils.formatDateTime(scope.row.receiveOrderTime, "yyyy-MM-dd") }}
                  </template>
                </GridColumn>
                <GridColumn field="saleOrderCreateTime" title="制单日期" align="left" :sortable="true" width="90">
                  <template #body="scope">
                    {{ $utils.formatDateTime(scope.row.saleOrderCreateTime, "yyyy-MM-dd") }}
                  </template>
                </GridColumn>
                <GridColumn field="orderRemark" title="订单备注" align="left" width="200" :overflowTextEllipsis="true">
                </GridColumn>
                <GridColumn field="orderNo" title="订单编号" align="left" :sortable="true" width="140"
                  :overflowTextEllipsis="true">
                  <template #body="scope">
                    <span v-if="scope.row.changedRecord" style="text-decoration: underline;"
                      @click="showChangedRecord(scope.row)" class="kingzon-pointer" :title="scope.row.orderNo">{{
                        scope.row.orderNo }}</span>
                    <span v-else :title="scope.row.orderNo">{{ scope.row.orderNo }}</span>
                  </template>
                </GridColumn>
                <GridColumn field="auditStatus" title="审核状态" width="90" align="center">
                  <template #body="scope">
                    <span v-if="scope.row.auditStatus == SaleOrderAuditStatus.待审核"
                      class="kingzon-tag kingzon-tag-danger">{{
                        SaleOrderAuditStatus[scope.row.auditStatus] }}</span>
                    <span v-else class="kingzon-tag">{{ SaleOrderAuditStatus[scope.row.auditStatus] }}</span>
                  </template>
                </GridColumn>
                <GridColumn field="productVersion" title="版本号" align="left" width="50" :overflowTextEllipsis="true">
                </GridColumn>



                <GridColumn field="status" title="完成状态" align="center " width="85" :sortable="true">
                  <template #body="scope">
                    <span v-if="scope.row.status == SaleOrderStatus.已完成" class="kingzon-tag kingzon-tag-success">{{
                      SaleOrderStatus[scope.row.status] }}</span>
                    <span v-else-if="scope.row.status == SaleOrderStatus.待下单" class="kingzon-tag kingzon-tag-warning">{{
                      SaleOrderStatus[scope.row.status] }}</span>
                    <span v-else class="kingzon-tag">{{ SaleOrderStatus[scope.row.status] }}</span>
                  </template>
                </GridColumn>
                <GridColumn field="isModel" title="样品/批量" align="center" width="85" :sortable="true">
                  <template #body="scope">
                    <span v-if="scope.row.isModel" class="kingzon-tag kingzon-tag-success">{{ "样品" }}</span>
                    <span v-else class="kingzon-tag">{{ "批量" }}</span>
                  </template>
                </GridColumn>
              </template>

              <template #right>
                <GridColumn field="isReview" title="评审状态" align="center" width="90" :sortable="true">
                  <template #body="scope">
                    <div v-if="isCheck == 'false' || scope.row.status == SaleOrderStatus.已完成">
                      <span v-if="scope.row.isReview" class="kingzon-tag">{{ "已评审" }}</span>
                      <span v-else class="kingzon-tag kingzon-tag-info">{{ "未评审" }}</span>
                    </div>
                    <SwitchButton v-else inputId="isReview" style="width:70px;height:24px" v-model="scope.row.isReview"
                      onText="通过" offText="待审" @click="reviewDetail(scope.row)">
                    </SwitchButton>
                  </template>
                </GridColumn>
                <GridColumn field="file" title="文件" align="center" width="100">
                  <template #body="scope">
                    <ButtonGroup style="height:24px">
                      <KingZonFileButton @onFileSelect="onFileSelect" @click="saleOrderId = scope.row.saleOrderId"
                        v-if="isCheck == 'false'">上传
                      </KingZonFileButton>
                      <LinkButton @click="showFileList(scope.row)">查看</LinkButton>
                    </ButtonGroup>
                  </template>
                </GridColumn>

                <GridColumn field="act" title="操作" align="center" width="120"
                  v-if="item.value != SaleOrderStatus.已完成 && isCheck == 'false'">
                  <template #body="scope">
                    <ButtonGroup style="height:24px">
                      <LinkButton
                        v-if="scope.row.status == SaleOrderStatus.进行中 && scope.row.auditStatus != SaleOrderAuditStatus.待审核"
                        @click="onClickEdit(scope.row, false)">订单编辑
                      </LinkButton>
                      <!-- <LinkButton
                        v-if="scope.row.status == SaleOrderStatus.进行中 && scope.row.auditStatus != SaleOrderAuditStatus.待审核"
                        @click="confirmComplete(scope.row, true)">订单完成
                      </LinkButton>
                      <LinkButton v-if="scope.row.status == SaleOrderStatus.已完成"
                        @click="confirmComplete(scope.row, false)">取消完成
                      </LinkButton> -->
                      <LinkButton v-if="scope.row.status == SaleOrderStatus.待下单" @click="orderConfirm(scope.row)">订单确认
                      </LinkButton>
                      <LinkButton
                        v-if="scope.row.status == SaleOrderStatus.进行中 && scope.row.auditStatus == SaleOrderAuditStatus.待审核"
                        @click="orderChangeConfirm(scope.row)">变更确认
                      </LinkButton>
                      <!-- <LinkButton
                        v-if="(scope.row.status == SaleOrderStatus.进行中 || scope.row.status == SaleOrderStatus.待下单) && scope.row.pickQuantity == 0"
                        @click="deleteRow(scope.row)">删除</LinkButton> -->
                    </ButtonGroup>
                  </template>
                </GridColumn>
              </template>
            </KingZonDataGrid>
          </div>
        </TabPanel>
      </Tabs>
      <!-- 自定义右键菜单 -->
      <Menu v-if="contextMenuVisible" :inline="true" :style="contextMenuStyle" ref="contextMenu" class="detail_menu">
        <MenuItem v-for="(item, index) in contextMenuItems" :key="index" :text="item.text"
          @click="onContextMenuClick(item)">
        </MenuItem>
      </Menu>
    </div>

    <Drawer v-if="showAddDrawer" @collapse="showAddDrawer = false;" :drawerStyle="{ width: '97%' }" region="east"
      ref="addOrderDrawer" :closable="true" :maskClosable="false">
      <template #header>
        <div class="f-full" style="line-height:30px">新增订单</div>
      </template>
      <AddSaleOrder @cancel="addCancel" @success="addSuccess" :isCheck="isCheck">
      </AddSaleOrder>
    </Drawer>
    <Drawer v-if="showEditDrawer" @collapse="showEditDrawer = false;" :drawerStyle="{ width: '97%' }" region="east"
      ref="editOrderDrawer" :closable="true" :maskClosable="false">
      <template #header>
        <div class="f-full" style="line-height:30px">{{ selectedRow.orderNo }} - 编辑</div>
      </template>
      <EditSaleOrder :isCheck="isCheck" @cancel="editCancel" @success="editSuccess" :orderId="editSaleOrderId"
        :detailId="selectedRow.id" :modifyDetail="modifyDetail">
      </EditSaleOrder>
    </Drawer>

    <!-- 排产明细 -->
    <Drawer v-if="isShowProductionDetail" @collapse="isShowProductionDetail = false;" :drawerStyle="{ width: '70%' }"
      region="east" ref="productionDetailDrawer" :closable="true" :maskClosable="false" title="产品在制明细">
      <ProductionDetail :selectedRow="selectedRow" @close="closeProductionDrawer"></ProductionDetail>
    </Drawer>

    <Dialog ref="fileUpload" :title="'销售订单文件上传'" :dialogStyle="{ width: '500px', maxHeight: '600px' }" :draggable="true"
      :resizable="true" :modal="true" :closed="true">
      <FileUpload @close="fileDialogClose" @submit="fileDialogSubmit" :uploadFileList="uploadFileList"
        :sourcId="saleOrderId" :sourceType="FileStorageSourceType.销售订单" :fileBase="FileBase.SaleOrder">
      </FileUpload>
    </Dialog>
    <Drawer :drawerStyle="{ width: '55%' }" region="east" ref="fileList" :closable="true" :maskClosable="false"
      v-if="showFileDrawer" @collapse="showFileDrawer = false;">
      <template #header>
        <div class="f-full" style="line-height:30px">{{ selectedRow.orderNo }} - 订单文件</div>
      </template>
      <FileList :sourceType="FileStorageSourceType.销售订单" :sourceId="saleOrderId" @close="fileListClose"
        :showPage="false" :fileBase="FileBase.SaleOrder">
      </FileList>
    </Drawer>
    <Dialog ref="importOrder" :title="'导入订单'" :dialogStyle="{ width: '400px', maxHeight: '600px' }" :draggable="true"
      :modal="true" :closed="true">
      <ImportOrder @submit="importSubmit"></ImportOrder>
    </Dialog>
    <Drawer v-if="isShowOrderConfirm" @collapse="confirmDrawerCollapse" :drawerStyle="{ width: '80%' }" region="east"
      ref="orderConfirmDrawer" :closable="true" :maskClosable="false" title="导入订单确认">
      <OrderConfirm :saleOrderId="selectedRow.saleOrderId" @success="confirmSuccess" @close="confirmClose">
      </OrderConfirm>
    </Drawer>
    <Drawer v-if="isShowOrderChangeConfirm" @collapse="isShowOrderChangeConfirm = false" :drawerStyle="{ width: '80%' }"
      region="east" ref="orderChangeConfirmDrawer" :closable="true" :maskClosable="false" :title="'订单变更确认'">
      <OrderChangeConfirm :saleOrderId="selectedRow.saleOrderId" @success="changeSuccess" @close="changeClose">
      </OrderChangeConfirm>
    </Drawer>
    <Dialog ref="changeRecord" :title="selectedRow.orderNo + ' - 变更记录'" @close="getSaleOrderDetailList"
      :dialogStyle="{ width: '700px', height: '600px' }" :draggable="true" :modal="true" :closed="true">
      <OrderChangeList :id="selectedRow.saleOrderId"></OrderChangeList>
    </Dialog>
  </div>
</template>

<script lang="ts">
import { DataType, FileBase, FileStorageSourceType, FilterType, SaleOrderAuditStatus, SaleOrderStatus } from '@/types/enums';
import { SaleOrderAdd, SaleOrderDetailAdd, SaleOrderDetailFlatDTO } from '@/types/sales';
import { Filter, type FilterTmp, Search, type SelectOption, type UploadFile } from '@/types/types';
import { Decimal } from 'decimal.js';
import { defineComponent } from 'vue';
import AddSaleOrder from './add-sale-order.vue';
import EditSaleOrder from './edit-sale-order.vue';
import ImportOrder from './import-order.vue';
import OrderChangeConfirm from './order-change-confirm.vue';
import OrderChangeList from './order-change-list.vue';
import OrderConfirm from './order-confirm.vue';
import ProductionDetail from './production-detail.vue';

export default defineComponent({
  name: 'saleOrder',
  components: {
    AddSaleOrder,
    EditSaleOrder,
    ProductionDetail,
    ImportOrder,
    OrderConfirm,
    OrderChangeConfirm,
    OrderChangeList
  },
  props: ['isCheck'],
  data() {
    return {
      isLoading: false,
      searchParams: [] as Search[],
      searchParamsHidden: [] as Search[],
      filterList: {
        saleOrderFilterList: [] as Filter[],
        saleOrderDetailFilterList: [] as Filter[],
        historyProductFilterList: [] as Filter[],
        customerFilterList: [] as Filter[]
      },
      selectedRow: {} as SaleOrderDetailFlatDTO,
      saleOrderDetailList: [] as SaleOrderDetailFlatDTO[][],
      statusOptionList: [] as SelectOption[],
      customerList: [] as SelectOption[],
      receiverList: [] as SelectOption[],
      editSaleOrder: {} as SaleOrderAdd,                // 用于编辑订单的数据，From
      editSaleOrderDetails: [] as SaleOrderDetailAdd[], // 用作编辑订单的明细数据，KinzonDataGrid
      showAddDrawer: false,                             // 新增订单drawer是否存在
      showEditDrawer: false,                            // 编辑订单drawer是否存在
      isShowProductionDetail: false,                    // 是否显示产品排产明细
      editSaleOrderId: 0,                               // 编辑订单的ID
      cellSpanColumns: ["orderNo", "customerOrderNo", "customerCode", "customerName", "auditStatus", "act",
        "orderTotalPrice", "customerAddress", "receiveName", "receiveOrderTime", "orderRemark", "file", "saleOrderCreateTime"] as string[],
      showFileDrawer: false,
      saleOrderId: 0,
      fileDrawer: {} as EasyUI.Drawer,
      uploadFileList: [] as UploadFile[],
      uploadFileDialog: {} as EasyUI.Dialog,

      addOrderDrawer: {} as EasyUI.Drawer, //新增订单drawer
      editOrderDrawer: {} as EasyUI.Drawer, //编辑订单drawer

      importOrderDialog: {} as EasyUI.Dialog, //导入订单dialog
      productionDetailDrawer: {} as EasyUI.Drawer, //产品排产明细drawer

      isShowOrderConfirm: false,
      orderConfirmDrawer: {} as EasyUI.Drawer,
      isShowOrderChangeConfirm: false,
      orderChangeConfirmDrawer: {} as EasyUI.Drawer,
      OrderChangeListDialog: {} as EasyUI.Dialog,

      //footerData: [{}],
      selectedTab: SaleOrderStatus.进行中,
      tabList: [] as SelectOption[],
      virtualPageSize: 20,
      selectRows: [] as any,
      selectIds: [],
      modifyDetail: false,

      contextMenuVisible: false,
      contextMenuStyle: { left: '0px', top: '0px' } as any,
      contextMenuItems: [] as SelectOption[],
      // 用于右键时选中的行
      contextMenuRow: {} as SaleOrderDetailFlatDTO,
    }
  },
  created() {
    this.tabList = this.$utils.getEnumKeyValueArray(SaleOrderStatus).filter(item => item.value != SaleOrderStatus.待下单);
    this.statusOptionList = this.$utils.getEnumKeyValueArray(SaleOrderStatus);
    this.statusOptionList.unshift({ value: '', text: '全部' });
  },
  mounted() {
    this.init(); //页面数据初始化
    // 点击页面其他地方隐藏右键菜单
    document.addEventListener('pointerdown', this.onDocumentPointerDown, true);
  },
  beforeUnmount() {
    document.removeEventListener('pointerdown', this.onDocumentPointerDown, true);
  },

  computed: {
    FileBase() {
      return FileBase;
    },
    FileStorageSourceType() {
      return FileStorageSourceType;
    },
    SaleOrderStatus() {
      return SaleOrderStatus;
    },
    SaleOrderAuditStatus() {
      return SaleOrderAuditStatus;
    },
    currentDataGrid() {
      var dataGrid = this.$refs['dataGrid' + this.selectedTab] as any;
      return dataGrid[0];
    },

    footerData() {
      if (this.saleOrderDetailList[this.selectedTab]) {
        var footerData = [{
          index: '合计',
          placeOrderQuantity: this.saleOrderDetailList[this.selectedTab].reduce((total, item: SaleOrderDetailFlatDTO) => {
            var t = new Decimal(total);
            var c = new Decimal(item.placeOrderQuantity);
            return t.add(c).toNumber();
          }, 0),
          deliveryQuantity: this.saleOrderDetailList[this.selectedTab].reduce((total, item: SaleOrderDetailFlatDTO) => {
            var t = new Decimal(total);
            var c = new Decimal(item.deliveryQuantity);
            return t.add(c).toNumber();
          }, 0),
          totalPrice: this.saleOrderDetailList[this.selectedTab].reduce((total, item: SaleOrderDetailFlatDTO) => {
            var t = new Decimal(total);
            var c = new Decimal(item.totalPrice ?? 0);
            return t.add(c).toNumber();
          }, 0),
          unDeliveryQuantity: this.saleOrderDetailList[this.selectedTab].reduce((total, item: SaleOrderDetailFlatDTO) => {
            var t = new Decimal(total);
            var c = new Decimal(item.placeOrderQuantity).sub(new Decimal(item.deliveryQuantity));
            return t.add(c).toNumber();
          }, 0),
        }];
        return footerData;
      }
      else {
        return [];
      }

    },
  },

  methods: {

    clean(): void {
      this.selectIds = [];
      var table = this.$refs['dataGrid' + this.selectedTab] as any;
      table = Array.isArray(table) ? table[0] : table;
      if (table) {
        table.selectedRows = [];
      }
      if (this.saleOrderDetailList[this.selectedTab]) {
        this.saleOrderDetailList[this.selectedTab].forEach((x: any) => {
          x.selected = false;
        })
      }
    },


    //初始化
    init(): void {

      this.addSearchParam();
      this.$nextTick(() => {
        this.getData();
      });
      this.uploadFileDialog = this.$refs.fileUpload as EasyUI.Dialog;
      this.importOrderDialog = this.$refs.importOrder as EasyUI.Dialog;
      this.OrderChangeListDialog = this.$refs.changeRecord as EasyUI.Dialog;
    },
    //构建查询面板内容
    addSearchParam(): void {
      this.searchParams.push(new Search('customerOrderNo', '客户订单号', 'saleOrder', FilterType.Contains, DataType.Text));
      this.searchParams.push(new Search('customerId', '客户', 'saleOrder', FilterType.Equals, DataType.SelectNum, "", this.customerList, "", "300px"));
      this.searchParams.push(new Search('customerMaterialNo', '客户产品图号', 'saleOrderDetail', FilterType.Contains, DataType.Text));
      this.searchParams.push(new Search('productNo', '产品编码', 'historyProduct', FilterType.Contains, DataType.Text));
      this.searchParams.push(new Search('productName', '产品名称', 'historyProduct', FilterType.Contains, DataType.Text));

      this.searchParamsHidden.push(new Search('specification', '规格型号', 'historyProduct', FilterType.Contains, DataType.Text));
      this.searchParamsHidden.push(new Search('deliveryTime', '客户交期', 'saleOrderDetail', FilterType.GreaterThanOrEqual, DataType.DateTime, undefined, undefined, "起始日期"));
      this.searchParamsHidden.push(new Search('deliveryTime', '', 'saleOrderDetail', FilterType.LessThanOrEqual, DataType.DateTime, undefined, undefined, "截止日期"));
      this.searchParamsHidden.push(new Search('remark', '备注', 'saleOrderDetail', FilterType.Contains, DataType.Text));
      this.searchParamsHidden.push(new Search('materialGrade', '材质牌号', 'historyProduct', FilterType.Contains, DataType.Text));


      this.searchParamsHidden.push(new Search('receiveUserId', '接收人', 'saleOrder', FilterType.Equals, DataType.SelectNum, "", this.receiverList));
      this.searchParamsHidden.push(new Search('receiveOrderTime', '接收日期', 'saleOrder', FilterType.GreaterThanOrEqual, DataType.DateTime, undefined, undefined, "起始日期"));
      this.searchParamsHidden.push(new Search('receiveOrderTime', '', 'saleOrder', FilterType.LessThanOrEqual, DataType.DateTime, undefined, undefined, "截止日期"));
      this.searchParamsHidden.push(new Search('createTime', '制单日期', 'saleOrder', FilterType.GreaterThanOrEqual, DataType.DateTime, undefined, undefined, "起始日期"));
      this.searchParamsHidden.push(new Search('createTime', '', 'saleOrder', FilterType.LessThanOrEqual, DataType.DateTime, undefined, undefined, "截止日期"));
      this.searchParamsHidden.push(new Search('remark', '订单备注', 'saleOrder', FilterType.Contains, DataType.Text));
      this.searchParamsHidden.push(new Search('orderNo', '订单编号', 'saleOrder', FilterType.Contains, DataType.Text));

      this.searchParamsHidden.push(new Search('version', '版本号', 'historyProduct', FilterType.Contains, DataType.Text));


      // this.searchParamsHidden.push(new Search('isUrgent', '是否加急', 'saleOrderDetail', FilterType.Equals, DataType.SelectBoolean, "", [{ text: '全部', value: "" }, { text: '是', value: "1" }, { text: '否', value: "0" }]));
      this.searchParams.push(new Search('isReview', '评审状态', 'saleOrderDetail', FilterType.Equals, DataType.SelectBoolean, "", [{ text: '全部', value: "" }, { text: '已评审', value: "1" }, { text: '未评审', value: "0" }]));
      // this.searchParamsHidden.push(new Search('status', '完成状态', 'saleOrderDetail', FilterType.Equals, DataType.SelectNum, "", this.statusOptionList));



    },

    tabSelect(tab: any): void {
      this.selectedTab = SaleOrderStatus[tab.title as string];
      this.selectIds = []
      var table = this.$refs['dataGrid' + this.selectedTab] as any;
      if (table && table.length > 0) {
        table[0].selectedRows = [];
      }

      this.getSaleOrderDetailList()
    },

    //拉取页面需要的数据
    // 获取订单数据 -> 获取客户数据 -> 获取接收人数据
    getData(): void {
      this.isLoading = true;
      this.getSaleOrderDetails().then(res => {
        // 表格赋值
        this.saleOrderDetailList[this.selectedTab] = res.data as SaleOrderDetailFlatDTO[];
        return this.getCustomers();
      }).then((res: any) => {
        this.customerList = res.data?.map((item: any) => {
          return { value: item.value, text: item.code + '-' + item.text }
        }) as SelectOption[];
        this.customerList.unshift({ value: '', text: '全部' });
        const search = this.searchParams.find((item: any) => item.key == 'customerId');
        if (search) { search.optionList = this.customerList; }
        return this.getReceivers();
      }).then((res: any) => {
        this.receiverList = res.data as SelectOption[];
        this.receiverList.unshift({ value: '', text: '全部' });
        const search = this.searchParamsHidden.find((item: any) => item.key == 'receiveUserId');
        if (search) { search.optionList = this.receiverList; }

      }).catch((err) => {
        this.$kingzon_messager.showError(err.length > 0 ? err : '数据加载失败');
      }).finally(() => {
        this.isLoading = false;
      });
    },

    // 获取销售订单详情
    getSaleOrderDetails(): Promise<HttpResponseType> {
      this.filterList.saleOrderDetailFilterList = this.filterList.saleOrderDetailFilterList.filter(item => item.key != 'status');
      var type = this.selectedTab == SaleOrderStatus.进行中 ? FilterType.NotEquals : FilterType.Equals;
      this.filterList.saleOrderDetailFilterList.push({ key: 'status', value: SaleOrderStatus.已完成, filterType: type, dataType: DataType.SelectNum });
      return this.$http.post('api/sales/SaleOrder/FilterSaleOrders', this.filterList, this.$refs['dataGrid' + this.selectedTab]);
    },
    getCustomers(): Promise<HttpResponseType> {
      return this.$http.post('api/common/Customer/GetCustomerOptions');
    },
    getReceivers(): Promise<HttpResponseType> {
      return this.$http.post('api/common/User/GetAllUserOptions');
    },

    // 重新获取订单数据，直接调用getData()方法，search条件会被清空
    getSaleOrderDetailList(): void {
      this.hideContextMenu();
      this.isLoading = true;
      this.getSaleOrderDetails()
        .then((res: any) => {
          this.saleOrderDetailList[this.selectedTab] = res.data as SaleOrderDetailFlatDTO[];
        })
        .catch((err) => {
          this.$kingzon_messager.showError(err.length > 0 ? err : '数据加载失败');
        }).finally(() => {
          this.isLoading = false;
        });
    },

    //组件查询参数并查询
    search(params: FilterTmp[]) {
      let saleOrderFilterList = params.find(item => item.table == 'saleOrder')?.filter ?? [];
      let saleOrderDetailFilterList = params.find(item => item.table == 'saleOrderDetail')?.filter ?? [];
      let historyProductFilterList = params.find(item => item.table == 'historyProduct')?.filter ?? [];
      let customerFilterList = params.find(item => item.table == 'customer')?.filter ?? [];
      this.filterList = {
        saleOrderFilterList: saleOrderFilterList,
        saleOrderDetailFilterList: saleOrderDetailFilterList,
        historyProductFilterList: historyProductFilterList,
        customerFilterList: customerFilterList
      };
      this.getSaleOrderDetailList();
    },

    //新增产品drawer打开
    onClickAdd(): void {
      this.showAddDrawer = true;
      this.$nextTick(() => {
        this.addOrderDrawer = this.$refs.addOrderDrawer as EasyUI.Drawer;
        this.addOrderDrawer.expand();
      });

    },
    //新增drawer关闭
    addCancel(): void {
      this.addOrderDrawer.collapse();
    },

    //新增成功
    addSuccess(res: any): void {
      //drawer关闭
      this.addOrderDrawer.collapse();
      this.$kingzon_messager.showInfo(res.data);
      //刷新列表
      this.isLoading = true;
      this.getSaleOrderDetailList();
    },

    //编辑产品drawer打开
    onClickEdit(row: SaleOrderDetailFlatDTO, modifyDetail: boolean): void {
      this.selectedRow = row;
      this.editSaleOrderId = row.saleOrderId;
      this.showEditDrawer = true;
      this.modifyDetail = modifyDetail;
      this.$nextTick(() => {
        this.editOrderDrawer = this.$refs.editOrderDrawer as EasyUI.Drawer;
        this.editOrderDrawer.expand();
      });
    },
    // 编辑drawer关闭
    editCancel() {
      this.editOrderDrawer.collapse();
    },

    //保存成功
    editSuccess(res: any): void {
      //drawer关闭
      this.editOrderDrawer.collapse();
      this.$kingzon_messager.showInfo(res.data);
      this.getSaleOrderDetailList();
    },

    //确认完成
    confirmComplete(row: SaleOrderDetailFlatDTO, isComplete: boolean): void {
      let message = row.deliveryRate < 1 ? '当前交货率为' + (row.deliveryRate * 100).toFixed(2) + '%，' : '';
      this.$kingzon_messager.showConfim(isComplete ? message + '确认完成后不可编辑、删除，是否确认完成？' : '是否确认取消完成？')
        .then(() => {
          this.confirmedComplete(row);
        })
    },

    //点击确认完成确认框的确定按钮后执行
    // 保存确认完成状态 -> 重新获取页面订单数据
    confirmedComplete(row: SaleOrderDetailFlatDTO): void {
      this.isLoading = true;
      this.confirmData(row.id)
        .then((res: any) => {
          this.$kingzon_messager.showInfo(res.data);
          return this.getSaleOrderDetailList();
        })
        .then(() => {
          var table = this.$refs['dataGrid' + this.selectedTab] as any;
          if (table && table.length > 0) {
            table[0].selectedRows = [];
          }
        }).catch(err => {
          this.isLoading = false;
          this.$kingzon_messager.showError(err.length > 0 ? err : '确认失败，服务器错误');
        })
    },

    //点击确认完成确认框的确定按钮后执行
    // 保存确认完成状态 -> 重新获取页面订单数据
    multiConfirmedComplete() {
      this.isLoading = true;
      this.multiConfirmData()
        .then((res: any) => {
          this.$kingzon_messager.showInfo(res.data);
          var table = this.$refs['dataGrid' + this.selectedTab] as any;
          if (table && table.length > 0) {
            table[0].selectedRows = [];
            this.selectIds = [];
          }
          return this.getSaleOrderDetailList();
        })
        .catch(err => {
          this.isLoading = false;
          this.$kingzon_messager.showError(err);
        })
    },
    multiConfirmData(): Promise<Object> {
      return this.$http.post('api/sales/SaleOrder/MultiCompleteSaleOrderDetail', this.selectIds);
    },
    confirmData(id: number): Promise<Object> {
      return this.$http.post('api/sales/SaleOrder/CompleteSaleOrderDetail', { id: id });
    },
    // 删除行，点击删除按钮响应
    deleteRow(row: SaleOrderDetailFlatDTO): void {
      this.$kingzon_messager.showConfim("确认是否删除？")
        .then(() => {
          this.confirmedDelete(row);
        })
    },
    //点击删除确认框的确定按钮后执行
    // 删除数据 -> 重新获取页面订单数据
    confirmedDelete(row: SaleOrderDetailFlatDTO): void {
      this.isLoading = true;
      this.deleteData(row.id)
        .then((res: any) => {
          this.$kingzon_messager.showInfo(res.data);
          var table = this.$refs['dataGrid' + this.selectedTab] as any;
          console.log(table, 'aaa')
          if (table && table.length > 0) {
            table[0].selectedRows = [];
          }
          this.getSaleOrderDetailList();
        }).catch(err => {
          this.isLoading = false;
          this.$kingzon_messager.showError(err.length > 0 ? err : '删除失败，服务器错误');
        });
    },
    //点击删除确认框的确定按钮后执行
    // 删除数据 -> 重新获取页面订单数据
    multiConfirmedDelete() {
      this.isLoading = true;
      this.multiDeleteData()
        .then((res: any) => {
          this.$kingzon_messager.showInfo(res.data);
          var table = this.$refs['dataGrid' + this.selectedTab] as any;
          console.log(table, 'aaa')
          if (table && table.length > 0) {
            table[0].selectedRows = [];
            this.selectIds = [];
          }
          this.getSaleOrderDetailList();
        }).catch(err => {
          this.isLoading = false;
          this.$kingzon_messager.showError(err.length > 0 ? err : '删除失败，服务器错误');
        });
    },
    deleteData(id: number): Promise<Object> {
      return this.$http.post('api/sales/SaleOrder/DeleteSaleOrderDetail', { id: id });
    },
    multiDeleteData(): Promise<Object> {
      return this.$http.post('api/sales/SaleOrder/MultiDeleteSaleOrderDetail', this.selectIds);
    },
    getRowCss(row: SaleOrderDetailFlatDTO): Object | null {
      let today = new Date().setHours(0, 0, 0, 0);
      let deliveryTime = new Date(row.deliveryTime).setHours(0, 0, 0, 0);
      let rootElement = document.querySelector(':root ');
      if (deliveryTime < today && row.status == SaleOrderStatus.进行中 && rootElement) {
        var color = getComputedStyle(rootElement).getPropertyValue('--color-red-bg').trim();
        return { backgroundColor: color };
      }
      return null;
    },

    //行合并
    cellSpan(data: any): any {
      if (this.cellSpanColumns.includes(data.column.field)) {
        // return this.$utils.getCellSpan(data.row, data.rowIndex, this.saleOrderDetailList[this.selectedTab], 'orderNo', "customerOrderNo");
        return this.$utils.getCellSpan(data.row, data.rowIndex, this.currentDataGrid.frozenRows, 'saleOrderId');
      }
      return { rowspan: 1, colspan: 1 };
      // return this.$utils.getCellSpanByOneOrTwoKey(data, this.saleOrderDetailList, "orderNo", this.cellSpanColumns);
    },

    //获取产品排产明细
    showProductionDetail(row: SaleOrderDetailFlatDTO): void {
      this.selectedRow = row;
      this.isShowProductionDetail = true;
      this.$nextTick(() => {
        this.productionDetailDrawer = this.$refs.productionDetailDrawer as EasyUI.Drawer;
        this.productionDetailDrawer.expand();
      });
    },

    //关闭产品排产明细
    closeProductionDrawer(): void {
      this.productionDetailDrawer.collapse();
    },
    //选中文件后
    onFileSelect(files) {
      this.uploadFileList = files;
      this.uploadFileDialog.open();
    },
    fileDialogClose() {
      this.uploadFileDialog.close();
    },
    fileDialogSubmit() {
      this.uploadFileDialog.close();
    },
    //文件列表drawer打开
    showFileList(row: SaleOrderDetailFlatDTO) {
      this.showFileDrawer = true;
      this.saleOrderId = row.saleOrderId;
      this.selectedRow = row;
      this.$nextTick(() => {
        this.fileDrawer = this.$refs.fileList as EasyUI.Drawer;
        this.fileDrawer.expand();
      })
    },
    //文件列表drawer关闭
    fileListClose(): void {
      this.fileDrawer.collapse();
    },
    //导入订单Dialog打开
    importOrderClick() {
      this.importOrderDialog.open();
    },
    importSubmit() {
      console.log('导入订单close');
      this.importOrderDialog.close()
      this.getSaleOrderDetailList();
    },
    orderConfirm(row: SaleOrderDetailFlatDTO) {
      this.selectedRow = row;
      this.isShowOrderConfirm = true;
      this.$nextTick(() => {
        this.orderConfirmDrawer = this.$refs.orderConfirmDrawer as EasyUI.Drawer;
        this.orderConfirmDrawer.expand();
      });
    },
    confirmDrawerCollapse() {
      this.isShowOrderConfirm = false;
      this.getSaleOrderDetailList();
    },
    confirmSuccess() {
      this.orderConfirmDrawer.collapse();
      this.getSaleOrderDetailList();
    },
    confirmClose() {
      this.orderConfirmDrawer.collapse();
    },
    orderChangeConfirm(row: SaleOrderDetailFlatDTO) {
      this.selectedRow = row;
      this.isShowOrderChangeConfirm = true;
      this.$nextTick(() => {
        this.orderChangeConfirmDrawer = this.$refs.orderChangeConfirmDrawer as EasyUI.Drawer;
        this.orderChangeConfirmDrawer.expand();
      });
    },
    changeSuccess() {
      this.orderChangeConfirmDrawer.collapse();
      this.getSaleOrderDetailList();
    },

    changeClose() {
      this.orderChangeConfirmDrawer.collapse();
    },


    showChangedRecord(row: SaleOrderDetailFlatDTO) {
      this.selectedRow = row;
      this.OrderChangeListDialog.open();

    },


    seletedRowsChanged() {
      var table = this.$refs['dataGrid' + this.selectedTab] as any;
      this.selectRows = table[0].selectedRows
      this.selectIds = table[0].selectedRows.map(x => x.id)
    },

    //批量完成或取消
    multiComplete() {
      console.log(this.selectIds)
      if (this.selectIds.length <= 0) {
        this.$kingzon_messager.showError("请至少选择一条数据")
        return;
      }
      this.$kingzon_messager.showConfim(this.selectedTab == SaleOrderStatus.进行中 ? "是否确认完成" + this.selectIds.length + "条数据" : "是否确认取消完成？")
        .then(() => {
          this.multiConfirmedComplete();
        })

    },

    //批量审核
    multiReview() {
      if (this.selectIds.length <= 0) {
        this.$kingzon_messager.showError("请至少选择一条数据")
        return;
      }
      this.$kingzon_messager.showConfim("是否确认批量审核通过")
        .then(() => {
          this.isLoading = true;
          this.$http.post('api/sales/SaleOrder/MultiReviewSaleOrderDetail', this.selectIds).then((res: any) => {
            this.$kingzon_messager.showInfo(res.data);
            return this.getSaleOrderDetails();
          })
            .then((res: any) => {
              this.saleOrderDetailList[this.selectedTab] = res.data as SaleOrderDetailFlatDTO[];
              var table = this.$refs['dataGrid' + this.selectedTab] as any;
              if (table && table.length > 0) {
                table[0].selectedRows = [];
                this.selectIds = [];
              }
              this.isLoading = false;
            })
            .catch((err) => {
              this.$kingzon_messager.showError(err);
              this.isLoading = false;
            });
        })
    },


    //批量删除
    multiDelete() {
      if (this.selectIds.length <= 0) {
        this.$kingzon_messager.showError("请至少选择一条数据")
        return;
      }
      this.$kingzon_messager.showConfim("确认是否删除？")
        .then(() => {
          this.multiConfirmedDelete()
        })

    },


    //右键自定义菜单
    rowContextMenu(event) {
      this.contextMenuRow = event.row;
      var pointerEvent = event.originalEvent as PointerEvent;
      // 如果是评审模式或待审核状态则不显示右键菜单
      if (this.isCheck == "true") {
        this.hideContextMenu();
        return;
      }

      pointerEvent.preventDefault();
      //待下单状态
      if (this.contextMenuRow.pickQuantity <= 0 && (this.contextMenuRow.status == SaleOrderStatus.待下单 || this.contextMenuRow.auditStatus == SaleOrderAuditStatus.待审核)) {
        this.contextMenuItems = [
          // { text: '编辑', value: 'edit' },
          { text: '删除', value: 'delete' },
        ];
      }
      //已完成状态
      if (this.contextMenuRow.status == SaleOrderStatus.已完成) {
        this.contextMenuItems = [
          { text: '取消完成', value: 'toggleComplete' },
        ];
      }
      //进行中状态
      if (this.contextMenuRow.status == SaleOrderStatus.进行中 &&
        this.contextMenuRow.auditStatus == SaleOrderAuditStatus.审核通过
      ) {
        this.contextMenuItems = [
          { text: '编辑', value: 'edit' },
          { text: '确认完成', value: 'toggleComplete' },
          { text: '删除', value: 'delete' },
        ];
      }
      this.contextMenuVisible = true;
      this.contextMenuStyle = {
        left: pointerEvent.clientX + 'px',
        top: pointerEvent.clientY + 'px'
      };
    },


    // 隐藏右键菜单
    hideContextMenu() {
      this.contextMenuVisible = false;
    },

    // 捕获阶段统一处理点击，决定是否隐藏右键菜单
    onDocumentPointerDown(event: PointerEvent) {
      // 如果菜单未显示，直接返回
      if (!this.contextMenuVisible) return;

      // 获取事件路径（兼容性处理）
      const path: EventTarget[] = (event as any).composedPath ? (event as any).composedPath() : ((event as any).path || this.buildEventPath(event));

      // 如果点击在自定义菜单内则不隐藏
      for (const node of path) {
        if (!(node instanceof Element)) continue;
        const el = node as Element;
        if (el.classList.contains('detail_menu')) {
          return; // 点击在菜单内，保留菜单
        }

      }

      // 否则隐藏菜单
      this.hideContextMenu();
    },

    // 备用：构造事件路径（IE/老浏览器回落）
    buildEventPath(event: Event): EventTarget[] {
      const path: EventTarget[] = [];
      let node: any = event.target;
      while (node) {
        path.push(node);
        node = node.parentNode || (node.getRootNode && node.getRootNode().host);
      }
      path.push(window);
      return path;
    },


    // 右键菜单点击事件
    onContextMenuClick(item: SelectOption) {
      this.contextMenuVisible = false;
      switch (item.value) {
        case 'edit':
          if (this.contextMenuRow) this.onClickEdit(this.contextMenuRow, true);
          break;
        case 'toggleComplete':
          if (this.contextMenuRow) this.confirmComplete(this.contextMenuRow, this.contextMenuRow.status != SaleOrderStatus.已完成);
          break;
        case 'delete':
          if (this.contextMenuRow) this.deleteRow(this.contextMenuRow);
          break;
      }
    },

    //明细评审状态切换
    reviewDetail(row) {
      this.isLoading = true;
      this.$http.get('api/sales/SaleOrder/ChangeDetailReviewStatusById', { id: row.id, isReview: row.isReview })
        .then((res: any) => {
          this.$kingzon_messager.showInfo(res.data);
          return this.getSaleOrderDetailList();
        })
        .catch((err) => {
          this.$kingzon_messager.showError(err);
          this.isLoading = false;
        });
    },


  }
})

</script>

<style scoped>
:deep(.menu-inline) {
  position: fixed;
  z-index: 9999;
}
</style>
