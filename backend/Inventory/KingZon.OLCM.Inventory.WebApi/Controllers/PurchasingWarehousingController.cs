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
using KingZon.OLCM.Purchase.DataBase.Entities;
using KingZon.OLCM.Purchase.DataBase.Enum;
using KingZon.OLCM.Quality.Shared.DataBase.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Transactions;
using CustomAttribute = Kingzon.OLCM.Base.Services.JsonStructure.CustomAttribute;

namespace KingZon.OLCM.Inventory.WebApi.Controllers
{
    [Route("api/inventory/[controller]/[action]")]
    public class PurchasingWarehousingController : KingZonControllerBase
    {
        private readonly StorageService _storageService;
        private readonly CommonService<Base_Category> _materialCategoryService;
        private readonly CommonService<PurchaseArrivalRecord> _purchaseArrivalRecord;
        private readonly CommonService<Purchase_Order_Detail> _purchaseOrderDetailService;
        private readonly CommonService<Purchase_Order> _purchaseOrderService;
        private readonly ProductService _productActService;
        private readonly InventoryService _inventoryService;
        private readonly CommonService<Quality_ArrivalInspection> _arrivalInspectionService;
        private readonly CommonService<Inventory_Record> _inventoryRecordService;
        private readonly CommonService<Inventory_ManageUser_RL> _inventoryAreaManageUserRL;
        private readonly CommonService<Inventory_Area> _inventoryArea;
        private readonly CommonService<Inventory_Location> _storageLocationService;
        private readonly CommonService<Inventory_Position> _storagePositionService;
        private readonly CommonService<Inventory_Category_RL> _storageLocation_MaterialCategory_RL_Service;
        private readonly CommonService<Inventory_OccupyDetail> _occupyDetailService;
        private readonly CommonService<Inventory_Record_RL> _inventoryRecordRLService;
        private readonly CommonService<Inventory_Record_Purchase_Ext> _inventoryRecordPurchaseExtService;
        private readonly SequenceService _sequenceService;
        private readonly SystemParaService _systemParaService;
        private readonly IUnitOfWork _unitOfWork;

        public PurchasingWarehousingController(
            StorageService storageService,
            CommonService<Base_Category> materialCategoryService,
            CommonService<PurchaseArrivalRecord> purchaseArrivalRecord,
            CommonService<Purchase_Order_Detail> purchaseOrderDetailService,
            CommonService<Purchase_Order> purchaseOrderService,
            ProductService productActService,
            InventoryService inventoryService,
            CommonService<Quality_ArrivalInspection> arrivalInspectionService,
            CommonService<Inventory_Record> inventoryRecordService,
            CommonService<Inventory_ManageUser_RL> inventoryAreaManageUserRL,
            CommonService<Inventory_Area> inventoryArea,
            CommonService<Inventory_Location> storageLocationService,
            CommonService<Inventory_Position> storagePositionService,
            CommonService<Inventory_Category_RL> storageLocation_MaterialCategory_RL_Service,
            CommonService<Inventory_OccupyDetail> occupyDetailService,
            CommonService<Inventory_Record_RL> inventoryRecordRLService,
            CommonService<Inventory_Record_Purchase_Ext> inventoryRecordPurchaseExtService,
            SequenceService sequenceService,
            SystemParaService systemParaService,
            IUnitOfWork unitOfWork
         )
        {
            _storageService = storageService;
            _materialCategoryService = materialCategoryService;
            _purchaseArrivalRecord = purchaseArrivalRecord;
            _purchaseOrderDetailService = purchaseOrderDetailService;
            _purchaseOrderService = purchaseOrderService;
            _productActService = productActService;
            _inventoryService = inventoryService;
            _arrivalInspectionService = arrivalInspectionService;
            _inventoryRecordService = inventoryRecordService;
            _inventoryAreaManageUserRL = inventoryAreaManageUserRL;
            _inventoryArea = inventoryArea;
            _storageLocationService = storageLocationService;
            _storagePositionService = storagePositionService;
            _storageLocation_MaterialCategory_RL_Service = storageLocation_MaterialCategory_RL_Service;
            _occupyDetailService = occupyDetailService;
            _inventoryRecordRLService = inventoryRecordRLService;
            _inventoryRecordPurchaseExtService = inventoryRecordPurchaseExtService;
            _sequenceService = sequenceService;
            _systemParaService = systemParaService;
            _unitOfWork = unitOfWork;
        }


