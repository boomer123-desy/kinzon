# 工艺单管理截图问题变更清单

## 2026-10-19：资料库首次切换标签自动刷新

- `resource-library.vue` 监听 `selectTabIndex` 并立即初始化，外层物料/设备切换与内层资源标签共用 `changeTab`。等待 `$nextTick` 完成条件渲染、表格 ref 和分页配置更新后，再查询物料列表。
- 外层模式改变时重建 Tabs，清除旧筛选与选择；隐藏资料库、切换模式或连续选择标签时使旧请求失效，防止旧结果覆盖当前列表。设备继续沿用按 craftId 加载的机器列表。
- 45 项组件回归通过，包含首次显示、初始即显示、设备返回物料、内层分类切换和快速切换。条件挂载与请求使用隔离组件验证，未进行宿主页面联调。

## 2026-10-19：修正制造 BOM 产出单位快照

- `SyncMBomAsync` 将版本默认产出单位保存为 `UnitConversionId=-1`，对应快照 `Id=-1、Ratio=1、Quantity=1`；`TotalQuantity` 仍为一单位产品/规格产出。
- 产出单位来源改为 `_productService.GetProductUnitList`，按当前产品和规格选取 `ProductIdDto.UnitList`。使用 `GetProductUnitDto.Code` 作为转换 ID、`UnitId` 作为物理单位、`Text` 作为名称、`Value` 作为比例；`Value` 已相对于默认单位，不再取倒数作为 Ratio。
- 一默认单位产出时，各单位快照 `Quantity=1/Ratio`。例如一片等于五米时，接口返回米的比例为 0.2，保存数量为 5；次单位和各包装关系一并保存，同为“箱”的不同包装按转换 ID 区分。
- 默认单位必须为 `Code=-1、Value=1`，规格必须包含 `IsMainUnit` 的产品主单位关系。缺失单位列表、非法/重复转换 ID、缺失物理单位或非正比例时阻止同步；字符串比例支持服务当前区域格式和 invariant 小数格式，不把小数点当成千分位。
- 重新保存工艺版本触发同步时，覆盖该关联 BOM 版本的旧单位快照，保留 BOM 版本关联及工序物料汇总口径。没有批量更新历史数据库记录。
- 隔离回归编译实际 `ProductIdFieldDto.cs`，覆盖产品/规格产出、服务单位列表、不同包装、区域小数格式、重复保存修复旧数据、缺失/非法转换及事务回滚；ProductService 和基础资料查询使用测试替身，不代表完整宿主数据库联调。

## 2026-10-09：工艺版本保存同步制造 BOM

- `Technical_CraftOrderVersion` 新增后台维护的可空 `BomVersionId` 和唯一索引，工艺版本与制造 BOM 版本一一对应；新建时创建关联，历史版本在首次保存或发布时补建。版本列表、详情、新建响应返回关联 ID，保存入参不接受修改该关联。
- `CraftOrderManageController` 注入 `Production_MBom`、`Production_MBom_Version`、`Production_MBom_Detail` 的公共服务，在原版本事务内、全部工序物料落库后同步。按产品/规格维护 BOM 头，每个工艺版本维护独立明细；同类型、同物料的领用量换算至基本单位后汇总，不统计产出及已删除工序。
- 以一单位产品/规格产出为 BOM 基准，`Ratio` 表示每单位产出的耗用系数。物料量在汇总后按 BOM 的 `decimal(12,4)` 精度保存；输入的单位快照必须含有效的基本单位转换关系。替料分别合计，`MainDetailId` 指向 BOM 主明细；重复保存复用已有明细 ID，清除已移除的物料及替料。
- 主料、替料有 `RefCraftOrderVersionId` 时，一次查询其有效制造 BOM 关联，保存为 `BomDetailType.Bom`，`DetailId` 指向关联的 BOM 版本 ID。保留版本引用供递归展开；按最终类型和 ID 汇总、匹配旧明细，同物料不同引用版本分别统计。未关联或已停用的引用 BOM 明确报错，随版本保存事务回滚；不在父版本保存时重建引用版本的 BOM。
- 发布、取消发布、废弃、切换主版本同步 BOM 状态和主版本差值；删除工艺版本/工艺单时停用对应 BOM 版本，保留历史明细。存在其他有效版本时保留 BOM 头。
- 新增可重复执行的 MySQL/MariaDB 字段升级脚本 `seed/upgrade_craft_bom_version.sql`。须先部署用户提供的制造 BOM 实体对应的数据表，再应用工艺版本关联字段；本次未操作业务数据库。
- 新增可重跑的隔离验证 `tests/verify_mbom_sync.py`（Python、.NET SDK 9、EF Core SQLite 8.0.26）。使用实际同步方法和实体验证汇总、替料、单位换算、重复保存、版本隔离、状态、精度、删除、唯一关联、响应字段和失败回滚，以及主料/替料 BOM 引用、引用版本切换和失效引用回滚，共 50 项断言通过；完整控制器的 C# 语法检查通过。

