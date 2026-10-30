const assert = require('node:assert/strict')
const fs = require('node:fs')
const path = require('node:path')
const os = require('node:os')
const { test } = require('node:test')
const ts = require('typescript')
const { parse, compileTemplate, compileScript } = require('@vue/compiler-sfc')
const { createRenderer, h, ref, reactive, nextTick } = require('vue')
const root = path.resolve(__dirname, '..')
const src = path.resolve(root, '../../..')
const cache = new Map()
function load(filename) {
    filename = path.resolve(filename)
    // The snapshot consolidated domain DTOs into schedule.ts; retain the old import alias in the isolated harness.
    if (filename === path.join(src, 'types/production-schedule.ts') && !fs.existsSync(filename)) filename = path.join(src, 'types/schedule.ts')
    if (cache.has(filename)) return cache.get(filename)
    let source = fs.readFileSync(filename, 'utf8')
    if (filename.endsWith('.vue')) source = parse(source, { filename }).descriptor.script.content
    const code = ts.transpileModule(source, { compilerOptions: { module: ts.ModuleKind.CommonJS, target: ts.ScriptTarget.ES2020 } }).outputText
    const module = { exports: {} }
    function localRequire(id) {
        if (id.endsWith('resource-library.vue')) return { default: { name: 'ResourceLibrary', render: () => null } }
        if (id.startsWith('@/')) return load(path.join(src, id.slice(2) + (path.extname(id) ? '' : '.ts')))
        if (id.startsWith('.')) return load(path.resolve(path.dirname(filename), id + (path.extname(id) ? '' : '.ts')))
        return require(id)
    }
    new Function('require', 'module', 'exports', code)(localRequire, module, module.exports)
    cache.set(filename, module.exports)
    return module.exports
}
const model = load(path.join(root, 'schedule.ts'))
const dto = load(path.join(src, 'types/production-schedule.ts'))
const renderer = createRenderer({ createElement: () => ({}), createText: () => ({}), createComment: () => ({}), insert() {}, remove() {}, setText() {}, setElementText() {}, patchProp() {}, parentNode: () => null, nextSibling: () => null })
function template(id = 31) {
    return { craftVersionId: id, version: 'V1', quantityUnit: 'KG',
        crafts: [{ id: 7, name: '切割' }], departments: [{ value: 1, text: '生产部' }],
        machines: [{ id: 8, text: 'M8', craftIds: [7], users: [{ id: 1, text: '甲' }, { id: 2, text: '乙' }] }],
        processs: [{ nodeSettingTemplateId: 10, order: 10, name: '切割', craftId: 7, isOutsource: false, isInbound: false, isReportWork: true,
            materials: [{ dataId: 100, dataType: 1, code: 'P100', name: '钢板', quantityPerUnit: 2, quantity: 2,
                unitId: 1, unitConversionId: 0, unitOptions: [{ id: 0, unitId: 1, unitName: 'KG', ratio: 1 }, { id: 5, unitId: 2, unitName: '箱', ratio: 5 }] }],
            machines: [{ machineId: 8, users: [] }] }] }
}
function mount(t, filename, input, get = async () => ({ data: template() }), post = async () => ({ data: { craftVersionId: 31, materials: [] } })) {
    const props = reactive(input), instance = ref(), events = [], updates = [], errors = [], previews = []
    const dialogStub = { render: () => null, methods: { open() {}, close() {} } }
    const component = { ...load(path.join(root, filename)).default, render: filename.endsWith('schedule-material-dialog.vue') ? () => h(dialogStub, {ref: 'dialog'}) : filename.endsWith('schedule-machine.vue') ? () => h(dialogStub, {ref: 'machineDialog'}) : () => null }
    const app = renderer.createApp({ render: () => h(component, { ...props, ref: instance, onSubmit: value => events.push(value), 'onUpdate:modelValue': value => updates.push(value), onPreview: value => previews.push(value) }) })
    app.config.globalProperties.$http = { get, post }
    app.config.globalProperties.$kingzon_messager = { showError: value => errors.push(value), showConfim: async () => {} }
    app.mount({}); t.after(() => app.unmount())
    return { vm: instance.value, props, events, updates, errors, previews }
}
async function settle() { await nextTick(); await nextTick() }

