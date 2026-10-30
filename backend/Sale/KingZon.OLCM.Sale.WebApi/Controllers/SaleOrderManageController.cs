using Aspose.Cells;
using Kingzon.OLCM.Base.Services;
using Kingzon.OLCM.Base.Services.Dto;
using Kingzon.OLCM.Base.Services.JsonStructure;
using Kingzon.OLCM.Common.Services;
using KingZon.Core;
using KingZon.Core.DataBase;
using KingZon.Core.Security;
using KingZon.Core.Web;
using KingZon.OLCM.Base.Service;
using KingZon.OLCM.Base.Service.Dto;
using KingZon.OLCM.Base.Shared.DataBase.Entities;
using KingZon.OLCM.Base.Shared.DataBase.Enum;
using KingZon.OLCM.Common.Service.Dto;
using KingZon.OLCM.Inventory.Service;
using KingZon.OLCM.Inventory.Service.Dto;
using KingZon.OLCM.Inventory.Shared.DataBase.Entities;
using KingZon.OLCM.Inventory.Shared.DataBase.Enum;
using KingZon.OLCM.Production.Shared.DataBase.Entities;
using KingZon.OLCM.Production.Shared.DataBase.Enum;
using KingZon.OLCM.Production.Shared.DataBase.JsonStructure;
using KingZon.OLCM.Sale.Service.Dto;
using KingZon.OLCM.Sale.Shared.DataBase.Entities;
using KingZon.OLCM.Sale.Shared.DataBase.Enum;
using KingZon.OLCM.Sale.Shared.DataBase.JsonStructure;
using KingZon.OLCM.Sale.WebApi.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Transactions;

namespace KingZon.OLCM.Sale.WebApi.Controllers
{

    [Route("api/sale/[controller]")]
    [ApiController]
    public class SaleOrderManageController : KingZonControllerBase
    {
        private readonly SystemParaService _systemParaService;
        private readonly CommonService<Sale_Order> _saleOrderService;
        private readonly CommonService<Sale_OrderDetail> _saleOrderDetailService;
        private readonly CommonService<Base_Goods> _goodsService;
        private readonly ProductService _productService;
        private readonly CommonService<Base_Partner> _customerService;
        private readonly CommonService<Base_ProductPackage> _packageServie;
        private readonly CommonService<Base_ProductSpecification> _specificationService;
        private readonly SequenceService _sequenceService;
        private readonly CommonService<Base_UnitConversion> _unitConversionService;
        private readonly CommonService<Production_Dispatch_Order> _productionService;
        private readonly CommonService<Production_Dispatch_SaleRL> _productionSaleRLService;
        private readonly CommonService<Inventory_Record_Production_Ext> _inventoryProductionService;
        private readonly CommonService<Inventory_DeliveryOrder> _deliveryOrderService;
        private readonly CommonService<Inventory_DeliveryOrderDetail> _deliveryOrderDetailService;
        private readonly CommonService<Inventory_Occupy> _occupyService;
        private readonly CommonService<Inventory_OccupyDetail> _occupyDetailService;
        private readonly InventoryService _inventoryService;
        private readonly Utils _utils;
        private readonly IUnitOfWork _unitOfWork;

        public SaleOrderManageController(SystemParaService systemParaService,
            CommonService<Sale_Order> saleOrderService,
            CommonService<Sale_OrderDetail> saleOrderDetailService,
            CommonService<Base_Goods> goodsService,
            ProductService productService,
            CommonService<Base_Partner> customerService,
            CommonService<Base_ProductPackage> packageServie,
            CommonService<Base_ProductSpecification> specificationService,
            SequenceService sequenceService,
            CommonService<Base_UnitConversion> unitConversionService,
            CommonService<Production_Dispatch_Order> productionService,
            CommonService<Production_Dispatch_SaleRL> productionSaleRLService,
            CommonService<Inventory_Record_Production_Ext> inventoryProductionService,
            CommonService<Inventory_DeliveryOrder> deliveryOrderService,
            CommonService<Inventory_DeliveryOrderDetail> deliveryOrderDetailService,
            CommonService<Inventory_Occupy> occupyService,
            CommonService<Inventory_OccupyDetail> occupyDetailService,
            InventoryService inventoryService,
            Utils utils,
            IUnitOfWork unitOfWork)
        {
            _systemParaService = systemParaService;
            _saleOrderService = saleOrderService;
            _saleOrderDetailService = saleOrderDetailService;
            _goodsService = goodsService;
            _productService = productService;
            _customerService = customerService;
            _packageServie = packageServie;
            _specificationService = specificationService;
            _sequenceService = sequenceService;
            _unitConversionService = unitConversionService;
            _productionService = productionService;
            _productionSaleRLService = productionSaleRLService;
            _inventoryProductionService = inventoryProductionService;
            _deliveryOrderService = deliveryOrderService;
            _deliveryOrderDetailService = deliveryOrderDetailService;
            _occupyService = occupyService;
            _occupyDetailService = occupyDetailService;
            _inventoryService = inventoryService;
            _utils = utils;
            _unitOfWork = unitOfWork;
        }

