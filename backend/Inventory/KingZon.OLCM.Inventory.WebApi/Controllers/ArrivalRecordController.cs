using Kingzon.OLCM.Base.Services;
using Kingzon.OLCM.Base.Services.JsonStructure;
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
using KingZon.OLCM.Purchase.DataBase.Entities;
using KingZon.OLCM.Purchase.DataBase.Enum;
using KingZon.OLCM.Quality.Shared.DataBase.Entities;
using KingZon.OLCM.Sale.Shared.DataBase.JsonStructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Transactions;
using System.Xml;

namespace KingZon.OLCM.Inventory.WebApi.Controllers
{
    [Route("api/inventory/[controller]/[action]")]
    public class ArrivalRecordController : KingZonControllerBase
    {
        private readonly CommonService<PurchaseArrivalRecord> _purchase_arrivalRecordService;
        private readonly CommonService<Purchase_Order_Detail> _purchaseOrderDetailService;
        private readonly CommonService<Purchase_Order> _purchaseOrderService;
        private readonly CommonService<Base_Product> _productService;
        private readonly ProductService _productActService;
        private readonly CommonService<Quality_ArrivalInspection> _arrivalInspectionService;
        private readonly CommonService<Quality_Abnormal> _abnormalService;
        private readonly CommonService<Inventory_Record> _inventoryService;
        private readonly CommonService<Inventory_Record_Purchase_Ext> _inventoryRecordPurchaseExtService;
        private readonly CommonService<Inventory_Position> _positionService;
        private readonly CommonService<Purchase_Order_Demand_RL> _purchaseDemandRLService;
        private readonly InventoryService _inventory_Service;
        private readonly SystemParaService _systemParaService;
        private readonly SequenceService _sequenceService;
        private readonly IUnitOfWork _unitOfWork;

        public ArrivalRecordController(
            CommonService<PurchaseArrivalRecord> purchase_arrivalRecordService,
            CommonService<Purchase_Order_Detail> purchaseOrderDetailService,
            CommonService<Purchase_Order> purchaseOrderService,
            CommonService<Base_Product> productService,
            ProductService productActService,
            CommonService<Quality_ArrivalInspection> arrivalInspectionService,
            CommonService<Quality_Abnormal> abnormalService,
            CommonService<Inventory_Record> inventoryService,
            CommonService<Inventory_Record_Purchase_Ext> inventoryRecordPurchaseExtService,
            CommonService<Inventory_Position> positionService,
            CommonService<Purchase_Order_Demand_RL> purchasedemandRLService,
            InventoryService inventory_Service,
            SystemParaService systemParaService,
            SequenceService sequenceService,
            IUnitOfWork unitOfWork
         )
        {
            _purchase_arrivalRecordService = purchase_arrivalRecordService;
            _purchaseOrderDetailService = purchaseOrderDetailService;
            _purchaseOrderService = purchaseOrderService;
            _productService = productService;
            _productActService = productActService;
            _arrivalInspectionService = arrivalInspectionService;
            _abnormalService = abnormalService;
            _inventoryService = inventoryService;
            _inventoryRecordPurchaseExtService = inventoryRecordPurchaseExtService;
            _positionService = positionService;
            _purchaseDemandRLService = purchasedemandRLService;
            _inventory_Service = inventory_Service;
            _systemParaService = systemParaService;
            _sequenceService = sequenceService;
            _unitOfWork = unitOfWork;
        }

