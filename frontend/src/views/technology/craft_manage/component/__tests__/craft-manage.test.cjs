// node --test; use the host project's vue, @vue/compiler-sfc and typescript dependencies.
const assert = require('node:assert/strict')
const fs = require('node:fs')
const path = require('node:path')
const { test } = require('node:test')
const ts = require('typescript')
const { parse, compileScript, compileTemplate } = require('@vue/compiler-sfc')
const { createRenderer, h, reactive, ref, nextTick } = require('vue')
const status = { 暂存: 0, 待审核: 1, 已发布: 2, 已废弃: 3 }
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
    const output = ts.transpileModule(source, { compilerOptions: {
        module: ts.ModuleKind.CommonJS, target: ts.ScriptTarget.ES2020
    }, reportDiagnostics: true })
    assert.deepEqual(output.diagnostics, [])
    const module = { exports: {} }
    const localRequire = id => {
        if (id.endsWith('.css')) return {}
        if (id.startsWith('.') && fs.existsSync(path.resolve(path.dirname(filename), id + '.ts'))) return load(path.resolve(path.dirname(filename), id + '.ts'))
        if (/types\/craft(\.js)?$/.test(id)) return load(path.join(__dirname, '../../../../../types/craft.ts'))
        // These shared modules are absent from this source snapshot. Stub their constants only.
        if (id.endsWith('/enums')) return {
            CraftOrderVersionStatus: status, SystemParaType: { 耗材偏差值: 1 }, ShowFormDataType: { 文本: 0 }, DataType: { Text: 0 }, FilterType: { Contains: 0 },
            ProductType: { 原材料: 0, 半成品: 1, 成品: 2, 0: '原材料', 1: '半成品', 2: '成品' },
            FileBase: { Craft: 90, CraftInstruction: 91 }, FileStorageSourceType: { 工序文件: 900, 工序作业指导书: 910 }
        }
        if (id.endsWith('/common')) return { InventoryDataType: { Product: 0, Specification: 1 } }
        if (id.endsWith('/types')) return { Search: class { constructor(...args) { this.args = args } } }
        if (id.endsWith('.vue')) return load(path.resolve(path.dirname(filename), id))
        return require(id)
    }
    new Function('require', 'module', 'exports', output.outputText)(localRequire, module, module.exports)
    cache.set(filename, module.exports)
    return module.exports
}
const renderer = createRenderer({
    createElement: () => ({}), createText: () => ({}), createComment: () => ({}),
    insert() {}, remove() {}, setText() {}, setElementText() {}, patchProp() {},
    parentNode: () => null, nextSibling: () => null
})
function mount(t, file, input = {}, http = {}, applyUpdates = true) {
    const component = { ...load(path.join(__dirname, '..', file)).default, render: () => null }
    const props = reactive(input), instance = ref(), messages = [], errors = [], events = []
    // Mount a parent that handles emitted changes, as create-craft does in the app.
    const apply = (method, payload) => {
        events.push([method, payload])
        if (!applyUpdates || !props.process) return
        const methods = load(path.join(__dirname, '../../create-craft.vue')).default.methods
        const owner = { selectedProcess: props.process, canEditCurrentVersion: !props.readonly, routeItems: [props.process] }
        owner.updateMaterialSort = methods.updateMaterialSort.bind(owner)
        owner.normalizeRoutes = methods.normalizeRoutes.bind(owner)
        methods[method].call(owner, payload)
    }
    const app = renderer.createApp({ render: () => h(component, {
        ...props, ref: instance, onCreated: order => events.push(['created', order]),
        onSaved: order => events.push(['saved', order]),
        onCopied: result => events.push(['copied', result]),
        onVersionSaved: () => events.push(['versionSaved']),
        onResourcesAdded: () => events.push(['resourcesAdded']),
        onUpdateProcess: changes => apply('updateProcess', changes),
        onUpdateMaterial: changes => apply('updateMaterial', changes),
        onBeforeSave: () => {
            events.push(['beforeSave'])
            if (applyUpdates && props.routes) {
                const methods = load(path.join(__dirname, '../../create-craft.vue')).default.methods
                methods.normalizeRoutes.call({ routeItems: props.routes })
            }
        },
        'onUpdate:routes': routes => events.push(['routes', routes])
    }) })
    app.config.globalProperties.$http = { get: async () => ({ data: [] }), post: async () => ({ data: [] }), ...http }
    app.config.globalProperties.$kingzon_messager = { showInfo: x => messages.push(x), showError: x => errors.push(x), showConfim: async () => {} }
    app.mount({})
    t.after(() => app.unmount())
    return { vm: instance.value, props, messages, errors, events }
}
let processKey = 0
const process = extra => ({ uiKey: `test-${++processKey}`, code: '10', materials: [], isInbound: false, isOutsource: false, ...extra })
const resource = (id = 10) => ({ dataId: id, dataType: 0, unitId: 1, materialName: `物料${id}`, materialType: '物料' })
const deferred = () => {
    let resolve, reject
    const promise = new Promise((yes, no) => { resolve = yes; reject = no })
    return { promise, resolve, reject }
}

test('new craft versions default to draft and publishing waits for a successful save', async t => {
    const { CraftVersionDto, CraftVersionDetailDto } = load(path.join(__dirname, '../../../../../types/craft.ts'))
    assert.equal(new CraftVersionDto().status, status.暂存)
    assert.equal(new CraftVersionDetailDto().status, status.暂存)
    const pending = deferred(), calls = []
    const { vm } = mount(t, 'craft-order-header.vue', {
        mode: 'edit', activeVersionId: 5, modelValue: { id: 9 },
        routes: [process({ isReportWork: true, isInbound: true })]
    }, { post: async (url, body) => {
        calls.push({ url, body })
        return url.endsWith('SaveCraftVersion') ? pending.promise : { data: {} }
    } })
    vm.versionList = [{ id: 5, status: status.暂存 }]
    vm.loadedVersionId = 5
    vm.init = async () => {}
    assert.equal(vm.canEditVersion, true)
    const publishing = vm.publishVersion()
    await vm.publishVersion()
    assert.equal(calls.length, 1)
    assert.ok(calls[0].url.endsWith('SaveCraftVersion'))
    pending.resolve({ data: {} })
    await publishing
    assert.ok(calls[1].url.endsWith('PublishCraftVersion'))
    assert.deepEqual(calls[1].body, { id: 5 })
    assert.equal(vm.saving, false)
})

test('failed or invalid drafts cannot publish and an empty route gives a useful error', async t => {
    const calls = []
    const { vm, props, errors } = mount(t, 'craft-order-header.vue', {
        mode: 'edit', activeVersionId: 5, modelValue: { id: 9 }, routes: []
    }, { post: async url => { calls.push(url); throw new Error('save failed') } })
    vm.versionList = [{ id: 5, status: status.暂存 }]; vm.loadedVersionId = 5
    await vm.publishVersion()
    assert.match(errors[0], /最后一道工序/)
    props.routes = [process({ isReportWork: true, isInbound: true, code: '' })]
    await nextTick(); await vm.publishVersion()
    assert.equal(calls.length, 0)
    props.routes[0].code = '10'
    await vm.publishVersion()
    assert.equal(calls.length, 1)
    assert.ok(calls[0].endsWith('SaveCraftVersion'))
    assert.equal(vm.saving, false)
})

test('obsolete versions can be restored to editable drafts or deleted', async t => {
    const calls = [], refreshed = []
    const { vm } = mount(t, 'craft-order-header.vue', { mode: 'edit', activeVersionId: 5, modelValue: { id: 9 } }, {
        post: async (url, body) => { calls.push({ url, body }); return { data: {} } }
    })
    vm.versionList = [{ id: 5, status: status.已废弃 }, { id: 6, isMain: true, status: status.已发布 }]
    vm.loadedVersionId = 5
    vm.init = async id => { refreshed.push(id); vm.versionList[0].status = status.暂存 }
    assert.equal(vm.canEditVersion, false)
    await vm.restoreVersion()
    assert.ok(calls[0].url.endsWith('RestoreCraftVersion'))
    assert.equal(vm.canEditVersion, true)
    vm.versionList[0].status = status.已废弃
    await vm.delVersion(vm.versionList[0])
    assert.ok(calls[1].url.endsWith('DeleteCraftVersion'))
    assert.deepEqual(refreshed, [5, null])
})

test('new input materials select the published main version, including cached and shared substitute requests', async t => {
    const pending = deferred(), calls = []
    const { vm } = mount(t, 'craft-info.vue', { process: process() }, {
        get: async (url, query) => { calls.push(query); return pending.promise }
    })
    vm.insertResources([{ ...resource(10), materialType: '半成品' }, resource(20)], 'input')
    vm.insertResources([{ ...resource(10), materialType: '半成品' }], 'substitute', { main: vm.inputMaterials[1], position: 'append' })
    await nextTick()
    assert.equal(calls.length, 1)
    pending.resolve({ data: [{ id: 9, text: 'V9', isMain: false }, { id: 2, text: 'V2', isMain: true }] })
    await new Promise(resolve => setImmediate(resolve))
    assert.equal(vm.inputMaterials[0].refCraftOrderVersionId, 2)
    assert.equal(vm.inputMaterials[1].substitutes[0].refCraftOrderVersionId, 2)
    assert.equal(vm.versionOptionsByProduct[10][1].isMain, true)
    vm.insertResources([resource(30)], 'input')
    vm.insertResources([{ ...resource(10), materialType: '半成品' }], 'substitute', { main: vm.inputMaterials.find(row => row.dataId === 30), position: 'append' })
    await nextTick(); await nextTick()
    assert.equal(calls.length, 1)
    assert.equal(vm.inputMaterials.find(row => row.dataId === 30).substitutes[0].refCraftOrderVersionId, 2)
})

