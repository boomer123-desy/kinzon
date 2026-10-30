using Kingzon.OLCM.Base.Services;
using Kingzon.OLCM.Common.Services;
using KingZon.Core;
using KingZon.Core.DataBase;
using KingZon.Core.Web;
using KingZon.OLCM.Base.Service;
using KingZon.OLCM.Base.Shared.DataBase.Entities;
using KingZon.OLCM.Base.Shared.DataBase.JsonStructure;
using KingZon.OLCM.Common.DataBase.Entities;
using KingZon.OLCM.Inventory.Service;
using KingZon.OLCM.Inventory.Shared.DataBase.Entities;
using KingZon.OLCM.Production.Shared.DataBase.Entities;
using KingZon.OLCM.Production.Shared.DataBase.JsonStructure;
using KingZon.OLCM.Sale.Shared.DataBase.Entities;
using KingZon.OLCM.Sale.Shared.DataBase.Enum;
using KingZon.OLCM.Sale.WebApi.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Text.Json;
using System.Transactions;

namespace KingZon.OLCM.Sale.WebApi.Controllers
{

    [Route("api/sale/[controller]")]
    [ApiController]

    public class SaleReturnOrderManageController : KingZonControllerBase
    {
        private readonly SystemParaService _systemParaService;
        private readonly CommonService<Sale_Order> _saleOrderService;
        private readonly CommonService<Sale_OrderDetail> _saleOrderDetailService;
        private readonly CommonService<Base_Goods> _goodsService;
        private readonly ProductService _productService;
        private readonly CommonService<Base_Partner> _customerService;
        private readonly CommonService<Base_ProductSpecification> _specificationService;
        private readonly CommonService<Base_UnitConversion> _unitConversionService;
        private readonly CommonService<Inventory_Record_Production_Ext> _inventoryProductionService;
        private readonly InventoryService _inventoryService;
        private readonly CommonService<Inventory_DeliveryOrder> _inventoryDeliveryOrderService;
        private readonly CommonService<Inventory_DeliveryOrderDetail> _inventoryDeliveryOrderDetailService;
        private readonly CommonService<Sale_ReturnOrderDetail> _saleReturnOrderDetailService;
        private readonly CommonService<Sale_ReturnOrder> _saleReturnOrderService;
        private readonly CommonService<SaleReturnArrivalRecord> _arrivalService;

        private readonly SequenceService _sequenceService;
        private readonly Utils _utils;
        private readonly IUnitOfWork _unitOfWork;

        public SaleReturnOrderManageController(SystemParaService systemParaService,
            CommonService<Sale_Order> saleOrderService,
            CommonService<Sale_OrderDetail> saleOrderDetailService,
            CommonService<Base_Goods> goodsService,
            ProductService productService,
            CommonService<Base_Partner> customerService,
            CommonService<Base_ProductSpecification> specificationService,
            SequenceService sequenceService,
            CommonService<Base_UnitConversion> unitConversionService,
            CommonService<Inventory_Record_Production_Ext> inventoryProductionService,
            InventoryService inventoryService,
            CommonService<Inventory_DeliveryOrder> inventoryDeliveryOrderService,
            CommonService<Inventory_DeliveryOrderDetail> inventoryDeliveryOrderDetailService,
            CommonService<Sale_ReturnOrderDetail> saleReturnOrderDetailService,
            CommonService<Sale_ReturnOrder> saleReturnOrderService,
            CommonService<SaleReturnArrivalRecord> arrivalService,
            Utils utils,
            IUnitOfWork unitOfWork)
        {
            _systemParaService = systemParaService;
            _saleReturnOrderDetailService = saleReturnOrderDetailService;
            _saleReturnOrderService = saleReturnOrderService;
            _sequenceService = sequenceService;
            _saleOrderService = saleOrderService;
            _saleOrderDetailService = saleOrderDetailService;
            _goodsService = goodsService;
            _productService = productService;
            _customerService = customerService;
            _specificationService = specificationService;
            _unitConversionService = unitConversionService;
            _inventoryProductionService = inventoryProductionService;
            _inventoryService = inventoryService;
            _inventoryDeliveryOrderService = inventoryDeliveryOrderService;
            _inventoryDeliveryOrderDetailService = inventoryDeliveryOrderDetailService;
            _arrivalService = arrivalService;
            _utils = utils;
            _unitOfWork = unitOfWork;
        }

