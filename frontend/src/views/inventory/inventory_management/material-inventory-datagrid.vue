<template>
    <KingZonDataGrid :id="dataGridId" :data="recordList" class="kingzon-page-fill" ref="dataGrid" :showDownLoad="true"
        :isDownLoad="true" :downloadLeft="showSelection ? 50 : 10" :pagination="showPage" :isCheckBoxSelection="true"
        :leftGroupRowWidth="200" :rightGroupRowWidth="rightGroupRowWidth" :footerData="footerData" :showFooter="true"
        :isShowCheckBox="showSelection" :exportFileName="exportFileName" @sortChange="refreshTable"
        @pageChange="refreshTable" idField="idField" @seletedRowsChanged="$emit('selectChange', $event)"
        style="overflow-y: unset;" :cellSpan="getCellSpan" :splitFields="['quantity', 'usedQuantity', 'remainQuantity']">
        <template #left>
            <GridColumn align="center" cellCss="datagrid-td-rownumber" width="40" field="index">
                <template #body="scope">
                    {{ scope.rowIndex + 1 }}
                </template>
            </GridColumn>
            <GridColumn field="recordNo" title="库存编码" width="120" align="left" :sortable="true">
                <template #body="scope">
                    <KingZonSymbol v-if="scope.row.sourceType == InventorySourceType.异常入库" symbolText="异"
                        :color="'var(--color-red)'" :rowText="scope.row.recordNo">
                    </KingZonSymbol>
                    <KingZonSymbol v-else-if="scope.row.sourceType == InventorySourceType.成品报废入库" symbolText="废"
                        :color="'var(--color-red)'" :rowText="scope.row.recordNo">
                    </KingZonSymbol>
                    <span v-else> {{ scope.row.recordNo }}</span>
                </template>
            </GridColumn>
        </template>
        <template #center>
            <GridColumn field="materialNo" title="物料编码" width="100" align="left" :overflowTextEllipsis="true"
                :sortable="true" v-if="showMaterialInfo">
            </GridColumn>
            <GridColumn field="materialName" title="物料名称" width="150" align="left" :overflowTextEllipsis="true"
                :sortable="true" v-if="showMaterialInfo">
            </GridColumn>
            <GridColumn field="materialTechnicalCode" title="技术型号" align="left" width="100" :overflowTextEllipsis="true"
                v-if="showMaterialInfo">
            </GridColumn>
            <GridColumn field="materialSpecification" title="规格型号" align="left" width="120" :overflowTextEllipsis="true"
                v-if="showMaterialInfo">
            </GridColumn>
            <GridColumn field="dataStatus" title="物料状态" align="left" width="90" v-if="showMaterialInfo">
                <template #body="scope">
                    {{ scope.row.dataStatus ? InventoryDataStatus[scope.row.dataStatus]
                        : "" }}</template>
            </GridColumn>
            <GridColumn field="supplierName" title="供应商" align="left" width="100" :overflowTextEllipsis="true"
                v-if="showMaterialInfo">
            </GridColumn>
            <GridColumn v-if="showInventoryType" field="inventoryType" title="操作类型" width="80" align="left">
                <template #body="scope">
                    {{ InventoryType[scope.row.inventoryType] }}
                </template>
            </GridColumn>
            <GridColumn v-if="showQuantity" field="quantity" title="出/入库量" width="110" align="right">
                <template #body="scope">
                    <div style="display: flex;flex-direction:column"
                        :title="scope.row.inventoryUnitList.map(x => x.quantity + x.unitName).join('\n')">
                        <span>{{ scope.row.quantity }}{{ scope.row.unit }}</span>
                        <div v-if="scope.row.inventoryUnitListExMain"
                            style="display: flex;flex-direction: row;justify-content: right;">
                            <div style="font-style: italic;color: var(--color-grey);font-size: 12px;margin-left: 5px;"
                                v-for="unit in scope.row.inventoryUnitListExMain" :key="unit.id">
                                {{ unit.quantity }}{{ unit.unitName }}</div>
                        </div>
                    </div>
                </template>
                <template #footer>
                    <div :title="handleFooterUnitData().map(u => `${u.quantity} ${u.unitName}`).join('\n')">
                        {{ handleFooterData("quantity") }}
                    </div>
                </template>
            </GridColumn>
            <GridColumn v-if="showUsedQuantity" field="usedQuantity" title="使用量" width="110" align="right">
                <template #body="scope">
                    <div style="display: flex;flex-direction:column"
                        :title="scope.row.inventoryUnitList.map(x => x.usedQuantity + x.unitName).join('\n')">
                        <span>{{ scope.row.usedQuantity }}{{ scope.row.unit }}</span>
                        <div v-if="scope.row.inventoryUnitListExMain"
                            style="display: flex;flex-direction: row;justify-content: right;">
                            <div style="font-style: italic;color: var(--color-grey);font-size: 12px;margin-left: 5px;"
                                v-for="unit in scope.row.inventoryUnitListExMain" :key="unit.id">
                                {{ unit.usedQuantity }}{{ unit.unitName }}</div>
                        </div>
                    </div>
                </template>
                <template #footer>
                    <div :title="handleFooterUnitData().map(u => `${u.usedQuantity} ${u.unitName}`).join('\n')">
                        {{ handleFooterData("usedQuantity") }}
                    </div>
                </template>
            </GridColumn>
            <GridColumn v-if="showRemainQuantity" field="remainQuantity" title="库存量" width="110" align="right"
                :sortable="true">
                <template #body="scope">
                    <div style="display: flex;flex-direction:column"
                        :title="scope.row.inventoryUnitList.map(x => x.remainQuantity + x.unitName).join('\n')">
                        <span>{{ scope.row.remainQuantity }}{{ scope.row.unit }}</span>
                        <div v-if="scope.row.inventoryUnitListExMain"
                            style="display: flex;flex-direction: row;justify-content: right;">
                            <div style="font-style: italic;color: var(--color-grey);font-size: 12px;margin-left: 5px;"
                                v-for="unit in scope.row.inventoryUnitListExMain" :key="unit.id">
                                {{ unit.remainQuantity }}{{ unit.unitName }}</div>
                        </div>
                    </div>
                </template>
                <template #footer>
                    <div :title="handleFooterUnitData().map(u => `${u.remainQuantity} ${u.unitName}`).join('\n')">
                        {{ handleFooterData("remainQuantity") }}
                    </div>
                </template>
            </GridColumn>
            <!-- <GridColumn field="unit" title="单位" width="60" align="left">
            </GridColumn> -->
            <GridColumn v-if="areaInfo.showMoney" field="price" title="单价(元)" width="85" align="right">
            </GridColumn>
            <GridColumn v-if="showPosition" field="positionInfo" title="库位" width="200" align="left"
                :overflowTextEllipsis="true">
            </GridColumn>
            <GridColumn field="sourceType" title="来源" width="90" align="left">
                <template #body="scope">
                    {{ InventorySourceType[scope.row.sourceType] }}
                </template>
            </GridColumn>
            <GridColumn field="sourceNo" title="来源单号" width="130" align="left">
            </GridColumn>
            <slot name="centerCol"></slot>
            <GridColumn field="createTime" title="操作时间" width="150" align="left" :sortable="true">
                <template #body="scope">
                    {{ $utils.formatDateTime(scope.row.createTime, "yyyy-MM-dd HH:mm:ss") }}
                </template>
            </GridColumn>
            <GridColumn field="inboundTime" title="初入库时间" width="150" align="left" :sortable="true">
                <template #body="">
                    开发中
                </template>
            </GridColumn>
            <GridColumn field="createUserName" title="操作员" width="70" align="left">
            </GridColumn>
            <GridColumn field="remark" title="备注" width="100" align="left" :overflowTextEllipsis="true">
            </GridColumn>

            <GridColumn field="materialCategory" title="物料类别" width="100" align="left" :overflowTextEllipsis="true"
                v-if="showMaterialInfo">
            </GridColumn>
            <GridColumn field="materialGrade" title="材质牌号" width="100" align="left" :overflowTextEllipsis="true"
                v-if="showMaterialInfo">
            </GridColumn>
            <GridColumn v-for="para in systemParaList" :key="para.id" :field="para.id.toString()"
                :title="para.dataValue" width="100" align="left">
                <template #body=scope>
                    <div :title="scope.row.customAttributeList?.find(x => x.systemParaId == para.id)?.value ?? ''">
                        {{scope.row.customAttributeList?.find(x => x.systemParaId == para.id)?.value ??
                            ""}}
                    </div>
                </template>
            </GridColumn>
        </template>
        <template #right>
            <GridColumn field="op" title="操作" :width="rightGroupRowWidth" align="center">
                <template #body="scope">
                    <ButtonGroup style="height:24px">
                        <LinkButton
                            v-if="showAbnormalReport && (scope.row.inventoryType == InventoryType.入库 || scope.row.inventoryType == InventoryType.线边入库)"
                            @click="doing()">上报异常
                        </LinkButton>
                        <slot name="operate" :row="scope.row"></slot>
                    </ButtonGroup>
                </template>
            </GridColumn>
        </template>
    </KingZonDataGrid>