test('version option refresh preserves existing references and does not substitute the latest non-main version', async t => {
    const row = { ...resource(10), materialType: '半成品', refCraftOrderVersionId: 7 }
    const { vm } = mount(t, 'craft-info.vue', { process: process({ materials: [row], isInbound: true }) }, {
        get: async () => ({ data: [{ id: 2, text: 'V2', isMain: true }] })
    })
    await vm.loadPublishedVersions(row, true)
    assert.equal(vm.inputMaterials[0].refCraftOrderVersionId, 7)
    vm.versionOptionsByProduct[20] = [{ id: 9, text: 'V9', isMain: false }]
    vm.insertResources([{ ...resource(20), materialType: '成品' }], 'input')
    vm.insertResources([{ ...resource(10), materialType: '成品' }], 'output')
    await nextTick(); await nextTick()
    assert.equal(vm.inputMaterials.find(row => row.dataId === 20).refCraftOrderVersionId, undefined)
    assert.equal(vm.outputMaterials[0].refCraftOrderVersionId, undefined)
})

test('late main-version responses never overwrite a manual choice or update removed rows', async t => {
    const pending = deferred()
    const { vm } = mount(t, 'craft-info.vue', { process: process() }, { get: () => pending.promise })
    vm.insertResources([{ ...resource(10), materialType: '半成品' }, { ...resource(20), materialType: '半成品' }])
    await nextTick()
    const removed = vm.inputMaterials[1]
    vm.inputMaterials[0].refCraftOrderVersionId = 7
    vm.removeMaterial({ row: removed, main: removed })
    pending.resolve({ data: [{ id: 2, text: 'V2', isMain: true }] })
    await new Promise(resolve => setImmediate(resolve))
    assert.equal(vm.inputMaterials[0].refCraftOrderVersionId, 7)
    assert.equal(removed.refCraftOrderVersionId, undefined)
})

test('create validates product/name, prevents duplicate submits, then opens the saved order', async t => {
    const pending = deferred(), calls = []
    const { vm, messages, events } = mount(t, 'craft-create-dialog.vue', {}, { post: async (url, body) => {
        calls.push({ url, body })
        return calls.length === 1 ? pending.promise : { data: { id: 20 } }
    } })
    vm.$.refs = { dialog: { open() {}, close() {} }, productDialog: { close() {} } }
    await vm.save()
    assert.equal(calls.length, 0)
    assert.equal(messages.length, 1)
    vm.applyProduct({ id: 7, productName: '产品 A', productNo: 'A' })
    vm.form.name = ' 测试工艺单 '
    const saving = vm.save()
    await vm.save()
    assert.equal(calls.length, 1)
    assert.deepEqual(calls[0].body, { productId: 7, specificationId: undefined, name: '测试工艺单' })
    assert.equal(events.length, 0)
    pending.resolve({ data: { id: 9, name: '测试工艺单' } })
    await saving
    assert.equal(calls[1].body.craftOrderId, 9)
    assert.equal(events[0][1].id, 9)
    assert.equal(vm.saving, false)
})

test('a failed initial version still opens the already-created order', async t => {
    const { vm, events, messages } = mount(t, 'craft-create-dialog.vue', {}, { post: async url => {
        if (url.endsWith('CreateCraftVersion')) throw new Error('version failed')
        return { data: { id: 9 } }
    } })
    vm.$.refs = { dialog: { close() {} }, productDialog: { close() {} } }
    vm.applyProduct({ id: 7, productName: 'A' })
    await vm.save()
    assert.equal(events[0][1].id, 9)
    assert.match(messages[0], /工艺单已创建/)
})

test('rename uses a separate draft, preserves product info, and sends only id/name', async t => {
    const calls = []
    const { vm } = mount(t, 'craft-order-header.vue', { mode: 'edit', modelValue: {
        id: 9, name: '原名称', productId: 7, description: '原说明', materialInfo: { dataTechnicalCode: 'T-9', dataUnit: '件' }
    } }, { post: async (url, body) => { calls.push(body); return { data: body } } })
    vm.startHeaderEdit()
    vm.draftName = '放弃修改'
    vm.cancel()
    assert.equal(vm.craftOrder.name, '原名称')
    vm.startHeaderEdit()
    vm.draftName = ' 新名称 '
    await vm.saveHeader()
    assert.deepEqual(calls, [{ id: 9, name: '新名称' }])
    assert.equal(vm.craftOrder.description, '原说明')
    assert.equal(vm.craftOrder.productId, 7)
    assert.equal(vm.showFormData.find(field => field.label === '技术型号').value, 'T-9')
})

test('input/output buttons target separate areas; inbound or outsource permits output', async t => {
    const { vm, props } = mount(t, 'craft-info.vue', { process: process() })
    assert.equal(vm.insertResources([resource()], 'input'), 1)
    assert.equal(vm.insertResources([resource()], 'output'), 0)
    props.process.isOutsource = true
    await nextTick()
    assert.equal(vm.insertResources([resource()], 'output'), 1)
    props.process.isInbound = true
    await nextTick()
    assert.equal(vm.insertResources([resource()], 'output'), 0) // Existing outsource output is not duplicated.
    assert.equal(vm.inputMaterials.length, 1)
    assert.equal(vm.outputMaterials.length, 1)
    props.process.isInbound = false
    await nextTick()
    assert.equal(vm.showOutput, true)
    props.process.isOutsource = false
    await nextTick()
    assert.equal(vm.showOutput, false)
    assert.equal(vm.selection, null)
    assert.equal(vm.outputMaterials.length, 1) // The workspace normalizes output data before saving; see the cleanup test below.
})

test('empty tables accept additions; selected main controls insertion order and batches deduplicate', t => {
    const { vm } = mount(t, 'craft-info.vue', { process: process() })
    assert.equal(vm.insertResources([resource(10), resource(20), resource(20)], 'input'), 2)
    vm.selectMaterial({ row: vm.inputMaterials[0], main: vm.inputMaterials[0] })
    assert.equal(vm.insertResources([resource(30)], 'input'), 1)
    assert.deepEqual(vm.inputMaterials.map(row => row.dataId), [10, 30, 20])
    assert.deepEqual(vm.inputMaterials.map(row => row.sortNo), [1, 2, 3])
})

test('substitutes attach to the selected main, even when a substitute is selected', t => {
    const { vm } = mount(t, 'craft-info.vue', { process: process() })
    assert.equal(vm.insertResources([resource(20)], 'substitute'), 0)
    vm.insertResources([resource(10)], 'input')
    const main = vm.inputMaterials[0]
    main.id = 101
    assert.equal(vm.insertResources([resource(10), resource(20), resource(20)], 'substitute'), 1)
    const child = main.substitutes[0]
    assert.equal(child.insteadSourceId, 101)
    vm.selectMaterial({ row: child, main })
    assert.equal(vm.insertResources([resource(30)], 'substitute'), 1)
    assert.deepEqual(main.substitutes.map(row => row.dataId), [20, 30])
    assert.equal(child.substitutes.length, 0)
    vm.removeMaterial({ row: child, main })
    assert.equal(main.substitutes.length, 1)
    vm.removeMaterial({ row: main, main })
    assert.equal(vm.process.materials.length, 0)
})

test('current product is allowed for inbound but blocked for consumption', t => {
    const { vm } = mount(t, 'craft-info.vue', { process: process({ isInbound: true }), craftOrderInfo: { productId: 10 } })
    assert.equal(vm.insertResources([resource(10)], 'input'), 0)
    assert.equal(vm.insertResources([resource(10)], 'output'), 1)
})

test('readonly versions and stale selections cannot mutate materials', async t => {
    const { vm, props } = mount(t, 'craft-info.vue', { process: process(), readonly: true })
    assert.equal(vm.insertResources([resource()], 'input'), 0)
    props.readonly = false
    await nextTick()
    vm.insertResources([resource()], 'input')
    const oldMain = vm.inputMaterials[0]
    props.process = process()
    await nextTick()
    assert.equal(vm.selection, null)
    vm.selectMaterial({ row: oldMain, main: oldMain })
    assert.equal(vm.insertResources([resource(20)], 'substitute'), 0)
})

test('save-version payload retains the single-level nested substitute model', async t => {
    const calls = []
    const routes = [process({ materials: [{ ...resource(10), ioType: 1, substitutes: [{ ...resource(20), ioType: 1 }] }] })]
    const { vm } = mount(t, 'craft-order-header.vue', { mode: 'edit', activeVersionId: 5, routes, modelValue: { id: 9 } }, {
        post: async (url, body) => { calls.push({ url, body }); return { data: {} } }
    })
    vm.versionList = [{ id: 5, status: status.待审核 }]
    vm.loadedVersionId = 5
    await vm.saveVersion()
    const payload = calls.find(call => call.url.endsWith('SaveCraftVersion')).body
    assert.equal(payload.nodeSettings[0].materials[0].substitutes[0].dataId, 20)
    assert.equal(payload.nodeSettings[0].materials.length, 1)
})

