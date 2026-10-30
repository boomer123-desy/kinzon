<template>
    <Loading v-show="isLoading"></Loading>
    <div class="kingzon-page-content">
        <BodyAddEdit ref="add" class="kingzon-page-fill" v-model:model="saleOrder" :addOrEdit="'add'"
            :canModifyOrderData="true" />
        <div class="kingzon-btn-group kingzon-page-footer">
            <LinkButton @click="cancel">取消</LinkButton>
            <LinkButton btnCls="c8" @click="save">保存</LinkButton>
            <LinkButton btnCls="c8" @click="commit">提交评审</LinkButton>
        </div>
    </div>
</template>

<script lang="ts">
import { defineComponent } from 'vue';
import BodyAddEdit from './body-add-edit.vue'
import { SaleOrder, SaleOrderDetail } from '@/types/sale'
import { ReviewStatus, SaleOrderType, FileStorageSourceType, FileBase } from '@/types/enums';
import { cloneDeep } from 'lodash';
import { type uploadFilesType } from '@/types/types';


export default defineComponent({
    name: 'addSaleOrder',
    emits: ['cancel', 'success'],
    components: {
        BodyAddEdit
    },
    props: [],
    data() {
        return {
            isLoading: false,
            defaultSaleOrder: new SaleOrder(),
            saleOrder: new SaleOrder(),
        }
    },
    created() {
        this.saleOrder.customerId = undefined;
        this.saleOrder.receiveUserId = this.$user.Id;
        let saleOrderDetails = [] as SaleOrderDetail[];
        this.saleOrder.saleOrderDetails = saleOrderDetails;
    },
    methods: {
        //取消按钮
        cancel(): void {
            this.$emit('cancel')
            this.clearData()
        },
        save(): void {
            this.saleOrder.reviewStatus = ReviewStatus.待提交;
            this.saleOrder.saleOrderDetails.forEach((t: SaleOrderDetail) => {
                t.reviewStatus = ReviewStatus.待提交;
            })
            var check = this.checkData();
            if (check) {
                this.postData(this.saleOrder);
            }
        },
        commit() {
            var check = this.checkData();
            if (check) {
                var postData = cloneDeep(this.saleOrder)
                postData.reviewStatus = ReviewStatus.评审中;
                postData.saleOrderDetails.forEach((t: SaleOrderDetail) => {
                    t.reviewStatus = ReviewStatus.评审中;
                })
                this.postData(postData);
            }
        },
        checkData() {
            if (this.saleOrder.orderType == SaleOrderType.备货订单 && (!this.saleOrder.customerId || !this.saleOrder.receiveUserId
                || !this.saleOrder.receiveOrderTime)) {
                return false;
            }

            if (this.saleOrder.orderType == SaleOrderType.客户订单 && (!this.saleOrder.customerId || !this.saleOrder.receiveUserId
                || !this.saleOrder.customerAddressIndex || !this.saleOrder.receiveOrderTime)) {
                return false;
            }

            if (this.saleOrder.saleOrderDetails.length === 0) {
                this.$kingzon_messager.showError('请选择产品');
                return false;
            }
            this.saleOrder.orderTotalPrice = this.saleOrder.orderTotalPrice ?? 0;
            this.saleOrder.saleOrderDetails.forEach((t: SaleOrderDetail) => {
                t.price = t.price ?? 0;
            })
            this.saleOrder.orderType = Number(this.saleOrder.orderType);
            return true;
        },
        //访问接口,保存数据
        postData(data: SaleOrder): void {
            //检查下单数量>0
            for (let i = 0; i < data.saleOrderDetails.length; i++) {
                let item = data.saleOrderDetails[i] as SaleOrderDetail;
                if (!item.placeOrderQuantity || item.placeOrderQuantity <= 0) {
                    this.$kingzon_messager.showWarning("'" + item.product?.productNo + "'下单数量必须大于0");
                    return;
                }
                if (item.planProductionQuantity < 0) {
                    this.$kingzon_messager.showWarning("'" + item.product?.productNo + "'计划生产数不能小于0");
                    return;
                }
                if (!item.salesUnitId) {
                    this.$kingzon_messager.showWarning("'" + item.product?.productNo + "'下单单位未选择");
                    return;
                }
                if (!item.deliveryTime) {
                    this.$kingzon_messager.showWarning("'" + item.product?.productNo + "'交期未确定");
                    return;
                }
            }
            var add = this.$refs.add as InstanceType<typeof BodyAddEdit>;
            data.customSaleAttributeList = add.getCustomeSaleAttributeList(); //客户下单扩展参数
            var uploadFileData = add.getFileList();
            this.isLoading = true;
            this.$http.post('api/sale/SaleOrderManage/AddOrder', data)
                .then((res: any) => {
                    this.$kingzon_messager.showInfo(res.data.msg);

                    var newOrderId = res.data.orderId;
                    var fileData = [] as uploadFilesType[];
                    if (uploadFileData.length > 0 && newOrderId) {
                        fileData.push({
                            sourceId: newOrderId,
                            files: uploadFileData,
                            sourceType: FileStorageSourceType.销售订单文件,
                            fileBase: FileBase.SaleOrder
                        } as uploadFilesType)
                    }
                    if (fileData.length > 0) {
                        //上传多组文件
                        this.$utils.uploadFiles(fileData,
                            () => {
                                this.isLoading = false; //关闭loading
                                this.$kingzon_messager.showInfo("文件上传成功");
                                this.clearData();
                                this.$emit('success', res);
                            })
                    } else {
                        //不上传文件
                        this.clearData();
                        this.$emit('success', res);
                    }
                }).catch(err => {
                    this.isLoading = false;
                    this, this.$kingzon_messager.showError(err);
                }).finally(() => {
                    if (uploadFileData.length <= 0) { //不上传文件
                        this.isLoading = false;
                    }
                });
        },
        //清空数据
        clearData(): void {
            this.saleOrder = { ...this.defaultSaleOrder }
        }
    }
})
</script>