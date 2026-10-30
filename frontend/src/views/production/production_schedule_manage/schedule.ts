import { ProductionScheduleStatus, ScheduleReceiptStatus, MaterialRequirementStatus } from '@/types/enums'
import {
    ProductionScheduleTemplateDto, ProductionSchedulePostDto, ProductionScheduleProcessDto,
    ProductionScheduleMaterialDto, ProductionScheduleMachineRlDto, ScheduleProcessEditDto,
    ScheduleMaterialEditDto, ScheduleMachineEditDto, ScheduleMaterialAvailabilityDto, type ScheduleUnitOptionDto,
    MaterialNodeDto,
    type MaterialTotalDto,
    QuantityResultDto,
    ScheduleProductDto,
    RequirementSnapshotDto,
    ProcessSettingDto,
    ScheduleSubmissionDto,
    ScheduleOrderDto,
    ScheduleNodeDto,
    PlanContextDto,
    MaterialRequirementDto,
    MaterialPlanDto,
    ResourceConflictDto,
    GanttEntryDto,
    TimelineTaskDto,
    ScheduleTimeChangeDto,
    TimelineDataDto
} from '@/types/production-schedule'

export function localDateTime(value: Date): string {
    const pad = (part: number) => String(part).padStart(2, '0')
    return `${value.getFullYear()}-${pad(value.getMonth() + 1)}-${pad(value.getDate())}T${pad(value.getHours())}:${pad(value.getMinutes())}:${pad(value.getSeconds())}`
}

export function flattenTree(nodes: ScheduleNodeDto[]): ScheduleNodeDto[] {
    return nodes.flatMap(node => [node, ...flattenTree(node.children)])
}

export const availableStock = (node: ScheduleNodeDto): number => Math.max(0, node.stock - node.occupied - (node.safetyStock ?? 0))

export const quantityApi = '/api/production/ProductionScheduleManage/GetMaterialQuantityTree'

export function validProductionQuantity(value: number): boolean {
    return Number.isFinite(value) && value > 0 && value <= 99999999.9999
        && Number(value.toFixed(4)) === value
}

/** Preserve backend path IDs, material/process sources and nullable supply quantities. */
export function materialTree(nodes: MaterialNodeDto[]): ScheduleNodeDto[] {
    return nodes.map(node => new ScheduleNodeDto({
        id: node.id, name: node.name, code: node.code, specification: node.specification,
        unit: node.unit, kind: node.kind, baseDemand: node.gross,
        stock: node.stock, occupied: node.occupied, safetyStock: node.safetyStock,
        orders: [], receipts: [], children: materialTree(node.children), serverMaterial: node
    }))
}

/** The service already calculated shared inventory and recursive demand; never recalculate it here. */
export function materialRequirements(nodes: ScheduleNodeDto[], context: PlanContextDto,
    result: QuantityResultDto): Record<string, MaterialRequirementDto> {
    const all = flattenTree(nodes)
    const names = new Map(all.map(node => [node.id, node.name]))
    return Object.fromEntries(all.map(node => {
        const value = node.serverMaterial!
        const requirement: MaterialRequirementDto = {
            id: `${result.craftOrderId}:${node.id}`, node, revision: context.revision,
            productQuantity: result.quantity, requiredDate: context.end,
            gross: value.gross, available: value.available, net: value.net,
            parentId: value.parentId,
            source: `${result.craftOrderCode} / ${names.get(value.parentId) ?? result.product.name}`,
            physicalAvailable: Math.max(value.stock - value.occupied - value.safetyStock, 0),
            production: node.kind === 'semi' ? value.net : 0, status: value.status,
            incoming: 0, reserved: 0, scheduled: 0, late: 0, receiptSupplies: []
        }
        return [node.id, requirement]
    }))
}

export const round = (value: number): number => Math.round((value + Number.EPSILON) * 100) / 100
export const activeOrders = (node: ScheduleNodeDto): ScheduleOrderDto[] => node.orders.filter(order => order.status !== ProductionScheduleStatus.已撤销)
export const goodsKey = (node: ScheduleNodeDto): string => `${node.code}|${node.specification}|${node.unit}`

/** Display totals separately; deadline-aware net demand still uses allocated supplies. */
export function quantitySummary(node: ScheduleNodeDto, requirement: MaterialRequirementDto) {
    if (node.serverMaterial) {
        const value = node.serverMaterial
        return { gross: value.gross, stock: value.stock, occupied: value.occupied,
            transit: value.transit, inProduction: value.inProduction,
            scheduledTotal: value.scheduledTotal, net: value.net }
    }
    const orders = activeOrders(node)
    return {
        gross: requirement.gross,
        stock: node.stock,
        occupied: round(node.occupied + requirement.reserved),
        transit: round((node.receipts ?? []).filter(receipt => receipt.status === ScheduleReceiptStatus.待入库).reduce((sum, receipt) => sum + receipt.quantity, 0)),
        inProduction: round(orders.filter(order => order.status === ProductionScheduleStatus.在制).reduce((sum, order) => sum + order.quantity, 0)),
        scheduledTotal: round(orders.filter(order => order.status !== ProductionScheduleStatus.在制).reduce((sum, order) => sum + order.quantity, 0)),
        net: requirement.net
    }
}

