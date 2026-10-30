# KingZonDataGrid 用法与参数

以项目根目录下 `frontend/sale_return_order/components/kingzon-datagrid.vue` 为准。普通列表参考 `frontend/old-sale_return_order/index.vue`；勾选示例参考 `frontend/sale_return_order/sale_return_order_manage/choice-customer-product.vue`。这是 EasyUI DataGrid 的项目封装，树形数据继续用 TreeGrid。

## 两种列组织方式

- `isCheckBoxSelection=false` 是默认模式，默认插槽接收业务列；按目标页面采用 GridColumn 或 `GridColumnGroup → GridHeaderRow → GridColumn`。冻结列组由页面定义。
- `isCheckBoxSelection=true` 时，封装自动生成左侧冻结列组和勾选列。`#left/#center/#right` 直接放 GridColumn，不重复包 GridColumnGroup/GridHeaderRow。center/right 只在提供对应插槽时生成，默认插槽在此模式不显示。
- 多级表头可用 `#multiLeftHeaderRow/#multiCenterHeaderRow/#multiRightHeaderRow` 提供额外 GridHeaderRow；相应首行仍在 left/center/right 中提供列。按实际表头配置 rowspan/colspan。
- `#detail="{ row }"` 和 `#group="{ row }"` 分别提供行详情和分组内容，封装只向它们传 row。GridColumn 的 `#body="scope"` 则沿用 EasyUI 的行/行索引作用域，不混淆两者。

## 组件声明的 props

| 参数 | 类型 / 默认值 | 实际用途 |
| --- | --- | --- |
| `initSelectedRows` | Array / `[]` | 初始选择集合，挂载及深度监听变化时用 JSON 复制到内部 selectedRows |
| `isCheckBoxSelection` | Boolean / `false` | 启用封装勾选逻辑及具名列插槽 |
| `leftGroupRowWidth` | Number / `200` | 勾选模式左冻结列组宽度，包含勾选列，按业务列总宽度设置 |
| `rightGroupRowWidth` | Number / `200` | 勾选模式右冻结列组宽度；center 当前模板宽度为200，没有 centerGroupRowWidth prop |
| `isShowCheckBox` | Boolean / `true` | 是否显示40px勾选列，不改变当前列插槽模式 |
| `showDownLoad` | Boolean / `false` | 显示导出入口；保留源码中的 DownLoad 大小写 |
| `exportFileName` | String / `'导出数据'` | 导出文件名前缀，组件追加时间戳及 `.xlsx` |
| `downloadLeft` | Number / `10` | 导出图标水平位置，单位px |
| `isDownLoad` | Boolean / `false` | 勾选模式下允许显示导出图标；普通模式只需 showDownLoad |
| `virtualScroll` | Boolean / `false` | 经内部 virtualScrollLocal 传给 DataGrid，导出时临时关闭后恢复 |
| `isDownloadRight` | Boolean / `false` | 导出时追加右侧表头和数据；操作列一般不导出 |
| `showFieldFilter` | Boolean / `true` | 列设置入口：FieldFilter 操作实际表格列并保存设置，不是后端业务查询筛选 |
| `splitFields` | Array / `[]` | 按字段名指定中间列的多单位导出拆列，详见导出约束 |

布尔参数用 `:参数="false/true"`；字符串 `"false"` 不等于 Boolean false。参数名按现有模板及源码，不自行改名。

## 透传属性、分页与请求

- 封装用 `v-bind="$attrs"` 传给内层 DataGrid。`data`、`id`、`idField`、`emptyMsg`、`selectionMode`、`cellSpan`、`sorts`、`pagination`、列移动/尺寸等按内层及现有页面契约使用，它们不是上表的封装 props。没有读取宿主 EasyUI 定义时，不臆造更多属性及回调参数。
- 组件默认开启 columnMoving、columnResizing、multiSort、pagination、lazy；默认页号1、每页20，pageList为 `[20, 50, 100, 1000]`。`virtualScroll` 是声明的 prop，通过内部状态单独传入。
- 分页、排序处理分别更新组件的 `pageIndex/pageSize` 和 `sorts`，不会自动向业务接口请求。父页沿用 `@pageChange="getData"`、`@sortChange="getData"` 发起查询；这些监听由 `$attrs` 传给内层 DataGrid，封装没有另行 `$emit` 同名事件。
- 列表请求沿用 `this.$http.post(url, filterDto, this.$refs.dataGrid)` 的现有宿主分页约定，响应数据更新业务数组。宿主 HTTP 封装的总数回填及分页协议需核对实际定义，不猜响应 total 字段，也不新增另一套分页模型覆盖组件状态。
- 本地小表格按实际需要关闭分页，仍使用现有透传方式。开启勾选时显式设置唯一 `idField`；同一路由下多个表格设置不同 `id`，避免列设置使用相同存储键。