test('component accepts only version ID, loads detached editable template and scales materials without changing the template', async t => {
    const source = template(), calls = []
    const { vm } = mount(t, 'component/create-schedule.vue', { craftVersionId: 31 }, async (url, params) => { calls.push({ url, params }); return { data: source } })
    await settle()
    assert.equal(calls.length, 1); assert.deepEqual(calls[0].params, { craftVersionId: 31 })
    assert.equal(vm.processes[0].nodeSettingTemplateId, 10)
    vm.form.quantity = 10; await settle()
    assert.equal(vm.processes[0].materials[0].quantity, 20)
    assert.equal(source.processs[0].materials[0].quantity, 2)
    vm.processes[0].materials[0].quantity = 30
    vm.processes[0].materials[0].quantityPerUnit = 3
    vm.form.quantity = 20; await settle()
    assert.equal(vm.processes[0].materials[0].quantity, 60)
    assert.equal(source.processs[0].materials[0].quantityPerUnit, 2)
})

test('system setting controls additions and order changes; new processes use no template ID', async t => {
    const { vm, props } = mount(t, 'component/create-schedule.vue', { craftVersionId: 31, allowProcessEdit: false })
    await settle(); vm.addProcess(); assert.equal(vm.processes.length, 1)
    props.allowProcessEdit = true; await settle(); vm.addProcess()
    assert.equal(vm.processes.length, 2); assert.equal(vm.currentProcess.nodeSettingTemplateId, 0)
    vm.currentProcess.name = '新增生产'; vm.moveProcess(-1)
    assert.equal(vm.processes[0].name, '新增生产')
    vm.removeProcess(); assert.equal(vm.processes.length, 1)
})

test('version races do not overwrite newer draft and load failure never falls back to demo data', async t => {
    const pending = new Map()
    const { vm, props } = mount(t, 'component/create-schedule.vue', { craftVersionId: 31 }, (url, data) => new Promise((resolve, reject) => pending.set(data.craftVersionId, { resolve, reject })))
    props.craftVersionId = 32; await settle()
    pending.get(32).resolve({ data: template(32) }); await settle()
    pending.get(31).resolve({ data: template(31) }); await settle()
    assert.equal(vm.template.craftVersionId, 32); assert.equal(vm.form.craftVersionId, 32)
    props.craftVersionId = 33; await settle(); pending.get(33).reject(Error('failed')); await settle()
    assert.equal(vm.processes.length, 0); assert.ok(vm.loadError)
})

test('hierarchical equipment selection preserves per-person hours and emits detached changes', t => {
    const selected = [{ machineId: 8, users: [] }]
    const { vm, updates } = mount(t, 'component/schedule-machine.vue', { modelValue: selected, machineOptions: template().machines, quantity: 10 })
    vm.openSelection(); vm.toggleUser(8, 1, true); vm.toggleUser(8, 2, true); vm.confirmSelection()
    assert.equal(updates[0][0].users[0].quantity, 5); assert.equal(updates[0][0].users[1].quantity, 5)
    assert.deepEqual(selected, [{ machineId: 8, users: [] }])
    const { vm: userVm, updates: userUpdates } = mount(t, 'component/schedule-machine.vue', { modelValue: updates[0], machineOptions: template().machines, quantity: 10 })
    userVm.updateUser({ machineId: 8, userId: 2 }, 'hours', 3.5)
    assert.equal(userUpdates[0][0].users[1].hours, 3.5)
    assert.equal(updates[0][0].users[1].hours, 0)
    assert.equal(userVm.rows[0].children.length, 2)
})