test('a slow previous version cannot replace the currently loaded routes', async t => {
    const first = deferred(), second = deferred()
    const { vm, events } = mount(t, 'craft-order-header.vue', { mode: 'edit', activeVersionId: 2 }, {
        get: (url, query) => query.versionId === 1 ? first.promise : second.promise
    })
    vm.versionList = [{ id: 1, status: status.待审核 }, { id: 2, status: status.待审核 }]
    const loadingFirst = vm.loadRoutes(1)
    const loadingSecond = vm.loadRoutes(2)
    assert.equal(vm.canEditVersion, false)
    assert.equal(vm.routeLoading, true)
    assert.equal(vm.loading, true)
    second.resolve({ data: { nodeSettings: [process({ code: '20' })] } })
    await loadingSecond
    assert.equal(vm.canEditVersion, true)
    assert.equal(vm.routeLoading, false)
    assert.equal(vm.loading, false)
    first.resolve({ data: { nodeSettings: [process({ code: '10' })] } })
    await loadingFirst
    assert.equal(vm.loadedVersionId, 2)
    assert.equal(events.filter(event => event[0] === 'routes').at(-1)[1][0].code, '20')
})

test('drag placement inserts batches before/after rows and appends independently of selection', t => {
    const { vm } = mount(t, 'craft-info.vue', { process: process({ isInbound: true }) })
    vm.insertResources([resource(10), resource(40)], 'input')
    const first = vm.inputMaterials[0], last = vm.inputMaterials[1]
    assert.equal(vm.insertResources([resource(20), resource(30)], 'input', { row: last, position: 'before' }), 2)
    assert.deepEqual(vm.inputMaterials.map(row => row.dataId), [10, 20, 30, 40])
    vm.insertResources([resource(15)], 'input', { row: first, position: 'after' })
    vm.selectMaterial({ row: first, main: first })
    vm.insertResources([resource(50)], 'input', { position: 'append' })
    assert.deepEqual(vm.inputMaterials.map(row => row.dataId), [10, 15, 20, 30, 40, 50])
    vm.insertResources([resource(60)], 'output', { position: 'append' })
    assert.deepEqual(vm.outputMaterials.map(row => row.dataId), [60])
    assert.equal(vm.insertResources([resource(70)], 'output', { row: first, position: 'before' }), 0)
    assert.deepEqual(vm.process.materials.map(row => row.sortNo), [1, 2, 3, 4, 5, 6, 7])
})

test('drop onto a main attaches substitutes without selecting it, and child placement stays one level', t => {
    const { vm } = mount(t, 'craft-info.vue', { process: process() })
    vm.insertResources([resource(10), resource(90)], 'input')
    const main = vm.inputMaterials[0]
    main.id = 101
    vm.insertResources([resource(20), resource(40)], 'substitute', { main, position: 'append' })
    vm.insertResources([resource(30)], 'substitute', { main, row: main.substitutes[1], position: 'before' })
    assert.deepEqual(main.substitutes.map(row => row.dataId), [20, 30, 40])
    assert.ok(main.substitutes.every(row => row.insteadSourceId === 101 && row.substitutes.length === 0))
    assert.equal(vm.inputMaterials[1].substitutes.length, 0)
    vm.removeMaterial({ row: main, main })
    assert.equal(vm.insertResources([resource(50)], 'substitute', { main, position: 'append' }), 0)
})

function dragTable(props = {}) {
    const component = load(path.join(__dirname, '..', 'craft-material-table.vue')).default
    const vm = { rows: [resource(10), resource(20)], ioType: 1, readonly: false, ...component.data(), ...props }
    for (const [key, method] of Object.entries(component.methods)) vm[key] = method.bind(vm)
    return vm
}
function dragEvent(vm, fraction = 0.1, { blank = false, nested = false } = {}) {
    const zone = {}, inner = {}, body = { scrollTop: 50, closest: () => zone, getBoundingClientRect: () => ({ top: 0, bottom: 400 }) }
    const tr = { querySelector: () => ({ dataset: { materialIndex: '0' } }), getBoundingClientRect: () => ({ top: 100, height: 40 }) }
    const target = { closest: selector => ({ '.material-drop-zone': nested ? inner : zone, '.datagrid-body': body, '.substitute-detail': null, 'tr.datagrid-row': blank ? null : tr })[selector] }
    return { target, currentTarget: zone, clientY: 100 + 40 * fraction, prevented: false,
        preventDefault() { this.prevented = true }, dataTransfer: { types: ['application/x-craft-resources'], getData: () => JSON.stringify([resource(30)]) } }
}

test('grid row feedback matches before, substitute and after drop targets; unrelated/nested drags ignored', () => {
    const vm = dragTable()
    for (const [fraction, target, position, css] of [[0.1,'input','before','material-drop-before'], [0.5,'substitute','append','material-drop-substitute'], [0.9,'input','after','material-drop-after']]) {
        const event = dragEvent(vm, fraction)
        vm.onDragOver(event)
        assert.equal(event.prevented, true)
        assert.equal(vm.dropState.target, target)
        assert.equal(vm.dropState.position, position)
        assert.equal(vm.rowClass(vm.rows[0]), css)
        assert.equal(vm.rowClass(vm.rows[1]), '')
    }
    vm.onDragOver(dragEvent(vm, 0.5, { blank: true }))
    assert.equal(vm.dropState.position, 'append')
    assert.equal(vm.dropState.row, undefined)
    vm.onDragOver(dragEvent(vm, 0.5, { nested: true }))
    assert.equal(vm.dropState, null)
    const external = dragEvent(vm); external.dataTransfer.types = ['Files']
    vm.onDragOver(external)
    assert.equal(external.prevented, false)
    vm.readonly = true
    const readonly = dragEvent(vm)
    vm.onDragOver(readonly)
    assert.equal(readonly.prevented, false)
})

test('child drop positions preserve parent; valid payload emits once and invalid payloads do not emit', () => {
    const main = resource(1), vm = dragTable({ parentRow: main }), events = []
    vm.$emit = (...args) => events.push(args)
    vm.onDragOver(dragEvent(vm, 0.1))
    assert.equal(vm.dropState.main, main)
    assert.equal(vm.dropState.target, 'substitute')
    assert.equal(vm.rowClass(vm.rows[0]), 'material-drop-before')
    vm.onDrop(dragEvent(vm, 0.9))
    assert.equal(events.length, 1)
    assert.equal(events[0][1].main, main)
    assert.equal(events[0][1].position, 'after')
    assert.equal(vm.dropState, null)
    for (const payload of ['bad-json', '{}', '[null]', '[]']) {
        const event = dragEvent(vm); event.dataTransfer.getData = () => payload
        vm.onDrop(event)
    }
    assert.equal(events.length, 1)
})

test('resource drag serializes multiple selected rows and readonly/loading prevents drag', t => {
    const { vm, props } = mount(t, 'resource-library.vue')
    vm.dataList = [ { ...resource(10), code: 'A', name: '甲' }, { ...resource(20), code: 'B', name: '乙' } ]
    vm.onSelectionChange(vm.dataList)
    const data = {}, event = { prevented: false, preventDefault() { this.prevented = true }, dataTransfer: { setData: (key, value) => data[key] = value } }
    vm.startDrag(event, vm.dataList[0])
    assert.deepEqual(JSON.parse(data['application/x-craft-resources']).map(row => row.dataId), [10,20])
    assert.equal(event.dataTransfer.effectAllowed, 'copy')
    vm.isLoading = true
    vm.startDrag(event, vm.dataList[0])
    assert.equal(event.prevented, true)
})

test('machine library ignores stale craft requests and exposes only the current craft machines', async t => {
    const first = deferred(), second = deferred(), calls = []
    const { vm, props } = mount(t, 'resource-library.vue', { craftId: 1 }, { get: (url, query) => {
        calls.push({ url, query }); return query.craftId === 1 ? first.promise : second.promise
    } })
    props.craftId = 2
    await nextTick()
    second.resolve({ data: [{ id: 20, code: 'M20', name: '磨床' }] })
    await nextTick(); await nextTick()
    first.resolve({ data: [{ id: 10, code: 'M10', name: '车床' }] })
    await nextTick(); await nextTick()
    assert.deepEqual(vm.machineRows.map(row => row.id), [20])
    assert.equal(vm.machineRows[0].craftId, 2)
    assert.equal(vm.machineLoading, false)
    assert.ok(calls.every(call => call.url.endsWith('/CraftOrderManage/GetCraftMachineList')))
    vm.selectedTab = 'machine'
    vm.filterList = [{ key: 'name', value: '磨' }]
    assert.equal(vm.gridRows.length, 1)
    vm.filterList = [{ key: 'name', value: '车' }]
    assert.equal(vm.gridRows.length, 0)
    props.craftId = undefined
    await nextTick()
    assert.deepEqual(vm.machineRows, [])
})

function machineSelector(props = {}) {
    const component = load(path.join(__dirname, '..', 'craft-machine.vue')).default
    const vm = { process: process({ craftId: 1, machineIds: [] }), readonly: false, loading: false,
        machineOptions: [{ id: 10, craftId: 1, code: 'M10', name: '车床' }, { id: 20, craftId: 1, code: 'M20', name: '磨床' }],
        $emit() {}, $kingzon_messager: { showInfo() {} }, ...component.data(), ...props }
    if (!props.$emit) vm.$emit = (name, changes) => { if (name === 'updateProcess') Object.assign(vm.process, changes) }
    for (const [key, method] of Object.entries(component.methods)) vm[key] = method.bind(vm)
    for (const [key, getter] of Object.entries(component.computed)) Object.defineProperty(vm, key, { get: getter.bind(vm) })
    return vm
}

