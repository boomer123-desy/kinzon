using Kingzon.OLCM.Base.Services;
using KingZon.Core;
using KingZon.Core.DataBase;
using KingZon.Core.Web;
using KingZon.OLCM.Base.Service;
using KingZon.OLCM.Base.Service.Lock;
using KingZon.OLCM.Base.Shared.DataBase.Entities;
using KingZon.OLCM.Base.Shared.DataBase.Enum;
using KingZon.OLCM.Base.Shared.DataBase.JsonStructure;
using KingZon.OLCM.Inventory.Service;
using KingZon.OLCM.Inventory.Shared.DataBase.Entities;
using KingZon.OLCM.Inventory.Shared.DataBase.Enum;
using KingZon.OLCM.Inventory.Shared.DataBase.TmpEntities;
using KingZon.OLCM.Inventory.WebApi.Dto;
using KingZon.OLCM.Sale.Shared.DataBase.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Transactions;
using InspectionConclusion = KingZon.OLCM.Base.Shared.DataBase.Enum.InspectionConclusion;

namespace KingZon.OLCM.Inventory.WebApi.Controllers
{
    [Route("api/inventory/[controller]/[action]")]
    [ApiController]
    public class ReturnArrivalController : KingZonControllerBase
    {
        private readonly CommonService<SaleReturnArrivalRecord> _arrivalRecordService;
        private readonly CommonService<Sale_ReturnOrderDetail> _returnDetailService;
        private readonly CommonService<Sale_ReturnOrder> _returnOrderService;
        private readonly CommonService<Base_Product> _productService;
        private readonly CommonService<Base_Partner> _customerService;
        private readonly CommonService<Base_Goods> _goodsService;
        private readonly CommonService<Base_ProductSpecification> _specificationService;
        private readonly CommonService<Inventory_Record> _inventoryRecordService;
        private readonly CommonService<Inventory_Record_RL> _inventoryRecordRLService;
        private readonly CommonService<Inventory_OccupyDetail> _occupyDetailService;
        private readonly InventoryService _inventoryService;
        private readonly SequenceService _sequenceService;
        private readonly IUnitOfWork _unitOfWork;

        public ReturnArrivalController(
            CommonService<SaleReturnArrivalRecord> arrivalRecordService,
            CommonService<Sale_ReturnOrderDetail> returnDetailService,
            CommonService<Sale_ReturnOrder> returnOrderService,
            CommonService<Base_Product> productService,
            CommonService<Base_Partner> customerService,
            CommonService<Base_Goods> goodsService,
            CommonService<Base_ProductSpecification> specificationService,
            CommonService<Inventory_Record> inventoryRecordService,
            CommonService<Inventory_Record_RL> inventoryRecordRLService,
            CommonService<Inventory_OccupyDetail> occupyDetailService,
            InventoryService inventoryService,
            SequenceService sequenceService,
            IUnitOfWork unitOfWork)
        {
            _arrivalRecordService = arrivalRecordService;
            _returnDetailService = returnDetailService;
            _returnOrderService = returnOrderService;
            _productService = productService;
            _customerService = customerService;
            _goodsService = goodsService;
            _specificationService = specificationService;
            _inventoryRecordService = inventoryRecordService;
            _inventoryRecordRLService = inventoryRecordRLService;
            _occupyDetailService = occupyDetailService;
            _inventoryService = inventoryService;
            _sequenceService = sequenceService;
            _unitOfWork = unitOfWork;
        }

