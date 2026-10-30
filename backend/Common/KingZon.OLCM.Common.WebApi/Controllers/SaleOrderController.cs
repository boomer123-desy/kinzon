using Kingzon.OLCM.Common.Services;
using Kingzon.OLCM.Common.Services.Lock;
using KingZon.Core;
using KingZon.Core.DataBase;
using KingZon.Core.Web;
using KingZon.OLCM.Common.DataBase.Entities;
using KingZon.OLCM.Common.DataBase.Enum;
using KingZon.OLCM.Common.Service.Dto;
using KingZon.OLCM.Sales.WebApi.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Index.HPRtree;
using System.Text.Json;
using System.Transactions;
using X.PagedList;


namespace KingZon.OLCM.Common.WebApi.Controllers
{
    [Route("api/sales/[controller]")]
    public class SaleOrderController : KingZonControllerBase
    {
        private readonly IUnitOfWork _unitWork;
        private readonly CommonService<SaleOrder> _saleOrderService;
        private readonly CommonService<SaleOrderDetail> _saleOrderDetailService;
        private readonly CommonService<CustomerAddress> _customerAddressService;
        private readonly CommonService<User> _userService;
        private readonly ProductService _productService;
        private readonly CommonService<Customer_Product_RL> _customer_Product_RLService;
        private readonly CommonService<HistoryProduct> _historyProductService;
        private readonly CommonService<KingZon.OLCM.Common.DataBase.Entities.Production> _productionService;
        private readonly CustomerService _customerService;
        private readonly SequenceService _sequenceService;
        private readonly CommonService<ProductionPlan> _productionPlanService;
        private readonly CommonService<StockPlan> _stockPlanService;
        private readonly FileStorageService _fileStorageService;
        private readonly CommonService<SaleOrderChange> _saleOrderChangeService;
        private readonly CommonService<DeliveryOrderDetail> _deliveryOrderDetailService;

        public SaleOrderController(
            CommonService<SaleOrder> saleOrder,
            CommonService<SaleOrderDetail> saleOrderDetail,
            CommonService<CustomerAddress> customerAddress,
            CommonService<User> user,
            CommonService<Customer_Product_RL> customer_Product_RLService,
            ProductService product,
            CommonService<HistoryProduct> historyProduct,
            CommonService<KingZon.OLCM.Common.DataBase.Entities.Production> productionService,
            CustomerService customerService,
            SequenceService sequenceService,
            CommonService<ProductionPlan> productionPlanService,
            CommonService<StockPlan> stockPlanService,
            FileStorageService fileStorageService,
            CommonService<SaleOrderChange> saleOrderChangeService,
            CommonService<DeliveryOrderDetail> deliveryOrderDetailService,
            IUnitOfWork unitWork
            )
        {
            _saleOrderService = saleOrder;
            _saleOrderDetailService = saleOrderDetail;
            _customerAddressService = customerAddress;
            _customerService = customerService;
            _userService = user;
            _productService = product;
            _customer_Product_RLService = customer_Product_RLService;
            _historyProductService = historyProduct;
            _productionService = productionService;
            _sequenceService = sequenceService;
            _productionPlanService = productionPlanService;
            _stockPlanService = stockPlanService;
            _fileStorageService = fileStorageService;
            _saleOrderChangeService = saleOrderChangeService;
            _deliveryOrderDetailService = deliveryOrderDetailService;
            _unitWork = unitWork;
        }

        /// <summary>
        /// 按filterList过滤数据
        /// </summary>
        /// <param name="filterList"></param>
        /// <returns></returns>
        [HttpPost("FilterSaleOrders", Name = "FilterSaleOrders")]
        public async Task<IActionResult> FilterSaleOrders([FromBody] FilterSaleOrdersPostDTO filters)
        {
            var query = this.GetSaleOrderData(filters).ApplyFilters(filters.CommonFilterList ?? []);
            if (this.SortInfoList.Count() <= 0)
            {
                query = query.OrderByDescending(o => o.SaleOrderId);
            }
            else
            {
                query = query.ApplySorts(this.SortInfoList);
            }
            var result = await query.ToPagedListAsync(this.PageModel);
            return Ok(result);
        }

        public class UndeliverQuantityDTO
        {
            public int? ProductId { get; set; }
            public decimal? UnDeliverQuantity { get; set; }
        }

        /// <summary>
        /// Controller内部使用的获取销售订单数据的方法
        /// </summary>
        /// <param name="filter"></param>
        /// <returns></returns>
        private IQueryable<SaleOrderDetailFlatDTO> GetSaleOrderData(FilterSaleOrdersPostDTO filter)
        {
            var saleOrderFilterList = filter.SaleOrderFilterList ?? [];
            var saleOrderDetailFilterList = filter.SaleOrderDetailFilterList ?? [];
            var historyProductFilterList = filter.HistoryProductFilterList ?? [];
            var customerFilterList = filter.CustomerFilterList ?? [];

            var query = _saleOrderDetailService.GetAll().Include(o => o.SaleOrder).ApplyFilters(saleOrderDetailFilterList);
            if (historyProductFilterList.Count() > 0)
            {
                var historyProducts = _historyProductService.GetAll(true).ApplyFilters(historyProductFilterList); //订单的HistoryProductId可为空 无查询条件是为左连接 有查询时为内连接 
                query = query.Join(historyProducts, o => o.HistoryProductId, e => e.Id, (o, e) => o).Distinct();
            }


            var unDeliver = from sale in _saleOrderDetailService.GetAll()
                            where sale.IsReview && sale.Status == SaleOrderStatus.进行中
                            && sale.SaleOrder.AuditStatus == SaleOrderAuditStatus.审核通过
                            group sale by sale.ProductId into g
                            select new UndeliverQuantityDTO
                            {
                                ProductId = g.Key,
                                UnDeliverQuantity = g.Sum(o => o.PlaceOrderQuantity - o.DeliveryQuantity)
                            };




            var result = from SaleOrderDetail in query
                         join SaleOrder in _saleOrderService.GetAll().Include(o => o.saleOrderChanges).ApplyFilters(saleOrderFilterList)
                                   on SaleOrderDetail.SaleOrderId equals SaleOrder.Id
                         join Product in _productService.GetAll()
                                   on SaleOrderDetail.ProductId equals Product.Id into tmp2 //订单导入的时候，Product可能为空
                         from Product in tmp2.DefaultIfEmpty()
                         join unDel in unDeliver
                                   on SaleOrderDetail.ProductId equals unDel.ProductId into unDelTmp
                         from unDel in unDelTmp.DefaultIfEmpty()
                         join Customer in _customerService.GetAll(true).ApplyFilters(customerFilterList)
                        on SaleOrder.CustomerId equals Customer.Id
                         join User in _userService.GetAll(true)
                             on SaleOrder.ReceiveUserId equals User.Id
                         select new SaleOrderDetailFlatDTO
                         {
                             TotalDiffQuantity = unDel != null ? unDel.UnDeliverQuantity ?? 0 : 0,
                             Id = SaleOrderDetail.Id,
                             InventoryQuantity = Product == null ? 0 : Product.InventoryQuantity,
                             PlaceOrderQuantity = SaleOrderDetail.PlaceOrderQuantity,
                             MakingQuantity = Product == null ? 0 : Product.ProcessQuantity,
                             //PickQuantity = SaleOrderDetail.PickWight > 0 ? (int)Math.Round((SaleOrderDetail.PickQuantity / SaleOrderDetail.PickWight) * (SaleOrderDetail.PickWight - SaleOrderDetail.PickAbnormalWeight)) : 0,
                             PickQuantity = Product != null ? Product.PickQuantity > 0 ? Product.PickQuantity : 0 : 0,
                             PackageQuantity = SaleOrderDetail.PackageQuantity,
                             PackagingQuantity = Product != null ? Product.PackagingQuantity > 0 ? Product.PackagingQuantity : 0 : 0,
                             DeliveryQuantity = SaleOrderDetail.DeliveryQuantity,
                             DeliveryTotalPrice = SaleOrderDetail.DeliveryQuantity * SaleOrderDetail.Price,
                             DeliveryRate = SaleOrderDetail.PlaceOrderQuantity > 0 ? SaleOrderDetail.DeliveryQuantity / SaleOrderDetail.PlaceOrderQuantity : 0,
                             TaxRate = SaleOrderDetail.TaxRate,
                             Price = SaleOrderDetail.Price,
                             PriceHaveTax = SaleOrderDetail.PriceHaveTax,
                             MoldCost = SaleOrderDetail.MoldCost,
                             TotalPrice = SaleOrderDetail.TotalPrice,
                             TotalPriceHaveTax = SaleOrderDetail.TotalPriceHaveTax,
                             IsUrgent = SaleOrderDetail.IsUrgent,
                             IsModel = SaleOrderDetail.IsModel,
                             IsReview = SaleOrderDetail.IsReview,
                             Status = SaleOrderDetail.Status,
                             DeliveryTime = SaleOrderDetail.DeliveryTime,
                             Remark = SaleOrderDetail.Remark,
                             ProductId = Product == null ? 0 : Product.Id,
                             DeliveryNoticeQuantity = SaleOrderDetail.DeliveryNoticeQuantity,
                             HistoryProductId = SaleOrderDetail.HistoryProductId ?? 0,
                             ProductNo = SaleOrderDetail.Status == SaleOrderStatus.待下单 ? (Product == null ? "" : Product.ProductNo) : (SaleOrderDetail.HistoryProduct == null ? "" : SaleOrderDetail.HistoryProduct!.ProductNo), //待下单的销售订单 显示Product对应的信息
                             ProductCustomerMaterialNo = SaleOrderDetail.CustomerMaterialNo,
                             ProductName = SaleOrderDetail.Status == SaleOrderStatus.待下单 ? (Product == null ? "" : Product.ProductName) : (SaleOrderDetail.HistoryProduct == null ? "" : SaleOrderDetail.HistoryProduct!.ProductName),
                             ProductCategory = SaleOrderDetail.Status == SaleOrderStatus.待下单 ? (Product == null ? ProductCategory.成品 : Product.ProductCategory) : (SaleOrderDetail.HistoryProduct == null ? ProductCategory.成品 : SaleOrderDetail.HistoryProduct!.ProductCategory),
                             ProductSpecification = SaleOrderDetail.Status == SaleOrderStatus.待下单 ? (Product == null ? "" : Product.Specification) : (SaleOrderDetail.HistoryProduct == null ? "" : SaleOrderDetail.HistoryProduct!.Specification),
                             ProductUnit = SaleOrderDetail.Status == SaleOrderStatus.待下单 ? (Product == null ? "" : Product.Unit) : (SaleOrderDetail.HistoryProduct == null ? "" : SaleOrderDetail.HistoryProduct!.Unit),
                             ProductVersion = SaleOrderDetail.Status == SaleOrderStatus.待下单 ? (Product == null ? "" : Product.Version) : (SaleOrderDetail.HistoryProduct == null ? "" : SaleOrderDetail.HistoryProduct!.Version),
                             ProductMaterialGrade = SaleOrderDetail.Status == SaleOrderStatus.待下单 ? (Product == null ? "" : Product.MaterialGrade) : (SaleOrderDetail.HistoryProduct == null ? "" : SaleOrderDetail.HistoryProduct!.MaterialGrade),
                             ProductMaterialSpecification = SaleOrderDetail.Status == SaleOrderStatus.待下单 ? (Product == null ? "" : Product.MaterialSpecification) : (SaleOrderDetail.HistoryProduct == null ? "" : SaleOrderDetail.HistoryProduct!.MaterialSpecification),
                             ProductWeight = SaleOrderDetail.Status == SaleOrderStatus.待下单 ? (Product == null ? 0 : Product.Weight) : (SaleOrderDetail.HistoryProduct == null ? 0 : SaleOrderDetail.HistoryProduct!.Weight),
                             ProductSerialNo = SaleOrderDetail.Status == SaleOrderStatus.待下单 ? (Product == null ? 0 : Product.SerialNo) : (SaleOrderDetail.HistoryProduct == null ? 0 : SaleOrderDetail.HistoryProduct!.SerialNo),
                             ProductRemark = SaleOrderDetail.Status == SaleOrderStatus.待下单 ? (Product == null ? "" : Product.Remark) : (SaleOrderDetail.HistoryProduct == null ? "" : SaleOrderDetail.HistoryProduct!.Remark),
                             ProductCustomAttribute = SaleOrderDetail.Status == SaleOrderStatus.待下单 ? (Product == null ? "" : Product.CustomAttribute) : (SaleOrderDetail.HistoryProduct == null ? "" : SaleOrderDetail.HistoryProduct!.CustomAttribute),
                             SaleOrderId = SaleOrderDetail.SaleOrderId,
                             OrderNo = SaleOrder.OrderNo,
                             OrderTotalPrice = SaleOrder.OrderTotalPrice,
                             OrderTotalPriceHaveTax = SaleOrder.OrderTotalPriceHaveTax,
                             OrderRemark = SaleOrder.Remark,
                             CustomerAddress = SaleOrder.CustomerAddress,
                             CustomerId = SaleOrder.CustomerId,
                             CustomerCode = Customer.Code,
                             CustomerName = Customer.Name,
                             ReceiveOrderTime = SaleOrder.ReceiveOrderTime,
                             ReceiveUserId = SaleOrder.ReceiveUserId,
                             ReceiveUserNo = User.UserNo,
                             ReceiveUserName = User.Name,
                             ReceiveName = User.Name,
                             ProductProductionPlanQuantity = Product == null ? 0 : Product.ProductionPlanQuantity,
                             ProductStockPlanQuantity = Product == null ? 0 : Product.StockPlanQuantity,
                             CustomerOrderNo = SaleOrder.CustomerOrderNo,
                             AuditStatus = SaleOrder.AuditStatus,
                             ProductRefineStatus = Product == null ? null : Product.RefineStatus,
                             ChangedRecord = SaleOrder.saleOrderChanges.Count() > 0 ? true : false,
                             ProductPackageQuantity = Product == null ? 0 : Product.PackageQuantity,
                             ProductUnPackageQuantity = Product == null ? 0 : Product.InventoryQuantity - Product.PackageQuantity,
                             StockPlanQuantity = SaleOrderDetail.StockPlanQuantity,
                             IsWaitStock = SaleOrderDetail.StockPlanQuantity < SaleOrderDetail.PlaceOrderQuantity && SaleOrderDetail.Status == SaleOrderStatus.进行中 &&
                                           SaleOrderDetail.IsReview && SaleOrder.AuditStatus == SaleOrderAuditStatus.审核通过 && !SaleOrderDetail.IsUrgent,
                             SaleOrderCreateTime = SaleOrder.CreateTime,
                         };
            return result;
        }

