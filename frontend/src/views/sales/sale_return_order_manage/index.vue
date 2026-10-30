<template>
    <Loading v-show="isLoading" />
    <div class="kingzon-page">
        <div class="kingzon-page-title">
            <span>销售退货管理</span>
        </div>
        <div class="kingzon-page-body">
            <SearchPanel :searchParams="searchParams" :searchParamsHidden="searchParamsHidden" @search="search" />

            <div class="kingzon-page-content">
                <div class="kingzon-btn-group" style="margin: 5px">
                    <LinkButton iconCls="icon-add" @click="onClickAdd">送货单退货</LinkButton>
                    <LinkButton iconCls="icon-add" @click="onClickAddHisProduct">产品直接退货</LinkButton>
                </div>
                <KingZonDataGrid ref="dataGrid" class="kingzon-page-fill" :data="saleReturnOrderDetailList"
                    :showDownLoad="true" exportFileName="销售退货订单" emptyMsg="没有符合条件的数据" selectionMode="null"
                    id="saleReturnOrderDataGrid" :cellSpan="cellSpan"
                    @rowContextMenu="rowContextMenu($event)" @pageChange="getData" @sortChange="getData">
                    <GridColumnGroup :frozen="true" align="left" width="520">
                        <GridHeaderRow>
                            <GridColumn field="index" align="center" width="40">
                                <template #body="scope">
                                    {{ scope.rowIndex + 1 }}
                                </template>
                            </GridColumn>
                            <GridColumn field="saleReturnOrderNo" title="退货单号" align="left" width="140" :sortable="true"
                                :overflowTextEllipsis="true" />
                            <GridColumn field="productNo" title="产品编码" align="left" width="100" :sortable="true"
                                :overflowTextEllipsis="true" />
                            <GridColumn field="productName" title="产品名称" align="left" width="120" :sortable="true"
                                :overflowTextEllipsis="true" />
                            <GridColumn field="productSpecification" title="规格型号" align="left" width="120"
                                :sortable="true" :overflowTextEllipsis="true" />
                            <GridColumn field="productTechnicalCode" title="技术型号" align="left" width="100"
                                :sortable="true" :overflowTextEllipsis="true" />
                        </GridHeaderRow>
                    </GridColumnGroup>
                    <GridColumnGroup :columnMoving="true" align="center">
                        <GridHeaderRow>
                            <GridColumn field="customerCode" title="客户" align="left" width="150" :sortable="true"
                                :overflowTextEllipsis="true">
                                <template #body="scope">
                                    <span :title="scope.row.customerCode + ' - ' + scope.row.customerName"> {{
                                        scope.row.customerCode }} - {{ scope.row.customerName }}</span>
                                </template>
                            </GridColumn>
                            <GridColumn field="inventoryQuantity" title="入库状态" align="center" width="80" :sortable="true">
                                <template #body="scope">
                                    <span v-if="Number(scope.row.inventoryQuantity || 0) > 0" class="kingzon-tag kingzon-tag-success">已入库</span>
                                    <span v-else class="kingzon-tag">未入库</span>
                                </template>
                            </GridColumn>
                            <GridColumn field="isComplete" title="是否完成" align="center" width="80" :sortable="true">
                                <template #body="scope">
                                    <span v-if="scope.row.isComplete" class="kingzon-tag kingzon-tag-success">已完成</span>
                                    <span v-else class="kingzon-tag">未完成</span>
                                </template>
                            </GridColumn>
                            <GridColumn field="saleReturnType" title="退货类型" width="80" align="center">
                                <template #body="scope">
                                    <span
                                        :class="scope.row.saleReturnType === SaleReturnType.正常品 ? 'kingzon-tag kingzon-tag-success' : 'kingzon-tag'">
                                        {{ SaleReturnType[scope.row.saleReturnType] }}
                                    </span>
                                </template>
                            </GridColumn>
                            <GridColumn field="returnQuantity" title="退货数量" align="right" width="110" :sortable="true">
                                <template #body="scope">
                                    <UnitQuantityShow :quantity="scope.row.returnQuantity"
                                        :unitId="scope.row.inputUnitConversionId" :unitList="scope.row.unitInfos" />
                                </template>
                            </GridColumn>
                            <GridColumn field="inventoryQuantity" title="入库数量" align="right" width="110"
                                :sortable="true">
                                <template #body="scope">
                                    <span :class="scope.row.inventoryQuantity ? 'kingzon-detail-text' : ''">
                                        <UnitQuantityShow :quantity="scope.row.inventoryQuantity"
                                            :unitId="scope.row.inputUnitConversionId" :unitList="scope.row.unitInfos"
                                            @click="inventoryDetail(scope.row)" />
                                    </span>
                                </template>
                            </GridColumn>
                            <GridColumn field="inputPrice" title="单价" align="right" width="100" :sortable="true">
                                <template #body="scope">
                                    {{ (scope.row.inputPrice).toFixed(4) }}
                                </template>
                            </GridColumn>
                            <GridColumn field="totalPrice" title="总价" align="right" width="100" :sortable="true">
                                <template #body="scope">
                                    {{ scope.row.totalPrice.toFixed(4) }}
                                </template>
                            </GridColumn>
                            <GridColumn field="saleReturnOrderTotalPrice" title="订单合计金额" align="right" width="110"
                                :sortable="true">
                                <template #body="scope">
                                    {{ scope.row.saleReturnOrderTotalPrice.toFixed(4) }}
                                </template>
                            </GridColumn>
                            <GridColumn field="saleReturnOrderReturnTime" title="退货日期" align="left" width="100"
                                :sortable="true">
                                <template #body="scope">
                                    {{ $utils.formatDateTime(scope.row.saleReturnOrderReturnTime, 'yyyy-MM-dd') }}
                                </template>
                            </GridColumn>
                            <GridColumn field="reason" title="退货原因" align="left" width="140"
                                :overflowTextEllipsis="true" />
                            <GridColumn field="productCustomerMaterialNo" title="客户产品图号" align="left" width="110"
                                :overflowTextEllipsis="true" />
                            <GridColumn field="deliveryOrderNo" title="送货单号" align="left" width="120" :sortable="true"
                                :overflowTextEllipsis="true" />
                            <GridColumn field="saleOrderNo" title="销售单号" align="left" width="120" :sortable="true"
                                :overflowTextEllipsis="true" />
                            <GridColumn field="deliveryQuantity" title="送货通知量" align="right" width="110"
                                :sortable="true">
                                <template #body="scope">
                                    <span v-if="scope.row.deliveryOrderNo">
                                        <UnitQuantityShow :quantity="scope.row.deliveryQuantity"
                                            :unitId="scope.row.inputUnitConversionId" :unitList="scope.row.unitInfos" />
                                    </span>
                                </template>
                            </GridColumn>
                            <GridColumn field="actualDeliveryQuantity" title="送货出库量" align="right" width="110"
                                :sortable="true">
                                <template #body="scope">
                                    <span v-if="scope.row.deliveryOrderNo">
                                        <UnitQuantityShow :quantity="scope.row.actualDeliveryQuantity"
                                            :unitId="scope.row.inputUnitConversionId" :unitList="scope.row.unitInfos" />
                                    </span>
                                </template>
                            </GridColumn>
                            <GridColumn field="remark" title="备注" align="left" width="140"
                                :overflowTextEllipsis="true" />
                            <GridColumn field="saleReturnOrderRemark" title="退货单备注" align="left" width="140"
                                :overflowTextEllipsis="true" />
                        </GridHeaderRow>
                    </GridColumnGroup>
                    <GridColumnGroup :frozen="true" align="right">
                        <GridHeaderRow>
                            <GridColumn field="file" title="文件" align="center" width="95">
                                <template #body="scope">
                                    <ButtonGroup style="height:24px">
                                        <KingZonFileButton @onFileSelect="onFileSelect"
                                            @click="sourceId = scope.row.saleReturnOrderId">上传
                                        </KingZonFileButton>
                                        <LinkButton @click="showFileList(scope.row)">查看</LinkButton>
                                    </ButtonGroup>
                                </template>
                            </GridColumn>
                            <GridColumn field="options" title="操作" align="center" width="190">
                                <template #body="scope">
                                    <ButtonGroup style="height:24px">
                                        <LinkButton v-if="canEdit(scope.row)" @click="editRow(scope.row)">编辑
                                        </LinkButton>
                                        <LinkButton v-if="canEdit(scope.row)" @click="deleteRow(scope.row)">删除
                                        </LinkButton>
                                        <LinkButton v-if="!scope.row.isComplete" @click="editRowComplete(scope.row)">确认完成
                                        </LinkButton>
                                        <LinkButton v-else-if="Number(scope.row.inventoryQuantity || 0) === 0" @click="cancelComplete(scope.row)">取消完成
                                        </LinkButton>
                                    </ButtonGroup>
                                </template>
                            </GridColumn>
                        </GridHeaderRow>
                    </GridColumnGroup>
                </KingZonDataGrid>
            </div>
        </div>
    </div>
    <Drawer v-if="showAddDrawer" ref="addDrawer" region="east" :drawerStyle="{ width: '90%' }" :closable="true"
        :maskClosable="false" @collapse="showAddDrawer = false">
        <template #header>
            <div class="f-full" style="line-height: 30px">新增送货单退货</div>
        </template>
        <AddSaleReturnOrder @cancel="closeAddDrawer" @submit="addSuccess" />
    </Drawer>
    <Drawer v-if="showProductDrawer" ref="productDrawer" region="east" :drawerStyle="{ width: '90%' }" :closable="true"
        :maskClosable="false" @collapse="showProductDrawer = false">
        <template #header>
            <div class="f-full" style="line-height: 30px">新增产品直接退货</div>
        </template>
        <AddSaleReturnProduct @cancel="closeProductDrawer" @submit="addProductSuccess" />
    </Drawer>
    <Drawer v-if="showEditDrawer" ref="editDrawer" region="east" :drawerStyle="{ width: '90%' }" :closable="true"
        :maskClosable="false" @collapse="showEditDrawer = false">
        <template #header>
            <div class="f-full" style="line-height: 30px">编辑销售退货</div>
        </template>
        <EditSaleReturnOrder :row="editRowData" @cancel="closeEditDrawer" @submit="editSuccess" />
    </Drawer>
    <Dialog ref="fileUpload" :title="'销售退货文件上传'" :dialogStyle="{ width: '500px', maxHeight: '600px' }" :draggable="true"
        :resizable="true" :modal="true" :closed="true">
        <FileUpload @close="fileDialogClose" @submit="fileDialogSubmit" :uploadFileList="uploadFileList"
            :sourcId="sourceId" :sourceType="FileStorageSourceType.销售退货文件" :fileBase="FileBase.SaleReturnOrder">
        </FileUpload>
    </Dialog>
    <Drawer :drawerStyle="{ width: '55%' }" region="east" ref="fileList" :closable="true" :maskClosable="false"
        v-if="showFileDrawer" @collapse="showFileDrawer = false;">
        <template #header>
            <div class="f-full" style="line-height:30px">{{ selectedRow.saleReturnOrderNo }} - 订单文件</div>
        </template>
        <FileList :sourceType="FileStorageSourceType.销售退货文件" :sourceId="sourceId" @close="fileListClose"
            :showPage="false" :fileBase="FileBase.SaleReturnOrder">
        </FileList>
    </Drawer>
    <Menu v-if="contextMenuVisible" :inline="true" :style="contextMenuStyle" class="detail_menu">
        <MenuItem v-for="item in contextMenuItems" :key="String(item.value)" :text="item.text"
            @click="onContextMenuClick(item)" />
    </Menu>
    <Dialog ref="inventoryDetailDialog"
        :title="selectedReturnDetail.saleReturnOrderNo + selectedReturnDetail.productName + '-入库详情'" :modal="true"
        closed :draggable="true" :dialogStyle="{ height: '90%', width: '85%' }">
        <SourceInventoryDetail :multiSourceIds="selectedReturnDetail.arrivalRecordIds"
            :sourceType="InventorySourceType.退货入库" ref="sourceInventory" :inventoryType="InventoryType.入库"
            :typeLable="'入库'" :isShowPrice="false" :formData="infoData" :rightWidth="0" :isShowPurchaseExt="false">
            <template #showForm>
                <KingZonShowForm :formData="infoData" :columnWidth="'25%'">
                </KingZonShowForm>
            </template>
        </SourceInventoryDetail>
    </Dialog>