test('machines validate craft membership, deduplicate, preserve invalid existing IDs and support removal', () => {
    const vm = machineSelector()
    assert.equal(vm.insertMachines({ craftId: 1, machines: [{ id: 10 }, { id: 10 }, { id: 99 }, null] }), 1)
    assert.deepEqual(vm.process.machineIds, [10])
    assert.equal(vm.insertMachines({ craftId: 2, machines: [{ id: 20 }] }), 0)
    vm.process.craftId = 2
    assert.equal(vm.selectedMachines[0].available, false)
    assert.deepEqual(vm.process.machineIds, [10])
    vm.removeMachine(10)
    assert.deepEqual(vm.process.machineIds, [])
    vm.process.craftId = 1
    vm.readonly = true
    assert.equal(vm.insertMachines({ craftId: 1, machines: [{ id: 20 }] }), 0)
    vm.process.machineIds = [10]
    vm.removeMachine(10)
    assert.deepEqual(vm.process.machineIds, [10])
    vm.readonly = false; vm.loading = true
    assert.equal(vm.insertMachines({ craftId: 1, machines: [{ id: 20 }] }), 0)
})

test('machine library drag payload is separate from materials and machine drops preserve IDs', async t => {
    const { vm: library } = mount(t, 'resource-library.vue', { craftId: 1 }, { get: async () => ({ data: [
        { id: 10, name: '车床' }, { id: 20, name: '磨床' }
    ] }) })
    await nextTick(); await nextTick()
    library.selectedTab = 'machine'
    library.onSelectionChange(library.machineRows)
    const data = {}, dataTransfer = { types: ['application/x-craft-machines'], setData: (key, value) => data[key] = value, getData: key => data[key] }
    library.startDrag({ dataTransfer, preventDefault() {} }, library.machineRows[0])
    assert.equal(data['application/x-craft-resources'], undefined)
    assert.equal(JSON.parse(data['application/x-craft-machines']).craftId, 1)
    const vm = machineSelector(), event = { dataTransfer, preventDefault() {} }
    vm.onDragOver(event)
    assert.equal(vm.dragActive, true)
    vm.onDrop(event)
    assert.deepEqual(vm.process.machineIds, [10,20])
    assert.equal(vm.dragActive, false)
    dataTransfer.types = ['application/x-craft-resources']
    vm.onDragOver(event)
    assert.equal(vm.dragActive, false)
})

test('save payload includes machine selection with the current craft', async t => {
    const calls = [], routes = [process({ craftId: 1, machineIds: [10, 20] })]
    const { vm } = mount(t, 'craft-order-header.vue', { mode: 'edit', activeVersionId: 5, routes, modelValue: { id: 9 } }, {
        post: async (url, body) => { calls.push({url,body}); return { data: {} } }
    })
    vm.versionList = [{ id: 5, status: status.待审核 }]; vm.loadedVersionId = 5
    await vm.saveVersion()
    const node = calls.find(call => call.url.endsWith('SaveCraftVersion')).body.nodeSettings[0]
    assert.equal(node.craftId, 1)
    assert.deepEqual(node.machineIds, [10,20])
})

test('library row selection clears on filtering/tab switches and unselected row actions use only that row', async t => {
    const { vm } = mount(t, 'resource-library.vue')
    vm.dataList = [resource(10), resource(20), resource(30)]
    vm.onSelectionChange([vm.dataList[0], vm.dataList[1]])
    assert.deepEqual(vm.getSelectedRows(vm.dataList[0]).map(row => row.dataId), [10,20])
    assert.deepEqual(vm.getSelectedRows(vm.dataList[2]).map(row => row.dataId), [30])
    vm.search([{ table: 'default', filter: [] }])
    assert.equal(vm.librarySelection.length, 0)
    vm.onSelectionChange([vm.dataList[0]])
    vm.tabSelect({ $el: { dataset: { theme: 'machine' } } })
    assert.equal(vm.librarySelection.length, 0)
    assert.equal(vm.gridRows.length, 0)
})

test('material navigation selects the exact input/output main and rejects children or stale rows', async t => {
    const { vm } = mount(t, 'craft-info.vue', { process: process({ isInbound: true }) })
    vm.insertResources([resource(10)], 'input')
    vm.insertResources([resource(10)], 'output')
    const calls = []
    vm.$.refs = { inputTable: { focusMaterial: row => { calls.push(['input', row]); return true } }, outputTable: { expandMaterial() {}, focusMaterial: row => { calls.push(['output', row]); return true } } }
    const output = vm.outputMaterials[0]
    assert.equal(await vm.focusMaterial(output), true)
    assert.equal(vm.selection.row, output)
    assert.equal(calls[0][0], 'output')
    assert.equal(await vm.focusMaterial(resource(10)), false)
    vm.insertResources([resource(20)], 'substitute')
    assert.equal(await vm.focusMaterial(output.substitutes[0]), false)
    assert.equal(await vm.focusMaterial(vm.inputMaterials[0]), true)
    assert.equal(calls.at(-1)[0], 'input')
})

test('KingZonDataGrid adapter expands and focuses through its internal grid and scrolls the actual row', async () => {
    const vm = dragTable(), row = vm.rows[0], calls = []
    const body = { scrollTop: 0, getBoundingClientRect: () => ({ top: 0, bottom: 100 }) }
    const tr = { closest: () => body, getBoundingClientRect: () => ({ top: 180, bottom: 210 }) }
    const element = { dataset: { materialIndex: '0' }, closest: selector => selector === 'tr' ? tr : vm.$el }
    vm.$el = { querySelectorAll: () => [element] }
    vm.$nextTick = async () => {}
    vm.$refs = { grid: { dataGrid: { expandRow: value => calls.push(['expand',value]), selectRow: value => calls.push(['select',value]) } } }
    vm.expandMaterial(row)
    assert.equal(await vm.focusMaterial(row), true)
    assert.deepEqual(calls.map(call => call[0]), ['expand','select'])
    assert.equal(body.scrollTop, 110)
})

test('tree navigation waits for the correct process editor and rejects a stale version navigation', async t => {
    const { vm } = mount(t, '../create-craft.vue', { craftOrderId: 1 })
    vm.activeVersionId = 5
    vm.routeItems = [process({ code: '10', materials: [resource(10)] }), process({ code: '20', materials: [resource(20)] })]
    const calls = []
    vm.$.refs = { processEditor: { showTab() {}, focusMaterial: row => calls.push(row) } }
    const row = vm.routeItems[1].materials[0]
    await vm.onTreeProcessSelect({ code: '20', material: row })
    assert.equal(vm.selectedProcess.code, '20')
    assert.equal(calls[0], row)
    const pending = vm.onTreeProcessSelect({ code: '10', material: vm.routeItems[0].materials[0] })
    vm.activeVersionId = 6
    await pending
    assert.equal(calls.length, 1)
})

test('outsourcing enforces inbound on the immediate predecessor, reordering and deletion recompute the requirement', async t => {
    const { vm } = mount(t, '../create-craft.vue', { craftOrderId: 1 })
    vm.canEditCurrentVersion = true
    vm.routeItems = [process({code:'10'}), process({code:'20'}), process({code:'30'})]
    vm.selectedProcessKey = vm.routeItems[0].uiKey
    vm.routeItems[1].isOutsource = true
    await nextTick()
    assert.equal(vm.routeItems[0].isInbound, true)
    assert.equal(vm.selectedInboundRequired, true)
    vm.routeItems[0].isInbound = false
    await nextTick()
    assert.equal(vm.routeItems[0].isInbound, true)
    const external = vm.routeItems[1], last = vm.routeItems[2]
    vm.routeItems = [vm.routeItems[0], last, external]
    vm.selectedProcessKey = vm.routeItems[1].uiKey
    await nextTick()
    assert.equal(last.isInbound, true)
    assert.equal(vm.selectedInboundRequired, true)
    external.isOutsource = false
    await nextTick()
    assert.equal(vm.selectedInboundRequired, false)
    assert.equal(last.isInbound, true) // Never undo a user's inbound choice automatically.
    external.isOutsource = true
    vm.routeItems = [vm.routeItems[0], external]
    await nextTick()
    assert.equal(vm.routeItems[0].isInbound, true)
})

test('first/consecutive external processes and read-only histories obey the boundary rules', async t => {
    const { vm } = mount(t, '../create-craft.vue', { craftOrderId: 1 })
    vm.routeItems = [process({code:'10',isOutsource:true}), process({code:'20',isOutsource:true}), process({code:'30',isOutsource:true})]
    await nextTick()
    assert.ok(vm.routeItems.every(row=>!row.isInbound))
    vm.canEditCurrentVersion = true
    await nextTick()
    assert.deepEqual(vm.routeItems.map(row=>row.isInbound), [true,true,false])
    vm.routeItems = [process({code:'10',isOutsource:true})]
    await nextTick()
    assert.equal(vm.routeItems[0].isInbound, false)
})

test('save normalizes inbound and sequence even if an immediate save precedes the UI watcher', async t => {
    const calls = [], routes = [process({code:'10',sequence:90}),process({code:'20',sequence:10,isOutsource:true})]
    const { vm } = mount(t, 'craft-order-header.vue', {mode:'edit',activeVersionId:5,routes,modelValue:{id:9}}, {
        post:async(url,body)=>{calls.push({url,body});return {data:{}}}
    })
    vm.versionList=[{id:5,status:status.待审核}];vm.loadedVersionId=5
    await vm.saveVersion()
    const nodes=calls.find(call=>call.url.endsWith('SaveCraftVersion')).body.nodeSettings
    assert.equal(nodes[0].isInbound,true)
    assert.deepEqual(nodes.map(row=>row.sequence),[10,20])
})

