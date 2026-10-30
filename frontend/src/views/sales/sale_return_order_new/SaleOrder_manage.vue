<template>
  <Loading v-show="isLoading"></Loading>
  <div class="kingzon-page">
    <div class="kingzon-page-title">
      <span>销售订单管理</span>
    </div>
    <div class="kingzon-page-body">
      <SearchPanel :searchParams="searchParams" :searchParamsHidden="searchParamsHidden" @search="search">
      </SearchPanel>
      <Tabs :plain="true" style="height: 100%" ref="tabs" @tabSelect="tabSelect">
        <TabPanel v-for="(item, index) in tabList" :key="index" :title="item.text">
          <div class="kingzon-page-content">
            <div class="kingzon-btn-group" style="margin:5px">
              <LinkButton @click="multiComplete()" style="margin-left: 4px;margin-top:2px;margin-bottom:2px">
                {{ selectedTab == SaleOrderStatus.进行中 ? '批量完成' : '批量取消完成' }}
              </LinkButton>
              <LinkButton v-if="selectedTab == SaleOrderStatus.进行中" @click="multiDelete()"
                style="margin-left: 4px;margin-top:2px;margin-bottom:2px">
                批量删除
              </LinkButton>
              <LinkButton v-if="selectedTab == SaleOrderStatus.进行中" @click="multiDeliveryNotice()"
                style="margin-left: 4px;margin-top:2px;margin-bottom:2px">
                批量通知发货
              </LinkButton>
              <LinkButton v-if="selectedTab == SaleOrderStatus.进行中" @click="multiPurchase()"
                style="margin-left: 4px;margin-top:2px;margin-bottom:2px">
                批量外购
              </LinkButton>
              <!-- <LinkButton class="c8" @click="importOrderClick()">导入订单
                            </LinkButton> -->
                            <LinkButton v-if="selectedTab == SaleOrderStatus.进行中" class="c8" @click="onClickAdd()">新增订单
                            </LinkButton>
                        </div>
                        <KingZonDataGrid v-if="customerSaleParaList" :showDownLoad="true" :exportFileName="'销售订单'"
                            class="kingzon-page-fill" :isDownLoad="true" :downloadLeft="50"
                            :ref="'dataGrid' + item.value" :data="saleOrderDetailList[item.value as number]"
                            :columnResizing="true" :columnMoving="true" :multiSort="true" selectionMode="null"
                            @pageChange="getSaleOrderDetailList" @sortChange="getSaleOrderDetailList"
                            emptyMsg="没有符合条件的数据" :cellSpan="cellSpan" :virtualScroll="false" :virtualPageSize="20"
                            :footerData="footerData" :showFooter="true" :leftGroupRowWidth="700" :rowCss="getRowCss"
                            :rightGroupRowWidth="item.value != SaleOrderStatus.已完成 ? 310 : 210" idField="id"
                            :isCheckBoxSelection="true" @seletedRowsChanged="seletedRowsChanged"
                            @rowContextMenu="rowContextMenu($event)" style="overflow-y: unset;">
                            <template #left>
                                <GridColumn cellCss="datagrid-td-rownumber" field="index" title="" align="center"
                                    width="40">
                                    <template #body="scope">
                                        <span>{{ scope.rowIndex + 1 }}</span>
                                    </template>
                                </GridColumn>
                                <GridColumn field="orderNo" title="销售单号" align="left" :sortable="true" width="120"
                                    :overflowTextEllipsis="true">
                                    <template #body="scope">
                                        <span v-if="scope.row.changedRecord" style="text-decoration: underline;"
                                            @click="showChangedRecord(scope.row)" class="kingzon-pointer"
                                            :title="scope.row.orderNo">{{
                                                scope.row.orderNo }}</span>
                                        <span v-else :title="scope.row.orderNo">{{ scope.row.orderNo }}</span>
                                    </template>
                                </GridColumn>
                                <GridColumn field="customerName" title="客户名称" align="left" width="115"
                                    :overflowTextEllipsis="true">
                                </GridColumn>
                                <GridColumn field="productNo" title="产品编码" align="left" :sortable="true" width="105">
                                    <template #body="scope">
                                        <KingZonSymbol :rowText="scope.row.productNo" :symbolText="'急'"
                                            :color="'var(--color-red)'" :showSymbol="scope.row.isUrgent">
                                        </KingZonSymbol>
                                    </template>
                                </GridColumn>
                                <GridColumn field="productName" title="产品名称" width="105" :overflowTextEllipsis="true">
                                </GridColumn>
                                <GridColumn field="productSpecification" title="规格型号" width="100"
                                    :overflowTextEllipsis="true">
                                </GridColumn>
                                <GridColumn field="productTechnicalCode" title="技术型号" width="75"
                                    :overflowTextEllipsis="true">
                                </GridColumn>
                            </template>
                            <template #center>
                                <GridColumn field="orderType" title="订单类型" align="left" :sortable="true" width="80"
                                    :overflowTextEllipsis="true">
                                    <template #body="scope">
                                        {{ SaleOrderType[scope.row.orderType] }}
                                    </template>
                                </GridColumn>
                                <GridColumn field="customerOrderNo" title="客户订单号" align="left" :sortable="true"
                                    width="90" :overflowTextEllipsis="true">
                                </GridColumn>
                                <GridColumn field="customerMaterialNo" title="客户产品图号" width="100"
                                    :overflowTextEllipsis="true">
                                </GridColumn>
                                <GridColumn field="placeOrderQuantity" title="下单数量" align="right" width="130"
                                    :sortable="true">
                                    <template #body="scope">
                                        <div v-Tooltip="scope.row.planQtyInfo" class="kingzon-pointer">
                                            <div> {{ scope.row.placeOrderQuantity }}
                                                {{ scope.row.salesUnit }}</div>
                                            <div style="display: flex;flex-direction: row;justify-content: right;">
                                                <div style="font-style: italic;color: var(--color-grey);font-size: 12px;margin-left: 5px;"
                                                    v-for="unit in scope.row.unitConversionList" :key="unit.unitId">
                                                    {{ unit.placeOrderQuantity }}{{ unit.unitName }}</div>
                                            </div>
                                        </div>
                                    </template>
                                </GridColumn>
                                <GridColumn v-if="item.value != SaleOrderStatus.已完成" field="unDeliveryQuantity"
                                    title="差欠数量" align="right" width="90" :sortable="true">
                                    <template #body="scope">
                                        <div> {{ scope.row.unDeliveryQuantity }}
                                            {{ scope.row.salesUnit }}</div>
                                        <div style="color: var(--color-grey);font-size: 12px;">
                                            {{ scope.row.unDeliveryQtyByMainUnit }}{{ scope.row.productUnit }}
                                        </div>
                                    </template>
                                </GridColumn>
                                <GridColumn field="deliveryNoticeQuantity" title="送货通知" align="right" width="90"
                                    :sortable="true">
                                    <template #body="scope">
                                        <div> {{ scope.row.deliveryNoticeQuantity }}
                                            {{ scope.row.salesUnit }}</div>
                                        <div style="color: var(--color-grey);font-size: 12px;">
                                            {{ scope.row.deliveryNoticeQtyByMainUnit }}{{ scope.row.productUnit }}
                                        </div>
                                    </template>
                                </GridColumn>
                                <GridColumn field="deliveryQuantity" title="已送货数量" align="right" width="90"
                                    :sortable="true">
                                    <template #body="scope">
                                        <div> {{ scope.row.deliveryQuantity }}
                                            {{ scope.row.salesUnit }}</div>
                                        <div style="color: var(--color-grey);font-size: 12px;">
                                            {{ scope.row.deliveryQtyByMainUnit }}{{ scope.row.productUnit }}
                                        </div>
                                    </template>
                                </GridColumn>
                                <GridColumn field="deliveryRate" title="交付率" align="right" width="65" :sortable="true">
                                    <template #body="scope">
                                        {{ (scope.row.deliveryRate * 100).toFixed(2) }}%
                                    </template>
                                </GridColumn>
                                <!-- 手动录入的生产数据 -->
                                <GridColumn v-if="item.value != SaleOrderStatus.已完成" field="totalInputProductionQty"
                                    title="录入生产" align="right" width="80">
                                    <template #body="scope">
                                        <span v-Tooltip="scope.row.inputProductionQtyInfo"
                                            v-if="scope.row.inputProductionList.length" class="kingzon-pointer">
                                            {{ $utils.decimalPlaces4(scope.row.totalInputProductionQty) }}{{
                                                scope.row.salesUnit }}</span>
                                    </template>
                                </GridColumn>
                                <GridColumn v-if="item.value != SaleOrderStatus.已完成" field="totalInputProductionQty"
                                    title="生产完成率" align="right" width="80">
                                    <template #body="scope">
                                        <span :class="scope.row.totalInputProductionQty ? 'kingzon-detail-text' : ''"
                                            @click="openInventoryGrid(scope.row)">{{
                                                (inputProductionCompleteRatio(scope.row) *
                                                    100).toFixed(2) }}%</span>
                                        <ProgressBar barCls="c6" style="height: 15px;" :value="inputProductionCompleteRatio(scope.row) *
                                            100" :showValue="false" @click="openInventoryGrid(scope.row)">
                                        </ProgressBar>
                                    </template>
                                </GridColumn>
                                <GridColumn v-if="item.value != SaleOrderStatus.已完成" field="totalInputDeliveryQty"
                                    title="录入发货" align="right" width="80">
                                    <template #body="scope">
                                        <span v-Tooltip="scope.row.inputDeliveryQtyInfo"
                                            v-if="scope.row.inputDeliveryList.length" class="kingzon-pointer">
                                            {{ scope.row.totalInputDeliveryQty }}{{ scope.row.salesUnit }}</span>
                                    </template>
                                </GridColumn>
                                <GridColumn v-if="item.value == SaleOrderStatus.已完成" field="inputProductionQtyInfo"
                                    title="录入生产" width="180">
                                    <template #body="scope">
                                        <div style="white-space: normal;" @click="openInventoryGrid(scope.row)"> {{
                                            scope.row.inputProductionQtyInfo.content }}</div>
                                    </template>
                                </GridColumn>
                                <GridColumn v-if="item.value == SaleOrderStatus.已完成" field="inputDeliveryQtyInfo"
                                    title="录入发货" width="180">
                                    <template #body="scope">
                                        <div style="white-space: normal;"> {{
                                            scope.row.inputDeliveryQtyInfo.content }}</div>
                                    </template>
                                </GridColumn>

                                <GridColumn v-if="item.value != SaleOrderStatus.已完成" field="processQuantity"
                                    title="产线数量" align="right" width="110" :sortable="true">
                                    <template #body="scope">
                                        <div class="kingzon-detail-text" @click="showProductionDetail(scope.row)">{{
                                            scope.row.processQuantity
                                            }}{{ scope.row.productUnit }}</div>
                                        <div style="color: var(--color-grey);font-size: 10px;">
                                            计划{{ scope.row.planProductionQtyByMainUnit }}/已排{{
                                                scope.row.productionQuantity }}</div>
                                    </template>
                                </GridColumn>
                                <GridColumn v-if="item.value != SaleOrderStatus.已完成" field="transitQuantity"
                                    title="外购在途" align="right" width="110" :sortable="true">
                                    <template #body="scope">
                                        <div class="kingzon-detail-text" @click="showPurchaseDetail(scope.row)">{{
                                            scope.row.transitQuantity
                                            }}{{ scope.row.productUnit }}</div>
                                        <div style="color: var(--color-grey);font-size: 10px;">
                                            计划{{ scope.row.planPurchaseQtyByMainUnit }}/已购{{
                                                scope.row.purchaseQuantity }}</div>
                                    </template>
                                </GridColumn>
                                <GridColumn v-if="item.value != SaleOrderStatus.已完成" field="stockQuantity"
                                    title="生产/外购入库" align="right" width="110" :sortable="true">
                                    <template #body="scope">
                                        <div class="kingzon-detail-text" @click="showInventoryDetail(scope.row)">{{
                                            scope.row.stockQuantity
                                            }}{{ scope.row.productUnit }}</div>
                                        <div style="color: var(--color-grey);font-size: 10px;">
                                            生产{{ scope.row.productionInventoryQuantity }}/外购{{
                                                scope.row.purchaseInventoryQuantity }}</div>
                                    </template>
                                </GridColumn>
                                <GridColumn field="deliveryTime" title="客户交期" align="left" width="90" :sortable="true">
                                    <template #body="scope">
                                        {{ $utils.formatDateTime(scope.row.deliveryTime, "yyyy-MM-dd") }}
                                    </template>
                                </GridColumn>
                                <GridColumn field="remark" title="备注" align="left" width="150"
                                    :overflowTextEllipsis="true">
                                </GridColumn>
                                <GridColumn field="price" title="不含税单价" align="right" width="80">
                                    <template #body="scope">{{ scope.row.price.toFixed(4) }}</template>
                                </GridColumn>
                                <GridColumn field="totalPrice" title="不含税总价" align="right" :sortable="true" width="100">
                                    <template #body="scope">{{ scope.row.totalPrice.toFixed(4) }}</template>
                                </GridColumn>
                                <GridColumn field="orderTotalPrice" title="合计金额" :sortable="true" align="right"
                                    width="100">
                                    <template #body="scope">{{ scope.row.orderTotalPrice.toFixed(4) }}</template>
                                </GridColumn>
                                <GridColumn field="customerAddress" title="客户地址" align="left" width="180">
                                    <template #body="scope">
                                        <div :title="scope.row.customerAddress ? ((scope.row.customerAddress.contact ?
                                            scope.row.customerAddress.contact + '-'
                                            : '') + (scope.row.customerAddress.phone ? scope.row.customerAddress.phone + '-'
                                                : '') + scope.row.customerAddress.address + (scope.row.customerAddress.remark ?
                                                    '【' + scope.row.customerAddress.remark + '】' : '')) : ''"
                                            class="kingzon-textEllipsis">
                                            {{ scope.row.customerAddress ? ((scope.row.customerAddress.contact ?
                                                scope.row.customerAddress.contact + '-'
                                                : '') + (scope.row.customerAddress.phone ? scope.row.customerAddress.phone +
                                                    '-'
                                                    : '') + scope.row.customerAddress.address +
                                                (scope.row.customerAddress.remark ?
                                                    '【' + scope.row.customerAddress.remark + '】' : '')) : '' }}
                                        </div>
                                    </template>
                                </GridColumn>
                                <GridColumn field="packageSpecification" title="外包装方案" align="left" width="80"
                                    :overflowTextEllipsis="true">
                                </GridColumn>
                                <GridColumn field="status" title="订单状态" align="center " width="85" :sortable="true">
                                    <template #body="scope">
                                        <span v-if="scope.row.status == SaleOrderStatus.已完成"
                                            class="kingzon-tag kingzon-tag-success">{{
                                                SaleOrderStatus[scope.row.status] }}</span>
                                        <span v-else-if="scope.row.status == SaleOrderStatus.待下单"
                                            class="kingzon-tag kingzon-tag-warning">{{
                                                SaleOrderStatus[scope.row.status] }}</span>
                                        <span v-else class="kingzon-tag">{{ SaleOrderStatus[scope.row.status] }}</span>
                                    </template>
                                </GridColumn>
                                <GridColumn field="orderExamineStatus" title="审核状态" width="85" align="center">
                                    <template #body="scope">
                                        <span v-if="scope.row.examineStatus == ExamineStatus.待审核"
                                            class="kingzon-tag kingzon-tag-danger">{{
                                                ExamineStatus[scope.row.orderExamineStatus] }}</span>
                                        <span v-else class="kingzon-tag">{{ ExamineStatus[scope.row.orderExamineStatus]
                                            }}</span>
                                    </template>
                                </GridColumn>
                                <GridColumn field="receiveName" title="接收人" align="left" width="70"
                                    :overflowTextEllipsis="true">
                                </GridColumn>
                                <GridColumn field="receiveOrderTime" title="接收日期" align="left" :sortable="true"
                                    width="90">
                                    <template #body="scope">
                                        {{ $utils.formatDateTime(scope.row.receiveOrderTime, "yyyy-MM-dd") }}
                                    </template>
                                </GridColumn>
                                <!-- 订单扩展信息 -->
                                <GridColumn v-for="item in saleExtentionParameter" :key="item.value" :field="item.code"
                                    :title="item.text" width="80" :overflowTextEllipsis="true">
                                    <template #body="scope">
                                        {{scope.row.saleExtendedInfo.find(x => x.id == Number(item.value))?.value}}
                                    </template>
                                </GridColumn>
                                <GridColumn v-for="item in customerSaleParaList" :key="item.systemPara.id"
                                    :field="item.systemPara.id.toString()" :title="item.systemPara.dataValue"
                                    width="80">
                                    <template #body="scope">
                                        <div class="kingzon-textEllipsis"
                                            :title="scope.row.customSaleAttributeList.find(x => x.systemParaId == Number(item.systemPara.id))?.value">
                                            {{scope.row.customSaleAttributeList.find(x => x.systemParaId ==
                                                Number(item.systemPara.id))?.value}}</div>
                                    </template>
                                </GridColumn>
                                <GridColumn field="orderRemark" title="订单备注" align="left" width="150"
                                    :overflowTextEllipsis="true">
                                </GridColumn>
                            </template>
                            <template #right>
                                <GridColumn field="reviewStatus" title="评审状态" align="center" width="80"
                                    :sortable="true">
                                    <template #body="scope">
                                        <span v-if="scope.row.reviewStatus == ReviewStatus.评审中"
                                            class="kingzon-tag kingzon-tag-warning">{{
                                                ReviewStatus[scope.row.reviewStatus] }}</span>
                                        <span v-else-if="scope.row.reviewStatus == ReviewStatus.评审驳回"
                                            class="kingzon-tag kingzon-tag-danger">
                                            {{ ReviewStatus[scope.row.reviewStatus] }}
                                        </span>
                                        <span v-else-if="scope.row.reviewStatus == ReviewStatus.待提交"
                                            class="kingzon-tag kingzon-tag-info">
                                            {{ ReviewStatus[scope.row.reviewStatus] }}
                                        </span>
                                        <span v-else class="kingzon-tag">{{ ReviewStatus[scope.row.reviewStatus]
                                            }}</span>
                                    </template>
                                </GridColumn>
                                <GridColumn field="file" title="文件" align="center" width="100">
                                    <template #body="scope">
                                        <ButtonGroup style="height:24px">
                                            <KingZonFileButton @onFileSelect="onFileSelect"
                                                @click="selectedRowSaleOrderId = scope.row.orderId">
                                                上传
                                            </KingZonFileButton>
                                            <LinkButton @click="showFileList(scope.row)">查看</LinkButton>
                                        </ButtonGroup>
                                    </template>
                                </GridColumn>
                                <GridColumn field="act" title="订单操作" align="center" width="120"
                                    v-if="item.value != SaleOrderStatus.已完成">
                                    <template #body="scope">
                                        <ButtonGroup style="height:24px">
                                            <LinkButton
                                                v-if="scope.row.orderStatus == SaleOrderStatus.进行中 && scope.row.orderReviewStatus != ReviewStatus.评审中"
                                                @click="onClickEdit(scope.row, false)">编辑
                                            </LinkButton>
                                            <LinkButton
                                                v-if="scope.row.orderStatus == SaleOrderStatus.进行中 && (scope.row.orderReviewStatus == ReviewStatus.待提交 || scope.row.orderReviewStatus == ReviewStatus.评审驳回)"
                                                @click="onClickReview(scope.row)">提交评审
                                            </LinkButton>
                                            <LinkButton v-if="scope.row.orderStatus == SaleOrderStatus.待下单"
                                                @click="orderConfirm(scope.row)">订单确认
                                            </LinkButton>
                                            <LinkButton
                                                v-if="scope.row.orderStatus == SaleOrderStatus.进行中 && scope.row.orderExamineStatus == ExamineStatus.待审核"
                                                @click="orderChangeConfirm(scope.row)">变更确认
                                            </LinkButton>
                                        </ButtonGroup>
                                    </template>
                                </GridColumn>
                            </template>
                        </KingZonDataGrid>
                    </div>
                </TabPanel>
            </Tabs>
            <!-- 自定义右键菜单 -->
            <Menu v-if="contextMenuVisible" :inline="true" :style="contextMenuStyle" ref="contextMenu"
                class="detail_menu">
                <MenuItem v-for="(item, index) in contextMenuItems" :key="index" :text="item.text"
                    @click="onContextMenuClick(item)">
                </MenuItem>
            </Menu>
        </div>
    </div>
    <Drawer v-if="showAddDrawer" @collapse="showAddDrawer = false;" :drawerStyle="{ width: '95%' }" region="east"
        ref="addOrderDrawer" :closable="true" :maskClosable="false">
        <template #header>
            <div style="line-height:30px">新增订单</div>
        </template>
        <AddSaleOrder @cancel="addCancel" @success="addSuccess">
        </AddSaleOrder>
    </Drawer>
    <Drawer v-if="showEditDrawer" @collapse="showEditDrawer = false;" :drawerStyle="{ width: '95%' }" region="east"
        ref="editOrderDrawer" :closable="true" :maskClosable="false">
        <template #header>
            <div class="f-full" style="line-height:30px">{{ selectedRow.orderNo }} - 编辑</div>
        </template>
        <EditSaleOrder @cancel="editCancel" @success="editSuccess" :orderId="selectedRow.orderId"
            :detailId="selectedRow.id" :modifyDetail="modifyDetail">
        </EditSaleOrder>
    </Drawer>
    <!-- 文件上传 -->
    <Dialog ref="fileUpload" :title="'订单文件上传'" :dialogStyle="{ width: '500px', maxHeight: '600px' }" :draggable="true"
        :resizable="true" :modal="true" :closed="true">
        <FileUpload @close="uploadFileDialog.close()" @submit="fileDialogSubmit" :uploadFileList="uploadFileList"
            :sourcId="selectedRowSaleOrderId" :sourceType="fileSourceType" :fileBase="fileSourceBase">
        </FileUpload>
    </Dialog>
    <!-- 文件查看 -->
    <Drawer :drawerStyle="{ width: '55%' }" region="east" ref="fileList" :closable="true" :maskClosable="false"
        v-if="showFileDrawer" @collapse="showFileDrawer = false;">
        <template #header>
            <div class="f-full" style="line-height: 30px">文件列表</div>
        </template>
        <FileList :sourceType="fileSourceType" :sourceId="selectedRowSaleOrderId" @close="fileListClose"
            :showPage="false" :fileBase="fileSourceBase">
        </FileList>
    </Drawer>
    <!-- 手动录入生产和送货数据 -->
    <Dialog ref="inputProduction" :title="'生产录入'" :dialogStyle="{ width: '1300px', height: '600px' }" :draggable="true"
        :resizable="true" :modal="true" :closed="true">
        <InputProduction @cancel="inputProductionDialog.close()" @success="inputProductionSucess" :row="selectedRow">
        </InputProduction>
    </Dialog>

    <Dialog ref="productionInventory"
        :title="(selectedRow.productNo ?? '') + '-' + (selectedRow.productName ?? '') + ' 生产入库详情'" :modal="true" closed
        :draggable="true" :dialogStyle="{ height: '85%', width: '85%' }">
        <div style="width: 100%;height: 100%;display:flex;flex-direction: column;">
            <KingZonShowForm :formData="inventoryFormData" columnWidth="20%"></KingZonShowForm>
            <InventoryGrid style="flex: 1;" ref="dataGrid" dataGridId="pigrid" :recordList="productionInventoryList"
                :areaInfo="new AreaInfoDto({ showMoney: false })" :systemParaList="inventorySystemParaList"
                :exportFileName="(selectedRow.productNo ?? '') + '-' + (selectedRow.productName ?? '') + ' 生产入库表'"
                :showQuantity="true" :showRemainQuantity="false" :showSelection="false" :showInventoryType="true"
                :rightGroupRowWidth="0" @refreshTable="getProductionInventoryList">
            </InventoryGrid>
        </div>
    </Dialog>
    <Drawer ref="launchDeliveryOrderNotice" v-if="isShowLaunchDeliveryOrderNotice" region="east"
        :drawerStyle="{ width: '95%' }" @collapse="isShowLaunchDeliveryOrderNotice = false" :closable="true" title="发起送货通知">
    <LaunchDeliveryOrderNotice :saleOrderDetailIds="selectRowIds" :customerId="selectedCustomerId"
      :isCanChangeCustomer="false" @cancel="closeLaunchDeliveryOrderNotice" @success="submitLauchDeliveryOrderNotice">
    </LaunchDeliveryOrderNotice>
  </Drawer>
