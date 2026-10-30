using Kingzon.OLCM.Base.Services;
using KingZon.Core;
using KingZon.Core.DataBase;
using KingZon.Core.Web;
using KingZon.Mes.Workflow.Model;
using KingZon.Mes.Workflow.Service.Interface;
using KingZon.OLCM.Base.Service;
using KingZon.OLCM.Base.Service.Dto;
using KingZon.OLCM.Base.Shared.DataBase.Entities;
using KingZon.OLCM.Base.Shared.DataBase.Enum;
using KingZon.OLCM.Base.Shared.DataBase.JsonStructure;
using KingZon.OLCM.Production.Shared.DataBase.Entities;
using KingZon.OLCM.Production.Shared.DataBase.Enum;
using KingZon.OLCM.Production.Shared.DataBase.JsonStructure;
using KingZon.OLCM.Purchase.DataBase.Entities;
using KingZon.OLCM.Technical.Shared.DataBase.Entities;
using KingZon.OLCM.Technical.Shared.DataBase.Enum;
using KingZon.OLCM.Technical.WebApi.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Transactions;

namespace KingZon.OLCM.Technical.WebApi.Controllers
{
    /// <summary>
    /// 标准工序单及工序单版本管理。
    /// </summary>
    [Route("api/technical/[controller]")]
    [ApiController]
    public class CraftOrderManageController : KingZonControllerBase
    {
        private readonly CommonService<Technical_CraftOrder> _craftOrderService;
        private readonly CommonService<Technical_CraftOrderVersion> _craftVersionService;
        private readonly CommonService<Technical_NodeSettingTemplate> _nodeSettingService;
        private readonly CommonService<Technical_NodeSettingTemplate_Machine_RL> _machineRLService;
        private readonly CommonService<Technical_NodeSettingTemplate_Material_RL> _materialRLService;
        private readonly CommonService<Production_MBom> _mBomService;
        private readonly CommonService<Production_MBom_Version> _mBomVersionService;
        private readonly CommonService<Production_MBom_Detail> _mBomDetailService;
        private readonly CommonService<Base_Craft> _craftService;
        private readonly CommonService<Base_Machine> _machineService;
        private readonly ProductService _productService;
        private readonly CommonService<Base_ProductSpecification> _specificationService;
        private readonly IWorkflowTemplateService _workflowTemplateService;
        private readonly IWorkflowTypeService _workflowTypeService;
        private readonly INodeTypeService _nodeTypeService;
        private readonly INodeTemplateService _nodeTemplateService;
        private readonly IRepository<Base_Goods> _goodsRepository;
        private readonly IRepository<Purchase_Order_Detail> _purchasedDetailRepository;
        private readonly FileStorageService _fileStorageService;
        private readonly Utils _utils;
        private readonly IUnitOfWork _unitOfWork;

        public CraftOrderManageController(
            CommonService<Technical_CraftOrder> craftOrderService,
            CommonService<Technical_CraftOrderVersion> craftVersionService,
            CommonService<Technical_NodeSettingTemplate> nodeSettingService,
            CommonService<Technical_NodeSettingTemplate_Machine_RL> machineRLService,
            CommonService<Technical_NodeSettingTemplate_Material_RL> materialRLService,
            CommonService<Production_MBom> mBomService,
            CommonService<Production_MBom_Version> mBomVersionService,
            CommonService<Production_MBom_Detail> mBomDetailService,
            CommonService<Base_Craft> craftService,
            CommonService<Base_Machine> machineService,
            ProductService productService,
            CommonService<Base_ProductSpecification> specificationService,
            IWorkflowTemplateService workflowTemplateService,
            IWorkflowTypeService workflowTypeService,
            INodeTypeService nodeTypeService,
            INodeTemplateService nodeTemplateService,
            IRepository<Base_Goods> goodsRepository,
            IRepository<Purchase_Order_Detail> purchasedDetailRepository,
            FileStorageService fileStorageService,
            Utils utils,
            IUnitOfWork unitOfWork)
        {
            _craftOrderService = craftOrderService;
            _craftVersionService = craftVersionService;
            _nodeSettingService = nodeSettingService;
            _machineRLService = machineRLService;
            _materialRLService = materialRLService;
            _mBomService = mBomService;
            _mBomVersionService = mBomVersionService;
            _mBomDetailService = mBomDetailService;
            _craftService = craftService;
            _machineService = machineService;
            _productService = productService;
            _specificationService = specificationService;
            _workflowTemplateService = workflowTemplateService;
            _workflowTypeService = workflowTypeService;
            _nodeTypeService = nodeTypeService;
            _nodeTemplateService = nodeTemplateService;
            _goodsRepository = goodsRepository;
            _purchasedDetailRepository = purchasedDetailRepository;
            _fileStorageService = fileStorageService;
            _utils = utils;
            _unitOfWork = unitOfWork;
        }

        #region 工艺单管理

        public class GetCraftProductOptions: ProductDto
        {
            public bool IsHaveCraftOrder { get; set; }
        }

        /// <summary>
        /// 获取产品/半成品列表（分页 + 筛选）
        /// </summary>
        [HttpPost("GetProductList")]
        public async Task<IActionResult> GetProductList([FromBody] List<Filter> filters)
        {
            var filterList = filters ?? new List<Filter>();
            var filter = new List<Filter>();

            var craftOrderInfo = from order in _craftOrderService.GetAll()
                                 group order by new { order.ProductId, order.SpecificationId } into g
                                 select new
                                 {
                                     Id = g.Max(x=>x.Id),
                                     ProductId = g.Key.ProductId,
                                     SpecificationId = g.Key.SpecificationId,
                                 };

            var productInfo = from x in _productService.GetProductList(filter, filter)
                              join craftOrder in craftOrderInfo
                                    on new { x.ProductId, SpecificationId = x.SpecificationId  }
                                    equals new { craftOrder.ProductId, SpecificationId = craftOrder.SpecificationId }
                                    into craftOrders
                              from craftOrder in craftOrders.DefaultIfEmpty()
                              select new GetCraftProductOptions
                              {
                                  ProductId = x.ProductId,
                                  ProductNo = x.ProductNo,
                                  ProductName = x.ProductName,
                                  ProductTechnicalCode = x.ProductTechnicalCode,
                                  ProductCategory = x.ProductCategory,
                                  ProductUnit = x.ProductUnit,
                                  IsDeleted = x.IsDeleted,
                                  SpecificationId = x.SpecificationId,
                                  ProductSpecification = x.ProductSpecification,
                                  ProductPackageWay = x.ProductPackageWay,
                                  PackageWayId = x.PackageWayId,
                                  Remark = x.Remark,
                                  ProductCategoryId = x.ProductCategoryId,
                                  ProductUnitId = x.ProductUnitId,
                                  ReviewStatus = x.ReviewStatus,
                                  ProductType = x.ProductType,
                                  InventoryQuantity = x.InventoryQuantity,
                                  UnPackageQuantity = x.UnPackageQuantity,
                                  PackageQuantity = x.PackageQuantity,
                                  ProductMainUnitId = x.ProductMainUnitId,
                                  ProductMainUnit = x.ProductMainUnit,
                                  CategoryAttribute = x.CategoryAttribute,
                                  ProductProperty = x.ProductProperty,
                                  MatureTime = x.MatureTime,
                                  CustomAttribute = x.CustomAttribute,
                                  SafetyStock = x.SafetyStock,
                                  MaxStock = x.MaxStock,
                                  MaterialSpecification = x.MaterialSpecification,
                                  OccupyInventory = x.OccupyInventory,
                                  AbnormalQuantity = x.AbnormalQuantity,
                                  MaterialGrade = x.MaterialGrade,
                                  IsHaveCraftOrder = craftOrder.Id>0?true:false
                              };
            var data = await productInfo
                .Where(x => x.ProductType != ProductType.物料)
                .ApplySorts(this.SortInfoList)
                .ApplyFilters(filterList)
                .ToPagedListAsync(this.PageModel);
            return Ok(data);
        }

        /// <summary>工序编辑时可选择的加工工艺。</summary>
        [HttpGet("GetCraftList")]
        public async Task<IActionResult> GetCraftList()
        {
            var data = await _craftService.GetAll()
                .OrderBy(x => x.Name)
                .Select(x => new { x.Id, x.Name })
                .ToListAsync();
            return Ok(data);
        }

        /// <summary>工序资料库：当前加工工艺关联的启用机器。</summary>
        [HttpGet("GetCraftMachineList")]
        public async Task<IActionResult> GetCraftMachineList([FromQuery] long craftId)
        {
            var machines = await _machineService.GetAll()
                .Where(machine => craftId > 0 && !machine.IsDeleted && !machine.IsStop
                    && machine.CraftRLs.Any(rl => rl.CraftId == craftId))
                .OrderBy(machine => machine.Code).ThenBy(machine => machine.Id)
                .Select(machine => new
                {
                    machine.Id,
                    machine.Code,
                    machine.Name,
                    machine.Model,
                    CategoryName = machine.Category.Name,
                    CraftId = craftId,
                })
                .ToListAsync();
            return Ok(machines);
        }

        /// <summary>
        /// 工序资料库：仅提供物料、半成品、成品。
        /// 与 FormulaManage 的资源查询分离，确保 DataId 始终是产品 Id、UnitId 始终是基本单位 Id。
        /// </summary>
        [HttpPost("GetCraftResourceList")]
        public async Task<IActionResult> GetCraftResourceList(
            [FromBody] List<Filter> filters,
            [FromQuery] ProductType productType = ProductType.物料)
        {
            var publishedVersions = _craftVersionService.GetAll();
            // 所有资料均来自产品实体；避免在匿名投影上使用 EF.Property。
            var data = await _productService.GetAll()
                .Where(product => product.ProductType == productType && !product.IsDeleted
                    && product.ReviewStatus == ReviewStatus.评审通过)
                .Select(product => new CraftResourceDto
                {
                    DataId = product.Id,
                    DataType = InventoryDataType.Product,
                    Code = product.ProductNo,
                    Name = product.ProductName,
                    UnitId = product.BaseUnitId,
                    UnitName = product.BaseUnit.Name,
                    TechCode = product.TechnicalCode,
                    Specification = product.MaterialSpecification,
                    MaterialGrade = product.MaterialGrade,
                    Remark = product.Remark,
                    // 展示可引用的已发布工艺版本，优先主版本。
                    Version = publishedVersions
                        .Where(version => version.CraftOrder.ProductId == product.Id
                            && version.CraftOrder.SpecificationId == null
                            && version.Status == CraftOrderVersionStatus.已发布)
                        .OrderByDescending(version => version.IsMain).ThenByDescending(version => version.SequenceNo)
                        .Select(version => version.Version).FirstOrDefault()
                })
                .ApplyFilters(filters ?? new List<Filter>())
                .ToPagedListAsync(this.PageModel);
            return Ok(data);
        }


