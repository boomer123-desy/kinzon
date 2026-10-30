# 项目开发约定

项目按 `frontend/`、`backend/` 统一归类，内部保留业务模块。页面、组件、前端类型和样式在 `frontend/`；Controller、DTO、实体、SQL、报表模板和后端验证在 `backend/`。目录说明见 `README.md`。

在本项目编写、修改或优化代码前，优先读取并遵循项目写法 skill：

[kingzon-project-style](C:/Users/11063/.codex/skills/kingzon-project-style/SKILL.md)

- 按任务读取 skill 中对应的后端、前端或工艺/排产参考，不必每次加载全部业务细节。
- 修改前核对目标模块现有 Controller、DTO、实体和 Vue 组件；沿用项目结构、命名、查询方式及组件约定。
- 后端通用写法优先参考 `backend/sale_return_order/controller/ProductController.cs`：在 Controller 内组织查询和保存，列表分页后按本页 ID 批量补充关联数据；编辑直接更新跟踪实体，子集合按新增、删除、修改处理，复用现有数据访问依赖与 IUnitOfWork。路由、分页参数、DTO 和业务口径仍以目标模块实际契约为准。
- 前端通用写法优先参考 `frontend/old-sale_return_order/index.vue`，并按页面类型对照退货、到货及库存的同类 index；普通列表采用现有页面骨架、SearchPanel、列组和 Drawer 处理方式。排产等特殊工作台布局仅对相应模块适用。
- 复杂前端界面参考 `frontend/technical/formula_manage` 的创建页及子组件。排版直接写在 div 的 `style` / `:style`，复用已有主题类；独立编辑、查询选择和重复业务区域按职责提取组件，简单布局留在父页。
- 前端样式先读 `frontend/technical/css/kingzon.css`，优先复用公共布局和交互 class。div 行内样式只补页面特有排版，不复制公共 CSS；检查现有高度及滚动规则，避免为局部需求改动全局样式。
- 前端 DTO 组织参考 `frontend/technical/types/technical.ts`：共享类型按领域集中，具名导出 class，属性显式声明类型与默认值，用本类的 `Partial` 构造参数及 `Object.assign` 初始化；区分聚合编辑模型、列表投影和前端专用字段。
- 普通表格参数与用法参考 `frontend/sale_return_order/components/kingzon-datagrid.vue` 及 skill 的表格参考。区分普通默认插槽和勾选模式的 `left/center/right`，按源码使用 props、透传属性及事件名，复用现有分页、勾选、列设置和导出能力。
- 树形表格使用 TreeGrid，其他数据表格使用 KingZonDataGrid；界面保持简洁。表单、选择区依原交互组织，不强制改成表格。
- 工艺 BOM 同步和排产物料查询的来源、用途不同，具体口径按 skill 及模块当前说明执行。
- 用户最新要求优先于 skill；源码或业务口径发生变化时，同步维护相关约定。

skill 安装在当前用户目录。更换机器或账户时需迁移该 skill 并更新上面的路径。
