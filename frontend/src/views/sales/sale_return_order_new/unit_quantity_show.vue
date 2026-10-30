<template>
    <div>
        <span style="margin-right: 2px;">
            {{ quantity ?? 0 }}{{ currentUnit?.unitName ?? '' }}
        </span>
    </div>
    <div v-if="otherUnits.length > 0" class="kingzon-text-subinfo">
        {{
            otherUnits
                .map(item =>
                    `${$utils.formatThousandSeparator(
                        item.quantity,
                        4
                    )}${item.unitName}`
                )
                .join(' ')
        }}
    </div>
</template>
<script lang="ts">
import { defineComponent } from 'vue'
import { UnitConversionHelper } from '@/utils/UnitConversionHelper'
import type { InventoryUnitDto } from '@/types/sale';

export default defineComponent({
    name: 'UnitQuantityShow',
    props: {
        quantity: {
            type: Number,
            default: 0,
        },
        unitInfos: {
            type: Array as () => InventoryUnitDto[],
            default: () => [],
        },
        unitId: {
            type: Number,
            default: -1,
        }
    },
    computed: {
        helper() {
            return new UnitConversionHelper(
                this.unitId,
                this.quantity ?? 0,
                this.unitInfos
            )
        },
        currentUnit() {
            return this.helper.getConversionUnitWithQuantity()
        },
        otherUnits() {
            return this.helper.getOtherUnitsWithQuantity()
        }
    }
})
</script>