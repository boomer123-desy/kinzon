# BOM 派工领料页

`material-demand-by-bom.vue` 使用 KingZonShowForm、TreeGrid 和现有 `select-material.vue`。普通物料选择列表继续由 SelectMaterial 内的 KingZonDataGrid 展示，不增加页面独立样式。

页面按用户提供的模板组织：ShowForm → Form/FormField 清单选择 → 选择其他物料按钮 → TreeGrid → 底部取消/确定 → 物料 Drawer。保留模板文案、列定义、列宽、顺序、冻结分组和两层表头；在原有空单元格中补齐展示及编辑绑定。formData 在 init 中初始化。

## 接入

- 将 `frontend/src/types/production.ts` 中补充的 BOM 展示字段及 `DetailWithProductDto`、`BomDemand*Dto`、`BomMaterialDemandsDto` 同步到宿主 `@/types/production`。
- 同步 `backend/Production/KingZon.OLCM.Production.WebApi/Controllers/BomManageController.cs`。新增 `GET /api/production/BomManage/GetProductionBomOptions?productId=...&specificationId=...`，返回当前产品/规格的全部有效版本、总量和单位快照，避免下拉选项受普通列表分页限制。删除/淘汰的版本或清单排除，默认选择主版本，没有主版本则选择列表首项。
- 明细沿用 `GET /api/production/BomManage/GetProductionBomDataById`；供应商和物料单位沿用原领料页的接口。
- 组件接收 `currentRow: BomDemandCurrentRowDto`，通过 `close`、`submit` 通知父页面；子页面不调用派工保存接口。

```vue
<MaterialDemandByBom :currentRow="currentRow"
    @close="materialDemandDrawer.collapse()"
    @submit="getMaterialDemands" />
```

`add-dispatch.vue` 提供原“领料需求”和“BOM领料”两个入口，共用 Drawer 和 `getMaterialDemands` 回调。确认时保存当前来源的编辑草稿，取消则保留原需求。提交时在父页增加 BOM 物料分支，继续使用现有 `materialDemands.materials` 列表，不额外新增保存列表。

编辑列表使用 `DispatchOrderEditDto`，领料草稿分别为 `FormulaMaterialDemandsDto`、`BomMaterialDemandsDto`；保存时由 `getPostData` 创建独立的 `PostDispatchOrderDto`，其中 `PostMaterialDemands.materials` 仅包含 `PostMaterialDemandDetails[]`。转换不覆盖原编辑行。需求物料、单位或数量缺失时提示检查，不用 `0` 填补必填值。

销售选择组件 `select-saleOrder.vue` 的 `currentRow` 仅使用 `GetDispatchOrderDto` 派工信息，已选销售明细通过 `selectedRows` 独立传入。该组件不依赖配方/BOM 领料草稿类型；同步宿主时应同时更新父页、子组件与 production DTO。

## 返回数据

`submit` 返回 `BomMaterialDemandsDto`：

| 字段 | 含义 |
| --- | --- |
| `basisType`、`basisId` | 派工依据类型及版本 ID；选择 BOM 时为物料清单=2、所选清单版本 ID |
| `bomVersionId` | 编辑页所选清单版本 ID |
| `bomUnitConversionId`、`bomUnitInfo` | 编辑页清单计量单位及换算快照 |
| `dispatchQuantity`、`dispatchUnitId`、`dispatchUnitConversionId` | 确认领料时的派工数量、单位 ID 和转换 ID，用于检查草稿是否仍有效 |
| `bomQuantity` | 派工量折合到所选清单单位的数量 |
| `convRatio` | 派工单位 ratio ÷ 清单单位 ratio，保存未舍入的换算系数 |
| `materialDetails` | 完整编辑草稿，包含主料、替料与其他物料，可传回组件恢复 |
| `materials` | 本次实际领用的扁平物料列表，只包含已选项 |

每项实际领用物料提供 `dataId`、`dataType`、`demandQuantity`、`baseDemandQuantity`、`unitId`、`unitConversionId`、`convUnitInfo`、`supplierId`、`dataStatus`、`detailRemark`。父页面应使用已经解析好的 `dataId/dataType` 组装物料提交项：BOM/配方明细中的 `detailId` 仍是引用版本 ID，规格明细的 `detailId` 是规格 ID，不能一概当作产品 ID。父页面保存接口是否接收 BOM 元数据，需以其 DTO 为准。

## 派工保存

`POST /api/production/DispatchOrderManage/AddDispatchOrderList` 仍接收派工记录数组。每条记录增加 `basisType`、`basisId`；BOM 分支提交如下结构：

```json
{
  "basisType": 2,
  "basisId": 501,
  "materialDemands": {
    "materials": [
      {
        "dataId": 41,
        "dataType": 1,
        "demandQuantity": 2,
        "baseDemandQuantity": 24,
        "unitId": 200,
        "unitConversionId": 9,
        "convUnitInfo": [{ "id": 9, "unitId": 200, "ratio": 12, "outboundQuantity": 2 }]
      }
    ]
  }
}
```

示例仅展示本次涉及字段，其他派工必填字段沿用原接口。`basisId` 对应 BOM 版本，不是 BOM 主表 ID。BOM 物料直接使用子页解析的 `dataId/dataType`，同时传递需求量、基础量、需求单位、单位快照、供应商、状态和备注；不传配方单位信息及完整树草稿。清除 BOM 后仅选择其他物料时，依据为未指定，仍走 BOM 页的物料映射分支。

原配方页及配方提交保留 `formulaVersionId/formulaUnitId/formulaConversionId/formulaQuantity/convRatio`，本次撤回通用单位结构的改造，不使用 `BaseUnitInfo/DispatchBasisUnitInfo`。配方订单填写依据类型=1、依据 ID=配方版本 ID。