test('version operations send object request bodies accepted by the typed http client', async t => {
    const calls=[]
    const {vm}=mount(t,'craft-order-header.vue',{mode:'edit',activeVersionId:5,modelValue:{id:9}}, {
        post:async(url,body)=>{calls.push({url,body});return {data:{}}}
    })
    vm.versionList=[{id:5,status:status.已发布},{id:6,status:status.待审核}]
    vm.init=async()=>{}
    await vm.obsoleteVersion()
    await vm.unpublishVersion()
    await vm.delVersion(vm.versionList[1])
    assert.deepEqual(calls.map(call=>call.body),[{id:5},{id:5},{id:6}])
})


test('editing a code keeps the selected process identity even while empty or duplicated', async t => {
    const { vm } = mount(t, '../create-craft.vue', { craftOrderId: 1 })
    vm.routeItems = [process(), process({code:'20'})]
    vm.selectedProcessKey = vm.routeItems[1].uiKey
    const selected = vm.selectedProcess, key = selected.uiKey
    for (const code of ['', '10', 'P', 'P20']) {
        selected.code = code
        await nextTick()
        assert.equal(vm.selectedProcess, selected)
        assert.equal(vm.selectedProcessKey, key)
    }
    vm.routeItems.reverse()
    await nextTick()
    assert.equal(vm.selectedProcess, selected)
})

test('process and material group navigation select their own tabs on the same process', async t => {
    const { vm } = mount(t, '../create-craft.vue', { craftOrderId: 1 })
    vm.routeItems = [process()]
    const tabs = []
    vm.$.refs = { processEditor: { showTab: index => tabs.push(index) } }
    const key = vm.routeItems[0].uiKey
    await vm.onTreeProcessSelect({key, tab:1})
    await vm.onTreeProcessSelect({key, tab:0})
    assert.deepEqual(tabs,[1,0])
})

test('unchecking inbound removes output mains and nested/legacy substitutes; required inbound preserves them', async t => {
    const { vm } = mount(t, '../create-craft.vue', { craftOrderId: 1 })
    vm.canEditCurrentVersion = true
    vm.routeItems = [process({isInbound:true,materials:[
        {...resource(1),ioType:1}, {...resource(2),id:22,ioType:2,substitutes:[resource(3)]},
        {...resource(4),ioType:1,insteadSourceId:22}
    ]})]
    await nextTick()
    vm.routeItems[0].isInbound = false
    await nextTick()
    assert.deepEqual(vm.routeItems[0].materials.map(row=>row.dataId),[1])
    vm.routeItems[0].materials.push({...resource(5),ioType:2})
    vm.routeItems.push(process({isOutsource:true}))
    await nextTick()
    assert.equal(vm.routeItems[0].isInbound,true)
    assert.equal(vm.routeItems[0].materials.length,2)
    vm.canEditCurrentVersion = false
    await nextTick()
    vm.routeItems[0].isInbound = false
    await nextTick()
    assert.equal(vm.routeItems[0].materials.length,2)
})

test('library selections clear only after accepted additions, including successful drops', async t => {
    const { vm } = mount(t, '../create-craft.vue', { craftOrderId: 1 })
    vm.canEditCurrentVersion = true
    vm.routeItems = [process()]
    let clear = 0, count = 0
    vm.$.refs = {resourceLibrary:{clearSelection(){clear++}}, processEditor:{addResources:async()=>count}}
    await vm.addResources({resources:[resource()],target:'input'})
    assert.equal(clear,0)
    count = 1
    await vm.addResources({resources:[resource()],target:'input'})
    assert.equal(clear,1)
    const {vm:info,events:dropEvents}=mount(t,'craft-info.vue',{process:process()})
    info.dropResources({resources:[resource()],target:'input',position:'append'})
    assert.equal(dropEvents.filter(event=>event[0]==='resourcesAdded').length,1)
    info.dropResources({resources:[resource()],target:'input',position:'append'})
    assert.equal(dropEvents.filter(event=>event[0]==='resourcesAdded').length,1)
})

test('machine hours follow machine identity and readonly/removal retain valid configuration', () => {
    const vm=machineSelector()
    vm.insertMachines({craftId:1,machines:[{id:10},{id:20}]})
    vm.setWorkHours(10,1.25)
    vm.setWorkHours(20,2.5)
    vm.process.machineIds.reverse()
    assert.deepEqual(vm.selectedMachines.map(row=>[row.id,row.theoreticalWorkHours]),[[20,2.5],[10,1.25]])
    vm.readonly=true
    vm.setWorkHours(10,8)
    assert.equal(vm.selectedMachines[1].theoreticalWorkHours,1.25)
    vm.readonly=false
    vm.removeMachine(10)
    assert.deepEqual(vm.process.machineSettings,[{machineId:20,theoreticalWorkHours:2.5}])
})

test('save carries process hours, reporting and machine hours; invalid codes/hours block submission', async t => {
    const calls=[], routes=[process({code:' A ',theoreticalWorkHours:1.25,isReportWork:true,
        machineIds:[10],machineSettings:[{machineId:10,theoreticalWorkHours:2.5}]})]
    const {vm}=mount(t,'craft-order-header.vue',{mode:'edit',activeVersionId:5,routes,modelValue:{id:9}}, {
        post:async(url,body)=>{calls.push(body);return {data:{}}}
    })
    vm.versionList=[{id:5,status:status.待审核}];vm.loadedVersionId=5;vm.init=async()=>{}
    await vm.saveVersion()
    const saved=calls[0].nodeSettings[0]
    assert.equal(saved.code,'A');assert.equal(saved.uiKey,undefined)
    assert.equal(saved.theoreticalWorkHours,1.25);assert.equal(saved.isReportWork,true)
    assert.deepEqual(saved.machineSettings,[{machineId:10,theoreticalWorkHours:2.5}])
    vm.routes[0].code=''
    await vm.saveVersion()
    vm.routes[0].code='A';vm.routes.push(process({code:'A'}))
    await vm.saveVersion()
    vm.routes.pop();vm.routes[0].theoreticalWorkHours=-1
    await vm.saveVersion()
    assert.equal(calls.length,1)
})


test('library loading belongs to the newest request and ends on success or failure', async t => {
    const first=deferred(),second=deferred(),third=deferred(),queue=[first,second,third]
    const {vm,errors}=mount(t,'resource-library.vue',{}, {post:()=>queue.shift().promise})
    vm.$.refs={dataGrid:{}}
    const old=vm.refreshTable(),current=vm.refreshTable()
    assert.equal(vm.isLoading,true)
    first.resolve({data:[{dataId:1}]});await old
    assert.equal(vm.isLoading,true);assert.equal(vm.dataList.length,0)
    second.resolve({data:[{dataId:2}]});await current
    assert.equal(vm.isLoading,false);assert.equal(vm.dataList[0].dataId,2)
    const failed=vm.refreshTable();third.reject(new Error('network'));await failed
    assert.equal(vm.isLoading,false);assert.equal(errors.length,1)
})

test('craft order list loading belongs to the newest request and deletion waits for refresh', async () => {
    const component=load(path.join(__dirname,'../../index.vue')).default
    const first=deferred(),second=deferred(),errors=[]
    const vm={...component.data(),$refs:{dataGrid:{}},$http:{post:()=>first.promise},
        $kingzon_messager:{showError:error=>errors.push(error)}}
    vm.getData=component.methods.getData.bind(vm)
    const old=vm.getData()
    vm.$http.post=()=>second.promise
    const current=vm.getData()
    first.resolve({data:[{id:1}]});await old
    assert.equal(vm.isLoading,true);assert.equal(vm.craftOrderList.length,0)
    second.resolve({data:[{id:2}]});await current
    assert.equal(vm.isLoading,false);assert.equal(vm.craftOrderList[0].id,2)

    const refresh=deferred()
    vm.getData=()=>refresh.promise
    vm.$http.post=async()=>({data:{}})
    vm.$kingzon_messager={showConfim:()=>Promise.resolve(),showInfo(){},showError:error=>errors.push(error)}
    vm.deleteCraftOrder=component.methods.deleteCraftOrder.bind(vm)
    const deleting=vm.deleteCraftOrder({id:2,name:'工艺单'})
    await Promise.resolve();await Promise.resolve()
    assert.equal(vm.deleting,true)
    refresh.resolve()
    await deleting
    assert.equal(vm.deleting,false)
    assert.equal(errors.length,0)
})

test('library detail tooltip and advanced filters expose specification, grade and version', t => {
    const {vm}=mount(t,'resource-library.vue')
    vm.filterList=[{key:'specification',value:'规格'},{key:'version',value:'V2'}]
    assert.deepEqual(vm.getFilters(),vm.filterList)
    const detail=vm.resourceTitle({dataId:1,name:'产品',techCode:'T1',specification:'S1',materialGrade:'G1',version:'V2',remark:'备注内容'})
    for(const value of ['T1','S1','G1','V2','备注内容']) assert.ok(detail.includes(value))
})


function freezeData(value) {
    if (value && typeof value === 'object' && !Object.isFrozen(value)) {
        Object.values(value).forEach(freezeData)
        Object.freeze(value)
    }
    return value
}

