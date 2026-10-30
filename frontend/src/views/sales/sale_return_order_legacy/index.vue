<template>
  <Loading v-show="isLoading"></Loading>
  <div class="kingzon-page">
    <div class="kingzon-page-title">
      <span>销售退货管理</span>
    </div>
    <div class="kingzon-page-body">
      <SearchPanel :searchParams="searchParams" :searchParamsHidden="searchParamsHidden" @search="search">
      </SearchPanel>
      <div class="kingzon-page-content">
        <div class="kingzon-btn-group" style="margin: 5px">
          <LinkButton iconCls="icon-add" @click="onClickAdd">送货单退货</LinkButton>
          <LinkButton iconCls="icon-add" @click="onClickAddHisProduct">产品直接退货</LinkButton>
        </div>
        <KingZonDataGrid :showDownLoad="true" exportFileName="销售退货订单" ref="dataGrid" :data="saleReturnOrderDetailList"
          class="kingzon-page-fill" @pageChange="getData()" @sortChange="getData()" :cellSpan="cellSpan"
          @cellClick="cellClick" emptyMsg="没有符合条件的数据" selectionMode="null"
          :sorts="[{ field: 'isComplete', order: 'asc' }, { field: 'saleReturnOrderNo', order: 'desc' }]">
          <GridColumnGroup :frozen="true" align="left" width="500">
            <GridHeaderRow>
              <GridColumn field="index" align="center" width="40">
                <template #body="scope">
                  {{ scope.rowIndex + 1 }}
                </template>
              </GridColumn>
              <GridColumn field="saleReturnOrderNo" title="退货单号" align="left" width="140" :sortable="true"
                :overflowTextEllipsis="true">
              </GridColumn>
              <GridColumn field="histotyProductNo" title="产品编码" align="left" width="80" :sortable="true"
                :overflowTextEllipsis="true">
              </GridColumn>
              <GridColumn field="historyProductCustomerMaterialNo" title="客户产品图号" align="left" width="120"
                :overflowTextEllipsis="true">
              </GridColumn>
              <GridColumn field="historyProductName" title="产品名称" align="left" width="120" :overflowTextEllipsis="true">
              </GridColumn>
            </GridHeaderRow>
          </GridColumnGroup>
          <GridColumnGroup :columnMoving="true" align="center">
            <GridHeaderRow>
              <GridColumn field="customerCode" title="客户编码" align="left" width="80" :sortable="true"
                :overflowTextEllipsis="true">
              </GridColumn>
              <GridColumn field="customerName" title="客户名称" align="left" width="120" :overflowTextEllipsis="true">
              </GridColumn>
              <GridColumn field="deliveryOrderNo" title="送货单号" align="left" width="140" :sortable="true"
                :overflowTextEllipsis="true">
              </GridColumn>
              <GridColumn field="isComplete" title="入库状态" align="center" width="80">
                <template #body="scope">
                  <span v-if="scope.row.isComplete" class="kingzon-tag kingzon-tag-success">已完成</span>
                  <span v-else class="kingzon-tag">未完成</span>
                </template>
              </GridColumn>
              <GridColumn field="saleReturnType" title="退货类型" width="80">
                <template #body="scope">
                  <span v-if="scope.row.saleReturnType == SaleReturnType.成品" class="kingzon-tag kingzon-tag-success">{{
                    SaleReturnType[scope.row.saleReturnType] }}</span>
                  <span v-else class="kingzon-tag">{{ SaleReturnType[scope.row.saleReturnType] }}</span>
                </template>
              </GridColumn>
              <GridColumn field="deliveryNoticeNo" :sortable="true" title="送货通知编号" width="160" align="left"
                :overflowTextEllipsis="true">
              </GridColumn>
              <GridColumn field="productionOrderNo" title="批次号" align="left" width="120" :sortable="true"
                :overflowTextEllipsis="true">
              </GridColumn>
              <GridColumn field="saleOrderNo" title="销售单号" align="left" width="140" :sortable="true"
                :overflowTextEllipsis="true">
              </GridColumn>
              <GridColumn field="historyProductVersion" title="版本号" align="left" width="60"
                :overflowTextEllipsis="true">
              </GridColumn>
              <GridColumn field="historyProductSpecification" title="规格型号" align="left" width="100"
                :overflowTextEllipsis="true">
              </GridColumn>
              <GridColumn field="historyProductMaterialGrade" title="材质牌号" align="left" width="100"
                :overflowTextEllipsis="true">
              </GridColumn>
              <GridColumn field="historyProductMaterialSpecification" title="原材料规格" align="left" width="100"
                :overflowTextEllipsis="true">
              </GridColumn>
              <GridColumn v-for="(item, index) in additionalParam" :key="item.systemPara.id"
                :field="item.systemPara.id.toString()" :title="item.systemPara.dataValue" width="100">
                <template #body="scope">
                  <span>
                    {{ scope.row.customAttributeList.length > index ? scope.row.customAttributeList[index].value : '' }}
                  </span>
                </template>
              </GridColumn>
              <GridColumn field="unit" title="单位" align="left" width="40" :overflowTextEllipsis="true">
                <template #body>{{ "件" }}</template>
              </GridColumn>
              <GridColumn field="deliveryQuantity" :sortable="true" title="送货单数量" align="right" width="90">
                <template #body="scope">
                  <span v-if="scope.row.deliveryOrderNo">{{ scope.row.deliveryQuantity }}</span>
                </template>
              </GridColumn>
              <GridColumn field="deliveryOrderCustomerUsedQuantity" :sortable="true" title="送货单使用数量" align="right"
                width="110">
                <template #body="scope">
                  <span v-if="scope.row.deliveryOrderNo">{{ scope.row.deliveryOrderCustomerUsedQuantity }}</span>
                </template>
              </GridColumn>
              <GridColumn field="returnQuantity" title="退货数量" align="right" width="80" :sortable="true">
                <template #body="scope">
                  {{ scope.row.returnQuantity }}
                </template>
              </GridColumn>
              <GridColumn field="inventoryQuantity" title="入库数量" :sortable="true" align="right" width="80">
                <template #body="scope">
                  <span class="kingzon-detail-text"> {{ scope.row.inventoryQuantity }}</span>
                </template>
              </GridColumn>
              <GridColumn field="price" title="单价" :sortable="true" align="right" width="90">
                <template #body="scope">
                  {{ (scope.row.price).toFixed(4) }}
                </template>
              </GridColumn>
              <GridColumn field="totalPrice" title="总价" :sortable="true" align="right" width="100">
                <template #body="scope">
                  {{
                    (scope.row.totalPrice).toFixed(4)
                  }}
                </template>
              </GridColumn>
              <GridColumn field="saleReturnOrderTotalPrice" :sortable="true" title="合计金额" align="right" width="100">
                <template #body="scope">
                  {{ (scope.row.saleReturnOrderTotalPrice).toFixed(4) }}
                </template>
              </GridColumn>
              <GridColumn field="saleReturnOrderReturnTime" :sortable="true" title="退货日期" align="left" width="90">
                <template #body="scope">
                  {{ $utils.formatDateTime(scope.row.saleReturnOrderReturnTime, 'yyyy-MM-dd') }}
                </template>
              </GridColumn>

              <GridColumn field="reason" title="退货原因" align="left" width="120" :overflowTextEllipsis="true">
              </GridColumn>
              <GridColumn field="remark" title="备注" align="left" width="120" :overflowTextEllipsis="true">
              </GridColumn>
              <GridColumn field="saleReturnOrderRemark" title="退货单备注" align="left" width="120"
                :overflowTextEllipsis="true">
              </GridColumn>
            </GridHeaderRow>
          </GridColumnGroup>

          <GridColumnGroup :frozen="true" align="right" width="265">
            <GridHeaderRow>
              <GridColumn field="file" title="文件" align="center" width="95">
                <template #body="scope">
                  <ButtonGroup style="height:24px">
                    <KingZonFileButton @onFileSelect="onFileSelect" @click="sourceId = scope.row.saleReturnOrderId">上传
                    </KingZonFileButton>
                    <LinkButton @click="showFileList(scope.row)">查看</LinkButton>
                  </ButtonGroup>
                </template>
              </GridColumn>
              <GridColumn field="act" title="操作" align="center">
                <template #body="scope">
                  <ButtonGroup style="height: 24px">
                    <LinkButton v-if="scope.row.inventoryQuantity == 0 && !scope.row.isComplete"
                      @click="deleteRow(scope.row)">删除</LinkButton>
                    <LinkButton v-if="!scope.row.isComplete" @click="editRow(scope.row)">编辑</LinkButton>
                    <LinkButton v-if="!scope.row.isComplete" @click="editRowComplete(scope.row)">确认完成</LinkButton>
                  </ButtonGroup>
                </template>
              </GridColumn>
            </GridHeaderRow>
          </GridColumnGroup>
        </KingZonDataGrid>
      </div>
    </div>
    <Drawer v-if="showDrawer" @collapse="showDrawer = false" :drawerStyle="{ width: '90%' }" region="east"
      ref="addSaleReturnOrderDrw" :closable="true" :maskClosable="false">
      <template #header>
        <div class="f-full" style="line-height: 30px">{{ title }}</div>
      </template>
      <AddSaleReturnOrder :editRow="editSaleReturnDetailRow" @cancel="addCancel" @submit="addSuccess">
      </AddSaleReturnOrder>
    </Drawer>

    <Drawer v-if="showHistoryDrawer" @collapse="showHistoryDrawer = false" :drawerStyle="{ width: '90%' }" region="east"
      ref="addHistoryReturn" :closable="true" :maskClosable="false">
      <template #header>
        <div class="f-full" style="line-height: 30px">{{ title }}</div>
      </template>
      <AddSaleReturnHisproduct :editRow="editSaleReturnDetailRow" @cancel="hisproductCancel"
        @submit="hisproductSuccess">
      </AddSaleReturnHisproduct>
    </Drawer>

    <Dialog ref="InventoryDetailDialog" :title="rowSaleReturnDetail.saleReturnOrderNo + '-入库详情'" :modal="true" closed
      :draggable="true" :dialogStyle="{ height: '85%', width: '75%' }">
      <RowInventoryDetail :rowSaleReturnDetail="rowSaleReturnDetail" @delRowInventoryDetail="getData()">
      </RowInventoryDetail>
    </Dialog>
  </div>
  <Dialog ref="fileUpload" :title="'销售退货文件上传'" :dialogStyle="{ width: '500px', maxHeight: '600px' }" :draggable="true"
    :resizable="true" :modal="true" :closed="true">
    <FileUpload @close="fileDialogClose" @submit="fileDialogSubmit" :uploadFileList="uploadFileList" :sourcId="sourceId"
      :sourceType="FileStorageSourceType.销售退货" :fileBase="FileBase.SaleReturnOrder">
    </FileUpload>
  </Dialog>
  <Drawer :drawerStyle="{ width: '55%' }" region="east" ref="fileList" :closable="true" :maskClosable="false"
    v-if="showFileDrawer" @collapse="showFileDrawer = false;">
    <template #header>
      <div class="f-full" style="line-height:30px">{{ selectedRow.orderNo }} - 订单文件</div>
    </template>
    <FileList :sourceType="FileStorageSourceType.销售退货" :sourceId="sourceId" @close="fileListClose" :showPage="false"
      :fileBase="FileBase.SaleReturnOrder">
    </FileList>
  </Drawer>
