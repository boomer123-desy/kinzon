<template>
    <Loading v-show="isLoading"></Loading>
    <div class="kingzon-page-content">
        <div class="kingzon-btn-group" style="margin: 4px;">
            <LinkButton class="c1" @click="selectMaterial()">选择物料</LinkButton>
        </div>
        <div style="flex: 1;display: flex;flex-direction: row;">
            <div style="flex:1;width: 0;">
                <MaterialDataGrid v-if="areaInfo" ref="dataGrid" :areaInfo="areaInfo" :initData="false"
                    :materialList="dataList" :showPage="false" :showSort="false" :showSelection="false"
                    :rightGroupRowWidth="715" @rowClick="rowClick">
                    <template #operate>
                        <GridColumn field="status" title="状态" width="60px" align="left" cellCss="kingzon-cell-editable">
                            <template #body="scope">
                                {{ scope.row.dataStatus ? InventoryDataStatus[scope.row.dataStatus] : '' }}
                            </template>
                        </GridColumn>
                        <GridColumn field="supplierName" title="供应商" width="80px" align="left"
                            cellCss="kingzon-cell-editable">
                        </GridColumn>
                        <GridColumn field="inboundQuantity" title="入库数量" width="100px" align="right"
                            cellCss="kingzon-cell-editable">
                            <template #body="scope">
                                <div style="display: flex;flex-direction: column;">
                                    <span v-for="unit in scope.row.unitQtyList" :key="unit.id">{{ unit.quantity }}{{
                                        unit.unitName }}</span>
                                </div>
                            </template>
                        </GridColumn>
                        <GridColumn field="positionInfo" title="库位库区" width="80px" align="left"
                            cellCss="kingzon-cell-editable">
                        </GridColumn>
                        <GridColumn field="inboundPrice" title="单价" width="75px" align="right"
                            cellCss="kingzon-cell-editable">
                            <template #body="scope">
                                <div style="display: flex;flex-direction: column;">
                                    <span v-for="unit in scope.row.unitQtyList" :key="unit.id">{{ unit.price ?
                                        unit.price + '元/' + unit.unitName : '' }}
                                    </span>
                                </div>
                            </template>
                        </GridColumn>
                        <GridColumn v-if="areaInfo.showMaterialCost" field="inboundMaterialCost" title="成本" width="75px"
                            align="right" cellCss="kingzon-cell-editable">
                            <template #body="scope">
                                <div style="display: flex;flex-direction: column;">
                                    <span v-for="unit in scope.row.unitQtyList" :key="unit.id">{{ unit.materialCost ?
                                        unit.materialCost + '元/' + unit.unitName : '' }}
                                    </span>
                                </div>
                            </template>
                        </GridColumn>
                        <GridColumn v-if="areaInfo.showProcessingCost" field="inboundProcessingCost" title="加工费"
                            width="75px" align="right" cellCss="kingzon-cell-editable">
                            <template #body="scope">
                                <div style="display: flex;flex-direction: column;">
                                    <span v-for="unit in scope.row.unitQtyList" :key="unit.id">{{ unit.processingCost ?
                                        unit.processingCost + '元/' + unit.unitName : '' }}
                                    </span>
                                </div>
                            </template>
                        </GridColumn>
                        <GridColumn field="otherInfo" title="其它信息" width="80px" align="left"
                            cellCss="kingzon-cell-editable">
                        </GridColumn>
                        <GridColumn title="操作" width="90" align="center">
                            <template #body="scope">
                                <ButtonGroup style="height:24px">
                                    <LinkButton @click="copyRow(scope.row)">复制</LinkButton>
                                    <LinkButton @click="deleteRow(scope.row)">删除</LinkButton>
                                </ButtonGroup>
                            </template>
                        </GridColumn>
                    </template>
                </MaterialDataGrid>
            </div>
            <Panel style=" width: 500px;">
                <template #header>
                    <div style="line-height:21px; align-self: center;">入库信息填写</div>
                </template>
                <Inbound ref="inbound" v-if="selectedRow" :areaId="areaId" :dataType="selectedRow.dataType"
                    :dataId="selectedRow.dataId!" :showSupplier="true" displayMode="small" :showStatus="true"
                    :showCost="true" :inboundList="cloneDeep(selectedRow.inboundList)"
                    :dataStatus="selectedRow.dataStatus" :supplierId="selectedRow.supplierId" :showAddMultiple="false"
                    :showAddUnit="true" @confirm="inboundConfirm" closeText="清空" @close="inboundClear()">
                    <template #showForm>
                        <KingZonShowForm :formData="formData" columnWidth="50%">
                        </KingZonShowForm>
                    </template>
                </Inbound>
            </Panel>
        </div>
        <div class="kingzon-btn-group kingzon-page-footer">
            <LinkButton @click="close">取消</LinkButton>
            <LinkButton btnCls="c8" @click="confirm">确定</LinkButton>
        </div>
    </div>
    <Drawer v-if="isSelectMaterial" ref="selectMaterialDrawer" title="选择物资" :drawerStyle="{ width: '98%' }"
        region="east" :closable="true" :maskClosable="false" @collapse="isSelectMaterial = false">
        <SelectMaterial :areaId="areaId" @close="selectMaterialDrawer.collapse()" @confirm="selectMaterialConfirm"
            confirmText="确定" :selectedMaterialList="selectedMaterialList">
        </SelectMaterial>
    </Drawer>
