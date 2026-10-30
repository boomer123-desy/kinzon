// NODE_PATH=<host project's node_modules> node --test production-schedule.test.cjs
const assert = require('node:assert/strict')
const fs = require('node:fs')
const path = require('node:path')
const { test } = require('node:test')
const ts = require('typescript')
const { parse, compileScript, compileTemplate } = require('@vue/compiler-sfc')
const { createRenderer, h, reactive, ref, nextTick } = require('vue')
const root = path.resolve(__dirname, '..')
const cache = new Map()
function load(filename) {
    filename = path.resolve(filename)
    if (cache.has(filename)) return cache.get(filename)
    let source = fs.readFileSync(filename, 'utf8')
    if (filename.endsWith('.vue')) {
        const { descriptor, errors } = parse(source, { filename })
        assert.deepEqual(errors, [])
        assert.deepEqual(compileTemplate({ source: descriptor.template.content, filename, id: filename }).errors, [])
        source = compileScript(descriptor, { id: filename }).content
    }
    const output = ts.transpileModule(source, { compilerOptions: { module: ts.ModuleKind.CommonJS, target: ts.ScriptTarget.ES2020 }, reportDiagnostics: true })
    assert.deepEqual(output.diagnostics, [])
    const module = { exports: {} }
    const localRequire = id => {
        if (id.startsWith('@/')) return load(path.resolve(root, '../../..', id.slice(2) + '.ts'))
        if (id.startsWith('.')) return load(path.resolve(path.dirname(filename), id.endsWith('.vue') ? id : id + '.ts'))
        return require(id)
    }
    new Function('require', 'module', 'exports', output.outputText)(localRequire, module, module.exports)
    cache.set(filename, module.exports)
    return module.exports
}
const model = { ...load(path.join(root, 'schedule.ts')), ...load(path.join(root, '__tests__/fixtures.ts')) }
const plan = load(path.join(root, 'schedule.ts'))
const gantt = load(path.join(root, 'schedule.ts'))
const timeline = load(path.join(root, 'schedule.ts'))
const api = load(path.join(root, 'schedule.ts'))
function materialQuantityColumns() {
    const source = fs.readFileSync(path.join(root, 'component/schedule-material-tree.vue'), 'utf8')
    return Array.from(source.matchAll(/<GridColumn\s+field="([^"]+)"\s+title="([^"]+)"/g)).slice(4, 11).map(item => item[2])
}
const context = { quantity: 100, start: '2026-08-28T08:00:00', end: '2026-09-05T18:00:00', revision: 1 }
const node = (id, extra = {}) => ({ id, name: id, code: id, specification: '', unit: 'PCS', kind: 'semi', baseDemand: 100, stock: 0, occupied: 0, orders: [], children: [], ...extra })
function submission(target, quantity, start = context.start) {
    const processes = plan.createProcesses(quantity, start)
    processes.forEach(process => { process.dispatches[0].operator = '张三'; process.dispatches[0].machine = process.code + '-01' })
    return { nodeId: target.id, quantity, start, end: processes.at(-1).end,
        details: { immediateStart: false, urgent: false, circulationCard: false, remark: '', processes, materials: [] } }
}
function order(value, id = 'APS-TEST') { return { id, quantity: value.quantity, start: value.start, end: value.end, status: 'planned', details: value.details } }
const renderer = createRenderer({ createElement: () => ({}), createText: () => ({}), createComment: () => ({}), insert() {}, remove() {}, setText() {}, setElementText() {}, patchProp() {}, parentNode: () => null, nextSibling: () => null })
function mount(t, file, input = {}, http = {}, route) {
    const component = { ...load(path.join(root, file)).default, render: () => null }
    const instance = ref(), props = reactive(input), events = [], updates = [], messages = []
    const app = renderer.createApp({ render: () => h(component, { ...props, ref: instance, onSave: value => events.push(value), 'onUpdate:modelValue': value => updates.push(value) }) })
    app.config.globalProperties.$kingzon_messager = { showInfo: message => messages.push(message), showError: error => messages.push(error) }
    app.config.globalProperties.$utils = { formatDateTime: value => String(value).slice(0, 16).replace('T', ' ') }
    app.config.globalProperties.$http = { get: async () => ({ data: [] }), post: async () => ({ data: [] }), ...http }
    if (route) app.config.globalProperties.$route = route
    app.mount({}); t.after(() => app.unmount())
    return { vm: instance.value, props, events, updates, messages }
}
function mountPlanningFixture(t) {
    const mounted = mount(t, 'index.vue')
    const { vm } = mounted
    vm.nodes = model.createMockTree()
    vm.product = { ...vm.product, ...model.product }
    vm.planStart = context.start; vm.planEnd = context.end
    vm.startInput = new Date(context.start); vm.endInput = new Date(context.end)
    for (const item of vm.allNodes) for (const order of item.orders) {
        order.requirementId = vm.requirements[item.id].id
        order.snapshot = plan.snapshot(vm.requirements[item.id])
    }
    return mounted
}

