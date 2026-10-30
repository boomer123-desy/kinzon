using Kingzon.OLCM.Base.Services;
using KingZon.Core.DataBase;
using KingZon.Core.Web;
using KingZon.OLCM.Amoeba.DataBase.Entities;
using KingZon.OLCM.Base.DataBase.Enum;
using KingZon.OLCM.Base.Service;
using KingZon.OLCM.Base.Service.Lock;
using KingZon.OLCM.Base.Shared.DataBase.Entities;
using KingZon.OLCM.Production.Shared.DataBase.Entities;
using KingZon.OLCM.Production.Shared.DataBase.JsonStructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Transactions;

namespace KingZon.OLCM.Production.WebApi.Controllers
{

    [Route("api/production/[controller]")]
    [ApiController]
    public class OperationRecordManageController : KingZonControllerBase
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
        private readonly IUnitOfWork _unitOfWork;

        public OperationRecordManageController(
            ProductService productService,
            CommonService<Base_ProductSpecification> specificationService,
            CommonService<Base_UnitConversion> unitConversionService,
            CommonService<Base_Unit> unitService,
            CommonService<Production_Dispatch_Order> dispatchOrderService,
            CommonService<Production_Dispatch_OperationRecord> dispatchOperationRecordService,
            CommonService<Amoeba_ProductProcessPrice> productProcessPriceService,
            CommonService<Production_Dispatch_OperationRecordRL> dispatchOperationRecordRLService,
            SequenceService sequenceService,
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
            _unitOfWork = unitOfWork;
        }

        /// <summary>
        /// 获取所有报工记录
        /// </summary>
        /// <param name="postData"></param>
        /// <returns></returns>
        [HttpPost("GetOperationRecordList")]
        public async Task<IActionResult> GetOperationRecordList([FromBody] GetOperationRecordListFilter postData)
        {
            var query = from record in _dispatchOperationRecordService.GetAll(true)
                        join product in _productService.GetAll(true)
                             on record.DispatchOrder.ProductId equals product.Id
                        select new GetDispatchOrderOperationDto
                        {
                            Id = record.Id,
                            DispatchOrderId = record.DispatchOrderId,
                            OrderNo = record.DispatchOrder.OrderNo,
                            RecordNo = record.RecordNo,
                            DepartmentId = record.DepartmentId,
                            DepartmentName = record.Department.Name,
                            OperateUserId = record.OperateUserId,
                            OperateUserName = record.OperateUser.Name,
                            OperateQuantity = record.OperateQuantity,
                            CompleteQuantity = record.CompleteQuantity,
                            BaseQuantity = record.BaseQuantity,
                            BaseCompleteQuantity = record.BaseCompleteQuantity,
                            IncomeQuantity = record.IncomeQuantity,
                            IncomeCompleteQuantity = record.IncomeCompleteQuantity,
                            MainQuantity = record.MainQuantity,
                            MainCompleteQuantity = record.MainCompleteQuantity,
                            Price = record.Price,
                            TotalPrice = record.TotalPrice,
                            Remark = record.Remark,
                            OperateType = record.OperateType,
                            CreateTime = record.CreateTime,
                            CreateUserName = record.CreateUserName,
                            ProductId = product.Id,
                            SpecificationId = record.DispatchOrder.SpecificationId,
                            TechnicalCode = product.TechnicalCode,
                            ProductNo = record.DispatchOrder.ProductSpecification != null ? record.DispatchOrder.ProductSpecification.Code ?? "" : product.ProductNo,
                            ProductName = record.DispatchOrder.ProductSpecification != null ? record.DispatchOrder.ProductSpecification.Name ?? "" : product.ProductName,
                            Specification = record.DispatchOrder.ProductSpecification != null ? record.DispatchOrder.ProductSpecification.Specification : string.Empty,
                            ProductCategory = record.DispatchOrder.ProductSpecification != null ? record.DispatchOrder.ProductSpecification!.ProductCategory != null ?
                                                    record.DispatchOrder.ProductSpecification!.ProductCategory!.Name : string.Empty
                                                        : product.ProductCategory.Name,
                            ProductCategoryId = record.DispatchOrder.ProductSpecification != null ? record.DispatchOrder.ProductSpecification!.ProductCategoryId ?? 0 : product.ProductCategoryId,
                            PackageWay = record.DispatchOrder.ProductSpecification != null && record.DispatchOrder.ProductSpecification.ContainerModel != null ? record.DispatchOrder.ProductSpecification.ContainerModel.DataValue : string.Empty,
                            DispatchUnit = record.DispatchOrder.DispatchUnit,
                            PackageId = record.DispatchOrder.PackageId,
                            PackageSpecification = record.DispatchOrder.ProductPackage != null ? record.DispatchOrder.ProductPackage.PackageSpecification : string.Empty,
                            IncomeId = record.IncomeId ?? 0,
                            IsDeleted = record.DispatchOrder.IsDeleted,
                        };

            if (postData.Departments.Any())
            {
                query = query.Where(x => postData.Departments.Contains(x.DepartmentId));
            }

            if (postData.IsPage)
            {
                var result = await query.ApplyFilters(postData.FilterList).ApplySorts(this.SortInfoList).ToPagedListAsync(this.PageModel);
                return Ok(result);
            }
            var data = await query.ApplyFilters(postData.FilterList).ApplySorts(this.SortInfoList).ToListAsync();
            return Ok(data);
        }