</template>

<script lang="ts">
import { defineComponent } from 'vue';
import SelectMaterial from './select-material.vue';
import MaterialDataGrid from './material-dataGrid.vue';
import { AreaInfoDto, AvailableMaterialDto, InboundDto, InventoryUnitOutboundDto } from "@/types/inventory";
import Inbound from "./inbound.vue"
import { InventoryDataStatus, InventoryDataType } from "@/types/enums";
import { cloneDeep } from 'lodash';
import { type SelectOption, CustomAttribute, ShowFormData } from '@/types/types'


export default defineComponent({
    name: 'MaterialInbound',
    emits: ['close', 'confirm'],
    components: {
        SelectMaterial, MaterialDataGrid, Inbound
    },
    props: {
        //显示的物料列表
        materialList: {
            type: Array as () => AvailableMaterialDto[],
            default: () => []
        },
        areaId: {
            type: Number,
            required: false,
        },
    },
    computed: {
        InventoryDataStatus() {
            return InventoryDataStatus;
        },
        cloneDeep() {
            return cloneDeep;
        },
        selectedMaterialList(): AvailableMaterialDto[] {
            var data = cloneDeep(this.dataList);
            //去重
            var map = new Map();
            data.forEach(item => {
                if (!map.has(item.dataType + "-" + item.dataId)) {
                    map.set(item.dataType + "-" + item.dataId, item);
                }
            });
            return Array.from(map.values());
        }
    },
    watch: {
        selectedRow(newVal, oldVal) {
            if (newVal) {
                this.formData = [
                    new ShowFormData("物料编码", newVal.materialNo ?? ""),
                    new ShowFormData("物料名称", newVal.materialName ?? ""),
                    new ShowFormData("技术型号", newVal.materialTechnicalCode ?? ""),
                    new ShowFormData("规格型号", newVal.materialSpecification ?? ""),
                    new ShowFormData("物料类别", newVal.materialCategory ?? ""),
                    new ShowFormData("材质牌号", newVal.materialGrade ?? ""),
                    new ShowFormData("单位", newVal.materialUnit ?? ""),
                    new ShowFormData("最近采购单价", newVal.price ?? "")];
            } else {
                this.formData = [];
            }
        }
    },
    data() {
        return {
            isLoading: false,
            isSelectMaterial: false,
            selectMaterialDrawer: {} as EasyUI.Drawer,
            areaInfo: undefined as AreaInfoDto | undefined,
            selectedRow: undefined as AvailableMaterialDto | undefined,
            formData: [] as ShowFormData[],
            dataList: [] as AvailableMaterialDto[],
        }
    },
    created() {
        this.init();
    },
    methods: {
        init() {
            this.dataList = cloneDeep(this.materialList);
            this.selectedRow = this.dataList.length > 0 ? this.dataList[0] : undefined;
            this.isLoading = true;
            this.getAreaData()
                .then(res => {
                    this.areaInfo = res.data as AreaInfoDto;
                }).catch((err) => {
                    this.$kingzon_messager.showError(err);
                }).finally(() => {
                    this.isLoading = false;
                });
        },
        getAreaData() {
            if (!this.areaId) {
                return Promise.resolve({ data: new AreaInfoDto() });
            }
            return this.$http.get("/api/inventory/InventoryManagement/GetAreaInfo", {
                areaId: this.areaId,
            });
        },
        rowClick(row: AvailableMaterialDto) {
            this.selectedRow = row;
            console.log(row)
            //刷新 inbound组件
            var inbound = this.$refs.inbound as InstanceType<typeof Inbound>;
            if (inbound) {
                inbound.refreshData();
            }
        },
        inboundConfirm(data: { dataList: InboundDto[], dataStatus?: InventoryDataStatus, supplierId?: number, supplierName?: string }) {
            if (!this.selectedRow) {
                return;
            }
            var index = this.dataList.findIndex(x => x == this.selectedRow);
            var nextRow = index < this.dataList.length - 1 ? this.dataList[index + 1] : undefined;
            this.selectedRow.dataStatus = data.dataStatus ?? InventoryDataStatus.None;
            this.selectedRow.supplierId = data.supplierId;
            this.selectedRow.supplierName = data.supplierName;
            this.selectedRow.inboundList = cloneDeep(data.dataList);
            this.selectedRow.unitQtyList = this.selectedRow.inboundList[0].unitQtyList;
            this.selectedRow.positionInfo = this.selectedRow.inboundList[0].positionName;
            this.selectedRow.otherInfo = (this.selectedRow.inboundList[0].customAttributeShowList?.map(x => x.name + ": " + x.value).join(",") ?? "") +
                (this.selectedRow.inboundList[0].remark ? (this.selectedRow.inboundList[0].customAttributeShowList?.length ?? 0 > 0 ? "。\n" : "") + "备注: " + this.selectedRow.inboundList[0].remark : "");

            //自动选中下一行
            var materialDataGrid = this.$refs.dataGrid as InstanceType<typeof MaterialDataGrid>;
            if (materialDataGrid) {
                if (nextRow) {
                    this.selectedRow = nextRow;
                    materialDataGrid.selectRow(nextRow);
                    //刷新 inbound组件
                    var inbound = this.$refs.inbound as InstanceType<typeof Inbound>;
                    if (inbound) {
                        inbound.refreshData();
                    }
                } else {
                    materialDataGrid.unSelectRow(this.selectedRow);
                    this.selectedRow = undefined;
                }
            }
        },
        inboundClear() {
            var inbound = this.$refs.inbound as InstanceType<typeof Inbound>;
            if (inbound) {
                inbound.clearData();
                inbound.addInbound();
            }
        },
        //复制行
        copyRow(row: AvailableMaterialDto) {
            var index = this.dataList.findIndex(x => x == row);
            if (index >= 0) {
                var newRow = cloneDeep(row);
                newRow.idField = newRow.idField + "-copy-" + new Date().getTime();
                this.dataList.splice(index + 1, 0, newRow);
            }
        },
        // 删除行
        deleteRow(row: AvailableMaterialDto) {
            var index = this.dataList.findIndex(x => x == row);
            if (index >= 0) {
                this.dataList.splice(index, 1);
                if (this.selectedRow == row) {
                    this.selectedRow = undefined;
                }
            }
        },
        selectMaterial() {
            this.isSelectMaterial = true;
            this.$nextTick(() => {
                this.selectMaterialDrawer = this.$refs.selectMaterialDrawer as EasyUI.Drawer;
                this.selectMaterialDrawer.expand();
            });
        },
        selectMaterialConfirm(materialList: AvailableMaterialDto[]) {
            const getKey = (item: { dataType: InventoryDataType; dataId: number | undefined }) => `${item.dataType}_${item.dataId}`;
            const selectedKeys = new Set(materialList.map(x => getKey({ dataType: x.dataType, dataId: x.dataId })));
            const currentKeys = new Set(this.dataList.map(x => getKey({ dataType: x.dataType, dataId: x.dataId })));
            // 新增
            const newList = materialList.filter(item => !currentKeys.has(getKey({ dataType: item.dataType, dataId: item.dataId })));
            this.dataList.push(...cloneDeep(newList));
            // 删除
            this.dataList = this.dataList.filter(item => selectedKeys.has(getKey({ dataType: item.dataType, dataId: item.dataId })));
            // 当前选中行是否被删除
            if (this.selectedRow && !selectedKeys.has(getKey({ dataType: this.selectedRow.dataType, dataId: this.selectedRow.dataId }))) {
                var materialDataGrid = this.$refs.dataGrid as InstanceType<typeof MaterialDataGrid>;
                if (materialDataGrid) {
                    var firstRow = this.dataList.length > 0 ? this.dataList[0] : undefined;
                    if (firstRow) {
                        this.selectedRow = firstRow;
                        materialDataGrid.selectRow(firstRow);
                        //刷新 inbound组件
                        var inbound = this.$refs.inbound as InstanceType<typeof Inbound>;
                        if (inbound) {
                            inbound.refreshData();
                        }
                    } else {
                        materialDataGrid.unSelectRow(this.selectedRow);
                        this.selectedRow = undefined;
                    }
                }
            }
            this.selectMaterialDrawer.collapse();

        },
        close() {
            this.$emit("close");
        },
        confirm() {
            this.isLoading = true;
            var postData = this.dataList.filter(x => (x.inboundList?.length ?? 0) > 0);
            this.$http.post("/api/inventory/InventoryManagement/InboundNewMaterialList", postData)
                .then(res => {
                    this.$kingzon_messager.showInfo(res.data as any as string);
                    this.$emit("confirm");
                }).catch((err) => {
                    this.$kingzon_messager.showError(err);
                }).finally(() => {
                    this.isLoading = false;
                });
        }
    }
})
</script>
<style scoped>
:deep(.datagrid-cell) {
    white-space: pre-wrap;
}
</style>