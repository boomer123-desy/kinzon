<template>
    <Loading v-show="isLoading" />
    <div class="kingzon-page" style="min-height: 0; min-width: 0">
        <div class="kingzon-page-title"><span>工序单排产管理</span></div>
        <div class="kingzon-page-body" style="flex: 1; min-height: 0; overflow: hidden">
            <div class="kingzon-page-content" style="min-height: 0">
                <div class="kingzon-page-fill"
                    style="display: flex; flex-direction: row; min-height: 0; overflow: auto; gap: 8px">
                    <div
                        style="display: flex; flex-direction: column; flex: 68 1 0; min-width: 560px; min-height: 0; overflow: hidden">
                        <div class="kingzon-btn-group" style="margin: 5px; flex-shrink: 0">
                            <LinkButton iconCls="icon-reload" :disabled="isLoading" @click="refreshStock">刷新库存
                            </LinkButton>
                            <LinkButton :disabled="isLoading" @click="recalculate">重新计算</LinkButton>
                            <LinkButton @click="openGantt()">工序甘特图</LinkButton>
                        </div>
                        <Panel title="计划信息" :collapsible="true" :bodyStyle="{ padding: '0' }" style="flex-shrink: 0">
                            <div style="display: flex; align-items: center; flex-wrap: wrap; gap: 8px; padding: 5px">
                                <LinkButton :disabled="isLoading" @click="openCraftPicker">选择工序单</LinkButton>
                                <span>{{ product.orderCode || '请先选择工序单及版本' }}</span>
                                <span v-if="queryError" style="color: var(--color-red)" role="alert">{{ queryError
                                }}</span>
                            </div>
                            <KingZonShowForm :formData="showFormData" columnWidth="20%" lableWidth="100px" />
                            <div style="display: flex; align-items: center; flex-wrap: wrap; gap: 24px; padding: 5px">
                                <div style="display: flex; align-items: center; gap: 8px">
                                    <span>产品数量</span>
                                    <NumberBox v-model="quantityInput" :min="0.0001" :max="99999999.9999" :precision="4"
                                        :spinners="true" style="width: 90px" /><span>{{ product.unit }}</span>
                                </div>
                                <div style="display: flex; align-items: center; gap: 8px">
                                    <span>半成品</span><strong>{{ semiNodes.length }}</strong><span>待排产</span><strong
                                        style="color: var(--color-yellow)">{{
                                            pendingNodes.length }}</strong>
                                </div>
                                <div style="display: flex; align-items: center; gap: 8px">
                                    <span>物料</span><strong>{{ materialNodes.length }}</strong><span>缺料</span><strong
                                        style="color: var(--color-red)">{{
                                            insufficientMaterials.length }}</strong>
                                </div>
                                <div style="display: flex; align-items: center; gap: 8px"
                                    title="库存缺量为 0 的有效需求节点数 / 总有效需求节点数">
                                    <span>需求满足率</span><strong>{{ quantityResult ? kitRate + '%' : '—'
                                    }}</strong><small>库存口径</small>
                                </div>
                            </div>
                            <div style="display: flex; align-items: center; flex-wrap: wrap; gap: 8px; padding: 5px">
                                <label>计划开始
                                    <DateTimeBox v-model="startInput" format="yyyy-MM-dd HH:mm" style="width: 170px" />
                                </label>
                                <label>需求交期
                                    <DateTimeBox v-model="endInput" format="yyyy-MM-dd HH:mm" style="width: 170px" />
                                </label>
                                <span>需求版本 R{{ revision }}</span><span v-if="staleOrderCount"
                                    style="color: var(--color-yellow)">{{
                                        staleOrderCount }}
                                    张旧版本排产单待复核</span>
                            </div>
                        </Panel>
                        <div
                            style="display: flex; align-items: center; justify-content: space-between; flex-wrap: wrap; gap: 8px; padding: 5px 0; flex-shrink: 0">
                            <div class="kingzon-btn-group">
                                <LinkButton plain :disabled="isFiltering" @click="expandAll">全部展开</LinkButton>
                                <LinkButton plain :disabled="isFiltering" @click="expandedIds = []">全部收起
                                </LinkButton>
                            </div>
                            <div style="display: flex; align-items: center; gap: 8px">
                                <TextBox v-model="keyword" placeholder="名称 / 编码 / 规格" style="width: 200px" />
                                <ComboBox v-model="statusFilter" :data="statusOptions" :editable="false"
                                    style="width: 130px" />
                                <LinkButton plain @click="resetFilters">清除筛选</LinkButton>
                            </div>
                        </div>
                        <ScheduleMaterialTree :dataList="treeRows" :selection="selectedTreeRow"
                            :requirements="requirements" :context="context" @select="selectRow" @expand="onExpand"
                            @collapse="onCollapse" @material="openMaterial" />
                        <div v-if="!treeRows.length" class="kingzon-text-grey" style="padding: 8px; text-align: center">
                            没有符合条件的数据</div>
                        <div class="kingzon-text-grey"
                            style="display: flex; flex-wrap: wrap; justify-content: space-between; gap: 8px; padding: 8px 0; flex-shrink: 0">
                            <span>共 {{ quantityResult?.nodeCount ?? 0 }} 个需求节点 ·
                                缺量仅抵扣可分配库存</span><span>在途、在制、排产数量未查询</span>
                        </div>
                    </div>
                    <div
                        style="display: flex; flex-direction: column; flex: 32 1 0; min-width: 380px; min-height: 0; overflow: hidden">
                        <ScheduleOrderList v-if="selectedSemi" :node="selectedSemi" :dataList="scheduleListRows"
                            :selectedRow="selectedListRow" :selectedOrder="selectedOrder" :hasDraft="!!currentDraft"
                            :readOnly="!!quantityResult" :shortage="shortage(selectedSemi)" @select="selectListRow"
                            @add="openSchedule(selectedSemi)" @cancelDraft="cancelDraftRow" @cancelOrder="cancelOrder"
                            @openGantt="openGantt" />
                        <div v-else class="kingzon-text-grey" style="padding: 24px 8px; text-align: center">
                            点击左侧半成品，查看排产单并创建排产
                        </div>
                        <CreateSchedule v-for="draft in drafts"
                            v-show="selectedId === draft.nodeId && selectedOrderId === draft.id" :key="draft.id"
                            :craftVersionId="draftNode(draft.nodeId).serverMaterial?.craftVersionId ?? selectedCraftVersionId"
                            :allowProcessEdit="allowProcessEdit" style="flex: 1 1 0; height: 0; min-height: 0; overflow: hidden"
                            @submit="$emit('scheduleSubmit', $event)" @cancel="closeScheduleDraft" />
                    </div>
                </div>
            </div>
        </div>
    </div>
    <ScheduleGantt ref="scheduleGantt" :nodes="nodes" :planStart="context.start" :planEnd="context.end"
        :revision="revision" :quantity="quantity" :product="product" @cancelOrder="cancelOrder"
        @reschedule="rescheduleOrder" />
    <Dialog ref="materialDialog" title="物料齐套 / 采购建议" :modal="true" :closed="true"
        :dialogStyle="{ width: '620px', maxWidth: '95vw' }">
        <div v-if="materialTarget" style="padding: 8px">
            <strong>{{ materialTarget.name }} / {{ materialTarget.code }}</strong>
            <p>
                需求来源：{{ requirements[materialTarget.id].source }} · 需求日期
                {{ formatDate(requirements[materialTarget.id].requiredDate) }}
            </p>
            <div style="display: flex; flex-wrap: wrap; align-items: center; gap: 8px">
                <span>总需求 {{ requirements[materialTarget.id].gross }}</span>
                <span>库存分配 {{ requirements[materialTarget.id].available }}</span>
                <span>预计入库 {{ requirements[materialTarget.id].incoming }}</span>
                <strong style="color: var(--color-red)">
                    净需求 {{ requirements[materialTarget.id].net }}
                    {{ materialTarget.unit }}
                </strong>
            </div>
            <p class="kingzon-text-grey">
                {{ quantityResult ? '当前仅查询工序领用物料及库存，采购与排产接口尚未接入。' : '采购建议仅登记本地预计到料，未连接采购或仓库系统。' }}
            </p>
            <Form v-if="!quantityResult && shortage(materialTarget) > 0" :model="receiptForm" :labelWidth="90"
                labelAlign="right">
                <FormField name="quantity" label="建议采购量:">
                    <NumberBox v-model="receiptForm.quantity" :min="0.01" :precision="2" style="width: 100%" />
                </FormField>
                <FormField name="arrival" label="预计到料:">
                    <DateTimeBox v-model="receiptForm.arrival" format="yyyy-MM-dd HH:mm" style="width: 100%" />
                </FormField>
                <FormField name="supplier" label="供应商:">
                    <TextBox v-model="receiptForm.supplier" style="width: 100%" />
                </FormField>
            </Form>
            <div v-for="receipt in plannedReceipts(materialTarget)" :key="receipt.id"
                style="display: flex; align-items: center; justify-content: space-between; gap: 8px; padding: 6px 0">
                <span>{{ receipt.id }} · {{ receipt.quantity }} {{ materialTarget.unit }} ·
                    {{ formatDate(receipt.arrival) }} · {{ receipt.supplier }}</span>
                <LinkButton plain @click="receipt.status = ScheduleReceiptStatus.已撤销">撤销建议</LinkButton>
            </div>
            <p v-if="receiptError" style="color: var(--color-red)" role="alert">{{ receiptError }}</p>
        </div>
        <div class="kingzon-btn-group kingzon-page-footer">
            <LinkButton @click="closeDialog('materialDialog')">关闭</LinkButton>
            <LinkButton v-if="!quantityResult && materialTarget && shortage(materialTarget) > 0" btnCls="c8"
                @click="saveReceipt">保存采购建议
            </LinkButton>
        </div>
    </Dialog>