        /// <summary>
        /// 删除报工记录
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet("DeleteDispatchOrderOpration")]
        public async Task<IActionResult> DeleteDispatchOrderOpration([FromQuery] long id)
        {
            using (var transaction = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                var operation = await _dispatchOperationRecordService.GetByIdAsync(id);
                if (operation == null)
                {
                    return Ok("删除成功");
                }

                if (operation.IncomeId.HasValue)
                {
                    throw new Exception("当前报工记录已结算为收入，请先删除对应收入");
                }

                var order = await _dispatchOrderService.GetByIdAsync(operation.DispatchOrderId);
                if (order == null)
                {
                    return Ok("删除成功");
                }

                var rls = await _dispatchOperationRecordRLService.GetAll().Include(x => x.TargetOperation).Where(x => x.SourceOperationId == operation.Id).ToListAsync();
                if (rls.Any() && operation.OperateType == OperateType.接单)
                {
                    throw new Exception($"当前记录已生成后续出单记录，请先删除出单记录{string.Join(",", rls.Select(x => x.TargetOperation.RecordNo))}");
                }
                //更新完工数量
                lock (QuantityLock.DiapatchCompleteLock)
                {
                    if (operation.OperateType == OperateType.出单)
                    {
                        //删除出接单关联
                        var useRLs = _dispatchOperationRecordRLService.GetAll().Include(x => x.SourceOperation).Where(x => x.TargetOperationId == operation.Id).ToList();
                        _dispatchOperationRecordRLService.Delete(useRLs);
                        _unitOfWork.SaveChange();
                        var ids = useRLs.Select(x => x.SourceOperationId).ToList();
                        var opRLs = _dispatchOperationRecordRLService.GetAll().Where(x => ids.Contains(x.SourceOperationId)).ToList();
                        //更新接单记录的已完工数量
                        foreach (var op in useRLs.Select(x => x.SourceOperation).ToList())
                        {
                            op.CompleteQuantity = opRLs.Where(x => x.SourceOperationId == op.Id).Sum(x => x.OperateQuantity);
                            op.MainCompleteQuantity = opRLs.Where(x => x.SourceOperationId == op.Id).Sum(x => x.MainQuantity);
                            op.BaseCompleteQuantity = opRLs.Where(x => x.SourceOperationId == op.Id).Sum(x => x.BaseQuantity);
                            op.IncomeCompleteQuantity = opRLs.Where(x => x.SourceOperationId == op.Id).Sum(x => x.IncomeQuantity);
                        }
                        _unitOfWork.SaveChange();
                        _dispatchOperationRecordService.Delete(operation);
                        _unitOfWork.SaveChange();
                        //更新派工单完工数量
                        var totalOps = _dispatchOperationRecordService.GetAll().Where(x => x.DispatchOrderId == order.Id && x.OperateType == OperateType.出单).ToList();
                        order.CompleteQuantity = totalOps.Sum(x => x.OperateQuantity);
                        order.MainCompleteQuantity = totalOps.Sum(x => x.MainQuantity);
                        order.BaseCompleteQuantity = totalOps.Sum(x => x.BaseQuantity);
                        order.IncomeCompleteQuantity = totalOps.Sum(x => x.IncomeQuantity);
                        //order.Status = DispatchOrderStatus.进行中;
                        _unitOfWork.SaveChange();
                    }

                    if (operation.OperateType == OperateType.接单)
                    {
                        _dispatchOperationRecordService.Delete(operation);
                        _unitOfWork.SaveChange();
                    }
                    transaction.Complete();
                }


                return Ok("删除成功");
            }
        }


        #region Dto

        public class GetOperationRecordListFilter
        {
            public bool IsPage { get; set; }
            public List<Filter> FilterList { get; set; } = new List<Filter>();
            public List<long> Departments { get; set; } = new List<long>();
        }


        public class GetDispatchOrderOperationDto
        {
            public long Id { get; set; }
            public bool IsDeleted { get; set; }
            public long DispatchOrderId { get; set; }
            public required string OrderNo { get; set; }
            public required string RecordNo { get; set; }
            public int DepartmentId { get; set; }
            public required string DepartmentName { get; set; }
            public long OperateUserId { get; set; }
            public required string OperateUserName { get; set; }
            public decimal OperateQuantity { get; set; }
            public decimal CompleteQuantity { get; set; }
            public decimal BaseQuantity { get; set; }
            public decimal BaseCompleteQuantity { get; set; }
            public decimal IncomeQuantity { get; set; }
            public decimal IncomeCompleteQuantity { get; set; }
            public decimal MainQuantity { get; set; }
            public decimal MainCompleteQuantity { get; set; }
            public decimal Price { get; set; }
            public decimal TotalPrice { get; set; }
            public string? Remark { get; set; }
            public OperateType OperateType { get; set; }
            public DateTime CreateTime { get; set; }
            public string? CreateUserName { get; set; }
            public long ProductId { get; set; }
            public long? SpecificationId { get; set; }
            public string? TechnicalCode { get; set; }
            public string ProductNo { get; set; } = string.Empty;
            public string ProductName { get; set; } = string.Empty;
            public string Specification { get; set; } = string.Empty;
            public string ProductCategory { get; set; } = string.Empty;
            public long ProductCategoryId { get; set; }
            public string PackageWay { get; set; } = string.Empty;
            public string DispatchUnit { get; set; } = string.Empty;
            public long? PackageId { get; set; }
            public string PackageSpecification { get; set; } = string.Empty;
            public long? IncomeId { get; set; }
            public UnitInfo? DispatchUnitInfo
            {
                get
                {
                    return JsonSerializer.Deserialize<UnitInfo>(DispatchUnit);
                }
            }
        }


        public class PostModifyOperation
        {
            public long Id { get; set; }
            public long OperateUserId { get; set; }
            public decimal Quantity { get; set; }
        }
        #endregion
    }
}
