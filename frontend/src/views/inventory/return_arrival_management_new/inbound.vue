<template>
    <Loading v-show="isLoading"></Loading>
    <div class="kingzon-page-content">
        <slot name="showForm">
            <KingZonShowForm :formData="formData" :columnWidth="displayMode == 'large' ? '20%' : '50%'">
            </KingZonShowForm>
        </slot>
        <div v-if="showStatus || showSupplier" style="display: flex;margin: 10px 25px 5px 10px"
            :style="displayMode == 'large' ? { flexDirection: 'row' } : { flexDirection: 'column' }">
            <div v-if="showStatus" class="field" :style="displayMode == 'large' ? {} : { marginBottom: '5px' }">
                <span class=" label">物料状态：</span>
                <ComboBox style="flex: 1;" v-model="newStatus" :data="statusOptions">
                </ComboBox>
            </div>
            <div v-if="showSupplier" class="field" style="flex: 2.5;">
                <span class=" label">供应商：</span>
                <ComboBox style="flex: 1;" v-model="newSupplier" :data="supplierOptions" @valueChange="supplierChange">
                </ComboBox>
            </div>
        </div>
        <div class="kingzon-page-fill" style="display: flex;flex-direction: column;padding: 5px 10px;">
            <div v-for="record in dataList" :key="record.index">
                <div style="background-color: var(--color-blue-bg);padding: 15px 0 5px;">
                    <div v-for="item in record.unitQtyList" :key="item.id" class="unit-row"
                        :class="{ small: displayMode !== 'large' }">
                        <div v-if="displayMode == 'large'" class="field">
                            <span class="label">入库量：</span>
                            <NumberBox class="box" v-model="item.quantity" :min="0" :precision="4"
                                @valueChange="handleUnitQty(record, item)" @focus="unitInputChange(record, item)">
                                <Addon>
                                    <span class="addon">{{ item.unitName }}</span>
                                </Addon>
                            </NumberBox>
                            <div v-if="item.id != -1"
                                style="display: flex;flex-direction: column;text-align: right;font-size: 12px;line-height: 14px;color:#666;">
                                <span>【{{ new Decimal(1).div(item.ratio).toDecimalPlaces(4) }}{{ item.unitName }}/{{
                                    materialInfo.dataUnit }}】</span>
                                <span>【{{ new Decimal(item.ratio).toDecimalPlaces(4) }}{{ materialInfo.dataUnit }}/{{
                                    item.unitName }}】</span>
                            </div>
                        </div>
                        <div v-if="displayMode == 'small'" class="field"
                            style="flex-direction: column;align-items: start;">
                            <div style="display: flex; flex-direction: row; align-items: center;width: 100%;">
                                <span class="label">入库量：</span>
                                <NumberBox style="flex:1" v-model="item.quantity" :min="0" :precision="4"
                                    @valueChange="handleUnitQty(record, item)" @focus="unitInputChange(record, item)">
                                    <Addon>
                                        <span class="addon">{{ item.unitName }}</span>
                                    </Addon>
                                </NumberBox>
                            </div>
                            <div v-if="item.id != -1"
                                style="width: 100%;display: flex;flex-direction: row; font-size: 12px;line-height: 14px;justify-content:end">
                                <span>【{{ new Decimal(1).div(item.ratio).toDecimalPlaces(4) }}{{ item.unitName }}/{{
                                    materialInfo.dataUnit }}】</span>
                                <span>【{{ new Decimal(item.ratio).toDecimalPlaces(4) }}{{ materialInfo.dataUnit }}/{{
                                    item.unitName }}】</span>
                            </div>
                        </div>
                        <div class="field" v-if="showPrice">
                            <span class="label" style="width: 70px;">单价：</span>
                            <NumberBox class="box" v-model="item.price" :min="0" :precision="4"
                                @valueChange="handleUnitQty(record, item)" @focus="unitInputChange(record, item)"
                                :disabled="priceDisabled">
                                <Addon>
                                    <span class="addon">元/{{ item.unitName }}</span>
                                </Addon>
                            </NumberBox>
                        </div>
                        <div class="field" v-if="areaInfo.showMaterialCost && showCost" style="flex: 0.75;">
                            <span class="label">成本：</span>
                            <NumberBox class="box" v-model="item.materialCost" :min="0" :precision="4"
                                @valueChange="handleUnitQty(record, item)" @focus="unitInputChange(record, item)">
                                <Addon>
                                    <span class="addon">元/{{ item.unitName }}</span>
                                </Addon>
                            </NumberBox>
                        </div>
                        <div class="field" v-if="areaInfo.showProcessingCost && showCost" style="flex: 0.75;">
                            <span class="label" style="width: 70px;">加工费：</span>
                            <NumberBox class="box" v-model="item.processingCost" :min="0" :precision="4"
                                @valueChange="handleUnitQty(record, item)" @focus="unitInputChange(record, item)">
                                <Addon>
                                    <span class="addon">元/{{ item.unitName }}</span>
                                </Addon>
                            </NumberBox>
                        </div>
                    </div>
                </div>
                <div class="field" style="margin-bottom: 10px;">
                    <span class=" label">库位库区：</span>
                    <TextBox class="box" :disabled="true" v-model="record.positionName"></TextBox>
                    <font-awesome-icon :icon="['fas', 'warehouse']" style="margin:0 5px;color:var(--color-blue)"
                        class="kingzon-pointer" @click="clickPositon(record)" />
                </div>
                <slot name="extend" :row="record">
                    <div v-if="showExtend && (purchaseParaList.length || productionParaList.length)" class="field"
                        style="margin-bottom: 10px;align-items: flex-start;">
                        <span class=" label" style="height: 30px;line-height: 30px;">额外信息：</span>
                        <div class="box" style="display: flex;flex-direction:column;">
                            <div style="display: flex;flex-direction: row;align-items: center;">
                                <div>
                                    <RadioButton name="group" value="0" v-model="record.selectedExtend">
                                    </RadioButton>
                                    <Label style="margin-left: 5px;">无</Label>
                                </div>
                                <div v-if="purchaseParaList.length">
                                    <RadioButton name="group" value="1" v-model="record.selectedExtend">
                                    </RadioButton>
                                    <Label style="margin-left: 5px;">外购信息</Label>
                                </div>
                                <div v-if="productionParaList.length">
                                    <RadioButton name="group" value="2" v-model="record.selectedExtend">
                                    </RadioButton>
                                    <Label style="margin-left: 5px;">生产信息</Label>
                                </div>
                            </div>
                            <div v-if="record.selectedExtend == '1'"
                                style="display: flex;flex-direction: row;flex-wrap: wrap;">
                                <div v-for="para in purchaseParaList" :key="para.systemPara.id" class="field"
                                    style="width: 50%;flex: none;margin-top: 5px;"
                                    :style="displayMode == 'large' ? { width: '50%' } : { width: '100%' }">
                                    <span class="label" style="flex-shrink: 0;">{{ para.systemPara.dataValue }}：</span>
                                    <MutiDisplayTypeInput :para="para" @update:value="para.systemPara.value = $event">
                                    </MutiDisplayTypeInput>
                                </div>
                            </div>
                            <div v-if="record.selectedExtend == '2'"
                                style="display: flex;flex-direction: row;flex-wrap: wrap;">
                                <div v-for="para in productionParaList" :key="para.systemPara.id" class="field"
                                    style="width: 50%;flex: none;margin-top: 5px;"
                                    :style="displayMode == 'large' ? { width: '50%' } : { width: '100%' }">
                                    <span class="label" style="flex-shrink: 0;">{{ para.systemPara.dataValue }}：</span>
                                    <MutiDisplayTypeInput :para="para" @update:value="para.systemPara.value = $event">
                                    </MutiDisplayTypeInput>
                                </div>
                            </div>
                        </div>
                    </div>
                </slot>
                <div class="field" style="margin-bottom: 10px;">
                    <span class=" label">备注：</span>
                    <TextBox class="box" :multiline="true" v-model="record.remark"></TextBox>
                    <LinkButton plain iconCls="icon-remove" style="align-self: center;" @click="delInbound(record)"
                        v-if="showAddMultiple">
                        删除</LinkButton>

                </div>
            </div>
            <LinkButton plain iconCls="icon-add" style="width:200px;align-self: center;flex-shrink:0"
                @click="addInbound()" v-if="showAddMultiple">新增
            </LinkButton>
        </div>
        <div class="kingzon-btn-group kingzon-page-footer">
            <LinkButton @click="close">{{ closeText }}</LinkButton>
            <LinkButton btnCls="c8" @click="confirm">确定</LinkButton>
        </div>
    </div>
    <Dialog ref="SelectPosition" title="选择库位" :modal="true" closed :draggable="true"
        :dialogStyle="{ height: '85%', width: '85%' }">
        <SelectPosition :dataType="dataType" :dataId="dataId" :positionId="selectedRow.positionId"
            :supplierId="supplierId" :dataStatus="dataStatus" @close="selectPositionDialog.close()"
            @confirm="selectPositionConfirm"></SelectPosition>
    </Dialog>
