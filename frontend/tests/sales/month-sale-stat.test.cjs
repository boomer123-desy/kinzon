const assert = require('node:assert/strict')
const fs = require('node:fs')
const os = require('node:os')
const path = require('node:path')
const { test } = require('node:test')
const dependencies = path.resolve(__dirname, '../../node_modules')
const ts = require(path.join(dependencies, 'typescript'))
const { parse, compileScript, compileTemplate } = require(path.join(dependencies, '@vue/compiler-sfc'))
const page = path.resolve(__dirname, '../../src/views/sales/sale_data_stat/index.vue')
const dto = path.resolve(__dirname, '../../src/types/sales.ts')
const enumFile = path.resolve(__dirname, '../../src/types/enums.ts')
// Shared host types are absent from this module snapshot; match the constructor
// contract used by existing pages and execute the actual SearchPanel methods.
const searchTypes = `import { DataType, FilterType } from './enums';
export class Search {
  constructor(public key: string, public label: string | undefined, public table: string,
    public filterType: FilterType, public dataType: DataType, public value?: unknown,
    public optionList?: { value: number; text: string }[], public placeholder?: string, public width?: string) {}
}
export class Filter {
  constructor(public key: string, public value: unknown, public filterType: FilterType, public dataType: DataType) {}
}
export interface FilterTmp { table: string; filter: Filter[] }
export class ShowFormData {
  constructor(public label: string, public value: string) {}
}
export class CustomAttribute { constructor(init: object) { Object.assign(this, init) } }`

function evaluate(source, requireModule) {
  const compiled = ts.transpileModule(source, { compilerOptions: { target: ts.ScriptTarget.ES2020, module: ts.ModuleKind.CommonJS } })
  const module = { exports: {} }
  new Function('require', 'module', 'exports', compiled.outputText)(requireModule, module, module.exports)
  return module.exports
}

const models = evaluate(fs.readFileSync(dto, 'utf8'), () => { throw new Error('Unexpected DTO dependency') })
const enums = evaluate(fs.readFileSync(enumFile, 'utf8'), () => { throw new Error('Unexpected enum dependency') })
const sharedTypes = evaluate(searchTypes, () => enums)
function moduleDependency(name) {
  if (name === 'vue') return { defineComponent: value => value }
  if (name === '@/types/enums') return enums
  if (name === '@/types/types') return sharedTypes
  if (name === '@/types/sale-data-stat' || name === '@/types/sales') return models
  if (name === 'lodash') return require(path.join(dependencies, 'lodash'))
  if (name === './component/sale-stat-week-detail.vue') return { default: { name: 'SaleStatWeekDetail', render() { return null } } }
  throw new Error('Unexpected dependency: ' + name)
}
const descriptor = parse(fs.readFileSync(page, 'utf8'), { filename: page }).descriptor
const options = evaluate(descriptor.script.content, moduleDependency).default
const detailFile = path.resolve(__dirname, '../../src/views/sales/sale_data_stat/component/sale-stat-week-detail.vue')
const detailDescriptor = parse(fs.readFileSync(detailFile, 'utf8'), { filename: detailFile }).descriptor
const detailOptions = evaluate(detailDescriptor.script.content, moduleDependency).default
const panelFile = path.resolve(__dirname, '../../src/components/search_panel/search-panel.vue')
const panelDescriptor = parse(fs.readFileSync(panelFile, 'utf8'), { filename: panelFile }).descriptor
const panel = evaluate(panelDescriptor.script.content, moduleDependency).default

// Execute the actual wrapper's column restoration to reproduce the stale-cache
// failure without loading unrelated host controls and export dependencies.
const wrapperFile = path.resolve(__dirname, '../../src/components/kingzon-datagrid.vue')
const wrapperScript = parse(fs.readFileSync(wrapperFile, 'utf8'), { filename: wrapperFile }).descriptor.script.content
const wrapperTree = ts.createSourceFile(wrapperFile + '.ts', wrapperScript, ts.ScriptTarget.Latest, true)
let restoreMethod
function findRestore(node) {
  if (ts.isMethodDeclaration(node) && node.name.getText(wrapperTree) === '_reOrderGroupRefs') restoreMethod = node.getText(wrapperTree)
  ts.forEachChild(node, findRestore)
}
findRestore(wrapperTree)
const restoreColumns = evaluate('export default { ' + restoreMethod + ' }', () => {}).default._reOrderGroupRefs