test('material editor emits patches without mutating frozen process, mains or substitutes', async t => {
    const main = {...resource(1),ioType:1,substitutes:[{...resource(2),ioType:1}]}
    const source = freezeData(process({materials:[main]}))
    const {vm,events}=mount(t,'craft-info.vue',{process:source},{},false)
    assert.equal(vm.insertResources([resource(3)],'input'),1)
    assert.equal(source.materials.length,1)
    vm.selectMaterial({row:main,main})
    assert.equal(vm.insertResources([resource(4)],'substitute'),1)
    assert.equal(main.substitutes.length,1)
    vm.removeMaterial({row:main.substitutes[0],main})
    vm.removeMaterial({row:main,main})
    assert.equal(source.materials.length,1)
    assert.equal(main.substitutes.length,1)
    assert.equal(events.filter(event=>event[0]==='updateProcess').length,2)
    assert.equal(events.filter(event=>event[0]==='updateMaterial').length,2)
})

test('machine editor emits replacement lists and never mutates frozen machine settings', () => {
    const source=freezeData(process({craftId:1,machineIds:[10],machineSettings:[{machineId:10,theoreticalWorkHours:1}]}))
    const events=[], vm=machineSelector({process:source,$emit:(name,changes)=>events.push([name,changes])})
    vm.setWorkHours(10,2.5)
    vm.insertMachines({craftId:1,machines:[{id:20}]})
    vm.removeMachine(10)
    assert.deepEqual(source.machineIds,[10])
    assert.equal(source.machineSettings[0].theoreticalWorkHours,1)
    assert.equal(events.length,3)
    assert.deepEqual(events[0][1].machineSettings,[{machineId:10,theoreticalWorkHours:2.5}])
    assert.deepEqual(events[1][1].machineIds,[10,20])
    assert.deepEqual(events[2][1],{machineIds:[],machineSettings:[]})
})

test('process form emits fields, protects required inbound and honors readonly props', async t => {
    const source=freezeData(process({name:'原名称',craftId:1,isInbound:true}))
    const {vm,props,events}=mount(t,'craft-process-info.vue',{process:source,inboundRequired:true},{},false)
    vm.updateProcess({code:'20'})
    vm.onCraftChange({id:2,name:'新工艺'})
    vm.updateProcess({isInbound:false})
    assert.equal(source.code,'10');assert.equal(source.name,'原名称');assert.equal(source.craftId,1)
    assert.deepEqual(events.map(event=>event[1]),[{code:'20'},{craftId:2,craftName:'新工艺',name:'新工艺'}])
    props.readonly=true
    await nextTick()
    vm.updateProcess({name:'不可改'})
    assert.equal(events.length,2)
})

test('material table cell changes emit updates for known rows only', () => {
    const row=freezeData({...resource(1),quantity:1}),events=[]
    const method=load(path.join(__dirname,'../craft-material-table.vue')).default.methods.updateMaterial
    const vm={readonly:false,rows:[row],$emit:(name,change)=>events.push([name,change])}
    method.call(vm,row,{quantity:2})
    assert.equal(row.quantity,1)
    assert.equal(events[0][1].row,row)
    assert.deepEqual(events[0][1].changes,{quantity:2})
    method.call(vm,resource(2),{quantity:3})
    vm.readonly=true;method.call(vm,row,{quantity:4})
    assert.equal(events.length,1)
})

test('process files distinguish instructions from other files and refresh only the uploaded group', async t => {
    const { vm, messages } = mount(t, 'craft-file.vue', { process: process({ id: 88 }), readonly: false })
    let opened = 0, closed = 0
    vm.$.refs = { fileUploadDialog: { open: () => opened++, close: () => closed++ } }
    assert.deepEqual(vm.fileGroups.map(group => [group.title, group.sourceType, group.fileBase]), [
        ['工序作业指导书', 910, 91], ['其他工序文件', 900, 90]
    ])
    assert.equal(vm.uploadSourceType, 900)
    const files = [{ name: '其他工序附件.pdf', url: 'pending' }]
    await vm.onFileSelect(files)
    assert.equal(vm.sourceId, 88)
    assert.equal(vm.uploadSourceId, 88, 'the upload captures the saved process ID')
    assert.equal(vm.uploadGroup.sourceType, 900)
    assert.equal(vm.uploadGroup.fileBase, 90)
    assert.deepEqual(vm.uploadFileList, files)
    assert.equal(opened, 1)
    const firstKeys = vm.fileGroups.map(group => group.listKey)
    vm.uploadComplete()
    assert.equal(closed, 1)
    assert.equal(vm.uploadFileList.length, 0)
    assert.equal(vm.uploadSourceId, 0)
    assert.deepEqual(vm.fileGroups.map(group => group.listKey), [firstKeys[0], firstKeys[1] + 1])
    await vm.onFileSelect([{ name: '作业指导书.pdf', url: 'pending' }], 910)
    assert.equal(vm.uploadSourceType, 910)
    assert.equal(vm.uploadGroup.fileBase, 91)
    assert.equal(vm.uploadSourceId, 88)
    const secondKeys = vm.fileGroups.map(group => group.listKey)
    vm.uploadComplete()
    assert.equal(opened, 2)
    assert.equal(closed, 2)
    assert.deepEqual(vm.fileGroups.map(group => group.listKey), [secondKeys[0] + 1, secondKeys[1]])
    assert.deepEqual(messages, ['其他工序文件上传成功', '工序作业指导书上传成功'])
})

test('process files reject readonly or unsaved uploads and clear pending files when the process changes', async t => {
    const { vm, props } = mount(t, 'craft-file.vue', { process: process({ id: 0 }), readonly: false })
    let opened = 0, closed = 0
    vm.$.refs = { fileUploadDialog: { open: () => opened++, close: () => closed++ } }
    const files = [{ name: '作业指导书.pdf', url: 'pending' }]
    await vm.onFileSelect(files, 910)
    assert.equal(opened, 0)
    assert.equal(vm.uploadSourceId, 0)
    assert.equal(vm.uploadFileList.length, 0)
    props.process = process({ id: 88 })
    props.readonly = true
    await nextTick()
    await vm.onFileSelect(files, 910)
    await vm.onFileSelect(files, 900)
    assert.equal(opened, 0)
    props.readonly = false
    await nextTick()
    await vm.onFileSelect([], 910)
    assert.equal(opened, 0)
    await vm.onFileSelect(files, 910)
    assert.equal(opened, 1)
    assert.equal(vm.uploadSourceId, 88)
    const previousKeys = vm.fileGroups.map(group => group.listKey)
    const previousClosed = closed
    props.process = process({ id: 99 })
    await nextTick()
    assert.equal(closed, previousClosed + 1)
    assert.equal(vm.sourceId, 99)
    assert.equal(vm.uploadSourceId, 0)
    assert.equal(vm.uploadFileList.length, 0)
    assert.deepEqual(vm.fileGroups.map(group => group.listKey), previousKeys.map(key => key + 1))
    vm.uploadComplete()
    assert.deepEqual(vm.fileGroups.map(group => group.listKey), previousKeys.map(key => key + 1), 'late completion from the previous process is ignored')
    await vm.onFileSelect(files)
    assert.equal(vm.uploadSourceId, 99)
    const cancelKeys = vm.fileGroups.map(group => group.listKey)
    vm.closeUpload()
    assert.equal(vm.uploadSourceId, 0)
    assert.equal(vm.uploadFileList.length, 0)
    assert.deepEqual(vm.fileGroups.map(group => group.listKey), cancelKeys, 'cancel does not refresh either list')
})

test('workspace applies changes without replacing process or selected material identities', async t => {
    const {vm}=mount(t,'../create-craft.vue',{craftOrderId:1})
    vm.routeItems=[process({materials:[{...resource(1),substitutes:[]}]})]
    vm.selectedProcessKey=vm.routeItems[0].uiKey;vm.canEditCurrentVersion=true
    await nextTick()
    const selected=vm.selectedProcess,main=selected.materials[0]
    vm.selectedMaterial={row:main,main}
    vm.updateProcess({code:'20',isReportWork:true,theoreticalWorkHours:1.5})
    vm.updateMaterial({row:main,changes:{quantity:2,substitutes:[resource(3)]}})
    await nextTick()
    assert.equal(vm.selectedProcess,selected);assert.equal(vm.selectedMaterial.row,main)
    assert.equal(main.quantity,2);assert.equal(main.substitutes[0].sortNo,1)
    const stale=resource(9);vm.updateMaterial({row:stale,changes:{quantity:9}})
    assert.equal(stale.quantity,undefined)
    vm.canEditCurrentVersion=false
    vm.updateProcess({code:'30'});vm.updateMaterial({row:main,changes:{quantity:5}})
    assert.equal(selected.code,'20');assert.equal(main.quantity,2)
})

test('route reorder and save do not mutate frozen route props', async t => {
    const routes=freezeData([process({sequence:10}),process({code:'20',sequence:20,isOutsource:true})]),calls=[]
    const {vm,events}=mount(t,'craft-order-header.vue',{mode:'edit',routes,selectedProcessKey:routes[1].uiKey,activeVersionId:5,modelValue:{id:9}}, {
        post:async(url,body)=>{calls.push(body);return {data:{}}}
    }, false)
    vm.versionList=[{id:5,status:status.待审核}];vm.loadedVersionId=5;vm.init=async()=>{}
    vm.moveProcess(-1)
    const updated=events.filter(event=>event[0]==='routes').at(-1)[1]
    assert.equal(updated[0].uiKey,routes[1].uiKey);assert.equal(updated[0].sequence,10)
    assert.equal(routes[1].sequence,20)
    await vm.saveVersion()
    assert.equal(calls[0].nodeSettings[0].isInbound,false)
    assert.ok(events.some(event=>event[0]==='beforeSave'))
    assert.equal(routes[0].isInbound,false)
})