        public class GetArrivalRecordForInventoryFilterDTO
        {
            public List<Filter> FilterList { get; set; } = new List<Filter>();
            public bool IsPaged { get; set; }
        }

        /// <summary>
        /// 获取到货数据
        /// </summary>
        /// <param name="dto"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> GetArrivalInspectionInventoryRecords([FromBody] GetArrivalRecordForInventoryFilterDTO postData)
        {
            var inspectionPara = await _systemParaService.GetAll().Where(x => x.DataType == SystemParaType.是否检验通过才能入库).FirstOrDefaultAsync();
            var isInspection = inspectionPara != null && inspectionPara.DataValue == "1" ? true : false;

            var inventoryQuery = from inventory in _inventoryRecordService.GetAll()
                                 where inventory.InventoryType == InventoryType.入库 &&
                                 inventory.SourceType == InventorySourceType.采购入库
                                 group inventory by inventory.SourceId into g
                                 select new
                                 {
                                     SourceId = g.Key,
                                     Price = g.OrderByDescending(x=>x.CreateTime).First().Price,
                                     TotalPrice = g.Sum(x => x.TotalPrice),
                                 };

            var purchaseQuery = from arrivalRecord in _purchaseArrivalRecord.GetAll()
                                join arrivalInspectionRecord in _arrivalInspectionService.GetAll()
                                        on arrivalRecord.Id equals arrivalInspectionRecord.ArrivalRecordId 
                                        into inspectionTemp
                                from arrivalInspectionRecord in inspectionTemp.DefaultIfEmpty()
                                join purchaseDetail in _purchaseOrderDetailService.GetAll()
                                        on arrivalRecord.PurchaseOrderDetailId equals purchaseDetail.Id
                                join productinfo in _productActService.GetProductList([], [])
                                                  on new { ProductId = purchaseDetail.DataId, InventoryDataType = purchaseDetail.DataType }
                                                  equals new
                                                  {
                                                      ProductId = productinfo.SpecificationId >0 ? productinfo.SpecificationId.Value : productinfo.ProductId,
                                                      InventoryDataType = productinfo.SpecificationId > 0 ? InventoryDataType.Specification :InventoryDataType.Product
                                                  }
                                                  into tempproduct
                                from productinfo in tempproduct.DefaultIfEmpty()
                                join inventory in inventoryQuery
                                        on arrivalRecord.Id equals inventory.SourceId 
                                        into inventoryTemp
                                from inventory in inventoryTemp.DefaultIfEmpty()
                                join position in _storagePositionService.GetAll()
                                    on arrivalRecord.PositionId equals position.Id into positionTemp
                                from position in positionTemp.DefaultIfEmpty()
                                let waitInventoryQuantity = arrivalInspectionRecord.Id > 0 ? ((arrivalInspectionRecord.QualifiedQuantity + arrivalInspectionRecord.ConcessionQuantity) * arrivalRecord.Ratio - arrivalRecord.InventoryQuantity) :
                                                                  !isInspection ? arrivalRecord.BaseArrivalQuantity - arrivalRecord.InventoryQuantity :
                                                                        0
                                select new ArrivalInspectionRecordDTO
                                {
                                    Id = arrivalInspectionRecord.Id > 0 ? arrivalInspectionRecord.Id : 0,
                                    ArrivalRecordId = arrivalRecord.Id,
                                    ArrivalRecordNo = arrivalRecord.ArrivalRecordNo,
                                    //物料信息
                                    ProductId = productinfo.ProductId > 0 ? productinfo.ProductId : 0,
                                    SpecificationId = productinfo.SpecificationId > 0 ? productinfo.SpecificationId : 0,
                                    ProductNo = productinfo.ProductId > 0 ? productinfo.ProductNo : string.Empty,
                                    ProductName = productinfo.ProductId > 0 ? purchaseDetail.ProductName ?? string.Empty : string.Empty,
                                    ProductType = productinfo.ProductId > 0 ? productinfo.ProductType : ProductType.物料,
                                    ProductCategoryId = productinfo.ProductId > 0 ? (productinfo.ProductCategoryId ?? 0) : 0, // 产品类别Id
                                    ProductCategory = productinfo.ProductId > 0 ? productinfo.ProductCategory : string.Empty, // 产品类别
                                    ProductUnit = productinfo.ProductId > 0 ? productinfo.ProductUnit ?? string.Empty : string.Empty,
                                    MaterialGrade = productinfo.ProductId > 0 ? productinfo.MaterialGrade : string.Empty,
                                    ProductTechnicalCode = productinfo.ProductId > 0 ? productinfo.ProductTechnicalCode : string.Empty,
                                    ProductPackageWay = productinfo.ProductId > 0 ? productinfo.ProductPackageWay : string.Empty,
                                    ProductSpecification = productinfo.ProductId > 0 ? productinfo.ProductType == ProductType.物料 ?
                                                                productinfo.MaterialSpecification :
                                                                    productinfo.ProductSpecification : string.Empty,
                                    ProductRemark = productinfo.ProductId > 0 ? productinfo.Remark : string.Empty,
                                    ProductCustomAttribute = productinfo.ProductId > 0 ? productinfo.CustomAttribute : null,
                                    ProductCategoryAttribute = productinfo.ProductId > 0 ? productinfo.CategoryAttribute : null,

                                    //到货单位
                                    SelfArrivalQuantity = arrivalRecord.ArrivalQuantity, //到货
                                    ArrivalUnitId = arrivalRecord.ArrivalUnitId,
                                    ArrivalUnitConversionId = arrivalRecord.ArrivalUnitConversionId,
                                    ArrivalUnit = arrivalRecord.ArrivalUnit,
                                    //采购信息
                                    PurchaseOrderNo = purchaseDetail.PurchaseOrder.OrderNo,
                                    PurchaseType = purchaseDetail.PurchaseType,
                                    SupplierName = purchaseDetail.PurchaseOrder.Supplier.CustomerName,
                                    Ratio = arrivalRecord.Ratio,
                                    PurchaseRemark = purchaseDetail.Remark,
                                    //检验信息：到货单位
                                    InspectionConclusion = arrivalRecord.InspectionConclusion,
                                    InspectionReason = arrivalInspectionRecord.Id > 0 ? arrivalInspectionRecord.InspectionResult : string.Empty,
                                    CreateUserName = arrivalInspectionRecord.Id > 0 ? arrivalInspectionRecord.CreateUserName : string.Empty,
                                    CreateTime = arrivalInspectionRecord.Id > 0 ? arrivalInspectionRecord.CreateTime : null,

                                    //到货信息
                                    Remark = arrivalRecord.Remark,
                                    CustomAttribute = arrivalRecord.CustomAttribute,
                                    //手动完成
                                    IsComplete = arrivalRecord.IsCompleted,
                                    PositionId = arrivalRecord.PositionId,
                                    ArrivalTime = arrivalRecord.ArrivalTime,
                                    ArrivalCreateTime = arrivalRecord.CreateTime,
                                    ArrivalCreateUserName = arrivalRecord.CreateUserName,
                                    DataStatus = arrivalRecord.DataStatus,
                                    UnitInfoString = arrivalRecord.UnitConversionList,
                                    //数量：物料主单位
                                    ArrivalQuantity = arrivalRecord.ArrivalQuantity * arrivalRecord.Ratio,
                                    InventoryQuantity = arrivalRecord.InventoryQuantity,//已入库
                                    QualifiedQuantity = (arrivalInspectionRecord.Id > 0 ? arrivalInspectionRecord.QualifiedQuantity : 0) * arrivalRecord.Ratio,
                                    ConcessionQuantity = (arrivalInspectionRecord.Id > 0 ? arrivalInspectionRecord.ConcessionQuantity : 0) * arrivalRecord.Ratio,
                                    ReturnQuantity = (arrivalInspectionRecord.Id > 0 ? arrivalInspectionRecord.ReturnQuantity : 0) * arrivalRecord.Ratio,
                                    ReceivableQuantity = waitInventoryQuantity,//待入库
                                    //采购数量
                                    PurchaseBaseQuantity = purchaseDetail.PurchaseQuantity * purchaseDetail.Ratio,

                                    InventoryPrice = inventory.SourceId > 0 ? inventory.Price : null,
                                    TotalPrice = inventory.SourceId > 0 ? inventory.TotalPrice : 0,
                                    PurchaseTotalPrice = purchaseDetail.TotalPrice,

                                    //是否完成：未完成：到货记录未手动完成且待入库数量>0;已完成：到货记录手动完成或者存在入库数量或者（待入库= 必检验：合格数+让步数 - 入库数；不检验：已检验的合格+让步或者未检验的到货-入库 ）数量<=0
                                    IsFinished = !arrivalRecord.IsCompleted && waitInventoryQuantity > 0 ? false :
                                                   (arrivalRecord.IsCompleted  || (waitInventoryQuantity <= 0 && arrivalRecord.InventoryQuantity>0)) ? true
                                                                 : null,
                                    //到货记录的库位信息
                                    PositionName = position.Id > 0 ? $"{position.Location.Area.AreaNo ?? ""}{position.Location.Area.Name}-{position.Location.Name}-{position.Name}" : string.Empty,
                                };

            if (postData.IsPaged)
            {
                var result = await purchaseQuery.ApplyFilters(postData.FilterList).ApplySorts(this.SortInfoList).ToPagedListAsync(this.PageModel);

                //获取物料参数
                var paraIds = result.Data.SelectMany(s => s.ProductCustomAttributeList.Select(x => x.systemParaId)).ToList();
                var paras = await _systemParaService.GetAll(true).Where(x => paraIds.Contains(x.Id)).ToListAsync();
                result.Data = result.Data.Select(s =>
                {
                    s.ProductCustomAttributeInfoList = s.ProductCustomAttributeList.Select(x =>
                    {
                        var para = paras.FirstOrDefault(p => p.Id == x.systemParaId);
                        var info = new CustomAttributeExtension
                        {
                            systemParaId = x.systemParaId,
                            value = x.value,
                            paraName = para != null ? para.DataValue : string.Empty
                        };
                        return info;
                    }).ToList();
                    return s;
                }).ToList();
                return Ok(result);
            }
            var totalData = await purchaseQuery.ApplyFilters(postData.FilterList).ApplySorts(this.SortInfoList).ToListAsync();
            return Ok(totalData);
        }