test('shared grid can move total into delivery headers when restoring a different week count', () => {
  const header = (field, title) => ({ field, title })
  const orders = Array.from({ length: 4 }, (_, index) => header(undefined, '订单' + (index + 1)))
  const deliveries = Array.from({ length: 4 }, (_, index) => header(undefined, '发货' + (index + 1)))
  const center = [...orders, header('orderAmount', '订单合计'), ...deliveries, header('returnAmount', '退货')]
  const left = [header('rank', '排名')]
  const saved = { leftHeaderColumns: [...left], centerHeaderColumns: [
    ...Array.from({ length: 5 }, (_, index) => header(undefined, '订单' + (index + 1))), header('orderAmount', '订单合计'),
    ...Array.from({ length: 5 }, (_, index) => header(undefined, '发货' + (index + 1))), header('returnAmount', '退货')
  ] }
  restoreColumns.call({ dataGrid: { groupRefs: [{ rows: [{ columns: left }] }, { rows: [{ columns: center }] }] },
    _createColumnInfo: value => value, _restoreColumnInfo() {} }, saved)
  assert.equal(center[4].title, '发货1', 'cached first-row order no longer matches the leaf columns')
})

test('rendered financial headers provide default slots and valid column spans for 4, 5 and 6 weeks', async () => {
  const vue = require(path.join(dependencies, 'vue'))
  const { renderToString } = require(path.join(dependencies, '@vue/server-renderer'))
  const template = compileTemplate({ source: descriptor.template.content, filename: page, id: page })
  const render = evaluate(template.code, () => vue).render
  function columns(nodes) {
    return nodes.flatMap(node => typeof node.type === 'symbol' && Array.isArray(node.children) ? columns(node.children) : [node])
      .filter(node => node.type?.name === 'GridColumn')
  }
  for (const weekCount of [4, 5, 6]) {
    const report = new models.MonthSaleDataStatDto({ year: 2026, month: 9,
      weeks: Array.from({ length: weekCount }, (_, index) => new models.FinanceWeekDto({ weekNo: index + 1, startDate: '2026-09-26', endDate: '2026-10-02' })) })
    const layouts = []
    const cells = []
    const row = { orderAmount: 987.65,
      orderWeeks: report.weeks.map(week => ({ weekNo: week.weekNo, amount: 1000 + week.weekNo })).reverse(),
      deliveryWeeks: report.weeks.map(week => ({ weekNo: week.weekNo, amount: 2000 + week.weekNo })).reverse(),
      weekAmounts: Object.fromEntries(report.weeks.flatMap(week => [
        ['orderWeek' + week.weekNo, 1000 + week.weekNo], ['deliveryWeek' + week.weekNo, 2000 + week.weekNo]
      ])) }
    const app = vue.createSSRApp({ ...options, render, created() {}, data() { return { ...options.data(), loaded: true, report } } })
    const passthrough = { render() { return vue.h('div', this.$slots.default?.()) } }
    for (const name of ['Loading', 'SearchPanel', 'LinkButton', 'font-awesome-icon', 'Dialog']) app.component(name, passthrough)
    app.component('KingZonDataGrid', { render() {
      assert.equal(this.$attrs.id, 'monthSaleDataStat-weekAmount-' + weekCount, 'cache key isolates header schema and financial week count')
      return vue.h('div', this.$slots.default())
    } })
    app.component('GridColumn', { name: 'GridColumn', render() { return null } })
    // EasyUI GridHeaderRow calls its default slot directly, and GridBase allocates
    // the column matrix from the actual row count of each group.
    app.component('GridHeaderRow', { name: 'GridHeaderRow', render() { return vue.h('div', this.$slots.default()) } })
    app.component('GridColumnGroup', { name: 'GridColumnGroup', render() {
      const rows = this.$slots.default()
      const layout = rows.map(() => [])
      rows.forEach((row, rowIndex) => {
        assert.equal(typeof row.children.default, 'function', 'every header row has a default slot')
        let columnIndex = 0
        for (const column of columns(row.children.default())) {
          while (layout[rowIndex][columnIndex] !== undefined) columnIndex++
          const span = column.props.rowspan ?? 1
          assert.ok(rowIndex + span <= rows.length, 'rowspan fits the rows in this column group')
          for (let offset = 0; offset < span; offset++) layout[rowIndex + offset][columnIndex] = column
          columnIndex++
        }
      })
      layouts.push(layout)
      cells.push(layout.at(-1).map(column => ({ field: column.props.field,
        value: column.children?.body?.({ row, column: column.props }).map(node => node.children).join('') })))
      return vue.h('div', rows)
    } })
    await renderToString(app)
    assert.deepEqual(layouts[0][0].map(column => column.props.field), ['rank', 'customerName'])
    assert.deepEqual(layouts[1][1].map(column => column.props.field), [
      ...Array.from({ length: weekCount }, (_, index) => 'orderWeek' + (index + 1)), 'orderAmount',
      ...Array.from({ length: weekCount }, (_, index) => 'deliveryWeek' + (index + 1)),
      'returnAmount', 'deliveryAmount', 'lastYearOrderAmount', 'yearOnYearRate', 'deliveryShareRate'
    ])
    for (const week of report.weeks) {
      assert.equal(cells[1].find(cell => cell.field === 'orderWeek' + week.weekNo).value, options.methods.amountText(1000 + week.weekNo))
      assert.equal(cells[1].find(cell => cell.field === 'deliveryWeek' + week.weekNo).value, options.methods.amountText(2000 + week.weekNo))
    }
    assert.equal(cells[1].find(cell => cell.field === 'orderAmount').value, '987.65')
  }
})

