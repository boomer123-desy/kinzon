using Kingzon.OLCM.Base.Services;
using Kingzon.OLCM.Base.Services.JsonStructure;
using KingZon.Core;
using KingZon.Core.DataBase;
using KingZon.Core.Web;
using KingZon.OLCM.Amoeba.DataBase.Entities;
using KingZon.OLCM.Base.DataBase.Enum;
using KingZon.OLCM.Base.Service;
using KingZon.OLCM.Base.Service.Dto;
using KingZon.OLCM.Base.Service.Lock;
using KingZon.OLCM.Base.Shared.DataBase.Entities;
using KingZon.OLCM.Base.Shared.DataBase.Enum;
using KingZon.OLCM.Base.Shared.DataBase.JsonStructure;
using KingZon.OLCM.Production.Shared.DataBase.Dto;
using KingZon.OLCM.Production.Shared.DataBase.Entities;
using KingZon.OLCM.Production.Shared.DataBase.Enum;
using KingZon.OLCM.Production.Shared.DataBase.JsonStructure;
using KingZon.OLCM.Production.WebApi.Dto;
using KingZon.OLCM.Sale.Service.Dto;
using KingZon.OLCM.Sale.Shared.DataBase.Entities;
using KingZon.OLCM.Technical.Shared.DataBase.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Transactions;

namespace KingZon.OLCM.Production.WebApi.Controllers
{

    [Route("api/production/[controller]")]
    [ApiController]
    public class DispatchOrderManageController : KingZonControllerBase
    {
        private readonly ProductService _productService;
        private readonly CommonService<Base_ProductSpecification> _specificationService;
        private readonly CommonService<Base_UnitConversion> _unitConversionService;
        private readonly CommonService<Base_Unit> _unitService;
        private readonly CommonService<Production_Dispatch_Order> _dispatchOrderService;
        private readonly CommonService<Production_Dispatch_OperationRecord> _dispatchOperationRecordService;
        private readonly CommonService<Amoeba_ProductProcessPrice> _productProcessPriceService;
        private readonly SequenceService _sequenceService;
        private readonly CommonService<Production_Material_Receive_Demand> _materialReceiveDemandService;
        private readonly CommonService<Technical_FormulaVersion> _formulaVersionService;
        private readonly CommonService<Production_Dispatch_SaleRL> _dispatchSaleRLService;
        private readonly CommonService<Sale_OrderDetail> _saleOrderDetailService;
        private readonly IUnitOfWork _unitOfWork;

        public DispatchOrderManageController(
            ProductService productService,
            CommonService<Base_ProductSpecification> specificationService,
            CommonService<Base_UnitConversion> unitConversionService,
            CommonService<Base_Unit> unitService,
            CommonService<Production_Dispatch_Order> dispatchOrderService,
            CommonService<Production_Dispatch_OperationRecord> dispatchOperationRecordService,
            CommonService<Amoeba_ProductProcessPrice> productProcessPriceService,
            SequenceService sequenceService,
            CommonService<Production_Material_Receive_Demand> materialReceiveDemandService,
            CommonService<Technical_FormulaVersion> formulaVersionService,
            CommonService<Production_Dispatch_SaleRL> dispatchSaleRLService,
            CommonService<Sale_OrderDetail> saleOrderDetailService,
            IUnitOfWork unitOfWork)
        {
            _productService = productService;
            _specificationService = specificationService;
            _unitConversionService = unitConversionService;
            _unitService = unitService;
            _dispatchOrderService = dispatchOrderService;
            _dispatchOperationRecordService = dispatchOperationRecordService;
            _productProcessPriceService = productProcessPriceService;
            _sequenceService = sequenceService;
            _materialReceiveDemandService = materialReceiveDemandService;
            _formulaVersionService = formulaVersionService;
            _dispatchSaleRLService = dispatchSaleRLService;
            _saleOrderDetailService = saleOrderDetailService;
            _unitOfWork = unitOfWork;
        }

