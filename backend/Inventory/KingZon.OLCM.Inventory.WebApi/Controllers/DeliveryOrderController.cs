using Kingzon.OLCM.Base.Services;
using Kingzon.OLCM.Base.Services.Dto;
using Kingzon.OLCM.Base.Services.JsonStructure;
using KingZon.Core;
using KingZon.Core.DataBase;
using KingZon.Core.Web;
using KingZon.OLCM.Base.Service.Lock;
using KingZon.OLCM.Base.Shared.DataBase.Entities;
using KingZon.OLCM.Base.Shared.DataBase.Enum;
using KingZon.OLCM.Base.Shared.DataBase.JsonStructure;
using KingZon.OLCM.Common.DataBase.Entities;
using KingZon.OLCM.Common.DataBase.Enum;
using KingZon.OLCM.Inventory.Service;
using KingZon.OLCM.Inventory.Service.Dto;
using KingZon.OLCM.Inventory.Shared.DataBase.Entities;
using KingZon.OLCM.Inventory.Shared.DataBase.Enum;
using KingZon.OLCM.Inventory.WebApi.Dto;
using KingZon.OLCM.Production.Shared.DataBase.JsonStructure;
using KingZon.OLCM.Sale.Service;
using KingZon.OLCM.Sale.Service.Dto;
using KingZon.OLCM.Sale.Shared.DataBase.Entities;
using KingZon.OLCM.Sale.Shared.DataBase.Enum;
using KingZon.OLCM.Sale.Shared.DataBase.JsonStructure;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NetTopologySuite.Index.HPRtree;
using System.Text.Json;
using System.Transactions;

namespace KingZon.OLCM.Inventory.WebApi.Controllers
{
    [Route("api/inventory/[controller]/[action]")]
    public class DeliveryOrderController : KingZonControllerBase
    {
        private readonly SaleService _saleOrderService;
        private readonly CommonService<Sale_OrderDetail> _saleOrderDetailService;
        private readonly CommonService<Base_Partner> _partnerService;
        private readonly CommonService<Base_Product> _productService;
        private readonly InventoryService _inventoryRecordService;
        private readonly CommonService<Inventory_DeliveryOrderDetail> _deliveryOrderDetailService;
        private readonly CommonService<Inventory_DeliveryOrder> _deliveryOrderService;
        private readonly CommonService<Inventory_Occupy> _occupyService;
        private readonly CommonService<Inventory_OccupyDetail> _occupyDetailService;
        private readonly CommonService<Base_SystemPara> _systemParaService;
        private readonly CommonService<Base_ProductSpecification> _productSpecificationService;
        private readonly CommonService<Inventory_Record_RL> _inventoryRecordRLService;
        private readonly CommonService<Inventory_Position> _positionService;
        SequenceService _sequenceService;
        private readonly SaleService _saleService;
        private readonly IUnitOfWork _unitOfWork;

        public DeliveryOrderController(SaleService saleOrderService,
            CommonService<Base_Partner> partnerService,
            CommonService<Sale_OrderDetail> saleOrderDetailService,
            CommonService<Base_Product> productService,
            InventoryService inventoryRecordService,
            CommonService<Inventory_DeliveryOrderDetail> deliveryOrderDetailService,
            CommonService<Inventory_DeliveryOrder> deliveryOrderSevice,
            CommonService<Inventory_Occupy> occupyService,
            CommonService<Inventory_OccupyDetail> occupyDetailService,
            CommonService<Base_SystemPara> systemParaService,
            CommonService<Base_ProductSpecification> productSpecificationService,
            CommonService<Inventory_Record_RL> inventoryRecordRLService,
            CommonService<Inventory_Position> positionService,
            SequenceService sequenceService,
            SaleService saleService,
            IUnitOfWork unitOfWork)
        {
            _saleOrderService = saleOrderService;
            _saleOrderDetailService = saleOrderDetailService;
            _partnerService = partnerService;
            _productService = productService;
            _inventoryRecordService = inventoryRecordService;
            _deliveryOrderDetailService = deliveryOrderDetailService;
            _deliveryOrderService = deliveryOrderSevice;
            _occupyService = occupyService;
            _occupyDetailService = occupyDetailService;
            _systemParaService = systemParaService;
            _productSpecificationService = productSpecificationService;
            _inventoryRecordRLService = inventoryRecordRLService;
            _positionService = positionService;
            _sequenceService = sequenceService;
            _saleService = saleService;
            _unitOfWork = unitOfWork;
        }


      

        //创建自己想要的集合里面的数据结构
        class OrderDetailWithTodoInfo
        {
            public required Sale_OrderDetail SaleOrderDetail { get; set; }
            public required long BaseUnitId { get; set; }
            public decimal TodoQuantity { get; set; }
        }

        //拿到满足发起送货通知的销售订单明细并且拿到对应的待处理的数量
        private IQueryable<OrderDetailWithTodoInfo> _GetSaleOrderDetailWithTodoQuantityQuery()
        {
            var orderTodoList = from delivery in _deliveryOrderDetailService.GetAll()
                               .Where(o => !o.IsCompleteDelivery)
                                group delivery by delivery.SaleOrderDetailId into g
                                select new
                                {
                                    SaleOrderDetailId = g.Key,
                                    TodoQuantity = g.Sum(o => o.DeliveryQuantity - o.ActualDeliveryQuantity), //主单位
                                };
            var q = from detail in _saleOrderDetailService.GetAll().Where(o => o.Status == Sale.Shared.DataBase.Enum.SaleOrderStatus.进行中)
                            .Include(o => o.Product).Include(x => x.ProductSpecification).Include(x => x.SaleOrder)
                    join todo in orderTodoList
                        on detail.Id equals todo.SaleOrderDetailId into tmp
                    from todo in tmp.DefaultIfEmpty()
                    let todoQuantiy = todo.SaleOrderDetailId > 0 ? todo.TodoQuantity : 0
                    select new OrderDetailWithTodoInfo()
                    {
                        BaseUnitId = detail.SpecificationId == null ? detail.Product.BaseUnitId : (long)detail.ProductSpecification!.SpecificationUnitId!,
                        SaleOrderDetail = detail,
                        TodoQuantity = todoQuantiy,
                    };
            return q;
        }


