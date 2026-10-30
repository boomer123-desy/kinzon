const assert = require('node:assert/strict')
const fs = require('node:fs')
const path = require('node:path')
const os = require('node:os')
const { test } = require('node:test')
const root = path.resolve(__dirname, '../../../../..')
const dependencies = path.join(root, 'frontend/technical/production_schedule_manage/node_modules')
const ts = require(path.join(dependencies, 'typescript'))
const { parse, compileScript, compileTemplate } = require(path.join(dependencies, '@vue/compiler-sfc'))
const file = path.resolve(__dirname, '../material-demand-by-bom.vue')
const dtoFile = path.join(root, 'frontend/production/types/production.ts')
const descriptor = parse(fs.readFileSync(file, 'utf8'), { filename: file }).descriptor
// Missing host enums/common controls are test doubles; production DTO and page are actual source.
const enumSource = `export enum BomDetailType { Product=1, Formula=2, Specication=3, Bom=4 }
export enum BomStatus { 待审核=1, 已生效=2, 淘汰=3 }
export enum InventoryDataStatus { None=0, 正常=1, 异常=2 }
export enum InventoryDataType { Product=1, Specification=2 }
export enum ProductType { 原材料=0, 半成品=1, 成品=2 }
export enum DispatchOrderStatus { 进行中=1 }
export enum ReviewStatus { 评审通过=2 }
export enum UnitConversionType { Product=1 }
export enum FormulaDetailType { Product=1, Formula=2 }
export enum SaleOrderType { 普通=1 }
${fs.readFileSync(path.join(root, 'frontend/production/types/enums.ts'), 'utf8')}`
const sharedSource = `export interface SelectOption { value: number | string; text: string; code?: unknown; extension?: string; isMainUnit?: boolean; ratio?: number }
export class ShowFormData { constructor(public label: string, public value: string | number) {} }
export interface UploadFile { id?: number }
export interface CustomAttribute { systemParaId: number; value: string }`
function evaluate(source, dependency) {
  const compiled = ts.transpileModule(source, { compilerOptions: { target: ts.ScriptTarget.ES2020, module: ts.ModuleKind.CommonJS } })
  const module = { exports: {} }
  new Function('require', 'module', 'exports', compiled.outputText)(dependency, module, module.exports)
  return module.exports
}
const enums = evaluate(enumSource)
const types = evaluate(sharedSource)
const models = evaluate(fs.readFileSync(dtoFile, 'utf8'), () => enums)
const options = evaluate(descriptor.script.content, name => {
  if (name === 'vue') return { defineComponent: value => value }
  if (name === 'lodash') return require(path.join(dependencies, 'lodash'))
  if (name === '@/types/production') return models
  if (name === '@/types/types') return types
  if (name === '@/types/enums') return enums
  if (name === './select-material.vue') return { default: {} }
  throw new Error(name)
}).default
function fixture() {
  const current = new models.BomDemandCurrentRowDto({ productId: 1, productNo: 'P1', productName: '产品', dispatchQuantity: 6, mainQuantity: 0,
    dispatchUnitInfo: { baseUnitId: 100, mainUnitId: 100, mainRatio: 4, productionUnitId: 300, unitConversionId: 30, productionUnitName: '包', baseRatio: 0 } })
  const bom = new models.BomIndexDto({ versionId: 501, bomId: 50, version: 'V2', isMain: true, totalQuantity: 2,
    unitConversionId: 20, unitInfoList: [{ id: -1, unitId: 100, unitName: '件', ratio: 1 }, { id: 20, unitId: 200, unitName: '箱', ratio: 10 }, { id: 30, unitId: 300, unitName: '包', ratio: 4 }] })
  const main = new models.BomDetailDto({ id: 10, detailType: enums.BomDetailType.Product, detailId: 41, detailProductType: 0,
    detailDataNo: 'M41', detailDataName: '主料', detailDataUnitId: 100, detailDataUnit: '件', unit: '件',
    unitConversionId: -1, curQuantity: 24, curBaseQuantity: 2.4,
    replaceData: [new models.BomDetailDto({ id: 10, detailType: enums.BomDetailType.Specication, detailId: 421,
      detailWithProductDto: { productId: 42, specificationId: 421, productNo: 'S42', productName: '替料' },
      detailDataUnitId: 101, detailDataUnit: 'kg', unit: 'kg', unitConversionId: -1, curQuantity: 2 })] })
  const units = [
    { productId: 41, idField: '41', unitList: [
      { code: '-1', value: '1', text: '件', unitId: 100, isMainUnit: true },
      { code: '9', value: '12', text: '箱', unitId: 200, isMainUnit: false }] },
    { productId: 42, specificationId: 421, idField: '42_421', unitList: [
      { code: '-1', value: '1', text: 'kg', unitId: 101, isMainUnit: false },
      { code: '8', value: '0.2', text: '件', unitId: 100, isMainUnit: true }] },
    { productId: 70, idField: '70', unitList: [{ code: '-1', value: '1', text: '件', unitId: 100, isMainUnit: true }] }
  ]
  return { current, bom, main, units }
}
function instance(source = fixture(), override) {
  const errors = [], events = [], requests = []
  const request = async (url, params) => {
    requests.push({ url, params })
    if (override) {
      const result = override(url, params)
      if (result) return result
    }
    if (url.endsWith('GetProductionBomOptions')) return { data: [source.bom] }
    if (url.endsWith('GetAllSupplier')) return { data: [{ id: 8, customerName: '供应商' }] }
    if (url.endsWith('GetProductionBomDataById')) return { data: { versionId: 501, detailList: [source.main] } }
    if (url.endsWith('GetProductUnitList')) return { data: source.units }
    throw new Error(url)
  }
  const view = { ...options.data(), currentRow: source.current,
    $http: { get: request, post: request }, $kingzon_messager: { showError: message => errors.push(message) },
    $emit: (...args) => events.push(args), $nextTick: callback => Promise.resolve().then(callback),
    $refs: { seleteMaterial: { expand() {}, collapse() {} } },
    $utils: { getEnumKeyValueArray: value => Object.entries(value).filter(([key]) => isNaN(Number(key))).map(([text, value]) => ({ text, value })) }
  }
  for (const [key, method] of Object.entries(options.methods)) view[key] = method.bind(view)
  for (const [key, method] of Object.entries(options.computed)) Object.defineProperty(view, key, { get: method.bind(view) })
  return { view, errors, events, requests, source }
}