        /// <summary>
        /// 获取不分页的销售订单数据, Production ProductionPlan内调用
        /// </summary>
        /// <param name="filters"></param>
        /// <returns></returns>
        [HttpPost("GetSaleOrders", Name = "GetSaleOrders")]
        public async Task<IActionResult> GetSaleOrders([FromBody] FilterSaleOrdersPostDTO filters)
        {
            var query = this.GetSaleOrderData(filters);
            if (this.SortInfoList.Count == 0)
            {
                this.SortInfoList.Add(new SortInfo { Field = "SaleOrderId", Order = "desc" });
            }
            var result = await query.ApplySorts(this.SortInfoList).ToListAsync();
            return Ok(result);
        }

        /// <summary>
        /// 获取某个客户的地址 -- 重构
        /// </summary>
        /// <param name="customer"></param>
        /// <returns></returns>
        [HttpPost("GetCustomerAddresses")]
        public async Task<IActionResult> GetCustomerAddresses([FromBody] IdObject idObject)
        {
            var result = await _customerAddressService.GetAll()
                .Where(o => o.CustomerId == idObject.Id)
                .OrderByDescending(o => o.Id)
                .ToListAsync();
            return Ok(result);
        }


        /// <summary>
        /// 获取产品列表,客户绑定的产品selected属性为true，未绑定的selected属性为false -重构
        /// </summary>
        /// <param name="filter"></param>
        /// <returns></returns>
        [HttpPost("GetPagedProductsBind")]
        public async Task<IActionResult> GetPagedProductsBind([FromBody] GetCustomerProductsPostDto filter)
        {
            var productFilterList = filter.ProductFilterList ?? [];
            var q = await (from product in _productService.GetAll()
                           join customerProduct in _customer_Product_RLService.GetAll()
                                .Where(o => o.CustomerId == filter.CustomerId)
                                on product.Id equals customerProduct.ProductId into customerProductGroup
                           from customerProduct in customerProductGroup.DefaultIfEmpty()
                           join saleOrderDetail in _saleOrderDetailService.GetAll()
                                on product.Id equals saleOrderDetail.ProductId into saleOrderDetailGroup
                           from saleOrderDetail in saleOrderDetailGroup.DefaultIfEmpty()
                           join saleOrder in _saleOrderService.GetAll()
                                .Where(x => x.CustomerId == filter.CustomerId)
                                on saleOrderDetail.SaleOrderId equals saleOrder.Id into saleOrderGroup
                           from saleOrder in saleOrderGroup.DefaultIfEmpty()
                           join customerProductRL in _customer_Product_RLService.GetAll()  //多左连接一次 是为了获取客户物料号
                               on product.Id equals customerProductRL.ProductId into customerProductRLTmp
                           from customerProductRL in customerProductRLTmp.DefaultIfEmpty()
                           select new
                           {
                               product.Id,
                               product.ProductNo,
                               product.ProductName,
                               product.Unit,
                               product.Specification,
                               product.MaterialGrade,
                               product.MaterialSpecification,
                               product.Version,
                               product.Remark,
                               product.InventoryQuantity,
                               product.DemandQuantity,
                               product.PackageQuantity,
                               product.PackagingQuantity,
                               product.PickQuantity,
                               product.ProcessQuantity,
                               product.ProductionPlanQuantity,
                               product.StockPlanQuantity,
                               Price = (customerProduct != null && customerProduct.Price > 0) ? customerProduct.Price : product.Price,
                               // 当前客户产品存在绑定关系时,记录默认设置为选中状态
                               Selected = customerProduct != null,
                               // 有绑定的订单时不能解绑
                               CheckBoxDisabled = saleOrderDetail != null && saleOrder != null,
                               CustomerMaterialNo = customerProductRL != null ? customerProductRL.CustomerMaterialNo : null,
                               CustomerProductRLId = customerProductRL != null ? customerProductRL.Id : 0
                           }).ToListAsync();

            var products = q.GroupBy(p => p.Id)
                .Select(g => new ProductCustomerDTO(g.First().ProductNo, g.First().ProductName, g.First().Unit)
                {
                    Id = g.First().Id,
                    CustomerMaterialNos = String.Join("，", g.Where(o => o.CustomerProductRLId > 0 && o.CustomerMaterialNo != null)
                                                            .GroupBy(o => o.CustomerProductRLId)
                                                            .Select(o => o.First().CustomerMaterialNo)),
                    Specification = g.First().Specification,
                    MaterialGrade = g.First().MaterialGrade,
                    MaterialSpecification = g.First().MaterialSpecification,
                    Version = g.First().Version,
                    Remark = g.First().Remark,
                    InventoryQuantity = g.First().InventoryQuantity,
                    Price = g.First().Price,
                    Selected = g.Any(s => s.Selected),
                    CheckBoxDisabled = g.Any(d => d.CheckBoxDisabled),
                    DemandQuantity = g.First().DemandQuantity,
                    StockPlanQuantity = g.First().StockPlanQuantity,
                    ProductionPlanQuantity = g.First().ProductionPlanQuantity,
                    PackageQuantity = g.First().PackageQuantity,
                    PickQuantity = g.First().PickQuantity,
                    ProcessQuantity = g.First().ProcessQuantity,
                    PackagingQuantity = g.First().PackagingQuantity,
                    ProductUnPackageQuantity = g.First().InventoryQuantity - g.First().PackageQuantity,
                });
            var res = await products.AsQueryable().ApplyFilters(productFilterList).ApplySorts(this.SortInfoList).ToPagedListAsync(this.PageModel);
            return Ok(res);
        }

        /// <summary>
        /// 获取当前客户所有已绑定的产品Id列表，用于选择客户绑定的产品页面使用
        /// </summary>
        /// <param name="idObject"></param>
        /// <returns></returns>
        [HttpPost("GetAllCustomerProducts")]
        public async Task<IActionResult> GetAllCustomerProducts([FromBody] IdObject idObject)
        {
            var products = await
                           (from custRl in _customer_Product_RLService.GetAll()
                                .Where(o => o.CustomerId == idObject.Id)
                            join product in _productService.GetAll()
                                 on custRl.ProductId equals product.Id
                            select product
                           ).ToListAsync();

            return Ok(products);
        }

        /// <summary>
        /// 绑定客户经理关系
        /// </summary>
        /// <param name="dto"></param>
        /// <returns></returns>
        [HttpPost("SaveCustomerProducts")]
        public async Task<IActionResult> SaveCustomerProducts([FromBody] SaveCustomerProductsPostDTO dto)
        {
            var newIds = dto.ProductIds ?? [];
            var oldRLs = await _customer_Product_RLService.GetAll().Where(o => o.CustomerId == dto.CustomerId).ToListAsync();
            var oldIds = oldRLs.Select(o => o.ProductId).Distinct(); //客户已绑定的产品Id列表

            var deleteIds = oldIds.Except(newIds).ToList();
            var addIds = newIds.Except(oldIds).ToList();

            var products = await _productService.GetAll(true).Where(o => newIds.Contains(o.Id)).ToListAsync();

            foreach (var id in deleteIds)
            {
                var rl = oldRLs.Where(o => o.ProductId == id).ToList(); //同一客户同一产品 可能有多条关联记录 多条记录的客户物料号不同 都解绑
                if (rl != null)
                {
                    _customer_Product_RLService.Delete(rl);
                }
            }

            foreach (var id in addIds)
            {
                var product = products.FirstOrDefault(o => o.Id == id);
                if (product == null)
                {
                    throw new Exception($"产品Id为{id}的产品不存在");
                }
                await _customer_Product_RLService.InsertAsync(new Customer_Product_RL()
                {
                    CustomerId = dto.CustomerId,
                    ProductId = id,
                    Price = 0,
                    CustomerMaterialNo = product.CustomerMaterialNo,
                });
            }

            await _unitWork.SaveChangeAsync();

            return Ok("保存成功");
        }


        /// <summary>
        /// 获取某个客户绑定的所有产品，分页、排序
        /// 选择客户绑定的产品页面使用
        /// </summary>
        /// <param name="filter"></param>
        /// <returns></returns>
        [HttpPost("GetPagedCustomerProducts")]
        public async Task<IActionResult> GetPagedCustomerProducts([FromBody] GetCustomerProductsPostDto filter)
        {
            var productFilterList = filter.ProductFilterList ?? [];
            var data = from custRl in _customer_Product_RLService.GetAll().Where(o => o.CustomerId == filter.CustomerId)  //同一客户同一产品 可能有多条关联记录 多条记录的客户物料号不同                                   
                       join product in _productService.GetAll()
                       on custRl.ProductId equals product.Id
                       select new
                       {
                           custRl,
                           product
                       };

            var query = from d in data.GroupBy(o => new { o.custRl.ProductId, o.custRl.CustomerId }).ToList() //同一客户同一产品 可能有多条关联记录 多条记录的客户物料号不同                                   
                        let product = d.First().product
                        let custRlPrice = d.Max(o => o.custRl.Price)
                        select new ProductCustomerDTO(product.ProductNo, product.ProductName, product.Unit)
                        {
                            Id = product.Id,
                            ProductNo = product.ProductNo,
                            CustomerMaterialNos = String.Join("，", d.Where(o => o.custRl.CustomerMaterialNo != null).Select(o => o.custRl.CustomerMaterialNo)),
                            ProductName = product.ProductName,
                            Unit = product.Unit,
                            Price = custRlPrice > 0 ? custRlPrice : product.Price,
                            Specification = product.Specification,
                            MaterialGrade = product.MaterialGrade,
                            MaterialSpecification = product.MaterialSpecification,
                            Version = product.Version,
                            Remark = product.Remark,
                            InventoryQuantity = product.InventoryQuantity,
                            CustomerMaterialNoList = d.Where(o => o.custRl.CustomerMaterialNo != null).Select(o => new SelectionDto()
                            {
                                Value = o.custRl.CustomerMaterialNo,
                                Text = o.custRl.CustomerMaterialNo
                            }).ToList(),
                        };
            var result = await query.AsQueryable()
                           .ApplyFilters(productFilterList)
                           .ApplySorts(this.SortInfoList)
                .ToPagedListAsync(this.PageModel);

            return Ok(result);
        }

        public class GetCustomerProductsPostDto
        {
            public int CustomerId { get; set; }
            public List<Filter>? ProductFilterList { get; set; }
        }
        /// <summary>
        /// 订单过滤条件DTO
        /// </summary>
        public class FilterSaleOrdersPostDTO
        {
            public List<Filter>? SaleOrderFilterList { get; set; }
            public List<Filter>? SaleOrderDetailFilterList { get; set; }
            public List<Filter>? HistoryProductFilterList { get; set; }
            public List<Filter>? CustomerFilterList { get; set; }
            public List<Filter>? CommonFilterList { get; set; }
        }