后端在新建派工单时填写 `BasisType/BasisId`，继续通过既有 `Production_Material_Receive_Demand` 保存物料。BOM 分支不写配方外键和 `FormulaUnitInfo`；旧客户端仅提交 `MaterialDemands.FormulaVersionId` 时，自动补为配方依据，原配方单位计算及保存方式保持不变。销售关联仍沿用原逻辑。

宿主需合并 `DispatchBasisType` 枚举，并确认派工实体和数据库已有 `BasisType/BasisId` 字段。本次补充工作区对应实体声明，没有执行数据库结构变更。前端枚举文件为模块补充片段，同步到宿主现有 `types/enums.ts` 时保留其他枚举。

仅修改派工数量时，父页按新旧派工量之比同步缩放 BOM 树形草稿、所选主料/替料、其他物料需求及出库数量快照，并更新派工数量快照与清单数量；保留所选单位、供应商、备注和手工调整后的需求比例。重新打开使用最新派工量和已缩放草稿。修改生产单位 ID 或单位转换 ID 后，提交仍提示重新确认，再次打开按新单位查询换算。父页不使用 mainQuantity 校验 BOM 草稿。旧草稿缺少派工输入快照时需要重新确认；恢复草稿时若查询到的换算系数变化，重新加载标准需求，不继续沿用旧的手工需求量。

## 计算与交互

- 输入为 `currentRow.dispatchQuantity` 和 `dispatchUnitInfo.productionUnitId/unitConversionId`，BOM 页独立计算，不读取 mainQuantity、baseRatio 或 mainRatio。
- 优先使用版本 `unitInfoList` 中派工单位和目标清单单位的完整有效关系。关系缺失时，在子页通过 `POST /api/sale/SaleOrderManage/GetProductUnitList` 按当前 productId/specificationId 查询最新单位；每次打开页面重新获取所需数据，同次编辑复用已加载的产品单位。
- 统一公式：`BOM数量 = 派工数量 × 派工单位.ratio ÷ 清单单位.ratio`，保留六位小数。两端 ratio 必须来自同一版本快照或同一产品/规格的最新单位列表，不能混用底数。派工单位同时匹配单位 ID 和转换 ID，避免同名包装串用；未提供转换 ID 时仅允许唯一单位匹配。
- 工艺同步旧快照可能同时存在规格单位和产品主单位且 Id 都为 0；同一转换 ID 保留版本首项，与同步时产出单位在前、页面默认单位的取值一致，不让后面的主单位覆盖产出单位。最新接口的基础单位（code=-1）或主单位（isMainUnit）与版本的转换 ID 不同时，按相同物理 unitId 唯一匹配；包装单位仍按具体转换 ID 匹配。匹配后的两端比例仍全部取最新列表，不沿用旧快照的比例。
- 查询使用版本默认清单单位，显示和提交使用当前选中单位，通过 `resolveBomConversion/getBomQuantity` 统一换算。提交的 bomUnitInfo 保存实际采用的目标单位快照，convRatio 保存实际系数；父页对照派工输入快照和同一六位精度进行校验。
- 无效派工数量、无效换算率、非有限结果及小于六位小数精度的数量阻止查询及提交。领用物料明细仍按自身基础单位换算，不改变物料出库快照口径。
- 后端保持只领用第一层的口径；同产品配方上提为首层原料，子 BOM 按其关联产品/规格领用，不把所有后代叠加为独立需求。
- 每个主料及其全部替料构成一组，默认选主料；选择替料时取消同组其他领用项。不同来源组的相同物料保留各自需求，不擅自合并或抵扣库存。
- 修改需求单位时保持基础物料需求不变，修改数量时更新基础数量及所选单位/基础单位/产品主单位出库快照。单位换算缺失、非正数量、换算后小于六位精度时阻止提交。
- 其他物料可新增、删除，重复打开选择器保留选择及编辑值；清除 BOM 保留其他物料。恢复草稿时复制数据，不修改父页面对象。输出去除 TreeGrid 的父引用，可序列化。
- 当前模板仅保留“选择其他物料”按钮。切换清单时加载标准需求，保留其他物料；派工数量变化后可立即查看缩放后的需求，重新打开时页头和清单数量均使用最新派工量。输入框暂时清空或为零时保留最后有效草稿，恢复正数后继续按有效派工量缩放，提交仍校验派工量。

## 验证

用例验证销售选择组件的实际 props 声明能够接收编辑行，并覆盖版本优先、最新产品/规格单位回退、空版本快照、重复零转换 ID、基础/主单位跨来源 ID 匹配、同名包装匹配、mainQuantity为0、换算率不可混用、接口失败、恢复草稿换算变化、六位小数舍入和派工输入变更校验。

```powershell
node --test frontend/src/views/production/dispatch_order_manage/__tests__/material-demand-by-bom.test.cjs
python backend/Production/tests/verify_bom_demand.py
python backend/Production/tests/verify_dispatch_bom.py
```

前端检查编译实际 BOM 页面和 production DTO，并提取新增派工页实际提交方法进行严格 TypeScript 类型检查；缺少的宿主枚举、HTTP、公共类型/组件使用测试替身。覆盖 BOM 选择、替料、单位快照、物料提交分支、依据字段、数量变化、编辑草稿保留、必填值校验和原配方提交兼容。后端 BOM 查询检查提取实际查询方法，在 SQLite 中执行；派工保存检查编译实际 Action、DTO、派工/领料实体类及依据枚举，宿主服务用内存替身补齐，不连接数据库。覆盖 BOM 依据与物料保存、配方单位保留、旧客户端兼容和销售关联，不代表完整宿主构建或真实数据库联调。
