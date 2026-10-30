<template>
    <div class="process-info">
        <Form :model="process" labelAlign="right" :labelWidth="85" class="kingzon-page-form">
            <FormField name="code" label="工序编号:" style="width: 30%">
                <TextBox :modelValue="process.code" @update:modelValue="updateProcess({ code: $event })" :disabled="readonly" />
            </FormField>
            <FormField name="craftId" label="加工工艺:" style="width: 30%">
                <ComboBox :modelValue="process.craftId" @update:modelValue="updateProcess({ craftId: $event })" :disabled="readonly" :data="craftOptions" valueField="id" textField="name"
                    placeholder="请选择加工工艺" @selectionChange="onCraftChange" />
            </FormField>
            <FormField name="name" label="工序名称:" style="width: 30%">
                <TextBox :modelValue="process.name" @update:modelValue="updateProcess({ name: $event })" :disabled="readonly" />
            </FormField>
            <FormField name="theoreticalWorkHours" label="理论工时(h):" style="width: 30%">
                <NumberBox :modelValue="process.theoreticalWorkHours" @update:modelValue="updateProcess({ theoreticalWorkHours: $event })" :min="0" :max="99999999.9999" :precision="4" :disabled="readonly" />
            </FormField>
            <FormField name="isReportWork" label="是否报工:" style="width: 30%">
                <CheckBox :modelValue="process.isReportWork" @update:modelValue="updateProcess({ isReportWork: $event })" :disabled="readonly" />
            </FormField>
            <FormField name="description" label="描述:" style="width: 40%">
                <TextBox :modelValue="process.description" @update:modelValue="updateProcess({ description: $event })" :disabled="readonly" />
            </FormField>
            <div name="isOutsource" style="width: 40%">
                <CheckBox :modelValue="process.isInbound" @update:modelValue="updateProcess({ isInbound: $event })" :disabled="readonly || inboundRequired" />入库
                <CheckBox :modelValue="process.isOutsource" @update:modelValue="updateProcess({ isOutsource: $event })" :disabled="readonly" />外协
                <span v-if="inboundRequired" class="inbound-required-tip">下一道工序为外协，本工序必须入库</span>
            </div>
        </Form>
    </div>
</template>

<script lang="ts">
import { CraftProcessDto } from '@/types/craft'
import { defineComponent, type PropType } from 'vue'

export default defineComponent({
    name: 'CraftProcessInfo',
    emits: ['updateProcess'],
    props: {
        readonly: Boolean,
        inboundRequired: Boolean,
        process: { type: Object as PropType<CraftProcessDto>, required: true }
    },
    data() {
        return { craftOptions: [] as Array<{ id: number; name: string }> }
    },
    mounted() {
        this.loadCraftOptions()
    },
    methods: {
        updateProcess(changes: Partial<CraftProcessDto>) {
            if (this.readonly || (this.inboundRequired && changes.isInbound === false)) return
            this.$emit('updateProcess', changes)
        },
        async loadCraftOptions() {
            const result = await this.$http.get('/api/technical/CraftOrderManage/GetCraftList')
            this.craftOptions = result?.data ?? []
        },
        onCraftChange(craft: { id: number; name: string } | null) {
            if (this.readonly) return
            const selected = craft ?? this.craftOptions.find(item => item.id === this.process.craftId)
            this.updateProcess({ craftId: selected?.id, craftName: selected?.name ?? '', name: selected?.name ?? '' })
        }
    }
})
</script>

<style scoped>
.inbound-required-tip { display: block; margin-top: 6px; color: #b45309; font-size: 12px; }
.process-info {
    padding: 12px 16px;
}
</style>
