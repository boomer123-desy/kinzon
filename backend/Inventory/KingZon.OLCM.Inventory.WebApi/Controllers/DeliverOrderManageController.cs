using Aspose.Cells;
using HtmlAgilityPack;
using Kingzon.OLCM.Common.Services;
using Kingzon.OLCM.Common.Services.Lock;
using KingZon.Core;
using KingZon.Core;
using KingZon.Core.DataBase;
using KingZon.Core.Web;
using KingZon.OLCM.Amoeba.DataBase.Entities;
using KingZon.OLCM.Common.DataBase.Entities;
using KingZon.OLCM.Common.DataBase.Enum;
using KingZon.OLCM.Common.DataBase.JsonStructure;
using KingZon.OLCM.Inventory.DataBase.Entities;
using KingZon.OLCM.Inventory.Service;
using KingZon.OLCM.Inventory.Service.Dto;
using KingZon.OLCM.Inventory.WebApi.Dto;
using KingZon.OLCM.Inventory.WebApi.DTO;
using KingZon.OLCM.Production.Service;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Index.HPRtree;
using System.Data;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Transactions;
using static KingZon.OLCM.Inventory.WebApi.Controllers.DeliverNoticeController;

namespace KingZon.OLCM.Inventory.WebApi.Controllers
{
    [Route("api/inventory/[controller]")]
    [ApiController]
    public class DeliverOrderManageController : KingZonControllerBase
    {
        private readonly IUnitOfWork _unitWork;
        private readonly InventoryManageService _inventoryManageService;
        private readonly SequenceService _sequenceService;
        private readonly CommonService<DeliveryOrderDetail> _deliveryOrderDetailService;
        private readonly CommonService<DeliveryOrder> _deliveryOrderService;
        private readonly CommonService<Product> _productService;
        private readonly CommonService<Customer> _customerService;
        private readonly CommonService<DliveryDetail_InventoryRecord_RL> _dliveryDetail_InventoryRecord_RLService;
        private readonly InventoryService _inventoryRecordService;
        private readonly CommonService<InventoryRecordRL> _inventoryRecordRLService;
        private readonly ProductionService _productionService;
        private readonly CustomerProductRLService _CustomerProductRLService;
        private readonly SystemParaService _systemParaService;
        private readonly CommonService<Template> _templateService;
        private readonly CommonService<TemplateItem> _templateItemService;
        private readonly CommonService<Customer_Product_RL> _customer_Product_RLService;
        private readonly CommonService<SaleOrderDetail> _saleOrderDetailService;
        private readonly CommonService<DateDimension> _dateDimensionService;
        private readonly FileStorageService _fileStorageService;
        private readonly CommonService<PackageRecord> _packageRecordService;
        private readonly CommonService<ArrivalRecord> _arrivalRecordService;
        private readonly CommonService<ProductionCard> _productionCardService;
        private readonly IConfiguration _configuration;

        public DeliverOrderManageController(
            IUnitOfWork unitWork,
            InventoryManageService inventoryManageService,
            SequenceService sequenceService,
            CommonService<DeliveryOrderDetail> deliveryOrderDetailService,
            CommonService<DeliveryOrder> deliveryOrderService,
            CommonService<Product> productService,
            CommonService<Customer> customerService,
            CommonService<DliveryDetail_InventoryRecord_RL> dliveryDetail_InventoryRecord_RLService,
            InventoryService inventoryRecordService,
            CommonService<InventoryRecordRL> inventoryRecordRLService,
            ProductionService productionService,
            CustomerProductRLService customerProductRLService,
            SystemParaService systemParaService,
            CommonService<Template> templateService,
            CommonService<TemplateItem> templateItemService,
            CommonService<Customer_Product_RL> customer_Product_RLService,
            CommonService<SaleOrderDetail> saleOrderDetailService,
            CommonService<DateDimension> dateDimensionService,
            FileStorageService fileStorageService,
            CommonService<PackageRecord> packageRecordService,
            CommonService<ArrivalRecord> arrivalRecordService,
            CommonService<ProductionCard> productionCardService,
            IConfiguration configuration
            )
        {
            _unitWork = unitWork;
            _inventoryManageService = inventoryManageService;
            _sequenceService = sequenceService;
            _deliveryOrderDetailService = deliveryOrderDetailService;
            _deliveryOrderService = deliveryOrderService;
            _productService = productService;
            _customerService = customerService;
            _deliveryOrderDetailService = deliveryOrderDetailService;
            _dliveryDetail_InventoryRecord_RLService = dliveryDetail_InventoryRecord_RLService;
            _inventoryRecordService = inventoryRecordService;
            _inventoryRecordRLService = inventoryRecordRLService;
            _productionService = productionService;
            _CustomerProductRLService = customerProductRLService;
            _fileStorageService = fileStorageService;
            _packageRecordService = packageRecordService;
            _arrivalRecordService = arrivalRecordService;
            _productionCardService = productionCardService;
            _systemParaService = systemParaService;
            _templateService = templateService;
            _templateItemService = templateItemService;
            _customer_Product_RLService = customer_Product_RLService;
            _saleOrderDetailService = saleOrderDetailService;
            _dateDimensionService = dateDimensionService;
            _deliveryOrderService = deliveryOrderService;
            _configuration = configuration;
        }
        public class PostDeliveryFilterDto
        {
            public List<Filter> filterList { get; set; } = new List<Filter>();
            public int Week { get; set; }
        }



        /// <summary>
        /// 送货单管理index数据获取
        /// </summary>
        /// <param name="filterList"></param>
        /// <returns></returns>
        [HttpPost("GetDeliverOrderManageIndexData", Name = "GetDeliverOrderManageIndexData")]
        public async Task<IActionResult> GetDeliverOrderManageIndexData([FromBody] PostDeliveryFilterDto filterInfo)
        {
            var dates = await _dateDimensionService.GetAll()
                .Where(x => x.Week == filterInfo.Week)
                .ToListAsync();

            var deliverys = _deliveryOrderService.GetAll();

            if (dates.Any())
            {
                var max = dates.OrderByDescending(x => x.Key).First();
                var maxDate = new DateTime(max.Year, max.Month, max.Day);
                var min = dates.OrderBy(x => x.Key).First();
                var minDate = new DateTime(min.Year, min.Month, min.Day);
                deliverys = deliverys.Where(x => x.DeliveryTime >= minDate && x.DeliveryTime <= maxDate);
            }



            //获取送货单数据
            var data = from deliverOrder in deliverys
                       join deliverOrderDetail in _deliveryOrderDetailService.GetAll()
                            on deliverOrder.Id
                            equals deliverOrderDetail.DeliveryOrderId
                       join saleOrderDetail in _saleOrderDetailService.GetAll()
                            on deliverOrderDetail.SaleOrderDetailId
                            equals saleOrderDetail.Id
                       join inventory in _inventoryRecordService.GetAll().Where(x => x.SourceType == SourceType.送货出库)
                            on deliverOrderDetail.Id equals inventory.SourceId
                       select new GetDeliverOrderManageDataDto()
                       {
                           ProductionOrderNo = deliverOrderDetail.ProductionOrderNo,
                           UnitWeight = inventory.EstimatedQuantity > 0 ? Math.Round(inventory.Quantity / inventory.EstimatedQuantity, 6, MidpointRounding.AwayFromZero) :
                                               deliverOrderDetail.HistoryProduct.Unit == "kg" ? deliverOrderDetail.HistoryProduct.Weight :
                                                    deliverOrderDetail.HistoryProduct.Weight / 1000,
                           DeliveryWeight = inventory.Quantity,
                           CustomerOrderNo = saleOrderDetail.SaleOrder.CustomerOrderNo,
                           DeliverOrderDetailId = deliverOrderDetail.Id,
                           DeliverOrderId = deliverOrder.Id,
                           DeliverOrderNo = deliverOrder.OrderNo ?? "",
                           CustomerId = deliverOrder.CustomerId,
                           CustomerCode = deliverOrder.Customer.Code ?? "",
                           CustomerName = deliverOrder.Customer.Name ?? "",
                           Remark = deliverOrder.Remark ?? "",
                           DeliveryUserId = deliverOrder.DeliveryUserId ?? 0,
                           DeliveryAddress = deliverOrder.DeliveryAddress ?? "",
                           DeliveryTime = deliverOrder.DeliveryTime,
                           Date = deliverOrder.DeliveryTime.ToString(),
                           DeliveryUserName = deliverOrder.DeliveryUserId != null ? deliverOrder.DeliveryUser!.Name : "",
                           DetailNo = deliverOrderDetail.DetailNo,
                           ProductId = deliverOrderDetail.HistoryProduct.ProductId,
                           HistoryProductId = deliverOrderDetail.HistoryProductId,
                           ProductNo = deliverOrderDetail.HistoryProduct.ProductNo ?? "",
                           CustomerMaterialNo = deliverOrderDetail.HistoryProduct.CustomerMaterialNo ?? "",
                           ProductName = deliverOrderDetail.HistoryProduct.ProductName ?? "",
                           Version = deliverOrderDetail.HistoryProduct.Version ?? "",
                           Specification = deliverOrderDetail.HistoryProduct.Specification ?? "",
                           MaterialGrade = deliverOrderDetail.HistoryProduct.MaterialGrade ?? "",
                           Unit = "kg",
                           DeliveryQuantity = deliverOrderDetail.DeliveryQuantity,
                           IsCanDelete = deliverOrderDetail.CustomerUsedQuantity > 0 || deliverOrderDetail.ReturnQuantity > 0 || deliverOrderDetail.AccountQuantity > 0 ? false : true,
                           SaleOrderNo = saleOrderDetail.SaleOrder.OrderNo,
                           IsQualified = deliverOrderDetail.OutboundInspectionRecord != null ?
                                        deliverOrderDetail.OutboundInspectionRecord.IsQualified == true ?
                                        IsQualifiedType.合格
                                        : IsQualifiedType.不合格
                                        : IsQualifiedType.待检验,
                           Month = deliverOrder.DeliveryTime.Month,
                           Year = deliverOrder.DeliveryTime.Year,
                           TotalPrice = saleOrderDetail.Price * deliverOrderDetail.DeliveryQuantity,
                           DeliveryMethod = deliverOrder.DeliveryMethod ?? string.Empty,
                           CustomAttribute = deliverOrderDetail.CustomAttribute
                       };

            var result = await data
                .ApplyFilters(filterInfo.filterList)
                .ApplySorts(this.SortInfoList)
                .ToPagedListAsync(this.PageModel);

            return Ok(result);
        }

