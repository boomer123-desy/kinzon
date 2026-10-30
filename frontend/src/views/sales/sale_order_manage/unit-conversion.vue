<template>
    <div style="display: flex;flex-direction: column;padding: 10px;height: 100%;">
        <div style="display: flex;flex-direction: row;justify-content: center;margin: 10px;">{{
            orderData.product.productNo }}-{{
                orderData.product.productName }} 下单数量：{{
                orderData.placeOrderQuantity }}
            {{ orderData.salesUnit }}</div>
        <div style="flex:1;overflow-y: scroll;display: flex;flex-direction: column;align-items: center;">
            <div v-for="unit in unitConversionList" :key="unit.id"
                style="display: flex;flex-direction: row;justify-content:space-between;width: 80%;margin-top: 4px;align-items: center;">
                <div style="width: 250px;">单位：
                    <span v-if="unit.id == -1">{{ unit.unitName }}</span>
                    <ComboBox v-else valueField="code" v-model="unit.id" @selectionChange="onUnitChange($event, unit)"
                        :data="unitList" :disabled="unit.id == -1">
                        <template #item="scope">
                            {{ scope.row.text }}{{ scope.row.extension ? '(' + scope.row.extension + ')' : '' }}
                        </template>
                    </ComboBox>
                </div>
                <span>下单数量：{{ unit.placeOrderQuantity }}</span>
                <div style="width: 20px;">
                    <font-awesome-icon :icon="['fas', 'trash']" v-if="unit.id != -1"
                        :style="{ 'color': 'var(--color-blue)', 'font-size': '17px' }" @click="deleteUnit(unit)" />
                </div>
            </div>
            <LinkButton btnCls="c8" style="margin-top: 5px;width: 80%;height: 32px;flex-shrink: 0;" @click="add">新增单位转换
            </LinkButton>
        </div>
        <div class="kingzon-btn-group kingzon-page-footer">
            <LinkButton @click="cancel">取消</LinkButton>
            <LinkButton btnCls="c8" @click="save">确定</LinkButton>
        </div>
    </div>
</template>

<script lang="ts">
import { defineComponent } from 'vue';
import { SaleOrderDetail, SaleUnitConversionDto, SaleGoodsUnitDto } from '@/types/sale';
import Decimal from 'decimal.js';
import { cloneDeep } from 'lodash';

export default defineComponent({
    name: 'unitConversion',
    props: {
        orderData: {
            type: SaleOrderDetail,
            required: true
        },
    },
    data() {
        return {
            unitConversionList: [] as SaleUnitConversionDto[]
        }
    },
    computed: {
        unitList() {
            return this.orderData.product.unitList?.filter(x => x.code != this.orderData.salesUnitConversionId && x.code != -1);
        }
    },
    watch: {
        'orderData.unitConversionList': {
            handler(val) {
                this.unitConversionList = val;
            },
            deep: true
        },
    },
    mounted() {
        this.unitConversionList = cloneDeep(this.orderData.unitConversionList);
    },
    methods: {
        add() {
            this.unitConversionList.push(new SaleUnitConversionDto())
        },
        onUnitChange(event: SaleGoodsUnitDto, row: SaleUnitConversionDto) {
            if (!event) {
                row.unitName = undefined;
                return
            };
            row.unitId = event.unitId;
            row.unitName = event.text;
            row.remark = event.extension;
            row.packageId = event.packageId;
            row.isBaseUnit = event.code == -1;
            //下单单位是主单位
            if (this.orderData.salesUnitConversionId == -1) {
                var num = Number(event.value) ?? 0;
                row.ratio = num ? Decimal.div(1, num).toNumber() : 0;
            } else {
                var saleUnit = this.orderData.product.unitList?.find(x => x.code == this.orderData.salesUnitConversionId);
                var saleRatio = Number(saleUnit?.value) ?? 0;
                var eventRatio = Number(event.value) ?? 0;
                row.ratio = eventRatio ? (saleRatio / eventRatio) : 0;
            }
            row.placeOrderQuantity = Decimal.mul(this.orderData.placeOrderQuantity, row.ratio).toDecimalPlaces(4).toNumber();
        },
        save() {
            var data = this.unitConversionList.filter(x => x.id)
            //如果有重复的单位转换 不允许保存
            var setA = new Set(data.map(x => x.id));
            var valid = true;
            for (var item of setA.values()) {
                var tmp = data.filter(x => x.id == item);
                if (tmp.length > 1) {
                    this.$kingzon_messager.showError("不允许重复录入单位转换")
                    valid = false;
                    break;
                }
            }

            if (valid) {
                this.$emit("success", data)
            }
        },
        cancel() {
            this.$emit("cancel");
        },
        deleteUnit(unit) {
            this.unitConversionList = this.unitConversionList.filter(x => x != unit)
        }
    }
})
</script>