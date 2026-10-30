// Run with node --test; requires vue, @vue/compiler-sfc and typescript on NODE_PATH.
const assert = require('node:assert/strict')
const fs = require('node:fs')
const path = require('node:path')
const Module = require('node:module')
const { test } = require('node:test')
const ts = require('typescript')
const { parse, compileScript, compileTemplate } = require('@vue/compiler-sfc')
const { createRenderer, h, nextTick, reactive, ref } = require('vue')

const filename = path.resolve(__dirname, '../craft-tree.vue')
const { descriptor, errors } = parse(fs.readFileSync(filename, 'utf8'), { filename })
assert.deepEqual(errors, [])
assert.deepEqual(compileTemplate({ source: descriptor.template.content, filename, id: 'craft-tree' }).errors, [])
const script = compileScript(descriptor, { id: 'craft-tree' })
const compiled = ts.transpileModule(script.content, {
    compilerOptions: { module: ts.ModuleKind.CommonJS, target: ts.ScriptTarget.ES2020 },
    reportDiagnostics: true
})
assert.deepEqual(compiled.diagnostics, [])
const craftTreeNodeType = { Process: 1, Input: 2, Output: 3, Product: 4, SemiProduct: 5, Material: 6, Substitute: 7 }
const productType = { 原材料: 0, 半成品: 1, 成品: 2, 0: '原材料', 1: '半成品', 2: '成品' }
const loaded = { exports: {} }
const localRequire = id => {
    if (/types\/craft(\.js)?$/.test(id)) return { CraftTreeNodeType: craftTreeNodeType }
    if (id.endsWith('/enums')) return { ProductType: productType }
    return require(id)
}
new Function('require', 'module', 'exports', compiled.outputText)(localRequire, loaded, loaded.exports)
const component = { ...loaded.exports.default, render: () => null }

// Use Vue's actual reactivity/lifecycle without requiring the complete business app.
const renderer = createRenderer({
    createElement: () => ({}), createText: () => ({}), createComment: () => ({}),
    insert() {}, remove() {}, setText() {}, setElementText() {}, patchProp() {},
    parentNode: () => null, nextSibling: () => null
})
const material = (extra = {}) => ({
    id: 11, dataId: 100, dataType: 0, materialType: '半成品', refCraftOrderVersionId: 7,
    materialName: '半成品 A', ioType: 1, substitutes: [], ...extra
})
const routes = (materials = [material()]) => [{ id: 1, code: 'P1', name: '工序一', materials }]
const deferred = () => {
    let resolve, reject
    const promise = new Promise((yes, no) => { resolve = yes; reject = no })
    return { promise, resolve, reject }
}
function mount(t, get, initialRoutes = routes()) {
    const props = reactive({ craftOrderId: 1, activeVersionId: 1, routes: initialRoutes })
    const instance = ref()
    const errors = []
    const selections = []
    const app = renderer.createApp({ render: () => h(component, {
        ...props, ref: instance, onSelectProcess: value => selections.push(value)
    }) })
    app.config.globalProperties.$http = { get }
    app.config.globalProperties.$kingzon_messager = { showError: error => errors.push(error) }
    app.mount({})
    let mounted = true
    const unmount = () => {
        if (mounted) app.unmount()
        mounted = false
    }
    t.after(unmount)
    return { vm: instance.value, props, errors, selections, unmount }
}
const firstMaterial = vm => vm.treeRows[0].children[0].children[0]
function flatten(rows) {
    return rows.flatMap(row => [row, ...flatten(row.children)])
}
const referencedTree = () => [{
    id: 'ref-process-2', name: '外部工序', typeText: '工序', state: 'open', children: [{
        id: 'ref-process-2-input', name: '领用', typeText: '领用', children: [{
            id: 'ref-material-22', name: '深层半成品', materialType: '半成品',
            dataId: 200, dataType: 0, refCraftOrderVersionId: 8, state: 'closed', children: []
        }]
    }, { id: 'ref-process-2-output', name: '产出', state: 'closed', children: [] }]
}]