        /// <summary>
        /// 获取送货通知列表，可按客户进行过滤
        /// </summary>
        /// <param name="filterList"></param>
        /// <returns></returns>
        [HttpPost("GetSelectDeliverNoticeList", Name = "GetSelectDeliverNoticeList")]
        public async Task<IActionResult> GetSelectDeliverNoticeList([FromBody] List<Filter> filterList)
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var localOptions = options;
            this.SortInfoList.Add(new SortInfo { Field = "IsDirectDelivery", Order = "desc" });
            this.SortInfoList.Add(new SortInfo { Field = "IsDelivery", Order = "desc" });


            var inventoryList = from rl in _dliveryDetail_InventoryRecord_RLService.GetAll()
                                group rl by rl.DeliveryOrderDetailId into g
                                select new
                                {
                                    DeliveryOrderDetailId = g.Key,
                                    OccupyQuantity = g.Sum(x=>x.UsedQuantity),
                                    InventoryList = g.Select(x => new DeliveryInventoryDto
                                    {
                                        InventoryId = x.InventoryRecordId,
                                        InventoryQuantity = x.UsedQuantity,
                                    }).ToList()
                                };

            var query = from x in _deliveryOrderDetailService.GetAll()
                                             .Include(x => x.DeliveryOrder)
                                             .Include(x => x.HistoryProduct)
                                             .ThenInclude(x => x.Product)
                                             .Include(x => x.OutboundInspectionRecord)
                                             .Include(x => x.SaleOrderDetail)
                                             .ThenInclude(x => x.SaleOrder)
                                             .ThenInclude(x => x.Customer)
                                             .Where(x => x.DeliveryOrderId == null)
                                    join inventory in inventoryList
                                            on x.Id equals inventory.DeliveryOrderDetailId 
                                            into temp
                                    from inventory in temp.DefaultIfEmpty()
                                    select new DeliveryNoticeIndexDto(x.SaleOrderDetail.SaleOrder.OrderNo, x.ProductionOrderNo, x.SaleOrderDetail.SaleOrder.Customer.Code, x.SaleOrderDetail.SaleOrder.Customer.Name)
                                    {
                                        Id = x.Id,
                                        DetailNo = x.DetailNo,
                                        CustomerOrderNo = x.SaleOrderDetail.SaleOrder.CustomerOrderNo,
                                        SaleOrderNo = x.SaleOrderDetail.SaleOrder.OrderNo,
                                        CustomerAddress = x.SaleOrderDetail.SaleOrder.CustomerAddress ?? "",
                                        ProductionOrderNo = x.ProductionOrderNo,
                                        CustomerId = x.SaleOrderDetail.SaleOrder.CustomerId,
                                        CustomerCode = x.SaleOrderDetail.SaleOrder.Customer.Code,
                                        CustomerName = x.SaleOrderDetail.SaleOrder.Customer.Name,
                                        IsQualified = x.IsDeleted == false ?
                                        x.OutboundInspectionRecord != null ?
                                        x.OutboundInspectionRecord.IsQualified == true ?
                                        IsQualifiedType.合格
                                        : IsQualifiedType.不合格
                                        : IsQualifiedType.待检验
                                        : IsQualifiedType.废弃,
                                        IsDelivery = x.OutboundInspectionRecord != null ? x.OutboundInspectionRecord.IsDelivery : false,
                                        ProductNo = x.HistoryProduct.ProductNo,
                                        ProductCustomerMaterialNo = x.SaleOrderDetail.CustomerMaterialNo ?? "",
                                        ProductName = x.HistoryProduct.ProductName,
                                        ProductVersion = x.HistoryProduct.Version ?? "",
                                        ProductMaterialGrade = x.HistoryProduct.MaterialGrade ?? "",
                                        ProductSpecification = x.HistoryProduct.Specification ?? "",
                                        NoticeQuantity = x.NoticeQuantity,
                                        ProductUnit = "kg",
                                        Remark = x.Remark ?? "",
                                        IsDirectDelivery = x.IsDirectDelivery,
                                        HaveDeliveryOrder = x.DeliveryOrderId != null ? true : false,
                                        IsDeleted = x.IsDeleted,
                                        IsOutboundInventory = x.IsOutboundInventory,
                                        DeliveryQuantity = x.IsOutboundInventory ? x.NoticeQuantity : 0,
                                        HistoryProductId = x.HistoryProductId,
                                        ProductId = x.HistoryProduct.ProductId,
                                        IsQualifiedBool = x.OutboundInspectionRecord != null ? x.OutboundInspectionRecord.IsQualified : null,
                                        IsAbnormal = x.IsAbnormal,
                                        OccupyQuantity = inventory.DeliveryOrderDetailId>0?inventory.OccupyQuantity:0,
                                        InventoryRecords = inventory.DeliveryOrderDetailId>0 ? inventory.InventoryList : new List<DeliveryInventoryDto>(),
                                        CustomAttributeList = string.IsNullOrEmpty(x.CustomAttribute) ? new List<CustomAttribute>() : JsonSerializer.Deserialize<List<CustomAttribute>>(x.CustomAttribute, localOptions) ?? new List<CustomAttribute>(),
                                    };
            var deliverNoticeList = await query
                                    .Where(x => (x.IsDirectDelivery || (x.IsDelivery == true && x.IsQualified == IsQualifiedType.合格)) && !x.IsAbnormal)
                                    .ApplyFilters(filterList)
                                    .ApplySorts(this.SortInfoList)
                                    .ToPagedListAsync(this.PageModel);

            return Ok(deliverNoticeList);
        }


        public class CreateDeliverOrderDto
        {
            public DeliveryOrderDto DeliverOrderInfo { get; set; } = null!;
            public List<DeliveryNoticeIndexDto> DeliveryNoticeList { get; set; } = null!;
        }

        public class DeliveryNoticeWithInventoryDto : DeliveryNoticeIndexDto
        {
            /// <summary>
            /// 该送货通知明细本次分配的库存记录及数量（生成送货单时指定）
            /// </summary>
            public List<OptionalDeliverNoticeListDto> OptionalDeliverNoticeList { get; set; } = new List<OptionalDeliverNoticeListDto>();

            //父类 DeliveryNoticeIndexDto 只有带参构造，派生类必须显式提供无参构造，否则编译/反序列化失败
            public DeliveryNoticeWithInventoryDto() : base(string.Empty, string.Empty, string.Empty, string.Empty)
            {
            }
        }