        /// <summary>
        /// 获取订单列表
        /// </summary>
        /// <returns></returns>
        [HttpPost("GetOrderList", Name = "GetOrderList")]
        public async Task<IActionResult> GetOrderList([FromBody] List<Filter> SaleOrderDetailFilterList, [FromQuery] bool? showPage = true)
        {
            var option = JsonHelper.DefaultJsonSerializerOptions;
            if (this.SortInfoList.Count <= 0)
            {
                this.SortInfoList.Add(new SortInfo
                {
                    Field = "OrderId",
                    Order = "desc"
                });
            }
            var query = _saleOrderDetailService.GetAll().Include(x => x.Product).Include(x => x.ProductSpecification).Include(x => x.SaleOrder)
                            .ApplyFilters(SaleOrderDetailFilterList);
#if !DEBUG
            //如果是超级管理员 返回所有客户的订单
            if (!this.CurrentUser.Roles.Any(x => x.RoleCode == Common.DataBase.Enum.RoleCodeEnum.superAdmin.ToString()))
            {
                var userId = this.CurrentUser.Id;
                var customerIds = _customerService.GetAll().Where(x => (x.SalesmanIds != null && MySqlJsonDbFunctionsExtensions.JsonContains(
                                        EF.Functions, x.SalesmanIds, userId)) || x.SalesmanIds == null).Select(x => x.Id);
                var specialPermission = this.CurrentUser.SpecialDataPermission;
                //如果用户(业务员)有查看所有客户的特殊权限 则不进行客户过滤，
                //否则只返回配置给该用户的客户 以及未指定业务员的客户 的订单
                if (!specialPermission.HasFlag(SpecialDataPermission.CustomerAll))
                {
                    query = query.Where(x => customerIds.Contains(x.SaleOrder.CustomerId));
                }
            }
#endif
            var saleData = query
                //_saleOrderDetailService.GetAll().Include(x => x.Product).Include(x => x.ProductSpecification).ApplyFilters(SaleOrderDetailFilterList)
                .Select(x => new SaleOrderDetailFlatDTO
                {
                    Id = x.Id,
                    OrderId = x.SaleOrderId,
                    OrderNo = x.SaleOrder.OrderNo,
                    OrderType = x.SaleOrder.OrderType,
                    DeliveryTime = x.DeliveryTime,
                    Remark = x.Remark,
                    OrderRemark = x.SaleOrder.Remark,
                    ReceiveUserId = x.SaleOrder.ReceiveUserId,
                    ReceiveName = x.SaleOrder.ReceiveUser.Name,
                    ReceiveOrderTime = x.SaleOrder.ReceiveOrderTime,
                    IsUrgent = x.IsUrgent,
                    Status = x.Status,
                    OrderStatus = x.SaleOrder.Status,
                    ReviewStatus = x.ReviewStatus,
                    OrderExamineStatus = x.SaleOrder.ExamineStatus,
                    OrderReviewStatus = x.SaleOrder.ReviewStatus,
                    CustomerId = x.SaleOrder.CustomerId,
                    CustomerCode = x.SaleOrder.Customer.CustomerCode,
                    CustomerName = x.SaleOrder.Customer.CustomerName,
                    CustomerOrderNo = x.SaleOrder.CustomerOrderNo,
                    CustomerMaterialNo = x.CustomerMaterialNo,
                    CustomerMaterialName = x.CustomerMaterialName,
                    CustomerAddress = x.SaleOrder.CustomerAddress != null ? JsonSerializer.Deserialize<CustomerAddressDto>(x.SaleOrder.CustomerAddress, option) : null,
                    GoodsId = x.GoodsId,
                    ProductId = x.ProductId,
                    SpecificationId = x.SpecificationId,
                    ProductNo = x.GoodsNo,
                    ProductName = x.GoodsName,
                    ProductSpecification = x.Specification,
                    ProductTechnicalCode = x.TechnicalCode,
                    PackageSpecification = x.PackageSpecification,
                    ProductUnit = x.ProductSpecification != null ? (x.ProductSpecification.SpecificationUnit != null ? x.ProductSpecification.SpecificationUnit.Name : null) : x.Product.BaseUnit.Name,
                    PlaceOrderQuantity = x.PlaceOrderQuantity,
                    Price = x.Price,
                    TotalPrice = x.TotalPrice,
                    OrderTotalPrice = x.SaleOrder.OrderTotalPrice,
                    SalesUnit = x.SalesUnit,
                    UnitConversionList = JsonSerializer.Deserialize<List<SaleUnitConversionInfo>>(x.UnitConversionInfo ?? "[]", option),
                    UnitConversionInfo = x.UnitConversionInfo,
                    StockQuantity = x.ProductionInventoryQuantity + x.PurchaseInventoryQuantity,
                    PlanProductionQuantity = x.PlanProductionQuantity,
                    PlanPurchaseQuantity = x.PlanPurchaseQuantity,
                    PlanDirectDliveryQuantity = x.PlanDirectDliveryQuantity,
                    ProcessQuantity = x.ProductionQuantity - x.ProductionInventoryQuantity,
                    TransitQuantity = x.PurchaseQuantity - x.PurchaseInventoryQuantity,
                    ProductionQuantity = x.ProductionQuantity,
                    ProductionInventoryQuantity = x.ProductionInventoryQuantity,
                    PurchaseQuantity = x.PurchaseQuantity,
                    PurchaseInventoryQuantity = x.PurchaseInventoryQuantity,
                    DeliveryNoticeQuantity = x.DeliveryNoticeQuantity,
                    DeliveryQuantity = x.DeliveryQuantity,
                    ProductUnitId = x.SpecificationId != null ? (x.ProductSpecification != null ? x.ProductSpecification.SpecificationUnitId : null) : x.Product.BaseUnitId,
                    SalesUnitId = x.SalesUnitId,
                    InputProductionList = JsonSerializer.Deserialize<List<InputProductionDto>>(x.InputProductionInfo ?? "[]", option),
                    InputDeliveryList = JsonSerializer.Deserialize<List<InputDeliveryDto>>(x.InputDeliveryInfo ?? "[]", option),
                    TotalInputDeliveryQty = x.TotalInputDeliveryQty,
                    TotalInputProductionQty = x.TotalInputProductionQty,
                    CustomSaleAttributeList = x.SaleOrder.CustomSaleAttribute != null ?
                            JsonSerializer.Deserialize<List<CustomAttribute>>(x.SaleOrder.CustomSaleAttribute ?? "[]", option) ?? new List<CustomAttribute>() :
                            new List<CustomAttribute>(),
                    SalesUnitConversionId = x.SalesUnitConversionId,
                }).ApplySorts(this.SortInfoList);
            var list = new List<SaleOrderDetailFlatDTO>();
            var data = await saleData.ToPagedListAsync(this.PageModel);
            //不分页时
            if (showPage.HasValue && !showPage.Value)
            {
                list = await saleData.ToListAsync();
            }
            else
            {
                list = data.Data.ToList();
            }
            var saleOrderDetailIds = list.Select(x => x.Id).ToList();
            var dispatchOrderData = _productionSaleRLService.GetAll().Include(x => x.DispatchOrder)
                                    .Where(x => saleOrderDetailIds.Contains(x.SaleOrderDetailId)).ToList();
            foreach (var x in list)
            {
                var ratio = getSaleToBaseUnitRatio(x.SalesUnitId, x.UnitConversionInfo, x.ProductUnitId);
                x.PlanProductionQtyByMainUnit = _utils.formatDigital4(x.PlanProductionQuantity * ratio);
                x.PlanPurchaseQtyByMainUnit = _utils.formatDigital4(x.PlanPurchaseQuantity * ratio);
                x.PlanDirectDliveryQtyByMainUnit = _utils.formatDigital4(x.PlanDirectDliveryQuantity * ratio);
                var PlaceOrderQtyByMainUnit = x.PlaceOrderQuantity * ratio;
                x.PlaceOrderQtyByMainUnit = PlaceOrderQtyByMainUnit;
                x.DeliveryNoticeQtyByMainUnit = x.DeliveryNoticeQuantity;
                x.DeliveryQtyByMainUnit = x.DeliveryQuantity;
                x.UnDeliveryQtyByMainUnit = _utils.formatDigital4(PlaceOrderQtyByMainUnit - x.DeliveryQtyByMainUnit);
                x.DeliveryQuantity = ratio != 0 ? Math.Round(x.DeliveryQuantity / ratio, 4, MidpointRounding.AwayFromZero) : 0; //送货数 售卖单位
                x.DeliveryNoticeQuantity = ratio != 0 ? Math.Round(x.DeliveryNoticeQuantity / ratio, 4, MidpointRounding.AwayFromZero) : 0; //送货通知数 售卖单位
                x.UnDeliveryQuantity = x.PlaceOrderQuantity - x.DeliveryQuantity; //差欠数 售卖单位
                x.DeliveryRate = PlaceOrderQtyByMainUnit > 0 ? Math.Round(x.DeliveryQtyByMainUnit / PlaceOrderQtyByMainUnit, 4, MidpointRounding.AwayFromZero) : 0;
                //待入库 售卖单位 
                x.UnStockQuantity = ratio != 0 ? Math.Round((x.PlaceOrderQtyByMainUnit - x.StockQuantity) / ratio, 4, MidpointRounding.AwayFromZero) : 0; ;
                x.SaleToBaseRatio = ratio;

                //手动关联的派工单
                var dispatchOrders = dispatchOrderData.Where(x => x.SaleOrderDetailId == x.Id).ToList();
                //总派工数
                x.DispatchedQtyByMainUnit = dispatchOrders.Sum(x => x.Quantity);
                x.DispatchedQuantity = ratio != 0 ? Math.Round(x.DispatchedQtyByMainUnit / ratio, 4, MidpointRounding.AwayFromZero) : 0;
                //派工已入库数
                x.InStockQtyByMainUnit = dispatchOrders.Sum(x => x.InboundQuantity);
                x.InStockQuantity = ratio != 0 ? Math.Round(x.InStockQtyByMainUnit / ratio, 4, MidpointRounding.AwayFromZero) : 0;
                //派工待入库数 = 派工数 - 已入库数
                x.DispatchingQtyByMainUnit = dispatchOrders.Where(x => x.DispatchOrder.Status == DispatchOrderStatus.进行中 || x.DispatchOrder.Status == DispatchOrderStatus.已暂停)
                                        .Sum(x => x.Quantity - x.InboundQuantity);
                x.DispatchingQuantity = ratio != 0 ? Math.Round(x.DispatchingQtyByMainUnit / ratio, 4, MidpointRounding.AwayFromZero) : 0;
                //派工差欠数（待派工）
                x.UnDispatchQtyByMainUnit = x.PlanProductionQtyByMainUnit - x.InStockQtyByMainUnit - x.DispatchingQtyByMainUnit;
                x.UnDispatchQuantity = ratio != 0 ? Math.Round(x.UnDispatchQtyByMainUnit / ratio, 4, MidpointRounding.AwayFromZero) : 0;
            }

            //不分页时
            if (showPage.HasValue && !showPage.Value)
            {
                return Ok(list);
            }
            data.Data = list;
            return Ok(data);
        }

        /// <summary>
        /// 获取订单扩展参数 和订单明细扩展参数
        /// </summary>
        /// <returns></returns>
        [HttpGet("GetSaleExtentionSystemPara", Name = "GetSaleExtentionSystemPara")]
        public async Task<IActionResult> GetSaleExtentionSystemPara()
        {
            var systemParaData = new List<SystemParaTreeDto>();
            await Task.Run(() =>
            {
                systemParaData = _systemParaService.GetSystermParaByTypesAndCode(new List<SystemParaType> { SystemParaType.销售订单扩展参数, SystemParaType.销售明细扩展参数 }, null);
            });
            var saleOrderExtentionData = systemParaData.Where(x => x.SystemPara.DataType == SystemParaType.销售订单扩展参数)
                   .Select(x => new SelectionDto
                   {
                       Value = x.SystemPara.Id.ToString(),
                       Text = x.SystemPara.DataValue,
                       Code = x.SystemPara.DataValue,
                   }).ToList();
            var saleOrderDetailExtentionData = systemParaData.Where(x => x.SystemPara.DataType == SystemParaType.销售明细扩展参数)
                 .Select(x => new SelectionDto
                 {
                     Value = x.SystemPara.Id.ToString(),
                     Text = x.SystemPara.DataValue,
                     Code = x.SystemPara.DataValue,
                 }).ToList();

            var res = new
            {
                OrderExtention = saleOrderExtentionData,
                DetailExtention = saleOrderDetailExtentionData,
            };
            return Ok(res);
        }