function instance(get, component = options, props = {}) {
  const errors = []
  const view = { ...component.data(), ...props, $refs: {}, $nextTick: callback => Promise.resolve().then(callback), $http: { get }, $kingzon_messager: { showError: error => errors.push(error) } }
  for (const [name, method] of Object.entries(component.methods)) view[name] = method.bind(view)
  for (const [name, method] of Object.entries(component.computed)) Object.defineProperty(view, name, { get: method.bind(view) })
  return { view, errors }
}

test('supplied March response renders order amounts in order columns, and zero in delivery columns', async () => {
  const response = JSON.parse(fs.readFileSync(path.join(__dirname, '../../../backend/Sales/tests/fixtures/month_sale_response.json'), 'utf8'))
  const { view } = instance(async () => ({ data: response }))
  await view.getData()
  const vue = require(path.join(dependencies, 'vue'))
  const { renderToString } = require(path.join(dependencies, '@vue/server-renderer'))
  const render = evaluate(compileTemplate({ source: descriptor.template.content, filename: page, id: page }).code, () => vue).render
  const cells = new Map()
  const app = vue.createSSRApp({ ...options, render, created() {}, data() {
    return { ...options.data(), report: view.report, tableRows: view.tableRows, footerData: view.footerData, gridKey: view.gridKey }
  } })
  const passthrough = { render() { return vue.h('div', this.$slots.default?.()) } }
  for (const name of ['Loading', 'SearchPanel', 'LinkButton', 'KingZonDataGrid', 'GridColumnGroup', 'GridHeaderRow', 'font-awesome-icon', 'Dialog']) app.component(name, passthrough)
  app.component('GridColumn', { name: 'GridColumn', props: ['field'], render() {
    if (this.$slots.body) {
      function slotText(nodes) {
        return nodes.map(node => node.type === vue.Comment ? '' : Array.isArray(node.children) ? slotText(node.children) : node.children ?? '').join('')
      }
      const body = view.tableRows.map(row => slotText(this.$slots.body({ row, column: { field: this.field } })).trim())
      const footer = view.footerData.map(row => slotText((this.$slots.footer ?? this.$slots.body)({ row, column: { field: this.field } })).trim())
      cells.set(this.field, [...body, ...footer])
    }
    return null
  } })
  await renderToString(app)
  assert.equal(view.footerData.length, 1, 'weekly totals appear once in the fixed footer')
  assert.equal(cells.get('orderAmount').at(-1), '4,208,055.81')
  assert.equal(cells.get('deliveryAmount').at(-1), '-', 'current delivery comparison column uses delivery total')
  assert.ok(cells.get('deliveryShareRate').every(value => value === '—'), 'zero delivery total does not produce an invalid share')
  assert.equal(cells.get('orderWeek1').at(-1), '528,337.92')
  assert.equal(cells.get('orderWeek2')[0], '20,565.00')
  assert.equal(cells.get('orderWeek3')[0], '19,050.00')
  assert.equal(cells.get('orderWeek5')[0], '41,969.00')
  for (const week of response.weeks) {
    assert.equal(cells.get('orderWeek' + week.weekNo).at(-1), view.amountText(response.orderWeeks.find(item => item.weekNo === week.weekNo).amount))
    assert.ok(cells.get('deliveryWeek' + week.weekNo).every(value => value === '-'), 'zero delivery remains zero for customers, subtotal and total')
  }
})