        public class  PostModifyArrivalStateDTO
        {
            public long Id { get; set; }
            public bool IsComplete { get; set; }
        }

        /// <summary>
        /// 修改到货记录完成状态
        /// </summary>
        /// <param name="Id"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        [HttpPost]
        public async Task<IActionResult> ModifyArrivalState([FromBody] PostModifyArrivalStateDTO postData)
        {
            var arrival = await _purchaseArrivalRecord.GetByIdAsync(postData.Id);
            if (arrival == null)
            {
                throw new Exception("到货记录不存在，请刷新数据");
            }
            arrival.IsCompleted = postData.IsComplete;
            await _unitOfWork.SaveChangeAsync();
            return Ok("保存成功！");
        }

        /// <summary>
        /// 新增采购入库
        /// </summary>
        /// <param name="refs"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> PostPurchaseWarehousingInventoryRecord([FromBody] GetMaxUsedQuantityPositionDTO postData)
        {
            using (var transaction = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                // 1、获取到货记录
                var arrivalRecord = await _purchaseArrivalRecord.GetByIdAsync(postData.ArrivalRecordId);
                _ = arrivalRecord ?? throw new Exception("到货记录不存在");

                if (arrivalRecord.IsCompleted)
                {
                    throw new Exception("到货记录已完成，不能再入库");
                }
                
                //待入库数量
                var inspectionPara = await _systemParaService.GetAll().Where(x => x.DataType == SystemParaType.是否检验通过才能入库).FirstOrDefaultAsync();
                var isInspection = inspectionPara != null && inspectionPara.DataValue == "1" ? true : false;

                // 2、获取采购订单明细
                var purchaseOrderDetail = await _purchaseOrderDetailService.GetAll().Include(x => x.PurchaseOrder).Where(o => o.Id == arrivalRecord.PurchaseOrderDetailId).FirstOrDefaultAsync();
                _ = purchaseOrderDetail ?? throw new Exception("采购订单明细不存在");
                var purchaseOrder = purchaseOrderDetail.PurchaseOrder;
                _ = purchaseOrder ?? throw new Exception("采购订单不存在");

                //采购单价
                var totalPrice = purchaseOrderDetail.TotalPrice;
                var purchaseQuantity = purchaseOrderDetail.PurchaseQuantity * purchaseOrderDetail.Ratio;
                var price = purchaseQuantity != 0 ? Math.Round(totalPrice / purchaseQuantity, 6, MidpointRounding.AwayFromZero) : 0;

                // 3、新增入库记录
                var inventorys = postData.InventoryRecordList.Select(o =>
                {
                    var mainUnit = o.UnitQtyList.FirstOrDefault(x => x.Id == -1);
                    var inputUnit = o.UnitQtyList.FirstOrDefault(x => x.Id == arrivalRecord.ArrivalUnitConversionId);
                    //var unitInfo = o.UnitQtyList.Select(x =>
                    //    new InventoryUnitDto
                    //    {
                    //        UnitName = x.UnitName,
                    //        Id = x.Id,
                    //        UnitId = x.UnitId,
                    //        ConversionType = x.ConversionType,
                    //        Ratio = x.Ratio,
                    //        Remark = x.Remark
                    //    }).DistinctBy(x => x.Id).ToList();
                    var entity = new Inventory_Record_Purchase_Ext
                    {
                        RecordNo = _sequenceService.GenerateCode(TableName.InventoryRecord),
                        InventoryType = InventoryType.入库,
                        SourceType = InventorySourceType.采购入库,
                        SourceId = postData.ArrivalRecordId,
                        DataType = purchaseOrderDetail.DataType,
                        DataId = purchaseOrderDetail.DataId,
                        PositionId = o.PositionId,
                        Quantity = mainUnit != null ? mainUnit.Quantity : 0,
                        Price = price,
                        Remark = o.Remark,
                        InputQuantity = inputUnit != null ? inputUnit.Quantity : 0,
                        SupplierId = purchaseOrder.SupplierId,
                        DataStatus = arrivalRecord.DataStatus,
                        InputUnitConversionId = arrivalRecord.ArrivalUnitConversionId,
                        InventoryUnitInfo = JsonSerializer.Serialize(o.UnitQtyList, JsonHelper.DefaultJsonSerializerOptions),
                        CustomAttribute = JsonSerializer.Serialize(o.CustomAttributeList, JsonHelper.DefaultJsonSerializerOptions),
                    };
                    return entity;
                }).ToList();

                if (inventorys.Any(x => x.Quantity <= 0 || x.InputQuantity <= 0))
                {
                    throw new Exception("请输入入库数量");
                }
                await _inventoryRecordPurchaseExtService.InsertAsync(inventorys);
                await _unitOfWork.SaveChangeAsync();

                lock (QuantityLock.InventoryUpdateLock)
                {
                    // 4、更新到货记录、采购记录、库位容量、物料库存
                    // 4.1、更新到货记录：入库数量、到货完成状态、可入库量
                    // 更新总入库量
                    var arrivalInspection = _arrivalInspectionService.GetAll().Where(o => o.ArrivalRecordId == postData.ArrivalRecordId).FirstOrDefault();
                    if (isInspection && arrivalInspection == null)
                    {
                        throw new Exception("到货记录待检验，请检验后入库");
                    }
                    var inventQuantity = _inventoryRecordService.GetAll()
                                .Where(o => o.SourceType == InventorySourceType.采购入库 &&
                                o.InventoryType == InventoryType.入库
                                && o.SourceId == postData.ArrivalRecordId).Sum(o => o.Quantity);
                    arrivalRecord.InventoryQuantity = inventQuantity;
                    var canInventoryQuantity = 0m;
                    var normalQuantity = 0m;
                    if (arrivalInspection != null)
                    {
                        normalQuantity = (arrivalInspection.QualifiedQuantity + arrivalInspection.ConcessionQuantity) * arrivalRecord.Ratio;
                    }
                    if (!isInspection && arrivalInspection == null)
                    {
                        normalQuantity = arrivalRecord.ArrivalQuantity * arrivalRecord.Ratio;

                    }
                    canInventoryQuantity = normalQuantity - arrivalRecord.InventoryQuantity;
                    if (canInventoryQuantity<0)
                    {
                        throw new Exception("入库数量超过到货记录可入库量");
                    }
                    _unitOfWork.SaveChange();
                    // 4.2、更新采购明细，入库量
                    purchaseOrderDetail.InventoryQuantity = _purchaseArrivalRecord.GetAll()
                                                                .Where(o => o.PurchaseOrderDetailId == purchaseOrderDetail.Id).Sum(o => o.InventoryQuantity);
                    var dataKeys = inventorys.Select(o => new Temp_Inventory_Keys
                    {
                        DataId = o.DataId,
                        DataType = o.DataType,
                    }).ToList();
                    // 4.3、更新物料库存、更新库位已使用量
                    var positionIds = inventorys.Where(o => o.PositionId.HasValue)  // 过滤掉 null 值
                                        .Select(o => (long)o.PositionId!).Distinct().ToList();
                    _inventoryService.UpdateDataInventory(dataKeys, positionIds);
                    _unitOfWork.SaveChange();
                    transaction.Complete();
                    return Ok("保存成功");
                }

            }
        }