function quantityResult(versionId = 31, quantity = 100) {
    const info = { dataType: 1, dataId: 7, code: '真实物料', name: '真实半成品', specification: '规格', unitId: 2, unit: 'KG' }
    const child = { ...info, dataId: 8, id: 'root/7/8', parentId: 'root/7', level: 2, kind: 'material', status: '部分缺料',
        sourceCraftVersionId: 32, materialRlIds: [103], nodeSettingTemplateIds: [203], craftVersionId: null,
        gross: 18.2345, stock: 20, occupied: 3, safetyStock: 1, available: 10.1234, net: 8.1111,
        transit: null, inProduction: null, scheduledTotal: null, children: [], substitutes: [] }
    const main = { ...info, id: 'root/7', parentId: 'root', level: 1, kind: 'semi', status: '待排产',
        sourceCraftVersionId: versionId, materialRlIds: [101, 102], nodeSettingTemplateIds: [201, 202], craftVersionId: 32,
        gross: 77.1234, stock: 20, occupied: 4, safetyStock: 2, available: 14, net: 63.1234,
        transit: null, inProduction: null, scheduledTotal: null, children: [child], substitutes: [
            { ...info, dataId: 9, materialRlIds: [104], nodeSettingTemplateIds: [202], craftVersionId: null, quantityPerParentUnit: 0.3333 }
        ] }
    return { craftOrderId: 21, craftOrderCode: '实际工序单', craftOrderName: '实际工序单名称', craftVersionId: versionId,
        craftVersion: `V${versionId}`, product: { ...info, dataId: 1, name: '实际产品' }, quantity, quantityUnitId: 3,
        quantityUnit: '箱', quantityBasis: 'inventory', nodeCount: 2, nodes: [main], materials: [] }
}

test('page starts empty and uses actual query quantities, units and source IDs without mock recomputation', async t => {
    const calls = []
    const { vm, messages } = mount(t, 'index.vue', {}, { post: async (url, body) => {
        calls.push({ url, body }); return { data: quantityResult(body.craftVersionId, body.quantity) }
    } })
    assert.deepEqual(vm.nodes, []); assert.equal(vm.product.code, '')
    vm.selectedCraftVersionId = 31; vm.quantityInput = 12.3456
    await vm.loadMaterialQuantities()
    assert.deepEqual(calls, [{ url: api.quantityApi, body: { craftVersionId: 31, quantity: 12.3456 } }])
    assert.equal(vm.product.name, '实际产品'); assert.equal(vm.product.unit, '箱')
    assert.equal(vm.product.orderCode, '实际工序单'); assert.equal(vm.product.version, 'V31')
    const n = vm.nodes[0]
    assert.deepEqual(n.serverMaterial.materialRlIds, [101, 102])
    assert.deepEqual(n.serverMaterial.nodeSettingTemplateIds, [201, 202])
    assert.equal(n.serverMaterial.sourceCraftVersionId, 31); assert.equal(n.serverMaterial.craftVersionId, 32)
    assert.equal(n.serverMaterial.substitutes[0].dataId, 9)
    assert.equal(vm.requirements[n.id].gross, 77.1234)
    assert.equal(vm.requirements[n.id].net, 63.1234)
    assert.equal(vm.requirements[n.children[0].id].available, 10.1234)
    assert.equal(vm.cellValue(n, 'net'), 63.1234)
    for (const key of ['transit', 'inProduction', 'scheduledTotal', 'incoming', 'reserved']) assert.equal(vm.cellValue(n, key), '未查询')
    assert.equal(plan.quantitySummary(n, vm.requirements[n.id]).transit, null)
    vm.keyword = n.children[0].name; assert.equal(vm.treeRows[0].children.length, 1)
    vm.openSchedule(n); assert.equal(vm.drafts.length, 0); assert.ok(messages.at(-1).includes('尚未接入'))
    vm.materialTarget = n; vm.saveReceipt(); assert.equal(n.receipts.length, 0)
})