        /// <summary>
        /// 获取送货单的filterlist
        /// </summary>
        public class GetDeliveryOrderFilterlistDTO
        {
            public int CustomerId { get; set; }
            public List<Filter> DeliveryOrderDetailFilterList { get; set; } = new List<Filter>();
            public List<Filter> DeliveryOrderFilterList { get; set; } = new List<Filter>();
            public List<Filter> SaleOrderDetailFilterList { get; set; } = new List<Filter>();
            public List<Filter> SaleReturnOrderFilterList { get; set; } = new List<Filter>();
            public List<Filter> SaleReturnOrderDetailFilterList { get; set; } = new List<Filter>();
            public List<Filter> ProductFilterList { get; set; } = new List<Filter>();
        }

        /// <summary>
        /// 获取送货单详情,用于页面显示
        /// </summary>
        /// <param name="filterlistDto"></param>
        /// <returns></returns>
        [HttpPost("GetDeliveryOrder", Name = "GetDeliveryOrder")]
        public async Task<IActionResult> GetDeliveryOrder([FromBody] GetDeliveryOrderFilterlistDTO filterlistDto)
        {
            var options = JsonHelper.DefaultJsonSerializerOptions;
            var deliveryOrder = filterlistDto.DeliveryOrderFilterList ?? [];
            var deliveryOrderDetail = filterlistDto.DeliveryOrderDetailFilterList ?? [];
            var product = filterlistDto.ProductFilterList ?? [];
            var saleOrderDetailFillters = filterlistDto.SaleOrderDetailFilterList ?? [];

            var deliveryResult = from a in _inventoryDeliveryOrderDetailService.GetAll().ApplyFilters(deliveryOrderDetail)
                                 join b in _inventoryDeliveryOrderService.GetAll().ApplyFilters(deliveryOrder) on a.DeliveryOrderId equals b.Id
                                 join c in _productService.GetAll().Include(o => o.BaseUnit).ApplyFilters(product) on a.ProductId equals c.Id
                                 join d in _saleOrderDetailService.GetAll().Include(o => o.SaleOrder).ApplyFilters(saleOrderDetailFillters) on a.SaleOrderDetailId equals d.Id
                                 join e in _specificationService.GetAll() on a.SpecificationId equals e.Id into specifications
                                 from e in specifications.DefaultIfEmpty()
                                 where b.CustomerId == filterlistDto.CustomerId
                                 select new DeliveryOrderDetailDTO
                                 {
                                     Id = a.Id,
                                     DeliveryQuantity = a.DeliveryQuantity,
                                     ActualDeliveryQuantity = a.ActualDeliveryQuantity,
                                     ReturnQuantity = a.ReturnQuantity,
                                     DeliveryTime = b.EstimatedDeliveryTime,
                                     OutboundTime = a.OutboundTime,
                                     DeliveryOrderId = a.DeliveryOrderId,
                                     DeliveryOrderNo = b.DeliveryOrderNo,
                                     SaleOrderDetailPrice = d.Price,
                                     ProductNo = c.ProductNo,
                                     ProductName = c.ProductName,
                                     ProductTechnicalCode = c.TechnicalCode,
                                     ProductCustomerMaterialNo = d.CustomerMaterialNo,
                                     HistoryProductNo = d.GoodsNo,
                                     HistoryProductName = d.GoodsName,
                                     HistoryProductSpecification = d.Specification,
                                     HistoryProductTechnicalCode = d.TechnicalCode,
                                     HistoryCustomerMaterialNo = d.CustomerMaterialNo,
                                     HistoryCustomerMaterialName = d.CustomerMaterialName,
                                     ProductMaterialSpecification = c.MaterialSpecification,
                                     ProductMaterialGrade = c.MaterialGrade,
                                     ProductSpecification = e != null ? e.Specification : "",
                                     ProductUnit = c.BaseUnit.Name,
                                     ProductId = d.ProductId,
                                     SaleOrderNo = d.SaleOrder.OrderNo,
                                     SaleReturnType = null,
                                     UnitInfos = JsonSerializer.Deserialize<List<InventoryUnitDto>>(a.UnitInfo ?? "[]", options)
                                 };
            var newDeliveryOrder = await deliveryResult
                .Where(x => x.ActualDeliveryQuantity - x.ReturnQuantity > 0)
                .ApplySorts(this.SortInfoList)
                .ToPagedListAsync(this.PageModel);
            return Ok(newDeliveryOrder);
        }

        public class GetCustomerDeliveryProductsPostDTO
        {
            public int CustomerId { get; set; }
            public List<Filter> ProductFilterList { get; set; } = new List<Filter>();
            public List<Filter> CustomerMaterialNoFilterList { get; set; } = new List<Filter>();
        }

