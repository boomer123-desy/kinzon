using KingZon.Core.DataBase;
using KingZon.OLCM.Base.Shared.DataBase.Enum;
using KingZon.OLCM.Base.Shared.DataBase.JsonStructure;
using System.Text.Json.Serialization;

namespace KingZon.OLCM.Inventory.WebApi.Dto
{
    public class GetReturnArrivalDetailsPostDto
    {
        public bool IsComplete { get; set; }
        public List<Filter> SaleReturnOrderFilterList { get; set; } = [];
        public List<Filter> SaleReturnOrderDetailFilterList { get; set; } = [];
        public List<Filter> ProductFilterList { get; set; } = [];
        public List<Filter> CustomerFilterList { get; set; } = [];
    }

    public class ReturnArrivalDetailDto
    {
        public long Id { get; set; }
        public long SaleReturnOrderId { get; set; }
        public string SaleReturnOrderNo { get; set; } = string.Empty;
        public DateTime SaleReturnTime { get; set; }
        public string CustomerCode { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public long ProductId { get; set; }
        public string ProductNo { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string? ProductUnit { get; set; }
        public long? ProductUnitId { get; set; }
        public string? CustomerMaterialNo { get; set; }
        public string? CustomerMaterialName { get; set; }
        public string? TechnicalCode { get; set; }
        public string? ProductCategory { get; set; }
        public string? MaterialGrade { get; set; }
        public string? MaterialSpecification { get; set; }
        public string ProductSpecification { get; set; } = string.Empty;
        public string? ProductRemark { get; set; }
        public long ReturnUnitConversionId { get; set; }
        public decimal ReturnQuantity { get; set; }
        public decimal ArrivalQuantity { get; set; }
        public decimal RemainingQuantity { get; set; }
        public bool IsComplete { get; set; }
        public string? Reason { get; set; }
        public string? Remark { get; set; }
        public string? SaleReturnOrderRemark { get; set; }
        public InventoryDataType DataType { get; set; }
        public long DataId { get; set; }
        [JsonIgnore]
        public string? UnitInfo { get; set; }
        public List<InventoryUnitDto> UnitInfos { get; set; } = [];
    }

    public class PostReturnArrivalRecordDto
    {
        public long SaleReturnOrderDetailId { get; set; }
        public decimal ArrivalQuantity { get; set; }
        /// <summary>-1 表示产品主单位。</summary>
        public long ArrivalUnitConversionId { get; set; } = -1;
        public InventoryDataStatus DataStatus { get; set; }
        public string? Remark { get; set; }
        public DateTime ArrivalTime { get; set; }
    }

    public class GetReturnArrivalRecordsPostDto
    {
        public bool IsCompleted { get; set; }
        public List<Filter> ArrivalRecordFilterList { get; set; } = [];
        public List<Filter> SaleReturnOrderFilterList { get; set; } = [];
        public List<Filter> SaleReturnOrderDetailFilterList { get; set; } = [];
        public List<Filter> ProductFilterList { get; set; } = [];
        public List<Filter> CustomerFilterList { get; set; } = [];
    }

    public class ReturnArrivalRecordDto
    {
        public long Id { get; set; }
        public string ArrivalRecordNo { get; set; } = string.Empty;
        public long SaleReturnOrderDetailId { get; set; }
        public string SaleReturnOrderNo { get; set; } = string.Empty;
        public string CustomerCode { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public long ProductId { get; set; }
        public InventoryDataType DataType { get; set; }
        public long DataId { get; set; }
        public string ProductNo { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string? ProductUnit { get; set; }
        public long? ProductUnitId { get; set; }
        public string? CustomerMaterialNo { get; set; }
        public string? CustomerMaterialName { get; set; }
        public string? TechnicalCode { get; set; }
        public string? ProductCategory { get; set; }
        public string? MaterialGrade { get; set; }
        public string? MaterialSpecification { get; set; }
        public string ProductSpecification { get; set; } = string.Empty;
        public string? ProductRemark { get; set; }
        public DateTime ArrivalTime { get; set; }
        public decimal ArrivalQuantity { get; set; }
        public decimal BaseArrivalQuantity { get; set; }
        public decimal InventoryQuantity { get; set; }
        public string ArrivalUnit { get; set; } = string.Empty;
        public long ArrivalUnitConversionId { get; set; }
        public bool IsCompleted { get; set; }
        public string? Remark { get; set; }
        public string? SaleReturnOrderRemark { get; set; }
        [JsonIgnore]
        public string? UnitConversionList { get; set; }
        public List<InventoryUnitDto> UnitInfos { get; set; } = [];
    }

    /// <summary>
    /// 批量入库查询返回的待到货入库数据。
    /// </summary>
    public class ReturnArrivalInventoryRecordDto
    {
        public long Id { get; set; }
        public string ArrivalRecordNo { get; set; } = string.Empty;
        public string SaleReturnOrderNo { get; set; } = string.Empty;
        public string? CustomerCode { get; set; }
        public string? CustomerName { get; set; }
        public long ProductId { get; set; }
        public string? ProductNo { get; set; }
        public string? ProductName { get; set; }
        public string? ProductUnit { get; set; }
        public long? ProductUnitId { get; set; }
        public string? CustomerMaterialNo { get; set; }
        public string? CustomerMaterialName { get; set; }
        public string? ProductSpecification { get; set; }
        public string? TechnicalCode { get; set; }
        public DateTime? ArrivalTime { get; set; }
        public decimal ArrivalQuantity { get; set; }
        public decimal BaseArrivalQuantity { get; set; }
        public decimal InventoryQuantity { get; set; }
        public decimal RemainingQuantity { get; set; }
        public string? ArrivalUnit { get; set; }
        public long? ArrivalUnitConversionId { get; set; }
        public InventoryDataType DataType { get; set; }
        public long DataId { get; set; }
        public string? Remark { get; set; }
        public string? UnitConversionList { get; set; }
        public List<InventoryUnitDto> UnitInfos { get; set; } = [];
    }

    public class PostReturnArrivalInventoryRecordDto
    {
        public List<InventoryUnitInboundDto> UnitQtyList { get; set; } = [];
        public long PositionId { get; set; }
        public string? Remark { get; set; }
    }

    /// <summary>到货保存并直接入库；所有行在同一事务内提交。</summary>
    public class PostReturnArrivalAndWarehousingDto
    {
        public List<PostReturnArrivalAndWarehousingItemDto> ArrivalRecords { get; set; } = [];
    }

    public class PostReturnArrivalAndWarehousingItemDto : PostReturnArrivalRecordDto
    {
        public List<PostReturnArrivalInventoryRecordDto> InventoryRecordList { get; set; } = [];
    }

    public class GetCompletedReturnArrivalInventoryRecordsPostDto
    {
        public List<Filter> ArrivalRecordFilterList { get; set; } = [];
        public List<Filter> SaleReturnOrderFilterList { get; set; } = [];
        public List<Filter> SaleReturnOrderDetailFilterList { get; set; } = [];
        public List<Filter> ProductFilterList { get; set; } = [];
        public List<Filter> CustomerFilterList { get; set; } = [];
    }

    /// <summary>已入库页的一条库存明细，保留其所属到货记录用于跨行合并。</summary>
    public class ReturnArrivalCompletedInventoryRecordDto
    {
        public long Id { get; set; }
        public string RecordNo { get; set; } = string.Empty;
        public long ArrivalRecordId { get; set; }
        public string ArrivalRecordNo { get; set; } = string.Empty;
        public DateTime ArrivalTime { get; set; }
        public string SaleReturnOrderNo { get; set; } = string.Empty;
        public string CustomerCode { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string? CustomerMaterialNo { get; set; }
        public string? CustomerMaterialName { get; set; }
        public string ProductNo { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string? ProductSpecification { get; set; }
        public string? TechnicalCode { get; set; }
        public decimal Quantity { get; set; }
        public long? PositionId { get; set; }
        public string? PositionName { get; set; }
        public string? Remark { get; set; }
        public string? InventoryUnitInfo { get; set; }
        public List<InventoryUnitDto> UnitInfos { get; set; } = [];
    }

    public class DeleteReturnArrivalInventoryRecordsPostDto
    {
        public List<long> Ids { get; set; } = [];
    }
}