        /// <summary>
        /// 根据销售订单明细Id，获取对应的明细数据对应的库存记录包含库存占用数据
        /// </summary>
        /// <param name="saleOrderDetailIds"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        [HttpPost]
        public async Task<IActionResult> GetSaleOrderDetailForDeliveryByIds([FromBody] List<long>? saleOrderDetailIds, [FromQuery] long? customerId)
        {
            var option = JsonHelper.DefaultJsonSerializerOptions;

            //1.查询所有指定ids对应的SaleOrderDetail明细,并且满足可以发货的条件
            var saleOrderDetailWithTodoQuanity = _GetSaleOrderDetailWithTodoQuantityQuery();
            if (saleOrderDetailIds != null && saleOrderDetailIds.Count > 0)
            {
                saleOrderDetailWithTodoQuanity = saleOrderDetailWithTodoQuanity.Where(o => saleOrderDetailIds.Contains(o.SaleOrderDetail.Id));
            }
            if (customerId.HasValue)
            {
                saleOrderDetailWithTodoQuanity = saleOrderDetailWithTodoQuanity.Where(o => o.SaleOrderDetail.SaleOrder.CustomerId == customerId.Value);
            }
            var saleOrderDetailList = await saleOrderDetailWithTodoQuanity
                    .Where(o => o.SaleOrderDetail.ReviewStatus == ReviewStatus.评审通过 && o.SaleOrderDetail.Status == SaleOrderStatus.进行中) //正常状态的销售单 才能发起送货
                 .Select(detail => new DeliveryOrderDetailListDto
                 {
                     Id = detail.SaleOrderDetail.Id,//用于默认排序
                     CustomerName = detail.SaleOrderDetail.SaleOrder.Customer.CustomerName,
                     SaleOrderDetailId = detail.SaleOrderDetail.Id,
                     GoodsId = detail.SaleOrderDetail.GoodsId,
                     ProductId = detail.SaleOrderDetail.ProductId,
                     SpecificationId = detail.SaleOrderDetail.SpecificationId,
                     SaleOrderNo = detail.SaleOrderDetail.SaleOrder.OrderNo,
                     SaleOrderType = detail.SaleOrderDetail.SaleOrder.OrderType,
                     SaleIsUrgent = detail.SaleOrderDetail.IsUrgent,
                     SaleCustomerAddress = JsonSerializer.Deserialize<CustomerAddressDto>(detail.SaleOrderDetail.SaleOrder.CustomerAddress ?? "", option),
                     DeliveryTime = detail.SaleOrderDetail.DeliveryTime,
                     ProductNo = detail.SaleOrderDetail.GoodsNo,
                     ProductName = detail.SaleOrderDetail.GoodsName,
                     Specification = detail.SaleOrderDetail.Specification,
                     PlaceOrderQuantity = detail.SaleOrderDetail.PlaceOrderQuantity,
                     TotalInputProductionQty = detail.SaleOrderDetail.TotalInputProductionQty ?? 0,
                     PlanDirectDliveryQuantity = detail.SaleOrderDetail.PlanDirectDliveryQuantity,
                     ProductionInventoryQuantityByMain = detail.SaleOrderDetail.ProductionInventoryQuantity,
                     PurchaseInventoryQuantityByMain = detail.SaleOrderDetail.PurchaseInventoryQuantity,
                     TechnicalCode = detail.SaleOrderDetail.TechnicalCode,
                     TodoQuantity = detail.TodoQuantity,
                     UnitConversionList = JsonSerializer.Deserialize<List<SaleUnitConversionInfo>>(detail.SaleOrderDetail.UnitConversionInfo ?? "[]", option),
                     UnitConversionInfo = detail.SaleOrderDetail.UnitConversionInfo,

                     ProductUnitId = detail.SaleOrderDetail.SpecificationId != null ? (detail.SaleOrderDetail.ProductSpecification != null ? detail.SaleOrderDetail.ProductSpecification.SpecificationUnitId : null) : detail.SaleOrderDetail.Product.BaseUnitId,
                     ProductUnit = detail.SaleOrderDetail.ProductSpecification != null ? (detail.SaleOrderDetail.ProductSpecification.SpecificationUnit != null ? detail.SaleOrderDetail.ProductSpecification.SpecificationUnit.Name : null) : detail.SaleOrderDetail.Product.BaseUnit.Name,
                     SalesUnitId = detail.SaleOrderDetail.SalesUnitId,
                     SalesUnit = detail.SaleOrderDetail.SalesUnit,
                     SalesUnitConversionId = detail.SaleOrderDetail.SalesUnitConversionId,
                     DeliveryQtyByMainUnit = detail.SaleOrderDetail.DeliveryNoticeQuantity,
                     ActualDeliveryQtyByMainUnit = detail.SaleOrderDetail.DeliveryQuantity,
                     TodoQtyByMainUnit = detail.TodoQuantity,
                     SaleRemark = detail.SaleOrderDetail.Remark,
                     SaleOrderRemark = detail.SaleOrderDetail.SaleOrder.Remark,
                     SaleCustomerMaterialNo = detail.SaleOrderDetail.CustomerMaterialNo,
                     SaleCustomerOrderNo = detail.SaleOrderDetail.SaleOrder.CustomerOrderNo,
                 }).ApplySorts(this.SortInfoList).ToListAsync();

            var inventoryRecordGroupBySaleOrderDetail = new Dictionary<long, List<InventoryRecordOccupyDetailDto>>();
            if (saleOrderDetailIds != null && saleOrderDetailIds.Count > 0)
            {
                var inventoryRecordData = _inventoryRecordService.GetInventoryRecordOccupyInfo(saleOrderDetailIds.ToArray());
                inventoryRecordGroupBySaleOrderDetail = inventoryRecordData
                    .GroupBy(x => x.SaleOrderDetailId)
                    .ToDictionary(g => g.Key, g => g.ToList());
            }

            var limitData = (from systemPara in _systemParaService.GetAll()
                             where systemPara.DataType == SystemParaType.最大送货量参数
                             select systemPara.DataValue).FirstOrDefault();
            decimal limitDataValue = 0;
            if (limitData != null)
            {
                limitDataValue = decimal.Parse(limitData);
            }

            var res = saleOrderDetailList.Select(x =>
            {
                //订单库存 = 排产已入库数量 + 外购已入库数量 + 计划直接发货数量 + 手动输入的生产总量
                var orderInventory = x.ProductionInventoryQuantity + x.PurchaseInventoryQuantity + x.PlanDirectDliveryQuantity + x.TotalInputProductionQty;
                //订单库存大于0即可发货
                if (orderInventory <= 0)
                {
                    return null;
                }

                var ratio = _saleOrderService.GetSaleToBaseUnitRatio(x.SalesUnitId, x.UnitConversionInfo, x.ProductUnitId);
                x.DeliveryQuantity = ratio != 0 ? Math.Round(x.DeliveryQtyByMainUnit / ratio, 4, MidpointRounding.AwayFromZero) : 0; //送货通知数 售卖单位
                x.ActualDeliveryQuantity = ratio != 0 ? Math.Round(x.ActualDeliveryQtyByMainUnit / ratio, 4, MidpointRounding.AwayFromZero) : 0; //已送货数 售卖单位
                x.TodoQuantity = ratio != 0 ? Math.Round(x.TodoQtyByMainUnit / ratio, 4, MidpointRounding.AwayFromZero) : 0; //待送货数 售卖单位
                x.SaleDeliveryRate = x.PlaceOrderQuantity > 0 ? x.ActualDeliveryQuantity / x.PlaceOrderQuantity : 0;
                //最大可发货数 销售单位
                var maxQuantity = x.PlaceOrderQuantity * (1 + limitDataValue);
                var canDeliveryQuantity = maxQuantity - x.ActualDeliveryQuantity; //待通知数 销售单位

                if (canDeliveryQuantity <= x.TodoQuantity)//如果最大可发货数小于等于待送货数，则不允许发起送货通知
                {
                    return null;
                }
                x.OrderInventory = orderInventory;
                x.MaxDeliveryQuantity = maxQuantity;
                x.SaleToBaseUnitRatio = ratio;
                x.ProductionInventoryQuantity = ratio != 0 ? Math.Round(x.ProductionInventoryQuantityByMain / ratio, 4, MidpointRounding.AwayFromZero) : 0; //生产入库 售卖单位;
                x.PurchaseInventoryQuantity = ratio != 0 ? Math.Round(x.PurchaseInventoryQuantityByMain / ratio, 4, MidpointRounding.AwayFromZero) : 0; //采购入库 售卖单位;

                x.InventoryRecordList = inventoryRecordGroupBySaleOrderDetail.TryGetValue(x.SaleOrderDetailId, out var inventoryRecordList) ? inventoryRecordList : new List<InventoryRecordOccupyDetailDto>();
                return x;
            }).Where(x => x != null).ToList();

            return Ok(res);
        }