        [HttpPost("GetCustomers", Name = "GetSaleReturnOrderCustomers")]
        public async Task<IActionResult> GetCustomers()
        {
            var customers = await _customerService.GetAll()
                .Select(x => new
                {
                    x.Id,
                    x.CustomerCode,
                    x.CustomerName,
                })
                .ToListAsync();

            return Ok(customers);
        }

        /// <summary>
        /// 获取送货单详情,用于页面显示
        /// </summary>
        /// <param name="filterlistDto"></param>
        /// <returns></returns>
        [HttpPost("GetCustomerDeliveryProducts", Name = "GetCustomerDeliveryProducts")]
        public async Task<IActionResult> GetCustomerDeliveryProducts([FromBody] GetCustomerDeliveryProductsPostDTO filterlistDto)
        {
            if (filterlistDto.CustomerId <= 0)
            {
                throw new Exception("请选择客户");
            }

            var deliveryDetailQuery = _inventoryDeliveryOrderDetailService.GetAll();
            var saleReturnQuery = _saleReturnOrderDetailService.GetAll();
            var goodsList = from goods in _goodsService.GetAll().ApplyFilters(filterlistDto.CustomerMaterialNoFilterList)
                            join product in _productService.GetAll().ApplyFilters(filterlistDto.ProductFilterList)
                                on goods.ProductId equals product.Id
                            let deliveryOrderDetails = deliveryDetailQuery
                                .Where(dd => dd.GoodsId == goods.Id && dd.ProductId == product.Id &&
                                (!dd.SpecificationId.HasValue || dd.SpecificationId == goods.SpecificationId)).ToList()
                            let latestDelivery = (from deliveryDetail in deliveryDetailQuery
                                                  join deliveryOrder in _inventoryDeliveryOrderService.GetAll()
                                                      on deliveryDetail.DeliveryOrderId equals deliveryOrder.Id
                                                  where deliveryDetail.GoodsId == goods.Id && deliveryDetail.ProductId == product.Id &&
                                                      (!deliveryDetail.SpecificationId.HasValue || deliveryDetail.SpecificationId == goods.SpecificationId)
                                                  orderby deliveryDetail.OutboundTime descending
                                                  select new { deliveryOrder.EstimatedDeliveryTime, deliveryDetail.OutboundTime })
                                .FirstOrDefault()
                            where goods.CustomerId == filterlistDto.CustomerId
                            select new GoodsDeliveryDto
                            {
                                GoodsId = goods.Id,
                                ProductId = product.Id,
                                SpecificationId = goods.SpecificationId,
                                ProductName = product.ProductName,
                                ProductNo = product.ProductNo,
                                CustomerMaterialNo = goods.CustomerMaterialNo,
                                CustomerMaterialName = goods.CustomerMaterialName,
                                TechnicalCode = product.TechnicalCode,
                                DeliveryTime = latestDelivery != null ? latestDelivery.EstimatedDeliveryTime : null,
                                OutboundTime = latestDelivery != null ? latestDelivery.OutboundTime : null,
                                Specification = goods.ProductSpecification != null ? goods.ProductSpecification.Specification : "",
                                DeliveryCount = deliveryOrderDetails.Sum(x => x.ActualDeliveryQuantity),
                                ReturnCount = saleReturnQuery
                                    .Where(x => x.GoodsId == goods.Id)
                                    .Sum(x => x.ReturnQuantity),
                                SalePrice = goods.SalesPrice,
                                Unit = goods.ProductSpecification != null && goods.ProductSpecification.SpecificationUnit != null ? goods.ProductSpecification.SpecificationUnit.Name  : product.BaseUnit.Name,
                                UnitId = (goods.SpecificationId != null ? goods.ProductSpecification!.SpecificationUnitId : goods.Product.BaseUnitId) ?? -1,
                                SaleUnit = goods.SalesUnit.Name,
                                SaleUnitId = goods.SalesUnitId,
                                SalesUnitConversionId = goods.SalesUnitConversionId,
                                MaterialGrade = product.MaterialGrade,
                                MaterialSpecification = product.MaterialSpecification,
                                Remark = product.Remark
                            };
            var res = await goodsList.ApplySorts(this.SortInfoList).ToPagedListAsync(this.PageModel);
            return Ok(res);
        }

