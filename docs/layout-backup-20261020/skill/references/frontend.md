# 前端写法：列表页与复杂界面

## 参考顺序

以下路径相对于项目根目录。先读公共 `kingzon.css`，再按页面类型选参考：普通列表读旧退货 index；复杂编辑界面读 formula_manage 的创建页和子组件，再结合目标源码：

| 参考源码 | 用途 |
| --- | --- |
| `frontend/technical/css/kingzon.css` | 公共布局、尺寸、滚动、按钮、状态标签及单元格交互样式 |
| `frontend/technical/types/technical.ts` | 按领域组织 DTO class、属性默认值、构造初始化及聚合编辑数据 |
| `frontend/sale_return_order/components/kingzon-datagrid.vue` | 普通表格参数、插槽、事件、分页与勾选；详细说明见 [公共表格用法](kingzon-datagrid.md) |
| `frontend/old-sale_return_order/index.vue` | 普通列表首要参考：页面骨架、查询、请求、冻结列、行操作与 Drawer |
| `frontend/technical/formula_manage/create-formula.vue` | 复杂界面首要参考：基本信息、主编辑 Tabs、资料库侧栏、底部确认及组件组合 |
| `frontend/technical/formula_manage/create-formula-version.vue` | 复杂版本编辑、初始化后挂载、子组件 v-model |
| `frontend/technical/formula_manage/component/formula-search.vue` | 独立资料库查询、按类型切换 Tab、选择事件 |
| `frontend/technical/formula_manage/component/formula-search-table.vue` | 可复用查询表格、props 输入及分页/选择事件 |
| `frontend/technical/formula_manage/index.vue` | 多状态列表、Drawer 打开复杂编辑页 |
| `frontend/new-sale_return_order/index.vue` | 同类退货查询、普通列表及新增/编辑/文件交互 |
| `frontend/sale_return_order/sale_return_order_manage/index.vue` | 退货管理、单据状态、明细及文件功能 |
| `frontend/new-sale_return_order/return_arrival_management/index.vue` | 多状态 Tab、按 Tab 切换查询面板、子组件刷新 |
| `frontend/inventory/inventory_management/index.vue` | 按仓库/库存模式组织 Tab、查询选项及过滤条件 |

参考共同结构和处理方式，业务字段、类型、路由及状态条件以目标模块为准。`frontend/technical/production_schedule_manage/index.vue` 的左右工作台、甘特图及请求并发处理属于该模块，不作为普通列表页默认模板。

## 公共样式与页面排版

优先使用 `frontend/technical/css/kingzon.css` 已提供的 class，页面仅在使用处补充特有的 `style/:style`。下表是实际样式语义，修改前核对当前文件：

| class | 用途及布局约束 |
| --- | --- |
| `kingzon-page` | 100%宽高的纵向 flex，隐藏横纵溢出，带上下5px padding |
| `kingzon-page-title` | 在 kingzon-page 内生效；标题高30px，首个 span 提供左侧主题蓝线及字体样式 |
| `kingzon-page-body` | 在 kingzon-page 内生效；纵向 flex，高度为 `calc(100vh - 40px)` |
| `kingzon-page-content` | 100%宽高的纵向 flex，组织编辑区、表格及底部操作 |
| `kingzon-page-fill` | 在 content 后代中生效：100%宽、`flex: 1 1 auto`、`height: 0`、`overflow-y: scroll`；实际填充仍要求其父级是有高度边界的 flex 容器 |
| `kingzon-page-panel` / `kingzon-page-form` | 包含字段的 flex 换行区域，已有 padding/子项间距 |
| `kingzon-page-footer` / `kingzon-btn-group` | 底部居中和边距；按钮组为直接 a 子项设置最小宽度及间距，直接 div 子项 inline-block |
| `kingzon-textEllipsis` | 单行隐藏溢出并显示省略号，仍需所在容器有可用宽度约束 |
| `kingzon-tag` 加 success/info/warning/danger 变体 | 项目已有状态标签；变体完整名称为 `kingzon-tag-success` 等，复用原颜色、边框和圆角 |
| `kingzon-detail-text` / `kingzon-pointer` | 可点击详情的主题色、hover及选中行处理；pointer 只提供悬停光标，不自动绑定点击事件 |
| `kingzon-cell-editable` / `kingzon-cell-change` | 编辑提示底线及修改高亮，用在对应单元格；change 当前文件后面的定义覆盖前面定义 |
| `kingzon-cell-tips` | 使用伪元素展示 `labelTooltip` 属性内容，需要在实际元素上提供该属性 |
| `Kingzon-Red-Switch` / `Kingzon-Delete-Switch` | 已有 EasyUI 开关状态颜色，保留 class 的大小写 |
| `kingzon-fileitem` / `kingzon-input-addon` / `kingzon-text-grey` | 文件项、输入附加区及灰色提示等已有呈现 |

