using Microsoft.AspNetCore.Mvc;
using KingZon.Core.Web;
using KingZon.OLCM.Common.DataBase.Entities;
using Microsoft.EntityFrameworkCore;
using KingZon.OLCM.Common.DataBase.Enum;
using KingZon.Core.DataBase;
using X.PagedList;
using KingZon.Mes.Workflow.Entity;
using KingZon.Mes.Workflow.Service.Interface;
using Kingzon.OLCM.Common.Services;
using System.Transactions;
using System.Text.Json;
using KingZon.OLCM.Common.Service.Dto;
using KingZon.OLCM.Technology.Service;
using KingZon.OLCM.Technology.Service.Dto;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Linq;
using System.Security.Cryptography;
using KingZon.Core;
using KingZon.OLCM.Amoeba.DataBase.Entities;
using Microsoft.AspNetCore.Authorization;
using KingZon.OLCM.Inventory.Service;

namespace KingZon.OLCM.Technology.WebApi.Controllers
{
    [Route("api/technology/[controller]")]
    [ApiController]
    public class ProcessOrderController : KingZonControllerBase
    {
        private readonly CommonService<Product> _productService;
        private readonly CommonService<Customer_Product_RL> _customerProductRLService;
        private readonly CommonService<Craft> _craftService;
        private readonly CommonService<Machine> _machineService;
        private readonly CommonService<SystemPara> _systemParaService;
        private readonly CommonService<RawMaterial> _rawMaterialService;
        private readonly CommonService<Mould> _mouldService;
        private readonly CommonService<MouldPart> _mouldPartService;
        private readonly CommonService<Mould_MouldPart_RL> _mouldMouldPartService;
        private readonly CommonService<Fixture> _fixtureService;
        private readonly CommonService<WorkflowTemplate> _commonWorkflowTemplateService;
        private readonly CommonService<NodeTemplate> _commonNodeTemplateService;
        private readonly CommonService<NodeTemplateSetting> _nodeTemplateSettingService;
        private readonly CommonService<NodeTemplateSetting_Material_RL> _nodeTemplateSetting_Material_RLService;
        private readonly CommonService<Customer> _customerService;
        private readonly CommonService<ProductionPlan> _productionPlanService;
        private readonly CommonService<LineTemplate> _lineTemplateService;
        private readonly CommonService<Machine_Craft_RL> _machine_Craft_RLService;
        private readonly CommonService<NodeTemplateSetting_WorkFixture_RL> _nodeTemplate_WorkFixture_RLService;
        private readonly CommonService<NodeTemplateSetting_Machine_RL> _nodeTemplateSetting_Machine_RLService;
        private readonly CommonService<ProductionPlan_Material_RL> _productionPlan_RawMaterial_RLService;
        private readonly CommonService<NodeSetting> _nodeSettingService;
        private readonly INodeTypeService _nodeTypeService;
        private readonly SequenceService _sequenceService;
        private readonly INodeTemplateService _nodetemplateService;
        private readonly IWorkflowTemplateService _workflowTemplateService;
        private readonly ProcessOrderService _processOrderService;
        private readonly CommonService<SemiProduct> _semiProductService;
        private readonly CommonService<Node_Market_Price> _node_Market_PriceService;
        private readonly CommonService<ProductNodeCost> _productNodeCostService;
        private readonly CommonService<ProcessOrder_Price> _processOrder_PriceService;
        private readonly CommonService<WorkFixtureCategory> _workFixtureCategoryService;
        private readonly InventoryManageService _inventoryManageService;
        private readonly IUnitOfWork _unitWork;


        public ProcessOrderController(
            CommonService<Product> productService,
            IUnitOfWork unitWork,
            CommonService<Customer_Product_RL> customerProductRLService,
            CommonService<Craft> craftService,
            CommonService<Machine> machineService,
            CommonService<SystemPara> systemParaService,
            CommonService<RawMaterial> rawMaterialService,
            CommonService<Mould> mouldService,
            CommonService<MouldPart> mouldPartService,
            CommonService<Mould_MouldPart_RL> mouldMouldPartService,
            CommonService<Fixture> fixtureService,
            CommonService<WorkflowTemplate> commonWorkflowTemplateService,
            CommonService<NodeTemplate> commonNodeTemplateService,
            CommonService<NodeTemplateSetting> nodeTemplateSettingService,
            CommonService<NodeTemplateSetting_Material_RL> nodeTemplateSetting_Material_RLService,
            CommonService<Customer> customerService,
            CommonService<ProductionPlan> productionPlanService,
            CommonService<LineTemplate> lineTemplateService,
            CommonService<Machine_Craft_RL> machine_Craft_RLService,
            CommonService<NodeTemplateSetting_WorkFixture_RL> nodeTemplate_WorkFixture_RLService,
            CommonService<NodeTemplateSetting_Machine_RL> nodeTemplateSetting_Machine_RLService,
            CommonService<ProductionPlan_Material_RL> productionPlan_RawMaterial_RLService,
            CommonService<NodeSetting> nodeSettingService,
            INodeTypeService nodeTypeService,
            SequenceService sequenceService,
            INodeTemplateService nodetemplateService,
            IWorkflowTemplateService workflowTemplateService,
            ProcessOrderService processOrderService,
            CommonService<SemiProduct> semiProductService,
            CommonService<Node_Market_Price> node_Market_PriceService,
            CommonService<ProductNodeCost> productNodeCostService,
            CommonService<ProcessOrder_Price> processOrder_PriceService,
            CommonService<WorkFixtureCategory> workFixtureCategoryService,
            InventoryManageService inventoryManageService
            )
        {
            _productService = productService;
            _unitWork = unitWork;
            _customerProductRLService = customerProductRLService;
            _craftService = craftService;
            _machineService = machineService;
            _systemParaService = systemParaService;
            _rawMaterialService = rawMaterialService;
            _mouldService = mouldService;
            _mouldPartService = mouldPartService;
            _mouldMouldPartService = mouldMouldPartService;
            _fixtureService = fixtureService;
            _processOrderService = processOrderService;
            _semiProductService = semiProductService;
            _node_Market_PriceService = node_Market_PriceService;
            _productNodeCostService = productNodeCostService;
            _processOrder_PriceService = processOrder_PriceService;
            _workFixtureCategoryService = workFixtureCategoryService;
            _inventoryManageService = inventoryManageService;
            _commonWorkflowTemplateService = commonWorkflowTemplateService;
            _commonNodeTemplateService = commonNodeTemplateService;
            _nodeTemplateSettingService = nodeTemplateSettingService;
            _nodeTemplateSetting_Material_RLService = nodeTemplateSetting_Material_RLService;
            _customerService = customerService;
            _productionPlanService = productionPlanService;
            _lineTemplateService = lineTemplateService;
            _machine_Craft_RLService = machine_Craft_RLService;
            _nodeTemplate_WorkFixture_RLService = nodeTemplate_WorkFixture_RLService;
            _nodeTemplateSetting_Machine_RLService = nodeTemplateSetting_Machine_RLService;
            _productionPlan_RawMaterial_RLService = productionPlan_RawMaterial_RLService;
            _nodeSettingService = nodeSettingService;
            _nodeTypeService = nodeTypeService;
            _sequenceService = sequenceService;
            _nodetemplateService = nodetemplateService;
            _workflowTemplateService = workflowTemplateService;
        }

        public class ProcessOrderFilterDto
        {
            public List<Filter>? ProcessOrderFilterList { get; set; }
            public List<Filter>? ProductFilterList { get; set; }
            public List<Filter>? CustomerFilterList { get; set; }
        }



