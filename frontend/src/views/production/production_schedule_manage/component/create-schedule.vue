<template>
    <div class="kingzon-page-content schedule-editor" style="min-height: 0; min-width: 0; overflow: hidden">
        <Loading v-show="isLoading" />
        <div class="kingzon-page-fill schedule-editor-scroll" style="min-height: 0; overflow-y: auto; overflow-x: hidden; padding: 0 8px">
            <div style="display: flex; justify-content: space-between; align-items: center; padding: 6px 0">
                <div style="min-width: 0">
                    <strong style="font-size: 14px">生产排产单</strong>
                    <div class="kingzon-text-grey" style="padding-top: 4px; overflow-wrap: anywhere">
                        {{ template.productName }} · {{ template.productCode }}
                    </div>
                </div>
                <span class="kingzon-tag kingzon-tag-info">编辑中</span>
            </div>
            <Panel title="排产基本信息" class="schedule-module" :bodyStyle="{ padding: '6px' }" style="margin-bottom: 8px">
                <div class="kingzon-text-grey" style="padding: 4px 0">
                    工艺版本 {{ template.version || '—' }}
                </div>
                <Form :model="form" :labelWidth="68" labelAlign="right" class="schedule-basic-form">
                    <FormField name="quantity" label="排产数量:" style="min-width: 0; margin: 0">
                        <div style="display: flex; align-items: center; gap: 4px; min-width: 0">
                            <NumberBox v-model="form.quantity" :min="0.0001" :max="99999999.9999" :precision="4"
                                :disabled="saving" style="width: 0; flex: 1; min-width: 0; height: 28px" />
                            <span class="kingzon-text-grey" style="flex-shrink: 0">{{ template.quantityUnit }}</span>
                        </div>
                    </FormField>
                    <FormField name="departmentId" label="派工部门:" style="min-width: 0; margin: 0">
                        <ComboBox v-model="form.departmentId" :data="template.departments" :editable="false"
                            :disabled="saving" style="width: 100%; height: 28px" />
                    </FormField>
                    <FormField name="start" label="计划开始:" style="min-width: 0; margin: 0">
                        <DateTimeBox v-model="start" format="yyyy-MM-dd HH:mm" :disabled="saving" style="width: 100%; height: 28px" />
                    </FormField>
                    <FormField name="end" label="计划完成:" style="min-width: 0; margin: 0">
                        <DateTimeBox v-model="end" format="yyyy-MM-dd HH:mm" :disabled="saving" style="width: 100%; height: 28px" />
                    </FormField>
                    <FormField name="urgent" label="加急:" style="min-width: 0; margin: 0">
                        <CheckBox v-model="form.urgent" :disabled="saving" />
                    </FormField>
                    <FormField name="remark" label="备注:" style="grid-column: 1 / -1; min-width: 0; margin: 0">
                        <TextBox v-model="form.remark" :multiline="true" :disabled="saving"
                            style="width: 100%; height: 45px" />
                    </FormField>
                </Form>
            </Panel>
            <section class="schedule-route-module">
                <div class="schedule-route-heading">
                    <strong>工序路线 <small class="kingzon-text-grey">{{ processes.length }} 道</small></strong>
                    <div>
                        <LinkButton :disabled="!allowProcessEdit || isLoading || saving" @click="openAddProcess">新增工序
                        </LinkButton>
                        <span v-if="!allowProcessEdit" class="kingzon-text-grey" style="margin-left: 8px">工序调整由系统参数控制</span>
                    </div>
                </div>
                <ScheduleProcessCard v-for="(process, index) in processes" :key="process.key" :process="process"
                    :index="index" :count="processes.length" :quantity="Number(form.quantity) || 0"
                    :expanded="expandedKey === process.key" :editable="allowProcessEdit" :saving="saving"
                    :crafts="template.crafts" :machineOptions="machinesFor(process)"
                    @toggle="toggleProcess(process.key)"
                    @move="moveProcess($event, process.key)" @copy="copyProcess(process)"
                    @remove="removeProcess(process.key)" @materials="openMaterials(process)"
                    @updateProcess="updateProcess(process, $event)" @dragStart="dragKey = $event" @dragEnd="dragKey = ''"
                    @dropProcess="dropProcess" />
            </section>
            <Panel class="schedule-module" title="统计汇总" :bodyStyle="{ padding: '8px' }" style="margin-top: 8px">
                <div style="display: flex; flex-wrap: wrap; gap: 4px 12px; line-height: 22px">
                    <span>共 {{ summary.processCount }} 道工序</span><span>{{ summary.machineCount }} 台设备</span>
                    <span>{{ summary.userCount }} 人 · {{ summary.allocations }} 条人员分配</span>
                    <span>人工工时 {{ summary.humanHours }} 人时</span><span>计划历时 {{ plannedHours ?? '—' }} h</span>
                    <span>物料 {{ summary.materialCount }} 种 · 齐套 {{ summary.readyCount }} 种 · 缺料
                        {{ summary.shortageCount }} 种 · 未查询 {{ summary.uncheckedCount }} 种</span>
                </div>
                <div class="kingzon-text-grey" style="padding-top: 4px">
                    人工工时为人员工时之和，不能代替设备工时或工序历时。
                </div>
            </Panel>
            <p v-if="loadError" style="color: var(--color-red)">{{ loadError }}</p>
            <p v-if="stockError" style="color: var(--color-red)">{{ stockError }}</p>
            <div v-if="errors.length" style="color: var(--color-red)">
                <p v-for="error in errors" :key="error">{{ error }}</p>
            </div>
        </div>
        <div class="kingzon-btn-group kingzon-page-footer schedule-editor-footer" style="flex-shrink: 0">
            <LinkButton :disabled="saving" @click="$emit('cancel')">取消</LinkButton>
            <LinkButton iconCls="icon-save" :disabled="isLoading || saving || !!loadError || !processes.length"
                @click="submit">保存排产单</LinkButton>
        </div>
        <Dialog ref="addProcessDialog" title="新增工序" :modal="true" :closed="true"
            :dialogStyle="{ width: '440px', maxWidth: '95vw' }">
            <div style="padding: 12px">
                <span>工序来源：</span>
                <ComboBox v-model="addSource" :data="addSourceOptions" :editable="false" style="width: 290px" />
            </div>
            <div class="kingzon-btn-group kingzon-page-footer">
                <LinkButton @click="closeAddProcess">取消</LinkButton>
                <LinkButton @click="confirmAddProcess">确定</LinkButton>
            </div>
        </Dialog>
    </div>