        /// <summary>
        /// 根据送货单明细新增销售退货单。
        /// </summary>
        [HttpPost("PostSaleReturnOrder", Name = "PostSaleReturnOrder")]
        public async Task<IActionResult> PostSaleReturnOrder([FromBody] PostSaleReturnOrderDTO postData)
        {
            if (postData.CustomerId <= 0)
            {
                throw new Exception("请选择客户");
            }

            if (postData.Details == null || postData.Details.Count == 0)
            {
                throw new Exception("退货单明细不能为空");
            }

            if (postData.Details.Where(x => x.DeliveryOrderDetailId > 0)
                .GroupBy(x => x.DeliveryOrderDetailId).Any(x => x.Count() > 1))
            {
                throw new Exception("同一送货明细不能重复退货");
            }

            if (postData.Details.Where(x => x.DeliveryOrderDetailId <= 0)
                .GroupBy(x => x.GoodsId).Any(x => x.Key.HasValue && x.Count() > 1))
            {
                throw new Exception("同一客户产品不能重复退货");
            }

            _ = await _customerService.GetByIdAsync(postData.CustomerId)
                ?? throw new Exception("客户不存在");

            using var transaction = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);

            var sequence = _sequenceService.GetDateSequenceNoForUse("SaleReturnOrder", DateTime.Now);
            var orderNo = $"XSTH{DateTime.Now:yyyyMMdd}{sequence.ToString().PadLeft(3, '0')}";
            var saleReturnOrder = new Sale_ReturnOrder(orderNo)
            {
                CustomerId = postData.CustomerId,
                ReturnTime = postData.ReturnTime.Date,
                Remark = postData.Remark,
                OrderTotalPrice = 0,
            };

            var saleReturnOrderDetails = new List<Sale_ReturnOrderDetail>();

            foreach (var item in postData.Details)
            {
                if (item.ReturnQuantity <= 0)
                {
                    throw new Exception("退货数量必须大于 0");
                }

                var (returnQuantity, unitInfo) = GetReturnQuantityInBaseUnit(item);
                Sale_ReturnOrderDetail detail;

                if (item.DeliveryOrderDetailId > 0)
                {
                    var deliveryDetail = await _inventoryDeliveryOrderDetailService.GetByIdAsync(item.DeliveryOrderDetailId)
                        ?? throw new Exception("送货单明细不存在");
                    var deliveryOrder = await _inventoryDeliveryOrderService.GetByIdAsync(deliveryDetail.DeliveryOrderId)
                        ?? throw new Exception("送货单不存在");
                    if (deliveryOrder.CustomerId != postData.CustomerId)
                    {
                        throw new Exception("送货单客户与退货客户不一致");
                    }

                    var availableQuantity = deliveryDetail.ActualDeliveryQuantity - deliveryDetail.ReturnQuantity;
                    if (returnQuantity > availableQuantity)
                    {
                        throw new Exception("退货数量不能大于可退货数量");
                    }

                    var saleOrderDetail = await _saleOrderDetailService.GetByIdAsync(deliveryDetail.SaleOrderDetailId)
                        ?? throw new Exception("销售订单明细不存在");
                    detail = CreateReturnDetail(saleReturnOrder, item, returnQuantity, unitInfo,
                        deliveryDetail.ProductId, deliveryDetail.GoodsId, deliveryDetail.SpecificationId,
                        item.Price ?? 0, saleOrderDetail.CustomerMaterialNo, deliveryDetail.Id);
                    deliveryDetail.ReturnQuantity += returnQuantity;
                }
                else
                {
                    if (!item.GoodsId.HasValue || item.GoodsId <= 0)
                    {
                        throw new Exception("产品直接退货时必须选择客户产品");
                    }

                    var goods = await _goodsService.GetByIdAsync(item.GoodsId.Value)
                        ?? throw new Exception("客户产品不存在");
                    if (goods.CustomerId != postData.CustomerId)
                    {
                        throw new Exception("客户产品与退货客户不一致");
                    }

                    var deliveredQuantity = await _inventoryDeliveryOrderDetailService.GetAll()
                        .Where(x => x.GoodsId == goods.Id).SumAsync(x => x.ActualDeliveryQuantity);
                    var returnedQuantity = await _saleReturnOrderDetailService.GetAll()
                        .Where(x => x.GoodsId == goods.Id)
                        .SumAsync(x => x.ReturnQuantity);
                    if (returnQuantity > deliveredQuantity - returnedQuantity)
                    {
                        throw new Exception("退货数量不能大于可退货数量");
                    }

                    detail = CreateReturnDetail(saleReturnOrder, item, returnQuantity, unitInfo,
                        goods.ProductId, goods.Id, goods.SpecificationId, item.Price ?? 0,
                        goods.CustomerMaterialNo, null);
                }

                saleReturnOrder.OrderTotalPrice += detail.TotalPrice;
                saleReturnOrderDetails.Add(detail);
            }