验证范围：公共服务、基础资料/单位查询及工作流类型使用测试替身；不替代完整宿主项目编译、MySQL 事务联调和 DI/实体注册验证。测试未应用历史工艺映射中引用缺失 `InvalidTime`/`EffectiveTime` 列的检查约束；本次没有修改该已有映射。

## 2026-09-24：修正工艺树图标偶发不显示

- `v3-easyui` 的 `TreeGridRow` 未按业务行 ID 设置 Vue key；懒加载过程中切换工艺单、版本或替换物料时，旧行的内部 loading 状态可能被新行复用，并以 `tree-loading` 覆盖节点枚举对应的图标。
- 工艺单或版本上下文变化时重建 TreeGrid；同一版本内，仅在正在加载的节点被替换或删除时重建，普通字段编辑继续复用节点并保留展开状态。
- 修改文件：`frontend/src/views/technology/craft_manage/component/craft-tree.vue`、`component/__tests__/craft-tree.test.cjs`；另更新本清单。

验证：58 项检查、隔离 Vue/TypeScript 类型检查和生产预览构建通过。

## 2026-09-23：修正页面 Loading 状态

- 工艺单编辑页将工艺单信息、版本列表、工艺路线和保存操作的状态合并为统一 Loading；首次进入、切换工艺单和切换版本时都持续显示到对应数据加载完成。
- 工艺单、版本和路线请求分别增加请求序号。快速连续切换时，已过期请求不能覆盖当前数据，也不能提前关闭当前请求的 Loading。
- 工艺单列表查询增加相同的最新请求保护；删除工艺单时使用独立删除状态，并等待删除后的列表刷新完成后再结束 Loading。
- 修改文件：`frontend/src/views/technology/craft_manage/index.vue`、`frontend/src/views/technology/craft_manage/component/craft-order-header.vue`、`component/__tests__/craft-manage.test.cjs`；另更新本清单。

验证：58 项检查、隔离 Vue/TypeScript 类型检查和生产预览构建通过。

## 2026-09-23：完成工序文件上传

- `craft-file.vue` 按项目现有文件界面的方式接入 `KingZonFileButton`、`FileUpload` 和 `FileList`，使用 `FileStorageSourceType.工序文件`、`FileBase.Craft`，并以已保存工序的 `process.id` 作为来源 ID。
- 上传成功后关闭上传窗口、清空待上传文件并刷新文件列表；文件列表沿用公共组件提供的查看、下载和删除功能。
- 新增但尚未保存的工序不显示上传按钮，并提示先保存版本；只读版本不显示上传入口。
- `craft-editor.vue` 向文件页签传入当前工序和只读状态。
- `CraftOrderManageController` 注入公共 `FileStorageService`；删除单道工序、删除版本或删除整个工艺单时，同步清理 `FileStorageSourceType.工序文件` 下以工序 Id 关联的附件，避免产生孤立文件。
- 修改文件：`frontend/src/views/technology/craft_manage/component/craft-file.vue`、`craft-editor.vue`、`component/__tests__/craft-manage.test.cjs`、`backend/Technical/KingZon.OLCM.Technical.WebApi/Controllers/CraftOrderManageController.cs`；另更新本清单。