</template>

<script lang="ts">
import { DataType, FileBase, FileStorageSourceType, FilterType, InventorySourceType, SaleReturnType, ShowFormDataType } from '@/types/enums'
import { SaleReturnOrderFilterListDTO, type SaleReturnOrderDetailViewDTO } from '@/types/sale';
import { type FilterTmp, Search, type SelectOption, ShowFormData, type UploadFile } from '@/types/types'
import { defineComponent } from 'vue'
import AddSaleReturnOrder from './add-sale-return-order.vue'
import AddSaleReturnProduct from './add-sale-return-product.vue'
import EditSaleReturnOrder from './edit-sale-return-order.vue'
import { cloneDeep } from 'lodash';
import UnitQuantityShow from '../delivery_order_manage/inventory/unit-quantity-show.vue';
import SourceInventoryDetail from '../delivery_order_manage/inventory/source-inventory-detail.vue';
import { InventoryType } from '../delivery_order_manage/inventory/inventory.js';



export default defineComponent({
    name: 'saleReturnOrder',
    components: { AddSaleReturnOrder, UnitQuantityShow, AddSaleReturnProduct, EditSaleReturnOrder, SourceInventoryDetail },
    data() {
        return {
            isLoading: false,
            showAddDrawer: false,
            showProductDrawer: false,
            searchParams: [] as Search[],
            searchParamsHidden: [] as Search[],
            saleReturnTypeList: [] as SelectOption[],
            saleReturnOrderDetailList: [] as SaleReturnOrderDetailViewDTO[],
            filterList: new SaleReturnOrderFilterListDTO(),
            uploadFileList: [] as UploadFile[],
            uploadFileDialog: {} as EasyUI.Dialog,
            sourceId: 0,
            showFileDrawer: false,
            fileDrawer: {} as EasyUI.Drawer,
            selectedRow: {} as any,
            showEditDrawer: false,
            editRowData: {} as SaleReturnOrderDetailViewDTO,
            selectedRows: [] as SaleReturnOrderDetailViewDTO[],
            contextMenuVisible: false,
            contextMenuStyle: { left: '0px', top: '0px' } as any,
            contextMenuItems: [] as SelectOption[],
            contextMenuRow: {} as SaleReturnOrderDetailViewDTO,
            cellSpanColumns: [
                'saleReturnOrderNo',
                'customerCode',
                'customerName',
                'saleReturnOrderTotalPrice',
                'saleReturnOrderReturnTime',
                'saleReturnOrderRemark',
                'file',
            ],
            selectedReturnDetail: {} as SaleReturnOrderDetailViewDTO,
            infoData: [] as ShowFormData[],
        }
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
        },
        InventorySourceType() {
            return InventorySourceType
        },
        InventoryType() {
            return InventoryType
        }
    },
    created() {
        this.saleReturnTypeList = this.$utils.getEnumKeyValueArray(SaleReturnType)
        this.addSearchParam()
    },
    mounted() {
        this.uploadFileDialog = this.$refs.fileUpload as EasyUI.Dialog;
        document.addEventListener('pointerdown', this.onDocumentPointerDown)
        this.getData()
    },
    beforeUnmount() {
        document.removeEventListener('pointerdown', this.onDocumentPointerDown)
    },
    methods: {
        addSearchParam(): void {
            this.searchParams.push(
                new Search('orderNo', '退货单号', 'saleReturnOrder', FilterType.Contains, DataType.Text),
                new Search('deliveryOrderNo', '送货单号', 'deliveryOrder', FilterType.Contains, DataType.Text),
                new Search('productNo', '产品编码', 'product', FilterType.Contains, DataType.Text),
                new Search('productName', '产品名称', 'product', FilterType.Contains, DataType.Text),
                new Search('materialSpecification', '规格型号', 'product', FilterType.Contains, DataType.Text),
                new Search('technicalCode', '技术型号', 'product', FilterType.Contains, DataType.Text),
            )

            this.searchParamsHidden.push(
                new Search('customerMaterialNo', '客户产品图号', 'saleReturnOrderDetail', FilterType.Contains, DataType.Text),
                new Search('saleReturnType', '退货类型', 'saleReturnOrderDetail', FilterType.Equals, DataType.SelectNum, undefined, this.saleReturnTypeList),
                new Search('isComplete', '完成状态', 'saleReturnOrderDetail', FilterType.Equals, DataType.SelectBoolean, '', [
                    { text: '全部', value: '' },
                    { text: '已完成', value: '1' },
                    { text: '未完成', value: '0' },
                ]),
                new Search('returnTime', '退货日期', 'saleReturnOrder', FilterType.GreaterThanOrEqual, DataType.DateTime, undefined, undefined, '开始时间'),
                new Search('returnTime', undefined, 'saleReturnOrder', FilterType.LessThanOrEqual, DataType.DateTime, undefined, undefined, '结束时间'),
                new Search('reason', '退货原因', 'saleReturnOrderDetail', FilterType.Contains, DataType.Text),
                new Search('remark', '备注', 'saleReturnOrderDetail', FilterType.Contains, DataType.Text),
            )
        },
        search(params: FilterTmp[]) {
            this.filterList.saleReturnOrderDetailFilterList = params.find(item => item.table === 'saleReturnOrderDetail')?.filter ?? []
            this.filterList.saleReturnOrderFilterList = params.find(item => item.table === 'saleReturnOrder')?.filter ?? []
            this.filterList.deliveryOrderFilterList = params.find(item => item.table === 'deliveryOrder')?.filter ?? []
            this.filterList.deliveryOrderDetailFilterList = params.find(item => item.table === 'deliveryOrderDetail')?.filter ?? []
            this.filterList.saleOrderDetailFilterList = params.find(item => item.table === 'saleOrderDetail')?.filter ?? []
            this.filterList.productFilterList = params.find(item => item.table === 'product')?.filter ?? []
            this.getData()
        },
        getData(): void {
            this.isLoading = true
            this.getSaleReturnOrderDetails()
                .then((res: any) => {
                    this.saleReturnOrderDetailList = res.data as SaleReturnOrderDetailViewDTO[]
                })
                .catch((err: any) => {
                    this.$kingzon_messager.showError(err)
                })
                .finally(() => {
                    this.isLoading = false
                })
        },
        onClickAdd() {
            this.showAddDrawer = true
            this.$nextTick(() => {
                const drawer = this.$refs.addDrawer as EasyUI.Drawer
                drawer.expand()
            })
        },
        addSuccess() {
            this.closeAddDrawer()
            this.getData()
        },
        addProductSuccess() {
            this.closeProductDrawer()
            this.getData()
        },
        closeAddDrawer() {
            const drawer = this.$refs.addDrawer as EasyUI.Drawer
            drawer.collapse()
        },
        closeProductDrawer() {
            (this.$refs.productDrawer as EasyUI.Drawer).collapse()
        },
        closeEditDrawer() {
            (this.$refs.editDrawer as EasyUI.Drawer).collapse()
        },
        editSuccess() {
            this.closeEditDrawer()
            this.getData()
        },
        onClickAddHisProduct() {
            this.showProductDrawer = true
            this.$nextTick(() => (this.$refs.productDrawer as EasyUI.Drawer).expand())
        },
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
        deleteRow(row: SaleReturnOrderDetailViewDTO) {
            this.$messager.confirm({
                title: '提示',
                msg: '确定删除这条退货明细吗？',
                result: (confirmed: boolean) => {
                    if (!confirmed) return
                    this.isLoading = true
                    this.$http.post('api/sale/SaleReturnOrderManage/DeleteSaleReturnOrderDetail', { id: row.id })
                        .then((res: any) => {
                            this.$kingzon_messager.showInfo(res.data || '删除成功')
                            this.getData()
                        })
                        .catch((err: unknown) => this.$kingzon_messager.showError(err || '删除失败'))
                        .finally(() => { this.isLoading = false })
                },
            })
        },
        editRowComplete(row: SaleReturnOrderDetailViewDTO) {
            this.$messager.confirm({
                title: '提示',
                msg: `退货数量为 ${row.inputReturnQuantity ?? row.returnQuantity}，入库数量为 ${row.inventoryQuantity}，确定确认完成吗？`,
                result: (confirmed: boolean) => {
                    if (!confirmed) return
                    this.isLoading = true
                    this.$http.post('api/sale/SaleReturnOrderManage/CompleteSaleReturnOrderDetail', { id: row.id })
                        .then((res: any) => {
                            this.$kingzon_messager.showInfo(res.data || '确认完成成功')
                            this.getData()
                        })
                        .catch((err: unknown) => this.$kingzon_messager.showError(err || '确认完成失败'))
                        .finally(() => { this.isLoading = false })
                },
            })
        },
        cancelComplete(row: SaleReturnOrderDetailViewDTO) {
            this.$messager.confirm({
                title: '提示',
                msg: '确定取消该退货明细的完成状态吗？',
                result: (confirmed: boolean) => {
                    if (!confirmed) return
                    this.isLoading = true
                    this.$http.post('api/sale/SaleReturnOrderManage/CancelCompleteSaleReturnOrderDetail', { id: row.id })
                        .then((res: any) => {
                            this.$kingzon_messager.showInfo(res.data || '取消完成成功')
                            this.getData()
                        })
                        .catch((err: unknown) => this.$kingzon_messager.showError(err || '取消完成失败'))
                        .finally(() => { this.isLoading = false })
                },
            })
        },
        canEdit(row: SaleReturnOrderDetailViewDTO) {
            return !row.isComplete && Number(row.inventoryQuantity || 0) === 0
        },
        editRow(row: SaleReturnOrderDetailViewDTO) {
            this.editRowData = cloneDeep(row)
            this.showEditDrawer = true
            this.$nextTick(() => (this.$refs.editDrawer as EasyUI.Drawer).expand())
        },
        selectedRowsChanged() {
            const dataGrid = this.$refs.dataGrid as any
            this.selectedRows = dataGrid?.selectedRows ?? dataGrid?.[0]?.selectedRows ?? []
        },
        rowContextMenu(event: any) {
            event.originalEvent?.preventDefault()
            this.contextMenuRow = event.row as SaleReturnOrderDetailViewDTO
            const editable = this.contextMenuRow.inventoryQuantity === 0 && !this.contextMenuRow.isComplete
            this.contextMenuItems = [
                ...(editable ? [
                    { text: '编辑明细', value: 'editDetail' },
                    { text: '删除明细', value: 'deleteDetail' },
                ] : []),
                ...(!this.contextMenuRow.isComplete ? [{ text: '确认完成', value: 'complete' }] : []),
                ...(this.contextMenuRow.isComplete && Number(this.contextMenuRow.inventoryQuantity || 0) === 0
                    ? [{ text: '取消完成', value: 'cancelComplete' }]
                    : []),
            ]

            if (this.contextMenuItems.length === 0) {
                this.contextMenuItems = [{ text: '已完成，无可用操作', value: 'none' }]
            }

            this.contextMenuStyle = {
                left: `${event.originalEvent?.clientX ?? 0}px`,
                top: `${event.originalEvent?.clientY ?? 0}px`,
            }
            this.contextMenuVisible = true
        },
        onContextMenuClick(item: SelectOption) {
            this.contextMenuVisible = false
            switch (item.value) {
                case 'editDetail': this.editRow(this.contextMenuRow); break
                case 'deleteDetail': this.deleteRow(this.contextMenuRow); break
                case 'complete': this.editRowComplete(this.contextMenuRow); break
                case 'cancelComplete': this.cancelComplete(this.contextMenuRow); break
            }
        },
        onDocumentPointerDown(event: PointerEvent) {
            if (!this.contextMenuVisible) return
            // 获取完整事件路径（含 Shadow DOM，兼容性处理），判断点击是否落在自定义菜单内
            const path = (event as any).composedPath
                ? (event as any).composedPath()
                : ((event as any).path || this.buildEventPath(event))
            for (const node of path) {
                if (!(node instanceof Element)) continue
                if (node.classList.contains('detail_menu')) return
            }
            this.hideContextMenu()
        },
        // 备用：构造事件路径（老浏览器回落）
        buildEventPath(event: Event): EventTarget[] {
            const path: EventTarget[] = []
            let node: any = event.target
            while (node) {
                path.push(node)
                node = node.parentNode || (node.getRootNode && node.getRootNode().host)
            }
            path.push(window)
            return path
        },
        hideContextMenu() {
            this.contextMenuVisible = false
        },
        getSaleReturnOrderDetails(): Promise<HttpResponseType> {
            return this.$http.post(
                'api/sale/SaleReturnOrderManage/GetSaleReturnOrderDetails',
                this.filterList,
                this.$refs.dataGrid,
            )
        },
        cellSpan(data: any) {
            if (!this.cellSpanColumns.includes(data.column.field)) {
                return { rowspan: 1, colspan: 1 }
            }

            const rowIndex = data.rowIndex
            const rows = this.saleReturnOrderDetailList
            const orderNo = data.row.saleReturnOrderNo

            if (rowIndex > 0 && rows[rowIndex - 1].saleReturnOrderNo === orderNo) {
                return { rowspan: 0, colspan: 0 }
            }

            let rowspan = 1
            for (let index = rowIndex + 1; index < rows.length && rows[index].saleReturnOrderNo === orderNo; index += 1) {
                rowspan += 1
            }
            return { rowspan, colspan: 1 }
        },
        inventoryDetail(row: SaleReturnOrderDetailViewDTO) {
            if (Number(row.inventoryQuantity || 0) <= 0) {
                this.$kingzon_messager.showInfo("暂无入库记录")
                return
            }
            this.selectedReturnDetail = row;
            this.infoData = [
                { label: '退货单号', value: row.saleReturnOrderNo ?? '', type: ShowFormDataType.文本, validate: true },
                { label: '客户', value: `${row.customerCode ?? ''}-${row.customerName ?? ''}`, type: ShowFormDataType.文本, validate: true },
                { label: '客户产品图号', value: row.customerCode ?? '', type: ShowFormDataType.文本, validate: true },
                { label: '客户产品名称', value: row.customerName ?? '', type: ShowFormDataType.文本, validate: true },
                { label: '产品编码', value: row.productNo ?? '', type: ShowFormDataType.文本, validate: true },
                { label: '产品名称', value: row.productName ?? '', type: ShowFormDataType.文本, validate: true },
                { label: '规格型号', value: row.productSpecification ?? '', type: ShowFormDataType.文本, validate: true },
                { label: '技术型号', value: row.productTechnicalCode ?? '', type: ShowFormDataType.文本, validate: true },
            ] as ShowFormData[]

            // 将最后一项的宽度设置撑满一行
            var lastOne = this.infoData.at(-1)
            if (lastOne) {
                lastOne.columnWidth = (this.infoData.length % 4) > 0 ? (5 - (this.infoData.length % 4)) * 25 + "%" : "25%"
            }
            this.$nextTick(() => {
                (this.$refs.inventoryDetailDialog as EasyUI.Dialog)?.open()
            })
        }
    },
})
</script>
<style scoped>
.detail_menu {
    position: fixed;
    z-index: 3000;
}
</style>