test('loading updates only the expanded node and coalesces repeat clicks', async t => {
    const pending = deferred()
    let requests = 0
    const { vm } = mount(t, () => { requests++; return pending.promise })
    const root = vm.treeRows
    const row = firstMaterial(vm)
    const loading = vm.onNodeExpand(row)
    await vm.onNodeExpand(row)
    assert.equal(requests, 1)
    assert.equal(vm.treeRows, root)
    assert.equal(row.state, 'open')
    assert.equal(row.children[0].name, '加载中...')
    pending.resolve({ data: referencedTree() })
    await loading
    await nextTick()
    assert.equal(vm.treeRows, root)
    assert.equal(firstMaterial(vm), row)
    assert.equal(row.state, 'open')
    assert.equal(row.loadState, 'loaded')
    assert.equal(row.children[0].name, '外部工序')
    vm.onNodeCollapse(row)
    await vm.onNodeExpand(row)
    assert.equal(requests, 1)
})

test('empty, null and direct responses finish loading and remain cached', async t => {
    for (const response of [{ data: [] }, { data: null }, null, []]) {
        let requests = 0
        const { vm, selections } = mount(t, async () => { requests++; return response })
        const row = firstMaterial(vm)
        await vm.onNodeExpand(row)
        assert.equal(row.state, 'open')
        assert.equal(row.loadState, 'loaded')
        assert.equal(row.children[0].name, '暂无工艺单')
        vm.selectRow(row.children[0])
        assert.equal(selections.length, 0)
        vm.onNodeCollapse(row)
        await vm.onNodeExpand(row)
        assert.equal(requests, 1)
    }
})

test('nested materials load recursively; empty groups are leaves', async t => {
    const { vm } = mount(t, async (url, query) => ({ data: query.dataId === 100 ? referencedTree() : [] }))
    const row = firstMaterial(vm)
    await vm.onNodeExpand(row)
    const process = row.children[0]
    const nested = process.children[0].children[0]
    assert.equal(nested.loadState, 'idle')
    assert.equal(process.children[1].state, 'open')
    assert.equal(process.children[1].children.length, 0)
    await vm.onNodeExpand(nested)
    assert.equal(nested.children[0].name, '暂无工艺单')
    assert.equal(nested.state, 'open')
})

test('collapse during a request stays collapsed after completion', async t => {
    const pending = deferred()
    const { vm } = mount(t, () => pending.promise)
    const row = firstMaterial(vm)
    const loading = vm.onNodeExpand(row)
    vm.onNodeCollapse(row)
    pending.resolve({ data: referencedTree() })
    await loading
    assert.equal(row.state, 'closed')
    assert.equal(row.loadState, 'loaded')
})

test('failures and malformed responses leave a retryable message', async t => {
    for (const failure of [new Error('network failed'), { data: { unexpected: true } }]) {
        let requests = 0
        const { vm, errors } = mount(t, async () => {
            requests++
            if (requests > 1) return { data: [] }
            if (failure instanceof Error) throw failure
            return failure
        })
        const row = firstMaterial(vm)
        await vm.onNodeExpand(row)
        assert.equal(row.loadState, 'error')
        assert.match(row.children[0].name, /加载失败/)
        assert.equal(row.state, 'open')
        assert.equal(errors.length, 1)
        vm.onNodeCollapse(row)
        await vm.onNodeExpand(row)
        assert.equal(requests, 2)
        assert.equal(row.children[0].name, '暂无工艺单')
    }
})