            await _saleReturnOrderService.InsertAsync(saleReturnOrder);
            await _saleReturnOrderDetailService.InsertAsync(saleReturnOrderDetails);
            await _unitOfWork.SaveChangeAsync();

            transaction.Complete();
            return Ok(saleReturnOrder.Id);
        }

        private static (decimal ReturnQuantity, string? UnitInfo) GetReturnQuantityInBaseUnit(PostSaleReturnOrderDetailDTO item)
        {
            if (item.ReturnUnitId <= 0)
            {
                return (item.ReturnQuantity, JsonSerializer.Serialize(item.UnitInfos));
            }

            var selectedUnit = item.UnitInfos?.SingleOrDefault(x => x.Id == item.ReturnUnitId)
                ?? throw new Exception("退货单位不存在");
            if (selectedUnit.Ratio <= 0)
            {
                throw new Exception("退货单位换算率必须大于 0");
            }
            return (item.ReturnQuantity * selectedUnit.Ratio, JsonSerializer.Serialize(item.UnitInfos));
        }

        private static Sale_ReturnOrderDetail CreateReturnDetail(
            Sale_ReturnOrder order, PostSaleReturnOrderDetailDTO item, decimal returnQuantity, string? unitInfo,
            long productId, long? goodsId, long? specificationId, decimal price, string? customerMaterialNo,
            long? deliveryOrderDetailId) => new()
            {
                SaleReturnOrder = order,
                DeliveryOrderDetailId = deliveryOrderDetailId,
                ProductId = productId,
                GoodsId = goodsId,
                SpecificationId = specificationId,
                ReturnUnitConversionId = item.ReturnUnitId,
                InputReturnQuantity = item.ReturnQuantity,
                UnitInfo = unitInfo,
                ReturnQuantity = returnQuantity,
                Price = price,
                TotalPrice = price * returnQuantity,
                Reason = item.Reason,
                Remark = item.Remark,
                SaleReturnType = item.SaleReturnType,
                CustomerMaterialNo = customerMaterialNo,
            };

