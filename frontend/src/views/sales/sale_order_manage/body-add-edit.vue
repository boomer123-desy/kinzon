<template>
    <div style="display: flex; flex-direction: column;height: 100%;">
        <Loading v-show="isLoading"></Loading>
        <Form ref="form" :model="saleOrder" :rules="saleOrder.orderType == SaleOrderType.客户订单 ? rules : rulesForStock"
            labelAlign="right" :labelWidth="70" class="kingzon-page-form" @validate="errors = $event"
            style="width: 100%;">
            <FormField name="orderType" label="订单类型:" style="width: 20%;" :labelWidth="90">
                <ComboBox v-model="saleOrder.orderType" :data="orderTypeOptions"
                    :disabled="detailId > 0 || !canModifyOrderData" @valueChange="onOrderTypeChange"></ComboBox>
            </FormField>
            <FormField name="customerId" label="客户:" style="width: 45%;" :labelWidth="90">
                <ComboBox :disabled="addOrEdit == 'edit'" v-model="saleOrder.customerId" :data="customerList"
                    @selectionChange="onCustomerChange">
                </ComboBox>
            </FormField>
            <FormField name="receiveUserId" label="接收人:" style="width: 35%;" :labelWidth="90">
                <ComboBox v-model="saleOrder.receiveUserId" :data="receiverList"
                    :disabled="detailId > 0 || !canModifyOrderData" textField="userName" valueField="id"></ComboBox>
            </FormField>
            <FormField name="customerOrderNo" label="客户订单号:" style="width: 20%;" :labelWidth="90"
                v-if="saleOrder.orderType == SaleOrderType.客户订单">
                <TextBox v-model="saleOrder.customerOrderNo" placeholder="客户订单号"
                    :disabled="detailId > 0 || !canModifyOrderData">
                </TextBox>
            </FormField>
            <FormField name="customerAddress" label="收货地址:" style="width: 45%" :labelWidth="90"
                v-if="saleOrder.orderType == SaleOrderType.客户订单">
                <ComboBox :placeholder="noAddress" valueField="index" v-model="saleOrder.customerAddressIndex"
                    @valueChange="onAddressChange($event)" :data="addressList" :limitToList="false"
                    :disabled="detailId > 0 || !canModifyOrderData">
                </ComboBox>
                <font-awesome-icon v-if="!(detailId > 0) && canModifyOrderData" @click="onClickAddAddress"
                    class="kingzon-pointer" :icon="['fas', 'plus']" size="xl"
                    style="color: var(--color-blue);  width: 20px; height: 20px;padding: 0 5px 0 10px;" />
            </FormField>
            <FormField name="receiveOrderTime" label="接收日期:" style="width: 35%;" :labelWidth="90">
                <DateBox v-model="saleOrder.receiveOrderTime" format="yyyy-MM-dd"
                    :disabled="detailId > 0 || !canModifyOrderData"
                    :panelStyle="{ width: '192px', height: '250px !important', maxHeight: '250px !important' }">
                </DateBox>
            </FormField>
            <!-- 扩展参数 -->
            <FormField v-for="(para) in customerSaleParaList" :key="para.systemPara.id" :labelWidth="90"
                :name="para.systemPara.id.toString()" :label="para.systemPara.dataValue + '：'"
                :style="{ width: para.systemPara.displayType == DisplayType.Textarea ? '100%' : '50%' }">
                <MutiDisplayTypeInput :para="para" @update:value="para.systemPara.value = $event">
                </MutiDisplayTypeInput>
            </FormField>
            <FormField name="remark" label="订单备注:" style="width: 100%" :labelWidth="90">
                <TextBox v-model="saleOrder.remark" :multiline="true" style="padding: 5px 0px; height: 50px;"
                    :disabled="detailId > 0 || !canModifyOrderData">
                </TextBox>
            </FormField>
            <FormField v-if="addOrEdit == 'add'" name="file" label="文件：" style="width: 100%" :labelWidth="90">
                <template #label>
                    <KingZonFileButton @onFileSelect="onFileSelect" style="width: 80px;height: 26px;">文件上传
                    </KingZonFileButton>
                </template>
                <!-- 文件列表 -->
                <FileUploadList v-model:uploadFileList="uploadFileData" style="width: 100%;"></FileUploadList>
            </FormField>
        </Form>
        <div style="margin: 0 0 10px 10px;" v-if="!(detailId > 0) && !isReview">
            <LinkButton @click="choiceProducts" class="c8">选择产品</LinkButton>
            <LinkButton @click="clear" style="margin-left: 10px;">清除</LinkButton>
        </div>
        <KingZonDataGrid id="saleOrderDataGrid" style="flex-grow: 1;" ref="dataGrid" :data="saleOrder.saleOrderDetails"
            :columnResizing="true" emptyMsg="请选择产品" :clickToEdit="true" editMode="cell" :lazy="false" :pagination=false
            :footerData="footerData" :showFooter="!canModifyOrderData" :rowCss="getRowCss">
            <GridColumnGroup :frozen="true" align="left" width="430">
                <GridHeaderRow>
                    <GridColumn cellCss="datagrid-td-rownumber" field="index" title="" align="center" width="40">
                        <template #body="scope">
                            {{ scope.rowIndex + 1 }}
                        </template>
                    </GridColumn>
                    <GridColumn field="product.productNo" title="产品编码" width="90" :overflowTextEllipsis="true">
                    </GridColumn>
                    <GridColumn field="product.productName" title="产品名称" width="120" :overflowTextEllipsis="true">
                    </GridColumn>
                    <GridColumn field="product.productTechnicalCode" title="技术型号" width="85"
                        :overflowTextEllipsis="true">
                    </GridColumn>
                    <GridColumn field="product.productSpecification" title="规格型号" width="95"
                        :overflowTextEllipsis="true">
                    </GridColumn>
                </GridHeaderRow>
            </GridColumnGroup>
            <GridColumnGroup :columnMoving="true">
                <GridHeaderRow>
                    <GridColumn field="customerMaterialNo" title="客户产品图号" width="95" :editable="true"
                        cellCss="kingzon-cell-editable">
                        <template #body="scope">
                            <div :title="scope.row.customerMaterialNo" class='kingzon-textEllipsis'>{{
                                scope.row.customerMaterialNo }}</div>
                        </template>
                        <template #edit="scope">
                            <TextBox
                                :disabled="scope.row.status == SaleOrderStatus.已完成 || (scope.row.reviewStatus == ReviewStatus.评审中 && !isReview)"
                                v-model="scope.row.customerMaterialNo">
                            </TextBox>
                        </template>
                    </GridColumn>
                    <GridColumn field="customerMaterialName" title="客户产品名称" width="95" :editable="true"
                        cellCss="kingzon-cell-editable">
                        <template #body="scope">
                            <div :title="scope.row.customerMaterialName" class='kingzon-textEllipsis'>{{
                                scope.row.customerMaterialName }}</div>
                        </template>
                        <template #edit="scope">
                            <TextBox
                                :disabled="scope.row.status == SaleOrderStatus.已完成 || (scope.row.reviewStatus == ReviewStatus.评审中 && !isReview)"
                                v-model="scope.row.customerMaterialName">
                            </TextBox>
                        </template>
                    </GridColumn>
                    <GridColumn field="product.productUnit" title="产品单位" width="65">
                    </GridColumn>
                    <GridColumn field="product.unDeliveryQuantity" title="订单送货差欠" width="100" align="right">
                    </GridColumn>
                    <GridColumn field="product.inventoryQuantity" title="库存数" align="right" width="100">
                        <template #body="scope">
                            <div>{{ scope.row.product.inventoryQuantity }}</div>
                            <div v-if="scope.row.product.ProductUnpackStockQty > 0">
                                <br />未包装{{ scope.row.product.ProductUnpackStockQty }}
                            </div>
                        </template>
                    </GridColumn>
                    <GridColumn field="product.stockQtyByProductMainUnit" title="统一单位库存" align="right" width="100">
                        <template #body="scope">
                            <div>{{ scope.row.product.stockQtyByProductMainUnit }}{{ scope.row.product.productMainUnit
                            }}</div>
                            <div v-if="scope.row.product.ProductUnpackStockQty > 0">
                                <br />未包装{{ scope.row.product.ProductUnpackStockQty }}{{
                                    scope.row.product.productMainUnit }}
                            </div>
                        </template>
                    </GridColumn>
                    <GridColumn field="product.unProductionQuantity" title="待排产数" width="100" align="right">
                    </GridColumn>
                    <GridColumn field="product.processQuantity" title="产线数量" width="100" align="right">
                    </GridColumn>
                    <GridColumn field="product.unPurchaseQuantity" title="待外购数" width="100" align="right">
                    </GridColumn>
                    <GridColumn field="product.transitQuantity" title="外购在途数" width="100" align="right">
                    </GridColumn>
                    <GridColumn field="product.productCategory" title="产品类别" width="100" :overflowTextEllipsis="true">
                    </GridColumn>
                    <GridColumn field="product.productPackageWay" title="包装方式" align="left" width="80"
                        :overflowTextEllipsis="true">
                    </GridColumn>
                    <GridColumn field="product.remark" title="产品备注" width="100" :overflowTextEllipsis="true">
                    </GridColumn>
                    <!-- 扩展参数 -->
                </GridHeaderRow>
            </GridColumnGroup>

            <GridColumnGroup :frozen="true" align="right" :width="!(detailId > 0) ? 775 : 675">
                <GridHeaderRow>
                    <GridColumn field="packageSpecification" title="外包装方案" width="100" :editable="true"
                        cellCss="kingzon-cell-editable">
                        <template #body="scope">
                            <div :title="scope.row.packageSpecification" class='kingzon-textEllipsis'>{{
                                scope.row.packageSpecification }}</div>
                        </template>
                        <template #edit="scope">
                            <ComboBox valueField="id" textField="packageSpecification" v-model="scope.row.packageId"
                                :data="scope.row.product.packageList" :panelStyle="{ minWidth: '150px' }"
                                @valueChange="onPackageChange($event, scope.row)"
                                :disabled="scope.row.status == SaleOrderStatus.已完成 || scope.row.reviewStatus == ReviewStatus.评审通过 || (scope.row.reviewStatus == ReviewStatus.评审中 && !isReview)">
                            </ComboBox>
                        </template>
                    </GridColumn>
                    <GridColumn field="placeOrderQuantity" title="下单数量" align="right" width="150" :editable="true"
                        cellCss="kingzon-cell-editable" :editRules="tableRules.placeOrderQuantity">
                        <template #body="scope">
                            <div style="display: flex;flex-direction: row;">
                                <div
                                    style="margin-right: 5px;width: 40px;height: 24px;display: flex;flex-direction: column;justify-content: space-between;">
                                    <span class="kingzon-pointer" @click.stop="addPlanQuantity(scope.row)"
                                        v-if="scope.row.status != SaleOrderStatus.已完成 || scope.row.reviewStatus == ReviewStatus.评审通过"
                                        style="color: var(--color-blue);font-size: 10px;line-height: 10px;">计划数量</span>
                                    <span class="kingzon-pointer" @click.stop="addUnitConversion(scope.row)"
                                        v-if="scope.row.status != SaleOrderStatus.已完成 || scope.row.reviewStatus == ReviewStatus.评审通过"
                                        style="color: var(--color-blue);font-size: 10px;line-height: 10px;">单位转换</span>
                                </div>
                                <span style="flex:1;line-height: 24px;"> {{ scope.row.placeOrderQuantity }} {{
                                    scope.row.salesUnit }}</span>
                            </div>
                            <div
                                style="color: var(--color-grey);font-size: 10px;display: flex;flex-direction: row;justify-content: space-between;">
                                <span>计</span>
                                <span style="margin-left: 4px;">
                                    直发{{ scope.row.planDirectDliveryQuantity }}/外购{{ scope.row.planPurchaseQuantity
                                    }}/生产{{ scope.row.planProductionQuantity }}</span>
                            </div>
                            <div v-if="scope.row.allOrganizeQuantity"
                                style="color: var(--color-grey);font-size: 10px;display: flex;flex-direction: row;justify-content: space-between;">
                                <span>实</span>
                                <span style="margin-left: 4px;">
                                    已发{{ scope.row.deliveryQuantity }}/已购{{ scope.row.purchaseQuantity
                                    }}/排产{{ scope.row.productionQuantity }}</span>
                            </div>
                            <div style="display: flex;flex-direction: row;justify-content: right;">
                                <div style="font-style: italic;color: var(--color-grey);font-size: 12px;margin-left: 5px;"
                                    v-for="unit in scope.row.unitConversionList" :key="unit.unitId">
                                    {{ unit.placeOrderQuantity }}{{ unit.unitName }}</div>
                            </div>
                        </template>
                        <template #edit="scope">
                            <NumberBox :precision="2"
                                :disabled="scope.row.status == SaleOrderStatus.已完成 || scope.row.reviewStatus == ReviewStatus.评审通过 || (scope.row.reviewStatus == ReviewStatus.评审中 && !isReview)"
                                v-model="scope.row.placeOrderQuantity" :min="minQuantity(scope.row)"
                                @valueChange="computeTotalPrice(scope.row)" :spinners="false"></NumberBox>
                            <ComboBox valueField="code" v-model="scope.row.salesUnitConversionId"
                                @valueChange="onUnitChange($event, scope.row)" :panelStyle="{ minWidth: '100px' }"
                                :data="scope.row.product.unitList"
                                :disabled="scope.row.status == SaleOrderStatus.已完成 || scope.row.reviewStatus == ReviewStatus.评审通过 || (scope.row.reviewStatus == ReviewStatus.评审中 && !isReview)">
                                <template #item="scope">
                                    {{ scope.row.text }}{{ scope.row.extension ? '(' + scope.row.extension + ')' : '' }}
                                </template>
                            </ComboBox>
                        </template>
                    </GridColumn>
                    <GridColumn field="price" title="不含税单价" align="right" width="80" :editable="true"
                        cellCss="kingzon-cell-editable">
                        <template #body="scope">
                            <span> {{ scope.row.price.toFixed(4) }} </span>
                        </template>
                        <template #edit="scope">
                            <NumberBox
                                :disabled="scope.row.status == SaleOrderStatus.已完成 || scope.row.reviewStatus == ReviewStatus.评审通过 || (scope.row.reviewStatus == ReviewStatus.评审中 && !isReview)"
                                v-model="scope.row.price" :min="0" :precision="4"
                                @valueChange="computeTotalPrice(scope.row)" :spinners="false">
                            </NumberBox>
                        </template>
                    </GridColumn>
                    <GridColumn field="totalPrice" title="总价" width="80" align="right">
                        <template #body="scope">
                            <span>{{ scope.row.totalPrice?.toFixed(4) }}</span>
                        </template>
                    </GridColumn>
                    <GridColumn field="deliveryTime" title="客户交期" width="100" :editable="true"
                        cellCss="kingzon-cell-editable" :editRules="tableRules.deliveryTime">
                        <template #body="scope">
                            <div v-if="scope.row.deliveryTime">
                                {{ $utils.formatDateTime(scope.row.deliveryTime, 'yyyy-MM-dd') }}
                            </div>
                        </template>
                        <template #edit="scope">
                            <DateBox
                                :disabled="scope.row.status == SaleOrderStatus.已完成 || (scope.row.reviewStatus == ReviewStatus.评审中 && !isReview)"
                                v-model="scope.row.deliveryTime" format="yyyy-MM-dd"
                                :panelStyle="{ width: '192px', height: '250px !important', maxHeight: '250px !important' }">
                            </DateBox>
                        </template>
                    </GridColumn>
                    <GridColumn field="isUrgent" title="是否加急" width="70" align="center" cellCss="kingzon-cell-editable">
                        <template #body="scope">
                            <SwitchButton
                                :disabled="scope.row.status == SaleOrderStatus.已完成 || (scope.row.reviewStatus == ReviewStatus.评审中 && !isReview)"
                                style="height:24px; width: 60px;" v-model="scope.row.isUrgent" onText="是" offText="否"
                                class="Kingzon-Red-Switch">
                            </SwitchButton>
                        </template>
                    </GridColumn>
                    <GridColumn field="remark" title="备注" width="100" :editable="true" cellCss="kingzon-cell-editable">
                        <template #body="scope">
                            <div :title="scope.row.remark" class='kingzon-textEllipsis'>{{ scope.row.remark }}</div>
                        </template>
                        <template #edit="scope">
                            <TextBox
                                :disabled="scope.row.status == SaleOrderStatus.已完成 || (scope.row.reviewStatus == ReviewStatus.评审中 && !isReview)"
                                v-model="scope.row.remark" :multiline="true" style="padding: 5px 0px; height: 50px;">
                            </TextBox>
                        </template>
                    </GridColumn>
                    <GridColumn field="action" title="操作" align="center" width="95" v-if="!(detailId > 0) && !isReview">
                        <template #body="scope">
                            <ButtonGroup style="height:24px">
                                <LinkButton @click="copeRow(scope.row, scope.rowIndex)">复制
                                </LinkButton>
                                <LinkButton
                                    v-if="scope.row.status != SaleOrderStatus.已完成 && scope.row.reviewStatus != ReviewStatus.评审中"
                                    @click="deleteRow(scope.row)">删除
                                </LinkButton>
                            </ButtonGroup>
                        </template>
                    </GridColumn>
                </GridHeaderRow>
            </GridColumnGroup>
        </KingZonDataGrid>
        <div v-if="canModifyOrderData" style="text-align: right; margin: 5px 1% 0 0;">
            {{ "订单总价：" + saleOrder.orderTotalPrice?.toFixed(4) + "元" }}
        </div>
        <Dialog ref="addAddressDialog" :closed="true" :modal="true" :title="'新增收货地址'"
            :dialogStyle="{ height: '550px', width: '500px' }">
            <AddAddress @close="addCancel" @submit="addSuccess" :addressList="addressList"
                :customerId="saleOrder.customerId">
            </AddAddress>
        </Dialog>
        <Drawer v-if="showDrawer" @collapse="showDrawer = false;" :drawerStyle="{ width: '95%' }" region="east"
            ref="choiceProductDrawer" :closable="true" :maskClosable="false">
            <template #header>
                <div style="display: flex;justify-content: left;">
                    <div>【{{ saleOrder.customer.customerName }}】选择产品</div>
                    <linkButton :plain="true" style="height: 24px;color: var(--color-blue);margin-left: 10px;"
                        @click="choiceOtherProducts()">配置客户产品
                    </linkButton>
                </div>
            </template>
            <ChoiceProduct ref="choiceProduct" @cancel="choiceCancel" @success="choiceSuccess"
                :model="saleOrder.customer" :originProducts="productList"
                :defaultCustomer="IsDefaultCustomer(saleOrder.customer)" />
        </Drawer>
        <Dialog ref="addUnitConversionDialog" :closed="true" :modal="true" title="单位转换" :draggable="true"
            :resizable="true" :dialogStyle="{ width: '600px', height: '400px' }">
            <UnitConversion @cancel="addUnitConversionCancel" @success="addUnitConversionSuccess"
                :orderData="selectdOrderDetail" />
        </Dialog>
        <Dialog ref="addPlanQtyDialog" :closed="true" :modal="true" title="计划数量" :draggable="true" :resizable="true"
            :dialogStyle="{ width: '600px', height: '400px' }">
            <PlanQuantity @cancel="addPlanQtyCancel" @success="addPlanQtySuccess" :orderData="selectdOrderDetail" />
        </Dialog>
        <!-- 新增客户产品 -->
        <Drawer v-if="showProduct" @collapse="addCustomerProductClose()" ref="selectProductDrawer"
            :title="saleOrder.customer.customerCode + '【' + saleOrder.customer.customerName + '】- 选择产品'"
            :drawerStyle="{ width: '95%' }" region="east" :maskClosable="false" :closable="true"
            @expand="$utils.updateModal()">
            <AddCustomerGoods :customer="saleOrder.customer"></AddCustomerGoods>
        </Drawer>
        <Dialog ref="fileUpload" :title="'产品文件上传'" :dialogStyle="{ width: '600px', maxHeight: '600px' }"
            :draggable="true" :resizable="true" :modal="true" :closed="true">
            <FileUpload @close="fileDialogClose" :uploadFileList="uploadFileList" :upload="false"
                @submit="uploadFileSubmit">
            </FileUpload>
        </Dialog>
    </div>
