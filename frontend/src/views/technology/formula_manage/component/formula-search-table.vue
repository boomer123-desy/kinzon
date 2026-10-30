<template>
    <KingZonDataGrid id="formulaSearchTableDataGrid" :data="dataList" style="height: 100%;" @pageChange="refreshTable"
        @sortChange="refreshTable" ref="dataGrid">
        <GridColumnGroup align="left" :frozen="true" width="250">
            <GridHeaderRow>
                <GridColumn align="center" cellCss="datagrid-td-rownumber" width="40" field="index">
                    <template #body="scope">
                        {{ scope.rowIndex + 1 }}
                    </template>
                </GridColumn>
                <GridColumn field="code" title="编码" width="90" align="left" :editable="true"
                    :overflowTextEllipsis="true">
                </GridColumn>
                <GridColumn field="name" title="名称" width="120" align="left" :overflowTextEllipsis="true">
                </GridColumn>
            </GridHeaderRow>
        </GridColumnGroup>
        <GridColumnGroup :columnMoving="true" align="center">
            <GridHeaderRow>
                <GridColumn field="specification" v-if="theme == 'RowMaterial'" title="规格型号" width="100" align="left"
                    :overflowTextEllipsis="true">
                </GridColumn>
                <GridColumn field="techCode" v-if="theme != 'Formula'" title="技术型号" width="100" align="left"
                    :overflowTextEllipsis="true">
                </GridColumn>
                <GridColumn field="formula" v-if="theme == 'Product' || theme == 'SemiProduct'" title="配方" width="120"
                    align="left">
                    <template #body="scope">
                        <div v-if="scope.row.detailType == FormulaDetailType.Formula" class="kingzon-textEllipsis"
                            :title="'[' + scope.row.formulaVersion + ']' + scope.row.formulaCode + '' + scope.row.formulaName">
                            [{{ scope.row.formulaVersion }}]{{ scope.row.formulaCode + scope.row.formulaName }}
                        </div>
                    </template>
                </GridColumn>
                <GridColumn field="formulaRemark" v-if="theme == 'Product' || theme == 'SemiProduct'" title="配方备注"
                    width="80" align="left" :overflowTextEllipsis="true">
                </GridColumn>
                <GridColumn field="version" v-if="theme == 'Formula'" title="版本号" width="50" align="left"
                    :overflowTextEllipsis="true">
                </GridColumn>
                <GridColumn field="product" v-if="theme == 'Formula'" title="产品" width="120" align="left">
                    <template #body="scope">
                        <div :title="scope.row.productCode + ' ' + scope.row.productName" class="kingzon-textEllipsis">
                            {{ scope.row.productCode }} {{ scope.row.productName }}
                        </div>
                    </template>
                </GridColumn>
                <GridColumn field="productRemark" v-if="theme == 'Formula'" title="产品备注" width="80" align="left"
                    :overflowTextEllipsis="true">
                </GridColumn>
                <GridColumn field="unit" v-if="theme != 'Formula'" title="单位" width="50" align="left">
                </GridColumn>
                <GridColumn field="remark" title="备注" width="100" align="left" :overflowTextEllipsis="true">
                </GridColumn>
            </GridHeaderRow>
        </GridColumnGroup>
        <GridColumnGroup :frozen="true" align="right" width="60">
            <GridHeaderRow>
                <GridColumn field="op" title="操作" width="60px" align="center">
                    <template #body="scope">
                        <ButtonGroup style="height:24px">
                            <LinkButton @click="choose(scope.row)">选择</LinkButton>
                        </ButtonGroup>
                    </template>
                </GridColumn>
            </GridHeaderRow>
        </GridColumnGroup>
    </KingZonDataGrid>
</template>

<script lang="ts">
import { FormulaDetailType } from '@/types/enums';
import { defineComponent, type PropType } from 'vue';

export default defineComponent({
    name: 'FormulaSearchTable',
    emits: ["refreshTable", "choose"],
    props: {
        theme: {
            type: String as PropType<"Formula" | "RowMaterial" | "Product" | "SemiProduct">,
            default: "Formula"
        },
        dataList: {
            type: Array,
        },
        tableRef: {
            type: String,
            default: 'datagrid'
        }
    },
    computed: {
        FormulaDetailType() {
            return FormulaDetailType;
        }
    },
    methods: {
        refreshTable() {
            this.$emit("refreshTable")
        },
        choose(row) {
            this.$emit("choose", row);
        }
    }
})
</script>