- 主题色使用项目已有 `var(--color-...)`，这些变量的定义不在本文件内，不臆造新主题变量或另设颜色体系。使用已有 tag 的圆角符合简洁界面要求，不需要删改公共外观。
- `kingzon-page-body` 的视口高度和 fill 的零高度/滚动规则会影响 Drawer、嵌套表单及左右区域。先检查父级高度链与谁负责滚动，需要时在当前 div 上补 `height/min-height/overflow`，不另复制整套公共布局，也不无依据覆盖全局选择器。
- 公共 `.combo-panel` 已限定下拉面板高度；已有组件覆盖、hover、伪元素等规则应复用，无法用行内样式表达的新增必要规则才放小范围 scoped 样式。
- 公共 CSS 中还包含特定看板样式，普通 CRUD 不因它们全局存在就套用。样式存在也不证明当前宿主已加载；接入时核对宿主加载位置，避免每个子组件重复引入整份全局 CSS。

## 标准页面骨架

- 页面顶部 `Loading v-show="isLoading"`，主体 `kingzon-page`。
- `kingzon-page-title` 放页面名称；`kingzon-page-body` 内先放 SearchPanel，再放 `kingzon-page-content`。
- content 中上方 `kingzon-btn-group` 放新增等操作，下方 KingZonDataGrid 使用 `kingzon-page-fill` 填充。参考页按钮组的 `margin: 5px` 和行内 ButtonGroup 的约24px高度，可按容器微调。
- 标准列表不另造 card、统计横条、独立 toolbar 或替换主题布局。Tabs、左右区域或大 Drawer 仅在具体业务需要时加入。
- 界面保持简洁，使用已有主题与 `kingzon-tag` 等类；无需样式时不新增 `<style>`。用户指定的旧 index 本身没有独立样式块。
- 必须解决长内容时添加必要宽高、`min-height: 0` 和 `overflow`，保留操作区可见；不因视觉统一把表单或选择区域改为表格。

## 复杂界面：div 直接排版，按业务拆组件

这是用户明确指定的复杂界面模板，参考 `frontend/technical/formula_manage`：

- 创建/编辑父页用已有 `kingzon-page-content`、`kingzon-page-fill` 和 `kingzon-page-footer`。基本信息在上方，中间 div 使用横向 flex，主编辑区 div 用 `flex: 1`，侧边资料库按内容设置宽度，底部放取消、确定操作。
- 公共骨架先用 kingzon.css 的 class；剩余排版直接写在对应 div 的 `style`，动态尺寸用 `:style`。页面特有 flex 方向、宽高、间距、padding、溢出与必要的 `min-height/min-width` 就近声明，不重复公共 class 已有属性；不要仅为这些属性创建一套页面 CSS class 或布局组件。
- Tabs、资料库和表格等组件自身的宽高也可直接写在使用处，和父 div 一起确定可用空间。示例侧栏550px/400px是配方和检验区域的当前宽度，实际功能按窗口和内容调整，不固定所有界面尺寸。
- 保留项目已有主题类和 CSS 变量。伪类、深层选择器或组件库行为修正无法通过行内样式表达时，可保留小范围 scoped 样式；例如参考 index 的菜单定位样式。布局行内书写不意味着新增装饰样式。
- 父页负责整体布局、当前 Tab、子区域模型、校验协调和最终提交；独立的基本信息、版本/明细编辑、检验及资料库属于可拆分的业务区域。
- 当区域有独立编辑、校验、查询、选择逻辑，或需要在多个地方复用时提取到 `component/`。组件按业务命名；简单容器、标题和一排按钮直接留在模板，不按 DOM 层级或固定行数拆分。
- 可复用表格再从查询区域中提取：现有 `formula-search.vue` 管查询条件、类型 Tab 和数据请求；`formula-search-table.vue` 接收 `theme/dataList`，负责 KingZonDataGrid 展示并上报 `refreshTable/choose`。避免每个资料类型复制一套表格和查询框架。
- 跨区域联动通过 props、命名 `v-model` 和业务事件；参考 `choose → chooseData`，由父页把选中资料放到当前编辑区域。子组件不直接改兄弟组件，也不无依据地修改父级 props。
- 需要收集多个表单校验和数据时，沿用已存在的组件 ref 方法，并用 `InstanceType<typeof Component>` 定义引用类型。父页统一校验、汇总并提交；新组件只提供实际需要的能力，不强制都实现相同的 getValid/getData 接口。
- 数据初始化后才能挂载的编辑区按需 `v-if="inited"`；切换时应保留查询或编辑状态的侧栏按需 `v-show`，不要无意销毁输入或重复发请求。
- 当前目录仅包含部分源码，创建页引用的若干编辑子组件定义未在快照中提供。它们证明既有组件边界，不代表可以凭空使用其内部字段或方法；实际开发查找宿主定义或实现本次确需的组件。

