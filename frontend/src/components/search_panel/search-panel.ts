import type { App, Plugin } from 'vue'
import SearchPanel from './search-panel.vue'

// 定义插件对象，实现 Plugin 接口
const plugin: Plugin = {
  install(app: App): void {
    // 注册一个全局组件
    app.component('SearchPanel', SearchPanel)
  }
}

export default plugin
