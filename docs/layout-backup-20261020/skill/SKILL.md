---
name: kingzon-project-style
description: 遵循 KingZon returnOrder 项目写法，普通列表参考旧退货 index，复杂界面参考 frontend/technical/formula_manage，复用 kingzon.css；DTO 组织参考 frontend/technical/types/technical.ts，普通表格参数参考公共 kingzon-datagrid.vue。页面特有排版直接写在 div 的 style 中，按业务职责提取组件。前端采用 Vue 3 Options API 和现有 KingZon/EasyUI 控件；后端优先参考 backend/sale_return_order/controller/ProductController.cs，在 Controller 内组织 EF 查询、分页关联补充及保存。用于本项目及用户指定沿用此风格的开发和优化，编写代码前优先读取。
---

# KingZon 项目写法

## 适用范围

原始工作区为 `D:/File/项目文件/实习/returnOrder`。实际操作以当前工作区为准，以下源码路径均相对于项目根目录。用于这个项目，或用户明确要求采用相同写法的项目；不要自动把业务规则套到其他项目。

项目已按 `frontend/`、`backend/` 统一归类，内部保留业务模块。原前端 `web`、`front`、`view` 目录的内容直接放在 `frontend/业务模块/`；后端源码、SQL、模板、工具及后端测试归入 `backend/业务模块/`。完整目录说明见项目根目录 `README.md`。

普通前端列表参考用户指定的 `frontend/old-sale_return_order/index.vue` 及退货、到货、库存的同类 index；复杂编辑界面参考用户指定的 `frontend/technical/formula_manage`，重点阅读创建页和资料库子组件。公共布局和主题以 `frontend/technical/css/kingzon.css` 为准，页面特有排版直接写在 div 的 style 中，按实际业务职责提取组件。业务字段、接口和类型以目标模块为准；工艺与排产页面只作为对应特殊交互的参考。

区分三类依据：参考页面决定通用写法；目标源码决定真实契约；已确认业务约定只对相应模块生效。用户的新要求优先于本 skill，修改前再次核对源码。

## 编写前先读

1. 读取项目及目标目录的 `AGENTS.md`，定位实际修改模块。
2. 后端优先参考 `backend/sale_return_order/controller/ProductController.cs`，按任务阅读列表、详情或保存方法，再核对目标 Controller、DTO/实体和路由；前端先读 `frontend/technical/css/kingzon.css`，再按类型选择参考：普通列表读 `frontend/old-sale_return_order/index.vue`，复杂编辑界面读 `frontend/technical/formula_manage/create-formula.vue` 和相关子组件。涉及数据类型时读 `frontend/technical/types/technical.ts`；涉及普通表格时读 `frontend/sale_return_order/components/kingzon-datagrid.vue` 及 [表格组件用法](references/kingzon-datagrid.md)，核对目标模块。详细入口见 [前端写法](references/frontend.md)。
3. 后端任务读取 [后端写法](references/backend.md)；界面任务读取 [前端写法](references/frontend.md)；涉及工序物料、BOM 或排产数量时，再读取 [模块约定](references/craft-and-schedule.md)。只读与任务有关的参考。
4. 在现有目录内修改，复用已有查询依赖、单位工具、消息及表格组件。实体或公共依赖缺失时先查找真实定义，必要时询问，避免猜字段、枚举值和业务数量。

## 默认写法