## 表格与列

- 使用公共表格前读取 [KingZonDataGrid 用法与参数](kingzon-datagrid.md)，再核对实际组件。普通模式和勾选模式的列插槽不同，组件 props 与透传属性也不同，不能只照抄某个页面的标签。
- 普通数据表格用 `KingZonDataGrid`，树形用 `TreeGrid`；库存 index 的 `DataGrid` 是业务子组件，不能仅凭名字把它当成另一套普通表格标准。
- 列表沿用 `ref="dataGrid"`、`:data`、`emptyMsg`、分页/排序事件；按需求开启 `showDownLoad` 和设置 `exportFileName`。只有无需行选择时用 `selectionMode="null"`。
- 复杂普通列表采用 `GridColumnGroup → GridHeaderRow → GridColumn`：左侧冻结序号、单号、编码、名称；中间业务列可移动；右侧冻结文件与操作。短表格可简化，不强行增加空列组。
- 序号用 `scope.rowIndex + 1`。编码和文本通常左对齐，状态居中，数量/金额右对齐；按内容设置宽度，长文本用 `overflowTextEllipsis`。
- 自定义单元格使用 `#body="scope"` 和 `scope.row`，状态用已有 tag 类，操作用 `ButtonGroup`、`LinkButton`。事件名、列字段及排序字段保持组件和接口契约。
- 日期用 `$utils.formatDateTime`；单位和数量精度来自业务定义。旧页面的固定“件”和金额 `.toFixed(4)` 只反映该页面口径，不能作为所有物料的单位和精度规则。
- 单据级字段合并仅在同单多明细确有需求时使用 `cellSpan` 和指定列名单。参考实现只合并当前数据中相邻相同单号，分页、排序或非连续数据不能跨段合并。
- 附加字段、文件按钮、详情点击按需要复用 `KingZonFileButton`、`FileUpload`、`FileList`、`kingzon-detail-text` 等，不给每个列表默认加上传功能。

## DTO 组织与初始化

以用户指定的 `frontend/technical/types/technical.ts` 为组织模板，字段仍以当前领域和后端 DTO 为准：

- 共享业务 DTO 按领域集中在 `types/`，通过具名导出 `export class XxxDto` 提供给页面和子组件，沿用实际 `@/types/...` 导入路径。不要在多个 Vue 文件或私有请求文件重复定义同一业务模型；局部拖动状态、组件引用等临时类型可保留在组件内。
- 同一领域按职责组织主资料、版本、明细、聚合编辑数据、资源查询、检验和列表投影。参考 `FormulaDto`、`FormulaVersionDto`、`FormulaDetailDto`、`FormulaVersionDataDto`、`FormulaResourceDto` 与 `FormulaIndexDto`，不用一个大 DTO 混合所有页面字段。
- 属性显式声明类型和默认值：必填文本通常 `string = ''`，数值按业务默认值初始化，枚举从 `@/types/enums` 导入并用枚举成员。可选字段采用 `?: T = undefined`；后端可空投影按契约采用 `T | null = null`。保留 undefined、null 和 0 的业务区别，版本号、状态及数量默认值不能跨模块照搬。
- 每个 class 使用 `constructor(init?: Partial<XxxDto>) { Object.assign(this, init) }`，其中泛型必须是当前类。参考中的 `FormulaInspectionDataDto` 构造参数误用了 `Partial<FormulaVersionDataDto>`，不复制该错误；真实契约中的历史字段拼写不随意改名。
- 聚合 DTO 用 `new XxxDto()` 初始化嵌套对象，集合用 `Array<XxxDto>` 明确元素类型，树节点用 `children: Array<本类> = []`。普通集合默认空数组；只有编辑流程需要初始空行时才使用 `[new XxxDto()]`。
- 用 `//前端使用` 或说明性注释标注编辑索引、底数、当前用量、可编辑状态、临时检验值及自定义参数等字段。提交按后端契约组织，不把所有临时计算字段自动视为保存字段。
- 新增和重置使用 `new XxxDto({...})` 取得类默认值。`Object.assign` 是浅赋值，不会递归实例化子 DTO、深拷贝或转换接口日期；`as XxxDto` 只是类型断言。需要嵌套默认值、编辑隔离或 Date 方法时，按实际消费位置显式构造、复制或转换，不能把 class 定义当作自动反序列化。

```ts
export class FormulaVersionDataDto {
  version: FormulaVersionDto = new FormulaVersionDto()
  detailList: Array<FormulaDetailDto> = [new FormulaDetailDto({})]
  index: number = 0

  constructor(init?: Partial<FormulaVersionDataDto>) {
    Object.assign(this, init)
  }
}
```