test('report page template/script compile and actual DTO/page pass strict TypeScript checks', t => {
  assert.deepEqual(compileTemplate({ source: descriptor.template.content, filename: page, id: page }).errors, [])
  assert.deepEqual(compileTemplate({ source: detailDescriptor.template.content, filename: detailFile, id: detailFile }).errors, [])
  const folder = fs.mkdtempSync(path.join(os.tmpdir(), 'month-sale-types-'))
  t.after(() => fs.rmSync(folder, { recursive: true, force: true }))
  fs.writeFileSync(path.join(folder, 'index.vue.ts'), compileScript(descriptor, { id: page }).content)
  fs.writeFileSync(path.join(folder, 'week-detail.vue.ts'), compileScript(detailDescriptor, { id: detailFile }).content)
  fs.writeFileSync(path.join(folder, 'vue-shim.d.ts'), "declare module '*.vue' { const component: import('vue').DefineComponent; export default component }")
  fs.copyFileSync(dto, path.join(folder, 'sale-data-stat.ts'))
  fs.copyFileSync(enumFile, path.join(folder, 'enums.ts'))
  fs.writeFileSync(path.join(folder, 'types.ts'), searchTypes)
  fs.writeFileSync(path.join(folder, 'host.d.ts'), `import 'vue'; import { MonthSaleDataStatDto, MonthSaleWeekDetailResultDto } from './sale-data-stat'; declare module 'vue' { interface ComponentCustomProperties { $utils: { getCellSpan(row: unknown, index: number, rows: unknown[], key: string): unknown }; $http: { get(url: string, params: unknown): Promise<HttpResponseType>; getFile(url: string, params: unknown): Promise<{ url?: string; name?: string }> }; $kingzon_messager: { showError(error: unknown): void } } } declare global { namespace EasyUI { interface Dialog { open(): void; close(): void } } interface HttpResponseType { data: MonthSaleDataStatDto & MonthSaleWeekDetailResultDto } }`)
  const program = ts.createProgram(['index.vue.ts', 'week-detail.vue.ts', 'sale-data-stat.ts', 'host.d.ts', 'vue-shim.d.ts'].map(file => path.join(folder, file)), {
    strict: true, noEmit: true, skipLibCheck: true, target: ts.ScriptTarget.ES2020, module: ts.ModuleKind.ESNext,
    moduleResolution: ts.ModuleResolutionKind.Node10, baseUrl: folder,
    paths: { vue: [path.join(dependencies, 'vue')], '@/types/sale-data-stat': [path.join(folder, 'sale-data-stat.ts')], '@/types/sales': [path.join(folder, 'sale-data-stat.ts')],
      '@/types/enums': [path.join(folder, 'enums.ts')], '@/types/types': [path.join(folder, 'types.ts')] }
  })
  const diagnostics = ts.getPreEmitDiagnostics(program)
  assert.equal(diagnostics.length, 0, ts.formatDiagnosticsWithColorAndContext(diagnostics, { getCurrentDirectory: () => folder, getCanonicalFileName: file => file, getNewLine: () => '\n' }))
})