        public class PostDeliveryList
        {
            public int InventoryId { get; set; }
            public decimal Quantity { get; set; }
        }
        public class PostDeliverDetailDto
        {
            public int Id { get; set; }
            public List<CustomAttribute> CustomAttributeList { get; set; } = new List<CustomAttribute>();
            public string? ProductionOrderNo { get; set; }
            public List<PostDeliveryList> InventoryList { get; set; } = new List<PostDeliveryList>();
        }


        public class CreateDeliverOrderAndOutBoundDto
        {
            public DeliveryOrderDto DeliverOrderInfo { get; set; } = null!;
            public List<PostDeliverDetailDto> DeliveryNoticeList { get; set; } = new List<PostDeliverDetailDto>();
        }

        /// <summary>
        /// 获取客户送货单打印模板数据
        /// </summary>
        /// <param name="customerId"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        [HttpPost("GetDeliveryOrderPrintTemplateData", Name = "GetDeliveryOrderPrintTemplateData")]
        public IActionResult GetDeliveryOrderPrintTemplateData([FromBody] PostTemplateDto postData)
        {
            var customerList = _customerService.GetAll()
                               .Where(o => o.Id == postData.customerId)
                               .Select(o => new
                               {
                                   CustomerName = o.Name,
                                   CustomerContact = o.Contact,
                                   CustomerPhone = o.Phone,
                               }).ToList();
            if (!customerList.Any())
            {
                throw new Exception("该客户不存在，请刷新数据");
            }
            var tmp = _deliveryOrderService.GetAll();
            if (postData.orderIds.Count() > 0)
            {
                tmp = tmp.Where(x => postData.orderIds.Contains(x.Id));
            }
            if (postData.orderId > 0)
            {
                tmp = tmp.Where(o => o.Id == postData.orderId);
            }
            var order = tmp.Select(o => new DeliveryTemplateDataDto
            {
                Id = o.Id,
                OrderNo = o.OrderNo,
                OrderRemark = o.Remark != null ? o.Remark : "",
                DeliveryUser = o.DeliveryUser!.Name,
                DeliveryTime = o.DeliveryTime.ToString("yyyy-MM-dd HH:mm:ss"),
                DeliveryDate = o.DeliveryTime.ToString("yyyy-MM-dd"),
                CreateUser = o.CreateUserName,
            }).ToList();
            if (!order.Any())
            {
                throw new Exception("该送货单不存在，请刷新数据");
            }
            var tempFilePath = "";
            //判断客户是否有模板
            var file = _fileStorageService.GetAll().Where(o => o.Base == FileBase.DeliveryOrderTemplate && o.SourceType == FileStorageSourceType.送货单模板 && o.SourceId == postData.customerId).FirstOrDefault();
            //有模板查找客户模板文件
            if (file != null)
            {
                tempFilePath = _fileStorageService.GetfilePath(file.Base.ToString(), file.Path, file.Uuid);
            }
            //没有模板查找默认模板文件
            if (file == null)
            {
                tempFilePath = Path.Combine(AppContext.BaseDirectory, _configuration.GetValue<String>("ExcelTmp:DeliveryOrderTemplate")!);
            }
            //如果没有找到模板文件，抛出异常
            if (!Path.Exists(tempFilePath))
            {
                throw new Exception("没有找到送货单模板文件:" + tempFilePath + "---" + Path.Combine(_configuration.GetValue<string>("File:Path")!, tempFilePath));
            }

            var enterprise = new
            {
                EnterPriseName = "重庆立凯旭精密机械有限公司",
                EnterPriseAddress = "重庆立凯旭精密机械有限公司",
                EnterpriseContact = "",
                EnterprisePhone = "",
            };
            var enterpriseList = new List<object>();
            enterpriseList.Add(enterprise);
            var deliveryOrders = GetDeliveryPrintData(postData);
            if (deliveryOrders == null || !deliveryOrders.Any())
            {
                throw new Exception("未找到送货单数据，请刷新重试");
            }
            if (postData.isDoc)
            {
                var str = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                string disposition = "";

                Workbook workbook = new Workbook(); // 新建 Workbook
                workbook.Worksheets.Clear();        // 删除默认 Sheet
                WorkbookDesigner designer = new WorkbookDesigner();
                designer.Workbook = workbook;
                Workbook templateWb = new Workbook(tempFilePath);  // 模板
                foreach (var delivery in deliveryOrders)
                {   // 获取该送货单的明细
                    var query = delivery.details;
                    if (query == null || !query.Any())
                        continue;
                    var queryOrder = order.Where(x => x.Id == delivery.orderId).ToList();
                    if (queryOrder == null || !queryOrder.Any())
                        continue;

                    // **复制模板 Sheet** 到新 Sheet
                    string sheetName = string.IsNullOrEmpty(queryOrder.FirstOrDefault()?.OrderNo) ? "Sheet" + (workbook.Worksheets.Count + 1) : queryOrder.First().OrderNo;
                    Worksheet newSheet = workbook.Worksheets.Add(sheetName);
                    // 拷贝模板 Sheet 内容
                    newSheet.Cells.CopyRows(templateWb.Worksheets[0].Cells, 0, 0, templateWb.Worksheets[0].Cells.MaxDataRow + 1);
                    // 切换 Designer 到新 Sheet
                    designer.Workbook = workbook;
                    designer.Workbook.Worksheets.ActiveSheetIndex = workbook.Worksheets.IndexOf(newSheet);

                    designer.SetDataSource("DeliveryOrderDetail", query);
                    designer.SetDataSource("Customer", customerList);
                    designer.SetDataSource("DeliveryOrder", queryOrder);
                    designer.SetDataSource("EnterPrise", enterpriseList);
                    designer.Process(false);


                    // 删除标识行
                    int dataStartRow = FindRowByCellValue(newSheet, "dataStart");
                    int dataEndRow = FindRowByCellValue(newSheet, "dataEnd");

                    if (dataStartRow == -1 || dataEndRow == -1)
                        throw new Exception("模板中未找到 datastart 或 dataend 标识");

                    var unitIndex = this.FindColumnIndexByHeaderText(newSheet, dataStartRow - 1, "单位");

                    if (unitIndex != -1)
                    {
                        string value = newSheet.Cells[(dataStartRow + 1), unitIndex].StringValue;
                        for (int i = 0; i < query.Count; i++)
                        {
                            newSheet.Cells[dataStartRow + 1 + i, unitIndex].PutValue(value);
                        }
                    }


                    if (dataStartRow != -1)
                    {
                        newSheet.Cells.DeleteRow(dataStartRow);
                        if (dataEndRow > dataStartRow) dataEndRow--;
                    }
                    if (dataEndRow != -1)
                        newSheet.Cells.DeleteRow(dataEndRow);
                }
                MemoryStream responseStream = new MemoryStream();
                // 保存整个 Workbook 到流
                workbook.Save(responseStream, SaveFormat.Xlsx);
                responseStream.Seek(0, SeekOrigin.Begin);
                disposition = $"attachment; filename={WebUtility.UrlEncode(DateTimeOffset.Now.ToUnixTimeMilliseconds() + "-送货单.xlsx")}";
                Response.Headers.Add("Access-Control-Expose-Headers", "Content-Disposition");
                Response.Headers.Add("Content-Disposition", disposition);
                return File(responseStream, str);
            }
            else
            {

                using (var mainWorkbook = new Workbook(tempFilePath)) 
                {
                    const int templateIndex = 0;
                    var designer = new WorkbookDesigner { Workbook = mainWorkbook };

                    foreach (var delivery in deliveryOrders)
                    {   // 获取该送货单的明细
                        var query = delivery.details;
                        if (query == null || !query.Any())
                            continue;
                        var queryOrder = order.Where(x => x.Id == delivery.orderId).ToList();
                        if (queryOrder == null || !queryOrder.Any())
                            continue;

                        int newSheetIndex = mainWorkbook.Worksheets.AddCopy(templateIndex);
                        Worksheet newSheet = mainWorkbook.Worksheets[newSheetIndex];
                        string safeName = $"Sheet_{delivery.orderId}";
                        if (safeName.Length > 31) safeName = safeName.Substring(0, 31);
                        newSheet.Name = safeName;

                        // 设置数据源
                        designer.SetDataSource("DeliveryOrderDetail", query);
                        designer.SetDataSource("Customer", customerList);
                        designer.SetDataSource("DeliveryOrder", queryOrder);
                        designer.SetDataSource("EnterPrise", enterpriseList);
                        designer.Process(newSheetIndex, false);

                        // === 定位 datastart / dataend ===
                        int dataStartRow = FindRowByCellValue(newSheet, "dataStart");
                        int dataEndRow = FindRowByCellValue(newSheet, "dataEnd");
                        if (dataStartRow == -1 || dataEndRow == -1)
                            throw new Exception("模板中未找到 datastart 或 dataend 标识");

                        var unitIndex = this.FindColumnIndexByHeaderText(newSheet, dataStartRow - 1, "单位");

                        if (unitIndex == -1)
                        {
                            for (var i = dataStartRow - 1; i >= 0; i--)
                            {
                                unitIndex = this.FindColumnIndexByHeaderText(newSheet, i - 1, "单位");
                                if (unitIndex != -1)
                                {
                                    break;
                                }
                            }
                        }


                        if (unitIndex != -1)
                        {
                            string value = newSheet.Cells[(dataStartRow + 1), unitIndex].StringValue;
                            for (int i = 0; i < query.Count; i++)
                            {
                                newSheet.Cells[dataStartRow + 1 + i, unitIndex].PutValue(value);
                            }
                        }

                        designer.ClearDataSource();

                    }


                    mainWorkbook.Worksheets.RemoveAt(templateIndex);  //删掉第一个模板，只留下后面数据的sheet

                    var htmlParts = _fileStorageService.GetMultiSheetHtmlStr(mainWorkbook, 683); //转换为浏览器认识的html格式

                    // 5. 返回拼接后的完整 HTML 内容
                    return Content(htmlParts, "text/html; charset=utf-8");
                }

            }
        }