验证：57 项检查、隔离类型检查和预览构建通过。文件服务的实际上传、下载和删除仍需在完整业务环境中用真实文件联调。

## 2026-09-23：工艺树图标改用节点枚举

- 在 `frontend/src/types/craft.ts` 增加 `CraftTreeNodeType`，明确工序、领用、产出、成品、半成品、原材料和替料节点。
- `craft-tree.vue` 的 `TYPE_ICONS` 改为 `Record<CraftTreeNodeType, string>`，不再使用中文展示文本作为图标表的键。
- 物料节点通过项目已有的 `ProductType` 转换为树节点枚举；引用工艺树数据在接口入口统一补齐节点类型，并兼容旧接口仅返回 `typeText` 的数据。
- 修改文件：`frontend/src/types/craft.ts`、`frontend/src/views/technology/craft_manage/component/craft-tree.vue`、`component/__tests__/craft-tree.test.cjs`、`component/__tests__/craft-manage.test.cjs`；另更新本清单。

## 2026-09-23：清理外观覆盖并调整版本区域

- 移除之前新增的主题配色、卡片圆角、阴影以及按钮、页签、表格选中效果的外观覆盖；新增和保存按钮使用项目现有的 `btnCls="c8"`。
- 保留用于工作区伸缩、表格滚动、树层级显示及拖拽插入位置提示的必要样式，包括对应的 `:deep()` 规则。
- 版本描述独占一行，输入框铺满剩余宽度；主版本勾选移到版本标题行。版本项高度由 25px 增至 34px，版本标题行和描述行最小高度为 46px。
- 修改文件：`frontend/src/views/technology/craft_manage/create-craft.vue`、`index.vue`，以及 `component/craft-create-dialog.vue`、`craft-order-header.vue`、`resource-library.vue`、`craft-material-table.vue`；另更新本清单。

验证：56 项检查通过，隔离类型检查及预览构建通过。构建保留 EasyUI 依赖自身的 `eval` 警告；未进行完整业务应用的浏览器联调。本次不改后端和数据库。

## 2026-09-23：合并独立样式与工序规则

- 删除 `craft-manage.css`，原样式按作用范围合并到 `create-craft.vue`、`index.vue`、`component/craft-create-dialog.vue` 的 `<style scoped>`。第三方组件内部样式使用 `:deep()`，不影响其他业务页面。
- 删除 `process-rules.ts`，外协前工序强制入库、非入库工序清理产出物料集中到 `create-craft.vue` 的 `normalizeRoutes` 方法，强制入库状态由页面计算属性判断。
- `craft-order-header.vue` 通过 `beforeSave` 事件要求父页面同步处理规则，再组装保存数据；路线调整通过 `update:routes` 交由父页面处理。
- 更新 `craft-manage.test.cjs` 和 `craft-lint.test.cjs`，保持保存前规则校验及不修改子组件 props 的测试覆盖。

验证：56 项检查通过，隔离类型检查通过；本次没有改变后端或数据库。

## 2026-09-23：prop 报错与代码组织统一

截图报错来自 `vue/no-mutating-props`，类型检查无法代替 ESLint。此次保留该规则，没有添加禁用注释。