</template>

<script lang="ts">
import { defineComponent } from 'vue'
import ScheduleProcessCard from './schedule-process-card.vue'
import {
    ProductionSchedulePostDto,
    ProductionScheduleTemplateDto,
    ScheduleProcessEditDto,
    type ScheduleMaterialEditDto,
    type QuantityResultDto,
    type MaterialTotalDto
} from '@/types/schedule'
import {
    createProductionScheduleDraft,
    productionSchedulePost,
    validateProductionSchedule,
    localDateTime,
    scheduleMaterialAvailability,
    scheduleEditorSummary,
    validProductionQuantity,
    quantityApi
} from '../schedule'

interface DialogRef {
    open(): void
    close(): void
}
export default defineComponent({
    name: 'CreateSchedule',
    components: { ScheduleProcessCard },
    props: {
        craftVersionId: { type: Number, required: true },
        // 宿主读取允许调整排产工序的系统参数后传入，不在此虚构系统参数编码。
        allowProcessEdit: { type: Boolean, default: false },
        initialQuantity: { type: Number, default: 1 },
        saving: Boolean
    },
    emits: ['submit', 'cancel'],
    data() {
        return {
            isLoading: false,
            loadError: '',
            requestToken: 0,
            nextKey: 1,
            selectedKey: '',
            expandedKey: '',
            dragKey: '',
            addSource: '',
            materialProcessKey: '',
            materialPreview: null as ScheduleMaterialEditDto[] | null,
            stocks: [] as MaterialTotalDto[],
            stockToken: 0,
            checkingStock: false,
            stockError: '',
            form: new ProductionSchedulePostDto(),
            template: new ProductionScheduleTemplateDto(),
            processes: [] as ScheduleProcessEditDto[],
            start: null as Date | null,
            end: null as Date | null,
            errors: [] as string[]
        }
    },
    computed: {
        currentProcess(): ScheduleProcessEditDto | undefined {
            return this.processes.find((process) => process.key === this.selectedKey)
        },
        materialProcess(): ScheduleProcessEditDto | undefined {
            return this.processes.find((process) => process.key === this.materialProcessKey)
        },
        effectiveProcesses(): ScheduleProcessEditDto[] {
            return this.materialPreview == null
                ? this.processes
                : this.processes.map((process) =>
                    process.key === this.materialProcessKey
                        ? new ScheduleProcessEditDto({ ...process, materials: this.materialPreview! })
                        : process
                )
        },
        availability() {
            return scheduleMaterialAvailability(this.effectiveProcesses, this.stocks)
        },
        summary() {
            return scheduleEditorSummary(this.effectiveProcesses, this.availability)
        },
        plannedHours(): number | null {
            if (
                !this.start ||
                !this.end ||
                !Number.isFinite(this.start.getTime()) ||
                !Number.isFinite(this.end.getTime()) ||
                this.end <= this.start
            )
                return null
            return Number(((this.end.getTime() - this.start.getTime()) / 3600000).toFixed(4))
        },
        addSourceOptions(): Array<{ value: string; text: string }> {
            return [
                { value: '', text: '临时工序' },
                ...this.template.processs.map((process, index) => ({
                    value: String(index),
                    text: `工艺模板：${process.code || index + 1} ${process.name}`
                }))
            ]
        }
    },
    watch: {
        craftVersionId: {
            immediate: true,
            handler() {
                this.init()
            }
        },
        'form.quantity'(value: number, previous: number) {
            if (!Number.isFinite(value) || value < 0) return
            for (const process of this.processes) {
                for (const material of process.materials)
                    material.quantity = Number((material.quantityPerUnit * value).toFixed(6))
                const users = process.machines.flatMap((machine) => machine.users)
                if (
                    previous > 0 &&
                    Math.abs(users.reduce((sum, user) => sum + user.quantity, 0) - previous) < 0.000001
                ) {
                    let remaining = value
                    users.forEach((user, index) => {
                        user.quantity =
                            index === users.length - 1
                                ? Number(remaining.toFixed(4))
                                : Number(((user.quantity / previous) * value).toFixed(4))
                        remaining -= user.quantity
                    })
                }
            }
        }
    },
    beforeUnmount() {
        this.requestToken++
        this.stockToken++
    },
    methods: {
        async init() {
            const token = ++this.requestToken
            this.stockToken++
            this.stocks = []
            this.stockError = ''
            this.checkingStock = false
            this.closeMaterials()
            this.dragKey = ''
            this.processes = []
            this.selectedKey = ''
            this.expandedKey = ''
            this.loadError = ''
            this.errors = []
            this.template = new ProductionScheduleTemplateDto()
            this.form = new ProductionSchedulePostDto({
                craftVersionId: this.craftVersionId,
                quantity: this.initialQuantity
            })
            this.start = null
            this.end = null
            if (!Number.isSafeInteger(this.craftVersionId) || this.craftVersionId <= 0) {
                this.isLoading = false
                this.loadError = '请选择有效的工艺版本'
                return
            }
            this.isLoading = true
            try {
                const response = await this.$http.get(
                    '/api/production/ProductionScheduleManage/GetScheduleTemplate',
                    { craftVersionId: this.craftVersionId }
                )
                if (token !== this.requestToken) return
                const data = response.data as ProductionScheduleTemplateDto
                if (data?.craftVersionId !== this.craftVersionId || !Array.isArray(data.processs))
                    throw new Error('工艺模板返回数据无效')
                this.template = new ProductionScheduleTemplateDto(data)
                this.processes = createProductionScheduleDraft(this.template, this.form.quantity)
                this.selectedKey = this.processes[0]?.key ?? ''
            } catch (error) {
                if (token === this.requestToken) {
                    this.loadError = '工艺模板加载失败，请重新打开'
                    this.$kingzon_messager.showError(error)
                }
            } finally {
                if (token === this.requestToken) this.isLoading = false
            }
        },
        toggleProcess(key: string) {
            if (this.saving || !this.processes.some(process => process.key === key)) return
            this.selectedKey = key
            this.expandedKey = this.expandedKey === key ? '' : key
        },
        machinesFor(process: ScheduleProcessEditDto) {
            return this.template.machines.filter((machine) => machine.craftIds.includes(process.craftId))
        },
        uniqueCode(prefix?: string): string {
            const codes = new Set(this.processes.map((process) => process.code))
            let number = prefix
                ? 1
                : Math.max(
                    0,
                    ...this.processes.map((process) => Number(process.code)).filter(Number.isFinite)
                ) + 10
            let code = ''
            do {
                code = prefix ? `${prefix.slice(0, 40)}-C${number++}` : String(number).padStart(3, '0')
                if (!prefix) number += 10
            } while (codes.has(code))
            return code
        },
        openAddProcess() {
            if (!this.allowProcessEdit || this.saving || this.isLoading) return
            this.addSource = ''
            const dialog = this.$refs.addProcessDialog as DialogRef
            dialog.open()
        },
        closeAddProcess() {
            const dialog = this.$refs.addProcessDialog as DialogRef
            dialog.close()
        },
        confirmAddProcess() {
            if (!this.allowProcessEdit || this.saving || this.isLoading) return
            const source =
                this.addSource === '' ? undefined : this.template.processs[Number(this.addSource)]
            if (this.addSource !== '' && !source) return
            this.addProcess(source)
            this.closeAddProcess()
        },
        addProcess(source?: ScheduleProcessEditDto) {
            if (!this.allowProcessEdit || this.saving || this.isLoading) return
            const process = source
                ? createProductionScheduleDraft(
                    new ProductionScheduleTemplateDto({ processs: [source] }),
                    Number(this.form.quantity) || 0
                )[0]
                : new ScheduleProcessEditDto({ name: '新工序' })
            this.insertProcess(process, this.processes.length, this.uniqueCode())
        },
        copyProcess(source: ScheduleProcessEditDto) {
            if (!this.allowProcessEdit || this.saving || !this.processes.includes(source)) return
            const process = new ScheduleProcessEditDto(JSON.parse(JSON.stringify(source)))
            process.name = `${source.name.slice(0, 95)}（副本）`
            this.insertProcess(process, this.processes.indexOf(source) + 1, this.uniqueCode(source.code || 'P'))
        },
        insertProcess(process: ScheduleProcessEditDto, index: number, code: string) {
            process.key = `added-${this.nextKey++}`
            process.code = code
            process.materials.forEach((material, position) => {
                material.key = `${process.key}-material-${position}`
            })
            this.processes.splice(index, 0, process)
            this.selectedKey = process.key
            this.expandedKey = process.key
        },
        moveProcess(step: number, key?: string) {
            key ??= this.selectedKey
            if (!this.allowProcessEdit || this.saving) return
            const index = this.processes.findIndex((process) => process.key === key),
                target = index + step
            if (index < 0 || target < 0 || target >= this.processes.length) return
            const [process] = this.processes.splice(index, 1)
            this.processes.splice(target, 0, process)
            this.selectedKey = key
        },
        dropProcess(targetKey: string) {
            const key = this.dragKey
            this.dragKey = ''
            if (!this.allowProcessEdit || this.saving || !key || key === targetKey) return
            const source = this.processes.findIndex((process) => process.key === key),
                target = this.processes.findIndex((process) => process.key === targetKey)
            if (source < 0 || target < 0) return
            const [process] = this.processes.splice(source, 1)
            this.processes.splice(target, 0, process)
            this.selectedKey = key
        },
        removeProcess(key?: string) {
            key ??= this.selectedKey
            if (!this.allowProcessEdit || this.saving) return
            const source = this.processes.find((process) => process.key === key)
            if (!source) return
            const token = this.requestToken
            const remove = () => {
                if (this.saving || !this.allowProcessEdit || token !== this.requestToken) return
                const index = this.processes.findIndex((process) => process.key === key)
                if (index < 0) return
                if (this.materialProcessKey === key) this.closeMaterials()
                this.processes.splice(index, 1)
                if (this.expandedKey === key) this.expandedKey = ''
                if (this.selectedKey === key)
                    this.selectedKey = this.processes[Math.min(index, this.processes.length - 1)]?.key ?? ''
            }
            if (source.materials.length || source.machines.length)
                return this.$kingzon_messager
                    .showConfim(`工序【${source.name}】已配置物料或设备，确认删除该草稿工序？`)
                    .then(remove)
                    .catch(() => { })
            remove()
        },
        updateProcess(process: ScheduleProcessEditDto, changes: Partial<ScheduleProcessEditDto>) {
            if (this.saving || !this.processes.includes(process)) return
            if (changes.machines) process.machines = changes.machines
            if (!this.allowProcessEdit) return
            for (const key of ['code', 'name', 'craftId', 'isOutsource', 'isInbound', 'isReportWork'] as const) {
                if (changes[key] !== undefined) Object.assign(process, { [key]: changes[key] })
            }
            if (changes.craftId !== undefined)
                process.machines = process.machines.filter((machine) =>
                    this.machinesFor(process).some((item) => item.id === machine.machineId)
                )
        },
        async openMaterials(process: ScheduleProcessEditDto) {
            if (this.saving || this.isLoading || !this.processes.includes(process)) return
            this.materialProcessKey = process.key
            await this.$nextTick()
            if (this.materialProcessKey === process.key)
                (this.$refs.materialDialog as DialogRef | undefined)?.open()
        },
        closeMaterials() {
            this.materialProcessKey = ''
            this.materialPreview = null
        },
        applyMaterials(materials: ScheduleMaterialEditDto[]) {
            if (this.saving || !this.materialProcess) return
            this.materialProcess.materials = materials
        },
        async checkStock() {
            if (this.checkingStock || this.saving) return
            if (!validProductionQuantity(Number(this.form.quantity))) {
                this.stockError = '请先填写有效的排产数量'
                return
            }
            const token = ++this.stockToken,
                version = this.craftVersionId
            this.checkingStock = true
            this.stockError = ''
            try {
                const response = await this.$http.post(
                    quantityApi,
                    {
                        craftVersionId: version,
                        quantity: Number(this.form.quantity)
                    }
                )
                if (token !== this.stockToken || version !== this.craftVersionId) return
                const data = response.data as QuantityResultDto
                if (data?.craftVersionId !== version || !Array.isArray(data.materials))
                    throw new Error('库存查询返回数据无效')
                this.stocks = data.materials
            } catch (error) {
                if (token === this.stockToken) {
                    this.stockError = '库存校验失败，当前保留上次成功查询结果'
                    this.$kingzon_messager.showError(error)
                }
            } finally {
                if (token === this.stockToken) this.checkingStock = false
            }
        },
        getPostData(): ProductionSchedulePostDto | null {
            if (this.isLoading || this.saving || this.loadError) return null
            if (this.materialProcessKey) {
                this.errors = ['请先确认或取消物料配置']
                return null
            }
            this.form.start =
                this.start && Number.isFinite(this.start.getTime()) ? localDateTime(this.start) : ''
            this.form.end = this.end && Number.isFinite(this.end.getTime()) ? localDateTime(this.end) : ''
            const value = productionSchedulePost(this.form, this.processes)
            this.errors = validateProductionSchedule(value, this.template)
            if (
                this.processes.some((process) => !process.isOutsource && process.machines.some((machine) => !machine.users.length))
            )
                this.errors.push('已选设备请配置人员')
            return this.errors.length ? null : value
        },
        submit() {
            const value = this.getPostData()
            if (value) this.$emit('submit', value)
        }
    }
})
</script>

<style scoped>
.schedule-editor { background: #fff; }
.schedule-editor-scroll { overscroll-behavior: contain; scrollbar-gutter: stable; }
.schedule-basic-form { display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); align-items: start; gap: 6px 8px; padding: 0; }
.schedule-basic-form :deep(.textbox) { max-width: 100%; box-sizing: border-box; }
.schedule-route-module { border: 1px solid #e5e7eb; background: #fff; padding: 6px; }
.schedule-route-heading { display: flex; align-items: center; flex-wrap: wrap; justify-content: space-between; gap: 4px 8px; padding-bottom: 6px; }
.schedule-module :deep(.panel-header), .schedule-module :deep(.panel-body) { background: #fff; border-color: #e5e7eb; }
.schedule-editor-footer { display: flex; justify-content: flex-end; align-items: center; gap: 6px; margin: 0; padding: 6px 8px; border-top: 1px solid #e5e7eb; background: #fff; z-index: 1; }
.schedule-editor-footer :deep(.l-btn) { margin: 0; }
</style>
