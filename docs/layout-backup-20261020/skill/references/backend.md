# 后端写法

## 首要参考与阅读入口

通用写法优先参考用户指定的 `backend/sale_return_order/controller/ProductController.cs`。它实际属于 Base 领域，不要按所在文件夹推断 namespace 或路由。按任务选择方法，再核对目标模块的 DTO、实体和现有接口：

| 场景 | ProductController 中的参考方法 |
| --- | --- |
| 依赖、构造注入与领域路由 | 类声明及构造函数 |
| LINQ 查询表达式、筛选与选择项投影 | `GetProductSelectionList`、`GetProductSpecificationSelectionList` |
| 分页后补充关联信息、组织树节点 | `GetProductList`、`GetSemiProductList` |
| 详情 DTO、导航信息与子集合 | `GetProductById`、`GetProductSpecById` |
| 事务、主键生成及关联新增 | `AddProduct`、`AddSemiProduct` |
| 跟踪实体更新、子集合增删改 | `EditProduct`、`EditProductSpec`、`ModifyPackage` |
| 批量读取后组装匿名返回对象 | `GetProductPackageList` |
| 私有业务方法与多接口复用 | `CheckCode`、`ModifyPackgeCustomer`、`BuildPackageWorkbookAsync` |

参考其组织方式，保留目标模块业务契约。产品编码规则、单位转换类型、默认单位的 `-1` 标记、硬删除和文件处理属于产品模块规则，不直接推广到工艺或排产。

## 结构与命名

优先读目标目录现有文件。Technical 的 Controller 位于 `backend/technical/*/controller/`，DTO 位于对应 `dto/`；其他领域保持自身目录及路由。

- 参考 Controller namespace：`KingZon.OLCM.Base.WebApi.Controllers`；Technical 使用 `KingZon.OLCM.Technical.WebApi.Controllers`。DTO 同样按领域使用 `Base.WebApi.Dto` 或 `Technical.WebApi.Dto`，以实际声明为准。
- 实体通常位于领域 `Shared.DataBase.Entities`，枚举位于 `Shared.DataBase.Enum`。
- `CommonService<T>`、相关 `Utils` 的现有 namespace 为 `Kingzon.OLCM.Base.Services`，与其他 `KingZon` 大小写不同；以实际声明为准。
- 使用 `KingZonControllerBase`、`[ApiController]` 及目标领域路由；参考文件为 `[Route("api/base/[controller]")]`，Technical 为 `api/technical/[controller]`。Action 沿用 `[HttpPost("ActionName")]` / `[HttpGet("ActionName")]` 与 `Task<IActionResult>`，GET/POST、参数名称和位置依接口契约决定。
- 使用已有 `CommonService<T>`、`ProductService`、`IRepository<T>` 等，不虚构宿主未提供的依赖。
- 查询、业务校验、结果组装和保存直接在 Controller 内组织；已有领域 Service 的能力可以调用，不为新功能额外建立查询 Service、通用仓储或自动映射层。
- 依赖 `private readonly`，命名 `_craftVersionService` 一类；构造参数逐行、赋值逐行。辅助方法用清晰动词，如 `GetMaterialTotals`、`BuildMaterialTree`。
- 保持附近源码的 using 分组、块状 namespace、XML 注释及已有 `#region` 分区。不要照抄无用 using、冗余分区或历史错误。

## 接口与查询

参考的主要流程是：获取 IQueryable → 业务条件与投影 → 筛选、分页 → 收集结果 ID → 批量读取关联 → 内存补充结果 → `Ok`。

- 多源关联可用 `from ... join ... select new ...`，简单过滤用链式 `Where` / `Select`；逐字段赋值，沿用真实 DTO。不为统一语法重写邻近查询。
- 筛选位置由字段决定：`GetProductSelectionList` 在实体查询上 `ApplyFilters(filters)`；`GetProductList` 在列表投影上 `ApplyFilters(dto.filterList)`。核对筛选字段是否能翻译成 SQL，分页前完成影响筛选和排序的数据库条件。
- 分页有两种现有契约：`GetProductList` 用请求中的 `PageIndex` / `PageSize` 创建 `PageModel`；`GetSemiProductList` 用 `this.PageModel`。按目标接口执行，不强制改成一种。排序只在接口支持时沿用 `ApplySorts(this.SortInfoList)`，不是参考中所有查询的固定步骤。

`GetProductList` 的分页后处理结构如下；查询、投影和筛选字段以源码为准：

```csharp
var pageModel = new PageModel() { PageIndex = dto.PageIndex, PageSize = dto.PageSize };
// 查询、投影和 ApplyFilters 后调用 ToPagedListAsync(pageModel)
var data = productData.Data.ToList();
var productIds = data.Select(x => x.ProductId).ToList();
var packageData = _productPackageService.GetAll()
    .Where(x => productIds.Contains(x.ProductId)).ToList();
// 用批量关联数据补充本页，并按接口要求组织 result
productData.Data = result;
return Ok(productData);
```

