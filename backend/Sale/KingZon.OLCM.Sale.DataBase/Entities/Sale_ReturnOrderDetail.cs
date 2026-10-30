using KingZon.Core.DataBase;
using KingZon.OLCM.Base.Shared.DataBase.Entities;
using KingZon.OLCM.Sale.Shared.DataBase.Enum;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel.DataAnnotations.Schema;

namespace KingZon.OLCM.Sale.Shared.DataBase.Entities
{
    public class SaleReturnOrderDetailMap : KingZonEntityTypeConfiguration<Sale_ReturnOrderDetail>
    {
        public override void Configure(EntityTypeBuilder<Sale_ReturnOrderDetail> builder)
        {
            builder.HasKey(x => x.Id);
            builder.HasOne(builder => builder.SaleReturnOrder)
                    .WithMany()
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasForeignKey(builder => builder.SaleReturnOrderId)
                    .IsRequired(true);
            builder.HasOne(builder => builder.Goods)
                     .WithMany()
                     .OnDelete(DeleteBehavior.Restrict)
                     .HasForeignKey(builder => builder.GoodsId)
                     .IsRequired(false);
            builder.HasOne(builder => builder.Product)
                 .WithMany()
                 .OnDelete(DeleteBehavior.Restrict)
                 .HasForeignKey(builder => builder.ProductId)
                 .IsRequired(true);
            builder.HasOne(builder => builder.Specification)
               .WithMany()
               .OnDelete(DeleteBehavior.Restrict)
               .HasForeignKey(builder => builder.SpecificationId)
               .IsRequired(false);
            var deliveryType = Type.GetType("KingZon.OLCM.Inventory.Shared.DataBase.Entities.Inventory_DeliveryOrderDetail, KingZon.OLCM.Inventory.Shared");
            builder.HasOne(deliveryType!, null)
                .WithMany()
                .HasForeignKey(nameof(Sale_ReturnOrderDetail.DeliveryOrderDetailId))
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
    public class Sale_ReturnOrderDetail : BaseEntity
    {
        public new long Id { get; set; }
        /// <summary>
        /// 退货数量( 主单位 )
        /// </summary>
        public decimal ReturnQuantity { get; set; }
        /// <summary>
        /// 用户退货时选择的单位
        /// -1 表示主单位
        /// </summary>
        public long ReturnUnitConversionId { get; set; }
        /// <summary>
        /// 用户实际输入的退货数量
        /// </summary>
        public decimal InputReturnQuantity { get; set; }
        /// <summary>
        /// 退货入库数量
        /// </summary>
        public decimal InventoryQuantity { get; set; }
        /// <summary>
        /// 是否完成
        /// </summary>
        public bool IsComplete { get; set; } = false;
        /// <summary>
        /// 单价
        /// </summary>
        [Column(TypeName = "decimal(12,4)")]
        public decimal Price { get; set; }
        /// <summary>
        /// 总价
        /// </summary>
        [Column(TypeName = "decimal(12,4)")]
        public decimal TotalPrice { get; set; }
        /// <summary>
        /// 退货原因
        /// </summary>
        public string? Reason { get; set; }
        /// <summary>
        /// 备注
        /// </summary>
        public string? Remark { get; set; }
        /// <summary>
        /// 送货单明细ID
        /// </summary>
        public long? DeliveryOrderDetailId { get; set; }
        /// <summary>
        /// 退货单ID
        /// </summary>
        public long SaleReturnOrderId { get; set; }
        /// <summary>
        /// 产品ID
        /// </summary>
        public long ProductId { get; set; }
        /// <summary>
        /// 商品ID
        /// </summary>
        public long? GoodsId { get; set; }
        /// <summary>
        /// 规格ID
        /// </summary>
        public long? SpecificationId { get; set; }
        /// <summary>
        /// 是否对账
        /// </summary>
        public bool IsAccount { get; set; }
        /// <summary>
        /// 多单位信息
        /// </summary>
        public string? UnitInfo {  get; set; }
        /// <summary>
        /// 退货类型
        /// </summary>
        public SaleReturnType SaleReturnType { get; set; }
        /// <summary>
        /// 客户产品图号，若为送货单生成退货详情记录，则新增时复制对应销售单的客户产品图号
        /// </summary>
        public string? CustomerMaterialNo { get; set; }
        #region 导航属性 

        public Sale_ReturnOrder SaleReturnOrder { get; set; } = null!;
        public Base_Product Product { get; set; } = null!;
        public Base_ProductSpecification? Specification { get; set; }
        public Base_Goods? Goods { get; set; }


        #endregion
    }
}