        /// <summary>
        /// 按销售退货订单明细查询退货到货数据。
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> GetReturnArrivalDetails([FromBody] GetReturnArrivalDetailsPostDto postData)
        {
            var orderFilters = postData.SaleReturnOrderFilterList ?? [];
            var detailFilters = postData.SaleReturnOrderDetailFilterList ?? [];
            var productFilters = postData.ProductFilterList ?? [];
            var details = _returnDetailService.GetAll().ApplyFilters(detailFilters);

            var query = from detail in details
                        join order in _returnOrderService.GetAll().ApplyFilters(orderFilters)
                            on detail.SaleReturnOrderId equals order.Id
                        join customer in _customerService.GetAll()
                            on order.CustomerId equals customer.Id
                        join product in _productService.GetAll()
                            .Include(x => x.BaseUnit)
                            .Include(x => x.ProductCategory)
                            .ApplyFilters(productFilters)
                            on detail.ProductId equals product.Id
                        join goods in _goodsService.GetAll().Include(x => x.SalesUnit)
                            on detail.GoodsId equals goods.Id into goodsItems
                        from goods in goodsItems.DefaultIfEmpty()
                        join specification in _specificationService.GetAll()
                            on detail.SpecificationId equals specification.Id into specifications
                        from specification in specifications.DefaultIfEmpty()
                        let arrivalQuantity = _arrivalRecordService.GetAll(false, null)
                            .Where(x => x.SaleReturnOrderDetailId == detail.Id)
                            .Sum(x => (decimal?)x.BaseArrivalQuantity) ?? 0
                        where (postData.IsComplete
                            ? arrivalQuantity >= detail.ReturnQuantity
                            : arrivalQuantity < detail.ReturnQuantity)
                        select new ReturnArrivalDetailDto
                        {
                            Id = detail.Id,
                            SaleReturnOrderId = order.Id,
                            SaleReturnOrderNo = order.OrderNo,
                            SaleReturnTime = order.ReturnTime,
                            CustomerCode = customer.CustomerCode,
                            CustomerName = customer.CustomerName,
                            ProductId = product.Id,
                            ProductNo = product.ProductNo,
                            ProductName = product.ProductName,
                            CustomerMaterialNo = detail.CustomerMaterialNo ?? (goods != null ? goods.CustomerMaterialNo : null),
                            TechnicalCode = product.TechnicalCode,
                            ProductCategory = product.ProductCategory != null ? product.ProductCategory.Name : null,
                            MaterialGrade = product.MaterialGrade,
                            MaterialSpecification = product.MaterialSpecification,
                            ProductSpecification = specification != null ? specification.Specification : string.Empty,
                            ProductRemark = product.Remark,
                            ReturnUnitConversionId = detail.ReturnUnitConversionId,
                            ReturnQuantity = detail.ReturnQuantity,
                            ArrivalQuantity = arrivalQuantity,
                            RemainingQuantity = detail.ReturnQuantity - arrivalQuantity,
                            IsComplete = arrivalQuantity >= detail.ReturnQuantity,
                            Reason = detail.Reason,
                            Remark = detail.Remark,
                            UnitInfo = detail.UnitInfo,
                        };

            var pagedResult = await query.ApplySorts(this.SortInfoList).ToPagedListAsync(this.PageModel);
            foreach (var item in pagedResult.Data)
            {
                item.UnitInfos = DeserializeUnitInfos(item.UnitInfo);
            }
            return Ok(pagedResult);
        }

        /// <summary>
        /// 查询销售退货到货记录。未入库、已入库页共用此接口。
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> GetReturnArrivalRecords([FromBody] GetReturnArrivalRecordsPostDto postData)
        {
            var query = from arrival in _arrivalRecordService.GetAll().ApplyFilters(postData.ArrivalRecordFilterList ?? [])
                        join detail in _returnDetailService.GetAll().ApplyFilters(postData.SaleReturnOrderDetailFilterList ?? [])
                            on arrival.SaleReturnOrderDetailId equals detail.Id
                        join order in _returnOrderService.GetAll().ApplyFilters(postData.SaleReturnOrderFilterList ?? [])
                            on detail.SaleReturnOrderId equals order.Id
                        join customer in _customerService.GetAll().ApplyFilters(postData.CustomerFilterList ?? [])
                            on order.CustomerId equals customer.Id
                        join product in _productService.GetAll()
                            .Include(x => x.BaseUnit)
                            .Include(x => x.ProductCategory)
                            .ApplyFilters(postData.ProductFilterList ?? [])
                            on detail.ProductId equals product.Id
                        join goods in _goodsService.GetAll().Include(x => x.SalesUnit)
                            on detail.GoodsId equals goods.Id into goodsItems
                        from goods in goodsItems.DefaultIfEmpty()
                        join specification in _specificationService.GetAll()
                            on detail.SpecificationId equals specification.Id into specifications
                        from specification in specifications.DefaultIfEmpty()
                        where arrival.IsCompleted == postData.IsCompleted
                        select new ReturnArrivalRecordDto
                        {
                            Id = arrival.Id,
                            ArrivalRecordNo = arrival.ArrivalRecordNo,
                            SaleReturnOrderDetailId = detail.Id,
                            SaleReturnOrderId = order.Id,
                            SaleReturnOrderNo = order.OrderNo,
                            CustomerCode = customer.CustomerCode,
                            CustomerName = customer.CustomerName,
                            ProductId = product.Id,
                            ProductNo = product.ProductNo,
                            ProductName = product.ProductName,
                            CustomerMaterialNo = detail.CustomerMaterialNo ?? (goods != null ? goods.CustomerMaterialNo : null),
                            TechnicalCode = product.TechnicalCode,
                            ProductCategory = product.ProductCategory != null ? product.ProductCategory.Name : null,
                            MaterialGrade = product.MaterialGrade,
                            MaterialSpecification = product.MaterialSpecification,
                            ProductSpecification = specification != null ? specification.Specification : string.Empty,
                            ProductRemark = product.Remark,
                            ArrivalTime = arrival.ArrivalTime,
                            ArrivalQuantity = arrival.ArrivalQuantity,
                            BaseArrivalQuantity = arrival.BaseArrivalQuantity,
                            InventoryQuantity = arrival.InventoryQuantity,
                            ArrivalUnit = arrival.ArrivalUnit,
                            ArrivalUnitConversionId = arrival.ArrivalUnitConversionId,
                            IsCompleted = arrival.IsCompleted,
                            Remark = arrival.Remark,
                            UnitConversionList = arrival.UnitConversionList,
                        };

            var pagedResult = await query.ApplySorts(this.SortInfoList).ToPagedListAsync(this.PageModel);
            foreach (var item in pagedResult.Data)
            {
                item.UnitInfos = DeserializeUnitInfos(item.UnitConversionList);
            }
            return Ok(pagedResult);
        }