## Options API 与方法职责

- 默认 `<script lang="ts">`、`defineComponent`，业务状态放 `data()`，派生展示/模板枚举放 `computed`，行为集中在 `methods`。`created()` 调用 `init()`，固定 Dialog 引用可在 `mounted()` 获取。
- 列表数组 `[] as XxxDTO[]`，查询模型 `new XxxFilterDTO()`，搜索数组 `[] as Search[]`；已有 DTO/枚举从实际 `@/types/...` 导入，局部业务组件相对路径导入。不要另造与现有类型重复的页面数据模型。
- 常用职责：`init` 初始化选项及列表，`addSearchParam/buildSearchParam` 构造搜索项，`search` 分配过滤条件，`getData` 刷新列表，`getXXX` 执行 HTTP 请求，`onClickAdd/editRow/deleteRow` 处理用户操作，`addSuccess/addCancel` 响应子组件。
- 中文短注释说明业务动作。缩进、分号及现有方法名跟随实际文件，旧 index 是两空格，部分新 index 是四空格；不统一重排整个文件。
- 不为了普通增删改查新增 API client、通用 CRUD composable、复杂状态容器或组件注册框架。已有 async/await 页继续采用原方式，无需改成 Promise 链。
- 对新业务数据给出准确 DTO/事件类型；参考旧页的 `any`、`var`、拼写错误及冗余代码不是应复制的风格要求。历史拼写如果属于真实接口字段，先确认兼容性再修改。

## 查询与 HTTP

- SearchPanel 传 `searchParams`、`searchParamsHidden`，触发 `@search="search"`。常用条件放前者，扩展条件放后者；不要另写一套普通列表的筛选条。
- 使用 `new Search(field, label, table, FilterType, DataType, ...)`。选项使用 `{ value, text }`；客户等需先查询再映射，枚举用 `$utils.getEnumKeyValueArray`。日期范围按现有规则构造 >= 和 <= 两项。
- `search(params: FilterTmp[])` 按 `table` 查找，给对应 FilterDTO 属性赋 `params.find(...)?.filter ?? []` 后调用 `getData()`。表名及属性必须对应当前后端，不照搬旧退货表名。
- 简单列表保留独立 `getXXX(): Promise<HttpResponseType>` 请求方法，使用 `this.$http.get/post`；列表 POST 第三个参数传 `this.$refs.dataGrid`，由宿主处理分页排序，取 `res.data` 更新列表，不猜测 Axios 外层包装。
- `getData` 负责 `isLoading=true → 请求 → 更新列表 → catch提示 → finally=false`，通常使用参考页的 `.then/.catch/.finally`。依赖选项的初始化按顺序执行，并将下一步 Promise return 给链。
- 错误用 `$kingzon_messager.showError(err || '网络错误')`，成功用 `showInfo`。已有删除/完成确认沿用页面的 `$messager.confirm` 或实际封装，成功后重新查询。
- 只在实际存在快速筛选、切换版本或重复提交风险时加请求序号/提交状态，确保旧响应不会覆盖当前数据；不把排产页面的多种 token 固定加到每个简单 index。

## Drawer、Dialog 与子组件

- 新增/编辑沿用 `showDrawer` 一类状态、编辑行/DTO、标题和子组件。新增先创建新的 DTO，编辑传真实行或按编辑隔离需要复制。
- 懒加载 Drawer 用 `v-if`，`region="east"`、`closable`、`maskClosable=false`；先置显示状态，再在 `$nextTick` 中从 `$refs` 取 `EasyUI.Drawer` 并 `expand()`。
- Drawer 大小按表单内容参考旧页90%、文件页55%等现有比例，不能固定所有 Drawer 宽度。详情 Dialog 用 `EasyUI.Dialog.open/close`，内容保持现有子组件。
- 子组件通过 props 接收资料，`@submit` 通知父页关闭并 `getData()`，`@cancel` 关闭；是否取消也刷新按实际业务确定。关闭时先处理有效 ref，再移除 `v-if` 实例，`@collapse` 同步显示状态。
- Tabs 页面由 index 管查询与当前状态，数据展示放业务子组件。按 Tab/模式使用对应过滤列表，需要时 `$nextTick` 调用已挂载子组件刷新，避免刷新未激活或不存在的引用。

## 验证

普通列表检查查询映射、分页、排序、导出、状态按钮、Drawer 打开关闭及提交后刷新；Tab 页面检查筛选是否串页。接口/宿主依赖缺失时说明实际验证边界。

工艺与排产的专用交互和验证入口见 [模块约定](craft-and-schedule.md)，不用将这些检查固定套在普通退货列表上。