test('formula export uses the loaded report filters, download wrapper and duplicate/loading guards', async t => {
  const { view, errors } = instance(() => {})
  const requests = []
  const links = []
  const originalDocument = global.document
  t.after(() => { global.document = originalDocument })
  global.document = {
    createElement(tag) {
      assert.equal(tag, 'a')
      const link = { click() { this.clicked = true } }
      links.push(link)
      return link
    },
    body: { appendChild(link) { link.attached = true }, removeChild(link) { link.attached = false } }
  }
  let respond
  view.$http.getFile = (url, params) => {
    requests.push({ url, params })
    return new Promise(resolve => { respond = resolve })
  }
  assert.equal(view.exportExcel(), undefined, 'unloaded report cannot export')
  view.report = new models.MonthSaleDataStatDto({ year: 2026, month: 3 })
  view.reportCustomerName = '甲'
  view.year = 2027
  view.month = 12
  view.customerName = '未提交条件'
  view.isLoading = true
  assert.equal(view.exportExcel(), undefined, 'refresh prevents exporting inconsistent report conditions')
  view.isLoading = false
  const pending = view.exportExcel()
  assert.equal(view.isExporting, true)
  view.exportExcel()
  assert.equal(requests.length, 1, 'repeated clicks do not duplicate exports')
  assert.deepEqual(requests[0], { url: '/api/sales/SaleDataStat/ExportMonthDataStat', params: { month: '2026-03-01', customerName: '甲', includeTax: false } })
  respond({ url: 'blob:report', name: '月报.xlsx' })
  await pending
  assert.equal(links[0].href, 'blob:report')
  assert.equal(links[0].download, '月报.xlsx')
  assert.equal(links[0].clicked, true)
  assert.equal(links[0].attached, false)
  assert.equal(view.isExporting, false)
  view.$http.getFile = async () => ({})
  await view.exportExcel()
  assert.equal(errors.length, 1, 'missing URL is shown as download failure')
  assert.equal(view.isExporting, false)
  view.$http.getFile = async () => { throw new Error('Export failed') }
  await view.exportExcel()
  assert.equal(errors.length, 2)
  assert.equal(view.isExporting, false)
  assert.equal(links.length, 1, 'failure does not trigger an invalid download')
})

test('week clicks open the dialog with customer/category/total scopes and loaded report dates', async () => {
  const { view } = instance(() => {})
  let opened = 0
  let closed = 0
  view.$refs = { weekDetailDialog: { open() { opened++ }, close() { closed++ } } }
  view.$nextTick = callback => Promise.resolve().then(callback)
  view.report = new models.MonthSaleDataStatDto({ year: 2026, month: 3,
    weeks: [new models.FinanceWeekDto({ weekNo: 2, startDate: '2026-03-28', endDate: '2026-04-03' })] })
  view.reportCustomerName = '甲'
  view.customerName = '未提交的新条件'
  const row = new models.MonthSaleTableRowDto({ customerId: 2, categoryId: 1, customerName: '甲',
    weekAmounts: { orderWeek2: 123.45, deliveryWeek2: 50 } })
  view.showWeekDetails(row, 'orderWeek2')
  await Promise.resolve()
  assert.equal(opened, 1)
  assert.equal(view.weekDetailQuery.month, '2026-03-01')
  assert.equal(view.weekDetailQuery.customerName, '甲')
  assert.equal(view.weekDetailQuery.customerId, 2)
  assert.equal(view.weekDetailQuery.expectedAmount, 123.45)
  assert.equal(view.weekDetailQuery.detailType, models.SaleStatDetailType.销售)
  view.showWeekDetails(new models.MonthSaleTableRowDto({ ...row, rowType: models.SaleStatRowType.小计 }), 'deliveryWeek2')
  assert.equal(view.weekDetailQuery.customerId, null)
  assert.equal(view.weekDetailQuery.filterCategory, true)
  assert.equal(view.weekDetailQuery.categoryId, 1)
  assert.equal(view.weekDetailQuery.detailType, models.SaleStatDetailType.送货)
  view.showWeekDetails(new models.MonthSaleTableRowDto({ rowType: models.SaleStatRowType.总计 }), 'orderWeek2')
  assert.equal(view.weekDetailQuery.filterCategory, false)
  assert.equal(view.weekDetailQuery.customerId, null)
  view.closeWeekDetails()
  assert.equal(closed, 1)
  assert.equal(view.showWeekDetail, false)
})