test('actual BOM page, submit methods and full production DTO pass strict type checks with host doubles', t => {
  assert.deepEqual(compileTemplate({ source: descriptor.template.content, filename: file, id: file }).errors, [])
  const folder = fs.mkdtempSync(path.join(os.tmpdir(), 'bom-demand-types-'))
  t.after(() => fs.rmSync(folder, { recursive: true, force: true }))
  fs.writeFileSync(path.join(folder, 'page.ts'), compileScript(descriptor, { id: file }).content)
  fs.copyFileSync(dtoFile, path.join(folder, 'production.ts'))
  fs.writeFileSync(path.join(folder, 'enums.ts'), enumSource)
  fs.writeFileSync(path.join(folder, 'types.ts'), sharedSource)
  fs.writeFileSync(path.join(folder, 'common.ts'), 'export interface FileStorage {} export interface SystemParaTreeDto {}')
  fs.writeFileSync(path.join(folder, 'shims.d.ts'), "declare module 'lodash' { export function cloneDeep<T>(value: T): T } declare module '*.vue' { const component: import('vue').DefineComponent; export default component }")
  fs.writeFileSync(path.join(folder, 'host.d.ts'), `import 'vue'; import type { SelectOption } from './types'; declare module 'vue' { interface ComponentCustomProperties {
    $http: { get(url: string, params?: unknown): Promise<{data: any}>; post(url: string, params: unknown): Promise<{data: any}> };
    $utils: { getEnumKeyValueArray(value: object): SelectOption[] }; $kingzon_messager: { showError(value: unknown): void }
  } } declare global { namespace EasyUI { interface Drawer { expand(): void; collapse(): void } } }`)
  const parentFile = path.resolve(__dirname, '../add-dispatch.vue')
  const parentScript = parse(fs.readFileSync(parentFile, 'utf8'), { filename: parentFile }).descriptor.script.content
  const ast = ts.createSourceFile('add-dispatch.ts', parentScript, ts.ScriptTarget.Latest, true, ts.ScriptKind.TS)
  const required = new Set(['submit', 'getPostData', 'buildPostMaterialDemand', 'isBomMaterialDemands', 'bomDemandChanged', 'updateBomDemandQuantity'])
  const actualMethods = []
  function visit(node) {
    if (ts.isPropertyAssignment(node) && node.name.getText(ast) === 'methods' && ts.isObjectLiteralExpression(node.initializer)) {
      for (const method of node.initializer.properties) {
        if (ts.isMethodDeclaration(method) && required.has(method.name.getText(ast))) actualMethods.push(method.getText(ast))
      }
    }
    ts.forEachChild(node, visit)
  }
  visit(ast)
  assert.equal(actualMethods.length, required.size, 'type check must include every actual submit method')
  // Only host-provided component state/messages are declared here; all mapping method bodies are actual source.
  fs.writeFileSync(path.join(folder, 'submit-check.ts'), `
    import { cloneDeep } from 'lodash';
    import { PostDispatchOrderDto, PostMaterialDemandDetails, type DispatchOrderEditDto,
      type FormulaMaterialDemandsDto, type BomMaterialDemandsDto, type BomDemandDetailDto } from './production';
    import { DispatchBasisType, FormulaDetailType, InventoryDataStatus, InventoryDataType, ProductType } from './enums';
    class SubmitCheck {
      orderList: DispatchOrderEditDto[] = [];
      $kingzon_messager!: { showError(value: unknown): void; showConfim(value: string): Promise<void> };
      submitData(_data: PostDispatchOrderDto[]) {}
      ${actualMethods.join('\n')}
    }
  `)
  const selectorFile = path.resolve(__dirname, '../select-saleOrder.vue')
  const selectorScript = parse(fs.readFileSync(selectorFile, 'utf8'), { filename: selectorFile }).descriptor.script.content
  const selectorAst = ts.createSourceFile('selector.ts', selectorScript, ts.ScriptTarget.Latest, true, ts.ScriptKind.TS)
  let selectorProps
  function readProps(node) {
    if (ts.isPropertyAssignment(node) && node.name.getText(selectorAst) === 'props') selectorProps = node.getText(selectorAst)
    ts.forEachChild(node, readProps)
  }
  readProps(selectorAst)
  assert.ok(selectorProps)
  fs.writeFileSync(path.join(folder, 'props-check.ts'), `
    import { defineComponent } from 'vue';
    import { GetDispatchOrderDto, PostDispatchOrderDto, DispatchOrderEditDto, SaleOrderDetailDispatchDTO } from './production';
    const Selector = defineComponent({ ${selectorProps} });
    const editingRow = new DispatchOrderEditDto();
    const props: InstanceType<typeof Selector>['$props'] = { currentRow: editingRow, selectedRows: editingRow.saleOrders };
  `)
  const program = ts.createProgram(['page.ts', 'production.ts', 'submit-check.ts', 'props-check.ts', 'host.d.ts', 'shims.d.ts'].map(name => path.join(folder, name)), {
    strict: true, skipLibCheck: true, noEmit: true, target: ts.ScriptTarget.ES2020, module: ts.ModuleKind.ESNext,
    moduleResolution: ts.ModuleResolutionKind.Node10, baseUrl: folder,
    paths: { vue: [path.join(dependencies, 'vue')], lodash: [path.join(dependencies, '@types/lodash')],
      '@/types/production': ['./production.ts'], '@/types/enums': ['./enums.ts'], '@/types/types': ['./types.ts'] }
  })
  const diagnostics = ts.getPreEmitDiagnostics(program)
  assert.equal(diagnostics.length, 0, ts.formatDiagnosticsWithColorAndContext(diagnostics, { getCurrentDirectory: () => folder, getCanonicalFileName: file => file, getNewLine: () => '\n' }))
})

