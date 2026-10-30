# 工艺、BOM 与排产的模块约定

记录已确认的业务选择，避免风格优化改变含义。以当前实体、Controller 和模块说明为准，不自动扩展到其他功能。

## 工序物料保存与制造 BOM

源码入口：`backend/technical/craft/controller/CraftOrderManageController.cs`、`backend/technical/craft/entities/Production_MBom*.cs`、`backend/technical/craft/entities/Technical_CraftOrderVersion.cs`。

- 每个工艺版本维护自己的一套制造 BOM 版本，通过 `BomVersionId` 关联；不能只为主工艺版本维护 BOM。
- 保存工序领用物料的同时，在同一事务维护 `Production_MBom`、版本及明细。汇总该版本所有有效工序的输入物料，产出不计入，保留主料与替料关系。
- 主料按最终 BOM 类型和目标 ID 汇总、匹配；引用工艺版本不同的同物料不能误合并。单位统一后合计，再按四位小数舍入。
- 替料 `MainDetailId` 指向 BOM 主明细真实 ID；为取得该 ID 的保存仍位于外层事务内。
- 带 `RefCraftOrderVersionId` 的物料映射为 `BomDetailType.Bom`，`DetailId` 是所引用的 **BOM 版本 ID**，不是 BOM 主表 ID。主料和替料都参与版本映射，缺失引用明确报错，不退回普通产品。
- 父版本保存不隐式重建所引用的其他版本。状态和主版本标记遵循同步代码，不凭枚举名称推测完整生命周期。
- 枚举当前约定：`BomStatus` 的待审核=1、已生效=2、淘汰=3；`BomDetailType` 的 Product=1、Formula=2、Specication=3、Bom=4。`Specication` 是现有拼写，勿擅自改名。

升级入口 `backend/technical/craft/seed/upgrade_craft_bom_version.sql`。脚本存在不表示已执行；修改实体、映射及迁移时核对真实宿主 schema。

## 排产物料数量查询

先读 `backend/technical/production_schedule/README.md` 及 Controller、DTO。

- 查询在 `ProductionScheduleManageController` 内组织和计算，沿用已有通用数据访问依赖，不另建专用查询 Service。用户要求“不要用 service”在这里指不把查询业务拆到新 Service，并非禁止现有 `CommonService<T>`。
- 数据源为选定 `Technical_CraftOrderVersion` 的有效工序及 `Technical_NodeSettingTemplate_Material_RL` 输入行，沿 `RefCraftOrderVersionId` 递归展开。**不使用制造 BOM 作为排产需求来源**，因为排产与工序单关联。
- 同版本物料按 `DataType + DataId + RefCraftOrderVersionId` 汇总，保留全部来源工序及物料行 ID；替料挂在主料下，不额外增加独立需求，不自动代料抵扣。
- 工序领用用量按一默认单位产出维护，沿用直接合计口径，不根据产出行倒算、不额外乘损耗率。物料量换算基本单位；规格产出默认单位比例只应用一次。
- 正常库存按有效正常入库记录剩余量统计；未完成占用按 `MAX(OccupyQuantity - ReleaseQuantity, 0)`。先扣占用和安全库存，形成按物料类型及 ID 共享的可分配池，跨树分支只分配一次。
- 同一物料重复路径保留各自需求 ID，库存汇总不重复相加。半成品缺量驱动下一层待生产需求；父级库存充足时保留子树但子需求为零。
- 当前只查物料需求及库存、占用，排产单查询尚未纳入。在途量、在制量、已排产量返回 `null`，缺量是库存口径，不能宣称已扣所有供应。
- 过滤保留命中节点祖先，不重新分配库存、不改变完整汇总结果。
- 批量取数后内存递归，校验循环、失效引用、单位配置和数量溢出。当前限制64层、10000节点，不因界面支持五层就截断更深的有效工艺。

制造 BOM 保存与排产需求查询是不同过程。新增排产持久化实体或供应查询时，先确认真实表结构和有效状态，再更新接口、文档及本参考，不拿示例数据填充结果。

## 工艺与排产前端的专用要求

普通列表写法仍优先参考 `frontend/old-sale_return_order/index.vue`。以下是模块特有要求，不作为其他 index 的默认布局：

- 排产页面左侧 TreeGrid 分配更多空间，选择半成品后在同层右侧展示排产信息；右侧顶部为该半成品排产列表，新增追加空白草稿行。切换半成品维护各自草稿。
- 数量列顺序：总需求量、库存总量、占用量、在途量、在制量、已排产量、缺量；主页面直接展示后端结果，保留真实工序及物料行 ID。该模块已按最新要求去掉列设置和导出；KingZonDataGrid 显式关闭 showFieldFilter/showDownLoad，TreeGrid 固定显示数量列。
- 排产共享 DTO 集中在 `frontend/technical/types/production-schedule.ts`，工艺列表与版本复用既有 craft DTO；多份 TS 逻辑整合到 `frontend/technical/production_schedule_manage/schedule.ts`，示例数据仅在测试目录。排产、到料及需求状态从公共 enums 文件导入枚举，保留现有接口值。
- `create-schedule.vue` 的工序区精简为面板/表单，选择方式不变，不改成表格；长表单在限定高度中滚动，物料齐套等表格仍用 KingZonDataGrid。
- 甘特图沿用已接入的 `@svar-ui/vue-gantt`，版本看 `package.json`；大 Drawer、时间拖动、缩放、两个月及至少五层树操作是该模块已确认要求。
- 当前主页面已接数量查询，示例仅供独立规划模型/测试；排产及采购保存尚未接入，对应入口关闭，不描述成业务持久化。
- 排产验证入口：`npm --prefix frontend/technical/production_schedule_manage test`。工艺组件测试在 `frontend/technical/craft_manage/component/__tests__/`，按修改选择入口；编译和逻辑测试不替代宿主页面视觉验证。
