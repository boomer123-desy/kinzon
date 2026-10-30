using KingZon.Core.DataBase;
using KingZon.OLCM.Common.DataBase.Entities;
using Kingzon.OLCM.Common.Services;
using KingZon.OLCM.Inventory.DataBase.Entities;
/*using KingZon.OLCM.Production.DataBase.Entities;*/
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using X.PagedList;
using KingZon.Core.Web;
using KingZon.OLCM.Sales.DataBase.Entities;
using KingZon.OLCM.Sales.WebApi.Dto;
using KingZon.OLCM.Sales.WebApi;
using KingZon.OLCM.Sales.DataBase;
using System.Transactions;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;
using KingZon.OLCM.Common.DataBase.Enum;
using Microsoft.Identity.Client;
using NetTopologySuite.Index.HPRtree;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace KingZon.OLCM.Common.WebApi.Controllers
{
    [Route("api/sales/[controller]")]
    public class SaleReturnOrderController : KingZonControllerBase
    {
        private readonly IUnitOfWork _unitWork;
        private readonly CommonService<SaleOrder> _saleOrder;
        private readonly CommonService<SaleOrderDetail> _saleOrderDetail;
        private readonly CommonService<DeliveryOrder> _deliveryOrder;
        private readonly CommonService<DeliveryOrderDetail> _deliveryOrderDetail;
        private readonly CommonService<Customer> _customerService;
        private readonly CommonService<HistoryProduct> _histotyProduct;
        private readonly CommonService<Product> _productService;
        private readonly CommonService<SaleReturnOrder> _saleReturnOrder;
        private readonly CommonService<SaleReturnOrderDetail> _saleReturnOrderDetail;
        private readonly CommonService<Customer_Product_RL> _customer_Product_RLService;
        private readonly SequenceService _sequenceService;
        /*  private readonly CommonService<Production> _production;*/

        public SaleReturnOrderController(
            CommonService<SaleOrder> saleOrder,
            CommonService<SaleOrderDetail> saleOrderDetail,
            IUnitOfWork unitWork,
            CommonService<DeliveryOrder> deliveryOrder,
            CommonService<DeliveryOrderDetail> deliveryOrderDetail,
            CommonService<Customer> customerService,
            CommonService<HistoryProduct> histotyProduct,
            CommonService<Product> productService,
            CommonService<SaleReturnOrder> saleReturnOrder,
            CommonService<SaleReturnOrderDetail> saleReturnOrderDetail,
            CommonService<Customer_Product_RL> customer_Product_RLService,
            SequenceService sequenceService
            )
        {
            _saleOrder = saleOrder;
            _saleOrderDetail = saleOrderDetail;
            _unitWork = unitWork;
            _deliveryOrder = deliveryOrder;
            _deliveryOrderDetail = deliveryOrderDetail;
            _customerService = customerService;
            _histotyProduct = histotyProduct;
            _saleReturnOrder = saleReturnOrder;
            _saleReturnOrderDetail = saleReturnOrderDetail;
            _customer_Product_RLService = customer_Product_RLService;
            _sequenceService = sequenceService;
            _productService = productService;
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
            public List<Filter> HistoryProductFilterList { get; set; } = new List<Filter>();
            public List<Filter> SaleReturnOrderFilterList { get; set; } = new List<Filter>();
            public List<Filter> SaleReturnOrderDetailFilterList { get; set; } = new List<Filter>();
            public List<Filter> CustomerMaterialNoFilterList { get; set; } = new List<Filter>();

        }
        /// <summary>
        /// 获取送货单详情,用于页面显示
        /// </summary>
        /// <param name="filterlistDto"></param>
        /// <returns></returns>
        [HttpPost("GetDeliveryOrder", Name = "GetDeliveryOrder")]
        public async Task<IActionResult> GetDeliveryOrder([FromBody] GetDeliveryOrderFilterlistDTO filterlistDto)
        {
            var deliveryOrder = filterlistDto.DeliveryOrderFilterList ?? [];
            var deliveryOrderDetail = filterlistDto.DeliveryOrderDetailFilterList ?? [];
            var histotyProduct = filterlistDto.HistoryProductFilterList ?? [];
            var saleOrderDetailFillters = filterlistDto.SaleOrderDetailFilterList ?? [];

            var deliveryResult = from a in _deliveryOrderDetail.GetAll().ApplyFilters(deliveryOrderDetail)
                                 join b in _deliveryOrder.GetAll().ApplyFilters(deliveryOrder) on a.DeliveryOrderId equals b.Id
                                 join c in _histotyProduct.GetAll().ApplyFilters(histotyProduct) on a.HistoryProductId equals c.Id
                                 join d in _saleOrderDetail.GetAll().Include(o => o.SaleOrder).ApplyFilters(saleOrderDetailFillters) on a.SaleOrderDetailId equals d.Id
                                 where b.CustomerId == filterlistDto.CustomerId
                                 select new DeliveryOrderDetailDTO()
                                 {
                                     Id = a.Id,
                                     DeliveryQuantity = a.DeliveryQuantity,
                                     CustomerUsedQuantity = a.CustomerUsedQuantity,
                                     ReturnQuantity = a.ReturnQuantity,
                                     AccountQuantity = a.AccountQuantity,
                                     DeliveryNoticeNo = a.DetailNo,
                                     DeliveryTime = b.DeliveryTime,
                                     Remark = a.Remark,
                                     ProductionOrderNo = a.ProductionOrderNo,
                                     SaleOrderDetailId = a.SaleOrderDetailId,
                                     DeliveryOrderId = a.DeliveryOrderId != null ? (int)a.DeliveryOrderId : 0,
                                     DeliveryOrderNo = b.OrderNo,
                                     ProductionId = a.ProductionId > 0 ? (int)a.ProductionId! : 0,
                                     SaleOrderDetailPrice = d.Price,
                                     HistoryProductId = a.HistoryProductId,
                                     HistoryProductNo = c.ProductNo,
                                     HistoryProductName = c.ProductName,
                                     HistoryProductCustomerMaterialNo = d.CustomerMaterialNo,
                                     HistoryProductMaterialSpecification = c.MaterialSpecification,
                                     HistoryProductMaterialGrade = c.MaterialGrade,
                                     HistoryProductSpecification = c.Specification ?? "",
                                     HistoryProductUnit = c.Unit,
                                     HistoryProductVersion = c.Version,
                                     ProductId = d.ProductId ?? 0,
                                     SaleOrderNo = d.SaleOrder.OrderNo,
                                     SaleReturnType = null,
                                 };
            var newDeliveryOrder = await deliveryResult.Where(x => x.DeliveryQuantity - x.ReturnQuantity > 0).ApplySorts(this.SortInfoList).ToPagedListAsync(this.PageModel);
            return Ok(newDeliveryOrder);
        }


        /// <summary>
        /// 获取退货单数据,用于页面显示
        /// </summary>
        /// <param name="FilterList"></param>
        /// <returns></returns>
        [HttpPost("GetSaleReturnOrderDetails", Name = "GetSaleReturnOrderDetails")]
        public async Task<IActionResult> GetSaleReturnOrderDetails([FromBody] GetDeliveryOrderFilterlistDTO filterList)
        {
            var saleReturn = filterList.SaleReturnOrderFilterList ?? [];
            var saleReturnDetail = filterList.SaleReturnOrderDetailFilterList ?? [];
            var delivery = filterList.DeliveryOrderFilterList ?? [];
            var historyProduct = filterList.HistoryProductFilterList ?? [];
            var deliveryDetail = filterList.DeliveryOrderDetailFilterList ?? [];
            var details = _saleReturnOrderDetail.GetAll().ApplyFilters(saleReturnDetail);

            if (deliveryDetail.Any())
            {
                details = from info in details
                          join deliveryDetailInfo in _deliveryOrderDetail.GetAll().ApplyFilters(deliveryDetail)
                          on info.DeliveryOrderDetailId equals deliveryDetailInfo.Id
                          select info;
            }
            if (filterList.SaleOrderDetailFilterList.Any())
            {
                details = from info in details.Include(o => o.DeliveryOrderDetail)
                          join saleOrderDetail in _saleOrderDetail.GetAll().ApplyFilters(filterList.SaleOrderDetailFilterList)
                          on info.DeliveryOrderDetail!.SaleOrderDetailId equals saleOrderDetail.Id
                          where info.DeliveryOrderDetailId != null
                          select info;
            }
            if (filterList.CustomerMaterialNoFilterList.Any())
            {
                var products = _customer_Product_RLService.GetAll().ApplyFilters(filterList.CustomerMaterialNoFilterList);
                var saleProducts = _saleReturnOrderDetail.GetAll().ApplyFilters(filterList.CustomerMaterialNoFilterList).Select(o => o.Id);
                details = details.Where(o => (products.Any(s => s.ProductId == o.ProductId && s.CustomerId == o.SaleReturnOrder.CustomerId) && o.DeliveryOrderDetailId == null)
                                                                        || (saleProducts.Contains(o.Id) && o.DeliveryOrderDetailId != null));
            }

            var saleReturnOrderDetailResult =  (from a in details
                                                     join b in _saleReturnOrder.GetAll().ApplyFilters(saleReturn)
                                                         on a.SaleReturnOrderId equals b.Id
                                                     join f in _customerService.GetAll()
                                                         on b.CustomerId equals f.Id
                                                     join c in _deliveryOrderDetail.GetAll()
                                                         on a.DeliveryOrderDetailId equals c.Id into temp
                                                     from c in temp.DefaultIfEmpty()
                                                     join d in _deliveryOrder.GetAll()
                                                         on c.DeliveryOrderId equals d.Id into temp2
                                                     from d in temp2.DefaultIfEmpty()
                                                     join g in _saleOrderDetail.GetAll().ApplyFilters(filterList.SaleOrderDetailFilterList)
                                                         on c.SaleOrderDetailId equals g.Id into temp3
                                                     from g in temp3.DefaultIfEmpty()
                                                     join sale in _saleOrder.GetAll()
                                                         on g.SaleOrderId equals sale.Id into saleTemp
                                                     from sale in saleTemp.DefaultIfEmpty()
                                                     join customer_Product in _customer_Product_RLService.GetAll()
                                                       on new { a.ProductId, a.SaleReturnOrder.CustomerId }
                                                       equals new { customer_Product.ProductId, customer_Product.CustomerId } into cusTemp
                                                     from customer_Product in cusTemp.DefaultIfEmpty()
                                                     select new
                                                     {
                                                         a,
                                                         b,
                                                         e = a.Product,
                                                         f,
                                                         c,
                                                         d,
                                                         g,
                                                         customer_Product,
                                                         sale,
                                                        
                                                     });

            var result = saleReturnOrderDetailResult.GroupBy(x => x.a)
                         .Select(info => new GetEditSaleReturnOrderDetailDTO()
                         {
                             Id = info.Key.Id,
                             ReturnQuantity = info.Key.ReturnQuantity,
                             InventoryQuantity = info.Key.InventoryQuantity,
                             Price = info.Key.Price,
                             TotalPrice = info.Key.TotalPrice,
                             IsComplete = info.Key.IsComplete,
                             Reason = info.Key.Reason,
                             Remark = info.Key.Remark,
                             DeliveryOrderDetialId = info.Key.DeliveryOrderDetailId,
                             DeliveryOrderNo = info.First().d != null ? info.First().d.OrderNo : string.Empty,
                             DeliveryQuantity = info.First().c != null ? info.First().c.DeliveryQuantity : 0,
                             DeliveryOrderCustomerUsedQuantity = info.First().c != null ? info.First().c.CustomerUsedQuantity : 0,
                             DeliveryOrderReturnQuantity = info.First().c != null ? info.First().c.ReturnQuantity : 0,
                             CustomerId = info.First().b.CustomerId,
                             CustomerCode = info.First().f.Code,
                             CustomerName = info.First().f.Name,
                             SaleReturnOrderId = info.Key.SaleReturnOrderId,
                             SaleReturnOrderRemark = info.First().b.Remark,
                             SaleReturnOrderReturnTime = info.First().b.ReturnTime,
                             SaleReturnOrderNo = info.First().b.OrderNo,
                             SaleReturnOrderTotalPrice = info.First().b.OrderTotalPrice,
                             ProductionOrderNo = info.First().c != null ? info.First().c.ProductionOrderNo : string.Empty,
                             ProductId = info.First().a.ProductId,
                             HistoryProductName = info.First().e.ProductName,
                             HistotyProductNo = info.First().e.ProductNo,
                             HistoryProductSpecification = info.First().e.Specification,
                             HistoryProductUnit = info.First().e.Unit,
                             HistoryProductMaterialGrade = info.First().e.MaterialGrade,
                             HistoryProductCustomAttribute = info.First().e.CustomAttribute,
                             HistoryProductCustomerMaterialNo = info.First().g != null ? info.First().g.CustomerMaterialNo : info.First().a.DeliveryOrderDetailId==null && info.First().customer_Product != null ? info.First().customer_Product.CustomerMaterialNo:string.Empty,
                             HistoryProductMaterialSpecification = info.First().e.MaterialSpecification,
                             HistoryProductVersion = info.First().e.Version,
                             DeliveryNoticeNo = info.First().c != null ? info.First().c.DetailNo : string.Empty,
                             SaleOrderNo = info.First().sale != null ? info.First().sale.OrderNo : string.Empty,
                             HistoryProductWeight = info.First().e.Unit == "g" ? Math.Round(info.First().e.Weight / 1000, 6, MidpointRounding.AwayFromZero) : info.First().e.Weight ,
                             SaleReturnType = info.Key.SaleReturnType,
                         }).ApplyFilters(filterList.HistoryProductFilterList ?? []);

            var newSaleOrder = await result.ApplyFilters(delivery).ApplySorts(this.SortInfoList).ToPagedListAsync(this.PageModel);
            return Ok(newSaleOrder);
        }

        /// <summary>
        /// 删除退货单明细
        /// </summary>
        /// <param name="Id"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        [HttpPost("DelSaleReturnOrderDetail", Name = "DelSaleReturnOrderDetail")]
        public async Task<IActionResult> DelSaleReturnOrderDetail([FromBody] int Id)
        {
            var detail = await _saleReturnOrderDetail.GetByIdAsync(Id);
            _ = detail ?? throw new Exception("退货单明细不存在");
            if (detail.IsComplete) throw new Exception("已完成的退货单明细不能删除");

            var details = await _saleReturnOrderDetail.GetAll().Where(x => x.SaleReturnOrderId == detail.SaleReturnOrderId).ToListAsync();
            var order = await _saleReturnOrder.GetByIdAsync(detail.SaleReturnOrderId);
            _ = order ?? throw new Exception("退货单不存在");

            order.OrderTotalPrice -= detail.TotalPrice;

            var deliveryOrderDetail = await _deliveryOrderDetail.GetByIdAsync(detail.DeliveryOrderDetailId);
            if (deliveryOrderDetail != null)
            {
                deliveryOrderDetail.ReturnQuantity -= detail.ReturnQuantity;
            }

            _saleReturnOrderDetail.Delete(detail);
            // 如果不存在未完成的记录，将主表记录标记为已完成
            if (!details.Any(o => !o.IsComplete))
            {
                order.IsComplete = true;
            }

            //  如果删除的是最后一个明细，删除主表
            if (!details.Any(o => o.Id != Id))
            {
                _saleReturnOrder.Delete(order);
            }
            await _unitWork.SaveChangeAsync();
            return Ok("删除成功");
        }

        /// <summary>
        /// 新增销售退货单DTO
        /// </summary>
        public class PostSaleReturnOrderDTO
        {
            public int CustomerId { get; set; }
            public DateTime ReturnTime { get; set; }
            public string? Remark { get; set; }
            public decimal OrderTotalPrice { get; set; }
            public List<PostSaleReturnOrderDetailDTO> PostSaleReturnOrderDetailDtoList { get; set; } = new List<PostSaleReturnOrderDetailDTO>();
        }
        /// <summary>
        /// 新增退货单
        /// </summary>
        /// <returns></returns>
        [HttpPost("PostSaleReturnOrder", Name = "PostSaleReturnOrder")]
        public async Task<IActionResult> PostSaleReturnOrder([FromBody] PostSaleReturnOrderDTO postSaleReturnOrder)
        {

            using (var transaction = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                //生成序列号
                var sequence = _sequenceService.GetDateSequenceNoForUse(TableName.SaleReturnOrder.ToString(), DateTime.Now);
                var saleReturnCode = "XSTH" + DateTime.Now.ToString("yyyyMMdd") + sequence.ToString().PadLeft(3, '0');
                //新增退货主表
                SaleReturnOrder saleReturnOrder = new SaleReturnOrder(saleReturnCode)
                {
                    OrderNo = saleReturnCode,
                    CustomerId = postSaleReturnOrder.CustomerId,
                    ReturnTime = postSaleReturnOrder.ReturnTime,
                    Remark = postSaleReturnOrder.Remark,
                    OrderTotalPrice = postSaleReturnOrder.OrderTotalPrice,
                };
                //新增退货明细
                List<SaleReturnOrderDetail> saleReturnOrderDetailsList = new List<SaleReturnOrderDetail>();
                if (postSaleReturnOrder.PostSaleReturnOrderDetailDtoList == null)
                {
                    throw new Exception("退货单明细不能为空");
                }
                foreach (var item in postSaleReturnOrder.PostSaleReturnOrderDetailDtoList)
                {
                    if (item.DeliveryOrderDetailDto == null)
                    {
                        throw new Exception("保存失败，明细数据请求错误");
                    }
                    var modifyReturnDeliveryOrderDetail = _deliveryOrderDetail.GetAll().Where(x => x.Id == item.DeliveryOrderDetailDto.Id).FirstOrDefault();
                    _ = modifyReturnDeliveryOrderDetail ?? throw new Exception("送货单明细不存在");
                    SaleReturnOrderDetail saleReturnOrderDetail = new SaleReturnOrderDetail()
                    {
                        SaleReturnOrder = saleReturnOrder,
                        DeliveryOrderDetailId = item.DeliveryOrderDetailDto.DeliveryOrderId,
                        Price = item.DeliveryOrderDetailDto.SaleOrderDetailPrice,
                        TotalPrice = item.DeliveryOrderDetailDto.SaleOrderDetailPrice * item.ReturnQuantity,
                        Reason = item.Reason,
                        Remark = item.Remark,
                        ReturnQuantity = item.ReturnQuantity,
                        InventoryQuantity = item.InventoryQuantity,
                        IsComplete = item.IsComplete,
                        DeliveryOrderDetail = modifyReturnDeliveryOrderDetail,
                        ProductId = item.DeliveryOrderDetailDto.ProductId,
                        HistoryProductId = item.DeliveryOrderDetailDto.HistoryProductId,
                        SaleReturnType = item.SaleReturnType,
                        CustomerMaterialNo = item.DeliveryOrderDetailDto.HistoryProductCustomerMaterialNo ?? string.Empty,
                    };
                    saleReturnOrder.OrderTotalPrice += saleReturnOrderDetail.TotalPrice;
                    saleReturnOrderDetail.DeliveryOrderDetail.ReturnQuantity += item.ReturnQuantity;
                    saleReturnOrderDetailsList.Add(saleReturnOrderDetail);
                }
                await _saleReturnOrder.InsertAsync(saleReturnOrder);
                await _saleReturnOrderDetail.InsertAsync(saleReturnOrderDetailsList);
                await _unitWork.SaveChangeAsync(); //新增销售退货
                transaction.Complete(); // 提交事务
                return Ok(saleReturnOrder.Id);
            }
        }

        /// <summary>
        /// 编辑需要获取对应的退货单数据
        /// </summary>
        /// <param name="saleReturnOrderId"></param>
        /// <returns></returns>
        [HttpPost("GetSaleReturnOrder", Name = "GetSaleReturnOrder")]
        public async Task<IActionResult> GetSaleReturnOrder([FromBody] int saleReturnOrderId)
        {
            //获取退货单
            var SaleReturnOrder = await _saleReturnOrder.GetByIdAsync(saleReturnOrderId);
            //获取退货单明细
            var SaleReturnOrderDetailList = await _saleReturnOrderDetail.GetAll().Where(x => x.SaleReturnOrderId == saleReturnOrderId).ToListAsync();
            //定义显示送货明细
            List<PostSaleReturnOrderDetailDTO> newSaleReturnOrderDetailDtoList = new List<PostSaleReturnOrderDetailDTO>();
            foreach (var item in SaleReturnOrderDetailList)
            {
                DeliveryOrderDetailDTO newDeliveryOrderDetailDto = new DeliveryOrderDetailDTO();
                PostSaleReturnOrderDetailDTO oneSaleReturnOrder = new PostSaleReturnOrderDetailDTO();
                oneSaleReturnOrder.InventoryQuantity = item.InventoryQuantity;
                oneSaleReturnOrder.ReturnQuantity = item.ReturnQuantity;
                oneSaleReturnOrder.Reason = item.Reason;
                oneSaleReturnOrder.Remark = item.Remark;
                oneSaleReturnOrder.IsComplete = item.IsComplete;
                oneSaleReturnOrder.SaleReturnType = item.SaleReturnType;
                var deliveryDto = _deliveryOrderDetail.GetAll().Where(x => x.Id == item.DeliveryOrderDetailId).FirstOrDefault();
                _ = deliveryDto ?? throw new Exception("送货单明细不存在");
                var historyProduct = _histotyProduct.GetAll().Where(x => x.Id == deliveryDto.HistoryProductId).FirstOrDefault();
                _ = historyProduct ?? throw new Exception("产品不存在");
                var deliveryOrder = _deliveryOrder.GetAll().Where(x => x.Id == deliveryDto.DeliveryOrderId).FirstOrDefault();
                _ = deliveryOrder ?? throw new Exception("送货单不存在");
                var saleOrderdetail = _saleOrderDetail.GetAll().Where(x => x.Id == deliveryDto.SaleOrderDetailId).FirstOrDefault();
                _ = saleOrderdetail ?? throw new Exception("销售订单明细不存在");
                newDeliveryOrderDetailDto.Id = item.DeliveryOrderDetailId == null ? 0 : (int)item.DeliveryOrderDetailId;   //todo:退货单 不一定对应送货单明细
                newDeliveryOrderDetailDto.AccountQuantity = deliveryDto.AccountQuantity;
                newDeliveryOrderDetailDto.ReturnQuantity = deliveryDto.ReturnQuantity - item.ReturnQuantity;
                newDeliveryOrderDetailDto.DeliveryQuantity = deliveryDto.DeliveryQuantity;
                newDeliveryOrderDetailDto.CustomerUsedQuantity = deliveryDto.CustomerUsedQuantity;
                newDeliveryOrderDetailDto.Remark = deliveryDto.Remark;
                //newDeliveryOrderDetailDto.IsDelivery = deliveryDto.IsDelivery;
                //newDeliveryOrderDetailDto.IsOutInventory = deliveryDto.IsOutInventory;
                newDeliveryOrderDetailDto.DeliveryTime = deliveryOrder.DeliveryTime;
                newDeliveryOrderDetailDto.ProductionOrderNo = deliveryDto.ProductionOrderNo;
                newDeliveryOrderDetailDto.ProductionId = deliveryDto.ProductionId > 0 ? (int)deliveryDto.ProductionId! : 0;
                newDeliveryOrderDetailDto.ProductName = historyProduct.ProductName;
                newDeliveryOrderDetailDto.SaleOrderDetailId = deliveryDto.SaleOrderDetailId;
                newDeliveryOrderDetailDto.DeliveryOrderNo = deliveryDto.DeliveryOrder != null ? deliveryDto.DeliveryOrder.OrderNo : string.Empty;
                newDeliveryOrderDetailDto.HistoryProductId = deliveryDto.HistoryProductId;
                newDeliveryOrderDetailDto.HistoryProductName = historyProduct.ProductName;
                newDeliveryOrderDetailDto.HistoryProductNo = historyProduct.ProductNo;
                newDeliveryOrderDetailDto.HistoryProductCustomerMaterialNo = saleOrderdetail.CustomerMaterialNo;
                newDeliveryOrderDetailDto.HistoryProductMaterialSpecification = historyProduct.MaterialSpecification;
                newDeliveryOrderDetailDto.HistoryProductMaterialGrade = historyProduct.MaterialGrade;
                newDeliveryOrderDetailDto.HistoryProductSpecification = historyProduct.Specification ?? "";
                newDeliveryOrderDetailDto.HistoryProductUnit = historyProduct.Unit;
                newDeliveryOrderDetailDto.HistoryProductVersion = historyProduct.Version;
                newDeliveryOrderDetailDto.SaleOrderDetailPrice = saleOrderdetail.Price;
                newDeliveryOrderDetailDto.ProductId = saleOrderdetail.ProductId ?? 0;
                newDeliveryOrderDetailDto.HistoryProductId = saleOrderdetail.HistoryProductId ?? 0;
                newDeliveryOrderDetailDto.DeliveryOrderId = deliveryDto.DeliveryOrderId != null ? (int)deliveryDto.DeliveryOrderId : 0;
                oneSaleReturnOrder.DeliveryOrderDetailDto = newDeliveryOrderDetailDto;
                newSaleReturnOrderDetailDtoList.Add(oneSaleReturnOrder);

            }
            return Ok(newSaleReturnOrderDetailDtoList);

        }

        /// <summary>
        /// 修改完成状态
        /// </summary>
        /// <param name="saleReturnOrderDetailId"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        [HttpPost("EditSaleReturnOrderDetailsComplete", Name = "EditSaleReturnOrderDetailsComplete")]
        public async Task<ActionResult> EditSaleReturnOrderDetailsComplete([FromBody] int saleReturnOrderDetailId)
        {
            var detail = await _saleReturnOrderDetail.GetByIdAsync(saleReturnOrderDetailId);
            _ = detail ?? throw new Exception("退货单明细不存在");
            var details = await _saleReturnOrderDetail.GetAll().Where(o => o.SaleReturnOrderId == detail.SaleReturnOrderId).ToListAsync();
            var order = await _saleReturnOrder.GetByIdAsync(detail.SaleReturnOrderId);
            _ = order ?? throw new Exception("退货单不存在");

            //// 修改数量、金额？
            //var deliveryOrderDetail = await _deliveryOrderDetail.GetAll().Where(o => o.Id == detail.DeliveryOrderDetailId).FirstOrDefaultAsync();
            //var changeQuantity = detail.InventoryQuantity - detail.ReturnQuantity;
            //detail.TotalPrice += detail.Price * changeQuantity;
            //orderPost.OrderTotalPrice += detail.Price * changeQuantity;
            //if(deliveryOrderDetail != null)
            //{
            //   deliveryOrderDetail.ReturnQuantity += changeQuantity;
            //}

            detail.IsComplete = true;

            // 如果不存在未完成的记录，将主表记录标记为已完成
            if (!details.Any(o => !o.IsComplete && o.Id != detail.Id))
            {
                order.IsComplete = true;
            }
            await _unitWork.SaveChangeAsync();
            return Ok("保存成功");
        }

        public class EditSaleReturnOrderDetailDTO
        {
            public int SaleReturnOrderId { get; set; }
            public DateTime ReturnTime { get; set; }
            public string? Remark { get; set; }
            public List<PostSaleReturnOrderDetailDTO> PostSaleReturnOrderDetailDtoList { get; set; } = new List<PostSaleReturnOrderDetailDTO>();
        }
        /// <summary>
        /// 编辑退货单数据-选取送货单
        /// </summary>
        /// <param name="editData"></param>
        /// <returns></returns>
        [HttpPost("EditSaleReturnOrderDetails", Name = "EditSaleReturnOrderDetails")]
        public async Task<ActionResult> EditSaleReturnOrderDetails([FromBody] EditSaleReturnOrderDetailDTO editData)
        {
            var saleOrder = await _saleReturnOrder.GetByIdAsync(editData.SaleReturnOrderId);
            if (saleOrder == null)
            {
                throw new Exception("数据不存在");
            }
            saleOrder.ReturnTime = editData.ReturnTime;
            saleOrder.Remark = editData.Remark;

            List<SaleReturnOrderDetail> editSaleReturnDetailData = new List<SaleReturnOrderDetail>();
            //获取所有的对应的退货单明细
            var allOldSaleReturnDetail = await _saleReturnOrderDetail.GetAll().Where(o => o.SaleReturnOrderId == saleOrder.Id).ToListAsync();
            //获取所有的不可删除的退货单明细
            var NotDelSaleReturnDetail = await _saleReturnOrderDetail.GetAll().Where(o => o.SaleReturnOrderId == saleOrder.Id && (o.InventoryQuantity > 0 || o.IsComplete == true)).ToListAsync();
            //获取所有的要删除的退货单明细
            var DelSaleReturnDetail = allOldSaleReturnDetail.Except(NotDelSaleReturnDetail).ToList();
            //将所有的要删除的退货单明细的数量减去和减去数据库内容
            foreach (var item in DelSaleReturnDetail)
            {
                var deliveryOrderDetail = await _deliveryOrderDetail.GetByIdAsync(item.DeliveryOrderDetailId);
                _ = deliveryOrderDetail ?? throw new Exception("送货单明细不存在");
                deliveryOrderDetail.ReturnQuantity -= item.ReturnQuantity;
                saleOrder.OrderTotalPrice -= item.TotalPrice;
                _saleReturnOrderDetail.Delete(item);
            }
            //直接将所有数据添加进入明细表
            if (NotDelSaleReturnDetail == null)
            {
                foreach (var item in editData.PostSaleReturnOrderDetailDtoList)
                {
                    var newSaleReturnOrder = new SaleReturnOrderDetail();
                    _ = item.DeliveryOrderDetailDto ?? throw new Exception("送货单明细不存在");
                    var oldDeliveryOrderdetail = _deliveryOrderDetail.GetAll().Where(o => o.Id == item.DeliveryOrderDetailDto.Id).FirstOrDefault();
                    _ = oldDeliveryOrderdetail ?? throw new Exception("送货单明细不存在");
                    oldDeliveryOrderdetail.ReturnQuantity += item.ReturnQuantity;
                    newSaleReturnOrder.SaleReturnOrderId = saleOrder.Id;
                    newSaleReturnOrder.IsComplete = item.IsComplete;
                    newSaleReturnOrder.ReturnQuantity = item.ReturnQuantity;
                    newSaleReturnOrder.Reason = item.Reason;
                    newSaleReturnOrder.Remark = item.Remark;
                    newSaleReturnOrder.DeliveryOrderDetailId = item.DeliveryOrderDetailDto.Id;
                    newSaleReturnOrder.Price = item.DeliveryOrderDetailDto.SaleOrderDetailPrice;
                    newSaleReturnOrder.TotalPrice = item.ReturnQuantity * item.DeliveryOrderDetailDto.SaleOrderDetailPrice;
                    newSaleReturnOrder.SaleReturnOrder = saleOrder;
                    newSaleReturnOrder.SaleReturnType = item.DeliveryOrderDetailDto.SaleReturnType == SaleReturnType.成品 ? SaleReturnType.成品 : SaleReturnType.废品;
                    saleOrder.OrderTotalPrice += newSaleReturnOrder.TotalPrice;
                    editSaleReturnDetailData.Add(newSaleReturnOrder);
                }
            }
            else  //将所有数据除了不可删除添加进入明细表
            {
                foreach (var item in NotDelSaleReturnDetail)
                {
                    var deliveryOrderId = item.DeliveryOrderDetailId;
                    editData.PostSaleReturnOrderDetailDtoList.RemoveAll(o => o.DeliveryOrderDetailDto!.Id == deliveryOrderId);
                }
                foreach (var item2 in editData.PostSaleReturnOrderDetailDtoList)
                {
                    _ = item2.DeliveryOrderDetailDto ?? throw new Exception("送货单明细不存在");
                    var newSaleReturnOrder = new SaleReturnOrderDetail();
                    var oldDeliveryOrderdetail = _deliveryOrderDetail.GetAll().Where(o => o.Id == item2.DeliveryOrderDetailDto.Id).FirstOrDefault();
                    _ = oldDeliveryOrderdetail ?? throw new Exception("送货单明细不存在");
                    oldDeliveryOrderdetail.ReturnQuantity += item2.ReturnQuantity;
                    newSaleReturnOrder.SaleReturnOrderId = saleOrder.Id;
                    newSaleReturnOrder.IsComplete = item2.IsComplete;
                    newSaleReturnOrder.ReturnQuantity = item2.ReturnQuantity;
                    newSaleReturnOrder.Reason = item2.Reason;
                    newSaleReturnOrder.Remark = item2.Remark;
                    newSaleReturnOrder.DeliveryOrderDetailId = item2.DeliveryOrderDetailDto.Id;
                    newSaleReturnOrder.Price = item2.DeliveryOrderDetailDto.SaleOrderDetailPrice;
                    newSaleReturnOrder.TotalPrice = item2.ReturnQuantity * item2.DeliveryOrderDetailDto.SaleOrderDetailPrice;
                    newSaleReturnOrder.SaleReturnOrder = saleOrder;
                    newSaleReturnOrder.ProductId = item2.DeliveryOrderDetailDto.ProductId;
                    newSaleReturnOrder.HistoryProductId = item2.DeliveryOrderDetailDto.HistoryProductId;
                    newSaleReturnOrder.SaleReturnType = item2.SaleReturnType;
                    saleOrder.OrderTotalPrice += newSaleReturnOrder.TotalPrice;
                    editSaleReturnDetailData.Add(newSaleReturnOrder);
                }

            }
            await _saleReturnOrderDetail.InsertAsync(editSaleReturnDetailData);
            await _unitWork.SaveChangeAsync();
            return Ok("保存成功");
        }




        public class DelEditSaleReturnDetail
        {
            public int SaleReturnOrderId { get; set; }
            public int DeliveryOrderDetailId { get; set; }

        }
        /// <summary>
        /// 删除退货单明细
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        [HttpPost("DelEditSaleReturnOrderDetail", Name = "DelEditSaleReturnOrderDetail")]
        public async Task<IActionResult> DelEditSaleReturnOrderDetail([FromBody] DelEditSaleReturnDetail delData)
        {
            var result = await _saleReturnOrderDetail.GetAll().Where(o => o.SaleReturnOrderId == delData.SaleReturnOrderId && o.DeliveryOrderDetailId == delData.DeliveryOrderDetailId).FirstOrDefaultAsync();
            _ = result ?? throw new Exception("退货单明细不存在");
            var deliveryOrderDetail = await _deliveryOrderDetail.GetAll().Where(o => o.Id == delData.DeliveryOrderDetailId).FirstOrDefaultAsync();
            _ = deliveryOrderDetail ?? throw new Exception("送货单明细不存在");
            deliveryOrderDetail.ReturnQuantity -= result.ReturnQuantity;
            _saleReturnOrderDetail.Delete(result);
            await _unitWork.SaveChangeAsync();
            return Ok("删除成功");
        }


        public class GetCustomerDeliveryProductsPostDTO
        {
            public int CustomerId { get; set; }
            public List<Filter> HistoryProductFilterList { get; set; } = new List<Filter>();
            public List<Filter> CustomerMaterialNoFilterList { get; set; } = new List<Filter>();
        }

        /// <summary>
        /// 获取客户的所有送过货的产品
        /// 从HistoryProduct表，关联DeliveryOrderDetail表，关联DeliveryOrder表，关联Customer表，
        /// </summary>
        /// <param name="idObject">Customer的Id</param>
        /// <returns></returns>
        [HttpPost("GetCustomerDeliveryProducts")]
        public async Task<IActionResult> GetCustomerDeliveryProducts([FromBody] GetCustomerDeliveryProductsPostDTO postData)
        {
            var products = from hisProduct in _histotyProduct.GetAll().ApplyFilters(postData.HistoryProductFilterList)
                           join product in _productService.GetAll()
                                on hisProduct.ProductId equals product.Id
                           join deliveryOrderDetail in _deliveryOrderDetail.GetAll().Include(o => o.DeliveryOrder).Include(o => o.HistoryProduct)
                                on hisProduct.Id equals deliveryOrderDetail.HistoryProductId
                           join deliveryOrder in _deliveryOrder.GetAll()
                                on deliveryOrderDetail.DeliveryOrderId equals deliveryOrder.Id
                           where deliveryOrder.CustomerId == postData.CustomerId
                           join saleReturnDetail in _saleReturnOrderDetail.GetAll()
                                .Where(o => o.SaleReturnOrder.CustomerId == postData.CustomerId)
                                on hisProduct.Id equals saleReturnDetail.HistoryProductId into saleReturnDetailGroup
                           from saleReturnDetail in saleReturnDetailGroup.DefaultIfEmpty()
                           join customerProduct in _customer_Product_RLService.GetAll()
                                on new { hisProduct.ProductId, postData.CustomerId } equals new { customerProduct.ProductId, customerProduct.CustomerId } into customerProductGroup
                           from customerProduct in customerProductGroup.DefaultIfEmpty()
                           select new
                           {
                               hisProduct,
                               product,
                               deliveryOrderDetail.DeliveryQuantity,
                               ReturnQuantity = saleReturnDetail != null ? saleReturnDetail.ReturnQuantity : 0,
                               customerProduct,
                               CustomerMaterialNo = customerProduct != null ? customerProduct.CustomerMaterialNo : string.Empty,
                           };
            if (postData.CustomerMaterialNoFilterList.Any())
            {
                products = products.ApplyFilters(postData.CustomerMaterialNoFilterList);
            }

            var groupdProducts = from product in products
                                 group product by product.product into g
                                 where g.FirstOrDefault() != null
                                 select new HistoryProductDelivery(g.First().hisProduct.ProductNo, g.First().hisProduct.ProductName, g.First().hisProduct.Unit)
                                 {
                                     Id = g.First().hisProduct.Id,
                                     CustomerMaterialNo = String.Join(",", g.Where(o => o.CustomerMaterialNo != string.Empty).Select(o => o.CustomerMaterialNo)),
                                     Version = g.First().hisProduct.Version,
                                     MaterialGrade = g.First().hisProduct.MaterialGrade,
                                     Specification = g.First().hisProduct.Specification,
                                     MaterialSpecification = g.First().hisProduct.MaterialSpecification,
                                     CustomAttribute = g.First().hisProduct.CustomAttribute,
                                     Remark = g.First().hisProduct.Remark,
                                     ProductCategory = g.First().hisProduct.ProductCategory,
                                     ProductId = g.First().hisProduct.ProductId,
                                     SerialNo = g.First().hisProduct.SerialNo,
                                     Product = g.First().product,
                                     DeliveryCount = g.Sum(o => o.DeliveryQuantity),
                                     ReturnCount = g.Sum(o => o.ReturnQuantity),
                                     Price = g.Where(o => o.customerProduct != null).Max(o => o.customerProduct.Price) > 0 ? g.Where(o => o.customerProduct != null).Max(o => o.customerProduct.Price) : g.First().product.Price,
                                 };
            var res = await groupdProducts.ApplySorts(this.SortInfoList).ToPagedListAsync(this.PageModel);
            return Ok(res);
        }

        [HttpPost("SaveHistoryProductSaleReturn")]
        public async Task<IActionResult> SaveHistoryProductSaleReturn([FromBody] SaleReturnOrderDTO order)
        {
            _ = order ?? throw new Exception("添加失败，请求错误，退货单数据不能为空");
            _ = order.SaleReturnOrderDetails ?? throw new Exception("添加失败，请求错误，退货详情不能为空");

            // 生成序列号
            var sequence = _sequenceService.GetDateSequenceNoForUse(TableName.SaleReturnOrder.ToString(), DateTime.Now);
            var orderNo = "XSTH" + DateTime.Now.ToString("yyyyMMdd") + sequence.ToString().PadLeft(3, '0');

            var customer = await _customerService.GetByIdAsync(order.CustomerId);
            _ = customer ?? throw new Exception("添加失败，客户不存在，请刷新后重试");
            var saleReturnOrder = new SaleReturnOrder(orderNo)
            {
                OrderTotalPrice = order.OrderTotalPrice,
                CustomerId = order.CustomerId,
                ReturnTime = order.ReturnTime.Date,
                Remark = order.Remark,
            };
            await _saleReturnOrder.InsertAsync(saleReturnOrder);

            var historyProductIds = order.SaleReturnOrderDetails.Select(o => o.HistoryProductId).ToList();
            var historyProducts = await _histotyProduct.GetAll().Where(o => historyProductIds.Contains(o.Id)).ToListAsync();
            var saleReturnOrderDetails = order.SaleReturnOrderDetails.Select(o => new SaleReturnOrderDetail
            {
                SaleReturnOrder = saleReturnOrder,
                Price = o.Price,
                TotalPrice = o.TotalPrice,
                ReturnQuantity = o.ReturnQuantity,
                Reason = o.Reason,
                Remark = o.Remark,
                ProductId = historyProducts.First(e => e.Id == o.HistoryProductId).ProductId,
                HistoryProductId = o.HistoryProductId,
                SaleReturnType = o.SaleReturnType,
            }).ToList();
            await _saleReturnOrderDetail.InsertAsync(saleReturnOrderDetails);
            await _unitWork.SaveChangeAsync();
            return Ok(saleReturnOrder.Id);
        }

        /// <summary>
        /// 编辑需要获取对应的退货单数据-选择产品
        /// </summary>
        /// <param name="idObject"></param>
        /// <returns></returns>
        [HttpPost("GetHistoryProductReturnDetails", Name = "GetHistoryProductReturnDetails")]
        public async Task<IActionResult> GetHistoryProductReturnDetails([FromBody] IdObject idObject)
        {
            var saleReturn = _saleReturnOrder.GetById(idObject.Id);
            _ = saleReturn ?? throw new Exception("数据加载失败，请刷新后重试");
            var details = from detail in _saleReturnOrderDetail.GetAll().Include(o => o.HistoryProduct)
                            .Where(o => o.SaleReturnOrderId == idObject.Id)
                          join delivery in _deliveryOrderDetail.GetAll().Include(o => o.DeliveryOrder).Include(o => o.HistoryProduct).ThenInclude(o => o.Product)
                            on detail.ProductId equals delivery.HistoryProduct.ProductId
                          join deliverOrder in _deliveryOrder.GetAll()
                            on delivery.DeliveryOrderId equals deliverOrder.Id
                          where deliverOrder.CustomerId == saleReturn.CustomerId
                          join returnDetail in _saleReturnOrderDetail.GetAll().Include(o => o.SaleReturnOrder)
                            .Where(o => o.SaleReturnOrderId != idObject.Id && o.SaleReturnOrder.CustomerId == saleReturn.CustomerId)
                            on detail.ProductId equals returnDetail.ProductId
                            into returnDetails
                          from returnDetail in returnDetails.DefaultIfEmpty()
                          select new
                          {
                              detail,
                              delivery.DeliveryQuantity,
                              ReturnQuantity = returnDetail != null ? returnDetail.ReturnQuantity : 0,
                          };
            var grouped = from detail in details
                          group detail by detail.detail.Id into g
                          select new SaleReturnOrderDetialDTO
                          {
                              Id = g.First().detail.Id,
                              HistoryProductId = g.First().detail.HistoryProductId ?? 0,
                              HistoryProduct = g.First().detail.HistoryProduct,
                              Price = g.First().detail.Price,
                              TotalPrice = g.First().detail.TotalPrice,
                              ReturnQuantity = g.First().detail.ReturnQuantity,
                              InventoryQuantity = g.First().detail.InventoryQuantity,
                              IsComplete = g.First().detail.IsComplete,
                              Reason = g.First().detail.Reason,
                              Remark = g.First().detail.Remark,
                              DeliveryCount = g.Sum(o => o.DeliveryQuantity),
                              ReturnCount = g.Sum(o => o.ReturnQuantity),
                              SaleReturnType = g.First().detail.SaleReturnType,
                          };


            var res = await grouped.ToListAsync();
            return Ok(res);
        }


        /// <summary>
        /// 编辑退货单数据-产品退货
        /// </summary>
        /// <param name="editData"></param>
        /// <returns></returns>
        [HttpPost("UpdateHisProductSaleReturn", Name = "UpdateHisProductSaleReturn")]
        public async Task<ActionResult> UpdateHisProductSaleReturn([FromBody] SaleReturnOrderDTO orderPost)
        {
            _ = orderPost ?? throw new Exception("添加失败，请求错误，退货单数据不能为空");
            _ = orderPost.SaleReturnOrderDetails ?? throw new Exception("添加失败，请求错误，退货详情不能为空");

            // 更改主表
            var saleReturnOrder = await _saleReturnOrder.GetByIdAsync(orderPost.Id);
            _ = saleReturnOrder ?? throw new Exception("添加失败，退货单不存在");
            var details = await _saleReturnOrderDetail.GetAll().Where(o => o.SaleReturnOrderId == saleReturnOrder.Id).ToListAsync();

            saleReturnOrder.OrderTotalPrice = orderPost.OrderTotalPrice;
            saleReturnOrder.ReturnTime = orderPost.ReturnTime.Date;
            saleReturnOrder.Remark = orderPost.Remark;

            // 计算从表需要更新、删除、新增的数据
            var oldIds = details.Select(o => o.Id).ToList();
            var newIds = orderPost.SaleReturnOrderDetails.Select(o => o.Id).ToList();

            // 删除的数据，数据库跟踪
            var delIds = oldIds.Except(newIds).ToList();
            var delDetails = details.Where(o => delIds.Contains(o.Id)).ToList();

            // 新增的数据，数据库不跟踪
            var addIds = newIds.Except(oldIds).ToList();
            var addDetails = orderPost.SaleReturnOrderDetails.Where(o => addIds.Contains(o.Id)).ToList();

            // 更新的数据，数据库跟踪
            var updateIds = oldIds.Except(delIds).ToList();
            var updateDetails = details.Where(o => updateIds.Contains(o.Id)).ToList();

            // 删除
            foreach (var detail in delDetails)
            {
                _saleReturnOrderDetail.Delete(detail);
            }

            // 新增
            var historyProductIds = addDetails.Select(o => o.HistoryProductId).ToList();
            var historyProducts = await _histotyProduct.GetAll().Where(o => historyProductIds.Contains(o.Id)).ToListAsync();
            var addSaleReturnOrderDetails = addDetails.Select(o => new SaleReturnOrderDetail
            {
                SaleReturnOrder = saleReturnOrder,
                Price = o.Price,
                TotalPrice = o.TotalPrice,
                ReturnQuantity = o.ReturnQuantity,
                Reason = o.Reason,
                Remark = o.Remark,
                ProductId = historyProducts.First(e => e.Id == o.HistoryProductId).ProductId,
                HistoryProductId = o.HistoryProductId,
                SaleReturnType = o.SaleReturnType,
            }).ToList();
            await _saleReturnOrderDetail.InsertAsync(addSaleReturnOrderDetails);

            // 更新
            foreach (var detail in updateDetails)
            {
                var postDetail = orderPost.SaleReturnOrderDetails.First(o => o.Id == detail.Id);
                detail.Price = postDetail.Price;
                detail.TotalPrice = postDetail.TotalPrice;
                detail.ReturnQuantity = postDetail.ReturnQuantity;
                detail.Reason = postDetail.Reason;
                detail.Remark = postDetail.Remark;
            }

            await _unitWork.SaveChangeAsync();
            return Ok("保存成功");
        }
    }
}