- C# 保持现有块状 namespace、四空格缩进、花括号格式。私有依赖命名 `_xxxService`，构造参数和赋值分行，DTO 属性初始化逐字段分行，字符串默认值用 `string.Empty`。
- Controller 以 `ProductController.cs` 为首要通用参考，沿用 `KingZonControllerBase`、目标领域路由、逐字段投影与 `Ok(data)`。列表先查询、筛选和分页，再按本页 ID 批量补充关联数据；分页参数和排序按目标接口契约执行。
- 查询与业务编排直接放在 Controller，复用 `CommonService<T>`、`ProductService` 等已有依赖；重复业务提取私有方法，不为本次功能新增查询 Service、通用仓储或映射框架。LINQ 查询表达式和链式调用按查询结构选用。
- 编辑时加载跟踪实体并逐字段赋值，子集合按新增、删除、修改处理，通过 `IUnitOfWork` 保存；多表原子保存沿用异步流事务，需要生成主键时先保存再关联。
- 批量读取再计算，简单关联保留清晰的 `Where` / `FirstOrDefault`，重复查找较多时用 `GroupBy`、`ToLookup` 和字典。只读查询按需 `AsNoTracking`，编辑查询保留跟踪；递归展开前收集数据，避免递归内逐项查库。
- 前端默认采用参考 index 的 `<template>`、`<script lang="ts">` 与 `defineComponent` Options API，业务状态放 `data()`、处理方法放 `methods`。常规列表沿用 `created → init → 构建查询条件 → 查询列表`，不为普通 CRUD 新增通用页面框架或 API 包装层。
- 前端共享 DTO 按领域集中在 `types/` 文件，参考 `technical.ts` 的具名导出 class、显式属性类型和默认值、`constructor(init?: Partial<本类>)` 与 `Object.assign(this, init)`。主资料、版本、明细、编辑聚合和列表投影按职责区分；组件复用领域 DTO，前端专用字段标注用途，具体规则见前端参考。
- 列表页使用 `kingzon-page → title/body → SearchPanel → content → 按钮组/KingZonDataGrid`，保留 `kingzon-page-fill`。通常分左侧身份冻结列、中间业务列、右侧操作冻结列；复杂树、甘特图或多区域工作台按具体需求处理。
- 公共布局、按钮间距、状态标签、可点击详情、文本省略和可编辑单元格优先复用 `kingzon.css` 的现有 class。读实际选择器、尺寸和溢出规则，不在页面复制一套相同 CSS，也不为局部布局修改公共样式。
- 复杂界面按 formula_manage 的方式用 div 直接排版：公共 class 之外的 flex 方向、宽高、间距和滚动写在对应 div 的 `style` / `:style`。父页组合主编辑区、Tabs、资料库侧栏和底部操作；独立编辑、查询选择或重复业务区域按职责提取子组件，不把所有逻辑堆在父页，也不为每个 div 拆组件。
- 请求沿用参考 index 的 `this.$http`、独立 `getXXX(): Promise<HttpResponseType>` 方法及 `.then/.catch/.finally`；列表请求传 `this.$refs.dataGrid`，按 `res.data` 更新。已有 async/await 页面保持现有方式，不批量改写。
- 搜索使用 `Search`、`FilterTmp[]`、领域 FilterDTO，按 `table` 将条件映射到对应过滤列表。复用 `@/types`、`$utils`、`$kingzon_messager` 和现有 EasyUI 控件。
- 树形表格用 `TreeGrid`，其他需要表格展示的区域用 `KingZonDataGrid`；表单与工序选择区域不因统一组件而强制改成表格。
- KingZonDataGrid 区分组件声明的 props 和向 EasyUI DataGrid 透传的属性。普通模式用默认列插槽，勾选模式用 `left/center/right` 列插槽并指定 `idField`；事件拼写、分页请求与导出范围按公共组件源码及表格参考执行，不另造一套公共表格。
- 界面保持简洁，沿用项目组件和主题样式。新增 CSS 主要解决布局、宽高、溢出和必要交互，不随意添加装饰色、阴影、圆角或额外卡片。
- 优化围绕减少重复读取、重复计算和杂乱布局，保持数据来源、精度、关联 ID、选择方式及对外字段契约。改变口径前核对用户需求。

## 验证与交付

选择与改动有关的现有验证入口，见参考文档。查询汇总重点检查重复物料、单位换算、替料、引用版本、软删除与空数据；界面重点检查选择、草稿、滚动和加载状态。文档或 skill 修改验证结构与链接即可，不必运行无关业务测试。

该工作区是业务模块源码集合，目前缺少完整宿主 `.sln/.csproj` 和部分公共定义。隔离测试通过只证明覆盖范围内的编译及行为，不能表述为完整宿主构建或真实数据库联调通过。说明实际改动、实际验证及必要限制。

## 代表性源码入口

| 用途 | 相对路径 |
| --- | --- |
| 后端首要参考：Controller 编排、分页后关联补充、详情与集合保存 | `backend/sale_return_order/controller/ProductController.cs` |
| Controller、事务及关联保存 | `backend/technical/craft/controller/CraftOrderManageController.cs` |
| 列表投影、筛选、分页 | `backend/technical/craft/controller/FormulaManageController.cs` |
| DTO 与命名 | `backend/technical/craft/dto/CraftDto.cs` |
| Controller 内批量查询与需求计算 | `backend/technical/production_schedule/controller/ProductionScheduleManageController.cs` |
| 普通列表参考：骨架、请求、查询、Drawer | `frontend/old-sale_return_order/index.vue` |
| 公共页面布局、主题及交互样式 | `frontend/technical/css/kingzon.css` |
| 前端领域 DTO、默认值与编辑聚合模型 | `frontend/technical/types/technical.ts` |
| 公共表格 props、插槽、事件、勾选、分页及导出 | `frontend/sale_return_order/components/kingzon-datagrid.vue` |
| 复杂界面：div 直接排版、主编辑区/侧栏/底部操作 | `frontend/technical/formula_manage/create-formula.vue` |
| 复杂版本编辑：初始化、Tabs、子组件模型 | `frontend/technical/formula_manage/create-formula-version.vue` |
| 资料库侧栏与查询表格的组件拆分 | `frontend/technical/formula_manage/component/formula-search.vue` |
| 同类退货列表、查询与编辑 | `frontend/new-sale_return_order/index.vue` |
| 普通表格、文件及详情交互 | `frontend/sale_return_order/sale_return_order_manage/index.vue` |
| 多状态 Tab 与子组件刷新 | `frontend/new-sale_return_order/return_arrival_management/index.vue` |
| 库存 Tab 与按模式切换查询条件 | `frontend/inventory/inventory_management/index.vue` |
| 工艺模块特定的列表与版本管理 | `frontend/technical/craft_manage/index.vue` |
| 编辑表格与子组件事件 | `frontend/technical/craft_manage/component/craft-material-table.vue` |
| TreeGrid、左右布局与排产列表 | `frontend/technical/production_schedule_manage/index.vue` |
| 工序表单与滚动区域 | `frontend/technical/production_schedule_manage/component/create-schedule.vue` |