test('route edits preserve node identity, collapse state, substitutes and in-flight requests', async t => {
    const pending = deferred()
    const { vm, props } = mount(t, () => pending.promise, routes([material({
        substitutes: [material({ id: 12, materialName: '替料' })]
    })]))
    const row = firstMaterial(vm)
    const loading = vm.onNodeExpand(row)
    vm.onNodeCollapse(vm.treeRows[0])
    vm.onNodeCollapse(vm.treeRows[0].children[0])
    props.routes[0].materials[0].quantity = 8
    await nextTick()
    assert.equal(firstMaterial(vm), row)
    assert.equal(row.quantityText, 8)
    assert.equal(vm.treeRows[0].state, 'closed')
    assert.equal(vm.treeRows[0].children[0].state, 'closed')
    pending.resolve({ data: [] })
    await loading
    assert.deepEqual(row.children.map(child => child.name), ['替料', '暂无工艺单'])
    assert.equal(row.state, 'open')
})

test('unsaved rows and repeated referenced processes have unique IDs and independent states', async t => {
    const { vm } = mount(t, async () => ({ data: referencedTree() }), routes([
        material({ id: 0 }), material({ id: 0 })
    ]))
    const [first, second] = vm.treeRows[0].children[0].children
    await vm.onNodeExpand(first)
    await vm.onNodeExpand(second)
    const all = flatten(vm.treeRows)
    assert.equal(new Set(all.map(row => row.id)).size, all.length)
    vm.onNodeCollapse(first.children[0])
    assert.equal(second.children[0].state, 'open')
})

test('version/order changes and product replacement discard stale responses', async t => {
    for (const change of [
        props => { props.activeVersionId = 2 },
        props => { props.craftOrderId = 2 },
        props => { props.routes[0].materials[0].dataId = 300 }
    ]) {
        const pending = deferred()
        const { vm, props } = mount(t, () => pending.promise)
        const oldRow = firstMaterial(vm)
        const oldRenderKey = vm.treeRenderKey
        const loading = vm.onNodeExpand(oldRow)
        change(props)
        await nextTick()
        const currentRow = firstMaterial(vm)
        assert.notEqual(currentRow, oldRow)
        assert.ok(vm.treeRenderKey > oldRenderKey)
        pending.resolve({ data: referencedTree() })
        await loading
        assert.equal(currentRow.loadState, 'idle')
        assert.equal(currentRow.state, 'closed')
        assert.equal(currentRow.children[0].name, '展开加载工艺单')
    }
})

test('nodes without refCraftOrderVersionId stay leaves and never request children', async t => {
    const { vm } = mount(t, () => assert.fail('must not request nodes without reference'), routes([
        material({ refCraftOrderVersionId: undefined })
    ]))
    const row = firstMaterial(vm)
    assert.equal(row.state, 'open')
    assert.equal(row.loadState, undefined)
    assert.equal(row.children.length, 0)
    await vm.onNodeExpand(row)
    assert.equal(row.children.length, 0)
})

test('adding or removing the reference toggles expandability', async t => {
    const { vm, props } = mount(t, async () => ({ data: referencedTree() }), routes([
        material({ refCraftOrderVersionId: undefined })
    ]))
    const leafRow = firstMaterial(vm)
    assert.equal(leafRow.state, 'open')

    props.routes[0].materials[0].refCraftOrderVersionId = 7
    await nextTick()
    const lazyRow = firstMaterial(vm)
    assert.equal(lazyRow, leafRow)
    assert.equal(lazyRow.loadState, 'idle')
    assert.equal(lazyRow.state, 'closed')

    await vm.onNodeExpand(lazyRow)
    assert.equal(lazyRow.loadState, 'loaded')
    assert.equal(lazyRow.state, 'open')

    props.routes[0].materials[0].refCraftOrderVersionId = undefined
    await nextTick()
    assert.equal(firstMaterial(vm), lazyRow)
    assert.equal(lazyRow.loadState, undefined)
    assert.equal(lazyRow.children.length, 0)
    assert.equal(lazyRow.state, 'open')
})

test('invalid material IDs cannot enter perpetual loading', async t => {
    const { vm } = mount(t, () => assert.fail('must not request invalid material'), routes([
        material({ dataId: 0 }), material({ id: 12, dataType: undefined })
    ]))
    for (const row of vm.treeRows[0].children[0].children) {
        assert.equal(row.state, 'open')
        assert.equal(row.children.length, 0)
        await vm.onNodeExpand(row)
        assert.equal(row.loadState, undefined)
    }
})