/** All stored timestamps are local wall times; never append Z to DateTimeBox values. */
export function timestamp(value: string): number {
    if (!/^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}(?::\d{2})?$/.test(value)) return NaN
    const date = new Date(value)
    return localDateTime(date).slice(0, value.length) === value ? date.getTime() : NaN
}
export function planTimestamp(value: string, end = false): string {
    return value.length === 10 ? `${value}T${end ? '18:00:00' : '08:00:00'}` : value
}
export function validQuantity(value: number): boolean {
    return Number.isFinite(value) && value > 0 && Math.abs(value * 100 - Math.round(value * 100)) < 0.000001
}

export function reservedQuantity(nodes: ScheduleNodeDto[], nodeId: string): number {
    return round(flattenTree(nodes).flatMap(node => activeOrders(node))
        .reduce((sum, order) => sum + (order.details?.materials ?? [])
            .filter(material => material.id === nodeId).reduce((total, material) => total + material.reserved, 0), 0))
}

/** Inventory and purchase receipts are allocated once per goods key, in tree order.
 * Child demand follows actual parent production, including existing commitments.
 * Existing schedules belong to their requirement; a late schedule cannot offset on-time net demand.
 */
export function calculateRequirements(nodes: ScheduleNodeDto[], context: PlanContextDto): Record<string, MaterialRequirementDto> {
    const product = context.product ?? new ScheduleProductDto()
    const result: Record<string, MaterialRequirementDto> = {}
    const all = flattenTree(nodes)
    const stock = new Map<string, number>()
    const receipts = new Map<string, { quantity: number; arrival: string }[]>()
    const receiptIds = new Set<string>()
    for (const node of all) {
        const key = goodsKey(node)
        if (!stock.has(key)) stock.set(key, availableStock(node))
        for (const receipt of node.receipts ?? []) {
            if (receipt.status !== ScheduleReceiptStatus.待入库 || receiptIds.has(receipt.id)) continue
            receiptIds.add(receipt.id)
            const entries = receipts.get(key) ?? []
            entries.push({ quantity: receipt.quantity, arrival: receipt.arrival })
            receipts.set(key, entries)
        }
    }
    // Reserve committed inventory before allocating the remaining pool.
    for (const node of all) {
        const key = goodsKey(node)
        stock.set(key, Math.max(0, (stock.get(key) ?? 0) - reservedQuantity(nodes, node.id)))
    }
    const visit = (node: ScheduleNodeDto, parent?: MaterialRequirementDto) => {
        const requiredDate = parent
            ? activeOrders(parent.node).reduce((earliest, order) => timestamp(order.start) < timestamp(earliest) ? order.start : earliest,
                parent.requiredDate)
            : planTimestamp(context.end, true)
        const gross = round(parent ? parent.production * node.baseDemand / parent.node.baseDemand : node.baseDemand * context.quantity / 100)
        const key = goodsKey(node)
        const reserved = reservedQuantity(nodes, node.id)
        const physicalAvailable = (stock.get(key) ?? 0) + reserved
        const allocation = Math.min(Math.max(0, gross - reserved), stock.get(key) ?? 0)
        stock.set(key, round((stock.get(key) ?? 0) - allocation))
        const available = round(Math.min(gross, reserved + allocation))
        const orders = activeOrders(node)
        const scheduled = round(orders.filter(order => timestamp(order.end) <= timestamp(requiredDate)).reduce((sum, order) => sum + order.quantity, 0))
        const late = round(orders.filter(order => timestamp(order.end) > timestamp(requiredDate)).reduce((sum, order) => sum + order.quantity, 0))
        let unmet = Math.max(0, gross - available - scheduled)
        let purchased = 0
        const receiptSupplies: { quantity: number; arrival: string }[] = []
        const supplies = (receipts.get(key) ?? []).sort((a, b) => timestamp(a.arrival) - timestamp(b.arrival))
        for (const supply of supplies) {
            const assigned = Math.min(unmet, supply.quantity)
            if (timestamp(supply.arrival) <= timestamp(requiredDate)) purchased = round(purchased + assigned)
            if (assigned > 0) receiptSupplies.push({ quantity: assigned, arrival: supply.arrival })
            supply.quantity = round(supply.quantity - assigned)
            unmet = round(unmet - assigned)
        }
        const incoming = round(purchased + scheduled)
        const net = round(Math.max(0, gross - available - incoming))
        // Purchased / stocked parent units need no child production. Preserve overplanned commitments.
        const production = node.kind === 'semi' ? round(Math.max(gross - available - purchased, scheduled + late, 0)) : 0
        let status = MaterialRequirementStatus.库存充足
        if (gross === 0) status = MaterialRequirementStatus.无需需求
        else if (!net) status = incoming > 0 ? MaterialRequirementStatus.已满足 : MaterialRequirementStatus.库存充足
        else if (node.kind === 'semi') status = scheduled > 0 ? MaterialRequirementStatus.部分排产 : late > 0 ? MaterialRequirementStatus.排产延期 : MaterialRequirementStatus.待排产
        else status = purchased > 0 ? MaterialRequirementStatus.采购中 : available > 0 ? MaterialRequirementStatus.部分缺料 : MaterialRequirementStatus.完全缺料
        const requirement: MaterialRequirementDto = {
            id: `${product.orderCode}:${node.id}`, node, revision: context.revision,
            productQuantity: context.quantity, requiredDate, gross, available, incoming, net,
            parentId: parent?.id ?? '', source: parent ? `${product.name} / ${parent.node.name}` : `${product.name} / ${product.orderCode}`,
            physicalAvailable, reserved, scheduled, late, production, status, receiptSupplies
        }
        result[node.id] = requirement
        node.children.forEach(child => visit(child, requirement))
    }
    nodes.forEach(node => visit(node))
    return result
}