        /// <summary>
        /// 根据客户产品Id(GoodsId)列表批量获取可用库存记录（按 goodsId 分组返回）
        /// </summary>
        /// <param name="goodsIds"></param>
        /// <returns></returns>
        [HttpPost]
        public IActionResult GetInventoryRecordByGoodsIds([FromBody] List<long>? goodsIds)
        {
            if (goodsIds == null || goodsIds.Count == 0)
            {
                throw new Exception("请先选择产品");
            }
            var data = _inventoryRecordService.GetInventoryRecordOccupyInfoByGoodsIds(goodsIds.Distinct().ToArray());
            return Ok(data);
        }
        /// <summary>
        /// 获取待处理的数量，让用户勾选时能拿到对销售订单的TodoQuantity
        /// </summary>
        /// <param name="selectRowIds"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> GetSaleOrderDetailWithTodoQuantity([FromBody] List<long> selectRowIds)
        {
            var todoQuantityList = await (from sale in _GetSaleOrderDetailWithTodoQuantityQuery()
                                          where selectRowIds.Contains(sale.SaleOrderDetail.Id)
                                          select new
                                          {
                                              SaleOrderDetail = sale.SaleOrderDetail,
                                              SaleOrderDetailId = sale.SaleOrderDetail.Id,
                                              TodoQtyByMainUnit = sale.TodoQuantity,
                                              BaseUnitId = sale.BaseUnitId
                                          }).ToListAsync();
            var res = todoQuantityList.Select(sale =>
            {
                var x = sale.SaleOrderDetail;
                var ratio = _saleOrderService.GetSaleToBaseUnitRatio(x.SalesUnitId, x.UnitConversionInfo, sale.BaseUnitId);
                var TodoQuantity = ratio != 0 ? Math.Round(sale.TodoQtyByMainUnit / ratio, 4, MidpointRounding.AwayFromZero) : 0; //待送货数 售卖单位
                return new
                {
                    SaleOrderDetailId = sale.SaleOrderDetailId,
                    TodoQuantity = TodoQuantity
                };
            });
            return Ok(res);
        }