</template>

<script lang="ts">
import { defineComponent } from 'vue';
import { Filter, type FilterTmp, Search, type SelectOption, type UploadFile, ShowFormData } from '@/types/types';
import { DataType, FileBase, FileStorageSourceType, FilterType, ReviewStatus, SaleOrderStatus, ExamineStatus, SaleOrderType, SystemParaType } from '@/types/enums';
import { SaleOrderDetailFlatDTO } from '@/types/sale'
import { Decimal } from 'decimal.js';
import AddSaleOrder from './add-sale-order.vue';
import EditSaleOrder from './edit-sale-order.vue';
import InputProduction from './manual-input-production.vue';
import { SystemParaTreeDto, Para } from '@/types/common';
import InventoryGrid from './inventory/material-inventory-datagrid.vue';
import { AreaInfoDto, InventoryRecordDto } from './inventory/inventory.js';
import LaunchDeliveryOrderNotice from '@/views/delivery_order_manage/launch_delivery_order/index.vue'



export default defineComponent({
    name: 'saleOrder',
    components: { AddSaleOrder, EditSaleOrder, InputProduction, InventoryGrid, LaunchDeliveryOrderNotice },
    data() {
        return {
            isLoading: false,
            searchParams: [] as Search[],
            searchParamsHidden: [] as Search[],
            filterList: [] as Filter[],
            tabList: [] as SelectOption[],
            selectedTab: SaleOrderStatus.进行中,
            statusOptionList: [] as SelectOption[],
            selectRows: [] as SaleOrderDetailFlatDTO[],//多选选中的数据行
            selectRowIds: [] as Array<Number>,
            contextMenuVisible: false,
            contextMenuStyle: { left: '0px', top: '0px' } as any,
            contextMenuItems: [] as SelectOption[],
            contextMenuRow: {} as SaleOrderDetailFlatDTO,// 用于右键时选中的行
            selectedRow: {} as SaleOrderDetailFlatDTO,//操作的行
            selectedRowSaleOrderId: 0,
            showFileDrawer: false,
            fileDrawer: {} as EasyUI.Drawer,
            uploadFileList: [] as UploadFile[],
            uploadFileDialog: {} as EasyUI.Dialog,
            addOrderDrawer: {} as EasyUI.Drawer, //新增订单drawer
            showAddDrawer: false,
            editOrderDrawer: {} as EasyUI.Drawer, //编辑订单drawer
            importOrderDialog: {} as EasyUI.Dialog, //导入订单dialog
            productionDetailDrawer: {} as EasyUI.Drawer, //产品排产明细drawer
            saleOrderDetailList: [] as SaleOrderDetailFlatDTO[][],
            cellSpanColumns: ["orderNo", "customerOrderNo", "orderType", "customerCode", "customerName", "auditStatus", "act",
                "orderTotalPrice", "customerAddress", "receiveName", "receiveOrderTime", "orderRemark", "file"] as string[],
            saleExtentionParameter: [] as SelectOption[],
            customerList: [] as SelectOption[],
            showEditDrawer: false,// 编辑订单drawer是否存在
            modifyDetail: false,
            fileSourceType: FileStorageSourceType.销售订单文件,
            fileSourceBase: FileBase.SaleOrder,
            inputProductionDialog: {} as EasyUI.Dialog,
            customerSaleParaList: undefined as SystemParaTreeDto[] | undefined,//客户-下单扩展参数
            productionInventoryList: [] as InventoryRecordDto[],
            inventorySystemParaList: [] as Para[],
            productionInventoryDialog: {} as EasyUI.Dialog,
            inventoryFormData: [] as ShowFormData[],
            launchDeliveryOrderNotice: {} as EasyUI.Drawer,
            selectedCustomerId: 0,
            isShowLaunchDeliveryOrderNotice: false
        }
    },
    computed: {
        SaleOrderStatus() {
            return SaleOrderStatus;
        },
        ReviewStatus() {
            return ReviewStatus;
        },
        ExamineStatus() {
            return ExamineStatus;
        },
        footerData() {
            if (this.saleOrderDetailList[this.selectedTab]) {
                var footerData = [{
                    index: '合计',
                    // placeOrderQuantity: this.saleOrderDetailList[this.selectedTab].reduce((total, item: SaleOrderDetailFlatDTO) => {
                    //     var t = new Decimal(total);
                    //     var c = new Decimal(item.placeOrderQtyByMainUnit);
                    //     return t.add(c).toNumber();
                    // }, 0),
                    // deliveryQuantity: this.saleOrderDetailList[this.selectedTab].reduce((total, item: SaleOrderDetailFlatDTO) => {
                    //     var t = new Decimal(total);
                    //     var c = new Decimal(item.deliveryQuantity);
                    //     return t.add(c).toNumber();
                    // }, 0),
                    totalPrice: this.saleOrderDetailList[this.selectedTab].reduce((total, item: SaleOrderDetailFlatDTO) => {
                        var t = new Decimal(total);
                        var c = new Decimal(item.totalPrice ?? 0);
                        return t.add(c).toNumber();
                    }, 0),
                    // unDeliveryQuantity: this.saleOrderDetailList[this.selectedTab].reduce((total, item: SaleOrderDetailFlatDTO) => {
                    //     var t = new Decimal(total);
                    //     var c = new Decimal(item.placeOrderQuantity).sub(new Decimal(item.deliveryQuantity));
                    //     return t.add(c).toNumber();
                    // }, 0),
                }];
                return footerData;
            }
            else {
                return [];
            }

        },
        SaleOrderType() {
            return SaleOrderType;
        },
        AreaInfoDto() {
            return AreaInfoDto;
        },
    },
    created() {
        this.tabList = this.$utils.getEnumKeyValueArray(SaleOrderStatus).filter(item => item.value != SaleOrderStatus.待下单);
        this.statusOptionList = this.$utils.getEnumKeyValueArray(SaleOrderStatus);
        this.statusOptionList.unshift({ value: '', text: '全部' });
    },
    mounted() {
        this.init();
        document.addEventListener('pointerdown', this.onDocumentPointerDown, true);
        this.uploadFileDialog = this.$refs.fileUpload as EasyUI.Dialog;
        this.productionInventoryDialog = this.$refs.productionInventory as EasyUI.Dialog;

    },
    beforeUnmount() {
        document.removeEventListener('pointerdown', this.onDocumentPointerDown, true);
    },
    methods: {
        init() {
            this.isLoading = true;
            this.getExtentionData()
                .then((res: any) => {
                    var data = res.data;
                    this.saleExtentionParameter = data.detailExtention.concat(data.orderExtention);
                    return this.getCustomerSaleParaData();
                }).then((res: any) => {
                    this.customerSaleParaList = res.data;
                    return this.getCustomers();
                }).then((res: any) => {
                    this.customerList = res.data?.map((item: any) => {
                        return { value: item.id, text: item.customerCode + '-' + item.customerName }
                    }) as SelectOption[];
                    this.customerList.unshift({ value: '', text: '全部' });
                    this.addSearchParam();
                    this.handleStatusFilter();
                    return this.getSaleOrderData()
                }).then((res: any) => {
                    var data = res.data as SaleOrderDetailFlatDTO[];
                    data.forEach(item => {
                        item.planQtyInfo = {
                            content: "计划生产：" + item.planProductionQuantity + item.salesUnit + "[" + item.planProductionQtyByMainUnit + item.productUnit + "]" + "\n"
                                + "计划外购：" + item.planPurchaseQuantity + item.salesUnit + "[" + item.planPurchaseQtyByMainUnit + item.productUnit + "]" + "\n"
                                + "计划直接发货：" + item.planDirectDliveryQuantity + item.salesUnit + "[" + item.planDirectDliveryQtyByMainUnit + item.productUnit + "]",
                            position: 'right',
                        }
                        item.inputProductionQtyInfo = {
                            content: item.inputProductionList?.map(input => {
                                return input.orderNo + "：" + input.quantity + input.reportUnit +
                                    (input.isOrderProduct ? " [" + this.$utils.decimalPlaces4(input.quantityBySalesUnit) + item.salesUnit + "]" : "")
                                    + (input.remark ? " (" + input.remark + ")" : "")
                            }).join("\n"),
                            position: 'right',
                        }
                        item.inputDeliveryQtyInfo = {
                            content: item.inputDeliveryList?.map(input => {
                                return input.orderNo + "：" + input.quantity + item.salesUnit + (input.remark ? " (" + input.remark + ")" : "")
                            }).join("\n"),
                            position: 'right',
                        }
                    })
                    this.saleOrderDetailList[this.selectedTab] = data;
                })
                .catch((err) => {
                    this.$kingzon_messager.showError(err);
                }).finally(() => {
                    this.isLoading = false;
                });
        },
        //构建查询面板内容
        addSearchParam(): void {
            this.searchParams.push(new Search('saleOrder.orderNo', '订单编号'));
            this.searchParams.push(new Search('saleOrder.customerId', '客户', 'default', FilterType.Equals, DataType.SelectNum, "", this.customerList, "", "300px"));
            this.searchParams.push(new Search('goodsNo', '产品编码'));
            this.searchParamsHidden.push(new Search('goodsName', '产品名称'));
            this.searchParams.push(new Search('technicalCode', '技术型号'));
            this.searchParamsHidden.push(new Search('saleOrder.customerOrderNo', '客户订单号'));
            this.searchParams.push(new Search('customerMaterialNo', '客户产品图号'));
            var reviewOptions = this.$utils.getEnumKeyValueArray(ReviewStatus);
            reviewOptions.unshift({ text: '全部', value: "" });
            var typeOptions = this.$utils.getEnumKeyValueArray(SaleOrderType);
            typeOptions.unshift({ text: '全部', value: "" });
            this.searchParamsHidden.push(new Search('reviewStatus', '评审状态', 'default', FilterType.Equals, DataType.SelectNum, "", reviewOptions));
            this.searchParamsHidden.push(new Search('saleOrder.orderType', '订单类型', 'default', FilterType.Equals, DataType.SelectNum, "", typeOptions));
            this.searchParamsHidden.push(new Search('specification', '产品规格型号'));
            this.searchParamsHidden.push(new Search('deliveryTime', '客户交期', 'default', FilterType.GreaterThanOrEqual, DataType.DateTime, undefined, undefined, "起始日期"));
            this.searchParamsHidden.push(new Search('deliveryTime', '', 'default', FilterType.LessThanOrEqual, DataType.DateTime, undefined, undefined, "截止日期"));
            this.searchParamsHidden.push(new Search('remark', '备注'));
            this.searchParamsHidden.push(new Search('saleOrder.receiveOrderTime', '接收日期', 'default', FilterType.GreaterThanOrEqual, DataType.DateTime, undefined, undefined, "起始日期"));
            this.searchParamsHidden.push(new Search('saleOrder.receiveOrderTime', '', 'default', FilterType.LessThanOrEqual, DataType.DateTime, undefined, undefined, "截止日期"));
            this.searchParamsHidden.push(new Search('saleOrder.remark', '订单备注'));
        },
        getExtentionData() {
            return this.$http.get("/api/sale/SaleOrderManage/GetSaleExtentionSystemPara");
        },
        getCustomerSaleParaData() {
            return this.$http.get('/api/base/SystemPara/GetAdditionalParam', { type: SystemParaType.客户下单附加参数 });
        },
        getCustomers(): Promise<HttpResponseType> {
            return this.$http.get('api/base/Customer/GetSalesManAllCustomer', { includeMain: true })
        },
        getSaleOrderData() {
            return this.$http.post("/api/sale/SaleOrderManage/GetOrderList", this.filterList, this.$refs['dataGrid' + this.selectedTab]);
        },
        //组件查询参数并查询
        search(params: FilterTmp[]) {
            this.filterList = params.find(item => item.table == 'default')?.filter ?? [];
            this.getSaleOrderDetailList();
        },
        tabSelect(tab: any): void {
            this.selectedTab = SaleOrderStatus[tab.title as string];
            this.selectRowIds = []
            var table = this.$refs['dataGrid' + this.selectedTab] as any;
            if (table && table.length > 0) {
                table[0].selectedRows = [];
            }
            this.getSaleOrderDetailList()
        },
        handleStatusFilter() {
            var statusFilter = this.filterList.find(x => x.key == 'status');
            if (!statusFilter) {
                statusFilter = {
                    key: 'status',
                    value: this.selectedTab,
                    filterType: FilterType.Equals,
                    dataType: DataType.Integer
                } as Filter
                this.filterList.push(statusFilter);
            } else {
                statusFilter.value = this.selectedTab
            }
        },
        getSaleOrderDetailList(): void {
            this.hideContextMenu();
            this.handleStatusFilter();
            this.isLoading = true;
            this.getSaleOrderData()
                .then((res: any) => {
                    var data = res.data as SaleOrderDetailFlatDTO[];
                    data.forEach(item => {
                        item.planQtyInfo = {
                            content: "计划生产：" + item.planProductionQuantity + item.salesUnit + "[" + item.planProductionQtyByMainUnit + item.productUnit + "]" + "\n"
                                + "计划外购：" + item.planPurchaseQuantity + item.salesUnit + "[" + item.planPurchaseQtyByMainUnit + item.productUnit + "]" + "\n"
                                + "计划直接发货：" + item.planDirectDliveryQuantity + item.salesUnit + "[" + item.planDirectDliveryQtyByMainUnit + item.productUnit + "]",
                            position: 'right',
                        }
                        item.inputProductionQtyInfo = {
                            content: item.inputProductionList?.map(input => {
                                return input.orderNo + "：" + input.quantity + input.reportUnit
                                    + (input.isOrderProduct ? " [" + input.quantityBySalesUnit + item.salesUnit + "]" : "")
                                    + (input.remark ? " (" + input.remark + ")" : "")
                            }).join("\n"),
                            position: 'right',
                        }
                        item.inputDeliveryQtyInfo = {
                            content: item.inputDeliveryList?.map(input => {
                                return input.orderNo + "：" + input.quantity + item.salesUnit + (input.remark ? " (" + input.remark + ")" : "")
                            }).join("\n"),
                            position: 'right',
                        }
                    })
                    this.saleOrderDetailList[this.selectedTab] = data;
                })
                .catch((err) => {
                    this.$kingzon_messager.showError(err.length > 0 ? err : '数据加载失败');
                }).finally(() => {
                    this.isLoading = false;
                });
        },
        // 隐藏右键菜单
        hideContextMenu() {
            this.contextMenuVisible = false;
        },
        // 右键菜单点击事件
        async onContextMenuClick(item: SelectOption) {
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
                case 'purchase':
                    this.$kingzon_messager.showInfo("功能开发中");
                    break
                case 'delivery':
                    this.$kingzon_messager.showInfo("功能开发中");
                    break
                case 'review':
                    if (this.contextMenuRow) await this.reviewRow(this.contextMenuRow);
                    break
                case 'inputProduction':
                    if (this.contextMenuRow) this.openInputProduction(this.contextMenuRow);
                    break;
            }
        },
        //新增订单drawer打开
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
      //刷新列表
      this.getSaleOrderDetailList();
    },
    //编辑产品drawer打开
    onClickEdit(row: SaleOrderDetailFlatDTO, modifyDetail: boolean): void {
      this.selectedRow = row;
      this.showEditDrawer = true;
      this.modifyDetail = modifyDetail;
      this.$nextTick(() => {
        this.editOrderDrawer = this.$refs.editOrderDrawer as EasyUI.Drawer;
        this.editOrderDrawer.expand();
      });
    },
    //确认完成
    confirmComplete(row: SaleOrderDetailFlatDTO, isComplete: boolean): void {
      let message = row.deliveryRate < 1 ? '当前交货率为' + (row.deliveryRate * 100).toFixed(2) + '%，' : '';
      this.$kingzon_messager.showConfim(isComplete ? message + '确认完成后不可编辑、删除，是否确认完成？' : '是否确认取消完成？')
        .then(() => {
          this.completeDetails([row.id]);
        })
    },
    //批量完成或取消
    multiComplete() {
      if (this.selectRowIds.length <= 0) {
        this.$kingzon_messager.showError("请至少选择一条数据")
        return;
      }
      this.$kingzon_messager.showConfim(this.selectedTab == SaleOrderStatus.进行中 ? "确认完成后不可编辑、删除，是否确认完成？" : "是否确认取消完成？")
        .then(() => {
          var ids = this.selectRows.map(x => x.id);
          this.completeDetails(ids);
        })
    },
    async completeDetails(ids: Array<number>) {
      this.isLoading = true;
      await this.$http.post("/api/sale/SaleOrderManage/CompleteSaleOrderDetail", ids)
        .then((res: any) => {
          this.$kingzon_messager.showInfo(res.data);
        })
        .catch((err) => {
          this.$kingzon_messager.showError(err.length > 0 ? err : '数据加载失败');
        }).finally(() => {
          this.isLoading = false;
        });
      this.getSaleOrderDetailList();
    },
    // 删除行，点击删除按钮响应
    deleteRow(row: SaleOrderDetailFlatDTO): void {
      this.$kingzon_messager.showConfim("确认是否删除？")
        .then(() => {
          this.deleteDetails([row.id]);
        })
    },
    //确认删除
    async deleteDetails(ids: Array<number>) {
      this.isLoading = true;
      var failMsg = undefined;
      await this.$http.post("/api/sale/SaleOrderManage/DeleteSaleOrderDetail", ids)
        .then((res: any) => {
          var data = res.data;
          if (data.length > 0) {
            failMsg = data.map(x => {
              return x.orderInfo + x.msg;
            }).join("。 ")
          }
          this.$kingzon_messager.showInfo("操作成功");
        })
        .catch((err) => {
          this.$kingzon_messager.showError(err.length > 0 ? err : '数据加载失败');
        }).finally(() => {
          this.isLoading = false;
        });
      if (failMsg) {
        this.$kingzon_messager.showConfim(failMsg);
      }
      this.getSaleOrderDetailList();
    },
    //批量删除
    multiDelete() {
      if (this.selectRowIds.length <= 0) {
        this.$kingzon_messager.showError("请至少选择一条数据")
        return;
      }
      this.$kingzon_messager.showConfim("确认是否删除？")
        .then(() => {
          var ids = this.selectRows.map(x => x.id);
          this.deleteDetails(ids);
        })
    },
    orderConfirm(row) {
      this.selectedRow = row;
      // this.isShowOrderConfirm = true;
      // this.$nextTick(() => {
      //     this.orderConfirmDrawer = this.$refs.orderConfirmDrawer as EasyUI.Drawer;
      //     this.orderConfirmDrawer.expand();
      // });
    },
    orderChangeConfirm(row) {
      this.selectedRow = row;
      //   this.isShowOrderChangeConfirm = true;
      //   this.$nextTick(() => {
      //     this.orderChangeConfirmDrawer = this.$refs.orderChangeConfirmDrawer as EasyUI.Drawer;
      //     this.orderChangeConfirmDrawer.expand();
      //   });
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
      this.selectedRowSaleOrderId = row.orderId;
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
    //获取产品排产明细
    showProductionDetail(row): void {
      this.selectedRow = row;
      this.$kingzon_messager.showInfo("功能开发中")
      // this.isShowProductionDetail = true;
      // this.$nextTick(() => {
      //     this.productionDetailDrawer = this.$refs.productionDetailDrawer as EasyUI.Drawer;
      //     this.productionDetailDrawer.expand();
      // });
    },
    showPurchaseDetail(row): void {
      this.selectedRow = row;
      this.$kingzon_messager.showInfo("功能开发中")
    },
    showInventoryDetail(row): void {
      this.selectedRow = row;
      this.$kingzon_messager.showInfo("功能开发中")
    },
    showChangedRecord(row) {
      this.selectedRow = row;
      //   this.OrderChangeListDialog.open();
    },
    async multiDeliveryNotice() {
      if (this.selectRowIds.length <= 0) {
        this.$kingzon_messager.showError('请至少选择一条数据')
        return
      }
      // this.$kingzon_messager.showInfo("功能开发中")
      const uniqueCustomerId = this.selectRows[0].customerId
      const isSameCustomer = this.selectRows.every((x) => x.customerId == uniqueCustomerId)
      if (!isSameCustomer) {
        this.$kingzon_messager.showError('只能选择同一客户进行批量发货！')
        return
      }
      const result = await this.$http.post('/api/inventory/DeliveryOrder/GetSaleOrderDetailWithTodoQuantity', this.selectRowIds)
      var todoQuantityList = result.data as any[]
      console.log("todoQuantityList", todoQuantityList)
      this.selectRows.forEach(row => {
        const matched = todoQuantityList.find(o => o.saleOrderDetailId == row.id)
        if (matched) {
          row.todoQuantity = matched.todoQuantity ?? 0
        } else {
          row.todoQuantity = 0
        }
      })
      var originlength = this.selectRows.length;
      this.selectRows = this.selectRows.filter(x => (x.productionInventoryQuantity + x.purchaseInventoryQuantity + x.planDirectDliveryQuantity
        + (x.totalInputProductionQty ?? 0) - x.deliveryQuantity) > x.todoQuantity);
      if (this.selectRows.length != originlength) {
        this.saleOrderDetailList[this.selectedTab].filter((item) => item.selected == true).forEach((item) => {
          var temp = this.selectRows.find(o => o.id == item.id);
          if (temp == undefined) {
            item.selected = false;
          }
        })
        this.$kingzon_messager.showError("有订单库存不足,不能发起通知!")
        return;
      }
      this.selectRowIds = this.selectRows.map(s => s.id);
      console.log("剔除后选中的id:", this.selectRowIds)
      this.selectedCustomerId = uniqueCustomerId
      this.isShowLaunchDeliveryOrderNotice = true
      this.$nextTick(() => {
        this.launchDeliveryOrderNotice = this.$refs.launchDeliveryOrderNotice as EasyUI.Drawer
        this.launchDeliveryOrderNotice.expand()
      })
    },
    multiPurchase() {
      if (this.selectRowIds.length <= 0) {
        this.$kingzon_messager.showError("请至少选择一条数据")
        return;
      }
      this.$kingzon_messager.showInfo("功能开发中")
    },
    seletedRowsChanged() {
      var table = this.$refs['dataGrid' + this.selectedTab] as any;
      this.selectRows = table[0].selectedRows
      this.selectRowIds = table[0].selectedRows.map(x => x.id)
    },
    //右键自定义菜单（订单明细操作）
    rowContextMenu(event) {
      this.contextMenuRow = event.row as SaleOrderDetailFlatDTO;
      var pointerEvent = event.originalEvent as PointerEvent;

      pointerEvent.preventDefault();
      //导入 待下单状态
      if (this.contextMenuRow.status == SaleOrderStatus.待下单) {
        this.contextMenuItems = [
          { text: '编辑', value: 'edit' },
          { text: '删除', value: 'delete' },
        ];
      }
      //新增 待提交状态
      if (this.contextMenuRow.reviewStatus == ReviewStatus.待提交) {
        this.contextMenuItems = [
          { text: '编辑', value: 'edit' },
          { text: '提交评审', value: 'review' },
          { text: '删除', value: 'delete' },
        ];
      }
      //驳回状态
      if (this.contextMenuRow.reviewStatus == ReviewStatus.评审驳回) {
        this.contextMenuItems = [
          { text: '编辑', value: 'edit' },
          { text: '提交评审', value: 'review' },
          { text: '删除', value: 'delete' },
        ];
      }
      //新增 评审中 不能操作
      if (this.contextMenuRow.reviewStatus == ReviewStatus.评审中) {
        this.contextMenuItems = [{ text: '评审中不能操作' }];
      }
      //进行中状态
      if (this.contextMenuRow.status == SaleOrderStatus.进行中 &&
        this.contextMenuRow.reviewStatus == ReviewStatus.评审通过) {
        this.contextMenuItems = [
          { text: '编辑', value: 'edit' },
          { text: '确认完成', value: 'toggleComplete' },
          { text: '生产录入', value: 'inputProduction' },
          { text: '删除', value: 'delete' },
        ];
        if (this.contextMenuRow.planPurchaseQuantity > 0) {
          this.contextMenuItems.push({ text: '外购下单', value: 'purchase' })
        }
        if (this.contextMenuRow.orderType == SaleOrderType.客户订单 && (this.contextMenuRow.stockQuantity > 0 || this.contextMenuRow.planDirectDliveryQuantity > 0)) {
          this.contextMenuItems.push({ text: '通知发货', value: 'delivery' })
        }
      }
      //已完成状态
      if (this.contextMenuRow.status == SaleOrderStatus.已完成) {
        this.contextMenuItems = [
          { text: '取消完成', value: 'toggleComplete' },
        ];
      }
      this.contextMenuVisible = true;
      this.contextMenuStyle = {
        left: pointerEvent.clientX + 'px',
        top: pointerEvent.clientY + 'px'
      };
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
        getRowCss(row): Object | null {
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
            var currentDataGrid = this.$refs['dataGrid' + this.selectedTab] as any;
            if (this.cellSpanColumns.includes(data.column.field)) {
                return this.$utils.getCellSpanMultiKey([['orderNo', "customerOrderNo"]], data.row, data.rowIndex, currentDataGrid[0].dataGrid.frozenRows);
            }
            return { rowspan: 1, colspan: 1 };
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
        async onClickReview(row) {
            await this.$kingzon_messager.showConfim("确认是否将订单" + row.orderNo + "提交评审？")
                .then(async () => {
                    this.isLoading = true;
                    await this.$http.post("/api/sale/SaleOrderManage/CommitReviewSaleOrder", { id: row.orderId })
                        .then((res: any) => {
                            this.$kingzon_messager.showInfo(res.data);
                        })
                        .catch((err) => {
                            this.$kingzon_messager.showError(err.length > 0 ? err : '数据加载失败');
                        }).finally(() => {
                            this.isLoading = false;
                        });
                })
            this.getSaleOrderDetailList();
        },
        async reviewRow(row: SaleOrderDetailFlatDTO) {
            await this.$kingzon_messager.showConfim("确认是否提交评审？")
                .then(async () => {
                    this.isLoading = true;
                    await this.$http.post("/api/sale/SaleOrderManage/ReviewSaleOrderDetail", { detailIds: [row.id], reviewStatus: ReviewStatus.评审中 })
                        .then((res: any) => {
                            this.$kingzon_messager.showInfo(res.data);
                        })
                        .catch((err) => {
                            this.$kingzon_messager.showError(err.length > 0 ? err : '数据加载失败');
                        }).finally(() => {
                            this.isLoading = false;
                        });
                })
            this.getSaleOrderDetailList();
        },
        openInputProduction(row) {
            this.selectedRow = row;
            this.$nextTick(() => {
                this.inputProductionDialog = this.$refs.inputProduction as EasyUI.Dialog;
                this.inputProductionDialog.open();
            })
        },
        inputProductionSucess() {
            this.inputProductionDialog.close();
            this.getSaleOrderDetailList();
        },
        inputProductionCompleteRatio(row: SaleOrderDetailFlatDTO) {
            return Decimal.div(row.totalInputProductionQty ?? 0, row.planProductionQuantity).toNumber();
        },
        async openInventoryGrid(row: SaleOrderDetailFlatDTO) {
            if (!row.totalInputProductionQty || row.totalInputProductionQty <= 0) {
                return;
            }
            this.selectedRow = row;
            this.inventoryFormData = [
                new ShowFormData("销售单号", this.selectedRow.orderNo),
                new ShowFormData("客户", this.selectedRow.customerName),
                new ShowFormData("客户交期", this.selectedRow.deliveryTime ? this.$utils.formatDateTime(this.selectedRow.deliveryTime, "yyyy-MM-dd") : ""),
                new ShowFormData("下单数", this.selectedRow.placeOrderQuantity + (this.selectedRow.salesUnit ?? "") + " [" + this.selectedRow.unitConversionList?.map(item => item.placeOrderQuantity + (item.unitName ?? "")).join(", ") + "]"),
                new ShowFormData("计划生产数", this.selectedRow.planProductionQuantity + (this.selectedRow.salesUnit ?? "")),
                new ShowFormData("产品编码", this.selectedRow.productNo),
                new ShowFormData("产品名称", this.selectedRow.productName),
                new ShowFormData("规格型号", this.selectedRow.productSpecification ?? ""),
                new ShowFormData("生产入库数", (this.$utils.decimalPlaces4(this.selectedRow.totalInputProductionQty ?? 0)) + (this.selectedRow.salesUnit ?? "")),
                new ShowFormData("技术型号", this.selectedRow.productTechnicalCode),
            ];
            await this.getProductionInventoryList(row);
            this.$nextTick(() => {
                this.productionInventoryDialog.open();
            });
        },
        async getProductionInventoryList(row: SaleOrderDetailFlatDTO) {
            this.isLoading = true;
            this.$http.get("/api/sale/SaleOrderManage/GetSaleProductionInventoryList", { id: row.id })
                .then((res: any) => {
                    var data = res.data as any;
                    this.productionInventoryList = data.records as InventoryRecordDto[];
                    this.inventorySystemParaList = data.systemParaData as Para[];
                })
                .catch((err) => {
                    this.$kingzon_messager.showError(err.length > 0 ? err : '数据加载失败');
                }).finally(() => {
                    this.isLoading = false;
                });
        },
        closeLaunchDeliveryOrderNotice() {
          this.launchDeliveryOrderNotice.collapse()
        },
        submitLauchDeliveryOrderNotice() {
          this.closeLaunchDeliveryOrderNotice()
        }
    }
})

</script>
<style scoped>
:deep(.menu-inline) {
  position: fixed;
  z-index: 9999;
}

:global(.tooltip-content) {
  white-space: pre-line !important;
}
</style>