        private int FindColumnIndexByHeaderText(Worksheet sheet, int headerRowIndex, string headerText)
        {
            var cells = sheet.Cells;
            int maxCol = cells.MaxDataColumn;
            for (int col = 0; col <= maxCol; col++)
            {
                var cell = cells[headerRowIndex, col];
                if (cell.StringValue.Contains(headerText))
                {
                    return col;
                }
            }
            return -1;
        }
        private List<DeliveryPrintDataDto> GetDeliveryPrintData(PostTemplateDto postData)
        {
            //包装信息
            var isNew = _systemParaService.GetAll().Where(o => o.Extention == "IsNew").FirstOrDefault();
            var DrawCode = _systemParaService.GetAll().Where(o => o.Extention == "Drawing_No").FirstOrDefault();

            var paras = _systemParaService.GetAll().Where(o => o.DataType == SystemParaType.送货通知扩展参数).ToList();

            //产品信息、送货信息、客户信息、销售订单信息
            var delivery = from o in _deliveryOrderDetailService.GetAll()
                .Include(o => o.DeliveryOrder)
                     .ThenInclude(o => o!.DeliveryUser)
                .Include(o => o.SaleOrderDetail)
                     .ThenInclude(o => o!.SaleOrder)
                          .ThenInclude(o => o.Customer)
                .Include(o => o.HistoryProduct)
                .Include(o => o.OutboundInspectionRecord)
                           select o;
            if (postData.orderIds.Count() > 0)
            {
                delivery = delivery.Where(o => postData.orderIds.Contains(o.DeliveryOrderId != null ? (int)o.DeliveryOrderId! : 0));
            }
            if (postData.orderId > 0)
            {
                delivery = delivery.Where(o => o.DeliveryOrderId == postData.orderId);
            }
            var data = (from o in delivery
                        join rl in _customer_Product_RLService.GetAll()
                        on new { id = o.DeliveryOrder!.CustomerId, product = o.HistoryProduct.ProductId }
                        equals new { id = rl.CustomerId, product = rl.ProductId }
                        into temp
                        from rl in temp.DefaultIfEmpty()
                        where o.DeliveryOrderId != null
                        select new { o, rl }).ToList();
            var orderData = data.DistinctBy(x => x.o.Id).GroupBy(o => o.o.DeliveryOrderId).Select(x =>
            {
                var first = x.First();
                var index = 0;
                var details = x.Select(y =>
                {
                    var addtionCusList = !string.IsNullOrEmpty(y.o.CustomAttribute)? JsonSerializer.Deserialize<List<CustomAttribute>>(y.o.CustomAttribute, JsonHelper.DefaultJsonSerializerOptions) : new List<CustomAttribute>();
                    var deliveryLotNoPara = paras.Where(o => o.Extention == "DeliveryLotNo").FirstOrDefault();
                    var deliveryLotNo = deliveryLotNoPara != null && addtionCusList!.Where(x => x.systemParaId == deliveryLotNoPara.Id).FirstOrDefault() != null ? addtionCusList!.Where(x => x.systemParaId == deliveryLotNoPara.Id).FirstOrDefault()!.value : "";
                    var packageRequirement = x.Where(o => o.rl != null).FirstOrDefault() != null ? x.Where(o => o.rl != null).FirstOrDefault()!.rl.PackageRequirement : null;
                    var customAttribute = y.o.HistoryProduct.CustomAttribute;
                    var levelsObj = !String.IsNullOrEmpty(packageRequirement) ?  JsonSerializer.Deserialize<PackageInfoDto>(packageRequirement, JsonHelper.DefaultJsonSerializerOptions) : new PackageInfoDto();

                    var packgeInfos = levelsObj!.levels.SelectMany(x => x.items).ToList();

                    decimal? unitBoxNumberByBag = packgeInfos.Where(o => o.itemCode == "boxQuantity").FirstOrDefault() != null ? packgeInfos.Where(o => o.itemCode == "boxQuantity").First().itemValue : null;
                    decimal? BagQuantity = packgeInfos.Where(o => o.itemCode == "boxQuantity").FirstOrDefault() != null ? packgeInfos.Where(o => o.itemCode == "bagQuantity").First().itemValue : null;
                    return new DeliveryTemplateDataDto
                    {
                        Index = ++index,
                        DeliveryUser = first.o.DeliveryOrder!.DeliveryUser!.Name,
                        DeliveryTime = first.o.DeliveryOrder!.DeliveryTime.ToString("yyyy-MM-dd HH:mm:ss"),
                        DeliveryDate = first.o.DeliveryOrder!.DeliveryTime.ToString("yyyy-MM-dd"),
                        OrderRemark = first.o.DeliveryOrder!.Remark != null ? first.o.DeliveryOrder!.Remark : "",
                        CustomerAddress = first.o.DeliveryOrder!.DeliveryAddress ?? "",
                        CreateUser = first.o.DeliveryOrder!.CreateUserName != null ? first.o.DeliveryOrder!.CreateUserName : "",
                        OrderNo = first.o.DeliveryOrder!.OrderNo,
                        ProductNo = y.o.HistoryProduct.ProductNo != null ? y.o.HistoryProduct.ProductNo : "",
                        ProductName = y.o.HistoryProduct.ProductName != null ? y.o.HistoryProduct.ProductName : "",
                        Specification = y.o.HistoryProduct.Specification != null ? y.o.HistoryProduct.Specification : "",
                        MaterialGrade = y.o.HistoryProduct.MaterialGrade != null ? y.o.HistoryProduct.MaterialGrade : "",
                        Version = y.o.HistoryProduct.Version != null ? y.o.HistoryProduct.Version : "",
                        DeliveryQuantity = y.o.DeliveryQuantity,
                        Price = y.o.SaleOrderDetail.Price,
                        TotalPrice = y.o.SaleOrderDetail.Price * y.o.DeliveryQuantity,
                        LotNo = y.o.ProductionOrderNo != null ? y.o.ProductionOrderNo : "",
                        CustomerMaterialNo = y.o.SaleOrderDetail.CustomerMaterialNo,
                        SaleOrderNo = y.o.SaleOrderDetail.SaleOrder.OrderNo,
                        Weight = y.o.HistoryProduct.Weight,
                        TotalWeight = y.o.HistoryProduct.Weight * y.o.DeliveryQuantity,
                        TotalWeightByKg = y.o.HistoryProduct.Unit == "kg" ? y.o.HistoryProduct.Weight * y.o.DeliveryQuantity : y.o.HistoryProduct.Weight * y.o.DeliveryQuantity / 1000,
                        QualityInspector = y.o.OutboundInspectionRecord != null ? y.o.OutboundInspectionRecord!.CreateUserName : "",
                        PackageRequirement = packageRequirement,
                        MaterialSpecification = y.o.HistoryProduct.MaterialSpecification != null ? y.o.HistoryProduct.MaterialSpecification : "",
                        CustomAttribute = customAttribute,
                        Remark = y.o.Remark != null ? y.o.Remark : "",
                        ProductCharacter = customAttribute != null && JsonSerializer.Deserialize<List<ProductCustomAttributeDto>>(customAttribute)!.Where(o => o.systemParaId == isNew!.Id).FirstOrDefault() != null ? JsonSerializer.Deserialize<List<ProductCustomAttributeDto>>(customAttribute)!.Where(o => o.systemParaId == isNew!.Id).FirstOrDefault()!.value : "",
                        DrawingCode = customAttribute != null && JsonSerializer.Deserialize<List<ProductCustomAttributeDto>>(customAttribute)!.Where(o => o.systemParaId == DrawCode!.Id).FirstOrDefault() != null ? JsonSerializer.Deserialize<List<ProductCustomAttributeDto>>(customAttribute)!.Where(o => o.systemParaId == DrawCode!.Id).FirstOrDefault()!.value : "",
                        UnitBoxNumberByBag = unitBoxNumberByBag,
                        BagQuantity = BagQuantity,
                        BoxQuantity = unitBoxNumberByBag * BagQuantity,
                        BoxWeight = y.o.HistoryProduct.Weight * (unitBoxNumberByBag * BagQuantity) / 1000,
                        BagWeight = (y.o.HistoryProduct.Weight * BagQuantity??0) / 100,
                        BoxNumber = ((unitBoxNumberByBag ?? 0) * (BagQuantity??0)) > 0 ? (int)(Math.Round((decimal)(y.o.DeliveryQuantity / ((unitBoxNumberByBag ?? 0) * (BagQuantity ?? 0))))) : 0,
                        BagNumber = BagQuantity > 0 ? (int)(Math.Round((decimal)(y.o.DeliveryQuantity / BagQuantity))) : 0,
                        DeliveryLotNo = deliveryLotNo
                    };
                }).ToList();

                return new DeliveryPrintDataDto
                {
                    orderId = (int)x.Key!,
                    details = details
                };
            }).ToList();

            return orderData;
        }
        //查找含有标识字符串的行号
        private int FindRowByCellValue(Worksheet sheet, string marker)
        {
            for (int i = 0; i <= sheet.Cells.MaxDataRow; i++)
            {
                for (int j = 0; j <= sheet.Cells.MaxDataColumn; j++)
                {
                    var value = sheet.Cells[i, j].StringValue?.Trim();
                    if (value == marker) return i;
                }
            }
            return -1;
        }
        private string ExportWorksheetRangeToHtml(Worksheet sheet, int dataStartRow, int dataEndRow, bool addStyle)
        {
            if (sheet == null)
                throw new ArgumentNullException(nameof(sheet));

            // 1️⃣ 先完整导出整个工作表为 HTML
            var options = new HtmlSaveOptions(SaveFormat.Html)
            {
                ExportActiveWorksheetOnly = true,
                ExportImagesAsBase64 = true,
                HiddenColDisplayType = HtmlHiddenColDisplayType.Remove,
                HiddenRowDisplayType = HtmlHiddenRowDisplayType.Remove,
                Encoding = System.Text.Encoding.UTF8,
            };

            string html;
            using (var ms = new MemoryStream())
            {
                sheet.Workbook.Save(ms, options);
                html = Encoding.UTF8.GetString(ms.ToArray());
            }

            // 2️⃣ 使用 HtmlAgilityPack 解析 HTML
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var tdNodes = doc.DocumentNode.SelectNodes("//td[.//img]");
            if (tdNodes != null)
            {
                foreach (var td in tdNodes)
                {
                    var spans = td.SelectNodes(".//span");
                    if (spans == null) { continue; }
                    foreach (var span in spans)
                    {
                        var img = td.SelectNodes(".//img");
                        if (img == null)
                        {
                            continue;
                        }
                        string spanStyle = span.GetAttributeValue("style", "");

                        // 移除 absolute 定位和 margin
                        //spanStyle = Regex.Replace(spanStyle, @"position\s*:\s*absolute\s*;?", "", RegexOptions.IgnoreCase);
                        //spanStyle = Regex.Replace(spanStyle, @"z-index\s*:\s*\d+\s*;?", "", RegexOptions.IgnoreCase);
                        spanStyle = Regex.Replace(spanStyle, @"margin-(top|left|right|bottom)\s*:\s*[^;]+\s*;?", "", RegexOptions.IgnoreCase);

                        // 改为 block 并居中
                        spanStyle += ";display:flex;justify-content: center;";

                        span.SetAttributeValue("style", spanStyle.Trim());
                    }
                }
            }

            // 获取 style 标签
            var styleNode = doc.DocumentNode.SelectSingleNode("//style");
            string styleHtml = styleNode?.OuterHtml ?? "";
            // 3️⃣ 获取 table 节点
            var tableNode = doc.DocumentNode.SelectSingleNode("//table");
            if (tableNode == null)
                throw new Exception("导出的 HTML 中未找到 <table> 节点");

            // 4️⃣ 获取所有 tr
            var allRows = tableNode.SelectNodes(".//tr").ToList();

            dataStartRow = tdNodes != null ? allRows.FindIndex(x => x.InnerText.Contains("dataStart")) : dataStartRow;
            dataEndRow = tdNodes != null ? allRows.FindIndex(x => x.InnerText.Contains("dataEnd")) : dataEndRow;

            // 5️⃣ 提取各区域
            var theadRows = allRows.Take(dataStartRow).Where(x => x.InnerText.Length > 0).Select(x => x.OuterHtml);
            var tbodyRows = allRows.Skip(dataStartRow + 1).Take(dataEndRow - dataStartRow - 1).Where(x => x.InnerText.Length > 0).Select(x => x.OuterHtml);
            var tfootRows = allRows.Skip(dataEndRow + 1).Where(x => x.InnerText.Length > 0).Select(x => x.OuterHtml);

            string theadHtml = string.Join("", theadRows);
            string tbodyHtml = string.Join("", tbodyRows);
            string tfootHtml = string.Join("", tfootRows);

            // 6️⃣ 保留 colgroup
            var colGroupHtml = tableNode.SelectSingleNode(".//colgroup")?.OuterHtml ?? "";

            // 7️⃣ 拼装完整表格
            var finalHtml = $"{styleHtml}<table class='delivery-table'>{colGroupHtml}<thead>{theadHtml}</thead><tbody>{tbodyHtml}</tbody><tfoot>{tfootHtml}</tfoot></table>";
            if (addStyle)
            {
                finalHtml = $"<table class='delivery-table'>{colGroupHtml}<thead>{theadHtml}</thead><tbody>{tbodyHtml}</tbody><tfoot>{tfootHtml}</tfoot></table>";

            }
            return finalHtml;
        }


