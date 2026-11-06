# 工序单排产管理

## 当前功能（2026-11-05）

- 先生成顶层排产单，再对子级排产。初次查询后展示顶层草稿，数量固定为当前计划数量；关闭草稿不代表已生成，只有后端保存成功后才解锁子级。
- “库存口径”后的“顶层排产单详情”按钮返回右侧顶层 Tab。当前页面生成的子单保存 `RootDispatchOrderId`，查询同时按顶层 ID 筛选，避免混入其他计划的同版本单据。
- 提交携带 `RootCraftVersionId`；子级还必须携带有效的 `RootDispatchOrderId`。后端校验顶层存在、属于当前顶层工艺，且子级工艺来自该工艺输入物料的引用范围。复用现有字段，无需新增数据库列。
- 工序单弹窗分页查询，选择已发布版本，优先主版本；也支持 props/路由传入版本。不存在默认测试版本。
- 选择后可直接进行成品排产；左侧仍是原工序输入物料需求 TreeGrid，选择半成品可排该引用版本。
- 排产单使用 Tabs。每个草稿有独立临时 ID，切换保留编辑，关闭需确认；保存只提交当前草稿，失败保留草稿，提交期间禁止重复提交或关闭。
- 工序支持新增、删除、拖动、上下排序；通过 `allowProcessEdit` 接收宿主工序调整设置。原工序加工工艺、外协、入库、报工属性只读；临时工序创建时选择工艺。
- 设备→人员 checkbox 选择；每人编辑数量、工时及批量设置工时。工序和整单统计人工工时，不将其视作设备历时。
- 物料弹窗独立编辑、确认应用、取消放弃。库存校验查询实际选择的物料，包括模板之外新增的物料，按全部工序共享主单位库存池。
- 已保存排产查询实际工序、设备人员、领料需求。未启动单可开始、删除；删除采用软删除，后端检查报工、领退料和下级单据。
- 甘特图保持原实现，本次没有接通真实排产甘特数据、拖动保存或改期。

## 接口

统一前缀 `/api/production/ProductionScheduleManage`，Controller 文件名沿用 `ProductionSchedueManageController.cs`。

| 方法 | 路径 | 用途 |
| --- | --- | --- |
| GET | GetScheduleTemplate | 按工艺版本返回工序/资源/领用物料模板 |
| POST | GetMaterialQuantityTree | 工序输入物料递归需求、库存、占用及排产供应展示 |
| POST | GetMaterialStock | 按 DataType + DataId 批量查询实际编辑物料库存 |
| GET | GetScheduleList | 按工艺版本查询实际排产单及明细 |
| POST | CreateSchedule | 接收 CreateProductionScheduleDto，保存当前排产单 |
| POST | StartSchedule | `{ id }`，启动未开始工作流 |
| POST | DeleteSchedule | `{ id }`，删除未执行的排产及领料需求 |

## 保存与数据口径

1. Controller 内校验已发布版本、工艺属性、部门、设备与人员关系、数量精度及单位，再用异步流 TransactionScope 统一保存。
2. 工序实例采用 `WF_Node_ProductionExt : WF_Node` TPT 继承，复用基类 Id。工作流 Service 新重载接收编辑后的线性路线，复制开始/结束节点及源模板权限。
3. `ProductionScheduleNodeType` 对应 **WF_NodeType.Id**：生产=1、外协=2、入库=3。这三条基础记录的结构类型必须是 `NodeType.node`，且工作流类型与模板一致。开始/结束节点必须使用另外的类型记录；若旧库 Id=1 是开始类型，须先协调基础数据，不能直接覆盖旧记录。
4. 普通生产生成生产节点；勾选入库或外协后追加独立入库节点，生成节点 NodeTemplateId 为空。按照界面路线建立前后关系，不能复用未调整的模板连线。
5. 同时保存派工单、节点、设备人员分配、工序物料快照、工序领料需求、设备人员领料分摊。产出快照不生成领料需求。
6. 工序领用数量是本单总需求。选定单位先按服务器保存的工艺快照或当前有效转换关系转主单位，不信任客户端比例。分摊采用累计舍入差额，避免小数差或多人重复领料。
7. 派工数量使用工艺产品/规格基础单位，BaseQuantity 为该基础单位数量，MainQuantity 按规格→产品主单位关系换算。收入计价、单价未从工艺模板取得，保持未配置，不伪造换算。
8. 在途量按用户要求暂不计算，返回 null。已排产/在制分别展示未启动/已启动工作流的剩余主单位量；库存缺量不抵扣没有需求归属关系的排产供应。保存仅生成领料需求，不实际出库或占用库存。
9. 暂时没有需求实例/计划分配关联，因此不同工序单查询同一物料时可能看到同一批供应；不得将其重复认作各自可用供应。

## 文件职责

- index.vue：工序选择、数量查询、草稿身份、保存与历史操作。
- component/schedule-craft-picker.vue：工序单及已发布版本选择。
- component/schedule-order-list.vue：Tabs，每个草稿 TabPanel 内直接挂载 create-schedule.vue，转发提交/取消事件；schedule-saved-detail.vue：真实排产详情及开始/删除入口。
- component/create-schedule.vue：单张独立排产草稿及工序编辑。
- component/schedule-process-card.vue、schedule-machine.vue、schedule-material-dialog.vue：工序摘要、资源和物料编辑。
- schedule.ts：计算、草稿映射、前端校验；共享 DTO 在 frontend/src/types/schedule.ts。

## 验证

```
node --test frontend/src/views/production/production_schedule_manage/__tests__/independent-schedule.test.cjs
python backend/Production/tests/verify_schedule_save.py
```

当前检查覆盖 Vue 模板/严格 TypeScript、草稿切换/失败重试/并发、实际选料库存、TPT 保存、工作流连线和权限、外协入库、主单位转换、领料分摊、开始和删除。

后端使用真实 Controller、工作流 Service 及实体，宿主公共依赖使用测试替身，数据库为隔离 SQLite。SQLite 不支持 MySQL 的环境事务与计算列，测试不证明 MySQL 回滚、锁或完整宿主构建。工作区未提供可运行完整前后端，未作宿主浏览器视觉检查。

旧 production-schedule.test.cjs / production-schedule-checks.test.cjs 仍引用迁移前 DTO 路径、旧页面及本地原型保存方法，不能作为当前页面集成验收入口；其历史甘特/规划模型留存，本次未改造。