        public class CustomerProductFilter
        {
            public List<Filter> FilterList { get; set; } = new List<Filter>();
            public long CustomerId { get; set; }
        }
        /// <summary>
        /// 获取客户的产品
        /// </summary>
        /// <param name="filterList"></param>
        /// <returns></returns>
        [HttpPost("GetCustomerProductList", Name = "GetCustomerProductList")]
        public async Task<IActionResult> GetCustomerProductList([FromBody] CustomerProductFilter postData)
        {
            var customer = await _customerService.GetByIdAsync(postData.CustomerId);
            if (customer == null)
            {
                throw new Exception("当前客户不存在，请刷新");
            }
            var goodQuery = _goodsService.GetAll(true).Where(x => x.CustomerId == customer.Id); //客户产品
            //客户名下的产品
            var goodsIds = goodQuery.Select(x => x.Id);

            //所有的产品及产品规格（可以下单 成品、半成品、原材料）
            var query = _productService.GetProductList([], []).Where(x => x.ReviewStatus == ReviewStatus.评审通过);
            var data = from info in query
                       join goods in goodQuery on
                        new { info.ProductId, info.SpecificationId } equals new { goods.ProductId, goods.SpecificationId } into tmp
                       from goods in tmp.DefaultIfEmpty()
                       select new SaleGoodsDto
                       {
                           ProductId = info.ProductId,
                           SpecificationId = info.SpecificationId,
                           ProductNo = info.ProductNo,
                           ProductName = info.ProductName,
                           ProductTechnicalCode = info.ProductTechnicalCode,
                           ProductSpecification = info.ProductSpecification,
                           ProductPackageWay = info.ProductPackageWay,
                           ProductCategory = info.ProductCategory,
                           ProductCategoryId = info.ProductCategoryId,
                           ProductUnit = info.ProductUnit,
                           ProductType = info.ProductType,
                           Remark = info.Remark,
                           InventoryQuantity = info.InventoryQuantity, //库存数(未包装+已包装) 对应主单位
                           ProductUnpackStockQty = info.UnPackageQuantity, //未包装库存数 对应主单位
                           ProductUnitId = info.ProductUnitId,
                           ProductMainUnit = info.ProductMainUnit,
                           ProductMainUnitId = info.ProductMainUnitId,
                           ProductIsDeleted = info.IsDeleted,
                           GoodsId = goods != null ? goods.Id : null,
                           CustomerMaterialNo = goods != null ? goods.CustomerMaterialNo : null,
                           CustomerMaterialName = goods != null ? goods.CustomerMaterialName : null,
                           SalesUnitId = goods != null ? goods.SalesUnitId : null,
                           SalesUnitConversionId = goods != null ? goods.SalesUnitConversionId : null,
                           PackageId = goods != null ? goods.PackageId : null,
                           PackageSpecification = goods != null && goods.ProductPackage != null ? goods.ProductPackage.PackageSpecification : null,
                           Price = goods != null ? goods.SalesPrice : 0,
                           IsDeleted = goods != null ? goods.IsDeleted : info.IsDeleted,
                           SalesUnitName = goods != null && goods.SalesUnit != null ? goods.SalesUnit.Name : null,
                       };

            if (customer.CustomerNo != "DEFAULT") //钧业可以获取所有产品（钧业没有goods记录）
            {
                data = data.Where(x => x.GoodsId != null && goodsIds.Any(e => e == x.GoodsId));
            }
            var result = await data.ApplyFilters(postData.FilterList)
                            .OrderBy(x => x.ProductId).ThenBy(x => x.SpecificationId)
                            .ToPagedListAsync(this.PageModel);

            var ProductData = result.Data.ToList();
            var productIdList = ProductData.Select(x => x.ProductId).Distinct().ToList();
            var productSpecIdList = ProductData.Select(x => x.SpecificationId).Distinct().ToList();
            this.handleProductQty(productIdList, productSpecIdList, ProductData);
            result.Data = ProductData;
            return Ok(result);
        }
        private void handleProductQty(List<long> productIdList, List<long?> productSpecIdList, IEnumerable<SaleGoodsDto> ProductData)
        {
            //获取产品规格到产品的单位转换(产品)
            var unitConversionData = _unitConversionService.GetAll().Include(x => x.ToUnit)
                                        .Where(x => x.ConversionType == UnitConversionType.SpecToProduct && productIdList.Contains(x.TargetId)).ToList();
            //获取产品的销售订单
            var saleOrderData = _saleOrderDetailService.GetAll().Include(x => x.Goods).Where(x => x.Status == SaleOrderStatus.进行中  //待下单和已完成的不统计
                                    && x.ReviewStatus != ReviewStatus.待提交 //待提交评审的不统计
                                    && productIdList.Contains(x.ProductId)
                                    && (x.SpecificationId == null || productSpecIdList.Contains(x.SpecificationId))).ToList();
            foreach (var product in ProductData)
            {
                if (product.SpecificationId == null) //成品
                {
                    product.StockQtyByProductMainUnit = product.InventoryQuantity;
                }
                else //成品规格 
                {
                    var unitConversion = unitConversionData.FirstOrDefault(x => x.FromUnitId == product.ProductUnitId && x.ToUnitId == product.ProductMainUnitId);
                    product.StockQtyByProductMainUnit = product.InventoryQuantity * (unitConversion?.Ratio ?? 0);
                }
                var orderList = saleOrderData.Where(x => x.ProductId == product.ProductId && ((product.SpecificationId == null && x.SpecificationId == null) //下单的成品
                                        || x.SpecificationId == product.SpecificationId) //下单的成品规格
                                        ).ToList();
                product.UnDeliveryQuantity = orderList.Sum(x => x.PlaceOrderQuantity * getSaleToBaseUnitRatio(x.SalesUnitId, x.UnitConversionInfo, product.ProductUnitId) - x.DeliveryQuantity);
                product.UnProductionQuantity = orderList.Sum(x => x.PlanProductionQuantity * getSaleToBaseUnitRatio(x.SalesUnitId, x.UnitConversionInfo, product.ProductUnitId) - x.ProductionQuantity);
                product.ProcessQuantity = orderList.Sum(x => x.ProductionQuantity - x.ProductionInventoryQuantity);
                product.UnPurchaseQuantity = orderList.Sum(x => x.PlanPurchaseQuantity * getSaleToBaseUnitRatio(x.SalesUnitId, x.UnitConversionInfo, product.ProductUnitId) - x.PurchaseQuantity);
                product.TransitQuantity = orderList.Sum(x => x.PurchaseQuantity - x.PurchaseInventoryQuantity);
            }
        }
        private decimal getSaleToBaseUnitRatio(long saleUnitId, string? unitConversionInfo, long? baseUnitId)
        {
            if (baseUnitId == null)
            {
                return 0; //主单位为空 返回0
            }
            var list = JsonSerializer.Deserialize<List<SaleUnitConversionInfo>>(unitConversionInfo ?? "[]") ?? new List<SaleUnitConversionInfo>();
            var baseUnit = list.FirstOrDefault(x => x.UnitId == baseUnitId && x.IsBaseUnit);

            if (saleUnitId == baseUnitId) //下单单位 = 主单位
            {   //下单规格 使用的是成品主单位（不是规格主单位） 可能出现saleUnitId == baseUnitId
                //但是此时一定会有单位转换数据（到规格主单位 的转换是必有的）
                if (baseUnit != null)
                {
                    return baseUnit.Ratio;
                }
                else
                { //下单单位是主单位 则返回1
                    return 1;
                }
            }
            return baseUnit?.Ratio ?? 0;
        }

        public class ProductIdDto
        {
            /// <summary>
            /// ProductId_SpecificationId_GoodsId
            /// </summary>
            public required string IdField { get; set; }
            public long ProductId { get; set; }
            public long? SpecificationId { get; set; }
            public List<Base_ProductPackage>? PackageList { get; set; }
            public List<GetProductUnitDto>? UnitList { get; set; }
            /// <summary>
            /// 是否过滤已存在客户产品的包装规格
            /// </summary>
            public bool? IsFilterGoods { get; set; }
        }
        /// <summary>
        /// 获取产品可选的外包装方式列表
        /// </summary>
        /// <param name="productIdList"></param>
        /// <returns></returns>
        [HttpPost("GetProductPackageList", Name = "GetProductPackageList")]
        public async Task<IActionResult> GetProductPackageList([FromBody] List<ProductIdDto> productIdList)
        {
            var productIds = productIdList.Select(x => x.ProductId).ToList();
            var pacakageData = await _packageServie.GetAll().Where(x => productIds.Contains(x.ProductId)).ToListAsync();
            foreach (var product in productIdList)
            {
                var pData = pacakageData.Where(x => x.ProductId == product.ProductId);
                if (product.SpecificationId == null) //没有规格时 获取 产品通用包装
                {
                    pData = pData.Where(x => x.SpecificationId == null);
                }
                if (product.SpecificationId != null) //有规格 获取 产品通用包装+规格指定包装
                {
                    pData = pData.Where(x => x.SpecificationId == null || x.SpecificationId == product.SpecificationId);
                }
                //过滤已存在客户产品的规格
                if (product.IsFilterGoods == true)
                {
                    var packegeIds = await _goodsService.GetAll(true).Where(x => x.ProductId == product.ProductId && x.SpecificationId == product.SpecificationId).Select(x => x.PackageId).ToListAsync();
                    pData = pData.Where(x => !packegeIds.Contains(x.Id));
                }
                product.PackageList = pData.ToList();
            }
            return Ok(productIdList);
        }

        public class GetProductUnitDto : SelectionDto
        {
            public long? PackageId { get; set; }
            /// <summary>
            /// 单位ID
            /// </summary>
            public long? UnitId { get; set; }
            public bool IsMainUnit { get; set; } = false;
        }

