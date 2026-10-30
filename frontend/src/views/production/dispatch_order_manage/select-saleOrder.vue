<template>
    <Loading v-show="isLoading"></Loading>
    <div class="kingzon-page-content">
        <div class="kingzon-page-fill" style="display: flex; flex-direction: column; overflow-y: unset;">
            <SearchPanel :searchParams="searchParams" :searchParamsHidden="searchParamsHidden" @search="search"
                style="margin: 5px;">
            </SearchPanel>
            <KingZonDataGrid id="saleGrid" class="kingzon-page-fill" ref="dataGrid" emptyMsg="没有符合条件的数据"
                :columnResizing="true" @pageChange="getDataAndRefresh" :isCheckBoxSelection="true"
                :leftGroupRowWidth="420" @sortChange="getDataAndRefresh" :idField="'id'" :data="saleRecords"
                @seletedRowsChanged="seletedRowsChanged" :initSelectedRows="selectedSales" style="overflow-y: unset;">
                <template #left>
                    <GridColumn field="index" align="center" width="40px">
                        <template #body="scope">
                            <span>{{ scope.rowIndex + 1 }}</span>
                        </template>
                    </GridColumn>
                    <GridColumn field="orderNo" title="销售单号" width="120" :sortable="true">
                    </GridColumn>
                    <GridColumn field="customerName" title="客户" width="120" :overflowTextEllipsis="true">
                    </GridColumn>
                    <GridColumn field="productTechnicalCode" title="技术型号" width="100" :overflowTextEllipsis="true"
                        :sortable="true">
                    </GridColumn>
                </template>
                <template #center>
                    <GridColumn field="productNo" title="产品编码" width="100" :sortable="true">
                    </GridColumn>
                    <GridColumn field="productName" title="产品名称" width="150" :overflowTextEllipsis="true"></GridColumn>
                    <GridColumn field="productSpecification" title="规格型号" width="120" :overflowTextEllipsis="true">
                    </GridColumn>
                    <GridColumn field="planProductionQuantity" title="订单计划生产" width="100" align="right">
                        <template #body="scope">
                            <div class="kingzon-pointer">
                                <div> {{ scope.row.planProductionQuantity }} {{ scope.row.salesUnit }}</div>
                                <div v-if="scope.row.salesUnitConversionId != -1"
                                    style="color: var(--color-grey);font-size: 12px;">
                                    {{ scope.row.planProductionQtyByMainUnit }}{{ scope.row.productUnit }}
                                </div>
                            </div>
                        </template>
                    </GridColumn>
                    <GridColumn field="dispatchingQuantity" title="派工待入库数" width="100" align="right">
                        <template #body="scope">
                            <div class="kingzon-pointer">
                                <div> {{ scope.row.dispatchingQuantity }} {{ scope.row.salesUnit }}</div>
                                <div v-if="scope.row.salesUnitConversionId != -1"
                                    style="color: var(--color-grey);font-size: 12px;">
                                    {{ scope.row.dispatchingQtyByMain }}{{ scope.row.productUnit }}
                                </div>
                            </div>
                        </template>
                    </GridColumn>
                    <GridColumn field="inStockQuantity" title="已入库数" width="100" align="right">
                        <template #body="scope">
                            <div class="kingzon-pointer">
                                <div> {{ scope.row.inStockQuantity }} {{ scope.row.salesUnit }}</div>
                                <div v-if="scope.row.salesUnitConversionId != -1"
                                    style="color: var(--color-grey);font-size: 12px;">
                                    {{ scope.row.inStockQtyByMainUnit }}{{ scope.row.productUnit }}
                                </div>
                            </div>
                        </template>
                    </GridColumn>
                    <GridColumn field="unDispatchQuantity" title="生产差欠数" width="100" align="right">
                        <template #body="scope">
                            <div class="kingzon-pointer">
                                <div :style="{ color: scope.row.unDispatchQuantity > 0 ? 'var(--color-green)' : '' }">
                                    {{
                                        scope.row.unDispatchQuantity }} {{ scope.row.salesUnit }}</div>
                                <div v-if="scope.row.salesUnitConversionId != -1"
                                    style="color: var(--color-grey);font-size: 12px;">
                                    {{ scope.row.unDispatchQtyByMainUnit }}{{ scope.row.productUnit }}
                                </div>
                            </div>
                        </template>
                    </GridColumn>
                    <GridColumn field="deliveryTime" title="客户交期" width="100" :sortable="true">
                        <template #body="scope">
                            {{ $utils.formatDateTime(scope.row.deliveryTime, 'yyyy-MM-dd') }}
                        </template>
                    </GridColumn>
                    <GridColumn field="remark" title="订单备注" width="120" :overflowTextEllipsis="true"></GridColumn>
                    <GridColumn field="customerMaterialNo" title="客户产品图号" width="100">
                    </GridColumn>
                    <GridColumn field="customerMaterialName" title="客户产品名称" width="120">
                    </GridColumn>
                    <GridColumn field="orderType" title="订单类型" width="80">
                        <template #body="scope">
                            {{ SaleOrderType[scope.row.orderType] }}
                        </template>
                    </GridColumn>
                </template>
            </KingZonDataGrid>
        </div>
        <KingZonShowForm :formData="formData" :columnWidth="'20%'"></KingZonShowForm>
        <KingZonDataGrid id="saleSelectGrid" style="height: 280px;" ref="selectDataGrid" emptyMsg="没有符合条件的数据"
            :columnResizing="true" :pagination="false" :showFooter="true" :footerData="footerData" :data="selectedSales"
            :clickToEdit="true" editMode="cell">
            <GridColumnGroup :frozen="true" width="380" align="left">
                <GridHeaderRow>
                    <GridColumn field="index" align="center" width="40px">
                        <template #body="scope">
                            <span>{{ scope.rowIndex + 1 }}</span>
                        </template>
                    </GridColumn>
                    <GridColumn field="orderNo" title="销售单号" width="120">
                    </GridColumn>
                    <GridColumn field="customerName" title="客户" width="120" :overflowTextEllipsis="true">
                    </GridColumn>
                    <GridColumn field="productTechnicalCode" title="技术型号" width="100" :overflowTextEllipsis="true">
                    </GridColumn>
                </GridHeaderRow>
            </GridColumnGroup>
            <GridColumnGroup :columnMoving="true" align="center">
                <GridHeaderRow>
                    <GridColumn field="productNo" title="产品编码" width="100">
                    </GridColumn>
                    <GridColumn field="productName" title="产品名称" width="150" :overflowTextEllipsis="true"></GridColumn>
                    <GridColumn field="productSpecification" title="规格型号" width="120" :overflowTextEllipsis="true">
                    </GridColumn>
                    <GridColumn field="planProductionQuantity" title="订单计划生产" width="100" align="right">
                        <template #body="scope">
                            <div class="kingzon-pointer">
                                <div> {{ scope.row.planProductionQuantity }} {{ scope.row.salesUnit }}</div>
                                <div v-if="scope.row.salesUnitConversionId != -1"
                                    style="color: var(--color-grey);font-size: 12px;">
                                    {{ scope.row.planProductionQtyByMainUnit }}{{ scope.row.productUnit }}
                                </div>
                            </div>
                        </template>
                    </GridColumn>
                    <GridColumn field="dispatchingQuantity" title="派工待入库数" width="100" align="right">
                        <template #body="scope">
                            <div class="kingzon-pointer">
                                <div> {{ scope.row.dispatchingQuantity }} {{ scope.row.salesUnit }}</div>
                                <div v-if="scope.row.salesUnitConversionId != -1"
                                    style="color: var(--color-grey);font-size: 12px;">
                                    {{ scope.row.dispatchingQtyByMain }}{{ scope.row.productUnit }}
                                </div>
                            </div>
                        </template>
                    </GridColumn>
                    <GridColumn field="inStockQuantity" title="已入库数" width="100" align="right">
                        <template #body="scope">
                            <div class="kingzon-pointer">
                                <div> {{ scope.row.inStockQuantity }} {{ scope.row.salesUnit }}</div>
                                <div v-if="scope.row.salesUnitConversionId != -1"
                                    style="color: var(--color-grey);font-size: 12px;">
                                    {{ scope.row.inStockQtyByMainUnit }}{{ scope.row.productUnit }}
                                </div>
                            </div>
                        </template>
                    </GridColumn>
                    <GridColumn field="deliveryTime" title="客户交期" width="100" :sortable="true">
                        <template #body="scope">
                            {{ $utils.formatDateTime(scope.row.deliveryTime, 'yyyy-MM-dd') }}
                        </template>
                    </GridColumn>
                    <GridColumn field="remark" title="订单备注" width="120" :overflowTextEllipsis="true"></GridColumn>
                    <GridColumn field="customerMaterialNo" title="客户产品图号" width="100">
                    </GridColumn>
                    <GridColumn field="customerMaterialName" title="客户产品名称" width="120">
                    </GridColumn>
                    <GridColumn field="orderType" title="订单类型" width="80">
                        <template #body="scope">
                            {{ SaleOrderType[scope.row.orderType] }}
                        </template>
                    </GridColumn>
                </GridHeaderRow>
            </GridColumnGroup>
            <GridColumnGroup :frozen="true" align="right" width="340">
                <GridHeaderRow>
                    <GridColumn field="unDispatchQuantity" title="生产差欠数" width="100" align="right" :sortable="true">
                        <template #body="scope">
                            <div class="kingzon-pointer">
                                <div :style="{ color: scope.row.unDispatchQuantity > 0 ? 'var(--color-green)' : '' }">
                                    {{ scope.row.unDispatchQuantity }} {{ scope.row.salesUnit }}</div>
                                <div v-if="scope.row.salesUnitConversionId != -1"
                                    style="color: var(--color-grey);font-size: 12px;">
                                    {{ scope.row.unDispatchQtyByMainUnit }}{{ scope.row.productUnit }}
                                </div>
                            </div>
                        </template>
                    </GridColumn>
                    <GridColumn field="dispatchQuantity" cellCss="kingzon-cell-editable" align="right" title="销售订单派工数"
                        width="180" :editable="true">
                        <template #body="scope">
                            <div> {{ scope.row.dispatchQuantity }}
                                {{ scope.row.dispatchUnit }}</div>
                            <!--  只显示 派工单位、销售单位、主单位 -->
                            <div style="display: flex;flex-direction: row;justify-content: right;">
                                <div style="font-style: italic;color: var(--color-grey);font-size: 12px;margin-left: 5px;"
                                    v-for="unit in scope.row.dispatchUnitListExMain" :key="unit.unitId">
                                    {{ unit.dispatchQuantity }}{{ unit.unitName }}</div>
                            </div>
                        </template>
                        <template #edit="scope">
                            <NumberBox :precision="2" v-model="scope.row.dispatchQuantity" :min="0"
                                @valueChange="onQtyChange($event, scope.row)">
                            </NumberBox>
                            <ComboBox valueField="id" v-model="scope.row.dispatchUnitConversionId"
                                textField="unitShowText" :panelStyle="{ minWidth: '100px' }"
                                :data="scope.row.dispatchUnitList" @valueChange="onUnitChange($event, scope.row)">
                                <template #item="scope">
                                    <div style=" white-space: nowrap ">{{ scope.row.unitShowText }}
                                    </div>
                                </template>
                            </ComboBox>
                        </template>
                    </GridColumn>
                    <GridColumn title="操作" align="center" width="60">
                        <template #body="scope">
                            <LinkButton @click="deleteRow(scope.row)">删除
                            </LinkButton>
                        </template>
                    </GridColumn>
                </GridHeaderRow>
            </GridColumnGroup>
        </KingZonDataGrid>
        <div class="kingzon-btn-group kingzon-page-footer">
            <LinkButton @click="$emit('close')">取消</LinkButton>
            <LinkButton btnCls="c8" @click="submit">确定</LinkButton>
        </div>
    </div>
