<template>
    <div>
        <div style="display: flex; flex-wrap: wrap; justify-content: space-between; align-items: center; gap: 4px; margin-bottom: 4px">
            <strong>设备与人员派工</strong>
            <LinkButton iconCls="icon-add" :disabled="disabled" @click="openSelection()">选择设备与人员</LinkButton>
        </div>
        <TreeGrid :data="rows" idField="id" treeField="name" :animate="false" :fitColumns="true" :style="{ height: `${Math.min(188, 34 + Math.max(1, modelValue.length + personCount) * 30)}px` }">
            <GridColumnGroup>
                <GridHeaderRow>
                    <GridColumn field="name" title="设备 / 人员" width="130" :overflowTextEllipsis="true" />
                    <GridColumn field="users" title="人员" width="40">
                        <template #body="{ row }">
                            <LinkButton v-if="!row.userId" iconCls="icon-add" plain :disabled="disabled"
                                @click="openSelection(row.machineId)">
                            </LinkButton>
                        </template>
                    </GridColumn>
                    <GridColumn field="quantity" title="分配数量" width="85" align="right">
                        <template #body="{ row }">
                            <NumberBox v-if="row.userId || !row.children?.length" :modelValue="row.quantity" :min="0" :max="quantity"
                                :precision="4" :disabled="disabled" style="width: 100%; min-width: 0; height: 28px"
                                @update:modelValue="updateUser(row, 'quantity', $event)" />
                            <span v-else>{{ row.quantity }}</span>
                        </template>
                    </GridColumn>
                    <GridColumn field="hours" title="工时(h)" width="85" align="right">
                        <template #body="{ row }">
                            <NumberBox v-if="row.userId || !row.children?.length" :modelValue="row.hours" :min="0" :max="99999999.9999"
                                :precision="4" :disabled="disabled" style="width: 100%; min-width: 0; height: 28px"
                                @update:modelValue="updateUser(row, 'hours', $event)" />
                            <span v-else>{{ row.hours }}</span>
                        </template>
                    </GridColumn>
                </GridHeaderRow>
            </GridColumnGroup>
        </TreeGrid>
    </div>
    <div style="display: flex; align-items: center; flex-wrap: wrap; gap: 8px; padding: 5px 0">
        <span>已分配 {{ totalQuantity }} / {{ quantity }} · 人员分配 {{ personCount }} 条</span>
        <span>统一工时：</span>
        <NumberBox v-model="batchHours" :min="0" :max="99999999.9999" :precision="4" :disabled="disabled"
            style="width: 100px" />
        <LinkButton :disabled="disabled || !modelValue.length" @click="applyHours">应用到设备 / 人员</LinkButton>
    </div>
    <Dialog ref="machineDialog" title="选择设备与人员" :modal="true" :closed="true"
        :dialogStyle="{ width: '680px', maxWidth: '95vw' }" @close="selectionOpen = false">
        <div style="max-height: 60vh; overflow: auto; padding: 8px">
            <div v-if="!selectionOptions.length" class="kingzon-text-grey">当前工艺暂无可选设备</div>
            <div v-for="machine in selectionOptions" :key="machine.id" style="padding: 6px 0">
                <div style="display: flex; align-items: center; gap: 6px">
                    <CheckBox :modelValue="draftMachineIds.includes(machine.id)" :disabled="disabled"
                        @update:modelValue="toggleMachine(machine.id, $event)" /><span>{{ machine.text }}</span>
                </div>
                <div v-if="draftMachineIds.includes(machine.id)"
                    style="display: flex; flex-wrap: wrap; gap: 12px; padding: 8px 0 0 24px">
                    <div v-for="user in machine.users" :key="user.id"
                        style="display: flex; align-items: center; gap: 5px">
                        <CheckBox :modelValue="(draftUserIds[machine.id] || []).includes(user.id)" :disabled="disabled"
                            @update:modelValue="toggleUser(machine.id, user.id, $event)" /><span>{{ user.text }}</span>
                    </div>
                    <span v-if="!machine.users.length" class="kingzon-text-grey">该设备暂无可选人员</span>
                </div>
            </div>
        </div>
        <div class="kingzon-btn-group kingzon-page-footer">
            <LinkButton @click="closeSelection">取消</LinkButton>
            <LinkButton :disabled="disabled" @click="confirmSelection">确定</LinkButton>
        </div>
    </Dialog>
</template>

<script lang="ts">
import { defineComponent, type PropType } from 'vue'
import { ScheduleMachineEditDto, SchedulePersonEditDto, type ScheduleMachineOptionDto } from '@/types/schedule'