- 参照退货单、单位转换及配方管理界面，工艺模块所有 Vue 组件统一使用 `defineComponent` + Options API。
- `create-craft.vue` 拥有工序和物料数据；子组件通过 `updateProcess` / `updateMaterial` 事件上报修改，`craft-editor.vue` 负责转发。修改后保持工序、主料对象身份，避免输入失焦和选中行丢失。
- 修复工序表单、机器添加/删除/工时编辑、物料添加/删除/替料、表格数量/损耗率/工艺版本的直接或嵌套数据修改。
- 工序路线排序和保存使用独立的路线数据副本，不修改传入的 `routes`。
- 共享事件和组件引用类型统一放入 `frontend/src/types/craft.ts`；组件使用项目常见的直接 `$refs` 类型声明。删除 `craft-ui.ts`，不再保留单独的引用包装层。
- 沿用现有 `component` 目录、EasyUI/KingZon 组件、页面公共样式和 `$http` 接口调用方式。本次不改后端及数据库结构。

本次修改文件：

- `frontend/src/views/technology/craft_manage/create-craft.vue`、`index.vue`
- `frontend/src/views/technology/craft_manage/component/craft-process-info.vue`、`craft-machine.vue`、`craft-info.vue`、`craft-material-table.vue`、`craft-editor.vue`
- `frontend/src/views/technology/craft_manage/component/craft-order-header.vue`、`craft-tree.vue`、`resource-library.vue`、`craft-create-dialog.vue`、`craft-file.vue`
- `frontend/src/types/craft.ts`
- 删除：`frontend/src/views/technology/craft_manage/craft-ui.ts`
- 测试：`frontend/src/views/technology/craft_manage/component/__tests__/craft-manage.test.cjs`、`craft-tree.test.cjs`；新增 `craft-lint.test.cjs`
- 本变更清单

验证：55 项行为回归（包含冻结 props 的修改测试）、Vue/TypeScript ESLint 规则检查、隔离 vue-tsc 严格类型检查及预览构建。工作区没有完整项目的 ESLint 配置，使用 `vue3-essential`、TypeScript `recommended` 及启用的 `vue/no-mutating-props`；没有修改宿主项目的 lint 配置。完整项目集成与界面联调仍需在宿主环境验证。

## 2026-09-22：第 1–9 项

本次对应截图第 1–9 项，并补充资料库加载提示。以下为代码实现及隔离测试结果，尚未在完整业务应用和实际数据库中联调。

| 截图项 | 本次行为 |
| --- | --- |
| 1 | 只有勾选入库的工序显示入库物料节点和编辑区域；引用工艺树采用同一规则。外协本身不再作为显示条件。 |
| 2 | 顶层工序节点打开工序页签；领用/入库分组和主料打开物料页签，主料同时定位对应记录。引用工艺及替料不参与跳转。 |
| 3 | 用稳定的 uiKey 标识工序和编辑区，工序编号可修改，输入过程中不因编号变化重建编辑区。排序只更新 sequence，保留用户编号；保存校验编号非空、唯一、长度不超过 50。 |
| 4 | 按钮或拖动添加成功后清空资料库选择；添加失败、重复或取消拖动不清空。 |
| 5 | 增加技术型号、规格型号、材质牌号、工艺版本、备注列及名称悬浮提示。支持规格及版本筛选；版本取已发布工艺版本，优先主版本。产品实体没有独立的产品版本字段。 |
| 6 | 取消入库后清除入库主料及其嵌套/历史平铺替料，保存端再次处理。先执行外协前工序强制入库规则，以免误清必须入库的物料。只读历史版本不在前端清料。 |
| 7 | 工序增加 TheoreticalWorkHours，工序设备关联增加 TheoreticalWorkHours。单位小时，4 位小数，范围 0–99999999.9999；随版本保存并回读。 |
| 8 | CreateCraftVersion 在同一事务内创建编号 10 的默认工序及工作流节点。 |
| 9 | 工序增加 IsReportWork 布尔字段及是否报工勾选项，默认 false，随版本保存并回读。 |

资料库增加请求期间的加载提示；旧请求不能覆盖新请求结果或提前结束新请求的加载状态。物料查询使用后台默认类型，避免依赖未提供的前端原材料枚举定义。

## 改动文件

相对于工作区根目录：