        public class DeliveryPrintDataDto
        {
            public int orderId { get; set; }
            public List<DeliveryTemplateDataDto> details { get; set; } = new List<DeliveryTemplateDataDto>();
        }
        /// <summary>
        /// 新增送货单
        /// </summary>
        /// <param name="submitData"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        [HttpPost("CreateDeliverOrder", Name = "CreateDeliverOrder")]
        public async Task<IActionResult> CreateDeliverOrder([FromBody] CreateDeliverOrderDto submitData)
        {
            //事务
            using (var transaction = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                var rateStr = _systemParaService.GetAll().Where(s => s.DataType == SystemParaType.销售单价比例).FirstOrDefault()?.DataValue ?? "0.95";
                var saleRate = 1 - Convert.ToDecimal(rateStr);

                //新增送货单
                var order = new DeliveryOrder(string.Empty)
                {
                    OrderNo = _sequenceService.GenerateCode(TableName.DeliveryOrder),
                    DeliveryAddress = submitData.DeliverOrderInfo.DeliveryAddress,
                    DeliveryTime = submitData.DeliverOrderInfo.DeliveryTime,
                    DeliveryUserId = submitData.DeliverOrderInfo.DeliveryUserId,
                    Remark = submitData.DeliverOrderInfo.Remark,
                    CustomerId = submitData.DeliverOrderInfo.CustomerId,
                    DeliveryMethod = submitData.DeliverOrderInfo.DeliveryMethodName,
                };
                await _deliveryOrderService.InsertAsync(order);

                //更新送货明细送货单Id
                var deliveryDetails = await _deliveryOrderDetailService.GetAll().Include(s => s.SaleOrderDetail).ThenInclude(s => s.Product).Where(s => submitData.DeliveryNoticeList.Select(x => x.Id).Contains(s.Id)).ToListAsync();
                var deliveryRls = await _dliveryDetail_InventoryRecord_RLService.GetAll().Include(s => s.InventoryRecord).Where(s => submitData.DeliveryNoticeList.Select(x => x.Id).Contains(s.DeliveryOrderDetailId)).ToListAsync();
                //判断送货明细是否出库
                foreach (var item in submitData.DeliveryNoticeList)
                {
                    var detail = deliveryDetails.FirstOrDefault(s => s.Id == item.Id);
                    if (detail == null)
                    {
                        throw new Exception($"送货通知记录{item.DetailNo}已删除，请刷新数据后再操作");
                    }
                    if (detail.DeliveryOrderId != null && detail.DeliveryOrderId > 0)
                    {
                        throw new Exception($"送货通知记录{item.DetailNo}已送货，请刷新数据后再操作");
                    }
                    detail.DeliveryOrder = order;
                    detail.SaleRate = saleRate;
                    detail.SalePrice = detail.SaleOrderDetail.Price;
                    var deliveryRlInfo = deliveryRls.Where(s => s.DeliveryOrderDetailId == item.Id).ToList();
                    var unitWeight = detail.SaleOrderDetail.Product!.Unit == "kg" ? detail.SaleOrderDetail.Product.Weight : detail.SaleOrderDetail.Product.Weight / 1000;
                    //未出库，新增出库记录和关联关系
                    if (!detail.IsOutboundInventory)
                    {
                        //新增出库记录
                        var outBound = new InventoryRecord(string.Empty)
                        {
                            RecordNo = _sequenceService.GenerateCode(TableName.InventoryRecord),
                            DataId = item.ProductId,
                            DataType = StorageMaterialType.成品,
                            EstimatedQuantity = detail.DeliveryQuantity,
                            Price = detail.SaleOrderDetail.Price,
                            Quantity = detail.DeliveryQuantity * unitWeight,
                            TotalPrice = detail.DeliveryQuantity * detail.SaleOrderDetail.Price,
                            InventoryType = InventoryType.出库,
                            ReceiveUserId = order.DeliveryUserId,
                            Remark = order.Remark,
                            SourceId = detail.Id,
                            SourceType = SourceType.送货出库,
                            InventoryPrice = deliveryRlInfo.Sum(s => s.InventoryRecord.InventoryPrice * s.UsedQuantity) / detail.DeliveryQuantity
                        };
                        await _inventoryManageService.InsertAsync(outBound);
                        //新增关联
                        var rls = deliveryRlInfo.Select(s => new InventoryRecordRL
                        {
                            SourceInventory = s.InventoryRecord,
                            TargetInventory = outBound,
                            Type = SourceType.送货出库,
                            Quantity = s.UsedQuantity * unitWeight,
                            UnitWeight = unitWeight,
                        }).ToList();
                        await _inventoryRecordRLService.InsertAsync(rls);
                        await _unitWork.SaveChangeAsync();
                    }
                }
                await _unitWork.SaveChangeAsync();

                //更新销售单送货数
                var saleDetails = deliveryDetails.Select(s => s.SaleOrderDetail).ToList();

                //更新产品送货数
                var products = deliveryDetails.Select(s => s.SaleOrderDetail.Product!).ToList();

                //更新库存占用量
                var sourceInventorys = deliveryRls.Select(s => s.InventoryRecord).ToList();

                this.ModifyDeliveryQuantity(saleDetails, products, sourceInventorys);

                //更新库存
                lock (QuantityLock.InventoryQuantityLock)
                {
                    _inventoryManageService.ModifyInventoryAbaoutData(sourceInventorys);
                    _unitWork.SaveChange();
                    transaction.Complete();
                }
                var info = new
                {
                    CustomerId = order.CustomerId,
                    OrderId = order.Id
                };
                return Ok(info);
            }
        }