</template>

<script lang="ts">
import { defineComponent } from 'vue';
import { AreaInfoDto, InboundDto, InventoryUnitDto, InventoryUnitInboundDto, StorageLocation_PositionDTO } from "@/types/inventory";
import { DataInfo, Base_Partner, SystemParaTreeDto } from "@/types/common";
import { type SelectOption, CustomAttribute, ShowFormData, CustomAttributeShowDto } from '@/types/types'
import Decimal from 'decimal.js';
import { InventoryDataType, InventoryDataStatus, SystemParaType } from "@/types/enums";
import SelectPosition from './select-position.vue'
import { cloneDeep } from 'lodash';


export default defineComponent({
    name: 'Inbound',
    emits: ["close", "confirm", "addInbound"],
    components: { SelectPosition },
    props: {
        //默认入库库房
        areaId: {
            type: Number,
            required: false,
        },
        //默认入库库位
        positionId: {
            type: Number,
            required: false,
        },
        dataType: {
            type: Number as () => InventoryDataType,
            required: true,
        },
        dataId: {
            type: Number,
            required: true,
        },
        supplierId: {
            type: Number,
            required: false,
        },
        dataStatus: {
            type: Number as () => InventoryDataStatus,
            required: false,
        },
        //采购单位、生产单位等 可填写入库数量的单位
        inboundUnitList: {
            type: Array as () => InventoryUnitDto[],
            required: false,
        },
        //待入库数量，获取默认库位时会用到
        inboundQuantity: {
            type: Number,
            required: false,
        },
        //编辑时传入
        inboundList: {
            type: Array as () => InboundDto[],
            required: false,
        },
        //KingZonShowForm 显示库房名称和库存
        showAreaInfo: {
            type: Boolean,
            required: false,
            default: true
        },
        //可录入成本数据 直接入库、盘点、合并
        showCost: {
            type: Boolean,
            required: false,
            default: false
        },
        //可录入 采购、生产额外信息
        showExtend: {
            type: Boolean,
            required: false,
            default: true
        },
        //可选择物料状态
        showStatus: {
            type: Boolean,
            required: false,
            default: false
        },
        //可选择供应商
        showSupplier: {
            type: Boolean,
            required: false,
            default: false
        },
        //可新增多条入库
        showAddMultiple: {
            type: Boolean,
            default: true
        },
        //可录入单价
        showPrice: {
            type: Boolean,
            default: true
        },
        displayMode: {
            type: String as () => "small" | "large",
            required: false,
            default: "large"
        },
        closeText: {
            type: String,
            required: false,
            default: "取消"
        },
        priceDisabled: {
            type: Boolean,
            default: false
        },
        //显示在入库单位的默认单位id，排列在第一行
        defUnitId: {
            type: Number,
            required: false
        }
    },
    computed: {
        Decimal() {
            return Decimal;
        }
    },
    created() {
        this.init();
    },
    mounted() {
        this.selectPositionDialog = this.$refs.SelectPosition as EasyUI.Dialog;
    },
    data() {
        return {
            isLoading: false,
            areaInfo: {} as AreaInfoDto,
            materialInfo: new DataInfo(),
            supplierInfo: {} as Base_Partner,
            unitInfo: [] as InventoryUnitDto[], //物料的可入库单位
            defaultPosition: {} as { id: number, name: string },
            formData: [] as ShowFormData[],
            dataList: [] as InboundDto[],
            purchaseParaList: [] as Array<SystemParaTreeDto>,//扩展参数
            productionParaList: [] as Array<SystemParaTreeDto>,//扩展参数
            selectPositionDialog: {} as EasyUI.Dialog,
            selectedRow: {} as InboundDto,
            selectedUnit: {} as InventoryUnitInboundDto,
            maxIndex: 0,
            statusOptions: [] as SelectOption[],
            supplierOptions: [] as SelectOption[],
            newStatus: undefined as InventoryDataStatus | undefined,
            newSupplier: undefined as number | undefined
        }
    },
    methods: {
        init() {
            if (this.dataStatus) {
                this.newStatus = this.dataStatus;
            }
            if (this.supplierId) {
                this.newSupplier = this.supplierId;
            }
            if (this.inboundList && this.inboundList.length > 0) {
                this.dataList = this.dataList.concat(cloneDeep(this.inboundList));
            }
            this.maxIndex = Math.max(0, ...(this.dataList?.map(x => x.index) ?? []));
            if (!this.dataList.length) {
                var newInbound = new InboundDto({ index: ++this.maxIndex });
                this.dataList.push(newInbound);
            }
            if (this.showStatus) {
                this.statusOptions = this.$utils.getEnumKeyValueArray(InventoryDataStatus).filter(x => x.value != InventoryDataStatus.None)
            }
            this.isLoading = true;
            this.getAreaData()
                .then(res => {
                    this.areaInfo = res.data as AreaInfoDto;
                    return this.getData();
                }).then(res => {
                    var data = res.data as any;
                    this.materialInfo = data.materialData;
                    this.supplierInfo = data.supplierData;
                    if (this.materialInfo) {
                        // console.log("this.materialInfo", this.materialInfo)
                        this.formData = [
                            new ShowFormData("物料编码", this.materialInfo.dataNo ?? ""),
                            new ShowFormData("物料名称", this.materialInfo.dataName ?? ""),
                            new ShowFormData("技术型号", this.materialInfo.dataTechnicalCode ?? ""),
                            new ShowFormData("规格型号", this.materialInfo.dataSpecification ?? ""),
                            new ShowFormData("材质牌号", this.materialInfo.dataMaterialGrade ?? ""),
                            new ShowFormData("物料类别", this.materialInfo.dataCategory ?? ""),
                        ]
                        if (!this.showStatus) {
                            this.formData = this.formData.concat([
                                new ShowFormData("状态", this.dataStatus ? InventoryDataStatus[this.dataStatus] : "")]);
                        }
                        if (!this.showSupplier) {
                            this.formData = this.formData.concat([new ShowFormData("供应商", this.supplierInfo?.customerName ?? "")]);
                        }
                        if (this.areaId && this.showAreaInfo) {
                            this.formData = this.formData.concat([
                                new ShowFormData("库房", this.areaInfo.code ? this.areaInfo.code + "-" + this.areaInfo.name : this.areaInfo.name),
                                new ShowFormData("本库库存", data.areaStockQty ? data.areaStockQty + " " + this.materialInfo.dataUnit : "")])
                        }
                        //将最后一项的宽度设置撑满一行
                        var lastOne = this.formData.at(-1);
                        if (lastOne) {
                            var columnCount = this.displayMode == 'large' ? 5 : 2;
                            lastOne.columnWidth = (this.formData.length % columnCount) > 0 ? (5 - this.formData.length % columnCount + 1) * (this.displayMode == 'large' ? 20 : 50) + "%" : (this.displayMode == 'large' ? "20%" : "50%");
                        }
                    }
                    this.unitInfo = data.materialUnitData;
                    if (this.inboundUnitList?.length) {
                        this.unitInfo = Array.from(
                            new Map(
                                [...this.unitInfo, ...this.inboundUnitList]
                                    .map(unit => [unit.id, unit])
                            ).values()
                        );
                    }
                    if (this.defUnitId) {
                        let defUnit = undefined as InventoryUnitDto | undefined;
                        // 遍历时顺手把目标项捞出来，剩下的自然就是其他项
                        const otherUnits = this.unitInfo.filter(x => {
                            if (x.id == this.defUnitId) {
                                defUnit = x;
                                return false;
                            }
                            return true;
                        });
                        if (defUnit) {
                            this.unitInfo = [defUnit, ...otherUnits];
                        }
                    }
                    return this.getExtendParamData()
                }).then(res => {
                    var data = res.data as any;
                    this.purchaseParaList = data.filter(x => x.systemPara.dataType == SystemParaType.外购物料信息);
                    this.productionParaList = data.filter(x => x.systemPara.dataType == SystemParaType.生产物料信息);
                    return this.getPositionData();
                }).then(res => {
                    var data = res.data as any;
                    this.defaultPosition = {
                        id: data.positionId,
                        name: data.positionInfo
                    };
                    this.dataList.forEach(item => {
                        item.index = item.index === undefined || item.index === null ? ++this.maxIndex : item.index;
                        if (item.unitQtyList.length == 0) {
                            item.unitQtyList = cloneDeep(this.unitInfo);
                        }
                        if (!item.positionId) {
                            item.positionId = cloneDeep(this.defaultPosition.id);
                            item.positionName = cloneDeep(this.defaultPosition.name);
                        }

                        this.$emit('addInbound', item)

                        this.handleInblundAttributeChange(item)
                    })
                    if (this.showSupplier) {
                        return this.getSupplierData()
                    }
                })
                .catch((err) => {
                    this.$kingzon_messager.showError(err);
                }).finally(() => {
                    this.isLoading = false;
                });
        },
        handleInblundAttributeChange(item: InboundDto) {
            if (item.purchaseAttributeList) {
                item.selectedExtend = "1";
                this.purchaseParaList.forEach(para => {
                    var exist = item.purchaseAttributeList?.find(x => x.systemParaId == para.systemPara.id);
                    if (exist) {
                        para.systemPara.value = exist.value as string;
                    }
                })
            } else if (item.productionAttributeList) {
                item.selectedExtend = "2";
                this.productionParaList.forEach(para => {
                    var exist = item.productionAttributeList?.find(x => x.systemParaId == para.systemPara.id);
                    if (exist) {
                        para.systemPara.value = exist.value as string;
                    }
                })
            }
        },
        getData() {
            return this.$http.post('/api/inventory/InventoryManagement/GetInboundMaterialInfo', {
                dataType: this.dataType,
                dataId: this.dataId,
                supplierId: this.supplierId,
                areaId: this.areaId,
                dataStatus: this.dataStatus
            })
        },
        getAreaData() {
            if (!this.areaId) {
                return Promise.resolve({ data: new AreaInfoDto() });
            }
            return this.$http.get("/api/inventory/InventoryManagement/GetAreaInfo", {
                areaId: this.areaId,
            });
        },
        getPositionData() {
            return this.$http.post('/api/inventory/InventoryManagement/GetInboundDefPosition', {
                dataType: this.dataType,
                dataId: this.dataId,
                supplierId: this.supplierId,
                dataStatus: this.dataStatus,
                categoryId: this.materialInfo.dataCategoryId,
                areaId: this.areaId,
                positionId: this.positionId,
                inboundQuantity: this.inboundQuantity
            })
        },
        getExtendParamData() {
            return this.$http.post('/api/base/SystemPara/GetAdditionalParamByTypes', [SystemParaType.外购物料信息, SystemParaType.生产物料信息]);
        },
        getSupplierData() {
            return this.$http.get('/api/base/Material/GetSupplierOptions', { productCategoryId: this.materialInfo.dataCategoryId, productId: this.dataType == InventoryDataType.Product ? this.dataId : undefined })
                .then(res => {
                    var data = res.data as any;
                    this.supplierOptions = data;
                });
        },
        handleUnitQty(row: InboundDto, changeUnit: InventoryUnitInboundDto) {
            row.unitQtyList.filter(x => x.id != changeUnit.id).forEach(item => {
                if (row == this.selectedRow && item == this.selectedUnit) {
                    return;
                }
                var ratio = new Decimal(changeUnit.ratio / item.ratio);
                item.quantity = Decimal.mul(changeUnit.quantity ?? 0, ratio).toDecimalPlaces(4).toNumber();
                item.price = Decimal.div(changeUnit.price ?? 0, ratio).toDecimalPlaces(4).toNumber();
                item.materialCost = Decimal.div(changeUnit.materialCost ?? 0, ratio).toDecimalPlaces(4).toNumber();
                item.processingCost = Decimal.div(changeUnit.processingCost ?? 0, ratio).toDecimalPlaces(4).toNumber();
            })
        },
        unitInputChange(row: InboundDto, changeUnit: InventoryUnitInboundDto) {
            this.selectedRow = row;
            this.selectedUnit = changeUnit;
        },
        clickPositon(row: InboundDto) {
            this.selectedRow = row;
            this.selectPositionDialog.open();
        },
        selectPositionConfirm(position: StorageLocation_PositionDTO) {
            this.selectedRow.positionId = position.id;
            this.selectedRow.positionName = position.fullName;
            this.selectPositionDialog.close();
        },
        addInbound() {
            var newInbound = new InboundDto({
                index: ++this.maxIndex,
                unitQtyList: cloneDeep(this.unitInfo),
                positionId: cloneDeep(this.defaultPosition.id),
                positionName: cloneDeep(this.defaultPosition.name)
            });
            this.dataList?.push(newInbound);
            this.$emit('addInbound', newInbound)
        },
        delInbound(row: InboundDto) {
            this.dataList = this.dataList.filter(x => x.index != row.index);
        },
        close() {
            this.$emit("close");
            //清空数据
            this.clearData();
        },
        confirm() {
            //验证数据 入库量必填 库位必选
            for (let i = 0; i < this.dataList.length; i++) {
                const record = this.dataList[i];
                if (record.unitQtyList.some(x => !x.quantity)) {
                    this.$kingzon_messager.showError(`第${i + 1}条数据请输入入库量`);
                    return;
                }

                if (!record.positionId) {
                    this.$kingzon_messager.showError(`第${i + 1}条数据请选择库位库区`);
                    return;
                }
                record.purchaseAttributeList = record.selectedExtend == "1" ?
                    this.purchaseParaList.map(x => { return { systemParaId: x.systemPara.id, value: x.systemPara.value } as CustomAttribute }) : undefined;
                record.productionAttributeList = record.selectedExtend == "2" ?
                    this.productionParaList.map(x => { return { systemParaId: x.systemPara.id, value: x.systemPara.value } as CustomAttribute }) : undefined;
                record.customAttributeShowList = record.selectedExtend == "1" ? this.purchaseParaList.map(x => { return { systemParaId: x.systemPara.id, name: x.systemPara.dataValue, value: x.systemPara.value } as CustomAttributeShowDto }) :
                    record.selectedExtend == "2" ? this.productionParaList.map(x => { return { systemParaId: x.systemPara.id, name: x.systemPara.dataValue, value: x.systemPara.value } as CustomAttributeShowDto }) :
                        undefined;
            }
            this.$emit("confirm", {
                dataList: this.dataList,
                dataStatus: this.showStatus && this.newStatus ? Number(this.newStatus) : undefined,
                supplierId: this.newSupplier && this.newSupplier ? Number(this.newSupplier) : undefined,
                supplierName: this.supplierOptions.find(x => x.value == this.newSupplier)?.text
            });
        },
        clearData() {
            this.dataList = [];
            this.selectedRow = {} as InboundDto;
            this.newStatus = undefined;
            this.newSupplier = undefined;
        },
        refreshData() {
            this.clearData();
            this.$nextTick(() => {
                this.init();
            });
        },
        //主单位的单价为供应商的采购单价
        supplierChange() {
            if (!this.newSupplier) {
                return;
            }
            if (this.dataType != InventoryDataType.Product) {
                return;
            }
            //物料 在供应商处 最近的采购单价
            var purchasePrice = Number(this.supplierOptions.find(x => x.value == this.newSupplier)?.extension);
            if (purchasePrice) {
                this.dataList.forEach(item => {
                    item.unitQtyList.forEach(unit => {
                        if (unit.id == -1) {
                            unit.price = purchasePrice;
                            unit.materialCost = purchasePrice;
                        }
                    })
                })
            }
        },
    }
})

</script>
<style scoped>
.field {
    display: flex;
    flex-direction: row;
    align-items: center;
    flex: 1;
    min-width: 0;
}

.field .label {
    width: 80px;
    text-align: right;
}

.field .box {
    flex: 1;
}

.addon {
    padding: 0 4px;
    color: #000;
}

.unit-row {
    display: flex;
    justify-content: space-between;
    align-items: start;
    margin-bottom: 10px;
}

.unit-row.small {
    flex-wrap: wrap;
    gap: 10px 4px;
}

.unit-row.small>.field {
    flex: 0 0 calc(50% - 2px);
}
</style>