test('refresh and recalculate request current quantity; changed input increments applied revision only after success', async t => {
    let requests = 0
    const { vm } = mount(t, 'index.vue', {}, { post: async (url, body) => {
        requests++; return { data: quantityResult(body.craftVersionId, body.quantity) }
    } })
    vm.selectedCraftVersionId = 31
    await vm.loadMaterialQuantities(); await vm.refreshStock()
    assert.equal(requests, 2); assert.equal(vm.revision, 1)
    vm.quantityInput = 0.0001; await vm.recalculate()
    assert.equal(vm.quantity, 0.0001); assert.equal(vm.revision, 2)
    assert.equal(vm.requirements['root/7'].gross, 77.1234)
    vm.quantityInput = 0; await vm.recalculate()
    vm.quantityInput = 1.00001; await vm.recalculate()
    vm.quantityInput = 1.00000000000001; await vm.recalculate()
    vm.quantityInput = 99999999.99999; await vm.recalculate()
    assert.equal(requests, 3); assert.equal(vm.quantity, 0.0001); assert.equal(vm.revision, 2)
})

test('version changes and concurrent refreshes ignore older responses and keep current loading state', async t => {
    const pending = []
    const { vm } = mount(t, 'index.vue', {}, { post: (url, body) => new Promise(resolve => pending.push({ body, resolve })) })
    vm.selectedCraftVersionId = 31
    const first = vm.loadMaterialQuantities()
    vm.changeCraftVersion(41)
    assert.deepEqual(vm.nodes, []); assert.equal(vm.isLoading, true)
    pending[0].resolve({ data: quantityResult(31) }); await first
    assert.deepEqual(vm.nodes, []); assert.equal(vm.isLoading, true)
    pending[1].resolve({ data: quantityResult(41) }); await new Promise(resolve => setImmediate(resolve))
    assert.equal(vm.quantityResult.craftVersionId, 41); assert.equal(vm.isLoading, false)
    const older = vm.refreshStock(), newer = vm.refreshStock()
    pending[3].resolve({ data: { ...quantityResult(41), nodes: [] } }); await newer
    pending[2].resolve({ data: quantityResult(41) }); await older
    assert.deepEqual(vm.nodes, []); assert.equal(vm.isLoading, false)
})

test('query errors and invalid payload do not fall back to example data', async t => {
    let fail = true
    const { vm, messages } = mount(t, 'index.vue', {}, { post: async () => {
        if (fail) throw new Error('接口错误')
        return { data: [] }
    } })
    vm.selectedCraftVersionId = 31; await vm.loadMaterialQuantities()
    assert.deepEqual(vm.nodes, []); assert.equal(vm.isLoading, false); assert.ok(vm.queryError)
    assert.equal(messages[0].message, '接口错误')
    fail = false; await vm.loadMaterialQuantities()
    assert.deepEqual(vm.nodes, []); assert.equal(messages.at(-1).message, '物料数量接口返回数据无效')
})

test('craft picker uses paged host query and selected order versions, prioritizing main version', async t => {
    const calls = []
    const { vm } = mount(t, 'index.vue', {}, {
        get: async (url, body) => { calls.push({ url, body }); return { data: [
            { id: 30, craftOrderId: 21, version: 'V30', isMain: false },
            { id: 31, craftOrderId: 21, version: 'V31', isMain: true }
        ] } },
        post: async (url, body) => {
            calls.push({ url, body })
            return { data: url === api.quantityApi ? quantityResult(body.craftVersionId, body.quantity) : [
                { id: 21, code: '工序单', name: '工序单名', productNo: 'P', productName: '产品' }
            ] }
        }
    })
    vm.openDialog = () => {}; vm.closeDialog = () => {}
    await vm.openCraftPicker(); vm.selectedCraftOrder = vm.craftOrders[0]; await vm.selectCraftOrder()
    assert.equal(vm.selectedCraftVersionId, 31); assert.equal(vm.quantityResult.craftVersionId, 31)
    assert.equal(calls[0].url, 'api/technical/CraftOrderManage/GetCraftOrderList')
    assert.deepEqual(calls[1].body, { craftOrderId: 21 })
    assert.deepEqual(calls[2].body, { craftVersionId: 31, quantity: 100 })
})

test('parent can supply and change craft version prop for automatic loading', async t => {
    const calls = []
    const { vm, props } = mount(t, 'index.vue', { craftVersionId: 31, initialQuantity: 5.4321 }, {
        post: async (url, body) => { calls.push(body); return { data: quantityResult(body.craftVersionId, body.quantity) } }
    })
    await new Promise(resolve => setImmediate(resolve))
    assert.equal(vm.quantityResult.craftVersionId, 31); assert.equal(vm.quantity, 5.4321)
    props.craftVersionId = 41; await nextTick(); await new Promise(resolve => setImmediate(resolve))
    assert.equal(vm.quantityResult.craftVersionId, 41); assert.equal(calls.length, 2)
})