        /// <summary>
        /// 获取所有工艺单
        /// </summary>
        /// <param name="filterList"></param>
        /// <returns></returns>
        [HttpPost("GetProcessOrderList", Name = "GetProcessOrderList")]
        public async Task<IActionResult> GetProcessOrderList([FromBody] ProcessOrderFilterDto filterList)
        {
            var orders = this.GetProcessOrderData(filterList);
            if (this.SortInfoList.Count > 0)
            {
                orders = orders.ApplySorts(this.SortInfoList);
            }
            else
            {
                orders = orders.OrderBy(o => o.OrderNo);
            }
            var data = await orders.ToPagedListAsync(this.PageModel);
            return Ok(data);
        }

        private IQueryable<ProcessOrderDto> GetProcessOrderData(ProcessOrderFilterDto filterList)
        {
            //过滤
            var processOrderFilters = filterList?.ProcessOrderFilterList ?? new List<Filter>();
            var customerFilters = filterList?.CustomerFilterList ?? new List<Filter>();
            var productFilters = filterList?.ProductFilterList ?? new List<Filter>();

            var products = _productService.GetAll(true)
                 .ApplyFilters(productFilters).ApplySorts(this.SortInfoList);
            if (customerFilters.Count > 0)
            {
                var customers = _customerProductRLService.GetAll(true).ApplyFilters(customerFilters);
                products = products.Join(customers, o => o.Id, e => e.ProductId, (o, e) => o).Distinct();
            }

            //查询所有工艺单
            var orders = (from order in _processOrderService.GetAll(true)
                          join product in products
                          on order.ProductId equals product.Id
                          select new ProcessOrderDto(order.OrderNo, product.ProductNo, product.ProductName, product.Unit)
                          {
                              OrderNo = order.OrderNo,
                              ProductNo = product.ProductNo,
                              ProductName = product.ProductName,
                              Unit = product.Unit,
                              OrderId = order.Id,
                              Id = product.Id,
                              Version = product.Version,
                              ProductCategory = product.ProductCategory,
                              Specification = product.Specification,
                              MaterialGrade = product.MaterialGrade,
                              Remark = order.Remark,
                              CustomAttribute = product.CustomAttribute,
                              IsOrderDeleted = order.IsDeleted,
                              IsDeleted = product.IsDeleted,
                              SerialNo = product.SerialNo,
                              Weight = product.Weight,
                              Price = product.Price,
                              WorkFlowTemplateId = order.WorkflowTemplateId,
                              CustomerMaterialNo = product.CustomerMaterialNo,
                              MaterialSpecification = product.MaterialSpecification,
                              CustomerList = product.Customer_Product_RL.Select(e => e.Customer).ToList(),
                              IsModify = true,
                              //IsModify = order.productionPlans.Where(o=>!o.IsDeleted).Count() == 0 ? true : false,
                              CreateTime = order.CreateTime,
                              CreateUserName = order.CreateUserName,
                              RecentModifyUserName = order.RecentModifyUser != null ? order.RecentModifyUser.Name : string.Empty,
                              RecentModifyTime = order.RecentModifyTime,
                          }).ApplyFilters(processOrderFilters);

            return orders;
        }

        /// <summary>
        /// 获取不分页的工艺单
        /// </summary>
        /// <param name="filterList"></param>
        /// <returns></returns>
        [HttpPost("GetProcessOrderUnpagedList", Name = "GetProcessOrderUnpagedList")]
        public async Task<IActionResult> GetProcessOrderUnpagedList([FromBody] ProcessOrderFilterDto filterList)
        {
            var orders = this.GetProcessOrderData(filterList);
            if (this.SortInfoList.Count > 0)
            {
                orders = orders.ApplySorts(this.SortInfoList);
            }
            else
            {
                orders = orders.OrderBy(o => o.OrderNo);
            }
            var data = await orders.ToListAsync();
            return Ok(data);
        }

        /// <summary>
        /// 新增工艺单
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        [HttpPost("InsertProcessOrder", Name = "InsertProcessOrder")]
        public async Task<IActionResult> InsertProcessOrder([FromBody] PostProcessOrderDto data)
        {
            using (var transaction = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                var tableName = $"{TableName.ProcessOrder}-{data.ProductInfo.ProductNo}";
                var sequence = _sequenceService.GetSequenceOrInsertForUse(tableName).ToString("00");
                var orderNo = $"{data.ProductInfo.ProductNo}-{sequence}";
                var msg = data.Sequence == sequence ? "添加成功" : $"添加成功，原流水号已使用，工艺单号变更为{orderNo}";
                var nodeTemplateSetting = data.NodeTemplateList;
                var nodeData = new List<NodeTemplate>();

                //新增工作流模板
                var workflowTemplate = new WorkflowTemplate
                {
                    Name = orderNo,
                    WorkflowTypeId = (int)CustomWorkflowType.生产流程,
                };
                _commonWorkflowTemplateService.Insert(workflowTemplate);

                //新增工艺单
                var cost = data.NodeTemplateList.Where(s => s.RecevieMaterialInfo != null && s.RecevieMaterialInfo.Count > 0 )
                                                .Sum(s => s.RecevieMaterialInfo!.Where(s => s.Quantity > 0 && s.MaterialType == MaterialType.原材料).Count() > 0 ? 
                                                                (s.RecevieMaterialInfo!.Where(s => s.Quantity > 0 && s.MaterialType == MaterialType.原材料).Sum(x => x.Quantity * x.Price) 
                                                                    / s.RecevieMaterialInfo!.Where(s => s.Quantity > 0 && s.MaterialType == MaterialType.原材料).Count()) : 
                                                                        0);
                
                var processOrder = new ProcessOrder(orderNo)
                {
                    OrderNo = orderNo,
                    ProductId = data.ProductInfo.Id,
                    Remark = data.Remark,
                    WorkflowTemplate = workflowTemplate,
                    CardStanderdQuantity = data.CardStanderdQuantity ?? 0,
                    MaterialCost = (cost ?? 0),
                    RecentModifyTime = DateTime.Now,
                    RecentModifyUserId = this.CurrentUser.Id,
                    CheckWeight = data.CheckWeight,
                    CheckWeightLimit = data.CheckWeightLimit / 100
                };
                _processOrderService.Insert(processOrder);

                await _unitWork.SaveChangeAsync();

                //创建生产流程的开始、结束节点
                var startNode = new NodeTemplate()
                {
                    Name = "开始",
                    WorkflowTemplate = workflowTemplate,
                    NodeTypeId = (int)CustomNodeType.开始,
                    Type = CustomNodeType.开始.ToString()
                };
                var endNode = new NodeTemplate()
                {
                    Name = "结束",
                    WorkflowTemplate = workflowTemplate,
                    NodeTypeId = (int)CustomNodeType.结束,
                    Type = CustomNodeType.结束.ToString()
                };
                _commonNodeTemplateService.Insert(startNode);
                _commonNodeTemplateService.Insert(endNode);


                //创建普通节点
                var nodeList = new List<NodeTemplateSetting>();
                NodeTemplateSetting? preNode = null;
                foreach (var item in nodeTemplateSetting)
                {
                    var list = _processOrderService.CreateNodeTemplateSetting(item, workflowTemplate, processOrder, data.ProductInfo.ProductNo, data.ProductInfo.ProductName, sequence, preNode);
                    nodeList = nodeList.Concat(list).ToList();
                    preNode = list.FirstOrDefault();
                }
                //_nodeTemplateSettingService.Insert(nodeList);

                //创建节点关联关系
                nodeData.Add(startNode);
                var nodeTemplateData = nodeList.Select(o => o.NodeTemplate).ToList();
                nodeData = nodeData.Concat(nodeTemplateData).ToList();
                nodeData.Add(endNode);

                _processOrderService.CreateLineTemplates(nodeData, workflowTemplate.Id);
                await _unitWork.SaveChangeAsync();
                var returnData = new ReturnProcessOrderDataDto()
                {
                    ProcessOrderId = processOrder.Id,
                    Msg = msg,
                    NodeTemplateSettingIds = nodeList.Where(o => o.NodeType != CustomNodeType.入库).Select(o => o.Id).ToList()
                };
                transaction.Complete();
                return Ok(returnData);
            }
        }

