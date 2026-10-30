using KingZon.Core.DataBase;
using KingZon.OLCM.Base.Shared.DataBase.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection.Metadata.Ecma335;

namespace KingZon.OLCM.Inventory.Shared.DataBase.Entities
{
    [Comment("送货订单明细")]
    public class Inventory_DeliveryOrderDetailMap : KingZonEntityTypeConfiguration<Inventory_DeliveryOrderDetail>
    {
        public override void Configure(EntityTypeBuilder<Inventory_DeliveryOrderDetail> builder)
        {
            builder.HasKey(x => x.Id);
            builder.HasOne(o => o.DeliveryOrder)
                .WithMany(x => x.DetialList)
                .HasForeignKey(o => o.DeliveryOrderId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(o => o.Product)
                .WithMany()
                .HasForeignKey(o => o.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(o => o.Specification)
                .WithMany()
                .HasForeignKey(o => o.SpecificationId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(o => o.Goods)
                .WithMany()
                .HasForeignKey(o => o.GoodsId)
                .OnDelete(DeleteBehavior.Restrict);

            var saleOrderDetail = Type.GetType("KingZon.OLCM.Sale.Shared.DataBase.Entities.Sale_OrderDetail, KingZon.OLCM.Sale.Shared");
            builder.HasOne(saleOrderDetail!, null)
                .WithMany()
                .HasForeignKey(nameof(Inventory_DeliveryOrderDetail.SaleOrderDetailId))
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(o => o.ProductPackage)
                .WithMany()
                .HasForeignKey(o => o.PackageId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }


    public class Inventory_DeliveryOrderDetail : BaseEntity
    {
        public new long Id { get; set; }
        /// <summary>
        /// 送货订单ID
        /// </summary>
        public long DeliveryOrderId { get; set; }
        /// <summary>
        /// 商品ID
        /// </summary>
        public long? GoodsId { get; set; }
        /// <summary>
        /// /产品ID
        /// </summary>
        public long ProductId { get; set; }
        /// <summary>
        /// 规格ID
        /// </summary>
        public long? SpecificationId { get; set; }
        /// <summary>
        /// 销售订单明细ID
        /// </summary>
        public long SaleOrderDetailId { get; set; }
        /// <summary>
        /// (通知)送货数量（主单位）
        /// </summary>
        [Column(TypeName = "decimal(12,4)")]
        public decimal DeliveryQuantity { get; set; }
        /// <summary>
        /// 实际送货数量(主单位）
        /// </summary>
        [Column(TypeName = "decimal(12,4)")]
        public decimal ActualDeliveryQuantity { get; set; }
        /// <summary>
        /// 退货数量（物料主单位）
        /// </summary>
        [Column(TypeName = "decimal(12,4)")]
        public decimal ReturnQuantity { get; set; }
        /// <summary>
        /// 是否已完成送货
        /// </summary>
        public bool IsCompleteDelivery { get; set; } = false;
        /// <summary>
        /// 出库时间
        /// </summary>
        public DateTime? OutboundTime { get; set; }
        /// <summary>
        /// 出库操作人
        /// </summary>
        public string? OutboundOperator { get; set; }
        /// <summary>
        /// 用户输入时选择的单位转换ID
        /// </summary>
        public long? InputUnitConversionId { get; set; }
        /// <summary>
        /// 用户输入时填入的数量（单位为用户选择的单位）
        /// </summary>
        [Column(TypeName = "decimal(12,4)")]
        public decimal? InputUnitQuantity { get; set; }
        /// <summary>
        /// 多单位转换率
        /// </summary>
        [Column(TypeName = "json")]
        public string? UnitInfo { get; set; }

        /// <summary>
        /// 阿米巴收入Income.Id
        /// </summary>
        public long? IncomeId { get; set; }

        /// <summary>
        /// 外包装方案Id
        /// </summary>
        public long? PackageId { get; set; }
        /// <summary>
        /// 使用托盘数量
        /// </summary>
        public int PalletQuantity { get; set; }

        #region 导航属性 

        public Inventory_DeliveryOrder DeliveryOrder { get; set; } = null!;
        public Base_Product Product { get; set; } = null!;
        public Base_ProductSpecification? Specification { get; set; }
        public Base_Goods? Goods { get; set; }
        public Base_ProductPackage? ProductPackage { get; set; }


        #endregion
    }

}