test('route query loads specified version and clears the old tree when entry is removed', async t => {
    const route = reactive({ query: { craftVersionId: '31' } })
    const { vm } = mount(t, 'index.vue', {}, {
        post: async (url, body) => ({ data: quantityResult(body.craftVersionId, body.quantity) })
    }, route)
    await new Promise(resolve => setImmediate(resolve))
    assert.equal(vm.quantityResult.craftVersionId, 31)
    route.query.craftVersionId = '41'; await nextTick(); await new Promise(resolve => setImmediate(resolve))
    assert.equal(vm.quantityResult.craftVersionId, 41)
    route.query.craftVersionId = ''; await nextTick()
    assert.equal(vm.selectedCraftVersionId, 0); assert.deepEqual(vm.nodes, []); assert.equal(vm.quantityResult, null)
})

test('failed refresh retains last successful quantities and selected version rather than changing applied inputs', async t => {
    let fail = false
    let malformed = false
    const { vm } = mount(t, 'index.vue', {}, { post: async (url, body) => {
        if (fail) throw new Error('刷新错误')
        const data = quantityResult(body.craftVersionId, body.quantity)
        if (malformed) data.nodes[0].children = undefined
        return { data }
    } })
    vm.selectedCraftVersionId = 31; await vm.loadMaterialQuantities()
    vm.quantityInput = 10; fail = true; await vm.recalculate()
    assert.equal(vm.quantity, 100); assert.equal(vm.quantityResult.quantity, 100)
    assert.equal(vm.revision, 1); assert.equal(vm.requirements['root/7'].gross, 77.1234)
    assert.ok(vm.queryError.includes('上次')); assert.equal(vm.isLoading, false)
    fail = false; malformed = true; await vm.recalculate()
    assert.equal(vm.quantity, 100); assert.equal(vm.quantityResult.quantity, 100)
    assert.equal(vm.requirements['root/7'].gross, 77.1234)
})