        /// <summary>
        /// 获取所有派工单
        /// </summary>
        /// <param name="filterList"></param>
        /// <returns></returns>
        [HttpPost("GetDispatchOrderList")]
        public async Task<IActionResult> GetDispatchOrderList([FromBody] GetDispatchDto dto)
        {
            var options = from op in _dispatchOperationRecordService.GetAll()
                          where op.OperateType == OperateType.接单
                          group op by op.DispatchOrderId
                          into g
                          select new GetReceiveOperationSumDto
                          {
                              DispatchOrderId = g.Key,
                              ReceiveQuantity = g.Sum(x => x.OperateQuantity),
                              MainReceiveQuantity = g.Sum(x => x.MainQuantity),
                              BaseReceiveQuantity = g.Sum(x => x.BaseQuantity),
                              IncomeReceiveQuantity = g.Sum(x => x.IncomeQuantity),
                          };
            var dispatchQuery = _dispatchOrderService.GetAll(true);
            if (dto.saleFilterList.Count > 0)
            {
                var saleIds = _saleOrderDetailService.GetAll().ApplyFilters(dto.saleFilterList).Select(x => x.Id);
                var rl = _dispatchSaleRLService.GetAll().Where(x => saleIds.Contains(x.SaleOrderDetailId));
                dispatchQuery = dispatchQuery.Join(rl, a => a.Id, b => b.DispatchOrderId, (a, b) => a);
            }
            var query = from order in dispatchQuery
                        join product in _productService.GetAll(true)
                             on order.ProductId equals product.Id
                        join priceInfo in _productProcessPriceService.GetAll()
                             on new { order.DepartmentId, order.ProductId, ProductSpecificationId = order.SpecificationId }
                             equals new { priceInfo.DepartmentId, priceInfo.ProductId, priceInfo.ProductSpecificationId }
                             into temp
                        from priceInfo in temp.DefaultIfEmpty()
                        join furmula in _formulaVersionService.GetAll(true)
                             on order.FormulaVersionId equals furmula.Id
                                into furmulaTemp
                        from furmula in furmulaTemp.DefaultIfEmpty()
                        join op in options
                        on order.Id equals op.DispatchOrderId
                             into opTemp
                        from op in opTemp.DefaultIfEmpty()
                        select new GetDispatchOrderDto
                        {
                            Id = order.Id,
                            OrderNo = order.OrderNo,
                            ProductId = product.Id,
                            SpecificationId = order.SpecificationId,
                            PackageId = order.PackageId,
                            PackageSpecification = order.ProductPackage != null ? order.ProductPackage!.PackageSpecification : string.Empty,
                            TechnicalCode = product.TechnicalCode,
                            ProductNo = order.ProductSpecification != null ? order.ProductSpecification!.Code ?? "" : product.ProductNo,
                            ProductName = order.ProductSpecification != null ? order.ProductSpecification!.Name ?? "" : product.ProductName,
                            Specification = order.ProductSpecification != null ? order.ProductSpecification!.Specification : string.Empty,
                            ProductCategory = order.ProductSpecification != null ? order.ProductSpecification!.ProductCategory != null ?
                                                    order.ProductSpecification!.ProductCategory!.Name : string.Empty
                                                        : product.ProductCategory.Name,
                            ProductCategoryId = order.ProductSpecification != null ? order.ProductSpecification!.ProductCategoryId ?? 0 : product.ProductCategoryId,
                            PackageWay = order.ProductSpecification != null && order.ProductSpecification!.ContainerModel != null ? order.ProductSpecification!.ContainerModel!.DataValue : string.Empty,
                            ProductRemark = order.ProductSpecification != null ? order.ProductSpecification!.Remark ?? "" : product.Remark,
                            DepartmentId = order.DepartmentId,
                            DepartmentName = order.Department.Name,
                            Price = order.Price,
                            Remark = order.Remark,
                            DispatchQuantity = order.DispatchQuantity,
                            CompleteQuantity = order.CompleteQuantity,
                            DispatchUnit = order.DispatchUnit,
                            BaseQuantity = order.BaseQuantity,
                            MainQuantity = order.MainQuantity,
                            MainCompleteQuantity = order.MainCompleteQuantity,
                            BaseCompleteQuantity = order.BaseCompleteQuantity,
                            IncomeQuantity = order.IncomeQuantity,
                            IncomeCompleteQuantity = order.IncomeCompleteQuantity,
                            Status = order.Status,
                            IsDeleted = order.IsDeleted,
                            DispatchDate = order.DispatchDate,
                            ProductType = product.ProductType,
                            ProcessPriceId = priceInfo != null ? priceInfo.Id : null,
                            CreateTime = order.CreateTime,
                            CreateUserName = order.CreateUserName,
                            PlanCompleteDate = order.PlanCompleteDate,
                            CompleteDate = order.CompleteDate,
                            ReceiveQuantity = op != null ? op.ReceiveQuantity ?? 0 : 0,
                            MainReceiveQuantity = op != null ? op.MainReceiveQuantity ?? 0 : 0,
                            BaseReceiveQuantity = op != null ? op.BaseReceiveQuantity ?? 0 : 0,
                            IncomeReceiveQuantity = op != null ? op.IncomeReceiveQuantity ?? 0 : 0,
                            IsStart = order.DispatchDate.Date <= DateTime.Now.Date ? true : false,
                            FormulaName = furmula != null ? furmula.Formula.Name : string.Empty,
                            FormulaVersionId = order.FormulaVersionId,
                            CustomAttribute = order.CustomAttribute,
                            TotalInboundQuantity = order.TotalInboundQuantity
                        };
            var result = await query.ApplyFilters(dto.filterList).ApplySorts(this.SortInfoList).ToPagedListAsync(this.PageModel);

            var dispatchIds = result.Data.Select(x => x.Id).ToList();
            var saleData = (from saleRL in _dispatchSaleRLService.GetAll().Include(x => x.Customer).Where(x => dispatchIds.Contains(x.DispatchOrderId))
                            join sale in _saleOrderDetailService.GetAll().Include(x => x.SaleOrder)
                                on saleRL.SaleOrderDetailId equals sale.Id
                            select new { saleRL, sale }).ToList();
            foreach (var item in result.Data)
            {
                var sale = saleData.Where(x => x.saleRL.DispatchOrderId == item.Id).ToList();
                if (sale.Count > 0)
                {
                    item.SaleOrders = sale.Select(x =>
                    {
                        var UnitList = x.saleRL.UnitConversionInfo != null ? JsonSerializer.Deserialize<List<DispatchUnitDto>>(x.saleRL.UnitConversionInfo, JsonHelper.DefaultJsonSerializerOptions) ?? new List<DispatchUnitDto>() : new List<DispatchUnitDto>();
                        return new SaleOrderDetailDispatchDTO
                        {
                            Id = x.sale.Id,
                            CustomerId = x.saleRL.CustomerId,
                            OrderNo = x.sale.SaleOrder.OrderNo,
                            CustomerName = x.saleRL.Customer.CustomerName,
                            DispatchQuantity = x.saleRL.DispatchQuantity,
                            DispatchUnitConversionId = x.saleRL.DispatchUnitConversionId,
                            DispatchUnit = UnitList.FirstOrDefault(e => e.Id == x.saleRL.DispatchUnitConversionId)?.UnitName,
                            DispatchUnitList = UnitList,
                        };
                    }
                   ).ToList();
                }
            }
            return Ok(result);
        }



