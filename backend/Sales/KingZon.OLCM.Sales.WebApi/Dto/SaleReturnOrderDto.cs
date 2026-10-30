using KingZon.OLCM.Common.DataBase.Entities;


namespace KingZon.OLCM.Sales.WebApi.Dto
{
    public class SaleReturnOrderDto
    {
        public int CustomerId { get; set; }

        public DateTime ReturnTime { get; set; }


        public List<PostSaleReturnOrderDetailDTO> SaleReturnOrderDetailDtos { get; set; } = new List<PostSaleReturnOrderDetailDTO>();
        public string? Remark { get; set; } 
    }

    public class HistoryProductDelivery: HistoryProduct
    {
        /// <summary>
        /// 已送货总数
        /// </summary>
        public int DeliveryCount { get; set; }
        /// <summary>
        /// 已退货总数
        /// </summary>
        public int ReturnCount { get; set; }
        public decimal? Price { get; set; }
        public HistoryProductDelivery(string productNo, string productName, string unit) : base(productNo, productName, unit)
        {
        }
    }

    public class SaleReturnOrderDTO : SaleReturnOrder
    {
        public List<SaleReturnOrderDetialDTO> SaleReturnOrderDetails { get; set; } = [];
        public SaleReturnOrderDTO(string orderNo) : base(orderNo)
        { 
        }
    }
    public class SaleReturnOrderDetialDTO : SaleReturnOrderDetail
    {
        public int ReturnCount { get; set; }
        public int DeliveryCount { get; set; }
        public new int HistoryProductId { get; set; }
    }
}