        /// <summary>
        /// 删除工艺单
        /// </summary>
        /// <param name="orderId"></param>
        /// <returns></returns>
        [HttpGet("DeleteProcessOrder", Name = "DeleteProcessOrder")]
        public async Task<IActionResult> DeleteProcessOrder([FromQuery] int orderId)
        {
            using (var transaction = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                var order = await _processOrderService.GetByIdAsync(orderId);
                if (order == null)
                {
                    throw new Exception("删除失败，请刷新数据");
                }
                var planOrders = _productionPlanService.GetAll(true).Where(o => o.ProcessOrderId == orderId);
                if (planOrders.Any())
                {
                    throw new Exception("该工艺单已用于生产，无法删除");
                }
                //查询对应的所有工艺单模板、节点模板、节点模板设置、节点模板设置材料关联
                var workflowTemplate = await _commonWorkflowTemplateService.GetByIdAsync(order.WorkflowTemplateId);
                var nodeSetting = await _nodeTemplateSettingService.GetAll(true).Where(o => o.ProcessOrderId == orderId).ToListAsync();
                var lineTempList = await _lineTemplateService.GetAll(true).Where(o => o.WorkflowTemplateId == order.WorkflowTemplateId).ToListAsync();
                var semiProduct = await _semiProductService.GetAll(true).Where(o => o.ProcessOrderId == orderId).ToArrayAsync();
                var nodeSettingMaterial = await _nodeTemplateSetting_Material_RLService.GetAll().Where(o => nodeSetting.Select(e => e.Id).Contains(o.NodeTemplateSettingId)).ToListAsync();
                var nodePrice = await _node_Market_PriceService.GetAll(true).Where(o => nodeSetting.Select(e => e.Id).ToList().Contains(o.NodeTemplateSettingId??0)).ToListAsync();
                var marketPrice = await _productNodeCostService.GetAll(true).Where(o => nodeSetting.Select(e => e.Id).ToList().Contains(o.NodeTemplateSettingId ?? 0)).ToListAsync();
                var processOrderRL = await _processOrder_PriceService.GetAll(true).Where(o => o.ProcessOrderId == orderId).ToListAsync();
                _productNodeCostService.Delete(marketPrice);
                _node_Market_PriceService.Delete(nodePrice);
                _processOrder_PriceService.Delete(processOrderRL);
                if (nodeSettingMaterial.Any())
                {
                    _nodeTemplateSetting_Material_RLService.Delete(nodeSettingMaterial);
                }
                if (lineTempList.Any())
                {
                    _lineTemplateService.Delete(lineTempList);
                }
                if (semiProduct.Any())
                {
                    _semiProductService.Delete(semiProduct);
                    await _unitWork.SaveChangeAsync();
                }
                if (nodeSetting.Any())
                {
                    _nodeTemplateSettingService.Delete(nodeSetting);
                }
                _processOrderService.Delete(order, false);
                if (workflowTemplate != null)
                {
                    _workflowTemplateService.Delete(workflowTemplate.Id);
                }
                await _unitWork.SaveChangeAsync();
                transaction.Complete();
                return Ok("删除成功");
            }
        }

        /// <summary>
        /// 修改工艺单状态
        /// </summary>
        /// <param name="orderId"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        [HttpGet("ModifyProcessOrderState", Name = "ModifyProcessOrderState")]
        public async Task<IActionResult> ModifyProcessOrderState([FromQuery] int orderId)
        {
            var order = await _processOrderService.GetByIdAsync(orderId);
            if (order == null)
            {
                throw new Exception("操作失败，请刷新数据");
            }
            order.IsDeleted = !order.IsDeleted;
            order.RecentModifyUserId = this.CurrentUser.Id;
            order.RecentModifyTime = DateTime.Now;
            await _unitWork.SaveChangeAsync();
            return Ok("保存成功");
        }