        /// <summary>
        /// 获取一张退货单的全部待到货明细，用于整单到货登记。
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetReturnArrivalOrderDetails([FromQuery]long SaleReturnOrderId)
        {
            var query = from detail in _returnDetailService.GetAll()
                        join order in _returnOrderService.GetAll() on detail.SaleReturnOrderId equals order.Id
                        join customer in _customerService.GetAll() on order.CustomerId equals customer.Id
                        join product in _productService.GetAll().Include(x => x.BaseUnit) on detail.ProductId equals product.Id
                        let arrivalQuantity = _arrivalRecordService.GetAll(false, null)
                            .Where(x => x.SaleReturnOrderDetailId == detail.Id)
                            .Sum(x => (decimal?)x.BaseArrivalQuantity) ?? 0
                        where order.Id == SaleReturnOrderId && arrivalQuantity < detail.ReturnQuantity
                        select new ReturnArrivalDetailDto
                        {
                            Id = detail.Id,
                            SaleReturnOrderId = order.Id,
                            SaleReturnOrderNo = order.OrderNo,
                            SaleReturnTime = order.ReturnTime,
                            CustomerCode = customer.CustomerCode,
                            CustomerName = customer.CustomerName,
                            ProductId = product.Id,
                            ProductNo = product.ProductNo,
                            ProductName = product.ProductName,
                            ReturnUnitConversionId = detail.ReturnUnitConversionId,
                            ReturnQuantity = detail.ReturnQuantity,
                            ArrivalQuantity = arrivalQuantity,
                            RemainingQuantity = detail.ReturnQuantity - arrivalQuantity,
                            IsComplete = false,
                            Reason = detail.Reason,
                            Remark = detail.Remark,
                            UnitInfo = detail.UnitInfo,
                        };

            var result = await query.OrderBy(x => x.Id).ToListAsync();
            foreach (var item in result)
            {
                item.UnitInfos = DeserializeUnitInfos(item.UnitInfo);
            }
            return Ok(result);
        }

        public class PostReturnArrivalWarehousingDto
        {
            public long ArrivalRecordId { get; set; }
            public InventoryDataType DataType { get; set; }
            public long DataId { get; set; }
            public List<PostReturnArrivalInventoryRecordDto> InventoryRecordList { get; set; } = [];
        }

        public class PostReturnArrivalInventoryRecordDto
        {
            public List<InventoryUnitInboundDto> UnitQtyList { get; set; } = [];
            public long PositionId { get; set; }
            public string? Remark { get; set; }
        }