test('main BOM loads with converted output quantity, physical units and stable distinct tree IDs', async () => {
  const { view, requests, errors } = instance()
  await view.init()
  assert.equal(errors.length, 0)
  assert.equal(view.loadedBomVersionId, 501)
  assert.equal(view.bomQuantity, 2.4)
  assert.equal(requests.find(item => item.url.endsWith('GetProductionBomDataById')).params.quantity, 2.4)
  const [main, sub] = view.allRows()
  assert.equal(main.id, sub.id)
  assert.notEqual(main.indexField, sub.indexField)
  assert.equal(main.isDemand, true)
  assert.equal(sub.isDemand, false)
  assert.equal(main.demandQuantity, 24)
  assert.equal(main.baseDemandQuantity, 24)
  assert.equal(sub.productId, 42)
  assert.equal(sub.specificationId, 421)
  assert.equal(sub.dataId, 421)
  assert.equal(sub.dataType, enums.InventoryDataType.Specification)
  assert.equal(view.typeText(main), '原材料', 'zero product type is preserved')
  view.submitData.bomUnitConversionId = -1
  view.changeBomUnit()
  assert.equal(view.bomQuantity, 24)
  assert.equal(main.demandQuantity, 24, 'changing displayed BOM unit does not scale material demand twice')
})

test('unit changes preserve physical demand, manual quantities update outbound snapshots', async () => {
  const { view } = instance()
  await view.init()
  const row = view.materialDetails[0]
  row.unitConversionId = 9
  view.changeDemandUnit(row)
  assert.equal(row.demandQuantity, 2)
  assert.equal(row.baseDemandQuantity, 24)
  row.demandQuantity = 3
  view.updateUnitInfo(row)
  assert.equal(row.baseDemandQuantity, 36)
  assert.equal(row.convUnitInfo.find(unit => unit.id === -1).outboundQuantity, 36)
  assert.equal(row.convUnitInfo.find(unit => unit.id === 9).outboundQuantity, 3)
})

test('alternatives are exclusive, unselected materials excluded, corrupted multiple selection rejected', async () => {
  const { view, errors, events } = instance()
  await view.init()
  const main = view.materialDetails[0], sub = main.children[0]
  sub.isDemand = true
  view.changeDemandStatus(sub)
  await view.$nextTick()
  assert.equal(main.isDemand, false)
  assert.equal(sub.isDemand, true)
  await view.submit()
  assert.equal(events[0][0], 'submit')
  assert.equal(events[0][1].materials.length, 1)
  assert.equal(events[0][1].materials[0].dataId, 421)
  main.isDemand = true
  await view.submit()
  assert.equal(events.length, 1)
  assert.ok(errors.at(-1).includes('只能领用一种'))
})

test('other materials retain edits, restore picker state, delete and survive BOM clearing', async () => {
  const { view } = instance()
  await view.init()
  const other = Object.assign(new models.GetProductListByProductionDto(), { id: 700, productId: 70, productNo: 'O70',
    productName: '其他料', productType: 0, productUnitId: 100, productUnit: '件' })
  await view.getOtherMaterials([other])
  let row = view.allRows().find(item => item.isOther)
  row.demandQuantity = 7
  row.detailRemark = '手工需求'
  await view.getOtherMaterials([other])
  row = view.allRows().find(item => item.isOther)
  assert.equal(row.demandQuantity, 7)
  assert.equal(row.detailRemark, '手工需求')
  assert.equal(view.otherMaterials[0].id, 700)
  view.clearBom()
  assert.equal(view.materialDetails.length, 1)
  assert.equal(view.materialDetails[0].isGroup, true)
  view.deleteRow(row)
  assert.equal(view.materialDetails.length, 0)
  assert.equal(view.otherMaterials.length, 0)
})

test('saved drafts are isolated from parent, tree parent cycles are removed from submit payload', async () => {
  const first = instance()
  await first.view.init()
  const row = first.view.materialDetails[0]
  row.demandQuantity = 30
  row.children[0].parent = row
  await first.view.submit()
  const draft = first.events[0][1]
  assert.doesNotThrow(() => JSON.stringify(draft))
  const source = fixture()
  source.current.materialDemands = draft
  const restored = instance(source)
  await restored.view.init()
  assert.equal(restored.view.materialDetails[0].demandQuantity, 30)
  restored.view.materialDetails[0].demandQuantity = 50
  assert.equal(source.current.materialDemands.materialDetails[0].demandQuantity, 30)
})