- 分页后只补充本页关联数据，如单位、包装、关系、文件及自定义属性。保留原分页对象和总数，不用补充后的树根数量替代总数。`GetProductList` 是本页数据组成树；完整需求树/递归图查询不照搬分页，按自身接口读取完整相关集合。
- 返回沿用 `Ok(data)`、`Ok()` 或已有成功消息。列表 DTO、匿名投影和直接返回集合都在参考中使用，不额外套统一成功信封或一律新增返回 DTO。

- 只读读取按需 `AsNoTracking`，用 `Select` 限定需要的数据；导航加载选择投影或现有 `Include`，避免无关整图加载。
- 核对 `GetAll()`、`GetAll(true)` 和软删除语义，不能猜布尔参数含义。历史列表可有意包含已删除数据，业务需求树通常需要排除失效工序及引用。
- 收集 ID 后批量查库；按关联用 `ToLookup`，按身份用 `GroupBy` 汇总，字典负责索引。递归中只进行内存计算。
- 简单关联保留清晰的 `Where` / `FirstOrDefault`，重复查找较多时建立 Lookup 或字典。详情可先加载主记录和必要 `Include`，再显式构造编辑 DTO 与子集合，不机械添加导航加载。
- 同一 DbContext 不并发执行多个 EF 查询。数据库批次按依赖顺序 await，不用逐行 `.Result` 或阻塞等待。新异步数据库读取使用 EF 异步方法，不把参考中个别 `Task.Run` 或同步调用当成必须复刻的模式。
- 共享、较大的 DTO 放领域 `dto/`；只服务单一 Controller 的小请求/投影类可按现有方式内嵌。沿用属性名称、`required`、类型、可空性和默认值，不擅自重命名 `filterList` 等已有字段。关联使用真实 ID，不用显示名称替代。
- 校验失败沿用现有中文业务异常，如 `throw new Exception("请选择工序单版本")`，避免引入新错误框架。

## 保存与数量

- 先加载主记录，校验存在性、关联资料及编码等业务约束；修改已加载的跟踪实体时逐字段赋值，通过 `_unitOfWork.SaveChangeAsync()` 保存。不要求每次赋值再调用 `Update`，编辑查询不使用 `AsNoTracking`。
- 子集合参考 `EditProduct` / `ModifyPackage`：按主记录范围读取旧集合，计算 `addData`、`delData`、`modifyData`；删除调用已有 `Delete`，新增构造实体后 `Insert` / `InsertAsync`，修改直接更新旧实体。新行判定按 DTO 的 ID 约定，不能统一假设 `null` 或 `0`。
- 多表关联保存沿用 `TransactionScope(TransactionScopeAsyncFlowOption.Enabled)` 与 `IUnitOfWork`。必要修改全部成功并 `SaveChangeAsync` 后调用 `transaction.Complete()`。
- 多表需要原子保存时使用事务；简单单次保存不必仅因参考中存在事务便额外包装。只在需要生成主键时增加中间保存，不机械照搬每条循环都保存。
- 需要数据库生成主键时允许事务内先保存主记录，再引用真实 ID 保存子记录。替料指向相应主明细 ID，不能混用工序物料 ID 与 BOM 明细 ID。
- 主资料关键字段改变时核对目标模块的关联同步需求。删除关联记录与文件沿用既有方法；参考中的产品硬删除不是全项目默认，子集合只处理对应主记录。
- 重复或独立业务可提取私有方法，如校验、关联维护、文件生成；Action 保留业务流程，不把每个简单查询或赋值拆成工具方法。
- JSON 数据沿用 `JsonHelper.DefaultJsonSerializerOptions` 序列化和反序列化；空串、`null` 与空集合按协议及实体约定处理，保存和详情读取相互对应。展示后处理放在批量查询后的内存阶段。
- 使用已有单位配置、换算快照及辅助函数（例如实际适用的 `JoinWithData`），先核对支持类型和口径，不能重复换算。
- 数量用 `decimal`；按目标字段精度控制，汇总先转换单位并合计再舍入。`decimal(12,4)` 上限及小正数舍入为零等校验仅适用于相应字段，不推广到所有数量。
- 未实现或无来源的供应量使用协议约定的 `null`，不要伪造零数量。库存、占用、需求和供应不能因名称相近便合并口径。
- 工序物料、BOM 与排产数量的具体口径读取 [模块约定](craft-and-schedule.md)。

## 验证入口

文档或 skill 修改验证结构、链接和与参考源码的一致性。以下入口仅在相应业务修改时运行：

```powershell
python backend/technical/production_schedule/tests/verify_quantity_query.py
python backend/technical/craft/tests/verify_mbom_sync.py
```

脚本使用实际目标源码与部分测试替身进行隔离验证。运行前读取脚本了解依赖和临时文件行为。完整宿主构建、DI 注册、MySQL 事务及迁移执行需在实际业务项目验证，不能把隔离 SQLite 结果当作完整联调结论。
