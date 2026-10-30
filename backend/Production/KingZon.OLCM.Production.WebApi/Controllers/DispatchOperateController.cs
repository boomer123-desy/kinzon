using Kingzon.OLCM.Base.Services;
using KingZon.Core.DataBase;
using KingZon.Core.Web;
using KingZon.OLCM.Amoeba.DataBase.Entities;
using KingZon.OLCM.Base.DataBase.Enum;
using KingZon.OLCM.Base.Service;
using KingZon.OLCM.Base.Service.Lock;
using KingZon.OLCM.Base.Shared.DataBase.Entities;
using KingZon.OLCM.Base.Shared.DataBase.Enum;
using KingZon.OLCM.Production.Shared.DataBase.Entities;
using KingZon.OLCM.Production.Shared.DataBase.Enum;
using KingZon.OLCM.Production.Shared.DataBase.JsonStructure;
using KingZon.OLCM.Production.WebApi.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using System.Text.Json;
using System.Transactions;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace KingZon.OLCM.Production.WebApi.Controllers
{

    [Route("api/production/[controller]")]
    [ApiController]
    public class DispatchOperateController : KingZonControllerBase
    {
        private readonly ProductService _productService;
        private readonly CommonService<Base_ProductSpecification> _specificationService;
        private readonly CommonService<Base_UnitConversion> _unitConversionService;
        private readonly CommonService<Base_Unit> _unitService;
        private readonly CommonService<Production_Dispatch_Order> _dispatchOrderService;
        private readonly CommonService<Production_Dispatch_OperationRecord> _dispatchOperationRecordService;
        private readonly CommonService<Amoeba_ProductProcessPrice> _productProcessPriceService;
        private readonly CommonService<Production_Dispatch_OperationRecordRL> _dispatchOperationRecordRLService;
        private readonly SequenceService _sequenceService;
        private readonly CommonService<Base_Department> _departmentService;
        private readonly IUnitOfWork _unitOfWork;

        public DispatchOperateController(
            ProductService productService,
            CommonService<Base_ProductSpecification> specificationService,
            CommonService<Base_UnitConversion> unitConversionService,
            CommonService<Base_Unit> unitService,
            CommonService<Production_Dispatch_Order> dispatchOrderService,
            CommonService<Production_Dispatch_OperationRecord> dispatchOperationRecordService,
            CommonService<Amoeba_ProductProcessPrice> productProcessPriceService,
            CommonService<Production_Dispatch_OperationRecordRL> dispatchOperationRecordRLService,
            SequenceService sequenceService,
            CommonService<Base_Department> departmentService,
            IUnitOfWork unitOfWork)
        {
            _productService = productService;
            _specificationService = specificationService;
            _unitConversionService = unitConversionService;
            _unitService = unitService;
            _dispatchOrderService = dispatchOrderService;
            _dispatchOperationRecordService = dispatchOperationRecordService;
            _productProcessPriceService = productProcessPriceService;
            _dispatchOperationRecordRLService = dispatchOperationRecordRLService;
            _sequenceService = sequenceService;
            _departmentService = departmentService;
            _unitOfWork = unitOfWork;
        }

        /// <summary>
        /// 获取所有待出接单的记录
        /// </summary>
        /// <param name="filters"></param>
        /// <returns></returns>
        [HttpPost("GetWaitOperateDispatchList")]
        public async Task<IActionResult> GetWaitOperateDispatchList([FromBody] List<Filter> filterList)
        {
            //var userId = this.CurrentUser.Id;
            var userId = 124;

            var options = from op in _dispatchOperationRecordService.GetAll()
                              //where op.OperateType == OperateType.接单
                          group op by op.DispatchOrderId
                          into g
                          select new GetReceiveOperationSumDto
                          {
                              DispatchOrderId = g.Key,
                              //已接单
                              ReceiveQuantity = g.Where(x => x.OperateType == OperateType.接单).Sum(x => x.OperateQuantity),
                              ReceiveUserQuantity = g.Where(x => x.OperateType == OperateType.接单 && x.OperateUserId == userId).Sum(x => x.OperateQuantity),
                              MainReceiveQuantity = g.Where(x => x.OperateType == OperateType.接单).Sum(x => x.MainQuantity),
                              MainReceiveUserQuantity = g.Where(x => x.OperateType == OperateType.接单 && x.OperateUserId == userId).Sum(x => x.MainQuantity),
                              BaseReceiveQuantity = g.Where(x => x.OperateType == OperateType.接单).Sum(x => x.BaseQuantity),
                              BaseReceiveUserQuantity = g.Where(x => x.OperateType == OperateType.接单 && x.OperateUserId == userId).Sum(x => x.BaseQuantity),
                              IncomeReceiveQuantity = g.Where(x => x.OperateType == OperateType.接单).Sum(x => x.IncomeQuantity),
                              IncomeReceiveUserQuantity = g.Where(x => x.OperateType == OperateType.接单 && x.OperateUserId == userId).Sum(x => x.IncomeQuantity),

                              //已出单
                              CompleteQuantity = g.Where(x => x.OperateType == OperateType.出单).Sum(x => x.OperateQuantity),
                              CompleteUserQuantity = g.Where(x => x.OperateType == OperateType.出单 && x.OperateUserId == userId).Sum(x => x.OperateQuantity),
                              MainCompleteQuantity = g.Where(x => x.OperateType == OperateType.出单).Sum(x => x.MainQuantity),
                              MainCompleteUserQuantity = g.Where(x => x.OperateType == OperateType.出单 && x.OperateUserId == userId).Sum(x => x.MainQuantity),
                              BaseCompleteQuantity = g.Where(x => x.OperateType == OperateType.出单).Sum(x => x.BaseQuantity),
                              BaseCompleteUserQuantity = g.Where(x => x.OperateType == OperateType.出单 && x.OperateUserId == userId).Sum(x => x.BaseQuantity),
                              IncomeCompleteQuantity = g.Where(x => x.OperateType == OperateType.出单).Sum(x => x.IncomeQuantity),
                              IncomeCompleteUserQuantity = g.Where(x => x.OperateType == OperateType.出单 && x.OperateUserId == userId).Sum(x => x.IncomeQuantity),
                          };

            var query = from order in _dispatchOrderService.GetAll()
                        join product in _productService.GetAll(true)
                             on order.ProductId equals product.Id
                        join op in options
                             on order.Id equals op.DispatchOrderId
                             into temp
                        from op in temp.DefaultIfEmpty()
                        where order.Status == DispatchOrderStatus.进行中
                        select new GetWaitDispatchOrderDto
                        {
                            Id = order.Id,
                            OrderNo = order.OrderNo,
                            ProductId = product.Id,
                            SpecificationId = order.SpecificationId,
                            PackageId = order.PackageId,
                            PackageSpecification = order.ProductPackage != null ? order.ProductPackage.PackageSpecification : string.Empty,
                            TechnicalCode = product.TechnicalCode,
                            ProductNo = order.ProductSpecification != null ? order.ProductSpecification.Code ?? "" : product.ProductNo,
                            ProductName = order.ProductSpecification != null ? order.ProductSpecification.Name ?? "" : product.ProductName,
                            Specification = order.ProductSpecification != null ? order.ProductSpecification.Specification : string.Empty,
                            ProductCategory = order.ProductSpecification != null ? order.ProductSpecification!.ProductCategory != null ?
                                                    order.ProductSpecification!.ProductCategory!.Name : string.Empty
                                                        : product.ProductCategory.Name,
                            ProductCategoryId = order.ProductSpecification != null ? order.ProductSpecification!.ProductCategoryId ?? 0 : product.ProductCategoryId,
                            PackageWay = order.ProductSpecification != null && order.ProductSpecification.ContainerModel != null ? order.ProductSpecification.ContainerModel!.DataValue : string.Empty,
                            ProductRemark = order.ProductSpecification != null ? order.ProductSpecification!.Remark : product.Remark,
                            DepartmentId = order.DepartmentId,
                            DepartmentName = order.Department.Name,
                            Price = order.Price,
                            Remark = order.Remark,
                            //生产单位
                            DispatchQuantity = order.DispatchQuantity,
                            CompleteQuantity = order.CompleteQuantity,
                            ReceiveQuantity = op != null ? op.ReceiveQuantity ?? 0 : 0,
                            ReceiveUserQuantity = op != null ? op.ReceiveUserQuantity ?? 0 : 0,
                            CompleteUserQuantity = op != null ? op.CompleteUserQuantity ?? 0 : 0,
                            DispatchUnit = order.DispatchUnit,
                            //主计量
                            MainQuantity = order.MainQuantity,
                            MainCompleteQuantity = order.MainCompleteQuantity,
                            MainReceiveQuantity = op != null ? op.MainReceiveQuantity ?? 0 : 0,
                            MainReceiveUserQuantity = op != null ? op.MainReceiveUserQuantity ?? 0 : 0,
                            MainCompleteUserQuantity = op != null ? op.MainCompleteUserQuantity ?? 0 : 0,
                            //产品单位
                            BaseQuantity = order.BaseQuantity,
                            BaseCompleteQuantity = order.BaseCompleteQuantity,
                            BaseReceiveQuantity = op != null ? op.BaseReceiveQuantity ?? 0 : 0,
                            BaseReceiveUserQuantity = op != null ? op.BaseReceiveUserQuantity ?? 0 : 0,
                            BaseCompleteUserQuantity = op != null ? op.BaseCompleteUserQuantity ?? 0 : 0,
                            //计价单位
                            IncomeQuantity = order.IncomeQuantity,
                            IncomeCompleteQuantity = order.IncomeCompleteQuantity,
                            IncomeReceiveQuantity = op != null ? op.IncomeReceiveQuantity ?? 0 : 0,
                            IncomeReceiveUserQuantity = op != null ? op.IncomeReceiveUserQuantity ?? 0 : 0,
                            IncomeCompleteUserQuantity = op != null ? op.IncomeCompleteUserQuantity ?? 0 : 0,
                            Status = order.Status,
                            IsDeleted = order.IsDeleted,
                            DispatchDate = order.DispatchDate,
                            ProductType = product.ProductType,
                            CreateTime = order.CreateTime,
                            CreateUserName = order.CreateUserName,
                            PlanCompleteDate = order.PlanCompleteDate,
                            CompleteDate = order.CompleteDate,
                            IsStart = order.DispatchDate.Date <= DateTime.Now.Date ? true : false,
                        };
            var result = await query.ApplyFilters(filterList).ApplySorts(this.SortInfoList).ToPagedListAsync(this.PageModel);
            return Ok(result);
        }

        /// <summary>
        /// 创建报工记录
        /// </summary>
        /// <param name="postData"></param>
        /// <returns></returns>
        [HttpPost("AddOperation")]
        public async Task<IActionResult> AddOperation([FromBody] PostOperationDto postData)
        {
            using (var transaction = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                var order = await _dispatchOrderService.GetByIdAsync(postData.DispatchOrderId);
                if (order == null)
                {
                    throw new Exception("派工单不存在，请刷新数据");
                }
                //当前可出单数量
                //var canDeliveryQuantity = await _dispatchOperationRecordService.GetAll()
                //    .Where(x => x.DispatchOrderId == postData.DispatchOrderId && x.OperateType == OperateType.接单)
                //    .SumAsync(x => x.OperateQuantity - x.CompleteQuantity);
                //var canReceiveQuantity = order.DispatchQuantity - await _dispatchOperationRecordService.GetAll()
                //    .Where(x => x.DispatchOrderId == postData.DispatchOrderId && x.OperateType == OperateType.接单)
                //    .SumAsync(x => x.OperateQuantity);

                //if(postData.OperateType == OperateType.接单 && postData.Quantity>canReceiveQuantity)
                //{
                //    throw new Exception($"当前派工单最大可接单数量为{canReceiveQuantity},请刷新页面");
                //}
                //if (postData.OperateType == OperateType.出单 && postData.Quantity > canDeliveryQuantity)
                //{
                //    throw new Exception($"当前派工单最大可出单数量为{canDeliveryQuantity},请刷新页面");
                //}
                var dispatchUnit = JsonSerializer.Deserialize<UnitInfo>(order.DispatchUnit);
                var operation = new Production_Dispatch_OperationRecord
                {
                    RecordNo = _sequenceService.GenerateCode(TableName.DispatchOperation),
                    DispatchOrderId = postData.DispatchOrderId,
                    DepartmentId = order.DepartmentId,
                    OperateType = postData.OperateType,
                    Price = postData.OperateType == OperateType.接单 ? 0 : postData.Price,
                    Remark = postData.Remark,
                    OperateUserId = (int)postData.OperateUserId,
                    OperateQuantity = postData.Quantity,
                    IncomeQuantity = postData.IncomeQuantity,
                    BaseQuantity = dispatchUnit != null && dispatchUnit.baseUnitConversionId == dispatchUnit.unitConversionId ? postData.Quantity :
                                    dispatchUnit != null && dispatchUnit.baseUnitConversionId == dispatchUnit.incomeUnitConversionId ? postData.IncomeQuantity :
                                        postData.Quantity * dispatchUnit!.baseRatio,
                    MainQuantity = dispatchUnit != null && dispatchUnit.mainUnitConversionId == dispatchUnit.unitConversionId ? postData.Quantity :
                                    dispatchUnit != null && dispatchUnit.mainUnitConversionId == dispatchUnit.incomeUnitConversionId ? postData.IncomeQuantity :
                                        postData.Quantity * dispatchUnit!.mainRatio,

                };
                await _dispatchOperationRecordService.InsertAsync(operation);
                await _unitOfWork.SaveChangeAsync();

                lock (QuantityLock.DiapatchCompleteLock)
                {
                    if (postData.OperateType == OperateType.出单)
                    {
                        Production_Dispatch_OperationRecord? lastOp = _dispatchOperationRecordService.GetAll()
                                                                                                        .Where(x => x.DispatchOrderId == order.Id &&
                                                                                                                    x.OperateType == OperateType.接单 &&
                                                                                                                    x.OperateUserId == postData.OperateUserId)
                                                                                                        .OrderByDescending(x => x.CreateTime)
                                                                                                        .FirstOrDefault();
                        if (lastOp == null)
                        {
                            throw new Exception("当前用户暂未接单，请先操作接单");
                        }
                        var rls = new List<Production_Dispatch_OperationRecordRL>();
                        //抵扣接单记录
                        var receiveOps = _dispatchOperationRecordService.GetAll()
                                                                            .Where(x => x.DispatchOrderId == order.Id &&
                                                                                        x.OperateType == OperateType.接单 &&
                                                                                        x.OperateQuantity > x.CompleteQuantity &&
                                                                                        x.OperateUserId == postData.OperateUserId)
                                                                            .OrderBy(x => x.CreateTime).ToList();
                        //存在未出单完的接单记录
                        if (receiveOps.Any())
                        {
                            var lastInfo = receiveOps.Last();
                            var totalQuantity = postData.Quantity;
                            var totalIncomeQuantity = postData.IncomeQuantity;
                            foreach (var item in receiveOps)
                            {
                                if (totalQuantity <= 0)
                                {
                                    break;
                                }
                                var isLast = lastInfo.Id == item.Id;
                                var leaveQuantity = item.OperateQuantity - item.CompleteQuantity;
                                var leaveIncomeQuantity = item.IncomeQuantity - item.IncomeCompleteQuantity;
                                if (leaveQuantity <= 0 && !isLast)
                                {
                                    continue;
                                }
                                //可出单数量<剩余报工数,不是最后一条记录
                                if (leaveQuantity < totalQuantity)
                                {
                                    var setQuanityt = isLast ? totalQuantity : leaveQuantity;
                                    var setIncomeQuanityt = isLast ? totalIncomeQuantity : leaveIncomeQuantity;
                                    var rl = new Production_Dispatch_OperationRecordRL
                                    {
                                        SourceOperation = item,
                                        TargetOperation = operation,
                                        OperateQuantity = setQuanityt,
                                        BaseQuantity = dispatchUnit != null && dispatchUnit.baseUnitConversionId == dispatchUnit.unitConversionId ? setQuanityt :
                                                             dispatchUnit != null && dispatchUnit.baseUnitConversionId == dispatchUnit.incomeUnitConversionId ? setIncomeQuanityt :
                                                                    setQuanityt * dispatchUnit!.baseRatio,
                                        MainQuantity = dispatchUnit != null && dispatchUnit.baseUnitConversionId == dispatchUnit.unitConversionId ? setQuanityt :
                                                             dispatchUnit != null && dispatchUnit.baseUnitConversionId == dispatchUnit.incomeUnitConversionId ? setIncomeQuanityt :
                                                                    setQuanityt * dispatchUnit!.mainRatio,
                                        IncomeQuantity = setIncomeQuanityt,
                                    };
                                    rls.Add(rl);
                                    totalQuantity = totalQuantity - setQuanityt;
                                    totalIncomeQuantity = totalIncomeQuantity - setIncomeQuanityt;
                                    item.CompleteQuantity += setQuanityt;
                                    item.IncomeCompleteQuantity += setIncomeQuanityt;
                                    continue;
                                }
                                //可出单数量>=剩余报工数
                                if (leaveQuantity >= totalQuantity)
                                {
                                    var rl = new Production_Dispatch_OperationRecordRL
                                    {
                                        SourceOperation = item,
                                        TargetOperation = operation,
                                        OperateQuantity = totalQuantity,
                                        BaseQuantity = dispatchUnit != null && dispatchUnit.baseUnitConversionId == dispatchUnit.unitConversionId ? totalQuantity :
                                                             dispatchUnit != null && dispatchUnit.baseUnitConversionId == dispatchUnit.incomeUnitConversionId ? totalIncomeQuantity :
                                                                    totalQuantity * dispatchUnit!.baseRatio,
                                        MainQuantity = dispatchUnit != null && dispatchUnit.baseUnitConversionId == dispatchUnit.unitConversionId ? totalQuantity :
                                                             dispatchUnit != null && dispatchUnit.baseUnitConversionId == dispatchUnit.incomeUnitConversionId ? totalIncomeQuantity :
                                                                    totalQuantity * dispatchUnit!.mainRatio,
                                        IncomeQuantity = totalIncomeQuantity,
                                    };
                                    rls.Add(rl);
                                    item.CompleteQuantity += totalQuantity;
                                    item.IncomeCompleteQuantity += totalIncomeQuantity;
                                    break;
                                }
                            }
                        }
                        //不存在，将超出数量记录在最后一条接单记录上
                        if (!receiveOps.Any() && lastOp != null)
                        {
                            var info = new Production_Dispatch_OperationRecordRL
                            {
                                SourceOperation = lastOp,
                                TargetOperation = operation,
                                OperateQuantity = postData.Quantity,
                                BaseQuantity = dispatchUnit != null && dispatchUnit.baseUnitConversionId == dispatchUnit.unitConversionId ? postData.Quantity :
                                                             dispatchUnit != null && dispatchUnit.baseUnitConversionId == dispatchUnit.incomeUnitConversionId ? postData.IncomeQuantity :
                                                                    postData.Quantity * dispatchUnit!.baseRatio,
                                MainQuantity = dispatchUnit != null && dispatchUnit.baseUnitConversionId == dispatchUnit.unitConversionId ? postData.Quantity :
                                                             dispatchUnit != null && dispatchUnit.baseUnitConversionId == dispatchUnit.incomeUnitConversionId ? postData.IncomeQuantity :
                                                                    postData.Quantity * dispatchUnit!.mainRatio,
                                IncomeQuantity = postData.IncomeQuantity,
                            };
                            rls.Add(info);
                            lastOp.CompleteQuantity += postData.Quantity;
                            lastOp.IncomeCompleteQuantity += postData.IncomeQuantity;
                        }
                        if (!receiveOps.Any() && lastOp == null)
                        {
                            throw new Exception("请先操作接单");
                        }

                        //新增出接单关联记录
                        _dispatchOperationRecordRLService.Insert(rls);
                        _unitOfWork.SaveChange();
                        //更新接单记录已完工数量
                        var ids = rls.Select(x => x.SourceOperation.Id).ToList();
                        var oprls = _dispatchOperationRecordRLService.GetAll().Where(x => ids.Contains(x.SourceOperationId)).ToList();
                        foreach (var op in receiveOps.Where(x => ids.Contains(x.Id)).ToList())
                        {
                            op.CompleteQuantity = oprls.Where(x => x.SourceOperationId == op.Id).Sum(x => x.OperateQuantity);
                            op.BaseCompleteQuantity = oprls.Where(x => x.SourceOperationId == op.Id).Sum(x => x.BaseQuantity);
                            op.MainCompleteQuantity = oprls.Where(x => x.SourceOperationId == op.Id).Sum(x => x.MainQuantity);
                            op.IncomeCompleteQuantity = oprls.Where(x => x.SourceOperationId == op.Id).Sum(x => x.IncomeQuantity);
                        }
                        _unitOfWork.SaveChange();
                        //更新派工单完工数量
                        var totalOps = _dispatchOperationRecordService.GetAll().Where(x => x.DispatchOrderId == order.Id && x.OperateType == OperateType.出单).ToList();
                        order.CompleteQuantity = totalOps.Sum(x => x.OperateQuantity);
                        order.MainCompleteQuantity = totalOps.Sum(x => x.MainQuantity);
                        order.BaseCompleteQuantity = totalOps.Sum(x => x.BaseQuantity);
                        order.IncomeCompleteQuantity = totalOps.Sum(x => x.IncomeQuantity);
                        //order.Status = order.DispatchQuantity == order.CompleteQuantity ? DispatchOrderStatus.已完成 : order.Status;
                        _unitOfWork.SaveChange();
                    }
                    transaction.Complete();
                }
                return Ok("保存成功");
            }
        }

        #region Dto 



        public class PostOperationDto
        {
            public decimal Quantity { get; set; }
            public decimal IncomeQuantity { get; set; }
            public decimal Price { get; set; }
            public long OperateUserId { get; set; }
            public long DispatchOrderId { get; set; }
            public string? Remark { get; set; }
            public OperateType OperateType { get; set; }
        }



        #endregion
    }
}
