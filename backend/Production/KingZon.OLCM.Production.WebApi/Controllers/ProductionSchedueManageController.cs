using Kingzon.OLCM.Base.Services;
using KingZon.Core;
using KingZon.Core.DataBase;
using KingZon.Core.Web;
using KingZon.OLCM.Base.Shared.DataBase.Entities;
using KingZon.OLCM.Base.Shared.DataBase.Enum;
using KingZon.OLCM.Base.Shared.DataBase.JsonStructure;
using KingZon.OLCM.Inventory.Shared.DataBase.Entities;
using KingZon.OLCM.Production.Shared.DataBase.Entities;
using KingZon.OLCM.Production.Shared.DataBase.Enum;
using KingZon.OLCM.Production.WebApi.Dto;
using KingZon.OLCM.Base.DataBase.Enum;
using KingZon.OLCM.Base.Service;
using KingZon.Mes.Workflow.Entity;
using KingZon.Mes.Workflow.Enum;
using KingZon.Mes.Workflow.Service;
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
using System.Transactions;

namespace KingZon.OLCM.Technical.WebApi.Controllers
{
    [Route("api/production/[controller]")]
    [ApiController]
    public class ProductionScheduleManageController : KingZonControllerBase
    {
        private readonly CommonService<Production_Dispatch_Order> _dispatchOrderService;
        private readonly CommonService<WF_Node_ProductionExt> _productionNodeService;
        private readonly CommonService<WF_Node_ProductionExt_MachineRL> _productionMachineService;
        private readonly CommonService<Production_Material_Receive_Demand> _receiveDemandService;
        private readonly CommonService<Production_Machine_Material_Receive_Demand> _machineDemandService;
        private readonly CommonService<WF_Node_ProductionExt_MaterialRL> _productionMaterialService;
        private readonly CommonService<WF_NodeType> _nodeTypeService;
        private readonly WorkflowService _workflowService;
        private readonly SequenceService _sequenceService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly CommonService<Technical_CraftOrder> _craftOrderService;
        private readonly CommonService<Technical_CraftOrderVersion> _craftVersionService;
        private readonly CommonService<WF_NodeTemplate_ProductionExt> _nodeSettingService;
        private readonly CommonService<WF_NodeTemplate_Material_RL> _materialRLService;
        private readonly CommonService<Base_Product> _productService;
        private readonly CommonService<Base_ProductSpecification> _specificationService;
        private readonly CommonService<Base_UnitConversion> _unitConversionService;
        private readonly CommonService<Inventory_Record> _inventoryService;

        private readonly CommonService<Base_Machine> _machineService;
        private readonly CommonService<Base_Craft> _craftService;
        private readonly CommonService<Base_Department> _departmentService;
        private readonly CommonService<WF_NodeTemplate_Machine_RL> _templateMachineService;