</template>
<script lang="ts">
import { BaseCustomer, BaseProductPackage, CustomerAddressDto, SystemParaTreeDto } from '@/types/common';
import { ReviewStatus, SaleOrderStatus, SaleOrderType, SystemParaType, DisplayType } from '@/types/enums';
import { SaleOrder, SaleOrderDetail, SaleGoodsDto, SaleUnitConversionDto, SaleGoodsUnitDto } from '@/types/sale';
import AddAddress from '@/views/customer_manage/add-address.vue';
import { cloneDeep, sum } from 'lodash';
import { defineComponent } from 'vue';
import ChoiceProduct from './choice-product.vue';
import type { CustomAttribute, SelectOption, UploadFile } from '@/types/types';
import Decimal from 'decimal.js';
// import AddCustomerProducts from './add-customer-products.vue';
import UnitConversion from './unit-conversion.vue';
import PlanQuantity from './plan-quantity.vue';
import AddCustomerGoods from '@/views/customer_manage/select-product.vue'

export default defineComponent({
    name: 'body-add-edit',
    props: ['model', 'addOrEdit', 'detailId', 'isReview', 'canModifyOrderData'],
    emits: ['update:model'],
    components: {
        AddAddress,
        ChoiceProduct,
        AddCustomerGoods, UnitConversion, PlanQuantity
    },
    data() {
        return {
            isLoading: false,
            saleOrder: new SaleOrder(),
            defaultSaleOrder: new SaleOrder(),
            customerList: [] as SelectOption[],
            receiverList: [],
            addressList: [] as CustomerAddressDto[],
            newAddressId: 0 as number, //  >0时，展示新增的地址，否则展示默认的地址
            showOtherQuantity: false,
            showDrawer: false,
            noAddress: '',  // 地址为空时提示用户
            minPlaceOrderQuantity: 1, // 最小下单数量
            rules: {
                customerId: 'required',
                orderType: 'required',
                receiveUserId: 'required',
                receiveOrderTime: 'required',
                customerAddress: 'required',
            },
            rulesForStock: {
                customerId: 'required',
                orderType: 'required',
                receiveUserId: 'required',
                receiveOrderTime: 'required',
            },
            tableRules: {
                placeOrderQuantity: 'required',
                deliveryTime: 'required',
                'product.productNo': 'required',
            },
            errors: {},
            addAddressDialog: {} as EasyUI.Dialog,  // 新增地点的Dialog
            choiceProductDrawer: {} as EasyUI.Drawer,  // 选择产品的Drawer
            orderTypeOptions: [] as SelectOption[],
            addCustomerProductsDialog: {} as EasyUI.Dialog,
            selectdOrderDetail: {} as SaleOrderDetail,
            addUnitConversionDialog: {} as EasyUI.Dialog,
            addPlanQtyDialog: {} as EasyUI.Dialog,
            showProduct: false,
            showProductDrawer: {} as EasyUI.Drawer,
            uploadFileList: [] as UploadFile[],
            uploadFileData: [] as UploadFile[],  //上传文件数据
            uploadFileDialog: {} as EasyUI.Dialog,
            customerSaleParaList: [] as SystemParaTreeDto[],//客户-下单扩展参数
        }
    },
    computed: {
        SaleOrderStatus() {
            return SaleOrderStatus;
        },
        SaleOrderType() {
            return SaleOrderType;
        },
        ReviewStatus() {
            return ReviewStatus;
        },
        DisplayType() {
            return DisplayType;
        },
        // 组合产品列表，用于选择产品时的数据
        productList() {
            var products = [] as any[];
            if (this.saleOrder.saleOrderDetails && this.saleOrder.saleOrderDetails.length > 0) {
                //去除直接复制的产品
                var list = this.saleOrder.saleOrderDetails
                    .map((item: SaleOrderDetail) => {
                        return {
                            ...item.product,
                            checkBoxDisabled: item.status === SaleOrderStatus.已完成
                        };
                    });
                list.forEach(item => {
                    if (products.findIndex((p: SaleGoodsDto) => p.idField == item.idField) == -1) {
                        products.push(item);
                    }
                })
            }
            return products;
        },
        minQuantity() {
            var that = this;
            return function (row: SaleOrderDetail) {
                var res = 0;
                if (row.status == SaleOrderStatus.进行中
                    // && row.reviewStatus != ReviewStatus.待提交
                ) {
                    //已排产+已外购+已送货(售卖单位)
                    var qty = Decimal.add(row.deliveryQuantity, row.productionQuantity).add(row.purchaseQuantity).toNumber();
                    var defaultPlaceOrderQuantity = that.defaultSaleOrder.saleOrderDetails.find((item: any) => item.id == row.id)?.placeOrderQuantity ?? 0;;
                    res = Math.max(res, Decimal.sub(defaultPlaceOrderQuantity, qty).toNumber());
                }
                return res;
            };
        },
        footerData() {
            var footerData = [{
                index: '合计',
                totalPrice: this.saleOrder.saleOrderDetails.reduce((total, item: SaleOrderDetail) => {
                    var t = new Decimal(total);
                    var c = new Decimal(item.totalPrice ?? 0);
                    return t.add(c).toNumber();
                }, 0),
            }];
            return footerData;
        },
    },
    watch: {
        saleOrder: {
            handler(val: SaleOrder) {
                this.$emit("update:model", val);
            },
            deep: true
        }
    },
    created() {
        this.defaultSaleOrder = cloneDeep(this.model);
        this.saleOrder = cloneDeep(this.model);

        this.orderTypeOptions = this.$utils.getEnumKeyValueArray(SaleOrderType);
    },
    mounted() {
        this.uploadFileDialog = this.$refs.fileUpload as EasyUI.Dialog;
        this.getData();
    },
    methods: {
        // 获取数据  获取收货人 -> 获取客户数据
        getData() {
            this.isLoading = true;
            var postProductDto = this.saleOrder.saleOrderDetails.map(x => {
                return {
                    idField: x.product.productId + "_" + x.product.specificationId + "_" + x.goodsId,
                    productId: x.product.productId,
                    specificationId: x.product.specificationId
                }
            });
            this.getReceivers()
                .then((res: any) => {
                    this.receiverList = res.data;
                    return this.getCustomers();
                })
                .then((res: any) => {
                    return this.getCustomerSaleParaData();
                })
                .then((res: any) => {
                    this.customerSaleParaList = res.data;
                    if (this.saleOrder.customSaleAttributeList && this.saleOrder.customSaleAttributeList.length > 0) {
                        this.customerSaleParaList.forEach((para: SystemParaTreeDto) => {
                            var value = this.saleOrder.customSaleAttributeList?.find((item: CustomAttribute) => item.systemParaId == para.systemPara.id)?.value;
                            if (value) {
                                para.systemPara.value = value as any;
                            }
                        });
                    }

                    if (this.saleOrder.customerId) {
                        return this.getCustomerAddresses();
                    }
                }).then((res: any) => {
                    if (this.saleOrder.saleOrderDetails.length > 0) {
                        //获取产品的外包装方式列表
                        return this.getProductPackageData(postProductDto)
                    }
                }).then((res: any) => {
                    if (res) {
                        var packageData = res.data as Array<{ idField: string, packageList: Array<BaseProductPackage> }>;
                        this.saleOrder.saleOrderDetails.forEach((item: SaleOrderDetail) => {
                            var idField = item.product.productId + "_" + item.product.specificationId + "_" + item.goodsId
                            var pData = packageData.find(e => e.idField == idField);
                            item.product.packageList = pData?.packageList;
                        });
                        if (this.saleOrder.saleOrderDetails.length > 0) {
                            //获取产品的单位列表
                            return this.getProductUnitData(postProductDto)
                        }
                    }
                }).then((res: any) => {
                    if (res) {
                        var unitData = res.data as Array<{ idField: string, unitList: Array<SaleGoodsUnitDto> }>;
                        this.saleOrder.saleOrderDetails.forEach((item: SaleOrderDetail) => {
                            var idField = item.product.productId + "_" + item.product.specificationId + "_" + item.goodsId
                            var uData = unitData.find(e => e.idField == idField);
                            item.product.unitList = uData?.unitList;
                        });
                    }
                })
                .catch((err: any) => {
                    this.$kingzon_messager.showError(err.length > 0 ? err : '数据获取失败');
                }).finally(() => {
                    this.isLoading = false;
                });
        },
        choiceProducts() {
            if (!this.saleOrder.customerId) {
                this.$kingzon_messager.showError('请先选择客户');
                return;
            }
            this.showDrawer = true;
            this.$nextTick(() => {
                this.choiceProductDrawer = this.$refs.choiceProductDrawer as EasyUI.Drawer;
                this.choiceProductDrawer.expand();
            });
        },
        clear() {
            // 已完成的订单详情不能清除
            this.saleOrder.saleOrderDetails = this.saleOrder.saleOrderDetails.filter((item: SaleOrderDetail) => item.status == SaleOrderStatus.已完成);
        },
        // 删除行
        deleteRow(row: SaleOrderDetail) {
            this.$messager.confirm({
                title: '提示',
                msg: '是否确认删除?',
                result: (r: boolean) => {
                    if (r) {
                        this.saleOrder.saleOrderDetails = this.saleOrder.saleOrderDetails.filter((item: SaleOrderDetail) => item !== row);
                        //更新合计金额
                        this.saleOrder.orderTotalPrice = sum(this.saleOrder.saleOrderDetails?.map((item: any) => item.totalPrice)) ?? 0;
                    }
                },
            });
        },
        // 计算详情总价 和 订单总价
        computeTotalPrice(row?: SaleOrderDetail) {
            if (row) {
                row.totalPrice = Decimal.mul((row.price ?? 0), (row.placeOrderQuantity ?? 0)).toNumber();
                // 状态为未完成的订单，原订单数量只能增加，不能减少
                if (row.status == SaleOrderStatus.进行中) {
                    this.minPlaceOrderQuantity = this.defaultSaleOrder.saleOrderDetails.find((item: any) => item.id == row.id)?.placeOrderQuantity ?? 1;
                }
                row.planProductionQuantity = row.placeOrderQuantity - row.planPurchaseQuantity - row.planDirectDliveryQuantity;
                row.unitConversionList.forEach(item => {
                    item.placeOrderQuantity = Decimal.mul(row.placeOrderQuantity, item.ratio).toDecimalPlaces(4).toNumber();
                })
            }
            this.saleOrder.orderTotalPrice = sum(this.saleOrder.saleOrderDetails?.map((item: any) => item.totalPrice)) ?? 0;
        },
        //订单类型变化后
        onOrderTypeChange(event: { currentValue, previousValue }) {
            var newType = event.currentValue;
            if (newType == SaleOrderType.备货订单) {
                //清空客户订单号、收货地址
                this.saleOrder.customerOrderNo = undefined;
                this.saleOrder.customerAddressIndex = undefined;
                this.saleOrder.customerAddress = undefined;
            }
            if (newType == SaleOrderType.客户订单 && this.IsDefaultCustomer(this.saleOrder.customer)) {
                //清空选中客户、收货地址、选中产品
                this.saleOrder.customerId = undefined;
                this.saleOrder.customer = new BaseCustomer();
                this.saleOrder.customerAddressIndex = undefined;
                this.saleOrder.customerAddress = undefined;
                this.saleOrder.saleOrderDetails = [];
            }
            this.isLoading = true;
            this.getCustomers() //更新客户列表
                .catch((err: any) => {
                    this.$kingzon_messager.showError(err.length > 0 ? err : '数据获取失败');
                }).finally(() => {
                    this.isLoading = false;
                });
        },
        // 客户变化后
        onCustomerChange(event: SelectOption) {
            //清空客户地址
            this.newAddressId = 0;
            this.addressList = [] as CustomerAddressDto[];

            if (!event || !event.value) { //客户置空
                this.saleOrder.customer = new BaseCustomer();
                this.saleOrder.customerAddressIndex = undefined;
                this.saleOrder.customerAddress = undefined;
                return
            }
            //客户赋值
            this.saleOrder.customer.id = Number(event.value);

            this.saleOrder.customer.customerName = event.text as string;
            this.saleOrder.customer.customerNo = event.code as string;
            if (this.addOrEdit == 'edit') { //作为编辑组件时 不用置空地址
                return;
            }
            //地址置空
            this.saleOrder.customerAddressIndex = undefined;
            this.saleOrder.customerAddress = undefined;

            if (this.saleOrder.orderType == SaleOrderType.备货订单) { //备货订单 无需选择地址
                return;
            }

            //获取客户地址 和下单扩展参数
            this.customerSaleParaList.forEach((para: SystemParaTreeDto) => {
                var value = event.extension?.find((item: CustomAttribute) => item.systemParaId == para.systemPara.id)?.value;
                if (value) {
                    para.systemPara.value = value as any;
                } else {
                    para.systemPara.value = undefined;
                }
            });
            this.isLoading = true;
            this.getCustomerAddresses()
                .catch((err: any) => {
                    this.$kingzon_messager.showError(err.length > 0 ? err : '获取地址数据失败');
                }).finally(() => {
                    this.isLoading = false;
                });
        },
        getCustomerSaleParaData() {
            return this.$http.get('/api/base/SystemPara/GetAdditionalParam', { type: SystemParaType.客户下单附加参数 });
        },
        getCustomers() {
            //业务员名下的客户 以及 未分配业务员的客户
            return this.$http.get('api/base/Customer/GetSalesManAllCustomer', {
                includeMain: this.saleOrder.orderType == SaleOrderType.客户订单 ? false : true
            }).then((res: any) => {
                this.customerList = res.data?.map((item: BaseCustomer) => {
                    return { value: item.id, text: item.customerCode + '-' + item.customerName, code: item.customerNo, extension: JSON.parse(item.customSaleAttribute ?? "[]") }
                });
            });
        },
        getReceivers(): Promise<HttpResponseType> {
            return this.$http.get('api/base/User/GetAllUser');
        },
        getCustomerAddresses() {
            return this.$http.get('api/base/Customer/GetCustomerAddresses', { customerId: this.saleOrder.customerId })
                .then((res: any) => {
                    // 选地址、新增地址时，地址列表展示的数据，需要对text去重
                    this.addressList = res.data?.map((item: CustomerAddressDto) => {
                        const text = (item.contact ? item.contact + '-' : "") + (item.phone ? item.phone + '-' : "") + item.address + (item.remark ? '【' + item.remark + '】' : '');
                        item.text = text;
                        return item;
                    });
                    if (this.addressList.length == 0) {
                        this.saleOrder.customerAddressIndex = undefined;
                        this.saleOrder.customerAddress = undefined;
                        this.noAddress = '该客户暂无收货地址，请点击右侧按钮添加';
                        return;
                    }
                    // 在新增切换客户时，如果有默认地址则展示默认地址，否则展示第一个地址
                    if (this.addOrEdit == 'add' && !this.newAddressId) {
                        var address = this.addressList.find((item: CustomerAddressDto) => item.isDefault)
                        this.saleOrder.customerAddressIndex = address?.index;
                        this.saleOrder.customerAddress = address;
                    }
                    if (this.newAddressId) {
                        var address1 = this.addressList.find((item: CustomerAddressDto) => item.index == this.newAddressId)
                        this.saleOrder.customerAddressIndex = address1?.index;
                        this.saleOrder.customerAddress = address1;
                    }
                });
        },
        // 新增地址按钮
        onClickAddAddress() {
            if (!this.saleOrder.customerId) {
                this.$kingzon_messager.showError('请先选择客户');
                return;
            }
            this.addAddressDialog = this.$refs.addAddressDialog as EasyUI.Dialog;
            this.addAddressDialog.open();
        },
        addCancel() {
            this.addAddressDialog.close();
        },
        addSuccess(id: number) {
            this.addAddressDialog.close();
            this.newAddressId = id;

            this.isLoading = true;
            // 获取客户地址列表
            this.getCustomerAddresses()
                .catch((err: any) => {
                    this.$kingzon_messager.showError(err.length > 0 ? err : '获取地址数据失败');
                }).finally(() => {
                    this.isLoading = false;
                });
        },
        // 选择产品页面内点击取消时
        choiceCancel() {
            this.choiceProductDrawer.collapse();
        },
        // 选择产品页面内点击确定时, 选中的产品
        choiceSuccess(products: SaleGoodsDto[]) {
            this.choiceProductDrawer.collapse();

            // 绑定关系被取消（有下单记录的不能取消）则从订单中删除选中的产品
            const setA = new Set(products.map((item: SaleGoodsDto) => item.idField));
            this.saleOrder.saleOrderDetails = this.saleOrder.saleOrderDetails.filter((item: SaleOrderDetail) => setA.has(item.product.idField));

            const setB = new Set(this.saleOrder.saleOrderDetails.map(x => x.product.idField));
            var postProductDto = products.filter(x => !setB.has(x.idField)) //过滤出新增的产品
                .map(x => {
                    return {
                        idField: x.idField,
                        productId: x.productId,
                        specificationId: x.specificationId
                    }
                });

            this.isLoading = true;
            var packageData = [] as Array<{ idField: string, packageList: Array<BaseProductPackage> }>;
            var unitData = [] as Array<{ idField: string, unitList: Array<SaleGoodsUnitDto> }>;
            //获取产品的外包装方式列表
            this.getProductPackageData(postProductDto)
                .then(res => {
                    packageData = res.data as Array<{ idField: string, packageList: Array<BaseProductPackage> }>;
                    //获取产品的单位列表
                    return this.getProductUnitData(postProductDto)
                }).then(res => {
                    unitData = res.data as Array<{ idField: string, unitList: Array<SaleGoodsUnitDto> }>;
                    // 新增的绑定关系，把选中的产品添加到订单中
                    products.forEach((item: SaleGoodsDto) => {
                        var pData = packageData.find(e => e.idField == item.idField);
                        item.packageList = pData?.packageList;
                        var uData = unitData.find(e => e.idField == item.idField);
                        item.unitList = uData?.unitList;

                        if (!setB.has(item.idField)) {
                            let saleOrderDetail = new SaleOrderDetail();
                            saleOrderDetail.goodsId = item.goodsId;//下钧业的备货订单时 为空
                            saleOrderDetail.product = item;
                            saleOrderDetail.placeOrderQuantity = 0;
                            saleOrderDetail.planProductionQuantity = 0;
                            saleOrderDetail.planPurchaseQuantity = 0;
                            saleOrderDetail.planDirectDliveryQuantity = 0;
                            saleOrderDetail.price = item.price;
                            saleOrderDetail.totalPrice = 0;
                            saleOrderDetail.customerMaterialNo = item.customerMaterialNo;
                            saleOrderDetail.customerMaterialName = item.customerMaterialName;
                            saleOrderDetail.packageId = item.packageId;
                            saleOrderDetail.packageSpecification = item.packageList?.find(x => x.id == saleOrderDetail.packageId)?.packageSpecification;
                            saleOrderDetail.salesUnitConversionId = item.goodsId && item.salesUnitConversionId ? item.salesUnitConversionId : -1;//对应主单位时 ConversionId为-1
                            var unit = item.unitList?.find(x => x.code == saleOrderDetail.salesUnitConversionId);
                            saleOrderDetail.salesUnitId = unit?.unitId; //item.goodsId && item.salesUnitId ? item.salesUnitId : item.productUnitId;
                            saleOrderDetail.salesUnit = unit?.text;
                            this.unitConversion(saleOrderDetail.salesUnitConversionId, -1, saleOrderDetail); //默认新增和主单位的单位转换
                            this.saleOrder.saleOrderDetails.push(saleOrderDetail);
                        }
                    });
                })
                .catch((err: any) => {
                    this.$kingzon_messager.showError(err.length > 0 ? err : '获取数据失败');
                }).finally(() => {
                    this.isLoading = false;
                });

            // 计算订单价格
            this.computeTotalPrice();
        },
        getProductPackageData(postProductDto) {
            if (postProductDto.length <= 0) {
                return Promise.resolve({ data: [] } as HttpResponseType);
            }
            return this.$http.post("api/sale/SaleOrderManage/GetProductPackageList", postProductDto)
        },
        getProductUnitData(postProductDto) {
            if (postProductDto.length <= 0) {
                return Promise.resolve({ data: [] } as HttpResponseType);
            }
            return this.$http.post("api/sale/SaleOrderManage/GetProductUnitList", postProductDto)
        },
        //复制同编码的产品
        copeRow(row: SaleOrderDetail, index) {
            let newRow = cloneDeep(row);
            newRow.id = 0;
            newRow.placeOrderQuantity = 0;
            newRow.totalPrice = 0;
            newRow.planProductionQuantity = 0;
            newRow.planPurchaseQuantity = 0;
            newRow.planDirectDliveryQuantity = 0;
            //清空单位转换的下单数量
            newRow.unitConversionList.forEach(item => {
                item.placeOrderQuantity = 0;
            })
            this.saleOrder.saleOrderDetails.splice(index + 1, 0, newRow);
        },
        //判断客户是不是企业自身
        IsDefaultCustomer(customer: BaseCustomer) {
            if (customer.customerNo == "DEFAULT") {
                return true;
            }
            return false;
        },
        //点击选择其他产品
        choiceOtherProducts() {
            this.showProduct = true;
            this.$nextTick(() => {
                this.showProductDrawer = this.$refs.selectProductDrawer as EasyUI.Drawer;
                this.showProductDrawer.expand();
            })
        },
        addOtherProductCancel() {
            this.addCustomerProductsDialog.close();
        },
        addOtherProductSuccess(rem: any, ins: any) {
            this.addCustomerProductsDialog.close();
            var choose = this.$refs.choiceProduct as any;
            if (choose) {
                choose.addNewProduct(rem, ins);
            }
        },
        onUnitChange({ currentValue, previousValue }, row: SaleOrderDetail) {
            if (!currentValue) {
                row.salesUnit = undefined;
                return
            };
            var uData = row.product.unitList?.find(x => x.code == currentValue);
            row.salesUnitId = uData?.unitId;
            row.salesUnit = uData?.text;
            //新增和主单位的单位转换
            this.unitConversion(currentValue, -1, row);
            //更新单位转换
            row.unitConversionList = row.unitConversionList.filter(x => x.id != row.salesUnitConversionId);
            row.unitConversionList.forEach(item => {
                this.unitConversion(row.salesUnitConversionId, item.id, row, item);
            })
        },
        onPackageChange({ currentValue, previousValue }, row: SaleOrderDetail) {
            if (!currentValue) {
                row.packageSpecification = undefined;
                return
            };
            var pData = row.product.packageList?.find(x => x.id == currentValue);
            row.packageSpecification = pData?.packageSpecification;
            var oldUnitConversionId = row.salesUnitConversionId;
            var tmp = row.product.unitList?.find(x => x.packageId == pData?.id);
            row.salesUnitConversionId = tmp?.code;
            row.salesUnitId = tmp?.unitId;
            row.salesUnit = tmp?.text;
            //手动触发变更
            if (oldUnitConversionId != row.salesUnitConversionId) {
                this.onUnitChange({ currentValue: row.salesUnitConversionId, previousValue: oldUnitConversionId }, row)
            }
        },
        onAddressChange({ currentValue, previousValue }) {
            if (!currentValue) {
                this.saleOrder.customerAddressIndex = undefined;
                this.saleOrder.customerAddress = undefined;
                return
            };
            var address = this.addressList.find(x => x.index == currentValue);
            this.saleOrder.customerAddress = address;
        },
        //新增单位转换
        addUnitConversion(row) {
            this.selectdOrderDetail = row;
            this.addUnitConversionDialog = this.$refs.addUnitConversionDialog as EasyUI.Dialog;
            this.addUnitConversionDialog.open();
        },
        //新增或更新单位转换
        unitConversion(fromUnitConversionId, toUnitConversionId, row: SaleOrderDetail, existConversion?: SaleUnitConversionDto | undefined) {
            if (fromUnitConversionId == toUnitConversionId) {
                return;
            }
            //已存在单位转换关系，更新转换比例和数量
            if (!existConversion) {
                existConversion = row.unitConversionList.find(x => x.id == toUnitConversionId);
            }

            var fromUnit = row.product.unitList?.find(x => x.code == fromUnitConversionId);
            var toUnit = row.product.unitList?.find(x => x.code == toUnitConversionId);
            if (fromUnit && toUnit) {
                var ratio = 0;
                if (toUnit.code == -1) { //toUnit是主单位
                    ratio = Number(fromUnit.value) ?? 0;
                } else { //toUnit不是是主单位 
                    var toUnitRatio = Number(toUnit.value) ?? 0;
                    var fromRatio = Number(fromUnit.value) ?? 0;
                    ratio = toUnitRatio ? (fromRatio / toUnitRatio) : 0;
                }
                if (existConversion) {
                    existConversion.ratio = ratio;
                    existConversion.placeOrderQuantity = Decimal.mul(ratio, row.placeOrderQuantity).toDecimalPlaces(4).toNumber();
                } else {
                    var conversion = {
                        id: toUnitConversionId,
                        unitId: toUnit.unitId,
                        unitName: toUnit.text,
                        ratio: ratio,
                        placeOrderQuantity: Decimal.mul(ratio, row.placeOrderQuantity).toDecimalPlaces(4).toNumber(),
                        remark: toUnit.extension,
                        packageId: toUnit.packageId,
                        isBaseUnit: toUnit.code == -1 //是否是主单位
                    } as SaleUnitConversionDto;
                    if (!row.unitConversionList.includes(conversion)) {
                        row.unitConversionList.push(conversion);
                    }
                }
            }
        },
        addUnitConversionSuccess(unitConversionList) {
            this.selectdOrderDetail.unitConversionList = unitConversionList;
            this.addUnitConversionDialog.close();
        },
        addUnitConversionCancel() {
            this.addUnitConversionDialog.close();
        },
        addPlanQuantity(row) {
            this.selectdOrderDetail = row;
            this.addPlanQtyDialog = this.$refs.addPlanQtyDialog as EasyUI.Dialog;
            this.addPlanQtyDialog.open();
        },
        addPlanQtyCancel() {
            this.addPlanQtyDialog.close();
        },
        addPlanQtySuccess(planQty) {
            this.selectdOrderDetail.planDirectDliveryQuantity = planQty.delivery;
            this.selectdOrderDetail.planPurchaseQuantity = planQty.purchase;
            this.selectdOrderDetail.planProductionQuantity = planQty.production;

            this.addPlanQtyDialog.close();
        },
        getRowCss(row): Object | null {
            let rootElement = document.querySelector(':root ');
            if (row.reviewStatus == ReviewStatus.评审中 && rootElement && !this.isReview) {
                var color = getComputedStyle(rootElement).getPropertyValue('--color-yellow-bg').trim();
                return { backgroundColor: color };
            }
            if (row.status == SaleOrderStatus.已完成 && rootElement) {
                var color1 = getComputedStyle(rootElement).getPropertyValue('--color-green-bg').trim();
                return { backgroundColor: color1 };
            }
            return null;
        },
        addCustomerProductClose() {
            this.showProduct = false;
            var chooseProduct = this.$refs.choiceProduct as InstanceType<typeof ChoiceProduct>;
            chooseProduct.getProductList();
        },
        onFileSelect(files) {
            this.uploadFileList = files;
            this.uploadFileDialog.open();
        },
        fileDialogClose() {
            this.uploadFileDialog.close();
        },
        uploadFileSubmit(files) {
            //此时文件未上传后端 新增产品时再添加文件
            this.uploadFileData = files;
            this.uploadFileDialog.close();
        },
        getFileList() {
            return this.uploadFileData;
        },
        getCustomeSaleAttributeList() {
            return this.customerSaleParaList.map(x => {
                return { systemParaId: x.systemPara.id, value: x.systemPara.value }
            })
        },
    }
})
</script>

<style scoped>
:deep(.datagrid-editable>.f-column) {
    display: flex;
    flex-direction: row;
}
</style>