        /// <summary>
        /// 根据工艺单ID获取工艺单信息
        /// </summary>
        /// <param name="orderId"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        [HttpGet("GetProcessOrderByOrderId", Name = "GetProcessOrderByOrderId")]
        public async Task<IActionResult> GetProcessOrderByOrderId([FromQuery] int orderId)
        {
            var result = new PostProcessOrderDto();

            //查询工艺单
            var order = await _processOrderService.GetAll(true).FirstOrDefaultAsync(o => o.Id == orderId);
            if (order == null)
            {
                throw new Exception("操作失败，请刷新数据");
            }
            result.ProcessOrderId = orderId;
            result.Remark = order.Remark;
            result.CardStanderdQuantity = order.CardStanderdQuantity;
            result.CheckWeight = order.CheckWeight;
            result.CheckWeightLimit = order.CheckWeightLimit * 100;
            //查询产品信息
            result.ProductInfo = _productService.GetAll(true).Where(o => o.Id == order.ProductId)
                .Select(o => new ProductDto(o.ProductNo, o.ProductName, o.Unit)
                {
                    Id = o.Id,
                    ProductNo = o.ProductNo,
                    ProductName = o.ProductName,
                    Specification = o.Specification,
                    MaterialGrade = o.MaterialGrade,
                    ProductCategory = o.ProductCategory,
                    Version = o.Version,
                    Unit = o.Unit,
                    Remark = o.Remark,
                    CustomAttribute = o.CustomAttribute,
                    IsDeleted = o.IsDeleted,
                    SerialNo = o.SerialNo,
                    Weight = o.Weight,
                    Price = o.Price,
                    CustomerList = o.Customer_Product_RL.Select(e => e.Customer).ToList(),
                    CustomerMaterialNo = o.CustomerMaterialNo,
                    MaterialSpecification = o.MaterialSpecification
                }).First();


            //查询领料记录
            var receiveMaterialInfo = await (from rl in _nodeTemplateSetting_Material_RLService.GetAll()
                                             join raw in _rawMaterialService.GetAll()
                                             on new { id = rl.DataId, type = rl.DataType } equals new { id = raw.Id, type = MaterialType.原材料 }
                                             into rawTemp
                                             from raw in rawTemp.DefaultIfEmpty()
                                             join semi in _semiProductService.GetAll()
                                             on new { id = rl.DataId, type = rl.DataType } equals new { id = semi.Id, type = MaterialType.半成品 }
                                             into semiTemp
                                             from semi in semiTemp.DefaultIfEmpty()
                                             join product in _productService.GetAll(true)
                                             on new { id = rl.DataId, type = rl.DataType } equals new { id = product.Id, type = MaterialType.产品 }
                                             into productTemp
                                             from product in productTemp.DefaultIfEmpty()
                                             where rl.NodeTemplateSetting.ProcessOrderId == orderId
                                             group new { rl, raw, semi, product } by rl.NodeTemplateSettingId into g
                                             select new
                                             {
                                                 SetId = g.Key,
                                                 MaterialList = g.Select(o => new MaterialDto(string.Empty, string.Empty)
                                                 {
                                                     MaterialId = o.rl.DataId,
                                                     Unit = o.raw != null ? o.raw.Unit : "件",
                                                     Remark = o.raw != null ? o.raw.Remark :
                                                                o.semi != null ? o.semi.Remark :
                                                                    o.product != null ? o.product.Remark :
                                                                    string.Empty,
                                                     MaterialCategory = o.raw != null ? o.raw.RawMaterialCategory.Name :
                                                                o.semi != null ? "半成品" :
                                                                    o.product != null ? "产品" :
                                                                    string.Empty,
                                                     MaterialCategoryId = o.raw != null ? o.raw.RawMaterialCategoryId : 0,
                                                     MaterialType = o.rl.DataType,
                                                     Specification = o.raw != null ? o.raw.Specification :
                                                                o.semi != null ? o.semi.Product.Specification :
                                                                    o.product != null ? o.product.Specification :
                                                                    string.Empty,
                                                     MaterialGrade = o.raw != null ? o.raw.MaterialGrade :
                                                                o.semi != null ? o.semi.Product.MaterialGrade :
                                                                    o.product != null ? o.product.MaterialGrade :
                                                                    string.Empty,
                                                     MaterialCode = o.raw != null ? o.raw.Code :
                                                                o.semi != null ? o.semi.Code :
                                                                    o.product != null ? o.product.ProductNo :
                                                                    string.Empty,
                                                     Quantity = o.raw != null && o.raw.Unit != null && o.raw.Unit.ToLower() == "kg" ? o.rl.Quantity * 1000 : o.rl.Quantity,
                                                     Selected = true,
                                                     MaterialName = o.raw != null ? o.raw.Name :
                                                                o.semi != null ? o.semi.Name :
                                                                    o.product != null ? o.product.ProductName :
                                                                    string.Empty,
                                                     Version = o.semi != null ? o.semi.Product.Version :
                                                                    o.product != null ? o.product.Version :
                                                                    string.Empty,
                                                     CustomerMaterialNo = o.semi != null ? string.Join("\n", o.semi.Product.Customer_Product_RL.Select(o => o.CustomerMaterialNo)) :
                                                                    o.product != null ? string.Join("\n", o.product.Customer_Product_RL.Select(o => o.CustomerMaterialNo)) :
                                                                    string.Empty,
                                                     Price = o.semi != null ? o.semi.InventoryPrice :
                                                                    o.product != null ? o.product.InventoryPrice :
                                                                       o.raw != null ? o.raw.InventoryPrice : 0,
                                                 }).ToList()
                                             }).ToListAsync();

            //查询工模治具配置记录
            var toolRLs = await (from rl in _nodeTemplate_WorkFixture_RLService.GetAll()
                                 join fixture in _fixtureService.GetAll()
                                 on rl.DataId equals fixture.Id
                                 where rl.DataType != WorkFixtureType.总成模具 && rl.DataType != WorkFixtureType.模具零件
                                 select new ReceiveWorkFixtureGetDto()
                                 {
                                     SetId = rl.NodeTemplateSettingId,
                                     WorkFixture = new WorkFixtureDto(fixture.Code, fixture.Name)
                                     {
                                         WorkFixtureId = fixture.Id,
                                         Code = fixture.Code,
                                         Name = fixture.Name,
                                         UseType = fixture.UseType,
                                         Remark = fixture.Remark,
                                         WorkFixtureCategory = fixture.WorkFixtureCategory != null ? fixture.WorkFixtureCategory.Name : "",
                                         ProductName = fixture.Product != null ? fixture.Product.ProductName : null,
                                         WorkFixtureType = fixture.WorkFixtureCategory != null ? fixture.WorkFixtureCategory.Type : WorkFixtureType.夹具,
                                         IsDelete = false
                                     }
                                 }).ToListAsync();

            var mouldRLs = await (from rl in _nodeTemplate_WorkFixture_RLService.GetAll()
                                  join mould in _mouldService.GetAll()
                                  on rl.DataId equals mould.Id
                                  where rl.DataType == WorkFixtureType.总成模具
                                  select new ReceiveWorkFixtureGetDto()
                                  {
                                      SetId = rl.NodeTemplateSettingId,
                                      WorkFixture = new WorkFixtureDto(mould.Code, mould.Name)
                                      {
                                          WorkFixtureId = mould.Id,
                                          Code = mould.Code,
                                          Name = mould.Name,
                                          ProcessOrder = mould.ProcessOrder,
                                          UseType = mould.UseType,
                                          Remark = mould.Remark,
                                          WorkFixtureCategory = mould.WorkFixtureCategory != null ? mould.WorkFixtureCategory.Name : "",
                                          CraftName = mould.Craft != null ? mould.Craft.Name : "",
                                          ProductName = mould.Product != null ? mould.Product.ProductName : null,
                                          WorkFixtureType = WorkFixtureType.总成模具,
                                          IsDelete = false
                                      }
                                  }).ToListAsync();

            var mouldPartRLs = await (from rl in _nodeTemplate_WorkFixture_RLService.GetAll()
                                      join mould in _mouldPartService.GetAll()
                                      on rl.DataId equals mould.Id
                                      where rl.DataType == WorkFixtureType.模具零件
                                      select new ReceiveWorkFixtureGetDto()
                                      {
                                          SetId = rl.NodeTemplateSettingId,
                                          WorkFixture = new WorkFixtureDto(mould.Code, mould.Name)
                                          {
                                              WorkFixtureId = mould.Id,
                                              Code = mould.Code,
                                              Name = mould.Name,
                                              ProcessOrder = mould.ProcessOrder,
                                              UseType = mould.UseType,
                                              Remark = mould.Remark,
                                              WorkFixtureCategory = mould.WorkFixtureCategory != null ? mould.WorkFixtureCategory.Name : "",
                                              CraftName = mould.Craft != null ? mould.Craft.Name : "",
                                              ProductName = mould.Product != null ? mould.Product.ProductName : null,
                                              WorkFixtureType = WorkFixtureType.模具零件,
                                              IsDelete = false
                                          }
                                      }).ToListAsync();

            var workFixtures = toolRLs.Concat(mouldRLs).Concat(mouldPartRLs).ToList();

            var setWorkFixtures = from a in workFixtures
                                  group a by a.SetId
                                  into query
                                  select new
                                  {
                                      SetId = query.Key,
                                      WorkFixtureList = query.Select(o => o.WorkFixture).ToList()
                                  };

            //查询设备配置记录
            var machineInfo = await (from rl in _nodeTemplateSetting_Machine_RLService.GetAll().Include(o => o.Machine)
                                     group rl by rl.NodeTemplateSettingId into g
                                     select new
                                     {
                                         SetId = g.Key,
                                         MachineCategory = g.Select(o => o.MachineCategory).ToList(),
                                         MachineIds = g.Select(o => o.Machine).ToList()
                                     }).ToListAsync();


            //查询工艺单对应工艺模板
            var nodeData = _commonNodeTemplateService.GetAll().Where(o => o.WorkflowTemplateId == order.WorkflowTemplateId);
            var lineList = _lineTemplateService.GetAll().Where(o => o.WorkflowTemplateId == order.WorkflowTemplateId);
            var model = new NodeTemplateSortModel()
            {
                NodeTemplates = nodeData.ToList(),
                LineTemplates = lineList.ToList()
            };
            var nodeList = _processOrderService.GetNodeTemplateListAsSort(model);

            //查询工艺单对应工艺模板设置
            var settingList = from nodes in nodeList
                              join set in _nodeTemplateSettingService.GetAll().Include(o => o.Craft)
                                                                              .Include(o => o.ProcessOrder)
                                                                                    .ThenInclude(o => o.Product)
                                                                              .Include(o => o.InventorySemiProduct)
                                                                              .Include(o => o.OutSourceSemiProduct)
                                                                              .Include(x=>x.CraftExtension)
                              .Where(o => o.ProcessOrderId == orderId && o.NodeType != CustomNodeType.入库)
                              on nodes equals set.NodeTemplateId
                              join rl in receiveMaterialInfo
                              on set.Id equals rl.SetId into rls
                              from query in rls.DefaultIfEmpty()
                              join workFixture in setWorkFixtures
                              on set.Id equals workFixture.SetId into fixtures
                              from tool in fixtures.DefaultIfEmpty()
                              join machine in machineInfo
                              on set.Id equals machine.SetId into machines
                              from machine in machines.DefaultIfEmpty()
                              select new NodeTemplateInfo(set.NodeName)
                              {
                                  Id = set.Id,
                                  NodeSettingTempateId = set.Id,
                                  ProductCharacter = set.ProductCharacter != null ? JsonSerializer.Deserialize<List<CharacterDto>>(set.ProductCharacter!) : new List<CharacterDto>(),
                                  ControlCharacter = set.ControlCharacter != null ? JsonSerializer.Deserialize<List<CharacterDto>>(set.ControlCharacter!) : new List<CharacterDto>(),
                                  CraftManageParas = set.CraftManageParas != null ? JsonSerializer.Deserialize<List<CraftParaDto>>(set.CraftManageParas!) : new List<CraftParaDto>(),
                                  //MachineInfo = set.MachineInfo != null ? JsonSerializer.Deserialize<MachineInfo>(set.MachineInfo) : new MachineInfo(),
                                  WorkFixtureInfo = tool != null ? tool.WorkFixtureList : new List<WorkFixtureDto>(),
                                  CraftId = set.CraftId,
                                  CraftExtensionId = set.CraftExtensionId,
                                  CraftName = $"{set.Craft.Code}-{set.Craft.Name}",
                                  CraftExtensionName = set.CraftExtension!=null? set.CraftExtension!.Name : "",
                                  NodeName = set.NodeName,
                                  UnitWeight = set.UnitWeight,
                                  IsInventory = set.IsInventory,
                                  IsOutSource = set.IsOutSource,
                                  MachineInfo = machine != null ? new MachineInfo()
                                  {
                                      MachineCategory = machine.MachineCategory.Distinct().ToList(),
                                      MachineIds = machine.MachineIds.Where(o => o != null).Select(o => new SelectionDto
                                      {
                                          Value = o.Id.ToString(),
                                          Code = o.Category,
                                          Text = o.Name
                                      }).ToList()
                                  } : new MachineInfo(),
                                  //OutSourceMaterialInfo = set.OutSourceMaterialInfo != null ? JsonSerializer.Deserialize<List<MaterialDto>>(set.OutSourceMaterialInfo) : new List<MaterialDto>(),
                                  OutSourceRequirement = set.OutsourceRequirement,
                                  Remark = set.Remark,
                                  InventorySemiProduct = set.InventorySemiProduct != null ? new SemiProductInfo($"{set.InventorySemiProduct!.Product.ProductNo}-{set.Order.ToString("000")}")
                                  {
                                      Code = $"{set.InventorySemiProduct!.Product.ProductNo}-{set.Order.ToString("000")}",
                                      Remark = set.InventorySemiProduct!.Remark,
                                      ProductNo = set.ProcessOrder!.Product.ProductNo,
                                      ProductName = set.ProcessOrder!.Product.ProductName,
                                      Order = set.Order.ToString("000"),
                                      Name = set.InventorySemiProduct!.Name
                                  } : null,
                                  OutSourceSemiProduct = set.OutSourceSemiProduct != null ? new SemiProductInfo(set.OutSourceSemiProduct!.Code.Substring(0, set.OutSourceSemiProduct!.Code.Length - 3))
                                  {
                                      Code = set.OutSourceSemiProduct!.Code.Substring(0, set.OutSourceSemiProduct!.Code.Length - 3),
                                      Remark = set.OutSourceSemiProduct!.Remark,
                                      ProductNo = set.ProcessOrder!.Product.ProductNo,
                                      ProductName = set.ProcessOrder!.Product.ProductName,
                                      Order = set.Order.ToString("000"),
                                      Name = set.OutSourceSemiProduct!.Name
                                  } : null,
                                  RecevieMaterialInfo = query != null ? query.MaterialList : new List<MaterialDto>(),
                                  CustomNodeType = (CustomNodeType)set.NodeType,
                                  IsReceive = set.Craft.IsReceiveMaterial,
                                  Order = set.Order,
                                  IsInventorySemiProduct = set.InventorySemiProduct != null ? true : false,
                                  IsOutSourceSemiProduct = set.OutSourceSemiProduct != null ? true : false,
                              };
            result.NodeTemplateList = settingList.ToList();
            return Ok(result);
        }