        public ProductionScheduleManageController(
            CommonService<Technical_CraftOrder> craftOrderService,
            CommonService<Technical_CraftOrderVersion> craftVersionService,
            CommonService<WF_NodeTemplate_ProductionExt> nodeSettingService,
            CommonService<WF_NodeTemplate_Material_RL> materialRLService,
            CommonService<Base_Product> productService,
            CommonService<Base_ProductSpecification> specificationService,
            CommonService<Base_UnitConversion> unitConversionService,
            CommonService<Inventory_Record> inventoryService,
            CommonService<Base_Machine> machineService,
            CommonService<Base_Craft> craftService,
            CommonService<Base_Department> departmentService,
            CommonService<WF_NodeTemplate_Machine_RL> templateMachineService,
            CommonService<Production_Dispatch_Order> dispatchOrderService,
            CommonService<WF_Node_ProductionExt> productionNodeService,
            CommonService<WF_Node_ProductionExt_MachineRL> productionMachineService,
            CommonService<Production_Material_Receive_Demand> receiveDemandService,
            CommonService<Production_Machine_Material_Receive_Demand> machineDemandService,
            CommonService<WF_Node_ProductionExt_MaterialRL> productionMaterialService,
            CommonService<WF_NodeType> nodeTypeService,
            WorkflowService workflowService,
            SequenceService sequenceService,
            IUnitOfWork unitOfWork)
        {
            _dispatchOrderService = dispatchOrderService;
            _productionNodeService = productionNodeService;
            _productionMachineService = productionMachineService;
            _receiveDemandService = receiveDemandService;
            _machineDemandService = machineDemandService;
            _productionMaterialService = productionMaterialService;
            _nodeTypeService = nodeTypeService;
            _workflowService = workflowService;
            _sequenceService = sequenceService;
            _unitOfWork = unitOfWork;
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
                    x.Id,
                    Text = x.Code + " " + x.Name,
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
            // 保留工艺模板的一道工序和只读工艺属性，排产界面支持增删排序及编辑资源分配。
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
                    var baseQuantity = GetMaterialBaseQuantity(new WF_NodeTemplate_Material_RL
                    {
                        Id = row.Id,
                        Quantity = 1m,
                        UnitId = row.UnitId,
                        UnitConversionId = row.UnitConversionId,
                        UnitListInfo = row.UnitListInfo,
                    }, info);
                    var unitId = row.UnitId > 0 ? row.UnitId : info.OutputUnitId;
                    var conversionId = unitId == info.UnitId ? 0 : row.UnitConversionId;
                    if (!units.Any(x => x.Id == conversionId && x.UnitId == unitId))
                        units.Add(new InventoryUnitInboundDto
                        {
                            Id = conversionId,
                            UnitId = unitId,
                            UnitName = unitId == info.OutputUnitId ? info.OutputUnit : info.Unit,
                            Ratio = baseQuantity
                        });
                    return new
                    {
                        Order = index + 1,
                        row.DataId,
                        row.DataType,
                        Code = info.Code,
                        Name = info.Name,
                        Quantity = row.Quantity,
                        QuantityPerUnit = row.Quantity,
                        UnitId = unitId,
                        UnitConversionId = conversionId,
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
                    IsReportWork = source.IsReport,
                    Materials = materialRows,
                    Machines = machinesByNode[source.Id]
                        .Where(x => machineCrafts.Contains((x.MachineId, source.CraftId ?? 0)))
                        .Select(x => new { x.MachineId, Users = Array.Empty<object>() }).ToList(),
                };
            }).ToList();
            return Ok(new
            {
                CraftVersionId = version.Id,
                version.Version,
                ProductCode = product.Code,
                ProductName = product.Name,
                QuantityUnit = product.OutputUnit,
                Processs = processs,
                Crafts = crafts,
                Departments = departments,
                Machines = machines,
            });
        }

        /// <summary>查询指定工艺版本的实际排产单及工序、资源分配。</summary>
        [HttpGet("GetScheduleList")]
        public async Task<IActionResult> GetScheduleList([FromQuery] long craftVersionId, [FromQuery] long? rootDispatchOrderId = null)
        {
            if (craftVersionId <= 0) throw new Exception("请选择工艺版本");
            var orders = await _dispatchOrderService.GetAll().AsNoTracking()
                .Where(x => !x.IsDeleted && x.CraftOrderVersionId == craftVersionId
                    && (!rootDispatchOrderId.HasValue || x.Id == rootDispatchOrderId || x.RootDispatchOrderId == rootDispatchOrderId))
                .OrderByDescending(x => x.Id)
                .Select(x => new { x.Id, x.OrderNo, x.CraftOrderVersionId, x.DispatchQuantity,
                    x.DispatchDate, x.PlanCompleteDate, x.Status, x.DispatchUnit, x.IsUrgent, x.Remark, x.RootDispatchOrderId,
                    WorkflowState = x.Workflow == null ? (WorkflowState?)null : x.Workflow.State,
                    DepartmentName = x.Department == null ? "" : x.Department.Name })
                .ToListAsync();
            var orderIds = orders.Select(x => x.Id).ToList();
            var nodes = await _productionNodeService.GetAll().AsNoTracking()
                .Where(x => !x.IsDeleted && orderIds.Contains(x.DispatchOrderId))
                .OrderBy(x => x.Order).ThenBy(x => x.Id)
                .Select(x => new { x.Id, x.DispatchOrderId, x.Order, x.Name, CraftName = x.Craft.Name,
                    x.PlanWorkHours, x.ActualWorkHours, x.ReciveQuantity, x.CompleteQuantity })
                .ToListAsync();
            var nodeIds = nodes.Select(x => x.Id).ToList();
            var machines = await _productionMachineService.GetAll().AsNoTracking()
                .Where(x => nodeIds.Contains(x.NodeId))
                .Select(x => new { x.Id, x.NodeId, x.MachineId, MachineName = x.Machine.Code + " " + x.Machine.Name,
                    x.UserId, UserName = x.User == null ? "" : x.User.Name,
                    Quantity = x.DispatchQuantity, Hours = x.PlanWorkHours, x.ActualWorkHours })
                .ToListAsync();
            var materialRows = await _receiveDemandService.GetAll().AsNoTracking()
                .Where(x => !x.IsDeleted && x.NodeId.HasValue && nodeIds.Contains(x.NodeId.Value))
                .OrderBy(x => x.Order).ThenBy(x => x.Id)
                .Select(x => new { x.Id, x.NodeId, x.DataId, x.DataType, x.DemandQuantity, x.BaseDemandQuantity,
                    UnitName = x.Unit.Name, x.BaseReceiveQuantity, x.BaseReturnQuantity }).ToListAsync();
            var productIds = materialRows.Where(x => x.DataType == InventoryDataType.Product).Select(x => x.DataId).Distinct().ToList();
            var specificationIds = materialRows.Where(x => x.DataType == InventoryDataType.Specification).Select(x => x.DataId).Distinct().ToList();
            var products = await _productService.GetAll().AsNoTracking().Where(x => productIds.Contains(x.Id))
                .Select(x => new ScheduleMaterialInfoDto { DataType = InventoryDataType.Product, DataId = x.Id, Code = x.ProductNo, Name = x.ProductName }).ToListAsync();
            var specifications = await _specificationService.GetAll().AsNoTracking().Where(x => specificationIds.Contains(x.Id))
                .Select(x => new ScheduleMaterialInfoDto { DataType = InventoryDataType.Specification, DataId = x.Id, Code = x.Code ?? "", Name = x.Name ?? "" }).ToListAsync();
            var materialInfos = products.Concat(specifications).ToDictionary(x => (x.DataType, x.DataId));
            var materialsByNode = materialRows.ToLookup(x => x.NodeId!.Value);
            var nodesByOrder = nodes.ToLookup(x => x.DispatchOrderId);
            var machinesByNode = machines.ToLookup(x => x.NodeId);
            return Ok(orders.Select(order => new
            {
                order.Id, order.OrderNo, CraftVersionId = order.CraftOrderVersionId, order.RootDispatchOrderId,
                Quantity = order.DispatchQuantity, Start = order.DispatchDate, End = order.PlanCompleteDate,
                StatusText = order.WorkflowState == WorkflowState.新建 ? "已排产" : order.Status.ToString(),
                CanStart = order.WorkflowState == WorkflowState.新建,
                CanDelete = order.WorkflowState == WorkflowState.新建,
                order.DispatchUnit, order.DepartmentName,
                Urgent = order.IsUrgent, order.Remark,
                Processes = nodesByOrder[order.Id].Select(node => new
                {
                    node.Id, node.Order, node.Name, node.CraftName, node.PlanWorkHours, node.ActualWorkHours,
                    node.ReciveQuantity, node.CompleteQuantity, MaterialCount = materialsByNode[node.Id].Count(),
                    Machines = machinesByNode[node.Id].ToList(),
                    Materials = materialsByNode[node.Id].Select(material => new
                    {
                        material.Id, material.DemandQuantity, material.UnitName, material.BaseDemandQuantity,
                        material.BaseReceiveQuantity, material.BaseReturnQuantity,
                        Code = materialInfos.GetValueOrDefault((material.DataType, material.DataId))?.Code ?? material.DataId.ToString(),
                        Name = materialInfos.GetValueOrDefault((material.DataType, material.DataId))?.Name ?? "已删除物料"
                    }).ToList()
                }).ToList()
            }).ToList());
        }

        /// <summary>工艺版本仅作为模板，实例按本次提交的工序顺序、物料和资源保存。</summary>
        [HttpPost("CreateSchedule")]
        public async Task<IActionResult> CreateSchedule([FromBody] CreateProductionScheduleDto postData)
        {
            if (postData == null || postData.CraftVersionId <= 0 || postData.RootCraftVersionId <= 0)
                throw new Exception("请选择工艺版本及顶层工艺版本");
            CheckScheduleNumber(postData.Quantity, "排产数量", true);
            if (postData.Start == default || postData.End <= postData.Start) throw new Exception("计划完成必须晚于计划开始");
            if (postData.CirculationCard) throw new Exception("当前排产不支持流转卡");
            if (postData.Processs == null || postData.Processs.Count == 0) throw new Exception("请至少配置一道工序");
            if (postData.Processs.Any(x => x == null || x.Materials == null || x.MachineRls == null
                || x.Materials.Any(y => y == null) || x.MachineRls.Any(y => y == null)))
                throw new Exception("工序物料和资源参数不完整");
            using var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
            if (!postData.RootDispatchOrderId.HasValue)
            {
                if (postData.CraftVersionId != postData.RootCraftVersionId)
                    throw new Exception("请先生成顶层排产单，再对子级排产");
            }
            else
            {
                var rootOrder = await _dispatchOrderService.GetAll().AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Id == postData.RootDispatchOrderId && !x.IsDeleted && x.RootDispatchOrderId == null)
                    ?? throw new Exception("顶层排产单不存在或已删除，请先生成顶层排产单");
                if (rootOrder.CraftOrderVersionId != postData.RootCraftVersionId)
                    throw new Exception("顶层排产单不属于当前计划工艺版本");
                if (postData.CraftVersionId == postData.RootCraftVersionId)
                    throw new Exception("顶层排产单已生成，不能作为自身的子级排产");
                var rootMaterials = await GetCraftMaterialDataAsync(postData.RootCraftVersionId);
                if (!rootMaterials.Versions.ContainsKey(postData.CraftVersionId))
                    throw new Exception("当前工艺版本不属于顶层排产单的领用物料引用范围");
            }
            var version = await _craftVersionService.GetAll().FirstOrDefaultAsync(x => x.Id == postData.CraftVersionId && !x.IsDeleted)
                ?? throw new Exception("工艺版本不存在或已删除");
            if (version.Status != CraftOrderVersionStatus.已发布) throw new Exception("仅允许使用已发布工艺版本排产");
            var craftOrder = await _craftOrderService.GetAll().FirstOrDefaultAsync(x => x.Id == version.CraftOrderId && !x.IsDeleted)
                ?? throw new Exception("工艺单不存在或已删除");
            if (postData.DepartmentId.HasValue && !await _departmentService.GetAll().AnyAsync(x => x.Id == postData.DepartmentId))
                throw new Exception("派工部门不存在");
            var templates = await _nodeSettingService.GetAll().AsNoTracking()
                .Where(x => x.CraftOrderVersionId == version.Id && !x.IsDeleted).ToDictionaryAsync(x => x.Id);
            var templateIds = templates.Keys.ToList();
            var templateMaterials = await _materialRLService.GetAll().AsNoTracking()
                .Where(x => templateIds.Contains(x.NodeSettingTemplateId)).ToListAsync();
            var craftIds = postData.Processs.Select(x => x.CraftId).Distinct().ToList();
            if (await _craftService.GetAll().CountAsync(x => craftIds.Contains(x.Id)) != craftIds.Count)
                throw new Exception("工序加工工艺不存在");
            var machineIds = postData.Processs.SelectMany(x => x.MachineRls).Select(x => x.MachineId).Distinct().ToList();
            var machines = await _machineService.GetAll().AsNoTracking()
                .Where(x => machineIds.Contains(x.Id) && !x.IsDeleted && !x.IsStop)
                .Select(x => new { x.Id, CraftIds = x.CraftRLs.Select(y => y.CraftId).ToList(),
                    UserIds = x.UserRLs.Select(y => y.UserId).ToList() }).ToDictionaryAsync(x => x.Id);
            var requiredTypes = postData.Processs.Select(x => (long)(x.IsOutsource ? ProductionScheduleNodeType.外协 : ProductionScheduleNodeType.生产))
                .Concat(postData.Processs.Any(x => x.IsInbound || x.IsOutsource) ? new[] { (long)ProductionScheduleNodeType.入库 } : Array.Empty<long>())
                .Distinct().ToList();
            var nodeTypes = await _nodeTypeService.GetAll().Where(x => requiredTypes.Contains(x.Id)).ToDictionaryAsync(x => x.Id);
            if (requiredTypes.Any(id => !nodeTypes.ContainsKey(id) || nodeTypes[id].Type != NodeType.node))
                throw new Exception("请配置排产节点类型：生产=1、外协=2、入库=3，工作流结构类型均须为 node");

            var graph = new CraftMaterialData();
            graph.Orders.Add(craftOrder.Id, craftOrder);
            graph.Materials.Add(version.Id, templateMaterials.Concat(postData.Processs.SelectMany(x => x.Materials)
                .Select(x => new WF_NodeTemplate_Material_RL { DataId = x.DataId, DataType = x.DataType })).ToList());
            var infos = await GetMaterialInfoAsync(graph);
            var output = infos[GetCraftProductKey(craftOrder)];
            var mainQuantity = FormatQuantity(postData.Quantity * output.OutputRatio);
            CheckScheduleNumber(mainQuantity, "主单位排产数量", true);
            var codes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var materialUnits = new Dictionary<ProcessMaterialDto, (decimal Ratio, string Json)>();
            var conversionIds = postData.Processs.SelectMany(x => x.Materials).Select(x => x.UnitConversionId).Where(x => x > 0).ToList();
            var conversions = await _unitConversionService.GetAll().AsNoTracking().Include(x => x.FromUnit)
                .Where(x => conversionIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id);
            foreach (var process in postData.Processs)
            {
                if (string.IsNullOrWhiteSpace(process.Name) || string.IsNullOrWhiteSpace(process.Code) || !codes.Add(process.Code.Trim()))
                    throw new Exception("工序名称、编码不能为空，编码不能重复");
                if (process.NodeSettingTemplateId > 0)
                {
                    if (!templates.TryGetValue(process.NodeSettingTemplateId, out var source)
                        || source.CraftId != process.CraftId || source.IsOutsource != process.IsOutsource
                        || source.IsInbound != process.IsInbound || source.IsReport != process.IsReportWork)
                        throw new Exception($"工序【{process.Name}】模板或工艺属性已变化，请重新加载");
                }
                else if (process.IsOutsource || process.IsInbound)
                    throw new Exception("临时工序仅支持普通生产，工艺属性不可更改");
                if (process.MachineRls.GroupBy(x => (x.MachineId, x.UserId)).Any(x => x.Count() > 1))
                    throw new Exception($"工序【{process.Name}】设备人员分配重复");
                foreach (var allocation in process.MachineRls)
                {
                    if (!machines.TryGetValue(allocation.MachineId, out var machine) || !machine.CraftIds.Contains(process.CraftId)
                        || (allocation.UserId != 0 && !machine.UserIds.Contains(checked((int)allocation.UserId))))
                        throw new Exception($"工序【{process.Name}】设备或人员不属于当前工艺配置");
                    CheckScheduleNumber(allocation.Quantity, "分配数量", !process.IsOutsource || allocation.UserId != 0);
                    CheckScheduleNumber(allocation.Hours, "设备或人员工时");

                }
                if (process.MachineRls.GroupBy(x => x.MachineId).Any(group => group.Any(x => x.UserId == 0) && group.Any(x => x.UserId != 0)))
                    throw new Exception("同一设备不能同时按设备和人员重复分配");
                var allocated = process.MachineRls.Sum(x => x.Quantity);
                if ((!process.IsOutsource || allocated > 0) && allocated != postData.Quantity)
                    throw new Exception($"工序【{process.Name}】分配数量合计必须等于排产数量");
                CheckScheduleNumber(process.MachineRls.Sum(x => x.Hours), "工序总工时");
                if (process.Materials.GroupBy(x => (x.DataType, x.DataId)).Any(x => x.Count() > 1))
                    throw new Exception($"工序【{process.Name}】存在重复物料");
                foreach (var material in process.Materials)
                {
                    CheckMaterialNumber(material.Quantity, "领用物料数量");
                    materialUnits.Add(material, GetScheduleMaterialUnit(material, infos[(material.DataType, material.DataId)],
                        templateMaterials.Where(x => x.NodeSettingTemplateId == process.NodeSettingTemplateId && x.DataType == material.DataType && x.DataId == material.DataId), conversions));
                }
            }
            CheckScheduleNumber(postData.Processs.SelectMany(x => x.MachineRls).Sum(x => x.Hours), "排产总工时");
            var order = new Production_Dispatch_Order
            {
                OrderNo = _sequenceService.GenerateCode(TableName.DispatchOrder),
                ProductId = output.ProductId, SpecificationId = craftOrder.SpecificationId,
                DepartmentId = postData.DepartmentId, DispatchDate = postData.Start, PlanCompleteDate = postData.End,
                DispatchQuantity = postData.Quantity, BaseQuantity = postData.Quantity, MainQuantity = mainQuantity,
                // 当前排产单位固定为工艺产品/规格的基础单位；收入计价保持未配置，不伪造单价和收入换算。
                DispatchUnit = JsonSerializer.Serialize(new
                {
                    productionUnitId = output.OutputUnitId, productionUnitName = output.OutputUnit,
                    baseUnitId = output.OutputUnitId, baseUnitName = output.OutputUnit, baseRatio = 1m,
                    mainUnitId = output.UnitId, mainUnitName = output.Unit, mainRatio = output.OutputRatio,
                    incomeUnitId = 0, incomeUnitName = "", incomeRatio = 0m,
                    unitConversionId = 0, baseUnitConversionId = 0, mainUnitConversionId = output.OutputConversionId
                }, JsonHelper.DefaultJsonSerializerOptions),
                Status = DispatchOrderStatus.进行中, BasisType = DispatchBasisType.工艺单,
                BasisId = version.Id, CraftOrderVersionId = version.Id, IsUrgent = postData.Urgent,
                RootDispatchOrderId = postData.RootDispatchOrderId,
                Remark = postData.Remark?.Trim(), PlanWorkHours = postData.Processs.SelectMany(x => x.MachineRls).Sum(x => x.Hours)
            };
            await _dispatchOrderService.InsertAsync(order);
            await _unitOfWork.SaveChangeAsync();
            var route = new List<WF_Node>();
            var permissionTemplates = new Dictionary<WF_Node, long>();
            var processNodes = new List<(ScheduleProcessDto Process, WF_Node_ProductionExt Node)>();
            foreach (var process in postData.Processs)
            {
                var typeId = (long)(process.IsOutsource ? ProductionScheduleNodeType.外协 : ProductionScheduleNodeType.生产);
                var node = new WF_Node_ProductionExt
                {
                    DispatchOrderId = order.Id, Order = route.Count + 1, CraftId = process.CraftId,
                    NodeTemplateId = process.NodeSettingTemplateId > 0 ? process.NodeSettingTemplateId : null,
                    NodeTemplateSettingId = process.NodeSettingTemplateId > 0 ? process.NodeSettingTemplateId : null,
                    Name = process.Code.Trim() + " · " + process.Name.Trim(), Type = "node", NodeType = nodeTypes[typeId], NodeTypeId = typeId,
                    PlanWorkHours = process.MachineRls.Sum(x => x.Hours)
                };
                route.Add(node);
                processNodes.Add((process, node));
                if (process.IsInbound || process.IsOutsource)
                {
                    var inbound = new WF_Node_ProductionExt
                    {
                        DispatchOrderId = order.Id, Order = route.Count + 1, CraftId = process.CraftId,
                        Name = process.Name.Trim() + " · 入库", Type = "node",
                        NodeTypeId = (long)ProductionScheduleNodeType.入库, NodeType = nodeTypes[(long)ProductionScheduleNodeType.入库]
                    };
                    route.Add(inbound);
                    // 生成的入库节点不冒充模板节点，但沿用源工序的权限配置。
                    if (process.NodeSettingTemplateId > 0) permissionTemplates.Add(inbound, process.NodeSettingTemplateId);
                }
            }
            var workflow = _workflowService.CreateWorkflow(order.Id, version.WorkflowTemplateId, order.OrderNo, route, permissionTemplates);
            await _unitOfWork.SaveChangeAsync();
            order.WorkflowId = workflow.Id;
            foreach (var (process, node) in processNodes)
            {
                var allocations = process.MachineRls.Select(x => new WF_Node_ProductionExt_MachineRL
                {
                    NodeId = node.Id, MachineId = x.MachineId, UserId = x.UserId > 0 ? checked((int)x.UserId) : null,
                    DispatchQuantity = x.Quantity, PlanWorkHours = x.Hours
                }).ToList();
                if (allocations.Count > 0) await _productionMachineService.InsertAsync(allocations);
                foreach (var material in process.Materials)
                {
                    var unit = materialUnits[material];
                    var baseQuantity = Math.Round(material.Quantity * unit.Ratio, 6, MidpointRounding.AwayFromZero);
                    CheckMaterialNumber(baseQuantity, "主单位领料数量");
                    if (material.Quantity > 0 && baseQuantity == 0) throw new Exception("主单位领料数量小于存储精度");
                    var unitQuantity = Math.Round(material.Quantity / postData.Quantity, 6, MidpointRounding.AwayFromZero);
                    CheckMaterialNumber(unitQuantity, "单位领料用量");
                    var demand = new Production_Material_Receive_Demand
                    {
                        DispatchOrderId = order.Id, NodeId = node.Id, DataId = material.DataId, DataType = material.DataType,
                        DemandQuantity = material.Quantity, BaseDemandQuantity = baseQuantity,
                        UnitQuantity = unitQuantity,
                        UnitId = material.UnitId, UnitConversionId = material.UnitConversionId > 0 ? material.UnitConversionId : null,
                        ConvUnitInfo = unit.Json, Order = material.Order
                    };
                    await _receiveDemandService.InsertAsync(demand);
                    var sourceMaterial = templateMaterials.FirstOrDefault(x => x.NodeSettingTemplateId == process.NodeSettingTemplateId
                        && x.IOType == NodeIOType.输入 && x.DataType == material.DataType && x.DataId == material.DataId);
                    await _productionMaterialService.InsertAsync(new WF_Node_ProductionExt_MaterialRL
                    {
                        NodeId = node.Id, DataId = material.DataId, DataType = material.DataType, IOType = NodeIOType.输入,
                        Quantity = material.Quantity, UnitId = material.UnitId, UnitConversionId = material.UnitConversionId ?? 0,
                        UnitListInfo = unit.Json, SortNo = material.Order,
                        RefCraftOrderVersionId = sourceMaterial?.RefCraftOrderVersionId, LossRate = sourceMaterial?.LossRate ?? 0m,
                        Remark = sourceMaterial?.Remark
                    });
                    // 每个设备人员组合按派工量分摊，最后一条承接小数差，避免一台设备多人时重复领料。
                    var assigned = allocations.Where(x => x.DispatchQuantity > 0).ToList();
                    decimal allocatedQuantity = 0m, assignedQuantity = 0m, assignedBase = 0m;
                    for (var index = 0; index < assigned.Count; index++)
                    {
                        var allocation = assigned[index];
                        var last = index == assigned.Count - 1;
                        allocatedQuantity += allocation.DispatchQuantity;
                        var cumulative = last ? material.Quantity : Math.Round(material.Quantity * allocatedQuantity / postData.Quantity, 6, MidpointRounding.AwayFromZero);
                        var cumulativeBase = last ? baseQuantity : Math.Round(baseQuantity * allocatedQuantity / postData.Quantity, 6, MidpointRounding.AwayFromZero);
                        await _machineDemandService.InsertAsync(new Production_Machine_Material_Receive_Demand
                        {
                            MachineId = allocation.MachineId, NodeSettingMachine = allocation, MaterialReceiveDemand = demand,
                            DemandQuantity = cumulative - assignedQuantity, BaseDemandQuantity = cumulativeBase - assignedBase
                        });
                        assignedQuantity = cumulative;
                        assignedBase = cumulativeBase;
                    }
                }
                // 产出信息来自只读工艺模板，不作为领料需求。入库节点与对应工序共享本次产出快照。
                var outputNode = process.IsInbound || process.IsOutsource ? (WF_Node_ProductionExt)route[route.IndexOf(node) + 1] : node;
                foreach (var material in templateMaterials.Where(x => x.NodeSettingTemplateId == process.NodeSettingTemplateId
                    && x.IOType != NodeIOType.输入 && x.InsteadSourceId == null))
                {
                    var quantity = Math.Round(material.Quantity * postData.Quantity, 6, MidpointRounding.AwayFromZero);
                    CheckMaterialNumber(quantity, "工序产出数量");
                    await _productionMaterialService.InsertAsync(new WF_Node_ProductionExt_MaterialRL
                    {
                        NodeId = outputNode.Id, DataId = material.DataId, DataType = material.DataType, IOType = material.IOType,
                        Quantity = quantity, UnitId = material.UnitId, UnitConversionId = material.UnitConversionId,
                        UnitListInfo = material.UnitListInfo, LossRate = material.LossRate, SortNo = material.SortNo,
                        RefCraftOrderVersionId = material.RefCraftOrderVersionId, Remark = material.Remark
                    });
                }
            }
            if (postData.ImmediateStart) _workflowService.StartWorkflow(workflow);
            await _unitOfWork.SaveChangeAsync();
            scope.Complete();
            return Ok(new { order.Id, order.OrderNo, order.WorkflowId });
        }

        [HttpPost("StartSchedule")]
        public async Task<IActionResult> StartSchedule([FromBody] ScheduleIdDto postData)
        {
            using var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
            var order = await _dispatchOrderService.GetAll().Include(x => x.Workflow!).ThenInclude(x => x.Nodes)
                .ThenInclude(x => x.NodeType).FirstOrDefaultAsync(x => x.Id == postData.Id && !x.IsDeleted)
                ?? throw new Exception("排产单不存在");
            var workflow = order.Workflow ?? throw new Exception("排产单未关联工作流");
            if (workflow.State != WorkflowState.新建) throw new Exception("仅允许启动未开工排产单");
            // 加载连线与目标节点，StartWorkflow 不依赖宿主是否启用延迟加载。
            await _productionNodeService.GetAll().Where(x => x.DispatchOrderId == order.Id)
                .Include(x => x.PreviousLines).Include(x => x.NextLines).ToListAsync();
            _workflowService.StartWorkflow(workflow);
            await _unitOfWork.SaveChangeAsync();
            scope.Complete();
            return Ok("排产已开始");
        }

        [HttpPost("DeleteSchedule")]
        public async Task<IActionResult> DeleteSchedule([FromBody] ScheduleIdDto postData)
        {
            using var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
            var order = await _dispatchOrderService.GetAll().Include(x => x.Workflow)
                .FirstOrDefaultAsync(x => x.Id == postData.Id && !x.IsDeleted) ?? throw new Exception("排产单不存在或已删除");
            if (order.Workflow?.State != WorkflowState.新建 || order.CompleteQuantity != 0 || order.TotalInboundQuantity != 0)
                throw new Exception("仅允许删除未启动且没有执行记录的排产单");
            var nodes = await _productionNodeService.GetAll().Include(x => x.Operator).Where(x => x.DispatchOrderId == order.Id).ToListAsync();
            var nodeIds = nodes.Select(x => x.Id).ToList();
            var resources = await _productionMachineService.GetAll().Where(x => nodeIds.Contains(x.NodeId)).ToListAsync();
            var demands = await _receiveDemandService.GetAll().Where(x => x.DispatchOrderId == order.Id).ToListAsync();
            var demandIds = demands.Select(x => x.Id).ToList();
            var machineDemands = await _machineDemandService.GetAll().Where(x => demandIds.Contains(x.MaterialReceiveDemandId)).ToListAsync();
            if (nodes.Any(x => x.State != NodeState.未处理 || x.Operator.Any() || x.ReciveQuantity != 0 || x.CompleteQuantity != 0 || x.ActualWorkHours != 0)
                || resources.Any(x => x.ReciveQuantity != 0 || x.CompleteQuantity != 0 || x.ActualWorkHours != 0)
                || demands.Any(x => x.BaseReceiveQuantity != 0 || x.BaseReturnQuantity != 0)
                || machineDemands.Any(x => x.BaseReceiveQuantity != 0 || x.BaseReturnQuantity != 0))
                throw new Exception("排产单已有工序执行或领退料记录，不能删除");
            if (await _dispatchOrderService.GetAll().AnyAsync(x => !x.IsDeleted && (x.ParentDispatchOrderId == order.Id || x.RootDispatchOrderId == order.Id)))
                throw new Exception("排产单已有下级派工，请先处理关联单据");
            order.IsDeleted = true;
            foreach (var node in nodes) node.IsDeleted = true;
            foreach (var demand in demands) demand.IsDeleted = true;
            foreach (var demand in machineDemands) demand.IsDeleted = true;
            await _unitOfWork.SaveChangeAsync();
            scope.Complete();
            return Ok("排产单已删除");
        }

        /// <summary>按编辑区的实际选料查询库存，包含模板之外新增、替换的物料。</summary>
        [HttpPost("GetMaterialStock")]
        public async Task<IActionResult> GetMaterialStock([FromBody] List<ScheduleMaterialKeyDto> materials)
        {
            if (materials == null || materials.Count > 2000) throw new Exception("物料查询参数无效");
            var graph = new CraftMaterialData();
            graph.Materials.Add(0, materials.DistinctBy(x => (x.DataType, x.DataId))
                .Select(x => new WF_NodeTemplate_Material_RL { DataId = x.DataId, DataType = x.DataType }).ToList());
            var infos = await GetMaterialInfoAsync(graph);
            var stock = await GetInventoryQuantityAsync(infos.Keys);
            return Ok(infos.Select(item => new
            {
                item.Value.DataType, item.Value.DataId, item.Value.Code, item.Value.Name, item.Value.UnitId, item.Value.Unit,
                item.Value.SafetyStock, Stock = stock.GetValueOrDefault(item.Key)?.Stock ?? 0,
                Occupied = stock.GetValueOrDefault(item.Key)?.Occupied ?? 0
            }).ToList());
        }

        private static void CheckScheduleNumber(decimal value, string label, bool positive = false)
        {
            if (value < 0 || (positive && value == 0) || value > 99999999.9999m || decimal.Round(value, 4) != value)
                throw new Exception($"{label}超出有效范围，最多4位小数");
        }

        private static void CheckMaterialNumber(decimal value, string label)
        {
            if (value < 0 || value > 999999999999.999999m || decimal.Round(value, 6) != value)
                throw new Exception($"{label}超出有效范围，最多6位小数");
        }

        private static (decimal Ratio, string Json) GetScheduleMaterialUnit(ProcessMaterialDto material, MaterialInfo info,
            IEnumerable<WF_NodeTemplate_Material_RL> sources, Dictionary<long, Base_UnitConversion> conversions)
        {
            // 客户端快照仅用于显示；保存时从原模板或当前单位定义重新取换算，不能信任提交的比例。
            if (material.UnitConversionId > 0 && !conversions.ContainsKey(material.UnitConversionId.Value))
                throw new Exception($"物料【{info.Name}】关联单位换算已删除，请重新选择单位");
            var units = sources.SelectMany(x => JsonSerializer.Deserialize<List<InventoryUnitInboundDto>>(
                string.IsNullOrWhiteSpace(x.UnitListInfo) ? "[]" : x.UnitListInfo, JsonHelper.DefaultJsonSerializerOptions) ?? new()).ToList();
            var matches = units.Where(x => x.Id == (material.UnitConversionId ?? 0) && x.UnitId == material.UnitId).ToList();
            if (matches.Select(x => x.Ratio).Distinct().Count() > 1) throw new Exception($"物料【{info.Name}】单位快照存在冲突");
            var unitName = matches.FirstOrDefault()?.UnitName ?? (material.UnitId == info.OutputUnitId ? info.OutputUnit : info.Unit);
            decimal ratio;
            if (material.UnitId == info.UnitId && (material.UnitConversionId ?? 0) == 0) ratio = 1m;
            else if (matches.Count > 0 && matches.All(x => x.Ratio == matches[0].Ratio)) ratio = matches[0].Ratio;
            else if (material.UnitId == info.OutputUnitId && (material.UnitConversionId ?? 0) == info.OutputConversionId) ratio = info.OutputRatio;
            else
            {
                var conversion = conversions.GetValueOrDefault(material.UnitConversionId ?? 0);
                if (conversion == null || conversion.SourceId != material.DataId || conversion.FromUnitId != material.UnitId)
                    throw new Exception($"物料【{info.Name}】缺少与主单位匹配的换算关系");
                if (material.DataType == InventoryDataType.Product && conversion.ConversionType == UnitConversionType.Product
                    && conversion.TargetId == info.DataId && conversion.ToUnitId == info.UnitId)
                    ratio = conversion.Ratio;
                else if (material.DataType == InventoryDataType.Specification && conversion.ConversionType == UnitConversionType.ProductSpec
                    && conversion.TargetId == info.DataId && conversion.ToUnitId == info.OutputUnitId)
                    ratio = conversion.Ratio * info.OutputRatio;
                else if (material.DataType == InventoryDataType.Specification && conversion.ConversionType == UnitConversionType.SpecToProduct
                    && conversion.TargetId == info.ProductId && conversion.ToUnitId == info.UnitId)
                    ratio = conversion.Ratio;
                else throw new Exception($"物料【{info.Name}】转换关系不属于当前产品或规格");
                unitName = conversion.FromUnit.Name;
            }
            if (ratio <= 0) throw new Exception($"物料【{info.Name}】单位换算必须大于0");
            var selected = new InventoryUnitInboundDto { Id = material.UnitConversionId ?? 0, UnitId = material.UnitId,
                UnitName = unitName,
                Ratio = ratio, Quantity = material.Quantity };
            var snapshot = new List<InventoryUnitInboundDto> { selected };
            if (selected.Id != 0 || selected.UnitId != info.UnitId)
                snapshot.Add(new InventoryUnitInboundDto { Id = 0, UnitId = info.UnitId, UnitName = info.Unit, Ratio = 1m,
                    Quantity = Math.Round(material.Quantity * ratio, 6, MidpointRounding.AwayFromZero) });
            return (ratio, JsonSerializer.Serialize(snapshot, JsonHelper.DefaultJsonSerializerOptions));
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
                await FillScheduleQuantitiesAsync(nodes);
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
                    InProduction = first.InProduction,
                    ScheduledTotal = first.ScheduledTotal,
                    OccurrenceCount = group.Count(),
                };
                ApplyMaterialInfo(first, total);
                return total;
            }).ToList();
        }

        #endregion

        #region 数据查询

        private async Task FillScheduleQuantitiesAsync(List<ScheduleMaterialNodeDto> nodes)
        {
            var rows = GetAllMaterialNodes(nodes).ToList();
            var products = rows.Where(x => x.DataType == InventoryDataType.Product).Select(x => x.DataId).Distinct().ToList();
            var specifications = rows.Where(x => x.DataType == InventoryDataType.Specification).Select(x => x.DataId).Distinct().ToList();
            var orders = await _dispatchOrderService.GetAll().AsNoTracking()
                .Where(x => !x.IsDeleted && x.Status != DispatchOrderStatus.已完成 && x.Workflow != null
                    && ((!x.SpecificationId.HasValue && products.Contains(x.ProductId))
                        || (x.SpecificationId.HasValue && specifications.Contains(x.SpecificationId.Value))))
                .Select(x => new { x.ProductId, x.SpecificationId, x.MainQuantity, x.MainCompleteQuantity, x.Workflow!.State }).ToListAsync();
            var totals = orders.GroupBy(x => (DataType: x.SpecificationId.HasValue ? InventoryDataType.Specification : InventoryDataType.Product,
                DataId: x.SpecificationId ?? x.ProductId)).ToDictionary(x => x.Key, x => new
                {
                    Scheduled = x.Where(y => y.State == WorkflowState.新建).Sum(y => Math.Max(0m, y.MainQuantity - y.MainCompleteQuantity)),
                    Working = x.Where(y => y.State == WorkflowState.进行中).Sum(y => Math.Max(0m, y.MainQuantity - y.MainCompleteQuantity))
                });
            foreach (var row in rows)
            {
                var value = totals.GetValueOrDefault((row.DataType, row.DataId));
                row.ScheduledTotal = value?.Scheduled ?? 0;
                row.InProduction = value?.Working ?? 0;
                if (row.Kind == "semi" && row.Net > 0 && row.ScheduledTotal + row.InProduction > 0) row.Status = "部分排产";
                // 在途量暂不计算；已有排产尚未绑定具体需求，不能直接扣减本分支缺量。
            }
        }

        private sealed class CraftMaterialData
        {
            public Dictionary<long, Technical_CraftOrderVersion> Versions { get; } = new();
            public Dictionary<long, Technical_CraftOrder> Orders { get; } = new();
            public Dictionary<long, List<WF_NodeTemplate_Material_RL>> Materials { get; } = new();
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
            var products = await _productService.GetAll().AsNoTracking().Where(x => productIds.Contains(x.Id) && !x.IsDeleted)
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
                .Where(x => specificationIds.Contains(x.Id) && !x.IsDeleted && !x.Product.IsDeleted)
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

        private static MaterialTotal CreateMaterialTotal(List<WF_NodeTemplate_Material_RL> rows,
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
            WF_NodeTemplate_Material_RL material)
        {
            return (material.DataType, material.DataId,
                material.RefCraftOrderVersionId > 0 ? material.RefCraftOrderVersionId : null);
        }

        private static decimal GetMaterialBaseQuantity(WF_NodeTemplate_Material_RL row, MaterialInfo info)
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
