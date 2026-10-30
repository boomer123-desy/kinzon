using Aspose.Cells;
using Kingzon.OLCM.Common.Services;
using KingZon.Core.DataBase;
using KingZon.Core.Web;
using KingZon.OLCM.Base.Shared.DataBase.Entities;
using KingZon.OLCM.Common.DataBase.Entities;
using KingZon.OLCM.Common.DataBase.Enum;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace KingZon.OLCM.Sales.WebApi.Controllers
{
    [Route("api/sales/[controller]")]
    [ApiController]
    public class SaleDataStatController : KingZonControllerBase
    {
        private readonly CommonService<SaleOrder> _saleOrderService;
        private readonly CommonService<SaleOrderDetail> _saleOrderDetailService;
        private readonly CommonService<HistoryProduct> _historyProductService;
        //private readonly CommonService<Base_Partner> _partnerService;
        private readonly CommonService<DeliveryOrder> _deliveryOrderService;
        private readonly CommonService<DeliveryOrderDetail> _deliveryOrderDetailService;
        private readonly CommonService<Base_Category> _categoryService;
        private readonly SystemParaService _systemParaService;
        private readonly IConfiguration _configuration;


        public SaleDataStatController(
            CommonService<SaleOrder> saleOrderService,
            CommonService<SaleOrderDetail> saleOrderDetailService,
            CommonService<HistoryProduct> historyProductService,
            //CommonService<Base_Partner> partnerService,
            CommonService<DeliveryOrder> deliveryOrderService,
            CommonService<DeliveryOrderDetail> deliveryOrderDetailService,
            CommonService<SaleReturnOrder> saleReturnOrderService,
            CommonService<Base_Category> categoryService,
            SystemParaService systemParaService,
            IConfiguration configuration)
        {
            _saleOrderService = saleOrderService;
            _saleOrderDetailService = saleOrderDetailService;
            _historyProductService = historyProductService;
            //_partnerService = partnerService;
            _deliveryOrderService = deliveryOrderService;
            _deliveryOrderDetailService = deliveryOrderDetailService;
            _categoryService = categoryService;
            _systemParaService = systemParaService;
            _configuration = configuration;
        }

        /// <summary>
        /// 按系统配置的财务结算日期统计订单、发货及退货金额。
        /// 同比使用去年同期销售额对比今年发货额，分类内按本年1月净发货额排名。
        /// </summary>
        [HttpGet("GetMonthDataStat", Name = "GetMonthDataStat")]
        public async Task<IActionResult> GetMonthDataStat([FromQuery] DateTime month, [FromQuery] string? customerName = null, [FromQuery] bool includeTax = false)
        {
            return Ok(await GetMonthReport(month, customerName, includeTax));
        }

        /// <summary>导出月报，统计行和比例保留为可重新计算的 Excel 公式。</summary>
        [HttpPost("ExportMonthDataStat")]
        public async Task<IActionResult> ExportMonthDataStat([FromBody] MonthSaleExportQueryDto dto)
        {
            var templatePath = Path.Combine(AppContext.BaseDirectory, _configuration.GetValue<System.String>("ExcelTmp:MonthSaleDataStatTemplate")!);
            //如果没有找到模板文件，抛出异常
            if (!Path.Exists(templatePath))
            {
                throw new Exception("没有找到出库单模板文件:" + templatePath + "---" + Path.Combine(_configuration.GetValue<string>("File:Path")!, templatePath));
            }
            var report = await GetMonthReport(dto.Month, dto.CustomerName, dto.IncludeTax);
            using var workbook = BuildMonthSaleWorkbook(report, dto.CustomerName, templatePath);
            using var stream = new MemoryStream();
            workbook.Save(stream, SaveFormat.Xlsx);
            Response.Headers.Append(
                "Access-Control-Expose-Headers",
                "Content-Disposition"
            );
            return File(stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"{report.Year}年{report.Month}月订单及销售发货月报表.xlsx");
        }

        private async Task<MonthSaleDataStatDto> GetMonthReport(DateTime month, string? customerName, bool includeTax)
        {
            if (month.Year < 2 || month.Year > 9998 || (month.Year == 2 && month.Month == 1))
            {
                throw new Exception("请选择有效的开账月份");
            }

            var financeDate = GetFinanceDate();
            var (start, endExclusive) = GetFinancePeriod(month, financeDate);
            var (lastYearStart, lastYearEnd) = GetFinancePeriod(month.AddYears(-1), financeDate);
            var (januaryStart, januaryEnd) = GetFinancePeriod(new DateTime(month.Year, 1, 1), financeDate);

            // 当前及去年同期销售单按创建日期计入账期，直接使用单头金额。
            var rows = await (
                from order in _saleOrderService.GetAll().AsNoTracking()
                where order.Status != SaleOrderStatus.待下单
                    && ((order.CreateTime >= start && order.CreateTime < endExclusive)
                        || (order.CreateTime >= lastYearStart && order.CreateTime < lastYearEnd))
                join category in _categoryService.GetAll().AsNoTracking() on order.Customer.CategoryId equals category.Id into tmp
                from category in tmp.DefaultIfEmpty()
                select new MonthSaleStatRow
                {
                    DateTime = order.CreateTime,
                    CategoryId = order.Customer.CategoryId,
                    CategoryName = category != null ? category.Name : string.Empty,
                    CustomerId = order.CustomerId,
                    CustomerName = order.Customer.Name,
                    OrderAmount = includeTax ? order.OrderTotalPriceHaveTax : order.OrderTotalPrice
                }).ToListAsync();

            // 送货只读取当前账期和本年1月，不读取无关历史数据。
            // 已关联有效送货单即按送货日期计入，不能用“通知时已出库”标记过滤送货时出库的记录。
            var deliveries = await (
                from detail in _deliveryOrderDetailService.GetAll().AsNoTracking()
                join order in _deliveryOrderService.GetAll().AsNoTracking() on detail.DeliveryOrderId equals order.Id
                join saleDetail in _saleOrderDetailService.GetAll(true).AsNoTracking() on detail.SaleOrderDetailId equals saleDetail.Id into saleDetails
                from saleDetail in saleDetails.DefaultIfEmpty()
                where ((order.DeliveryTime >= start && order.DeliveryTime < endExclusive)
                        || (order.DeliveryTime >= januaryStart && order.DeliveryTime < januaryEnd))
                join category in _categoryService.GetAll().AsNoTracking() on order.Customer.CategoryId equals category.Id into tmp
                from category in tmp.DefaultIfEmpty()
                select new MonthSaleStatRow
                {
                    DateTime = order.DeliveryTime,
                    CategoryId = order.Customer.CategoryId,
                    CategoryName = category != null ? category.Name : string.Empty,
                    CustomerId = order.CustomerId,
                    CustomerName = order.Customer.Name,
                    DeliveryAmount = saleDetail != null ? (includeTax ? saleDetail.PriceHaveTax : saleDetail.Price) * detail.DeliveryQuantity : 0m,
                    ReturnAmount = saleDetail != null ? (includeTax ? saleDetail.PriceHaveTax : saleDetail.Price) * detail.ReturnQuantity : 0m
                }).ToListAsync();
            rows.AddRange(deliveries);

            var report = BuildMonthSaleStat(month, rows, customerName, financeDate);
            report.IncludeTax = includeTax;
            return report;
        }

        private static Workbook BuildMonthSaleWorkbook(MonthSaleDataStatDto report, string? customerName,string templatePath)
        {
            var workbook = new Workbook(templatePath);
            var sheet = workbook.Worksheets[0];
            var subtotalStyle = sheet.Cells[5, 1].GetStyle();
            var totalStyle = sheet.Cells[6, 1].GetStyle();
            sheet.Cells.DeleteRows(5, 2, true); // 读取模板中的小计/总计样式后移除样式原型行。
            var weekCount = report.Weeks.Count;
            if (weekCount < 6)
            {
                sheet.Cells.DeleteColumns(2 + weekCount, 6 - weekCount, true);
                sheet.Cells.DeleteColumns(3 + 2 * weekCount, 6 - weekCount, true);
            }

            var fields = new List<string> { "Rank", "CustomerName" };
            fields.AddRange(report.Weeks.Select(x => $"OrderWeek{x.WeekNo}"));
            fields.Add("OrderAmount");
            fields.AddRange(report.Weeks.Select(x => $"DeliveryWeek{x.WeekNo}"));
            fields.AddRange(new[] { "ReturnAmount", "DeliveryAmount", "LastYearOrderAmount", "YearOnYearRate", "DeliveryShareRate" });
            var data = new DataTable("Rows");
            foreach (var field in fields) data.Columns.Add(field, typeof(object));
            var totalRow = 4 + report.Categories.Sum(x => x.Customers.Count + 1);
            foreach (var category in report.Categories)
            {
                var startRow = 4 + data.Rows.Count;
                foreach (var customer in category.Customers)
                    AddMonthSaleExportRow(data, customer, customer.CustomerName, customer.Rank, null, totalRow);
                AddMonthSaleExportRow(data, category, category.CategoryName + "小计", null, startRow, totalRow);
            }
            AddMonthSaleExportRow(data, report, "总计", null, 4, totalRow);

            var designer = new WorkbookDesigner { Workbook = workbook };
            designer.SetDataSource(data);
            designer.SetDataSource("Title", $"{report.Year}年{report.Month}月订单及销售发货月报表（{(report.IncludeTax ? "含税" : "不含税")}）");
            designer.SetDataSource("Period", $"账期：{report.StartDate:yyyy-MM-dd} 至 {report.EndDate:yyyy-MM-dd}；"
                + $"客户：{(string.IsNullOrWhiteSpace(customerName) ? "全部" : customerName.Trim())}；分类内按本年1月净发货额排名。"
                + "同比＝（今年发货额－去年同期销售额）÷去年同期销售额绝对值。");
            designer.SetDataSource("DeliveryTitle", $"{report.Year}年{report.Month}月发货额");
            designer.SetDataSource("LastYearTitle", $"{report.Year - 1}年{report.Month}月销售额");
            foreach (var week in report.Weeks)
                designer.SetDataSource($"Week{week.WeekNo}", $"{week.StartDate:MM-dd}至{week.EndDate:MM-dd}");
            designer.Process(0, false);
            designer.ClearDataSource();
            for (var i = 0; i < data.Rows.Count; i++)
            {
                if (!data.Rows[i].IsNull("Rank")) continue;
                sheet.Cells.CreateRange(4 + i, 0, 1, data.Columns.Count).ApplyStyle(
                    i == data.Rows.Count - 1 ? totalStyle : subtotalStyle,
                    new StyleFlag { FontBold = true, FontItalic = true });
            }
            sheet.PageSetup.Orientation = PageOrientationType.Landscape;
            sheet.PageSetup.PrintTitleRows = "$3:$4";
            sheet.PageSetup.FitToPagesWide = 1;
            sheet.PageSetup.FitToPagesTall = 0;
            workbook.Settings.FormulaSettings.CalculationMode = CalcModeType.Automatic;
            workbook.CalculateFormula();
            return workbook;
        }

        private static void AddMonthSaleExportRow(DataTable data, MonthSaleStatBaseDto stat, string name,
            int? rank, int? startRow, int totalRow)
        {
            var weekCount = (data.Columns.Count - 8) / 2;
            var row = 4 + data.Rows.Count;
            var orderTotalColumn = 2 + weekCount;
            var deliveryStartColumn = orderTotalColumn + 1;
            var returnColumn = deliveryStartColumn + weekCount;
            var deliveryColumn = returnColumn + 1;
            var lastYearColumn = returnColumn + 2;
            var values = new object[data.Columns.Count];
            values[0] = rank.HasValue ? (object)rank.Value : DBNull.Value;
            values[1] = name;
            if (startRow.HasValue)
            {
                // formula 智能标记直接填入 SUBTOTAL，总计会忽略范围内的分类小计。
                for (var column = 2; column <= lastYearColumn; column++)
                    values[column] = row == startRow.Value ? "=0"
                        : $"=SUBTOTAL(9,{CellsHelper.CellIndexToName(startRow.Value, column)}:{CellsHelper.CellIndexToName(row - 1, column)})";
            }
            else
            {
                for (var i = 0; i < weekCount; i++)
                {
                    values[2 + i] = (double)(stat.OrderWeeks.FirstOrDefault(x => x.WeekNo == i + 1)?.Amount ?? 0m);
                    values[deliveryStartColumn + i] = (double)(stat.DeliveryWeeks.FirstOrDefault(x => x.WeekNo == i + 1)?.Amount ?? 0m);
                }
                values[orderTotalColumn] = $"=SUM({CellsHelper.CellIndexToName(row, 2)}:{CellsHelper.CellIndexToName(row, orderTotalColumn - 1)})";
                values[deliveryColumn] = $"=SUM({CellsHelper.CellIndexToName(row, deliveryStartColumn)}:{CellsHelper.CellIndexToName(row, returnColumn - 1)})";
                values[returnColumn] = (double)stat.ReturnAmount;
                values[lastYearColumn] = (double)stat.LastYearOrderAmount;
            }
            var delivery = CellsHelper.CellIndexToName(row, deliveryColumn);
            var lastYear = CellsHelper.CellIndexToName(row, lastYearColumn);
            var totalDelivery = $"${CellsHelper.ColumnIndexToName(deliveryColumn)}${totalRow + 1}";
            values[returnColumn + 3] = $"=IF({lastYear}=0,\"—\",ROUND(({delivery}-{lastYear})/ABS({lastYear}),6))";
            values[returnColumn + 4] = $"=IF({totalDelivery}=0,\"—\",ROUND({delivery}/{totalDelivery},6))";
            data.Rows.Add(values);
        }

        /// <summary>销售周金额下钻。单头金额只累计一次，保留没有产品明细的销售单。</summary>
        [HttpGet("GetWeekSaleOrderDetails")]
        public async Task<IActionResult> GetWeekSaleOrderDetails([FromQuery] MonthSaleWeekDetailQueryDto dto)
        {
            var week = GetReportWeek(dto);
            var end = week.EndDate.AddDays(1);
            var keyword = dto.CustomerName?.Trim().ToLower();
            var rows = await (
                from order in _saleOrderService.GetAll().AsNoTracking()
                where order.Status != SaleOrderStatus.待下单
                    && order.CreateTime >= week.StartDate && order.CreateTime < end
                //join catetory in _categoryService.GetAll().AsNoTracking() on order.CustomerId equals catetory.Id
                where (!dto.CustomerId.HasValue || order.CustomerId == dto.CustomerId.Value)
                    && (!dto.FilterCategory || order.Customer.CategoryId == dto.CategoryId)
                    && (string.IsNullOrEmpty(keyword) || order.Customer.Name.ToLower().Contains(keyword))
                join detail in _saleOrderDetailService.GetAll().AsNoTracking() on order.Id equals detail.SaleOrderId into details
                from detail in details.DefaultIfEmpty()
                join product in _historyProductService.GetAll(true).AsNoTracking()
                    on (detail != null ? detail.HistoryProductId : null) equals product.Id into products
                from product in products.DefaultIfEmpty()
                orderby order.CreateTime, order.Id, detail.Id
                select new MonthSaleWeekDetailDto
                {
                    OrderId = order.Id,
                    OrderNo = order.OrderNo,
                    SaleOrderNo = order.OrderNo,
                    CustomerId = order.CustomerId,
                    CustomerName = order.Customer.Name,
                    DateTime = order.CreateTime,
                    CustomerOrderNo = order.CustomerOrderNo ?? string.Empty,
                    DetailId = detail != null ? detail.Id : null,
                    CustomerMaterialNo = detail != null ? detail.CustomerMaterialNo ?? string.Empty : string.Empty,
                    ProductNo = product != null ? product.ProductNo : string.Empty,
                    ProductName = product != null ? product.ProductName : string.Empty,
                    Specification = product != null ? product.Specification ?? string.Empty : string.Empty,
                    Version = product != null ? product.Version ?? string.Empty : string.Empty,
                    Unit = product != null ? product.Unit : string.Empty,
                    Quantity = detail != null ? detail.PlaceOrderQuantity : null,
                    Price = detail != null ? detail.Price : null,
                    PriceHaveTax = detail != null ? detail.PriceHaveTax : null,
                    TotalPrice = detail != null ? detail.TotalPrice : null,
                    TotalPriceHaveTax = detail != null ? detail.TotalPriceHaveTax : null,
                    OrderTotalPrice = order.OrderTotalPrice,
                    OrderTotalPriceHaveTax = order.OrderTotalPriceHaveTax,
                    MoldCost = detail != null ? (decimal)detail.MoldCost : null,
                    Remark = detail != null ? detail.Remark ?? string.Empty : order.Remark ?? string.Empty
                }).ToListAsync();
            foreach (var row in rows) row.Id = $"order-{row.OrderId}-detail-{row.DetailId}";
            return Ok(new MonthSaleWeekDetailResultDto
            {
                Rows = rows,
                OrderCount = rows.Select(x => x.OrderId).Distinct().Count(),
                DetailCount = rows.Count(x => x.DetailId.HasValue),
                TotalAmount = rows.GroupBy(x => x.OrderId).Sum(x => dto.IncludeTax ? x.First().OrderTotalPriceHaveTax ?? 0 : x.First().OrderTotalPrice ?? 0)
            });
        }

        /// <summary>送货/退货金额下钻，单价取销售明细；退货可查询整个财务月。</summary>
        [HttpGet("GetWeekDeliveryDetails")]
        public async Task<IActionResult> GetWeekDeliveryDetails([FromQuery] MonthSaleWeekDetailQueryDto dto)
        {
            var week = GetReportWeek(dto);
            var end = week.EndDate.AddDays(1);
            var keyword = dto.CustomerName?.Trim().ToLower();
            var rows = await (
                from detail in _deliveryOrderDetailService.GetAll().AsNoTracking()
                join order in _deliveryOrderService.GetAll().AsNoTracking() on detail.DeliveryOrderId equals order.Id
                where order.DeliveryTime >= week.StartDate && order.DeliveryTime < end
                    && (!dto.OnlyReturns || detail.ReturnQuantity > 0)
                //join partner in _partnerService.GetAll().AsNoTracking() on order.CustomerId equals partner.Id
                where (!dto.CustomerId.HasValue || order.CustomerId == dto.CustomerId.Value)
                    && (!dto.FilterCategory || order.Customer.CategoryId == dto.CategoryId)
                    && (string.IsNullOrEmpty(keyword) || order.Customer.Name.ToLower().Contains(keyword))
                join saleDetail in _saleOrderDetailService.GetAll(true).AsNoTracking()
                    on detail.SaleOrderDetailId equals saleDetail.Id into saleDetails
                from saleDetail in saleDetails.DefaultIfEmpty()
                join saleOrder in _saleOrderService.GetAll().AsNoTracking()
                    on (saleDetail != null ? saleDetail.SaleOrderId : (long?)null) equals saleOrder.Id into saleOrders
                from saleOrder in saleOrders.DefaultIfEmpty()
                join product in _historyProductService.GetAll(true).AsNoTracking()
                    on detail.HistoryProductId equals product.Id into products
                from product in products.DefaultIfEmpty()
                orderby order.DeliveryTime, order.Id, detail.Id
                select new MonthSaleWeekDetailDto
                {
                    OrderId = order.Id,
                    OrderNo = order.OrderNo,
                    SaleOrderNo = saleOrder != null ? saleOrder.OrderNo : string.Empty,
                    CustomerId = order.CustomerId,
                    CustomerName = order.Customer.Name,
                    DateTime = order.DeliveryTime,
                    DetailId = detail.Id,
                    DetailNo = detail.DetailNo,
                    CustomerOrderNo = saleOrder != null ? saleOrder.CustomerOrderNo ?? string.Empty : string.Empty,
                    CustomerMaterialNo = product != null ? product.CustomerMaterialNo ?? string.Empty : string.Empty,
                    ProductNo = product != null ? product.ProductNo : string.Empty,
                    ProductName = product != null ? product.ProductName : string.Empty,
                    Specification = product != null ? product.Specification ?? string.Empty : string.Empty,
                    Version = product != null ? product.Version ?? string.Empty : string.Empty,
                    Unit = product != null ? product.Unit : string.Empty,
                    Quantity = detail.DeliveryQuantity,
                    Price = saleDetail != null ? saleDetail.Price : null,
                    PriceHaveTax = saleDetail != null ? saleDetail.PriceHaveTax : null,
                    TotalPrice = saleDetail != null ? saleDetail.Price * detail.DeliveryQuantity : null,
                    TotalPriceHaveTax = saleDetail != null ? saleDetail.PriceHaveTax * detail.DeliveryQuantity : null,
                    ReturnQuantity = detail.ReturnQuantity,
                    ReturnAmount = saleDetail != null ? saleDetail.Price * detail.ReturnQuantity : null,
                    ReturnAmountHaveTax = saleDetail != null ? saleDetail.PriceHaveTax * detail.ReturnQuantity : null,
                    Remark = detail.Remark ?? string.Empty
                }).ToListAsync();
            foreach (var row in rows) row.Id = $"delivery-{row.DetailId}";
            return Ok(new MonthSaleWeekDetailResultDto
            {
                Rows = rows,
                OrderCount = rows.Select(x => x.OrderId).Distinct().Count(),
                DetailCount = rows.Count,
                TotalAmount = rows.Sum(x => dto.OnlyReturns
                    ? (dto.IncludeTax ? x.ReturnAmountHaveTax : x.ReturnAmount) ?? 0
                    : (dto.IncludeTax ? x.TotalPriceHaveTax : x.TotalPrice) ?? 0)
            });
        }

        private int GetFinanceDate()
        {
            var paras = _systemParaService.GetSystermParaByTypesAndCode(new List<SystemParaType> { SystemParaType.财务结算日期 }, null);
            if (paras.Count == 0) throw new Exception("未配置系统参数，财务结算日期");
            if (!int.TryParse(paras.First().SystemPara.DataValue, out var financeDate) || financeDate < 1 || financeDate > 31)
                throw new Exception("财务结算日期配置无效");
            return financeDate;
        }

        private FinanceWeekDto GetReportWeek(MonthSaleWeekDetailQueryDto dto)
        {
            if (dto.Month.Year < 2 || dto.Month.Year > 9998 || (dto.Month.Year == 2 && dto.Month.Month == 1)) throw new Exception("请选择有效的开账月份");
            var financeDate = GetFinanceDate();
            if (dto.OnlyReturns && dto.WeekNo == 0)
            {
                var (start, end) = GetFinancePeriod(dto.Month, financeDate);
                return new FinanceWeekDto { StartDate = start, EndDate = end.AddDays(-1), Days = (end - start).Days };
            }
            var weeks = GetFinanceWeeks(dto.Month.Year, dto.Month.Month, financeDate);
            var week = weeks.FirstOrDefault(x => x.WeekNo == dto.WeekNo);
            if (week == null) throw new Exception("请选择有效的财务周");
            return week;
        }

        /// <summary>按客户计算一次统计，小计及总计累加客户结果，不重复扫描原始数据。</summary>
        private static MonthSaleDataStatDto BuildMonthSaleStat(DateTime month, List<MonthSaleStatRow> rows, string? customerName, int financeDate)
        {
            var weeks = GetFinanceWeeks(month.Year, month.Month, financeDate);
            var start = weeks[0].StartDate;
            var endExclusive = weeks[weeks.Count - 1].EndDate.AddDays(1);
            var (lastYearStart, lastYearEnd) = GetFinancePeriod(month.AddYears(-1), financeDate);
            var (januaryStart, januaryEnd) = GetFinancePeriod(new DateTime(month.Year, 1, 1), financeDate);
            var keyword = customerName?.Trim();
            var result = new MonthSaleDataStatDto
            {
                Year = month.Year,
                Month = month.Month,
                StartDate = start,
                EndDate = endExclusive.AddDays(-1),
                Weeks = weeks,
                OrderWeeks = CreateWeekStats(weeks),
                DeliveryWeeks = CreateWeekStats(weeks)
            };

            foreach (var group in rows.GroupBy(x => x.CategoryId))
            {
                var category = new MonthSaleCategoryStatDto
                {
                    CategoryId = group.Key,
                    CategoryName = group.Select(x => x.CategoryName).FirstOrDefault(x => !string.IsNullOrWhiteSpace(x)) ?? "未分类",
                    OrderWeeks = CreateWeekStats(weeks),
                    DeliveryWeeks = CreateWeekStats(weeks)
                };
                foreach (var customerGroup in group.GroupBy(x => x.CustomerId))
                {
                    var customer = new MonthSaleCustomerStatDto
                    {
                        CustomerId = customerGroup.Key,
                        CustomerName = customerGroup.Select(x => x.CustomerName).FirstOrDefault(x => !string.IsNullOrWhiteSpace(x)) ?? string.Empty,
                        OrderWeeks = CreateWeekStats(weeks),
                        DeliveryWeeks = CreateWeekStats(weeks)
                    };
                    foreach (var row in customerGroup)
                    {
                        var netAmount = row.DeliveryAmount - row.ReturnAmount;
                        if (row.DateTime >= lastYearStart && row.DateTime < lastYearEnd) customer.LastYearOrderAmount += row.OrderAmount;
                        if (row.DateTime >= januaryStart && row.DateTime < januaryEnd) customer.JanuaryNetDeliveryAmount += netAmount;
                        if (row.DateTime < start || row.DateTime >= endExclusive) continue;

                        customer.OrderAmount += row.OrderAmount;
                        customer.DeliveryAmount += row.DeliveryAmount;
                        customer.ReturnAmount += row.ReturnAmount;
                        var weekIndex = weeks.FindIndex(x => row.DateTime >= x.StartDate && row.DateTime < x.EndDate.AddDays(1));
                        customer.OrderWeeks[weekIndex].Amount += row.OrderAmount;
                        customer.DeliveryWeeks[weekIndex].Amount += row.DeliveryAmount;
                    }
                    CompleteStat(customer);
                    category.Customers.Add(customer);
                }
                category.Customers = category.Customers.OrderByDescending(x => x.JanuaryNetDeliveryAmount)
                    .ThenBy(x => x.CustomerName).ThenBy(x => x.CustomerId).ToList();
                for (var index = 0; index < category.Customers.Count; index++) category.Customers[index].Rank = index + 1;
                // 搜索保留原排名；小计及总计只统计当前筛选结果。
                if (!string.IsNullOrEmpty(keyword))
                {
                    category.Customers = category.Customers.Where(x => x.CustomerName.Contains(keyword, StringComparison.OrdinalIgnoreCase)).ToList();
                }
                if (category.Customers.Count == 0) continue;
                foreach (var customer in category.Customers) AddStat(category, customer);
                CompleteStat(category);
                result.Categories.Add(category);
                AddStat(result, category);
            }
            result.Categories = result.Categories.OrderBy(x => x.CategoryName).ThenBy(x => x.CategoryId).ToList();
            CompleteStat(result);
            // 占比统一使用当前筛选结果的送货总额，不能累加或平均各客户比例。
            result.DeliveryShareRate = GetDeliveryShareRate(result.DeliveryAmount, result.DeliveryAmount);
            foreach (var category in result.Categories)
            {
                category.DeliveryShareRate = GetDeliveryShareRate(category.DeliveryAmount, result.DeliveryAmount);
                foreach (var customer in category.Customers)
                {
                    customer.DeliveryShareRate = GetDeliveryShareRate(customer.DeliveryAmount, result.DeliveryAmount);
                }
            }
            return result;
        }

        private static List<MonthSaleWeekStatDto> CreateWeekStats(List<FinanceWeekDto> weeks)
        {
            return weeks.Select(x => new MonthSaleWeekStatDto
            {
                WeekNo = x.WeekNo
            }).ToList();
        }

        private static void AddStat(MonthSaleStatBaseDto target, MonthSaleStatBaseDto source)
        {
            target.OrderAmount += source.OrderAmount;
            target.DeliveryAmount += source.DeliveryAmount;
            target.ReturnAmount += source.ReturnAmount;
            target.JanuaryNetDeliveryAmount += source.JanuaryNetDeliveryAmount;
            target.LastYearOrderAmount += source.LastYearOrderAmount;
            for (var index = 0; index < target.OrderWeeks.Count; index++)
            {
                target.OrderWeeks[index].Amount += source.OrderWeeks[index].Amount;
                target.DeliveryWeeks[index].Amount += source.DeliveryWeeks[index].Amount;
            }
        }

        private static void CompleteStat(MonthSaleStatBaseDto target)
        {
            target.NetDeliveryAmount = target.DeliveryAmount - target.ReturnAmount;
            target.YearOnYearRate = GetGrowthRate(target.DeliveryAmount, target.LastYearOrderAmount);
        }

        private static decimal? GetGrowthRate(decimal current, decimal previous)
        {
            return previous == 0 ? null : Math.Round((current - previous) / Math.Abs(previous), 6, MidpointRounding.AwayFromZero);
        }

        private static decimal? GetDeliveryShareRate(decimal amount, decimal total)
        {
            return total == 0 ? null : Math.Round(amount / total, 6, MidpointRounding.AwayFromZero);
        }

        private class MonthSaleStatRow
        {
            public DateTime DateTime { get; set; }
            public long? CategoryId { get; set; }
            public string CategoryName { get; set; } = string.Empty;
            public long CustomerId { get; set; }
            public string CustomerName { get; set; } = string.Empty;
            public decimal OrderAmount { get; set; }
            public decimal DeliveryAmount { get; set; }
            public decimal ReturnAmount { get; set; }
        }

        /// <summary>所选月份为账期结束月；结算日超过当月天数时取月末，查询使用左闭右开区间。</summary>
        private static (DateTime Start, DateTime EndExclusive) GetFinancePeriod(DateTime month, int financeDate)
        {
            if (financeDate < 1 || financeDate > 31) throw new ArgumentOutOfRangeException(nameof(financeDate), "财务结算日期配置无效");
            if (month.Year == 1 && month.Month == 1) throw new ArgumentOutOfRangeException(nameof(month), "无效的财务月份");
            var endMonth = new DateTime(month.Year, month.Month, 1);
            var startMonth = endMonth.AddMonths(-1);
            var start = startMonth.AddDays(Math.Min(financeDate, DateTime.DaysInMonth(startMonth.Year, startMonth.Month)) - 1);
            var endExclusive = endMonth.AddDays(Math.Min(financeDate, DateTime.DaysInMonth(endMonth.Year, endMonth.Month)) - 1);
            return (start, endExclusive);
        }

        /// <summary>例如3月为2月26日至3月25日；周六至周五，首尾周按账期截断。</summary>
        public static List<FinanceWeekDto> GetFinanceWeeks(int year, int month, int financeDate)
        {
            if (year < 1 || year > 9999 || month < 1 || month > 12)
                throw new ArgumentOutOfRangeException(nameof(month), "无效的财务月份");
            var (start, endExclusive) = GetFinancePeriod(new DateTime(year, month, 1), financeDate);
            var end = endExclusive.AddDays(-1);
            var result = new List<FinanceWeekDto>();
            var current = start;
            while (current <= end)
            {
                var daysToFriday = ((int)DayOfWeek.Friday - (int)current.DayOfWeek + 7) % 7;
                var weekEnd = current.AddDays(Math.Min(daysToFriday, (end - current).Days));
                result.Add(new FinanceWeekDto
                {
                    WeekNo = result.Count + 1,
                    StartDate = current,
                    EndDate = weekEnd,
                    Days = (weekEnd - current).Days + 1
                });
                current = weekEnd.AddDays(1);
            }
            return result;
        }
    }
}
