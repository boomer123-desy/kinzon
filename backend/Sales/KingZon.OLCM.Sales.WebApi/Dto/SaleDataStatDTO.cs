/// <summary>
/// 月度销售送货统计公共数据
/// </summary>
public class MonthSaleStatBaseDto
{
    /// <summary>订单金额合计</summary>
    public decimal OrderAmount { get; set; }

    /// <summary>发货金额合计</summary>
    public decimal DeliveryAmount { get; set; }

    /// <summary>本行送货金额占当前查询送货总额的比例，0.1表示10%；总额为零时为null</summary>
    public decimal? DeliveryShareRate { get; set; }

    /// <summary>退货金额合计</summary>
    public decimal ReturnAmount { get; set; }

    /// <summary>净发货金额</summary>
    public decimal NetDeliveryAmount { get; set; }

    /// <summary>本年1月净发货额，用于客户排名</summary>
    public decimal JanuaryNetDeliveryAmount { get; set; }

    /// <summary>去年同财务月销售订单金额</summary>
    public decimal LastYearOrderAmount { get; set; }

    /// <summary>上一财务月净发货额</summary>
    public decimal PreviousMonthNetDeliveryAmount { get; set; }

    /// <summary>今年发货额相对去年销售额的增长率，0.1表示10%；去年销售额为零时为null</summary>
    public decimal? YearOnYearRate { get; set; }

    /// <summary>环比增长率，0.1表示10%；比较期为零时为null</summary>
    public decimal? MonthOnMonthRate { get; set; }

    /// <summary>每周销售订单金额</summary>
    public List<MonthSaleWeekStatDto> OrderWeeks { get; set; } = new();

    /// <summary>每周送货金额</summary>
    public List<MonthSaleWeekStatDto> DeliveryWeeks { get; set; } = new();
}

/// <summary>
/// 月度销售送货统计
/// </summary>
public class MonthSaleDataStatDto : MonthSaleStatBaseDto
{
    public bool IncludeTax { get; set; }
    public int Year { get; set; }

    public int Month { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    /// <summary>表头财务周日期范围，仅在报表顶层返回</summary>
    public List<FinanceWeekDto> Weeks { get; set; } = new();

    /// <summary>分类统计</summary>
    public List<MonthSaleCategoryStatDto> Categories { get; set; } = new();
}

/// <summary>
/// Category分类统计
/// </summary>
public class MonthSaleCategoryStatDto : MonthSaleStatBaseDto
{
    public long? CategoryId { get; set; }

    public string CategoryName { get; set; } = string.Empty;

    /// <summary>客户统计</summary>
    public List<MonthSaleCustomerStatDto> Customers { get; set; } = new();
}

/// <summary>
/// 客户统计
/// </summary>
public class MonthSaleCustomerStatDto : MonthSaleStatBaseDto
{
    public long CustomerId { get; set; }

    public string CustomerName { get; set; } = string.Empty;

    /// <summary>分类内按本年1月净发货额降序排列的序号</summary>
    public int Rank { get; set; }
}

/// <summary>
/// 财务周统计
/// </summary>
public class MonthSaleWeekStatDto
{
    public int WeekNo { get; set; }

    public decimal Amount { get; set; }
}

/// <summary>报表表头的财务周日期范围</summary>
public class FinanceWeekDto
{
    public int WeekNo { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public int Days { get; set; }
}

/// <summary>月报 Excel 导出，沿用月报查询的月份及客户条件。</summary>
public class MonthSaleExportQueryDto
{
    public bool IncludeTax { get; set; }
    public DateTime Month { get; set; }
    public string? CustomerName { get; set; }
}

/// <summary>周金额明细查询，日期范围由财务月和周号确定。</summary>
public class MonthSaleWeekDetailQueryDto
{
    public bool IncludeTax { get; set; }
    public bool OnlyReturns { get; set; }
    public DateTime Month { get; set; }
    public int WeekNo { get; set; }
    public long? CustomerId { get; set; }
    public bool FilterCategory { get; set; }
    public long? CategoryId { get; set; }
    public string? CustomerName { get; set; }
}

public class MonthSaleWeekDetailResultDto
{
    public decimal TotalAmount { get; set; }
    public int OrderCount { get; set; }
    public int DetailCount { get; set; }
    public List<MonthSaleWeekDetailDto> Rows { get; set; } = new();
}

/// <summary>销售/送货周明细；销售单头金额可重复显示，不可按行累计。</summary>
public class MonthSaleWeekDetailDto
{
    public string Id { get; set; } = string.Empty;
    public long OrderId { get; set; }
    public string OrderNo { get; set; } = string.Empty;
    public string SaleOrderNo { get; set; } = string.Empty;
    public long CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public DateTime DateTime { get; set; }
    public long? DetailId { get; set; }
    public string DetailNo { get; set; } = string.Empty;
    public string CustomerOrderNo { get; set; } = string.Empty;
    public string CustomerMaterialNo { get; set; } = string.Empty;
    public string ProductNo { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string Specification { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public int? Quantity { get; set; }
    public decimal? Price { get; set; }
    public decimal? PriceHaveTax { get; set; }
    public decimal? TotalPrice { get; set; }
    public decimal? TotalPriceHaveTax { get; set; }
    public decimal? OrderTotalPrice { get; set; }
    public decimal? OrderTotalPriceHaveTax { get; set; }
    public decimal? MoldCost { get; set; }
    public int? ReturnQuantity { get; set; }
    public decimal? ReturnAmount { get; set; }
    public decimal? ReturnAmountHaveTax { get; set; }
    public string Remark { get; set; } = string.Empty;
}