        /// <summary>
        /// 获取退货单数据,用于页面显示
        /// </summary>
        /// <param name="FilterList"></param>
        /// <returns></returns>
        [HttpPost("GetSaleReturnOrderDetails", Name = "GetSaleReturnOrderDetails")]
        public async Task<IActionResult> GetSaleReturnOrderDetails([FromBody] GetDeliveryOrderFilterlistDTO filterList)
        {
            var options = JsonHelper.DefaultJsonSerializerOptions;
            var saleReturnOrderFilters = filterList.SaleReturnOrderFilterList ?? [];
            var saleReturnOrderDetailFilters = filterList.SaleReturnOrderDetailFilterList ?? [];
            var deliveryOrderFilters = filterList.DeliveryOrderFilterList ?? [];
            var deliveryOrderDetailFilters = filterList.DeliveryOrderDetailFilterList ?? [];
            var saleOrderDetailFilters = filterList.SaleOrderDetailFilterList ?? [];
            var productFilters = filterList.ProductFilterList ?? [];

            // Keep the filtering sequence consistent with the old controller: first
            // filter return details, then constrain them by delivery/sale-order details.
            var details = _saleReturnOrderDetailService.GetAll()
                .ApplyFilters(saleReturnOrderDetailFilters);

            if (deliveryOrderDetailFilters.Any())
            {
                details = from detail in details
                          join deliveryDetail in _inventoryDeliveryOrderDetailService.GetAll()
                              .ApplyFilters(deliveryOrderDetailFilters)
                              on detail.DeliveryOrderDetailId equals deliveryDetail.Id
                          select detail;
            }

            if (deliveryOrderFilters.Any())
            {
                details = from detail in details
                          join deliveryDetail in _inventoryDeliveryOrderDetailService.GetAll()
                              on detail.DeliveryOrderDetailId equals deliveryDetail.Id
                          join deliveryOrder in _inventoryDeliveryOrderService.GetAll()
                              .ApplyFilters(deliveryOrderFilters)
                              on deliveryDetail.DeliveryOrderId equals deliveryOrder.Id
                          select detail;
            }

            if (saleOrderDetailFilters.Any())
            {
                details = from detail in details
                          join deliveryDetail in _inventoryDeliveryOrderDetailService.GetAll()
                              on detail.DeliveryOrderDetailId equals deliveryDetail.Id
                          join saleOrderDetail in _saleOrderDetailService.GetAll()
                              .ApplyFilters(saleOrderDetailFilters)
                              on deliveryDetail.SaleOrderDetailId equals saleOrderDetail.Id
                          select detail;
            }

            var result = from detail in details
                         join returnOrder in _saleReturnOrderService.GetAll()
                             .ApplyFilters(saleReturnOrderFilters)
                             on detail.SaleReturnOrderId equals returnOrder.Id
                         join customer in _customerService.GetAll()
                             on returnOrder.CustomerId equals customer.Id
                         join product in _productService.GetAll()
                             .Include(x => x.BaseUnit)
                             .ApplyFilters(productFilters)
                             on detail.ProductId equals product.Id
                         join goods in _goodsService.GetAll()
                             on detail.GoodsId equals goods.Id into goodsItems
                         from goods in goodsItems.DefaultIfEmpty()
                         join specification in _specificationService.GetAll()
                             on detail.SpecificationId equals specification.Id into specifications
                         from specification in specifications.DefaultIfEmpty()
                         join deliveryDetail in _inventoryDeliveryOrderDetailService.GetAll()
                             on detail.DeliveryOrderDetailId equals deliveryDetail.Id into deliveryDetails
                         from deliveryDetail in deliveryDetails.DefaultIfEmpty()
                         join deliveryOrder in _inventoryDeliveryOrderService.GetAll()
                             on deliveryDetail.DeliveryOrderId equals deliveryOrder.Id into deliveryOrders
                         from deliveryOrder in deliveryOrders.DefaultIfEmpty()
                         join saleOrderDetail in _saleOrderDetailService.GetAll()
                             on deliveryDetail.SaleOrderDetailId equals saleOrderDetail.Id into saleOrderDetails
                         from saleOrderDetail in saleOrderDetails.DefaultIfEmpty()
                         join saleOrder in _saleOrderService.GetAll()
                             on saleOrderDetail.SaleOrderId equals saleOrder.Id into saleOrders
                         from saleOrder in saleOrders.DefaultIfEmpty()
                         let arrivals = _arrivalService.GetAll(false,null)
                              .Where(a => a.SaleReturnOrderDetailId == detail.Id).ToList()
                         select new SaleReturnOrderDetailViewDTO
                         {
                             Id = detail.Id,
                             ReturnQuantity = detail.ReturnQuantity,
                             InventoryQuantity = detail.InventoryQuantity,
                             InputReturnQuantity = detail.InputReturnQuantity,
                             InputUnitConversionId = detail.ReturnUnitConversionId,
                             Price = detail.Price,
                             InputPrice = detail.Price,
                             TotalPrice = detail.TotalPrice,
                             IsComplete = detail.IsComplete,
                             IsAccount = detail.IsAccount,
                             Reason = detail.Reason,
                             Remark = detail.Remark,
                             SaleReturnType = detail.SaleReturnType,
                             DeliveryOrderDetailId = detail.DeliveryOrderDetailId,
                             DeliveryOrderNo = deliveryOrder != null ? deliveryOrder.DeliveryOrderNo : string.Empty,
                             DeliveryQuantity = deliveryDetail != null ? deliveryDetail.DeliveryQuantity : 0,
                             ActualDeliveryQuantity = deliveryDetail != null ? deliveryDetail.ActualDeliveryQuantity : 0,
                             DeliveryOrderReturnQuantity = deliveryDetail != null ? deliveryDetail.ReturnQuantity : 0,
                             CustomerId = returnOrder.CustomerId,
                             CustomerCode = customer.CustomerCode,
                             CustomerName = customer.CustomerName,
                             SaleReturnOrderId = detail.SaleReturnOrderId,
                             SaleReturnOrderRemark = returnOrder.Remark,
                             SaleReturnOrderReturnTime = returnOrder.ReturnTime,
                             SaleReturnOrderNo = returnOrder.OrderNo,
                             SaleReturnOrderTotalPrice = returnOrder.OrderTotalPrice,
                             ProductId = detail.ProductId,
                             ProductNo = product.ProductNo,
                             ProductName = product.ProductName,
                             ProductTechnicalCode = product.TechnicalCode,
                             ProductMaterialSpecification = product.MaterialSpecification,
                             ProductMaterialGrade = product.MaterialGrade,
                             ProductSpecification = specification != null ? specification.Specification : string.Empty,
                             ProductUnit = product.BaseUnit.Name,
                             ProductCustomerMaterialNo = detail.CustomerMaterialNo ?? saleOrderDetail.CustomerMaterialNo ?? "",
                             SaleOrderNo = saleOrder != null ? saleOrder.OrderNo : string.Empty,
                             ArrivalRecordIds = arrivals.Select(x=>x.Id).ToList(),
                             UnitInfos = detail.UnitInfo == null ? new List<InventoryUnitOutboundDto>() : JsonSerializer.Deserialize<List<InventoryUnitOutboundDto>>(detail.UnitInfo!, options) ?? new List<InventoryUnitOutboundDto>(),
                         };

            var sortedResult = this.SortInfoList == null || !this.SortInfoList.Any()
                ? result.OrderBy(x => x.InventoryQuantity > 0).ThenByDescending(x => x.SaleReturnOrderNo)
                : result.ApplySorts(this.SortInfoList);
            var pagedResult = await sortedResult.ToPagedListAsync(this.PageModel);
            foreach (var item in pagedResult.Data)
            {
                if (item.InputUnitConversionId == -1)
                {
                    continue;
                }

                var unit = item.UnitInfos.FirstOrDefault(
                    x => x.Id == item.InputUnitConversionId
                );

                if (unit == null)
                {
                    continue;
                }

                item.InputPrice *= unit.Ratio;
            }
            return Ok(pagedResult);
        }