        /// <summary>
        /// 获取生产相关数据的产品列表
        /// </summary>
        /// <param name="filterList"></param>
        /// <returns></returns>
        [HttpPost("GetProductListByProduction")]
        public async Task<IActionResult> GetProductListByProduction([FromBody] GetProductListByProductionFilters postData)
        {
            var query = _productService.GetProductList([], []);
            if (postData.DepartmentId.HasValue)
            {
                var deptProducts = _productProcessPriceService.GetAll().Where(x => x.DepartmentId == postData.DepartmentId);
                query = query.Join(deptProducts,
                    a => new { ProductId = a.ProductId, SpecificationId = a.SpecificationId },
                    b => new { ProductId = b.ProductId, SpecificationId = b.ProductSpecificationId },
                    (a, b) => a);
            }
            var products = await query
                .Select(x => new GetProductListByProductionDto
                {
                    Id = x.SpecificationId != null ? (-x.SpecificationId ?? 0) : x.ProductId,
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
                    ProductMainUnitId = x.ProductMainUnitId,
                    ProductMainUnit = x.ProductMainUnit,
                    MaterialGrade = x.MaterialGrade,
                    MaterialSpecification = x.MaterialSpecification,
                    CustomAttribute = x.CustomAttribute,
                    OccupyInventory = x.OccupyInventory,
                    AbnormalQuantity = x.AbnormalQuantity,
                })
                .Where(s => !postData.productIds.Contains(s.ProductId))
                .ApplyFilters(postData.filterList)
                .ApplySorts(this.SortInfoList)
                .ToPagedListAsync(this.PageModel);

            var productList = products.Data.Select(x => x.ProductId).ToList();

            //获取各单位生产数量
            var productionInfo = await (from info in _dispatchOrderService.GetAll()
                                        where info.Status == DispatchOrderStatus.进行中
                                        && info.BaseQuantity > info.BaseCompleteQuantity
                                        && productList.Contains(info.ProductId)
                                        group info by new
                                        {
                                            info.ProductId,
                                            info.SpecificationId,
                                            UnitId = EF.Functions.JsonExtract<long>(info.DispatchUnit, "$.baseUnitId")
                                        } into g
                                        select new GetProductionSumInfoDto
                                        {
                                            ProductId = g.Key.ProductId,
                                            ProductSpecificationId = g.Key.SpecificationId,
                                            UnitId = Convert.ToInt64(g.Key.UnitId),
                                            UnitName = EF.Functions.JsonExtract<string>(g.First().DispatchUnit, "$.baseUnitName"),
                                            Quantity = g.Sum(x => x.BaseQuantity - x.BaseCompleteQuantity)
                                        }).ToListAsync();


            foreach (var item in products.Data)
            {
                var list = productionInfo.Where(x => x.ProductId == item.ProductId && x.ProductSpecificationId == item.SpecificationId).ToList();
                item.WaitProductionList = list;
            }

            return Ok(products);
        }

        /// <summary>
        /// 获取存在工序价格的产品部门
        /// </summary>
        /// <param name="products"></param>
        /// <returns></returns>
        [HttpPost("GetDepartmentsByPrcessPrice")]
        public async Task<IActionResult> GetDepartmentsByPrcessPrice([FromBody] List<PostDepartmentsByPrcessPriceDto> products)
        {
            var nullSpe = products.Where(x => x.SpecificationId == null).Select(x => x.ProductId).ToList();
            var unNullSpe = products.Where(x => x.SpecificationId.HasValue).Select(x => x.SpecificationId).ToList();
            var departments = await (from info in _productProcessPriceService.GetAll()
                                     where ((nullSpe.Contains(info.ProductId) && info.ProductSpecificationId == null)
                                     || unNullSpe.Contains(info.ProductSpecificationId))
                                     && (info.Department.ComputedType == ComputedType.包装统计
                                     || info.Department.ComputedType == ComputedType.生产统计)
                                     select new GetDepartmentsByPrcessPriceDto
                                     {
                                         Id = info.Id,
                                         ProductId = info.ProductId,
                                         SpecificationId = info.ProductSpecificationId,
                                         UnitId = info.PriceUnitId,
                                         UnitName = info.PriceUnit != null ? info.PriceUnit.Name : string.Empty,
                                         Price = info.Price,
                                         Value = info.DepartmentId,
                                         Text = info.Department.Name,
                                         PriceUnitConversionId = info.PriceUnitConversionId,
                                     }).ToListAsync();
            return Ok(departments);
        }