        /// <summary>
        /// 获取送货通知订单的基本信息
        /// </summary>
        /// <param name="orderId"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        [HttpGet]
        public async Task<IActionResult> GetDeliveryOrderInfo([FromQuery] long orderId)
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var orderInfo = await (from deliveryOrder in _deliveryOrderService.GetAll().Where(o => o.Id == orderId)
                                    .Include(x => x.Partner)
                                   join pos in _positionService.GetAll()
                                                  on deliveryOrder.PositionId equals pos.Id into posTmp
                                   from pos in posTmp.DefaultIfEmpty()
                                   select new GetDeliveryOrderDto
                                   {
                                       Id = deliveryOrder.Id,
                                       DeliveryOrderNo = deliveryOrder.DeliveryOrderNo,
                                       CustomerId = deliveryOrder.CustomerId,
                                       CustomerName = deliveryOrder.Partner.CustomerName,
                                       CustomAttributesList = JsonSerializer.Deserialize<List<CustomAttribute>>(deliveryOrder.CustomAttribute ?? "[]", options) ?? new List<CustomAttribute>(),
                                       CustomerAddress = JsonSerializer.Deserialize<CustomerAddressDto>(deliveryOrder.CustomerAddress ?? "", options) ?? new CustomerAddressDto(),
                                       Remark = deliveryOrder.Remark,
                                       IsDirectOutbound = deliveryOrder.IsDirectOutbound,
                                       IsInventoryTransfer = deliveryOrder.IsInventoryTransfer,
                                       EstimatedDeliveryTime = deliveryOrder.EstimatedDeliveryTime,
                                       TransferPositionName = pos != null ? $"{pos.Location.Area.AreaNo ?? ""}{pos.Location.Area.Name}-{pos.Location.Name}-{pos.Name}" : null,
                                       TransferPositionId = deliveryOrder.PositionId,
                                       TransferAreaId = pos != null ? pos.Location.StorageLocation_AreaId : null,
                                   }).FirstOrDefaultAsync();
            if (orderInfo == null)
            {
                throw new Exception("送货单不存在，请刷新重试");
            }
            var jsonOption = JsonHelper.DefaultJsonSerializerOptions;

            //2.获取对应的每条明细的信息
            var deliveryOrderDetailList = await (from deliveryOrderDetail in _deliveryOrderDetailService.GetAll().Where(o => o.DeliveryOrderId == orderId)
                                                 join saleDetail in this._GetSaleOrderDetailWithTodoQuantityQuery()
                                                     on deliveryOrderDetail.SaleOrderDetailId equals saleDetail.SaleOrderDetail.Id
                                                 join saleOrder in _saleOrderService.GetAll()
                                                     on saleDetail.SaleOrderDetail.SaleOrderId equals saleOrder.Id
                                                 join occupy in _occupyService.GetAll().Include(x => x.DetailList)
                                                        .Where(x => x.SourceType == OccupySourceType.销售送货 || x.SourceType == OccupySourceType.库存发货暂存)
                                                     on deliveryOrderDetail.Id equals occupy.SourceId into occupyTmp
                                                 from occupy in occupyTmp.DefaultIfEmpty()
                                                 let saleOrderDetail = saleDetail.SaleOrderDetail
                                                 select new DeliveryOrderDetailListDto
                                                 {
                                                     DeliveryOrderDetailId = deliveryOrderDetail.Id,
                                                     SaleOrderDetailId = saleOrderDetail.Id,
                                                     SaleOrderNo = saleOrder.OrderNo,
                                                     SaleCustomerAddress = JsonSerializer.Deserialize<CustomerAddressDto>(saleOrder.CustomerAddress ?? "", jsonOption),
                                                     DeliveryTime = saleOrderDetail.DeliveryTime,
                                                     GoodsId = saleOrderDetail.GoodsId,
                                                     ProductId = deliveryOrderDetail.ProductId,
                                                     ProductName = saleOrderDetail.GoodsName,
                                                     ProductNo = saleOrderDetail.GoodsNo,
                                                     ProductUnitId = saleOrderDetail.SpecificationId != null ? (saleOrderDetail.ProductSpecification != null ? saleOrderDetail.ProductSpecification.SpecificationUnitId : null) : saleOrderDetail.Product.BaseUnitId,
                                                     ProductUnit = saleOrderDetail.SpecificationId != null ? (saleOrderDetail.ProductSpecification != null && saleOrderDetail.ProductSpecification.SpecificationUnit != null ? saleOrderDetail.ProductSpecification!.SpecificationUnit!.Name : "") : saleOrderDetail.Product.BaseUnit.Name,
                                                     SpecificationId = deliveryOrderDetail.SpecificationId != null ? deliveryOrderDetail.SpecificationId : null,
                                                     Specification = saleOrderDetail.Specification,
                                                     TechnicalCode = saleOrderDetail.TechnicalCode,
                                                     SaleCustomerMaterialNo = saleOrderDetail.CustomerMaterialNo,
                                                     SaleCustomerOrderNo = saleOrder.CustomerOrderNo,
                                                     UnitConversionList = JsonSerializer.Deserialize<List<SaleUnitConversionInfo>>(saleOrderDetail.UnitConversionInfo ?? "[]", jsonOption),
                                                     UnitConversionInfo = saleOrderDetail.UnitConversionInfo,
                                                     CustomerName = saleOrder.Customer.CustomerName,
                                                     DeliveryQtyByMainUnit = deliveryOrderDetail.DeliveryQuantity,
                                                     ActualDeliveryQtyByMainUnit = deliveryOrderDetail.ActualDeliveryQuantity,
                                                     PlaceOrderQuantity = saleOrderDetail.PlaceOrderQuantity,
                                                     TotalInputProductionQty = saleOrderDetail.TotalInputProductionQty ?? 0,
                                                     PlanDirectDliveryQuantity = saleOrderDetail.PlanDirectDliveryQuantity,
                                                     ProductionInventoryQuantityByMain = saleOrderDetail.ProductionInventoryQuantity,
                                                     PurchaseInventoryQuantityByMain = saleOrderDetail.PurchaseInventoryQuantity,
                                                     TodoQtyByMainUnit = saleDetail.TodoQuantity - (deliveryOrderDetail.DeliveryQuantity - deliveryOrderDetail.ActualDeliveryQuantity),
                                                     InventoryRecordList = new List<InventoryRecordOccupyDetailDto>(),
                                                     SalesUnit = saleOrderDetail.SalesUnit,
                                                     SalesUnitId = saleOrderDetail.SalesUnitId,
                                                     SalesUnitConversionId = saleOrderDetail.SalesUnitConversionId,
                                                     InputUnitQuantity = deliveryOrderDetail.InputUnitQuantity,
                                                     InputUnitConversionId = deliveryOrderDetail.InputUnitConversionId,
                                                     UnitInfoOutbound = JsonSerializer.Deserialize<List<InventoryUnitOutboundDto>>(deliveryOrderDetail.UnitInfo ?? "[]", jsonOption) ?? new List<InventoryUnitOutboundDto>(),
                                                     OutboundOperator = deliveryOrderDetail.OutboundOperator,
                                                     OccupySourceType = occupy != null ? occupy.SourceType : null,
                                                     OccupyQuantity = occupy != null ? occupy.OccupyQuantity : null,
                                                     OccupyQuantityByMain = occupy != null && occupy.DetailList != null ? occupy.DetailList.Sum(x => x.OccupyQuantity) : null, //大于0 则代表占用了库存
                                                     OccupyUnitConversionId = occupy != null ? occupy.OccupyUnitConversionId : null,
                                                     OccupyUnitInfo = occupy != null ? JsonSerializer.Deserialize<List<InventoryUnitOutboundDto>>(occupy.UnitInfo ?? "[]", jsonOption) ?? new List<InventoryUnitOutboundDto>() : new List<InventoryUnitOutboundDto>(),
                                                     SaleRemark = saleOrderDetail.Remark,
                                                 }).ToListAsync();