        /// <summary>
        /// 销售退货到货入库。入库界面复用标准 Inbound 组件，数量按产品主单位校验和保存。
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> PostReturnArrivalWarehousing([FromBody] PostReturnArrivalWarehousingDto postData)
        {
            var arrival = await _arrivalRecordService.GetByIdAsync(postData.ArrivalRecordId)
                ?? throw new Exception("到货记录不存在，请刷新后重试");
            if (arrival.IsCompleted)
            {
                throw new Exception("该到货记录已完成入库，请刷新后重试");
            }

            var detail = await _returnDetailService.GetByIdAsync(arrival.SaleReturnOrderDetailId)
                ?? throw new Exception("销售退货明细不存在，请刷新后重试");

            var inventoryRecords = BuildReturnArrivalInventoryRecords(arrival, detail, postData.InventoryRecordList);
            await CommitReturnArrivalWarehousingAsync(inventoryRecords);
            return Ok("入库成功");
        }

        /// <summary>
        /// 删除销售退货入库记录，并重新计算到货记录/退货明细的入库数量与完成状态。
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> DeleteReturnArrivalInventoryRecord([FromQuery] long id)
        {
            var inventoryRecord = await _inventoryRecordService.GetByIdAsync(id);
            if (inventoryRecord == null)
            {
                return Ok("删除成功");
            }
            if (inventoryRecord.SourceType != InventorySourceType.退货入库)
            {
                throw new Exception("该库存记录不属于销售退货入库，不能删除");
            }

            var usedRls = await _inventoryRecordRLService.GetAll()
                .Where(o => o.SourceInventoryId == inventoryRecord.Id).ToListAsync();
            if (usedRls.Any())
            {
                throw new Exception("该记录已被使用，不能删除");
            }

            var occupyDetails = await _occupyDetailService.GetAll()
                .Where(o => o.InventoryRecordId == inventoryRecord.Id).ToListAsync();
            if (occupyDetails.Any())
            {
                throw new Exception("该记录已被占用，不能删除");
            }

            var arrival = await _arrivalRecordService.GetByIdAsync(inventoryRecord.SourceId)
                ?? throw new Exception("到货记录不存在，请刷新后重试");
            var detail = await _returnDetailService.GetByIdAsync(arrival.SaleReturnOrderDetailId)
                ?? throw new Exception("销售退货明细不存在，请刷新后重试");

            using var transaction = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
            _inventoryRecordService.Delete(inventoryRecord);
            await _unitOfWork.SaveChangeAsync();

            lock (QuantityLock.InventoryUpdateLock)
            {
                // 重新计算到货记录的入库数量与完成状态
                arrival.InventoryQuantity = _inventoryRecordService.GetAll()
                    .Where(o => o.SourceType == InventorySourceType.退货入库
                             && o.SourceId == arrival.Id
                             && o.InventoryType == InventoryType.入库)
                    .Sum(o => o.Quantity);
                arrival.IsCompleted = arrival.InventoryQuantity >= arrival.BaseArrivalQuantity;
                _unitOfWork.SaveChange();

                // 重新计算退货明细的入库数量与完成状态
                detail.InventoryQuantity = _arrivalRecordService.GetAll()
                    .Where(x => x.SaleReturnOrderDetailId == detail.Id)
                    .Sum(x => x.InventoryQuantity);
                detail.IsComplete = detail.InventoryQuantity >= detail.ReturnQuantity;
                _unitOfWork.SaveChange();

                // 更新物料库存、更新库位已使用量
                var dataKeys = new List<Temp_Inventory_Keys>
                {
                    new Temp_Inventory_Keys { DataType = inventoryRecord.DataType, DataId = inventoryRecord.DataId }
                };
                var positionIds = inventoryRecord.PositionId.HasValue
                    ? new List<long> { (long)inventoryRecord.PositionId! }
                    : new List<long> { 0 };
                _inventoryService.UpdateDataInventory(dataKeys, positionIds);
                _unitOfWork.SaveChange();
            }
            transaction.Complete();
            return Ok("删除成功");
        }