        /// <summary>
        /// 获取工序单基本信息
        /// </summary>
        [HttpGet("GetCraftOrderInfo")]
        public async Task<IActionResult> GetCraftOrderInfo([FromQuery] long craftOrderId)
        {
            if (craftOrderId <= 0)
            {
                return BadRequest("工序单Id不能为空");
            }
            return Ok(await GetCraftOrderInfoAsync(craftOrderId));
        }

        /// <summary>
        /// 获取产品的主版本工序树结构（用于左侧树懒加载）
        /// </summary>
        [HttpGet("GetCraftTreeByProductId")]
        public async Task<IActionResult> GetCraftTreeByProductId([FromQuery] InventoryDataType dataType, long dataId)
        {
            if (dataId <= 0)
            {
                return Ok(new List<object>());
            }

            // 查找该产品的工序单
            var craftOrder = await _craftOrderService.GetAll()
                .FirstOrDefaultAsync(x => dataType == InventoryDataType.Product ? x.ProductId == dataId : x.SpecificationId == dataId);

            if (craftOrder == null)
            {
                return Ok(new List<object>());
            }

            // 查找主版本
            var mainVersion = await _craftVersionService.GetAll()
                .FirstOrDefaultAsync(x => x.CraftOrderId == craftOrder.Id && x.IsMain);

            if (mainVersion == null)
            {
                return Ok(new List<object>());
            }

            // 加载该版本的工序
            var nodes = await _nodeSettingService.GetAll()
                .Where(x => x.CraftOrderVersionId == mainVersion.Id)
                .OrderBy(x => x.Sequence)
                .ToListAsync();

            if (nodes.Count == 0)
            {
                return Ok(new List<object>());
            }

            var nodeIds = nodes.Select(x => x.Id).ToList();

            // 加载物料（使用 JoinWithData 获取物料名称）
            var materialQuery = _materialRLService.GetAll()
                .Where(x => nodeIds.Contains(x.NodeSettingTemplateId) && x.InsteadSourceId == null);

            var materialJoinQuery = _utils.JoinWithData(materialQuery, "DataType", "DataId", (materialRl, materialInfo) =>
            new
            {
                Id = materialRl.Id,
                DataId = materialRl.DataId,
                DataType = materialRl.DataType,
                IOType = materialRl.IOType,
                Quantity = materialRl.Quantity,
                MaterialNo = materialInfo.DataNo ?? "",
                MaterialName = materialInfo.DataName ?? "",
                MaterialType = materialInfo.DataProductType,
                NodeSettingTemplateId = materialRl.NodeSettingTemplateId,
                RefCraftOrderVersionId = materialRl.RefCraftOrderVersionId,
            }, false, false, null);

            var materials = materialJoinQuery.ToList();

            // 构建树结构
            var result = nodes.Select(node =>
            {
                var nodeMaterials = materials.Where(m => m.NodeSettingTemplateId == node.Id).ToList();
                var inputs = nodeMaterials.Where(m => m.IOType == NodeIOType.输入).ToList();
                var outputs = nodeMaterials.Where(m => m.IOType == NodeIOType.输出).ToList();

                return new
                {
                    id = $"ref-process-{node.Id}",
                    name = $"{node.Code} {node.Name}".Trim(),
                    code = node.Code,
                    typeText = "工序",
                    state = "open",
                    children = new object[]
                    {
                        new {
                            id = $"ref-process-{node.Id}-input",
                            name = "领用主料",
                            typeText = "领用",
                            state = inputs.Count > 0 ? "open" : "closed",
                            children = inputs.Select(m => new {
                                id = $"ref-material-{m.Id}",
                                name = m.MaterialName,
                                code = m.MaterialNo,
                                typeText = m.MaterialType,
                                quantityText = m.Quantity.ToString("0.############################"),
                                dataId = m.DataId,
                                dataType = m.DataType,
                                materialType = m.MaterialType,
                                refCraftOrderVersionId = m.RefCraftOrderVersionId,
                                state =  m.MaterialType != ProductType.物料.ToString() ? "closed" : "open",
                                children = m.MaterialType != ProductType.物料.ToString() ? new List<object>() :  null
                            }).ToList()
                        },
                        new {
                            id = $"ref-process-{node.Id}-output",
                            name = "入库物料",
                            typeText = "产出",
                            state = outputs.Count > 0 ? "open" : "closed",
                            children = outputs.Select(m => new {
                                id = $"ref-material-{m.Id}",
                                name = m.MaterialName,
                                code = m.MaterialNo,
                                typeText = m.MaterialType,
                                quantityText = m.Quantity.ToString("0.############################"),
                                dataId = m.DataId,
                                dataType = m.DataType,
                                materialType = m.MaterialType,
                                refCraftOrderVersionId = m.RefCraftOrderVersionId,
                                state = m.MaterialType != ProductType.物料.ToString() ? "closed" : "open",
                                children = m.MaterialType != ProductType.物料.ToString() ? new List<object>() :  null
                            }).ToList()
                        }
                    }.Take(node.IsInbound || node.IsOutsource ? 2 : 1).ToArray()
                };
            }).ToList();

            return Ok(result);
        }

        /// <summary>
        /// 创建工序单主数据
        /// </summary>
        [HttpPost("CreateCraftOrder")]
        public async Task<IActionResult> CreateCraftOrder([FromBody] SaveCraftOrderDto postData)
        {
            ValidateCraftOrder(postData);

            var productExists = await _productService.GetAll(true)
                .AnyAsync(x => x.Id == postData.ProductId && x.ProductType != ProductType.物料);
            if (!productExists)
            {
                throw new Exception("选择的产品或半成品不存在，请重新选择");
            }
            if (postData.SpecificationId.HasValue && !await _specificationService.GetAll(true)
                .AnyAsync(x => x.Id == postData.SpecificationId.Value && x.ProductId == postData.ProductId))
            {
                throw new Exception("所选规格不属于当前产品，请重新选择");
            }

            // 确保每个产品/半成品只对应一个工序单
            var existingOrder = await _craftOrderService.GetAll(true)
                .FirstOrDefaultAsync(x => x.ProductId == postData.ProductId && x.SpecificationId == postData.SpecificationId);
            if (existingOrder != null)
            {
                throw new Exception($"该产品已存在工艺单（编码：{existingOrder.Code}），请直接编辑该工艺单");
            }

            if (!string.IsNullOrEmpty(postData.Code))
            {
                var isSameCode = _craftOrderService.GetAll(true).Where(x => x.Code == postData.Code).Any();
                if (isSameCode)
                {
                    throw new Exception("该工艺单编码已存在，请重新修改");
                }
            }

            var entity = new Technical_CraftOrder
            {
                Code = string.IsNullOrWhiteSpace(postData.Code) ? string.Empty : postData.Code.Trim(),
                Name = postData.Name.Trim(),
                Description = string.IsNullOrWhiteSpace(postData.Description) ? null : postData.Description.Trim(),
                ProductId = postData.ProductId,
                SpecificationId = postData.SpecificationId,
            };
            await _craftOrderService.InsertAsync(entity);
            await _unitOfWork.SaveChangeAsync();

            if (string.IsNullOrWhiteSpace(entity.Code))
            {
                entity.Code = $"CO{entity.Id:D8}";
                await _unitOfWork.SaveChangeAsync();
            }

            return Ok(await GetCraftOrderInfoAsync(entity.Id));
        }

        /// <summary>
        /// 根据产品ID查找工序单，确保每个产品/半成品只对应一个工序单
        /// </summary>
        [HttpGet("GetCraftOrderByProductId")]
        public async Task<IActionResult> GetCraftOrderByProductId([FromQuery] InventoryDataType dataType, [FromQuery] long dataId)
        {
            if (dataId <= 0)
            {
                throw new Exception("dataId 不能为空");
            }

            var craftOrder = await _craftOrderService.GetAll()
                .FirstOrDefaultAsync(x => dataType == InventoryDataType.Product ? x.ProductId == dataId : x.SpecificationId == dataId);

            if (craftOrder == null)
            {
                return Ok(new CraftOrderInfoDto());
            }

            return Ok(await GetCraftOrderInfoAsync(craftOrder.Id));
        }

        /// <summary>
        /// 已有工艺单只允许改名，产品、规格、编码和说明保持不变。
        /// </summary>
        [HttpPost("UpdateCraftOrder")]
        public async Task<IActionResult> UpdateCraftOrder([FromBody] RenameCraftOrderDto postData)
        {
            if (postData == null || postData.Id <= 0)
            {
                throw new Exception("请选择需要保存的工序单");
            }
            if (string.IsNullOrWhiteSpace(postData.Name) || postData.Name.Trim().Length > 100)
            {
                throw new Exception("请输入不超过100个字符的工艺单名称");
            }

            var entity = await _craftOrderService.GetByIdAsync(postData.Id)
                ?? throw new Exception("工序单不存在，请刷新后重试");
            entity.Name = postData.Name.Trim();
            await _unitOfWork.SaveChangeAsync();

            return Ok(await GetCraftOrderInfoAsync(entity.Id));
        }

        private static void ValidateCraftOrder(SaveCraftOrderDto? postData)
        {
            if (postData == null || postData.ProductId <= 0)
            {
                throw new Exception("请选择产品或半成品");
            }
            if (string.IsNullOrWhiteSpace(postData.Name) || postData.Name.Trim().Length > 100)
            {
                throw new Exception("请输入工序单名称");
            }
        }

