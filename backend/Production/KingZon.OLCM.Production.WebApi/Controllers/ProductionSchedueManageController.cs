using Kingzon.OLCM.Base.Services;
using KingZon.Core;
using KingZon.Core.DataBase;
using KingZon.Core.Web;
using KingZon.OLCM.Base.Shared.DataBase.Entities;
using KingZon.OLCM.Base.Shared.DataBase.Enum;
using KingZon.OLCM.Base.Shared.DataBase.JsonStructure;
using KingZon.OLCM.Inventory.Shared.DataBase.Entities;
using KingZon.OLCM.Inventory.Shared.DataBase.Enum;
using KingZon.OLCM.Technical.Shared.DataBase.Entities;
using KingZon.OLCM.Technical.Shared.DataBase.Enum;
using KingZon.OLCM.Technical.WebApi.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace KingZon.OLCM.Technical.WebApi.Controllers
{
    [Route("api/production/[controller]")]
    [ApiController]
    public class ProductionScheduleManageController : KingZonControllerBase
    {
        private readonly CommonService<Technical_CraftOrder> _craftOrderService;
        private readonly CommonService<Technical_CraftOrderVersion> _craftVersionService;
        private readonly CommonService<Technical_NodeSettingTemplate> _nodeSettingService;
        private readonly CommonService<Technical_NodeSettingTemplate_Material_RL> _materialRLService;
        private readonly CommonService<Base_Product> _productService;
        private readonly CommonService<Base_ProductSpecification> _specificationService;
        private readonly CommonService<Base_UnitConversion> _unitConversionService;
        private readonly CommonService<Inventory_Record> _inventoryService;

        private readonly CommonService<Base_Machine> _machineService;
        private readonly CommonService<Base_Craft> _craftService;
        private readonly CommonService<Base_Department> _departmentService;
        private readonly CommonService<Technical_NodeSettingTemplate_Machine_RL> _templateMachineService;

        public ProductionScheduleManageController(
            CommonService<Technical_CraftOrder> craftOrderService,
            CommonService<Technical_CraftOrderVersion> craftVersionService,
            CommonService<Technical_NodeSettingTemplate> nodeSettingService,
            CommonService<Technical_NodeSettingTemplate_Material_RL> materialRLService,
            CommonService<Base_Product> productService,
            CommonService<Base_ProductSpecification> specificationService,
            CommonService<Base_UnitConversion> unitConversionService,
            CommonService<Inventory_Record> inventoryService,
            CommonService<Base_Machine> machineService,
            CommonService<Base_Craft> craftService,
            CommonService<Base_Department> departmentService,
            CommonService<Technical_NodeSettingTemplate_Machine_RL> templateMachineService)
        {
            _craftOrderService = craftOrderService;
            _craftVersionService = craftVersionService;
            _nodeSettingService = nodeSettingService;
            _materialRLService = materialRLService;
            _productService = productService;
            _specificationService = specificationService;
            _unitConversionService = unitConversionService;
            _inventoryService = inventoryService;
            _machineService = machineService;
            _craftService = craftService;
            _departmentService = departmentService;
            _templateMachineService = templateMachineService;
        }

        /// <summary>
        /// 独立排产编辑器的初始化模板。仅查询，不修改工艺、排产或领料数据。
        /// </summary>
        [HttpGet("GetScheduleTemplate")]
        public async Task<IActionResult> GetScheduleTemplate([FromQuery] long craftVersionId)
        {
            var version = await _craftVersionService.GetAll().AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == craftVersionId && !x.IsDeleted)
                ?? throw new Exception("工艺版本不存在或已删除");
            if (version.Status != CraftOrderVersionStatus.已发布)
                throw new Exception("请选择已发布的工艺版本进行排产");
            var order = await _craftOrderService.GetAll().AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == version.CraftOrderId && !x.IsDeleted)
                ?? throw new Exception("工艺单不存在或已删除");
            var nodes = await _nodeSettingService.GetAll().AsNoTracking()
                .Where(x => x.CraftOrderVersionId == version.Id && !x.IsDeleted)
                .OrderBy(x => x.Sequence).ThenBy(x => x.Id).ToListAsync();
            var nodeIds = nodes.Select(x => x.Id).ToList();
            var materials = await _materialRLService.GetAll().AsNoTracking()
                .Where(x => nodeIds.Contains(x.NodeSettingTemplateId) && x.IOType == NodeIOType.输入 && x.InsteadSourceId == null)
                .OrderBy(x => x.SortNo).ThenBy(x => x.Id).ToListAsync();
            var graph = new CraftMaterialData();
            graph.Versions.Add(version.Id, version);
            graph.Orders.Add(order.Id, order);
            graph.Materials.Add(version.Id, materials);
            var infos = await GetMaterialInfoAsync(graph);
            var product = infos[GetCraftProductKey(order)];
            var crafts = await _craftService.GetAll().AsNoTracking().OrderBy(x => x.Name)
                .Select(x => new { x.Id, x.Name }).ToListAsync();
            var departments = await _departmentService.GetAll().AsNoTracking().OrderBy(x => x.Name)
                .Select(x => new { Value = x.Id, Text = x.Name }).ToListAsync();
            var machines = await _machineService.GetAll().AsNoTracking()
                .Where(x => !x.IsDeleted && !x.IsStop).OrderBy(x => x.Code)
                .Select(x => new
                {
                    x.Id, Text = x.Code + " " + x.Name,
                    CraftIds = x.CraftRLs.Select(rl => rl.CraftId).ToList(),
                    Users = x.UserRLs.Select(rl => new { Id = rl.UserId, Text = rl.User.Name }).ToList(),
                }).ToListAsync();
            var nodeMachines = await _templateMachineService.GetAll().AsNoTracking()
                .Where(x => nodeIds.Contains(x.NodeSettingTemplateId) && !x.IsDeleted)
                .Select(x => new { x.NodeSettingTemplateId, x.MachineId }).ToListAsync();
            var materialsByNode = materials.ToLookup(x => x.NodeSettingTemplateId);
            var machinesByNode = nodeMachines.ToLookup(x => x.NodeSettingTemplateId);
            var machineCrafts = machines.SelectMany(machine => machine.CraftIds.Select(craftId => (machine.Id, craftId)))
                .ToHashSet();
            // 编辑时保留一道工艺工序；是否外协、是否入库分别配置，不在模板阶段拆成执行节点。
            var processs = nodes.Select((source, processIndex) =>
            {
                var materialRows = materialsByNode[source.Id].Select((row, index) =>
                {
                    var info = infos[(row.DataType, row.DataId)];
                    // 工艺快照和领料需求均使用物料主单位比例，校验后补充标准主单位项。
                    var units = JsonSerializer.Deserialize<List<InventoryUnitInboundDto>>(
                        string.IsNullOrWhiteSpace(row.UnitListInfo) ? "[]" : row.UnitListInfo,
                        JsonHelper.DefaultJsonSerializerOptions) ?? new();
                    if (!units.Any(x => x.Id == 0 && x.UnitId == info.UnitId))
                        units.Add(new InventoryUnitInboundDto { Id = 0, UnitId = info.UnitId, UnitName = info.Unit, Ratio = 1m });
                    var baseQuantity = GetMaterialBaseQuantity(new Technical_NodeSettingTemplate_Material_RL
                    {
                        Id = row.Id, Quantity = 1m, UnitId = row.UnitId,
                        UnitConversionId = row.UnitConversionId, UnitListInfo = row.UnitListInfo,
                    }, info);
                    var unitId = row.UnitId > 0 ? row.UnitId : info.OutputUnitId;
                    var conversionId = unitId == info.UnitId ? 0 : row.UnitConversionId;
                    if (!units.Any(x => x.Id == conversionId && x.UnitId == unitId))
                        units.Add(new InventoryUnitInboundDto { Id = conversionId, UnitId = unitId,
                            UnitName = unitId == info.OutputUnitId ? info.OutputUnit : info.Unit, Ratio = baseQuantity });
                    return new
                    {
                        Order = index + 1, row.DataId, row.DataType, Code = info.Code, Name = info.Name,
                        Quantity = row.Quantity, QuantityPerUnit = row.Quantity,
                        UnitId = unitId, UnitConversionId = conversionId,
                        ConvUnitInfo = JsonSerializer.Serialize(units, JsonHelper.DefaultJsonSerializerOptions),
                        UnitOptions = units,
                    };
                }).ToList();
                return new
                {
                    source.Code,
                    NodeSettingTemplateId = source.Id,
                    Order = (processIndex + 1) * 10,
                    source.Name,
                    CraftId = source.CraftId ?? 0,
                    source.IsOutsource,
                    source.IsInbound,
                    source.IsReportWork,
                    Materials = materialRows,
                    Machines = machinesByNode[source.Id]
                        .Where(x => machineCrafts.Contains((x.MachineId, source.CraftId ?? 0)))
                        .Select(x => new { x.MachineId, Users = Array.Empty<object>() }).ToList(),
                };
            }).ToList();
            return Ok(new
            {
                CraftVersionId = version.Id, version.Version,
                ProductCode = product.Code, ProductName = product.Name, QuantityUnit = product.OutputUnit,
                Processs = processs,
                Crafts = crafts, Departments = departments, Machines = machines,
            });
        }

        #region 物料数量查询

        /// <summary>
        /// 按工序单版本查询领用物料需求树及库存数量。
        /// </summary>
        [HttpPost("GetMaterialQuantityTree")]
        public async Task<IActionResult> GetMaterialQuantityTree([FromBody] ProductionScheduleQuantityQueryDto query)
        {
            if (query == null || query.CraftVersionId <= 0 || query.Quantity <= 0
                || query.Quantity > 99999999.9999m || decimal.Round(query.Quantity, 4) != query.Quantity)
            {
                throw new Exception("请选择工序单版本，生产数量必须大于0、不超过99999999.9999且最多4位小数");
            }
            try
            {
                var graph = await GetCraftMaterialDataAsync(query.CraftVersionId);
                var craft = graph.Versions[query.CraftVersionId];
                var order = graph.Orders[craft.CraftOrderId];
                var infos = await GetMaterialInfoAsync(graph);
                var stock = await GetInventoryQuantityAsync(infos.Keys);
                var product = infos[GetCraftProductKey(order)];
                var totals = GetMaterialTotals(graph, infos);
                var nodes = BuildMaterialTree(graph, infos, stock, totals, craft.Id, query.Quantity, out var nodeCount);
                var summary = GetMaterialSummary(nodes);
                return Ok(new ProductionScheduleQuantityResultDto
                {
                    CraftOrderId = order.Id,
                    CraftOrderCode = order.Code,
                    CraftOrderName = order.Name,
                    CraftVersionId = craft.Id,
                    CraftVersion = craft.Version,
                    Product = product,
                    Quantity = query.Quantity,
                    QuantityUnitId = product.OutputUnitId,
                    QuantityUnit = product.OutputUnit,
                    NodeCount = nodeCount,
                    Materials = summary,
                    Nodes = FilterMaterialTree(nodes, query.Keyword?.Trim(), query.ShortageOnly),
                });
            }
            catch (OverflowException)
            {
                throw new Exception("工序物料展开数量超出计算范围，请检查用量和生产数量");
            }
        }

        #endregion

        #region 需求计算

        /// <summary>
        /// 按工艺引用展开需求树，并按物料身份共享库存分配。
        /// </summary>
        private static List<ScheduleMaterialNodeDto> BuildMaterialTree(CraftMaterialData graph,
            Dictionary<(InventoryDataType DataType, long DataId), MaterialInfo> infos,
            Dictionary<(InventoryDataType DataType, long DataId), StockTotal> stock,
            Dictionary<long, List<MaterialTotal>> totals, long rootVersionId, decimal quantity, out int count)
        {
            var remaining = stock.ToDictionary(x => x.Key,
                x => Math.Max(0m, x.Value.Stock - x.Value.Occupied - infos[x.Key].SafetyStock));
            var nodeCount = 0;

            List<ScheduleMaterialNodeDto> Expand(long versionId, decimal production, string parentId,
                int level, HashSet<long> ancestors)
            {
                if (level > 64)
                {
                    throw new Exception("工艺引用层级超过64层，请检查引用关系");
                }
                if (!ancestors.Add(versionId))
                {
                    throw new Exception($"工艺版本存在循环引用，版本Id：{versionId}");
                }
                var version = graph.Versions[versionId];
                var output = infos[GetCraftProductKey(graph.Orders[version.CraftOrderId])];
                var nodes = new List<ScheduleMaterialNodeDto>();
                foreach (var material in totals[versionId])
                {
                    if (++nodeCount > 10000)
                    {
                        throw new Exception("工序物料展开超过10000个需求节点，请检查引用关系");
                    }
                    var key = (material.DataType, material.DataId);
                    var info = infos[key];
                    // 工序用量按一默认单位产出维护；引用规格版本时需转换产出基准。
                    var gross = FormatQuantity(production * material.Quantity / output.OutputRatio);
                    var allocated = Math.Min(gross, remaining.GetValueOrDefault(key));
                    remaining[key] = remaining.GetValueOrDefault(key) - allocated;
                    var net = Math.Max(0m, gross - allocated);
                    var inventory = stock.GetValueOrDefault(key) ?? new StockTotal();
                    var reference = material.CraftVersionId;
                    var node = new ScheduleMaterialNodeDto
                    {
                        Id = $"{parentId}/{(int)material.DataType}:{material.DataId}:{reference.GetValueOrDefault()}",
                        ParentId = parentId,
                        Level = level,
                        SourceCraftVersionId = versionId,
                        MaterialRlIds = material.MaterialRlIds,
                        NodeSettingTemplateIds = material.NodeIds,
                        CraftVersionId = reference,
                        Kind = reference.HasValue ? "semi" : "material",
                        Status = gross == 0 ? "无需需求" : net == 0 ? "库存充足"
                            : reference.HasValue ? "待排产" : allocated > 0 ? "部分缺料" : "完全缺料",
                        Gross = gross,
                        Stock = FormatQuantity(inventory.Stock),
                        Occupied = FormatQuantity(inventory.Occupied),
                        SafetyStock = FormatQuantity(info.SafetyStock),
                        Available = FormatQuantity(allocated),
                        Net = FormatQuantity(net),
                    };
                    ApplyMaterialInfo(info, node);
                    foreach (var replacement in material.Substitutes)
                    {
                        var substitute = new ScheduleMaterialSubstituteDto
                        {
                            MaterialRlIds = replacement.MaterialRlIds,
                            NodeSettingTemplateIds = replacement.NodeIds,
                            CraftVersionId = replacement.CraftVersionId,
                            QuantityPerParentUnit = FormatQuantity(replacement.Quantity / output.OutputRatio),
                        };
                        ApplyMaterialInfo(infos[(replacement.DataType, replacement.DataId)], substitute);
                        node.Substitutes.Add(substitute);
                    }
                    if (reference.HasValue)
                    {
                        node.Children = Expand(reference.Value, net, node.Id, level + 1, ancestors);
                    }
                    nodes.Add(node);
                }
                ancestors.Remove(versionId);
                return nodes;
            }

            var product = infos[GetCraftProductKey(graph.Orders[graph.Versions[rootVersionId].CraftOrderId])];
            var result = Expand(rootVersionId, quantity * product.OutputRatio, $"craft:{rootVersionId}", 1, new HashSet<long>());
            count = nodeCount;
            return result;
        }

        private static List<ScheduleMaterialSummaryDto> GetMaterialSummary(List<ScheduleMaterialNodeDto> nodes)
        {
            return GetAllMaterialNodes(nodes).GroupBy(x => (x.DataType, x.DataId)).Select(group =>
            {
                var first = group.First();
                var total = new ScheduleMaterialSummaryDto
                {
                    Gross = FormatQuantity(group.Sum(x => x.Gross)),
                    Available = FormatQuantity(group.Sum(x => x.Available)),
                    Net = FormatQuantity(group.Sum(x => x.Net)),
                    Stock = first.Stock,
                    Occupied = first.Occupied,
                    SafetyStock = first.SafetyStock,
                    OccurrenceCount = group.Count(),
                };
                ApplyMaterialInfo(first, total);
                return total;
            }).ToList();
        }

        #endregion

        #region 数据查询

        private sealed class CraftMaterialData
        {
            public Dictionary<long, Technical_CraftOrderVersion> Versions { get; } = new();
            public Dictionary<long, Technical_CraftOrder> Orders { get; } = new();
            public Dictionary<long, List<Technical_NodeSettingTemplate_Material_RL>> Materials { get; } = new();
            public Dictionary<long, int> NodeSequences { get; } = new();
        }

        private async Task<CraftMaterialData> GetCraftMaterialDataAsync(long rootId)
        {
            var graph = new CraftMaterialData();
            var pending = new List<long> { rootId };
            while (pending.Count > 0)
            {
                if (graph.Versions.Count + pending.Count > 10000)
                {
                    throw new Exception("工艺引用版本过多，请检查引用关系");
                }
                var versions = await _craftVersionService.GetAll().AsNoTracking().Where(x => pending.Contains(x.Id)).ToListAsync();
                var missing = pending.Except(versions.Select(x => x.Id)).ToList();
                if (missing.Count > 0)
                {
                    throw new Exception($"工序单版本不存在或已删除：{string.Join(",", missing)}");
                }
                var orderIds = versions.Select(x => x.CraftOrderId).Distinct().Where(x => !graph.Orders.ContainsKey(x)).ToList();
                var orders = await _craftOrderService.GetAll().AsNoTracking().Where(x => orderIds.Contains(x.Id)).ToListAsync();
                foreach (var order in orders)
                {
                    graph.Orders.Add(order.Id, order);
                }
                if (orderIds.Any(x => !graph.Orders.ContainsKey(x)))
                {
                    throw new Exception("引用的工序单不存在或已删除");
                }
                var nodes = await _nodeSettingService.GetAll().AsNoTracking().Where(x => pending.Contains(x.CraftOrderVersionId)).ToListAsync();
                var nodeIds = nodes.Select(x => x.Id).ToList();
                var rows = await _materialRLService.GetAll().AsNoTracking()
                    .Where(x => nodeIds.Contains(x.NodeSettingTemplateId) && x.IOType == NodeIOType.输入).ToListAsync();
                var nodeVersions = nodes.ToDictionary(x => x.Id, x => x.CraftOrderVersionId);
                var inputsByVersion = rows.ToLookup(x => nodeVersions[x.NodeSettingTemplateId]);
                foreach (var node in nodes)
                {
                    graph.NodeSequences.Add(node.Id, node.Sequence);
                }
                foreach (var version in versions)
                {
                    var inputs = inputsByVersion[version.Id].ToList();
                    var mains = inputs.Where(x => !x.InsteadSourceId.HasValue).ToDictionary(x => x.Id);
                    if (inputs.Any(x => x.InsteadSourceId.HasValue && (!mains.TryGetValue(x.InsteadSourceId.Value, out var main)
                        || main.NodeSettingTemplateId != x.NodeSettingTemplateId)))
                    {
                        throw new Exception($"工序单版本 {version.Id} 存在无主料或跨工序关联的替料");
                    }
                    graph.Versions.Add(version.Id, version);
                    graph.Materials.Add(version.Id, inputs);
                }
                pending = rows.Where(x => x.RefCraftOrderVersionId > 0).Select(x => x.RefCraftOrderVersionId!.Value)
                    .Distinct().Where(x => !graph.Versions.ContainsKey(x)).ToList();
            }
            return graph;
        }

        private sealed class MaterialInfo : ScheduleMaterialInfoDto
        {
            public decimal SafetyStock { get; set; }
            public long ProductId { get; set; }
            public long OutputUnitId { get; set; }
            public string OutputUnit { get; set; } = string.Empty;
            public decimal OutputRatio { get; set; } = 1m;
            public long OutputConversionId { get; set; }
        }

        private async Task<Dictionary<(InventoryDataType DataType, long DataId), MaterialInfo>> GetMaterialInfoAsync(CraftMaterialData graph)
        {
            var keys = graph.Orders.Values.Select(GetCraftProductKey)
                .Concat(graph.Materials.Values.SelectMany(x => x).Select(x => (x.DataType, x.DataId))).Distinct().ToList();
            if (keys.Any(x => x.DataType != InventoryDataType.Product && x.DataType != InventoryDataType.Specification))
            {
                throw new Exception("工序领用物料仅支持产品或规格类型");
            }
            var productIds = keys.Where(x => x.DataType == InventoryDataType.Product).Select(x => x.DataId).ToList();
            var specificationIds = keys.Where(x => x.DataType == InventoryDataType.Specification).Select(x => x.DataId).ToList();
            var products = await _productService.GetAll().AsNoTracking().Where(x => productIds.Contains(x.Id))
                .Select(x => new MaterialInfo
                {
                    DataType = InventoryDataType.Product,
                    DataId = x.Id,
                    ProductId = x.Id,
                    Code = x.ProductNo,
                    Name = x.ProductName,
                    Specification = x.MaterialSpecification ?? string.Empty,
                    UnitId = x.BaseUnitId,
                    Unit = x.BaseUnit.Name,
                    SafetyStock = x.SafetyStock,
                    OutputUnitId = x.BaseUnitId,
                    OutputUnit = x.BaseUnit.Name,
                }).ToListAsync();
            var specifications = await _specificationService.GetAll().AsNoTracking()
                .Where(x => specificationIds.Contains(x.Id) && !x.Product.IsDeleted)
                .Select(x => new MaterialInfo
                {
                    DataType = InventoryDataType.Specification,
                    DataId = x.Id,
                    ProductId = x.ProductId,
                    Code = x.Code ?? string.Empty,
                    Name = x.Name ?? string.Empty,
                    Specification = x.Specification,
                    UnitId = x.Product.BaseUnitId,
                    Unit = x.Product.BaseUnit.Name,
                    SafetyStock = x.SafetyStock,
                    OutputUnitId = x.SpecificationUnitId ?? x.Product.BaseUnitId,
                    OutputUnit = x.SpecificationUnit != null ? x.SpecificationUnit.Name : x.Product.BaseUnit.Name,
                }).ToListAsync();
            var result = products.Concat(specifications).ToDictionary(x => (x.DataType, x.DataId));
            if (keys.Any(x => !result.ContainsKey(x)))
            {
                throw new Exception("工序单引用的物料不存在或已删除");
            }
            var conversions = await _unitConversionService.GetAll().AsNoTracking().Where(x => specificationIds.Contains(x.SourceId)
                && x.ConversionType == UnitConversionType.SpecToProduct).ToListAsync();
            foreach (var info in specifications.Where(x => x.OutputUnitId != x.UnitId))
            {
                var matches = conversions.Where(x => x.SourceId == info.DataId && x.TargetId == info.ProductId
                    && x.FromUnitId == info.OutputUnitId && x.ToUnitId == info.UnitId).ToList();
                info.OutputRatio = matches.Count == 1 && matches[0].Ratio > 0 ? matches[0].Ratio : 0m;
                info.OutputConversionId = info.OutputRatio > 0 ? matches[0].Id : 0;
            }
            if (graph.Orders.Values.Any(x => result[GetCraftProductKey(x)].OutputRatio <= 0))
            {
                throw new Exception("工序单产出规格缺少唯一有效的基本单位转换关系");
            }
            if (result.Values.Any(x => x.UnitId <= 0 || x.OutputUnitId <= 0 || x.SafetyStock < 0))
            {
                throw new Exception("工序物料基本单位或安全库存配置无效");
            }
            return result;
        }

        private sealed class StockTotal
        {
            public decimal Stock { get; set; }
            public decimal Occupied { get; set; }
        }

        private async Task<Dictionary<(InventoryDataType DataType, long DataId), StockTotal>> GetInventoryQuantityAsync(
            IEnumerable<(InventoryDataType DataType, long DataId)> keys)
        {
            var productIds = keys.Where(x => x.DataType == InventoryDataType.Product).Select(x => x.DataId).ToList();
            var specificationIds = keys.Where(x => x.DataType == InventoryDataType.Specification).Select(x => x.DataId).ToList();
            // 与现有库存汇总口径一致：正常入库余量，排除异常库存；占用只统计尚未释放的部分。
            var records = await _inventoryService.GetAll().AsNoTracking().Where(x => x.InventoryType == InventoryType.入库
                    && x.SourceType != InventorySourceType.异常入库 && x.RemainQuantity > 0
                    && ((x.DataType == InventoryDataType.Product && productIds.Contains(x.DataId))
                        || (x.DataType == InventoryDataType.Specification && specificationIds.Contains(x.DataId))))
                .Select(x => new
                {
                    x.DataType,
                    x.DataId,
                    x.RemainQuantity,
                    Occupations = x.OccupyList.Where(y => !y.IsCompleted)
                        .Select(y => new { y.OccupyQuantity, y.ReleaseQuantity }).ToList(),
                }).ToListAsync();
            return records.GroupBy(x => (x.DataType, x.DataId)).ToDictionary(x => x.Key, x => new StockTotal
            {
                Stock = x.Sum(y => y.RemainQuantity),
                Occupied = x.Sum(y => y.Occupations.Sum(z => Math.Max(0m, z.OccupyQuantity - z.ReleaseQuantity))),
            });
        }

        #endregion

        #region 物料汇总与单位换算

        private sealed class MaterialTotal
        {
            public InventoryDataType DataType { get; set; }
            public long DataId { get; set; }
            public long? CraftVersionId { get; set; }
            public decimal Quantity { get; set; }
            public List<long> MaterialRlIds { get; set; } = new();
            public List<long> NodeIds { get; set; } = new();
            public List<MaterialTotal> Substitutes { get; set; } = new();
        }

        private static Dictionary<long, List<MaterialTotal>> GetMaterialTotals(CraftMaterialData graph,
            Dictionary<(InventoryDataType DataType, long DataId), MaterialInfo> infos)
        {
            var result = new Dictionary<long, List<MaterialTotal>>();
            foreach (var (versionId, rows) in graph.Materials)
            {
                var substitutes = rows.Where(x => x.InsteadSourceId.HasValue)
                    .ToLookup(x => x.InsteadSourceId!.Value);
                var groups = rows.Where(x => !x.InsteadSourceId.HasValue)
                    .OrderBy(x => graph.NodeSequences[x.NodeSettingTemplateId])
                    .ThenBy(x => x.SortNo).ThenBy(x => x.Id)
                    .GroupBy(GetMaterialKey);
                var totals = new List<MaterialTotal>();
                foreach (var group in groups)
                {
                    var total = CreateMaterialTotal(group.ToList(), graph, infos);
                    var replacementGroups = group.SelectMany(row => substitutes[row.Id]
                            .OrderBy(x => x.SortNo).ThenBy(x => x.Id))
                        .GroupBy(GetMaterialKey);
                    foreach (var replacementGroup in replacementGroups)
                    {
                        if (replacementGroup.Key == group.Key)
                        {
                            throw new Exception($"工序物料 {group.First().Id} 的替料不能与主料相同");
                        }
                        total.Substitutes.Add(CreateMaterialTotal(replacementGroup.ToList(), graph, infos));
                    }
                    totals.Add(total);
                }
                result.Add(versionId, totals);
            }
            return result;
        }

        private static MaterialTotal CreateMaterialTotal(List<Technical_NodeSettingTemplate_Material_RL> rows,
            CraftMaterialData graph, Dictionary<(InventoryDataType DataType, long DataId), MaterialInfo> infos)
        {
            var first = rows.First();
            var key = GetMaterialKey(first);
            if (key.CraftVersionId.HasValue)
            {
                var order = graph.Orders[graph.Versions[key.CraftVersionId.Value].CraftOrderId];
                if ((first.DataType == InventoryDataType.Product && first.DataId != order.ProductId)
                    || (first.DataType == InventoryDataType.Specification && first.DataId != order.SpecificationId))
                {
                    throw new Exception($"工序物料 {first.Id} 与引用工序单的产品/规格不匹配");
                }
            }
            return new MaterialTotal
            {
                DataType = first.DataType,
                DataId = first.DataId,
                CraftVersionId = key.CraftVersionId,
                Quantity = rows.Sum(x => GetMaterialBaseQuantity(x, infos[(x.DataType, x.DataId)])),
                MaterialRlIds = rows.Select(x => x.Id).ToList(),
                NodeIds = rows.Select(x => x.NodeSettingTemplateId).Distinct().ToList(),
            };
        }

        private static (InventoryDataType DataType, long DataId, long? CraftVersionId) GetMaterialKey(
            Technical_NodeSettingTemplate_Material_RL material)
        {
            return (material.DataType, material.DataId,
                material.RefCraftOrderVersionId > 0 ? material.RefCraftOrderVersionId : null);
        }

        private static decimal GetMaterialBaseQuantity(Technical_NodeSettingTemplate_Material_RL row, MaterialInfo info)
        {
            if (row.Quantity < 0)
            {
                throw new Exception($"工序物料 {row.Id} 用量不能为负数");
            }
            var unitId = row.UnitId > 0 ? row.UnitId : info.OutputUnitId;
            if (unitId == info.UnitId)
            {
                return row.Quantity;
            }
            List<InventoryUnitInboundDto> units;
            try
            {
                units = JsonSerializer.Deserialize<List<InventoryUnitInboundDto>>(
                    string.IsNullOrWhiteSpace(row.UnitListInfo) ? "[]" : row.UnitListInfo, JsonHelper.DefaultJsonSerializerOptions) ?? new();
            }
            catch (JsonException)
            {
                throw new Exception($"工序物料 {row.Id} 单位快照格式无效");
            }
            var matches = units.Where(x => x.UnitId == unitId && x.Id == row.UnitConversionId).ToList();
            if (matches.Count > 1)
            {
                throw new Exception($"工序物料 {row.Id} 单位快照存在重复转换关系");
            }
            var ratio = matches.Count == 1 ? matches[0].Ratio
                : unitId == info.OutputUnitId && (row.UnitConversionId == 0 || row.UnitConversionId == info.OutputConversionId) ? info.OutputRatio : 0m;
            if (ratio <= 0)
            {
                throw new Exception($"工序物料 {row.Id} 缺少有效的基本单位转换关系");
            }
            return row.Quantity * ratio;
        }

        private static (InventoryDataType DataType, long DataId) GetCraftProductKey(Technical_CraftOrder order)
            => order.SpecificationId.HasValue ? (InventoryDataType.Specification, order.SpecificationId.Value)
                : (InventoryDataType.Product, order.ProductId);

        #endregion

        #region 数据整理

        private static decimal FormatQuantity(decimal value) => Math.Round(value, 4, MidpointRounding.AwayFromZero);

        private static void ApplyMaterialInfo(ScheduleMaterialInfoDto source, ScheduleMaterialInfoDto target)
        {
            target.DataType = source.DataType;
            target.DataId = source.DataId;
            target.Code = source.Code;
            target.Name = source.Name;
            target.Specification = source.Specification;
            target.UnitId = source.UnitId;
            target.Unit = source.Unit;
        }

        private static IEnumerable<ScheduleMaterialNodeDto> GetAllMaterialNodes(IEnumerable<ScheduleMaterialNodeDto> nodes)
        {
            foreach (var node in nodes)
            {
                yield return node;
                foreach (var child in GetAllMaterialNodes(node.Children))
                {
                    yield return child;
                }
            }
        }

        private static List<ScheduleMaterialNodeDto> FilterMaterialTree(List<ScheduleMaterialNodeDto> nodes, string? keyword, bool shortageOnly)
        {
            if (string.IsNullOrEmpty(keyword) && !shortageOnly)
            {
                return nodes;
            }
            var result = new List<ScheduleMaterialNodeDto>();
            foreach (var node in nodes)
            {
                node.Children = FilterMaterialTree(node.Children, keyword, shortageOnly);
                var matches = (string.IsNullOrEmpty(keyword) || $"{node.Code} {node.Name} {node.Specification}"
                    .Contains(keyword, StringComparison.OrdinalIgnoreCase)) && (!shortageOnly || node.Net > 0);
                if (matches || node.Children.Count > 0)
                {
                    result.Add(node);
                }
            }
            return result;
        }

        #endregion
    }
}