test('detail component selects the correct API, keeps nullable category scope, and clears loading on failure', async () => {
  const query = new models.MonthSaleWeekDetailQueryDto({ month: '2026-03-01', weekNo: 2,
    filterCategory: true, categoryId: null, detailType: models.SaleStatDetailType.送货 })
  const calls = []
  const { view, errors } = instance(async (url, params) => {
    calls.push({ url, params })
    return { data: new models.MonthSaleWeekDetailResultDto({ totalAmount: 123.45, orderCount: 1 }) }
  }, detailOptions, { query })
  await view.getData()
  assert.equal(calls[0].url, 'api/sales/SaleDataStat/GetWeekDeliveryDetails')
  assert.equal(calls[0].params.filterCategory, true)
  assert.equal(calls[0].params.categoryId, undefined)
  assert.equal(view.result.totalAmount, 123.45)
  query.detailType = models.SaleStatDetailType.销售
  await view.getData()
  assert.equal(calls[1].url, 'api/sales/SaleDataStat/GetWeekSaleOrderDetails')
  view.$http.get = async () => { throw new Error('Unavailable') }
  await view.getData()
  assert.equal(errors.length, 1)
  assert.equal(view.isLoading, false)
})

test('SearchPanel filters map to month/customer query and reset restores current month', async () => {
  const requests = []
  const { view, errors } = instance(async (url, params) => {
    requests.push({ url, params })
    return { data: new models.MonthSaleDataStatDto() }
  })
  view.addSearchParam()
  const inputs = require(path.join(dependencies, 'lodash')).cloneDeep(view.searchParams)
  inputs.find(item => item.key === 'year').value = 2025
  inputs.find(item => item.key === 'month').value = '12'
  inputs.find(item => item.key === 'customerName').value = ' 甲 '
  const filters = panel.methods.handleSearchParams(inputs, [])
  await view.search([{ table: 'unrelated', filter: [] }, ...filters])
  assert.deepEqual(requests[0].params, { month: '2025-12-01', customerName: '甲', includeTax: false })
  let resetRequest
  const panelView = { searchParams: view.searchParams, searchParamsHidden: [],
    $emit(event) { assert.equal(event, 'reset'); resetRequest = view.reset() } }
  panel.methods.reset.call(panelView)
  await resetRequest
  const now = new Date()
  const currentMonth = `${now.getFullYear()}-${String(now.getMonth() + 1).padStart(2, '0')}-01`
  assert.deepEqual(requests[1].params, { month: currentMonth, customerName: undefined, includeTax: false })
  assert.equal(view.searchParams.find(item => item.key === 'year').value, now.getFullYear())
  assert.equal(view.searchParams.find(item => item.key === 'month').value, now.getMonth() + 1)
  await view.search([])
  assert.deepEqual(requests[2].params, requests[1].params)
  await view.search([{ table: 'monthSaleStat', filter: [new sharedTypes.Filter('month', 13, enums.FilterType.Equals, enums.DataType.SelectNum)] }])
  assert.equal(requests.length, 3)
  assert.equal(errors.length, 1)
})

