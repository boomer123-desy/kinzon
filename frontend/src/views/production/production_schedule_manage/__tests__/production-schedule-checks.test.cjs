const assert = require('node:assert/strict')
const fs = require('node:fs')
const os = require('node:os')
const path = require('node:path')
const { test } = require('node:test')
const ts = require('typescript')
const { parse, compileScript, compileTemplate } = require('@vue/compiler-sfc')
const { ESLint } = require('eslint')
const root = path.resolve(__dirname, '..')
const vueFiles = ['index.vue', ...fs.readdirSync(path.join(root, 'component')).filter(file => file.endsWith('.vue')).map(file => 'component/' + file)]
const modelFiles = ['schedule.ts', '__tests__/fixtures.ts']
const dtoFiles = ['production-schedule.ts', 'craft.ts', 'enums.ts']

test('all schedule Vue SFC scripts and templates compile', () => {
    for (const file of vueFiles) {
        const filename = path.join(root, file)
        const { descriptor, errors } = parse(fs.readFileSync(filename, 'utf8'), { filename })
        assert.deepEqual(errors, [])
        assert.deepEqual(compileTemplate({ source: descriptor.template.content, filename, id: filename }).errors, [])
        assert.ok(compileScript(descriptor, { id: filename }).content)
    }
})

test('schedule model and component scripts pass strict TypeScript checking', t => {
    // The host app/types are absent in this snapshot. Stub absent host HTTP/utilities, EasyUI refs and the shared Filter type; compile actual domain DTOs/enums.
    const folder = fs.mkdtempSync(path.join(os.tmpdir(), 'schedule-types-'))
    t.after(() => fs.rmSync(folder, { recursive: true, force: true }))
    fs.mkdirSync(path.join(folder, 'component'))
    fs.mkdirSync(path.join(folder, '__tests__'))
    fs.mkdirSync(path.join(folder, 'types'))
    for (const file of dtoFiles) fs.copyFileSync(path.join(root, '../../../types', file), path.join(folder, 'types', file))
    for (const file of modelFiles) fs.copyFileSync(path.join(root, file), path.join(folder, file))
    for (const file of vueFiles) {
        const { descriptor } = parse(fs.readFileSync(path.join(root, file), 'utf8'))
        fs.writeFileSync(path.join(folder, file + '.ts'), compileScript(descriptor, { id: file }).content)
    }
    fs.writeFileSync(path.join(folder, 'base-pc.d.ts'), `declare module '@kingzon/base-pc' { export interface Filter { field: string; value: unknown; filterType: number; dataType: number } } declare module '@/views/technology/craft_manage/component/resource-library.vue' {const component: import('vue').DefineComponent; export default component}`)
    fs.writeFileSync(path.join(folder, 'host.d.ts'), `import 'vue'; declare module 'vue' { interface ComponentCustomProperties { $kingzon_messager: { showInfo(message: string): void; showError(message: unknown): void; showConfim(message: string): Promise<void> }; $utils: { formatDateTime(value: string | Date, format: string): string }; $http: { get(url: string, params?: unknown): Promise<{ data: any }>; post(url: string, data: unknown, grid?: unknown): Promise<{ data: any }> }; $route?: { query: Record<string, unknown> } } } declare global { namespace EasyUI { interface Dialog { open(): void; close(): void } interface Drawer { expand(): void } } }`)
    const vuePackage = path.dirname(require.resolve('vue/package.json'))
    const program = ts.createProgram(vueFiles.map(file => path.join(folder, file + '.ts')).concat([...modelFiles, ...dtoFiles.map(file => 'types/' + file), 'base-pc.d.ts', 'host.d.ts'].map(file => path.join(folder, file))), {
        strict: true, noEmit: true, skipLibCheck: true, target: ts.ScriptTarget.ES2020, module: ts.ModuleKind.ESNext,
        moduleResolution: ts.ModuleResolutionKind.Node10, baseUrl: folder,
        paths: { vue: [vuePackage], '@svar-ui/vue-gantt': [path.join(root, '../../../../node_modules/@svar-ui/vue-gantt/types/index.d.ts')], '@/types/*': [path.join(folder, 'types/*')] }
    })
    const errors = ts.getPreEmitDiagnostics(program)
    assert.equal(errors.length, 0, ts.formatDiagnosticsWithColorAndContext(errors, { getCurrentDirectory: () => folder, getCanonicalFileName: x => x, getNewLine: () => '\n' }))
})

test('schedule code passes Vue ownership and TypeScript lint checks', async () => {
    const eslint = new ESLint({ useEslintrc: false, resolvePluginsRelativeTo: path.dirname(require.resolve('eslint-plugin-vue/package.json')), baseConfig: {
        root: true, env: { browser: true, es2022: true, node: true }, parser: require.resolve('vue-eslint-parser'),
        parserOptions: { parser: require.resolve('@typescript-eslint/parser'), ecmaVersion: 'latest', sourceType: 'module' },
        plugins: ['vue', '@typescript-eslint'], extends: ['plugin:@typescript-eslint/recommended', 'plugin:vue/vue3-essential'],
        rules: { 'vue/no-mutating-props': 'error' }
    } })
    const results = await eslint.lintFiles([...vueFiles, ...modelFiles].map(file => path.join(root, file)).concat(dtoFiles.map(file => path.join(root, '../../../types', file))))
    assert.equal(results.reduce((sum, result) => sum + result.errorCount + result.warningCount, 0), 0, (await eslint.loadFormatter('stylish')).format(results))
})
