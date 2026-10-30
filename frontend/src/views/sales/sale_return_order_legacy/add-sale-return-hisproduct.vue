<template>
  <Loading v-show="isloading"></Loading>
  <div class="kingzon-page-content" style="min-width: 450px; min-height: 370px">
    <Form ref="form" :model="saleReturnOrder" :rules="rules" labelAlign="right" :labelWidth="50" :messages="validMes"
      class="kingzon-page-form" style="justify-content: flex-start" @validate="errors = $event">
      <FormField name="customerId" label="客户:" style="flex: 0 0 30%">
        <ComboBox style="width: 50%" v-model="saleReturnOrder.customerId" :data="customerList" textField="name"
          valueField="id"
          :disabled="historyProductDetails.length > 0 ? true : false || editRow.saleReturnOrderId != 0 ? true : false">
        </ComboBox>
      </FormField>
      <FormField name="returnTime" label="退货日期:" style="flex: 0 0 30%" :labelWidth="70">
        <DateBox :panelStyle="{ width: '192px', height: '250px !important', maxHeight: '250px !important' }"
          format="yyyy-MM-dd" v-model="saleReturnOrder.returnTime">
        </DateBox>
      </FormField>
      <FormField name="remark" label="备注:" style="flex-basis: 100%">
        <TextBox v-model="saleReturnOrder.remark" :multiline="true" style="width: 300px; height: 40px"></TextBox>
      </FormField>
    </Form>

    <div class="kingzon-btn-group" style="text-align: left; margin: 5px">
      <LinkButton btnCls="c8" @click="chooseProducts">选择产品</LinkButton>
      <LinkButton @click="clearDeliverOrder" v-if="editRow.saleReturnOrderId == 0">清空</LinkButton>
      <KingZonFileButton v-if="editRow.saleReturnOrderId == 0" @onFileSelect="onFileSelect($event)" :multiple="true">
        文件上传
      </KingZonFileButton>
      <span v-for="(item, index) in files" :key="index" style="color: var(--color-blue);">{{ item.name }}
        <LinkButton @click="deleteFiles(index)" :plain="true" iconCls="icon-clear" style="height: 24px;"></LinkButton>
      </span>
    </div>

    <!-- 选择产品退货的详情表 -->
    <KingZonDataGrid class="kingzon-page-fill" :pagination="false" ref="dataGrid" :data="historyProductDetails"
      :columnResizing="true" :columnMoving="true" :multiSort="true" :dblclickToEdit="true" editMode="cell"
      selectionMode="null" emptyMsg="请选择送货单或产品">
      <GridColumnGroup :frozen="true" width="340">
        <GridHeaderRow>
          <GridColumn field="index" align="center" width="40">
            <template #body="scope">
              {{ scope.rowIndex + 1 }}
            </template>
          </GridColumn>
          <GridColumn field="historyProduct.productNo" title="产品编码" width="80" :overflowTextEllipsis="true">
          </GridColumn>
          <GridColumn field="historyProduct.customerMaterialNo" title="客户产品图号" width="100" :overflowTextEllipsis="true">
          </GridColumn>
          <GridColumn field="historyProduct.productName" title="产品名称" width="120" :overflowTextEllipsis="true">
          </GridColumn>
        </GridHeaderRow>
      </GridColumnGroup>
      <GridColumnGroup>
        <GridHeaderRow>
          <GridColumn field="historyProduct.version" title="版本号" width="50" :overflowTextEllipsis="true">
          </GridColumn>
          <GridColumn field="historyProduct.materialGrade" title="材质牌号" width="100" :overflowTextEllipsis="true">
          </GridColumn>

          <GridColumn field="historyProduct.specification" title="规格型号" width="100" :overflowTextEllipsis="true">
          </GridColumn>

          <GridColumn field="historyProduct.materialSpecification" title="原材料规格" width="100"
            :overflowTextEllipsis="true">
          </GridColumn>
          <GridColumn field="historyProduct.unit" title="单位" width="40" :overflowTextEllipsis="true">
            <template #body>件</template>
          </GridColumn>
          <GridColumn field="historyProduct.deliveryCount" title="送货总数" align="right" width="70">
          </GridColumn>
          <GridColumn field="historyProduct.returnCount" title="已退货总数" align="right" width="80">
          </GridColumn>
          <GridColumn v-if="editRow.saleReturnOrderId" field="inventoryQuantity" title="已入库数量" align="right" width="80">
          </GridColumn>
          <GridColumn field="returnQuantity" title="本次退货数量" align="right" width="90" editRules="required"
            :editable="true" cellCss="kingzon-cell-editable">
            <template #edit="scope">
              <NumberBox v-model="scope.row.returnQuantity" :min="minReturnQuantity(scope.row)" :precision="0"
                :max="maxReturn(scope.row)" :spinners="false"
                :disabled="scope.row.isComplete || scope.row.inventoryQuantity > 0"
                @valueChange="computeTotalPrice(scope.row)">
              </NumberBox>
            </template>
          </GridColumn>
          <GridColumn field="price" title="单价" align="right" width="80" :editable="true"
            cellCss="kingzon-cell-editable">
            <template #body="scope">
              {{ scope.row.price?.toFixed(4) }}
            </template>
            <template #edit="scope">
              <NumberBox v-model="scope.row.price" :min="0" :spinners="true" :precision="4"
                :disabled="scope.row.isComplete || scope.row.inventoryQuantity > 0"
                @valueChange="computeTotalPrice(scope.row)">
              </NumberBox>
            </template>
          </GridColumn>
          <GridColumn field="totalPrice" title="总价" align="right" width="100" :editable="true"
            cellCss="kingzon-cell-editable">
            <template #body="scope">
              {{ scope.row.totalPrice?.toFixed(4) }}
            </template>
            <template #edit="scope">
              <NumberBox v-model="scope.row.totalPrice" :min="0" :spinners="true" :precision="4"
                :disabled="scope.row.isComplete || scope.row.inventoryQuantity > 0">
              </NumberBox>
            </template>
          </GridColumn>
          <GridColumn field="saleReturnType" title="退货类型" width="80" :editable="true" cellCss="kingzon-cell-editable">
            <template #body="scope">
              {{ SaleReturnType[scope.row.saleReturnType] }}
            </template>
            <template #edit="scope">
              <ComboBox v-model="scope.row.saleReturnType" :data="saleReturnTypeList"
                :disabled="scope.row.isComplete || scope.row.inventoryQuantity > 0">
              </ComboBox>
            </template>
          </GridColumn>
          <GridColumn field="reason" title="退货原因" :editable="true" width="120" cellCss="kingzon-cell-editable">
            <template #edit="scope">
              <TextBox v-model="scope.row.reason" :disabled="scope.row.isComplete || scope.row.inventoryQuantity > 0">
              </TextBox>
            </template>
          </GridColumn>
          <GridColumn field="remark" title="备注" :editable="true" width="120" cellCss="kingzon-cell-editable">
            <template #edit="scope">
              <TextBox v-model="scope.row.remark" :disabled="scope.row.isComplete || scope.row.inventoryQuantity > 0">
              </TextBox>
            </template>
          </GridColumn>
        </GridHeaderRow>
      </GridColumnGroup>
      <GridColumnGroup :frozen="true" align="right" width="60">
        <GridHeaderRow>
          <GridColumn field="act" title="操作" align="center">
            <template #body="scope">
              <LinkButton style="height: 24px" v-if="!scope.row.isComplete && scope.row.inventoryQuantity == 0"
                @click="deleteRow(scope.row)">删除
              </LinkButton>
            </template>
          </GridColumn>
        </GridHeaderRow>
      </GridColumnGroup>
    </KingZonDataGrid>
    <div style="margin-right: 20px; text-align: right">
      合计：{{ orderTotalPrice.toFixed(4) }}元
    </div>
    <div class="kingzon-btn-group kingzon-page-footer">
      <LinkButton @click="close">取消</LinkButton>
      <LinkButton btnCls="c8" @click="submit">确定</LinkButton>
    </div>
  </div>

  <Dialog ref="choiceProductsDialog" title="选择产品" :dialogStyle="{ width: '1200px', height: '680px' }" :modal="true"
    :draggable="true" :resizable="true" closed>
    <ChoiceProduct @cancel="choiceCancel" @success="choiceSuccess" :customerId="saleReturnOrder.customerId"
      :originProducts="histortProducts" />
  </Dialog>
  <!-- 文件上传 -->
  <Dialog ref="fileUpload" title="文件上传" :dialogStyle="{ width: '600px', maxHeight: '600px' }" :draggable="true"
    :resizable="true" :modal="true" :closed="true">
    <FileUpload @close="fileDialogClose" :uploadFileList="uploadFiles" :upload="false" @submit="uploadFileSubmit">
    </FileUpload>
  </Dialog>