test('real response produces ranked customer rows, category subtotals and one total without recomputing ratios', async () => {
  const report = new models.MonthSaleDataStatDto({ year: 2026, month: 9, deliveryAmount: 100, netDeliveryAmount: 75, lastYearOrderAmount: 80, yearOnYearRate: 0.25,
    orderWeeks: [{ weekNo: 1, amount: 123.45 }], deliveryWeeks: [{ weekNo: 1, amount: 100 }],
    categories: [new models.MonthSaleCategoryStatDto({ categoryId: 1, categoryName: '汽车', netDeliveryAmount: 100,
      customers: [new models.MonthSaleCustomerStatDto({ customerId: 2, customerName: '甲', rank: 3, netDeliveryAmount: 100, yearOnYearRate: null,
        orderWeeks: [{ weekNo: 1, amount: 123.45 }], deliveryWeeks: [{ weekNo: 1, amount: 100 }], deliveryShareRate: 0.666667 })] })] })
  const { view } = instance(async (url, params) => {
    assert.equal(url, 'api/sales/SaleDataStat/GetMonthDataStat')
    assert.equal(params.month, '2026-09-01')
    assert.equal(params.customerName, '甲')
    return { data: report }
  })
  view.year = 2026
  view.month = 9
  view.customerName = ' 甲 '
  await view.getData()
  assert.deepEqual(view.tableRows.map(row => row.rowType), ['customer', 'subtotal'])
  assert.deepEqual(view.footerData.map(row => row.rowType), ['total'])
  assert.equal(view.tableRows[0].rank, 3)
  assert.equal(view.tableRows[0].yearOnYearRate, null)
  assert.equal(view.tableRows[0].orderWeeks[0].amount, 123.45)
  assert.equal(view.tableRows[0].deliveryWeeks[0].amount, 100)
  assert.equal(view.tableRows[0].weekAmounts.orderWeek1, 123.45)
  assert.equal(view.tableRows[0].weekAmounts.deliveryWeek1, 100)
  assert.equal(view.tableRows[0].deliveryShareRate, 0.666667)
  assert.equal(view.footerData[0].yearOnYearRate, 0.25)
  assert.equal(view.footerData[0].lastYearOrderAmount, 80)
  assert.equal(view.footerData[0].deliveryAmount, 100)
  assert.equal(view.footerData[0].weekAmounts.orderWeek1, 123.45)
  assert.equal(view.footerData[0].weekAmounts.deliveryWeek1, 100)
  assert.equal(view.isLoading, false)
  assert.equal(view.gridKey, 1)
})

test('zero comparison is displayed safely, six weeks and cross-year comparison titles work', () => {
  const { view } = instance(() => {})
  assert.equal(view.rateText(null), '—')
  assert.equal(view.rateText(0), '0.00%')
  assert.equal(view.rateText(-1), '↓ -100.00%')
  assert.equal(view.percentText(null), '—')
  assert.equal(view.percentText(0), '0.00%')
  assert.equal(view.percentText(0.666667), '66.67%')
  assert.equal(view.percentText(1), '100.00%')
  assert.equal(view.weekTitle(6), '第六周')
  assert.equal(view.weekRange({ startDate: '2026-09-26T00:00:00', endDate: '2026-10-02T00:00:00' }), '09-26至10-02')
  view.report = new models.MonthSaleDataStatDto({ year: 2026, month: 1 })
  assert.equal(view.previousMonthTitle, '2025年12月')
  assert.equal(view.lastYearTitle, '2025年1月')
})

test('failed refresh retains the loaded report and clears loading, empty response clears old rows', async () => {
  const { view, errors } = instance(async () => { throw new Error('Unavailable') })
  view.loaded = true
  view.report = new models.MonthSaleDataStatDto({ year: 2026, month: 9 })
  view.footerData = [new models.MonthSaleTableRowDto({ customerName: '总计', weekAmounts: { orderWeek1: 123.45 } })]
  await view.getData()
  assert.equal(errors.length, 1)
  assert.equal(view.report.month, 9)
  assert.equal(view.footerData[0].weekAmounts.orderWeek1, 123.45)
  assert.equal(view.isLoading, false)
  view.$http.get = async () => ({ data: new models.MonthSaleDataStatDto({ year: 2026, month: 10 }) })
  await view.getData()
  assert.deepEqual(view.tableRows, [])
  assert.deepEqual(view.footerData, [])
})


