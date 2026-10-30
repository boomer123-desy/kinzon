const assert = require('node:assert/strict')
const fs = require('node:fs')
const path = require('node:path')
const { test } = require('node:test')
const dependencies = path.resolve(__dirname, '../../../../../node_modules')
const ts = require(path.join(dependencies, 'typescript'))
const { parse, compileTemplate } = require(path.join(dependencies, '@vue/compiler-sfc'))
const filename = path.resolve(__dirname, '../material-demand-detail.vue')
const { descriptor } = parse(fs.readFileSync(filename, 'utf8'))
assert.deepEqual(compileTemplate({ source: descriptor.template.content, filename, id: filename }).errors, [])
const compiled = ts.transpileModule(descriptor.script.content, {
  compilerOptions: { module: ts.ModuleKind.CommonJS, target: ts.ScriptTarget.ES2020 }
})
const result = { exports: {} }
new Function('require', 'module', 'exports', compiled.outputText)(name => {
  if (name === 'vue') return { defineComponent: value => value }
  if (name === 'lodash') return require(path.join(dependencies, 'lodash'))
  if (name === '@/types/enums') return { MaterialDemandType: { 补料需求: 2 } }
  return {}
}, result, result.exports)
const options = result.exports.default
function instance(records = []) {
  const view = { records, materialDrawer: { collapse() {} },
    getTotalUnitList: async () => {}, getUnitConversition: async () => {},
    $kingzon_messager: { showError: error => { throw error } } }
  for (const name of ['_buildNewMaterialRow', 'addMaterial', 'cancelNewMaterial', 'handleMaterialSubmit']) {
    view[name] = options.methods[name].bind(view)
  }
  Object.defineProperty(view, 'maxOrder', { get: options.computed.maxOrder.bind(view) })
  return view
}

test('supplement numbering tolerates missing values and numeric strings without modifying the source', () => {
  const source = { id: 10, order: '12', demandQuantity: 9 }
  const view = instance([source, { id: 11 }, { id: 12, order: null }])
  view.addMaterial(source)
  view.addMaterial(source)
  assert.deepEqual(view.records.slice(-2).map(row => row.order), [13, 14])
  assert.equal(source.order, '12')
  assert.equal(source.demandQuantity, 9)
  assert.equal(view.records.at(-1).id, 0)
  assert.equal(view.records.at(-1).materialDemandType, 2)
})

test('cancel retains saved ordering and batch selection receives consecutive numeric orders', async () => {
  const saved = { id: 10, order: 5 }
  const view = instance([saved])
  view.addMaterial(saved)
  view.cancelNewMaterial(view.records.at(-1))
  assert.equal(saved.order, 5)
  await view.handleMaterialSubmit([{ productId: 1, order: 999 }, { productId: 2 }])
  assert.deepEqual(view.records.map(row => row.order), [5, 6, 7])
  assert.equal(instance().maxOrder, 0)
})