        public class IdDto
        {
            public long Id { get; set; }
        }

        /// <summary>
        /// 修改未完成且未入库的退货明细；允许修改单价，不允许修改退货单主表日期、备注。
        /// </summary>
        [HttpPost("UpdateSaleReturnOrderDetail", Name = "UpdateSaleReturnOrderDetail")]
        public async Task<IActionResult> UpdateSaleReturnOrderDetail([FromBody] UpdateSaleReturnOrderDetailDTO updateData)
        {
            var detail = await _saleReturnOrderDetailService.GetByIdAsync(updateData.Id)
                ?? throw new Exception("退货单明细不存在");
            if (detail.IsComplete || detail.InventoryQuantity > 0)
            {
                throw new Exception("已完成或已入库的退货明细不能编辑");
            }
            if (updateData.ReturnQuantity <= 0 || updateData.Price < 0)
            {
                throw new Exception("退货数量必须大于 0，单价不能小于 0");
            }

            var (returnQuantity, unitInfo) = GetReturnQuantityInBaseUnit(new PostSaleReturnOrderDetailDTO
            {
                ReturnQuantity = updateData.ReturnQuantity,
                ReturnUnitId = updateData.ReturnUnitId,
                UnitInfos = updateData.UnitInfos,
            });
            var order = await _saleReturnOrderService.GetByIdAsync(detail.SaleReturnOrderId)
                ?? throw new Exception("退货单不存在");

            using var transaction = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
            if (detail.DeliveryOrderDetailId.HasValue)
            {
                var deliveryDetail = await _inventoryDeliveryOrderDetailService.GetByIdAsync(detail.DeliveryOrderDetailId.Value)
                    ?? throw new Exception("送货单明细不存在");
                var availableQuantity = deliveryDetail.ActualDeliveryQuantity - deliveryDetail.ReturnQuantity + detail.ReturnQuantity;
                if (returnQuantity > availableQuantity)
                {
                    throw new Exception("退货数量不能大于可退货数量");
                }
                deliveryDetail.ReturnQuantity = deliveryDetail.ReturnQuantity - detail.ReturnQuantity + returnQuantity;
            }
            else if (detail.GoodsId.HasValue)
            {
                var deliveredQuantity = await _inventoryDeliveryOrderDetailService.GetAll()
                    .Where(x => x.GoodsId == detail.GoodsId.Value).SumAsync(x => x.ActualDeliveryQuantity);
                var returnedQuantity = await _saleReturnOrderDetailService.GetAll()
                    .Where(x => x.GoodsId == detail.GoodsId.Value && x.Id != detail.Id).SumAsync(x => x.ReturnQuantity);
                if (returnQuantity > deliveredQuantity - returnedQuantity)
                {
                    throw new Exception("退货数量不能大于可退货数量");
                }
            }

            order.OrderTotalPrice = order.OrderTotalPrice - detail.TotalPrice + updateData.Price * returnQuantity;
            detail.ReturnQuantity = returnQuantity;
            detail.InputReturnQuantity = updateData.ReturnQuantity;
            detail.ReturnUnitConversionId = updateData.ReturnUnitId;
            detail.UnitInfo = unitInfo;
            detail.Price = updateData.Price;
            detail.TotalPrice = updateData.Price * returnQuantity;
            detail.SaleReturnType = updateData.SaleReturnType;
            detail.Reason = updateData.Reason;
            detail.Remark = updateData.Remark;
            await _unitOfWork.SaveChangeAsync();
            transaction.Complete();
            return Ok("保存成功");
        }