        /// <summary>
        /// 获取产品可选的单位列表（成品单位、成品规格单位、成品包装单位）
        /// </summary>
        /// <param name="productIdList"></param>
        /// <returns></returns>
        [HttpPost("GetProductUnitList", Name = "GetProductUnitList")]
        public async Task<IActionResult> GetProductUnitList([FromBody] List<ProductIdDto> productIdList)
        {
            var productIds = productIdList.Select(x => x.ProductId).ToList();
            var specIds = productIdList.Select(x => x.SpecificationId).ToList();
            var productData = _productService.GetAll(true).Include(x => x.BaseUnit).Where(x => productIds.Contains(x.Id)).ToList();
            var specData = _specificationService.GetAll(true).Include(x => x.SpecificationUnit).Where(x => specIds.Contains(x.Id)).ToList();
            var pacakageData = await _packageServie.GetAll().Where(x => productIds.Contains(x.ProductId)).ToListAsync();
            var unitData = _unitConversionService.GetAll().Include(x => x.FromUnit)
                .Where(x => productIds.Contains(x.TargetId) || specIds.Contains(x.TargetId)).ToList();
            foreach (var entity in productIdList)
            {
                var product = productData.FirstOrDefault(x => x.Id == entity.ProductId);
                if (product == null) continue;
                var uData = new List<GetProductUnitDto>();
                if (entity.SpecificationId == null)
                {
                    //成品基础主单位
                    uData.Add(new GetProductUnitDto()
                    {
                        Code = "-1",
                        UnitId = product.BaseUnitId,
                        Value = "1",
                        Text = product.BaseUnit.Name,
                    });
                    //成品基础次单位
                    var baseUnitList = unitData.Where(x => x.ConversionType == UnitConversionType.Product && x.TargetId == product.Id);
                    foreach (var baseUnit in baseUnitList)
                    {
                        uData.Add(new GetProductUnitDto()
                        {
                            Code = baseUnit.Id.ToString(),
                            UnitId = baseUnit.FromUnitId,
                            Value = baseUnit.Ratio.ToString(),
                            Text = baseUnit.FromUnit.Name,
                        });
                    }
                    //成品包装单位
                    var packageUnitList = (from package in pacakageData.Where(x => x.ProductId == product.Id && x.SpecificationId == null)
                                           join unit in unitData
                                           on new { packId = package.Id, unitId = package.PackageUnitId != null ? (long)package.PackageUnitId : 0L }
                                           equals new { packId = unit.SourceId, unitId = unit.FromUnitId }
                                           select new GetProductUnitDto()
                                           {
                                               Code = unit.Id.ToString(),
                                               UnitId = unit.FromUnitId,
                                               Value = unit.Ratio.ToString(),
                                               Text = unit.FromUnit.Name,
                                               Extension = package.PackageSpecification,
                                               PackageId = unit.SourceId
                                           }).ToList();
                    uData.AddRange(packageUnitList);
                }
                if (entity.SpecificationId != null)
                {
                    var spec = specData.FirstOrDefault(x => x.Id == entity.SpecificationId);
                    if (spec == null) continue;
                    //规格基础主单位
                    uData.Add(new GetProductUnitDto()
                    {
                        Code = "-1",
                        UnitId = spec.SpecificationUnitId,
                        Value = "1",
                        Text = spec.SpecificationUnit?.Name,
                    });
                    //规格基础次单位
                    var baseUnitList = unitData.Where(x => x.ConversionType == UnitConversionType.ProductSpec && x.TargetId == spec.Id);
                    foreach (var baseUnit in baseUnitList)
                    {
                        uData.Add(new GetProductUnitDto()
                        {
                            Code = baseUnit.Id.ToString(),
                            UnitId = baseUnit.FromUnitId,
                            Value = baseUnit.Ratio.ToString(),
                            Text = baseUnit.FromUnit.Name,
                        });
                    }
                    //规格对应成品的主单位
                    var mainUnit = unitData.FirstOrDefault(x => x.ConversionType == UnitConversionType.SpecToProduct && x.SourceId == spec.Id);
                    if (mainUnit != null)
                    {
                        uData.Add(new GetProductUnitDto()
                        {
                            Code = mainUnit.Id.ToString(),
                            UnitId = mainUnit.ToUnitId,
                            Value = (mainUnit.Ratio != 0 ? 1 / mainUnit.Ratio : 0).ToString(),
                            Text = mainUnit.ToUnit.Name,
                            Extension = "成品主单位",
                            IsMainUnit = true,
                        });
                    }
                    //规格包装单位
                    var packageUnitList = (from package in pacakageData.Where(x => x.ProductId == spec.ProductId && (x.SpecificationId == null || x.SpecificationId == spec.Id))
                                           join unit in unitData
                                           on new { packId = package.Id, unitId = package.PackageUnitId != null ? (long)package.PackageUnitId : 0L }
                                           equals new { packId = unit.SourceId, unitId = unit.FromUnitId }
                                           select new GetProductUnitDto()
                                           {
                                               Code = unit.Id.ToString(),
                                               UnitId = unit.FromUnitId,
                                               Value = package.SpecificationId != null ? unit.Ratio.ToString() : (mainUnit != null && mainUnit.Ratio != 0 ? unit.Ratio / mainUnit.Ratio : 0).ToString(),
                                               Text = unit.FromUnit.Name,
                                               Extension = package.PackageSpecification,
                                               PackageId = unit.SourceId
                                           }).ToList();
                    uData.AddRange(packageUnitList);

                }

                entity.UnitList = uData;
            }
            return Ok(productIdList);
        }

        /// <summary>
        /// 新增销售订单
        /// </summary>
        /// <param name="order"></param>
        /// <returns></returns>
        [HttpPost("AddOrder", Name = "AddOrder")]
        public async Task<IActionResult> AddOrder([FromBody] AddSaleOrderDto order)
        {
            var customer = await _customerService.GetAll().FirstOrDefaultAsync(x => x.Id == order.CustomerId);
            if (customer == null)
            {
                throw new Exception("客户不存在，请刷新重试");
            }
            var saleOrder = new Sale_Order()
            {
                OrderType = order.OrderType,
                CustomerId = customer.Id,
                OrderNo = _sequenceService.GenerateCode(TableName.SaleOrder),
                CustomerAddress = JsonSerializer.Serialize(order.CustomerAddress, JsonHelper.DefaultJsonSerializerOptions),
                OrderTotalPrice = order.OrderTotalPrice,
                Remark = order.Remark,
                ReceiveOrderTime = order.ReceiveOrderTime,
                ReceiveUserId = order.ReceiveUserId,
                CustomerOrderNo = order.CustomerOrderNo,
                Status = SaleOrderStatus.进行中,
                ReviewStatus = order.ReviewStatus,
                ExamineStatus = ExamineStatus.审核通过,
                CustomSaleAttribute = order.CustomSaleAttributeList != null && order.CustomSaleAttributeList.Count > 0 ?
                            JsonSerializer.Serialize(order.CustomSaleAttributeList, JsonHelper.DefaultJsonSerializerOptions) : null,
            };
            _saleOrderService.Insert(saleOrder);
            await this.AddOrderDetail(order.SaleOrderDetails, customer, saleOrder);

            await _unitOfWork.SaveChangeAsync();
            var result = new
            {
                msg = "新增成功",
                orderId = saleOrder.Id
            };
            return Ok(result);
        }
        private async Task<List<Sale_OrderDetail>> AddOrderDetail(List<AddSaleOrderDetailDto> saleOrderDetails, Base_Partner customer, Sale_Order saleOrder)
        {
            var createdDetails = new List<Sale_OrderDetail>();
            var productIds = saleOrderDetails.Select(x => x.Product.ProductId).ToList();
            var specIds = saleOrderDetails.Select(x => x.Product.SpecificationId).ToList();
            var goodsIds = saleOrderDetails.Select(x => x.GoodsId).ToList();
            var packageIds = saleOrderDetails.Select(x => x.PackageId).ToList();
            var productData = await _productService.GetAll(true).Where(x => productIds.Contains(x.Id)).ToListAsync();
            var specData = await _specificationService.GetAll().Where(x => specIds.Contains(x.Id)).ToListAsync();
            var goodsData = await _goodsService.GetAll().Where(x => goodsIds.Contains(x.Id) ||
                                (x.CustomerId == customer.Id && productIds.Contains(x.ProductId))).ToListAsync();
            var packageData = await _packageServie.GetAll().Where(x => packageIds.Contains(x.Id)).ToListAsync();
            foreach (var detail in saleOrderDetails)
            {
                var product = productData.FirstOrDefault(x => x.Id == detail.Product.ProductId);
                var spec = specData.FirstOrDefault(x => x.Id == detail.Product.SpecificationId);
                //下单成品 成品被硬删或软删
                if (detail.Product.SpecificationId == null && (product == null || product.IsDeleted))
                {
                    throw new Exception($" 产品{detail.Product.ProductNo}-{detail.Product.ProductName}不存在，请刷新重试");
                }
                //下单规格 成品被硬删
                if (detail.Product.SpecificationId != null && product == null)
                {
                    throw new Exception($" 数据错误，请刷新重试");
                }
                //下单规格 规格被删
                if (detail.Product.SpecificationId != null && spec == null)
                {
                    throw new Exception($" 产品{detail.Product.ProductNo}-{detail.Product.ProductName}不存在，请刷新重试");
                }
                if (detail.PackageId != null)
                {
                    var package = packageData.FirstOrDefault(x => x.Id == detail.PackageId);
                    if (package == null)
                    {
                        throw new Exception($" 产品{detail.Product.ProductNo}-{detail.Product.ProductName}，外包装方案{detail.PackageSpecification}不存在，请刷新重试");
                    }
                }
                var goods = goodsData.FirstOrDefault(x => x.Id == detail.GoodsId);
                if (customer.CustomerNo != "DEFAULT" && goods == null)
                {
                    goods = new Base_Goods()
                    {
                        ProductId = product.Id,
                        SpecificationId = spec?.Id,
                        CustomerId = customer.Id,
                        SalesPrice = detail.Price,
                        SalesUnitId = detail.SalesUnitId,
                        CustomerMaterialNo = detail.CustomerMaterialNo,
                        CustomerMaterialName = detail.CustomerMaterialName,
                        PackageId = detail.PackageId,
                        SalesUnitConversionId = detail.salesUnitConversionId
                    };
                    _goodsService.Insert(goods);
                }
                if (goods != null)
                {
                    if (goods.PackageId != detail.PackageId) //包装方式变了 新增外部产品记录
                    {
                        //客户是否存在此包装的外部产品
                        var exist = goodsData.FirstOrDefault(x => x.PackageId == detail.PackageId &&
                                                                    x.ProductId == detail.Product.ProductId &&
                                                                        x.SpecificationId == detail.Product.SpecificationId);
                        if (exist != null)
                        {//更新外部产品信息
                            exist.SalesPrice = detail.Price;
                            exist.SalesUnitId = detail.SalesUnitId;
                            exist.CustomerMaterialNo = detail.CustomerMaterialNo;
                            exist.CustomerMaterialName = detail.CustomerMaterialName;
                            goods = exist;
                        }
                        else
                        {//生成新的外部产品
                            goods = new Base_Goods()
                            {
                                ProductId = goods.ProductId,
                                SpecificationId = goods.SpecificationId,
                                CustomerId = goods.CustomerId,
                                SalesPrice = detail.Price,
                                SalesUnitId = detail.SalesUnitId,
                                CustomerMaterialNo = detail.CustomerMaterialNo,
                                CustomerMaterialName = detail.CustomerMaterialName,
                                PackageId = detail.PackageId,
                                SalesUnitConversionId = detail.salesUnitConversionId
                            };
                            _goodsService.Insert(goods);
                        }
                    }
                    else
                    { //更新外部产品信息
                        goods.SalesPrice = detail.Price;
                        goods.SalesUnitId = detail.SalesUnitId;
                        goods.CustomerMaterialNo = detail.CustomerMaterialNo;
                        goods.CustomerMaterialName = detail.CustomerMaterialName;
                    }
                }
                var orderDetail = new Sale_OrderDetail()
                {
                    SaleOrder = saleOrder,
                    Goods = goods,
                    ProductId = product.Id,
                    SpecificationId = detail.Product.SpecificationId,
                    PlaceOrderQuantity = detail.PlaceOrderQuantity,
                    PlanProductionQuantity = detail.PlanProductionQuantity,
                    PlanPurchaseQuantity = detail.PlanPurchaseQuantity,
                    PlanDirectDliveryQuantity = detail.PlanDirectDliveryQuantity,
                    Price = detail.Price,
                    TotalPrice = detail.TotalPrice,
                    SalesUnit = detail.SalesUnit,
                    SalesUnitId = detail.SalesUnitId,
                    IsUrgent = detail.IsUrgent,
                    Remark = detail.Remark,
                    DeliveryTime = detail.DeliveryTime,
                    Status = SaleOrderStatus.进行中,
                    ReviewStatus = detail.ReviewStatus,
                    UnitConversionInfo = JsonSerializer.Serialize(detail.UnitConversionList, JsonHelper.DefaultJsonSerializerOptions),
                    PackageId = detail.PackageId,
                    PackageSpecification = detail.PackageSpecification,
                    GoodsNo = detail.Product.ProductNo!,
                    GoodsName = detail.Product.ProductName!,
                    Specification = detail.Product.ProductSpecification,
                    TechnicalCode = detail.Product.ProductTechnicalCode,
                    GoodsCategory = detail.Product.ProductCategory!,
                    CustomerMaterialNo = detail.CustomerMaterialNo,
                    CustomerMaterialName = detail.CustomerMaterialName,
                    GoodsUnit = detail.Product.ProductUnit,
                    GoodsRemark = detail.Product.Remark,
                    GoodsPackageWay = detail.Product.ProductPackageWay,
                    SalesUnitConversionId = detail.salesUnitConversionId
                };
                _saleOrderDetailService.Insert(orderDetail);
                createdDetails.Add(orderDetail);
            }
            return createdDetails;
        }