        public class SaveCustomerProductsPostDTO
        {
            public int CustomerId { get; set; }
            public List<int>? ProductIds { get; set; }
        }

        /// <summary>
        /// 获取销售单产品在制详情
        /// </summary>
        /// <param name="productId"></param>
        /// <returns></returns>
        [HttpGet("GetProductionDetailByProductId", Name = "GetProductionDetailByProductId")]
        public async Task<IActionResult> GetProductionDetailByProductId(int productId)
        {
            var detail = await (from info in _productionService.GetAll()
                                join plan in _productionPlanService.GetAll(true)
                                on new { id = info.SourceId, type = info.SourceType } equals new { id = plan.Id, type = ProductionSourceType.生产计划 }
                                into query
                                from plan in query.DefaultIfEmpty()
                                where info.ProductId == productId && info.Status != ProductionStatus.已完成
                                select new ProductionDetailDto(info.OrderNo, info.Status.ToString())
                                {
                                    Id = info.Id,
                                    ProductionQuantity = info.ProductionQuantity,
                                    CompleteQuantity = info.CompleteQuantity,
                                    AbnormalQuantity = info.AbnormalQuantity,
                                    CompleteTime = info.CompleteTime,
                                    StartTime = info.StartTime,
                                    PlanCompleteTime = info.PlanCompleteTime,
                                    PlanStartTime = info.PlanStartTime,
                                    ProductionPlanNo = plan != null ? plan.PlanNo : string.Empty,
                                    ProductId = info.ProductId,
                                    Status = info.Status,
                                    Remark = info.Remark,
                                    OrderNo = info.OrderNo,
                                    IsDeleted = info.IsDeleted

                                }).ApplySorts(this.SortInfoList).ToPagedListAsync(this.PageModel);
            return Ok(detail);
        }

        /// <summary>
        /// 暂停排产单
        /// </summary>
        /// <param name="productionId"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        [HttpGet("SuspendProduction", Name = "SuspendProduction")]
        public async Task<IActionResult> SuspendProduction(int productionId)
        {
            var production = await _productionService.GetByIdAsync(productionId);
            _ = production ?? throw new Exception("该排产单不存在，请刷新数据");

            production.Status = ProductionStatus.已暂停;
            await _unitWork.SaveChangeAsync();
            return Ok("保存成功");
        }


        /// <summary>
        /// 按Id获取一条销售订单数据（包括详情），编辑时调用 代码重构
        /// </summary>
        /// <param name="idObject"></param>
        /// <returns></returns>
        [HttpGet("GetSaleOrderDetailById", Name = "GetSaleOrderDetailById")]
        public async Task<IActionResult> GetSaleOrderDetailById([FromQuery] int orderId, int? detailId)
        {
            if (this.SortInfoList.Count == 0)
            {
                this.SortInfoList.Add(new SortInfo { Field = "SaleOrderDetails.Id", Order = "desc" });
            }
            var result = await _saleOrderService.GetAll().Where(o => o.Id == orderId)
                           .Include(o => o.ReceiveUser)
                           .Include(o => o.SaleOrderDetails).ThenInclude(o => o.Product).ThenInclude(o => o.Customer_Product_RL).ThenInclude(o => o.Customer)
                           .Include(o => o.SaleOrderDetails).ThenInclude(d => d.HistoryProduct)
                           .ApplySorts(this.SortInfoList)
                           .FirstOrDefaultAsync();
            if (result == null)
            {
                throw new Exception("销售订单不存在，请刷新数据");
            }
            if (detailId.HasValue && detailId > 0)
            {
                result.SaleOrderDetails = result.SaleOrderDetails.Where(x => x.Id == detailId).ToList();
            }
            return Ok(result);
        }

        /// <summary>
        /// 新增一条订单  代码重构
        /// </summary>
        /// <param name="saleOrder"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        [HttpPost("AddSaleOrder")]
        public async Task<IActionResult> AddSaleOrder([FromBody] SaleOrder saleOrderPost)
        {
            _ = saleOrderPost ?? throw new Exception("添加失败，请求错误");
            _ = saleOrderPost.SaleOrderDetails ?? throw new Exception("添加失败，订单明细不能为空");

            using var transaction = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);

            var customer = await _customerService.GetByIdAsync(saleOrderPost.CustomerId);
            _ = customer ?? throw new Exception("添加失败，客户不存在");

            if (saleOrderPost.CustomerOrderNo != null)
            {
                var tmpOrder = _saleOrderService.GetAll().Any(o => o.CustomerId == saleOrderPost.CustomerId && o.CustomerOrderNo == saleOrderPost.CustomerOrderNo);
                if (tmpOrder)
                {
                    throw new Exception("添加失败，客户订单号已存在");
                }
            }

            //生成订单编号
            var orderNo = _sequenceService.GenerateCode(TableName.SaleOrder);

            var saleOrder = new SaleOrder(orderNo, saleOrderPost.CustomerAddress)
            {
                Status = SaleOrderStatus.进行中,
                ReceiveOrderTime = saleOrderPost.ReceiveOrderTime.Date,
                CustomerId = saleOrderPost.CustomerId,
                ReceiveUserId = saleOrderPost.ReceiveUserId,
                OrderTotalPrice = saleOrderPost.OrderTotalPrice,
                OrderTotalPriceHaveTax = saleOrderPost.OrderTotalPriceHaveTax,
                Remark = saleOrderPost.Remark,
                CustomerOrderNo = saleOrderPost.CustomerOrderNo,
            };


            // 新增时，订单明细中的产品信息是保存在product里
            var productIds = saleOrderPost.SaleOrderDetails.Select(o => o.ProductId).ToList();
            List<Product> products = await _productService.GetAll().Where(o => productIds.Contains(o.Id)).ToListAsync();

            List<Customer_Product_RL> cp_rl = await _customer_Product_RLService.GetAll()
                                                    .Where(o => o.CustomerId == saleOrder.CustomerId && productIds.Contains(o.ProductId))
                                                    .ToListAsync();
            foreach (SaleOrderDetail detailPost in saleOrderPost.SaleOrderDetails)
            {
                var product = products.FirstOrDefault(p => p.Id == detailPost.ProductId);
                _ = product ?? throw new Exception("添加失败，产品不存在");

                var historyProduct = new HistoryProduct(product.ProductNo, product.ProductName, product.Unit)
                {
                    ProductId = product.Id,
                    CustomerMaterialNo = product.CustomerMaterialNo,
                    ProductNo = product.ProductNo,
                    Specification = product.Specification,
                    MaterialGrade = product.MaterialGrade,
                    MaterialSpecification = product.MaterialSpecification,
                    Version = product.Version,
                    Remark = product.Remark,
                    EnterpriseId = product.EnterpriseId,
                    IsDeleted = false,
                    CustomAttribute = product.CustomAttribute,
                    SerialNo = product.SerialNo,
                    Weight = product.Weight,
                    ProductCategory = product.ProductCategory,
                };
                await _historyProductService.InsertAsync(historyProduct);
                var detail = new SaleOrderDetail()
                {
                    PlaceOrderQuantity = detailPost.PlaceOrderQuantity,
                    Price = detailPost.Price,
                    PriceHaveTax = detailPost.PriceHaveTax,
                    TaxRate = detailPost.TaxRate,
                    MoldCost = detailPost.MoldCost,
                    TotalPrice = detailPost.TotalPrice,
                    TotalPriceHaveTax = detailPost.TotalPriceHaveTax,
                    IsUrgent = detailPost.IsUrgent,
                    IsModel = detailPost.IsModel,
                    IsReview = detailPost.IsReview,
                    DeliveryTime = detailPost.DeliveryTime.Date,
                    Status = SaleOrderStatus.进行中,
                    ProductId = product.Id,
                    SaleOrder = saleOrder,
                    Remark = detailPost.Remark,
                    HistoryProduct = historyProduct,
                    CustomerMaterialNo = detailPost.CustomerMaterialNo
                };
                await _saleOrderDetailService.InsertAsync(detail);

                //更新最新客户图号和销售价格
                var cpRL = cp_rl.OrderByDescending(x => x.CreateTime).FirstOrDefault(cp => cp.ProductId == detailPost.ProductId);
                if (cpRL != null)
                {
                    cpRL.CustomerMaterialNo = detailPost.CustomerMaterialNo;
                    cpRL.Price = detailPost.Price;
                }
                if (cpRL == null)
                {
                    var customerProduct = new Customer_Product_RL()
                    {
                        CustomerId = saleOrder.CustomerId,
                        ProductId = product.Id,
                        CustomerMaterialNo = detailPost.CustomerMaterialNo,
                        Price = detailPost.Price,
                    };
                    _customer_Product_RLService.Insert(customerProduct);
                }

                //客户-客户物料号-产品
                //var cp = cp_rl.FirstOrDefault(cp => cp.CustomerMaterialNo == detailPost.CustomerMaterialNo);
                //if (cp == null)
                //{//新增关系表
                //    var customerProduct = new Customer_Product_RL()
                //    {
                //        CustomerId = saleOrder.CustomerId,
                //        ProductId = product.Id,
                //        CustomerMaterialNo = detailPost.CustomerMaterialNo,
                //        Price = detailPost.Price,
                //    };
                //    _customer_Product_RLService.Insert(customerProduct);
                //}
                //else
                //{
                //    if (cp.ProductId != product.Id)
                //    {
                //        cp.ProductId = product.Id;
                //    }
                //    cp.Price = detailPost.Price; //使其价格一致保持最后一次下单时的价格
                //}

                //product.Price = detailPost.Price;            // 修改产品表，使其价格一致保持最后一次下单时的价格
            }

            await _saleOrderService.InsertAsync(saleOrder);
            await _unitWork.SaveChangeAsync();

            // 加锁修改产品需求数量
            lock (QuantityLock.SaleQuantityLock)
            {
                foreach (var product in products)
                {
                    product.DemandQuantity = _productService.GetProductDemandQuantity(product.Id);
                }
                _unitWork.SaveChange();
                transaction.Complete();
            }
            return Ok("添加成功");
        }

        /// <summary>
        /// 编辑订单-代码重构
        /// </summary>
        /// <param name="saleOrderPost"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        [HttpPost("ModifySaleOrder")]
        public async Task<IActionResult> ModifySaleOrder([FromBody] SaleOrder saleOrderPost)
        {
            _ = saleOrderPost ?? throw new Exception("保存失败，请求错误");
            _ = saleOrderPost.SaleOrderDetails ?? throw new Exception("保存失败，订单明细不能为空");
            if (saleOrderPost.CustomerOrderNo != null)
            {
                var tmpOrder = _saleOrderService.GetAll().Any(o => o.CustomerId == saleOrderPost.CustomerId
                                                                    && o.CustomerOrderNo == saleOrderPost.CustomerOrderNo
                                                                    && o.Id != saleOrderPost.Id);
                if (tmpOrder)
                {
                    throw new Exception("保存失败，客户订单号已存在");
                }
            }

            // 使用事务
            using var transaction = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);

            // 原有的订单主表数据
            var saleOrder = await _saleOrderService.GetByIdAsync(saleOrderPost.Id);
            _ = saleOrder ?? throw new Exception("保存失败，数据不存在");
            // 原有销售订单明细数据
            var saleOrderDetails = await _saleOrderDetailService.GetAll()
                                         .Where(o => o.SaleOrderId == saleOrderPost.Id)
                                         .ToListAsync();
            //更新数据库原有的基础信息
            saleOrder.CustomerAddress = saleOrderPost.CustomerAddress;
            saleOrder.OrderTotalPrice = saleOrderPost.OrderTotalPrice;
            saleOrder.OrderTotalPriceHaveTax = saleOrderPost.OrderTotalPriceHaveTax;
            saleOrder.Remark = saleOrderPost.Remark;
            saleOrder.ReceiveOrderTime = saleOrderPost.ReceiveOrderTime.Date;
            saleOrder.ReceiveUserId = saleOrderPost.ReceiveUserId;
            saleOrder.CustomerOrderNo = saleOrderPost.CustomerOrderNo;

            //查出原有数据、新数据中的productId
            var productIds = saleOrderDetails.Select(o => o.ProductId)
                                .Union(saleOrderPost.SaleOrderDetails.Select(x => x.ProductId))
                                .Distinct().ToList();
            // 原有数据中的historyProductId
            var historyIds = saleOrderDetails.Select(o => o.HistoryProductId).ToList();
            var products = await _productService.GetAll(false).Where(o => productIds.Contains(o.Id)).ToListAsync();
            var historyProducts = await _historyProductService.GetAll().Where(o => historyIds.Contains(o.Id)).ToListAsync();
            var cp_rl = await _customer_Product_RLService.GetAll()
                        .Where(o => o.CustomerId == saleOrder.CustomerId && productIds.Contains(o.ProductId))
                        .ToListAsync();
            // 用传入的saleOrderPost的saleOrderDetails与查出来的saleOrderDetails做对比，分别查出需要增删改的数组
            //数据库内的详情id
            var detailIds = saleOrderDetails.Select(o => o.Id).ToList();
            //新传入的详情id
            var detailPostIds = saleOrderPost.SaleOrderDetails.Select(o => o.Id).ToList();