test('submission matches Processs DTO, flattens equipment/person pairs, rejects incomplete allocation, and contains no editor fields', async t => {
    const { vm, events } = mount(t, 'component/create-schedule.vue', { craftVersionId: 31, initialQuantity: 10 })
    await settle()
    vm.start = new Date(2026, 9, 28, 8); vm.end = new Date(2026, 9, 28, 18)
    vm.submit(); assert.equal(events.length, 0)
    vm.currentProcess.machines[0].users = [{ userId: 1, quantity: 4, hours: 2 }, { userId: 2, quantity: 6, hours: 3 }]
    vm.submit(); assert.equal(events.length, 1)
    const value = events[0]
    assert.equal(value.processs[0].machineRls.length, 2)
    assert.equal(value.processs[0].machineRls.reduce((sum, row) => sum + row.quantity, 0), 10)
    assert.equal(value.processs[0].isReportWork, true)
    assert.equal(value.processs[0].materials[0].quantity, 20)
    assert.ok(!('key' in value.processs[0])); assert.ok(!('unitOptions' in value.processs[0].materials[0]))
    vm.currentProcess.materials[0].quantity = 999
    assert.equal(value.processs[0].materials[0].quantity, 20)
})

test('validators reject invalid units, resources and duplicate personnel before emitting', () => {
    const source = new dto.ProductionScheduleTemplateDto(template())
    const processes = model.createProductionScheduleDraft(source, 10)
    processes[0].machines[0].users = [{ userId: 1, quantity: 5, hours: 2 }, { userId: 1, quantity: 5, hours: 1 }]
    const value = model.productionSchedulePost(new dto.ProductionSchedulePostDto({ craftVersionId: 31, quantity: 10, start: '2026-10-28T08:00:00', end: '2026-10-28T18:00:00' }), processes)
    assert.ok(model.validateProductionSchedule(value, source).some(message => message.includes('不能重复')))
    value.processs[0].materials[0].unitId = 99
    assert.ok(model.validateProductionSchedule(value, source).some(message => message.includes('单位换算')))
})

test('independent SFCs compile and component scripts/type DTOs pass strict TypeScript checking', t => {
    const folder = fs.mkdtempSync(path.join(os.tmpdir(), 'independent-schedule-types-'))
    t.after(() => fs.rmSync(folder, { recursive: true, force: true }))
    fs.mkdirSync(path.join(folder, 'component')); fs.mkdirSync(path.join(folder, 'types'))
    const files = ['component/create-schedule.vue', 'component/schedule-machine.vue', 'component/schedule-process-card.vue', 'component/schedule-material-dialog.vue', 'component/schedule-order-list.vue']
    for (const name of files) {
        const filename = path.join(root, name), source = fs.readFileSync(filename, 'utf8')
        const { descriptor, errors } = parse(source, { filename }); assert.deepEqual(errors, [])
        assert.deepEqual(compileTemplate({ source: descriptor.template.content, filename, id: name }).errors, [])
        fs.writeFileSync(path.join(folder, name + '.ts'), compileScript(descriptor, { id: name }).content)
    }
    for (const name of ['schedule.ts', 'craft.ts', 'enums.ts']) fs.copyFileSync(path.join(src, 'types', name), path.join(folder, 'types', name))
    fs.copyFileSync(path.join(root, 'schedule.ts'), path.join(folder, 'schedule.ts'))
    fs.writeFileSync(path.join(folder, 'host.d.ts'), `import 'vue'; declare module 'vue' { interface ComponentCustomProperties { $http: { get(url: string, params: unknown): Promise<{data: any}>; post(url: string, data: unknown): Promise<{data: any}> }; $kingzon_messager: {showError(error: unknown): void; showConfim(message: string): Promise<void>}; $utils: {formatDateTime(value: string | Date, format: string): string} } }`)
    fs.writeFileSync(path.join(folder, 'modules.d.ts'), `declare module '@/views/*' {const component: any; export default component} declare module '@kingzon/base-pc' {export interface Filter {field: string; value: unknown}}`)
    const program = ts.createProgram([...files.map(name => path.join(folder, name + '.ts')), ...['host.d.ts', 'modules.d.ts', 'schedule.ts'].map(name => path.join(folder, name))], {
        strict: true, noEmit: true, skipLibCheck: true, target: ts.ScriptTarget.ES2020, module: ts.ModuleKind.ESNext,
        moduleResolution: ts.ModuleResolutionKind.Node10, baseUrl: folder,
        paths: { vue: [path.dirname(require.resolve('vue/package.json'))], '@/types/production-schedule': [path.join(folder, 'types/schedule.ts')], '@/types/*': [path.join(folder, 'types/*')] }
    })
    const errors = ts.getPreEmitDiagnostics(program)
    assert.equal(errors.length, 0, ts.formatDiagnosticsWithColorAndContext(errors, { getCurrentDirectory: () => folder, getCanonicalFileName: x => x, getNewLine: () => '\n' }))
})