        [HttpPost("DeleteDeliverOrder", Name = "DeleteDeliverOrder")]
        public async Task<IActionResult> DeleteDeliverOrder([FromBody] GetDeliverOrderManageDataDto submitData)
        {
            using (var transaction = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                //获取送货单和明细
                var order = await _deliveryOrderService.GetAll()
                                    .Where(s => s.Id == submitData.DeliverOrderId).FirstOrDefaultAsync();
                if (order == null)
                {
                    throw new Exception("送货单不存在，请刷新页面");
                }
                var deliveryDetails = await _deliveryOrderDetailService.GetAll().Include(s => s.SaleOrderDetail).ThenInclude(s => s.Product).Where(s => s.DeliveryOrderId == order.Id).ToListAsync();
                //更新送货明细orderId为空
                foreach (var item in deliveryDetails)
                {
                    item.DeliveryOrderId = null;
                    item.SalePrice = 0;
                    item.SaleRate = 0;
                }
                await _unitWork.SaveChangeAsync();
                _deliveryOrderService.Delete(order);
                await _unitWork.SaveChangeAsync();
                //明细不出库记录，删除出库记录
                var ids = deliveryDetails.Select(x => x.Id).ToList();
                var outBounds = await _inventoryManageService.GetAll().Where(x => deliveryDetails.Select(s => s.Id).Contains(x.SourceId) && x.SourceType == SourceType.送货出库 && x.InventoryType == InventoryType.出库).ToListAsync();
                var inventoryRls = await _inventoryRecordRLService.GetAll().Include(s => s.SourceInventory).Where(s => outBounds.Select(x => x.Id).Contains(s.TargetInventoryId)).ToListAsync();
                var occupyInfos = await _dliveryDetail_InventoryRecord_RLService.GetAll(true).Include(x=>x.InventoryRecord).Where(x => ids.Contains(x.DeliveryOrderDetailId)).ToListAsync();
                var deleteInventorys = new List<InventoryRecord>();
                var modifyInventorys = new List<InventoryRecord>();
                var deleteInventoryRLs = new List<InventoryRecordRL>();
                var deleteOccupys = new List<DliveryDetail_InventoryRecord_RL>();
                foreach (var item in deliveryDetails)
                {
                    var outBound = outBounds.FirstOrDefault(s => s.SourceId == item.Id);
                    var rls = inventoryRls.Where(s => s.TargetInventoryId == (outBound == null ? 0 : outBound.Id)).ToList();
                    var sources = rls.Select(s => s.SourceInventory).ToList();
                    var occupys = occupyInfos.Where(x => !x.IsDeleted && x.DeliveryOrderDetailId == item.Id).ToList();
                    if (!item.IsOutboundInventory && outBound != null)
                    {
                        deleteInventorys.Add(outBound);
                        deleteInventoryRLs = deleteInventoryRLs.Union(rls).ToList();
                        modifyInventorys.Add(outBound);
                        deleteOccupys = deleteOccupys.Concat(occupys).ToList();
                    }
                    modifyInventorys = modifyInventorys.Union(sources).ToList();
                }
                _inventoryRecordRLService.Delete(deleteInventoryRLs);
                _inventoryManageService.Delete(deleteInventorys);
                _dliveryDetail_InventoryRecord_RLService.Delete(deleteOccupys, false);
                //恢复通知占用
                var resetIds = deleteOccupys.Select(x => x.DeliveryOrderDetailId).ToList();
                var resetList = occupyInfos.Where(x => x.IsDeleted && resetIds.Contains(x.DeliveryOrderDetailId)).ToList();
                foreach(var item in resetList)
                {
                    item.IsDeleted = false;
                }
                await _unitWork.SaveChangeAsync();
                //更新销售单送货量
                var saleOrderDetails = deliveryDetails.Select(s => s.SaleOrderDetail).ToList();
                //更新产品送货量
                var products = deliveryDetails.Select(s => s.SaleOrderDetail.Product!).ToList();
                //更新库存记录占用
                this.ModifyDeliveryQuantity(saleOrderDetails, products, occupyInfos.Select(x=>x.InventoryRecord).ToList());
                //更新库存
                lock (QuantityLock.InventoryQuantityLock)
                {
                    _inventoryManageService.ModifyInventoryAbaoutData(modifyInventorys);
                    _unitWork.SaveChange();
                    transaction.Complete();
                }
                return Ok("删除成功");
            }



        }