        /// <summary>
        /// 新增派工单
        /// </summary>
        /// <param name="postData"></param>
        /// <returns></returns>
        [HttpPost("AddDispatchOrderList")]
        public async Task<IActionResult> AddDispatchOrderList([FromBody] List<PostDispatchOrderDto> postData)
        {
            var list = new List<Production_Dispatch_Order>();
            var materialDemands = new List<Production_Material_Receive_Demand>();
            var priceIds = postData.Select(x => x.ProcessPriceId).ToList();
            foreach (var item in postData)
            {
                var basisType = item.BasisType;
                var basisId = item.BasisId;
                // 兼容原配方提交，BOM直接使用提交的清单版本Id。
                if (basisType == DispatchBasisType.未指定 && item.MaterialDemands?.FormulaVersionId != null)
                {
                    basisType = DispatchBasisType.配方;
                    basisId = item.MaterialDemands.FormulaVersionId;
                }
                if (basisType == DispatchBasisType.物料清单 && (!basisId.HasValue || basisId <= 0))
                    throw new Exception("请选择物料清单版本");
                if (item.MaterialDemands?.Materials.Count > 0 && item.DispatchQuantity <= 0)
                    throw new Exception("派工数量必须大于零");
                //新增派工单
                var order = new Production_Dispatch_Order
                {
                    OrderNo = _sequenceService.GenerateCode(TableName.DispatchOrder),
                    DispatchUnit = item.DispatchUnit,
                    ProductId = item.ProductId,
                    SpecificationId = item.SpecificationId,
                    DepartmentId = item.DepartmentId,
                    DispatchDate = item.DispatchDate,
                    DispatchQuantity = item.DispatchQuantity,
                    BaseQuantity = item.BaseQuantity,
                    IncomeQuantity = item.IncomeQuantity,
                    Price = item.Price ?? 0,
                    Remark = item.OrderRemark,
                    Status = DispatchOrderStatus.进行中,
                    PlanCompleteDate = item.PlanCompleteDate,
                    MainQuantity = item.MainQuantity,
                    PackageId = item.PackageId,
                    BasisType = basisType,
                    BasisId = basisId,
                    FormulaVersionId = basisType != DispatchBasisType.物料清单 ? item.MaterialDemands?.FormulaVersionId : null,
                    FormulaUnitInfo = basisType != DispatchBasisType.物料清单 && item.MaterialDemands != null && item.MaterialDemands.FormulaUnitId.HasValue ? JsonSerializer.Serialize(new FormulaUnitInfo
                    {
                        FormulaQuantity = item.MaterialDemands.FormulaQuantity,
                        FormulaUnitId = item.MaterialDemands.FormulaUnitId,
                        FormulaConversionId = item.MaterialDemands.FormulaConversionId,
                        ConvRatio = item.MaterialDemands.ConvRatio,
                    }, JsonHelper.DefaultJsonSerializerOptions) : null,
                    CustomAttribute = item.ProductionAttributeList != null && item.ProductionAttributeList.Count > 0 ? JsonSerializer.Serialize(item.ProductionAttributeList, JsonHelper.DefaultJsonSerializerOptions) : null,
                };
                list.Add(order);

                //新增领料需求
                if (item.MaterialDemands != null && item.MaterialDemands.Materials.Any())
                {
                    var sortOrder = 1;
                    var materialList = item.MaterialDemands.Materials.Select(x => new Production_Material_Receive_Demand
                    {
                        Order = sortOrder++,
                        DataId = x.DataId,
                        DataType = x.DataType,
                        DispatchOrder = order,
                        DemandQuantity = x.DemandQuantity,
                        BaseDemandQuantity = x.BaseDemandQuantity,
                        UnitId = x.UnitId,
                        UnitConversionId = x.UnitConversionId > 0 ? x.UnitConversionId : null,
                        UnitQuantity = x.DemandQuantity / item.DispatchQuantity,
                        SupplierId = x.SupplierId,
                        DataStatus = x.DataStatus,
                        FormulaUnitInfo = basisType != DispatchBasisType.物料清单 && x.FormulaUnitId.HasValue ? JsonSerializer.Serialize(new FormulaUnitInfo
                        {
                            FormulaQuantity = x.FormulaQuantity,
                            FormulaUnitId = x.FormulaUnitId,
                            FormulaConversionId = x.FormulaConversionId,
                            ConvRatio = x.ConvRatio
                        }, JsonHelper.DefaultJsonSerializerOptions) : null,
                        Remark = x.DetailRemark,
                        ConvUnitInfo = JsonSerializer.Serialize(x.ConvUnitInfo, JsonHelper.DefaultJsonSerializerOptions),
                    }).ToList();
                    materialDemands = materialDemands.Concat(materialList).ToList();
                }

                //新增分配的销售订单
                if (item.SaleOrders != null && item.SaleOrders.Count > 0)
                {
                    var unitConversionIds = item.SaleOrders.SelectMany(x => x.DispatchUnitList).Select(x => x.Id).Where(x => x != -1).ToList();
                    var unitConversionData = _unitConversionService.GetAll(true).Where(x => unitConversionIds.Contains(x.Id)).ToList();
                    foreach (var sale in item.SaleOrders)
                    {
                        var mainUnit = sale.DispatchUnitList?.FirstOrDefault(x => x.Id == -1);
                        foreach (var unit in sale.DispatchUnitList!)
                        {
                            if (unit.Id == -1)
                            {
                                unit.ConversionType = null;
                            }
                            var unitConversion = unitConversionData.FirstOrDefault(x => x.Id == unit.Id);
                            if (unitConversion != null)
                            {
                                unit.ConversionType = unitConversion.ConversionType;
                            }
                        }
                        var saleRl = new Production_Dispatch_SaleRL
                        {
                            DispatchOrder = order,
                            CustomerId = sale.CustomerId,
                            SaleOrderDetailId = sale.Id,
                            DispatchQuantity = sale.DispatchQuantity,
                            DispatchUnitConversionId = sale.DispatchUnitConversionId,
                            Quantity = mainUnit?.DispatchQuantity ?? 0,
                            InboundQuantity = 0,
                            UnitConversionInfo = sale.DispatchUnitList != null && sale.DispatchUnitList.Count > 0 ? JsonSerializer.Serialize(sale.DispatchUnitList, JsonHelper.DefaultJsonSerializerOptions) : null,
                        };
                        _dispatchSaleRLService.Insert(saleRl);
                    }
                }
            }
            await _dispatchOrderService.InsertAsync(list);
            await _materialReceiveDemandService.InsertAsync(materialDemands);
            await _unitOfWork.SaveChangeAsync();
            return Ok("保存成功");
        }