test('process cards reorder stable keys and copy detached configuration with unique code and material identities', async t => {
    const { vm } = mount(t, 'component/create-schedule.vue', { craftVersionId: 31, allowProcessEdit: true })
    await settle()
    const source = vm.processes[0], sourceKey = source.key, sourceCode = source.code
    source.machines[0].users = [{userId: 1, quantity: 1, hours: 2}]
    vm.copyProcess(source)
    const copy = vm.currentProcess
    assert.notEqual(copy.key, sourceKey); assert.notEqual(copy.code, sourceCode)
    assert.notEqual(copy.materials[0].key, source.materials[0].key)
    copy.materials[0].quantity = 99; copy.machines[0].users[0].hours = 8
    assert.equal(source.materials[0].quantity, 2); assert.equal(source.machines[0].users[0].hours, 2)
    vm.dragKey = copy.key; vm.dropProcess(sourceKey)
    assert.equal(vm.processes[0].key, copy.key); assert.equal(vm.processes[1].key, sourceKey)
    assert.equal(source.code, sourceCode)
    const post = model.productionSchedulePost(vm.form, vm.processes)
    assert.equal(post.processs[0].order, 10); assert.equal(post.processs[1].order, 20)
    assert.equal(vm.selectedKey, copy.key)
})

test('material modal uses isolated drafts; cancel retains source and confirm applies unit conversion once', t => {
    const source = model.createProductionScheduleDraft(new dto.ProductionScheduleTemplateDto(template()), 10)[0].materials
    const { vm, updates, previews } = mount(t, 'component/schedule-material-dialog.vue', {modelValue: source, processKey: 'p', processName: '切割', quantity: 10})
    vm.open(); vm.changeQuantity(vm.rows[0], 30)
    assert.equal(source[0].quantity, 20); assert.equal(previews.at(-1)[0].quantity, 30)
    vm.cancel(); assert.equal(updates.length, 0)
    vm.open(); vm.changeUnit(vm.rows[0], vm.rows[0].unitOptions[1]); vm.confirm()
    assert.equal(updates.length, 1); assert.equal(updates[0][0].quantity, 4)
    assert.equal(updates[0][0].unitId, 2); assert.equal(source[0].unitId, 1)
    updates[0][0].quantity = 999; assert.equal(vm.rows[0].quantity, 4)
})

test('shared stock is allocated once across processes and after unit conversion; missing stock remains unknown', () => {
    const processes = model.createProductionScheduleDraft(new dto.ProductionScheduleTemplateDto(template()), 10)
    const other = new dto.ScheduleProcessEditDto(JSON.parse(JSON.stringify(processes[0])))
    other.key = 'second'; other.materials[0].key = 'second-m'; other.materials[0].unitId = 2; other.materials[0].unitConversionId = 5; other.materials[0].quantity = 4
    processes.push(other)
    const stocks = [{dataType: 1, dataId: 100, stock: 40, occupied: 5, safetyStock: 5}]
    const available = model.scheduleMaterialAvailability(processes, stocks)
    assert.equal(available[`${processes[0].key}/${processes[0].materials[0].key}`].allocated, 20)
    assert.equal(available['second/second-m'].allocated, 2); assert.equal(available['second/second-m'].shortage, 2)
    const summary = model.scheduleEditorSummary(processes, available)
    assert.equal(summary.materialCount, 1); assert.equal(summary.shortageCount, 1); assert.equal(summary.readyCount, 0)
    const unknown = model.scheduleMaterialAvailability(processes, [])
    assert.equal(unknown['second/second-m'].shortage, null)
    assert.equal(model.scheduleEditorSummary(processes, unknown).uncheckedCount, 1)
})