test('tax toggle reloads report and preserves loaded tax scope for week/return details', async () => {
  const calls = []
  const { view } = instance(async (url, params) => {
    calls.push({ url, params })
    return { data: new models.MonthSaleDataStatDto({ year: 2026, month: 10, includeTax: params.includeTax,
      startDate: '2026-09-26', endDate: '2026-10-25', weeks: [new models.FinanceWeekDto({ weekNo: 1, startDate: '2026-09-26', endDate: '2026-10-02' })] }) }
  })
  view.year = 2026; view.month = 10
  await view.toggleTax()
  assert.equal(calls[0].params.includeTax, true)
  assert.equal(view.report.includeTax, true)
  assert.match(view.reportTitle, /含税/)
  const row = new models.MonthSaleTableRowDto({ customerId: 7, customerName: '甲', returnAmount: 13, weekAmounts: { deliveryWeek1: 113 } })
  view.showWeekDetails(row, 'deliveryWeek1')
  assert.equal(view.weekDetailQuery.includeTax, true)
  view.showReturnDetails(row)
  assert.equal(view.weekDetailQuery.detailType, models.SaleStatDetailType.退货)
  assert.equal(view.weekDetailQuery.weekNo, 0)
  assert.equal(view.weekDetailQuery.customerId, 7)
  assert.equal(view.weekDetailQuery.expectedAmount, 13)
  assert.match(view.weekDetailTitle, /2026-09-26 至 2026-10-25/)
  assert.equal(view.weekDetailQuery.includeTax, true)
  view.$http.get = async () => { throw new Error('tax refresh failed') }
  await view.toggleTax()
  assert.equal(view.includeTax, true, 'failed switch restores the loaded tax mode')
})

test('return detail requests only returned deliveries and shows both tax bases with unique head footer totals', async () => {
  const calls = []
  const query = new models.MonthSaleWeekDetailQueryDto({ month: '2026-10-01', weekNo: 0, includeTax: true,
    detailType: models.SaleStatDetailType.退货 })
  const { view } = instance(async (url, params) => { calls.push({ url, params }); return { data: {} } }, detailOptions, { query })
  await view.getData()
  assert.equal(calls[0].params.onlyReturns, true)
  assert.equal(calls[0].params.includeTax, true)
  view.result = new models.MonthSaleWeekDetailResultDto({ orderCount: 1, detailCount: 2, totalAmount: 33.9, rows: [
    new models.MonthSaleWeekDetailDto({ orderId: 1, totalPrice: 100, totalPriceHaveTax: 113,
      orderTotalPrice: 150, orderTotalPriceHaveTax: 169.5, returnQuantity: 1, returnAmount: 10, returnAmountHaveTax: 11.3 }),
    new models.MonthSaleWeekDetailDto({ orderId: 1, totalPrice: 50, totalPriceHaveTax: 56.5,
      orderTotalPrice: 150, orderTotalPriceHaveTax: 169.5, returnQuantity: 2, returnAmount: 20, returnAmountHaveTax: 22.6 })
  ] })
  assert.equal(view.footerData[0].index, '1张单据 / 2条明细')
  assert.equal(view.footerData[0].totalPrice, 150)
  assert.equal(view.footerData[0].totalPriceHaveTax, 169.5)
  assert.equal(view.footerData[0].returnAmount, 30)
  assert.equal(view.footerData[0].returnAmountHaveTax.toFixed(2), '33.90')
  query.detailType = models.SaleStatDetailType.销售
  assert.equal(view.footerData[0].orderTotalPrice, 150)
  assert.equal(view.footerData[0].orderTotalPriceHaveTax, 169.5, 'repeated head amounts are counted once')
  const spanCalls = []
  view.$utils = { getCellSpan: (...args) => { spanCalls.push(args); return { rowspan: 2, colspan: 1 } } }
  assert.deepEqual(view.cellSpan({ column: { field: 'orderTotalPriceHaveTax' }, row: view.result.rows[0], rowIndex: 0 }), { rowspan: 2, colspan: 1 })
  assert.equal(spanCalls[0][3], 'orderId')
  assert.deepEqual(view.cellSpan({ column: { field: 'productNo' }, row: view.result.rows[0], rowIndex: 0 }), { rowspan: 1, colspan: 1 })
  assert.equal(detailDescriptor.template.content.includes('KingZonShowForm'), false)
  for (const field of ['price', 'priceHaveTax', 'totalPrice', 'totalPriceHaveTax', 'orderTotalPrice', 'orderTotalPriceHaveTax', 'returnAmount', 'returnAmountHaveTax']) {
    assert.ok(detailDescriptor.template.content.includes('field="' + field + '"'))
  }
})