test('empty BOM list supports manual materials; missing/invalid unit ratios block false success', async () => {
  const source = fixture()
  source.units[0].unitList[0].value = '0'
  const { view, events, errors } = instance(source)
  await view.init()
  assert.ok(view.materialDetails[0].unitError)
  await view.submit()
  assert.equal(events.length, 0)
  assert.ok(errors.at(-1).includes('单位'))
  const empty = instance(fixture(), url => url.endsWith('GetProductionBomOptions') ? Promise.resolve({ data: [] }) : undefined)
  await empty.view.init()
  assert.equal(empty.view.isLoading, false)
  assert.equal(empty.view.materialDetails.length, 0)
  await empty.view.submit()
  assert.equal(empty.events.length, 0)
})

test('query failure keeps visible draft and blocks submitting it as a newly selected BOM', async () => {
  const { view, errors, events } = instance()
  await view.init()
  view.boms.push(new models.BomIndexDto({ ...view.boms[0], versionId: 502 }))
  view.submitData.bomVersionId = 502
  view.$http.get = async () => { throw new Error('查询失败') }
  await view.getBomData()
  assert.equal(view.materialDetails[0].detailDataNo, 'M41')
  assert.equal(view.loadedBomVersionId, 501)
  assert.equal(view.isLoading, false)
  await view.submit()
  assert.equal(events.length, 0)
  assert.equal(errors.length, 2)
})

test('child BOM references are resolved to physical material IDs instead of submitted as version IDs', async () => {
  const source = fixture()
  source.main.detailType = enums.BomDetailType.Bom
  source.main.detailId = 88001
  source.main.detailWithProductDto = { productId: 41, productNo: 'M41', productName: '子清单物料' }
  const { view, events } = instance(source)
  await view.init()
  await view.submit()
  const material = events[0][1].materials[0]
  assert.equal(material.detailId, 88001, 'source reference is retained')
  assert.equal(material.dataId, 41, 'inventory consumption uses physical material ID')
  assert.equal(material.productId, 41)
})

test('invalid BOM totals block queries, empty manual drafts never create NaN outbound quantities', async () => {
  const source = fixture()
  source.bom.totalQuantity = 0
  const { view, requests, errors } = instance(source)
  await view.init()
  assert.equal(requests.filter(item => item.url.endsWith('GetProductionBomDataById')).length, 0)
  assert.ok(errors.length)
  const row = new models.BomDemandDetailDto({ isOther: true, unitConversionId: -1, demandRatio: 1,
    unitList: [new models.BomDemandUnitDto({ id: -1, unitId: 100, ratio: 1 })] })
  view.updateUnitInfo(row)
  assert.equal(row.convUnitInfo[0].outboundQuantity, 0)
})

test('a late previous-product response cannot overwrite a new current row', async () => {
  let finishOld
  const { view } = instance(fixture(), (url, params) => url.endsWith('GetProductionBomOptions') && params.productId === 1
    ? new Promise(resolve => { finishOld = resolve }) : undefined)
  const old = view.init()
  view.currentRow = new models.BomDemandCurrentRowDto({ productId: 2 })
  view.$http.get = async url => ({ data: url.endsWith('GetProductionBomOptions') ? [] : [] })
  await view.init()
  finishOld({ data: [fixture().bom] })
  await old
  assert.equal(view.boms.length, 0)
  assert.equal(view.materialDetails.length, 0)
  assert.equal(view.isLoading, false)
})

function loadDispatchPage(name) {
  const filename = path.resolve(__dirname, `../${name}.vue`)
  const parsed = parse(fs.readFileSync(filename, 'utf8'), { filename }).descriptor
  assert.deepEqual(compileTemplate({ source: parsed.template.content, filename, id: filename }).errors, [])
  compileScript(parsed, { id: filename })
  return evaluate(parsed.script.content, dependency => {
    if (dependency === 'vue') return { defineComponent: value => value }
    if (dependency === 'lodash') return require(path.join(dependencies, 'lodash'))
    if (dependency.startsWith('@/types/production')) return models
    if (dependency === '@/types/enums') return enums
    if (dependency === '@/types/types') return types
    if (dependency.endsWith('.vue') || dependency === 'decimal.js') return { default: {} }
    throw new Error(dependency)
  }).default
}

test('BOM submission uses the material branch and fills basis identity without formula unit fields', async () => {
  const bom = instance()
  await bom.view.init()
  await bom.view.submit()
  const demand = bom.events[0][1]
  assert.equal(demand.basisType, enums.DispatchBasisType.物料清单)
  assert.equal(demand.basisId, 501)
  assert.equal(demand.bomVersionId, 501)
  assert.equal(demand.baseUnitInfo, undefined)
  const { view, requests, errors } = parentInstance()
  view.getMaterialDemands(demand)
  assert.equal(view.orderList[0].basisType, enums.DispatchBasisType.物料清单)
  view.submit()
  const posted = requests[0][0]
  assert.deepEqual(errors, [])
  assert.equal(posted.basisId, 501, 'BOM version ID, not master ID 50')
  assert.equal(posted.materialDemands.materials[0].dataId, 41)
  assert.equal(posted.materialDemands.materials[0].baseDemandQuantity, 24)
  assert.equal(posted.materialDemands.materials[0].demandQuantity, 24)
  assert.equal(posted.materialDemands.formulaVersionId, undefined)
  assert.equal(posted.materialDemands.formulaUnitInfo, undefined)
  assert.equal(posted.materialDemands.bomVersionId, undefined, 'editor draft must not be sent')
  assert.equal(posted.materialDemands.materialDetails, undefined)
  assert.equal(posted.baseUnitInfo, undefined)
  assert.equal(view.orderList[0].materialDemands, demand, 'serialization must retain the original editable draft')
  assert.ok(view.isBomMaterialDemands(view.orderList[0].materialDemands))
  // Substitute consumption must preserve specification identity, not the source detail/version ID.
  bom.view.materialDetails[0].children[0].isDemand = true
  bom.view.changeDemandStatus(bom.view.materialDetails[0].children[0])
  await Promise.resolve()
  await bom.view.submit()
  view.getMaterialDemands(bom.events.at(-1)[1])
  view.submit()
  assert.equal(requests.at(-1)[0].materialDemands.materials[0].dataId, 421)
  assert.equal(requests.at(-1)[0].materialDemands.materials[0].dataType, enums.InventoryDataType.Specification)
  const row = view.orderList[0]
  row.dispatchQuantity = 12
  view.onChangeDispatchQuantity(row)
  const previousRequests = requests.length
  view.submit()
  assert.equal(requests.length, previousRequests + 1)
  assert.equal(row.materialDemands.dispatchQuantity, 12)
  assert.equal(view.bomDemandChanged(row), false)
  view.materialDemand(row, 0, true)
  assert.equal(view.currentBomRow.dispatchQuantity, 12)
  assert.equal(view.currentBomRow.materialDemands, row.materialDemands)
})