/** Material readiness for one new schedule; already committed parent orders consume supply first. */
export function materialPlans(node: ScheduleNodeDto, quantity: number, start: string,
    requirements: Record<string, MaterialRequirementDto>, nodes: ScheduleNodeDto[]): MaterialPlanDto[] {
    return node.children.map(child => {
        const requirement = requirements[child.id]
        const perUnit = child.baseDemand / node.baseDemand
        const required = round(perUnit * quantity)
        const available = Math.max(0, round((requirement?.available ?? availableStock(child)) - reservedQuantity(nodes, child.id)))
        const committed = round(activeOrders(node).reduce((sum, order) => sum + (order.details?.materials ?? [])
            .filter(material => material.id === child.id).reduce((total, material) => total + Math.max(0, material.required - material.reserved), 0), 0))
        const supplies = [
            ...activeOrders(child).map(order => ({ quantity: order.quantity, arrival: order.end })),
            ...(requirement?.receiptSupplies ?? [])
        ].sort((a, b) => timestamp(a.arrival) - timestamp(b.arrival))
        let remainingCommitment = committed
        let remaining = Math.max(0, required - available)
        let incoming = 0
        let readyAt = remaining > 0 ? '' : start
        for (const supply of supplies) {
            const consumed = Math.min(remainingCommitment, supply.quantity)
            remainingCommitment = round(remainingCommitment - consumed)
            const usable = Math.max(0, supply.quantity - consumed)
            if (timestamp(supply.arrival) <= timestamp(start)) incoming = round(incoming + usable)
            if (remaining > 0 && usable > 0) {
                remaining = round(Math.max(0, remaining - usable))
                if (!remaining) readyAt = supply.arrival
            }
        }
        return { node: child, perUnit, required, available, incoming: Math.min(incoming, Math.max(0, required - available)),
            shortage: round(Math.max(0, required - available - incoming)), readyAt }
    })
}

export function createProcesses(quantity: number, start: string): ProcessSettingDto[] {
    let cursor = timestamp(start)
    return [['010', '下料', 'QBXL-18', 0.04], ['020', '冷挤压', 'QBJY-28', 0.06],
        ['030', '机加', 'QBJJ-04', 0.08], ['040', '热处理', 'QBRCL-09', 0.03]].map((entry, index) => {
        const [code, name, craftCode, rate] = entry
        const begin = cursor
        cursor += (1 + quantity * Number(rate)) * 3600000
        return { code: String(code), name: String(name), craftCode: String(craftCode), inbound: index === 3, outsource: false,
            start: localDateTime(new Date(begin)), end: localDateTime(new Date(cursor)), predecessor: index ? String(index * 10).padStart(3, '0') : '',
            supplier: '', warehouse: index === 3 ? '半成品仓' : '', setupHours: 1, hoursPerUnit: Number(rate),
            dispatches: [{ id: index + 1, operator: '', machine: '', quantity }] }
    })
}

export function resourceConflicts(processes: ProcessSettingDto[], nodes: ScheduleNodeDto[]): ResourceConflictDto[] {
    const booked = flattenTree(nodes).flatMap(node => activeOrders(node).flatMap(order => (order.details?.processes ?? [])
        .filter(process => !process.outsource).flatMap(process => process.dispatches.map(dispatch => ({
            label: `${order.id} / ${process.name}`, process, dispatch
        })))))
    const drafts = processes.filter(process => !process.outsource).flatMap(process => process.dispatches.map(dispatch => ({
        label: `本单 / ${process.name}`, process, dispatch
    })))
    const conflicts: ResourceConflictDto[] = []
    drafts.forEach((draft, index) => {
        for (const other of [...booked, ...drafts.slice(0, index)]) {
            if (timestamp(draft.process.start) >= timestamp(other.process.end) || timestamp(other.process.start) >= timestamp(draft.process.end)) continue
            for (const key of ['machine', 'operator'] as const) {
                const resource = draft.dispatch[key]
                if (resource && resource === other.dispatch[key]) conflicts.push({ resource, first: draft.label, second: other.label })
            }
        }
    })
    return conflicts
}

