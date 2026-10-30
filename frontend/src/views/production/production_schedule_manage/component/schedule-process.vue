<template>
    <Panel :title="modelValue.code + ' ' + modelValue.name" :bodyStyle="{ padding: '8px' }" style="margin: 8px 0">
        <div class="kingzon-text-grey" style="margin-bottom: 8px">前序：{{ modelValue.predecessor || '无' }}</div>
        <Form :model="modelValue" :labelWidth="90" labelAlign="right" style="display: flex; flex-wrap: wrap; gap: 8px">
            <FormField name="outsource" label="生产方式:" style="flex: 1 1 180px; min-width: 0">
                <ComboBox :modelValue="modelValue.outsource" :data="productionModes" :editable="false"
                    style="width: 100%" @update:modelValue="updateField('outsource', $event)" />
            </FormField>
            <FormField name="inbound" label="完工入库:" style="flex: 1 1 180px; min-width: 0">
                <CheckBox :modelValue="modelValue.inbound" @update:modelValue="updateField('inbound', $event)" />
            </FormField>
        </Form>
        <div v-if="modelValue.outsource" style="padding-top: 8px">
            外协 {{ quantity }} {{ unit }} · 开始 / 完成时间对应发出 / 返回时间
        </div>
        <template v-else>
            <div style="display: flex; align-items: center; justify-content: space-between; gap: 8px; margin: 8px 0">
                <span>派工 {{ dispatchQuantity }} / {{ quantity }} {{ unit }}</span>
                <LinkButton plain iconCls="icon-add" @click="$emit('addDispatch')">增加派工</LinkButton>
            </div>
            <div v-for="(dispatch, index) in modelValue.dispatches" :key="dispatch.id"
                style="display: flex; align-items: end; flex-wrap: wrap; gap: 8px; margin-bottom: 8px">
                <label style="display: flex; flex-direction: column; gap: 4px; flex: 1 1 100px; min-width: 0">人员
                    <ComboBox :modelValue="dispatch.operator" :data="operators" :editable="false" style="width: 100%"
                        @update:modelValue="updateDispatch(dispatch.id, 'operator', $event)" />
                </label>
                <label style="display: flex; flex-direction: column; gap: 4px; flex: 1 1 120px; min-width: 0">设备
                    <ComboBox :modelValue="dispatch.machine" :data="machines" :editable="false" style="width: 100%"
                        @update:modelValue="updateDispatch(dispatch.id, 'machine', $event)" />
                </label>
                <label style="display: flex; flex-direction: column; gap: 4px; flex: 1 1 80px; min-width: 0">派工数量
                    <NumberBox :modelValue="dispatch.quantity" :min="0.01" :max="quantity" :precision="2"
                        style="width: 100%" @update:modelValue="updateDispatch(dispatch.id, 'quantity', $event)" />
                </label>
                <LinkButton v-if="index > 0" plain iconCls="icon-remove" title="删除派工"
                    @click="removeDispatch(dispatch.id)" />
            </div>
        </template>
    </Panel>
</template>

<script lang="ts">
import { defineComponent, type PropType } from 'vue'
import { ProcessSettingDto, DispatchRowDto } from '@/types/schedule'
import { localDateTime, timestamp } from '../schedule'

export default defineComponent({
    name: 'ScheduleProcess',
    props: {
        modelValue: { type: Object as PropType<ProcessSettingDto>, required: true },
        quantity: { type: Number, required: true },
        unit: { type: String, required: true },
        operators: { type: Array as PropType<Array<{ text: string; value: string }>>, required: true },
        machines: { type: Array as PropType<Array<{ text: string; value: string }>>, required: true }
    },
    emits: ['update:modelValue', 'addDispatch'],
    data() {
        return { productionModes: [{ text: '内部生产', value: false }, { text: '外协', value: true }] }
    },
    computed: {
        dispatchQuantity(): number {
            return this.modelValue.dispatches.reduce((total, row) => total + (Number(row.quantity) || 0), 0)
        }
    },
    methods: {
        updateField<K extends keyof ProcessSettingDto>(key: K, value: ProcessSettingDto[K]) {
            this.$emit('update:modelValue', new ProcessSettingDto({ ...this.modelValue, [key]: value }))
        },
        asDate(value: string): Date | null {
            return Number.isFinite(timestamp(value)) ? new Date(value) : null
        },
        updateDate(key: 'start' | 'end', value: Date | null) {
            this.updateField(key, value && Number.isFinite(value.getTime()) ? localDateTime(value) : '')
        },
        updateDispatch<K extends keyof DispatchRowDto>(id: number, key: K, value: DispatchRowDto[K]) {
            const dispatches = this.modelValue.dispatches.map(row => row.id === id
                ? new DispatchRowDto({ ...row, [key]: value }) : row)
            this.updateField('dispatches', dispatches)
        },
        removeDispatch(id: number) {
            this.updateField('dispatches', this.modelValue.dispatches.filter(row => row.id !== id))
        }
    }
})
</script>