        [HttpPost]
        public async Task<IActionResult> GetAll([FromBody] GetAllArrivalRecordPostDto postData)
        {
            var q = from arrivalRecord in _purchase_arrivalRecordService.GetAll().ApplyFilters(postData.ArrivalRecordFilterList)
                    join purchaseOrderDetail in _purchaseOrderDetailService.GetAll().ApplyFilters(postData.PurchaseOrderFilterList)
                        on arrivalRecord.PurchaseOrderDetailId equals purchaseOrderDetail.Id
                    join product in _productService.GetAll().ApplyFilters(postData.ProductFilterList) on
                        new
                        {
                            DataType = purchaseOrderDetail.DataType,
                            DataId = purchaseOrderDetail.DataId
                        }
                        equals new
                        {
                            DataType = InventoryDataType.Product,
                            DataId = product.Id
                        }
                    join arrivalInspection in _arrivalInspectionService.GetAll()
                        on arrivalRecord.Id equals arrivalInspection.ArrivalRecordId into arrivalInspectionTemple
                    from arrivalInspection in arrivalInspectionTemple.DefaultIfEmpty()
                    where arrivalRecord.IsInspection == postData.IsInspection
                    select new GetAllArrivalRecordDto()
                    {
                        ArrivalRecordId = arrivalRecord.Id,
                        ArrivalRecordNo = arrivalRecord.ArrivalRecordNo,
                        PurchaseOrderNo = purchaseOrderDetail.PurchaseOrder.OrderNo,
                        PurchaseType = purchaseOrderDetail.PurchaseOrder.PurchaseType,
                        ProductNo = product.ProductNo,
                        ProductName = product.ProductName,
                        ProductCategoryName = product.ProductCategory.Name,
                        MaterialGrade = product.MaterialGrade,
                        MaterialSpecification = product.MaterialSpecification,
                        CategoryAttribute = product.CategoryAttribute,
                        ArrivalQuantity = arrivalRecord.ArrivalQuantity,
                        //已检验的页面需要显示的数据
                        //三元表达式 isInspection ? A :B 如果isInspection为true取A，如果为false取B
                        InspectionResult = postData.IsInspection ? arrivalInspection.InspectionResult : "",
                        AbnormalQuantity = postData.IsInspection ? arrivalInspection.AbnormalQuantity : 0,
                        ScrapQuantity = postData.IsInspection ? arrivalInspection.ScrapQuantity : 0,
                        ReturnQuantity = postData.IsInspection ? arrivalInspection.ReturnQuantity : 0,
                        QualifiedQuantity = postData.IsInspection ? arrivalInspection.QualifiedQuantity : 0,
                        Unit = arrivalRecord.ArrivalUnit

                    };
            var result = await q.ToPagedListAsync(this.PageModel);
            return Ok(result);
        }
        /// <summary>
        /// 检验页面需要获取的数据
        /// </summary>
        /// <param name="arrivalRecordId"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<IActionResult> PostInspectionPageMessage([FromQuery] long arrivalRecordId)
        {
            var q = from arrivalRecord in _purchase_arrivalRecordService.GetAll()
                    join purchaseOrderDetail in _purchaseOrderDetailService.GetAll() on
                    arrivalRecord.PurchaseOrderDetailId equals purchaseOrderDetail.Id
                    join product in _productService.GetAll() on
                    new
                    {
                        DataType = purchaseOrderDetail.DataType,
                        DataId = purchaseOrderDetail.DataId
                    }
                    equals new
                    {
                        DataType = InventoryDataType.Product,
                        DataId = product.Id
                    }
                    where arrivalRecord.Id == arrivalRecordId
                    select new GetAllArrivalRecordDto()
                    {
                        ArrivalRecordNo = arrivalRecord.ArrivalRecordNo,
                        Unit = arrivalRecord.ArrivalUnit,
                        PurchaseOrderNo = purchaseOrderDetail.PurchaseOrder.OrderNo,
                        ProductNo = product.ProductNo,
                        ProductName = product.ProductName,
                        ProductCategoryName = product.ProductCategory.Name,
                        MaterialGrade = product.MaterialGrade,
                        MaterialSpecification = product.MaterialSpecification,
                        ArrivalQuantity = arrivalRecord.ArrivalQuantity,
                    };
            var result = await q.FirstOrDefaultAsync();
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> CreateInspectionResult([FromBody] CreateInspectionResultPostDto postData)
        {
            //定义一个事务
            using var transaction = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
            var arrvalRecord = _purchase_arrivalRecordService.GetById(postData.ArrivalRecordId);
            if (arrvalRecord == null)
            {
                throw new Exception("到货记录未找到，可能已被删除，请刷新页面后重试");
            }
            arrvalRecord.IsInspection = true;
            //在质检结果表里新增数据
            var inspectionResult = new Quality_ArrivalInspection()
            {
                ArrivalRecordId = postData.ArrivalRecordId,
                InspectionResult = postData.CheckValue,
                QualifiedQuantity = postData.QualifiedQuantity,
                Remark = postData.InspectionRemark,
                ReturnQuantity = postData.ReturnQuantity,
                AbnormalQuantity = postData.AbnormalQuantity,
                ScrapQuantity = postData.ScrapQuantity,
            };
            _arrivalInspectionService.Insert(inspectionResult);
            await _unitOfWork.SaveChangeAsync();
            //在异常表中插入数据
            if (postData.AbnormalQuantity > 0)
            {
                var abnormal = new Quality_Abnormal()
                {
                    SourceType = Quality.Shared.DataBase.Enum.AbnormalSourceType.检验异常,
                    SourceId = inspectionResult.Id,
                    Remark = postData.AbnormalRemark,
                    AbnormalQuantity = postData.AbnormalQuantity
                };
                _abnormalService.Insert(abnormal);
            }
            //在报废表中新增
            if (postData.ScrapQuantity > 0)
            {
                var scrap = new Quality_Scrap()
                {
                    SourceType = Quality.Shared.DataBase.Enum.ScrapSourceType.检验报废,
                    SourceId = inspectionResult.Id,
                    Remark = postData.ScrapRemark,
                    ScrapQuantity = postData.ScrapQuantity
                };

            }
            //在退货表中新增
            if (postData.ReturnQuantity > 0)
            {
                var back = new Purchase_ReturnOrderDetail()
                {
                    SourceType = Purchase.DataBase.Enum.ReturnOrderDetailSourceType.检验退货,
                    SourceId = inspectionResult.Id,
                    ReturnQuantity = postData.ReturnQuantity
                };

            }
            var result = await _unitOfWork.SaveChangeAsync();
            //调用这个方法，事务完成
            transaction.Complete();
            return Ok(result);
        }

        /// <summary>
        /// 获取采购明细
        /// </summary>
        /// <param name="dto"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> FilterPurchaseOrderDetails([FromBody] FilterPurchaseOrderDetailsPostDTO dto)
        {
            var purchaseDetails = _purchaseOrderDetailService.GetAll().Where(o => o.IsComplete == false).ApplyFilters(dto.PurchaseOrderDetailFilterList ?? []);
            // 1、获取最大到货量
            var limitInfo = await _systemParaService.GetAll().Where(x => x.DataType == SystemParaType.采购到货偏差值).FirstOrDefaultAsync();
            var limit = limitInfo != null ? Convert.ToDecimal(limitInfo.DataValue) : 0.05m;
            // 2、求当前到货量
            var groupQuantity = (from arrival in _purchase_arrivalRecordService.GetAll()
                                 join inspection in _arrivalInspectionService.GetAll()
                                        on arrival.Id equals inspection.ArrivalRecordId
                                        into inspectionTemp
                                 from inspection in inspectionTemp.DefaultIfEmpty()
                                 where arrival.PurchaseOrderDetailId.HasValue
                                 let quantity = inspection != null ? (inspection.QualifiedQuantity + inspection.ConcessionQuantity) *  arrival.Ratio : arrival.BaseArrivalQuantity
                                 group new { arrival, quantity } by arrival.PurchaseOrderDetailId into g
                                 select new
                                 {
                                     Id = g.Key,
                                     NormalQuantity = g.Sum(o => o.quantity),
                                     ArrivalQuantity = g.Sum(o => o.arrival.BaseArrivalQuantity) // 没有检验功能,暂时将所有到货量作为,可入库量
                                                                                               //ArrivalQuantity = g.Sum(o =>o.ReceivableQuantity) // 可入库量 = 实际到货量
                                 });
            var details = from detail in purchaseDetails
                          join productinfo in _productActService.GetProductList([], [])
                                            on new { ProductId = detail.DataId, InventoryDataType = detail.DataType }
                                            equals new
                                            {
                                                ProductId = productinfo.SpecificationId > 0 ? (productinfo.SpecificationId ?? 0) : productinfo.ProductId,
                                                InventoryDataType = productinfo.SpecificationId > 0 ? InventoryDataType.Specification : InventoryDataType.Product
                                            } into tempproduct
                          from productinfo in tempproduct.DefaultIfEmpty()
                          join quantity in groupQuantity on detail.Id equals quantity.Id into tempquantity
                          from quantity in tempquantity.DefaultIfEmpty()
                          select new PurchaseOrderDetailDTO
                          {
                              IdField = detail.Id,
                              PurchaseOrderId = detail.PurchaseOrderId,
                              PurchaseOrderDetailId = detail.Id,
                              PurchaseOrderNo = detail.PurchaseOrder.OrderNo,
                              PurchaseType = detail.PurchaseType,
                              SupplierId = detail.PurchaseOrder.SupplierId,
                              SupplierName = detail.PurchaseOrder.Supplier.CustomerName,
                              ProductNo = productinfo.ProductId > 0 ? productinfo.ProductNo : string.Empty,
                              ProductName = productinfo.ProductId > 0 ? productinfo.ProductName ?? "" : string.Empty,
                              ProductId = productinfo.ProductId > 0 ? productinfo.ProductId : 0,
                              SpecificationId = detail.DataType == InventoryDataType.Specification ? detail.DataId : 0,
                              ProductSpecification = productinfo.ProductId > 0 ? productinfo.ProductType == ProductType.物料 ?
                                                            productinfo.MaterialSpecification : productinfo.ProductSpecification
                                                                : string.Empty, // 规格型号
                              ProductType = productinfo.ProductId > 0 ? productinfo.ProductType : ProductType.物料,     // 类型
                              ProductCategory = productinfo.ProductId > 0 ? productinfo.ProductCategory : string.Empty, // 产品类别
                              ProductCategoryId = productinfo.ProductId > 0 ? productinfo.ProductCategoryId ?? 0 : 0,   // 产品类别Id
                              // 基础单位，原材料、半成品和成品取ProductUnit，成品规格取productUnit
                              MainUnit = productinfo.ProductId > 0 ? productinfo.ProductUnit ?? "" : string.Empty,
                              PurchaseUnitId = detail.PurchaseUnitId,
                              PurchaseUnit = detail.PurchaseUnit,
                              // 产品主单位 = 采购单位，只显示产品主单位
                              ProductUnit = productinfo.ProductId > 0 ? productinfo.ProductUnit:string.Empty,
                              PurchaseUnitConversionId = detail.PurchaseUnitConversionId,
                              PurchaseQuantity = detail.PurchaseQuantity * detail.Ratio,
                              MaxPurchaseQuantity = detail.PurchaseQuantity * detail.Ratio * (1 + limit),
                              AlreadyArrivalQuantity = quantity.Id > 0 ? quantity.ArrivalQuantity : 0,
                              // 合格到货量
                              QualifiedArrivalQuantity = quantity.Id > 0 ? quantity.NormalQuantity : 0,
                              AlreadyInventoryQuantity = detail.InventoryQuantity,
                              CustomAttribute = detail.CustomAttribute,
                              MaterialGrade = productinfo.ProductId > 0 ? productinfo.MaterialGrade : string.Empty,
                              ProductPackageWay = productinfo.ProductId > 0 ? productinfo.ProductPackageWay : string.Empty,
                              ProductTechnicalCode = productinfo.ProductId > 0 ? productinfo.ProductTechnicalCode : string.Empty,
                              Selected = false,
                              Ratio = detail.Ratio,
                              ProductRemark = productinfo.ProductId > 0 ? productinfo.Remark : string.Empty,
                              DetailRemark = detail.Remark,
                              CreateUserName = detail.CreateUserName,
                              CreateTime = detail.CreateTime,
                              PurchaseUnitInfo = detail.UnitConversionList,
                          };
            if (dto.IsPaged)
            {
                var result = await details.ApplyFilters(dto.CommonFilterList).ApplySorts(this.SortInfoList).ToPagedListAsync(this.PageModel);
                return Ok(result);
            }
            var resultTemp = await details.ApplySorts(this.SortInfoList).ToListAsync();
            return Ok(resultTemp);
        }

        /// <summary>
        /// 查询到货记录
        /// </summary>
        /// <param name="dto"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> FilterArrivalRecords([FromBody] List<Filter> filterList)
        {
            var inspectionPara = await _systemParaService.GetAll().Where(x => x.DataType == SystemParaType.是否检验通过才能入库).FirstOrDefaultAsync();
            var isInspection = inspectionPara != null && inspectionPara.DataValue == "1" ? true : false;

            //var purchases = _purchaseOrderDetailService.GetAll();
            var details = from arrival in _purchase_arrivalRecordService.GetAll()
                          join detail in _purchaseOrderDetailService.GetAll()
                                    on arrival.PurchaseOrderDetailId equals detail.Id
                                    into purchaseTemp
                          from detail in purchaseTemp.DefaultIfEmpty()
                          join productinfo in _productActService.GetProductList([], [])
                                            on new { ProductId = detail.DataId, InventoryDataType = detail.DataType }
                                            equals new
                                            {
                                                ProductId = productinfo.SpecificationId > 0 ? (productinfo.SpecificationId ?? 0) : productinfo.ProductId,
                                                InventoryDataType = productinfo.SpecificationId > 0 ? InventoryDataType.Specification : InventoryDataType.Product
                                            } into tempproduct
                          from productinfo in tempproduct.DefaultIfEmpty()
                          join inspection in _arrivalInspectionService.GetAll()
                                    on arrival.Id equals inspection.ArrivalRecordId 
                                    into inspectionTemp
                          from inspection in inspectionTemp.DefaultIfEmpty()
                          join position in _positionService.GetAll()
                                on arrival.PositionId equals position.Id
                                into positionTemp
                          from position in positionTemp.DefaultIfEmpty()
                          let waitInventoryQuantity = inspection.Id > 0 ? ((inspection.QualifiedQuantity + inspection.ConcessionQuantity) * arrival.Ratio - arrival.InventoryQuantity) :
                                                                  !isInspection ? arrival.BaseArrivalQuantity - arrival.InventoryQuantity :
                                                                        0
                          select new ArrivalRecordDto
                          {
                              InsepctionReason = inspection.Id>0?inspection.InspectionResult:string.Empty,
                              Id = arrival.Id,
                              RecordNo = arrival.ArrivalRecordNo,
                              SourceId = arrival.PurchaseOrderDetailId,
                              PurchaseOrderNo = detail.Id > 0 ? detail.PurchaseOrder.OrderNo : string.Empty,
                              OrderNo = detail.Id > 0 ? detail.PurchaseOrder.OrderNo : string.Empty,
                              ProductNo = productinfo.ProductId > 0 ? productinfo.ProductNo : string.Empty,
                              ProductName = productinfo.ProductId > 0 ? productinfo.ProductName ?? "" : string.Empty,
                              ProductId = productinfo.ProductId > 0 ? productinfo.ProductId : 0,
                              ProductTechnicalCode = productinfo.ProductId > 0 ? productinfo.ProductTechnicalCode : string.Empty,
                              PurchaseType = detail.Id > 0 ? detail.PurchaseType : PurchaseType.直接采购,
                              ProductType = productinfo.ProductId > 0 ? productinfo.ProductType : ProductType.物料,
                              ProductCategoryId = productinfo.ProductId > 0 ? productinfo.ProductCategoryId ?? 0 : 0,
                              ProductCategory = productinfo.ProductId > 0 ? productinfo.ProductCategory : string.Empty,
                              ProductRemark = productinfo.ProductId > 0 ? productinfo.Remark : string.Empty,
                              MaterialGrade = productinfo.ProductId > 0 ? productinfo.MaterialGrade : string.Empty,
                              ProductPackageWay = productinfo.ProductId > 0 ? productinfo.ProductPackageWay : string.Empty,
                              ProductSpecification = productinfo.ProductId > 0 ? productinfo.ProductType == ProductType.物料 ?
                                                            productinfo.MaterialSpecification : productinfo.ProductSpecification
                                                                : string.Empty,
                              SpecificationId = productinfo.SpecificationId > 0 ? productinfo.SpecificationId : 0,
                              SupplierName = detail.Id > 0 ? detail.PurchaseOrder.Supplier.CustomerName : string.Empty,
                              SupplierId = detail.Id > 0 ? detail.PurchaseOrder.SupplierId : 0,
                              InspectionConclusion = arrival.InspectionConclusion,
                              MainUnit = productinfo.ProductId > 0 ? productinfo.ProductUnit ?? "" : string.Empty,
                              ArrivalQuantity = arrival.ArrivalQuantity,
                              BaseArrivalQuantity = arrival.BaseArrivalQuantity,
                              ArrivalUnit = arrival.ArrivalUnit,
                              ArrivalUnitConversionId = arrival.ArrivalUnitConversionId,
                              ArrivalUnitId = arrival.ArrivalUnitId,
                              Ratio = arrival.Ratio,
                              UnitConversionListString = arrival.UnitConversionList,
                              AlreadyInventoryQuantity = arrival.InventoryQuantity,
                              CreateUserName = arrival.CreateUserName,
                              CreateTime = arrival.CreateTime,
                              ArrivalTime = arrival.ArrivalTime,
                              Remark = arrival.Remark,
                              CustomAttribute = arrival.CustomAttribute,
                              //到货检验保存多单位后再优化
                              ReturnQuantity = (inspection.Id > 0 ? inspection.ReturnQuantity : 0) * arrival.Ratio,
                              QualifiedQuantity = (inspection.Id > 0 ? inspection.QualifiedQuantity : 0) * arrival.Ratio,
                              ConcessionQuantity = (inspection.Id > 0 ? inspection.ConcessionQuantity : 0) * arrival.Ratio,
                              AbnormalQuantity = (inspection.Id > 0 ? inspection.AbnormalQuantity : 0) * arrival.Ratio,
                              ScrapQuantity = (inspection.Id > 0 ? inspection.ScrapQuantity : 0) * arrival.Ratio,
                              DataStatus = arrival.DataStatus == InventoryDataStatus.None ? null : arrival.DataStatus,
                              PositionName = position.Id > 0 ? (position.Location.Area.AreaNo ?? "") + position.Location.Area.Name + "-" + position.Location.Name + "-" + position.Name : string.Empty,
                              PurchaseRemark = detail.Id > 0 ? detail.Remark : string.Empty,
                              //是否完成：未完成：到货记录未手动完成且待入库数量>0;已完成：到货记录手动完成或者存在入库数量或者（待入库= 必检验：合格数+让步数 - 入库数；不检验：已检验的合格+让步或者未检验的到货-入库 ）数量<=0
                              IsFinished = !arrival.IsCompleted && waitInventoryQuantity > 0 ? false :
                                                   (arrival.IsCompleted || (waitInventoryQuantity <= 0 && arrival.InventoryQuantity > 0)) ? true
                                                                 : null,
                          };
            var result = await details.ApplyFilters(filterList).ApplySorts(this.SortInfoList).ToPagedListAsync(this.PageModel);
            return Ok(result);
        }
        /// <summary>
        /// 新增到货记录,采购
        /// </summary>
        /// <param name="arrivalRecords"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        [HttpPost]
        public async Task<IActionResult> SaveArrivalRecords([FromBody] List<PostArrivalRecordDto> arrivalRecords)
        {
            using (var transaction = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                _ = arrivalRecords ?? throw new Exception("新增失败，到货记录不能为空");
                var paras = await _systemParaService.GetAll().Where(x => x.DataType == SystemParaType.采购到货偏差值 || x.DataType == SystemParaType.是否到货时自动生成入库记录).ToListAsync();
                // 1、获取最大到货量
                var limitInfo = paras.Where(x=>x.DataType == SystemParaType.采购到货偏差值).FirstOrDefault();
                var limit = limitInfo != null ? Convert.ToDecimal(limitInfo.DataValue) : 0.05m;

                var isInvent = paras.Where(x => x.DataType == SystemParaType.是否到货时自动生成入库记录).FirstOrDefault();
                var isInventTag = isInvent != null && isInvent.DataValue == "1";


                if (isInventTag && arrivalRecords.Any(o => o.PositionId == 0 || !o.PositionId.HasValue))
                {
                    throw new Exception("请选择库位，库位不能为空");
                }

                // 2、采购明细
                var detailId = arrivalRecords.Select(o => o.PurchaseOrderDetailId).ToList();
                var purchaseDetails = await _purchaseOrderDetailService.GetAll().Include(x=>x.PurchaseOrder).Where(o => detailId.Contains(o.Id)).ToListAsync();
                var demadRLs = await _purchaseDemandRLService.GetAll().Where(o => detailId.Contains(o.PurchaseOrderDetailId)).Include(o=>o.Purchase_Demand).ToListAsync();
                if (purchaseDetails == null) throw new Exception("当前采购明细不存在，请刷新页面");
                var preArrivalRecord = await (from arrival in _purchase_arrivalRecordService.GetAll()
                                              where detailId.Contains(arrival.PurchaseOrderDetailId ?? 0)
                                              join inspection in _arrivalInspectionService.GetAll()
                                                   on arrival.Id equals inspection.ArrivalRecordId into inspectionTemp
                                              from inspection in inspectionTemp.DefaultIfEmpty()
                                              let quantity = inspection != null ? (inspection.QualifiedQuantity + inspection.ConcessionQuantity): arrival.ArrivalQuantity 
                                              select new
                                              {
                                                  arrival,
                                                  inspection,
                                                  quantity = quantity * arrival.Ratio,
                                              }).ToListAsync();
                var postList = new List<PurchaseArrivalRecord>();
                var inventorysList = new List<Inventory_Record_Purchase_Ext>();

               
                // 3、生成到货记录
                foreach (var arrivalRecord in arrivalRecords)
                {
                    var info = purchaseDetails.FirstOrDefault(x => x.Id == arrivalRecord.PurchaseOrderDetailId);
                    if (info == null)
                    {
                        throw new Exception($"采购明细不存在，请刷新数据");
                    }
                    if (info.IsComplete)
                    {
                        var samePurchaseArrival = arrivalRecords.Where(o => o.PurchaseOrderDetailId == info.Id && o != arrivalRecord).ToList();
                        if (samePurchaseArrival.Count <= 0 || samePurchaseArrival.All(e => !e.IsComplete))
                        {
                            throw new Exception($"采购明细【{info.PurchaseOrder.OrderNo}】的物料{info.ProductName}已完成到货，不能再新增到货记录");
                        }
                    }

                    // 本次到货量 + 已到货量<= maxQuantity
                    // 求每个detail的总已到货量，每个detail的最大到货量
                    var totalQuantity = preArrivalRecord.Where(o => o.arrival.PurchaseOrderDetailId == info.Id).Sum(x => x.quantity);
                    var maxQuantity = info.PurchaseQuantity * info.Ratio * (1 + limit);
                    
                    if ((arrivalRecord.BaseArrivalQuantity) + totalQuantity > maxQuantity)
                    {
                        throw new Exception($"订单【{info.PurchaseOrder.OrderNo}】的物料总到货数量超出最大可到货限制{maxQuantity}");
                    }
                    //同时手动完成到货
                    if (arrivalRecord.IsComplete)
                    {
                        info.IsComplete = true;
                        info.CompleteTime = DateTime.Now;
                    }
                    //新增到货计量
                    var entity = new PurchaseArrivalRecord()
                    {
                        ArrivalRecordNo = _sequenceService.GenerateCode(TableName.Inventory_ArrivalRecord),
                        ArrivalTime = arrivalRecord.ArrivalTime,
                        ArrivalQuantity = arrivalRecord.ArrivalQuantity,
                        BaseArrivalQuantity = arrivalRecord.BaseArrivalQuantity,
                        InspectionConclusion = InspectionConclusion.合格入库,
                        Remark = arrivalRecord.Remark,
                        PurchaseOrderDetailId = arrivalRecord.PurchaseOrderDetailId,
                        CustomAttribute = JsonSerializer.Serialize(arrivalRecord.CustomAttributeList.DistinctBy(x=>x.systemParaId),JsonHelper.DefaultJsonSerializerOptions),
                        ArrivalUnit = arrivalRecord.ArrivalUnit ?? "",
                        ArrivalUnitId = arrivalRecord.ArrivalUnitId,
                        ArrivalUnitConversionId = arrivalRecord.ArrivalUnitConversionId ?? -1,
                        UnitConversionList = JsonSerializer.Serialize(arrivalRecord.UnitInfoList.DistinctBy(x => x.Id), JsonHelper.DefaultJsonSerializerOptions),
                        Ratio = arrivalRecord.Ratio,
                        PositionId = arrivalRecord.PositionId ?? 0,
                        //IsCompleted = isInventTag,
                        InventoryQuantity = isInventTag ? arrivalRecord.ArrivalQuantity * arrivalRecord.Ratio : 0,
                        DataStatus = arrivalRecord.DataStatus,
                    };
                    if (entity.BaseArrivalQuantity <= 0 || entity.ArrivalQuantity <= 0)
                    {
                        throw new Exception($"采购明细【{info.PurchaseOrder.OrderNo}】的物料主单位转换异常");
                    }
                    postList.Add(entity);

                }
                await _purchase_arrivalRecordService.InsertAsync(postList);
                await _unitOfWork.SaveChangeAsync();

                // 5、生成入库记录
                if (isInventTag)
                {
                    foreach (var post in postList)
                    {
                        var temp = purchaseDetails.FirstOrDefault(x=>x.Id == post.PurchaseOrderDetailId);
                        if (temp == null)
                        {
                            throw new Exception($"采购明细不存在，请刷新数据");
                        }
                        //采购单价
                        var price = (temp.PurchaseQuantity * temp.Ratio) != 0 ? temp.TotalPrice / (temp.PurchaseQuantity * temp.Ratio) : 0;
                        //var unitList = JsonSerializer.Deserialize<List<InventoryUnitInboundDto>>(post.UnitConversionList ?? "[]", JsonHelper.DefaultJsonSerializerOptions) ?? new List<InventoryUnitInboundDto>();
                        //var mainUnit = unitList.FirstOrDefault(x => x.Id == -1);
                        //if (mainUnit == null)
                        //{
                        //    throw new Exception($"采购明细【{temp.PurchaseOrder.OrderNo}】的物料主单位转换异常");
                        //}
                        // 到货时生成入库记录
                        var inventorys = new Inventory_Record_Purchase_Ext()
                        {
                            RecordNo = _sequenceService.GenerateCode(TableName.InventoryRecord),
                            InventoryType = InventoryType.入库,
                            PositionId = post.PositionId,
                            Quantity = post.BaseArrivalQuantity,
                            InputQuantity = post.ArrivalQuantity,
                            InventoryUnitInfo = post.UnitConversionList,
                            DataType = temp.DataType,
                            DataId = temp.DataId,
                            SourceType = InventorySourceType.采购入库,
                            SourceId = post.Id,
                            SupplierId = temp.PurchaseOrder.SupplierId,
                            Remark = post.Remark,
                            DataStatus = post.DataStatus,
                            CustomAttribute = post.CustomAttribute,
                            Price = price,
                            InputUnitConversionId = post.ArrivalUnitConversionId,
                        };
                        inventorysList.Add(inventorys);
                    }
                    // 4.2 插入入库记录
                    await _inventoryRecordPurchaseExtService.InsertAsync(inventorysList);
                    await   _unitOfWork.SaveChangeAsync();

                    lock (QuantityLock.InventoryUpdateLock)
                    {
                        // 获取所有采购明细的到货记录，计算每个采购明细的入库量
                        var arrivals = _purchase_arrivalRecordService.GetAll().Where(o => detailId.Contains(o.PurchaseOrderDetailId??0)).ToList();
                        var arrivalIds = arrivals.Select(o => o.Id).ToList();
                        var inventorys = _inventoryService.GetAll().Where(o => o.SourceType == InventorySourceType.采购入库 && o.InventoryType == InventoryType.入库
                        && arrivalIds.Contains(o.SourceId)).ToList();
                        foreach (var item in purchaseDetails)
                        {
                            var currentIds = arrivals.Where(x => x.PurchaseOrderDetailId == item.Id).Select(x => x.Id).ToList();
                            foreach (var arrival in arrivals.Where(x => x.PurchaseOrderDetailId == item.Id))
                            {
                                arrival.InventoryQuantity = inventorys.Where(o => o.SourceId == arrival.Id).Sum(o => o.Quantity);
                            }
                            item.InventoryQuantity = inventorys.Where(o => currentIds.Contains(o.SourceId)).Sum(o => o.Quantity);
                        }

                        var dataKeys = inventorysList.Select(o => new Temp_Inventory_Keys
                        {
                            DataId = o.DataId,
                            DataType = o.DataType,
                        }).ToList();
                        // 4.3、更新物料库存、更新库位已使用量
                        var positionIds = inventorysList.Where(o => o.PositionId.HasValue)  // 过滤掉 null 值
                                            .Select(o => (long)o.PositionId!).Distinct().ToList();
                        _inventory_Service.UpdateDataInventory(dataKeys, positionIds);
                        _unitOfWork.SaveChange();
                        transaction.Complete();
                        return Ok("保存成功");
                    }
                }
                transaction.Complete();
                return Ok("保存成功");
            }
        }

        /// <summary>
        /// 删除到货记录
        /// </summary>
        /// <param name="arrivalRecords"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> DeleteArrivalRecord([FromBody] ArrivalRecordDto dto)
        {
            var arrivalRecord = await _purchase_arrivalRecordService.GetByIdAsync(dto.Id);
            if (arrivalRecord == null)
            {
                return Ok("删除成功");
            }
            var inventorys = await _inventoryService.GetAll().Where(o => o.SourceId == dto.Id &&
                                (o.SourceType == InventorySourceType.采购入库)).ToListAsync();
            if (inventorys.Any())
            {
                throw new Exception("删除失败，该到货记录已入库");
            }
            var arrivalInspectionRecord = await _arrivalInspectionService.GetAll().FirstOrDefaultAsync(o => o.ArrivalRecordId == dto.Id);
            if (arrivalInspectionRecord != null)
            {
                throw new Exception("删除失败，该到货记录已检验");
            }
            //var returnDetail = await _purchaseReturnOrderDetailService.GetAll().FirstOrDefaultAsync(o => o.PurchaseReturnSourceId == delData.Id && o.PurchaseReturnSourceType == ReturnSourceType.不合格退货);
            //if (returnDetail != null)
            //{
            //    throw new Exception("删除失败，该到货记录已退货");
            //}
            // 删除到货
            _purchase_arrivalRecordService.Delete(arrivalRecord);
            await _unitOfWork.SaveChangeAsync();
            // 更新采购明细
            var purchaseDetail = await _purchaseOrderDetailService.GetByIdAsync(arrivalRecord.PurchaseOrderDetailId);
            if (purchaseDetail == null)
            {
                throw new Exception("采购明细不存在，请刷新数据");
            }
            if (purchaseDetail.IsComplete)
            {
                throw new Exception("采购明细已完成，不能直接删除");
            }
            //if (purchaseDetail.IsComplete)
            //{
            //    purchaseDetail.IsComplete = purchaseDetail.PurchaseQuantity <= totalQuantity;
            //    purchaseDetail.CompleteTime = purchaseDetail.IsComplete ? purchaseDetail.CompleteTime : null;
            //}
            await _unitOfWork.SaveChangeAsync();
            return Ok("删除成功");
        }

        /// <summary>
        /// 修改到货记录
        /// </summary>
        /// <param name="dto"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        [HttpPost]
        public async Task<IActionResult> EditArrivalOrderInfo([FromBody] ModifyArrivalRecordDto dto)
        {
            var arrivalRecord = _purchase_arrivalRecordService.GetById(dto.Id);
            if (arrivalRecord == null)
            {
                throw new Exception("到货记录数据不存在，请刷新数据");
            }
            arrivalRecord.Remark = dto.Remark;
            arrivalRecord.CustomAttribute = dto.CustomerAttribute;
            arrivalRecord.DataStatus = dto.DataStatus;
            arrivalRecord.ArrivalTime = dto.ArrivalTime;
            await _unitOfWork.SaveChangeAsync();
            return Ok("保存成功");
        }

        #region Dto
        public class FilterPurchaseOrderDetailsPostDTO
        {
            public bool IsPaged { get; set; } = true;
            public List<Filter>? PurchaseOrderDetailFilterList { get; set; }
            public List<Filter> CommonFilterList { get; set; } = new List<Filter>();

        }
        public class FilterArrivalRecordsPostDTO
        {
            public List<Filter> ArrivalRecordFilterList { get; set; } = new List<Filter>();
            public List<Filter> CommonFilterList { get; set; } = new List<Filter>();
            public List<Filter> CustomerFilterList { get; set; } = new List<Filter>();
        }
        public class PurchaseOrderDetailDTO
        {
            // 属性定义
            public long IdField { get; set; }
            public long PurchaseOrderId { get; set; }
            public string PurchaseOrderNo { get; set; } = string.Empty;
            public PurchaseType PurchaseType { get; set; }
            public long SupplierId { get; set; }
            public string SupplierName { get; set; } = string.Empty;
            public string? ProductNo { get; set; }
            public long ProductId { get; set; }
            public long? SpecificationId { get; set; }
            public string ProductName { get; set; } = string.Empty;
            public ProductType ProductType { get; set; }
            public string? ProductCategory { get; set; } = string.Empty;
            public long ProductCategoryId { get; set; }
            public string MainUnit { get; set; } = string.Empty;
            public string? PurchaseUnit {  get; set; }
            public long PurchaseUnitId {  get; set; }
            public long PurchaseUnitConversionId { get; set; }
            public decimal PurchaseQuantity { get; set; }
            public decimal MaxPurchaseQuantity { get; set; }
            public decimal AlreadyArrivalQuantity { get; set; }
            public decimal QualifiedArrivalQuantity { get; set; }
            public decimal AlreadyInventoryQuantity { get; set; }
            public bool Selected { get; set; }
            public string? CustomAttribute { get; set; }
            public List<CustomAttribute> CustomAttributeList
            {
                get
                {
                    return string.IsNullOrEmpty(CustomAttribute) ? new List<CustomAttribute>() : JsonSerializer.Deserialize<List<CustomAttribute>>(CustomAttribute) ?? new List<CustomAttribute>();
                }
            }
            public string? ProductSpecification { get; set; }
            public string? MaterialGrade { get; set; }
            public string? ProductPackageWay { get; set; }
            public string? ProductTechnicalCode { get; set; }  // 技术型号
            public long? PurchaseOrderDetailId { get; set; }
            public decimal Ratio { get; set; }
            public string? ProductRemark { get; set; } = string.Empty ;
            public string? DetailRemark { get; set; } = string.Empty;
            public string? CreateUserName { get; set; } = string.Empty;
            public DateTime CreateTime { get; set; }
            public string? PurchaseUnitInfo { get; set; }
            public string? ProductUnit { get; set; }
            public string? PositionName { get; set; }

            public List<InventoryUnitInboundDto> PurchaseUnitList
            {
                get
                {
                    var list = !string.IsNullOrEmpty(PurchaseUnitInfo) ? JsonSerializer.Deserialize<List<SaleUnitConversionInfo>>(PurchaseUnitInfo, JsonHelper.DefaultJsonSerializerOptions) ?? new List<SaleUnitConversionInfo>() : new List<SaleUnitConversionInfo>();
                    var unitList = list.Select(x => new InventoryUnitInboundDto
                    {
                        UnitName = x.UnitName,
                        Id = x.Id,
                        UnitId = x.UnitId,
                        Ratio = (x.Ratio != 0 ? 1 / x.Ratio : 0) * Ratio,
                        Quantity = x.PlaceOrderQuantity
                    }
                    ).ToList();

                    unitList.Add(new InventoryUnitInboundDto
                    {
                        UnitName = PurchaseUnit ?? string.Empty,
                        UnitId = PurchaseUnitId,
                        Id = PurchaseUnitConversionId,
                        Ratio = Ratio,
                        Quantity = Math.Round(Ratio != 0 ? PurchaseQuantity / Ratio : 0, 6, MidpointRounding.AwayFromZero)
                    });
                    return unitList.DistinctBy(x => x.Id).OrderByDescending(s => s.Id == PurchaseUnitConversionId).ToList();
                }
            }
        }

        public class ArrivalRecordDto : PurchaseOrderDetailDTO
        {
            public long Id { get; set; }
            public string? RecordNo { get; set; }
            public string? InsepctionReason { get; set; }
            #region 新增
            /// <summary>
            /// 到货来源单号
            /// </summary>
            public string? OrderNo { get; set; }
            /// <summary>
            /// 到货来源Id
            /// </summary>
            public long? SourceId { get; set; }

            #endregion
            public DateTime ArrivalTime { get; set; }
            public decimal ArrivalQuantity { get; set; }
            /// <summary>
            /// 主单位：到货数量
            /// </summary>
            public decimal BaseArrivalQuantity { get; set; }
            public bool IsComplete { get; set; }
            public string? ArrivalUnit { get; set; }
            public long? ArrivalUnitId { get; set; }
            public long? ArrivalUnitConversionId { get; set; }
            public string? UnitConversionListString { get; set; }
            public List<InventoryUnitInboundDto> UnitConversionList
            {
                get
                {
                    var list = string.IsNullOrEmpty(UnitConversionListString) ? new List<InventoryUnitInboundDto>() :
                                    JsonSerializer.Deserialize<List<InventoryUnitInboundDto>>(UnitConversionListString,JsonHelper.DefaultJsonSerializerOptions) ?? new List<InventoryUnitInboundDto>();
                    return list.OrderByDescending(x => x.Id == ArrivalUnitConversionId).ToList();
                }
            }
            public InspectionConclusion? InspectionConclusion { get; set; }
            public new decimal Ratio { get; set; }
            public string? Remark { get; set; }
            public string? PurchaseRemark { get; set; }
            public new string? CreateUserName { get; set; } = string.Empty;

            // 库位库区相关字段
            public long? PositionId { get; set; }
            public InventoryDataStatus? DataStatus { get; set; }
            /// <summary>
            /// 合格数量
            /// </summary>
            public decimal QualifiedQuantity { get; set; }
            /// <summary>
            /// 异常数量
            /// </summary>
            public decimal AbnormalQuantity { get; set; }
            /// <summary>
            /// 报废数量
            /// </summary>
            public decimal ScrapQuantity { get; set; }
            /// <summary>
            /// 退货数量
            /// </summary>
            public decimal ReturnQuantity { get; set; }
            /// <summary>
            /// 让步接收数量
            /// </summary>
            public decimal ConcessionQuantity { get; set; }

            public bool? IsFinished { get; set; }
        }


        public class PostArrivalRecordDto
        {
            public long PurchaseOrderDetailId { get; set; }
            public decimal ArrivalQuantity { get; set; }
            /// <summary>
            /// 主单位到货数量
            /// </summary>
            public decimal BaseArrivalQuantity { get; set; }
            public long? ArrivalUnitConversionId { get; set; }
            public long ArrivalUnitId { get; set; }
            public string ArrivalUnit { get; set; } = string.Empty;
            public decimal Ratio { get; set; }
            public InventoryDataStatus DataStatus { get; set; }
            public long? PositionId { get; set; }
            public string? Remark { get; set; }
            public List<CustomAttribute> CustomAttributeList { get; set; } = new List<CustomAttribute>();
            public long DataId { get; set; }
            public InventoryDataType DataType { get; set; }
            public List<InventoryUnitInboundDto> UnitInfoList { get; set; } = new List<InventoryUnitInboundDto>();
            public bool IsComplete { get; set; }
            public DateTime ArrivalTime { get; set; }
        }

        public class ModifyArrivalRecordDto
        {
            public long Id { get; set; }
            public string? Remark { get; set; }
            public string? CustomerAttribute { get; set; }
            public InventoryDataStatus DataStatus { get; set; }
            public DateTime ArrivalTime { get; set; }
        }

        #endregion
    }
}