/** Revalidate at the owner boundary, so stale drawers / duplicated save events cannot overplan. */
export function validateSchedule(value: ScheduleSubmissionDto, nodes: ScheduleNodeDto[], context: PlanContextDto): string[] {
    const requirements = calculateRequirements(nodes, context)
    const node = flattenTree(nodes).find(item => item.id === value.nodeId)
    if (!node || node.kind !== 'semi') return ['排产需求不存在']
    const requirement = requirements[node.id]
    const errors: string[] = []
    if (!validQuantity(value.quantity) || value.quantity > requirement.net) errors.push(`排产数量须大于 0 且不超过当前净需求 ${requirement.net}，最多两位小数`)
    if (value.quantity > Math.max(0, requirement.net - requirement.late)) errors.push('已有逾期排产覆盖本次缺口，请先在甘特图撤销或复核旧单，避免重复生产')
    const start = timestamp(value.start), end = timestamp(value.end)
    if (!Number.isFinite(start) || !Number.isFinite(end) || end <= start) errors.push('开始、完成时间无效或顺序错误')
    if (end > timestamp(requirement.requiredDate)) errors.push('排产完成不能晚于需求日期，请先调整交期或上级排产')
    const processes = value.details.processes
    if (!processes.length) errors.push('请至少设置一道工序')
    const codes = new Set<string>()
    for (let index = 0; index < processes.length; index++) {
        const process = processes[index]
        const ps = timestamp(process.start), pe = timestamp(process.end)
        if (!Number.isFinite(ps) || !Number.isFinite(pe) || pe <= ps || ps < start || pe > end) errors.push(`${process.name}的工序时间须在排产时间内且结束晚于开始`)
        if (!process.code || codes.has(process.code)) errors.push('工序编码不能重复或为空')
        codes.add(process.code)
        const previous = processes[index - 1]
        if (previous ? process.predecessor !== previous.code || ps < timestamp(previous.end) : !!process.predecessor) errors.push(`${process.name}的前序关系或开始时间错误`)
        if (process.inbound && !process.warehouse.trim()) errors.push(`${process.name}请选择目标仓库`)
        if (process.outsource) {
            if (!process.supplier.trim()) errors.push(`${process.name}请填写外协供应商`)
        } else {
            if (!process.dispatches.length || process.dispatches.some(row => !row.operator || !row.machine || !validQuantity(Number(row.quantity)))) errors.push(`${process.name}请完整填写人员、设备和派工数量`)
            if (Math.abs(process.dispatches.reduce((sum, row) => sum + Number(row.quantity), 0) - value.quantity) > 0.000001) errors.push(`${process.name}派工数量合计须等于排产数量`)
            const setup = Number(process.setupHours), rate = Number(process.hoursPerUnit)
            const load = Math.max(0, ...process.dispatches.map(row => Number(row.quantity)))
            if (!Number.isFinite(setup) || setup < 0 || !Number.isFinite(rate) || rate <= 0) errors.push(`${process.name}请填写有效的调机工时和单位工时`)
            else if (pe - ps + 1000 < (setup + load * rate) * 3600000) errors.push(`${process.name}计划时长不足以完成调机和派工数量`)
        }
    }
    const materials = materialPlans(node, value.quantity, value.start, requirements, nodes)
    for (const material of materials) {
        if (material.readyAt && timestamp(material.readyAt) > start) errors.push(`${material.node.name}最早齐套时间为 ${material.readyAt.replace('T', ' ')}`)
        if (value.details.immediateStart && material.required > material.available) errors.push(`立即开始要求 ${material.node.name} 当前库存齐套`)
    }
    if (value.details.immediateStart && (start > Date.now() || end <= Date.now())) errors.push('立即开始要求当前时间处于排产时间范围内')
    if (resourceConflicts(processes, nodes).length) errors.push('设备或人员存在时间冲突，请调整工序时间或资源')
    return [...new Set(errors)]
}

export function snapshot(requirement: MaterialRequirementDto): RequirementSnapshotDto {
    const { revision, productQuantity, requiredDate, gross, available, incoming, net, parentId, source } = requirement
    return { revision, productQuantity, requiredDate, gross, available, incoming, net, parentId, source }
}

/** Re-evaluate material risk after new receipts / child schedules instead of retaining a stale shortage badge. */
export function orderMaterialRisks(nodes: ScheduleNodeDto[], context: PlanContextDto): Record<string, string[]> {
    const requirements = calculateRequirements(nodes, context)
    const results: Record<string, string[]> = {}
    for (const node of flattenTree(nodes)) {
        const orders = activeOrders(node).sort((a, b) => timestamp(a.start) - timestamp(b.start))
        for (const child of node.children) {
            const r = requirements[child.id]
            let free = Math.max(0, round(r.available - r.reserved))
            const supplies = [...r.receiptSupplies.map(supply => ({ ...supply })),
                ...activeOrders(child).map(order => ({ quantity: order.quantity, arrival: order.end }))].sort((a, b) => timestamp(a.arrival) - timestamp(b.arrival))
            for (const order of orders) {
                const material = order.details?.materials.find(item => item.id === child.id)
                const required = material?.required ?? round(order.quantity * child.baseDemand / node.baseDemand)
                let unmet = Math.max(0, round(required - (material?.reserved ?? 0)))
                const allocated = Math.min(unmet, free)
                free = round(free - allocated); unmet = round(unmet - allocated)
                for (const supply of supplies) {
                    if (timestamp(supply.arrival) > timestamp(order.start)) continue
                    const amount = Math.min(unmet, supply.quantity)
                    unmet = round(unmet - amount); supply.quantity = round(supply.quantity - amount)
                }
                if (unmet > 0) (results[order.id] ??= []).push(`${child.name} 缺 ${unmet} ${child.unit}`)
            }
        }
    }
    return results
}