</template>

<script lang="ts">
import { FileBase, FileStorageSourceType, SaleReturnType } from '@/types/enums';
import { PostSaleReturnOrderDetailDTO, SaleReturnOrder, SaleReturnOrderDetail } from '@/types/sales';
import type { SelectOption, UploadFile } from '@/types/types';
import { cloneDeep } from 'lodash';
import { defineComponent } from 'vue';
import ChoiceProduct from './choice-product.vue';

export default defineComponent({
  components: {
    ChoiceProduct
  },
  //接收父组件传递的编辑行数据
  props: ['editRow'],
  name: 'addSaleReturnOrder',
  emits: ['cancel', 'submit'],
  data() {
    return {
      validMes: {
        customerId: {
          required: '请先选择客户'
        }
      },
      rules: {
        customerId: 'required',
        returnTime: 'required'
      },
      customerList: [] as any,
      isloading: false,

      customer: [],
      originReturnData: [] as PostSaleReturnOrderDetailDTO[],
      returnDeliveryOrderDetailList: [] as PostSaleReturnOrderDetailDTO[],
      historyProductDetails: [] as SaleReturnOrderDetail[],
      saleReturnOrder: {} as SaleReturnOrder,
      saleReturnTypeList: [] as SelectOption[],
      errors: {},
      choiceProductDialog: {} as EasyUI.Dialog,
      fileUploadDialog: {} as EasyUI.Dialog,
      showFileDrawer: false,
      uploadFiles: [] as UploadFile[],
      files: [] as UploadFile[],
      orderId: 0,
    }
  },
  created() {
    this.init()
  },
  mounted() {
    this.choiceProductDialog = this.$refs.choiceProductsDialog as EasyUI.Dialog;
    this.fileUploadDialog = this.$refs.fileUpload as EasyUI.Dialog;
  },

  computed: {
    // 当前所选的历史产品
    histortProducts() {
      return this.historyProductDetails.map(item => item.historyProduct);
    },
    // 计算订单总价
    orderTotalPrice() {
      let totalPrice = 0;
      if (this.historyProductDetails.length > 0) {
        this.historyProductDetails.map(item => {
          totalPrice += item.returnQuantity * item.price;
        });
      }
      if (this.returnDeliveryOrderDetailList.length > 0) {
        this.returnDeliveryOrderDetailList.map(item => {
          totalPrice += item.returnQuantity * item.deliveryOrderDetailDto.saleOrderDetailPrice;
        });
      }
      return totalPrice;
    },

    // 通过传入当前行的详情数据,计算最小值
    minReturnQuantity() {
      return (row: any) => {
        let val = 0;
        if (row.inventoryQuantity > 0) {
          let origin = this.originReturnData.find((item: any) => item.id && item.id == row.id);
          val = origin?.returnQuantity ?? 0;
        }
        return val;
      }
    },
    SaleReturnType() {
      return SaleReturnType;
    },
    maxReturn() {
      return (row: any) => {
        return row.historyProduct.deliveryCount - row.historyProduct.returnCount;
      }
    },
    FileStorageSourceType() {
      return FileStorageSourceType;
    },
    FileBase() {
      return FileBase;
    },
  },
  watch: {
    orderTotalPrice: function (val) {
      this.saleReturnOrder.orderTotalPrice = val;
    }
  },

  methods: {
    //初始化需要进行判断是新增或者编辑，若是编辑则需要获取对应的送货数据，若是新增则只需获取客户列表
    init() {
      this.saleReturnTypeList = this.$utils.getEnumKeyValueArray(SaleReturnType);
      if (!this.editRow.saleReturnOrderId) {
        //代表新增,只需获取客户列表,初始化数据
        this.getcustomerList();
        this.saleReturnOrder.returnTime = new Date();
        return;
      }

      this.saleReturnOrder.id = this.editRow.saleReturnOrderId;
      this.saleReturnOrder.orderNo = this.editRow.saleReturnOrderNo;
      this.saleReturnOrder.customerId = this.editRow.customerId;
      this.saleReturnOrder.returnTime = new Date(this.editRow.saleReturnOrderReturnTime);
      this.saleReturnOrder.remark = this.editRow.saleReturnOrderRemark;
      // 选取产品的编辑
      if (this.editRow.saleReturnOrderId && !this.editRow.deliveryOrderDetialId) {
        this.getHistoryProductEditData();
      }
    },
    // 获取历史产品退货数据
    getHistoryProductEditData() {
      this.isloading = true
      this.$http.post('/api/sales/SaleReturnOrder/GetHistoryProductReturnDetails', { id: this.editRow.saleReturnOrderId })
        .then((res) => {
          //获取原始销售退货数据
          this.originReturnData = cloneDeep(res.data) as PostSaleReturnOrderDetailDTO[]
          //获取销售退货数据
          this.historyProductDetails = cloneDeep(res.data) as SaleReturnOrderDetail[]
          this.historyProductDetails.forEach((item: any) => {
            item.historyProduct.returnCount = item.returnCount;
            item.historyProduct.deliveryCount = item.deliveryCount;
            if (item.isComplete || item.inventoryQuantity > 0) {
              item.historyProduct.checkBoxDisabled = true;
            }
          });
          this.getcustomerList();
        })
        .catch((err) => {
          this.$kingzon_messager.showError(err)
          this.isloading = false
        });
    },

    //取消新增按钮
    close(): void {
      this.$emit('cancel')
    },
    //确定新增按钮
    submit(): void {
      //验证数据
      let form = this.$refs.form as EasyUI.Form
      if (!form.valid) {
        return
      }
      if (this.returnDeliveryOrderDetailList.length == 0 && this.historyProductDetails.length == 0) {
        this.$kingzon_messager.showError('请选择产品')
        return
      }
      if (this.historyProductDetails.some(item => item.saleReturnType == null)) {
        this.$kingzon_messager.showError('请选择退货类型');
        return;
      }
      // 选择产品状态下,执行产品退货操作,否则执行送货退货操作
      if (this.historyProductDetails.length > 0) {
        this.saveHistoryProductReturn();
        return;
      }
    },
    //获取客户列表http请求
    getCustomers(): Promise<HttpResponseType> {
      return this.$http.post('api/common/Customer/GetCustomer', [])
    },
    //获取客户列表
    getcustomerList(): void {
      this.isloading = true
      this.getCustomers()
        .then((res: any) => {
          this.customerList = res.data as any[]
          this.customerList = this.customerList.map((item) => {
            return { id: item.id, code: item.code, name: item.code + '-' + item.name }
          })
        })
        .catch((err) => {
          this.$kingzon_messager.showError(err)
        })
        .finally(() => {
          this.isloading = false
        })
    },

    //清空送货
    clearDeliverOrder(): void {
      if (this.returnDeliveryOrderDetailList.length == 0 && this.historyProductDetails.length == 0) {
        return
      }
      this.$kingzon_messager
        .showConfim('是否确认清空？')
        .then(() => {
          this.historyProductDetails = this.historyProductDetails.filter((item: any) => item.historyProduct.checkBoxDisabled);
        })
    },
    //该组件删除本条事件
    deleteRow(editData: any): void {
      // 选择产品状态下,删除详情表一条记录
      this.historyProductDetails = this.historyProductDetails.filter((item: any) => item.historyProductId != editData.historyProductId)
    },

    // 选择产品退货按钮
    chooseProducts(): void {
      if (!this.saleReturnOrder.customerId) {
        this.$kingzon_messager.showErrorTip('请先选择客户')
        return;
      }
      this.choiceProductDialog.open();
    },
    // 选择产品退货取消
    choiceCancel(): void {
      this.choiceProductDialog.close();
    },
    // 选择产品退货成功
    choiceSuccess(data: any): void {
      this.choiceProductDialog.close();
      let oldIds = this.historyProductDetails.map((item: any) => item.historyProductId);
      let newIds = data.map((item: any) => item.id);
      // 保留原数组 新数组的交集
      this.historyProductDetails = this.historyProductDetails.filter((item: any) => newIds.includes(item.historyProductId));
      // 遍历新数组，如若原数组中不存在，则新增
      data.forEach((item: any) => {
        if (!oldIds.includes(item.id)) {
          let newDetail = new SaleReturnOrderDetail();
          newDetail.historyProductId = item.id;
          newDetail.historyProduct = item;
          newDetail.returnQuantity = 0;
          newDetail.price = item.price ?? 0;
          newDetail.totalPrice = newDetail.returnQuantity * newDetail.price;
          this.historyProductDetails.push(newDetail);
        }
      });
    },
    // 计算总价
    computeTotalPrice(row: any): void {
      row.totalPrice = (row.returnQuantity ?? 0) * (row.price ?? 0);
    },

    // 保存产品退货
    saveHistoryProductReturn() {
      // 验证是否有未填写的退货数量
      let stopNull = this.historyProductDetails.some((item: any) => {
        return !item.returnQuantity;
      });
      if (stopNull) {
        this.$kingzon_messager.showError('退货数量必须大于0');
        return;
      }
      // 验证是否有退货数量超过送货数量
      let stopBeyond = this.historyProductDetails.some((item: any) => {
        return item.returnQuantity > item.historyProduct.deliveryCount - item.historyProduct.returnCount;
      });
      if (stopBeyond) {
        this.$kingzon_messager.showError('本次退货数量与已退货总数之和不能大于送货总数');
        return;
      }
      // 详情中的单价和总价有空值时赋值为0
      this.historyProductDetails.forEach((item: any) => {
        item.price = item.price ?? 0;
        item.totalPrice = item.totalPrice ?? 0;
        item.saleReturnType = Number(item.saleReturnType);
      });
      this.saleReturnOrder.SaleReturnOrderDetails = this.historyProductDetails;
      if (this.editRow.saleReturnOrderId) {
        this.editHisProductReturn();
      }
      else {
        this.addHisProductReturn();
      }
    },
    // 新增产品退货
    addHisProductReturn() {
      // 提交数据
      this.isloading = true;
      this.$http.post('/api/sales/SaleReturnOrder/SaveHistoryProductSaleReturn', this.saleReturnOrder)
        .then((res: any) => {
          this.orderId = res.data;
          this.$kingzon_messager.showInfo("保存成功");
          this.uploadFile();
        })
        .catch((err) => {
          this.$kingzon_messager.showError(err);
          this.isloading = false;
        });
    },
    // 编辑产品退货
    editHisProductReturn() {
      // 提交数据
      this.isloading = true;
      this.$http.post('/api/sales/SaleReturnOrder/UpdateHisProductSaleReturn', this.saleReturnOrder)
        .then((res: any) => {
          this.$kingzon_messager.showInfo(res.data);
          this.$emit('submit');
        })
        .catch((err) => {
          this.$kingzon_messager.showError(err);
        })
        .finally(() => {
          this.isloading = false;
        });
    },
    //上传文件
    onFileSelect(files: UploadFile[]): void {
      this.uploadFiles = files
      this.fileUploadDialog.open()
    },

    fileDialogClose(): void {
      this.fileUploadDialog.close()
    },

    uploadFileSubmit(files): void {
      this.files.push(...files)
      this.fileDialogClose()
    },

    deleteFiles(index: number): void {
      this.files.splice(index, 1)
    },

    //实际上传文件
    uploadFile(): void {
      if (this.orderId > 0 && this.files.length > 0) {
        this.isloading = true; //打开loading
        this.$utils.uploadFile(this.orderId, this.files, FileStorageSourceType.销售退货, FileBase.SaleReturnOrder, () => {
          this.$kingzon_messager.showInfo("文件上传成功");
          this.isloading = false;
        })
      }
      this.isloading = false;
      this.$emit('submit');
    }
  }
})
</script>