</template>

<script lang="ts">
import { defineComponent } from 'vue';
import { Filter, Search, type FilterTmp, type SelectOption, ShowFormData } from '@/types/types';
import { DataType, FilterType, SaleOrderType, SaleOrderStatus, UnitConversionType, SystemParaType } from '@/types/enums';
import { cloneDeep } from 'lodash';
import Decimal from 'decimal.js';
import { SaleOrderDetailDispatchDTO, SaleUnitConversionDto, DispatchUnitDto, type GetDispatchOrderDto } from '@/types/production';

export default defineComponent({
    name: 'SelectSaleOrder',
    emits: ['close', 'submit'],
    data() {
        return {
            isLoading: false,
            saleRecords: [] as SaleOrderDetailDispatchDTO[],
            searchParams: [] as Search[],
            searchParamsHidden: [] as Search[],
            formData: [] as ShowFormData[],
            filterList: [] as Filter[],
            productionQtyMap: new Map<number, { quantity: number, unitName: string, unitShowText: string, ratio: number, unitId: number }>(),
            customerList: [] as SelectOption[],
            selectedSales: [] as SaleOrderDetailDispatchDTO[],
            totalDispatchUnitMap: new Map<string, { productNo: string, productName: string, productSpec?: string, data: DispatchUnitDto[] }>(),
        };
    },
    props: {
        currentRow: {
            type: Object as () => GetDispatchOrderDto,
            required: true
        },
        selectedRows: {
            type: Array as () => SaleOrderDetailDispatchDTO[],
            required: false,
            default: [] as SaleOrderDetailDispatchDTO[]
        }
    },
    watch: {
        selectedSales: {
            handler() {
                this.totalDispatchUnitMap = new Map<string, { productNo: string, productName: string, productSpec?: string, data: DispatchUnitDto[] }>();
                //按产品分组
                var groupedByProduct = this.selectedSales.reduce((acc, row) => {
                    var key = `${row.productId}-${row.specificationId}`;
                    if (!acc[key]) {
                        acc[key] = [];
                    }
                    acc[key].push(row);
                    return acc;
                }, {} as Record<string, SaleOrderDetailDispatchDTO[]>);
                Object.entries(groupedByProduct).forEach(([key, rows]) => {
                    var allUnitData = rows.flatMap((row: SaleOrderDetailDispatchDTO) => row.dispatchUnitList ?? []);
                    var uniqueAllUnitData = new Map(allUnitData.map(item => [item.id, item])).values();
                    this.totalDispatchUnitMap.set(key, {
                        productNo: rows[0].productNo,
                        productName: rows[0].productName,
                        productSpec: rows[0].productSpecification,
                        data: cloneDeep(Array.from(uniqueAllUnitData))
                    });
                });

                this.calcTotal();
            },
            deep: true
        }
    },
    computed: {
        footerData() {
            if (this.selectedRows) {
                return [{
                    index: '合计',
                    dispatchQuantity: this.totalDispatchUnitMap.size == 1 ?
                        Array.from(this.totalDispatchUnitMap.values())[0].data.map(item => item.dispatchQuantity + ' ' + item.unitName).join(' ')
                        : Array.from(this.totalDispatchUnitMap.values()).map(entity => '[' + entity.productNo + ']' + entity.data.map(item => item.dispatchQuantity + ' ' + item.unitName).join(' ')).join('\n')
                }]
            }
            return [{
                index: '合计'
            }]
        },
        SaleOrderType() {
            return SaleOrderType;
        },
    },
    mounted() {
        this.init();
    },
    methods: {
        init() {
            if (this.selectedRows && this.selectedRows.length > 0) {
                this.selectedSales = [...this.selectedRows];
            }
            // console.log('this.currentRow', this.currentRow);
            if (this.currentRow.dispatchUnitInfo?.unitConversionId) {
                var pUnit = this.currentRow.unitList.find(x => x.value == this.currentRow.dispatchUnitInfo!.unitConversionId);
                if (pUnit) {
                    this.productionQtyMap.set(Number(pUnit.value),
                        {
                            quantity: this.currentRow.dispatchQuantity,
                            unitName: this.currentRow.dispatchUnitInfo.productionUnitName,
                            unitShowText: pUnit.text,
                            ratio: 1,
                            unitId: Number(pUnit.extension),
                        });
                }
            }
            if (this.currentRow.dispatchUnitInfo?.incomeUnitConversionId) {
                var iUnit = this.currentRow.unitList.find(x => x.value == this.currentRow.dispatchUnitInfo!.incomeUnitConversionId);
                if (iUnit) {
                    this.productionQtyMap.set(Number(iUnit.value),
                        {
                            quantity: this.currentRow.incomeQuantity,
                            unitName: this.currentRow.dispatchUnitInfo.incomeUnitName,
                            unitShowText: iUnit.text,
                            ratio: this.currentRow.dispatchUnitInfo.incomeRatio,
                            unitId: Number(iUnit.extension)
                        });
                }
            }
            if (this.currentRow.dispatchUnitInfo?.baseUnitConversionId) {
                var bUnit = this.currentRow.unitList.find(x => x.value == this.currentRow.dispatchUnitInfo!.baseUnitConversionId);
                if (bUnit) {
                    this.productionQtyMap.set(Number(bUnit.value),
                        {
                            quantity: this.currentRow.baseQuantity,
                            unitName: this.currentRow.dispatchUnitInfo.baseUnitName,
                            unitShowText: bUnit.text,
                            ratio: this.currentRow.dispatchUnitInfo.baseRatio,
                            unitId: Number(bUnit.extension),
                        });
                }
            }
            if (this.currentRow.dispatchUnitInfo?.mainUnitConversionId) {
                var mUnit = this.currentRow.unitList.find(x => x.value == this.currentRow.dispatchUnitInfo!.mainUnitConversionId);
                if (mUnit) {
                    this.productionQtyMap.set(Number(mUnit.value), {
                        quantity: this.currentRow.mainQuantity,
                        unitName: this.currentRow.dispatchUnitInfo.mainUnitName,
                        unitShowText: mUnit.text,
                        ratio: this.currentRow.dispatchUnitInfo.mainRatio,
                        unitId: Number(mUnit.extension)
                    });
                }
            }
            this.formData = [
                new ShowFormData('派工产品编码', this.currentRow.productNo),
                new ShowFormData('派工产品名称', this.currentRow.productName),
                new ShowFormData('派工产品类别', this.currentRow.productCategory),
                new ShowFormData('派工产品备注', this.currentRow.remark ?? "", undefined, undefined, undefined, undefined, '40%'),
                new ShowFormData('技术型号', this.currentRow.productTechnicalCode ?? ""),
                new ShowFormData('规格型号', this.currentRow.materialSpecification ?? this.currentRow.productSpecification ?? ""),
                new ShowFormData('外包装方案', this.currentRow.pacakgeList.find(x => x.value == this.currentRow.packageId)?.text ?? ""),
                new ShowFormData('派工单总量', Array.from(this.productionQtyMap.values()).map(item => `${item.quantity} ${item.unitName}`).join(', '), undefined, undefined, undefined, undefined, '40%'),
            ]
            this.filterList = [new Filter('technicalCode', this.currentRow.productTechnicalCode, FilterType.Equals, DataType.Text)];
            this.isLoading = true;
            this.getCustomers()
                .then((res: any) => {
                    this.customerList = res.data?.map((item: any) => {
                        return { value: item.id, text: item.customerCode + '-' + item.customerName }
                    }) as SelectOption[];
                    this.customerList.unshift({ value: '', text: '全部' });

                    this.addSearchParam();

                    return this.getData()
                })
                .then((res: any) => {
                    this.saleRecords = res.data;
                    this.handleUnitData();
                })
                .catch(err => {
                    this.$kingzon_messager.showError(err);
                })
                .finally(() => {
                    this.isLoading = false;
                })
        },
        getData() {
            var statusFilter = {
                key: 'status',
                value: SaleOrderStatus.进行中,
                filterType: FilterType.Equals,
                dataType: DataType.Integer
            } as Filter;
            this.filterList.push(statusFilter);
            return this.$http.post('/api/sale/SaleOrderManage/GetOrderList', this.filterList, this.$refs.dataGrid);
        },
        //构建查询面板内容
        addSearchParam(): void {
            this.searchParams.push(new Search('saleOrder.orderNo', '销售单号', 'default'));
            this.searchParams.push(new Search('saleOrder.customerId', '客户', 'default', FilterType.Equals, DataType.SelectNum, "", this.customerList, "", "300px"));
            this.searchParams.push(new Search('technicalCode', '技术型号', 'default', undefined, undefined, this.currentRow.productTechnicalCode));
            this.searchParams.push(new Search('customerMaterialNo', '客户产品图号', 'default'));
            this.searchParamsHidden.push(new Search('goodsNo', '产品编码', 'default'));
            this.searchParamsHidden.push(new Search('goodsName', '产品名称', 'default'));
            this.searchParamsHidden.push(new Search('specification', '规格型号', 'default'));
            this.searchParamsHidden.push(new Search('remark', '订单备注'));
            this.searchParamsHidden.push(new Search('deliveryTime', "客户交期", "default", FilterType.GreaterThanOrEqual, DataType.DateTime, undefined, undefined, '开始时间'));
            this.searchParamsHidden.push(new Search('deliveryTime', "", "default", FilterType.LessThanOrEqual, DataType.DateTime, undefined, undefined, '结束时间'));
        },
        handleUnitData() {
            var productionToMainRatio = this.currentRow.dispatchUnitInfo?.mainRatio ? this.currentRow.dispatchUnitInfo.mainRatio : 0;
            var productionUnitData = Array.from(this.productionQtyMap.entries()).map(item => {
                return {
                    id: item[0],
                    unitName: item[1].unitName,
                    unitShowText: item[1].unitShowText,
                    dispatchQuantity: 0,
                    ratio: item[1].ratio ? Decimal.div(productionToMainRatio, item[1].ratio).toNumber() : 0, //转换为针对主单位的ratio
                    unitId: item[1].unitId
                } as DispatchUnitDto
            });
            this.saleRecords.forEach((row) => {
                var saleToMainRatio = row.saleToBaseRatio; //销售单位的对主单位的转化
                var saleUnitData = [{
                    id: row.salesUnitConversionId,
                    unitName: row.salesUnit, //销售单位
                    unitShowText: row.salesUnit,
                    dispatchQuantity: 0,
                    ratio: row.saleToBaseRatio, //销售单位的对主单位的转化
                    unitId: row.salesUnitId
                } as DispatchUnitDto];
                if (row.unitConversionList.length > 0) {
                    saleUnitData = saleUnitData.concat(row.unitConversionList.map((item: SaleUnitConversionDto) => {
                        var ratio = item.ratio ? Decimal.div(saleToMainRatio, item.ratio).toNumber() : 0;//转换为针对主单位的ratio
                        return {
                            id: item.id,
                            unitName: item.unitName,
                            unitShowText: item.unitName + (item.remark ? "【" + item.remark + "】" : ''),
                            dispatchQuantity: 0,
                            ratio: ratio,
                            unitId: item.unitId,
                            remark: item.remark
                        } as DispatchUnitDto
                    }))
                }
                var unitData = saleUnitData;
                //派工产品 和 销售单产品相同，才需要把派工产品的单位数据也加上
                if (this.currentRow.productId == row.productId && this.currentRow.specificationId == row.specificationId) {

                    unitData = unitData.concat(productionUnitData);
                }
                // console.log('unitData', unitData);
                //去重 
                var uniqueUnitData = new Map(unitData.map(item => [item.id, item])).values();
                row.dispatchUnitList = cloneDeep(Array.from(uniqueUnitData));
                // console.log('row.dispatchUnitList', row.dispatchUnitList);
                //默认使用销售单位 作为派工单位
                row.dispatchUnitConversionId = row.salesUnitConversionId;
                row.dispatchUnit = row.salesUnit;
                row.dispatchQuantity = 0;
                row.dispatchUnitListExMain = row.dispatchUnitList.filter((item) => item.id != row.dispatchUnitConversionId);
                // console.log('row.dispatchUnitListExMain', row.dispatchUnitListExMain);
            })
        },
        search(params: FilterTmp[]) {
            this.filterList = params.find(item => item.table == 'default')?.filter ?? [];
            this.getDataAndRefresh();
        },
        submit() {
            //数据验证
            var totalDispatchUnitList = this.totalDispatchUnitMap.get(`${this.currentRow.productId}-${this.currentRow.specificationId}`)?.data;
            var valid = true;
            this.productionQtyMap.forEach((value, key) => {
                var totalDispatchQty = totalDispatchUnitList?.find(item => item.id == key)?.dispatchQuantity ?? 0;
                if (totalDispatchQty > value.quantity) {
                    this.$kingzon_messager.showError(`派工单位【${value.unitName}】的销售单派工总量【${totalDispatchQty}】不能大于派工单总量【${value.quantity}】`);
                    valid = false;
                }
            });
            if (valid) {
                var data = cloneDeep(this.selectedSales.filter(x => x.dispatchQuantity));
                this.$emit('submit', data);
            }
        },
        seletedRowsChanged(rows: SaleOrderDetailDispatchDTO[]) {
            this.selectedSales = rows;
        },
        getDataAndRefresh() {
            this.isLoading = true;
            this.getData()
                .then((res: any) => {
                    this.saleRecords = res.data;
                    this.handleUnitData();
                })
                .catch(err => {
                    this.$kingzon_messager.showError(err);
                })
                .finally(() => {
                    this.isLoading = false;
                })
        },
        getCustomers() {
            return this.$http.get('api/base/Customer/GetSalesManAllCustomer', { includeMain: true })
        },
        deleteRow(row: SaleOrderDetailDispatchDTO) {
            this.$kingzon_messager.showConfim("是否确认删除该销售单")
                .then(() => {
                    this.selectedSales = this.selectedSales.filter(x => x.id != row.id);
                    //表格取消选中
                    var saleRow = this.saleRecords.find(x => x.id == row.id);
                    if (saleRow) {
                        saleRow.selected = false; // 确保从数据源中移除选中状态
                    }
                    var dataGrid = this.$refs.dataGrid as EasyUI.DataGrid;
                    if (dataGrid) {
                        dataGrid.selectedRows = dataGrid.selectedRows.filter((i) => i.id !== row.id);
                    }
                })
        },
        onUnitChange(param: { currentValue, previousValue }, row: SaleOrderDetailDispatchDTO) {
            row.dispatchUnitConversionId = param.currentValue;
            var selectedUnit = row.dispatchUnitList.find((item: DispatchUnitDto) => item.id == param.currentValue);
            if (selectedUnit) {
                row.dispatchUnit = selectedUnit.unitName;
                row.dispatchUnitListExMain = row.dispatchUnitList.filter((item: DispatchUnitDto) => item.id != param.currentValue);
            }
            this.calcRow(row);
            this.calcTotal();
        },
        onQtyChange(param: { currentValue, previousValue }, row: SaleOrderDetailDispatchDTO) {
            this.calcRow(row);
            this.calcTotal();
        },
        calcRow(row: SaleOrderDetailDispatchDTO) {
            //出库量（主单位）
            var ratio = row.dispatchUnitList?.find(x => x.id == row.dispatchUnitConversionId)?.ratio ?? 0;
            //出库量 多单位
            row.dispatchUnitListExMain = [];
            row.dispatchUnitList?.forEach(item => {
                var itemRatio = item.ratio ? ratio / item.ratio : 0;
                item.dispatchQuantity = this.decimalPlaces4(Decimal.mul(row.dispatchQuantity ?? 0, itemRatio)).toNumber();
                if (item.id != row.dispatchUnitConversionId) {
                    row.dispatchUnitListExMain?.push(item);
                }
            });
        },
        calcTotal() {
            //根据产品分组
            var groupedByProduct = this.selectedSales.reduce((acc, row) => {
                const key = `${row.productId}-${row.specificationId}`;
                if (!acc[key]) {
                    acc[key] = [];
                }
                acc[key].push(row);
                return acc;
            }, {} as Record<string, SaleOrderDetailDispatchDTO[]>);
            //每组产品 统计多单位的派工量
            this.totalDispatchUnitMap.forEach((entity, key) => {
                const rows = groupedByProduct[key] || [];
                const qtyMap = new Map();
                rows.flatMap(x => x.dispatchUnitList ?? []).forEach(item => {
                    qtyMap.set(
                        item.id,
                        Decimal.add(
                            qtyMap.get(item.id) ?? new Decimal(0),
                            item.dispatchQuantity
                        )
                    );
                });
                entity.data.forEach(item => {
                    item.dispatchQuantity = this.decimalPlaces4(qtyMap.get(item.id) ?? new Decimal(0)).toNumber();
                });
            });
        },
        decimalPlaces4(data) {
            //最多保留4位小数
            if (data.decimalPlaces() > 4) {
                data = data.toDecimalPlaces(4, Decimal.ROUND_HALF_UP);
            }
            return data;
        },
    },
})
</script>

<style scoped>
:deep(.datagrid-editable>.f-column) {
    display: flex;
    flex-direction: row;
}
</style>