function parentInstance() {
  const parent = loadDispatchPage('add-dispatch')
  const requests = [], errors = []
  const drawer = { expand() {}, collapse() {} }
  const view = { ...parent.data(), orderList: [new models.DispatchOrderEditDto({ productId: 1, departmentId: 2,
    dispatchQuantity: 6, mainQuantity: 0, incomeQuantity: 6, unitList: [],
    dispatchUnitInfo: { productionUnitId: 300, unitConversionId: 30, incomeUnitId: 300, incomeRatio: 1, baseUnitId: 100, baseRatio: 0, mainUnitId: 100, mainRatio: 0 } })],
    $kingzon_messager: { showError: error => errors.push(error), showConfim: () => Promise.resolve() },
    $nextTick: callback => Promise.resolve().then(callback), $refs: { materialDemand: drawer }, materailDemandDrawer: drawer }
  for (const [key, method] of Object.entries(parent.methods)) view[key] = method.bind(view)
  view.submitData = data => requests.push(JSON.parse(JSON.stringify(data)))
  return { view, requests, errors }
}

test('initialization restores income pricing for a preselected department and production unit', async () => {
  const { view, errors } = parentInstance()
  view.selectList = [new models.DispatchOrderEditDto({ productId: 1, departmentId: 2, dispatchQuantity: 6,
    dispatchUnitInfo: { productionUnitId: 100, unitConversionId: -1, baseUnitId: 100, mainUnitId: 100 } })]
  view.getDepartmentList = async () => ({ data: [{ productId: 1, value: 2, id: 88, price: 5,
    unitId: 100, unitName: '件', priceUnitConversionId: -1 }] })
  view.getTotalUnitList = async () => ({ data: [{ idField: '1', unitList: [
    { code: '-1', unitId: 100, text: '件', isMainUnit: true },
    { code: '20', unitId: 200, text: '箱', isMainUnit: false }
  ] }] })
  view.getProductPackageList = async () => ({ data: [] })
  view.getExtendParamData = async () => ({ data: [] })
  view.getUnitConversition = async () => ({ data: [] })
  await view.init()
  assert.deepEqual(errors, [])
  const row = view.orderList[0]
  assert.equal(row.processPriceId, 88)
  assert.equal(row.dispatchUnitInfo.incomeUnitId, 100)
  assert.equal(row.dispatchUnitInfo.incomeRatio, 1)
  assert.equal(row.incomeQuantity, 6)
  assert.equal(row.mainQuantity, 6)
})

test('income validation distinguishes missing pricing unit from invalid converted quantity', () => {
  const { view, errors, requests } = parentInstance()
  const row = view.orderList[0]
  row.dispatchUnitInfo.incomeUnitId = undefined
  view.submit()
  assert.match(errors.at(-1), /收入计价单位/)
  row.dispatchUnitInfo.incomeUnitId = 200
  for (const value of [0, -1, NaN, Infinity]) {
    row.incomeQuantity = value
    view.submit()
    assert.match(errors.at(-1), /收入计价数量无效/)
  }
  assert.equal(requests.length, 0)
})

test('formula submission keeps its original unit fields and fills formula basis identity', () => {
  const child = loadDispatchPage('material-demand')
  const events = []
  const childView = { ...child.data(), currentRow: { dispatchQuantity: 6 },
    materialDetails: [{ detailType: enums.FormulaDetailType.Product, detailId: 41, isDemand: true,
      children: [], demandQuantity: 12, baseDemandQuantity: 12, unitId: 100, formulaUnitId: 200,
      formulaQuantity: 3, formulaConversionId: 9, convRatio: 4, convUnitInfo: [], supplierId: 0 }],
    submitData: { formulaVersionId: 501, formulaUnitConversionId: 20 },
    formulaUnitInfo: { extension: '200', value: '20', text: 'kg' }, convTotalQuantity: 24, convRatio: 4,
    $emit: (...args) => events.push(args), $kingzon_messager: { showError: error => { throw new Error(error) } } }
  for (const [key, method] of Object.entries(child.methods)) childView[key] = method.bind(childView)
  childView.submit()
  const { view, requests, errors } = parentInstance()
  view.getMaterialDemands(events[0][1])
  view.submit()
  assert.deepEqual(errors, [])
  const order = requests[0][0]
  assert.equal(order.basisType, enums.DispatchBasisType.配方)
  assert.equal(order.basisId, 501)
  assert.equal(order.materialDemands.formulaVersionId, 501)
  assert.equal(order.materialDemands.formulaQuantity, 24)
  assert.equal(order.materialDemands.formulaUnitId, 200)
  assert.equal(order.materialDemands.convRatio, 4)
  assert.equal(order.materialDemands.materials[0].formulaQuantity, 3)
  assert.equal(order.materialDemands.materials[0].convRatio, 4)
  assert.equal(order.materialDemands.materials[0].baseUnitInfo, undefined)
})