        /// <summary>
        /// 生成销售退货到货记录；不生成任何入库记录，也不改变退货明细的入库数量。
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> SaveReturnArrivalRecords([FromBody] List<PostReturnArrivalRecordDto> arrivalRecords)
        {
            if (arrivalRecords == null || arrivalRecords.Count == 0)
            {
                throw new Exception("到货记录不能为空");
            }

            var detailIds = arrivalRecords.Select(x => x.SaleReturnOrderDetailId).Distinct().ToList();
            var returnDetails = await _returnDetailService.GetAll()
                .Where(x => detailIds.Contains(x.Id)).ToListAsync();
            if (returnDetails.Count != detailIds.Count)
            {
                throw new Exception("销售退货明细不存在，请刷新后重试");
            }
            var productIds = returnDetails.Select(x => x.ProductId).Distinct().ToList();
            var products = await _productService.GetAll().Include(x => x.BaseUnit)
                .Where(x => productIds.Contains(x.Id)).ToListAsync();

            using var transaction = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
            var existingRecords = await _arrivalRecordService.GetAll()
                .Where(x => detailIds.Contains(x.SaleReturnOrderDetailId ?? 0)).ToListAsync();
            var newRecords = new List<SaleReturnArrivalRecord>();
            foreach (var item in arrivalRecords)
            {
                if (item.ArrivalQuantity <= 0)
                {
                    throw new Exception("到货数量必须大于 0");
                }

                var returnDetail = returnDetails.First(x => x.Id == item.SaleReturnOrderDetailId);
                var product = products.FirstOrDefault(x => x.Id == returnDetail.ProductId)
                    ?? throw new Exception("退货产品不存在，请刷新后重试");
                var unitInfos = DeserializeUnitInfos(returnDetail.UnitInfo);
                var selectedUnit = unitInfos.SingleOrDefault(x => x.Id == item.ArrivalUnitConversionId)
                        ?? throw new Exception("到货单位不存在，请刷新后重试");
                var ratio = selectedUnit?.Ratio ?? 1;
                if (ratio <= 0)
                {
                    throw new Exception("到货单位换算率必须大于 0");
                }
                var baseArrivalQuantity = item.ArrivalQuantity * ratio;
                var arrivedQuantity = existingRecords.Where(x => x.SaleReturnOrderDetailId == returnDetail.Id)
                    .Sum(x => x.BaseArrivalQuantity);
                var batchQuantity = newRecords.Where(x => x.SaleReturnOrderDetailId == returnDetail.Id)
                    .Sum(x => x.BaseArrivalQuantity);
                if (arrivedQuantity + batchQuantity + baseArrivalQuantity > returnDetail.ReturnQuantity)
                {
                    throw new Exception("到货数量不能大于退货数量");
                }

                var newUnits = unitInfos.Select(x => new InventoryUnitInboundDto
                {
                    Id = x.Id,
                    UnitName = x.UnitName,
                    UnitId = x.UnitId,
                    ConversionType = x.ConversionType,
                    Ratio = x.Ratio,
                    Quantity = x.Ratio != 0 ? baseArrivalQuantity / x.Ratio : 0
                }).ToList();

                newRecords.Add(new SaleReturnArrivalRecord
                {
                    ArrivalRecordNo = _sequenceService.GenerateCode(TableName.Inventory_ArrivalRecord),
                    ArrivalTime = item.ArrivalTime.Date,
                    ArrivalQuantity = item.ArrivalQuantity,
                    BaseArrivalQuantity = baseArrivalQuantity,
                    InventoryQuantity = 0,
                    IsCompleted = false,
                    IsInspection = false,
                    InspectionConclusion = InspectionConclusion.退货,
                    Remark = item.Remark,
                    SaleReturnOrderDetailId = returnDetail.Id,
                    ArrivalUnit = selectedUnit?.UnitName ?? product.BaseUnit.Name,
                    ArrivalUnitId = selectedUnit?.UnitId ?? product.BaseUnitId,
                    ArrivalUnitConversionId = item.ArrivalUnitConversionId,
                    Ratio = ratio,
                    UnitConversionList = JsonSerializer.Serialize(newUnits),
                });
            }

            await _arrivalRecordService.InsertAsync(newRecords);
            await _unitOfWork.SaveChangeAsync();
            transaction.Complete();
            return Ok("到货记录新增成功");
        }

        private static List<InventoryUnitDto> DeserializeUnitInfos(string? unitInfo)
        {
            var units =string.IsNullOrWhiteSpace(unitInfo)
                ? []
                : JsonSerializer.Deserialize<List<InventoryUnitDto>>(unitInfo, JsonHelper.DefaultJsonSerializerOptions) ?? [];
            units = units.OrderBy(x=>x.Id).ToList(); 
            return units;
        }

        #region 批量入库