        /// <summary>
        /// 编辑工艺单
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        [HttpPost("ModifyProcessOrder", Name = "ModifyProcessOrder")]
        public async Task<IActionResult> ModifyProcessOrder([FromBody] PostProcessOrderDto data)
        {
            using (var transaction = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                //查询原工艺单工序信息
                var order = await _processOrderService.GetByIdAsync(data.ProcessOrderId);
                if (order == null)
                {
                    throw new Exception("操作失败，请刷新数据");
                }
                //查询工艺单对应工艺模板(包含入库节点)
                var workflowTemplate = await _commonWorkflowTemplateService.GetByIdAsync(order.WorkflowTemplateId);
                if (workflowTemplate == null)
                {
                    throw new Exception("操作失败，请刷新数据");
                }
                var planOrders = _productionPlanService.GetAll().Where(o => o.ProcessOrderId == data.ProcessOrderId);
                //var newNode = data.NodeTemplateList.Where(o => o.NodeSettingTempateId <= 0).ToList();
                //if (planOrders.Where(o=>o.ProductionQuantity>0).Any() &&  newNode.Any())
                //{
                //    throw new Exception("该工艺单已用于生产，无法新增工序");
                //}
                var nodeData = _commonNodeTemplateService.GetAll().Where(o => o.WorkflowTemplateId == order.WorkflowTemplateId);
                var lineList = _lineTemplateService.GetAll().Where(o => o.WorkflowTemplateId == order.WorkflowTemplateId);
                var model = new NodeTemplateSortModel()
                {
                    NodeTemplates = nodeData.ToList(),
                    LineTemplates = lineList.ToList()
                };
                var workflows = _processOrderService.GetNodeTemplateListAsSort(model);

                var startNode = await _commonNodeTemplateService.GetAll().FirstOrDefaultAsync(o => o.WorkflowTemplateId == order.WorkflowTemplateId && o.NodeTypeId == (int)CustomNodeType.开始);
                var endNode = await _commonNodeTemplateService.GetAll().FirstOrDefaultAsync(o => o.WorkflowTemplateId == order.WorkflowTemplateId && o.NodeTypeId == (int)CustomNodeType.结束);
                order.Remark = data.Remark;
                order.CardStanderdQuantity = data.CardStanderdQuantity ?? 0;
                order.RecentModifyUserId = this.CurrentUser.Id;
                order.RecentModifyTime = DateTime.Now;
                order.CheckWeight = data.CheckWeight;
                order.CheckWeightLimit = data.CheckWeightLimit / 100;
                var cost = data.NodeTemplateList.Where(s => s.RecevieMaterialInfo != null && s.RecevieMaterialInfo.Count > 0)
                                 .Sum(s => s.RecevieMaterialInfo!.Where(s => s.Quantity > 0 && s.MaterialType == MaterialType.原材料).Count() > 0 ?
                                                (s.RecevieMaterialInfo!.Where(s => s.Quantity > 0 && s.MaterialType == MaterialType.原材料).Sum(x => x.Quantity * x.Price) 
                                                    / s.RecevieMaterialInfo!.Where(s => s.Quantity > 0 && s.MaterialType == MaterialType.原材料).Count()) : 
                                                        0);
                order.MaterialCost = cost ?? 0;
                //查询工艺单对应工艺模板设置
                var nodeInfo = (from nodes in workflows
                                join set in _nodeTemplateSettingService.GetAll()
                                                                            .Include(o => o.InventorySemiProduct)
                                                                            .Include(o => o.OutSourceSemiProduct)
                                                                       .Where(o => o.ProcessOrderId == data.ProcessOrderId)
                                on nodes equals set.NodeTemplateId
                                select new
                                {
                                    nodeTemplate = set.NodeTemplate,
                                    nodeSetting = set
                                }).ToList();
                var totalSetIds = nodeInfo.Select(x => x.nodeSetting.Id).ToList();
                var allSemiInfos = await _semiProductService.GetAll().Where(x => totalSetIds.Contains(x.NodeTemplateSettingId)).ToListAsync();
                var allSemiIds = allSemiInfos.Select(x => x.Id).ToList();
                var semiInventorys = await _inventoryManageService.GetAll().Where(x => x.DataType == StorageMaterialType.半成品 && allSemiIds.Contains(x.DataId)).ToListAsync();

                var listIds = nodeInfo.Select(o => o.nodeSetting.Id).ToList();
                var produtionNode = nodeInfo.Where(o => o.nodeSetting.NodeType != CustomNodeType.入库).ToList();
                //if (planOrders.Where(o => o.ProductionQuantity > 0).Any() && produtionNode.Count() > data.NodeTemplateList.Count)
                //{
                //    throw new Exception("该工艺单已用于生产，无法删除工序");
                //}
                //查询所有对应的领用物料并删除
                var sequence = order.OrderNo.Substring(order.OrderNo.Length - 2);
                var rawMaterilRLs = await _nodeTemplateSetting_Material_RLService.GetAll().Where(o => listIds.Contains(o.NodeTemplateSettingId)).ToListAsync();
                _nodeTemplateSetting_Material_RLService.Delete(rawMaterilRLs);

                //查询所有对应的工模治具并删除
                var workFixtureRLs = await _nodeTemplate_WorkFixture_RLService.GetAll().Where(o => listIds.Contains(o.NodeTemplateSettingId)).ToListAsync();
                _nodeTemplate_WorkFixture_RLService.Delete(workFixtureRLs);

                //查询所有对应的设备并删除
                var machineRLs = await _nodeTemplateSetting_Machine_RLService.GetAll().Where(o => listIds.Contains(o.NodeTemplateSettingId)).ToListAsync();
                _nodeTemplateSetting_Machine_RLService.Delete(machineRLs);

                //判断节点是否已生产
                var settingNodes = (from node in nodeInfo.Select(o => o.nodeSetting)
                                    join setting in _nodeSettingService.GetAll()
                                    on node.Id equals setting.NodeTemplateSettingId
                                    select node).ToList();
                var planNodes = (from node in nodeInfo.Select(o => o.nodeSetting)
                                 join plan in _productionPlan_RawMaterial_RLService.GetAll()
                                 on node.Id equals plan.NodeTemplateSettingId
                                 select node).ToList();
                var produtionNodeList = settingNodes.Union(planNodes).ToList();

                //生成新的工序序列,新增序列，删除序列
                var nodeList = new List<NodeTemplateSetting>();
                var delectSemiProducts = new List<SemiProduct>();
                NodeTemplateSetting? preNode = null;
                foreach (var item in data.NodeTemplateList)
                {
                    //查找修改的原工序
                    var originNode = nodeInfo.Where(o => o.nodeSetting.Id == item.NodeSettingTempateId).FirstOrDefault()?.nodeTemplate;
                    var originSet = nodeInfo.Where(o => o.nodeSetting.Id == item.NodeSettingTempateId).FirstOrDefault()?.nodeSetting;
                    var isProduction = produtionNodeList.Contains(originSet);
                    //新增工序
                    if (originSet == null)
                    {
                        var list = _processOrderService.CreateNodeTemplateSetting(item, workflowTemplate, order, data.ProductInfo.ProductNo, data.ProductInfo.ProductName, sequence, preNode);
                        nodeList = nodeList.Concat(list).ToList();
                        preNode = list.FirstOrDefault();
                        continue;
                    }
                    //获取当前工序半成品
                    var semiInfo = allSemiInfos.Where(o => o.NodeTemplateSettingId == originSet.Id && o.ProductId == order.ProductId && o.ProcessOrderId == order.Id).ToList();
                    //if (planOrders.Where(o => o.ProductionQuantity > 0).Any() && produtionNode[nodeIndex] != null
                    //    && (produtionNode[nodeIndex].nodeSetting.Id != item.NodeSettingTempateId
                    //    ||produtionNode[nodeIndex].nodeSetting.IsInventory != item.IsInventory
                    //    || produtionNode[nodeIndex].nodeSetting.IsOutSource != item.IsOutSource))
                    //{
                    //    throw new Exception("该工艺单已用于生产，无法修改工序信息");
                    //}
                    //修改旧节点信息
                    originSet.Order = item.Order;
                    originSet.NodeName = item.NodeName;
                    originSet.CraftId = item.CraftId;
                    originSet.ProductCharacter = item.ProductCharacter != null && item.ProductCharacter.Count > 0 ? JsonSerializer.Serialize(item.ProductCharacter) : null;
                    originSet.CraftExtensionId = item.CraftExtensionId;
                    originSet.ControlCharacter = item.ControlCharacter != null && item.ControlCharacter.Count > 0 ? JsonSerializer.Serialize(item.ControlCharacter) : null;
                    originSet.UnitWeight = (item.UnitWeight ?? 0);
                    originSet.CraftManageParas = item.CraftManageParas != null && item.CraftManageParas.Count > 0 ? JsonSerializer.Serialize(item.CraftManageParas, JsonHelper.DefaultJsonSerializerOptions) : null;
                    originSet.Remark = item.Remark;
                    originSet.OutsourceRequirement = item.OutSourceRequirement;
                    originSet.IsInventory = item.IsInventory;
                    originSet.IsOutSource = item.IsOutSource;
                    originSet.NodeType = item.IsOutSource ? CustomNodeType.外协 : CustomNodeType.生产;
                    //修改成本相关价格
                    originSet.MaterialCost = item.RecevieMaterialInfo != null && item.RecevieMaterialInfo.Any(x => x.Quantity > 0 && x.MaterialType == MaterialType.原材料) ?
                                                item.RecevieMaterialInfo.Where(x=>x.Quantity>0 &&  x.MaterialType == MaterialType.原材料).Sum(x => ((x.Unit != null && x.Unit.ToLower() == "kg") ? (x.Quantity / 1000) : x.Quantity) * (x.Price ?? 0)) / item.RecevieMaterialInfo.Where(x => x.Quantity > 0 && x.MaterialType == MaterialType.原材料).Count() : 0;

                    originSet.TheoreticalSemiProductCost = (preNode!=null?preNode.TheoreticalSemiProductCost:0) + originSet.TheoreticalMaterialCost + originSet.TheoreticalNodePrice;
                    originSet.SemiProductCost = (preNode!=null?preNode.SemiProductCost:0) + originSet.MaterialCost + originSet.NodePrice;

                    //修改NodeTemplate
                    if (originNode == null)
                    {
                        throw new Exception("请刷新数据");
                    }
                    originNode.NodeTypeId = item.IsOutSource ? (int)CustomNodeType.外协 : (int)CustomNodeType.生产;
                    originNode.Name = item.NodeName;
                    originNode.Type = item.IsOutSource ? CustomNodeType.外协.ToString() : CustomNodeType.生产.ToString();

                    //旧节点加入流程
                    nodeList.Add(originSet);

                    //修改关联关系
                    _processOrderService.CreateNodeSettingRL(item, originSet);

                    //是否入库:

                    //1、勾选入库或外协需生成入库工序，且生成入库半成品
                    if(item.IsInventory || item.IsOutSource)
                    {
                        //存在工序半成品，更新半成品相关信息
                        if (semiInfo.Any() && item.InventorySemiProduct!=null)
                        {
                            originSet.InventorySemiProduct = semiInfo.First();
                            originSet.InventorySemiProduct.Code = $"{data.ProductInfo.ProductNo}-{item.Order.ToString("000")}-{sequence}";
                            originSet.InventorySemiProduct.Name = $"{item.NodeName}-{data.ProductInfo.ProductName}";
                            originSet.InventorySemiProduct.InventoryPrice = originSet.TheoreticalSemiProductCost;
                            //恢复生产使用
                            originSet.InventorySemiProduct.Remark = string.Empty;
                            originSet.InventorySemiProduct.IsDeleteNode = false;
                        }
                        //不存在工序半成品，新增半成品
                        if (!semiInfo.Any() && item.InventorySemiProduct != null)
                        {
                            var entity = new SemiProduct(string.Empty,string.Empty)
                            {
                                Code = $"{data.ProductInfo.ProductNo}-{item.Order.ToString("000")}-{sequence}",
                                Name = $"{item.NodeName}-{data.ProductInfo.ProductName}",
                                NodeTemplateSettingId = originSet.Id,
                                ProcessOrderId = order.Id,
                                ProductId = order.ProductId,
                                InventoryPrice = originSet.TheoreticalSemiProductCost
                            };
                            _semiProductService.Insert(entity);
                            await _unitWork.SaveChangeAsync();
                            originSet.InventorySemiProduct = entity;
                        }

                        //如果需要外协，当前节点外协半成品始终是上一个节点的入库半成品
                        if (item.IsOutSource && preNode!=null)
                        {
                            originSet.OutSourceSemiProduct = preNode.InventorySemiProduct;
                        }

                        //获取入库工序
                        var index = nodeInfo.Select(x=>x.nodeTemplate.Id).ToList().IndexOf(originNode.Id);
                        //后工序为入库节点则为当前入库工序，直接修改入库工序相关信息
                        if (index >= 0 && (index+1)!= nodeInfo.Count() && nodeInfo[index + 1].nodeSetting.NodeType == CustomNodeType.入库)
                        {
                            var nextNodeSet = nodeInfo[index + 1].nodeSetting;
                            nextNodeSet.InventorySemiProduct = originSet.InventorySemiProduct;
                            nextNodeSet.OutSourceSemiProduct = originSet.OutSourceSemiProduct;
                            nextNodeSet.UnitWeight = originSet.UnitWeight;
                            nextNodeSet.CraftManageParas = originSet.CraftManageParas;
                            nextNodeSet.Remark = originSet.Remark;
                            nextNodeSet.OutsourceRequirement = originSet.OutsourceRequirement;
                            nextNodeSet.ControlCharacter = originSet.ControlCharacter;
                            nextNodeSet.ProductCharacter = originSet.ProductCharacter;
                            nextNodeSet.CraftExtensionId = originSet.CraftExtensionId;
                            nodeList.Add(nextNodeSet);
                        }
                        //新增入库工序
                        else
                        {
                            var entity = new NodeTemplateSetting("入库")
                            {
                                NodeTemplate = new NodeTemplate()
                                {
                                    Name = "入库",
                                    WorkflowTemplateId = order.WorkflowTemplateId,
                                    NodeTypeId = (int)CustomNodeType.入库,
                                    DataId = item.CraftId,
                                    Type = CustomNodeType.入库.ToString()
                                },
                                ProcessOrder = order,
                                CraftId = item.CraftId,
                                NodeType = CustomNodeType.入库,
                                Remark = item.Remark,
                                IsInventory = true,
                                IsOutSource = false,
                                NodeName = "入库",
                                InventorySemiProduct = originSet.InventorySemiProduct,
                                UnitWeight = originSet.UnitWeight ?? 0
                            };
                            _nodeTemplateSettingService.Insert(entity);
                            //新节点加入流程
                            nodeList.Add(entity);
                        }
                    }

                    //2、既不入库也不外协，没有入库工序，没有半成品
                    if((!item.IsInventory && !item.IsOutSource ) || item.InventorySemiProduct==null)
                    {
                        originSet.InventorySemiProduct = null;
                        //没有配置生产信息且没有库存记录，可删除
                        if (semiInfo.Any() && !isProduction)
                        {
                            var canDeleteSemis = semiInfo.Where(x => !semiInventorys.Any(s => s.DataId == x.Id)).ToList();
                            _semiProductService.Delete(canDeleteSemis);
                        }
                        if(semiInfo.Any() && isProduction)
                        {
                            foreach(var semi in semiInfo)
                            {
                                semi.Code = $"{data.ProductInfo.ProductNo}-{item.Order.ToString("000")}-{sequence}";
                                semi.Name = $"{item.NodeName}-{data.ProductInfo.ProductName}";
                                semi.Remark = $"当前工序{originSet.NodeName}不再自动生成半成品";
                            }
                        }
                        originSet.OutSourceSemiProduct = null;
                    }

                    preNode = originSet;
                }

                //删除Line
                var deleteLines = await _lineTemplateService.GetAll().Where(o => o.WorkflowTemplateId == order.WorkflowTemplateId).ToListAsync();
                _lineTemplateService.Delete(deleteLines);


                //求新旧工序差集得到删除序列
                var deleteNodes = nodeInfo.Select(o => o.nodeSetting).Except(nodeList).ToList();
                var produtionNodeIdList = produtionNodeList.Select(o => o.Id).ToList();
                //软删除
                var softNodes = deleteNodes.Where(o => produtionNodeIdList.Contains(o.Id)).ToList();
                //硬删除
                var unSoftNodes = deleteNodes.Except(softNodes).ToList();


                //获取直接删除节点的工序价
                var marketPriceInfos = await _node_Market_PriceService.GetAll().Where(s => unSoftNodes.Select(x => x.Id).Contains((s.NodeTemplateSettingId ?? 0))).ToListAsync();
                var nodePriceInfos = await _productNodeCostService.GetAll().Where(s => unSoftNodes.Select(x => x.Id).Contains((s.NodeTemplateSettingId ?? 0))).ToListAsync();

                var deleteSemiProducts = await _semiProductService.GetAll()
                    .Where(o => deleteNodes.Select(o => o.InventorySemiProductId)
                    .Contains(o.Id)).ToListAsync();

                foreach (var item in deleteSemiProducts)
                {
                    var node = softNodes.FirstOrDefault(o => o.Id == item.NodeTemplateSettingId);
                    item.IsDeleteNode = node != null ? true : false;
                    item.Code = item.Code + "_废";
                    item.Remark = node != null ? $"{item.Remark}工艺单变更：{node.Order.ToString("000")}-{node.NodeName}已作废" : item.Remark;
                }

                var unSoftSemiProducts = deleteSemiProducts.Where(o => unSoftNodes.Select(s => s.Id).ToList().Contains(o.NodeTemplateSettingId)).ToList();
                _semiProductService.Delete(unSoftSemiProducts);

                _nodeTemplateSettingService.Delete(softNodes);
                //删除工序价格
                _node_Market_PriceService.Delete(marketPriceInfos);
                _productNodeCostService.Delete(nodePriceInfos);
                await _unitWork.SaveChangeAsync();
                _nodeTemplateSettingService.Delete(unSoftNodes, false);
                _commonNodeTemplateService.Delete(unSoftNodes.Select(o => o.NodeTemplate).ToList());
                await _unitWork.SaveChangeAsync();

                


                //新增节点关联关系
                var sendNodes = new List<NodeTemplate>();
                if (startNode != null && endNode != null)
                {
                    sendNodes.Add(startNode);
                    var lineData = nodeList.Select(o => o.NodeTemplate).ToList();
                    sendNodes = sendNodes.Concat(lineData).ToList();
                    sendNodes.Add(endNode);
                    _processOrderService.CreateLineTemplates(sendNodes, order.WorkflowTemplateId);
                }
                var pickPrice = await _productNodeCostService.GetAll().Where(x=>x.ProductId == order.ProductId && !x.NodeTemplateSettingId.HasValue).FirstOrDefaultAsync();
                order.TheoreticalProductCost = order.MaterialCost + nodeList.Where(x=>x.NodeType!= CustomNodeType.入库).Sum(x=>x.NodePrice) + (pickPrice!=null?pickPrice.Price:0);

                await _unitWork.SaveChangeAsync();
                var returnData = new ReturnProcessOrderDataDto()
                {
                    ProcessOrderId = order.Id,
                    Msg = "保存成功",
                    NodeTemplateSettingIds = nodeList.Where(o => o.NodeType != CustomNodeType.入库).Select(o => o.Id).ToList()
                };
                transaction.Complete();
                return Ok(returnData);
            }
        }