        /// <summary>
        /// 更新送货数
        /// </summary>
        /// <param name="saleOrderDetails"></param>
        /// <param name="products"></param>
        /// <param name="sourceInventorys"></param>
        private void ModifyDeliveryQuantity(List<SaleOrderDetail> saleOrderDetails, List<Product> products, List<InventoryRecord> sourceInventorys)
        {
            //更新销售单送货数
            var saleInfos = _deliveryOrderDetailService.GetAll().Where(s => saleOrderDetails.Select(x => x.Id).Contains(s.SaleOrderDetailId)).ToList();
            foreach (var item in saleOrderDetails)
            {
                item.DeliveryNoticeQuantity = saleInfos.Where(s => s.SaleOrderDetailId == item.Id).Sum(s => s.DeliveryQuantity);
                item.DeliveryQuantity = saleInfos.Where(s => s.SaleOrderDetailId == item.Id && s.DeliveryOrderId != null && s.DeliveryOrderId > 0).Sum(s => s.DeliveryQuantity);
            }
            _unitWork.SaveChange();

            //更新产品送货数
            var productInfos = _deliveryOrderDetailService.GetAll().Include(s => s.SaleOrderDetail).Where(s => products.Select(x => x!.Id).Contains(s.SaleOrderDetail.ProductId ?? 0) && s.DeliveryOrder == null).ToList();
            foreach (var item in products)
            {
                item!.DeliveryNoticeQuantity = productInfos.Where(s => s.SaleOrderDetail.ProductId == item.Id).Sum(s => s.DeliveryQuantity);
            }
            _unitWork.SaveChange();

            //更新库存占用量
            var deliveryOccupInfos = _dliveryDetail_InventoryRecord_RLService.GetAll()
               .Where(s => !s.DeliveryOrderDetail.IsOutboundInventory && s.DeliveryOrderDetail.DeliveryOrder == null
               && sourceInventorys.Select(x => x.Id).Contains(s.InventoryRecordId))
               .ToList();
            foreach (var item in sourceInventorys)
            {
                var product = products.FirstOrDefault(s => s.Id == item.DataId);
                var unitWeight = product != null ? (product.Unit == "kg" ? product.Weight : product.Weight / 1000) : 1;
                item.OccupiedQuantity = deliveryOccupInfos.Where(s => s.InventoryRecordId == item.Id).Sum(s => s.UsedQuantity) * unitWeight;
            }
        }


        [HttpGet("ModifyProductPackage")]
        public async Task<IActionResult> ModifyProductPackage()
        {
            var rls = await _customer_Product_RLService.GetAll().ToListAsync();

            var templates = await _templateItemService.GetAll().ToListAsync();

            foreach(var rl in rls)
            {

                if(rl.PackageRequirement == null)
                {
                    continue;
                }

                var levelsObj = !String.IsNullOrEmpty(rl.PackageRequirement) ? JsonSerializer.Deserialize<PackageInfoDto>(rl.PackageRequirement, JsonHelper.DefaultJsonSerializerOptions) : new PackageInfoDto();


                if (levelsObj == null)
                {
                    continue;
                }

                var packgeInfos = levelsObj!.levels.SelectMany(x => x.items).ToList();

                foreach (var item in packgeInfos)
                {
                    var temp = templates.FirstOrDefault(s => s.ItemName == item.itemName && s.Unit == item.itemUnit);
                    if (temp != null)
                    {
                        item.itemCode = temp.Code;
                    }
                }

                rl.PackageRequirement = JsonSerializer.Serialize(levelsObj, JsonHelper.DefaultJsonSerializerOptions);
            }

            await _unitWork.BulkSaveChangeAsync();

            return Ok("保存成功");
        }

        /// <summary>
        /// 获取送货通知库存
        /// </summary>
        /// <param name="deliveryId"></param>
        /// <returns></returns>
        [HttpGet("GetInventoryByDeliveryList", Name = "GetInventoryByDeliveryList")]
        public async Task<IActionResult> GetInventoryByDeliveryList([FromQuery] int id)
        {
            var deliveryDetail = await _deliveryOrderDetailService.GetAll(true).Include(x => x.SaleOrderDetail).ThenInclude(s => s.Product).Where(s => s.Id == id).FirstOrDefaultAsync();

            if (deliveryDetail == null)
            {
                throw new Exception("送货通知不存在，请刷新页面");
            }

            var unitWeight = deliveryDetail.SaleOrderDetail.Product!.Unit == "kg" ? deliveryDetail.SaleOrderDetail.Product.Weight : (deliveryDetail.SaleOrderDetail.Product.Weight / 1000);
            var inventorys = from inventory in _inventoryManageService.GetAll()
                             join package in _packageRecordService.GetAll()
                                 on new { id = inventory.SourceId, type = inventory.SourceType }
                                 equals new { id = package.Id, type = SourceType.包装入库 }
                                 into pakcageTemp
                             from package in pakcageTemp.DefaultIfEmpty()
                             join arrival in _arrivalRecordService.GetAll()
                                 on new { id = inventory.SourceId, type = inventory.SourceType }
                                 equals new { id = arrival.Id, type = arrival.SourceType == ArrivalDataType.采购到货 ? SourceType.采购入库 : SourceType.外协入库 }
                                 into arrivalTemp
                             from arrival in arrivalTemp.DefaultIfEmpty()
                             join card in _productionCardService.GetAll(true)
                                 on new { id = inventory.SourceId, type = inventory.SourceType }
                                 equals new { id = card.Id, type = SourceType.生产入库 }
                                 into cardTemp
                             from card in cardTemp.DefaultIfEmpty()
                             join rl in _dliveryDetail_InventoryRecord_RLService.GetAll()
                             .Where(x => x.DeliveryOrderDetailId == deliveryDetail.Id)
                                 on inventory.Id
                                 equals rl.InventoryRecordId
                                 into rlTemp
                             from rl in rlTemp.DefaultIfEmpty()
                             where inventory.InventoryType == InventoryType.入库
                                && inventory.Quantity > inventory.UsedQuantity
                                && inventory.DataId == deliveryDetail.SaleOrderDetail.ProductId && inventory.DataType == StorageMaterialType.成品
                                && inventory.DataStatusType == DataStatusType.已包装
                                && inventory.Position != null && inventory.Position!.Location.Area.IsDeliveryProduct
                             orderby inventory.CreateTime
                             select new OptionalDeliverNoticeListDto
                             {
                                 InventoryRecordId = inventory.Id,
                                 InStockTime = inventory.CreateTime,
                                 ModifyTime = inventory.UpdateTime,
                                 InventoryQuantity = (inventory.Quantity - inventory.UsedQuantity) < unitWeight ? 1 : (int)Math.Round((inventory.Quantity - inventory.UsedQuantity) / unitWeight, 0, MidpointRounding.AwayFromZero),
                                 AvailableQuantity = inventory.IsAbnormalReport || inventory.IsException || inventory.IsInspection == false ? 0 :
                                                       ((inventory.Quantity - inventory.UsedQuantity - inventory.OccupiedQuantity) >0 && (inventory.Quantity - inventory.UsedQuantity - inventory.OccupiedQuantity) < unitWeight ? 1 : (int)Math.Round((inventory.Quantity - inventory.UsedQuantity - inventory.OccupiedQuantity) / unitWeight, 0, MidpointRounding.AwayFromZero)) + (rl != null && !deliveryDetail.IsOutboundInventory ? rl.UsedQuantity : 0),
                                 InventoryRecordNo = inventory.RecordNo,
                                 InventoryRemark = inventory.Remark,
                                 PackageRecordNo = package != null ? package.RecordNo :
                                                       arrival != null ? arrival.RecordNo :
                                                           card != null ? card.CardNo :
                                                               string.Empty,
                                 PackageRemark = package != null ? package.Remark :
                                                     arrival != null ?
                                                         arrival.Remark : string.Empty,
                                 Position = inventory.Position != null ? $"{inventory.Position!.Location.Area.Name}/{inventory.Position!.Location.Name}/{inventory.Position!.Name}" : "",
                                 Price = inventory.InventoryPrice,
                                 ProductionNo = package != null ? package.ProductionNo :
                                                   arrival != null ? arrival.RecordNo :
                                                       inventory.RecordNo,
                                 SourceType = inventory.SourceType,
                                 IsAbnormal = inventory.IsAbnormalReport,
                                 IsException = inventory.IsException,
                                 IsInspection = inventory.IsInspection == false ? false : true,
                                 OccupyQuantity = rl != null ? rl.UsedQuantity : 0,
                             };
            var result = await inventorys.ToListAsync();
            return Ok(result);
        }