        /// <summary>
        /// 删除未入库且未完成的退货明细；若为最后一条明细，同时删除退货单。
        /// </summary>
        [HttpPost("DeleteSaleReturnOrderDetail", Name = "DeleteSaleReturnOrderDetail")]
        public async Task<IActionResult> DeleteSaleReturnOrderDetail([FromBody] IdDto id)
        {
            var detail = await _saleReturnOrderDetailService.GetByIdAsync(id.Id)
                ?? throw new Exception("退货单明细不存在");
            if (detail.IsComplete || detail.InventoryQuantity > 0)
            {
                throw new Exception("已完成或已入库的退货明细不能删除");
            }

            using var transaction = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
            var order = await _saleReturnOrderService.GetByIdAsync(detail.SaleReturnOrderId)
                ?? throw new Exception("退货单不存在");
            if (detail.DeliveryOrderDetailId.HasValue)
            {
                var deliveryDetail = await _inventoryDeliveryOrderDetailService.GetByIdAsync(detail.DeliveryOrderDetailId.Value)
                    ?? throw new Exception("送货单明细不存在");
                deliveryDetail.ReturnQuantity -= detail.ReturnQuantity;
            }

            var detailCount = await _saleReturnOrderDetailService.GetAll()
                .CountAsync(x => x.SaleReturnOrderId == order.Id);
            _saleReturnOrderDetailService.Delete(detail);
            if (detailCount == 1)
            {
                _saleReturnOrderService.Delete(order);
            }
            else
            {
                order.OrderTotalPrice -= detail.TotalPrice;
                order.IsComplete = false;
            }

            await _unitOfWork.SaveChangeAsync();
            transaction.Complete();
            return Ok("删除成功");
        }

        /// <summary>
        /// 将一条退货明细标记为完成；当所属退货单全部完成时同步完成主表。
        /// </summary>
        [HttpPost("CompleteSaleReturnOrderDetail", Name = "CompleteSaleReturnOrderDetail")]
        public async Task<IActionResult> CompleteSaleReturnOrderDetail([FromBody] IdDto id)
        {
            var detail = await _saleReturnOrderDetailService.GetByIdAsync(id.Id)
                ?? throw new Exception("退货单明细不存在");
            if (detail.IsComplete)
            {
                throw new Exception("该退货明细已完成");
            }

            var order = await _saleReturnOrderService.GetByIdAsync(detail.SaleReturnOrderId)
                ?? throw new Exception("退货单不存在");
            detail.IsComplete = true;
            var hasUncompletedDetail = await _saleReturnOrderDetailService.GetAll()
                .AnyAsync(x => x.SaleReturnOrderId == order.Id && x.Id != detail.Id && !x.IsComplete);
            if (!hasUncompletedDetail)
            {
                order.IsComplete = true;
            }
            await _unitOfWork.SaveChangeAsync();
            return Ok("确认完成成功");
        }

        /// <summary>
        /// 仅未入库的退货明细可取消完成，防止已进入入库流程的数据被重新编辑。
        /// </summary>
        [HttpPost("CancelCompleteSaleReturnOrderDetail", Name = "CancelCompleteSaleReturnOrderDetail")]
        public async Task<IActionResult> CancelCompleteSaleReturnOrderDetail([FromBody] IdDto id)
        {
            var detail = await _saleReturnOrderDetailService.GetByIdAsync(id.Id)
                ?? throw new Exception("退货单明细不存在");
            if (!detail.IsComplete)
            {
                throw new Exception("该退货明细尚未完成");
            }
            if (detail.InventoryQuantity > 0)
            {
                throw new Exception("已入库的退货明细不能取消完成");
            }

            var order = await _saleReturnOrderService.GetByIdAsync(detail.SaleReturnOrderId)
                ?? throw new Exception("退货单不存在");
            detail.IsComplete = false;
            order.IsComplete = false;
            await _unitOfWork.SaveChangeAsync();
            return Ok("取消完成成功");
        }
    }
}
