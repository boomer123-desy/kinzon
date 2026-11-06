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
        if (filename === path.join(root, 'index.vue') && id.endsWith('.vue')) return { default: { render: () => null } }
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
    const app = renderer.createApp({ render: () => h(component, { ...props, ref: instance, onSubmit: value => events.push(value), onScheduleSubmit: value => events.push(value), 'onUpdate:modelValue': value => updates.push(value), onPreview: value => previews.push(value) }) })
    app.config.globalProperties.$http = { get, post }
    app.config.globalProperties.$kingzon_messager = { showInfo() {}, showError: value => errors.push(value), showConfim: async () => {} }
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

test('system setting controls additions and deletion; temporary process requires initial craft selection', async t => {
    const { vm, props } = mount(t, 'component/create-schedule.vue', { craftVersionId: 31, allowProcessEdit: false })
    await settle(); vm.addProcess(); assert.equal(vm.processes.length, 1)
    props.allowProcessEdit = true; await settle(); vm.addProcess(); assert.equal(vm.processes.length,1)
    vm.addName='新增生产'; vm.addCraftId=7; vm.addProcess()
    assert.equal(vm.processes.length, 2); assert.equal(vm.currentProcess.nodeSettingTemplateId, 0)
    assert.equal(vm.processes[1].name, '新增生产'); assert.equal(vm.processes[1].craftId,7)
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
    const files = ['component/create-schedule.vue', 'component/schedule-machine.vue', 'component/schedule-process-card.vue', 'component/schedule-material-dialog.vue', 'component/schedule-order-list.vue', 'component/schedule-saved-detail.vue', 'component/schedule-craft-picker.vue', 'index.vue']
    for (const name of files) {
        const filename = path.join(root, name), source = fs.readFileSync(filename, 'utf8')
        const { descriptor, errors } = parse(source, { filename }); assert.deepEqual(errors, [])
        assert.deepEqual(compileTemplate({ source: descriptor.template.content, filename, id: name }).errors, [])
        fs.writeFileSync(path.join(folder, name + '.ts'), compileScript(descriptor, { id: name }).content)
    }
    for (const name of ['schedule.ts', 'craft.ts', 'enums.ts']) fs.copyFileSync(path.join(src, 'types', name), path.join(folder, 'types', name))
    fs.copyFileSync(path.join(root, 'schedule.ts'), path.join(folder, 'schedule.ts'))
    fs.writeFileSync(path.join(folder, 'host.d.ts'), `import 'vue'; declare module 'vue' { interface ComponentCustomProperties { $http: { get(url: string, params: unknown): Promise<{data: any}>; post(url: string, data: unknown, grid?: unknown): Promise<{data: any}> }; $route: {query: Record<string,unknown>}; $kingzon_messager: {showInfo(message:string):void; showError(error: unknown): void; showConfim(message: string): Promise<void>}; $utils: {formatDateTime(value: string | Date, format: string): string} } }`)
    fs.writeFileSync(path.join(folder, 'modules.d.ts'), `declare namespace EasyUI {interface Dialog {open():void;close():void}} declare module '*.vue' {const component: any; export default component} declare module '@/types/types' {export class Search {constructor(...args:any[])}} declare module '@/views/*' {const component: any; export default component} declare module '@kingzon/base-pc' {export interface Filter {field: string; value: unknown}}`)
    const program = ts.createProgram([...files.map(name => path.join(folder, name + '.ts')), ...['host.d.ts', 'modules.d.ts', 'schedule.ts'].map(name => path.join(folder, name))], {
        strict: true, noEmit: true, skipLibCheck: true, target: ts.ScriptTarget.ES2020, module: ts.ModuleKind.ESNext,
        moduleResolution: ts.ModuleResolutionKind.Node10, baseUrl: folder,
        paths: { vue: [path.dirname(require.resolve('vue/package.json'))], '@/types/production-schedule': [path.join(folder, 'types/schedule.ts')], '@/types/*': [path.join(folder, 'types/*')] }
    })
    const errors = ts.getPreEmitDiagnostics(program)
    assert.equal(errors.length, 0, ts.formatDiagnosticsWithColorAndContext(errors, { getCurrentDirectory: () => folder, getCanonicalFileName: x => x, getNewLine: () => '\n' }))
})