        /// <summary>
        /// 新增销售订单并直接生成送货单：根据上传的数据先生成销售单记录，再生成送货单记录，整个流程在后端一次完成。
        /// </summary>
        /// <param name="postData">销售订单信息 + 送货单信息（送货明细与销售订单明细按顺序一一对应）</param>
        [HttpPost("AddOrderAndCreateDelivery", Name = "AddOrderAndCreateDelivery")]
        public async Task<IActionResult> AddOrderAndCreateDelivery([FromBody] AddOrderAndCreateDeliveryDto postData)
        {
            var customer = await _customerService.GetAll().FirstOrDefaultAsync(x => x.Id == postData.Order.CustomerId);
            if (customer == null)
            {
                throw new Exception("客户不存在，请刷新重试");
            }
            if (postData.Order.SaleOrderDetails == null || postData.Order.SaleOrderDetails.Count == 0)
            {
                throw new Exception("请选择产品");
            }
            var deliveryDetails = postData.Delivery?.Details ?? new List<AddOrderAndCreateDeliveryDetailDto>();
            if (deliveryDetails.Count > 0 && deliveryDetails.Count != postData.Order.SaleOrderDetails.Count)
            {
                throw new Exception("送货明细与销售明细数量不一致");
            }

            using var transaction = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);

            //1. 先生成销售单记录
            var saleOrder = new Sale_Order()
            {
                OrderType = postData.Order.OrderType,
                CustomerId = customer.Id,
                OrderNo = _sequenceService.GenerateCode(TableName.SaleOrder),
                CustomerAddress = JsonSerializer.Serialize(postData.Order.CustomerAddress, JsonHelper.DefaultJsonSerializerOptions),
                OrderTotalPrice = postData.Order.OrderTotalPrice,
                Remark = postData.Order.Remark,
                ReceiveOrderTime = postData.Order.ReceiveOrderTime,
                ReceiveUserId = postData.Order.ReceiveUserId,
                CustomerOrderNo = postData.Order.CustomerOrderNo,
                Status = SaleOrderStatus.进行中,
                ReviewStatus = postData.Order.ReviewStatus,
                ExamineStatus = ExamineStatus.审核通过,
                CustomSaleAttribute = postData.Order.CustomSaleAttributeList != null && postData.Order.CustomSaleAttributeList.Count > 0 ?
                            JsonSerializer.Serialize(postData.Order.CustomSaleAttributeList, JsonHelper.DefaultJsonSerializerOptions) : null,
            };
            _saleOrderService.Insert(saleOrder);
            var saleOrderDetails = await this.AddOrderDetail(postData.Order.SaleOrderDetails, customer, saleOrder);
            await _unitOfWork.SaveChangeAsync(); //先保存一次，获取销售单及其明细的 Id

            //2. 再生成送货单记录
            long deliveryOrderId = 0;
            if (postData.Delivery != null && deliveryDetails.Count > 0)
            {
                var sequence = _sequenceService.GetDateSequenceNoForUse("DeliveryOrder", DateTime.Now);
                var deliveryOrder = new Inventory_DeliveryOrder()
                {
                    DeliveryOrderNo = $"XSDD{DateTime.Now:yyyyMMdd}{sequence.ToString().PadLeft(3, '0')}",
                    CustomerId = customer.Id,
                    CustomerAddress = JsonSerializer.Serialize(postData.Delivery.CustomerAddress ?? postData.Order.CustomerAddress, JsonHelper.DefaultJsonSerializerOptions),
                    EstimatedDeliveryTime = postData.Delivery.EstimatedDeliveryTime,
                    IsDirectOutbound = postData.Delivery.IsDirectOutbound,
                    IsInventoryTransfer = postData.Delivery.IsInventoryTransfer,
                    Remark = postData.Delivery.Remark,
                    CustomAttribute = postData.Delivery.CustomAttributeList != null && postData.Delivery.CustomAttributeList.Count > 0 ?
                                JsonSerializer.Serialize(postData.Delivery.CustomAttributeList, JsonHelper.DefaultJsonSerializerOptions) : null,
                };
                _deliveryOrderService.Insert(deliveryOrder);

                for (var i = 0; i < saleOrderDetails.Count; i++)
                {
                    var saleOrderDetail = saleOrderDetails[i];
                    var deliveryDetailDto = deliveryDetails.ElementAtOrDefault(i);
                    if (deliveryDetailDto == null || deliveryDetailDto.DeliveryQuantity <= 0)
                    {
                        continue; //该明细本次不送货
                    }
                    var deliveryOrderDetail = new Inventory_DeliveryOrderDetail()
                    {
                        DeliveryOrder = deliveryOrder,
                        SaleOrderDetailId = saleOrderDetail.Id,
                        ProductId = saleOrderDetail.ProductId,
                        SpecificationId = saleOrderDetail.SpecificationId,
                        GoodsId = saleOrderDetail.GoodsId,
                        PackageId = deliveryDetailDto.PackageId ?? saleOrderDetail.PackageId,
                        DeliveryQuantity = deliveryDetailDto.DeliveryQuantity, //主单位
                        InputUnitQuantity = deliveryDetailDto.InputUnitQuantity, //用户选中单位
                        InputUnitConversionId = deliveryDetailDto.InputUnitConversionId,
                        UnitInfo = JsonSerializer.Serialize(deliveryDetailDto.UnitInfo, JsonHelper.DefaultJsonSerializerOptions),
                    };
                    _deliveryOrderDetailService.Insert(deliveryOrderDetail);

                    //锁定库存：指定库存占用时生成库存占用记录
                    if (deliveryDetailDto.IsAllocate && deliveryDetailDto.DeliveryQuantity > 0)
                    {
                        this.AddDeliverySaleOccupy(deliveryOrderDetail, deliveryDetailDto);
                    }

                    //累加销售订单明细的已通知送货数量（主单位）
                    saleOrderDetail.DeliveryNoticeQuantity += deliveryDetailDto.DeliveryQuantity;
                }
                deliveryOrderId = deliveryOrder.Id;
            }

            await _unitOfWork.SaveChangeAsync();
            transaction.Complete();

            var result = new
            {
                msg = "新增成功",
                orderId = saleOrder.Id,
                deliveryOrderId = deliveryOrderId,
                saleOrderDetailIds = saleOrderDetails.Select(x => x.Id).ToList()
            };
            return Ok(result);
        }