        /// <summary>
        /// 修改派工单
        /// </summary>
        /// <param name="postData"></param>
        /// <returns></returns>
        [HttpPost("ModifyDispatchOrder")]
        public async Task<IActionResult> ModifyDispatchOrder([FromBody] PostDispatchOrderDto postData)
        {
            var order = await _dispatchOrderService.GetByIdAsync(postData.Id);
            if (order == null)
            {
                throw new Exception("派工单不存在，请刷新数据");
            }
            var dispatchUnit = JsonSerializer.Deserialize<UnitInfo>(order.DispatchUnit);

            //派工数量不可小于已报工数量
            var quantity = await _dispatchOperationRecordService.GetAll(true)
                .Where(x => x.OperateType == OperateType.接单 && x.DispatchOrderId == order.Id).SumAsync(x => x.OperateQuantity);
            if (quantity > postData.DispatchQuantity)
            {
                throw new Exception($"派工数量{postData.DispatchQuantity}不可小于已报工数量{quantity}");
            }

            if (quantity > 0 && postData.DepartmentId != order.DepartmentId)
            {
                throw new Exception("已存在报工记录不可修改派工部门");
            }

            //已存在报工记录不可修改单位和比例
            if (quantity > 0 && postData.DispatchUnitInfo.productionUnitId != dispatchUnit!.productionUnitId)
            {
                throw new Exception("已存在报工记录不可修改生产单位");
            }



            //修改生产和计价单位转换比例
            //var unitConversition = await _unitConversionService.GetAll().FirstOrDefaultAsync(x => x.TargetId == postData.ProcessPriceId &&
            //                                                                                 x.SourceId == postData.ProductId &&
            //                                                                                 x.ToUnitId == postData.DispatchUnitInfo.incomeUnitId &&
            //                                                                                 x.FromUnitId == postData.DispatchUnitInfo.productionUnitId);
            //if (unitConversition != null)
            //{
            //    unitConversition.Ratio = postData.DispatchUnitInfo.incomeRatio;
            //}

            ////新增生产单位和计价单位转换
            //if (unitConversition == null && postData.ProcessPriceId.HasValue)
            //{
            //    var entity = new Base_UnitConversion
            //    {
            //        TargetId = postData.ProcessPriceId.Value,
            //        ToUnitId = postData.DispatchUnitInfo.incomeUnitId,
            //        FromUnitId = postData.DispatchUnitInfo.productionUnitId,
            //        SourceId = postData.ProductId,
            //        ConversionType = UnitConversionType.DispatchToProcessPrice,
            //        Ratio = postData.DispatchUnitInfo.incomeRatio,
            //    };
            //    await _unitConversionService.InsertAsync(entity);
            //}

            //修改派工单
            order.DepartmentId = postData.DepartmentId;
            order.DispatchDate = postData.DispatchDate;
            order.DispatchQuantity = postData.DispatchQuantity;
            order.BaseQuantity = postData.BaseQuantity;
            order.IncomeQuantity = postData.IncomeQuantity;
            order.MainQuantity = postData.MainQuantity;
            order.Remark = postData.OrderRemark;
            order.DispatchUnit = postData.DispatchUnit;
            order.PlanCompleteDate = postData.PlanCompleteDate;
            order.PackageId = postData.PackageId;
            order.CustomAttribute = postData.ProductionAttributeList != null && postData.ProductionAttributeList.Count > 0 ? JsonSerializer.Serialize(postData.ProductionAttributeList, JsonHelper.DefaultJsonSerializerOptions) : null;

            await _unitOfWork.SaveChangeAsync();
            return Ok("保存成功");
        }


        /// <summary>
        /// 修改派工单状态
        /// </summary>
        /// <param name="Id"></param>
        /// <param name="status"></param>
        /// <returns></returns>
        [HttpGet("SetDispatchOrderStatus")]
        public async Task<IActionResult> SetDispatchOrderStatus([FromQuery] long id, DispatchOrderStatus? status)
        {
            var order = await _dispatchOrderService.GetByIdAsync(id);
            if (order == null)
            {
                throw new Exception("派工单不存在，请刷新数据");
            }
            if (status.HasValue)
            {
                order.Status = status.Value;
                order.IsDeleted = false;
            }
            if (!status.HasValue)
            {
                order.IsDeleted = true;
            }
            order.CompleteDate = status == DispatchOrderStatus.已完成 ? DateTime.Now : null;
            await _unitOfWork.SaveChangeAsync();
            return Ok("保存成功");
        }

        /// <summary>
        /// 删除派工单
        /// </summary>
        /// <param name="Id"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        [HttpGet("DeleteDispatchOrder")]
        public async Task<IActionResult> DeleteDispatchOrder([FromQuery] long Id)
        {
            var dispatch = await _dispatchOrderService.GetByIdAsync(Id);
            if (dispatch == null)
            {
                return Ok("删除成功");
            }
            if (dispatch.TotalInboundQuantity > 0)
            {
                throw new Exception("派工单已入库，请先删除入库记录");
            }
            var operations = await _dispatchOperationRecordService.GetAll(true).Where(x => x.DispatchOrderId == dispatch.Id).ToListAsync();
            if (operations.Any())
            {
                throw new Exception("派工单已生成报工记录，请先删除报工记录");
            }
            using var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);

            //物料需求
            var materials = await _materialReceiveDemandService.GetAll(true).Where(s => s.DispatchOrderId == dispatch.Id).ToListAsync();
            if (materials.Any(s => s.BaseReceiveQuantity > 0 || s.BaseReturnQuantity > 0))
            {
                throw new Exception("请先删除领料记录和退料记录");
            }
            //删除分配的销售订单
            var saleRlData = _dispatchSaleRLService.GetAll().Where(x => x.DispatchOrderId == dispatch.Id).ToList();
            var saleDetailIds = saleRlData.Select(x => x.SaleOrderDetailId).ToList();
            _dispatchSaleRLService.Delete(saleRlData);
            _materialReceiveDemandService.Delete(materials);
            _dispatchOrderService.Delete(dispatch, false);

            if (saleDetailIds.Count > 0)
            {
                var saleOrderDetailData = _saleOrderDetailService.GetAll().Where(x => saleDetailIds.Any(e => e == x.Id)).ToList();
                lock (QuantityLock.DiapatchCompleteLock)
                {
                    //销售明细的手动入库明细
                    foreach (var saleOrderDetail in saleOrderDetailData)
                    {
                        if (saleOrderDetail.InputProductionInfo != null)
                        {
                            var inputProductionInfo = JsonSerializer.Deserialize<List<InputProductionDto>>(saleOrderDetail.InputProductionInfo);
                            //已存在工单 更新数量
                            var exitsData = inputProductionInfo?.FirstOrDefault(x => x.ProductionId == dispatch.Id);
                            if (exitsData != null)
                            { //删除
                                inputProductionInfo?.Remove(exitsData);
                                saleOrderDetail.TotalInputProductionQty = inputProductionInfo != null ? inputProductionInfo.Where(x => x.IsOrderProduct).Sum(x => x.QuantityBySalesUnit) : null;
                                saleOrderDetail.InputProductionInfo = inputProductionInfo != null ? JsonSerializer.Serialize(inputProductionInfo, JsonHelper.DefaultJsonSerializerOptions) : null;
                            }
                        }
                    }
                }
            }
            await _unitOfWork.SaveChangeAsync();
            scope.Complete();