test('manual materials from BOM editor submit without a BOM version or formula fields', async () => {
  const bom = instance()
  await bom.view.init()
  await bom.view.getOtherMaterials([Object.assign(new models.GetProductListByProductionDto(), {
    id: 700, productId: 70, productNo: 'O70', productType: 0, productUnitId: 100, productUnit: '件' })])
  bom.view.allRows().find(row => row.isOther).demandQuantity = 7
  bom.view.clearBom()
  await bom.view.submit()
  const { view, requests } = parentInstance()
  view.getMaterialDemands(bom.events.at(-1)[1])
  view.submit()
  const order = requests[0][0]
  assert.equal(order.basisType, enums.DispatchBasisType.未指定)
  assert.equal(order.basisId, undefined)
  assert.equal(order.materialDemands.materials[0].dataId, 70)
  assert.equal(order.materialDemands.materials[0].demandQuantity, 7)
  assert.equal(order.materialDemands.formulaVersionId, undefined)
})

test('version conversion wins and zero/stale parent mainQuantity is ignored', async () => {
  const source = fixture()
  const { view, requests, events, errors } = instance(source)
  await view.init()
  await view.submit()
  assert.deepEqual(errors, [])
  assert.equal(requests.find(item => item.url.endsWith('GetProductionBomDataById')).params.quantity, 2.4)
  assert.equal(requests.filter(item => item.url.endsWith('GetProductUnitList') && item.params.some(p => p.productId === 1)).length, 0)
  const demand = events[0][1]
  assert.equal(demand.convRatio, 0.4)
  assert.equal(demand.bomQuantity, 2.4)
  const parent = parentInstance().view
  parent.getMaterialDemands(demand)
  assert.equal(parent.bomDemandChanged(parent.orderList[0]), false)
  parent.orderList[0].mainQuantity = 999
  assert.equal(parent.bomDemandChanged(parent.orderList[0]), false)
  parent.orderList[0].dispatchUnitInfo.unitConversionId = 31
  assert.equal(parent.bomDemandChanged(parent.orderList[0]), true)
})

function addLatestOutputUnits(source) {
  source.units.push({ productId: 1, specificationId: source.current.specificationId,
    idField: source.current.specificationId ? '1_' + source.current.specificationId : '1', unitList: [
      { code: '30', unitId: 300, text: '包', value: '8' },
      { code: '20', unitId: 200, text: '箱', value: '2' }
    ] })
}

test('missing version relationship falls back to a fresh product/specification unit pair without mixing ratios', async () => {
  const source = fixture()
  source.current.specificationId = 2003
  source.bom.unitInfoList = [source.bom.unitInfoList[1]] // Version has target ratio 10 but no dispatch unit.
  addLatestOutputUnits(source)
  const { view, requests, events, errors } = instance(source)
  await view.init()
  await view.submit()
  assert.deepEqual(errors, [])
  const request = requests.find(item => item.url.endsWith('GetProductUnitList') && item.params.some(p => p.productId === 1))
  assert.deepEqual(request.params, [{ productId: 1, specificationId: 2003, idField: '1_2003' }])
  assert.equal(requests.find(item => item.url.endsWith('GetProductionBomDataById')).params.quantity, 24)
  assert.equal(events[0][1].bomQuantity, 24)
  assert.equal(events[0][1].convRatio, 4, 'latest 8 / 2, not latest 8 / version 10')
  assert.equal(events[0][1].bomUnitInfo.ratio, 2, 'snapshot records the ratio actually used')
  const parent = parentInstance().view
  parent.getMaterialDemands(events[0][1])
  assert.equal(parent.bomDemandChanged(parent.orderList[0]), false)
})

test('latest units support an empty version unit snapshot and use the matching package conversion ID', async () => {
  const source = fixture()
  source.bom.unitInfoList = []
  addLatestOutputUnits(source)
  source.units.at(-1).unitList.unshift({ code: '31', unitId: 300, text: '包【其它包装】', value: '100' })
  const { view, errors, requests } = instance(source)
  await view.init()
  assert.deepEqual(errors, [])
  assert.equal(requests.find(item => item.url.endsWith('GetProductionBomDataById')).params.quantity, 24)
  assert.equal(view.bomQuantity, 24)
})

function legacyBoxFixture() {
  const source = fixture()
  source.current.productId = 1776
  source.current.specificationId = 2003
  source.current.dispatchUnitInfo = { productionUnitId: 164, unitConversionId: 35, baseUnitId: 1, mainUnitId: 147 }
  source.bom.totalQuantity = 1
  source.bom.unitConversionId = 0
  // Actual legacy snapshot: the output specification precedes the main unit, both with Id=0.
  source.bom.unitInfoList = [
    { id: 0, unitId: 1, unitName: '片', ratio: 1 },
    { id: 0, unitId: 147, unitName: '米', ratio: 1 }
  ]
  source.units.push({ productId: 1776, specificationId: 2003, idField: '1776_2003', unitList: [
    { code: '-1', unitId: 1, text: '片', value: '1', isMainUnit: false },
    { code: '137', unitId: 147, text: '米', value: '0.2', isMainUnit: true },
    { code: '68', unitId: 164, text: '箱【其它包装】', value: '4', isMainUnit: false },
    { code: '35', unitId: 164, text: '箱【123333】', value: '0.66', isMainUnit: false }
  ] })
  return source
}