        /// <summary>
        /// 获取库存记录关联的所有出入库记录
        /// </summary>
        /// <param name="filters"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> GetInventoryRecordsByArrivalRecord([FromBody] List<Filter> filters)
        {
            var inventoryRecords = await (from inventory in _inventoryRecordService.GetAll().ApplyFilters(filters)
                                   join materiainfo in _productActService.GetMaterialList([])
                                                              on new { ProductId = inventory.DataId, InventoryDataType = inventory.DataType }
                                                              equals new { ProductId = materiainfo.ProductId, InventoryDataType = InventoryDataType.Product }
                                                              into tempmateria
                                   from materiainfo in tempmateria.DefaultIfEmpty()
                                   join semiproductinfo in _productActService.GetSemiProductList([])
                                                    on new { ProductId = inventory.DataId, InventoryDataType = inventory.DataType }
                                                    equals new { ProductId = semiproductinfo.ProductId, InventoryDataType = InventoryDataType.Product }
                                                    into tempsemi
                                   from semiproductinfo in tempsemi.DefaultIfEmpty()
                                   join productinfo in _productActService.GetProductList([], [])
                                                     on new { ProductId = inventory.DataId, InventoryDataType = inventory.DataType }
                                                     equals new { ProductId = productinfo.ProductId, InventoryDataType = InventoryDataType.Product } into tempproduct
                                   from productinfo in tempproduct.DefaultIfEmpty()
                                   join productspeciinfo in _productActService.GetProductList([], [])
                                                on new { ProductId = inventory.DataId, InventoryDataType = inventory.DataType }
                                                equals new
                                                {
                                                    ProductId = productspeciinfo.SpecificationId == null ? (long)0 : productspeciinfo.SpecificationId.Value,
                                                    InventoryDataType = InventoryDataType.Specification
                                                }
                                                into tempproductspeci
                                   from productspeciinfo in tempproductspeci.DefaultIfEmpty()
                                   select new InventoryRecordSearchDTO
                                   {
                                       Id = inventory.Id,
                                       InventRecordNo = inventory.RecordNo,
                                       InventoryType = inventory.InventoryType,
                                       Quantity = inventory.Quantity,
                                       UsedQuantity = inventory.UsedQuantity,
                                       MainUnit = materiainfo.ProductId > 0 ? materiainfo.ProductUnit ?? "" :
                                                                  productinfo.ProductId > 0 ? productinfo.ProductUnit ?? "" :
                                                                      productspeciinfo.ProductId > 0 ? productspeciinfo.ProductUnit ?? "" :
                                                                           semiproductinfo.ProductId > 0 ? semiproductinfo.ProductUnit ?? "" : "",
                                       InventoryPosition = inventory.Position != null ? inventory.Position.Location.Area.Name + "/" + inventory.Position.Location.Name 
                                                                                            + "/" + inventory.Position.Name : "",
                                       Price = inventory.Price,
                                       TotalPrice = inventory.TotalPrice,
                                       Remark = inventory.Remark,
                                       CreateUserName = inventory.CreateUserName,
                                       CreateTime = inventory.CreateTime,
                                   }).ApplySorts(this.SortInfoList).ToPagedListAsync(this.PageModel);
            return Ok(inventoryRecords);
        }

