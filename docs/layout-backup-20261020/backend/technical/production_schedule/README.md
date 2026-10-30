# 排产物料数量查询

查询直接在 `ProductionScheduleManageController` 内实现，数据源为工序单版本、工序及领用物料关联表。按 `RefCraftOrderVersionId` 展开半成品引用，不依赖制造 BOM，不需要新注册查询 Service。

## 接口

`POST /api/technical/ProductionScheduleManage/GetMaterialQuantityTree`

```json
{
  "craftVersionId": 123,
  "quantity": 100,
  "keyword": "",
  "shortageOnly": false
}
```

- `craftVersionId`：所选 `Technical_CraftOrderVersion.Id`，使用该版本保存的全部工序领用物料，不切换至主版本。
- `quantity`：本次成品生产量，使用工序单产品/规格的默认单位，大于零、最大 `99999999.9999`、最多四位小数。返回 `quantityUnitId/quantityUnit` 说明生产单位，物料行统一使用基本单位。
- `keyword`：编码、名称、规格搜索，不区分大小写，保留命中子项的祖先。
- `shortageOnly`：保留库存缺量大于零的节点及必要祖先。

返回工序单及版本信息、成品信息、生产量/单位、`nodes` 需求树和 `materials` 物料汇总。`quantityBasis` 为 `inventory`，缺量尚未抵扣在途、在制和排产供应。筛选只影响 `nodes`；`nodeCount/materials` 为筛选前的完整计算结果，不重新分配库存。

## 来源与关联

- 读取 `Technical_NodeSettingTemplate` 中该版本未删除的工序。
- 只统计 `Technical_NodeSettingTemplate_Material_RL.IOType == 输入`；产出不计入需求，已删除工序的物料不计入。
- 同一版本的领用物料按 `DataType + DataId + RefCraftOrderVersionId` 汇总，先转换基本单位再合计；同物料引用不同工艺版本分别保留。
- `InsteadSourceId` 指向同工序主料，替料分别合计并挂在对应主料下，不独立增加需求，不自动抵扣主料。
- 半成品沿 `RefCraftOrderVersionId` 展开所引用工序单版本的领用物料，并校验领用物料与引用工序单产品/规格匹配。

树节点包含 `sourceCraftVersionId`、`materialRlIds`、`nodeSettingTemplateIds`，完整保留汇总前的工序及物料行来源，便于排产单关联。`craftVersionId` 表示该半成品引用的工艺版本；替料也保留对应版本及来源行。路径 `id/parentId` 按物料身份与引用版本组成，不随物料行整体替换而改变，同物料不同路径不会共用需求 ID。

| 字段 | 显示 | 口径 |
| --- | --- | --- |
| `gross` | 总需求量 | 本工艺引用路径的需求，子项随父项待生产量展开 |
| `stock` | 库存总量 | 物料正常入库记录的 `RemainQuantity` 合计 |
| `occupied` | 占用量 | 未完成占用记录的 `MAX(OccupyQuantity - ReleaseQuantity, 0)` 合计 |
| `transit` | 在途量 | 未查询，返回 `null` |
| `inProduction` | 在制量 | 未查询，返回 `null` |
| `scheduledTotal` | 已排产量 | 未查询，返回 `null` |
| `net` | 缺量 | `MAX(gross - available, 0)`，仅为库存口径缺量 |
| `available` | 库存分配 | 分给本需求的库存，跨分支只分配一次 |
| `safetyStock` | 安全库存 | 基础资料配置，共享库存池初始化时扣减一次 |

`materials` 按 `dataType + dataId` 汇总需求、库存分配、缺量和出现次数；库存、占用、安全库存只取物料池一次，不能把树中重复显示的库存列相加。

## 计算口径

工序物料 `Quantity` 沿用工艺维护时每一默认单位产出的用量口径，直接合计领用用量，不根据产出行倒算，也不额外叠加损耗率。

```text
共享可用库存 = MAX(正常库存 - 未释放占用 - 安全库存, 0)
根级物料总需求 = 成品生产量 × 各工序同物料领用用量合计
半成品待生产量 = MAX(半成品总需求 - 分配库存, 0)
子项总需求 = 父项待生产基本量 / 父项产出单位换算比例 × 子项领用基本量合计
```

工序物料依 `UnitId/UnitConversionId/UnitListInfo` 换算；基本单位用量无需额外快照。规格默认单位换算使用现有 `Base_UnitConversion` 中的 `SpecToProduct` 配置。使用规格默认单位但未传快照时可使用该配置；其他非基本单位必须有有效快照。规格产出的换算比例只应用一次，产品库存和其规格库存为独立池，不自动互相抵扣。

先按工序 `Sequence`、物料 `SortNo/Id` 汇总，再深度优先分配共享库存。节点需求保留四位小数。库存满足的半成品仍保留子树，子项生产需求为零。按层级批量查询工序单版本/工序/物料，再批量读取资料和库存；递归计算不查询数据库，全程 `AsNoTracking`。

不存在/已删除的工序单、版本或物料、循环引用、无主料或跨工序关联替料、物料与引用工序单不匹配及无效单位配置明确报错。限制为64层、10000个需求节点，支持五层及更深正常工艺引用。

## 接入与验证

代码组织参照 `CraftOrderManageController`：统一 `_xxxService` 依赖命名、构造函数和逐字段初始化；接口方法负责组织查询，计算及数据整理使用 Controller 私有方法。领用与替料按身份 `GroupBy` 汇总，批次物料通过 `ToLookup` 按版本分组，减少重复扫描；来源关联、库存分配顺序、精度及返回字段保持原有口径。

- Controller 和 DTO 放入 Technical WebApi，DTO 命名空间为 `KingZon.OLCM.Technical.WebApi.Dto`。
- 沿用项目现有 `CommonService<T>` 数据查询方式，需要工序单、版本、工序、物料关联、产品、规格、单位转换、库存的通用查询依赖；已移除独立 `ProductionScheduleQuantityService`。
- 不新增表或字段，不操作业务数据库，不查询/修改排产单，不锁定库存。前端已接入本查询接口，可选择工序单及版本并刷新数量。
- 前端直接展示服务端返回数量，不再用本地规划模型重算；未查询的供应列显示“未查询”，不可解释为实际数量为零。

```powershell
python backend/technical/production_schedule/tests/verify_quantity_query.py
```

隔离测试编译实际 Controller、DTO 和工序单/工序物料实体，在无制造 BOM 表的 SQLite 数据库验证读取工序领用、排除产出/已删除工序、跨工序汇总、来源关联、替料、五层引用、单位换算、共享库存、筛选、只读行为和错误处理，40项断言通过。公共查询服务、基础资料、库存、单位转换和工作流使用测试替身；完整宿主编译、MySQL 联调及实际注册仍需业务项目验证。未应用历史版本映射中引用缺失时间列的检查约束。