            foreach (var x in deliveryOrderDetailList)
            {
                var ratio = _saleOrderService.GetSaleToBaseUnitRatio(x.SalesUnitId, x.UnitConversionInfo, x.ProductUnitId);
                x.DeliveryQuantity = ratio != 0 ? Math.Round(x.DeliveryQtyByMainUnit / ratio, 4, MidpointRounding.AwayFromZero) : 0; //送货数 售卖单位
                x.ActualDeliveryQuantity = ratio != 0 ? Math.Round(x.ActualDeliveryQtyByMainUnit / ratio, 4, MidpointRounding.AwayFromZero) : 0; //已送货数 售卖单位
                x.TodoQuantity = ratio != 0 ? Math.Round(x.TodoQtyByMainUnit / ratio, 4, MidpointRounding.AwayFromZero) : 0; //待送货数 售卖单位
                x.SaleToBaseUnitRatio = ratio;
                x.ProductionInventoryQuantity = ratio != 0 ? Math.Round(x.ProductionInventoryQuantityByMain / ratio, 4, MidpointRounding.AwayFromZero) : 0; //生产入库 售卖单位;
                x.PurchaseInventoryQuantity = ratio != 0 ? Math.Round(x.PurchaseInventoryQuantityByMain / ratio, 4, MidpointRounding.AwayFromZero) : 0; //采购入库 售卖单位;
            }
            var saleOrderIds = deliveryOrderDetailList.GroupBy(o => o.SaleOrderDetailId).Select(o => o.Key).ToArray();
            var inventoryRecordWithOccupyList = _inventoryRecordService.GetInventoryRecordOccupyInfo(orderId);

            var inventoryOccupyQtyByDelivery = inventoryRecordWithOccupyList.Where(x => x.DeliveryDetailOccupyQuantity > 0 || x.DeliveryDetailOutboundOccupyQuantity > 0)
              .GroupBy(x => x.InventoryRecordId).Select(x => new
              {
                  InventoryRecordId = x.Key,
                  OccupyQuantity = x.Sum(e => e.DeliveryDetailOutboundOccupyQuantity + e.DeliveryDetailOccupyQuantity)
              }).ToList();
            var inventoryRecordGroupByDeliveryOrderDetail = inventoryRecordWithOccupyList.Select(x =>
            {
                var qty = inventoryOccupyQtyByDelivery.FirstOrDefault(e => e.InventoryRecordId == x.InventoryRecordId)?.OccupyQuantity ?? 0;
                x.OriginRemainingQuantity = x.RemainingQuantity;
                x.DeliveryTotalOccupyQuantity = qty;
                x.RemainingQuantity = x.RemainingQuantity - qty;
                x.OccupyQuantity = x.OccupyQuantity - qty;
                return x;
            }).GroupBy(x => (long)x.DeliveryOrderDetailId!)
               .ToDictionary(g => g.Key, g => g.ToList());

            foreach (var detail in deliveryOrderDetailList)
            {
                if (inventoryRecordGroupByDeliveryOrderDetail.TryGetValue(detail.DeliveryOrderDetailId, out var inventoryRecordList))
                {
                    detail.InventoryRecordList = inventoryRecordList;
                }
            }
            orderInfo.DeliveryOrderDetailList = deliveryOrderDetailList;
            return Ok(orderInfo);
        }
        /// <summary>
        /// 删除送货通知单
        /// </summary>
        /// <param name="Id"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<IActionResult> DeleteDeliveryOrder([FromQuery] long Id)
        {
            using var transaction = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
            var order = await _deliveryOrderService.GetByIdAsync(Id);
            if (order == null)
            {
                throw new Exception("该送货订单不存在,请刷新页面");
            }
            var orderDetail = await _deliveryOrderDetailService.GetAll().Where(o => o.DeliveryOrderId == Id).ToListAsync();
            bool hasActualDelivery = orderDetail.Any(o => o.ActualDeliveryQuantity > 0);
            
            if (hasActualDelivery)
            {
                throw new Exception("该通知单已有部分明细已经出库，无法删除!");
            }
            var orderDetailIds = orderDetail.Select(o => o.Id);

            var occupy = await _occupyService.GetAll().Where(o => orderDetailIds.Contains(o.SourceId) && (o.SourceType ==OccupySourceType.销售送货 || o.SourceType == OccupySourceType.库存发货暂存 )).ToListAsync();
            var occupyIds = occupy.Select(o => o.Id).ToList();
            var occupyDetails = await _occupyDetailService.GetAll().Where(o => occupyIds.Contains(o.OccupyId) && !o.IsCompleted).ToListAsync();
            _occupyDetailService.Delete(occupyDetails, false);
            _occupyService.Delete(occupy, false);
            _deliveryOrderDetailService.Delete(orderDetail, false);
            _deliveryOrderService.Delete(order, false);
            await _unitOfWork.SaveChangeAsync();
            //更新销售订单上的统计数据
            var saleOrderDetailIds = orderDetail.Select(o => o.SaleOrderDetailId).Distinct().ToList();
            lock (QuantityLock.SaleQuantityLock)
            {
                _saleService.UpdateSaleDelivery(saleOrderDetailIds);
                _unitOfWork.SaveChange();
                transaction.Complete();
            }
            return Ok("删除成功");
        }