        /// <summary>
        /// 删除入库记录
        /// </summary>
        /// <param name="inventId"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        [HttpGet]
        public async Task<IActionResult> DeletePurchaseInventoryRecord([FromQuery] long id)
        {
            using (var transaction = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                var inventoryRecord = await _inventoryService.GetByIdAsync(id);
                if (inventoryRecord == null)
                {
                    return Ok("删除成功");
                }

                var usedRls = await _inventoryRecordRLService.GetAll().Where(o => o.SourceInventoryId == inventoryRecord.Id).ToListAsync();
                if (usedRls.Any())
                {
                    throw new Exception("该记录已被使用，不能删除");
                }

                var occupyDetails = await _occupyDetailService.GetAll().Where(o => o.InventoryRecordId == inventoryRecord.Id).ToListAsync();
                if(occupyDetails.Any())
                {
                    throw new Exception("该记录已被占用，不能删除");
                }

                var arrivalOrder = _purchaseArrivalRecord.GetAll().Where(o => o.Id == inventoryRecord.SourceId).FirstOrDefault();
                _ = arrivalOrder ?? throw new Exception("到货记录不存在");

                //if (arrivalOrder.IsCompleted)
                //{
                //    throw new Exception("当前到货已入库完成，请先取消完成");
                //}

                var purchaseOrderDetail = _purchaseOrderDetailService.GetAll().Where(o => o.Id == arrivalOrder.PurchaseOrderDetailId).FirstOrDefault();
                _ = purchaseOrderDetail ?? throw new Exception("采购订单明细不存在");

                // 1、删除入库记录
                _inventoryRecordService.Delete(inventoryRecord);
                await _unitOfWork.SaveChangeAsync();
                lock (QuantityLock.InventoryUpdateLock)
                {
                    // 2、更新到货记录入库数量
                    arrivalOrder.InventoryQuantity = _inventoryRecordService.GetAll()
                                                                            .Where(o => o.SourceType == InventorySourceType.采购入库 &&
                                                                            o.SourceId == inventoryRecord.SourceId &&
                                                                            o.InventoryType == InventoryType.入库)
                                                                            .Sum(o => o.Quantity);
                    _unitOfWork.SaveChange();

                    // 3、更新采购明细入库数量
                    purchaseOrderDetail.InventoryQuantity = _purchaseArrivalRecord.GetAll().Where(o => o.PurchaseOrderDetailId == purchaseOrderDetail.Id).Sum(o => o.InventoryQuantity);
                    _unitOfWork.SaveChange();
                    var dataKeys = new List<Temp_Inventory_Keys>()
                                    {
                                        new Temp_Inventory_Keys
                                        {
                                            DataId = inventoryRecord.DataId,
                                            DataType = inventoryRecord.DataType,
                                        }
                                    };
                    // 4、更新物料库存、更新库位已使用量
                    var positionIds = inventoryRecord.PositionId;
                    _inventoryService.UpdateDataInventory(dataKeys, [positionIds ?? 0]);
                    _unitOfWork.SaveChange();
                    transaction.Complete(); // 提交事务
                    return Ok("删除成功");
                }
            }
        }