test('legacy duplicate zero IDs preserve the specification output and convert boxes using latest units', async () => {
  const { view, errors, events, requests } = instance(legacyBoxFixture())
  await view.init()
  await view.submit()
  assert.deepEqual(errors, [])
  assert.equal(view.bomUnits.find(unit => unit.id === 0).unitId, 1)
  assert.equal(requests.find(item => item.url.endsWith('GetProductionBomDataById')).params.quantity, 3.96)
  assert.equal(events[0][1].bomQuantity, 3.96)
  assert.equal(events[0][1].convRatio, 0.66)
  assert.equal(events[0][1].bomUnitConversionId, 0)
  assert.equal(events[0][1].bomUnitInfo.unitId, 1)
  assert.equal(events[0][1].bomUnitInfo.id, -1)
})

test('legacy main unit ID zero resolves to latest main unit and uses both latest ratios', async () => {
  const source = legacyBoxFixture()
  source.bom.unitInfoList = [source.bom.unitInfoList[1]]
  const { view, errors, events, requests } = instance(source)
  await view.init()
  await view.submit()
  assert.deepEqual(errors, [])
  assert.equal(requests.find(item => item.url.endsWith('GetProductionBomDataById')).params.quantity, 19.8)
  assert.equal(events[0][1].bomQuantity, 19.8)
  assert.equal(events[0][1].bomUnitInfo.id, 137)
  assert.equal(events[0][1].bomUnitInfo.ratio, 0.2)
})

test('specification snapshot and latest list can use a conversion ID for opposite units', async () => {
  const source = legacyBoxFixture()
  source.bom.unitConversionId = 137
  source.bom.unitInfoList[0].id = 137
  source.bom.unitInfoList[0].ratio = 5
  const { view, errors, requests } = instance(source)
  await view.init()
  assert.deepEqual(errors, [])
  assert.equal(requests.find(item => item.url.endsWith('GetProductionBomDataById')).params.quantity, 3.96)
})

test('missing box conversion cannot fall back to another package with the same unit ID', async () => {
  const source = legacyBoxFixture()
  source.units.at(-1).unitList = source.units.at(-1).unitList.filter(unit => unit.code !== '35')
  const { view, errors, requests, events } = instance(source)
  await view.init()
  await view.submit()
  assert.ok(errors.length)
  assert.equal(requests.some(item => item.url.endsWith('GetProductionBomDataById')), false)
  assert.equal(events.length, 0)
})

test('invalid quantities or unavailable conversions never submit a false zero or infinity', async () => {
  for (const value of [0, -1, NaN, Infinity]) {
    for (const target of ['quantity', 'ratio']) {
      const source = fixture()
      if (target === 'quantity') source.current.dispatchQuantity = value
      else source.bom.unitInfoList[2].ratio = value
      const { view, requests, events, errors } = instance(source)
      await view.init()
      assert.equal(requests.some(item => item.url.endsWith('GetProductionBomDataById')), false)
      assert.ok(errors.length)
      await view.submit()
      assert.equal(events.length, 0)
    }
  }
})

test('failed latest-unit request prevents querying and submitting BOM details', async () => {
  const source = fixture()
  source.bom.unitInfoList = []
  const { view, requests, events, errors } = instance(source, (url, params) =>
    url.endsWith('GetProductUnitList') && params.some(item => item.productId === 1)
      ? Promise.reject(new Error('单位接口失败')) : undefined)
  await view.init()
  assert.equal(view.isLoading, false)
  assert.equal(errors[0].message, '单位接口失败')
  assert.equal(requests.some(item => item.url.endsWith('GetProductionBomDataById')), false)
  await view.submit()
  assert.equal(events.length, 0)
})

test('reopening a draft refreshes changed latest conversion before accepting old material quantities', async () => {
  const source = fixture()
  source.bom.unitInfoList = []
  addLatestOutputUnits(source)
  const first = instance(source)
  await first.view.init()
  first.view.materialDetails[0].demandQuantity = 77
  await first.view.submit()
  source.current.materialDemands = first.events[0][1]
  source.units.at(-1).unitList[0].value = '4'
  const reopened = instance(source)
  await reopened.view.init()
  assert.equal(reopened.requests.find(item => item.url.endsWith('GetProductionBomDataById')).params.quantity, 12)
  assert.notEqual(reopened.view.materialDetails[0].demandQuantity, 77)
  assert.equal(reopened.view.isLoading, false)
  assert.equal(source.current.materialDemands.materialDetails[0].demandQuantity, 77, 'parent draft stays isolated')
})

test('query and submit use the same rounding and current dispatch inputs', async () => {
  const source = fixture()
  source.current.dispatchQuantity = 1
  source.bom.unitInfoList[2].ratio = 1
  source.bom.unitInfoList[1].ratio = 3
  const { view, requests, events } = instance(source)
  await view.init()
  await view.submit()
  const requested = requests.find(item => item.url.endsWith('GetProductionBomDataById')).params.quantity
  assert.equal(requested, 0.333333)
  assert.equal(events[0][1].bomQuantity, requested)
  const parent = parentInstance().view
  parent.orderList[0].dispatchQuantity = 1
  parent.getMaterialDemands(events[0][1])
  assert.equal(parent.bomDemandChanged(parent.orderList[0]), false)
  parent.orderList[0].dispatchQuantity = 2
  assert.equal(parent.bomDemandChanged(parent.orderList[0]), true)
})