test('inventory check uses current root version and preview allocation; discarding preview restores requirements', async t => {
    const calls = []
    const { vm } = mount(t, 'component/create-schedule.vue', {craftVersionId:31, initialQuantity:10}, undefined, async (url, data) => {
        calls.push({url,data}); return {data:{craftVersionId:31,materials:[{dataType:1,dataId:100,stock:25,occupied:0,safetyStock:0}]}}
    })
    await settle(); await vm.checkStock()
    assert.equal(calls[0].data.quantity, 10); assert.equal(vm.summary.readyCount, 1)
    vm.materialProcessKey = vm.processes[0].key
    vm.materialPreview = JSON.parse(JSON.stringify(vm.processes[0].materials)); vm.materialPreview[0].quantity = 30
    assert.equal(vm.summary.shortageCount, 1)
    assert.equal(vm.getPostData(), null)
    vm.closeMaterials(); assert.equal(vm.summary.readyCount, 1)
})

test('batch hours changes people hours without changing quantity or inferring equipment duration', t => {
    const selected = [{machineId:8,users:[{userId:1,quantity:4,hours:1},{userId:2,quantity:6,hours:2}]}]
    const { vm, updates } = mount(t,'component/schedule-machine.vue',{modelValue:selected,machineOptions:template().machines,quantity:10})
    vm.batchHours = 3; vm.applyHours()
    assert.deepEqual(updates[0][0].users.map(user=>user.hours), [3,3])
    assert.deepEqual(updates[0][0].users.map(user=>user.quantity), [4,6])
    const process = new dto.ScheduleProcessEditDto({machines:updates[0]})
    const summary = model.scheduleEditorSummary([process],{})
    assert.equal(summary.humanHours,6); assert.equal(summary.userCount,2); assert.equal(selected[0].users[0].hours,1)
})


test('adding from a template retains source identity but isolates materials and clears personnel allocations', async t => {
    const {vm} = mount(t, 'component/create-schedule.vue', {craftVersionId:31,initialQuantity:5,allowProcessEdit:true})
    await settle()
    vm.addProcess(vm.template.processs[0])
    const added = vm.currentProcess
    assert.equal(added.nodeSettingTemplateId,10); assert.equal(added.materials[0].quantity,10)
    assert.notEqual(added.key,vm.processes[0].key); assert.notEqual(added.code,vm.processes[0].code)
    assert.equal(added.machines[0].users.length,0)
    added.materials[0].quantity=500
    assert.equal(vm.processes[0].materials[0].quantity,10)
})

test('configured process deletion confirms and cannot remove a freshly reloaded draft', async t => {
    const {vm} = mount(t, 'component/create-schedule.vue', {craftVersionId:31,allowProcessEdit:true})
    await settle()
    let approve
    vm.$kingzon_messager.showConfim = () => new Promise(resolve => {approve=resolve})
    const pending = vm.removeProcess()
    assert.equal(vm.processes.length,1)
    await vm.init(); approve(); await pending
    assert.equal(vm.processes.length,1)
    vm.$kingzon_messager.showConfim = async () => {throw Error('cancel')}
    await vm.removeProcess(); assert.equal(vm.processes.length,1)
    vm.$kingzon_messager.showConfim = async () => {}
    await vm.removeProcess(); assert.equal(vm.processes.length,0)
})