        /// <summary>
        /// 获取对应工艺的加工设备类别
        /// </summary>
        /// <param name="craftId"></param>
        /// <returns></returns>
        [HttpGet("GetMachineCategoryListByCraft", Name = "GetMachineCategoryListByCraft")]
        public async Task<IActionResult> GetMachineCategoryListByCraft(int craftId)
        {
            var rls = await (from category in _machine_Craft_RLService.GetAll().Include(o => o.Machine)
                             where category.CraftId == craftId
                             group category by category.Machine.Category
                             into query
                             select new SelectionDto
                             {
                                 Value = query.Key,
                                 Text = query.Key
                             }).ToListAsync();
            return Ok(rls);
        }
        public class GetMachineDataDto
        {
            public List<string> CategoryList { get; set; } = new List<string>();
            public int CraftId { get; set; }
        }

        /// <summary>
        /// 获取指定类别的机器列表
        /// </summary>
        /// <param name="categoryList"></param>
        /// <returns></returns>
        [HttpPost("GetMachineListByCategory", Name = "GetMachineListByCategory")]
        public async Task<ActionResult> GetMachineListByCategory([FromBody] GetMachineDataDto data)
        {
            var machines = await (from rl in _machine_Craft_RLService.GetAll()
                                  join machine in _machineService.GetAll().Where(o => data.CategoryList.Contains(o.Category))
                                  on rl.MachineId equals machine.Id
                                  where rl.CraftId == data.CraftId && !machine.IsStop
                                  select new SelectionDto
                                  {
                                      Value = machine.Id.ToString(),
                                      Text = $"{machine.Code}-{machine.Name}",
                                      Code = machine.Category
                                  }).ToListAsync();
            return Ok(machines);
        }

