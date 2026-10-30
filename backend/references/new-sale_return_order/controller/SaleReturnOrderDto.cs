using KingZon.OLCM.Base.Shared.DataBase.JsonStructure;
using KingZon.OLCM.Sale.Shared.DataBase.Enum;

namespace KingZon.OLCM.Sale.WebApi.Dto
{
    public class DeliveryOrderDetailDTO
    {
        public long Id { get; set; }
        public long DeliveryOrderId { get; set; }
        public string? DeliveryOrderNo { get; set; }
        public decimal DeliveryQuantity { get; set; }
        public decimal ActualDeliveryQuantity { get; set; }
        public decimal ReturnQuantity { get; set; }
        public string? Remark { get; set; }
        public DateTime? DeliveryTime { get; set; }
        /// <summary>送货明细实际出库时间。</summary>
        public DateTime? OutboundTime { get; set; }
        public long SaleOrderDetailId { get; set; }
        public long ProductId { get; set; }
        public string? ProductNo { get; set; }

        public decimal SaleOrderDetailPrice { get; set; }
        public string? ProductName { get; set; }
        /// <summary>当前产品资料库的技术型号。</summary>
        public string? ProductTechnicalCode { get; set; }
        /// <summary>
        /// ��Ʒ���
        /// </summary>
        public string? ProductSpecification { get; set; }
        public string? ProductMaterialGrade { get; set; }
        public string? ProductMaterialSpecification { get; set; }
        public string? ProductCustomerMaterialNo { get; set; }
        /// <summary>销售订单保存的历史产品资料。</summary>
        public string? HistoryProductNo { get; set; }
        public string? HistoryProductName { get; set; }
        public string? HistoryProductSpecification { get; set; }
        public string? HistoryProductTechnicalCode { get; set; }
        public string? HistoryCustomerMaterialNo { get; set; }
        public string? HistoryCustomerMaterialName { get; set; }
        /// <summary>
        /// ��Ʒ��λ
        /// </summary>
        public string? ProductUnit { get; set; }
        /// <summary>
        /// �ͻ�֪ͨ��
        /// </summary>
        public string? DeliveryNoticeNo { get; set; }
        public string? SaleOrderNo { get; set; }
        public List<InventoryUnitDto>? UnitInfos { get; set; }

        //�˻�����
        public SaleReturnType? SaleReturnType { get; set; }
    }