test('codes are submitted independently from order and duplicates are rejected', () => {
    const source = new dto.ProductionScheduleTemplateDto(template())
    const processes = model.createProductionScheduleDraft(source,1)
    processes.push(new dto.ScheduleProcessEditDto(JSON.parse(JSON.stringify(processes[0]))))
    const value = model.productionSchedulePost(new dto.ProductionSchedulePostDto({craftVersionId:31,quantity:1,start:'2026-10-28T08:00:00',end:'2026-10-28T18:00:00'}),processes)
    assert.equal(value.processs[0].order,10); assert.equal(value.processs[1].order,20)
    assert.equal(value.processs[0].code,value.processs[1].code)
    assert.ok(model.validateProductionSchedule(value,source).some(message=>message.includes('工序编码')))
})


test('checkbox dialog confirms detached machine/person choices, preserves hours and cancels without changes', async t => {
    const selected = [{machineId:8, users:[{userId:1,quantity:4,hours:3}]}]
    const options = [...template().machines, {id:9,text:'M9',craftIds:[7],users:[{id:3,text:'丙'}]}]
    const {vm,updates,props} = mount(t,'component/schedule-machine.vue',{modelValue:selected,machineOptions:options,quantity:10})
    vm.openSelection(); vm.toggleUser(8,2,true); vm.closeSelection()
    assert.equal(updates.length,0); assert.equal(selected[0].users.length,1)
    vm.openSelection(); assert.deepEqual(vm.draftUserIds[8],[1])
    vm.toggleUser(8,2,true); vm.toggleMachine(9,true); vm.toggleUser(9,3,true)
    vm.confirmSelection()
    assert.equal(updates.length,1); assert.equal(updates[0][0].users[0].hours,3)
    assert.equal(updates[0][0].users[0].quantity,4)
    assert.equal(updates[0][0].users[1].quantity,3); assert.equal(updates[0][1].users[0].quantity,3)
    assert.equal(selected[0].users.length,1)
    vm.openSelection(); props.machineOptions = []; await settle()
    assert.equal(vm.selectionOpen,false); vm.confirmSelection(); assert.equal(updates.length,1)
})

test('person dialog preserves other equipment and disabled state blocks applying draft', async t => {
    const options = [...template().machines, {id:9,text:'M9',craftIds:[7],users:[{id:3,text:'丙'}]}]
    const selected = [{machineId:8,users:[{userId:1,quantity:4,hours:3}]},{machineId:9,users:[{userId:3,quantity:6,hours:2}]}]
    const {vm,props,updates} = mount(t,'component/schedule-machine.vue',{modelValue:selected,machineOptions:options,quantity:10})
    vm.openSelection(8); assert.equal(vm.selectionOptions.length,1)
    vm.toggleMachine(9,false); vm.toggleUser(8,2,true); vm.confirmSelection()
    assert.deepEqual(JSON.parse(JSON.stringify(updates[0][1])),selected[1])
    vm.openSelection(); props.disabled=true; await settle(); vm.confirmSelection()
    assert.equal(vm.selectionOpen,false); assert.equal(updates.length,1)
})


test('independent outsource/inbound flags round-trip without creating extra processes', () => {
    for (const isOutsource of [false,true]) for (const isInbound of [false,true]) {
        const source=template(); Object.assign(source.processs[0],{isOutsource,isInbound})
        const processes=model.createProductionScheduleDraft(new dto.ProductionScheduleTemplateDto(source),10)
        assert.equal(processes.length,1)
        if (!isOutsource) processes[0].machines[0].users=[{userId:1,quantity:10,hours:1}]
        const value=model.productionSchedulePost(new dto.ProductionSchedulePostDto({craftVersionId:31,quantity:10,start:'2026-10-29T08:00:00',end:'2026-10-29T18:00:00'}),processes)
        assert.equal(value.processs[0].isOutsource,isOutsource)
        assert.equal(value.processs[0].isInbound,isInbound)
        assert.ok(!('nodeType' in value.processs[0]))
        assert.deepEqual(model.validateProductionSchedule(value,source),[])
        processes[0].isOutsource=!isOutsource; processes[0].isInbound=!isInbound
        assert.equal(source.processs[0].isInbound,isInbound)
    }
})