        private async Task<CraftOrderInfoDto> GetCraftOrderInfoAsync(long craftOrderId)
        {
            var query = from order in _craftOrderService.GetAll()
                        where order.Id == craftOrderId
                        join product in _productService.GetAll()
                            on order.ProductId equals product.Id
                        join specification in _specificationService.GetAll()
                            on order.SpecificationId equals specification.Id into specifications
                        from specification in specifications.DefaultIfEmpty()
                        let purchaseId = specification != null ? specification.PurchaseOrderDetailId : product.PurchaseOrderDetailId
                        join purchase in _purchasedDetailRepository.GetAll(true)
                            on purchaseId equals purchase.Id into purchases
                        from purchase in purchases.DefaultIfEmpty()
                        select new CraftOrderInfoDto
                        {
                            Id = order.Id,
                            Code = order.Code,
                            Name = order.Name,
                            Description = order.Description,
                            ProductId = order.ProductId,
                            SpecificationId = order.SpecificationId,
                            ProductNo = product.ProductNo,
                            ProductName = product.ProductName,
                            SpecificationName = specification != null ? specification.Name : null,
                            MaterialInfo = new DataInfo
                            {
                                DataNo = specification != null ? specification.Code : product.ProductNo,
                                DataName = specification != null ? specification.Name : product.ProductName,
                                DataSpecification = specification != null ? specification.Specification : product.MaterialSpecification,
                                DataMaterialGrade = specification != null ? "" : product.MaterialGrade,
                                DataProductType = product.ProductType.ToString(),
                                DataTechnicalCode = product.TechnicalCode,
                                DataCategory = specification != null
                                    ? (specification.ProductCategory != null ? specification.ProductCategory.Name : null)
                                    : (product.ProductCategory != null ? product.ProductCategory.Name : null),
                                DataCategoryId = specification != null ? specification.ProductCategoryId : product.ProductCategoryId,
                                DataUnit = specification != null
                                    ? (specification.SpecificationUnit != null ? specification.SpecificationUnit.Name : null)
                                    : (product.BaseUnit != null ? product.BaseUnit.Name : null),
                                DataUnitId = specification != null ? specification.SpecificationUnitId : product.BaseUnitId,
                                DataBaseUnitId = product.BaseUnitId,
                                DataBaseUnitName = product.BaseUnit != null ? product.BaseUnit.Name : null,
                                DataProductId = product.Id,
                                DataMinStock = specification != null ? specification.SafetyStock : product.SafetyStock,
                                DataMaxStock = specification != null ? specification.MaxStock : product.MaxStock,
                                DataTotalStock = specification != null ? specification.SpecificationInventoryQuantity : product.InventoryQuantity,
                                DataRemark = specification != null ? specification.Remark : product.Remark,
                                CurrentPurchasePrice = purchase != null ? purchase.Price : null,
                            },
                            CreateTime = order.CreateTime,
                            CreateUserName = order.CreateUserName,
                            UpdateTime = order.UpdateTime,
                            UpdateUserName = order.UpdateUserName,
                        };
            var data = await query.FirstOrDefaultAsync()
                ?? throw new Exception("工序单不存在或关联产品已删除，请刷新后重试");

            // 客户料号为一对多，单独查询后汇总，避免主查询连表产生重复工艺单。
            var customerMaterials = await _goodsRepository.GetAll()
                .Where(goods => goods.ProductId == data.ProductId && goods.SpecificationId == data.SpecificationId
                    && !goods.Customer.IsDeleted)
                .OrderBy(goods => goods.Id)
                .Select(goods => new { goods.CustomerMaterialNo, goods.Customer.CustomerName })
                .ToListAsync();
            data.MaterialInfo!.DataCustomerMaterialNo = string.Join("\n", customerMaterials
                .Where(goods => !string.IsNullOrEmpty(goods.CustomerMaterialNo))
                .Select(goods => goods.CustomerMaterialNo));
            data.MaterialInfo.DataMaterialNoCustomerName = string.Join("\n", customerMaterials
                .Select(goods => (goods.CustomerMaterialNo ?? "") + "-" + goods.CustomerName));
            return data;
        }

        /// <summary>
        /// 分页查询工艺单列表（含产品信息，回填版本数/主版本）。
        /// </summary>
        [HttpPost("GetCraftOrderList")]
        public async Task<IActionResult> GetCraftOrderList([FromBody] GetCraftOrderListDto postData)
        {
            var query = from craftOrder in _craftOrderService.GetAll().ApplyFilters(postData.CraftOrderFilterList ?? new List<Filter>())
                        join product in _productService.GetAll().ApplyFilters(postData.ProductFilterList ?? new List<Filter>())
                            on craftOrder.ProductId equals product.Id
                        join specification in _specificationService.GetAll()
                            on craftOrder.SpecificationId equals specification.Id into specificationItems
                        from specification in specificationItems.DefaultIfEmpty()
                        select new CraftOrderListItemDto
                        {
                            Id = craftOrder.Id,
                            Code = craftOrder.Code,
                            Name = craftOrder.Name,
                            Description = craftOrder.Description,
                            ProductId = craftOrder.ProductId,
                            SpecificationId = craftOrder.SpecificationId,
                            ProductNo = product.ProductNo,
                            ProductName = product.ProductName,
                            SpecificationName = specification != null ? specification.Name : null,
                            CreateTime = craftOrder.CreateTime,
                            CreateUserName = craftOrder.CreateUserName,
                        };

            var pagedResult = await query.ApplySorts(this.SortInfoList).ToPagedListAsync(this.PageModel);

            // 批量回填版本数/主版本：先查当前页 Id -> 分组统计 -> 回填，避免 N+1
            var craftOrderIds = pagedResult.Data.Select(x => x.Id).ToList();
            var versionGroups = await _craftVersionService.GetAll()
                .Where(x => craftOrderIds.Contains(x.CraftOrderId))
                .GroupBy(x => x.CraftOrderId)
                .Select(g => new
                {
                    CraftOrderId = g.Key,
                    Count = g.Count(),
                    MainVersion = g.Where(v => v.IsMain).Select(v => v.Version).FirstOrDefault(),
                })
                .ToListAsync();
            var versionMap = versionGroups.ToDictionary(x => x.CraftOrderId);
            foreach (var item in pagedResult.Data)
            {
                if (versionMap.TryGetValue(item.Id, out var versionInfo))
                {
                    item.VersionCount = versionInfo.Count;
                    item.MainVersion = versionInfo.MainVersion;
                }
            }

            return Ok(pagedResult);
        }

        #endregion

        #region 工艺单版本管理

        /// <summary>
        /// 查询指定工序单的版本列表，含工序单/产品信息以及每个版本下的工序明细数量。
        /// 主版本排在最前，其余版本按版本号倒序返回。
        /// </summary>
        /// <param name="craftOrderId">工序单 Id</param>
        [HttpGet("GetCraftVersionList")]
        public async Task<IActionResult> GetCraftVersionList([FromQuery] long craftOrderId)
        {
            if (craftOrderId <= 0)
            {
                throw new Exception("请先选择工序单");
            }

            var query = from version in _craftVersionService.GetAll()
                        where version.CraftOrderId == craftOrderId
                        orderby version.IsMain descending, version.SequenceNo descending
                        select new CraftVersionDto
                        {
                            BomVersionId = version.BomVersionId,
                            Id = version.Id,
                            CraftOrderId = version.CraftOrderId,
                            CraftOrderCode = version.CraftOrder.Code,
                            CraftOrderName = version.CraftOrder.Name,
                            SequenceNo = version.SequenceNo,
                            Version = version.Version,
                            IsMain = version.IsMain,
                            Status = version.Status,
                            Remark = version.Remark,
                            CreateTime = version.CreateTime,
                            CreateUserName = version.CreateUserName,
                        };

            var result = await query.ToListAsync();

            // 批量统计每个版本的工序明细数量，避免逐条查库
            var versionIds = result.Select(x => x.Id).ToList();
            var nodeCounts = await _nodeSettingService.GetAll()
                .Where(x => versionIds.Contains(x.CraftOrderVersionId))
                .GroupBy(x => x.CraftOrderVersionId)
                .Select(x => new { CraftOrderVersionId = x.Key, Count = x.Count() })
                .ToDictionaryAsync(x => x.CraftOrderVersionId, x => x.Count);

            foreach (var item in result)
            {
                item.NodeCount = nodeCounts.TryGetValue(item.Id, out var count) ? count : 0;
            }

            return Ok(result);
        }

        /// <summary>
        /// 查询产品或半成品可引用的已发布工序单版本。
        /// 工序物料只能引用与自身产品一致且已发布的版本。
        /// </summary>
        [HttpGet("GetPublishedCraftVersionOptions")]
        public async Task<IActionResult> GetPublishedCraftVersionOptions([FromQuery] long productId)
        {
            if (productId <= 0)
            {
                return Ok(new List<object>());
            }

            var result = await _craftVersionService.GetAll()
                .Where(x => x.CraftOrder.ProductId == productId && x.Status == CraftOrderVersionStatus.已发布)
                .OrderByDescending(x => x.IsMain)
                .ThenByDescending(x => x.SequenceNo)
                .Select(x => new
                {
                    Id = x.Id,
                    Text = x.Version,
                    IsMain = x.IsMain,
                    Remark = x.Remark,
                })
                .ToListAsync();
            return Ok(result);
        }

        /// <summary>
        /// 以 craftVersion 为根加载完整编辑模型：版本头 + 其下全部工序（含每道工序的设备 MachineIds）。
        /// </summary>
        [HttpGet("GetCraftVersion")]
        public async Task<IActionResult> GetCraftVersion([FromQuery] long versionId)
        {

            var options = JsonHelper.DefaultJsonSerializerOptions;
            if (versionId <= 0)
            {
                throw new Exception("请选择工序单版本");
            }

            var version = await _craftVersionService.GetAll()
                .Include(x => x.CraftOrder)
                .FirstOrDefaultAsync(x => x.Id == versionId)
                ?? throw new Exception("工序单版本不存在，请刷新后重试");

            var nodes = await _nodeSettingService.GetAll()
                .Include(x => x.Craft)
                .Where(x => x.CraftOrderVersionId == version.Id)
                .OrderBy(x => x.Sequence)
                .ToListAsync();

            var nodeIds = nodes.Select(x => x.Id).ToList();
            var machineRows = await _machineRLService.GetAll()
                .Where(x => nodeIds.Contains(x.NodeSettingTemplateId))
                .ToListAsync();
            var machineGroups = machineRows.GroupBy(x => x.NodeSettingTemplateId)
                .ToDictionary(group => group.Key, group => group.Select(machine => new NodeMachineSettingDto
                {
                    MachineId = machine.MachineId,
                    TheoreticalWorkHours = machine.TheoreticalWorkHours
                }).ToList());

            var materialQuery = _materialRLService.GetAll().Where(x => nodeIds.Contains(x.NodeSettingTemplateId));

            var materialJoinQuery = _utils.JoinWithData(materialQuery, "DataType", "DataId", (materialRl, materialInfo) =>
            new NodeMaterialRLDto
            {
                Id = materialRl.Id,
                DataId = materialRl.DataId,
                MaterialNo = materialInfo.DataNo ?? "",
                MaterialName = materialInfo.DataName ?? "",
                MaterialDescription = materialInfo.DataMaterialGrade ?? "",
                MaterialType = materialInfo.DataProductType,
                DataType = materialRl.DataType,
                IOType = materialRl.IOType,
                Quantity = materialRl.Quantity,
                UnitId = materialRl.UnitId,
                UnitName = materialRl.Unit.Name,
                UnitConversionId = materialRl.UnitConversionId,
                LossRate = materialRl.LossRate,
                SortNo = materialRl.SortNo,
                RefCraftOrderVersionId = materialRl.RefCraftOrderVersionId,
                RefCraftOrderVersionName = materialRl.RefCraftOrderVersion != null ? materialRl.RefCraftOrderVersion.Version : string.Empty,
                Remark = materialRl.Remark,
                NodeSettingTemplateId = materialRl.NodeSettingTemplateId,
                InsteadSourceId = materialRl.InsteadSourceId,
                Substitutes = new List<NodeMaterialRLDto>(),
                UnitList = JsonSerializer.Deserialize<List<InventoryUnitInboundDto>>(materialRl.UnitListInfo ?? "[]", options)!,
            }, false, false, null);

            var materialRows = materialJoinQuery.ToList();

            // 拆成两堆：原物料（主列表） + 替料（按指向的原物料 Id 分组）
            var mainMaterials = materialRows.Where(x => x.InsteadSourceId == null)
                .OrderBy(x => x.SortNo).ThenBy(x => x.Id).ToList();
            var substituteGroups = materialRows
                .Where(x => x.InsteadSourceId != null)
                .GroupBy(x => x.InsteadSourceId!.Value)
                .ToDictionary(g => g.Key, g => g.OrderBy(x => x.SortNo).ThenBy(x => x.Id).ToList());

            // 把替料挂到对应原物料上
            foreach (var main in mainMaterials)
            {
                if (substituteGroups.TryGetValue(main.Id, out var subs))
                {
                    main.Substitutes = subs;
                }
            }

            // 再按工序分组挂到每个 NodeSettingTemplateDto 上
            var materialsByNode = mainMaterials
                .GroupBy(x => x.NodeSettingTemplateId)
                .ToDictionary(g => g.Key, g => g.ToList());

            var result = new CraftVersionDetailDto
            {
                Id = version.Id,
                CraftOrderId = version.CraftOrderId,
                CraftOrderCode = version.CraftOrder.Code,
                CraftOrderName = version.CraftOrder.Name,
                SequenceNo = version.SequenceNo,
                Version = version.Version,
                IsMain = version.IsMain,
                Status = version.Status,
                Remark = version.Remark,
                NodeSettings = nodes.Select(node => new NodeSettingTemplateDto
                {
                    Id = node.Id,
                    Sequence = node.Sequence,
                    Code = node.Code,
                    Name = node.Name,
                    IsOutsource = node.IsOutsource,
                    IsInbound = node.IsInbound,
                    TheoreticalWorkHours = node.TheoreticalWorkHours,
                    IsReportWork = node.IsReportWork,
                    Description = node.Description,
                    NodeTemplateId = node.NodeTemplateId,
                    CraftId = node.CraftId,
                    CraftName = node.Craft?.Name,
                    MachineIds = machineGroups.TryGetValue(node.Id, out var machines) ? machines.Select(machine => machine.MachineId).ToList() : new List<long>(),
                    MachineSettings = machineGroups.TryGetValue(node.Id, out var settings) ? settings : new List<NodeMachineSettingDto>(),
                    Materials = materialsByNode.TryGetValue(node.Id, out var materials) ? materials : new()
                }).ToList(),
            };

            return Ok(result);
        }