        public class GetReturnArrivalInventoryRecordsPostDto
        {
            public List<long> ArrivalRecordIds { get; set; } = [];
        }

        /// <summary>
        /// 批量入库：按到货记录 Id 查询待到货入库的数据（待入库量 &gt; 0 的到货记录）。
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> GetReturnArrivalInventoryRecords([FromBody] GetReturnArrivalInventoryRecordsPostDto postData)
        {
            if (postData.ArrivalRecordIds == null || postData.ArrivalRecordIds.Count == 0)
            {
                return Ok(new List<ReturnArrivalInventoryRecordDto>());
            }

            var query = from arrival in _arrivalRecordService.GetAll()
                        where postData.ArrivalRecordIds.Contains(arrival.Id)
                        join detail in _returnDetailService.GetAll()
                            on arrival.SaleReturnOrderDetailId equals detail.Id
                        join order in _returnOrderService.GetAll()
                            on detail.SaleReturnOrderId equals order.Id
                        join customer in _customerService.GetAll()
                            on order.CustomerId equals customer.Id
                        join product in _productService.GetAll().Include(x => x.BaseUnit)
                            on detail.ProductId equals product.Id
                        let remainingQuantity = arrival.BaseArrivalQuantity - arrival.InventoryQuantity
                        where remainingQuantity > 0
                        select new ReturnArrivalInventoryRecordDto
                        {
                            Id = arrival.Id,
                            ArrivalRecordNo = arrival.ArrivalRecordNo,
                            SaleReturnOrderNo = order.OrderNo,
                            CustomerCode = customer.CustomerCode,
                            CustomerName = customer.CustomerName,
                            ProductId = product.Id,
                            ProductNo = product.ProductNo,
                            ProductName = product.ProductName,
                            ProductUnit = product.BaseUnit.Name,
                            ProductUnitId = product.BaseUnitId,
                            ArrivalTime = arrival.ArrivalTime,
                            ArrivalQuantity = arrival.ArrivalQuantity,
                            BaseArrivalQuantity = arrival.BaseArrivalQuantity,
                            InventoryQuantity = arrival.InventoryQuantity,
                            RemainingQuantity = remainingQuantity,
                            ArrivalUnit = arrival.ArrivalUnit,
                            ArrivalUnitConversionId = arrival.ArrivalUnitConversionId,
                            DataType = detail.SpecificationId == null ? InventoryDataType.Product : InventoryDataType.Specification,
                            DataId = detail.SpecificationId ?? detail.ProductId,
                            Remark = arrival.Remark,
                            UnitConversionList = arrival.UnitConversionList,
                        };

            var result = await query.ToListAsync();
            foreach (var item in result)
            {
                item.UnitInfos = DeserializeUnitInfos(item.UnitConversionList);
            }
            return Ok(result);
        }

        public class PostReturnArrivalBatchWarehousingDto
        {
            public List<PostReturnArrivalBatchItemDto> ArrivalRecords { get; set; } = [];
        }

        public class PostReturnArrivalBatchItemDto
        {
            public long ArrivalRecordId { get; set; }
            public List<PostReturnArrivalInventoryRecordDto> InventoryRecordList { get; set; } = [];
        }