</template>

<script lang="ts">
import { ProductionScheduleStatus, ScheduleReceiptStatus, MaterialRequirementStatus } from '@/types/enums'
import { defineComponent } from 'vue'
import { ShowFormDataType } from '@/types/enums'
import CreateSchedule from './component/create-schedule.vue'
import ScheduleGantt from './component/schedule-gantt.vue'
import ScheduleOrderList from './component/schedule-order-list.vue'
import ScheduleMaterialTree from './component/schedule-material-tree.vue'
import {
    flattenTree,
    localDateTime,
    calculateRequirements,
    materialPlans,
    activeOrders,
    validateSchedule,
    snapshot,
    validQuantity,
    timestamp,
    round,
    scheduleChange,
    materialTree,
    materialRequirements,
    quantityApi,
    validProductionQuantity,
    materialCellValue
} from './schedule'
import {
    type ScheduleNodeDto,
    type ScheduleSubmissionDto,
    type ScheduleTreeRowDto,
    type ScheduleOrderDto,
    type PlanContextDto,
    type MaterialRequirementDto,
    type ScheduleTimeChangeDto,
    QuantityResultDto,
    ScheduleProductDto,
    ScheduleListRowDto,
    ScheduleDraftDto,
    ReceiptFormDto
} from '@/types/production-schedule'