test('late failures after a version switch or unmount do not show stale errors', async t => {
    for (const dispose of [
        mounted => { mounted.props.activeVersionId = 2 },
        mounted => mounted.unmount()
    ]) {
        const pending = deferred()
        const mounted = mount(t, () => pending.promise)
        const loading = mounted.vm.onNodeExpand(firstMaterial(mounted.vm))
        dispose(mounted)
        await nextTick()
        pending.reject(new Error('stale request'))
        await loading
        assert.equal(mounted.errors.length, 0)
    }
})

test('legacy display fields and typeText-only nested materials remain supported', async t => {
    const { vm, selections } = mount(t, async () => [{
        id: 'legacy-child', typeText: '成品', dataId: 200, dataType: 0, refCraftOrderVersionId: 8
    }], routes([material({ materialName: undefined, name: '旧名称', code: 'M1' })]))
    const row = firstMaterial(vm)
    assert.equal(row.name, '旧名称')
    assert.equal(row.code, 'M1')
    vm.selectRow(row)
    assert.deepEqual(selections, [{ key: undefined, code: 'P1', tab: 1, material: row.rootMaterial }])
    await vm.onNodeExpand(row)
    assert.equal(row.children[0].loadState, 'idle')
    assert.equal(row.children[0].state, 'closed')
})

test('name column grows with expanded lazy levels and ignores collapsed descendants', async t => {
    const { vm } = mount(t, async () => ({ data: referencedTree() }))
    const initialWidth = vm.nameColumnWidth
    const row = firstMaterial(vm)
    await vm.onNodeExpand(row)
    const nested = row.children[0].children[0].children[0]
    const expandedWidth = vm.nameColumnWidth
    assert.ok(expandedWidth > initialWidth)
    await vm.onNodeExpand(nested)
    assert.ok(vm.nameColumnWidth > expandedWidth)
    vm.onNodeCollapse(row)
    assert.equal(vm.nameColumnWidth, initialWidth)
})

test('tree material navigation carries the exact top-level row and ignores substitutes and lazy descendants', async t => {
    const data = routes()
    data[0].materials[0].substitutes = [{ id: 12, dataId: 999, materialName: '替料' }]
    const { vm, props, selections } = mount(t, async () => ({ data: referencedTree() }), data)
    const row = firstMaterial(vm)
    vm.selectRow(row)
    assert.equal(selections[0].material, props.routes[0].materials[0])
    vm.selectRow(row.fixedChildren[0])
    await vm.onNodeExpand(row)
    vm.selectRow(row.lazyChildren[0])
    assert.equal(selections.length, 1)
    vm.selectRow(vm.treeRows[0])
    assert.deepEqual(selections[1], { key: undefined, code: props.routes[0].code, tab: 0 })
})


test('inbound or outsource processes show output groups and node editing preserves selection targets', async t => {
    const data=[{uiKey:'draft-fixed',code:'10',name:'工序',isInbound:false,isOutsource:false,materials:[]}]
    const {vm,props,selections}=mount(t,async()=>({data:[]}),data)
    assert.equal(vm.treeRows[0].children.length,1)
    props.routes[0].isOutsource=true
    await nextTick()
    assert.equal(vm.treeRows[0].children.length,2)
    const node=vm.treeRows[0],group=node.children[1]
    vm.selectRow(group)
    assert.deepEqual(selections[0],{key:'draft-fixed',code:'10',tab:1})
    props.routes[0].code=''
    await nextTick()
    assert.equal(vm.treeRows[0],node)
    assert.equal(vm.treeRows[0].children[1],group)
    props.routes[0].isOutsource=false
    await nextTick()
    assert.equal(vm.treeRows[0].children.length,1)
})