        /// <summary>
        /// 销售退货批量入库。一次处理多条到货记录，全部校验通过后统一生成入库记录。
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> PostReturnArrivalBatchWarehousing([FromBody] PostReturnArrivalBatchWarehousingDto postData)
        {
            if (postData.ArrivalRecords == null || postData.ArrivalRecords.Count == 0)
            {
                throw new Exception("请选择待入库的到货记录");
            }

            var arrivalIds = postData.ArrivalRecords.Select(x => x.ArrivalRecordId).Distinct().ToList();
            var arrivals = await _arrivalRecordService.GetAll()
                .Where(x => arrivalIds.Contains(x.Id)).ToListAsync();
            if (arrivals.Count != arrivalIds.Count)
            {
                throw new Exception("到货记录不存在，请刷新后重试");
            }

            var detailIds = arrivals.Select(x => x.SaleReturnOrderDetailId ?? 0).Distinct().ToList();
            var details = await _returnDetailService.GetAll()
                .Where(x => detailIds.Contains(x.Id)).ToListAsync();

            var inventoryRecords = new List<Inventory_Record>();
            foreach (var item in postData.ArrivalRecords)
            {
                var arrival = arrivals.First(x => x.Id == item.ArrivalRecordId);
                if (arrival.IsCompleted)
                {
                    throw new Exception($"到货记录[{arrival.ArrivalRecordNo}]已完成入库，请刷新后重试");
                }

                var detail = details.FirstOrDefault(x => x.Id == arrival.SaleReturnOrderDetailId)
                    ?? throw new Exception("销售退货明细不存在，请刷新后重试");

                inventoryRecords.AddRange(
                    BuildReturnArrivalInventoryRecords(arrival, detail, item.InventoryRecordList));
            }

            await CommitReturnArrivalWarehousingAsync(inventoryRecords);
            return Ok("批量入库成功");
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 构建单条到货记录的入库记录：校验数量与库位、生成 Inventory_Record，并累计到货记录/退货明细的入库数量。
        /// </summary>
        private List<Inventory_Record> BuildReturnArrivalInventoryRecords(
            SaleReturnArrivalRecord arrival,
            Sale_ReturnOrderDetail detail,
            List<PostReturnArrivalInventoryRecordDto> inventoryRecordList)
        {
            if (inventoryRecordList == null || inventoryRecordList.Count == 0)
            {
                throw new Exception("请添加入库记录");
            }

            var dataType = detail.SpecificationId == null ? InventoryDataType.Product : InventoryDataType.Specification;
            var dataId = detail.SpecificationId ?? detail.ProductId;

            var quantities = inventoryRecordList.Select(item => new
            {
                Item = item,
                Quantity = item.UnitQtyList?.SingleOrDefault(unit => unit.Id == -1)?.Quantity ?? 0,
            }).ToList();
            if (quantities.Any(item => item.Quantity <= 0 || item.Item.PositionId <= 0))
            {
                throw new Exception("入库数量必须大于 0，且必须选择库位库区");
            }

            var totalQuantity = quantities.Sum(item => item.Quantity);
            var remainingQuantity = arrival.BaseArrivalQuantity - arrival.InventoryQuantity;
            if (totalQuantity > remainingQuantity)
            {
                throw new Exception("入库数量不能大于待入库量");
            }

            var inventoryRecords = quantities.Select(item => new Inventory_Record
            {
                RecordNo = _sequenceService.GenerateCode(TableName.InventoryRecord),
                InventoryType = InventoryType.入库,
                PositionId = item.Item.PositionId,
                Quantity = item.Quantity,
                InputQuantity = arrival.ArrivalQuantity,
                InventoryUnitInfo = arrival.UnitConversionList,
                DataType = dataType,
                DataId = dataId,
                Price = detail.Price,
                TotalPrice = detail.Price * item.Quantity,
                SourceType = InventorySourceType.退货入库,
                SourceId = arrival.Id,
                Remark = item.Item.Remark,
                DataStatus = InventoryDataStatus.已包装,
                InputUnitConversionId = arrival.ArrivalUnitConversionId,
            }).ToList();

            // 累计入库数量并更新完成状态
            arrival.InventoryQuantity += totalQuantity;
            detail.InventoryQuantity += totalQuantity;
            detail.IsComplete = arrival.InventoryQuantity >= arrival.BaseArrivalQuantity;
            arrival.IsCompleted = arrival.InventoryQuantity >= arrival.BaseArrivalQuantity;

            return inventoryRecords;
        }

        /// <summary>
        /// 提交退货入库：事务内插入入库记录，锁内更新物料库存与库位已使用量。
        /// </summary>
        private async Task CommitReturnArrivalWarehousingAsync(List<Inventory_Record> inventoryRecords)
        {
            using var transaction = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
            await _inventoryRecordService.InsertAsync(inventoryRecords);
            await _unitOfWork.SaveChangeAsync();

            lock (QuantityLock.InventoryUpdateLock)
            {
                var dataKeys = inventoryRecords.Select(item => new Temp_Inventory_Keys
                {
                    DataType = item.DataType,
                    DataId = item.DataId,
                }).DistinctBy(item => new { item.DataType, item.DataId }).ToList();
                var positionIds = inventoryRecords.Where(item => item.PositionId.HasValue)
                    .Select(item => (long)item.PositionId!).Distinct().ToList();
                _inventoryService.UpdateDataInventory(dataKeys, positionIds);
                _unitOfWork.SaveChange();
            }
            transaction.Complete();
        }

        #endregion
    }
}
