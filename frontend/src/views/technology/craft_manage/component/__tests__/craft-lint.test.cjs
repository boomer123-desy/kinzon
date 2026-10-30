// Run with node --test using the host project's ESLint 8, eslint-plugin-vue 9,
// @typescript-eslint/parser and eslint-plugin 7, and TypeScript 5 dependencies.
const assert = require('node:assert/strict')
const path = require('node:path')
const { test } = require('node:test')
const { ESLint } = require('eslint')

test('craft components and shared types pass Vue prop ownership and TypeScript lint rules', async () => {
    // This snapshot has no project ESLint configuration. Do not change or weaken the host's rules.
    const eslint = new ESLint({
        useEslintrc: false,
        resolvePluginsRelativeTo: path.dirname(require.resolve('eslint-plugin-vue/package.json')),
        baseConfig: {
            root: true,
            env: { browser: true, es2022: true, node: true },
            parser: require.resolve('vue-eslint-parser'),
            parserOptions: {
                parser: require.resolve('@typescript-eslint/parser'),
                ecmaVersion: 'latest',
                sourceType: 'module'
            },
            plugins: ['vue', '@typescript-eslint'],
            extends: ['plugin:@typescript-eslint/recommended', 'plugin:vue/vue3-essential'],
            rules: { 'vue/no-mutating-props': 'error' }
        }
    })
    const root = path.resolve(__dirname, '../..').replaceAll('\\', '/')
    const results = await eslint.lintFiles([
        `${root}/**/*.vue`,
        path.resolve(__dirname, '../../../../../types/craft.ts')
    ])
    const messages = (await eslint.loadFormatter('stylish')).format(results)
    assert.equal(results.reduce((sum, result) => sum + result.errorCount + result.warningCount, 0), 0, messages)
})