test('MRP propagates parent production, not finished goods gross demand; follows source and dates', () => {
    const nodes = model.createMockTree(), r = plan.calculateRequirements(nodes, context)
    assert.equal(r.a.gross, 100); assert.equal(r.a.available, 15); assert.equal(r.a.incoming, 40); assert.equal(r.a.net, 45)
    assert.equal(r['a-shell'].gross, 85); assert.equal(r['a-shell'].net, 55)
    assert.equal(r.steel.gross, 99); assert.equal(r.pipe.gross, 110)
    assert.equal(r.e.production, 0); assert.equal(r.film.gross, 0)
    assert.equal(r['a-shell'].parentId, r.a.id); assert.equal(r['a-shell'].requiredDate, '2026-08-28T08:00')
    assert.equal(r.screw.gross, 216)
})
test('safe stock, date cutoff, cancellation and zero demand are handled without negative net', () => {
    const n = node('x', { stock: 20, occupied: 5, safetyStock: 3 })
    n.orders = [{ id: 'on-time', quantity: 40, start: context.start, end: context.end }, { id: 'late', quantity: 25, start: context.start, end: '2026-09-06T18:00:00' }, { id: 'cancelled', quantity: 90, start: context.start, end: context.end, status: 'cancelled' }]
    n.receipts = [{ id: 'purchase', quantity: 10, arrival: context.end, supplier: '供应商', status: 'planned' }, { id: 'late-purchase', quantity: 8, arrival: '2026-09-08T08:00:00', supplier: '', status: 'planned' }]
    const r = plan.calculateRequirements([n], context).x
    assert.equal(r.available, 12); assert.equal(r.incoming, 50); assert.equal(r.net, 38); assert.equal(r.late, 25)
    assert.equal(model.availableStock(node('bad', { stock: 2, occupied: 10 })), 0)
    assert.equal(plan.calculateRequirements([node('zero')], { ...context, quantity: 0 }).zero.net, 0)
    assert.ok(Number.isNaN(plan.timestamp('2026-02-30T08:00')))
    assert.ok(Number.isNaN(plan.timestamp('2026-08-28T08:00Z')))
})
test('same goods inventory and receipts are allocated only once across separate requirement instances', () => {
    const receipt = { id: 'PUR-1', quantity: 20, arrival: context.start, supplier: '供应商', status: 'planned' }
    const nodes = [node('one', { kind: 'material', code: 'shared', stock: 30, baseDemand: 40, receipts: [receipt] }), node('two', { kind: 'material', code: 'shared', stock: 30, baseDemand: 40, receipts: [receipt] })]
    const r = plan.calculateRequirements(nodes, context)
    assert.equal(r.one.available, 30); assert.equal(r.one.incoming, 10); assert.equal(r.one.net, 0)
    assert.equal(r.two.available, 0); assert.equal(r.two.incoming, 10); assert.equal(r.two.net, 30)
    assert.equal(r.one.receiptSupplies[0].quantity + r.two.receiptSupplies[0].quantity, 20)
})
test('material readiness consumes earlier commitments and waits for known receipt date', () => {
    const raw = node('raw', { kind: 'material', stock: 10 }), parent = node('parent', { children: [raw] })
    raw.receipts = [{ id: 'PUR-1', quantity: 90, arrival: '2026-08-30T08:00:00', supplier: '', status: 'planned' }]
    const first = submission(parent, 30)
    first.details.materials = [{ id: 'raw', required: 30, reserved: 10, shortage: 20, consumed: 0, issued: false }]
    parent.orders.push(order(first))
    const nodes = [parent], requirements = plan.calculateRequirements(nodes, context)
    const material = plan.materialPlans(parent, 30, context.start, requirements, nodes)[0]
    assert.equal(material.available, 0); assert.equal(material.shortage, 30); assert.equal(material.readyAt, '2026-08-30T08:00:00')
    assert.ok(plan.validateSchedule(submission(parent, 30), nodes, context).some(error => error.includes('最早齐套')))
    assert.ok(!plan.validateSchedule(submission(parent, 30, '2026-08-30T08:00:00'), nodes, context).some(error => error.includes('最早齐套')))
})
test('validation rejects overplan, broken dependencies, undersized capacity and missing outsource / warehouse fields', () => {
    const n = node('n')
    assert.deepEqual(plan.validateSchedule(submission(n, 30), [n], context), [])
    assert.ok(plan.validateSchedule(submission(n, 101), [n], context).some(error => error.includes('当前净需求')))
    const v = submission(n, 30)
    v.details.processes[1].start = v.start
    v.details.processes[2].dispatches[0].quantity = 20
    v.details.processes[3].warehouse = ''
    v.details.processes[0].outsource = true
    const errors = plan.validateSchedule(v, [n], context)
    for (const fragment of ['前序关系', '派工数量合计', '目标仓库', '外协供应商']) assert.ok(errors.some(error => error.includes(fragment)), errors.join(';'))
    const short = submission(n, 30)
    short.details.processes[0].end = '2026-08-28T08:01:00'
    assert.ok(plan.validateSchedule(short, [n], context).some(error => error.includes('时长不足')))
})
test('resource overlaps reject both equipment and staff, but adjacent bookings are allowed', () => {
    const n = node('n'), existing = submission(n, 30)
    n.orders.push(order(existing))
    const duplicate = submission(n, 30)
    assert.ok(plan.resourceConflicts(duplicate.details.processes, [n]).length >= 8)
    assert.ok(plan.validateSchedule(duplicate, [n], context).some(error => error.includes('时间冲突')))
    assert.equal(plan.resourceConflicts(submission(n, 30, existing.end).details.processes, [n]).length, 0)
    n.orders[0].status = 'cancelled'
    assert.equal(plan.resourceConflicts(duplicate.details.processes, [n]).length, 0)
})
test('immediate execution requires physical stock and a current execution window; late commitments cannot duplicate', () => {
    const raw = node('raw', { kind: 'material' }), n = node('n', { children: [raw] })
    const v = submission(n, 10); v.details.immediateStart = true
    assert.ok(plan.validateSchedule(v, [n], context).some(error => error.includes('当前库存齐套')))
    assert.ok(plan.validateSchedule(v, [n], context).some(error => error.includes('当前时间')))
    n.orders.push({ id: 'late', quantity: 100, start: context.start, end: '2026-09-08T08:00:00' })
    assert.ok(plan.validateSchedule(submission(n, 10), [n], context).some(error => error.includes('重复生产')))
})
test('owner saves detached operation/material snapshots, reserves once, rejects double save and releases cancellation', t => {
    const { vm } = mountPlanningFixture(t)
    const n = vm.allNodes.find(item => item.id === 'd'), value = submission(n, 40)
    vm.scheduleTarget = n; vm.saveSingleSchedule(value)
    const saved = n.orders.at(-1)
    assert.ok(saved); assert.equal(saved.requirementId, vm.requirements.d.id); assert.equal(saved.snapshot.net, 80)
    assert.equal(saved.details.materials[0].reserved, 80); assert.equal(plan.reservedQuantity(vm.nodes, 'washer'), 80)
    value.details.processes[0].name = 'changed'
    assert.equal(saved.details.processes[0].name, '下料')
    vm.saveSingleSchedule(value); assert.equal(n.orders.length, 1)
    vm.cancelOrder(saved.id); assert.equal(plan.reservedQuantity(vm.nodes, 'washer'), 0); assert.equal(vm.requirements.d.net, 80)
})
test('tree filtering preserves ancestors and seven quantity columns remain in the agreed order', t => {
    const { vm } = mountPlanningFixture(t)
    vm.keyword = '钢管'; assert.equal(vm.treeRows.length, 1); assert.equal(vm.treeRows[0].id, 'a'); assert.equal(vm.treeRows[0].children[0].id, 'a-shell')
    assert.equal(vm.treeRows[0].state, 'open')
    assert.deepEqual(materialQuantityColumns(), ['总需求量', '库存总量', '占用量', '在途量', '在制量', '已排产量', '缺量'])
})
test('purchase suggestions update forecast without changing physical inventory and prevent duplicate / late supply', t => {
    const { vm } = mountPlanningFixture(t)
    const raw = vm.allNodes.find(item => item.id === 'adhesive'), stock = raw.stock
    vm.materialTarget = raw; vm.receiptForm = { quantity: vm.shortage(raw), arrival: new Date(vm.requirements[raw.id].requiredDate), supplier: '供应商' }
    vm.saveReceipt(); assert.equal(vm.shortage(raw), 0); assert.equal(raw.stock, stock); assert.equal(raw.receipts.length, 1)
    vm.saveReceipt(); assert.equal(raw.receipts.length, 1)
    raw.receipts[0].status = 'cancelled'; assert.ok(vm.shortage(raw) > 0)
    vm.receiptForm = { quantity: vm.shortage(raw), arrival: new Date('2026-09-30T08:00:00'), supplier: '供应商' }
    vm.saveReceipt(); assert.ok(vm.receiptError.includes('需求日期')); assert.equal(raw.receipts.length, 1)
})
test('drawer quantity changes keep single dispatch quantities aligned and saves every operation', async t => {
    const n = node('drawer'), nodes = [n], requirements = plan.calculateRequirements(nodes, context)
    const { vm, events } = mount(t, 'component/create-schedule.vue', { node: n, nodes, requirements, context })
    vm.form.quantity = 20; await nextTick()
    assert.ok(vm.processes.every(process => process.dispatches[0].quantity === 20))
    vm.processes.forEach(process => { process.dispatches[0].operator = '张三'; process.dispatches[0].machine = process.code })
    vm.suggestTimes(); vm.save()
    assert.deepEqual(vm.errors, []); assert.equal(events.length, 1); assert.equal(events[0].details.processes.length, 4)
    assert.equal(events[0].details.immediateStart, false)
})
test('gantt renders operation-level dependencies, live material risk, stale revisions and cancelled orders', () => {
    const raw = node('raw', { kind: 'material' }), n = node('gantt', { children: [raw] }), v = submission(n, 20)
    v.details.materials = [{ id: 'raw', required: 20, reserved: 0, shortage: 20, consumed: 0, issued: false }]
    const saved = order(v); saved.snapshot = plan.snapshot(plan.calculateRequirements([n], context)[n.id]); n.orders.push(saved)
    const rows = gantt.ganttEntries([n], context)
    assert.equal(rows.length, 4); assert.equal(rows[1].process.predecessor, rows[0].process.code); assert.ok(rows[0].risk.includes('缺 20'))
    raw.receipts = [{ id: 'PUR', quantity: 100, arrival: context.start, supplier: '', status: 'planned' }]
    assert.ok(!gantt.ganttEntries([n], context)[0].risk.includes('缺 20'))
    assert.ok(gantt.ganttEntries([n], { ...context, revision: 2 })[0].risk.includes('需求版本'))
    assert.equal(gantt.ganttEntries([n], context, '', false).length, 1)
    saved.status = 'cancelled'; assert.equal(gantt.ganttEntries([n], context).length, 0)
})
test('gantt uses calendar month bounds and keeps exact local hours for long ranges', t => {
    const n = node('timeline'), v = submission(n, 20)
    n.orders.push(order(v))
    const { vm } = mount(t, 'component/schedule-gantt.vue', { nodes: [n], planStart: context.start, planEnd: '2026-11-05T18:00:00', revision: 1, quantity: 100 })
    assert.equal(vm.range.start.getDate(), 1); assert.equal(vm.range.start.getHours(), 0)
    assert.ok(vm.range.end > new Date('2026-11-05T18:00:00'))
    const date = new Date(v.start), midnight = new Date(date); midnight.setHours(0, 0, 0, 0)
    assert.equal(plan.timestamp(v.start) - midnight.getTime(), 8 * 3600000)
    const task = vm.chartData.tasks.find(task => task.id === 'order:APS-TEST')
    assert.equal(task.start.getTime(), date.getTime()); assert.equal(task.start.getHours(), 8)
})