export function ganttEntries(nodes: ScheduleNodeDto[], context: PlanContextDto, targetId = '', operationView = true): GanttEntryDto[] {
    const requirements = calculateRequirements(nodes, context)
    const materialRisks = orderMaterialRisks(nodes, context)
    return flattenTree(nodes).filter(node => node.kind === 'semi' && (!targetId || node.id === targetId)).flatMap(node =>
        activeOrders(node).flatMap(order => {
            const messages = [...(materialRisks[order.id] ?? [])]
            if (order.snapshot?.revision !== context.revision) messages.push('需求版本已变更，待复核')
            if (timestamp(order.end) > timestamp(requirements[node.id].requiredDate)) messages.push('完成晚于需求日期')
            if (requirements[node.id].scheduled + requirements[node.id].late > Math.max(0, requirements[node.id].gross - requirements[node.id].available)) messages.push('排产超过当前生产需求')
            if (!order.details?.processes.length) messages.push('旧排产未设置工序')
            const processes = operationView && order.details?.processes.length ? order.details.processes : [undefined]
            const otherNodes = nodesWithoutOrder(nodes, order.id)
            const conflicts = resourceConflicts(order.details?.processes ?? [], otherNodes)
            return processes.map(process => ({ key: `${order.id}:${process?.code ?? 'summary'}`, node, order, process,
                start: process?.start ?? order.start, end: process?.end ?? order.end, risk: messages.join('；'),
                conflict: process ? conflicts.some(conflict => conflict.first === `本单 / ${process.name}` || conflict.second === `本单 / ${process.name}`) : conflicts.length > 0 }))
        }))
}

function nodesWithoutOrder(nodes: ScheduleNodeDto[], id: string): ScheduleNodeDto[] {
    return nodes.map(node => ({ ...node, orders: node.orders.filter(order => order.id !== id), children: nodesWithoutOrder(node.children, id) }))
}

/** Retain every ancestor of scheduled semi-finished goods, at any BOM depth. */
export function timelineData(nodes: ScheduleNodeDto[], context: PlanContextDto, targetId = '', operationView = true): TimelineDataDto {
    const product = context.product ?? new ScheduleProductDto({ name: '生产计划' })
    const entries = ganttEntries(nodes, context)
    const result: TimelineDataDto = { tasks: [], links: [] }
    const included = new Set<string>()
    const target = flattenTree(nodes).find(node => node.id === targetId)
    const scoped = new Set((targetId ? (target ? flattenTree([target]) : []) : flattenTree(nodes)).map(node => node.id))
    const mark = (node: ScheduleNodeDto, inside = !targetId): boolean => {
        const selected = inside || node.id === targetId
        const children = node.children.map(child => mark(child, selected)).some(Boolean)
        const present = children || (selected && node.kind === 'semi' && activeOrders(node).length > 0)
        if (present) included.add(node.id)
        return present
    }
    nodes.forEach(node => mark(node))
    const visit = (node: ScheduleNodeDto, parent: string) => {
        if (!included.has(node.id)) return
        const groupId = `semi:${node.id}`
        result.tasks.push({ id: groupId, parent, text: `${node.name} / ${node.code}`, type: 'summary', open: true,
            start: new Date(context.start), end: new Date(context.end), nodeId: node.id, statusText: '半成品' })
        for (const order of activeOrders(node)) {
            // Ancestors kept for context must not leak unrelated sibling orders into a focused view.
            if (!scoped.has(node.id)) continue
            const orderId = `order:${order.id}`
            const risk = entries.find(entry => entry.order.id === order.id)
            result.tasks.push({ id: orderId, parent: groupId, text: order.id, type: 'task', open: true,
                start: new Date(order.start), end: new Date(order.end), nodeId: node.id, orderId: order.id, quantity: order.quantity, locked: order.status === ProductionScheduleStatus.在制,
                status: order.status ?? ProductionScheduleStatus.已排产, statusText: risk?.conflict ? '资源冲突' : risk?.risk || scheduleStatusText(order.status), details: risk?.risk })
            if (operationView) for (const process of order.details?.processes ?? []) {
                const id = `process:${order.id}:${process.code}`
                const entry = entries.find(item => item.order.id === order.id && item.process?.code === process.code)
                result.tasks.push({ id, parent: orderId, text: `${process.code} ${process.name}`, type: 'task', open: true,
                    start: new Date(process.start), end: new Date(process.end), nodeId: node.id, orderId: order.id, processCode: process.code,
                    quantity: order.quantity, locked: order.status === ProductionScheduleStatus.在制, status: order.status ?? ProductionScheduleStatus.已排产, statusText: entry?.conflict ? '资源冲突' : entry?.risk || scheduleStatusText(order.status),
                    details: process.outsource ? `外协：${process.supplier}` : process.dispatches.map(row => `${row.machine} / ${row.operator}`).join('、') })
                if (process.predecessor) result.links.push({ id: `link:${id}`, source: `process:${order.id}:${process.predecessor}`, target: id, type: 'e2s' })
            }
        }
        node.children.forEach(child => visit(child, groupId))
    }
    nodes.forEach(node => visit(node, 'product'))
    if (result.tasks.length) {
        // Give summary rows explicit child bounds; no dependency on PRO auto-summary features.
        for (const task of [...result.tasks].reverse()) {
            if (task.type !== 'summary') continue
            const children = result.tasks.filter(child => child.parent === task.id)
            if (children.length) { task.start = new Date(Math.min(...children.map(child => child.start.getTime()))); task.end = new Date(Math.max(...children.map(child => child.end.getTime()))) }
        }
        const roots = result.tasks.filter(task => task.parent === 'product')
        result.tasks.unshift({ id: 'product', parent: 0, text: `${product.name} / ${product.orderCode}`, type: 'summary', open: true,
            start: new Date(Math.min(...roots.map(task => task.start.getTime()))), end: new Date(Math.max(...roots.map(task => task.end.getTime()))), statusText: '产品' })
    }
    const branches = new Set(result.tasks.map(task => task.parent))
    for (const task of result.tasks) task.open = branches.has(task.id)
    return result
}