| 文件 | 用途 |
| --- | --- |
| `frontend/src/views/technology/craft_manage/create-craft.vue` | 稳定工序选择、树导航、成功添加后清空资料库 |
| `frontend/src/views/technology/craft_manage/component/craft-order-header.vue` | 稳定路线标识、可编辑编号校验、保存新增字段 |
| `frontend/src/views/technology/craft_manage/component/craft-editor.vue` | 页签切换和添加成功事件传递 |
| `frontend/src/views/technology/craft_manage/component/craft-tree.vue` | 入库节点条件显示和节点导航信息 |
| `frontend/src/views/technology/craft_manage/component/craft-process-info.vue` | 编号可编辑、工时和是否报工配置 |
| `frontend/src/views/technology/craft_manage/component/craft-info.vue` | 入库区域条件、拖动添加成功事件 |
| `frontend/src/views/technology/craft_manage/component/craft-machine.vue` | 每台设备的工时编辑、删除同步配置、拖动成功事件 |
| `frontend/src/views/technology/craft_manage/component/resource-library.vue` | 补齐资料列、提示、筛选、加载和清空选择 |
| `frontend/src/views/technology/craft_manage/process-rules.ts` | 9 月 23 日已合并入 `create-craft.vue` 并删除 |
| `frontend/src/views/technology/craft_manage/craft-ui.ts` | 原组件接口文件，9 月 23 日已移入 `types/craft.ts` 并删除 |
| `frontend/src/types/craft.ts` | 稳定标识、工时、报工、设备配置及资料字段类型 |
| `backend/Technical/KingZon.OLCM.Technical.WebApi/Controllers/CraftOrderManageController.cs` | 资料查询、树节点、默认工序、字段保存读取及业务校验 |
| `backend/Technical/KingZon.OLCM.Technical.WebApi/Dto/CraftDto.cs` | 资料字段、工序工时、设备工时配置、报工字段 |
| `backend/Technical/KingZon.OLCM.Technical.DataBase/Entities/Technical_NodeSettingTemplate.cs` | 工序工时及报工实体字段和映射 |
| `backend/Technical/KingZon.OLCM.Technical.DataBase/Entities/Technical_NodeSettingTemplate_Machine_RL.cs` | 设备关联工时字段和映射 |
| `backend/Technical/sql/upgrade_craft_work_hours.sql` | MySQL/MariaDB 可重复执行的字段升级脚本 |
| `frontend/src/views/technology/craft_manage/component/__tests__/craft-manage.test.cjs` | 管理界面行为回归测试 |
| `frontend/src/views/technology/craft_manage/component/__tests__/craft-tree.test.cjs` | 树节点与展开状态回归测试 |
| `docs/technical/CRAFT_CHANGELOG.md` | 本清单 |

## 验证与部署

- 49 项 Vue 行为回归全部通过，测试使用真实 Vue 响应式机制，未加载完整业务页面。
- 隔离 vue-tsc 严格检查 0 错误；完整项目的全局组件及部分共享类型不在本快照中，使用接口替身。
- Vite 预览构建成功；第三方 EasyUI 包自身有 eval 构建警告。
- 提取本次后台查询，在 EF Core/Pomelo 隔离环境验证 MySQL SQL 翻译，并用 SQLite 执行验证资料字段及已发布版本选择。
- 使用实际工序及设备关联实体和摘取的控制器方法验证默认工序创建、工时及报工落库、取消入库清料、外协前工序规则和非法输入拒绝。宿主工作流服务、事务及物料保存使用测试替身，未替代完整服务集成测试。
- 工作区没有完整 `.csproj`、前端 `package.json`/`tsconfig` 或实际数据库连接，因此不能据此宣称整个项目无错误或线上已修复。

部署前先选择实际业务数据库并执行 `seed/upgrade_craft_work_hours.sql`，再同步部署前后端代码。该脚本本次尚未执行到实际数据库，历史工时默认为 0、是否报工默认为 false。