test('seven quantity totals separate transit, running, planned and cancelled supplies while keeping deadline net', () => {
    const n = node('totals', { stock: 20, occupied: 5 })
    n.receipts = [{ id: 'PUR', quantity: 10, arrival: context.end, supplier: '', status: 'planned' }]
    n.orders = [
        { id: 'planned', quantity: 20, start: context.start, end: context.end, status: 'planned' },
        { id: 'running', quantity: 15, start: context.start, end: context.end, status: 'running' },
        { id: 'late', quantity: 5, start: context.start, end: '2026-09-08T08:00:00' },
        { id: 'cancelled', quantity: 99, start: context.start, end: context.end, status: 'cancelled' }
    ]
    assert.deepEqual(plan.quantitySummary(n, plan.calculateRequirements([n], context).totals), {
        gross: 100, stock: 20, occupied: 5, transit: 10, inProduction: 15, scheduledTotal: 25, net: 40
    })
})

test('inline draft rows preserve selection across semi-finished goods and do not reserve until saved', t => {
    const { vm } = mountPlanningFixture(t)
    const d = vm.allNodes.find(node => node.id === 'd'), a = vm.allNodes.find(node => node.id === 'a')
    assert.deepEqual(materialQuantityColumns(),
        ['总需求量', '库存总量', '占用量', '在途量', '在制量', '已排产量', '缺量'])
    vm.openSchedule(d); const draftId = vm.currentDraft.id
    vm.openSchedule(d); assert.equal(vm.drafts.length, 1); assert.equal(d.orders.length, 0)
    assert.deepEqual(vm.scheduleListRows.map(row => ({ ...row })), [{ id: draftId, quantity: null, start: '', end: '', status: 'draft', draft: true }])
    assert.equal(vm.selectedListRow.id, draftId)
    assert.equal(plan.reservedQuantity(vm.nodes, 'washer'), 0)
    vm.openSchedule(a); assert.equal(vm.drafts.length, 2)
    const aDraftId = vm.currentDraft.id, savedRow = vm.scheduleListRows.find(row => !row.draft)
    vm.selectListRow(savedRow); assert.equal(vm.selectedOrder.id, savedRow.id); assert.equal(vm.scheduleTarget, null)
    assert.equal(vm.currentDraft.id, aDraftId)
    vm.selectListRow(vm.scheduleListRows.find(row => row.draft)); assert.equal(vm.selectedOrder, undefined)
    assert.equal(vm.scheduleTarget.id, a.id)
    vm.selectRow(vm.treeRows.find(row => row.id === 'd')); assert.equal(vm.selectedOrderId, draftId)
    vm.cancelDraftRow(draftId); assert.equal(vm.drafts.length, 1); assert.equal(vm.currentDraft, undefined)
    assert.deepEqual(vm.scheduleListRows, []); assert.equal(d.orders.length, 0)
    assert.equal(vm.selectedId, 'd')
})