    public class GoodsDeliveryDto
    {
        public long GoodsId { get; set; }
        public long ProductId { get; set; }
        public long? SpecificationId { get; set; }
        /// <summary>
        /// 产品编码
        /// </summary>
        public string ProductNo { get; set; } = string.Empty;
        /// <summary>
        /// 产品名称
        /// </summary>
        public string ProductName { get; set; } = string.Empty;
        /// <summary>
        /// 规格型号
        /// </summary>
        public string? Specification { get; set; }
        /// <summary>
        /// 单位
        /// </summary>
        public string Unit { get; set; } = string.Empty;
        /// <summary>
        /// 材质牌号
        /// </summary>
        public string? MaterialGrade { get; set; }
        /// <summary>
        /// 产品备注
        /// </summary>
        public string? Remark { get; set; }
        /// <summary>
        /// 客户图号
        /// </summary>
        public string? CustomerMaterialNo { get; set; }
        /// <summary>客户产品名称。</summary>
        public string? CustomerMaterialName { get; set; }
        /// <summary>产品当前技术型号。</summary>
        public string? TechnicalCode { get; set; }
        /// <summary>
        /// 原材料规格
        /// </summary>
        public string? MaterialSpecification { get; set; }
        /// <summary>
        /// 已送货总数
        /// </summary>
        public decimal DeliveryCount { get; set; }
        /// <summary>
        /// 已退货总数
        /// </summary>
        public decimal ReturnCount { get; set; }
        public decimal? Price { get; set; }
        /// <summary>客户产品售价（销售单位）。</summary>
        public decimal SalePrice { get; set; }
        public long UnitId { get; set; }
        public string? SaleUnit { get; set; }
        public long? SaleUnitId { get; set; }
        public long? SalesUnitConversionId { get; set; }
        /// <summary>最近一次送货通知/出库时间，直接退货页仅作展示。</summary>
        public DateTime? DeliveryTime { get; set; }
        public DateTime? OutboundTime { get; set; }
        /// <summary>
        /// 客户产品可用的单位换算信息，数量字段均以产品主单位保存。
        /// </summary>
        public List<InventoryUnitDto> UnitInfos { get; set; } = [];
    }
    public class SaleReturnOrderDetailViewDTO
    {
        public long Id { get; set; }
        public decimal ReturnQuantity { get; set; }
        public decimal InventoryQuantity { get; set; }
        public decimal Price { get; set; }
        /// <summary>按输入退货单位换算后的单价，仅用于列表显示。</summary>
        public decimal InputPrice { get; set; }
        public decimal TotalPrice { get; set; }
        public bool IsComplete { get; set; }
        public bool IsAccount { get; set; }
        public string? Reason { get; set; }
        public string? Remark { get; set; }
        public SaleReturnType SaleReturnType { get; set; }
        public long? DeliveryOrderDetailId { get; set; }
        public string DeliveryOrderNo { get; set; } = string.Empty;
        public decimal DeliveryQuantity { get; set; }
        public decimal? InputReturnQuantity { get; set; }
        public long? InputUnitConversionId { get; set; }
        public decimal ActualDeliveryQuantity { get; set; }
        public decimal DeliveryOrderReturnQuantity { get; set; }
        public long CustomerId { get; set; }
        public string CustomerCode { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public long SaleReturnOrderId { get; set; }
        public string? SaleReturnOrderRemark { get; set; }
        public DateTime SaleReturnOrderReturnTime { get; set; }
        public string SaleReturnOrderNo { get; set; } = string.Empty;
        public decimal SaleReturnOrderTotalPrice { get; set; }
        public long ProductId { get; set; }
        public string ProductNo { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string? ProductMaterialSpecification { get; set; }
        public string? ProductMaterialGrade { get; set; }
        public string ProductSpecification { get; set; } = string.Empty;
        public string? ProductTechnicalCode { get; set; }
        public string ProductUnit { get; set; } = string.Empty;
        public string ProductCustomerMaterialNo { get; set; } = string.Empty;
        public string SaleOrderNo { get; set; } = string.Empty;
        public List<long> ArrivalRecordIds { get; set; } = [];
        public List<InventoryUnitOutboundDto> UnitInfos { get; set; } = new List<InventoryUnitOutboundDto>();
    }

    /// <summary>
    /// 新增销售退货单。
    /// 明细只接收送货单明细 ID 和本次退货信息，产品、单价等业务数据由服务端重新读取。
    /// </summary>
    public class PostSaleReturnOrderDTO
    {
        public long CustomerId { get; set; }
        public DateTime ReturnTime { get; set; }
        public string? Remark { get; set; }
        public List<PostSaleReturnOrderDetailDTO> Details { get; set; } = [];
    }

    public class PostSaleReturnOrderDetailDTO
    {
        /// <summary>
        /// 送货单退货时必填；产品直接退货时为 0。
        /// </summary>
        public long DeliveryOrderDetailId { get; set; }
        /// <summary>
        /// 产品直接退货时必填。价格、产品及规格信息均由服务端根据 GoodsId 重取。
        /// </summary>
        public long? GoodsId { get; set; }
        public decimal ReturnQuantity { get; set; }
        public long ReturnUnitId { get; set; }
        /// <summary>产品主单位单价，服务端仍会校验数量及产品归属。</summary>
        public decimal? Price { get; set; }
        public List<InventoryUnitDto> UnitInfos { get; set; } = [];
        public SaleReturnType SaleReturnType { get; set; }
        public string? Reason { get; set; }
        public string? Remark { get; set; }
    }

    /// <summary>
    /// 修改未完成的销售退货明细。
    /// </summary>
    public class UpdateSaleReturnOrderDetailDTO
    {
        public long Id { get; set; }
        public decimal ReturnQuantity { get; set; }
        public long ReturnUnitId { get; set; }
        public List<InventoryUnitDto> UnitInfos { get; set; } = [];
        public SaleReturnType SaleReturnType { get; set; }
        /// <summary>产品主单位单价。</summary>
        public decimal Price { get; set; }
        public string? Reason { get; set; }
        public string? Remark { get; set; }
    }

    /// <summary>
    /// 修改退货单主表信息。
    /// </summary>
    public class UpdateSaleReturnOrderDTO
    {
        public long Id { get; set; }
        public DateTime ReturnTime { get; set; }
        public string? Remark { get; set; }
    }

}