</template>
<!-- <LinkButton @click="doing()">追溯</LinkButton> -->
<!-- <LinkButton @click="doing()">异常处理结果</LinkButton> -->
<script lang="ts">
import { defineComponent } from 'vue';
import { InventoryType, InventorySourceType, InventoryDataStatus } from "@/types/enums";
import { InventoryRecordDto, AreaInfoDto } from "@/types/inventory";
import { Para } from "@/types/common";
import Decimal from 'decimal.js';

export default defineComponent({
    name: 'MaterialInventoryDataGrid',
    emits: ["refreshTable", "selectChange"],
    data() {
        return {
        }
    },
    props: {
        recordList: {
            type: Array as () => Array<InventoryRecordDto>,
            required: true,
        },
        areaInfo: {
            type: Object as () => AreaInfoDto,
            required: true,
        },
        //扩展参数列
        systemParaList: {
            type: Array as () => Array<Para>,
            required: false,
        },
        exportFileName: {
            type: String,
            required: false,
            default: "库存明细"
        },
        materialUnit: {
            type: String,
            default: "",
        },
        showInventoryType: {
            type: Boolean,
            default: false,
        },
        showQuantity: {
            type: Boolean,
            default: false,
        },
        showUsedQuantity: {
            type: Boolean,
            default: false,
        },
        showRemainQuantity: {
            type: Boolean,
            default: true,
        },
        showPosition: {
            type: Boolean,
            default: true,
        },
        showSelection: {
            type: Boolean,
            default: true
        },
        showPage: {
            type: Boolean,
            default: false
        },
        rightGroupRowWidth: {
            type: Number,
            default: 120
        },
        dataGridId: {
            type: String,
            default: "igrid"
        },
        showMaterialInfo: {
            type: Boolean,
            default: false,
        },
        showAbnormalReport: {
            type: Boolean,
            default: true,
        }
    },
    computed: {
        footerData() {
            return [{ index: '合计', unit: this.materialUnit }]
        },
        InventorySourceType() {
            return InventorySourceType;
        },
        InventoryType() {
            return InventoryType;
        },
        dataGrid() {
            return this.$refs.dataGrid as EasyUI.DataGrid;
        },
        InventoryDataStatus() {
            return InventoryDataStatus;
        }
    },
    created() {
    },
    methods: {
        handleFooterData(key) {
            var decimalTotal = this.recordList.reduce((sum, record) => Decimal.add(sum, record[key]), new Decimal(0));
            //最多保留6位小数
            if (decimalTotal.decimalPlaces() > 6) {
                decimalTotal = decimalTotal.toDecimalPlaces(6, Decimal.ROUND_HALF_UP);
            }
            return this.$utils.formatThousandSeparator(decimalTotal.toNumber());
        },
        handleFooterUnitData() {
            const grouped = new Map<number, {
                unitName: string; quantity: Decimal; usedQuantity: Decimal; remainQuantity: Decimal;
            }>();

            for (const record of this.recordList) {
                for (const unit of record.inventoryUnitList ?? []) {
                    const existing = grouped.get(unit.id);

                    if (existing) {
                        existing.quantity = existing.quantity.plus(unit.quantity);
                        existing.usedQuantity = existing.usedQuantity.plus(unit.usedQuantity);
                        existing.remainQuantity = existing.remainQuantity.plus(unit.remainQuantity);
                    } else {
                        grouped.set(unit.id, {
                            unitName: unit.unitName,
                            quantity: new Decimal(unit.quantity),
                            usedQuantity: new Decimal(unit.usedQuantity),
                            remainQuantity: new Decimal(unit.remainQuantity)
                        });
                    }
                }
            }

            return [...grouped.entries()].map(([unitId, { unitName, quantity, usedQuantity, remainQuantity }]) => ({
                unitName,
                quantity: this.$utils.formatThousandSeparator(
                    quantity.toDecimalPlaces(6, Decimal.ROUND_HALF_UP).toNumber()
                ),
                usedQuantity: this.$utils.formatThousandSeparator(
                    usedQuantity.toDecimalPlaces(6, Decimal.ROUND_HALF_UP).toNumber()
                ),
                remainQuantity: this.$utils.formatThousandSeparator(
                    remainQuantity.toDecimalPlaces(6, Decimal.ROUND_HALF_UP).toNumber()
                )
            }));
        },
        getCellSpan(data) {
            var cellSpanColumns = ["recordNo", 'inventoryType', 'createTime', 'inboundTime', 'createUserName', 'op'];
            if (cellSpanColumns.includes(data.column.field)) {
                var rowIndex = data.rowIndex
                return this.$utils.getCellSpanMultiKey([["recordNo"]], data.row, rowIndex, this.recordList);
            }
            return { rowspan: 1, colspan: 1 };
        },
        refreshTable() {
            this.$emit("refreshTable");
        },
        doing() {
            this.$kingzon_messager.showInfo("功能开发中");
        }
    }
})
</script>