        /// <summary>
        /// 新增特性参数
        /// </summary>
        /// <param name="para"></param>
        /// <returns></returns>
        [HttpPost("InsertCharacterSystemPara", Name = "InsertCharacterSystemPara")]
        public async Task<IActionResult> InsertCharacterSystemPara([FromBody] SystemPara para)
        {
            var list = _systemParaService.GetAll().Where(o => o.DataType == para.DataType);
            if (list.Any())
            {
                para.Order = list.Max(o => o.Order) + 1;
            }
            else
            {
                para.Order = 1;
            }
            await _systemParaService.InsertAsync(para);
            await _unitWork.SaveChangeAsync();
            var result = new SelectionDto
            {
                Value = para.Id.ToString(),
                Code = para.Code,
                Text = para.DataValue
            };
            return Ok(result);
        }

        public class PostMaterialDto
        {
            public List<Filter> CommonFilterList { get; set; } = new List<Filter>();
            public List<Filter> CustomerFilterList { get; set; } = new List<Filter>();
            public MaterialType MaterialType { get; set; }
        }

        /// <summary>
        /// 获取原材料列表
        /// </summary>
        /// <param name="filterList"></param>
        /// <returns></returns>
        [HttpPost("GetRawMaterialList", Name = "GetRawMaterialList")]
        public async Task<IActionResult> GetRawMaterialList([FromBody] PostMaterialDto postData)
        {
            var result = new ToPageListResult<MaterialDto>();
            switch (postData.MaterialType)
            {
                case MaterialType.原材料:
                    result = await _rawMaterialService.GetAll()
                                                            .Select(o => new MaterialDto(o.Code, o.Name)
                                                            {
                                                                MaterialId = o.Id,
                                                                MaterialCode = o.Code,
                                                                MaterialName = o.Name,
                                                                Specification = o.Specification,
                                                                MaterialGrade = o.MaterialGrade,
                                                                MaterialCategory = o.RawMaterialCategory.Name,
                                                                Unit = o.Unit,
                                                                Remark = o.Remark,
                                                                MaterialCategoryId = o.RawMaterialCategoryId,
                                                                MaterialType = MaterialType.原材料,
                                                                Price = o.InventoryPrice
                                                            }).ApplyFilters(postData.CommonFilterList).ApplySorts(this.SortInfoList).ToPagedListAsync(this.PageModel);
                    break;
                case MaterialType.产品:
                    var products = _productService.GetAll();
                    if (postData.CustomerFilterList.Any())
                    {
                        var productIds = _customerProductRLService.GetAll().ApplyFilters(postData.CustomerFilterList).Select(o => o.ProductId);
                        products = products.Where(o => productIds.Contains(o.Id));
                    }
                    result = await products
                                             .Select(o => new MaterialDto(o.ProductNo, o.ProductName)
                                             {
                                                 MaterialId = o.Id,
                                                 MaterialCode = o.ProductNo,
                                                 MaterialName = o.ProductName,
                                                 Specification = o.Specification,
                                                 MaterialGrade = o.MaterialGrade,
                                                 MaterialCategory = "产品",
                                                 Unit = "件",
                                                 Remark = o.Remark,
                                                 Quantity = 1,
                                                 MaterialType = MaterialType.产品,
                                                 Version = o.Version,
                                                 CustomerMaterialNo = string.Join("\n", o.Customer_Product_RL.Select(e => e.CustomerMaterialNo).ToList()),
                                                 Price = o.InventoryPrice
                                             }).ApplyFilters(postData.CommonFilterList).ApplySorts(this.SortInfoList).ToPagedListAsync(this.PageModel);
                    break;
                case MaterialType.半成品:
                    var productInfos = _productService.GetAll();
                    if (postData.CustomerFilterList.Any())
                    {
                        var productIds = _customerProductRLService.GetAll().ApplyFilters(postData.CustomerFilterList).Select(o => o.ProductId);
                        products = productInfos.Where(o => productIds.Contains(o.Id));
                    }
                    result = await (from semi in _semiProductService.GetAll()
                                    join product in productInfos
                                    on semi.ProductId equals product.Id
                                    where !semi.IsDeleteNode && !semi.NodeTemplateSetting.IsDeleted
                                        && semi.ProcessOrder != null && !semi.ProcessOrder!.IsDeleted
                                        && !semi.Product.IsDeleted
                                    select new MaterialDto(semi.Code, semi.Name)
                                    {
                                        MaterialId = semi.Id,
                                        MaterialCode = semi.Code,
                                        MaterialName = semi.Name,
                                        Specification = product.Specification,
                                        MaterialGrade = product.MaterialGrade,
                                        MaterialCategory = "半成品",
                                        Unit = "件",
                                        Remark = semi.Remark,
                                        Quantity = 1,
                                        MaterialType = MaterialType.半成品,
                                        Version = product.Version,
                                        CustomerMaterialNo = string.Join("\n", product.Customer_Product_RL.Select(e => e.CustomerMaterialNo).ToList()),
                                        Price = semi.InventoryPrice
                                    }).ApplyFilters(postData.CommonFilterList).ApplySorts(this.SortInfoList).ToPagedListAsync(PageModel);
                    break;
                default:
                    break;
            }

            return Ok(result);
        }

        /// <summary>
        /// 获取工艺单最新序列
        /// </summary>
        /// <param name="productNo"></param>
        /// <returns></returns>
        [HttpGet("GetProcessOrderNewSequence", Name = "GetProcessOrderNewSequence")]
        public Task<OkObjectResult> GetProcessOrderNewSequence(string productNo)
        {
            return System.Threading.Tasks.Task.Run(() =>
            {
                var tableName = $"{TableName.ProcessOrder}-{productNo}";
                var sequence = _sequenceService.GetSequenceOrInsert(tableName).SequenceNo.ToString("00");
                return Ok(sequence);
            });
        }

        //报工台 mac地址配置使用
        [AllowAnonymous]
        [HttpGet("GeGaugeCategoryList", Name = "GeGaugeCategoryList")]
        public async Task<IActionResult> GeGaugeCategoryList()
        {
            var data = await _workFixtureCategoryService.GetAll().Where(o => o.Type == WorkFixtureType.检具)
                .Select(o => new SelectionDto
                {
                    Value = o.Id.ToString(),
                    Code = o.Code,
                    Text = o.Name,
                }).ToListAsync();
            return Ok(data);
        }
    }


}