        /// <summary>
        /// 生成送货明细对应的库存占用（锁定库存）
        /// </summary>
        private void AddDeliverySaleOccupy(Inventory_DeliveryOrderDetail deliveryOrderDetail, AddOrderAndCreateDeliveryDetailDto detail)
        {
            if (detail.InputQuantity == null || detail.InputQuantity <= 0)
            {
                throw new Exception("请填写本次送货数量");
            }
            var inputUnitInfo = detail.UnitInfo.FirstOrDefault(u => u.Id == detail.InputUnitConversionId);
            if (inputUnitInfo == null)
            {
                throw new Exception("送货单位错误，请刷新重试");
            }
            //校验库存占用明细
            if (detail.InventoryRecordList == null || detail.InventoryRecordList.Count == 0)
            {
                throw new Exception("请至少选择一条库存进行占用");
            }
            var inventoryIds = detail.InventoryRecordList.Select(x => x.InventoryRecordId).Distinct().ToList();
            var inventoryData = _inventoryService.GetAll().Where(x => inventoryIds.Contains(x.Id)).ToList();
            foreach (var occupy in detail.InventoryRecordList)
            {
                var inventoryRecord = inventoryData.FirstOrDefault(x => x.Id == occupy.InventoryRecordId);
                if (inventoryRecord == null)
                {
                    throw new Exception("占用的库存不存在，请刷新重试");
                }
                if (occupy.OccupyQuantity <= 0)
                {
                    throw new Exception("库存占用数量必须大于0");
                }
                if (occupy.OccupyQuantity > inventoryRecord.RemainQuantity)
                {
                    throw new Exception($"占用的库存{inventoryRecord.RecordNo}余量不足，请刷新重试");
                }
            }

            var inventoryOccupy = new Inventory_Occupy()
            {
                SourceType = OccupySourceType.销售送货,
                SourceId = deliveryOrderDetail.Id,
                OccupyQuantity = detail.InputQuantity.Value,
                OccupyUnitConversionId = detail.InputUnitConversionId,
                DataType = detail.SpecificationId.HasValue ? InventoryDataType.Specification : InventoryDataType.Product,
                DataId = detail.SpecificationId ?? detail.ProductId,
                OccupyUnitId = inputUnitInfo.UnitId,
                OccupyUnit = inputUnitInfo.UnitName,
                UnitInfo = JsonSerializer.Serialize(detail.UnitInfo, JsonHelper.DefaultJsonSerializerOptions)
            };
            _occupyService.Insert(inventoryOccupy);

            foreach (var occupy in detail.InventoryRecordList)
            {
                var inventoryOccupyDetail = new Inventory_OccupyDetail()
                {
                    Occupy = inventoryOccupy,
                    OccupyQuantity = occupy.OccupyQuantity,
                    InputQuantity = occupy.InputQuantity,
                    InputUnitConversionId = occupy.InputUnitConversionId,
                    PositionId = occupy.PositionId,
                    InventoryRecordId = occupy.InventoryRecordId,
                    UnitInfo = JsonSerializer.Serialize(occupy.UnitInfo, JsonHelper.DefaultJsonSerializerOptions)
                };
                _occupyDetailService.Insert(inventoryOccupyDetail);
            }
        }

        /// <summary>
        /// 新增销售订单并直接生成送货单 请求参数
        /// </summary>
        public class AddOrderAndCreateDeliveryDto
        {
            /// <summary> 销售订单信息（与 AddOrder 入参一致） </summary>
            public AddSaleOrderDto Order { get; set; } = new AddSaleOrderDto();
            /// <summary> 送货单信息（可选，不传或明细为空则只生成销售单） </summary>
            public AddOrderAndCreateDeliveryOrderDto? Delivery { get; set; }
        }

        public class AddOrderAndCreateDeliveryOrderDto
        {
            public CustomerAddressDto? CustomerAddress { get; set; }
            public DateTime? EstimatedDeliveryTime { get; set; }
            public bool IsDirectOutbound { get; set; }
            public bool IsInventoryTransfer { get; set; }
            public string? Remark { get; set; }
            public List<CustomAttribute> CustomAttributeList { get; set; } = new List<CustomAttribute>();
            /// <summary> 送货明细，与销售订单明细按顺序一一对应 </summary>
            public List<AddOrderAndCreateDeliveryDetailDto> Details { get; set; } = new List<AddOrderAndCreateDeliveryDetailDto>();
        }

        public class AddOrderAndCreateDeliveryDetailDto
        {
            public long ProductId { get; set; }
            public long? SpecificationId { get; set; }
            public long? GoodsId { get; set; }
            /// <summary> 外包装方案Id </summary>
            public long? PackageId { get; set; }
            /// <summary> 本次送货量：主单位 </summary>
            public decimal DeliveryQuantity { get; set; }
            /// <summary> 本次送货量：用户选中单位 </summary>
            public decimal? InputUnitQuantity { get; set; }
            public long? InputUnitConversionId { get; set; }
            public List<InventoryUnitOutboundDto> UnitInfo { get; set; } = new List<InventoryUnitOutboundDto>();
            /// <summary> 是否指定库存占用（锁定库存） </summary>
            public bool IsAllocate { get; set; }
            /// <summary> 库存占用明细 </summary>
            public List<AddOrderAndCreateDeliveryOccupyDetailDto>? InventoryRecordList { get; set; }
        }

        public class AddOrderAndCreateDeliveryOccupyDetailDto
        {
            public long InventoryRecordId { get; set; }
            public long? PositionId { get; set; }
            /// <summary> 占用数量：主单位 </summary>
            public decimal OccupyQuantity { get; set; }
            /// <summary> 占用数量：选中单位 </summary>
            public decimal? InputQuantity { get; set; }
            public long? InputUnitConversionId { get; set; }
            public List<InventoryUnitOutboundDto> UnitInfo { get; set; } = new List<InventoryUnitOutboundDto>();
        }

        public class GetSaleOrderDto
        {
            public long orderId { get; set; }

            public long? detailId { get; set; }
        }
        /// <summary>
        /// 获取订单详情
        /// </summary>
        /// <param name="orderId"></param>
        /// <param name="detailId"></param>
        /// <returns></returns>
        [HttpPost("GetSaleOrderById", Name = "GetSaleOrderById")]
        public async Task<IActionResult> GetSaleOrderById([FromBody] GetSaleOrderDto dto)
        {
            var data = _saleOrderDetailService.GetAll().Include(x => x.SaleOrder).ThenInclude(x => x.Customer)
                            .Include(x => x.Product).Include(x => x.ProductSpecification).Where(x => x.SaleOrderId == dto.orderId);
            if (dto.detailId != null)
            {
                data = data.Where(x => dto.detailId == x.Id);
            }
            var detailData = await data.ToListAsync();
            if (detailData.Count() <= 0)
            {
                throw new Exception("订单不存在，请刷新重试");
            }
            var orderData = detailData.First().SaleOrder;

            var result = new ModifyGetSaleOrderDto
            {
                Id = orderData.Id,
                OrderNo = orderData.OrderNo,
                CustomerAddress = JsonSerializer.Deserialize<CustomerAddressDto>(orderData.CustomerAddress ?? "{}", JsonHelper.DefaultJsonSerializerOptions),
                OrderTotalPrice = orderData.OrderTotalPrice,
                Remark = orderData.Remark,
                Status = orderData.Status,
                ReceiveOrderTime = orderData.ReceiveOrderTime,
                ReceiveUserId = orderData.ReceiveUserId,
                CustomerId = orderData.CustomerId,
                Customer = orderData.Customer,
                CustomerOrderNo = orderData.CustomerOrderNo,
                OrderType = orderData.OrderType,
                ReviewStatus = orderData.ReviewStatus,
                CustomerAddressIndex = JsonSerializer.Deserialize<CustomerAddressDto>(orderData.CustomerAddress ?? "{}", JsonHelper.DefaultJsonSerializerOptions)?.Index,
                CustomSaleAttributeList = orderData.CustomSaleAttribute != null ? JsonSerializer.Deserialize<List<CustomAttribute>>(orderData.CustomSaleAttribute, JsonHelper.DefaultJsonSerializerOptions) : new List<CustomAttribute>(),
                SaleOrderDetails = detailData.Select(x =>
                {
                    var ProductUnitId = x.SpecificationId != null ? x.ProductSpecification!.SpecificationUnitId : x.Product.BaseUnitId;
                    var ratio = getSaleToBaseUnitRatio(x.SalesUnitId, x.UnitConversionInfo, ProductUnitId);
                    var ProductionQuantity = x.ProductionQuantity > 0 ? x.ProductionQuantity / ratio : 0;
                    var PurchaseQuantity = x.PurchaseQuantity > 0 ? x.PurchaseQuantity / ratio : 0;
                    var DeliveryQuantity = x.DeliveryQuantity > 0 ? x.DeliveryQuantity / ratio : 0;
                    return new ModifyGetSaleOrderDetailDto()
                    {
                        Id = x.Id,
                        PlaceOrderQuantity = x.PlaceOrderQuantity,
                        PlanProductionQuantity = x.PlanProductionQuantity,
                        PlanPurchaseQuantity = x.PlanPurchaseQuantity,
                        PlanDirectDliveryQuantity = x.PlanDirectDliveryQuantity,
                        Price = x.Price,
                        TotalPrice = x.TotalPrice,
                        IsUrgent = x.IsUrgent,
                        ReviewStatus = x.ReviewStatus,
                        Status = x.Status,
                        DeliveryTime = x.DeliveryTime,
                        Remark = x.Remark,
                        GoodsId = x.GoodsId,
                        SaleOrderId = x.SaleOrderId,
                        CustomerMaterialNo = x.CustomerMaterialNo,
                        CustomerMaterialName = x.CustomerMaterialName,
                        PackageSpecification = x.PackageSpecification,
                        PackageId = x.PackageId,
                        SalesUnit = x.SalesUnit,
                        SalesUnitId = x.SalesUnitId,
                        salesUnitConversionId = x.SalesUnitConversionId,
                        UnitConversionList = JsonSerializer.Deserialize<List<SaleUnitConversionInfo>>(x.UnitConversionInfo ?? "[]", JsonHelper.DefaultJsonSerializerOptions),
                        ProductionQuantity = ProductionQuantity,
                        PurchaseQuantity = PurchaseQuantity,
                        DeliveryQuantity = DeliveryQuantity,
                        AllOrganizeQuantity = ProductionQuantity + PurchaseQuantity + DeliveryQuantity,
                        Product = new SaleGoodsDto
                        {
                            ProductId = x.ProductId,
                            SpecificationId = x.SpecificationId,
                            ProductNo = x.GoodsNo,
                            ProductName = x.GoodsName,
                            ProductTechnicalCode = x.TechnicalCode,
                            ProductSpecification = x.Specification,
                            PackageSpecification = x.PackageSpecification,
                            ProductUnit = x.GoodsUnit,
                            ProductUnitId = x.SpecificationId != null ? x.ProductSpecification!.SpecificationUnitId : x.Product.BaseUnitId,
                            ProductCategory = x.GoodsCategory,
                            Remark = x.GoodsRemark,
                            ProductPackageWay = x.GoodsPackageWay,
                            InventoryQuantity = x.SpecificationId != null ? x.ProductSpecification!.SpecificationInventoryQuantity : x.Product.InventoryQuantity, //库存数(未包装+已包装) 对应主单位
                            ProductUnpackStockQty = x.SpecificationId != null ? 0 : x.Product.UnPackageQuantity, //未包装库存数 对应主单位
                        }
                    };
                }).ToList(),
            };
            var ProductData = result.SaleOrderDetails.Select(x => x.Product);
            var productIdList = ProductData.Select(x => x.ProductId).Distinct().ToList();
            var productSpecIdList = ProductData.Select(x => x.SpecificationId).Distinct().ToList();
            this.handleProductQty(productIdList, productSpecIdList, ProductData);
            return Ok(result);
        }