        /// <summary>
        /// 新增送货单
        /// </summary>
        /// <param name="submitData"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        [HttpPost("CreateDeliverOrderAndOutBound", Name = "CreateDeliverOrderAndOutBound")]
        public async Task<IActionResult> CreateDeliverOrderAndOutBound([FromBody] CreateDeliverOrderAndOutBoundDto submitData)
        {
            //事务
            using (var transaction = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                // 获取销售单价比例
                var rateStr = _systemParaService.GetAll().Where(s => s.DataType == SystemParaType.销售单价比例).FirstOrDefault()?.DataValue ?? "0.95";
                var saleRate = 1 - Convert.ToDecimal(rateStr);

                // 新增送货单
                var order = new DeliveryOrder(string.Empty)
                {
                    OrderNo = _sequenceService.GenerateCode(TableName.DeliveryOrder),
                    DeliveryAddress = submitData.DeliverOrderInfo.DeliveryAddress,
                    DeliveryTime = submitData.DeliverOrderInfo.DeliveryTime,
                    DeliveryUserId = submitData.DeliverOrderInfo.DeliveryUserId,
                    Remark = submitData.DeliverOrderInfo.Remark,
                    CustomerId = submitData.DeliverOrderInfo.CustomerId,
                    DeliveryMethod = submitData.DeliverOrderInfo.DeliveryMethodName,
                };
                await _deliveryOrderService.InsertAsync(order);
                var ids = submitData.DeliveryNoticeList.Select(s => s.Id).ToList();
                var inventoryIds = submitData.DeliveryNoticeList.SelectMany(s => s.InventoryList.Select(x => x.InventoryId)).ToList();
                // 获取送货单对应的送货通知
                var deliveryDetails = await _deliveryOrderDetailService.GetAll()
                                                    .Include(s => s.SaleOrderDetail)
                                                        .ThenInclude(s => s.Product)
                                            .Where(s => ids.Contains(s.Id)).ToListAsync();
                var deliveryRls = await _dliveryDetail_InventoryRecord_RLService.GetAll()
                                                    .Include(s => s.InventoryRecord)
                                                    .Where(s => ids.Contains(s.DeliveryOrderDetailId))
                                                    .ToListAsync();
                var originInventorys = await _inventoryManageService.GetAll().Where(s => inventoryIds.Contains(s.Id)).ToListAsync();
                var inventoryList = new List<InventoryRecord>();
                var inventoryRlsList = new List<InventoryRecordRL>();
                var deliveryOccupInfos = new List<DliveryDetail_InventoryRecord_RL>();
                //新增送货明细出库记录和关联记录
                foreach (var detail in submitData.DeliveryNoticeList)
                {
                    var delivery = deliveryDetails.FirstOrDefault(s => s.Id == detail.Id);
                    if (delivery==null)
                    {
                        throw new Exception($"送货通知记录{detail.Id}已作废或删除，请刷新数据后再操作");
                    }
                    if (delivery.DeliveryOrderId.HasValue)
                    {
                        throw new Exception($"送货通知记录{delivery.DetailNo}已生成送货单，不可重复操作");
                    }
                    delivery.DeliveryOrder = order;
                    delivery.SaleRate = saleRate;
                    delivery.SalePrice = delivery.SaleOrderDetail.Price;
                    delivery.ProductionOrderNo = detail.ProductionOrderNo??"";
                    delivery.CustomAttribute = detail.CustomAttributeList.Any()? JsonSerializer.Serialize(detail.CustomAttributeList, JsonHelper.DefaultJsonSerializerOptions):null;
                    //生成送货通知时已出库
                    if (delivery.IsOutboundInventory)
                    {
                        delivery.DeliveryQuantity = delivery.NoticeQuantity;
                        continue;
                    }
                    var rls = deliveryRls.Where(s => s.DeliveryOrderDetailId == detail.Id).ToList();
                    //存在占用，作废原占用，新增本次出库和送货关联
                    if (rls.Any())
                    {
                        _dliveryDetail_InventoryRecord_RLService.Delete(rls);
                    }
                    var currentInventorys = originInventorys.Where(s => detail.InventoryList.Select(x => x.InventoryId).Contains(s.Id)).ToList();
                    var unitWeight = delivery.SaleOrderDetail.Product!.Unit == "kg" ? delivery.SaleOrderDetail.Product.Weight : delivery.SaleOrderDetail.Product.Weight / 1000;
                    var outBound = new InventoryRecord(string.Empty)
                    {
                        RecordNo = _sequenceService.GenerateCode(TableName.InventoryRecord),
                        DataId = delivery.SaleOrderDetail.ProductId??0,
                        DataType = StorageMaterialType.成品,
                        EstimatedQuantity = detail.InventoryList.Sum(x=>x.Quantity),
                        Price = delivery.SaleOrderDetail.Price,
                        Quantity = detail.InventoryList.Sum(x => x.Quantity) * unitWeight,
                        TotalPrice = detail.InventoryList.Sum(x => x.Quantity) * delivery.SaleOrderDetail.Price,
                        InventoryType = InventoryType.出库,
                        ReceiveUserId = order.DeliveryUserId,
                        Remark = order.Remark,
                        SourceId = detail.Id,
                        SourceType = SourceType.送货出库,
                        InventoryPrice = currentInventorys.Sum(s => s.InventoryPrice * s.UsedQuantity) / detail.InventoryList.Sum(x => x.Quantity)
                    };
                    inventoryList.Add(outBound);
                    var inventoryRls = detail.InventoryList.Select(s =>
                    {
                        var inventory = currentInventorys.FirstOrDefault(x => x.Id == s.InventoryId);
                        if (inventory == null)
                        {
                            throw new Exception($"库存记录{s.InventoryId}不存在，请刷新数据后再操作");
                        }
                        var remainQuantity = unitWeight != 0 ? Math.Round((inventory.Quantity - inventory.UsedQuantity - inventory.OccupiedQuantity) / unitWeight, 0, MidpointRounding.AwayFromZero) : 0;
                        remainQuantity += rls.Where(x=>x.InventoryRecordId == inventory.Id).Sum(x=>x.UsedQuantity);
                        if (remainQuantity==0 && inventory.Quantity>inventory.UsedQuantity)
                        {
                            remainQuantity = 1;
                        }
                        if (s.Quantity > remainQuantity)
                        {
                            throw new Exception($"库存记录{inventory.RecordNo}可用数量不足，剩余可用数量{remainQuantity}，请刷新数据后再操作");
                        }
                        return new InventoryRecordRL
                        {
                            SourceInventory = inventory,
                            TargetInventory = outBound,
                            Type = SourceType.送货出库,
                            Quantity = s.Quantity * unitWeight,
                            UnitWeight = unitWeight,
                        };
                    }).ToList();
                    var occupyList = detail.InventoryList.Select(s =>new DliveryDetail_InventoryRecord_RL
                    {
                        InventoryRecordId = s.InventoryId,
                        DeliveryOrderDetailId = detail.Id,
                        UsedQuantity = (int)s.Quantity
                    }).ToList();
                    inventoryRlsList = inventoryRlsList.Concat(inventoryRls).ToList();
                    deliveryOccupInfos = deliveryOccupInfos.Concat(occupyList).ToList();
                    delivery.DeliveryQuantity = (int)detail.InventoryList.Sum(x => x.Quantity);
                }

                await _inventoryManageService.InsertAsync(inventoryList);
                await _inventoryRecordRLService.InsertAsync(inventoryRlsList);
                await _dliveryDetail_InventoryRecord_RLService.InsertAsync(deliveryOccupInfos);
                await _unitWork.SaveChangeAsync();

                //更新销售单送货数
                var saleDetails = deliveryDetails.Select(s => s.SaleOrderDetail).ToList();

                //更新产品送货数
                var products = deliveryDetails.Select(s => s.SaleOrderDetail.Product!).ToList();

                //更新库存占用量（sourceInventorys 已于本次分配查询时获取）

                this.ModifyDeliveryQuantity(saleDetails, products, deliveryRls.Select(x=>x.InventoryRecord).ToList());

                //更新库存
                lock (QuantityLock.InventoryQuantityLock)
                {
                    _inventoryManageService.ModifyInventoryAbaoutData(originInventorys);
                    _unitWork.SaveChange();
                    transaction.Complete();
                }
                var info = new
                {
                    CustomerId = order.CustomerId,
                    OrderId = order.Id
                };
                return Ok(info);
            }
        }


        
    }
}