test('resource library loads after its conditional grid mounts on first tab activation', async t => {
    for (const initial of [0, 1]) {
        const original = load(path.join(__dirname, '..', 'resource-library.vue')).default
        const Grid = { render: () => null }
        const component = { ...original, render() {
            return this.selectTabIndex === 1 || this.selectTabIndex === 2 ? h(Grid, { ref: 'dataGrid' }) : null
        } }
        const props = reactive({ selectTabIndex: initial }), instance = ref(), calls = []
        const app = renderer.createApp({ render: () => h(component, { ...props, ref: instance }) })
        app.config.globalProperties.$http = { post: async (url, filters, grid) => {
            assert.ok(grid, 'grid must exist when loading the newly visible panel')
            calls.push({ url, filters }); return { data: [{ dataId: 10, code: 'A' }] }
        } }
        app.mount({}); t.after(() => app.unmount())
        if (initial === 0) {
            await nextTick(); assert.equal(calls.length, 0)
            props.selectTabIndex = 1
        }
        await nextTick(); await nextTick(); await nextTick()
        assert.equal(calls.length, 1, 'first visible material tab loads without another click')
        assert.equal(instance.value.dataList[0].dataId, 10)
        props.selectTabIndex = 2
        await nextTick(); await nextTick()
        assert.equal(instance.value.selectedTab, 'machine')
        assert.equal(calls.length, 1, 'machine panel uses its existing machine loading path')
        props.selectTabIndex = 1
        await nextTick(); await nextTick(); await nextTick()
        assert.equal(calls.length, 2, 'returning to materials reloads after grid pagination updates')
        await instance.value.tabSelect({ $el: { dataset: { theme: 'product' } } })
        assert.ok(calls[2].url.includes('productType=2'))
    }
})

test('resource tab changes cancel pending loads and coalesce selections before the render completes', async t => {
    const pending = deferred(), calls = []
    const { vm, props } = mount(t, 'resource-library.vue', { selectTabIndex: 1 }, { post: (url) => {
        calls.push(url); return pending.promise
    } })
    vm.$.refs = { dataGrid: {} }
    const first = vm.tabSelect({ $el: { dataset: { theme: 'product' } } })
    const latest = vm.tabSelect({ $el: { dataset: { theme: 'semiProduct' } } })
    await nextTick()
    assert.equal(calls.length, 1)
    assert.ok(calls[0].includes('productType=1'))
    props.selectTabIndex = 2
    await nextTick(); await nextTick()
    pending.resolve({ data: [{ dataId: 99 }] })
    await first; await latest
    assert.equal(vm.selectedTab, 'machine')
    assert.equal(vm.dataList.length, 0)
    assert.equal(vm.isLoading, false)
    await vm.tabSelect({ $el: { dataset: { theme: 'material' } } })
    assert.equal(vm.selectedTab, 'machine', 'stale event from removed material tabs is ignored')
})


test('outsource retains output material until both inbound and outsource are disabled', async t => {
    const {vm}=mount(t,'../create-craft.vue',{craftOrderId:1})
    vm.canEditCurrentVersion=true
    vm.routeItems=[process({isOutsource:true,isInbound:false,materials:[
        {...resource(1),ioType:1},{...resource(2),id:22,ioType:2,substitutes:[resource(3)]},
        {...resource(4),ioType:1,insteadSourceId:22}
    ]})]
    await nextTick()
    assert.equal(vm.routeItems[0].materials.length,3)
    assert.equal(vm.routeItems[0].materials[1].substitutes.length,1)
    vm.routeItems[0].isOutsource=false
    await nextTick()
    assert.deepEqual(vm.routeItems[0].materials.map(row=>row.dataId),[1])
})

test('saving outsource without inbound retains output material in payload', async t => {
    const routes=[process({code:'10',craftId:1,name:'外协',isOutsource:true,isInbound:false,materials:[{...resource(2),ioType:2}]})],calls=[]
    const {vm}=mount(t,'craft-order-header.vue',{mode:'edit',routes,activeVersionId:5,modelValue:{id:9}}, {
        post:async(url,body)=>{calls.push(body);return {data:{}}}
    },false)
    vm.versionList=[{id:5,status:status.待审核}];vm.loadedVersionId=5;vm.init=async()=>{}
    await vm.saveVersion()
    assert.equal(calls.length,1)
    assert.equal(calls[0].nodeSettings[0].isInbound,false)
    assert.equal(calls[0].nodeSettings[0].materials[0].ioType,2)
})

test('copy version opens on the current version and selects the main version of another source', async t => {
    const calls = [], opened = []
    const { vm } = mount(t, 'copy-craft-version.vue', {}, {
        get: async (url, query) => {
            calls.push({ url, query })
            return { data: [
                { id: query.craftOrderId * 10 + 1, craftOrderId: query.craftOrderId, version: 'V1', status: status.暂存 },
                { id: query.craftOrderId * 10 + 2, craftOrderId: query.craftOrderId, version: 'V2', isMain: true, status: status.已发布 },
                { id: 999, craftOrderId: 999, version: 'other', isMain: true }
            ] }
        },
        post: async () => ({ data: [{ id: 9, code: 'CO-9', name: '当前工艺单' }] })
    })
    vm.$.refs = { dialog: { open: () => opened.push(true), close() {} }, dataGrid: {} }
    const order = { id: 9, code: 'CO-9', name: '当前工艺单' }
    await vm.open(order, 91)
    assert.equal(opened.length, 1)
    assert.equal(vm.opened, true)
    assert.equal(vm.form.craftOrderId, 9)
    assert.equal(vm.form.sourceCraftOrderId, 9)
    assert.equal(vm.form.sourceVersionId, 91, 'the active version takes precedence over the main version')
    assert.equal(vm.versions.length, 2, 'only versions owned by the selected source are accepted')
    assert.notEqual(vm.targetOrder, order, 'target header is a detached DTO')
    await vm.selectOrder({ id: 10, code: 'CO-10', name: '来源工艺单' })
    assert.equal(vm.form.craftOrderId, 9, 'changing the source must not change the copy target')
    assert.equal(vm.form.sourceVersionId, 102)
    assert.match(vm.versionOptions.find(item => item.value === 102).text, /主版本/)
    assert.deepEqual(calls.map(call => call.query.craftOrderId), [9, 10])
    assert.ok(calls.every(call => call.url.endsWith('/GetCraftVersionList')))
})

test('copy version source changes ignore stale version responses and errors', async t => {
    const first = deferred(), latest = deferred(), failed = deferred()
    const { vm, errors } = mount(t, 'copy-craft-version.vue', {}, {
        get: (url, query) => ({ 1: first, 2: latest, 3: failed })[query.craftOrderId].promise
    })
    vm.opened = true
    vm.form.craftOrderId = 9
    const selectingFirst = vm.selectOrder({ id: 1 })
    const selectingFailed = vm.selectOrder({ id: 3 })
    const selectingLatest = vm.selectOrder({ id: 2 })
    assert.equal(vm.form.sourceVersionId, 0)
    assert.equal(vm.versionLoading, true)
    latest.resolve({ data: [{ id: 22, craftOrderId: 2, isMain: true }] })
    await selectingLatest
    first.resolve({ data: [{ id: 11, craftOrderId: 1, isMain: true }] })
    failed.reject(new Error('stale source failed'))
    await selectingFirst
    await selectingFailed
    assert.equal(vm.form.sourceCraftOrderId, 2)
    assert.equal(vm.form.sourceVersionId, 22)
    assert.equal(vm.versions[0].id, 22)
    assert.equal(vm.versionLoading, false)
    assert.deepEqual(errors, [])
})

test('copy version close and reopen invalidate pending source and order requests', async t => {
    const oldOrders = deferred(), oldVersions = deferred()
    let posts = 0
    const { vm, events } = mount(t, 'copy-craft-version.vue', {}, {
        post: () => ++posts === 1 ? oldOrders.promise : Promise.resolve({ data: [{ id: 2 }] }),
        get: (url, query) => query.craftOrderId === 1 ? oldVersions.promise
            : Promise.resolve({ data: [{ id: 22, craftOrderId: 2, isMain: true }] })
    })
    vm.$.refs = { dialog: { open() {}, close() {} }, dataGrid: {} }
    const initialOpen = vm.open({ id: 1 })
    await nextTick()
    assert.equal(vm.versionLoading, true)
    vm.close()
    assert.equal(vm.opened, false)
    assert.equal(vm.versions.length, 0)
    await vm.confirm()
    assert.equal(posts, 1, 'closed dialogs cannot submit')
    await vm.open({ id: 2 })
    oldOrders.resolve({ data: [{ id: 1 }] })
    oldVersions.resolve({ data: [{ id: 11, craftOrderId: 1, isMain: true }] })
    await initialOpen
    assert.equal(vm.targetOrder.id, 2)
    assert.equal(vm.form.craftOrderId, 2)
    assert.equal(vm.form.sourceVersionId, 22)
    assert.equal(vm.orders[0].id, 2)
    assert.deepEqual(events, [])
})