test('adding template processes isolates configuration and keeps route order', async t => {
    const { vm } = mount(t, 'component/create-schedule.vue', { craftVersionId: 31, allowProcessEdit: true })
    await settle()
    const source=vm.processes[0]
    source.machines[0].users=[{userId:1,quantity:1,hours:2}]
    vm.addProcess(vm.template.processs[0]); const added=vm.currentProcess
    assert.notEqual(added.key,source.key); assert.notEqual(added.code,source.code)
    assert.notEqual(added.materials[0].key,source.materials[0].key)
    added.materials[0].quantity=99
    assert.equal(source.materials[0].quantity,2); assert.equal(source.machines[0].users[0].hours,2)
    assert.equal(vm.processes[0],source); assert.equal(added.machines[0].users.length,0)
    vm.moveProcess(-1,added.key);assert.equal(vm.processes[0],added)
    assert.equal(added.materials[0].quantity,99)
    vm.dragKey=added.key;vm.dropProcess(source.key);assert.equal(vm.processes[0],source)
    assert.equal(source.machines[0].users[0].hours,2)
    assert.deepEqual(model.productionSchedulePost(vm.form,vm.processes).processs.map(row=>row.order),[10,20])
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

test('material dialog emits close after cleanup and an old close cannot dismiss a reopened dialog', async t => {
    let cleaned=false
    const closes=[]
    const {vm,props}=mount(t,'component/schedule-material-dialog.vue',{
        processKey:'p',quantity:10,onClose:()=>closes.push(cleaned)
    })
    vm.$refs.dialog.close=()=>{
        nextTick(()=>{cleaned=true})
        vm.onClose()
        vm.onClose()
    }
    vm.open();vm.cancel()
    assert.equal(closes.length,0)
    await settle();assert.deepEqual(closes,[true])
    vm.open();const pending=vm.onClose();vm.open();await pending
    assert.equal(closes.length,1);assert.equal(vm.opened,true)
    props.saving=true;await settle();vm.cancel()
    assert.equal(vm.opened,true)
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
        calls.push({url,data}); return {data:[{dataType:1,dataId:100,stock:25,occupied:0,safetyStock:0}]}
    })
    await settle(); await vm.checkStock()
    assert.equal(calls[0].url, '/api/production/ProductionScheduleManage/GetMaterialStock')
    assert.deepEqual(calls[0].data, [{dataType:1,dataId:100}]); assert.equal(vm.summary.readyCount, 1)
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
    assert.deepEqual(JSON.parse(JSON.stringify(updates[0][1])),{quantity:0,hours:0,...selected[1]})
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

test('craft and flags stay read-only while equipment allocation remains editable', async t => {
    const {vm,props}=mount(t,'component/create-schedule.vue',{craftVersionId:31,allowProcessEdit:false})
    await settle()
    const process=vm.processes[0]
    vm.updateProcess(process,{isOutsource:true,isInbound:true})
    assert.equal(process.isOutsource,false);assert.equal(process.isInbound,false)
    props.allowProcessEdit=true;await settle()
    vm.updateProcess(process,{isOutsource:true,isInbound:true})
    assert.equal(process.isOutsource,false);assert.equal(process.isInbound,false)
    vm.updateProcess(process,{craftId:999,name:'changed',code:'changed',isReportWork:false,machines:[{machineId:8,users:[{userId:1,quantity:1,hours:3}]}]})
    assert.equal(process.craftId,7);assert.equal(process.name,'切割');assert.equal(process.isReportWork,true)
    assert.equal(process.machines[0].users[0].hours,3)
    await vm.openMaterials(process)
    assert.equal(vm.materialProcess,process)
    vm.closeMaterials();assert.equal(vm.materialProcess,undefined)
    props.saving=true;await settle()
    vm.updateProcess(process,{isInbound:false});await vm.openMaterials(process)
    assert.equal(process.isInbound,false);assert.equal(vm.materialProcess,undefined)
    vm.updateProcess(process,{machines:[]});assert.equal(process.machines.length,1)
})


test('outsource accepts template equipment without internal staff and preserves explicit personnel allocations', async t => {
    const source=template();source.processs[0].isOutsource=true
    const {vm}=mount(t,'component/create-schedule.vue',{craftVersionId:31,allowProcessEdit:true},async()=>({data:source}))
    await settle();vm.start=new Date(2026,9,29,8);vm.end=new Date(2026,9,29,18)
    assert.deepEqual(vm.getPostData().processs[0].machineRls.map(row => [row.machineId,row.userId,row.quantity]),[[8,0,0]])
    vm.currentProcess.machines[0].users=[{userId:1,quantity:1,hours:2}]
    assert.equal(vm.getPostData().processs[0].machineRls[0].hours,2)
    vm.updateProcess(vm.currentProcess,{isOutsource:false})
    assert.equal(vm.getPostData().processs[0].machineRls[0].hours,2)
    vm.currentProcess.machines[0].users=[]
    assert.equal(vm.currentProcess.isOutsource,true)
    assert.deepEqual(vm.getPostData().processs[0].machineRls.map(row => [row.machineId,row.userId,row.quantity]),[[8,0,0]])
})


test('process expansion starts collapsed and opens only the newly added process', async t => {
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
    vm.addProcess(vm.template.processs[0])
    const copied = vm.currentProcess
    assert.equal(vm.expandedKey, copied.key)
    assert.equal(vm.processes[0], first)
    assert.equal(vm.processes[1], copied)
})

test('process deletion respects permission and saving while summaries preserve resource quantities', async t => {
    const actions = []
    const { vm, props } = mount(t, 'component/schedule-process-card.vue', {
        process: new dto.ScheduleProcessEditDto({ name: '长工序名称'.repeat(30), machines: [
            { machineId: 8, users: [{ userId: 1, hours: 2, quantity: 1 }] },
            { machineId: 9, users: [{ userId: 1, hours: 3, quantity: 1 }] }
        ] }), index: 0, count: 2, quantity: 2, editable: true,
        onMove: step => actions.push(['move', step]), onCopy: () => actions.push(['copy']), onRemove: () => actions.push(['remove'])
    })
    vm.moveProcess(-1);vm.moveProcess(1);vm.removeProcess()
    assert.deepEqual(actions, [['move',1],['remove']])
    assert.equal(vm.userCount, 1); assert.equal(vm.allocationCount, 2); assert.equal(vm.humanHours, 5)
    props.saving = true; await settle(); vm.removeProcess()
    assert.equal(actions.length, 2)
    props.saving = false; props.editable = false; await settle(); vm.removeProcess()
    assert.equal(actions.length, 2)
})


test('schedule tabs create unique drafts, switch without discarding them, and submit only the active draft', async t => {
    const calls=[]
    const { vm, events } = mount(t, 'index.vue', { craftVersionId: 0 }, undefined, async(url,data)=>{calls.push({url,data});return {data:{id:4}}})
    vm.loadMaterialQuantities=async()=>{};vm.loadScheduleOrders=async()=>{}
    vm.rootScheduleId=10;vm.selectedCraftVersionId=31
    vm.nodes = [new dto.ScheduleNodeDto({id:'semi-1',kind:'semi',name:'成品',children:[],orders:[],serverMaterial:new dto.MaterialNodeDto({craftVersionId:32})})]
    vm.shortage = () => 100
    vm.openSchedule(vm.nodes[0]); const first = vm.currentDraft
    vm.openSchedule(vm.nodes[0]); const second = vm.currentDraft
    assert.notEqual(first.id, second.id); assert.equal(vm.scheduleListRows.length, 2)
    vm.selectOrder(first.id); assert.equal(vm.currentDraft.id, first.id)
    const value = new dto.ProductionSchedulePostDto({craftVersionId:32,quantity:12})
    await vm.submitDraft(second.id, value); assert.equal(events.length,0)
    await vm.submitDraft(first.id,value); assert.equal(events.length,1); assert.equal(events[0].quantity,12)
    assert.equal(calls[0].url,'/api/production/ProductionScheduleManage/CreateSchedule')
    assert.equal(vm.drafts.length,1)
    vm.openSchedule(vm.nodes[0]);const third=vm.currentDraft
    vm.$kingzon_messager.showConfim = async () => { throw Error('cancel') }
    await vm.cancelDraftRow(third.id); assert.equal(vm.drafts.length,2)
    vm.$kingzon_messager.showConfim = async () => {}
    await vm.cancelDraftRow(third.id); assert.equal(vm.drafts.length,1)
    assert.equal(vm.currentDraft.id,second.id)
})

test('closing an inactive draft leaves the active editor selected and stale confirmations cannot close new drafts', async t => {
    const { vm } = mount(t, 'index.vue', {craftVersionId:0})
    vm.rootScheduleId=10
    vm.nodes = [new dto.ScheduleNodeDto({id:'semi-1',kind:'semi',children:[],orders:[]})]
    vm.shortage = () => 100
    vm.openSchedule(vm.nodes[0]); const first = vm.currentDraft
    vm.openSchedule(vm.nodes[0]); const second = vm.currentDraft
    await vm.cancelDraftRow(first.id); assert.equal(vm.currentDraft.id,second.id)
    let confirm
    vm.$kingzon_messager.showConfim = () => new Promise(resolve => {confirm=resolve})
    const closing = vm.cancelDraftRow(second.id)
    vm.drafts=[]; vm.openSchedule(vm.nodes[0]); const newest=vm.currentDraft
    confirm(); await closing
    assert.equal(vm.drafts.length,1); assert.equal(vm.currentDraft.id,newest.id)
})

test('save failure retains draft and in-flight duplicate submits cannot create another order', async t => {
    let rejectSave, calls=0
    const {vm,events,errors}=mount(t,'index.vue',{craftVersionId:0},undefined,()=>{calls++;return new Promise((resolve,reject)=>{rejectSave=reject})})
    vm.rootScheduleId=10;vm.selectedCraftVersionId=21
    vm.nodes=[new dto.ScheduleNodeDto({id:'semi',kind:'semi',serverMaterial:new dto.MaterialNodeDto({craftVersionId:31})})]
    vm.shortage=()=>100;vm.openSchedule(vm.nodes[0]);const id=vm.currentDraft.id
    const value=new dto.ProductionSchedulePostDto({craftVersionId:31,quantity:1})
    const pending=vm.submitDraft(id,value)
    await vm.submitDraft(id,value);await vm.cancelDraftRow(id)
    assert.equal(calls,1);assert.equal(vm.drafts.length,1);assert.equal(vm.savingDraftId,id)
    rejectSave(new Error('save failed'));await pending
    assert.equal(vm.drafts.length,1);assert.equal(vm.savingDraftId,'');assert.equal(events.length,0);assert.equal(errors.length,1)
})

test('child schedules stay locked until a root with the exact plan quantity is saved', async t => {
    const calls=[],notices=[]
    const {vm}=mount(t,'index.vue',{craftVersionId:0},undefined,async(url,data)=>{calls.push({url,data});return {data:{id:calls.length===1?101:102}}})
    vm.$kingzon_messager.showInfo=message=>notices.push(message)
    vm.quantityResult=new dto.QuantityResultDto({craftVersionId:31,quantity:25})
    vm.selectedCraftVersionId=31;vm.quantity=25
    const child=new dto.ScheduleNodeDto({id:'child',kind:'semi',serverMaterial:new dto.MaterialNodeDto({craftVersionId:32})})
    vm.nodes=[child];vm.shortage=()=>25
    vm.openSchedule(child);assert.equal(vm.drafts.length,0);assert.equal(vm.canAddSchedule,false)
    vm.openRootSchedule();const rootDraft=vm.currentDraft
    vm.openRootSchedule();assert.equal(vm.drafts.length,1);assert.equal(vm.currentDraft.id,rootDraft.id)
    await vm.submitDraft(rootDraft.id,new dto.ProductionSchedulePostDto({craftVersionId:31,quantity:24}))
    assert.equal(calls.length,0);assert.equal(vm.rootScheduleId,0)
    vm.loadScheduleOrders=async()=>{}
    await vm.submitDraft(rootDraft.id,new dto.ProductionSchedulePostDto({craftVersionId:31,quantity:25}))
    assert.equal(vm.rootScheduleId,101);assert.equal(calls[0].data.rootCraftVersionId,31)
    assert.equal(calls[0].data.rootDispatchOrderId,null);assert.equal(vm.drafts.length,0)
    vm.openSchedule(child);assert.equal(vm.currentDraft.nodeId,'child')
    await vm.submitDraft(vm.currentDraft.id,new dto.ProductionSchedulePostDto({craftVersionId:32,quantity:5}))
    assert.equal(calls[1].data.rootDispatchOrderId,101);assert.equal(calls[1].data.rootCraftVersionId,31)
    assert.ok(notices.some(message=>message.includes('请先生成顶层')))
})

test('root editor keeps plan quantity read-only and validates it before submission', async t => {
    const {vm}=mount(t,'component/create-schedule.vue',{craftVersionId:31,initialQuantity:25,quantityReadOnly:true})
    await settle();assert.equal(vm.form.quantity,25)
    vm.start=new Date(2026,10,5,8);vm.end=new Date(2026,10,5,18)
    vm.form.quantity=24;await settle();assert.equal(vm.getPostData(),null)
    assert.ok(vm.errors.some(message=>message.includes('顶层排产数量必须等于计划数量')))
})

test('saved order requests cannot overwrite another selected material and root product can be scheduled without semi nodes', async t => {
    const pending=[]
    const {vm}=mount(t,'index.vue',{craftVersionId:0},(url,params)=>new Promise(resolve=>pending.push({params,resolve})))
    vm.quantityResult=new dto.QuantityResultDto({craftVersionId:31,quantity:10})
    vm.selectedCraftVersionId=31
    vm.product=new dto.ScheduleProductDto({name:'成品',code:'P1',unit:'kg'})
    vm.rootScheduleId=10;vm.openRootSchedule();assert.equal(vm.selectedSemi.name,'成品')
    const semi=new dto.ScheduleNodeDto({id:'semi',kind:'semi',serverMaterial:new dto.MaterialNodeDto({craftVersionId:32})})
    vm.nodes=[semi];vm.selectRow({...semi,source:semi})
    pending[1].resolve({data:[{id:2,orderNo:'NEW',craftVersionId:32}]});await settle()
    pending[0].resolve({data:[{id:1,orderNo:'OLD',craftVersionId:31}]});await settle()
    assert.equal(vm.savedOrders[0].orderNo,'NEW');assert.equal(vm.selectedId,'semi')
    assert.equal(vm.rootScheduleId,10)
})

test('stock check includes material newly selected outside template', async t => {
    const calls=[]
    const {vm}=mount(t,'component/create-schedule.vue',{craftVersionId:31},undefined,async(url,data)=>{calls.push(data);return {data:data.map(item=>({...item,stock:100,occupied:0,safetyStock:0}))}})
    await settle()
    vm.processes[0].materials.push(new dto.ScheduleMaterialEditDto({key:'new',dataType:1,dataId:999,quantity:2,unitId:1,unitConversionId:0,unitOptions:[{id:0,unitId:1,ratio:1}]}))
    await vm.checkStock();assert.ok(calls[0].some(item=>item.dataId===999))
    assert.equal(vm.availability[`${vm.processes[0].key}/new`].shortage,0)
})

test('rescaling tiny schedule quantities does not generate negative person quantities', async t => {
    const {vm}=mount(t,'component/create-schedule.vue',{craftVersionId:31,initialQuantity:5})
    await settle()
    vm.processes[0].machines[0].users=Array.from({length:5},(_,index)=>({userId:index+1,quantity:1,hours:0}))
    vm.form.quantity=0.0003;await settle()
    const values=vm.processes[0].machines[0].users.map(user=>user.quantity)
    assert.ok(values.every(value=>value>=0));assert.equal(Number(values.reduce((sum,value)=>sum+value,0).toFixed(4)),0.0003)
})

test('page templates compile and tabs keep editors mounted by their independent draft keys', () => {
    const filename=path.join(root,'index.vue'),source=fs.readFileSync(filename,'utf8')
    const {descriptor,errors}=parse(source,{filename}); assert.deepEqual(errors,[])
    assert.deepEqual(compileTemplate({source:descriptor.template.content,filename,id:'schedule-page'}).errors,[])
    assert.match(source,/<KeepAlive :key="`\$\{selectedCraftVersionId\}:\$\{rootScheduleId\}`">/)
    assert.doesNotMatch(source,/<CreateSchedule|<template #draft=/)
    assert.match(source,/@submitDraft="submitDraft"/)
    assert.doesNotMatch(descriptor.template.content,/v-model="(?:quantityInput|startInput|endInput)"/)
    const list=fs.readFileSync(path.join(root,'component/schedule-order-list.vue'),'utf8')
    assert.match(list,/<Tabs /); assert.match(list,/<TabPanel v-for=[\s\S]*?<CreateSchedule v-if="row.draft" :key="row.id"[\s\S]*?<\/TabPanel>/)
    assert.doesNotMatch(list,/productionScheduleOrderGrid/)
})


test('TabPanel directly owns separate editor instances across tabs and cached material selections', async t => {
    const { KeepAlive } = require('vue')
    const filename = path.join(root, 'component/schedule-order-list.vue')
    const descriptor = parse(fs.readFileSync(filename,'utf8'),{filename}).descriptor
    const compiled = compileTemplate({source:descriptor.template.content,filename,id:'schedule-order-tabs'})
    assert.deepEqual(compiled.errors,[])
    const module = {exports:{}}
    const code = ts.transpileModule(compiled.code,{compilerOptions:{module:ts.ModuleKind.CommonJS,target:ts.ScriptTarget.ES2020}}).outputText
    new Function('require','module','exports',code)(require,module,module.exports)
    const instances=new Map(),requests=[]
    const Editor = {...load(path.join(root,'component/create-schedule.vue')).default,render:()=>null,
        mounted(){ instances.set(this.$.vnode.key,this) }}
    const options=load(filename).default
    const List = {...options,components:{...options.components,CreateSchedule:Editor},render:module.exports.render}
    const state=reactive({node:'A',selected:'draft-A1',rows:{A:[new dto.ScheduleListRowDto({id:'draft-A1',draft:true}),new dto.ScheduleListRowDto({id:'draft-A2',draft:true})],B:[new dto.ScheduleListRowDto({id:'draft-B1',draft:true})]}})
    const nodes={A:new dto.ScheduleNodeDto({id:'A',name:'甲',code:'A',orders:[],serverMaterial:{craftVersionId:31}}),B:new dto.ScheduleNodeDto({id:'B',name:'乙',code:'B',orders:[],serverMaterial:{craftVersionId:32}})}
    const app=renderer.createApp({render(){
        const node=nodes[state.node], rows=state.rows[state.node]
        return h(KeepAlive,null,{default:()=>h(List,{key:node.id,node,dataList:rows,selectedRow:rows.find(row=>row.id===state.selected),shortage:100})})
    }})
    const panel={render(){return h('div',this.$slots.default?.())}}
    app.component('Tabs',{...panel,methods:{select(){}}})
    for (const name of ['TabPanel','LinkButton','KingZonDataGrid','GridColumn']) app.component(name,panel)
    app.config.globalProperties.$http={get:async(url,params)=>{requests.push(params.craftVersionId);return {data:template(params.craftVersionId)}},post:async()=>({data:{materials:[]}})}
    app.config.globalProperties.$utils={formatDateTime:value=>value}
    app.config.globalProperties.$kingzon_messager={showError(error){throw error},showConfim:async()=>{}}
    app.mount({});t.after(()=>app.unmount());await settle()
    const first=instances.get('draft-A1'),second=instances.get('draft-A2')
    assert.ok(first);assert.ok(second);assert.notEqual(first,second)
    first.form.quantity=12;first.form.remark='第一张';second.form.quantity=24;await settle()
    state.selected='draft-A2';await settle()
    assert.equal(instances.get('draft-A1'),first);assert.equal(first.form.quantity,12)
    state.node='B';state.selected='draft-B1';await settle()
    const third=instances.get('draft-B1');assert.equal(third.form.craftVersionId,32)
    assert.equal(first.form.craftVersionId,31);assert.equal(first.form.remark,'第一张')
    state.node='A';state.selected='draft-A1';await settle()
    assert.equal(instances.get('draft-A1'),first);assert.equal(first.form.quantity,12)
    assert.equal(second.form.quantity,24);assert.deepEqual(requests,[31,31,32])
    state.rows.A=state.rows.A.filter(row=>row.id!=='draft-A1');await settle()
    assert.equal(first.$.isUnmounted,true);assert.equal(second.$.isUnmounted,false)
    assert.equal(third.$.isUnmounted,false)
})


test('equipment-only allocation saves quantity and hours and transfers them to personnel without duplicate totals', async t => {
    const {vm,updates}=mount(t,'component/schedule-machine.vue',{
        modelValue:[new dto.ScheduleMachineEditDto({machineId:8,quantity:10,hours:6})],
        machineOptions:template().machines,quantity:10
    })
    assert.equal(vm.rows[0].hours,6)
    vm.updateUser({machineId:8,userId:0},'hours',8)
    assert.equal(updates.at(-1)[0].hours,8)
    vm.openSelection(8);vm.toggleUser(8,1,true);vm.toggleUser(8,2,true);vm.confirmSelection()
    const assigned=updates.at(-1)[0]
    assert.equal(assigned.users.reduce((n,u)=>n+u.quantity,0),10)
    assert.equal(assigned.users.reduce((n,u)=>n+u.hours,0),6)
    const form=new dto.ProductionSchedulePostDto({craftVersionId:31,quantity:10,start:'2026-11-06T08:00:00',end:'2026-11-06T18:00:00'})
    const process=new dto.ScheduleProcessEditDto({code:'010',name:'切割',craftId:7,machines:[new dto.ScheduleMachineEditDto({machineId:8,quantity:10,hours:6})]})
    const post=model.productionSchedulePost(form,[process])
    assert.equal(post.processs[0].machineRls[0].userId,0)
    assert.equal(post.processs[0].machineRls[0].hours,6)
    assert.deepEqual(model.validateProductionSchedule(post,template()),[])
    assert.equal(model.scheduleEditorSummary([process],{}).humanHours,6)
})


test('compact root editor copies detached process configuration and separates displayed resource hours', async t => {
    const {vm}=mount(t,'component/create-schedule.vue',{craftVersionId:31,initialQuantity:10,quantityReadOnly:true,allowProcessEdit:true})
    await settle()
    const source=vm.processes[0]
    source.machines=[new dto.ScheduleMachineEditDto({machineId:8,quantity:10,hours:3})]
    vm.copyProcess(source)
    assert.equal(vm.processes.length,2)
    const copy=vm.processes[1]
    assert.notEqual(copy.key,source.key);assert.notEqual(copy.code,source.code)
    assert.notEqual(copy.materials[0].key,source.materials[0].key)
    copy.machines[0].hours=7
    assert.equal(source.machines[0].hours,3)
    assert.deepEqual(vm.dispatchHours,{machine:10,person:0})
    vm.toggleProcess(source.key);assert.equal(vm.expandedKey,source.key)
    vm.toggleProcess(copy.key);assert.equal(vm.expandedKey,copy.key)
    assert.equal(vm.form.quantity,10)
})