            return Ok("删除成功");
        }


        /// <summary>
        /// 更新派工单加工单价
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        [HttpPost("ModifyDispatchOrderPrice")]
        public async Task<IActionResult> ModifyDispatchOrderPrice([FromBody] PostModifyDispatchOrderPriceDto postData)
        {
            var order = await _dispatchOrderService.GetByIdAsync(postData.Id);
            if (order == null)
            {
                throw new Exception("派工单不存在，请刷新数据");
            }
            order.Price = postData.Price;

            //更新报工记录
            var ids = postData.Records.Select(x => x.Id).ToList();
            var operations = await _dispatchOperationRecordService.GetAll().Where(x => ids.Contains(x.Id)).ToListAsync();
            foreach (var operation in operations)
            {
                var info = postData.Records.FirstOrDefault(x => x.Id == operation.Id);
                if (info == null)
                {
                    continue;
                }
                if (operation.IncomeId.HasValue && operation.Price != info.Price)
                {
                    throw new Exception($"报工记录{operation.RecordNo}已结算为收入，不可直接修改");
                }
                operation.Price = info.Price;
            }
            await _unitOfWork.SaveChangeAsync();
            return Ok("保存成功");
        }

        [HttpGet("BulkModifyDispatchOrderUnit")]
        public async Task<IActionResult> BulkModifyDispatchOrderUnit()
        {
            var dispatchOrders = await _dispatchOrderService.GetAll(true).ToListAsync();
            var unitConversions = await _unitConversionService.GetAll(true).ToListAsync();
            foreach (var order in dispatchOrders)
            {
                var unitInfo = JsonSerializer.Deserialize<UnitInfo>(order.DispatchUnit);
                if (unitInfo == null)
                {
                    continue;
                }
                unitInfo.baseUnitConversionId = -1;
                var info = unitConversions.FirstOrDefault(s => s.ConversionType == UnitConversionType.SpecToProduct && s.SourceId == order.SpecificationId);
                unitInfo.mainUnitConversionId = info != null ? info.Id : -1;
                order.DispatchUnit = JsonSerializer.Serialize(unitInfo, JsonHelper.DefaultJsonSerializerOptions);
            }
            await _unitOfWork.SaveChangeAsync();
            return Ok("保存成功");
        }

        #region 领料需求