/** Two whole calendar months by default; retain longer requested intervals. */
export function timelineRange(context: PlanContextDto, tasks: TimelineTaskDto[], offset = 0): { start: Date; end: Date } {
    const earliest = Math.min(timestamp(context.start), ...tasks.map(task => task.start.getTime()))
    const start = new Date(earliest); start.setDate(1); start.setHours(0, 0, 0, 0); start.setMonth(start.getMonth() + offset)
    const end = new Date(start); end.setMonth(end.getMonth() + 2)
    if (!offset) {
        const latest = Math.max(timestamp(context.end), ...tasks.map(task => task.end.getTime()))
        while (end.getTime() <= latest) end.setMonth(end.getMonth() + 1)
    }
    return { start, end }
}

/** Prepare and validate detached changes before the owner updates its stored order. */
export function scheduleChange(nodes: ScheduleNodeDto[], context: PlanContextDto, change: ScheduleTimeChangeDto): {
    errors: string[]; order?: ScheduleOrderDto; value?: ScheduleSubmissionDto
} {
    const node = flattenTree(nodes).find(item => item.orders.some(order => order.id === change.orderId))
    const order = node?.orders.find(item => item.id === change.orderId)
    if (!node || !order || order.status === ProductionScheduleStatus.已撤销) return { errors: ['排产单不存在或已撤销'] }
    if (order.status === ProductionScheduleStatus.在制) return { errors: ['在制排产单不能直接拖动改期'] }
    if (![change.start, change.end].every(value => Number.isFinite(timestamp(value)))) return { errors: ['请选择有效的排产时间'] }
    const details = order.details ? JSON.parse(JSON.stringify(order.details)) as ScheduleSubmissionDto['details']
        : { immediateStart: false, urgent: false, circulationCard: false, remark: '', processes: [], materials: [] }
    let start = change.start, end = change.end
    if (change.processCode) {
        const process = details.processes.find(item => item.code === change.processCode)
        if (!process) return { errors: ['工序不存在'] }
        process.start = start; process.end = end
        start = details.processes[0].start; end = details.processes[details.processes.length - 1].end
    } else if (details.processes.length) {
        const delta = timestamp(start) - timestamp(order.start)
        for (const process of details.processes) {
            process.start = localDateTime(new Date(timestamp(process.start) + delta))
            process.end = localDateTime(new Date(timestamp(process.end) + delta))
        }
        details.processes[details.processes.length - 1].end = end
    }
    const value: ScheduleSubmissionDto = { nodeId: node.id, quantity: order.quantity, start, end, details }
    const withoutOrder = (items: ScheduleNodeDto[]): ScheduleNodeDto[] => items.map(item => ({ ...item,
        orders: item.orders.filter(existing => existing.id !== order.id), children: withoutOrder(item.children) }))
    const remaining = withoutOrder(nodes)
    const errors = validateSchedule(value, remaining, context).filter(error => order.details || error !== '请至少设置一道工序')
    // Moving a parent earlier must respect active descendant completion dates, including late supplies.
    const required = calculateRequirements(remaining, context)[node.id]
    if (order.quantity > Math.max(0, required.net - required.late) && !errors.length) errors.push('当前需求不足以保留此排产数量')
    return { errors, order, value }
}

// 主界面直接显示服务端数量，独立规划数据沿用原有计算。
function formatQuantityDate(value: string): string {
    return value.slice(0, 16).replace('T', ' ')
}

export function materialStatusClass(node: ScheduleNodeDto, requirements: Record<string, MaterialRequirementDto>): string {
    const r = requirements[node.id]
    return r.late || (node.kind === 'material' && r.net)
        ? 'kingzon-tag-danger'
        : r.net
            ? 'kingzon-tag-warning'
            : ''
}

export function materialStockTip(node: ScheduleNodeDto, requirements: Record<string, MaterialRequirementDto>): string {
    const r = requirements[node.id]
    return `实际库存 ${node.stock} / 外部占用 ${node.occupied} / 安全库存 ${node.safetyStock ?? 0} / 本计划预留 ${r.reserved} / 本需求分配 ${r.available}`
}

export function materialQuantityTip(node: ScheduleNodeDto, key: string, requirements: Record<string, MaterialRequirementDto>): string {
    if (['stock', 'occupied', 'available'].includes(key)) return materialStockTip(node, requirements)
    if (node.serverMaterial) {
        if (['transit', 'inProduction', 'scheduledTotal', 'incoming', 'reserved', 'scheduled', 'late'].includes(key))
            return '当前接口未查询此数量'
        if (key === 'net') return '库存口径缺量；未抵扣在途、在制和排产供应'
    }
    return (
        (
            {
                transit: '未入库的采购 / 到货计划总量；仅按期到货抵扣缺量',
                inProduction: '已开始且未撤销排产单数量',
                scheduledTotal: '尚未开始且未撤销的排产单数量，包含延期排产',
                net: '总需求扣除分配库存及按期供应后的缺量；安全库存、共享库存分配及延期会影响结果'
            } as Record<string, string>
        )[key] ?? ''
    )
}