test('new inline form starts with blank quantity and dates', t => {
    const n = node('blank'), nodes = [n]
    const { vm } = mount(t, 'component/create-schedule.vue', { node: n, nodes, requirements: plan.calculateRequirements(nodes, context), context, blank: true })
    assert.equal(vm.form.quantity, null); assert.equal(vm.form.start, null); assert.equal(vm.form.end, null)
    vm.save(); assert.ok(vm.errors.length)
})

test('gantt preserves five levels, focused ancestor paths, dependencies and two full months', () => {
    const child = node('child'), parent = node('parent', { children: [child] }), sibling = node('sibling')
    child.orders.push(order(submission(child, 10), 'CHILD'))
    sibling.orders.push(order(submission(sibling, 10), 'SIBLING'))
    const data = timeline.timelineData([parent, sibling], context, child.id)
    const chain = []; let task = data.tasks.find(task => task.id === 'process:CHILD:040')
    while (task) { chain.unshift(task.id); task = data.tasks.find(candidate => candidate.id === task.parent) }
    assert.deepEqual(chain, ['product', 'semi:parent', 'semi:child', 'order:CHILD', 'process:CHILD:040'])
    assert.ok(!data.tasks.some(task => task.orderId === 'SIBLING')); assert.equal(data.links.length, 3)
    const range = timeline.timelineRange(context, data.tasks)
    assert.equal(range.start.getMonth(), 7); assert.equal(range.end.getMonth(), 9)
    assert.equal(range.end.getDate(), 1)
    assert.deepEqual(timeline.timelineData([parent], context, 'missing').tasks, [])
    child.orders[0].status = 'cancelled'; assert.deepEqual(timeline.timelineData([parent], context).tasks, [])
})