        /// <summary>
        /// 获取当前派工单的领料需求
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet("GetMaterialDemandByProduction")]
        public async Task<IActionResult> GetMaterialDemandByProduction([FromQuery] long id)
        {
            var materials = from detail in _materialReceiveDemandService.GetAll(true)
                            join info in _productService.GetProductList([], [])
                                 on new { type = detail.DataType, dataId = detail.DataId }
                                 equals new { type = info.SpecificationId.HasValue ? InventoryDataType.Specification : InventoryDataType.Product, dataId = info.SpecificationId.HasValue ? info.SpecificationId.Value : info.ProductId }
                            where detail.DispatchOrderId == id
                            select new GetMaterialDemandByProductionDto
                            {
                                Id = detail.Id,
                                Order = detail.Order,
                                ProductId = info.ProductId,
                                ProductNo = info.ProductNo,
                                ProductName = info.ProductName,
                                ProductTechnicalCode = info.ProductTechnicalCode,
                                ProductCategory = info.ProductCategory,
                                ProductUnit = info.ProductUnit,
                                IsDeleted = info.IsDeleted,
                                SpecificationId = info.SpecificationId,
                                ProductSpecification = info.ProductSpecification ?? info.MaterialSpecification,
                                ProductPackageWay = info.ProductPackageWay,
                                PackageWayId = info.PackageWayId,
                                Remark = info.Remark,
                                ProductCategoryId = info.ProductCategoryId,
                                ProductUnitId = info.ProductUnitId,
                                ReviewStatus = info.ReviewStatus,
                                ProductType = info.ProductType,
                                InventoryQuantity = info.InventoryQuantity,
                                UnPackageQuantity = info.UnPackageQuantity,
                                ProductMainUnitId = info.ProductMainUnitId,
                                ProductMainUnit = info.ProductMainUnit,
                                MaterialGrade = info.MaterialGrade,
                                MaterialSpecification = info.MaterialSpecification,
                                CustomAttribute = info.CustomAttribute,
                                OccupyInventory = info.OccupyInventory,
                                AbnormalQuantity = info.AbnormalQuantity,
                                DemandQuantity = detail.DemandQuantity,
                                BaseDemandQuantity = detail.BaseDemandQuantity,
                                IsDemand = detail.IsDeleted ? false : true,
                                BaseReceiveQuantity = detail.BaseReceiveQuantity,
                                BaseReturnQuantity = detail.BaseReturnQuantity,
                                UnitId = detail.UnitId,
                                UnitConversionId = detail.UnitConversionId.HasValue ? detail.UnitConversionId : -1,
                                FormulaUnitInfo = detail.FormulaUnitInfo,
                                DetailRemark = detail.Remark,
                                ConvUnitInfoStr = detail.ConvUnitInfo,
                                SupplierId = detail.SupplierId,
                                SupplierName = detail.Supplier != null ? detail.Supplier.CustomerName : string.Empty,
                                DataStatus = detail.DataStatus,
                                MaterialDemandType = detail.MaterialDemandType,
                            };

            var result = await materials.ApplySorts(this.SortInfoList).ToListAsync();
            return Ok(result);
        }

        /// <summary>
        /// 删除领料需求
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        [HttpGet("DeleteMaterialDemand")]
        public async Task<IActionResult> DeleteMaterialDemand([FromQuery] long id)
        {
            var material = await _materialReceiveDemandService.GetByIdAsync(id);
            if (material == null)
            {
                return Ok("删除成功");
            }
            if (material.BaseReceiveQuantity > 0 || material.BaseReturnQuantity > 0)
            {
                throw new Exception("请先删除领料记录和退料记录");
            }
            _materialReceiveDemandService.Delete(material, false);
            await _unitOfWork.SaveChangeAsync();
            return Ok("删除成功");
        }

        /// <summary>
        /// 切换物料需求状态
        /// </summary>
        /// <param name="id"></param>
        /// <param name="isDeleted"></param>
        /// <returns></returns>
        [HttpGet("ChangeDemandStatus")]
        public async Task<IActionResult> ChangeDemandStatus([FromQuery] long id, bool isDeleted)
        {
            var material = await _materialReceiveDemandService.GetByIdAsync(id);
            if (material == null)
            {
                throw new Exception("物料需求不存在，请刷新数据");
            }
            material.IsDeleted = isDeleted;
            await _unitOfWork.SaveChangeAsync();
            return Ok("保存成功");
        }

        public class PostChangeMaterialDemandDto
        {
            public long Id { get; set; }
            public decimal DemandQuantity { get; set; }
            public decimal BaseDemandQuantity { get; set; }
            public long UnitId { get; set; }
            public long? UnitConversionId { get; set; }
            public long? SupplierId { get; set; }
            public InventoryDataStatus DataStatus { get; set; }
            public string? Remark { get; set; }
            public List<InventoryUnitOutboundDto> ConvUnitInfo { get; set; } = new List<InventoryUnitOutboundDto>();
        }

        /// <summary>
        /// 修改物料需求数量和单位
        /// </summary>
        /// <param name="postData"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        [HttpPost("ChangeMaterialDemand")]
        public async Task<IActionResult> ChangeMaterialDemand([FromBody] PostChangeMaterialDemandDto postData)
        {
            var material = await _materialReceiveDemandService.GetByIdAsync(postData.Id);
            if (material == null)
            {
                throw new Exception("物料需求不存在，请刷新数据");
            }
            if (material.BaseReceiveQuantity > 0)
            {
                throw new Exception("当前物料需求已领用，不可修改");
            }
            material.DemandQuantity = postData.DemandQuantity;
            material.BaseDemandQuantity = postData.BaseDemandQuantity;
            material.UnitId = postData.UnitId;
            material.ConvUnitInfo = JsonSerializer.Serialize(postData.ConvUnitInfo, JsonHelper.DefaultJsonSerializerOptions);
            material.UnitConversionId = postData.UnitConversionId > 0 ? postData.UnitConversionId : null;
            material.Remark = postData.Remark;
            material.SupplierId = postData.SupplierId;
            material.DataStatus = postData.DataStatus;
            await _unitOfWork.SaveChangeAsync();
            return Ok("保存成功");
        }

        public class PostAddMaterialDemandDto
        {
            public long DispatchOrderId { get; set; }
            public long DataId { get; set; }
            public InventoryDataType DataType { get; set; }
            public decimal DemandQuantity { get; set; }
            public decimal BaseDemandQuantity { get; set; }
            public long UnitId { get; set; }
            public long? UnitConversionId { get; set; }
            public long? SupplierId { get; set; }
            public InventoryDataStatus DataStatus { get; set; }
            public string? DetailRemark { get; set; }
            public List<InventoryUnitOutboundDto> ConvUnitInfo { get; set; } = new List<InventoryUnitOutboundDto>();
            public FormulaUnitInfo? FormulaUnitInfo { get; set; }
            public MaterialDemandType MaterialDemandType { get; set; }
        }

        /// <summary>
        ///  生产补料需求
        ///  <param name="postData"></param>
        ///  <returns></returns>
        /// </summary>
        [HttpPost("AddMaterialDemand")]
        public async Task<IActionResult> AddMaterialDemand([FromBody] List<PostAddMaterialDemandDto> postData)
        {
            var sortOrder = (await _materialReceiveDemandService.GetAll(true)
                .Where(x => x.DispatchOrderId == postData.First().DispatchOrderId)
                .MaxAsync(o => (int?)o.Order) ?? 0) + 1;

            foreach (var item in postData)
            {
                var material = new Production_Material_Receive_Demand
                {
                    Order = sortOrder++,
                    DataId = item.DataId,
                    DataType = item.DataType,
                    DispatchOrderId = item.DispatchOrderId,
                    DemandQuantity = item.DemandQuantity,
                    BaseDemandQuantity = item.BaseDemandQuantity,
                    UnitId = item.UnitId,
                    UnitConversionId = item.UnitConversionId > 0 ? item.UnitConversionId : null,
                    SupplierId = item.SupplierId,
                    DataStatus = item.DataStatus,
                    FormulaUnitInfo = item.FormulaUnitInfo != null ? JsonSerializer.Serialize(item.FormulaUnitInfo, JsonHelper.DefaultJsonSerializerOptions) : null,
                    Remark = item.DetailRemark,
                    ConvUnitInfo = JsonSerializer.Serialize(item.ConvUnitInfo, JsonHelper.DefaultJsonSerializerOptions),
                    MaterialDemandType = item.MaterialDemandType
                };
                await _materialReceiveDemandService.InsertAsync(material);
            }
            await _unitOfWork.SaveChangeAsync();
            return Ok("新增成功");
        }
    }

    #endregion


    #region 编辑 分配的销售单

    #endregion

    #region Dto

    public class GetProductListByProductionFilters
    {
        public List<long> productIds { get; set; } = new List<long>();
        public List<Filter> filterList { get; set; } = new List<Filter>();
        public long? DepartmentId { get; set; } //某个部门的产品
    }
    public class GetDispatchDto
    {
        public List<Filter> filterList { get; set; } = new List<Filter>();
        public List<Filter> saleFilterList { get; set; } = new List<Filter>();
    }

    public class PostDispatchOrderDto
    {
        public DispatchBasisType BasisType { get; set; }
        public long? BasisId { get; set; }
        public long? Id { get; set; }
        public long ProductId { get; set; }
        public long? SpecificationId { get; set; }
        public long? PackageId { get; set; }
        public int DepartmentId { get; set; }
        public decimal? Price { get; set; }
        public string? OrderRemark { get; set; }
        public long? ProcessPriceId { get; set; }
        public required string DispatchUnit { get; set; }
        public decimal DispatchQuantity { get; set; }
        public decimal BaseQuantity { get; set; }
        public decimal IncomeQuantity { get; set; }
        public DateTime DispatchDate { get; set; }
        public DateTime? PlanCompleteDate { get; set; }
        public UnitInfo DispatchUnitInfo { get; set; } = null!;
        public decimal MainQuantity { get; set; }
        public PostMaterialDemands? MaterialDemands { get; set; }
        public List<Kingzon.OLCM.Base.Services.JsonStructure.CustomAttribute>? ProductionAttributeList { get; set; }
        public List<SaleOrderDetailDispatchDTO>? SaleOrders { get; set; }

    }

    public class PostMaterialDemands
    {
        public long? FormulaVersionId { get; set; }
        public long? FormulaUnitId { get; set; }
        public long? FormulaConversionId { get; set; }
        public decimal? FormulaQuantity { get; set; }
        public decimal? ConvRatio { get; set; }
        public List<PostMaterialDemandDetails> Materials { get; set; } = new List<PostMaterialDemandDetails>();
    }


        public class PostMaterialDemandDetails
        {
            public long DataId { get; set; }
            public long? SupplierId { get; set; }
            public InventoryDataType DataType { get; set; }
            public InventoryDataStatus DataStatus { get; set; }
            public decimal DemandQuantity { get; set; }
            public decimal BaseDemandQuantity { get; set; }
            public long UnitId { get; set; }
            public long? UnitConversionId { get; set; }
            public long? FormulaUnitId { get; set; }
            public long? FormulaConversionId { get; set; }
            public decimal? FormulaQuantity { get; set; }
            public decimal? ConvRatio { get; set; }
            public string? DetailRemark { get; set; }
            public List<InventoryUnitOutboundDto> ConvUnitInfo { get; set; } = new List<InventoryUnitOutboundDto>();
        }




    public class GetProductListByProductionDto : ProductDto
    {
        public long Id { get; set; }
        public List<GetProductionSumInfoDto> WaitProductionList { get; set; } = new List<GetProductionSumInfoDto>();
        public List<CustomAttribute> CustomAttributeList
        {
            get
            {
                var list = string.IsNullOrEmpty(CustomAttribute) ? new List<CustomAttribute>() : JsonSerializer.Deserialize<List<CustomAttribute>>(CustomAttribute);
                return list ?? new List<CustomAttribute>();
            }
        }
    }


    public class GetProductionSumInfoDto
    {
        public long? ProductId { get; set; }
        public long? ProductSpecificationId { get; set; }
        public long? UnitId { get; set; }
        public string? UnitName { get; set; }
        public decimal? Quantity { get; set; }
    }


    public class PostDepartmentsByPrcessPriceDto
    {
        public long ProductId { get; set; }
        public long? SpecificationId { get; set; }
    }


    public class GetDepartmentsByPrcessPriceDto
    {
        public long Id { get; set; }
        public long ProductId { get; set; }
        public long? SpecificationId { get; set; }
        public decimal Price { get; set; }
        public long? UnitId { get; set; }
        public string? UnitName { get; set; }
        public required string Text { get; set; }
        public long Value { get; set; }
        public long? PriceUnitConversionId { get; set; }
    }

    public class PostModifyPriceDetail
    {
        public long Id { get; set; }
        public decimal Price { get; set; }
    }

    public class PostModifyDispatchOrderPriceDto
    {
        public long Id { get; set; }
        public decimal Price { get; set; }
        public List<PostModifyPriceDetail> Records { get; set; } = new List<PostModifyPriceDetail>();
    }



    public class GetMaterialDemandByProductionDto : GetProductListByProductionDto
    {
        public decimal DemandQuantity { get; set; }
        public decimal BaseDemandQuantity { get; set; }
        public decimal BaseReceiveQuantity { get; set; }
        public decimal BaseReturnQuantity { get; set; }
        public decimal ReceiveQuantity
        {
            get
            {
                var baseRatioInfo = ConvUnitInfo.FirstOrDefault(s => s.Id == UnitConversionId);
                var baseRatio = UnitConversionId > 0 ? 0m : 1m;
                baseRatio = baseRatioInfo != null ? baseRatioInfo.Ratio : baseRatio;
                return baseRatio > 0 ? Math.Round(BaseReceiveQuantity / baseRatio, 6, MidpointRounding.AwayFromZero) : 0;
            }
        }
        public decimal ReturnQuantity
        {
            get
            {
                var baseRatioInfo = ConvUnitInfo.FirstOrDefault(s => s.Id == UnitConversionId);
                var baseRatio = UnitConversionId > 0 ? 0m : 1m;
                baseRatio = baseRatioInfo != null ? baseRatioInfo.Ratio : baseRatio;
                return baseRatio > 0 ? Math.Round(BaseReturnQuantity / baseRatio, 6, MidpointRounding.AwayFromZero) : 0;
            }
        }
        public long UnitId { get; set; }
        public long? UnitConversionId { get; set; }
        public bool IsDemand { get; set; }
        public string? FormulaUnitInfo { get; set; }
        public string? DetailRemark { get; set; }
        public long? SupplierId { get; set; }
        public string? SupplierName { get; set; }
        public InventoryDataStatus DataStatus { get; set; }
        public required string ConvUnitInfoStr { get; set; }
        public List<InventoryUnitDto> ConvUnitInfo
        {
            get
            {
                try
                {
                    return JsonSerializer.Deserialize<List<InventoryUnitDto>>(ConvUnitInfoStr ?? "[]") ?? new List<InventoryUnitDto>();
                }
                catch
                {
                    return new List<InventoryUnitDto>();
                }
            }
        }
        public int Order { get; set; }
        public MaterialDemandType MaterialDemandType { get; set; }
    }
    #endregion
}