        /// <summary>
        /// 以 craftVersion 为根整体保存（唯一保存入口）：
        /// 更新版本头（备注 / 主版本切换），并统一处理该版本下的全部工序（新增/更新/软删）及每道工序的设备；
        /// 全程一个事务，要么全部成功要么全部失败。
        /// </summary>
        [HttpPost("SaveCraftVersion")]
        public async Task<IActionResult> SaveCraftVersion([FromBody] SaveCraftVersionDto postData)
        {
            if (postData == null || postData.Id <= 0)
            {
                throw new Exception("请选择需要保存的工序单版本");
            }

            var version = await _craftVersionService.GetByIdAsync(postData.Id)
                ?? throw new Exception("工序单版本不存在，请刷新后重试");
            if (postData.CraftOrderId > 0 && version.CraftOrderId != postData.CraftOrderId)
            {
                throw new Exception("工序单版本与工序单不匹配，请刷新后重试");
            }
            if (version.Status is not (CraftOrderVersionStatus.暂存 or CraftOrderVersionStatus.待审核))
            {
                throw new Exception("仅暂存的版本允许编辑，请先取消发布或恢复版本");
            }

            using var transaction = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);

            version.Remark = postData.Remark;
            version.Status = CraftOrderVersionStatus.暂存;

            if (postData.IsMain && !version.IsMain)
            {
                await ClearOtherMainVersionsAsync(version.CraftOrderId, version.Id);
                version.IsMain = true;
            }

            // 工序明细依赖版本对应的工作流模板。新增工序时会先创建 WF_NodeTemplate，
            // 再把生成的 Id 写入 Technical_NodeSettingTemplate。
            await SaveNodeSettingsAsync(
                version.Id,
                version.WorkflowTemplateId,
                postData.NodeSettings ?? new List<NodeSettingTemplateDto>());

            await _unitOfWork.SaveChangeAsync();
            await SyncMBomAsync(version);
            await _unitOfWork.SaveChangeAsync();
            transaction.Complete();
            return Ok("保存成功");
        }

        /// <summary>
        /// 新建工序单版本（默认建立一道工序，不复制工序）：版本号自动取最大序号 +1（V1、V2…）；
        /// 该工序单还没有主版本时自动设为主版本，状态为暂存。
        /// </summary>
        [HttpPost("CreateCraftVersion")]
        public async Task<IActionResult> CreateCraftVersion([FromBody] CreateCraftVersionDto postData)
        {
            if (postData == null || postData.CraftOrderId <= 0)
            {
                throw new Exception("请先选择工序单");
            }
            using var transaction = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
            var entity = await CreateCraftVersionAsync(postData.CraftOrderId, postData.Remark,
                new List<NodeSettingTemplateDto>
                {
                    new NodeSettingTemplateDto { Code = "10", Name = "新工序", Sequence = 10 }
                });
            transaction.Complete();
            return Ok(new
            {
                Id = entity.Id,
                SequenceNo = entity.SequenceNo,
                Version = entity.Version,
                BomVersionId = entity.BomVersionId,
                IsMain = entity.IsMain,
                Status = entity.Status,
            });
        }

        /// <summary>复制所选版本的工序、设备及物料，在目标工艺单下建立独立的暂存版本。</summary>
        [HttpPost("CopyCraftVersion")]
        public async Task<IActionResult> CopyCraftVersion([FromBody] CopyCraftVersionDto postData)
        {
            if (postData == null || postData.CraftOrderId <= 0)
                throw new Exception("请先选择目标工艺单");
            if (postData.SourceCraftOrderId <= 0 || postData.SourceVersionId <= 0)
                throw new Exception("请选择来源工艺单及版本");

            using var transaction = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
            var source = await _craftVersionService.GetAll().AsNoTracking()
                .Where(x => x.Id == postData.SourceVersionId && !x.IsDeleted && !x.CraftOrder.IsDeleted)
                .Select(x => new { x.CraftOrderId, x.Remark })
                .FirstOrDefaultAsync()
                ?? throw new Exception("来源工艺单版本不存在或已删除，请重新选择");
            if (source.CraftOrderId != postData.SourceCraftOrderId)
                throw new Exception("来源版本与所选工艺单不匹配，请重新选择");

            var nodes = await GetCopyNodeSettingsAsync(postData.SourceVersionId);
            var entity = await CreateCraftVersionAsync(postData.CraftOrderId, source.Remark, nodes);
            transaction.Complete();
            return Ok(new
            {
                Id = entity.Id,
                SequenceNo = entity.SequenceNo,
                Version = entity.Version,
                BomVersionId = entity.BomVersionId,
                IsMain = entity.IsMain,
                Status = entity.Status,
            });
        }

        private async Task<Technical_CraftOrderVersion> CreateCraftVersionAsync(
            long craftOrderId, string? remark, List<NodeSettingTemplateDto> nodes)
        {
            var craftOrder = await _craftOrderService.GetAll()
                .FirstOrDefaultAsync(x => x.Id == craftOrderId && !x.IsDeleted)
                ?? throw new Exception("目标工艺单不存在或已删除，请刷新后重试");

            var maxSequenceNo = await _craftVersionService.GetAll()
                .Where(x => x.CraftOrderId == craftOrder.Id)
                .Select(x => (int?)x.SequenceNo)
                .MaxAsync() ?? 0;

            var hasMainVersion = await _craftVersionService.GetAll()
                .AnyAsync(x => x.CraftOrderId == craftOrder.Id && x.IsMain);

            var sequenceNo = maxSequenceNo + 1;

            // 一个工序单版本对应一个独立的工作流模板，类型固定为“工序单版本”。
            var workflowTemplateId = _workflowTemplateService.Create(
                $"{craftOrder.Code}-{sequenceNo}",
                (int)CraftOrderWorkflowType.工序单版本);

            var entity = new Technical_CraftOrderVersion
            {
                CraftOrderId = craftOrder.Id,
                SequenceNo = sequenceNo,
                Version = $"V{sequenceNo}",
                IsMain = !hasMainVersion,
                WorkflowTemplateId = workflowTemplateId,
                Status = CraftOrderVersionStatus.暂存,
                Remark = remark,
            };

            entity.CraftOrder = craftOrder;

            await _craftVersionService.InsertAsync(entity);
            await _unitOfWork.SaveChangeAsync();

            await SaveNodeSettingsAsync(entity.Id, workflowTemplateId, nodes);
            await _unitOfWork.SaveChangeAsync();
            await SyncMBomAsync(entity);
            await _unitOfWork.SaveChangeAsync();
            return entity;
        }

        private async Task<List<NodeSettingTemplateDto>> GetCopyNodeSettingsAsync(long sourceVersionId)
        {
            var nodes = await _nodeSettingService.GetAll().AsNoTracking()
                .Where(x => x.CraftOrderVersionId == sourceVersionId && !x.IsDeleted)
                .OrderBy(x => x.Sequence).ThenBy(x => x.Id)
                .ToListAsync();
            var nodeIds = nodes.Select(x => x.Id).ToList();
            var machines = await _machineRLService.GetAll().AsNoTracking()
                .Where(x => nodeIds.Contains(x.NodeSettingTemplateId) && !x.IsDeleted)
                .ToListAsync();
            var machinesByNode = machines.ToLookup(x => x.NodeSettingTemplateId);
            var materials = await _materialRLService.GetAll().AsNoTracking()
                .Where(x => nodeIds.Contains(x.NodeSettingTemplateId))
                .OrderBy(x => x.SortNo).ThenBy(x => x.Id)
                .ToListAsync();
            var mainMaterials = materials.Where(x => !x.InsteadSourceId.HasValue).ToList();
            var mainKeys = mainMaterials.Select(x => (x.NodeSettingTemplateId, x.Id)).ToHashSet();
            if (materials.Any(x => x.InsteadSourceId.HasValue
                && !mainKeys.Contains((x.NodeSettingTemplateId, x.InsteadSourceId.Value))))
                throw new Exception("来源版本存在未关联本工序主料的替料，请先修复后复制");
            var materialsByNode = mainMaterials.ToLookup(x => x.NodeSettingTemplateId);
            var substitutes = materials.Where(x => x.InsteadSourceId.HasValue)
                .ToLookup(x => x.InsteadSourceId!.Value);

            // 仅复制业务字段，所有工序、工作流节点、物料及替料主明细 Id 均由保存过程重新生成。
            return nodes.Select(node => new NodeSettingTemplateDto
            {
                Sequence = node.Sequence,
                Code = node.Code,
                Name = node.Name,
                IsOutsource = node.IsOutsource,
                IsInbound = node.IsInbound,
                TheoreticalWorkHours = node.TheoreticalWorkHours,
                IsReportWork = node.IsReportWork,
                Description = node.Description,
                CraftId = node.CraftId,
                MachineIds = machinesByNode[node.Id].Select(x => x.MachineId).ToList(),
                MachineSettings = machinesByNode[node.Id].Select(x => new NodeMachineSettingDto
                {
                    MachineId = x.MachineId,
                    TheoreticalWorkHours = x.TheoreticalWorkHours
                }).ToList(),
                Materials = materialsByNode[node.Id].Select(material =>
                {
                    var copy = CopyNodeMaterial(material);
                    copy.Substitutes = substitutes[material.Id].Select(CopyNodeMaterial).ToList();
                    return copy;
                }).ToList()
            }).ToList();
        }