test('copy version search uses the grid pagination contract and ignores older list results', async t => {
    const pending = deferred(), calls = []
    const { vm } = mount(t, 'copy-craft-version.vue', {}, { post: async (url, body, grid) => {
        calls.push({ url, body: JSON.parse(JSON.stringify(body)), grid })
        return calls.length === 1 ? pending.promise : { data: [{ id: 2 }] }
    } })
    vm.opened = true
    vm.$.refs = { dataGrid: { pageIndex: 4 } }
    const grid = vm.$refs.dataGrid
    const previous = vm.getOrders()
    const filter = [{ field: 'code', value: 'CO-2' }]
    vm.search([{ table: 'craftOrder', filter }])
    await nextTick()
    pending.resolve({ data: [{ id: 1 }] })
    await previous
    assert.equal(grid.pageIndex, 1)
    assert.equal(calls[1].grid, grid)
    assert.deepEqual(calls[1].body, { craftOrderFilterList: filter, productFilterList: [] })
    assert.ok(calls.every(call => call.url.endsWith('/GetCraftOrderList')))
    assert.equal(vm.orders[0].id, 2)
})

test('copy version submits only source and target IDs once, preserves failure state and emits the created version', async t => {
    const failure = deferred(), success = deferred(), calls = []
    let closed = 0
    const { vm, events, errors } = mount(t, 'copy-craft-version.vue', {}, { post: async (url, body) => {
        calls.push({ url, body })
        return calls.length === 1 ? failure.promise : success.promise
    } })
    vm.$.refs = { dialog: { close: () => closed++ } }
    vm.opened = true
    vm.form.craftOrderId = 9
    vm.form.sourceCraftOrderId = 2
    vm.form.sourceVersionId = 21
    vm.sourceOrder = { id: 2 }
    vm.versions = [{ id: 21, craftOrderId: 2 }]
    const first = vm.confirm()
    await vm.confirm()
    vm.close()
    await vm.selectOrder({ id: 3 })
    assert.equal(calls.length, 1)
    assert.equal(vm.opened, true)
    assert.equal(vm.form.sourceCraftOrderId, 2)
    assert.equal(vm.saving, true)
    assert.deepEqual(JSON.parse(JSON.stringify(calls[0].body)), {
        craftOrderId: 9, sourceCraftOrderId: 2, sourceVersionId: 21
    })
    assert.notEqual(calls[0].body, vm.form, 'the in-flight request has its own DTO')
    failure.reject(new Error('copy failed'))
    await first
    assert.equal(vm.saving, false)
    assert.equal(vm.opened, true)
    assert.equal(vm.form.sourceVersionId, 21)
    assert.equal(vm.versions.length, 1)
    assert.match(errors[0].message, /copy failed/)
    assert.equal(closed, 0)
    const second = vm.confirm()
    success.resolve({ data: { id: 92, craftOrderId: 9 } })
    await second
    assert.ok(calls.every(call => call.url.endsWith('/CopyCraftVersion')))
    assert.deepEqual(events, [['copied', { craftOrderId: 9, versionId: 92 }]])
    assert.equal(vm.opened, false)
    assert.equal(closed, 1)
})

test('copy version rejects mismatched source versions and does not close on an invalid response', async t => {
    const calls = []
    const { vm, errors, events } = mount(t, 'copy-craft-version.vue', {}, {
        post: async (url, body) => { calls.push(body); return { data: {} } }
    })
    vm.opened = true
    vm.form.craftOrderId = 9
    vm.form.sourceCraftOrderId = 2
    vm.form.sourceVersionId = 11
    vm.versions = [{ id: 11, craftOrderId: 1 }]
    await vm.confirm()
    assert.equal(calls.length, 0)
    assert.match(vm.error, /请选择来源工艺单及版本/)
    vm.versions = [{ id: 11, craftOrderId: 2 }]
    await vm.confirm()
    assert.equal(vm.opened, true)
    assert.equal(vm.saving, false)
    assert.match(errors[0].message, /复制结果未返回版本编号/)
    assert.deepEqual(events, [])
})

test('copy version header blocks unsaved changes and opens a clean draft with the active version', async t => {
    const opened = []
    const { vm, props, messages } = mount(t, 'craft-order-header.vue', {
        mode: 'edit', activeVersionId: 5, modelValue: { id: 9, code: 'CO-9', name: '工艺单' },
        routes: [process({ name: '切割', craftId: 1 })]
    })
    vm.versionList = [{ id: 5, version: 'V1', remark: '原备注', status: status.暂存 }]
    vm.loadedVersionId = 5
    vm.originalVersion = vm.getVersionSnapshot()
    vm.$.refs = { copyVersionDialog: { open: (...args) => opened.push(args) } }
    vm.activeVersion.remark = '未保存的备注'
    vm.openCopyVersion()
    assert.equal(opened.length, 0)
    assert.match(messages[0], /先暂存/)
    vm.activeVersion.remark = '原备注'
    vm.openCopyVersion()
    assert.equal(opened.length, 1)
    assert.equal(opened[0][0].id, 9)
    assert.equal(opened[0][1], 5)
    vm.saving = true
    vm.openCopyVersion()
    vm.saving = false
    props.mode = 'view'
    await nextTick()
    vm.openCopyVersion()
    assert.equal(opened.length, 1, 'saving or read-only headers cannot open the dialog')
})

test('copy version header refreshes only the matching target and reports a refresh failure', async t => {
    const pending = deferred(), refreshed = []
    const { vm, events, errors } = mount(t, 'craft-order-header.vue', {
        mode: 'edit', activeVersionId: 5, modelValue: { id: 9 }
    })
    vm.init = async id => { refreshed.push(id); await pending.promise }
    await vm.onVersionCopied({ craftOrderId: 8, versionId: 81 })
    assert.deepEqual(refreshed, [])
    const refreshing = vm.onVersionCopied({ craftOrderId: 9, versionId: 91 })
    assert.equal(vm.saving, true)
    assert.deepEqual(refreshed, [91])
    pending.resolve()
    await refreshing
    assert.equal(vm.saving, false)
    assert.equal(events.filter(event => event[0] === 'versionSaved').length, 1)
    vm.init = async () => { throw new Error('refresh failed') }
    await vm.onVersionCopied({ craftOrderId: 9, versionId: 92 })
    assert.match(errors[0].message, /refresh failed/)
    assert.equal(vm.saving, false)
    assert.equal(events.filter(event => event[0] === 'versionSaved').length, 1)
})


test('revert version changes restores saved metadata, nested materials, equipment and route order', async t => {
    const posts = []
    const savedRoutes = [process({ id: 10, name: '加工', craftId: 7, isInbound: true,
        machineIds: [8], machineSettings: [{ machineId: 8, theoreticalWorkHours: 2 }],
        materials: [{ ...resource(1), id: 101, ioType: 1, quantity: 3,
            unitList: [{ id: 1, ratio: 1 }], substitutes: [{ ...resource(2), id: 102, quantity: 4 }] }] }),
        process({ id: 20, name: '入库', isInbound: true })]
    const { vm, props, events } = mount(t, 'craft-order-header.vue', {
        mode: 'edit', activeVersionId: 5, modelValue: { id: 9 }, routes: []
    }, { get: async () => ({ data: { nodeSettings: savedRoutes } }),
        post: async (url, body) => { posts.push({ url, body }); return { data: {} } } })
    vm.versionList = [{ id: 5, version: 'V1', remark: '原备注', isMain: false, status: status.暂存 }]
    await vm.loadRoutes(5)
    props.routes = events.filter(event => event[0] === 'routes').at(-1)[1]
    await nextTick()
    const original = JSON.parse(JSON.stringify(props.routes))
    vm.activeVersion.version = '改名'
    vm.activeVersion.remark = '修改备注'
    vm.activeVersion.isMain = true
    props.routes[0].materials[0].quantity = 9
    props.routes[0].materials[0].substitutes[0].quantity = 8
    props.routes[0].materials[0].unitList[0].ratio = 5
    props.routes[0].machineSettings[0].theoreticalWorkHours = 6
    props.routes.splice(1, 1)
    props.routes.unshift(process({ name: '临时工序' }))
    vm.dragIndex = 1
    assert.ok(vm.versionChanges.length > 0)
    const count = events.length
    vm.saving = true
    vm.revertVersionChanges()
    assert.equal(events.length, count, 'saving blocks rollback')
    vm.saving = false
    vm.revertVersionChanges()
    props.routes = events.filter(event => event[0] === 'routes').at(-1)[1]
    await nextTick()
    assert.deepEqual(JSON.parse(JSON.stringify(props.routes)), original)
    assert.equal(vm.activeVersion.version, 'V1')
    assert.equal(vm.activeVersion.remark, '原备注')
    assert.equal(vm.activeVersion.isMain, false)
    assert.equal(vm.dragIndex, null)
    assert.equal(vm.versionChanges.length, 0)
    assert.deepEqual(posts, [])
    props.routes[0].materials[0].quantity = 12
    assert.equal(vm.originalEditData.routes[0].materials[0].quantity, 3)
    vm.originalEditData.version.id = 6
    const staleCount = events.length
    vm.revertVersionChanges()
    assert.equal(events.length, staleCount, 'another version baseline cannot be applied')
    vm.originalEditData.version.id = 5
    props.mode = 'view'
    await nextTick()
    const readonlyCount = events.length
    vm.revertVersionChanges()
    assert.equal(events.length, readonlyCount, 'read-only mode blocks rollback')
})