## 勾选、事件与 ref

| 事件 | 参数与触发时机 |
| --- | --- |
| `rendered` | 无参数；封装完成勾选恢复及行高处理后发出 |
| `seletedRowsChanged` | 当前内部 selectedRows 数组；单行勾选或表头全选后发出，保留源码拼写 seleted |
| `singleRowChange` | 当前 row；单行勾选后发出，表头全选不发出 |

- `selectedRows` 是按 idField 去重的跨页选择集合；`checkedRows` 是当前页中 selected 为真的行。渲染时用 selectedRows 恢复当前页标记，不把当前页勾选等同于所有已选择行。
- 表头全选只处理当前 DataGrid 数据，跳过 `row.checkBoxDisabled`；行 CheckBox 同样遵守该字段。`row.unCheckeText` 为真时用文本替代勾选框；全选循环只检查 checkBoxDisabled，不能只靠 unCheckeText 禁止选择。
- 封装会修改行的 `selected` 字段，选择用 `selectionMode="null"` 可关闭另一路行选择。`initSelectedRows` 是初始/外部同步输入，不是名为 selectedRows 的 v-model。
- ref 可读取现有 `selectedRows/checkedRows/addedRows/removedRows`；后两者相对 initSelectedRows 按 idField 比较。内部表格通过 `.dataGrid` 访问，需要调用 EasyUI 方法时先核对定义和挂载状态，不把封装 ref 直接当作 EasyUI DataGrid。
- 列顺序、隐藏、冻结及宽度按 `${route.path}_datagrid_${id || 'default'}` 保存到 localStorage。字段和标题保持稳定；源码主要保存普通列或左/中列组，不声称会保存任意右侧列和所有多级表头配置。

勾选模式示例（业务 DTO、查询 URL 和列字段以实际模块为准）：

```vue
<KingZonDataGrid ref="dataGrid" class="kingzon-page-fill"
  id="materialChoiceGrid" idField="id" :data="materialList"
  :isCheckBoxSelection="true" :initSelectedRows="selectedMaterials"
  :leftGroupRowWidth="240" selectionMode="null" emptyMsg="暂无物料"
  @seletedRowsChanged="selectedMaterials = $event"
  @pageChange="getData" @sortChange="getData">
  <template #left>
    <GridColumn field="code" title="物料编码" :width="200" />
  </template>
  <template #center>
    <GridColumn field="name" title="物料名称" :width="180" />
  </template>
</KingZonDataGrid>
```

## 导出与验证边界

- 导出读取当前表格 DOM 和已加载数据，没有查询所有服务端分页数据；普通分页列表不能表述为全量导出。虚拟滚动导出临时渲染 `$attrs.data`，仍不代表获取数据库全量。
- 当前实现按左/中/右分区表格处理导出，主要导出左/中，右侧由 isDownloadRight 决定。简单无列组或不同 DOM 结构先检查兼容性，不无依据宣称所有布局都支持。
- `splitFields` 对应 `centerColumns` 的字段：解析单元格中的“数值 + 单位”文本，按每个字段出现的单位创建导出子列，保留主单位文本，缺失单位填 `/`。它不做物料单位换算，也不改变页面列或后端数量；需要此功能时核对实际单元格文本和多级表头。
- 使用现有组件能力，不为业务页面复制公共组件的列存储、跨页勾选或导出实现。公共行为确需改变时另按明确需求修改组件，并检查其他使用页。
- 验证与改动相关的普通/勾选插槽、idField、分页排序查询、跨页勾选、禁选行、列设置隔离及导出范围。只有文档更新时验证结构、引用和参数与源码一致，不运行无关业务测试。
