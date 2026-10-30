<template>
    <Loading v-show="isLoading"></Loading>
    <div class="kingzon-page">
        <div class="kingzon-page-title">
            <span>配方管理</span>
        </div>
        <div class="kingzon-page-body">
            <SearchPanel :searchParams="searchParams" :searchParamsHidden="searchParamsHidden" @search="search">
                <template #customer>
                    <div v-if="selectedTab == 1">
                        <CheckBox style="margin-left: 20px" v-model="isOnlyShowOn" @click="refreshTable()">
                        </CheckBox>
                        <span style="margin-left: 5px">仅显示启用记录</span>
                    </div>
                </template>
            </SearchPanel>
            <Tabs :plain="true" style="height:100%" ref="tabs" @tabSelect="tabSelect">
                <TabPanel v-for="(item, index) in tabList" :key="item.value" :title="item.text">
                    <div class="kingzon-page-content">
                        <div class="kingzon-btn-group" style="margin: 5px">
                            <LinkButton v-if="selectedTab == 1" iconCls="icon-add" @click="add()">新增</LinkButton>
                        </div>
                        <KingZonDataGrid :id="`formulaDataGrid_${index}`" :data="formulaList[item.value]"
                            class="kingzon-page-fill" :cellSpan="cellSpan" :ref="'dataGrid_' + item.value"
                            @pageChange="refreshTable" @sortChange="refreshTable"
                            @rowContextMenu="rowContextMenu($event)">
                            <GridColumnGroup align="left" :frozen="true" width="360">
                                <GridHeaderRow>
                                    <GridColumn align="center" cellCss="datagrid-td-rownumber" width="40" field="index">
                                        <template #body="scope">
                                            {{ scope.rowIndex + 1 }}
                                        </template>
                                    </GridColumn>
                                    <GridColumn field="formulaCode" title="配方编码" width="90" align="left"
                                        :editable="true">
                                    </GridColumn>
                                    <GridColumn field="formulaName" title="配方名称" width="230" align="left"
                                        :overflowTextEllipsis="true">
                                    </GridColumn>
                                </GridHeaderRow>
                            </GridColumnGroup>
                            <GridColumnGroup :columnMoving="true" align="center">
                                <GridHeaderRow>
                                    <GridColumn field="version" title="配方版本" width="100" align="left"
                                        :overflowTextEllipsis="true">
                                    </GridColumn>
                                    <GridColumn field="isMain" title="主版本" width="60" align="left">
                                        <template #body="scope">
                                            {{ scope.row.isMain ? "是" : "否" }}
                                        </template>
                                    </GridColumn>
                                    <GridColumn field="status" title="状态" width="80" align="center">
                                        <template #body="scope">
                                            <span v-if="scope.row.status == FormulaStatus.待审核"
                                                class="kingzon-tag kingzon-tag-warning">{{
                                                    FormulaStatus[scope.row.status] }}</span>
                                            <span v-if="scope.row.status == FormulaStatus.已生效" class="kingzon-tag">{{
                                                FormulaStatus[scope.row.status] }}</span>
                                            <span v-if="scope.row.status == FormulaStatus.淘汰"
                                                class="kingzon-tag kingzon-tag-danger">{{
                                                    FormulaStatus[scope.row.status] }}</span>
                                        </template>
                                    </GridColumn>
                                    <GridColumn field="remark" title="版本备注" width="150" align="left"
                                        :overflowTextEllipsis="true">
                                    </GridColumn>
                                    <GridColumn field="productTechnicalCode" title="对应产品型号" width="100" align="left"
                                        :overflowTextEllipsis="true">
                                    </GridColumn>
                                    <GridColumn field="productName" title="对应产品名称" width="150" align="left"
                                        :overflowTextEllipsis="true">
                                    </GridColumn>
                                    <GridColumn field="productRemark" title="对应产品备注" width="150" align="left"
                                        :overflowTextEllipsis="true">
                                    </GridColumn>
                                    <GridColumn field="formulaRemark" title="配方备注" width="150" align="left"
                                        :overflowTextEllipsis="true">
                                    </GridColumn>
                                    <GridColumn v-for="item in customParaList" :key="item.systemPara.id"
                                        :field="'f' + item.systemPara.id.toString()" :title="item.systemPara.dataValue"
                                        width="160" align="left" :overflowTextEllipsis="true">
                                        <template #body=scope>
                                            {{scope.row.formulaCustomAttributeList?.find(x => x.systemParaId ==
                                                item.systemPara.id)?.value ?? ""}}
                                        </template>
                                    </GridColumn>
                                    <GridColumn v-for="item in versioncustomParaList" :key="item.systemPara.id"
                                        :field="'v' + item.systemPara.id.toString()" :title="item.systemPara.dataValue"
                                        width="160" align="left" :overflowTextEllipsis="true">
                                        <template #body=scope>
                                            {{scope.row.versionCustomAttributeList?.find(x => x.systemParaId ==
                                                item.systemPara.id)?.value ?? ""}}
                                        </template>
                                    </GridColumn>
                                    <GridColumn field="createUserName" title="创建人" width="80" align="left"
                                        :overflowTextEllipsis="true">
                                    </GridColumn>
                                    <GridColumn field="createTime" title="创建时间" width="150" align="left"
                                        :sortable="true">
                                        <template #body="scope">
                                            {{ $utils.formatDateTime(scope.row.createTime, "yyyy-MM-dd HH:mm:ss") }}
                                        </template>
                                    </GridColumn>
                                </GridHeaderRow>
                            </GridColumnGroup>
                            <GridColumnGroup :frozen="true" align="right" :width="item.value == 1 ? '290' : '140'">
                                <GridHeaderRow>
                                    <GridColumn v-if="item.value == 1" field="isDeleted" title="是否禁用" align="center"
                                        width="80">
                                        <template #body="scope">
                                            <SwitchButton style="width: 64px; height: 24px"
                                                v-model="scope.row.isDeleted" onText="是" offText="否"
                                                class="Kingzon-Red-Switch" @click="disableFormula(scope.row)">
                                            </SwitchButton>
                                        </template>
                                    </GridColumn>
                                    <GridColumn field="op" title="操作" :width="item.value == 1 ? '210' : '140'"
                                        align=" center">
                                        <template #body="scope">
                                            <ButtonGroup v-if="item.value == 1" style="height:24px">
                                                <LinkButton @click="editFormula(scope.row)">编辑</LinkButton>
                                                <LinkButton @click="onClickCheck(scope.row)"
                                                    v-if="scope.row.formulaStatus == FormulaStatus.待审核 && !scope.row.formulaIsDeleted">
                                                    提交审核
                                                </LinkButton>
                                                <LinkButton @click="onClickCheck(scope.row)">详情</LinkButton>
                                                <LinkButton v-if="scope.row.formulaStatus == FormulaStatus.已生效"
                                                    @click="obsoleteFormula(scope.row)">淘汰
                                                </LinkButton>
                                                <LinkButton @click="delFormula(scope.row)">删除</LinkButton>
                                            </ButtonGroup>
                                            <ButtonGroup v-if="item.value == 2" style="height:24px">
                                                <LinkButton @click="onClickCheck(scope.row)">详情</LinkButton>
                                                <LinkButton @click="recoverFormula(scope.row)">恢复</LinkButton>
                                                <LinkButton @click="delFormula(scope.row)">删除</LinkButton>
                                            </ButtonGroup>
                                        </template>
                                    </GridColumn>
                                </GridHeaderRow>
                            </GridColumnGroup>
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
    <Drawer :drawerStyle="{ width: '95%' }" region="east" ref="addDrawer" :closable="true" :maskClosable="false"
        v-if="showAdd" @collapse="showAdd = false;" title="新增配方">
        <AddFormula @close="closeAdd" @submit="submitAdd">
        </AddFormula>
    </Drawer>
    <Drawer :drawerStyle="{ width: '95%' }" region="east" ref="editDrawer" :closable="true" :maskClosable="false"
        v-if="showEdit" @collapse="showEdit = false;" title="编辑配方">
        <ModifyFormula :formulaId="selectedRow.formulaId" @close="closeEdit" @submit="submitEdit">
        </ModifyFormula>
    </Drawer>