        [HttpPost("ModifySaleOrder", Name = "ModifySaleOrder")]
        public async Task<IActionResult> ModifySaleOrder([FromBody] AddSaleOrderDto order)
        {
            var customer = await _customerService.GetAll().FirstOrDefaultAsync(x => x.Id == order.CustomerId);
            if (customer == null)
            {
                throw new Exception("客户不存在，请刷新重试");
            }
            var saleOrder = _saleOrderService.GetAll().Include(x => x.SaleOrderDetails).FirstOrDefault(x => x.Id == order.Id);
            if (saleOrder == null)
            {
                throw new Exception("订单不存在，请刷新重试");
            }
            saleOrder.OrderType = order.OrderType;
            saleOrder.ReceiveUserId = order.ReceiveUserId;
            saleOrder.CustomerOrderNo = order.CustomerOrderNo;
            saleOrder.CustomerAddress = JsonSerializer.Serialize(order.CustomerAddress, JsonHelper.DefaultJsonSerializerOptions);
            saleOrder.ReceiveOrderTime = order.ReceiveOrderTime;
            saleOrder.Remark = order.Remark;
            saleOrder.OrderTotalPrice = order.OrderTotalPrice;
            saleOrder.ReviewStatus = order.ReviewStatus;
            saleOrder.CustomSaleAttribute = order.CustomSaleAttributeList != null && order.CustomSaleAttributeList.Count > 0 ?
                            JsonSerializer.Serialize(order.CustomSaleAttributeList, JsonHelper.DefaultJsonSerializerOptions) : null;

            var newDetails = order.SaleOrderDetails.Where(x => x.Id <= 0).ToList();
            var delDetails = saleOrder.SaleOrderDetails.Where(x => !order.SaleOrderDetails.Any(e => e.Id == x.Id)).ToList();
            var modifyDetails = saleOrder.SaleOrderDetails.Where(x => order.SaleOrderDetails.Any(e => e.Id == x.Id)).ToList();
            await AddOrderDetail(newDetails, customer, saleOrder);
            foreach (var detail in delDetails)
            {
                _saleOrderDetailService.Delete(detail, false);
            }
            foreach (var detail in modifyDetails)
            {
                var newDetial = order.SaleOrderDetails.First(x => x.Id == detail.Id);
                this.ModifyDetail(detail, newDetial);
            }

            await _unitOfWork.SaveChangeAsync();
            return Ok("修改成功");
        }
        private void ModifyDetail(Sale_OrderDetail detail, AddSaleOrderDetailDto newDetial)
        {
            detail.CustomerMaterialNo = newDetial.CustomerMaterialNo;
            detail.CustomerMaterialName = newDetial.CustomerMaterialName;
            detail.Remark = newDetial.Remark;
            detail.IsUrgent = newDetial.IsUrgent;
            detail.DeliveryTime = newDetial.DeliveryTime;
            detail.Price = newDetial.Price;
            detail.TotalPrice = newDetial.TotalPrice;
            detail.PackageSpecification = newDetial.PackageSpecification;
            detail.PackageId = newDetial.PackageId;
            detail.PlaceOrderQuantity = newDetial.PlaceOrderQuantity;
            detail.PlanProductionQuantity = newDetial.PlanProductionQuantity;
            detail.PlanPurchaseQuantity = newDetial.PlanPurchaseQuantity;
            detail.PlanDirectDliveryQuantity = newDetial.PlanDirectDliveryQuantity;
            detail.UnitConversionInfo = JsonSerializer.Serialize(newDetial.UnitConversionList, JsonHelper.DefaultJsonSerializerOptions);
            detail.ReviewStatus = newDetial.ReviewStatus;
            detail.SalesUnit = newDetial.SalesUnit;
            detail.SalesUnitId = newDetial.SalesUnitId;
            detail.SalesUnitConversionId = newDetial.salesUnitConversionId;
        }
        [HttpPost("ModifySaleOrderDetail", Name = "ModifySaleOrderDetail")]
        public async Task<IActionResult> ModifySaleOrderDetail([FromBody] AddSaleOrderDetailDto detail)
        {
            using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                var oldDetail = _saleOrderDetailService.GetAll().FirstOrDefault(x => x.Id == detail.Id);
                if (oldDetail == null)
                {
                    throw new Exception("订单明细不存在，请刷新重试");
                }
                this.ModifyDetail(oldDetail, detail);
                await _unitOfWork.SaveChangeAsync();
                //更新订单总价
                var order = _saleOrderService.GetAll().Include(x => x.SaleOrderDetails).FirstOrDefault(x => x.Id == oldDetail.SaleOrderId);
                if (order != null)
                {
                    order.OrderTotalPrice = order.SaleOrderDetails.Sum(x => x.TotalPrice);
                }
                scope.Complete();
                return Ok("操作成功");
            }
        }

        /// <summary>
        /// 删除订单
        /// </summary>
        /// <param name="details"></param>
        /// <returns></returns>
        [HttpPost("DeleteSaleOrderDetail", Name = "DeleteSaleOrderDetail")]
        public async Task<IActionResult> DeleteSaleOrderDetail([FromBody] List<long> detailIds)
        {
            using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                var detailData = await _saleOrderDetailService.GetAll().Include(x => x.SaleOrder).Where(x => detailIds.Contains(x.Id)).ToListAsync();
                var faildData = new List<ActionResult>();
                foreach (var detailId in detailIds)
                {
                    var detail = detailData.FirstOrDefault(x => x.Id == detailId);
                    if (detail == null)
                    {
                        continue;
                    }
                    var orderInfo = $"订单{detail.SaleOrder.OrderNo}，产品{detail.GoodsNo}-{detail.GoodsName}";
                    if (detail.ProductionQuantity > 0)
                    {
                        faildData.Add(new ActionResult
                        {
                            Msg = "已排产，无法删除",
                            OrderInfo = orderInfo
                        });
                        continue;
                    }
                    if (detail.PurchaseQuantity > 0)
                    {
                        faildData.Add(new ActionResult
                        {
                            Msg = "已外购，无法删除",
                            OrderInfo = orderInfo
                        });
                        continue;
                    }
                    if (detail.DeliveryQuantity > 0 || detail.DeliveryNoticeQuantity > 0)
                    {
                        faildData.Add(new ActionResult
                        {
                            Msg = "已进入送货阶段，无法删除",
                            OrderInfo = orderInfo
                        });
                        continue;
                    }
                    _saleOrderDetailService.Delete(detail, false);
                }
                await _unitOfWork.SaveChangeAsync();

                var orderIds = detailData.Select(x => x.SaleOrderId).Distinct().ToList();
                var orderData = _saleOrderService.GetAll().Include(x => x.SaleOrderDetails).Where(x => orderIds.Contains(x.Id)).ToList();
                foreach (var order in orderData)
                {
                    if (order.SaleOrderDetails.Count() <= 0)
                    {
                        //删除订单
                        _saleOrderService.Delete(order, false);
                    }
                    else
                    { //更新订单总价
                        order.OrderTotalPrice = order.SaleOrderDetails.Sum(x => x.TotalPrice);
                    }
                }
                await _unitOfWork.SaveChangeAsync();
                scope.Complete();

                return Ok(faildData);
            }
        }
        public class ActionResult
        {
            public required string Msg { get; set; }
            public required string OrderInfo { get; set; }
        }

        [HttpPost("CompleteSaleOrderDetail", Name = "CompleteSaleOrderDetail")]
        public async Task<IActionResult> CompleteSaleOrderDetail([FromBody] List<long> detailIds)
        {
            using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                var detailData = await _saleOrderDetailService.GetAll().Include(x => x.SaleOrder).Where(x => detailIds.Contains(x.Id)).ToListAsync();

                foreach (var detailId in detailIds)
                {
                    var detail = detailData.FirstOrDefault(x => x.Id == detailId);
                    if (detail == null)
                    {
                        continue;
                    }
                    if (detail.ReviewStatus != ReviewStatus.评审通过)
                    {
                        continue; //未评审通过的订单 不能确认完成
                    }
                    if (detail.Status == SaleOrderStatus.进行中)
                    {
                        detail.Status = SaleOrderStatus.已完成;
                    }
                    else if (detail.Status == SaleOrderStatus.已完成)
                    {
                        detail.Status = SaleOrderStatus.进行中;
                    }
                }
                await _unitOfWork.SaveChangeAsync();
                var orderIds = detailData.Select(x => x.SaleOrderId).Distinct().ToList();
                var orderData = _saleOrderService.GetAll().Include(x => x.SaleOrderDetails).Where(x => orderIds.Contains(x.Id)).ToList();
                foreach (var order in orderData)
                {
                    var unFinish = order.SaleOrderDetails.Where(x => x.Status != SaleOrderStatus.已完成).ToList(); //未完成的订单明细
                    if (unFinish.Count() <= 0)
                    {
                        order.Status = SaleOrderStatus.已完成; //明细全部完成 订单则为已完成
                    }
                    else
                    {
                        order.Status = SaleOrderStatus.进行中;
                    }
                }
                await _unitOfWork.SaveChangeAsync();
                scope.Complete();
                return Ok("操作成功");
            }
        }
        /// <summary>
        /// 订单提交评审
        /// </summary>
        /// <param name="idObject"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        [HttpPost("CommitReviewSaleOrder", Name = "CommitReviewSaleOrder")]
        public async Task<IActionResult> CommitReviewSaleOrder([FromBody] IdLongObject idObject)
        {
            var saleOrder = await _saleOrderService.GetAll().Include(x => x.SaleOrderDetails).FirstOrDefaultAsync(x => x.Id == idObject.Id);
            if (saleOrder == null)
            {
                throw new Exception("订单不存在，请刷新重试");
            }
            saleOrder.ReviewStatus = ReviewStatus.评审中;
            foreach (var detail in saleOrder.SaleOrderDetails)
            {
                detail.ReviewStatus = ReviewStatus.评审中;
            }
            await _unitOfWork.SaveChangeAsync();
            return Ok("操作成功");
        }
        public class ReviewOrderDto
        {
            public List<long> detailIds { get; set; } = new List<long>();
            public ReviewStatus ReviewStatus { get; set; }

        }
        /// <summary>
        /// 订单明细 提交评审/评审驳回/评审通过
        /// </summary>
        /// <param name="dto"></param>
        /// <returns></returns>
        [HttpPost("ReviewSaleOrderDetail", Name = "ReviewSaleOrderDetail")]
        public async Task<IActionResult> ReviewSaleOrderDetail([FromBody] ReviewOrderDto dto)
        {
            using (var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                var detailData = await _saleOrderDetailService.GetAll().Include(x => x.SaleOrder).Where(x => dto.detailIds.Contains(x.Id)).ToListAsync();

                foreach (var detail in detailData)
                {
                    detail.ReviewStatus = dto.ReviewStatus;
                }
                await _unitOfWork.SaveChangeAsync();
                //更新订单的评审状态
                var orderIds = detailData.Select(x => x.SaleOrderId).Distinct().ToList();
                var orderData = _saleOrderService.GetAll().Include(x => x.SaleOrderDetails).Where(x => orderIds.Contains(x.Id)).ToList();
                foreach (var order in orderData)
                {
                    var notItems = order.SaleOrderDetails.Where(x => x.ReviewStatus != dto.ReviewStatus).ToList();
                    if (notItems.Count() <= 0)
                    {
                        order.ReviewStatus = dto.ReviewStatus; //明细全部提交评审/评审驳回/评审通过 订单则为提交评审/评审驳回/评审通过
                    }
                }
                await _unitOfWork.SaveChangeAsync();
                scope.Complete();
                return Ok("操作成功");
            }
        }

        [HttpGet("GetProductionByNo", Name = "GetProductionByNo")]
        public async Task<IActionResult> GetProductionByNo([FromQuery] string productionNo, decimal saleToBaseRatio)
        {
            var data = await _productionService.GetAll().Include(x => x.ProductSpecification).Include(x => x.Product).FirstOrDefaultAsync(x => x.OrderNo == productionNo);
            if (data == null)
            {
                throw new Exception($"排产单{productionNo}不存在，请重新输入");
            }
            var unitInfo = JsonSerializer.Deserialize<UnitInfo>(data.DispatchUnit);
            var reportToBaseRatio = unitInfo != null ? unitInfo.baseRatio : 0; //主单位到报工单位的转换率
            var reportToSaleRatio = saleToBaseRatio != 0 ? reportToBaseRatio / saleToBaseRatio : 0;
            var result = new
            {
                ProductionId = data.Id,
                ReportUnit = unitInfo?.incomeUnitName,
                ReportUnitConversionId = unitInfo?.incomeUnitConversionId,
                Ratio = reportToSaleRatio,
                ProductNo = data.ProductSpecification != null ? data.ProductSpecification.Code : data.Product.ProductNo,
                ProductName = data.ProductSpecification != null ? data.ProductSpecification.Name : data.Product.ProductName
            };
            return Ok(result);
        }
        /// <summary>
        /// 存入手动录入的生产和发货信息
        /// </summary>
        /// <param name="dto"></param>
        /// <returns></returns>
        [HttpPost("SaveInputProduction", Name = "SaveInputProduction")]
        public async Task<IActionResult> SaveInputProduction([FromBody] PostInputProductionDto dto)
        {
            var order = await _saleOrderService.GetAll().Include(x => x.SaleOrderDetails).FirstOrDefaultAsync(x => x.Id == dto.OrderId);
            if (order == null)
            {
                throw new Exception("销售订单不存在，请刷新重试");
            }
            var detail = order.SaleOrderDetails.FirstOrDefault(x => x.Id == dto.DetailId);
            if (detail == null)
            {
                throw new Exception("销售订单不存在，请刷新重试");
            }
            if (dto.ProductionList.Count() > 0)
            {
                detail.InputProductionInfo = JsonSerializer.Serialize(dto.ProductionList, JsonHelper.DefaultJsonSerializerOptions);
                detail.TotalInputProductionQty = dto.TotalProductionQuantity;

            }
            else
            {
                detail.InputProductionInfo = null;
                detail.TotalInputProductionQty = null;
            }
            if (dto.DeliveryList.Count() > 0)
            {
                detail.InputDeliveryInfo = JsonSerializer.Serialize(dto.DeliveryList, JsonHelper.DefaultJsonSerializerOptions);
                detail.TotalInputDeliveryQty = dto.TotalDeliveryQuantity;
            }
            else
            {
                detail.InputDeliveryInfo = null;
                detail.TotalInputDeliveryQty = null;
            }

            await _unitOfWork.SaveChangeAsync();
            return Ok("操作成功");
        }
        public class PostInputProductionDto
        {
            public long OrderId { get; set; }
            public long DetailId { get; set; }
            public decimal TotalProductionQuantity { get; set; }
            public decimal TotalDeliveryQuantity { get; set; }
            public List<InputProductionDto> ProductionList { get; set; } = new List<InputProductionDto>();
            public List<InputDeliveryDto> DeliveryList { get; set; } = new List<InputDeliveryDto>();
        }

        [HttpGet("GetSaleProductionInventoryList")]
        public async Task<IActionResult> GetSaleProductionInventoryList([FromQuery] long id)
        {
            var saleOrderDetail = await _saleOrderDetailService.GetAll().FirstOrDefaultAsync(x => x.Id == id);
            if (saleOrderDetail == null)
            {
                throw new Exception("订单不存在，请刷新重试");
            }
            var query = _inventoryProductionService.GetAll().Include(x => x.OccupyList)
                        .Include(x => x.Position).ThenInclude(x => x.Location).ThenInclude(x => x.Area)
                        .Include(x => x.TargetInventoryRecords)
                        .Where(x => x.SaleOrderDetailId == saleOrderDetail.Id && x.SourceType == InventorySourceType.报工入库 &&
                            x.InventoryType == InventoryType.入库);
            var data = await _inventoryService.JoinWithSource(query, "SourceType", "SourceId",
                (inventory, source) => new
                {
                    inventory = inventory,
                    sourceNo = source.sourceNo
                }).ToListAsync();
            var records = data.Select(x =>
            {
                //decimal occupyQuantity = 0;
                //if (x.inventory.OccupyList != null)
                //{
                //    occupyQuantity = x.inventory.OccupyList.Where(x => !x.IsCompleted).Sum(x => x.OccupyQuantity - x.ReleaseQuantity);
                //}
                var occupyList = x.inventory.OccupyList?.Where(x => !x.IsCompleted).ToList();
                var inventoryUnitList = _inventoryService.GetInventoryUnitShowDtoList(x.inventory, x.inventory.InventoryUnitInfo, occupyList);
                var positionInfo = x.inventory?.Position != null ? $"{x.inventory.Position.Location.Area.Code ?? ""}{x.inventory.Position.Location.Area.Name}-{x.inventory.Position.Location.Name}-{x.inventory.Position.Name}" : "";

                return new InventoryRecordDto
                {
                    Id = x.inventory!.Id,
                    DataId = x.inventory.DataId,
                    RecordNo = x.inventory.RecordNo,
                    DataType = x.inventory.DataType,
                    SourceType = x.inventory.SourceType,
                    SourceId = x.inventory.SourceId,
                    InventoryType = x.inventory.InventoryType,
                    CreateTime = x.inventory.CreateTime,
                    CreateUserName = x.inventory.CreateUserName,
                    PositionId = x.inventory.PositionId,
                    Quantity = x.inventory.Quantity,
                    UsedQuantity = x.inventory.UsedQuantity,
                    RemainQuantity = x.inventory.RemainQuantity,
                    InboundUnitConversionId = x.inventory.InputUnitConversionId,
                    Price = x.inventory.Price,
                    Remark = x.inventory.Remark,
                    CustomAttributeList = x.inventory.CustomAttribute != null ? JsonSerializer.Deserialize<List<CustomAttributeShowDto>>(x.inventory.CustomAttribute ?? "[]", JsonHelper.DefaultJsonSerializerOptions) : null,
                    DataStatus = x.inventory.DataStatus,
                    SupplierId = x.inventory.SupplierId,
                    MaterialCost = x.inventory.MaterialCost,
                    ProcessingCost = x.inventory.ProcessingCost,
                    SourceNo = x.sourceNo,
                    PositionInfo = positionInfo,
                    InventoryUnitList = inventoryUnitList,
                    Unit = inventoryUnitList?.FirstOrDefault(e => e.Id == -1)?.UnitName, //主单位
                };
            }).ToList();
            //扩展参数
            var systemParaIds = records.SelectMany(x => x.CustomAttributeList ?? []).Select(x => x.systemParaId).Distinct().ToList();
            var systemParaData = _systemParaService.GetAll(true).Where(x => systemParaIds.Contains(x.Id)).ToList();
            var res = new
            {
                records = records,
                systemParaData = systemParaData,
            };

            return Ok(res);
        }
    }
}