test('missing selected unit or quantity blocks posting instead of filling required values with zero', async () => {
  const bom = instance()
  await bom.view.init()
  await bom.view.submit()
  const { view, requests, errors } = parentInstance()
  const demand = bom.events[0][1]
  view.getMaterialDemands(demand)
  const material = demand.materials[0]
  const unitId = material.unitId
  material.unitId = undefined
  view.submit()
  assert.equal(requests.length, 0)
  assert.match(errors.at(-1).message, /需求单位和需求数量/)
  material.unitId = unitId
  material.demandQuantity = undefined
  view.submit()
  assert.equal(requests.length, 0)
  assert.match(errors.at(-1).message, /需求单位和需求数量/)
})


test('dispatch quantity scales BOM tree, selected materials and unit snapshots before reopening', async () => {
  const bom = instance()
  await bom.view.init()
  const main = bom.view.materialDetails[0]
  main.demandQuantity = 30 // Preserve the manually adjusted amount proportionally.
  bom.view.updateUnitInfo(main)
  await bom.view.submit()
  const { view, requests } = parentInstance()
  view.getMaterialDemands(bom.events[0][1])
  const original = view.orderList[0].materialDemands
  const row = view.orderList[0]
  row.dispatchQuantity = 12
  view.onChangeDispatchQuantity(row)
  const updated = row.materialDemands
  assert.equal(original.dispatchQuantity, 6, 'rescaling does not mutate the previous draft')
  assert.equal(updated.dispatchQuantity, 12)
  assert.equal(updated.bomQuantity, 4.8)
  assert.equal(updated.materials[0].demandQuantity, 60)
  assert.equal(updated.materials[0].baseDemandQuantity, 60)
  assert.equal(updated.materials[0].convUnitInfo.find(unit => unit.id === -1).outboundQuantity, 60)
  assert.equal(updated.materialDetails[0].demandQuantity, 60)
  assert.equal(updated.materialDetails[0].curQuantity, 48)
  assert.equal(updated.materialDetails[0].standardQuantity, 48)
  assert.equal(updated.materialDetails[0].baseQuantity, original.materialDetails[0].baseQuantity, 'BOM standard denominator remains fixed')
  assert.equal(updated.materialDetails[0].children[0].curQuantity, 4)
  assert.equal(view.bomDemandChanged(row), false)
  view.materialDemand(row, 0, true)
  const reopened = instance({ ...fixture(), current: view.currentBomRow })
  await reopened.view.init()
  assert.equal(reopened.view.formData.find(item => item.label === '生产单位派工').value, '12')
  assert.equal(reopened.view.bomQuantity, 4.8)
  assert.equal(reopened.view.materialDetails[0].demandQuantity, 60, 'reopening retains rescaled manual demand')
  assert.equal(reopened.requests.some(request => request.url.endsWith('GetProductionBomDataById')), false, 'valid updated draft is reused')
  await reopened.view.submit()
  assert.equal(reopened.events[0][1].materials[0].demandQuantity, 60)
  view.submit()
  assert.equal(requests[0][0].materialDemands.materials[0].demandQuantity, 60)
  // Consecutive spinner events before nextTick must not lose the last quantity.
  row.dispatchQuantity = 18
  view.onChangeDispatchQuantity(row)
  assert.equal(row.materialDemands.materials[0].demandQuantity, 90)
  row.dispatchQuantity = 6
  view.onChangeDispatchQuantity(row)
  assert.equal(row.materialDemands.materials[0].demandQuantity, 30)
})

test('quantity scaling preserves substitute choice, other materials and independent drafts per dispatch row', async () => {
  const bom = instance()
  await bom.view.init()
  const main = bom.view.materialDetails[0]
  const alternate = main.children[0]
  alternate.isDemand = true
  bom.view.changeDemandStatus(alternate)
  await Promise.resolve()
  const other = new models.BomDemandDetailDto({ isOther: true, isDemand: true, productId: 70,
    dataId: 70, unitId: 100, unitConversionId: -1, demandQuantity: 3, baseDemandQuantity: 3,
    demandRatio: 1, indexField: 'other', groupKey: 'other',
    unitList: [new models.BomDemandUnitDto({ id: -1, unitId: 100, ratio: 1 })] })
  bom.view.materialDetails.push(other)
  await bom.view.submit()
  const { view } = parentInstance()
  const row = view.orderList[0]
  view.getMaterialDemands(bom.events[0][1])
  const untouched = JSON.stringify(row.materialDemands)
  const second = new models.DispatchOrderEditDto({ ...row })
  view.orderList.push(second)
  row.dispatchQuantity = 12
  view.onChangeDispatchQuantity(row)
  assert.equal(JSON.stringify(second.materialDemands), untouched)
  assert.equal(row.materialDemands.materialDetails[0].isDemand, false)
  assert.equal(row.materialDemands.materialDetails[0].children[0].isDemand, true)
  assert.equal(row.materialDemands.materials.find(item => item.dataId === 421).demandQuantity, 4)
  assert.equal(row.materialDemands.materials.find(item => item.dataId === 70).demandQuantity, 6)
  // An empty temporary NumberBox value must not destroy the last positive snapshot.
  row.dispatchQuantity = 0
  view.onChangeDispatchQuantity(row)
  row.dispatchQuantity = 18
  view.onChangeDispatchQuantity(row)
  assert.equal(row.materialDemands.materials.find(item => item.dataId === 70).demandQuantity, 9)
  // Changing production units is not merely a quantity change; require a fresh conversion.
  row.dispatchUnitInfo = { ...row.dispatchUnitInfo, unitConversionId: 31 }
  row.dispatchQuantity = 24
  view.onChangeDispatchQuantity(row)
  assert.equal(view.bomDemandChanged(row), true)
  view.materialDemand(row, 0, true)
  assert.equal(view.currentBomRow.materialDemands, undefined)
})