            // 需要删除的details,原数据中存在,新数据中不存在
            var deleteDetails = saleOrderDetails.Where(o => !detailPostIds.Contains(o.Id)).ToList();
            // 需要新增的details,新数据中存在,原数据中不存在
            var insertDetails = saleOrderPost.SaleOrderDetails.Where(o => !detailIds.Contains(o.Id)).ToList();
            // 需要更新的details,新数据中存在,原数据中存在
            var updateDetails = saleOrderPost.SaleOrderDetails.Where(o => o.Id > 0 && detailIds.Contains(o.Id)).ToList();

            // 依次执行删除操作
            foreach (var detail in deleteDetails)
            {
                // 原数据存在，新数据不存在，执行删除操作
                // 旧数据已评审&&不加急，需求数量减去旧数据中下单数量
                var product = products.FirstOrDefault(p => p.Id == detail.ProductId);
                _ = product ?? throw new Exception("保存失败，产品数据不存在");

                var historyProduct = historyProducts.FirstOrDefault(h => h.Id == detail.HistoryProductId);
                if (historyProduct != null)
                {
                    _historyProductService.Delete(historyProduct);
                }

                _saleOrderDetailService.Delete(detail);
            }

            // 依次执行新增操作
            foreach (var detailPost in insertDetails)
            {
                // 如果原数据中不存在，新数据中存在，则执行新增操作
                var product = products.FirstOrDefault(p => p.Id == detailPost.ProductId);
                _ = product ?? throw new Exception("保存失败，未找到该产品");

                // 新增历史产品数据
                var historyProduct = new HistoryProduct(product.ProductNo, product.ProductName, product.Unit)
                {
                    ProductId = product.Id,
                    CustomerMaterialNo = product.CustomerMaterialNo,
                    ProductNo = product.ProductNo,
                    Specification = product.Specification,
                    MaterialGrade = product.MaterialGrade,
                    MaterialSpecification = product.MaterialSpecification,
                    Version = product.Version,
                    Remark = product.Remark,
                    EnterpriseId = product.EnterpriseId,
                    IsDeleted = false,
                    CustomAttribute = product.CustomAttribute,
                    SerialNo = product.SerialNo,
                    Weight = product.Weight,
                    ProductCategory = product.ProductCategory,
                };
                await _historyProductService.InsertAsync(historyProduct);
                // 新增订单详情数据
                var detail = new SaleOrderDetail()
                {
                    PlaceOrderQuantity = detailPost.PlaceOrderQuantity,
                    Price = detailPost.Price,
                    PriceHaveTax = detailPost.PriceHaveTax,
                    TaxRate = detailPost.TaxRate,
                    MoldCost = detailPost.MoldCost,
                    TotalPrice = detailPost.TotalPrice,
                    TotalPriceHaveTax = detailPost.TotalPriceHaveTax,
                    IsUrgent = detailPost.IsUrgent,
                    IsModel = detailPost.IsModel,
                    IsReview = detailPost.IsReview,
                    DeliveryTime = detailPost.DeliveryTime.Date,
                    Status = SaleOrderStatus.进行中,
                    ProductId = product.Id,
                    SaleOrderId = saleOrder.Id,
                    Remark = detailPost.Remark,
                    HistoryProduct = historyProduct,
                    CustomerMaterialNo = detailPost.CustomerMaterialNo
                };
                await _saleOrderDetailService.InsertAsync(detail);

                //更新最新客户图号和销售价格
                var cpRL = cp_rl.OrderByDescending(x => x.CreateTime).FirstOrDefault(cp => cp.ProductId == detailPost.ProductId);
                if (cpRL != null)
                {
                    cpRL.CustomerMaterialNo = detailPost.CustomerMaterialNo;
                    cpRL.Price = detailPost.Price;
                }
                if (cpRL == null)
                {
                    var customerProduct = new Customer_Product_RL()
                    {
                        CustomerId = saleOrder.CustomerId,
                        ProductId = product.Id,
                        CustomerMaterialNo = detailPost.CustomerMaterialNo,
                        Price = detailPost.Price,
                    };
                    _customer_Product_RLService.Insert(customerProduct);
                }


                // 修改产品表，使其价格一致保持最后一次下单时的价格
                //product.Price = detailPost.Price;

                //客户-客户物料号-产品
                //var cp = cp_rl.FirstOrDefault(cp => cp.CustomerMaterialNo == detailPost.CustomerMaterialNo);
                //if (cp == null)
                //{//新增关系表
                //    var customerProduct = new Customer_Product_RL()
                //    {
                //        CustomerId = saleOrder.CustomerId,
                //        ProductId = product.Id,
                //        CustomerMaterialNo = detailPost.CustomerMaterialNo,
                //        Price = detailPost.Price,
                //    };
                //    _customer_Product_RLService.Insert(customerProduct);
                //}
                //else
                //{
                //    if (cp.ProductId != product.Id)
                //    {
                //        cp.ProductId = product.Id;
                //    }
                //    cp.Price = detailPost.Price; //使其价格一致保持最后一次下单时的价格
                //}
            }

            // 依次执行更新操作
            foreach (var detailPost in updateDetails)
            {
                var detail = saleOrderDetails.FirstOrDefault(x => x.Id == detailPost.Id);
                _ = detail ?? throw new Exception("保存失败，详情数据不存在");
                var product = products.FirstOrDefault(p => p.Id == detail.ProductId);
                _ = product ?? throw new Exception("保存失败，产品数据不存在");
                // 如果原数据中存在，新数据中也存在，则执行编辑操作
                // 旧数据已评审&&不加急，新数据已评审&&不加急，需求变化量 = 新下单数量 - 旧下单数量
                // 旧数据已评审&&不加急，新数据未评审||加急  ，需求变化量 = -旧下单数量
                // 旧数据未评审||加急  ，新数据已评审&&不加急，需求变化量 = 新下单数量
                // 旧数据未评审||不加急，新数据未评审||不加急，需求变化量 = 0
                var changeDemandQuantity = 0;
                if (detail.IsReview && !detail.IsUrgent && detailPost.IsReview && !detailPost.IsUrgent)
                {
                    changeDemandQuantity = detailPost.PlaceOrderQuantity - detail.PlaceOrderQuantity;
                }
                if (detail.IsReview && !detail.IsUrgent && (!detailPost.IsReview || detail.IsUrgent))
                {
                    changeDemandQuantity = -detail.PlaceOrderQuantity;
                }
                if ((!detail.IsReview || detail.IsUrgent) && detailPost.IsReview && !detailPost.IsUrgent)
                {
                    changeDemandQuantity = detailPost.PlaceOrderQuantity;
                }
                detail.MoldCost = detailPost.MoldCost;
                detail.Price = detailPost.Price;
                detail.PriceHaveTax = detailPost.PriceHaveTax;
                detail.TaxRate = detailPost.TaxRate;
                detail.PlaceOrderQuantity = detailPost.PlaceOrderQuantity;
                detail.TotalPrice = detailPost.TotalPrice;
                detail.TotalPriceHaveTax = detailPost.TotalPriceHaveTax;
                detail.DeliveryTime = detailPost.DeliveryTime.Date;
                detail.IsModel = detailPost.IsModel;
                detail.IsUrgent = detailPost.IsUrgent;
                detail.IsReview = detailPost.IsReview;
                detail.Remark = detailPost.Remark;
                detail.CustomerMaterialNo = detailPost.CustomerMaterialNo;
                //更新最新客户图号和销售价格
                var cpRL = cp_rl.OrderByDescending(x => x.CreateTime).FirstOrDefault(cp => cp.ProductId == detailPost.ProductId);
                if (cpRL != null)
                {
                    cpRL.CustomerMaterialNo = detailPost.CustomerMaterialNo;
                    cpRL.Price = detailPost.Price;
                }
                if (cpRL == null)
                {
                    var customerProduct = new Customer_Product_RL()
                    {
                        CustomerId = saleOrder.CustomerId,
                        ProductId = product.Id,
                        CustomerMaterialNo = detailPost.CustomerMaterialNo,
                        Price = detailPost.Price,
                    };
                    _customer_Product_RLService.Insert(customerProduct);
                }

                // 修改产品表，使其价格一致保持最后一次更新时的价格
                // 改产产品的需求数量，可能有4中情况
                //product.Price = detailPost.Price;

                ////客户-客户物料号-产品
                //var cp = cp_rl.FirstOrDefault(cp => cp.CustomerMaterialNo == detailPost.CustomerMaterialNo);
                //if (cp == null)
                //{//新增关系表
                //    var customerProduct = new Customer_Product_RL()
                //    {
                //        CustomerId = saleOrder.CustomerId,
                //        ProductId = product.Id,
                //        CustomerMaterialNo = detailPost.CustomerMaterialNo,
                //        Price = detailPost.Price,
                //    };
                //    _customer_Product_RLService.Insert(customerProduct);
                //}
                //else
                //{
                //    if (cp.ProductId != product.Id)
                //    {
                //        cp.ProductId = product.Id;
                //    }
                //    cp.Price = detailPost.Price; //使其价格一致保持最后一次下单时的价格
                //}
            }
            await _unitWork.SaveChangeAsync();

            // 加锁逐条修改产品的需求数量
            lock (QuantityLock.SaleQuantityLock)
            {
                foreach (var product in products)
                {
                    product.DemandQuantity = _productService.GetProductDemandQuantity(product.Id);
                }
                _unitWork.SaveChange();
                transaction.Complete();
            }