</template>

<script lang="ts">
import { Customer, SystemParaTreeDto } from '@/types/common'
import { DataType, FileBase, FileStorageSourceType, FilterType, SaleReturnType, SystemParaType } from '@/types/enums'
import { GetEditSaleReturnOrderDetailDTO, GetSaleReturnOrderDetailFilterListDTO } from '@/types/sales'
import { Filter, type FilterTmp, Search, type SelectOption, type UploadFile } from '@/types/types'
import RowInventoryDetail from '@/views/inventory/customer_return/row-inventory-detail.vue'
import { defineComponent } from 'vue'
import AddSaleReturnHisproduct from './add-sale-return-hisproduct.vue'
import AddSaleReturnOrder from './add-sale-return-order.vue'

export default defineComponent({
  name: 'saleReturnOrder',
  components: {
    AddSaleReturnOrder,
    AddSaleReturnHisproduct,
    RowInventoryDetail,
  },
  data() {
    return {
      editSaleReturnDetailRow: {} as GetEditSaleReturnOrderDetailDTO,
      isLoading: false,
      customerList: [] as Customer[],
      customerOptionList: [] as SelectOption[],
      searchParams: [] as Search[],
      searchParamsHidden: [] as Search[],
      additionalParam: [] as SystemParaTreeDto[],
      filterList: new GetSaleReturnOrderDetailFilterListDTO(),
      // saleOrderDetailList: [] as SaleOrderDetail[],
      saleReturnOrderDetailList: [] as GetEditSaleReturnOrderDetailDTO[],
      showDrawer: false, // 新增退货drawer是否存在
      saleReturnOrderDetailFilterListTmp: [] as Filter[],
      cellSpanColumns: ['saleReturnOrderNo', 'file', 'saleReturnOrderReturnTime', 'customerName', 'customerCode', 'saleReturnOrderTotalPrice', 'saleReturnOrderRemark', "deliveryOrderNo"],
      title: '新增退货',
      showHistoryDrawer: false,
      addHistoryReturnDrawer: {} as EasyUI.Drawer,
      saleReturnTypeList: [] as SelectOption[],
      rowSaleReturnDetail: {} as GetEditSaleReturnOrderDetailDTO,
      detailInventoryDialog: {} as EasyUI.Dialog,
      fileDrawer: {} as EasyUI.Drawer,
      showFileDrawer: false,
      uploadFileList: [] as UploadFile[],
      uploadFileDialog: {} as EasyUI.Dialog,
      selectedRow: {} as any,
      sourceId: 0,
    }
  },
  created() {
    this.init() //页面数据初始化
  },
  computed: {
    SaleReturnType() {
      return SaleReturnType
    },
    FileStorageSourceType() {
      return FileStorageSourceType
    },
    FileBase() {
      return FileBase
    }
  },
  mounted() {
    this.detailInventoryDialog = this.$refs.InventoryDetailDialog as EasyUI.Dialog;
    this.uploadFileDialog = this.$refs.fileUpload as EasyUI.Dialog;
  },
  methods: {
    //行合并
    cellSpan(data: any): any {
      if (this.cellSpanColumns.includes(data.column.field)) {
        var rowIndex = data.rowIndex
        return this.getCellSpan(data.row, rowIndex)
      }
      return { rowspan: 1, colspan: 1 }
    },
    getCellSpan(row: any, rowIndex: number): any {
      try {
        if (
          rowIndex > 0 &&
          this.saleReturnOrderDetailList[rowIndex - 1].saleReturnOrderNo == row.saleReturnOrderNo
        ) {
          return { rowspan: 0, colspan: 0 }
        }

        if (
          rowIndex == 0 ||
          this.saleReturnOrderDetailList[rowIndex - 1].saleReturnOrderNo != row.saleReturnOrderNo
        ) {
          var count = 1
          for (var i = rowIndex + 1; i < this.saleReturnOrderDetailList.length; i++) {
            if (this.saleReturnOrderDetailList[i].saleReturnOrderNo == row.saleReturnOrderNo) {
              count++
            } else {
              break
            }
          }
          return { rowspan: count, colspan: 1 }
        }
      } catch (err) {
        return {
          rowspan: 1,
          colspan: 1
        }
      }
      return {
        rowspan: 1,
        colspan: 1
      }
    },
    //初始化
    init(): void {
      this.saleReturnTypeList = this.$utils.getEnumKeyValueArray(SaleReturnType);
      this.isLoading = true
      this.getCustomers()
        .then((res: any) => {
          this.customerList = res.data
          this.customerOptionList = this.customerList.map((item: any) => {
            return { value: item.id, text: item.code + '-' + item.name }
          })
          this.addSearchParam()
          return this.getAdditionalParam()
        }).then((res: any) => {
          this.additionalParam = res.data as SystemParaTreeDto[];
          return this.getSaleOrderDetails()
        })
        .then((res: any) => {
          this.saleReturnOrderDetailList = res.data as GetEditSaleReturnOrderDetailDTO[]
        })
        .catch((err) => {
          this.$kingzon_messager.showError(err || '网络错误')
        }).finally(() => {
          this.isLoading = false
        })
    },
    //获取客户
    getCustomers(): Promise<HttpResponseType> {
      return this.$http.post('api/common/Customer/GetCustomer', [])
    },
    //构建查询面板内容
    addSearchParam(): void {
      this.searchParams.push(new Search('orderNo', '退货单号', 'saleReturnOrder', FilterType.Contains, DataType.Text, undefined, undefined, '退货单号'))
      this.searchParams.push(new Search('customerId', '客户', 'saleReturnOrder', FilterType.Equals, DataType.SelectNum, undefined, this.customerOptionList, '客户', "350px"))
      this.searchParams.push(new Search('deliveryOrderNo', '送货单号', 'deliveryOrder', FilterType.Contains, DataType.Text, undefined, undefined, '送货单号'))
      this.searchParams.push(new Search('histotyProductNo', '产品编码', 'historyProduct', FilterType.Contains, DataType.Text, undefined, undefined, '产品编码'))
      this.searchParams.push(new Search('historyProductName', '产品名称', 'historyProduct', FilterType.Contains, DataType.Text, undefined, undefined, '产品名称'))
      this.searchParamsHidden.push(new Search('saleReturnType', '退货类型', 'saleReturnOrderDetail', FilterType.Equals, DataType.SelectNum, undefined, this.saleReturnTypeList, '退货类型'))
      this.searchParamsHidden.push(new Search('detailNo', '送货通知编号', 'deliveryOrderDetail', FilterType.Contains, DataType.Text))
      this.searchParamsHidden.push(new Search('productionOrderNo', '批次号', 'deliveryOrderDetail', FilterType.Contains, DataType.Text,))
      this.searchParamsHidden.push(new Search('saleOrderNo', '销售单号', 'deliveryOrder', FilterType.Contains, DataType.Text,))
      this.searchParamsHidden.push(new Search('customerMaterialNo', '客户产品图号', 'customerMaterialNo', FilterType.Contains, DataType.Text, undefined, undefined, '客户产品图号'))
      this.searchParamsHidden.push(new Search('historyProductVersion', '版本号', 'historyProduct', FilterType.Contains, DataType.Text, undefined, undefined, '版本号'))
      this.searchParamsHidden.push(new Search('historyProductSpecification', '规格型号', 'historyProduct', FilterType.Contains, DataType.Text, undefined, undefined, '规格型号'))
      this.searchParamsHidden.push(new Search('historyProductMaterialGrade', '材质牌号', 'historyProduct', FilterType.Contains, DataType.Text, undefined, undefined, '材质牌号'))
      this.searchParamsHidden.push(new Search('historyProductMaterialSpecification', '原材料规格', 'historyProduct', FilterType.Contains, DataType.Text, undefined, undefined, '原材料规格'))
      let optionList = [{ text: '全部', value: "" }, { text: '已完成', value: "1" }, { text: '未完成', value: "0" }] as SelectOption[]
      this.searchParamsHidden.push(new Search('isComplete', '完成状态', 'saleReturnOrderDetail', FilterType.Equals, DataType.SelectBoolean, '', optionList, '完成状态'))
      this.searchParamsHidden.push(new Search('returnTime', '退货日期', 'saleReturnOrder', FilterType.GreaterThanOrEqual, DataType.DateTime, undefined, undefined, '开始时间'))
      this.searchParamsHidden.push(new Search('returnTime', undefined, 'saleReturnOrder', FilterType.LessThanOrEqual, DataType.DateTime, undefined, undefined, '结束时间'))
      this.searchParamsHidden.push(new Search('reason', '退货原因', 'saleReturnOrderDetail', FilterType.Contains, DataType.Text, undefined, undefined, '退货原因'))
      this.searchParamsHidden.push(new Search('remark', '备注', 'saleReturnOrderDetail', FilterType.Contains, DataType.Text, undefined, undefined, '备注'))
    },
    //组件查询参数并查询
    search(params: FilterTmp[]) {
      this.filterList.saleOrderDetailFilterList = params.find((item) => item.table == 'saleOrderDetail' || item.table == 'customerMaterialNo')?.filter ?? []
      this.filterList.saleReturnOrderDetailFilterList = params.find((item) => item.table == 'saleReturnOrderDetail')?.filter ?? []
      this.filterList.saleReturnOrderFilterList = params.find((item) => item.table == 'saleReturnOrder')?.filter ?? []
      this.filterList.deliveryOrderFilterList = params.find((item) => item.table == 'deliveryOrder')?.filter ?? []
      this.filterList.deliveryOrderDetailFilterList = params.find((item) => item.table == 'deliveryOrderDetail')?.filter ?? []
      this.filterList.historyProductFilterList = params.find((item) => item.table == 'historyProduct')?.filter ?? []
      this.filterList.customerMaterialNoFilterList = params.find((item) => item.table == 'customerMaterialNo')?.filter ?? []
      this.getData()
    },
    //刷新页面数据
    getData(): void {
      this.isLoading = true
      this.getSaleOrderDetails()
        .then((res: any) => {
          this.saleReturnOrderDetailList = res.data as GetEditSaleReturnOrderDetailDTO[]
        })
        .catch((err) => {
          this.$kingzon_messager.showError(err || '网络错误')
        })
        .finally(() => {
          this.isLoading = false
        })
    },
    //获取销售退货http请求
    getSaleOrderDetails(): Promise<HttpResponseType> {
      return this.$http.post('api/sales/SaleReturnOrder/GetSaleReturnOrderDetails',
        this.filterList,
        this.$refs.dataGrid
      )
    },
    getAdditionalParam(): Promise<HttpResponseType> {
      return this.$http.get('/api/common/SystemPara/GetAdditionalParam', { type: SystemParaType.产品附加信息 });
    },
    //新增退货
    onClickAdd() {
      this.editSaleReturnDetailRow = new GetEditSaleReturnOrderDetailDTO()
      this.title = '新增退货';
      this.showDrawer = true
      this.$nextTick(() => {
        let drawer = this.$refs.addSaleReturnOrderDrw as EasyUI.Drawer
        drawer.expand()
      })
    },
    //删除行
    deleteRow(editRow: any) {
      this.$messager.confirm({
        title: '提示',
        msg: '是否确定删除?',
        result: (r: any) => {
          if (!r) {
            return
          }
          this.isLoading = true
          this.$http
            .post('/api/sales/SaleReturnOrder/DelSaleReturnOrderDetail', editRow.id)
            .then((res: any) => {
              this.$kingzon_messager.showInfo(res.data)
              return this.getSaleOrderDetails()
            })
            .then((res: any) => {
              this.saleReturnOrderDetailList = res.data as GetEditSaleReturnOrderDetailDTO[]
            })
            .catch((err) => {
              this.$kingzon_messager.showError(err || '网络错误')
            })
            .finally(() => {
              this.isLoading = false
            })
        }
      })
    },
    //确定完成
    editRowComplete(editRow: any) {
      this.$messager.confirm({
        title: '提示',
        msg:
          '退货数量为' +
          editRow.returnQuantity +
          '，' +
          '入库数量为' +
          editRow.inventoryQuantity +
          '，' +
          '是否确认已完成该退货？',
        result: (r: any) => {
          if (!r) {
            return
          }
          this.isLoading = true
          this.$http
            .post('/api/sales/SaleReturnOrder/EditSaleReturnOrderDetailsComplete', editRow.id)
            .then((res: any) => {
              this.$kingzon_messager.showInfo(res.data)
              return this.getSaleOrderDetails()
            })
            .then((res: any) => {
              this.saleReturnOrderDetailList = res.data as GetEditSaleReturnOrderDetailDTO[]
            })
            .catch((err) => {
              this.$kingzon_messager.showError(err || '网络错误')
            })
            .finally(() => {
              this.isLoading = false
            })
        }
      })
    },
    //编辑行
    editRow(editRow: any) {
      this.editSaleReturnDetailRow = editRow
      if (editRow.deliveryOrderDetialId) {
        this.title = '编辑送货单退货';
        this.showDrawer = true
        this.$nextTick(() => {
          let drawer = this.$refs.addSaleReturnOrderDrw as EasyUI.Drawer
          drawer.expand()
        })
      }
      if (!editRow.deliveryOrderDetialId) {
        this.title = '编辑产品退货';
        this.showHistoryDrawer = true
        this.$nextTick(() => {
          this.addHistoryReturnDrawer = this.$refs.addHistoryReturn as EasyUI.Drawer
          this.addHistoryReturnDrawer.expand()
        })
      }

    },
    //退货详情页取消
    addCancel() {
      this.showDrawer = false
      this.getData()
      var drawer = this.$refs.addSaleReturnOrderDrw as EasyUI.Drawer
      drawer.collapse()
    },
    //退货详情页成功
    addSuccess() {
      this.showDrawer = false
      var drawer = this.$refs.addSaleReturnOrderDrw as EasyUI.Drawer
      drawer.collapse()
      this.getData()
    },

    // 新增产品退货
    onClickAddHisProduct() {
      this.editSaleReturnDetailRow = new GetEditSaleReturnOrderDetailDTO()
      this.title = '新增产品退货';
      this.showHistoryDrawer = true
      this.$nextTick(() => {
        this.addHistoryReturnDrawer = this.$refs.addHistoryReturn as EasyUI.Drawer
        this.addHistoryReturnDrawer.expand()
      })
    },

    // 产品退货页取消
    hisproductCancel() {
      this.addHistoryReturnDrawer.collapse()
    },

    // 产品退货页成功
    hisproductSuccess() {
      this.addHistoryReturnDrawer.collapse()
      this.getData()
    },
    //查看入库记录详情
    cellClick(ref: any) {
      let columnName = ref.column.field
      if (columnName == 'inventoryQuantity') {
        if (ref.row.inventoryQuantity == 0) {
          this.$kingzon_messager.showInfo('暂无入库记录')
          return
        }
        this.rowSaleReturnDetail = Object.assign({}, ref.row)
        this.detailInventoryDialog.open()
      }
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
    showFileList(row: any) {
      this.showFileDrawer = true;
      this.sourceId = row.saleReturnOrderId;
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
  }
})
</script>