test('dragging order shifts all processes and rejects overlap, short capacity, deadline and running orders without mutation', () => {
    const n = node('move'), value = submission(n, 10), saved = order(value); n.orders.push(saved)
    const original = JSON.stringify(saved)
    const change = { orderId: saved.id, start: '2026-08-29T08:00:00', end: model.localDateTime(new Date(plan.timestamp(value.end) + 86400000)) }
    const result = timeline.scheduleChange([n], context, change)
    assert.deepEqual(result.errors, []); assert.equal(JSON.stringify(saved), original)
    assert.equal(result.value.details.processes[0].start, change.start)
    assert.equal(plan.timestamp(result.value.details.processes[1].start) - plan.timestamp(value.details.processes[1].start), 86400000)
    const broken = timeline.scheduleChange([n], context, { orderId: saved.id, processCode: '020', start: value.start, end: value.end })
    assert.ok(broken.errors.some(error => error.includes('前序')))
    assert.ok(timeline.scheduleChange([n], context, { ...change, end: '2026-09-06T18:00:00' }).errors.some(error => error.includes('需求日期')))
    assert.ok(timeline.scheduleChange([n], context, { ...change, end: '2026-08-29T09:00:00' }).errors.some(error => error.includes('时长') || error.includes('工序时间')))
    saved.status = 'running'; assert.ok(timeline.scheduleChange([n], context, change).errors.some(error => error.includes('在制')))
})


test('process editor emits detached changes and retains the original selection controls without mutating parent data', t => {
    const process = plan.createProcesses(20, context.start)[0]
    process.dispatches.push({ id: 99, operator: '李四', machine: '02', quantity: 10 })
    const original = JSON.parse(JSON.stringify(process))
    const { vm, updates } = mount(t, 'component/schedule-process.vue', {
        modelValue: process, quantity: 20, unit: 'PCS', operators: [], machines: []
    })
    vm.updateField('outsource', true)
    assert.equal(updates.at(-1).outsource, true)
    vm.updateDispatch(1, 'operator', '王五')
    assert.equal(updates.at(-1).dispatches[0].operator, '王五')
    assert.notEqual(updates.at(-1).dispatches[0], process.dispatches[0])
    vm.removeDispatch(99)
    assert.equal(updates.at(-1).dispatches.length, 1)
    vm.updateDate('start', new Date('2026-08-29T09:15:00'))
    assert.equal(updates.at(-1).start, '2026-08-29T09:15:00')
    assert.deepEqual(process, original)
    assert.deepEqual(vm.productionModes, [{ text: '内部生产', value: false }, { text: '外协', value: true }])
})

test('status enums retain query values and cancellation semantics, while Gantt uses actual product information', () => {
    const enums = load(path.join(root, '../../../types/enums.ts'))
    const dto = load(path.join(root, '../../../types/production-schedule.ts'))
    assert.equal(enums.ProductionScheduleStatus.已排产, 'planned')
    assert.equal(enums.ScheduleReceiptStatus.已撤销, 'cancelled')
    assert.equal(enums.MaterialRequirementStatus.待排产, '待排产')
    assert.equal(new dto.MaterialNodeDto().transit, null)
    const first = new dto.ScheduleNodeDto(), second = new dto.ScheduleNodeDto()
    first.orders.push({ id: 'one' }); assert.equal(second.orders.length, 0)
    const n = node('actual-plan'), v = submission(n, 20)
    n.orders.push(order(v))
    const data = timeline.timelineData([n], { ...context, product: { name: '实际产品', orderCode: 'ACTUAL-001' } })
    assert.equal(data.tasks[0].text, '实际产品 / ACTUAL-001')
    assert.equal(data.tasks.find(task => task.orderId).status, enums.ProductionScheduleStatus.已排产)
    assert.equal(plan.scheduleStatusText(enums.ProductionScheduleStatus.已撤销), '已撤销')
    n.orders[0].status = enums.ProductionScheduleStatus.已撤销
    assert.equal(plan.activeOrders(n).length, 0)
})