export function materialCellValue(node: ScheduleNodeDto, key: string, requirements: Record<string, MaterialRequirementDto>, context: PlanContextDto): string | number {
    if (node.serverMaterial) {
        if (['transit', 'inProduction', 'scheduledTotal'].includes(key))
            return (node.serverMaterial as unknown as Record<string, number | null>)[key] ?? '未查询'
        if (['incoming', 'reserved', 'scheduled', 'late'].includes(key)) return '未查询'
        if (key === 'requiredDate') return formatQuantityDate(context.end)
        if (key === 'source') return requirements[node.id].source
        return (node.serverMaterial as unknown as Record<string, string | number>)[key] ?? '—'
    }
    if (key === 'requiredDate') return formatQuantityDate(requirements[node.id].requiredDate)
    return (
        (
            {
                ...node,
                ...requirements[node.id],
                ...quantitySummary(node, requirements[node.id])
            } as unknown as Record<string, string | number>
        )[key] ?? '—'
    )
}

export function scheduleStatusText(value: ProductionScheduleStatus = ProductionScheduleStatus.已排产): string {
    return Object.entries(ProductionScheduleStatus).find(([, status]) => status === value)?.[0] ?? '—'
}


/** 独立编辑器只创建草稿，不能复用或修改工艺模板对象。 */
export function createProductionScheduleDraft(template: ProductionScheduleTemplateDto, quantity: number): ScheduleProcessEditDto[] {
    return template.processs.map((source, index) => {
        const process = new ScheduleProcessEditDto(JSON.parse(JSON.stringify(source)))
        process.code = source.code || String((index + 1) * 10).padStart(3, '0')
        process.key = `template-${index}-${source.nodeSettingTemplateId}`
        process.order = (index + 1) * 10
        process.materials = source.materials.map((material, rowIndex) => new ScheduleMaterialEditDto({
            ...JSON.parse(JSON.stringify(material)), key: `${process.key}-material-${rowIndex}`,
            quantity: Number((material.quantityPerUnit * quantity).toFixed(6))
        }))
        process.machines = source.machines.map(machine => new ScheduleMachineEditDto({ machineId: machine.machineId }))
        return process
    })
}

export function productionSchedulePost(form: ProductionSchedulePostDto, processes: ScheduleProcessEditDto[]): ProductionSchedulePostDto {
    return new ProductionSchedulePostDto({
        departmentId: form.departmentId, craftVersionId: form.craftVersionId, quantity: Number(form.quantity),
        start: form.start, end: form.end, immediateStart: form.immediateStart, urgent: form.urgent,
        circulationCard: form.circulationCard, remark: form.remark,
        processs: processes.map((process, index) => new ProductionScheduleProcessDto({
            nodeSettingTemplateId: process.nodeSettingTemplateId || 0, order: (index + 1) * 10,
            code: process.code.trim(), name: process.name, craftId: process.craftId,
            isOutsource: process.isOutsource, isInbound: process.isInbound, isReportWork: process.isReportWork,
            materials: process.materials.map((material, position) => new ProductionScheduleMaterialDto({
                order: position + 1, dataId: material.dataId, dataType: material.dataType,
                quantity: Number(material.quantity), unitId: material.unitId,
                unitConversionId: material.unitConversionId,
                convUnitInfo: JSON.stringify(material.unitOptions.map(unit => ({
                    id: unit.id, unitId: unit.unitId, unitName: unit.unitName, ratio: unit.ratio, quantity: unit.quantity
                })))
            })),
            // 一行表示该设备下一个人的分配数量，设备数量取人员数量之和，不能重复累加机器总量。
            machineRls: process.machines.flatMap(machine => machine.users.map(user => new ProductionScheduleMachineRlDto({
                machineId: machine.machineId, userId: user.userId, quantity: Number(user.quantity), hours: Number(user.hours)
            })))
        }))
    })
}