        private static NodeMaterialRLDto CopyNodeMaterial(Technical_NodeSettingTemplate_Material_RL material)
        {
            return new NodeMaterialRLDto
            {
                DataId = material.DataId,
                DataType = material.DataType,
                IOType = material.IOType,
                Quantity = material.Quantity,
                UnitId = material.UnitId,
                UnitConversionId = material.UnitConversionId,
                UnitList = JsonSerializer.Deserialize<List<InventoryUnitInboundDto>>(
                    string.IsNullOrWhiteSpace(material.UnitListInfo) ? "[]" : material.UnitListInfo,
                    JsonHelper.DefaultJsonSerializerOptions) ?? new(),
                LossRate = material.LossRate,
                SortNo = material.SortNo,
                RefCraftOrderVersionId = material.RefCraftOrderVersionId,
                Remark = material.Remark,
            };
        }

        /// <summary>
        /// 发布工序单版本（暂存 -> 已发布），当前跳过审核，保留主版本选择。
        /// </summary>
        [HttpPost("PublishCraftVersion")]
        public async Task<IActionResult> PublishCraftVersion([FromBody] PublishCraftVersionDto postData)
        {
            if (postData == null || postData.Id <= 0)
            {
                throw new Exception("请选择需要发布的工序单版本");
            }

            var version = await _craftVersionService.GetByIdAsync(postData.Id)
                ?? throw new Exception("工序单版本不存在，请刷新后重试");
            if (version.Status is not (CraftOrderVersionStatus.暂存 or CraftOrderVersionStatus.待审核))
            {
                throw new Exception(version.Status == CraftOrderVersionStatus.已发布
                    ? "该版本已发布，不能重复发布"
                    : "已废弃的版本不能发布");
            }

            var nodes = await _nodeSettingService.GetAll()
                .Where(x => x.CraftOrderVersionId == version.Id)
                .ToListAsync();
            if (nodes.Count == 0)
            {
                throw new Exception("发布前至少需要维护一道工序");
            }
            if (nodes.GroupBy(x => x.Sequence).Any(x => x.Count() > 1))
            {
                throw new Exception("存在重复的工序顺序，请先调整后再发布");
            }
            if (nodes.Any(x => string.IsNullOrWhiteSpace(x.Name)))
            {
                throw new Exception("存在未命名的工序，请先完善后再发布");
            }
            var lastNode = nodes.OrderBy(x => x.Sequence).Last();
            if (!lastNode.IsReportWork || !(lastNode.IsInbound || lastNode.IsOutsource))
            {
                throw new Exception("最后一道工序必须是报工工序且为入库或外协，请检查后发布");
            }

            using var transaction = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);