test('process flag edits respect the system setting and material editor opens the active draft', async t => {
    const {vm,props}=mount(t,'component/create-schedule.vue',{craftVersionId:31,allowProcessEdit:false})
    await settle()
    const process=vm.processes[0]
    vm.updateProcess(process,{isOutsource:true,isInbound:true})
    assert.equal(process.isOutsource,false);assert.equal(process.isInbound,false)
    props.allowProcessEdit=true;await settle()
    vm.updateProcess(process,{isOutsource:true,isInbound:true})
    assert.equal(process.isOutsource,true);assert.equal(process.isInbound,true)
    await vm.openMaterials(process)
    assert.equal(vm.materialProcess,process)
    vm.closeMaterials();assert.equal(vm.materialProcess,undefined)
    props.saving=true;await settle()
    vm.updateProcess(process,{isInbound:false});await vm.openMaterials(process)
    assert.equal(process.isInbound,true);assert.equal(vm.materialProcess,undefined)
})


test('outsource accepts template equipment without internal staff and preserves explicit personnel allocations', async t => {
    const source=template();source.processs[0].isOutsource=true
    const {vm}=mount(t,'component/create-schedule.vue',{craftVersionId:31,allowProcessEdit:true},async()=>({data:source}))
    await settle();vm.start=new Date(2026,9,29,8);vm.end=new Date(2026,9,29,18)
    assert.equal(vm.getPostData().processs[0].machineRls.length,0)
    vm.currentProcess.machines[0].users=[{userId:1,quantity:1,hours:2}]
    assert.equal(vm.getPostData().processs[0].machineRls[0].hours,2)
    vm.updateProcess(vm.currentProcess,{isOutsource:false})
    assert.equal(vm.getPostData().processs[0].machineRls[0].hours,2)
    vm.currentProcess.machines[0].users=[]
    assert.equal(vm.getPostData(),null)
})


test('process expansion starts collapsed and remains independent of selected process and reorder', async t => {
    const { vm } = mount(t, 'component/create-schedule.vue', { craftVersionId: 31, allowProcessEdit: true })
    await settle()
    const first = vm.currentProcess
    assert.ok(first)
    assert.equal(vm.expandedKey, '')
    vm.toggleProcess(first.key)
    assert.equal(vm.expandedKey, first.key)
    vm.toggleProcess(first.key)
    assert.equal(vm.expandedKey, '')
    assert.equal(vm.currentProcess, first)
    vm.copyProcess(first)
    const copied = vm.currentProcess
    assert.equal(vm.expandedKey, copied.key)
    vm.moveProcess(-1, copied.key)
    assert.equal(vm.expandedKey, copied.key)
    assert.equal(vm.processes[0], copied)
})

test('process more menu preserves operation events and permission and order boundaries', async t => {
    const actions = []
    const { vm, props } = mount(t, 'component/schedule-process-card.vue', {
        process: new dto.ScheduleProcessEditDto({ name: '长工序名称'.repeat(30), machines: [
            { machineId: 8, users: [{ userId: 1, hours: 2, quantity: 1 }] },
            { machineId: 9, users: [{ userId: 1, hours: 3, quantity: 1 }] }
        ] }), index: 0, count: 2, quantity: 2, editable: true,
        onMove: step => actions.push(['move', step]), onCopy: () => actions.push(['copy']), onRemove: () => actions.push(['remove'])
    })
    vm.processAction('up'); vm.processAction('down'); vm.processAction('copy'); vm.processAction('remove')
    assert.deepEqual(actions, [['move', 1], ['copy'], ['remove']])
    assert.equal(vm.userCount, 1); assert.equal(vm.allocationCount, 2); assert.equal(vm.humanHours, 5)
    props.saving = true; await settle(); vm.processAction('copy'); vm.processAction('remove')
    assert.equal(actions.length, 3)
    props.saving = false; props.editable = false; await settle(); vm.processAction('down')
    assert.equal(actions.length, 3)
})