export function validateProductionSchedule(value: ProductionSchedulePostDto, template: ProductionScheduleTemplateDto): string[] {
    const errors: string[] = []
    if (!Number.isSafeInteger(value.craftVersionId) || value.craftVersionId <= 0 || value.craftVersionId !== template.craftVersionId)
        errors.push('请选择有效的工艺版本')
    if (!validProductionQuantity(value.quantity)) errors.push('排产数量须大于0、不超过99999999.9999且最多4位小数')
    if (!Number.isFinite(timestamp(value.start)) || !Number.isFinite(timestamp(value.end)) || timestamp(value.end) <= timestamp(value.start))
        errors.push('请选择有效的计划开始、完成时间，完成时间必须晚于开始时间')
    if (value.departmentId != null && !template.departments.some(item => item.value === value.departmentId)) errors.push('所选部门已失效')
    if (!value.processs.length) errors.push('请至少配置一道工序')
    const processCodes = new Set<string>()
    for (const process of value.processs) {
        if (!process.code || process.code.length > 50 || processCodes.has(process.code)) errors.push('工序编码不能为空、不能重复，且最多50个字符')
        processCodes.add(process.code)
        const label = process.name || `第${process.order / 10}道工序`
        if (!process.name.trim()) errors.push('工序名称不能为空')
        if (typeof process.isOutsource !== 'boolean' || typeof process.isInbound !== 'boolean') errors.push(`${label}外协或入库设置无效`)
        if (!template.crafts.some(craft => craft.id === process.craftId)) errors.push(`${label}请选择加工工艺`)
        const materials = new Set<string>()
        for (const material of process.materials) {
            const key = `${material.dataType}:${material.dataId}`
            if (materials.has(key)) errors.push(`${label}存在重复物料`)
            materials.add(key)
            if (!(material.dataId > 0) || !Number.isFinite(material.quantity) || material.quantity < 0
                || material.quantity >= 1e12 || Number(material.quantity.toFixed(6)) !== material.quantity)
                errors.push(`${label}物料数量无效，最多6位小数`)
            const units = JSON.parse(material.convUnitInfo) as ScheduleUnitOptionDto[]
            const selected = units.filter(unit => unit.unitId === material.unitId && unit.id === (material.unitConversionId ?? 0))
            if (selected.length !== 1 || !Number.isFinite(selected[0].ratio) || selected[0].ratio <= 0)
                errors.push(`${label}物料缺少有效单位换算`)
        }
        const pairs = new Set<string>()
        for (const row of process.machineRls) {
            const machine = template.machines.find(item => item.id === row.machineId && item.craftIds.includes(process.craftId))
            if (!machine || !machine.users.some(user => user.id === row.userId)) errors.push(`${label}设备或人员已失效`)
            const pair = `${row.machineId}:${row.userId}`
            if (pairs.has(pair)) errors.push(`${label}同一设备下的人员不能重复`)
            pairs.add(pair)
            if (!validProductionQuantity(row.quantity) || !Number.isFinite(row.hours) || row.hours < 0
                || row.hours > 99999999.9999 || Number(row.hours.toFixed(4)) !== row.hours)
                errors.push(`${label}人员分配数量或工时无效`)
        }
        if (!process.isOutsource && (!process.machineRls.length || Math.abs(process.machineRls.reduce((total, row) => total + row.quantity, 0) - value.quantity) > 0.000001))
            errors.push(`${label}人员分配数量合计须等于排产数量`)
    }
    return [...new Set(errors)]
}


/** 物料按路线顺序共用库存池；新增、改用的物料若未查到库存，保留未查询状态。 */
export function scheduleMaterialAvailability(processes: ScheduleProcessEditDto[], stocks: MaterialTotalDto[]): Record<string, ScheduleMaterialAvailabilityDto> {
    const pool = new Map<string, number>()
    for (const stock of stocks) {
        if (![stock.stock, stock.occupied, stock.safetyStock].every(value => Number.isFinite(value) && value >= 0)) continue
        const key = `${stock.dataType}:${stock.dataId}`
        if (!pool.has(key)) pool.set(key, Math.max(0, stock.stock - stock.occupied - stock.safetyStock))
    }
    const result: Record<string, ScheduleMaterialAvailabilityDto> = {}
    for (const process of processes) for (const material of process.materials) {
        const key = `${material.dataType}:${material.dataId}`
        const units = material.unitOptions.filter(unit => unit.id === (material.unitConversionId ?? 0) && unit.unitId === material.unitId)
        const ratio = units.length === 1 ? units[0].ratio : 0
        const row = new ScheduleMaterialAvailabilityDto()
        if (pool.has(key) && Number.isFinite(ratio) && ratio > 0 && Number.isFinite(material.quantity) && material.quantity >= 0) {
            const required = material.quantity * ratio
            const allocated = Math.min(pool.get(key)!, required)
            pool.set(key, Math.max(0, pool.get(key)! - allocated))
            row.allocated = Number((allocated / ratio).toFixed(6))
            row.shortage = Number(((required - allocated) / ratio).toFixed(6))
        }
        result[`${process.key}/${material.key}`] = row
    }
    return result
}

export function scheduleEditorSummary(processes: ScheduleProcessEditDto[], availability: Record<string, ScheduleMaterialAvailabilityDto>) {
    const machines = new Set<number>(), users = new Set<number>()
    const materials = new Map<string, { unknown: boolean; shortage: boolean }>()
    let allocations = 0, humanHours = 0
    for (const process of processes) {
        for (const machine of process.machines) {
            machines.add(machine.machineId)
            for (const user of machine.users) {
                users.add(user.userId); allocations++
                if (Number.isFinite(user.hours) && user.hours >= 0) humanHours += user.hours
            }
        }
        for (const material of process.materials) {
            const key = `${material.dataType}:${material.dataId}`
            const state = materials.get(key) ?? { unknown: false, shortage: false }
            const row = availability[`${process.key}/${material.key}`]
            state.unknown ||= row?.shortage == null
            state.shortage ||= (row?.shortage ?? 0) > 0
            materials.set(key, state)
        }
    }
    return {
        processCount: processes.length, machineCount: machines.size, userCount: users.size, allocations,
        humanHours: Number(humanHours.toFixed(4)), materialCount: materials.size,
        readyCount: [...materials.values()].filter(row => !row.unknown && !row.shortage).length,
        shortageCount: [...materials.values()].filter(row => !row.unknown && row.shortage).length,
        uncheckedCount: [...materials.values()].filter(row => row.unknown).length
    }
}
