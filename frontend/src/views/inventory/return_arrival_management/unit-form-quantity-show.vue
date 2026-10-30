<template>
    <div class="unit-quantity-show">
        <div v-if="quantityList.length > 0" class="main-unit">
            {{ quantityList[0].text }}{{ quantityList[0].extension }}
        </div>

        <div v-if="quantityList.length > 1" class="other-units">
            <span v-for="item in quantityList.slice(1)" :key="item.value" class="other-unit">
                {{ item.text }}{{ item.extension }}
            </span>
        </div>
    </div>
</template>

<script lang="ts">
import type { InventoryUnitDto, InventoryUnitShowDto } from '@/types/inventory';
import type { SelectOption } from '@/types/types';
import Decimal from 'decimal.js';
import { defineComponent } from 'vue';

export default defineComponent({
    name: 'UnitQuantityShow',
    props: {
        quantity: {
            type: Number,
            default: 0,
        },
        unitList: {
            type: Array as () => InventoryUnitDto[],
            default: () => [],
        },
        unitId: {
            type: Number,
            default: -1
        },
        isChange: {
            type: Boolean,
            default: true,
        },
        field: {
            type: String,
            default: 'quantity',
        },
    },
    computed: {
        quantityList(): SelectOption[] {
            const list: SelectOption[] = [];

            list.push(this.getSelectOption(this.unitList.find(x => x.id == this.unitId)))

            for (const item of this.unitList) {
                if (item.id == this.unitId) continue;
                list.push(this.getSelectOption(item))
            }

            return Array.from(
                new Map(list.map(item => [item.code, item])).values()
            ) as SelectOption[];
        },
    },
    methods: {
        getSelectOption(item) {
            const unitQuantity = item.ratio !== 0
                ? new Decimal(this.quantity)
                    .dividedBy(item.ratio)
                    .toDecimalPlaces(6, Decimal.ROUND_HALF_UP)
                    .toNumber()
                : 0;

            const value = this.isChange
                ? unitQuantity
                : item[this.field] ?? 0;

            return {
                text: this.$utils.formatThousandSeparator(value),
                value,
                code: item.id as any,
                extension: item.unitName.replace(/【[\s\S]*?】/g, ''),
            };
        }
    }
});
</script>

<style scoped>
.unit-quantity-show {
    display: flex;
    flex-direction: column;
    align-items: flex-start;
    justify-content: center;
    width: 100%;
    min-width: 0;
    line-height: 20px;
}

.main-unit {
    font-size: 14px;
    color: var(--color-text);
    white-space: nowrap;
}

.other-units {
    display: flex;
    align-items: center;
    flex-wrap: wrap;
    gap: 0 6px;
    width: 100%;
    min-width: 0;
}

.other-unit {
    font-size: 12px;
    color: var(--color-grey);
    font-style: italic;
    white-space: nowrap;
}
</style>