            return Ok("保存成功");
        }

        public class PostEditSaleOrderDetailDto
        {
            public int Id { get; set; }
            public int PlaceOrderQuantity { get; set; }
            public DateTime DeliveryTime { get; set; }
            public decimal? Price { get; set; }
            public decimal? MoldCost { get; set; }
            public bool IsModel { get; set; }
            public bool IsUrgent { get; set; }
            public string? Remark { get; set; }
            public string? CustomerMaterialNo { get; set; }
            public decimal? PriceHaveTax { get; set; }
            public decimal? TaxRate { get; set; }
        }

        /// <summary>
        /// 订单明细编辑
        /// </summary>
        /// <param name="saleOrderPost"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        [HttpPost("EditSaleOrderDetail")]
        public async Task<IActionResult> EditSaleOrderDetail([FromBody] PostEditSaleOrderDetailDto postData)
        {
            using (var transaction = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                // 原有销售订单明细数据
                var saleOrderDetail = await _saleOrderDetailService.GetAll(true)
                                                                    .Include(x => x.SaleOrder)
                                                                    .Include(x => x.Product)
                                                                    .Include(x => x.HistoryProduct)
                                                                    .Where(o => o.Id == postData.Id)
                                                                    .FirstOrDefaultAsync();
                if (saleOrderDetail == null)
                {
                    throw new Exception("销售明细数据不存在，请刷新");
                }

                // 原有的订单主表数据
                var saleOrder = saleOrderDetail.SaleOrder;


                var product = saleOrderDetail.Product;
                if (product == null)
                {
                    throw new Exception("产品数据不存在，请刷新");
                }
                var historyProduct = saleOrderDetail.HistoryProduct;
                if (historyProduct != null)
                {
                    historyProduct.CustomerMaterialNo = saleOrderDetail.CustomerMaterialNo;
                }


                var cp_rl = await _customer_Product_RLService.GetAll()
                            .Where(o => o.CustomerId == saleOrder.CustomerId && o.ProductId == product.Id)
                            .ToListAsync();

                //更新明细
                saleOrderDetail.MoldCost = postData.MoldCost ?? 0;
                saleOrderDetail.Price = postData.Price ?? 0;
                saleOrderDetail.PlaceOrderQuantity = postData.PlaceOrderQuantity;
                saleOrderDetail.TotalPrice = (postData.Price ?? 0) * postData.PlaceOrderQuantity + postData.MoldCost ?? 0;
                saleOrderDetail.DeliveryTime = postData.DeliveryTime.Date;
                saleOrderDetail.IsModel = postData.IsModel;
                saleOrderDetail.IsUrgent = postData.IsUrgent;
                saleOrderDetail.Remark = postData.Remark;
                saleOrderDetail.CustomerMaterialNo = postData.CustomerMaterialNo;
                saleOrderDetail.PriceHaveTax = postData.PriceHaveTax ?? 0;
                saleOrderDetail.TaxRate = postData.TaxRate ?? 0;
                saleOrderDetail.TotalPriceHaveTax = (postData.PriceHaveTax ?? 0) * postData.PlaceOrderQuantity + postData.MoldCost ?? 0;

                //更新客户图号
                var cp = cp_rl.FirstOrDefault(cp => cp.CustomerMaterialNo == postData.CustomerMaterialNo);
                if (cp == null && !string.IsNullOrEmpty(postData.CustomerMaterialNo))
                {
                    //新增关系表
                    var customerProduct = new Customer_Product_RL()
                    {
                        CustomerId = saleOrder.CustomerId,
                        ProductId = product.Id,
                        CustomerMaterialNo = postData.CustomerMaterialNo,
                        Price = (postData.Price ?? 0),
                    };
                    _customer_Product_RLService.Insert(customerProduct);
                }
                if (cp != null)
                {
                    cp.Price = (postData.Price ?? 0); //使其价格一致保持最后一次下单时的价格
                }
                await _unitWork.SaveChangeAsync();

                //更新订单合计价格
                saleOrder.OrderTotalPrice = await _saleOrderDetailService.GetAll().Where(x => x.SaleOrderId == saleOrder.Id).SumAsync(x => x.TotalPrice);
                saleOrder.OrderTotalPriceHaveTax = await _saleOrderDetailService.GetAll().Where(x => x.SaleOrderId == saleOrder.Id).SumAsync(x => x.TotalPriceHaveTax);
                // 加锁逐条修改产品的需求数量
                lock (QuantityLock.SaleQuantityLock)
                {
                    product.DemandQuantity = _productService.GetProductDemandQuantity(product.Id);
                    _unitWork.SaveChange();
                    transaction.Complete();
                }

                return Ok("保存成功");
            }
        }




        /// <summary>
        /// 确认完成功能 -- 重构
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        [HttpPost("CompleteSaleOrderDetail")]
        public async Task<IActionResult> CompleteSaleOrderDetail([FromBody] IdObject idObject)
        {
            using (var transaction = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                var saleOrderDetail = await _saleOrderDetailService.GetAll().Include(o => o.SaleOrder)
                                .Where(o => o.Id == idObject.Id)
                                .FirstOrDefaultAsync();
                _ = saleOrderDetail ?? throw new Exception("保存失败，未找到订单详情数据,请刷新后重试");

                saleOrderDetail.Status = saleOrderDetail.Status == SaleOrderStatus.已完成 ? SaleOrderStatus.进行中 : SaleOrderStatus.已完成;
                await _unitWork.SaveChangeAsync();
                var flag = await _saleOrderDetailService.GetAll()
                            .AnyAsync(o => o.SaleOrderId == saleOrderDetail.SaleOrderId && o.Status != SaleOrderStatus.已完成);
                if (!flag)
                {
                    saleOrderDetail.SaleOrder.Status = SaleOrderStatus.已完成;
                }
                await _unitWork.SaveChangeAsync();

                lock (QuantityLock.SaleQuantityLock)
                {
                    // 如果已评审不加急的订单,需减去需求数量
                    if (saleOrderDetail.IsReview && !saleOrderDetail.IsUrgent)
                    {
                        var product = _productService.GetById(saleOrderDetail.ProductId);
                        if (product != null)
                        {
                            product.DemandQuantity = _productService.GetProductDemandQuantity(product.Id);
                        }
                        _unitWork.SaveChange();
                    }

                    transaction.Complete();
                }
                return Ok("保存成功");
            }
        }

        /// <summary>
        /// 批量确认完成功能
        /// </summary>
        /// <param name="idObject"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        [HttpPost("MultiCompleteSaleOrderDetail")]
        public async Task<IActionResult> MultiCompleteSaleOrderDetail([FromBody] List<int> ids)
        {
            using (var transaction = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                var saleOrderDetails = await _saleOrderDetailService.GetAll().Where(x => ids.Contains(x.Id)).Include(o => o.SaleOrder)
                                .ToListAsync();
                if (!saleOrderDetails.Any())
                {
                    throw new Exception("保存失败，未找到订单数据,请刷新后重试");
                }
                foreach (var data in saleOrderDetails)
                {
                    data.Status = data.Status == SaleOrderStatus.已完成 ? SaleOrderStatus.进行中 : SaleOrderStatus.已完成;

                }
                await _unitWork.SaveChangeAsync();

                var orders = await _saleOrderService.GetAll()
                            .Where(o => saleOrderDetails.Select(x => x.SaleOrderId).ToList().Contains(o.Id))
                            .ToListAsync();
                //订单详情有未完成的，修改订单状态为未完成
                //订单详情全部已完成，修改订单状态为已完成
                foreach (var item in orders)
                {
                    item.Status = _saleOrderDetailService.GetAll().Where(x => x.SaleOrderId == item.Id).ToList().Any(x => x.Status != SaleOrderStatus.已完成)
                        ? SaleOrderStatus.进行中
                        : SaleOrderStatus.已完成;
                }
                await _unitWork.SaveChangeAsync();

                lock (QuantityLock.SaleQuantityLock)
                {
                    // 如果已评审不加急的订单,需减去需求数量
                    if (saleOrderDetails.Any(x => x.IsReview) && saleOrderDetails.Any(x => !x.IsUrgent))
                    {
                        var saleOrderDetailsData = saleOrderDetails.Where(z => z.IsReview == true && z.IsUrgent == false).ToList();
                        var products = _productService.GetAll().Where(x => saleOrderDetailsData.Select(o => o.ProductId).ToList().Contains(x.Id)).ToList();
                        if (products != null)
                        {
                            foreach (var product in products)
                            {
                                product.DemandQuantity = _productService.GetProductDemandQuantity(product.Id);

                            }
                        }
                        _unitWork.SaveChange();
                    }

                    transaction.Complete();
                }
                return Ok("保存成功");
            }
        }

        /// <summary>
        /// 批量审批销售订单详情
        /// </summary>
        /// <param name="idObject"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        [HttpPost("MultiReviewSaleOrderDetail")]
        public async Task<IActionResult> MultiReviewSaleOrderDetail([FromBody] List<int> ids)
        {
            using (var transaction = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                var saleOrderDetails = await _saleOrderDetailService.GetAll(true).Include(x => x.Product).Where(x => ids.Contains(x.Id)).ToListAsync();
                foreach (var item in saleOrderDetails)
                {
                    item.IsReview = true;
                }
                await _unitWork.SaveChangeAsync();
                var products = saleOrderDetails.Where(x => x.Product != null).Select(x => x.Product!).ToList();
                foreach (var item in products)
                {
                    item.DemandQuantity = _productService.GetProductDemandQuantity(item.Id);
                }
                await _unitWork.SaveChangeAsync();
                transaction.Complete();
                return Ok("保存成功");
            }
        }


        /// <summary>
        /// 删除订单详情、订单，历史产品 --代码重构
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        [HttpPost("DeleteSaleOrderDetail")]
        public async Task<IActionResult> DeleteSaleOrderDetail([FromBody] IdObject idObject)
        {
            var saleOrderDetail = await _saleOrderDetailService.GetAll()
                                       .Where(o => o.Id == idObject.Id)
                                       .Include(o => o.SaleOrder)
                                       .FirstOrDefaultAsync();
            _ = saleOrderDetail ?? throw new Exception("删除失败，订单详情数据不存在,请刷新后重试");
            //前端已做校验
            if (saleOrderDetail.PickQuantity > 0)
            {
                throw new Exception("删除失败，该订单已拣货");
            }

            using var transaction = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);

            var product = await _productService.GetByIdAsync(saleOrderDetail.ProductId);
            var historyProduct = await _historyProductService.GetByIdAsync(saleOrderDetail.HistoryProductId);

            // 删除订单详情
            _saleOrderDetailService.Delete(saleOrderDetail);
            // 如果历史产品表中有数据，删除
            if (historyProduct != null)
            {
                _historyProductService.Delete(historyProduct, false);
            }

            // 如果删完了，订单主表也需要删了
            var flag = await _saleOrderDetailService.GetAll()
                        .AnyAsync(o => o.SaleOrderId == saleOrderDetail.SaleOrderId && o.Id != saleOrderDetail.Id);
            if (!flag)
            {
                _saleOrderService.Delete(saleOrderDetail.SaleOrder);
            }
            await _unitWork.SaveChangeAsync();

            lock (QuantityLock.OrderTotalPriceLock)
            {
                var orderDetails = _saleOrderDetailService.GetAll().Where(o => o.SaleOrderId == saleOrderDetail.SaleOrderId).ToList();
                saleOrderDetail.SaleOrder.OrderTotalPrice = orderDetails.Sum(o => o.TotalPrice); // 重新计算订单总价
                _unitWork.SaveChange();
            }
            lock (QuantityLock.SaleQuantityLock)
            {
                // 如果产品表中有数据，需减去需求数量
                if (saleOrderDetail.IsReview && !saleOrderDetail.IsUrgent && product != null)
                {
                    product.DemandQuantity = _productService.GetProductDemandQuantity(product.Id);
                }
                _unitWork.SaveChange();
                transaction.Complete();
            }
            return Ok("删除成功");
        }

        /// <summary>
        /// 批量删除订单详情、订单，历史产品
        /// </summary>
        /// <param name="idObject"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        [HttpPost("MultiDeleteSaleOrderDetail")]
        public async Task<IActionResult> MultiDeleteSaleOrderDetail([FromBody] List<int> ids)
        {
            using (var transaction = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                var saleOrderDetails = await _saleOrderDetailService.GetAll()
                                       .Where(o => ids.Contains(o.Id))
                                       .Include(o => o.SaleOrder)
                                       .ToListAsync();
                if (!saleOrderDetails.Any())
                {
                    throw new Exception("删除失败，订单详情数据不存在,请刷新后重试");
                }
                //前端已做校验
                if (saleOrderDetails.Any(x => x.PickQuantity > 0))
                {
                    throw new Exception("删除失败，该订单已拣货");
                }

                var products = await _productService.GetAll().Where(x => saleOrderDetails.Select(o => o.ProductId).ToList().Contains(x.Id)).ToListAsync();
                var historyProducts = await _historyProductService.GetAll().Where(x => saleOrderDetails.Select(o => o.HistoryProductId).ToList().Contains(x.Id)).ToListAsync();

                // 删除订单详情
                _saleOrderDetailService.Delete(saleOrderDetails);
                // 如果历史产品表中有数据，删除
                if (historyProducts.Any())
                {
                    _historyProductService.Delete(historyProducts, false);
                }
                await _unitWork.SaveChangeAsync();
                // 如果删完了，订单主表也需要删了
                var saleOrderIds = saleOrderDetails.Select(x => x.SaleOrderId).Distinct().ToList();
                foreach (var item in saleOrderIds)
                {
                    var saleOrderDetailList = await _saleOrderDetailService.GetAll()
                                            .Where(o => o.SaleOrderId == item)
                                            .ToListAsync();
                    if (!saleOrderDetailList.Any())
                    {
                        var saleOrder = await _saleOrderService.GetByIdAsync(item);
                        if (saleOrder != null)
                        {
                            _saleOrderService.Delete(saleOrder);
                        }
                    }
                    await _unitWork.SaveChangeAsync();
                }

                lock (QuantityLock.OrderTotalPriceLock)
                {
                    foreach (var saleOrderDetail in saleOrderDetails)
                    {
                        var orderDetails = _saleOrderDetailService.GetAll().Where(o => o.SaleOrderId == saleOrderDetail.SaleOrderId).ToList();
                        saleOrderDetail.SaleOrder.OrderTotalPrice = orderDetails.Sum(o => o.TotalPrice); // 重新计算订单总价
                    }
                    _unitWork.SaveChange();
                }
                lock (QuantityLock.SaleQuantityLock)
                {
                    // 如果已评审不加急的订单,需减去需求数量
                    if (saleOrderDetails.Any(x => x.IsReview) && saleOrderDetails.Any(x => !x.IsUrgent) && products.Any())
                    {
                        var saleOrderDetailsData = saleOrderDetails.Where(z => z.IsReview && !z.IsUrgent).ToList();
                        var productList = _productService.GetAll().Where(x => saleOrderDetailsData.Select(o => o.ProductId).ToList().Contains(x.Id)).ToList();
                        if (productList.Any())
                        {
                            foreach (var product in productList)
                            {
                                product.DemandQuantity = _productService.GetProductDemandQuantity(product.Id);

                            }
                        }
                        _unitWork.SaveChange();
                    }

                    transaction.Complete();

                }
                return Ok("删除成功");
            }
        }

        /// <summary>
        /// 验证导入的客户订单数据
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        [HttpPost("GetImportData")]
        public Task<OkObjectResult> GetImportData([FromBody] List<SaleOrderImportDto> data)
        {
            return Task.Run(() =>
            {
                var customerNos = data.Select(x => x.CustomerNo).Distinct().ToArray();
                var customerNames = data.Select(x => x.CustomerName).Distinct().ToList();
                var customers = _customerService.GetAll()
                                .Where(x => customerNos.Contains(x.Code) || customerNames.Contains(x.Name))
                                .ToList();
                //获取客户Id 处理客户订单号、客户产品图号、下单数量、收货日期
                foreach (var item in data)
                {
                    var customer = customers.Where(x => x.Code == item.CustomerNo || x.Name == item.CustomerName);
                    if (customer.Count() > 0)
                    {
                        item.CustomerId = customer.First().Id;
                        item.CustomerNo = customer.First().Code;
                        item.CustomerName = customer.First().Name;
                        if (customer.Count() > 1)
                        {
                            item.CustomerOptions = customer.Select(x => new SelectionDto()
                            {
                                Value = x.Id.ToString(),
                                Code = x.Code,
                                Text = x.Code + "-" + x.Name
                            }).ToList();
                        }
                    }
                    else
                    {
                        item.CustomerOptions = _customerService.GetAll().Select(x => new SelectionDto()
                        {
                            Value = x.Id.ToString(),
                            Code = x.Code,
                            Text = x.Code + "-" + x.Name
                        }).ToList();
                        item.CustomerOptions.Add(new SelectionDto()
                        {
                            Value = "0",
                            Code = "",
                            Text = "-"
                        });
                    }
                    if (string.IsNullOrEmpty(item.CustomerOrderNo))
                    {
                        item.CustomerOrderNo = null;
                    }
                    if (string.IsNullOrEmpty(item.CustomerOrderNo))
                    {
                        item.CustomerOrderNo = null;
                    }
                    if (item.Quantity < 0 || item.Quantity == null)
                    {
                        item.Quantity = 0;
                    }
                    if (item.DeliveryTime == null)
                    {
                        item.DeliveryTime = DateTime.Now.Date;
                    }
                    item.ReceiveUserId = CurrentUser.Id;
                    item.ReceiveTime = DateTime.Now.Date;
                    item.ReceiveUserName = CurrentUser.UserName;
                }

                return Ok(data);
            });
        }
        /// <summary>
        /// 导入客户订单
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        [HttpPost("ImportData", Name = "ImportData")]
        public async Task<IActionResult> ImportData([FromBody] List<SaleOrderImportDto> data)
        {
            var customerData = _customerService.GetAll().Where(o => data.Select(o => o.CustomerId).ToArray().Contains(o.Id)).ToList();

            using var transaction = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);

            foreach (var customer in customerData)
            {
                //某客户的所有订单 按照客户订单号分组
                var orderData = data.Where(x => x.CustomerId == customer.Id)
                                    .GroupBy(o => o.CustomerOrderNo).ToList();
                foreach (var oData in orderData)
                {
                    //客户-客户订单号 已经存在时
                    var existOrder = _saleOrderService.GetAll().Include(o => o.ReceiveUser)
                                        .Include(o => o.SaleOrderDetails).ThenInclude(o => o.HistoryProduct)
                                        .Include(o => o.SaleOrderDetails).ThenInclude(o => o.Product)
                                        .Where(x => x.CustomerId == customer.Id && x.CustomerOrderNo == oData.Key)
                                        .FirstOrDefault();
                    if (existOrder != null)
                    {
                        var existOrderDetails = _saleOrderDetailService.GetAll()
                                    .Where(x => x.SaleOrderId == existOrder.Id)
                                    .ToList();
                        //如果订单已经确认下单了 检查订单详情是否有更改（订单接收人、接收时间不检查） 更改订单状态为待审核
                        if (existOrder.Status != SaleOrderStatus.待下单)
                        {
                            var isChange = false;
                            var deleteTmp = existOrderDetails.Where(x => !oData.Select(o => o.CustomerMaterialNo).Contains(x.CustomerMaterialNo)).ToList();
                            if (deleteTmp.Count() > 0)
                            {
                                isChange = true;
                            }
                            var addTmp = oData.Where(x => !existOrderDetails.Select(o => o.CustomerMaterialNo).Contains(x.CustomerMaterialNo)).ToList();
                            if (addTmp.Count() > 0)
                            {
                                isChange = true;
                            }
                            var tmp = oData.Where(x => existOrderDetails.Select(o => o.CustomerMaterialNo).Contains(x.CustomerMaterialNo));
                            //客户订单号-客户物料号
                            foreach (var detail in tmp)
                            {
                                var existOrderDetail = existOrderDetails.Where(x => x.CustomerMaterialNo == detail.CustomerMaterialNo).FirstOrDefault();
                                if (existOrderDetail!.PlaceOrderQuantity != detail.Quantity || existOrderDetail.DeliveryTime.Date != detail.DeliveryTime?.Date || existOrderDetail.Remark != detail.Remark)
                                {
                                    isChange = true;
                                    break;
                                }
                            }
                            if (isChange)
                            {
                                //已有订单明细 若发生变化则生成变更记录
                                foreach (var detail in oData)
                                {
                                    detail.CustomerOptions = null;
                                }
                                var newOlder = JsonSerializer.Serialize(oData, JsonHelper.DefaultJsonSerializerOptions);

                                //加锁 一个销售订单 只能存在一条未确认的更改记录
                                lock (ImportLock.saleOrderChangeLock)
                                {
                                    var changeData = _saleOrderChangeService.GetAll().Where(x => x.SaleOrderId == existOrder.Id
                                                && x.SaleOrderChangeType == SaleOrderChangeType.客户导入 && !x.SaleConfirm).FirstOrDefault(); //是否已经存在销售部未确认的更改
                                    if (changeData != null)
                                    {
                                        changeData.CurrentOrder = newOlder;
                                    }
                                    else
                                    {
                                        var oldOrder = JsonSerializer.Serialize(existOrder, JsonHelper.DefaultJsonSerializerOptions);
                                        var change = new SaleOrderChange(oldOrder, newOlder)
                                        {
                                            SaleOrderId = existOrder.Id,
                                            SaleOrderChangeType = SaleOrderChangeType.客户导入,
                                            SaleConfirm = false,
                                            ProductionConfirm = true,
                                        };
                                        _saleOrderChangeService.Insert(change);
                                    }
                                    _unitWork.SaveChange();
                                }

                                existOrder.AuditStatus = SaleOrderAuditStatus.待审核; //订单状态变为待审核
                                //若订单已生产 则自动暂停生产（销售审核、生产部审核通过后  解除暂停状态）【达旺】!!!!  ProductionConfirm=false,
                            }
                        }
                        //订单为待下单状态时 修改订单 修改订单详情(增删改)
                        if (existOrder.Status == SaleOrderStatus.待下单)
                        {
                            existOrder.CustomerAddress = oData.First().Address ?? "";
                            existOrder.ReceiveOrderTime = oData.First().ReceiveTime ?? DateTime.Now.Date;
                            existOrder.ReceiveUserId = oData.First().ReceiveUserId ?? CurrentUser.Id;
                            foreach (var detail in oData)
                            {
                                var product = _customer_Product_RLService.GetAll()
                                        .Where(x => x.CustomerId == customer.Id && x.CustomerMaterialNo == detail.CustomerMaterialNo)
                                        .FirstOrDefault();
                                var existOrderDetail = existOrderDetails.Where(x => x.CustomerMaterialNo == detail.CustomerMaterialNo).FirstOrDefault();
                                if (existOrderDetail != null)
                                {
                                    existOrderDetail.PlaceOrderQuantity = detail.Quantity ?? 0;
                                    existOrderDetail.DeliveryTime = detail.DeliveryTime ?? DateTime.Now.Date;
                                    existOrderDetail.Remark = detail.Remark;
                                    existOrderDetail.ProductId = existOrderDetail.ProductId == null ? product?.ProductId : existOrderDetail.ProductId;
                                }
                                else
                                {
                                    var saleOrderDetail = new SaleOrderDetail()
                                    {
                                        SaleOrder = existOrder,
                                        PlaceOrderQuantity = detail.Quantity ?? 0,
                                        CustomerMaterialNo = detail.CustomerMaterialNo,
                                        Remark = detail.Remark,
                                        DeliveryTime = detail.DeliveryTime ?? DateTime.Now.Date,
                                        ProductId = product?.ProductId,
                                        Status = SaleOrderStatus.待下单,
                                        IsUrgent = false,
                                        IsModel = false,
                                        IsReview = false,
                                    };
                                    await _saleOrderDetailService.InsertAsync(saleOrderDetail);
                                }
                            }

                            var deleteDetails = existOrderDetails.Where(x => !oData.Select(o => o.CustomerMaterialNo).Contains(x.CustomerMaterialNo)).ToList();
                            _saleOrderDetailService.Delete(deleteDetails);
                        }

                        _saleOrderDetailService.GetAll()
                                    .Include(o => o.SaleOrder)
                                    .Where(x => x.SaleOrder.CustomerId == customer.Id && x.SaleOrder.CustomerOrderNo == oData.Key)
                                    .FirstOrDefault();

                        continue;
                    }

                    var saleOrder = new SaleOrder("", "")
                    {
                        CustomerId = customer.Id,
                        Status = SaleOrderStatus.待下单,
                        AuditStatus = SaleOrderAuditStatus.待审核,
                        CustomerOrderNo = oData.Key,
                        ReceiveOrderTime = oData.First().ReceiveTime ?? DateTime.Now.Date,
                        ReceiveUserId = oData.First().ReceiveUserId ?? CurrentUser.Id,
                        CustomerAddress = oData.First().Address ?? "",
                    };
                    await _saleOrderService.InsertAsync(saleOrder);
                    //订单明细
                    var details = oData.ToList();
                    foreach (var detail in details)
                    {
                        var product = _customer_Product_RLService.GetAll()
                                        .Where(x => x.CustomerId == customer.Id && x.CustomerMaterialNo == detail.CustomerMaterialNo)
                                        .FirstOrDefault();
                        var saleOrderDetail = new SaleOrderDetail()
                        {
                            SaleOrder = saleOrder,
                            PlaceOrderQuantity = detail.Quantity ?? 0,
                            CustomerMaterialNo = detail.CustomerMaterialNo,
                            Remark = detail.Remark,
                            DeliveryTime = detail.DeliveryTime ?? DateTime.Now.Date,
                            ProductId = product?.ProductId,
                            Status = SaleOrderStatus.待下单,
                            IsUrgent = false,
                            IsModel = false,
                            IsReview = false,
                            Price = product?.Price ?? 0,
                            TotalPrice = (product?.Price ?? 0) * (detail.Quantity ?? 0),
                        };
                        await _saleOrderDetailService.InsertAsync(saleOrderDetail);
                    }
                }

            }
            await _unitWork.SaveChangeAsync();
            transaction.Complete();

            return Ok("导入成功");
        }

        /// <summary>
        /// 获取导入的客户订单信息
        /// </summary>
        /// <returns></returns>
        [HttpGet("GetImportOrderInfo", Name = "GetImportOrderInfo")]
        public async Task<IActionResult> GetImportOrderInfo([FromQuery] int id)
        {
            var saleOrder = await _saleOrderService.GetAll().Where(o => o.Id == id)
                                        .Include(o => o.SaleOrderDetails).ThenInclude(o => o.Product).ThenInclude(o => o.Customer_Product_RL).ThenInclude(o => o.Customer)
                                        .Include(o => o.SaleOrderDetails).ThenInclude(o => o.HistoryProduct)
                                        .Include(o => o.Customer)
                                        .Include(o => o.ReceiveUser)
                                        .FirstOrDefaultAsync();
            if (saleOrder == null)
            {
                throw new Exception("订单不存在，请刷新重试");
            }
            var products = await _customer_Product_RLService.GetAll().Where(o => o.CustomerId == saleOrder.CustomerId).Select(o => o.Product).ToListAsync();
            var selectedProduct = _productService.GetAll(true).Where(o => saleOrder.SaleOrderDetails.Select(o => o.ProductId).Contains(o.Id)).ToList();
            products = products.Union(selectedProduct).ToList();  //可能选择的产品不在客户产品关系表中
            var files = await _fileStorageService.GetAll().Where(o => o.SourceType == FileStorageSourceType.销售订单 && o.SourceId == saleOrder.Id).ToListAsync();
            var data = new SaleOrderDTO("", "")
            {
                Id = saleOrder.Id,
                OrderNo = saleOrder.OrderNo,
                CustomerOrderNo = saleOrder.CustomerOrderNo,
                Customer = saleOrder.Customer,
                CustomerId = saleOrder.CustomerId,
                ReceiveUserId = saleOrder.ReceiveUserId,
                ReceiveUser = saleOrder.ReceiveUser,
                ReceiveOrderTime = saleOrder.ReceiveOrderTime,
                CustomerAddress = saleOrder.CustomerAddress,
                Remark = saleOrder.Remark,
                OrderTotalPrice = saleOrder.OrderTotalPrice,
                OrderTotalPriceHaveTax = saleOrder.OrderTotalPriceHaveTax,
                OrderFileList = files,
                ProductList = products,
                SaleOrderDetails = saleOrder.SaleOrderDetails.Select(detail => new SaleOrderDetail()
                {
                    Id = detail.Id,
                    ProductId = detail.ProductId,
                    Product = detail.Product,
                    IsUrgent = detail.IsUrgent,
                    IsModel = detail.IsModel,
                    CustomerMaterialNo = detail.CustomerMaterialNo,
                    PlaceOrderQuantity = detail.PlaceOrderQuantity,
                    DeliveryTime = detail.DeliveryTime,
                    //Price = detail.Product != null ? detail.Product.Price : 0,
                    Price = detail.Price,
                    PriceHaveTax = detail.PriceHaveTax,
                    TaxRate = detail.TaxRate,
                    TotalPrice = detail.TotalPrice,
                    TotalPriceHaveTax = detail.TotalPriceHaveTax,
                    MoldCost = detail.MoldCost,
                    Remark = detail.Remark,
                    PickQuantity = detail.PickQuantity,
                    Status = detail.Status,
                    HistoryProduct = detail.HistoryProduct,
                    HistoryProductId = detail.HistoryProductId,
                }).ToList()
            };

            return Ok(data);
        }

        /// <summary>
        /// 暂存导入的客户订单
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        [HttpPost("SaveImportOrder", Name = "SaveImportOrder")]
        public async Task<IActionResult> SaveImportOrder([FromBody] SaleOrderDTO order)
        {
            var saleOrder = await _saleOrderService.GetAll().Where(o => o.Id == order.Id)
                            .Include(o => o.SaleOrderDetails)
                            .FirstOrDefaultAsync();
            if (saleOrder == null)
            {
                throw new Exception("订单不存在，请刷新重试");
            }
            //更新订单
            saleOrder.CustomerOrderNo = order.CustomerOrderNo;
            saleOrder.CustomerAddress = order.CustomerAddress ?? "";
            saleOrder.Remark = order.Remark;
            saleOrder.OrderTotalPrice = order.OrderTotalPrice;
            saleOrder.OrderTotalPriceHaveTax = order.OrderTotalPriceHaveTax;
            saleOrder.ReceiveOrderTime = order.ReceiveOrderTime;
            saleOrder.ReceiveUserId = order.ReceiveUserId;
            saleOrder.CustomerId = order.CustomerId;
            //已有文件更新
            var oldfiles = await _fileStorageService.GetAll().Where(o => o.SourceType == FileStorageSourceType.销售订单 && o.SourceId == saleOrder.Id).ToListAsync();
            var newFiles = order.OrderFileList ?? new List<FileStorage>();
            var deleteFiles = oldfiles.Where(o => !newFiles.Select(n => n.Id).Contains(o.Id)).ToList();
            foreach (var file in deleteFiles)
            {
                //删除服务器文件
                var filePath = _fileStorageService.GetfilePath(file.Base.ToString(), file.Path, file.Uuid);
                System.IO.File.Delete(filePath);
                //删除文件记录
                _fileStorageService.Delete(file);
            }
            //更新订单详情
            if (order.SaleOrderDetails != null)
            {
                var products = await _productService.GetAll().Where(o => order.SaleOrderDetails.Select(x => x.ProductId).Contains(o.Id)).ToListAsync();

                foreach (var detail in order.SaleOrderDetails)
                {
                    var saleOrderDetail = saleOrder.SaleOrderDetails.FirstOrDefault(o => o.Id == detail.Id);
                    if (saleOrderDetail == null)
                    {
                        throw new Exception(detail.CustomerMaterialNo + "订单明细不存在，请刷新重试");
                    }

                    if (detail.ProductId != null)
                    {
                        var product = products.FirstOrDefault(p => p.Id == detail.ProductId);
                        if (product == null)
                        {
                            throw new Exception(detail.CustomerMaterialNo + "产品不存在，请刷新重试");
                        }
                        saleOrderDetail.ProductId = detail.ProductId;

                    }
                    saleOrderDetail.CustomerMaterialNo = detail.CustomerMaterialNo;
                    saleOrderDetail.IsUrgent = detail.IsUrgent;
                    saleOrderDetail.IsModel = detail.IsModel;
                    saleOrderDetail.PlaceOrderQuantity = detail.PlaceOrderQuantity;
                    saleOrderDetail.DeliveryTime = detail.DeliveryTime;
                    saleOrderDetail.TaxRate = detail.TaxRate;
                    saleOrderDetail.Price = detail.Price;
                    saleOrderDetail.PriceHaveTax = detail.PriceHaveTax;
                    saleOrderDetail.TotalPrice = detail.TotalPrice;
                    saleOrderDetail.TotalPriceHaveTax = detail.TotalPriceHaveTax;
                    saleOrderDetail.MoldCost = detail.MoldCost;
                    saleOrderDetail.Remark = detail.Remark;
                }
            }

            _unitWork.SaveChange();
            return Ok("暂存成功");

        }

        /// <summary>
        /// 确认导入的客户订单
        /// </summary>
        /// <returns></returns>
        [HttpPost("ConfirmImportOrder", Name = "ConfirmImportOrder")]
        public async Task<IActionResult> ConfirmImportOrder([FromBody] SaleOrderDTO order)
        {
            var saleOrder = await _saleOrderService.GetAll().Where(o => o.Id == order.Id)
                            .Include(o => o.SaleOrderDetails)
                            .FirstOrDefaultAsync();
            if (saleOrder == null)
            {
                throw new Exception("订单不存在，请刷新重试");
            }
            using var transaction = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);

            //生成订单编号
            var orderNo = _sequenceService.GenerateCode(TableName.SaleOrder);
            //更新订单
            saleOrder.OrderNo = orderNo;
            saleOrder.CustomerOrderNo = order.CustomerOrderNo;
            saleOrder.CustomerAddress = order.CustomerAddress;
            saleOrder.Remark = order.Remark;
            saleOrder.OrderTotalPrice = order.OrderTotalPrice;
            saleOrder.OrderTotalPriceHaveTax = order.OrderTotalPriceHaveTax;
            saleOrder.ReceiveOrderTime = order.ReceiveOrderTime;
            saleOrder.ReceiveUserId = order.ReceiveUserId;
            saleOrder.CustomerId = order.CustomerId;
            saleOrder.AuditStatus = SaleOrderAuditStatus.审核通过;
            saleOrder.Status = SaleOrderStatus.进行中;
            //已有文件更新
            var oldfiles = await _fileStorageService.GetAll().Where(o => o.SourceType == FileStorageSourceType.销售订单 && o.SourceId == saleOrder.Id).ToListAsync();
            var newFiles = order.OrderFileList ?? new List<FileStorage>();
            var deleteFiles = oldfiles.Where(o => !newFiles.Select(n => n.Id).Contains(o.Id)).ToList();
            foreach (var file in deleteFiles)
            {
                //删除服务器文件
                var filePath = _fileStorageService.GetfilePath(file.Base.ToString(), file.Path, file.Uuid);
                System.IO.File.Delete(filePath);
                //删除文件记录
                _fileStorageService.Delete(file);
            }

            var products = new List<Product>();
            if (order.SaleOrderDetails != null)
            {
                var productIds = order.SaleOrderDetails.Select(o => o.ProductId).ToList();
                products = await _productService.GetAll().Where(o => productIds.Contains(o.Id)).ToListAsync();
                List<Customer_Product_RL> cp_rl = await _customer_Product_RLService.GetAll()
                                         .Where(o => o.CustomerId == saleOrder.CustomerId)
                                         .ToListAsync();
                foreach (var detail in order.SaleOrderDetails)
                {
                    var saleOrderDetail = saleOrder.SaleOrderDetails.FirstOrDefault(o => o.Id == detail.Id);
                    if (saleOrderDetail == null)
                    {
                        throw new Exception(detail.CustomerMaterialNo + "订单明细不存在，请刷新重试");
                    }
                    var product = products.FirstOrDefault(p => p.Id == detail.ProductId);
                    if (product == null)
                    {
                        throw new Exception(detail.CustomerMaterialNo + "产品不存在，请刷新重试");
                    }

                    //生成历史产品
                    var historyProduct = new HistoryProduct(product.ProductNo, product.ProductName, product.Unit)
                    {
                        ProductId = product.Id,
                        CustomerMaterialNo = product.CustomerMaterialNo,
                        ProductNo = product.ProductNo,
                        Specification = product.Specification,
                        MaterialGrade = product.MaterialGrade,
                        MaterialSpecification = product.MaterialSpecification,
                        Version = product.Version,
                        Remark = product.Remark,
                        EnterpriseId = product.EnterpriseId,
                        IsDeleted = false,
                        CustomAttribute = product.CustomAttribute,
                        SerialNo = product.SerialNo,
                        Weight = product.Weight,
                        ProductCategory = product.ProductCategory,
                    };
                    await _historyProductService.InsertAsync(historyProduct);

                    //更新订单详情
                    saleOrderDetail.CustomerMaterialNo = detail.CustomerMaterialNo;
                    saleOrderDetail.ProductId = detail.ProductId;
                    saleOrderDetail.HistoryProduct = historyProduct;
                    saleOrderDetail.IsUrgent = detail.IsUrgent;
                    saleOrderDetail.IsModel = detail.IsModel;
                    saleOrderDetail.PlaceOrderQuantity = detail.PlaceOrderQuantity;
                    saleOrderDetail.DeliveryTime = detail.DeliveryTime;
                    saleOrderDetail.TaxRate = detail.TaxRate;
                    saleOrderDetail.Price = detail.Price;
                    saleOrderDetail.PriceHaveTax = detail.PriceHaveTax;
                    saleOrderDetail.TotalPrice = detail.TotalPrice;
                    saleOrderDetail.TotalPriceHaveTax = detail.TotalPriceHaveTax;
                    saleOrderDetail.MoldCost = detail.MoldCost;
                    saleOrderDetail.Remark = detail.Remark;
                    saleOrderDetail.Status = SaleOrderStatus.进行中;

                    //客户-客户物料号-产品
                    var cp = cp_rl.FirstOrDefault(cp => cp.CustomerMaterialNo == saleOrderDetail.CustomerMaterialNo);
                    if (cp == null)
                    {//新增关系表
                        var customerProduct = new Customer_Product_RL()
                        {
                            CustomerId = saleOrder.CustomerId,
                            ProductId = product.Id,
                            CustomerMaterialNo = saleOrderDetail.CustomerMaterialNo,
                            Price = saleOrderDetail.Price,
                        };
                        _customer_Product_RLService.Insert(customerProduct);
                        cp_rl.Add(customerProduct);
                    }
                    else
                    {
                        if (cp.ProductId != product.Id)
                        {
                            cp.ProductId = product.Id;
                        }
                        cp.Price = saleOrderDetail.Price; //使其价格一致保持最后一次下单时的价格
                    }
                    //product.Price = saleOrderDetail.Price;
                }
            }

            await _unitWork.SaveChangeAsync();
            // 加锁修改产品需求数量
            lock (QuantityLock.SaleQuantityLock)
            {
                foreach (var product in products)
                {
                    product.DemandQuantity = _productService.GetProductDemandQuantity(product.Id);
                }
                _unitWork.SaveChange();
                transaction.Complete();
            }

            return Ok("订单确认成功");
        }

        /// <summary>
        /// 获取某订单 待确认的变更记录
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet("GetOrderChange", Name = "GetOrderChange")]
        public async Task<IActionResult> GetOrderChange([FromQuery] int id)
        {
            var change = await _saleOrderChangeService.GetAll().Where(o => o.SaleOrderId == id
                                && o.SaleOrderChangeType == SaleOrderChangeType.客户导入 && !o.SaleConfirm).FirstOrDefaultAsync();
            if (change == null)
            {
                throw new Exception("未找到变更记录,请刷新重试");
            }
            var data = JsonSerializer.Deserialize<List<SaleOrderImportDto>>(change.CurrentOrder, JsonHelper.DefaultJsonSerializerOptions);
            return Ok(data);
        }

        /// <summary>
        /// 确认客户订单的更改
        /// </summary>
        /// <returns></returns>
        [HttpPost("ConfirmOrderChange", Name = "ConfirmOrderChange")]
        public async Task<IActionResult> ConfirmOrderChange([FromBody] SaleOrderDTO order)
        {
            var saleOrder = await _saleOrderService.GetAll().Where(o => o.Id == order.Id)
                            .Include(o => o.SaleOrderDetails)
                            .FirstOrDefaultAsync();
            if (saleOrder == null)
            {
                throw new Exception("订单不存在，请刷新重试");
            }

            using var transaction = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);

            lock (ImportLock.saleOrderChangeLock) //保证一个变更记录 只能确认一次
            {
                var changeData = _saleOrderChangeService.GetAll().Where(x => x.SaleOrderId == saleOrder.Id
                                                 && x.SaleOrderChangeType == SaleOrderChangeType.客户导入 && !x.SaleConfirm).FirstOrDefault(); //是否存在销售部未确认的更改
                if (changeData == null)
                {
                    throw new Exception("未找到变更记录,请刷新重试");
                }
                //修改变更记录状态 销售完成变更确认
                changeData.SaleConfirm = true;
                _unitWork.SaveChange();
            }

            //更新订单
            saleOrder.CustomerAddress = order.CustomerAddress;
            saleOrder.Remark = order.Remark;
            saleOrder.OrderTotalPrice = order.OrderTotalPrice;
            saleOrder.OrderTotalPriceHaveTax = order.OrderTotalPriceHaveTax;
            saleOrder.ReceiveOrderTime = order.ReceiveOrderTime;
            saleOrder.ReceiveUserId = order.ReceiveUserId;
            saleOrder.AuditStatus = SaleOrderAuditStatus.审核通过;

            //已有文件更新
            var oldfiles = await _fileStorageService.GetAll().Where(o => o.SourceType == FileStorageSourceType.销售订单 && o.SourceId == saleOrder.Id).ToListAsync();
            var newFiles = order.OrderFileList ?? new List<FileStorage>();
            var deleteFiles = oldfiles.Where(o => !newFiles.Select(n => n.Id).Contains(o.Id)).ToList();
            foreach (var file in deleteFiles)
            {
                //删除服务器文件
                var filePath = _fileStorageService.GetfilePath(file.Base.ToString(), file.Path, file.Uuid);
                System.IO.File.Delete(filePath);
                //删除文件记录
                _fileStorageService.Delete(file);
            }

            var products = new List<Product>();
            if (order.SaleOrderDetails != null)
            {
                var productIds = order.SaleOrderDetails.Select(o => o.ProductId).ToList();
                products = await _productService.GetAll().Where(o => productIds.Contains(o.Id)).ToListAsync();
                List<Customer_Product_RL> cp_rl = await _customer_Product_RLService.GetAll()
                                         .Where(o => o.CustomerId == saleOrder.CustomerId)
                                         .ToListAsync(); //客户所有的产品
                foreach (var detail in order.SaleOrderDetails)
                {
                    var product = products.FirstOrDefault(p => p.Id == detail.ProductId);
                    if (product == null)
                    {
                        throw new Exception(detail.CustomerMaterialNo + "产品不存在，请刷新重试");
                    }
                    if (detail.Id <= 0)//新导入的产品
                    {
                        //生成历史产品
                        var newHistory = new HistoryProduct(product.ProductNo, product.ProductName, product.Unit)
                        {
                            ProductId = product.Id,
                            CustomerMaterialNo = product.CustomerMaterialNo,
                            ProductNo = product.ProductNo,
                            Specification = product.Specification,
                            MaterialGrade = product.MaterialGrade,
                            MaterialSpecification = product.MaterialSpecification,
                            Version = product.Version,
                            Remark = product.Remark,
                            EnterpriseId = product.EnterpriseId,
                            IsDeleted = false,
                            CustomAttribute = product.CustomAttribute,
                            SerialNo = product.SerialNo,
                            Weight = product.Weight,
                            ProductCategory = product.ProductCategory,
                        };
                        await _historyProductService.InsertAsync(newHistory);
                        var newDetail = new SaleOrderDetail()
                        {
                            SaleOrder = saleOrder,
                            PlaceOrderQuantity = detail.PlaceOrderQuantity,
                            Price = detail.Price,
                            PriceHaveTax = detail.PriceHaveTax,
                            TaxRate = detail.TaxRate,
                            TotalPrice = detail.TotalPrice,
                            TotalPriceHaveTax = detail.TotalPriceHaveTax,
                            MoldCost = detail.MoldCost,
                            CustomerMaterialNo = detail.CustomerMaterialNo,
                            Remark = detail.Remark,
                            DeliveryTime = detail.DeliveryTime,
                            ProductId = product.Id,
                            HistoryProduct = newHistory,
                            Status = saleOrder.Status,
                            IsUrgent = detail.IsUrgent,
                            IsModel = detail.IsModel,
                            IsReview = false,
                        };
                        await _saleOrderDetailService.InsertAsync(newDetail);
                        continue;
                    }

                    var saleOrderDetail = saleOrder.SaleOrderDetails.FirstOrDefault(o => o.Id == detail.Id);
                    if (saleOrderDetail == null)
                    {
                        throw new Exception(detail.CustomerMaterialNo + "订单明细不存在，请刷新重试");
                    }

                    //更新订单详情
                    saleOrderDetail.CustomerMaterialNo = detail.CustomerMaterialNo;
                    saleOrderDetail.IsUrgent = detail.IsUrgent;
                    saleOrderDetail.IsModel = detail.IsModel;
                    saleOrderDetail.PlaceOrderQuantity = detail.PlaceOrderQuantity;
                    saleOrderDetail.DeliveryTime = detail.DeliveryTime;
                    saleOrderDetail.Price = detail.Price;
                    saleOrderDetail.PriceHaveTax = detail.PriceHaveTax;
                    saleOrderDetail.TaxRate = detail.TaxRate;
                    saleOrderDetail.TotalPrice = detail.TotalPrice;
                    saleOrderDetail.TotalPriceHaveTax = detail.TotalPriceHaveTax;
                    saleOrderDetail.MoldCost = detail.MoldCost;
                    saleOrderDetail.Remark = detail.Remark;
                    if (saleOrderDetail.ProductId != detail.ProductId)
                    {
                        //已经送货的订单明细不能修改产品
                        if (saleOrderDetail.PickQuantity > 0 || saleOrderDetail.DeliveryNoticeQuantity > 0 || saleOrderDetail.DeliveryQuantity > 0)
                        {
                            throw new Exception(saleOrderDetail.CustomerMaterialNo + "订单明细已经拣选送货，不能修改产品");
                        }

                        saleOrderDetail.ProductId = detail.ProductId;
                        //删除历史产品
                        var oldHistory = await _historyProductService.GetByIdAsync(saleOrderDetail.HistoryProductId);
                        if (oldHistory != null)
                        {
                            _historyProductService.Delete(oldHistory);
                        }
                        //生成新历史产品
                        var newHistory1 = new HistoryProduct(product.ProductNo, product.ProductName, product.Unit)
                        {
                            ProductId = product.Id,
                            CustomerMaterialNo = product.CustomerMaterialNo,
                            ProductNo = product.ProductNo,
                            Specification = product.Specification,
                            MaterialGrade = product.MaterialGrade,
                            MaterialSpecification = product.MaterialSpecification,
                            Version = product.Version,
                            Remark = product.Remark,
                            EnterpriseId = product.EnterpriseId,
                            IsDeleted = false,
                            CustomAttribute = product.CustomAttribute,
                            SerialNo = product.SerialNo,
                            Weight = product.Weight,
                            ProductCategory = product.ProductCategory,
                        };
                        await _historyProductService.InsertAsync(newHistory1);
                        saleOrderDetail.HistoryProduct = newHistory1;
                    }

                    //客户-客户物料号-产品
                    var cp = cp_rl.FirstOrDefault(cp => cp.CustomerMaterialNo == saleOrderDetail.CustomerMaterialNo);
                    if (cp == null)
                    {//新增关系表
                        var customerProduct = new Customer_Product_RL()
                        {
                            CustomerId = saleOrder.CustomerId,
                            ProductId = product.Id,
                            CustomerMaterialNo = saleOrderDetail.CustomerMaterialNo,
                            Price = saleOrderDetail.Price,
                        };
                        _customer_Product_RLService.Insert(customerProduct);
                        cp_rl.Add(customerProduct);
                    }
                    else
                    {
                        if (cp.ProductId != product.Id)
                        {
                            cp.ProductId = product.Id;
                        }
                        cp.Price = saleOrderDetail.Price; //使其价格一致保持最后一次下单时的价格
                    }
                    //product.Price = saleOrderDetail.Price;
                }

                //删除的订单详情 删除历史产品
                var deleteDetails = saleOrder.SaleOrderDetails.Where(o => !order.SaleOrderDetails.Select(x => x.Id).Contains(o.Id)).ToList();
                foreach (var deleteDetail in deleteDetails)
                {
                    if (deleteDetail.PickQuantity > 0 || deleteDetail.DeliveryNoticeQuantity > 0 || deleteDetail.DeliveryQuantity > 0)
                    {
                        throw new Exception(deleteDetail.CustomerMaterialNo + "订单明细已经拣选送货，不能删除");
                    }
                    var oldHistory = await _historyProductService.GetByIdAsync(deleteDetail.HistoryProductId);
                    if (oldHistory != null)
                    {
                        _historyProductService.Delete(oldHistory);
                    }
                    _saleOrderDetailService.Delete(deleteDetail);
                }
            }

            await _unitWork.SaveChangeAsync();

            // 加锁修改产品需求数量
            lock (QuantityLock.SaleQuantityLock)
            {
                foreach (var product in products)
                {
                    product.DemandQuantity = _productService.GetProductDemandQuantity(product.Id);
                }
                _unitWork.SaveChange();
                transaction.Complete();
            }

            return Ok("更改确认成功");
        }

        /// <summary>
        /// 获取某订单所有的变更记录
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet("GetOrderChangeList", Name = "GetOrderChangeList")]
        public async Task<IActionResult> GetOrderChangeList([FromQuery] int id)
        {
            var changes = await _saleOrderChangeService.GetAll().Where(o => o.SaleOrderId == id).ToListAsync();
            return Ok(changes);
        }

        [HttpPost("DeleteOrderChange", Name = "DeleteOrderChange")]
        public async Task<IActionResult> DeleteOrderChange([FromBody] IdObject idObject)
        {
            var change = _saleOrderChangeService.GetById(idObject.Id);
            if (change == null)
            {
                throw new Exception("未找到变更记录,请刷新重试");
            }
            _saleOrderChangeService.Delete(change);
            var order = _saleOrderService.GetById(change.SaleOrderId);
            if (order != null)
            {
                order.AuditStatus = SaleOrderAuditStatus.审核通过; //订单状态back to 审核通过
            }
            await _unitWork.SaveChangeAsync();
            return Ok("删除成功");
        }

        [HttpGet("GetOrderChangeById", Name = "GetOrderChangeById")]
        public async Task<IActionResult> GetOrderChangeById([FromQuery] int id)
        {
            var change = await _saleOrderChangeService.GetByIdAsync(id);
            if (change == null)
            {
                throw new Exception("未找到变更记录,请刷新重试");
            }
            var current = JsonSerializer.Deserialize<List<SaleOrderImportDto>>(change.CurrentOrder, JsonHelper.DefaultJsonSerializerOptions);
            var previous = JsonSerializer.Deserialize<SaleOrder>(change.PreviousOrder, JsonHelper.DefaultJsonSerializerOptions);

            var data = new
            {
                SaleOrder = previous,
                Import = current
            };
            return Ok(data);
        }


        /// <summary>
        /// 修改单条明细审核状态
        /// </summary>
        /// <param name="id"></param>
        /// <param name="isReview"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        [HttpGet("ChangeDetailReviewStatusById", Name = "ChangeDetailReviewStatusById")]
        public async Task<IActionResult> ChangeDetailReviewStatusById([FromQuery] int id, bool isReview)
        {
            using (var transaction = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                var saleOrderDetail = await _saleOrderDetailService.GetAll(true).Include(x => x.Product).Where(x => x.Id == id).FirstOrDefaultAsync();
                if (saleOrderDetail == null)
                {
                    throw new Exception("销售明细不存在，请刷新页面");
                }
                saleOrderDetail.IsReview = isReview;
                await _unitWork.SaveChangeAsync();
                if (saleOrderDetail.Product == null)
                {
                    throw new Exception("销售产品不存在，请刷新页面");
                }
                saleOrderDetail.Product.DemandQuantity = _productService.GetProductDemandQuantity(saleOrderDetail.ProductId ?? 0);
                await _unitWork.SaveChangeAsync();
                transaction.Complete();
                return Ok("保存成功");
            }
        }
    }
}