</template>

<script lang="ts">
import { defineComponent } from 'vue';
import { FormulaStatus, SystemParaType, FilterType, DataType } from '@/types/enums'
import { type SelectOption, Search, Filter, type FilterTmp, type UploadFile } from '@/types/types';
import { SystemParaTreeDto } from '@/types/common';
import AddFormula from './create-formula.vue';
import { FormulaIndexDto } from '@/types/technical';
import ModifyFormula from './modify-formula.vue';

export default defineComponent({
    name: 'FormulaManage',
    components: { AddFormula, ModifyFormula },
    data() {
        return {
            isLoading: false,
            tabList: [{ value: 1, text: "当前配方", statusList: [FormulaStatus.待审核, FormulaStatus.已生效] }, { value: 2, text: "淘汰配方", statusList: [FormulaStatus.淘汰] }],
            selectedTab: 1,
            searchParams: [] as Search[],
            searchParamsHidden: [] as Search[],
            isOnlyShowOn: true,
            customParaList: [] as SystemParaTreeDto[],//配方扩展参数
            versioncustomParaList: [] as SystemParaTreeDto[],//版本扩展参数
            showAdd: false,
            addDrawer: {} as EasyUI.Drawer,
            formulaList: {} as any, //配方列表
            filterList: [] as Filter[],
            statusList: [] as SelectOption[],
            contextMenuVisible: false,
            contextMenuStyle: { left: '0px', top: '0px' } as any,
            contextMenuItems: [] as SelectOption[],
            contextMenuRow: {} as FormulaIndexDto,// 用于右键时选中的行
            showEdit: false,
            editDrawer: {} as EasyUI.Drawer,
            selectedRow: {} as FormulaIndexDto
        }
    },
    computed: {
        FormulaStatus() {
            return FormulaStatus;
        }
    },
    created() {
        this.init();
    },
    mounted() {
        this.statusList = this.$utils.getEnumKeyValueArray(FormulaStatus).filter(x => x.value != FormulaStatus.淘汰);
        this.addSearchParam();
        document.addEventListener('pointerdown', this.onDocumentPointerDown, true);
    },
    beforeUnmount() {
        document.removeEventListener('pointerdown', this.onDocumentPointerDown, true);
    },
    methods: {
        init() {
            this.isLoading = true;
            this.getCustomerParamData()
                .then((res: any) => {
                    this.customParaList = res.data;
                    return this.getVersionCustomerParamData()
                })
                .then((res: any) => {
                    this.versioncustomParaList = res.data;
                    return this.getData();
                })
                .then((res: any) => {
                    this.formulaList[this.selectedTab] = res.data;
                    this.formulaList[this.selectedTab].forEach(item => {
                        item.formulaCustomAttributeList = JSON.parse(item.formulaCustomAttribute ?? '[]');
                        item.versionCustomAttributeList = JSON.parse(item.versionCustomAttribute ?? '[]');
                    })
                })
                .catch((err) => {
                    this.$kingzon_messager.showError(err);
                })
                .finally(() => {
                    this.isLoading = false;
                })
        },
        //查询面板
        addSearchParam(): void {
            this.searchParams.push(new Search('formula.Code', '配方编码'));
            this.searchParams.push(new Search('formula.Name', '配方名称'));
            this.searchParams.push(new Search('version', '配方版本'));
            this.searchParams.push(new Search('formula.product.technicalCode', '产品型号'));
            this.searchParamsHidden.push(new Search('status', '状态', "default", FilterType.Equals, DataType.SelectNum, undefined, this.statusList));
            this.searchParamsHidden.push(new Search('remark', '版本备注'));
            this.searchParamsHidden.push(new Search('formula.product.ProductName', '产品名称'));
            this.searchParamsHidden.push(new Search('createUserName', '创建人'));
            this.searchParamsHidden.push(new Search('createTime', "创建时间", "default", FilterType.GreaterThanOrEqual, DataType.DateTime, undefined, undefined, '开始时间'));
            this.searchParamsHidden.push(new Search('createTime', "", "default", FilterType.LessThanOrEqual, DataType.DateTime, undefined, undefined, '结束时间'));

        },
        getCustomerParamData() {
            return this.$http.get('/api/base/SystemPara/GetAdditionalParam', { type: SystemParaType.配方附加参数, });
        },
        getVersionCustomerParamData() {
            return this.$http.get('/api/base/SystemPara/GetAdditionalParam', { type: SystemParaType.配方版本附加参数, });
        },
        getData() {
            if (this.isOnlyShowOn && this.selectedTab == 1) {
                var filter = {
                    key: 'isDeleted',
                    value: false,
                    filterType: FilterType.Equals,
                    dataType: DataType.SelectBoolean
                } as Filter;
                this.filterList = [...this.filterList.filter(x => x.key != 'isDeleted'), filter]
            } else {
                this.filterList = this.filterList.filter(x => x.key != 'isDeleted');
            }
            var existFilterStatus = this.filterList.find(x => x.key == 'status')?.value;
            var tabStatusList = this.tabList.find(x => x.value == this.selectedTab)?.statusList ?? [];
            if (!existFilterStatus || !tabStatusList.includes(existFilterStatus)) {
                var statusFilter = {
                    key: 'status',
                    value: tabStatusList,
                    filterType: FilterType.IntArrayContains,
                    dataType: DataType.multiSelectNum
                } as Filter;
                this.filterList = [...this.filterList.filter(x => x.key != 'status'), statusFilter]
            }
            return this.$http.post("/api/technical/FormulaManage/GetFormulaList", this.filterList, this.$refs['dataGrid_' + this.selectedTab])
        },
        search(params: FilterTmp[]) {
            this.filterList = params.find((item) => item.table == 'default')?.filter ?? [] //单表查询
            this.refreshTable();
        },
        refreshTable() {
            this.isLoading = true;
            this.getData()
                .then((res: any) => {
                    this.formulaList[this.selectedTab] = res.data;
                    this.formulaList[this.selectedTab].forEach(item => {
                        item.formulaCustomAttributeList = JSON.parse(item.formulaCustomAttribute ?? '[]');
                        item.versionCustomAttributeList = JSON.parse(item.versionCustomAttribute ?? '[]');
                    })
                })
                .catch((err) => {
                    this.$kingzon_messager.showError(err);
                })
                .finally(() => {
                    this.isLoading = false;
                })
        },
        tabSelect(tab) {
            var data = this.tabList.find(x => x.text == tab.title);
            this.selectedTab = data!.value;
            //刷新数据
            this.refreshTable();
        },
        add() {
            this.showAdd = true;
            this.$nextTick(() => {
                this.addDrawer = this.$refs.addDrawer as EasyUI.Drawer;
                this.addDrawer.expand();
            })
        },
        closeAdd() {
            this.addDrawer.collapse();
        },
        submitAdd() {
            this.addDrawer.collapse();
            //刷新数据
            this.refreshTable();
        },
        cellSpan(data: any) {
            var cellSpanColumns = ['formulaCode', 'formulaName', 'productTechnicalCode', 'productName', 'productRemark', 'formulaRemark', 'op'];
            if (cellSpanColumns.includes(data.column.field)) {
                var rowIndex = data.rowIndex;
                return this.$utils.getCellSpanMultiKey([['formulaId']], data.row, rowIndex, this.formulaList[this.selectedTab]);
            }
            return { rowspan: 1, colspan: 1 };
        },
        //右键自定义菜单（订单明细操作）
        rowContextMenu(event) {
            this.contextMenuRow = event.row as FormulaIndexDto;
            var pointerEvent = event.originalEvent as PointerEvent;

            pointerEvent.preventDefault();
            //待审核状态
            if (this.contextMenuRow.status == FormulaStatus.待审核) {
                this.contextMenuItems = [
                    { text: '提交审核', value: 'check' },
                    { text: '删除', value: 'delete' },
                ];
            }
            //生效状态
            if (this.contextMenuRow.status == FormulaStatus.已生效) {
                this.contextMenuItems = [
                    { text: '删除', value: 'delete' },
                ];
            }
            //淘汰
            if (this.contextMenuRow.status == FormulaStatus.淘汰) {
                this.contextMenuItems = [
                    { text: '删除', value: 'delete' },
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
        // 隐藏右键菜单
        hideContextMenu() {
            this.contextMenuVisible = false;
        },
        // 右键菜单点击事件
        async onContextMenuClick(item: SelectOption) {
            this.contextMenuVisible = false;
            switch (item.value) {
                case 'check':
                    if (this.contextMenuRow) this.onClickCheck(this.contextMenuRow);
                    break;
                case 'delete':
                    if (this.contextMenuRow) await this.deleteRow(this.contextMenuRow);
                    break;
            }
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
        async deleteRow(row) {
            await this.$kingzon_messager.showConfim("是否删除配方‘" + row.formulaCode + row.formulaName + "’的" + row.version + "版本?")
                .then(() => {
                    this.isLoading = true;
                    return this.$http.post("/api/technical/FormulaManage/DeleteFormulaVersion", { id: row.versionId })
                }).then((res: any) => {
                    this.$kingzon_messager.showInfo(res.data);
                })
                .catch((err) => {
                    if (typeof err === 'string') {
                        this.$kingzon_messager.showError(err);
                    } else { console.log(err); }
                })
                .finally(() => {
                    this.isLoading = false;
                })

            this.refreshTable();
        },
        onClickCheck(row) {
            this.$kingzon_messager.showInfo("功能开发中")
        },
        editFormula(row) {
            this.selectedRow = row;
            this.showEdit = true;
            this.$nextTick(() => {
                this.editDrawer = this.$refs.editDrawer as EasyUI.Drawer;
                this.editDrawer.expand();
            })
        },
        closeEdit() {
            this.editDrawer.collapse();
        },
        submitEdit() {
            this.editDrawer.collapse();
            //刷新数据
            this.refreshTable();
        },
        async delFormula(row) {
            await this.$kingzon_messager.showConfim("是否删除配方‘" + row.formulaCode + row.formulaName + "’?")
                .then(() => {
                    this.isLoading = true;
                    return this.$http.post("/api/technical/FormulaManage/DeleteFormula", { id: row.formulaId })
                }).then((res: any) => {
                    this.$kingzon_messager.showInfo(res.data);
                })
                .catch((err) => {
                    if (typeof err === 'string') {
                        this.$kingzon_messager.showError(err);
                    } else { console.log(err); }
                })
                .finally(() => {
                    this.isLoading = false;
                })

            this.refreshTable();
        },
        async obsoleteFormula(row) {
            await this.$kingzon_messager.showConfim("是否淘汰配方‘" + row.formulaCode + row.formulaName + "’?")
                .then(() => {
                    this.isLoading = true;
                    return this.$http.post("/api/technical/FormulaManage/ObsoleteFormula", { id: row.formulaId })
                }).then((res: any) => {
                    this.$kingzon_messager.showInfo(res.data);
                })
                .catch((err) => {
                    if (typeof err === 'string') {
                        this.$kingzon_messager.showError(err);
                    } else { console.log(err); }
                })
                .finally(() => {
                    this.isLoading = false;
                })

            this.refreshTable();
        },
        async recoverFormula(row) {
            await this.$kingzon_messager.showConfim("是否恢复配方‘" + row.formulaCode + row.formulaName + "’?")
                .then(() => {
                    this.isLoading = true;
                    return this.$http.post("/api/technical/FormulaManage/RecoverFormula", { id: row.formulaId })
                }).then((res: any) => {
                    this.$kingzon_messager.showInfo(res.data);
                })
                .catch((err) => {
                    if (typeof err === 'string') {
                        this.$kingzon_messager.showError(err);
                    } else { console.log(err); }
                })
                .finally(() => {
                    this.isLoading = false;
                })

            this.refreshTable();
        },
        async disableFormula(row) {
            this.isLoading = true;
            await this.$http.post("/api/technical/FormulaManage/DisabledFormulaVersion", { id: row.versionId })
                .then((res: any) => {
                    this.$kingzon_messager.showInfo(res.data);
                })
                .catch((err) => {
                    this.$kingzon_messager.showError(err);
                })
                .finally(() => {
                    this.isLoading = false;
                })

            this.refreshTable();
        }
    }
})

</script>

<style scoped>
:deep(.menu-inline) {
    position: fixed;
    z-index: 9999;
}
</style>