interface DialogRef { open(): void; close(): void }

export default defineComponent({
    name: 'ScheduleMachine',
    props: {
        modelValue: { type: Array as PropType<ScheduleMachineEditDto[]>, default: () => [] },
        machineOptions: { type: Array as PropType<ScheduleMachineOptionDto[]>, default: () => [] },
        quantity: { type: Number, default: 0 }, disabled: Boolean
    },
    emits: ['update:modelValue'],
    data() {
        return {
            batchHours: 0 as number | null, selectionOpen: false, selectionMachineId: 0,
            draftMachineIds: [] as number[], draftUserIds: {} as Record<number, number[]>
        }
    },
    computed: {
        selectionOptions(): ScheduleMachineOptionDto[] {
            return this.selectionMachineId ? this.machineOptions.filter(machine => machine.id === this.selectionMachineId) : this.machineOptions
        },
        selectedMachineIds(): number[] { return this.modelValue.map(machine => machine.machineId) },
        totalQuantity(): number { return Number(this.modelValue.reduce((sum, machine) => sum + (machine.users.length ? machine.users.reduce((n, user) => n + user.quantity, 0) : machine.quantity), 0).toFixed(4)) },
        personCount(): number { return this.modelValue.reduce((sum, machine) => sum + machine.users.length, 0) },
        rows() {
            return this.modelValue.map(machine => ({
                id: `machine-${machine.machineId}`, machineId: machine.machineId, userId: 0,
                name: this.machineOptions.find(item => item.id === machine.machineId)?.text ?? `设备 ${machine.machineId}`,
                quantity: machine.users.length ? Number(machine.users.reduce((sum, user) => sum + user.quantity, 0).toFixed(4)) : machine.quantity,
                hours: machine.users.length ? Number(machine.users.reduce((sum, user) => sum + (Number(user.hours) || 0), 0).toFixed(4)) : machine.hours, state: 'open',
                children: machine.users.map(user => ({
                    id: `machine-${machine.machineId}-user-${user.userId}`, machineId: machine.machineId,
                    ...user, name: this.users(machine.machineId).find(item => item.id === user.userId)?.text ?? `人员 ${user.userId}`
                }))
            }))
        }
    },
    watch: {
        machineOptions() { if (this.selectionOpen) this.closeSelection() },
        disabled(value: boolean) { if (value && this.selectionOpen) this.closeSelection() }
    },
    methods: {
        openSelection(machineId = 0) {
            if (this.disabled) return
            this.selectionMachineId = machineId
            this.draftMachineIds = [...this.selectedMachineIds]
            this.draftUserIds = Object.fromEntries(this.modelValue.map(machine => [machine.machineId, machine.users.map(user => user.userId)]))
            this.selectionOpen = true
                ; (this.$refs.machineDialog as DialogRef).open()
        },
        closeSelection() {
            this.selectionOpen = false
                ; (this.$refs.machineDialog as DialogRef).close()
        },
        toggleMachine(machineId: number, checked: boolean) {
            if (this.disabled || !this.selectionOpen || !this.selectionOptions.some(machine => machine.id === machineId)) return
            this.draftMachineIds = checked ? [...new Set([...this.draftMachineIds, machineId])] : this.draftMachineIds.filter(id => id !== machineId)
        },
        toggleUser(machineId: number, userId: number, checked: boolean) {
            if (this.disabled || !this.selectionOpen || !this.draftMachineIds.includes(machineId) || !this.selectionOptions.some(machine => machine.id === machineId) || !this.users(machineId).some(user => user.id === userId)) return
            const selected = this.draftUserIds[machineId] ?? []
            this.draftUserIds[machineId] = checked ? [...new Set([...selected, userId])] : selected.filter(id => id !== userId)
        },
        confirmSelection() {
            if (this.disabled || !this.selectionOpen) return
            const selected = this.machineOptions.filter(machine => this.draftMachineIds.includes(machine.id))
            // 保留已有分配；无人员时按设备分配，有人员时按人员分配。
            const ids = [...this.selectedMachineIds.filter(id => selected.some(machine => machine.id === id)),
            ...selected.filter(machine => !this.selectedMachineIds.includes(machine.id)).map(machine => machine.id)]
            const result = ids.map(machineId => {
                const old = this.modelValue.find(machine => machine.machineId === machineId)
                const users = this.users(machineId).filter(user => (this.draftUserIds[machineId] ?? []).includes(user.id))
                const userIds = [...(old?.users ?? []).filter(user => users.some(item => item.id === user.userId)).map(user => user.userId),
                ...users.filter(user => !old?.users.some(item => item.userId === user.id)).map(user => user.id)]
                const result = new ScheduleMachineEditDto({
                    ...old, machineId, users: userIds.map(userId =>
                        new SchedulePersonEditDto(old?.users.find(user => user.userId === userId) ?? { userId }))
                })
                if (old?.users.length && !result.users.length) {
                    result.quantity = old.users.reduce((sum, user) => sum + user.quantity, 0)
                    result.hours = old.users.reduce((sum, user) => sum + user.hours, 0)
                }
                if (old && !old.users.length && result.users.length) {
                    let quantity = Number(old.quantity) || 0, hours = Number(old.hours) || 0
                    result.users.forEach((user, index) => {
                        user.quantity = Number((quantity / (result.users.length - index)).toFixed(4))
                        user.hours = Number((hours / (result.users.length - index)).toFixed(4))
                        quantity = Number((quantity - user.quantity).toFixed(4))
                        hours = Number((hours - user.hours).toFixed(4))
                    })
                }
                return result
            })
            const added = result.flatMap<{ quantity: number }>(machine => !machine.users.length
                ? (!machine.quantity && !this.modelValue.some(old => old.machineId === machine.machineId && !old.users.length) ? [machine] : [])
                : machine.users.filter(user => !this.modelValue.find(old => old.machineId === machine.machineId)?.users.some(old => old.userId === user.userId) && user.quantity === 0))
            const assigned = result.reduce((sum, machine) => sum + (machine.users.length ? machine.users.reduce((n, user) => n + user.quantity, 0) : machine.quantity), 0)
            let remaining = Number(Math.max(0, this.quantity - assigned).toFixed(4))
            added.forEach((user, index) => {
                user.quantity = Number((remaining / (added.length - index)).toFixed(4))
                remaining = Number((remaining - user.quantity).toFixed(4))
            })
            this.$emit('update:modelValue', result)
            this.closeSelection()
        },
        applyHours() {
            const hours = this.batchHours
            if (this.disabled || hours == null || !Number.isFinite(hours) || hours < 0 || hours > 99999999.9999 || Number(hours.toFixed(4)) !== hours) return
            this.$emit('update:modelValue', this.modelValue.map(machine => new ScheduleMachineEditDto({
                ...machine, hours: machine.users.length ? machine.hours : hours,
                users: machine.users.map(user => new SchedulePersonEditDto({ ...user, hours }))
            })))
        },
        users(machineId: number) { return this.machineOptions.find(machine => machine.id === machineId)?.users ?? [] },
        userIds(machineId: number): number[] { return this.modelValue.find(machine => machine.machineId === machineId)?.users.map(user => user.userId) ?? [] },
        changeMachines(ids: number[] | null) {
            if (this.disabled) return
            const allowed = new Set(this.machineOptions.map(machine => machine.id))
            this.$emit('update:modelValue', [...new Set(ids ?? [])].filter(id => allowed.has(id)).map(id =>
                this.modelValue.find(machine => machine.machineId === id) ?? new ScheduleMachineEditDto({ machineId: id })))
        },
        changeUsers(machineId: number, ids: number[] | null) {
            if (this.disabled) return
            const allowed = new Set(this.users(machineId).map(user => user.id))
            const remaining = Number(Math.max(0, this.quantity - this.totalQuantity).toFixed(4))
            const selected = [...new Set(ids ?? [])].filter(id => allowed.has(id))
            this.$emit('update:modelValue', this.modelValue.map(machine => machine.machineId !== machineId ? machine
                : new ScheduleMachineEditDto({
                    ...machine, users: selected.map((id, index) =>
                        machine.users.find(user => user.userId === id) ?? new SchedulePersonEditDto({
                            userId: id, quantity: !machine.users.length ? (index === selected.length - 1
                                ? Number((remaining - Number((remaining / selected.length).toFixed(4)) * index).toFixed(4))
                                : Number((remaining / selected.length).toFixed(4))) : 0
                        }))
                })))
        },
        updateUser(row: { machineId: number; userId: number }, field: 'quantity' | 'hours', value: number | null) {
            if (this.disabled) return
            this.$emit('update:modelValue', this.modelValue.map(machine => machine.machineId !== row.machineId ? machine
                : new ScheduleMachineEditDto({
                    ...machine, ...(!row.userId && !machine.users.length ? { [field]: Number(value) || 0 } : {}),
                    users: machine.users.map(user => user.userId !== row.userId ? user
                        : new SchedulePersonEditDto({ ...user, [field]: Number(value) || 0 }))
                })))
        }
    }
})
</script>