            version.Status = CraftOrderVersionStatus.已发布;
            await SyncMBomAsync(version);
            await _unitOfWork.SaveChangeAsync();
            transaction.Complete();
            return Ok("发布成功");
        }

        /// <summary>
        /// 废弃工序单版本（已发布 -> 已废弃）。主版本不允许直接废弃。
        /// </summary>
        [HttpPost("ObsoleteCraftVersion")]
        public async Task<IActionResult> ObsoleteCraftVersion([FromBody] IdLongObject postData)
        {
            var id = postData?.Id ?? 0;
            if (id <= 0)
            {
                throw new Exception("请选择需要废弃的工序单版本");
            }

            var version = await _craftVersionService.GetByIdAsync(id)
                ?? throw new Exception("工序单版本不存在，请刷新后重试");
            if (version.Status != CraftOrderVersionStatus.已发布)
            {
                throw new Exception("仅已发布的版本可以废弃");
            }
            if (version.IsMain)
            {
                throw new Exception("主版本不能直接废弃，请先切换主版本后再操作");
            }

            using var transaction = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
            version.Status = CraftOrderVersionStatus.已废弃;
            await SyncMBomMetadataAsync(version);
            await _unitOfWork.SaveChangeAsync();
            transaction.Complete();
            return Ok("废弃成功");
        }

        /// <summary>
        /// 恢复已废弃版本为暂存，编辑后可重新发布。
        /// </summary>
        [HttpPost("RestoreCraftVersion")]
        public async Task<IActionResult> RestoreCraftVersion([FromBody] IdLongObject postData)
        {
            var id = postData?.Id ?? 0;
            if (id <= 0)
            {
                throw new Exception("请选择需要恢复的工序单版本");
            }

            var version = await _craftVersionService.GetByIdAsync(id)
                ?? throw new Exception("工序单版本不存在，请刷新后重试");
            if (version.Status != CraftOrderVersionStatus.已废弃)
            {
                throw new Exception("仅已废弃的版本可以恢复");
            }

            using var transaction = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
            version.Status = CraftOrderVersionStatus.暂存;
            await SyncMBomMetadataAsync(version);
            await _unitOfWork.SaveChangeAsync();
            transaction.Complete();
            return Ok("恢复成功");
        }

        /// <summary>
        /// 清除同工序单下其它版本的主版本标记，分两次保存避免唯一索引冲突。
        /// 返回被清除主标记的旧主版本列表，供调用方按需追加额外操作（如废弃）。
        /// </summary>
        private async Task<List<Technical_CraftOrderVersion>> ClearOtherMainVersionsAsync(long craftOrderId, long excludeVersionId)
        {
            var oldMains = await _craftVersionService.GetAll()
                .Where(x => x.CraftOrderId == craftOrderId && x.IsMain && x.Id != excludeVersionId)
                .ToListAsync();
            foreach (var item in oldMains)
            {
                item.IsMain = false;
            }
            await _unitOfWork.SaveChangeAsync();
            return oldMains;
        }


        /// <summary>
        /// 取消发布（已发布 -> 暂存）：清除主版本标记，允许重新编辑后再发布。
        /// </summary>
        [HttpPost("UnpublishCraftVersion")]
        public async Task<IActionResult> UnpublishCraftVersion([FromBody] IdLongObject postData)
        {
            var id = postData?.Id ?? 0;
            if (id <= 0)
            {
                throw new Exception("请选择需要取消发布的工序单版本");
            }

            var version = await _craftVersionService.GetByIdAsync(id)
                ?? throw new Exception("工序单版本不存在，请刷新后重试");
            if (version.Status != CraftOrderVersionStatus.已发布)
            {
                throw new Exception("仅已发布的版本可以取消发布");
            }

            if(_materialRLService.GetAll().Any(x=>x.RefCraftOrderVersionId == id))
            {
                throw new Exception("该版本已被引用无法取消发布");
            }

            using var transaction = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
            version.Status = CraftOrderVersionStatus.暂存;
            version.IsMain = false;
            await SyncMBomMetadataAsync(version);
            await _unitOfWork.SaveChangeAsync();
            transaction.Complete();
            return Ok("取消发布成功");
        }

        /// <summary>
        /// 删除整个工序单（软删版本、工序、设备，物料明细物理清理）。
        /// </summary>
        [HttpPost("DeleteCraftOrder")]
        public async Task<IActionResult> DeleteCraftOrder([FromBody] IdLongObject postData)
        {
            var id = postData?.Id ?? 0;
            if (id <= 0)
            {
                throw new Exception("请选择需要删除的工序单");
            }

            var craftOrder = await _craftOrderService.GetByIdAsync(id)
                ?? throw new Exception("工序单不存在，请刷新后重试");

            using var transaction = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);

            // 级联软删该工序单下所有版本及其关联数据
            var versions = await _craftVersionService.GetAll()
                .Where(x => x.CraftOrderId == id)
                .ToListAsync();

            foreach (var version in versions)
            {
                if(_materialRLService.GetAll().Any(x => x.RefCraftOrderVersionId == version.Id))
                {
                    throw new Exception("该工序单已经被引用，无法删除");
                }

                // 工序和设备保留历史记录；物料关联没有软删除字段，物理清理。
                var nodeTemplates = await _nodeSettingService.GetAll()
                    .Where(x => x.CraftOrderVersionId == version.Id)
                    .ToListAsync();

                if (nodeTemplates.Count > 0)
                {
                    var nodeIds = nodeTemplates.Select(x => x.Id).ToList();

                    // 软删设备关联
                    var machineRls = await _machineRLService.GetAll()
                        .Where(x => nodeIds.Contains(x.NodeSettingTemplateId))
                        .ToListAsync();
                    foreach (var rl in machineRls)
                    {
                        rl.IsDeleted = true;
                    }

                    // 清理物料关联
                    var materialRls = await _materialRLService.GetAll()
                        .Where(x => nodeIds.Contains(x.NodeSettingTemplateId))
                        .ToListAsync();
                    foreach (var rl in materialRls)
                    {
                        _materialRLService.Delete(rl,false);
                    }

                    DeleteNodeFiles(nodeIds);
                }

                // 软删除的工序仍引用 NodeTemplateId，保留工作流节点及连线。
                foreach (var node in nodeTemplates)
                {
                    node.IsDeleted = true;
                }
                // 软删版本
                version.IsDeleted = true;
                await ArchiveMBomVersionAsync(version);
            }

            // 软删工序单
            craftOrder.IsDeleted = true;
            await _unitOfWork.SaveChangeAsync();

            transaction.Complete();

            return Ok("删除成功");
        }

        /// <summary>
        /// 以工序单版本为根，统一处理其下的工序（新增/更新/软删）与设备；工序先落库拿到真实 Id，再挂设备。
        /// </summary>
        private async Task SaveNodeSettingsAsync(long craftOrderVersionId, long workflowTemplateId, List<NodeSettingTemplateDto> nodeSettings)
        {
            var inputList = nodeSettings ?? new List<NodeSettingTemplateDto>();
            var activeInput = inputList.Where(x => !x.IsDeleted).OrderBy(x => x.Sequence).ToList();
            // 按实际工序顺序处理；第一道工序外协时没有前工序。
            for (var index = 1; index < activeInput.Count; index++)
            {
                if (activeInput[index].IsOutsource)
                {
                    activeInput[index - 1].IsInbound = true;
                }
            }
            foreach (var input in activeInput)
            {
                input.Code = input.Code?.Trim() ?? string.Empty;
                if (!input.IsInbound && !input.IsOutsource)
                {
                    var outputs = (input.Materials ?? new()).Where(row => row.IOType == NodeIOType.输出)
                        .Select(row => row.Id).Where(id => id > 0).ToHashSet();
                    input.Materials = (input.Materials ?? new()).Where(row => row.IOType != NodeIOType.输出
                        && (!row.InsteadSourceId.HasValue || !outputs.Contains(row.InsteadSourceId.Value))).ToList();
                }
                if (input.TheoreticalWorkHours < 0 || input.TheoreticalWorkHours > 99999999.9999m
                    || (input.MachineSettings ?? new()).Any(machine => machine.TheoreticalWorkHours < 0
                        || machine.TheoreticalWorkHours > 99999999.9999m))
                    throw new Exception("理论加工工时必须介于0与99999999.9999小时之间");
                if ((input.MachineSettings ?? new()).GroupBy(machine => machine.MachineId).Any(group => group.Count() > 1)
                    || (input.MachineSettings ?? new()).Any(machine => !(input.MachineIds ?? new()).Contains(machine.MachineId)))
                    throw new Exception("设备工时配置与已选设备不一致，请重新选择");
            }
            if (activeInput.Any(input => input.Code.Length == 0 || input.Code.Length > 50)
                || activeInput.GroupBy(input => input.Code).Any(group => group.Count() > 1))
                throw new Exception("工序编号不能为空、不能重复，且最多50个字符");
            if (activeInput.Any(x => string.IsNullOrWhiteSpace(x.Name)))
            {
                throw new Exception("请填写每道工序的名称后再保存");
            }

            var selectedCraftIds = activeInput
                .Where(x => x.CraftId.HasValue && x.CraftId.Value > 0)
                .Select(x => x.CraftId!.Value)
                .Distinct()
                .ToList();
            if (selectedCraftIds.Count > 0)
            {
                var validCraftIds = await _craftService.GetAll()
                    .Where(x => selectedCraftIds.Contains(x.Id))
                    .Select(x => x.Id)
                    .ToListAsync();
                if (validCraftIds.Count != selectedCraftIds.Count)
                {
                    throw new Exception("所选加工工艺不存在或已失效，请重新选择");
                }
            }

            await ValidateNodeMachinesAsync(activeInput);

            var existing = await _nodeSettingService.GetAll()
                .Where(x => x.CraftOrderVersionId == craftOrderVersionId)
                .ToListAsync();

            // 1) 软删：提交里被标记删除、或本次提交里已不存在的旧工序，一并级联软删其设备
            var keepIds = activeInput.Where(x => x.Id > 0).Select(x => x.Id).ToHashSet();
            var removedNodes = existing.Where(x => !keepIds.Contains(x.Id)).ToList();
            if (removedNodes.Count > 0)
            {
                var removedNodeIds = removedNodes.Select(x => x.Id).ToList();
                var orphanMachines = await _machineRLService.GetAll()
                    .Where(x => removedNodeIds.Contains(x.NodeSettingTemplateId))
                    .ToListAsync();
                foreach (var rl in orphanMachines)
                {
                    _machineRLService.Delete(rl);
                }
                DeleteNodeFiles(removedNodeIds);
                foreach (var node in removedNodes)
                {
                    _nodeSettingService.Delete(node);
                }
            }

            // 2) 新增 / 更新工序头
            var toApply = new List<(Technical_NodeSettingTemplate Node, NodeSettingTemplateDto Input)>();
            foreach (var input in activeInput)
            {
                Technical_NodeSettingTemplate node;
                if (input.Id > 0)
                {
                    node = existing.FirstOrDefault(x => x.Id == input.Id)
                        ?? throw new Exception($"工序【{input.Name ?? input.Code}】不存在，请刷新后重试");
                    node.Sequence = input.Sequence;
                    node.Code = input.Code ?? string.Empty;
                    node.Name = input.Name ?? string.Empty;
                    node.IsOutsource = input.IsOutsource;
                    node.IsInbound = input.IsInbound;
                    node.TheoreticalWorkHours = input.TheoreticalWorkHours;
                    node.IsReportWork = input.IsReportWork;
                    node.Description = input.Description;
                    node.CraftId = input.CraftId;
                    // 兼容历史工序：以前保存过但没有工作流节点模板时，在本次保存补建。
                    if (node.NodeTemplateId <= 0)
                    {
                        node.NodeTemplateId = CreateProcessNodeTemplate(workflowTemplateId, input);
                    }
                }
                else
                {
                    // 先落工作流节点模板，后落业务工序；NodeTemplateId 不接受前端伪造值。
                    var nodeTemplateId = CreateProcessNodeTemplate(workflowTemplateId, input);
                    node = new Technical_NodeSettingTemplate
                    {
                        CraftOrderVersionId = craftOrderVersionId,
                        Sequence = input.Sequence,
                        Code = input.Code ?? string.Empty,
                        Name = input.Name ?? string.Empty,
                        IsOutsource = input.IsOutsource,
                        IsInbound = input.IsInbound,
                        TheoreticalWorkHours = input.TheoreticalWorkHours,
                        IsReportWork = input.IsReportWork,
                        Description = input.Description,
                        NodeTemplateId = nodeTemplateId,
                        CraftId = input.CraftId,
                    };
                    await _nodeSettingService.InsertAsync(node);
                }
                toApply.Add((node, input));
            }

            // 3) 先保存拿到工序真实 Id，再整体替换每道工序的设备
            await _unitOfWork.SaveChangeAsync();
            foreach (var (node, input) in toApply)
            {
                await ReplaceNodeMachinesAsync(node.Id, input.MachineIds ?? new List<long>(), input.MachineSettings ?? new());
                await ReplaceNodeMaterialsAsync(node.Id, input.Materials ?? new List<NodeMaterialRLDto>());
            }
        }

        private async Task ValidateNodeMachinesAsync(List<NodeSettingTemplateDto> nodes)
        {
            var machineIds = nodes.SelectMany(node => node.MachineIds ?? new List<long>()).Distinct().ToList();
            if (machineIds.Count == 0) return;
            var availablePairs = await _machineService.GetAll()
                .Where(machine => machineIds.Contains(machine.Id) && !machine.IsDeleted && !machine.IsStop)
                .SelectMany(machine => machine.CraftRLs)
                .Select(rl => new { rl.MachineId, rl.CraftId })
                .ToListAsync();
            var validPairs = availablePairs.Select(pair => (pair.MachineId, pair.CraftId)).ToHashSet();
            foreach (var node in nodes)
            {
                if ((node.MachineIds ?? new List<long>()).Any(id => id <= 0 || !node.CraftId.HasValue
                    || !validPairs.Contains((id, node.CraftId.Value))))
                {
                    throw new Exception($"工序【{node.Name ?? node.Code}】包含不属于当前工艺、已停用或已删除的机器，请重新选择");
                }
            }
        }

        /// <summary>
        /// 为工序单版本中的一道工序建立对应的工作流节点模板。
        /// 节点类型来自工序单自己的枚举（流程开始=1、工序节点=10、流程结束=99），
        /// 当前业务工序均使用“工序节点”。
        /// </summary>
        private long CreateProcessNodeTemplate(long workflowTemplateId, NodeSettingTemplateDto input)
        {
            if (workflowTemplateId <= 0)
            {
                throw new Exception("工序单版本未关联工作流模板，无法保存工序");
            }

            var nodeTemplate = new NodeTemplateIM
            {
                Name = input.Name?.Trim() ?? string.Empty,
                NodeTypeId = (int)CraftOrderNodeType.节点,
                Type = CraftOrderNodeType.节点.ToString(),
            };

            return _nodeTemplateService.Create(workflowTemplateId, nodeTemplate).Id;
        }

        /// <summary>
        /// 软删某道工序的全部设备（Machine_RL）
        /// </summary>
        private async Task SoftDeleteNodeMachinesAsync(long nodeSettingId)
        {
            var machineRls = await _machineRLService.GetAll()
                .Where(x => x.NodeSettingTemplateId == nodeSettingId)
                .ToListAsync();
            foreach (var rl in machineRls)
            {
                _machineRLService.Delete(rl,false);
            }
        }

        /// <summary>
        /// 整体替换某道工序的设备（先软删旧的，再按 MachineIds 重建）
        /// </summary>
        private async Task ReplaceNodeMachinesAsync(long nodeSettingId, List<long> machineIds, List<NodeMachineSettingDto> settings)
        {
            await SoftDeleteNodeMachinesAsync(nodeSettingId);

            var distinctMachineIds = (machineIds ?? new List<long>()).Where(x => x > 0).Distinct().ToList();
            if (distinctMachineIds.Count == 0)
            {
                return;
            }

            var newRls = distinctMachineIds.Select(machineId => new Technical_NodeSettingTemplate_Machine_RL
            {
                NodeSettingTemplateId = nodeSettingId,
                MachineId = machineId,
                TheoreticalWorkHours = settings.FirstOrDefault(setting => setting.MachineId == machineId)?.TheoreticalWorkHours ?? 0,
            }).ToList();
            await _machineRLService.InsertAsync(newRls);
        }

        /// <summary>
        /// 整体替换工序的领用/产出物料。先保存主物料取得真实 Id，再保存替代料并关联主物料，
        /// 从而保证前端一次提交的嵌套模型能够被完整持久化。
        /// </summary>
        private async Task ReplaceNodeMaterialsAsync(long nodeSettingId, List<NodeMaterialRLDto> materials)
        {
            var allInputs = (materials ?? new List<NodeMaterialRLDto>())
                .Where(x => x.DataId > 0)
                .SelectMany(x => new[] { x }.Concat(x.Substitutes ?? new List<NodeMaterialRLDto>()))
                .ToList();
            var refVersionInputs = allInputs
                .Where(x => x.RefCraftOrderVersionId.HasValue && x.RefCraftOrderVersionId.Value > 0)
                .ToList();
            if (refVersionInputs.Count > 0)
            {
                var refVersionIds = refVersionInputs
                    .Select(x => x.RefCraftOrderVersionId!.Value)
                    .Distinct()
                    .ToList();
                var publishedVersions = await _craftVersionService.GetAll()
                    .Where(x => refVersionIds.Contains(x.Id) && x.Status == CraftOrderVersionStatus.已发布)
                    .Select(x => new { x.Id, x.CraftOrder.ProductId })
                    .ToListAsync();
                var versionProductMap = publishedVersions.ToDictionary(x => x.Id, x => x.ProductId);
                if (refVersionInputs.Any(x => !versionProductMap.TryGetValue(x.RefCraftOrderVersionId!.Value, out var productId)
                    || productId != x.DataId))
                {
                    throw new Exception("引用工艺版本必须是该成品或半成品已发布的版本");
                }
            }

            var existing = await _materialRLService.GetAll()
                .Where(x => x.NodeSettingTemplateId == nodeSettingId)
                .ToListAsync();
            foreach (var item in existing)
            {
                _materialRLService.Delete(item);
            }

            var mainItems = (materials ?? new List<NodeMaterialRLDto>())
                .Where(x => x.DataId > 0 && x.InsteadSourceId == null)
                .ToList();
            var created = new List<(Technical_NodeSettingTemplate_Material_RL Entity, NodeMaterialRLDto Input)>();
            foreach (var input in mainItems)
            {
                var entity = CreateMaterialEntity(nodeSettingId, input, null);
                await _materialRLService.InsertAsync(entity);
                created.Add((entity, input));
            }
            await _unitOfWork.SaveChangeAsync();

            var substituteEntities = new List<Technical_NodeSettingTemplate_Material_RL>();
            foreach (var (main, input) in created)
            {
                foreach (var substitute in input.Substitutes ?? new List<NodeMaterialRLDto>())
                {
                    if (substitute.DataId <= 0)
                    {
                        continue;
                    }
                    substituteEntities.Add(CreateMaterialEntity(nodeSettingId, substitute, main.Id));
                }
            }
            if (substituteEntities.Count > 0)
            {
                await _materialRLService.InsertAsync(substituteEntities);
            }
        }

        private static Technical_NodeSettingTemplate_Material_RL CreateMaterialEntity(
            long nodeSettingId, NodeMaterialRLDto input, long? insteadSourceId)
        {
            return new Technical_NodeSettingTemplate_Material_RL
            {
                NodeSettingTemplateId = nodeSettingId,
                DataId = input.DataId,
                DataType = input.DataType,
                IOType = input.IOType,
                InsteadSourceId = insteadSourceId,
                Quantity = input.Quantity,
                UnitId = input.UnitId,
                UnitConversionId = input.UnitConversionId,
                UnitListInfo = JsonSerializer.Serialize(input.UnitList ?? new List<InventoryUnitInboundDto>(), JsonHelper.DefaultJsonSerializerOptions),
                LossRate = input.LossRate,
                SortNo = input.SortNo,
                RefCraftOrderVersionId = input.RefCraftOrderVersionId,
                Remark = input.Remark,
            };
        }

        /// <summary>删除工序时同步移除以工序 Id 关联的作业指导书及其他工序文件。</summary>
        private void DeleteNodeFiles(IEnumerable<long> nodeIds)
        {
            var ids = nodeIds.Where(id => id > 0).Distinct().ToList();
            if (ids.Count == 0)
            {
                return;
            }
            var files = _fileStorageService.GetAll()
                .Where(file => ids.Contains(file.SourceId)
                    && (file.SourceType == FileStorageSourceType.工序文件
                        || file.SourceType == FileStorageSourceType.工序作业指导书))
                .ToList();
            if (files.Count > 0)
            {
                _fileStorageService.DeleteWihtFiles(files);
            }
        }

        /// <summary>
        /// 删除工序单版本（软删除）。
        /// 暂存和已废弃版本可删除；主版本需先切换后再删；删除时级联软删其下工序与设备。
        /// </summary>
        [HttpPost("DeleteCraftVersion")]
        public async Task<IActionResult> DeleteCraftVersion([FromBody] IdLongObject postData)
        {
            var id = postData?.Id ?? 0;
            if (id <= 0)
            {
                throw new Exception("请选择需要删除的工序单版本");
            }

            var version = await _craftVersionService.GetByIdAsync(id)
                ?? throw new Exception("工序单版本不存在，请刷新后重试");
            if (version.Status is not (CraftOrderVersionStatus.暂存 or CraftOrderVersionStatus.待审核 or CraftOrderVersionStatus.已废弃))
            {
                throw new Exception("仅暂存或已废弃的版本可以删除");
            }
            if (version.IsMain)
            {
                throw new Exception("主版本不能删除，请先切换主版本或删除整个工序单");
            }
            if (await _materialRLService.GetAll().AnyAsync(x => x.RefCraftOrderVersionId == id))
            {
                throw new Exception("该版本已被工序物料引用，不能删除");
            }

            using var transaction = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);

            // 级联软删该版本下的工序及其设备
            var nodeTemplates = await _nodeSettingService.GetAll()
                .Where(x => x.CraftOrderVersionId == version.Id)
                .ToListAsync();
            if (nodeTemplates.Count > 0)
            {
                var nodeIds = nodeTemplates.Select(x => x.Id).ToList();
                var machineRls = await _machineRLService.GetAll()
                    .Where(x => nodeIds.Contains(x.NodeSettingTemplateId))
                    .ToListAsync();
                foreach (var rl in machineRls)
                {
                    rl.IsDeleted = true;
                }
                var materialRls = await _materialRLService.GetAll()
                    .Where(x => nodeIds.Contains(x.NodeSettingTemplateId))
                    .ToListAsync();
                foreach (var rl in materialRls)
                {
                    _materialRLService.Delete(rl, false);
                }
                DeleteNodeFiles(nodeIds);
            }
            foreach (var node in nodeTemplates)
            {
                node.IsDeleted = true;
            }

            // 工序和版本是软删除，仍保留模板外键；工作流节点、连线及模板一并保留。
            version.IsDeleted = true;
            await ArchiveMBomVersionAsync(version);
            await _unitOfWork.SaveChangeAsync();
            transaction.Complete();
            return Ok("删除成功");
        }

        #endregion

        #region 工序管理


        #endregion

        #region 制造BOM同步

        // 必须在工艺版本保存的同一个事务内调用，使用已落库的工序物料作为唯一数据源。
        private async Task SyncMBomAsync(Technical_CraftOrderVersion craftVersion)
        {
            var craftOrder = await _craftOrderService.GetByIdAsync(craftVersion.CraftOrderId)
                ?? throw new Exception("工艺单不存在，无法同步制造BOM");
            Production_MBom bom;
            Production_MBom_Version bomVersion;
            // 先检查是否有对应物料BOM
            if (craftVersion.BomVersionId.HasValue)
            {
                bomVersion = await _mBomVersionService.GetByIdAsync(craftVersion.BomVersionId.Value)
                    ?? throw new Exception("关联的制造BOM版本不存在，请检查版本关联");
                bom = await _mBomService.GetByIdAsync(bomVersion.MBomId)
                    ?? throw new Exception("关联的制造BOM不存在");
                if (bomVersion.IsDeleted || bom.IsDeleted || bom.ProductId != craftOrder.ProductId
                    || bom.ProductSpecificationId != craftOrder.SpecificationId)
                    throw new Exception("制造BOM与当前工艺产品/规格不匹配或已停用");
            }
            // 没有就新建
            else
            {
                var matching = await _mBomService.GetAll()
                    .Where(x => x.ProductId == craftOrder.ProductId
                        && x.ProductSpecificationId == craftOrder.SpecificationId)
                    .Take(2).ToListAsync();
                if (matching.Count > 1) throw new Exception("当前产品/规格存在多个制造BOM，请先整理重复数据");
                // bom可能存在 新增版本
                bom = matching.SingleOrDefault() ?? new Production_MBom
                {
                    ProductId = craftOrder.ProductId,
                    ProductSpecificationId = craftOrder.SpecificationId,
                };
                if (bom.Id == 0)
                {
                    await _mBomService.InsertAsync(bom);
                    await _unitOfWork.SaveChangeAsync();
                }
                bomVersion = new Production_MBom_Version { MBomId = bom.Id, Version = craftVersion.Version, UnitInfo = "[]" };
                await _mBomVersionService.InsertAsync(bomVersion);
                await _unitOfWork.SaveChangeAsync();
                craftVersion.BomVersionId = bomVersion.Id;
            }

            var units = _productService.GetProductUnitList(new List<ProductIdDto>
            {
                new()
                {
                    ProductId = craftOrder.ProductId,
                    SpecificationId = craftOrder.SpecificationId,
                    IdField = craftOrder.SpecificationId.HasValue
                        ? $"{craftOrder.ProductId}_{craftOrder.SpecificationId}" : craftOrder.ProductId.ToString()
                }
            });
            var unitList = units.SingleOrDefault(x => x.ProductId == craftOrder.ProductId
                && x.SpecificationId == craftOrder.SpecificationId)?.UnitList;
            if (unitList == null || unitList.Count == 0)
                throw new Exception("工艺产品/规格未配置单位，无法同步制造BOM");

            // Code 是转换 ID，Value 已是相对于当前产品/规格默认单位的比例，不能再次取倒数。
            // 一默认单位产出对应的各单位数量 = 1 / Ratio，包装按转换 ID 分别保留。
            var outputUnits = new List<InventoryUnitInboundDto>();
            foreach (var unit in unitList)
            {
                if (!long.TryParse(unit.Code, out var conversionId) || (conversionId != -1 && conversionId <= 0)
                    || !unit.UnitId.HasValue || unit.UnitId.Value <= 0
                    || (!decimal.TryParse(unit.Value, NumberStyles.Float, CultureInfo.CurrentCulture, out var ratio)
                        && !decimal.TryParse(unit.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out ratio))
                    || ratio <= 0 || outputUnits.Any(x => x.Id == conversionId))
                    throw new Exception($"工艺单位【{unit.Text}】缺少有效转换关系或转换ID重复，请检查单位配置");
                outputUnits.Add(new InventoryUnitInboundDto
                {
                    Id = conversionId,
                    UnitId = unit.UnitId.Value,
                    UnitName = unit.Text ?? string.Empty,
                    Ratio = ratio,
                    Quantity = 1m / ratio
                });
            }
            var defaultUnit = outputUnits.SingleOrDefault(x => x.Id == -1);
            if (defaultUnit == null || defaultUnit.Ratio != 1m)
                throw new Exception("工艺产品/规格缺少有效的默认单位（转换ID=-1，比例=1）");
            if (craftOrder.SpecificationId.HasValue && !unitList.Any(x => x.IsMainUnit))
                throw new Exception("工艺规格缺少到产品主单位的有效转换关系");

            bomVersion.TotalQuantity = 1m;
            bomVersion.UnitConversionId = defaultUnit.Id;
            bomVersion.UnitInfo = JsonSerializer.Serialize(outputUnits, JsonHelper.DefaultJsonSerializerOptions);
            ApplyMBomMetadata(craftVersion, bomVersion);

            // 物料处理
            var nodes = await _nodeSettingService.GetAll()
                .Where(x => x.CraftOrderVersionId == craftVersion.Id).ToListAsync();
            var nodeSequence = nodes.ToDictionary(x => x.Id, x => x.Sequence);
            var nodeIds = nodeSequence.Keys.ToList();
            var sourceMaterials = await _materialRLService.GetAll()
                .Where(x => nodeIds.Contains(x.NodeSettingTemplateId) && x.IOType == NodeIOType.输入).ToListAsync();
            var substitutes = sourceMaterials.Where(x => x.InsteadSourceId.HasValue)
                .ToLookup(x => x.InsteadSourceId!.Value);
            // 暂存物料bom信息
            var totals = new Dictionary<(BomDetailType Type, long Id), MBomMaterialTotal>();
            // 暂存物料信息
            var dataInfoCache = new Dictionary<(InventoryDataType Type, long Id), DataInfo>();
            foreach (var material in sourceMaterials.Where(x => x.InsteadSourceId == null)
                .OrderBy(x => nodeSequence[x.NodeSettingTemplateId]).ThenBy(x => x.SortNo).ThenBy(x => x.Id))
            {
                // 转换为Bom信息
                var main = ReadMBomMaterial(material, dataInfoCache);
                var key = (main.Type, main.Id);
                if (!totals.TryGetValue(key, out var total)) totals.Add(key, total = main);
                else total.Quantity += main.Quantity;
                foreach (var substitute in substitutes[material.Id].OrderBy(x => x.SortNo).ThenBy(x => x.Id))
                {
                    var replacement = ReadMBomMaterial(substitute, dataInfoCache);
                    var replacementKey = (replacement.Type, replacement.Id);
                    if (replacementKey == key) throw new Exception("BOM替料不能与对应主料相同");
                    if (total.Substitutes.TryGetValue(replacementKey, out var existingReplacement))
                        existingReplacement.Quantity += replacement.Quantity;
                    else total.Substitutes.Add(replacementKey, replacement);
                }
            }

            // 相同物料保留原明细Id；删除已移除的物料，防止反复保存产生重复明细或失效替料关联。
            var existing = await _mBomDetailService.GetAll().Where(x => x.VersionId == bomVersion.Id).ToListAsync();
            var keptIds = new HashSet<long>();
            var mainDetails = new List<(Production_MBom_Detail Detail, MBomMaterialTotal Total)>();
            var order = 0;
            // 统计物料已有Bom
            var versionIds = totals.Values
                .Select(x => x.CraftVersionId)
                .Distinct()
                .ToList();

            var bomIds = await (
                from craftversion in _craftVersionService.GetAll()
                where versionIds.Contains(craftversion.Id)
                join bomversion in _mBomVersionService.GetAll()
                    on craftversion.BomVersionId equals bomversion.Id
                select new IdDTO
                {
                    CraftVersionId = craftversion.Id,
                    BomVersionId = bomversion.Id,
                    BomId = bomversion.MBomId
                }
            ).ToListAsync();
            foreach (var total in totals.Values)
            {
                var detail = existing.FirstOrDefault(x => !x.IsSubstitute && x.DetailType == total.Type && x.DetailId == total.Id)
                    ?? new Production_MBom_Detail { MBomId = bom.Id, VersionId = bomVersion.Id, UnitInfo = "[]" };
                ApplyMBomMaterial(detail, total, order++, null,bomIds);
                if (detail.Id == 0) await _mBomDetailService.InsertAsync(detail);
                // 记录主物料Bom 为替料新增
                mainDetails.Add((detail, total));
            }
            await _unitOfWork.SaveChangeAsync(); // 替料必须关联BOM主明细Id，而非工序物料Id。
            foreach (var (main, total) in mainDetails)
            {
                keptIds.Add(main.Id);
                foreach (var replacement in total.Substitutes.Values)
                {
                    var detail = existing.FirstOrDefault(x => x.IsSubstitute && x.MainDetailId == main.Id
                        && x.DetailType == replacement.Type && x.DetailId == replacement.Id)
                        ?? new Production_MBom_Detail { MBomId = bom.Id, VersionId = bomVersion.Id, UnitInfo = "[]" };
                    ApplyMBomMaterial(detail, replacement, order++, main.Id, bomIds);
                    if (detail.Id == 0) await _mBomDetailService.InsertAsync(detail);
                    else keptIds.Add(detail.Id);
                }
            }
            foreach (var obsolete in existing.Where(x => !keptIds.Contains(x.Id)).OrderByDescending(x => x.IsSubstitute))
                _mBomDetailService.Delete(obsolete,false);
            await _unitOfWork.SaveChangeAsync();
            await RefreshMBomAsync(bom, bomVersion);
        }

        private sealed class MBomMaterialTotal
        {
            public BomDetailType Type { get; init; }
            public long Id { get; init; }
            public long UnitId { get; init; }
            public string UnitName { get; init; } = "";
            public decimal Quantity { get; set; }
            public long? CraftVersionId { get; set; }
            public Dictionary<(BomDetailType Type, long Id), MBomMaterialTotal> Substitutes { get; } = new();
        }

        private sealed class IdDTO
        {
            public long BomId { get; set; }
            public long BomVersionId { get; set; }
            public long CraftVersionId { get; set; }

        }

        private MBomMaterialTotal ReadMBomMaterial(Technical_NodeSettingTemplate_Material_RL material,
            Dictionary<(InventoryDataType Type, long Id), DataInfo> cache)
        {
            var type = material.DataType switch
            {
                InventoryDataType.Product => BomDetailType.Product,
                InventoryDataType.Specification => BomDetailType.Specication,
                _ => throw new Exception("制造BOM仅支持工序领用的产品或规格物料"),
            };
            var key = (material.DataType, material.DataId);
            if (!cache.TryGetValue(key, out var info))
            {
                info = _utils.GetDataInfo(material.DataType, material.DataId)
                    ?? throw new Exception($"BOM物料 {material.DataId} 不存在");
                cache.Add(key, info);
            }
            var baseUnitId = info.DataBaseUnitId ?? info.DataUnitId ?? 0;
            var unitId = material.UnitId > 0 ? material.UnitId : info.DataUnitId ?? 0;
            if (baseUnitId <= 0 || unitId <= 0 || material.Quantity < 0) throw new Exception("BOM物料单位未配置或用量为负数");
            var ratio = 1m;
            if (unitId != baseUnitId)
            {
                var units = JsonSerializer.Deserialize<List<InventoryUnitInboundDto>>(
                    string.IsNullOrWhiteSpace(material.UnitListInfo) ? "[]" : material.UnitListInfo,
                    JsonHelper.DefaultJsonSerializerOptions) ?? new();
                ratio = units.FirstOrDefault(x => x.Id == material.UnitConversionId && x.UnitId == unitId)?.Ratio
                    ?? (unitId == info.DataUnitId && (material.UnitConversionId == 0 || material.UnitConversionId == info.ToBaseUnitConversionId)
                        ? info.ToBaseUnitRatio : null) ?? 0;
                if (ratio <= 0) throw new Exception($"BOM物料【{info.DataName}】缺少到基本单位的有效转换关系");
            }
            return new MBomMaterialTotal
            {
                Type = type,
                Id = material.DataId,
                UnitId = baseUnitId,
                UnitName = info.DataBaseUnitName ?? info.DataUnit ?? "",
                Quantity = material.Quantity * ratio,
                CraftVersionId = material.RefCraftOrderVersionId
            };
        }

        private static void ApplyMBomMaterial(Production_MBom_Detail detail, MBomMaterialTotal total, int order, long? mainId,List<IdDTO>? bomIds)
        {
            var quantity = Math.Round(total.Quantity, 4, MidpointRounding.AwayFromZero);
            if (quantity > 99999999.9999m || (total.Quantity > 0 && quantity == 0))
                throw new Exception("BOM汇总用量超出decimal(12,4)范围，请调整工序物料用量");
            var bomInfo = bomIds?.FirstOrDefault(x => x.CraftVersionId == total.CraftVersionId);
            if(bomInfo != null)
            {
                detail.DetailType = BomDetailType.Bom;
                detail.DetailId = bomInfo.BomVersionId;
            }
            else
            {
                detail.DetailType = total.Type;
                detail.DetailId = total.Id;
            }
            detail.Quantity = quantity;
            detail.Ratio = quantity; // 一单位产出的耗用系数，不能对混合单位物料计算重量百分比。
            detail.UnitConversionId = 0;
            detail.UnitInfo = JsonSerializer.Serialize(new List<InventoryUnitInboundDto>
            {
                new() { Id = 0, UnitId = total.UnitId, UnitName = total.UnitName, Ratio = 1m, Quantity = quantity }
            }, JsonHelper.DefaultJsonSerializerOptions);
            detail.Order = order;
            detail.IsSubstitute = mainId.HasValue;
            detail.MainDetailId = mainId;
        }

        private static void ApplyMBomMetadata(Technical_CraftOrderVersion craft, Production_MBom_Version bom)
        {
            bom.Version = craft.Version;
            bom.Remark = craft.Remark;
            bom.IsMain = craft.IsMain;
            bom.Status = craft.Status switch
            {
                CraftOrderVersionStatus.暂存 or CraftOrderVersionStatus.待审核 => BomStatus.待审核,
                CraftOrderVersionStatus.已发布 => BomStatus.已生效,
                CraftOrderVersionStatus.已废弃 => BomStatus.淘汰,
                _ => throw new Exception("无法识别工艺版本状态"),
            };
        }

        private async Task SyncMBomMetadataAsync(Technical_CraftOrderVersion craft)
        {
            if (!craft.BomVersionId.HasValue) return; // 历史未关联版本在首次保存或发布时补建。
            var version = await _mBomVersionService.GetByIdAsync(craft.BomVersionId.Value)
                ?? throw new Exception("关联的制造BOM版本不存在");
            var bom = await _mBomService.GetByIdAsync(version.MBomId) ?? throw new Exception("关联的制造BOM不存在");
            ApplyMBomMetadata(craft, version);
            await RefreshMBomAsync(bom, version);
        }

        private async Task ArchiveMBomVersionAsync(Technical_CraftOrderVersion craft)
        {
            if (!craft.BomVersionId.HasValue) return;
            var version = await _mBomVersionService.GetByIdAsync(craft.BomVersionId.Value)
                ?? throw new Exception("关联的制造BOM版本不存在");
            var bom = await _mBomService.GetByIdAsync(version.MBomId) ?? throw new Exception("关联的制造BOM不存在");
            version.IsDeleted = true;
            version.IsMain = false;
            version.Status = BomStatus.淘汰;
            await RefreshMBomAsync(bom, version); // 保留历史BOM明细，只停用对应版本。
        }

        private async Task RefreshMBomAsync(Production_MBom bom, Production_MBom_Version changed)
        {
            var versions = (await _mBomVersionService.GetAll().Where(x => x.MBomId == bom.Id).ToListAsync())
                .Where(x => !x.IsDeleted).ToList();
            if (changed.IsMain) foreach (var other in versions.Where(x => x.Id != changed.Id)) other.IsMain = false;
            bom.IsDeleted = versions.Count == 0;
            bom.Status = versions.Any(x => x.Status == BomStatus.已生效) ? BomStatus.已生效
                : versions.Any(x => x.Status == BomStatus.待审核) ? BomStatus.待审核 : BomStatus.淘汰;
            var versionIds = versions.Select(x => x.Id).ToList();
            var mainVersion = versions.SingleOrDefault(x => x.IsMain);
            var details = await _mBomDetailService.GetAll().Where(x => versionIds.Contains(x.VersionId)).ToListAsync();
            var baseline = details.Where(x => x.VersionId == mainVersion?.Id && !x.IsSubstitute)
                .GroupBy(x => (x.DetailType, x.DetailId)).ToDictionary(x => x.Key, x => x.First());
            foreach (var detail in details)
            {
                if (detail.IsSubstitute || !baseline.TryGetValue((detail.DetailType, detail.DetailId), out var main))
                {
                    detail.QuantityDiff2MainVersion = null;
                    detail.RatioDiff2MainVersion = null;
                }
                else
                {
                    detail.QuantityDiff2MainVersion = detail.Quantity - main.Quantity;
                    detail.RatioDiff2MainVersion = detail.Ratio - main.Ratio;
                }
            }
        }

        #endregion
    }
}
