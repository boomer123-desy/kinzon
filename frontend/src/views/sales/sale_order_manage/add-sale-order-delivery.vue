<template>
    <Loading v-show="isLoading"></Loading>
    <div class="kingzon-page-content">
        <div class="kingzon-page-fill" style="display:flex;flex-direction:row;width:100%; overflow-y: unset;">
            <div
                style="flex:1;width:0;display:flex;flex-direction:column;height:100%;padding-right: 2px;margin-right: 2px;">
                <Form ref="form" :model="saleOrder" labelAlign="right" :labelWidth="70" class="kingzon-page-form"
                    :rules="rules" style="width: 100%;">
                    <FormField name="customerId" label="客户:" style="width: 45%;" :labelWidth="90">
                        <ComboBox v-model="saleOrder.customerId" :data="customerList"
                            @selectionChange="onCustomerChange">
                        </ComboBox>
                    </FormField>
                    <FormField name="receiveUserId" label="接收人:" style="width: 35%;" :labelWidth="90">
                        <ComboBox v-model="saleOrder.receiveUserId" :data="receiverList" textField="userName"
                            valueField="id">
                        </ComboBox>
                    </FormField>
                    <FormField name="customerOrderNo" label="客户订单号:" style="width: 20%;" :labelWidth="90">
                        <TextBox v-model="saleOrder.customerOrderNo" placeholder="客户订单号">
                        </TextBox>
                    </FormField>
                    <FormField name="customerAddress" label="收货地址:" style="width: 45%" :labelWidth="90">
                        <ComboBox :placeholder="noAddress" valueField="index" v-model="saleOrder.customerAddressIndex"
                            @valueChange="onAddressChange($event)" :data="addressList" :limitToList="false">
                        </ComboBox>
                    </FormField>
                    <FormField name="receiveOrderTime" label="接收日期:" style="width: 35%;" :labelWidth="90">
                        <DateBox v-model="saleOrder.receiveOrderTime" format="yyyy-MM-dd"
                            :panelStyle="{ width: '192px', height: '250px !important', maxHeight: '250px !important' }">
                        </DateBox>
                    </FormField>
                    <FormField name="deliveryMode" label="送货方式" style="width: 25%" :labelWidth="90">
                        <ComboBox v-model="saleOrder.deliveryMode" :data="modeSelection">
                        </ComboBox>
                    </FormField>
                    <FormField v-show="saleOrder.deliveryMode == DeliveryModeType.物流" name="logisticsId" label="物流公司"
                        style="width: 25%">
                        <ComboBox v-model="saleOrder.logisticsId" :data="logisticsSelection"
                            @selectionChange="getDefTransitPrice">
                        </ComboBox>
                    </FormField>
                    <FormField v-show="saleOrder.deliveryMode == DeliveryModeType.物流" name="transitPrice" label="运费含税单价"
                        style="width:25%" :labelWidth="90">
                        <NumberBox v-model="saleOrder.transitPrice" :min='0' :precision="2" :spinners="false"
                            :value="defTransitPrice">
                            <Addon class="kingzon-input-addon">
                                <span>{{ '元/Kg' }}</span>
                            </Addon>
                        </NumberBox>
                    </FormField>
                    <FormField v-show="saleOrder.deliveryMode == DeliveryModeType.物流" name="costCategoryId" label="费用类别"
                        style="width: 25%">
                        <ComboBox v-model="saleOrder.costCategoryId" :data="costCategoryList">
                        </ComboBox>
                    </FormField>
                    <!-- 扩展参数 -->
                    <FormField v-for="(para) in customerSaleParaList" :key="para.systemPara.id" :labelWidth="90"
                        :name="para.systemPara.id.toString()" :label="para.systemPara.dataValue + '：'"
                        :style="{ width: para.systemPara.displayType == DisplayType.Textarea ? '100%' : '50%' }">
                        <MutiDisplayTypeInput :para="para" @update:value="para.systemPara.value = $event">
                        </MutiDisplayTypeInput>
                    </FormField>
                    <FormField name="remark" label="订单备注:" style="width: 100%" :labelWidth="90">
                        <TextBox v-model="saleOrder.remark" :multiline="true" style="padding: 5px 0px; height: 50px;">
                        </TextBox>
                    </FormField>
                    <FormField name="file" label="文件：" style="width: 100%" :labelWidth="90">
                        <template #label>
                            <KingZonFileButton @onFileSelect="onFileSelect" style="width: 80px;height: 26px;">文件上传
                            </KingZonFileButton>
                        </template>
                        <!-- 文件列表 -->
                        <FileUploadList v-model:uploadFileList="uploadFileData" style="width: 100%;"></FileUploadList>
                    </FormField>
                </Form>
                <div style="display: flex;flex-direction: row;align-items: center;padding-right: 50px;">
                    <div style="margin: 0 0 10px 10px;">
                        <LinkButton @click="choiceProducts" class="c8">选择产品</LinkButton>
                        <LinkButton @click="clear" style="margin-left: 10px;">清除</LinkButton>
                    </div>
                    <div style="flex:1"></div>
                    <div v-if="saleOrder.deliveryMode == DeliveryModeType.物流" style="display: flex;">
                        <div>总重:{{ getTotalWeight }}kg 单价:{{ getTransitPrice }}元/kg
                        </div>
                        <div style="font-weight: 600;margin-left: 20px;margin-right: 5px;">预估含税运费：{{ getTransitCost
                            }}元
                        </div>
                    </div>
                    <div v-else>
                        <div style="font-weight: 600;margin-left: 20px;margin-right: 5px;">总重:{{ getTotalWeight }}kg
                        </div>
                    </div>
                </div>
                <!-- 销售订单明细 -->
                <KingZonDataGrid ref="saleOrderDetailDataGrid" :data="saleOrder.saleOrderDetails" :pagination="false"
                    :clickToEdit="true" editMode="cell" selectionMode="single" @rowSelect="onSaleOrderDetailRowSelect">
                    <GridColumnGroup :frozen="true" align="left" width="430">
                        <GridHeaderRow>
                            <GridColumn field="index" title="" align="center" width="40">
                                <template #body="scope">
                                    {{ scope.rowIndex + 1 }}
                                </template>
                            </GridColumn>
                            <GridColumn field="product.productNo" title="产品编码" width="90" :overflowTextEllipsis="true">
                                <template #body="scope">
                                    <div
                                        style="display: flex;flex-direction: row;align-items: center;overflow: hidden;">
                                        <span class="kingzon-textEllipsis">{{ scope.row.product?.productNo }}</span>
                                        <span v-if="isRowAllocated(scope.row)" class="allocated-tag"
                                            title="已分配库存">已分配</span>
                                    </div>
                                </template>
                            </GridColumn>
                            <GridColumn field="product.productName" title="产品名称" width="120"
                                :overflowTextEllipsis="true">
                            </GridColumn>
                            <GridColumn field="product.productSpecification" title="规格型号" width="95"
                                :overflowTextEllipsis="true">
                            </GridColumn>
                        </GridHeaderRow>
                    </GridColumnGroup>
                    <GridColumnGroup>
                        <GridHeaderRow>
                            <!-- 外包装方案：可编辑（对应 launch_delivery_order 的包装列） -->
                            <GridColumn field="packageId" title="外包装方案" align="left" width="120" :editable="true"
                                cellCss="kingzon-cell-editable">
                                <template #body="scope">
                                    <span :title="getPackageSpecification(scope.row)" class="kingzon-textEllipsis">{{
                                        getPackageSpecification(scope.row) }}</span>
                                </template>
                                <template #edit="scope">
                                    <ComboBox valueField="id" textField="packageSpecification"
                                        v-model="scope.row.packageId" :data="getPackageList(scope.row)"
                                        :panelStyle="{ minWidth: '150px' }" @valueChange="onPackageChange(scope.row)">
                                    </ComboBox>
                                </template>
                            </GridColumn>
                            <GridColumn field="packageNum" title="外包装数量" align="center" width="90">
                                <template #body="scope">
                                    <div v-if="scope.row.packageNum > 0 && scope.row.packageId > 0">
                                        {{ scope.row.packageNum }}{{ scope.row.packageUnitName }} {{ scope.row.palletNum
                                        }}托盘
                                    </div>
                                </template>
                            </GridColumn>
                            <GridColumn field="totalWeight" title="理论重量(Kg)" align="right" width="90">
                                <template #body="scope">
                                    {{ $utils.formatThousandSeparator(scope.row.totalWeight ?? 0, 4) }}
                                </template>
                            </GridColumn>
                            <GridColumn field="customerMaterialNo" title="客户产品图号" width="100" :editable="true"
                                cellCss="kingzon-cell-editable">
                                <template #body="scope">
                                    <div :title="scope.row.customerMaterialNo" class='kingzon-textEllipsis'>{{
                                        scope.row.customerMaterialNo }}</div>
                                </template>
                                <template #edit="scope">
                                    <TextBox v-model="scope.row.customerMaterialNo"></TextBox>
                                </template>
                            </GridColumn>
                            <GridColumn field="customerMaterialName" title="客户产品名称" width="100" :editable="true"
                                cellCss="kingzon-cell-editable">
                                <template #body="scope">
                                    <div :title="scope.row.customerMaterialName" class='kingzon-textEllipsis'>{{
                                        scope.row.customerMaterialName }}</div>
                                </template>
                                <template #edit="scope">
                                    <TextBox v-model="scope.row.customerMaterialName"></TextBox>
                                </template>
                            </GridColumn>
                            <GridColumn field="deliveryTime" title="客户交期" width="100" :editable="true"
                                cellCss="kingzon-cell-editable">
                                <template #body="scope">
                                    <div v-if="scope.row.deliveryTime">
                                        {{ $utils.formatDateTime(scope.row.deliveryTime, 'yyyy-MM-dd') }}
                                    </div>
                                </template>
                                <template #edit="scope">
                                    <DateBox v-model="scope.row.deliveryTime" format="yyyy-MM-dd"
                                        :panelStyle="{ width: '192px', height: '250px !important', maxHeight: '250px !important' }">
                                    </DateBox>
                                </template>
                            </GridColumn>
                            <GridColumn field="remark" title="订单详情备注" width="100" :editable="true"
                                cellCss="kingzon-cell-editable">
                                <template #body="scope">
                                    <div :title="scope.row.remark" class='kingzon-textEllipsis'>{{
                                        scope.row.remark }}</div>
                                </template>
                                <template #edit="scope">
                                    <TextBox v-model="scope.row.remark"></TextBox>
                                </template>
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
                            <GridColumn field="product.stockQtyByProductMainUnit" title="统一单位库存" align="right"
                                width="100">
                                <template #body="scope">
                                    <div>{{ scope.row.product.stockQtyByProductMainUnit }}{{
                                        scope.row.product.productMainUnit
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
                            <GridColumn field="product.productCategory" title="产品类别" width="100"
                                :overflowTextEllipsis="true">
                            </GridColumn>
                            <GridColumn field="product.productPackageWay" title="包装方式" align="left" width="80"
                                :overflowTextEllipsis="true">
                            </GridColumn>
                            <GridColumn field="product.remark" title="产品备注" width="100" :overflowTextEllipsis="true">
                            </GridColumn>
                            <!-- 扩展参数 -->
                        </GridHeaderRow>
                    </GridColumnGroup>
                    <GridColumnGroup :frozen="true" align="right" width="500">
                        <GridHeaderRow>
                            <GridColumn field="placeOrderQuantity" title="下单数量" align="right" width="190"
                                :editable="true" cellCss="kingzon-cell-editable">
                                <template #body="scope">
                                    <div style="display: flex;flex-direction: row;align-items: flex-start;">
                                        <div
                                            style="width: 46px;flex-shrink: 0;display: flex;flex-direction: column;line-height: 14px;">
                                            <span class="kingzon-pointer" @click.stop="addUnitConversion(scope.row)"
                                                style="color: var(--color-blue);font-size: 10px;white-space: nowrap;">单位转换
                                            </span>
                                        </div>
                                        <span style="flex:1;line-height: 22px;text-align: right;">{{
                                            scope.row.placeOrderQuantity }} {{ scope.row.salesUnit }}</span>
                                    </div>
                                    <!-- 只显示用户添加的换算单位（选择性显示） -->
                                    <div v-if="scope.row.unitConversionList && scope.row.unitConversionList.length > 0"
                                        style="display: flex;flex-direction: row;justify-content: right;flex-wrap: wrap;">
                                        <div style="font-style: italic;color: var(--color-grey);font-size: 12px;margin-left: 5px;"
                                            v-for="unit in scope.row.unitConversionList" :key="unit.id">
                                            {{ unit.placeOrderQuantity }}{{ unit.unitName }}</div>
                                    </div>
                                </template>
                                <template #edit="scope">
                                    <NumberBox :precision="2" v-model="scope.row.placeOrderQuantity"
                                        @valueChange="onPlaceOrderQuantityChange(scope.row)" :spinners="false">
                                    </NumberBox>
                                    <ComboBox valueField="code" v-model="scope.row.salesUnitConversionId"
                                        @valueChange="onUnitChange($event, scope.row)"
                                        :panelStyle="{ minWidth: '100px' }" :data="scope.row.product.unitList">
                                        <template #item="s">
                                            {{ s.row.text }}{{ s.row.extension ? '(' + s.row.extension + ')' : '' }}
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
                                    <NumberBox v-model="scope.row.price" :min="0" :precision="4"
                                        @valueChange="computeTotalPrice(scope.row)" :spinners="false">
                                    </NumberBox>
                                </template>
                            </GridColumn>
                            <GridColumn field="totalPrice" title="总价" width="80" align="right">
                                <template #body="scope">
                                    <span>{{ scope.row.totalPrice?.toFixed(4) }}</span>
                                </template>
                            </GridColumn>
                            <GridColumn field="completed" title="完成" align="center" width="70">
                                <template #header>
                                    <span class="completion-toggle" title="全选完成">
                                        <span class="completion-label">完成</span>
                                        <CheckBox class="completion-checkbox" :modelValue="isAllDetailsCompleted"
                                            @update:modelValue="setAllDetailsCompleted"></CheckBox>
                                    </span>
                                </template>
                                <template #body="scope">
                                    <span :title="isDetailCompleted(scope.row) ? '已完成' : '进行中'">
                                        <CheckBox :modelValue="isDetailCompleted(scope.row)"
                                            @update:modelValue="setDetailCompleted(scope.row, $event)" @click.stop>
                                        </CheckBox>
                                    </span>
                                </template>
                            </GridColumn>
                            <GridColumn field="action" title="操作" align="center" width="110">
                                <template #body="scope">
                                    <LinkButton @click="copyRow(scope.row)">复制</LinkButton>
                                    <LinkButton @click="deleteRow(scope.row)" style="margin-left: 5px;">删除</LinkButton>
                                </template>
                            </GridColumn>
                        </GridHeaderRow>
                    </GridColumnGroup>
                </KingZonDataGrid>
            </div>
            <div>
                <!-- 分配库存的操作页面 -->
                <Panel title="确认送货数量" :collapsible="false" style="height:100%;width:100%;">
                    <InventoryAllocation ref="inventoryAllocation" :key="allocationPanelKey" type="delivery"
                        :deliveryOrderDetail="currentSelectedRow" @save="onAllocateSave" @reset="onAllocateReset">
                    </InventoryAllocation>
                </Panel>
            </div>
        </div>
        <div class="kingzon-btn-group kingzon-page-footer">
            <LinkButton @click="cancel">取消</LinkButton>
            <LinkButton btnCls="c8" @click="save">生成发货单</LinkButton>
        </div>
    </div>
    <Dialog ref="fileUpload" :title="'产品文件上传'" :dialogStyle="{ width: '600px', maxHeight: '600px' }" :draggable="true"
        :resizable="true" :modal="true" :closed="true">
        <FileUpload @close="fileDialogClose" :uploadFileList="uploadFileList" :upload="false"
            @submit="uploadFileSubmit">
        </FileUpload>
    </Dialog>
    <Drawer v-if="showDrawer" @collapse="showDrawer = false;" :drawerStyle="{ width: '95%' }" region="east"
        ref="choiceProductDrawer" :closable="true" :maskClosable="false">
        <template #header>
            <div style="display: flex;justify-content: left;">
                <div>【{{ saleOrder.customer.customerName }}】选择产品</div>
            </div>
        </template>
        <ChoiceProduct ref="choiceProduct" @cancel="choiceCancel" @success="choiceSuccess" :model="saleOrder.customer"
            :originProducts="productList" :defaultCustomer="IsDefaultCustomer(saleOrder.customer)" />
    </Drawer>
    <Dialog ref="uploadFileDialog" title="订单文件上传" :dialogStyle="{ width: '600px', maxHeight: '600px' }"
        :draggable="true" :resizable="true" :modal="true" :closed="true">
        <FileUpload @close="uploadFileDialog.close()" :uploadFileList="uploadFileList" :upload="false"
            @submit="uploadFileSubmit">
        </FileUpload>
    </Dialog>
    <!-- 单位转换（选择性添加换算单位，参照 add-sale-order） -->
    <Dialog ref="addUnitConversionDialog" :closed="true" :modal="true" title="单位转换" :draggable="true" :resizable="true"
        :dialogStyle="{ width: '600px', height: '400px' }">
        <UnitConversion @cancel="addUnitConversionCancel" @success="addUnitConversionSuccess"
            :orderData="unitConversionOrderDetail" />
    </Dialog>
</template>

<script lang="ts">
import { defineComponent } from 'vue';
import { DeliveryOrderDetailListDto, SaleGoodsDto, SaleGoodsUnitDto, SaleOrder, SaleOrderDetail, SaleOrderDetailDlivery, SaleUnitConversionDto, InventoryRecordOccupyDetailDto, SaleOrderDeliveryDto, GetCostCategoryByCostCategoryFeatureDto, GetPackageUnitToTargetUnitRatioDto } from '@/types/sale'
import { ReviewStatus, SaleOrderType, FileStorageSourceType, FileBase, DisplayType, SystemParaType, SaleOrderStatus, DeliveryModeType, CostCategoryFeature, UnitConversionType } from '@/types/enums';
import { cloneDeep, sum } from 'lodash';
import { CustomAttribute, type SelectOption, type UploadFile, type uploadFilesType } from '@/types/types';
import { BaseCustomer, BaseProductPackage, type CustomerAddressDto, type SystemParaTreeDto } from '@/types/common.js';
import InventoryAllocation, { OccupyInfoDto } from '../delivery_order_manage/launch_delivery_order/inventory-allocation.vue';
import ChoiceProduct from './choice-product.vue';
import UnitConversion from './unit-conversion.vue';
import { UnitConversionHelper } from '@/utils/UnitConversionHelper.js';
import Decimal from 'decimal.js';
import type { InventoryUnitShowDto } from './inventory/inventory.js';

export default defineComponent({
    name: 'addSaleOrderDelivery',
    emits: ['cancel', 'success'],
    components: {
        InventoryAllocation,
        ChoiceProduct,
        UnitConversion
    },
    props: [],
    data() {
        return {
            isLoading: false,
            defaultSaleOrder: new SaleOrderDeliveryDto(),
            saleOrder: new SaleOrderDeliveryDto(),
            customerList: [] as SelectOption[],
            addressList: [] as CustomerAddressDto[],
            newAddressId: 0 as number, //  >0时，展示新增的地址，否则展示默认的地址
            customerSaleParaList: [] as SystemParaTreeDto[],//客户-下单扩展参数
            noAddress: '',
            receiverList: [] as any[],
            uploadFileList: [] as UploadFile[],
            uploadFileData: [] as UploadFile[],  //上传文件数据
            uploadFileDialog: {} as EasyUI.Dialog,
            deliveryDetailDataGrid: undefined as EasyUI.DataGrid | undefined,
            addUnitConversionDialog: {} as EasyUI.Dialog,
            unitConversionOrderDetail: {} as SaleOrderDetailDlivery, // 单位转换弹窗当前编辑的行
            currentSelectedRow: undefined as DeliveryOrderDetailListDto | undefined,
            deliveryOrderDetailList: [] as DeliveryOrderDetailListDto[],
            currentSelectedIndex: 0,
            currentSelectedDetail: undefined as SaleOrderDetail | undefined, //当前选中的销售明细
            allocationPanelKey: 0 as number, // 删除/清空后自增，强制重挂载右侧分配面板以重置
            inventoryRecordCache: {} as Record<number, InventoryRecordOccupyDetailDto[]>, // goodsId → 可用库存记录
            allocationMap: new Map() as Map<SaleOrderDetail, DeliveryOrderDetailListDto>, // 销售明细 → 送货明细（保留每行分配状态）
            showDrawer: false,
            choiceProductDrawer: {} as EasyUI.Drawer,
            maximumDeliveryLimitQty: 0 as number, //最大送货量参数
            rules: {
                customerId: 'required',
                receiveUserId: 'required',
                receiveOrderTime: 'required',
                customerAddress: 'required',
            },
            costCategoryList: [] as SelectOption[],
            modeSelection: [] as SelectOption[],
            logisticsSelection: [] as SelectOption[],
            defTransitPrice: 0 as number, //系统获取的默认运费单价
            province: null as string | null,
            city: null as string | null,
            county: null as string | null,
            originLogisticsCompanyId: 0 as number,  // 原单物流公司（编辑场景；本页新增流程为 0）
            originTransitPrice: 0 as number,  // 原单运费单价（编辑场景；本页新增流程为 0）
        }
    },
    computed: {
        DisplayType() {
            return DisplayType
        },
        SaleOrderStatus() {
            return SaleOrderStatus
        },
        DeliveryModeType() {
            return DeliveryModeType
        },
        getSelectedAddress() {
            return this.addressList.find(o => o.index == this.saleOrder.customerAddressIndex)
        },
        getLogisticsId() {
            return this.saleOrder.logisticsId ?? this.getSelectedAddress?.logisticsCompanyId; //如果用户未选择物流公司，则使用客户地址的默认物流公司
        },
        getTransitPrice() {
            return this.saleOrder.transitPrice == null ? this.defTransitPrice : this.saleOrder.transitPrice; //如果用户未填入运费单价，则使用系统获取的默认运费单价(用户可以填0)
        },
        // 明细总重量（用于按重量区间查询默认运费单价；各明细 totalWeight 由 getDeliveryOrderDetailListWeight 维护）
        getTotalWeight() {
            const details = this.saleOrder.saleOrderDetails ?? [];
            return details.reduce((acc, x) => acc.plus((x as SaleOrderDetailDlivery).totalWeight ?? 0), new Decimal(0)).toNumber();
        },
        //预估运费
        getTransitCost() {
            return Decimal.mul(this.getTransitPrice ?? 0, this.getTotalWeight ?? 0).toDecimalPlaces(2).toNumber()
        },
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
        isAllDetailsCompleted(): boolean {
            const details = this.saleOrder.saleOrderDetails ?? [];
            return details.length > 0 && details.every((detail: SaleOrderDetail) =>
                detail.status === SaleOrderStatus.已完成
            );
        },
    },
    created() {
        this.saleOrder.customerId = undefined;
        this.saleOrder.receiveUserId = this.$user.Id;
        let saleOrderDetails = [] as SaleOrderDetailDlivery[];
        this.saleOrder.saleOrderDetails = saleOrderDetails;
    },
    async mounted() {
        this.uploadFileDialog = this.$refs.uploadFileDialog as EasyUI.Dialog
        this.receiverList = (await this.getReceivers()).data as any[]
        this.getCustomers();
        this.customerSaleParaList = (await this.getCustomerSaleParaData()).data as any[]
        this.modeSelection = this.$utils.getEnumKeyValueArray(DeliveryModeType)
        this.getMaximumDeliveryQtySystemPara();
        // 运费相关下拉数据（送货方式/物流公司/费用类别）
        this.getLogisticsList();
        this.getCostCategoryList();

        if (this.saleOrder.customSaleAttributeList && this.saleOrder.customSaleAttributeList.length > 0) {
            this.customerSaleParaList.forEach((para: SystemParaTreeDto) => {
                var value = this.saleOrder.customSaleAttributeList?.find((item: CustomAttribute) => item.systemParaId == para.systemPara.id)?.value;
                if (value) {
                    para.systemPara.value = value as any;
                }
            });
        }
        if (this.saleOrder.customerId) {
            this.getCustomerAddresses();
        }
    },
    methods: {
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
            //地址置空
            this.saleOrder.customerAddressIndex = undefined;
            this.saleOrder.customerAddress = undefined;

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
        isDetailCompleted(row: SaleOrderDetail): boolean {
            return row.status === SaleOrderStatus.已完成;
        },
        setDetailCompleted(row: SaleOrderDetail, completed: boolean): void {
            row.status = completed ? SaleOrderStatus.已完成 : SaleOrderStatus.进行中;
        },
        setAllDetailsCompleted(completed: boolean): void {
            this.saleOrder.saleOrderDetails.forEach((row: SaleOrderDetail) => {
                this.setDetailCompleted(row, completed);
            });
        },
        async getCostCategoryList() {
            var res = await this.$http.get('/api/base/Category/GetCostCategoryByCostCategoryFeature', { feature: CostCategoryFeature.运费 })
            var data = res.data as GetCostCategoryByCostCategoryFeatureDto[]
            this.costCategoryList = data.map(x => ({
                value: x.costCategoryId,
                text: x.name,
            } as SelectOption))
            // 新增送货单默认使用第一个运费费用类别；已存在的值不覆盖。
            if (this.saleOrder.costCategoryId == null && this.costCategoryList.length > 0) {
                this.saleOrder.costCategoryId = Number(this.costCategoryList[0].value)
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
        // 物流公司变化、地址变化、重量变化时重新获取默认运费单价
        // 优先级：地址记录的运费 < Base_LogisticsPrice 按(物流+省市县+重量)查出的运费 < 原单运费(编辑场景)
        async getDefTransitPrice() {
            if (!this.getLogisticsId) {
                this.defTransitPrice = 0
                return
            }
            // 每次重新计算前先清空，避免切换物流/地址后保留上一条默认单价。
            this.defTransitPrice = 0
            const address = this.getSelectedAddress
            this.province = address?.province ?? null
            this.city = address?.city ?? null
            this.county = address?.county ?? null

            var addressPrice = address?.logisticsPrice ?? 0
            var deliveryOrderPrice = this.originTransitPrice ?? 0

            // 重量为 0：不查后端，只考虑地址单价/原单单价
            if (this.getTotalWeight == 0) {
                if (this.saleOrder.logisticsId == this.originLogisticsCompanyId && deliveryOrderPrice) {
                    this.defTransitPrice = deliveryOrderPrice
                    return
                }
                if (this.saleOrder.logisticsId == address?.logisticsCompanyId && addressPrice) {
                    this.defTransitPrice = addressPrice
                    return
                }
                this.defTransitPrice = 0
                return
            }

            this.isLoading = true
            try {
                var logisticsPrice = 0
                var result = await this.$http.post('/api/base/LogisticsPrice/GetLogisticsPriceByLocationAndWeight', {
                    logisticsId: this.getLogisticsId,
                    province: this.province,
                    city: this.city,
                    county: this.county,
                    weight: this.getTotalWeight
                })
                var data = result.data as any
                if (data) {
                    logisticsPrice = (data.priceHaveTax as number) || 0
                }
                // 原单默认运费 > 物流单价表单价 > 地址单价
                if (deliveryOrderPrice && this.saleOrder.logisticsId == this.originLogisticsCompanyId) {
                    this.defTransitPrice = deliveryOrderPrice
                } else if (logisticsPrice) {
                    this.defTransitPrice = logisticsPrice
                } else if (addressPrice && this.saleOrder.logisticsId == address?.logisticsCompanyId) {
                    this.defTransitPrice = addressPrice
                } else {
                    this.defTransitPrice = 0
                }
            } catch (error) {
                this.$kingzon_messager.showError(error)
            } finally {
                this.isLoading = false
            }
        },

        // ============ 外包装方案相关（自 launch_delivery_order.vue 移植） ============
        // 外包装方案下拉数据（首项为"无外包装方案"，后接该行产品的包装方案列表）
        getPackageList(row: SaleOrderDetailDlivery): any[] {
            return [
                { packageSpecification: '无外包装方案', id: 0 },
                ...(row.product?.packageList ?? [])
            ]
        },
        // 当前行选中的外包装方案说明（未选/无时显示"无外包装方案"）
        getPackageSpecification(row: SaleOrderDetailDlivery): string {
            if (!row.packageId || row.packageId <= 0) {
                return '无外包装方案'
            }
            return this.getPackageList(row).find(o => o.id == row.packageId)?.packageSpecification ?? '无外包装方案'
        },
        // 换包装方案：刷新说明 + 重量 + 默认运费单价
        async onPackageChange(row: SaleOrderDetailDlivery) {
            row.packageSpecification = this.getPackageSpecification(row)
            await this.getDeliveryOrderDetailListWeight(row)
            await this.getDefTransitPrice()
        },
        // 1 个包装单位 = ratio 个目标单位（后端换算）
        async getPackageUnitToDeliveryOrderUnitRatio(postData): Promise<GetPackageUnitToTargetUnitRatioDto | undefined> {
            try {
                var res = await this.$http.post('/api/sale/SaleOrderManage/GetPackageUnitToTargetUnitRatio', postData)
                return res.data as GetPackageUnitToTargetUnitRatioDto
            } catch (error) {
                this.$kingzon_messager.showInfo(error)
                return undefined
            }
        },
        // 重新计算每行 外包装数量/托盘数/理论重量，并按新总重刷新默认运费单价
        async refreshDeliveryWeight() {
            await this.getDeliveryOrderDetailListWeight()
            await this.getDefTransitPrice()
        },
        // 逐行计算：外包装数量(箱/包)、托盘数、理论重量（送货量按直发数量，未填则按下单数量）
        async getDeliveryOrderDetailListWeight(row: SaleOrderDetailDlivery | undefined = undefined) {
            this.isLoading = true
            try {
                for (const x of this.saleOrder.saleOrderDetails as SaleOrderDetailDlivery[]) {
                    if (row && row !== x) {
                        continue
                    }
                    x.totalWeight = 0
                    x.packageNum = 0
                    x.palletNum = 0
                    x.packageUnitName = ''
                    const packageId = x.packageId ?? 0
                    const qty = x.planDirectDliveryQuantity > 0 ? x.planDirectDliveryQuantity : (x.placeOrderQuantity ?? 0)
                    // 未填送货量时无法计算重量。
                    if (!qty) {
                        continue
                    }
                    // 未选包装方案时，按销售单位换算为 Kg 估算重量。
                    if (!packageId) {
                        this.getWeightWithNoPackage(x, qty)
                        continue
                    }
                    const packageInfo = this.getPackageList(x).find(o => o.id == packageId) as any
                    if (!packageInfo) {
                        continue
                    }
                    const packageUnitId = packageInfo.packageUnitId
                    const packageWeight = packageInfo.packageWegiht ?? 0
                    const palletPackageRatio = packageInfo.palletPackageRatio ?? 0 // 每托盘可放几箱/包
                    const palletWeight = packageInfo.palletWegiht ?? 0
                    const mainUnitId = x.product?.productUnitId // 产品主单位
                    if (!packageUnitId || !mainUnitId) {
                        continue
                    }
                    const postData = new GetPackageUnitToTargetUnitRatioDto({
                        packageId: packageId,
                        targetId: x.product?.specificationId ? x.product.specificationId : x.product?.productId,
                        unitConversionType: x.product?.specificationId ? UnitConversionType.PackageToProductSpec : UnitConversionType.PackageToProduct,
                        packageUnitId: packageUnitId,
                        targetUnitId: mainUnitId
                    })
                    const data = await this.getPackageUnitToDeliveryOrderUnitRatio(postData) as GetPackageUnitToTargetUnitRatioDto
                    if (!data) {
                        x.packageNum = 0
                        x.packageUnitName = ''
                        x.palletNum = 0
                        continue
                    }
                    // 后端返回“包装单位 -> 主单位”比例；送货量是销售单位，需折算后再计算包装数。
                    const salesUnitRatio = Number(x.product?.unitList?.find(unit => unit.code == x.salesUnitConversionId)?.value) || 1
                    const ratio = new Decimal(data.ratio).div(salesUnitRatio)
                    if (!ratio.greaterThan(0)) {
                        continue
                    }
                    x.packageUnitName = data.packageUnitName
                    const packageNum = new Decimal(qty).div(ratio).ceil().toNumber() // 多少箱/包，向上取整
                    x.packageNum = packageNum
                    const totalPackageWeight = new Decimal(packageNum).times(packageWeight).toDecimalPlaces(4).toNumber()
                    let totalPalletWeight = 0
                    if (palletPackageRatio > 0) {
                        const palletNum = new Decimal(packageNum).div(palletPackageRatio).ceil().toNumber() // 多少托盘，向上取整
                        x.palletNum = palletNum
                        totalPalletWeight = new Decimal(palletNum).times(palletWeight).toDecimalPlaces(4).toNumber()
                    }
                    x.totalWeight = Decimal.add(totalPackageWeight, totalPalletWeight).toNumber()
                }
            } finally {
                this.isLoading = false
            }
        },
        // 未选包装方案时，将销售单位数量换算为 Kg；找不到 Kg 单位则不参与运费重量汇总。
        getWeightWithNoPackage(row: SaleOrderDetailDlivery, quantity: number) {
            const unitList = row.product?.unitList ?? []
            const kgUnit = unitList.find(unit => unit.text?.toLowerCase() == 'kg')
            const salesUnit = unitList.find(unit => unit.code == row.salesUnitConversionId)
            const kgUnitRatio = Number(kgUnit?.value)
            const salesUnitRatio = Number(salesUnit?.value)
            if (!kgUnit || !Number.isFinite(kgUnitRatio) || kgUnitRatio <= 0 || !Number.isFinite(salesUnitRatio) || salesUnitRatio <= 0) {
                return
            }
            row.totalWeight = new Decimal(quantity).times(salesUnitRatio).div(kgUnitRatio).toDecimalPlaces(4).toNumber()
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
                    if (this.newAddressId) {
                        var address1 = this.addressList.find((item: CustomerAddressDto) => item.index == this.newAddressId)
                        this.saleOrder.customerAddressIndex = address1?.index;
                        this.saleOrder.customerAddress = address1;
                    }
                });
        },
        getReceivers(): Promise<HttpResponseType> {
            return this.$http.get('api/base/User/GetAllUser');
        },
        getCustomerSaleParaData() {
            return this.$http.get('/api/base/SystemPara/GetAdditionalParam', { type: SystemParaType.客户下单附加参数 });
        },
        getCustomers() {
            //业务员名下的客户 以及 未分配业务员的客户
            return this.$http.get('api/base/Customer/GetSalesManAllCustomer', {
                includeMain: false
            }).then((res: any) => {
                this.customerList = res.data?.map((item: BaseCustomer) => {
                    return { value: item.id, text: item.customerCode + '-' + item.customerName, code: item.customerNo, extension: JSON.parse(item.customSaleAttribute ?? "[]") }
                });
            });
        },
        async onAddressChange({ currentValue, previousValue }) {
            if (!currentValue) {
                this.saleOrder.customerAddressIndex = undefined;
                this.saleOrder.customerAddress = undefined;
                await this.getDefTransitPrice()
                return
            };
            var address = this.addressList.find(x => x.index == currentValue);
            this.saleOrder.customerAddress = address;
            // 物流模式下：未手动选物流公司时，默认采用地址的物流公司（便于按地址带出默认运费单价）
            if (this.saleOrder.deliveryMode == DeliveryModeType.物流 && address?.logisticsCompanyId && !this.saleOrder.logisticsId) {
                this.saleOrder.logisticsId = address.logisticsCompanyId
            }
            await this.getDefTransitPrice()
        },
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
        //初始化送货明细列表
        initDeliveryOrderDetailList(rows: DeliveryOrderDetailListDto[]): void {
            rows.forEach((item) => {
                //如果送货单没有地址  
                if (!this.saleOrder.customerAddressIndex && item.saleCustomerAddress) {
                    this.saleOrder.customerAddressIndex = item.saleCustomerAddress.index
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
        onAllocateSave(value: OccupyInfoDto) {
            if (!this.currentSelectedRow) {
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

            //同步本次送货量到销售订单明细的直发数量（用于直接生成发货记录）
            var saleOrderDetail = this.currentSelectedDetail || this.saleOrder.saleOrderDetails.find(x =>
                x.product?.productId == this.currentSelectedRow?.productId && x.product?.specificationId == this.currentSelectedRow?.specificationId);
            if (saleOrderDetail && value.occupyInputQtyBySalesUnit > 0) {
                saleOrderDetail.planDirectDliveryQuantity = value.occupyInputQtyBySalesUnit;
                this.computeTotalPrice(saleOrderDetail);
            }
            //写回分配状态（面板数据来自 clone，需回存以便切换行/保存时读取）
            if (this.currentSelectedDetail) {
                this.allocationMap.set(this.currentSelectedDetail, this.currentSelectedRow);
            }
            //送货量变了，重新计算包装/重量并刷新默认运费单价
            this.refreshDeliveryWeight()
            this.$kingzon_messager.showInfo('库存分配已保存');
        },
        onAllocateReset() {
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
            //需要改掉其它明细中，相应库存记录的总的占用量
            this._changeOtherOriginOccupyQuantity()
            //写回分配状态
            if (this.currentSelectedDetail) {
                this.allocationMap.set(this.currentSelectedDetail, this.currentSelectedRow);
            }
            //重置后送货量变了，重新计算包装/重量并刷新默认运费单价
            this.refreshDeliveryWeight()
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
        getOrderInventory(row: DeliveryOrderDetailListDto): number {
            return Decimal.add(row.planDirectDliveryQuantity || 0, row.totalInputProductionQty || 0).add(row.purchaseInventoryQuantity || 0).add(row.productionInventoryQuantity || 0).toNumber();
        },
        getOrderInventoryRemain(row: DeliveryOrderDetailListDto): number {
            return Decimal.sub(this.getOrderInventory(row), row.actualDeliveryQuantity || 0).sub(row.todoQuantity || 0).toNumber();
        },
        //获取最大送货数量限制的附加参数
        async getMaximumDeliveryQtySystemPara() {
            const result = await this.$http.get('/api/base/SystemPara/GetAdditionalParam', {
                type: SystemParaType.最大送货量参数
            })
            var list = result.data as SystemParaTreeDto[]
            this.maximumDeliveryLimitQty = Number(list[0]?.systemPara.dataValue ?? 0)
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
            this.$kingzon_messager.showConfim('确认清除所有数据吗').then(() => {
                // 已完成的订单详情不能清除
                this.saleOrder.saleOrderDetails = [];
                // 清空选中明细并重置右侧分配面板
                this.resetAllocationSelection()
                this.refreshDeliveryWeight()
            })
        },
        choiceCancel() {
            this.choiceProductDrawer.collapse();
        },
        IsDefaultCustomer(customer: BaseCustomer) {
            if (customer.customerNo == "DEFAULT") {
                return true;
            }
            return false;
        },
        // 选择产品页面内点击确定时, 选中的产品
        async choiceSuccess(products: SaleGoodsDto[]) {
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
                            let saleOrderDetail = new SaleOrderDetailDlivery();
                            saleOrderDetail.goodsId = item.goodsId;
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
                            saleOrderDetail.unitConversionList = []
                            // 参照 add-sale-order：默认只建“主单位”换算，其余单位通过“单位转换”弹窗按需添加（选择性显示）
                            this.ensureMainUnitConversion(saleOrderDetail)
                            this.saleOrder.saleOrderDetails.push(saleOrderDetail);
                        }
                    });
                    return this.loadInventoryRecords(this.saleOrder.saleOrderDetails);
                })
                .then(() => {
                    this.isLoading = false
                })
                .catch((err: any) => {
                    this.$kingzon_messager.showError(err.length > 0 ? err : '获取数据失败');
                }).finally(() => {
                    this.isLoading = false;
                });

            // 计算订单价格
            this.computeTotalPrice();
            // 新行默认带了外包装方案，按默认送货量算一次包装/重量（并刷新默认运费单价）
            this.refreshDeliveryWeight()


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
        onUnitChange({ currentValue, previousValue }, row: SaleOrderDetailDlivery) {
            if (!currentValue) {
                row.salesUnit = undefined;
                return
            };
            var uData = row.product.unitList?.find(x => x.code == currentValue);
            row.salesUnitId = uData?.unitId;
            row.salesUnit = uData?.text;
            //先按新销售单位补/更新“主单位”换算
            this.unitConversion(currentValue, -1, row);
            //移除旧的销售单位换算条目（保留用户添加的其它单位，稍后按新销售单位重算）
            row.unitConversionList = row.unitConversionList.filter(x => x.id != row.salesUnitConversionId);
            row.salesUnitConversionId = currentValue
            //按新销售单位重算所有换算数量
            row.unitConversionList.forEach(item => {
                this.unitConversion(row.salesUnitConversionId, item.id, row, item);
            })
            this.ensureMainUnitConversion(row);
            this.computeTotalPrice(row);
            if (this.currentSelectedRow
                && this.currentSelectedRow.saleOrderDetailId == row.id) {
                this.currentSelectedRow.inputUnitConversionId = currentValue;
                this.$nextTick(() => {
                    setTimeout(() => {
                        (this.$refs.inventoryAllocation as any)?.reAllocateByDeliveryQuantity?.();
                        (this.$refs.saleOrderDetailDataGrid as any).resizeDataGrid()
                    }, 0);
                });
            }
            // 单位变了，重新计算包装/重量并刷新默认运费单价
            this.refreshDeliveryWeight()
        },
        //下单数量变化（直接在 NumberBox 上按销售单位录入）：同步送货量并刷新价格/换算/运费
        onPlaceOrderQuantityChange(row: SaleOrderDetailDlivery) {
            this.computeTotalPrice(row);
            if (this.currentSelectedDetail === row && this.currentSelectedRow) {
                this.currentSelectedRow.inputUnitQuantity = row.placeOrderQuantity
                this.$nextTick(() => {
                    setTimeout(() => {
                        (this.$refs.inventoryAllocation as any)?.reAllocateByDeliveryQuantity?.();
                        (this.$refs.saleOrderDetailDataGrid as any).resizeDataGrid()
                    }, 0);
                });
            }
            // 下单数量变了，重新计算包装/重量并刷新默认运费单价
            this.refreshDeliveryWeight()
        },
        // 计算详情总价 和 订单总价
        computeTotalPrice(row?: SaleOrderDetail) {
            if (row) {
                row.totalPrice = Decimal.mul((row.price ?? 0), (row.placeOrderQuantity ?? 0)).toNumber();
                row.planProductionQuantity = (row.placeOrderQuantity - row.planPurchaseQuantity - row.planDirectDliveryQuantity);
                row.unitConversionList.forEach(item => {
                    item.placeOrderQuantity = Decimal.mul(row.placeOrderQuantity, item.ratio).toDecimalPlaces(4).toNumber();
                })
            }
            this.saleOrder.orderTotalPrice = sum(this.saleOrder.saleOrderDetails?.map((item: any) => item.totalPrice)) ?? 0;
        },
        // 保证主单位换算始终存在（配送/汇总要用到），参照 add-sale-order.unitConversion
        ensureMainUnitConversion(row: SaleOrderDetailDlivery) {
            if (row.unitConversionList.some(x => x.id == -1)) {
                return
            }
            if (row.salesUnitConversionId == -1) {
                var main = row.product.unitList?.find(x => x.code == -1)
                if (main) {
                    row.unitConversionList.push({
                        id: -1,
                        unitId: main.unitId,
                        unitName: main.text,
                        ratio: 1,
                        placeOrderQuantity: row.placeOrderQuantity ?? 0,
                        packageId: main.packageId,
                        isBaseUnit: true,
                    } as SaleUnitConversionDto)
                }
            } else {
                this.unitConversion(row.salesUnitConversionId, -1, row)
            }
        },
        // 新增/更新一行单位换算（照 add-sale-order.unitConversion）；from=销售单位，to=换算目标单位
        unitConversion(fromUnitConversionId, toUnitConversionId, row: SaleOrderDetailDlivery, existConversion?: SaleUnitConversionDto | undefined) {
            if (fromUnitConversionId == toUnitConversionId) {
                return;
            }
            if (!existConversion) {
                existConversion = row.unitConversionList.find(x => x.id == toUnitConversionId);
            }
            var fromUnit = row.product.unitList?.find(x => x.code == fromUnitConversionId);
            var toUnit = row.product.unitList?.find(x => x.code == toUnitConversionId);
            if (fromUnit && toUnit) {
                var ratio = 0;
                if (toUnit.code == -1) { //目标为主单位：1 个销售单位 = fromUnit.value 个主单位
                    ratio = Number(fromUnit.value) ?? 0;
                } else { //目标非主单位
                    var toUnitRatio = Number(toUnit.value) ?? 0;
                    var fromRatio = Number(fromUnit.value) ?? 0;
                    ratio = toUnitRatio ? (fromRatio / toUnitRatio) : 0;
                }
                if (existConversion) {
                    existConversion.ratio = ratio;
                    existConversion.placeOrderQuantity = Decimal.mul(ratio, row.placeOrderQuantity ?? 0).toDecimalPlaces(4).toNumber();
                } else {
                    var conversion = {
                        id: Number(toUnitConversionId),
                        unitId: toUnit.unitId,
                        unitName: toUnit.text,
                        ratio: ratio,
                        placeOrderQuantity: Decimal.mul(ratio, row.placeOrderQuantity ?? 0).toDecimalPlaces(4).toNumber(),
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
        // 打开“单位转换”弹窗（选择性添加其它换算单位）
        addUnitConversion(row: SaleOrderDetailDlivery) {
            this.unitConversionOrderDetail = row;
            this.addUnitConversionDialog = this.$refs.addUnitConversionDialog as EasyUI.Dialog;
            this.addUnitConversionDialog.open();
        },
        addUnitConversionSuccess(unitConversionList: SaleUnitConversionDto[]) {
            this.unitConversionOrderDetail.unitConversionList = unitConversionList;
            // 保存后保证主单位换算在列，并刷新换算数量/价格/重量
            this.ensureMainUnitConversion(this.unitConversionOrderDetail);
            this.computeTotalPrice(this.unitConversionOrderDetail);
            this.refreshDeliveryWeight();
            this.addUnitConversionDialog.close();
        },
        addUnitConversionCancel() {
            this.addUnitConversionDialog.close();
        },
        // 清除当前选中明细，并强制重挂载右侧分配面板以重置其内部状态
        resetAllocationSelection(): void {
            this.currentSelectedRow = undefined
            this.currentSelectedDetail = undefined
            this.currentSelectedIndex = -1
            this.allocationPanelKey++
        },
        // 复制一行（克隆插入到原行之后），复制行视为新行：清空直发/送货数量与包装重量，需重新分配
        copyRow(row: SaleOrderDetailDlivery) {
            if (!row) {
                return
            }
            var index = this.saleOrder.saleOrderDetails.findIndex(x => x === row)
            if (index < 0) {
                return
            }
            var copy = cloneDeep(row) as SaleOrderDetailDlivery
            copy.id = 0
            copy.reviewStatus = ReviewStatus.待提交
            copy.status = SaleOrderStatus.待下单
            copy.selected = false
            // 新复制行不沿用原行的直发/包装重量状态（避免误重复送货），保留产品、下单数量、单位、单价、包装方案等
            copy.planDirectDliveryQuantity = 0
            copy.planProductionQuantity = row.placeOrderQuantity ?? 0
            copy.totalWeight = 0
            copy.packageNum = 0
            copy.palletNum = 0
            copy.packageUnitName = ''

            this.saleOrder.saleOrderDetails.splice(index + 1, 0, copy)
            this.computeTotalPrice()
            // 自动选中复制行，方便直接在右侧面板分配库存
            this.selectForDelivery(copy)
            this.refreshDeliveryWeight()
        },
        deleteRow(row: SaleOrderDetail) {
            this.$kingzon_messager.showConfim('确认删除选中数据吗？').then(() => {
                this.saleOrder.saleOrderDetails = this.saleOrder.saleOrderDetails.filter((item: SaleOrderDetail) => item !== row);
                this.saleOrder.orderTotalPrice = sum(this.saleOrder.saleOrderDetails?.map((item: any) => item.totalPrice)) ?? 0;
                // 删除的是当前选中行：有剩余则选中同位置下一条，否则清空选中并重置右侧分配面板
                if (row.goodsId == this.currentSelectedRow?.goodsId) {
                    if (this.saleOrder.saleOrderDetails.length > 0) {
                        var idx = Math.min(this.currentSelectedIndex, this.saleOrder.saleOrderDetails.length - 1)
                        this.selectForDelivery(this.saleOrder.saleOrderDetails[Math.max(0, idx)])
                    } else {
                        this.resetAllocationSelection()
                    }
                }
                // 删除行后重新汇总重量并刷新默认运费单价
                this.refreshDeliveryWeight()
            })
        },
        //批量获取选中产品的可用库存（按 goodsId 分组缓存），然后选中第一条明细
        async loadInventoryRecords(details: SaleOrderDetail[]) {
            var goodsIds = details
                .filter(x => x.goodsId)
                .map(x => x.goodsId)
                .filter((v, i, a) => a.indexOf(v) == i);
            if (goodsIds.length > 0) {
                this.isLoading = true;
                try {
                    const result = await this.$http.post('/api/inventory/DeliveryOrder/GetInventoryRecordByGoodsIds', goodsIds);
                    var data = result.data as any[];
                    data.forEach(item => {
                        var goodsId = item.goodsId;
                        this.inventoryRecordCache[goodsId] = item.occupyDetailDtos ?? [];
                    });
                } catch (error) {
                    this.$kingzon_messager.showError(error);
                } finally {
                    this.isLoading = false;
                }
            }
            if (details.length > 0) {
                this.selectForDelivery(details[0]);
            }
        },
        //选择销售订单明细后，在右侧填充送货/库存信息
        selectForDelivery(row: SaleOrderDetail) {
            if (!row) {
                return;
            }
            //复用已组装过的送货明细（保留库存分配状态），否则重新组装并填充可用库存
            var base = this.allocationMap.get(row);
            if (!base) {
                base = this.buildDeliveryDetailFromSaleOrderDetail(row);
                if (row.goodsId && this.inventoryRecordCache[row.goodsId]) {
                    base.inventoryRecordList = this.inventoryRecordCache[row.goodsId];
                    base.isAllocate = base.inventoryRecordList.length > 0;
                    base.backupInfo.inventoryRecordList = cloneDeep(base.inventoryRecordList
                        .filter(record => (record.occupyInputQuantity ?? 0) > 0)
                        .map(record => Object.assign({}, record)));
                }
                this.allocationMap.set(row, base);
            }
            base.inputUnitQuantity = row.placeOrderQuantity
            base.inputUnitConversionId = row.salesUnitConversionId ?? -1
            //每次选中都生成新引用，确保右侧面板深监听刷新（重复点击同一行/已选中行也能刷新）
            this.currentSelectedRow = cloneDeep(base);
            this.currentSelectedDetail = row;
            this.currentSelectedIndex = this.saleOrder.saleOrderDetails.findIndex(x => x === row);

            if (base.inputUnitQuantity != 0) {
                this.$nextTick(() => {
                    setTimeout(() => {
                        (this.$refs.inventoryAllocation as any)?.reAllocateByDeliveryQuantity?.();
                    }, 0);
                });
            }
        },
        onSaleOrderDetailRowSelect(row: SaleOrderDetail) {
            this.selectForDelivery(row);
        },
        //判断该行是否已分配过库存（用于"已分配"标记）
        isRowAllocated(row: SaleOrderDetail): boolean {
            var info = this.allocationMap.get(row);
            return !!info && (info.inventoryRecordList ?? []).some(record => (record.occupyInputQuantity ?? 0) > 0);
        },
        //销售订单明细 → 送货明细（右侧库存分配面板用）
        buildDeliveryDetailFromSaleOrderDetail(detail: SaleOrderDetail): DeliveryOrderDetailListDto {
            var item = new DeliveryOrderDetailListDto();
            item.saleOrderDetailId = detail.id;
            item.saleOrderNo = this.saleOrder.orderNo;
            item.goodsId = detail.goodsId;
            item.productId = detail.product?.productId;
            item.specificationId = detail.product?.specificationId;
            item.productNo = detail.product?.productNo;
            item.productName = detail.product?.productName;
            item.specification = detail.product?.productSpecification;
            item.technicalCode = detail.product?.productTechnicalCode;
            item.placeOrderQuantity = detail.placeOrderQuantity;
            item.inputUnitQuantity = detail.placeOrderQuantity;
            item.salesUnit = detail.salesUnit;
            item.salesUnitId = detail.salesUnitId ?? 0;
            item.salesUnitConversionId = detail.salesUnitConversionId;
            item.saleToBaseUnitRatio = Number(detail.product?.unitList?.find(x => x.code == detail.salesUnitConversionId)?.value) || 1;
            item.unitConversionList = detail.product?.unitList?.map(u => {
                return {
                    id: u.code,
                    unitId: u.unitId,
                    unitName: u.text,
                    ratio: u.value,
                    packageId: detail.packageId,
                } as SaleUnitConversionDto
            }) ?? [];
            //单位信息（含主单位）
            item.unitInfo = detail.unitConversionList.map(u => {
                return {
                    id: u.id,
                    unitId: u.unitId,
                    unitName: u.unitName,
                    ratio: u.ratio,
                    remark: u.remark,
                } as InventoryUnitShowDto;
            });
            item.inputUnitConversionId = detail.salesUnitConversionId ?? 0;
            item.occupyInputQtyByMainUnit = item.inputUnitQuantity;
            item.occupyInputQtyBySalesUnit = item.inputUnitQuantity;
            item.isAllocate = false;
            item.isAlreadyOutbound = item.inputUnitQuantity > 0;
            //新订单产品暂无具体库存记录，留空由后端在生成发货时分配
            item.inventoryRecordList = [] as InventoryRecordOccupyDetailDto[];
            //备份初始数据（右侧面板"重置"时恢复）
            item.backupInfo = {
                inputUnitConversionId: item.inputUnitConversionId,
                inputUnitQuantity: item.inputUnitQuantity,
                isAllocate: item.isAllocate,
                isAlreadyOutbound: item.isAlreadyOutbound,
                inventoryRecordList: []
            };
            return item;
        },
        //取消按钮
        cancel(): void {
            this.$emit('cancel')
            this.clearData()
        },
        save(): void {
            this.saleOrder.reviewStatus = ReviewStatus.评审通过;
            this.saleOrder.saleOrderDetails.forEach((t: SaleOrderDetail) => {
                t.reviewStatus = ReviewStatus.评审通过;
            })
            var check = this.checkData();
            if (check) {
                this.postData(this.saleOrder);
            }
        },
        checkData() {
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
            data.customSaleAttributeList = this.getCustomeSaleAttributeList(); //客户下单扩展参数
            var uploadFileData = this.uploadFileData;
            this.isLoading = true;
            //组装送货单信息：送货明细与销售明细按顺序一一对应，由后端先生成销售单再生成送货单
            var deliveryDetails = data.saleOrderDetails.map((detail: SaleOrderDetailDlivery) => {
                //右侧面板已组装过的送货明细（含库存分配状态），没有则按下单数量/销售单位组装
                var deliveryInfo = this.allocationMap.get(detail);
                //本次送货量：销售单位 = 直发数量（右侧面板会同步到这里），没有直发则按下单数量
                var deliveryQtyBySale = detail.planDirectDliveryQuantity > 0 ? detail.planDirectDliveryQuantity : (detail.placeOrderQuantity ?? 0);
                var inputUnitConversionId = deliveryInfo?.inputUnitConversionId != null && deliveryInfo.inputUnitConversionId != 0
                    ? deliveryInfo.inputUnitConversionId : (detail.salesUnitConversionId ?? -1);
                var inputUnitQuantity = deliveryInfo && deliveryInfo.inputUnitQuantity > 0 ? deliveryInfo.inputUnitQuantity : deliveryQtyBySale;
                //单位信息（含主单位），用于换算主单位送货量
                var unitConversionInfo = detail.unitConversionList.map(u => {
                    return {
                        id: u.id,
                        unitId: u.unitId,
                        unitName: u.unitName,
                        ratio: u.ratio,
                        remark: u.remark,
                    } as InventoryUnitShowDto;
                });
                var helper = new UnitConversionHelper(inputUnitConversionId, inputUnitQuantity, unitConversionInfo);
                //库存占用明细（仅 指定库存 的明细，后端据此锁定库存）
                var inventoryRecordList = (deliveryInfo?.inventoryRecordList ?? [])
                    .filter(record => (record.occupyInputQuantity ?? 0) > 0)
                    .map(record => {
                        var occupyHelper = new UnitConversionHelper(record.occupyInputUnitConversionId ?? 0, record.occupyInputQuantity ?? 0, record.inventoryUnitsInfoList);
                        return {
                            inventoryRecordId: record.inventoryRecordId,
                            positionId: record.positionId,
                            occupyQuantity: record.deliveryDetailOccupyQuantity, //主单位
                            inputQuantity: record.occupyInputQuantity, //选中单位
                            inputUnitConversionId: record.occupyInputUnitConversionId,
                            unitInfo: occupyHelper.getAllUnitWithQuantity().map((x) => {
                                var unit = x.unit;
                                return {
                                    id: unit.id,
                                    unitId: unit.unitId,
                                    unitName: unit.unitName,
                                    ratio: unit.ratio,
                                    remark: unit.remark,
                                    conversionType: unit.conversionType,
                                    outboundQuantity: x.quantity,
                                };
                            })
                        };
                    });
                return {
                    productId: detail.product?.productId,
                    specificationId: detail.product?.specificationId,
                    goodsId: detail.goodsId,
                    packageId: detail.packageId,
                    palletNum: detail.palletNum,
                    deliveryQuantity: helper.getMainUnitWithQuantity()?.quantity ?? inputUnitQuantity, //主单位
                    inputUnitQuantity: inputUnitQuantity, //用户选中单位（默认=销售单位）
                    inputUnitConversionId: inputUnitConversionId,
                    unitInfo: helper.getAllUnitWithQuantity().map((x) => {
                        return {
                            id: x.unit.id,
                            unitId: x.unit.unitId,
                            unitName: x.unit.unitName,
                            ratio: x.unit.ratio,
                            remark: x.unit.remark,
                            conversionType: x.unit.conversionType,
                            outboundQuantity: x.quantity,
                        };
                    }),
                    isAllocate: deliveryInfo?.isAllocate ?? false,
                    inventoryRecordList: inventoryRecordList
                };
            });
            //仅客户订单生成送货单（备货订单不生成）
            var isGenerateDelivery = data.orderType == SaleOrderType.客户订单;
            var saveData = {
                Order: data,
                Delivery: isGenerateDelivery ? {
                    customerAddress: this.saleOrder.customerAddress,
                    estimatedDeliveryTime: this.saleOrder.receiveOrderTime ? this.$utils.formatDateTime(this.saleOrder.receiveOrderTime, 'yyyy-MM-dd') : undefined,
                    isDirectOutbound: false,
                    isInventoryTransfer: false,
                    transitCost: this.getTransitCost,
                    totalWeight: this.getTotalWeight,
                    transitPrice: this.getTransitPrice,
                    deliveryMode: Number(this.saleOrder.deliveryMode),
                    logisticsId: this.saleOrder.logisticsId,
                    costCategoryId: this.saleOrder.costCategoryId,
                    remark: this.saleOrder.remark ?? '',
                    customAttributeList: [],
                    details: deliveryDetails
                } : null
            };
            this.$http.post('api/sale/SaleOrderManage/AddOrderAndCreateDelivery', saveData)
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
                    this.$kingzon_messager.showError(err);
                }).finally(() => {
                    if (uploadFileData.length <= 0) { //不上传文件
                        this.isLoading = false;
                    }
                });
        },
        //客户下单扩展参数
        getCustomeSaleAttributeList() {
            return this.customerSaleParaList.map(x => {
                return { systemParaId: x.systemPara.id, value: x.systemPara.value }
            })
        },
        //清空数据
        clearData(): void {
            this.saleOrder = { ...this.defaultSaleOrder }
            this.saleOrder.customerId = undefined;
            this.saleOrder.receiveUserId = this.$user.Id;
            this.saleOrder.saleOrderDetails = [] as SaleOrderDetail[];
            //清空发货记录相关数据
            this.deliveryOrderDetailList = [];
            this.currentSelectedRow = undefined;
            this.currentSelectedDetail = undefined;
            this.currentSelectedIndex = -1;
            this.allocationMap = new Map();
            this.allocationPanelKey++; // 强制重挂载右侧分配面板，避免残留上一次的内部状态
            this.inventoryRecordCache = {};
            this.uploadFileData = [];
        }
    }
})
</script>
<style scoped>
:deep(.datagrid-editable>.f-column) {
    display: flex;
    flex-direction: row;
}

/* 已分配标记 */
.allocated-tag {
    margin-left: 4px;
    flex-shrink: 0;
    padding: 0 4px;
    line-height: 14px;
    font-size: 12px;
    color: #fff;
    background-color: #52c41a;
    border-radius: 2px;
}

/* 单位信息容器：右对齐展示 */
.unit-quantity-cell {
    display: flex;
    flex-direction: column;
    align-items: flex-end;
}

.completion-toggle {
    display: inline-flex;
    align-items: center;
    justify-content: center;
    min-width: 32px;
    min-height: 24px;
    cursor: pointer;
}

.completion-label {
    white-space: nowrap;
}

.completion-toggle :deep(.completion-checkbox) {
    display: none;
}

.completion-toggle:hover .completion-label {
    display: none;
}

.completion-toggle:hover :deep(.completion-checkbox) {
    display: inline-flex;
}
</style>