        #region Dto

        public class FilterArrivalInspectionRecordsPostDTO
        {
            public List<Filter> ArrivalRecordFilterList { get; set; } = new List<Filter>();
            public List<Filter> CommonFilterList { get; set; } = new List<Filter>();
            public bool IsFinished { get; set; } = false;
            public bool IsPaged { get; set; } = false;
        }
        public class GetMaxUsedQuantityPositionDTO
        {
            public long ArrivalRecordId { get; set; }
            /// <summary>
            /// 总入库数量：物料主单位
            /// </summary>
            public decimal TotalQuantity { get; set; }
            public List<PostInventoryRecordDTO> InventoryRecordList { get; set; } = new List<PostInventoryRecordDTO>();
        }


        public class CustomAttributeExtension : CustomAttribute
        {
            public string? paraName { get; set; }
        }
        public class ArrivalInspectionRecordDTO
        {
            public long Id { get; set; }
            public long ArrivalRecordId { get; set; }
            public InspectionConclusion InspectionConclusion { get; set; }
            public string ArrivalRecordNo { get; set; } = string.Empty;
            public string PurchaseOrderNo { get; set; } = string.Empty;
            public string? PurchaseRemark { get; set; }
            public PurchaseType PurchaseType { get; set; }
            public string? ProductNo { get; set; }
            public string ProductName { get; set; } = string.Empty;
            public ProductType ProductType { get; set; }
            public long ProductCategoryId { get; set; }
            public string? ProductCategory { get; set; }
            public string? ProductRemark { get; set; }
            public string? ProductCustomAttribute { get; set; }
            public string? ProductCategoryAttribute { get; set; }
            public List<CustomAttribute> ProductCustomAttributeList
            {
                get
                {
                    var custom = string.IsNullOrEmpty(ProductCustomAttribute) ? new List<CustomAttribute>() : JsonSerializer.Deserialize<List<CustomAttribute>>(ProductCustomAttribute) ?? new List<CustomAttribute>();
                    var category = string.IsNullOrEmpty(ProductCategoryAttribute) ? new List<CustomAttribute>() : JsonSerializer.Deserialize<List<CustomAttribute>>(ProductCategoryAttribute) ?? new List<CustomAttribute>();
                    return custom.Union(category).ToList();
                }
            }
            public List<CustomAttributeExtension> ProductCustomAttributeInfoList { get; set; } = new List<CustomAttributeExtension>();
            public long ProductId { get; set; }
            /// <summary>
            /// 到货单位：到货数量
            /// </summary>
            public decimal SelfArrivalQuantity { get; set; }
            public string? ArrivalUnit { get; set; }
            public long ArrivalUnitId { get; set; }
            public long? ArrivalUnitConversionId { get; set; }
            public decimal Ratio { get; set; }
            public decimal ArrivalQuantity { get; set; }
            public decimal QualifiedQuantity { get; set; }
            public decimal ConcessionQuantity { get; set; }
            public decimal ReturnQuantity { get; set; }
            public decimal InventoryQuantity { get; set; }
            public string ProductUnit { get; set; } = string.Empty;
            public decimal? InventoryPrice { get; set; }
            public decimal TotalPrice { get; set; }
            public string? InspectionReason { get; set; }
            public string? CreateUserName { get; set; }
            public DateTime? CreateTime { get; set; }
            public string? Remark { get; set; }
            public string? CustomAttribute { get; set; }
            public bool IsComplete { get; set; }
            public long? SpecificationId { get; set; }
            public string? MaterialGrade { get; set; }
            public string? ProductTechnicalCode { get; set; }
            public string? ProductPackageWay { get; set; }
            public string? ProductSpecification { get; set; }
            public string SupplierName { get; set; } = string.Empty;
            public decimal ReceivableQuantity { get; set; } 
            public long PositionId { get; set; }
            public DateTime ArrivalCreateTime { get; set; }
            public string? ArrivalCreateUserName { get; set; }
            public DateTime ArrivalTime { get; set; }
            public InventoryDataStatus DataStatus { get; set; }
            public string? UnitInfoString { get; set; }
            public decimal PurchaseBaseQuantity { get; set; }
            public decimal PurchaseTotalPrice { get; set; }
            public decimal PurchasePrice
            {
                get
                {
                    return PurchaseBaseQuantity != 0 ? Math.Round(PurchaseTotalPrice / PurchaseBaseQuantity, 6, MidpointRounding.AwayFromZero) : 0;
                }
            }
            public List<InventoryUnitInboundDto> UnitInfoList
            {
                get
                {
                    var list = !string.IsNullOrEmpty(UnitInfoString) ? JsonSerializer.Deserialize<List<InventoryUnitInboundDto>>(UnitInfoString, JsonHelper.DefaultJsonSerializerOptions) ?? new List<InventoryUnitInboundDto>() :
                                     new List<InventoryUnitInboundDto>();
                    return list.OrderByDescending(x => x.Id == ArrivalUnitConversionId).ToList();

                    //return new List<InventoryUnitDto>
                    //{
                    //    new InventoryUnitDto
                    //    {
                    //        Id = ArrivalUnitConversionId??-1,
                    //        UnitName = ArrivalUnit??"",
                    //        Ratio = Ratio,
                    //        UnitId = ArrivalUnitId,
                    //    }
                    //};
                }
            }
            public List<CustomAttribute> CustomAttributeList
            {
                get
                {
                    return string.IsNullOrEmpty(CustomAttribute) ? new List<CustomAttribute>() : JsonSerializer.Deserialize<List<CustomAttribute>>(CustomAttribute) ?? new List<CustomAttribute>();
                }
            }
            /// <summary>
            /// 入库完成状态：为空表示到货记录不能在采购入库页面查询，无法入库
            /// </summary>
            public bool? IsFinished { get; set; }

            public string? PositionName { get; set; }
        }




        public class PostInventoryRecordDTO
        {
            public long? PositionId { get; set; }
            //数量信息
            public List<InventoryUnitInboundDto> UnitQtyList { get; set; } = new List<InventoryUnitInboundDto>();
            public string? Remark { get; set; }
            public List<CustomAttribute> CustomAttributeList { get; set; } = new List<CustomAttribute>();
        }

        public class InventoryRecordSearchDTO
        {
            public long Id { get; set; }
            public string InventRecordNo { get; set; } = string.Empty;
            public InventoryType InventoryType { get; set; }
            public decimal Quantity { get; set; }
            public decimal UsedQuantity { get; set; }
            public string MainUnit { get; set; } = string.Empty;
            public decimal Price { get; set; }
            public decimal TotalPrice { get; set; }
            public string InventoryPosition { get; set; } = string.Empty;
            public string? CreateUserName { get; set; }
            public DateTime? CreateTime { get; set; }
            public string? Remark { get; set; }
        }

        public class Ids { 
            public long Id { get; set; }
        }
        #endregion
    }
}