export default defineComponent({
    name: 'ProductionScheduleManage',
    emits: ['scheduleSubmit'],
    components: { CreateSchedule, ScheduleGantt, ScheduleOrderList, ScheduleMaterialTree },
    props: {
        craftVersionId: { type: Number, default: 9168 },
        allowProcessEdit: { type: Boolean, default: false },
        initialQuantity: { type: Number, default: 100 }
    },
    data() {
        return {
            isLoading: false,
            product: new ScheduleProductDto(),
            quantityResult: null as QuantityResultDto | null,
            queryError: '',
            requestToken: 0,
            craftListToken: 0,
            versionRequestToken: 0,
            selectedCraftVersionId: 0,
            nodes: [] as ScheduleNodeDto[],
            quantityInput: this.initialQuantity as number | null,
            quantity: this.initialQuantity,
            revision: 1,
            startInput: new Date() as Date | null,
            endInput: new Date(Date.now() + 7 * 86400000) as Date | null,
            planStart: localDateTime(new Date()),
            planEnd: localDateTime(new Date(Date.now() + 7 * 86400000)),
            keyword: '',
            statusFilter: '' as MaterialRequirementStatus | '',
            selectedId: '',
            expandedIds: [] as string[],
            statusOptions: [
                { text: '全部', value: '' },
                ...Object.values(MaterialRequirementStatus).map(value => ({ text: value, value }))
            ],
            orderSequence: 6,
            receiptSequence: 1,
            scheduleTarget: null as ScheduleNodeDto | null,
            scheduleError: '',
            materialTarget: null as ScheduleNodeDto | null,
            drafts: [] as ScheduleDraftDto[],
            draftSequence: 1,
            selectedOrderId: '',
            receiptError: '',
            receiptForm: new ReceiptFormDto()
        }
    },
    created() {
        this.init()
    },
    beforeUnmount() {
        this.requestToken++
        this.craftListToken++
        this.versionRequestToken++
    },
    watch: {
        craftVersionId(value: number) {
            this.changeCraftVersion(value)
        },
        '$route.query.craftVersionId'(value: unknown) {
            if (!this.craftVersionId) this.changeCraftVersion(Number(value))
        }
    },
    computed: {
        ScheduleReceiptStatus() { return ScheduleReceiptStatus },
        context(): PlanContextDto {
            return {
                quantity: this.quantity,
                start: this.planStart,
                end: this.planEnd,
                revision: this.revision,
                product: this.product
            }
        },
        requirements(): Record<string, MaterialRequirementDto> {
            return this.quantityResult
                ? materialRequirements(this.nodes, this.context, this.quantityResult)
                : calculateRequirements(this.nodes, this.context)
        },
        showFormData() {
            const product = this.product
            return [
                ['产品编码', product.code],
                ['产品名称', product.name],
                ['客户产品编码', product.customerCode],
                ['产品规格', product.specification],
                ['技术型号', product.technicalCode],
                ['来源工序单', product.orderCode],
                ['工艺版本', product.version],
                ['当前计划数量', `${this.quantity} ${product.unit}`],
                ['需求版本', `R${this.revision}`],
                ['负责人', product.owner]
            ].map(([label, value]) => ({
                label,
                value,
                type: ShowFormDataType.文本,
                validate: true,
                tagClass: '',
                timeFormat: ''
            }))
        },
        allNodes(): ScheduleNodeDto[] {
            return flattenTree(this.nodes)
        },
        semiNodes(): ScheduleNodeDto[] {
            return this.allNodes.filter((node) => node.kind === 'semi')
        },
        materialNodes(): ScheduleNodeDto[] {
            return this.allNodes.filter((node) => node.kind === 'material')
        },
        pendingNodes(): ScheduleNodeDto[] {
            return this.semiNodes.filter((node) => this.shortage(node) > 0)
        },
        insufficientMaterials(): ScheduleNodeDto[] {
            return this.materialNodes.filter((node) => this.shortage(node) > 0)
        },
        kitRate(): number {
            const entries = Object.values(this.requirements).filter(
                (requirement) => requirement.gross > 0
            )
            return entries.length
                ? Math.round(
                    (entries.filter((requirement) => requirement.net === 0).length / entries.length) * 100
                )
                : 100
        },
        staleOrderCount(): number {
            return this.allNodes
                .flatMap((node) => activeOrders(node))
                .filter((order) => order.snapshot?.revision !== this.revision).length
        },
        selectedSemi(): ScheduleNodeDto | undefined {
            return this.semiNodes.find((node) => node.id === this.selectedId)
        },
        currentDraft(): ScheduleDraftDto | undefined {
            return this.drafts.find((draft) => draft.nodeId === this.selectedId)
        },
        selectedOrder(): ScheduleOrderDto | undefined {
            return this.selectedSemi?.orders.find((order) => order.id === this.selectedOrderId)
        },
        scheduleListRows(): ScheduleListRowDto[] {
            const rows = (this.selectedSemi?.orders ?? []).map((order) => new ScheduleListRowDto({
                id: order.id, quantity: order.quantity, start: order.start, end: order.end,
                status: order.status ?? ProductionScheduleStatus.已排产, draft: false
            })) as ScheduleListRowDto[]
            if (this.currentDraft) rows.push(new ScheduleListRowDto({ id: this.currentDraft.id, status: ProductionScheduleStatus.草稿, draft: true }))
            return rows
        },
        selectedListRow(): ScheduleListRowDto | null {
            return this.scheduleListRows.find((row) => row.id === this.selectedOrderId) ?? null
        },
        isFiltering(): boolean {
            return !!this.keyword.trim() || !!this.statusFilter
        },
        treeRows(): ScheduleTreeRowDto[] {
            const keyword = this.keyword.trim().toLowerCase()
            const visit = (nodes: ScheduleNodeDto[]): ScheduleTreeRowDto[] =>
                nodes.flatMap((node) => {
                    const children = visit(node.children)
                    const match =
                        (!keyword ||
                            `${node.name} ${node.code} ${node.specification}`.toLowerCase().includes(keyword)) &&
                        (!this.statusFilter || this.requirements[node.id].status === this.statusFilter)
                    if (this.isFiltering && !match && !children.length) return []
                    return [
                        {
                            ...node,
                            source: node,
                            children,
                            state:
                                !children.length || this.isFiltering || this.expandedIds.includes(node.id)
                                    ? ('open' as const)
                                    : ('closed' as const),
                            iconCls: node.kind === 'semi' ? 'tree-folder' : 'tree-file'
                        }
                    ]
                })
            return visit(this.nodes)
        },
        selectedTreeRow(): ScheduleTreeRowDto | null {
            const find = (rows: ScheduleTreeRowDto[]): ScheduleTreeRowDto | null => {
                for (const row of rows) {
                    if (row.id === this.selectedId) return row
                    const found = find(row.children)
                    if (found) return found
                }
                return null
            }
            return find(this.treeRows)
        }
    },
    methods: {
        init() {
            const routeId = Number(this.$route?.query.craftVersionId)
            this.selectedCraftVersionId = this.craftVersionId || (Number.isSafeInteger(routeId) && routeId > 0 ? routeId : 0)
            if (this.selectedCraftVersionId) this.loadMaterialQuantities()
        },
        async openCraftPicker() {
            this.openDialog('craftPicker')

        },
        changeCraftVersion(value: number) {
            const id = Number(value)
            if (id === this.selectedCraftVersionId && this.quantityResult?.craftVersionId === id) return
            this.requestToken++
            this.versionRequestToken++
            this.selectedCraftVersionId = Number.isSafeInteger(id) && id > 0 ? id : 0
            this.clearQuantityView()
            this.isLoading = false
            if (this.selectedCraftVersionId) this.loadMaterialQuantities()
        },
        clearQuantityView() {
            this.nodes = []
            this.quantityResult = null
            this.selectedId = ''
            this.selectedOrderId = ''
            this.scheduleTarget = null
            this.drafts = []
            this.materialTarget = null
            this.queryError = ''
            this.product = new ScheduleProductDto()
        },
        async loadMaterialQuantities() {
            const craftVersionId = this.selectedCraftVersionId
            const quantity = Number(this.quantityInput)
            if (!Number.isSafeInteger(craftVersionId) || craftVersionId <= 0)
                return this.showNotice('请先选择工序单及工艺版本')
            if (!validProductionQuantity(quantity))
                return this.showNotice('产品数量须大于0、不超过99999999.9999且最多4位小数')
            const start = this.startInput, end = this.endInput
            if (!start || !end || !Number.isFinite(start.getTime()) || !Number.isFinite(end.getTime()) || end <= start)
                return this.showNotice('请选择有效的计划时间，交期必须晚于开始时间')
            const nextStart = localDateTime(start), nextEnd = localDateTime(end)
            const token = ++this.requestToken
            this.isLoading = true
            this.queryError = ''
            try {
                const response = await this.$http.post(quantityApi, { craftVersionId, quantity })
                if (token !== this.requestToken) return
                const data = response.data as QuantityResultDto
                if (!data || data.craftVersionId !== craftVersionId || data.quantity !== quantity || !Array.isArray(data.nodes))
                    throw new Error('物料数量接口返回数据无效')
                const nodes = materialTree(data.nodes)
                const changed = this.quantityResult && (this.quantity !== quantity || this.planStart !== nextStart || this.planEnd !== nextEnd)
                if (changed) this.revision++
                this.quantity = data.quantity
                this.planStart = nextStart
                this.planEnd = nextEnd
                this.quantityResult = new QuantityResultDto(data)
                this.nodes = nodes
                this.product = new ScheduleProductDto({
                    code: data.product.code, name: data.product.name, specification: data.product.specification,
                    unit: data.quantityUnit, orderCode: data.craftOrderCode, version: data.craftVersion,
                    customerCode: '', technicalCode: '', owner: ''
                })
                this.expandedIds = this.allNodes.filter(node => node.children.length).map(node => node.id)
                this.selectedId = this.allNodes.some(node => node.id === this.selectedId) ? this.selectedId : ''
                this.selectedOrderId = ''
                this.scheduleTarget = null
                this.drafts = []
                this.materialTarget = null
            } catch (error) {
                if (token === this.requestToken) {
                    this.queryError = this.quantityResult ? '查询失败，请重试；数量为上次成功查询结果' : '查询失败，请重试'
                    this.$kingzon_messager.showError(error || '物料数量查询失败')
                }
            } finally {
                if (token === this.requestToken) this.isLoading = false
            }
        },
        plannedReceipts(node: ScheduleNodeDto) {
            return (node.receipts ?? []).filter((receipt) => receipt.status === ScheduleReceiptStatus.待入库)
        },
        shortage(node: ScheduleNodeDto): number {
            return this.requirements[node.id]?.net ?? 0
        },
        cellValue(node: ScheduleNodeDto, key: string): string | number {
            return materialCellValue(node, key, this.requirements, this.context)
        },
        expandAll() {
            this.expandedIds = this.allNodes.filter((node) => node.children.length).map((node) => node.id)
        },
        onExpand(row: ScheduleTreeRowDto) {
            if (!this.isFiltering && !this.expandedIds.includes(row.id)) this.expandedIds.push(row.id)
        },
        onCollapse(row: ScheduleTreeRowDto) {
            if (this.isFiltering) row.state = 'open'
            else this.expandedIds = this.expandedIds.filter((id) => id !== row.id)
        },
        selectRow(row: ScheduleTreeRowDto | null) {
            this.selectedId = row?.id ?? ''
            this.scheduleError = ''
            this.selectOrder(this.currentDraft?.id ?? this.selectedSemi?.orders[0]?.id ?? '')
        },
        selectOrder(id: string) {
            this.selectedOrderId = id
            this.scheduleTarget = id === this.currentDraft?.id ? (this.selectedSemi ?? null) : null
            this.scheduleError = ''
        },
        selectListRow(row: ScheduleListRowDto | null) {
            this.selectOrder(row?.id ?? '')
        },
        cancelDraftRow(id: string) {
            this.selectOrder(id)
            this.closeScheduleDraft()
        },
        draftNode(id: string): ScheduleNodeDto {
            return this.allNodes.find((node) => node.id === id)!
        },
        resetFilters() {
            this.keyword = ''
            this.statusFilter = ''
        },
        showNotice(message: string) {
            this.$kingzon_messager.showInfo(message)
        },
        async recalculate() {
            await this.loadMaterialQuantities()
        },
        async refreshStock() {
            await this.loadMaterialQuantities()
        },
        openSchedule(node: ScheduleNodeDto) {
            if (node.kind !== 'semi' || !this.shortage(node)) return
            this.selectedId = node.id
            let draft = this.currentDraft
            if (!draft) {
                draft = new ScheduleDraftDto({ id: `draft-${this.draftSequence++}`, nodeId: node.id })
                this.drafts.push(draft)
            }
            this.selectOrder(draft.id)
        },
        saveSingleSchedule(value: ScheduleSubmissionDto) {
            if (this.quantityResult) return this.showNotice('排产单保存接口尚未接入')
            if (!this.scheduleTarget || this.scheduleTarget.id !== value.nodeId) return
            const errors = validateSchedule(value, this.nodes, this.context)
            if (errors.length) {
                this.scheduleError = errors.join('；')
                return
            }
            const node = this.scheduleTarget,
                requirement = this.requirements[node.id]
            const materials = materialPlans(
                node,
                value.quantity,
                value.start,
                this.requirements,
                this.nodes
            )
            const details = JSON.parse(JSON.stringify(value.details)) as ScheduleSubmissionDto['details']
            details.materials = materials.map((m) => ({
                id: m.node.id,
                required: m.required,
                consumed: 0,
                issued: false,
                reserved: round(Math.min(m.required, m.available)),
                shortage: m.shortage
            }))
            node.orders.push({
                id: `APS-${String(this.orderSequence++).padStart(3, '0')}`,
                quantity: value.quantity,
                start: value.start,
                end: value.end,
                status: details.immediateStart ? ProductionScheduleStatus.在制 : ProductionScheduleStatus.已排产,
                requirementId: requirement.id,
                snapshot: snapshot(requirement),
                details
            })
            this.closeScheduleDraft()
            this.selectedOrderId = node.orders[node.orders.length - 1].id
            this.showNotice(
                `已保存 ${node.name} 的排产单及工序、物料快照${materials.some((m) => m.shortage) ? '，当前缺料已标记为风险' : ''}`
            )
        },
        closeScheduleDraft() {
            const nodeId = this.scheduleTarget?.id
            this.drafts = this.drafts.filter((draft) => draft.nodeId !== nodeId)
            this.scheduleTarget = null
            this.scheduleError = ''
            this.selectedOrderId = this.selectedSemi?.orders[0]?.id ?? ''
        },
        rescheduleOrder(change: ScheduleTimeChangeDto) {
            const result = scheduleChange(this.nodes, this.context, change)
            if (result.errors.length || !result.order || !result.value) {
                this.showNotice(result.errors.join('；'))
                return
            }
            result.order.start = result.value.start
            result.order.end = result.value.end
            if (result.order.details) result.order.details.processes = result.value.details.processes
            this.showNotice('已更新本地排产时间，并重新校验交期、齐套和资源')
        },
        cancelOrder(id: string) {
            const order = this.allNodes.flatMap((node) => node.orders).find((item) => item.id === id)
            if (!order || order.status === ProductionScheduleStatus.已撤销) return
            if (order.status === ProductionScheduleStatus.在制)
                return this.showNotice('已开始排产不能直接撤销，请先处理执行记录')
            order.status = ProductionScheduleStatus.已撤销
            this.showNotice('已撤销本地排产，库存预留已释放，需求已重新计算')
        },
        openMaterial(node: ScheduleNodeDto) {
            this.materialTarget = node
            this.receiptError = ''
            this.receiptForm = new ReceiptFormDto({
                quantity: this.shortage(node),
                arrival: new Date(this.requirements[node.id].requiredDate),
                supplier: ''
            })
            this.openDialog('materialDialog')
        },
        saveReceipt() {
            if (this.quantityResult) return this.showNotice('采购接口尚未接入')
            const node = this.materialTarget
            if (!node) return
            const quantity = Number(this.receiptForm.quantity),
                arrival = this.receiptForm.arrival
            if (!validQuantity(quantity) || quantity > this.shortage(node)) {
                this.receiptError = `采购数量须大于 0 且不超过净需求 ${this.shortage(node)}，最多两位小数`
                return
            }
            if (
                !arrival ||
                !Number.isFinite(arrival.getTime()) ||
                timestamp(localDateTime(arrival)) > timestamp(this.requirements[node.id].requiredDate)
            ) {
                this.receiptError = '预计到料时间必须有效且不晚于需求日期'
                return
            }
            if (!this.receiptForm.supplier.trim()) {
                this.receiptError = '请填写供应商'
                return
            }
            node.receipts = [
                ...(node.receipts ?? []),
                {
                    id: `PUR-${String(this.receiptSequence++).padStart(3, '0')}`,
                    quantity,
                    arrival: localDateTime(arrival),
                    supplier: this.receiptForm.supplier.trim(),
                    status: ScheduleReceiptStatus.待入库
                }
            ]
            this.receiptError = ''
            this.receiptForm.quantity = this.shortage(node)
            this.showNotice('已登记本地预计到料，实际库存未增加')
        },
        openDialog(ref: string) {
            ; (this.$refs[ref] as EasyUI.Dialog).open()
        },
        closeDialog(ref: string) {
            ; (this.$refs[ref] as EasyUI.Dialog).close()
        },
        openGantt(id = '') {
            ; (this.$refs.scheduleGantt as InstanceType<typeof ScheduleGantt> | undefined)?.open(id)
        },
        formatDate(value: string): string {
            return this.$utils.formatDateTime(value, 'yyyy-MM-dd HH:mm')
        },

    }
})
</script>