        /// <summary>
        /// 新增一个送货通知
        /// </summary>
        /// <param name="postData"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        [HttpPost]
        public async Task<IActionResult> CreateDeliveryOrder([FromBody] CreateOrUpdateDeliveryOrderPostDto postData)
        {
            //验证数据的合法性
            _ValidateDeliveryOrderData(postData);

            using var transaction = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
            //新增送货单
            var deliveryOrder = new Inventory_DeliveryOrder()
            {
                DeliveryOrderNo = _sequenceService.GenerateCode(TableName.Inventory_DeliveryOrder),
                CustomerId = postData.CustomerId,
                CustomerAddress = JsonSerializer.Serialize(postData.CustomerAddress, JsonHelper.DefaultJsonSerializerOptions),
                EstimatedDeliveryTime = postData.EstimatedDeliveryTime,
                CustomAttribute = JsonSerializer.Serialize(postData.CustomAttributeList, JsonHelper.DefaultJsonSerializerOptions),
                IsDirectOutbound = postData.IsDirectOutbound,
                IsInventoryTransfer = postData.IsInventoryTransfer,
                PositionId = postData.TransferPositionId,
                Remark = postData.Remark,
                //运费？？
            };
            _deliveryOrderService.Insert(deliveryOrder);

            //新增送货明细
            foreach (var detail in postData.DeliveryOrderDetailList)
            {
                this.AddDeliveryDetail(deliveryOrder, detail);
            }
            await _unitOfWork.SaveChangeAsync();

            //更新销售订单上的统计数据
            var saleOrderDetailIds = postData.DeliveryOrderDetailList.Select(o => o.SaleOrderDetailId).Distinct().ToList();
            lock (QuantityLock.SaleQuantityLock)
            {
                _saleService.UpdateSaleDelivery(saleOrderDetailIds);
                _unitOfWork.SaveChange();
                transaction.Complete();
            }

            return Ok("新增成功");
        }
        private void AddDeliveryDetail(Inventory_DeliveryOrder deliveryOrder,CreateOrUpdateDeliveryDetailPostDto detail) {
            var deliveryOrderDetail = new Inventory_DeliveryOrderDetail()
            {
                DeliveryOrder = deliveryOrder,
                GoodsId = detail.GoodsId,
                ProductId = detail.ProductId,
                SpecificationId = detail.SpecificationId,
                SaleOrderDetailId = detail.SaleOrderDetailId,
                DeliveryQuantity = detail.DeliveryQuantity,
                InputUnitConversionId = detail.InputUnitConversionId,
                InputUnitQuantity = detail.InputQuantity,
                UnitInfo = JsonSerializer.Serialize(detail.UnitInfo, JsonHelper.DefaultJsonSerializerOptions)
            };
            _deliveryOrderDetailService.Insert(deliveryOrderDetail);
            _unitOfWork.SaveChange();
            //有分配的情况下新增库存占用记录
            if (detail.IsAllocate)
            {
                this.AddDeliverySaleOccupy(deliveryOrderDetail, detail);
            }
        }
        private void AddDeliverySaleOccupy(Inventory_DeliveryOrderDetail deliveryOrderDetail, CreateOrUpdateDeliveryDetailPostDto detail) {
            if (detail.InputQuantity > 0)
            {//库存占用表新增记录
                var inputUnitInfo = detail.UnitInfo.FirstOrDefault(u => u.Id == detail.InputUnitConversionId);
                if (inputUnitInfo == null)
                {
                    throw new Exception($"销售单{detail.SaleOrderNo}【产品{detail.ProductNo}】送货单位错误，请刷新重试");
                }
                var inventoryOccupy = new Inventory_Occupy()
                {
                    SourceType = OccupySourceType.销售送货,
                    SourceId = deliveryOrderDetail.Id,
                    OccupyQuantity = detail.InputQuantity,
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
                    //库存占用明细表新增记录
                    this.AddDeliverySaleOccupyDetail(inventoryOccupy, occupy);
                }
            }
        }
        private void AddDeliverySaleOccupyDetail(Inventory_Occupy inventoryOccupy, CreateOrUpdateDeliveryInventoryPostDto occupy) {
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

        [HttpPost]
        /// <summary>
        /// 修改送货通知单
        /// </summary>
        /// <returns></returns>
        public async Task<IActionResult> UpdateDeliveryOrder([FromBody] CreateOrUpdateDeliveryOrderPostDto postData)
        {
            var deliveryOrder = await _deliveryOrderService.GetByIdAsync(postData.Id);
            if (deliveryOrder == null)
            {
                throw new Exception("送货单不存在，请刷新重试");
            }
            if (deliveryOrder.IsCompleteDelivery)
            {
                throw new Exception("送货单已出库，不能编辑");
            }
            using var transaction = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);

            var deliveryDetailData = _deliveryOrderDetailService.GetAll().Where(x => x.DeliveryOrderId == deliveryOrder.Id).ToList();
            _ValidateDeliveryOrderData(postData, deliveryDetailData);

            //修改 送货通知单的基础信息
            deliveryOrder.CustomerId = postData.CustomerId;
            deliveryOrder.CustomerAddress = JsonSerializer.Serialize(postData.CustomerAddress, JsonHelper.DefaultJsonSerializerOptions);
            deliveryOrder.EstimatedDeliveryTime = postData.EstimatedDeliveryTime;
            deliveryOrder.CustomAttribute = JsonSerializer.Serialize(postData.CustomAttributeList, JsonHelper.DefaultJsonSerializerOptions);
            deliveryOrder.IsDirectOutbound = postData.IsDirectOutbound;
            deliveryOrder.IsInventoryTransfer = postData.IsInventoryTransfer;
            deliveryOrder.PositionId = postData.TransferPositionId;
            deliveryOrder.Remark = postData.Remark;

            var detialIds = deliveryDetailData.Select(x => x.Id).ToList();
            var occupyData = _occupyService.GetAll().Include(x => x.DetailList)
                .Where(x => (x.SourceType == OccupySourceType.销售送货 || x.SourceType == OccupySourceType.库存发货暂存) && detialIds.Contains(x.SourceId)).ToList();
            //新增、编辑 送货明细
            foreach (var detail in postData.DeliveryOrderDetailList)
            {
                var oldDetail = deliveryDetailData.FirstOrDefault(x => x.Id == detail.DeliveryOrderDetailId);
                var oldOccupy = occupyData.FirstOrDefault(x => x.SourceId == detail.DeliveryOrderDetailId);
                //新增送货明细
                if (oldDetail == null)
                {
                    if (oldOccupy != null)
                    {
                        _occupyDetailService.Delete(oldOccupy.DetailList ?? new List<Inventory_OccupyDetail>());
                        _occupyService.Delete(oldOccupy);
                    }
                    AddDeliveryDetail(deliveryOrder, detail);
                    continue;
                }

                if (oldDetail.IsCompleteDelivery)
                { //送货已出库 不能修改
                    continue;
                }

                //编辑送货明细
                oldDetail.DeliveryQuantity = detail.DeliveryQuantity;
                oldDetail.InputUnitConversionId = detail.InputUnitConversionId;
                oldDetail.InputUnitQuantity = detail.InputQuantity;
                oldDetail.UnitInfo = JsonSerializer.Serialize(detail.UnitInfo, JsonHelper.DefaultJsonSerializerOptions);
                //处理占用
                if (detail.IsAllocate && detail.InputQuantity > 0) //有库存占用
                {
                    if (oldOccupy == null)
                    {
                        this.AddDeliverySaleOccupy(oldDetail, detail);
                    }
                    else
                    {
                        var inputUnitInfo = detail.UnitInfo.FirstOrDefault(u => u.Id == detail.InputUnitConversionId);
                        if (inputUnitInfo == null)
                        {
                            throw new Exception($"销售单{detail.SaleOrderNo}【产品{detail.ProductNo}】送货单位错误，请刷新重试");
                        }
                        oldOccupy.SourceType = OccupySourceType.销售送货;
                        oldOccupy.OccupyQuantity = detail.InputQuantity;
                        oldOccupy.OccupyUnitConversionId = detail.InputUnitConversionId;
                        oldOccupy.OccupyUnitId = inputUnitInfo.UnitId;
                        oldOccupy.OccupyUnit = inputUnitInfo.UnitName;
                        oldOccupy.UnitInfo = JsonSerializer.Serialize(detail.UnitInfo, JsonHelper.DefaultJsonSerializerOptions);
                        //新增、编辑 占用明细
                        foreach (var occupy in detail.InventoryRecordList)
                        {
                            var oldOccupuDetail = oldOccupy!.DetailList?.FirstOrDefault(x => x.InventoryRecordId == occupy.InventoryRecordId);
                            if (oldOccupuDetail == null)
                            {
                                //库存占用明细表新增记录
                                this.AddDeliverySaleOccupyDetail(oldOccupy, occupy);
                            }
                            else {
                                oldOccupuDetail.OccupyQuantity = occupy.OccupyQuantity;
                                oldOccupuDetail.InputQuantity = occupy.InputQuantity;
                                oldOccupuDetail.InputUnitConversionId = occupy.InputUnitConversionId;
                                oldOccupuDetail.UnitInfo = JsonSerializer.Serialize(occupy.UnitInfo, JsonHelper.DefaultJsonSerializerOptions);
                            }
                        }
                        //删除占用明细
                        var delOccupyDetailData = oldOccupy.DetailList?.Where(x => !detail.InventoryRecordList.Any(e => e.InventoryRecordId == x.InventoryRecordId)).ToList();
                        if (delOccupyDetailData != null && delOccupyDetailData.Count > 0)
                        {
                            _occupyDetailService.Delete(delOccupyDetailData);
                        }
                    }
                }
                else
                {
                    if (oldOccupy != null)
                    {
                        _occupyDetailService.Delete(oldOccupy.DetailList ?? new List<Inventory_OccupyDetail>());
                        _occupyService.Delete(oldOccupy);
                    }
                }
            }
            
            //删除送货明细
            var delDeliveryDetailData = deliveryDetailData.Where(x => !postData.DeliveryOrderDetailList.Any(e => e.DeliveryOrderDetailId == x.Id)).ToList();
            var delOccupyData = occupyData.Where(x => delDeliveryDetailData.Any(e => e.Id == x.SourceId)).ToList();
            _occupyDetailService.Delete(delOccupyData.SelectMany(x => x.DetailList ?? new List<Inventory_OccupyDetail>()).ToList());
            _occupyService.Delete(delOccupyData);
            _deliveryOrderDetailService.Delete(delDeliveryDetailData);

            await _unitOfWork.SaveChangeAsync();

            //更新销售订单上的统计数据
            var saleOrderDetailIds = postData.DeliveryOrderDetailList.Select(o => o.SaleOrderDetailId).Distinct().ToList();
            lock (QuantityLock.SaleQuantityLock)
            {
                _saleService.UpdateSaleDelivery(saleOrderDetailIds);
                _unitOfWork.SaveChange();
                transaction.Complete();
            }

            return Ok("操作成功");
        }
        /// <summary>
        /// 新增或修改时，验证提交的DevliveryOrder数据
        /// </summary>
        /// <param name="postData"></param>
        /// <exception cref="Exception"></exception>
        private void _ValidateDeliveryOrderData(CreateOrUpdateDeliveryOrderPostDto postData, List<Inventory_DeliveryOrderDetail>? deliveryDetailData = null)
        {
            //判断每一条明细的本次送货量是否大于订单数量
            //先获取对应的限制的参数
            if (postData.DeliveryOrderDetailList == null || postData.DeliveryOrderDetailList.Count == 0)
            {
                throw new Exception("请至少添加一条明细数据！");
            }
            if (postData.IsInventoryTransfer && postData.TransferPositionId == null)
            {
                throw new Exception("请选择调拨的库房库区库位");
            }
            if (postData.TransferPositionId != null)
            {
                var postion = _positionService.GetAll().FirstOrDefault(x => x.Id == postData.TransferPositionId);
                if (postion == null)
                {
                    throw new Exception("请选择正确的库位不存在，请刷新重试");
                }
            }
            var limitData = (from systemPara in _systemParaService.GetAll()
                             where systemPara.DataType == SystemParaType.最大送货量参数
                             select systemPara.DataValue).FirstOrDefault();
            decimal limitDataValue = 0;
            if (limitData != null)
            {
                limitDataValue = decimal.Parse(limitData);
            }
            var saleDetailIds = postData.DeliveryOrderDetailList.Select(x => x.SaleOrderDetailId).Distinct().ToList();
            var saleDetailData = _GetSaleOrderDetailWithTodoQuantityQuery().Where(x => saleDetailIds.Contains(x.SaleOrderDetail.Id)).ToList();
            var inventoryIds = postData.DeliveryOrderDetailList.Where(x => x.InventoryRecordList != null && x.InventoryRecordList.Any()).SelectMany(x => x.InventoryRecordList.Select(y => y.InventoryRecordId)).Distinct().ToList();
            var inventoryData = _inventoryRecordService.GetAll().Where(x => inventoryIds.Contains(x.Id)).ToList();
            //把传回来的每一条明细的数据去判断是否满足
            foreach (var deliveryDetail in postData.DeliveryOrderDetailList)
            {
                if (deliveryDetail.InputQuantity <= 0)
                {
                    continue;
                }
                var saleDetail = saleDetailData.FirstOrDefault(x => x.SaleOrderDetail.Id == deliveryDetail.SaleOrderDetailId);
                if (saleDetail == null)
                {
                    continue;
                }
                var maxCanDeliveryQtyBySale = saleDetail.SaleOrderDetail.PlaceOrderQuantity * (1 + limitDataValue);
                var noticedQuantityByMain = saleDetail.SaleOrderDetail.DeliveryQuantity + saleDetail.TodoQuantity;
                var ratio = _saleOrderService.GetSaleToBaseUnitRatio(saleDetail.SaleOrderDetail.SalesUnitId, saleDetail.SaleOrderDetail.UnitConversionInfo, deliveryDetail.ProductUnitId);
                var noticedQuantityBySale = ratio != 0 ? noticedQuantityByMain / ratio : 0;
                var canDeliveryQuantity = maxCanDeliveryQtyBySale - noticedQuantityBySale;
                var oldDeliveryDetail = deliveryDetailData?.FirstOrDefault(x => x.Id == deliveryDetail.DeliveryOrderDetailId);
                if (oldDeliveryDetail != null)
                {
                    var detailUnDeliveryByMain = oldDeliveryDetail.DeliveryQuantity - oldDeliveryDetail.ActualDeliveryQuantity;
                    var detailUnDeliveryBySale = ratio != 0 ? detailUnDeliveryByMain / ratio : 0;
                    canDeliveryQuantity += detailUnDeliveryBySale;
                }
                if (canDeliveryQuantity < deliveryDetail.InputQuantityBySale)
                {
                    throw new Exception($"销售单{deliveryDetail.SaleOrderNo}【产品{deliveryDetail.ProductNo}】的差欠总量为{canDeliveryQuantity}{deliveryDetail.SalesUnit}，本次送货量{deliveryDetail.InputQuantityBySale}{deliveryDetail.SalesUnit}已超出");
                }
                if (deliveryDetail.IsAllocate)
                {
                    if (deliveryDetail.InventoryRecordList == null || !deliveryDetail.InventoryRecordList.Any())
                    {
                        throw new Exception($"销售单{deliveryDetail.SaleOrderNo}【产品{deliveryDetail.ProductNo}】请至少选择一条库存进行占用!");
                    }
                    //分配量是否等于发货量
                    decimal totalOccupyQtyByMain = deliveryDetail.InventoryRecordList?.Sum(o => o.OccupyQuantity) ?? 0;
                    if (totalOccupyQtyByMain != deliveryDetail.DeliveryQuantity)
                    {
                        throw new Exception($"销售单{deliveryDetail.SaleOrderNo}【产品{deliveryDetail.ProductNo}】库存占用总量与送货量不等，请检查");
                    }
                    //库存分配量 是否小于库存余量
                    foreach (var occupy in deliveryDetail.InventoryRecordList!)
                    {
                        var inventoryRecord = inventoryData.FirstOrDefault(x => x.Id == occupy.InventoryRecordId);
                        if (inventoryRecord == null)
                        {
                            throw new Exception($"销售单{deliveryDetail.SaleOrderNo}【产品{deliveryDetail.ProductNo}】占用的库存{occupy.InventoryRecordNo}不存在，，请刷新重试");
                        }
                        if (occupy.OccupyQuantity > inventoryRecord.RemainQuantity)
                        {
                            throw new Exception($"销售单{deliveryDetail.SaleOrderNo}【产品{deliveryDetail.ProductNo}】占用的库存{occupy.InventoryRecordNo}余量不足，请刷新重试");
                        }
                    }
                }
            }
        }
    }
}