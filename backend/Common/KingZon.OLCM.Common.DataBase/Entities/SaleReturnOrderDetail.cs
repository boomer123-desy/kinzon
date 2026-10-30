using KingZon.Core.DataBase;
using KingZon.OLCM.Common.DataBase.Entities;
using KingZon.OLCM.Common.DataBase.Enum;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KingZon.OLCM.Common.DataBase.Entities
{
    public class SaleReturnOrderDetailMap : KingZonEntityTypeConfiguration<SaleReturnOrderDetail>
    {
        public override void Configure(EntityTypeBuilder<SaleReturnOrderDetail> builder)
        {
            builder.HasKey(x => x.Id);
            builder.HasOne(builder => builder.SaleReturnOrder)
                .WithMany()
                .OnDelete(Microsoft.EntityFrameworkCore.DeleteBehavior.Cascade)
                .HasForeignKey(builder => builder.SaleReturnOrderId)
                .IsRequired(true);

            builder.HasOne(builder => builder.Product)
                .WithMany()
                .OnDelete(Microsoft.EntityFrameworkCore.DeleteBehavior.Restrict)
                .HasForeignKey(builder => builder.ProductId)
                .IsRequired(true);
            builder.HasOne(builder => builder.DeliveryOrderDetail)
                .WithMany()
                .OnDelete(Microsoft.EntityFrameworkCore.DeleteBehavior.Restrict)
                .HasForeignKey(builder => builder.DeliveryOrderDetailId)
                .IsRequired(false);
            builder.HasOne(builder => builder.HistoryProduct)
                .WithMany()
                .OnDelete(Microsoft.EntityFrameworkCore.DeleteBehavior.Restrict)
                .HasForeignKey(builder => builder.HistoryProductId)
                .IsRequired(false);
        }
    }
    public class SaleReturnOrderDetail : BaseEntity
    {
        /// <summary>
        /// 退货数量
        /// </summary>
        public int ReturnQuantity { get; set; }
        /// <summary>
        /// 退货入库数量
        /// </summary>
        public int InventoryQuantity { get; set; }
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
        public int? DeliveryOrderDetailId { get; set; }
        /// <summary>
        /// 退货单ID
        /// </summary>
        public int SaleReturnOrderId { get; set; }
        /// <summary>
        /// 导航属性
        /// </summary>
        public SaleReturnOrder SaleReturnOrder { get; set; } = null!;
        public DeliveryOrderDetail? DeliveryOrderDetail { get; set; }
        /// <summary>
        /// 历史产品ID
        /// </summary>
        public int? HistoryProductId { get; set; }
        /// <summary>
        /// 产品ID
        /// </summary>
        public int ProductId { get; set; }
        public Product Product { get; set; } = null!;
        public HistoryProduct? HistoryProduct { get; set; }
        /// <summary>
        /// 是否对账
        /// </summary>
        public bool IsAccount { get; set; }
        /// <summary>
        /// 退货类型
        /// </summary>
        public SaleReturnType SaleReturnType { get; set; }
        /// <summary>
        /// 客户产品图号，若为送货单生成退货详情记录，则新增时复制对应销售单的客户产品图号
        /// </summary>
        public string? CustomerMaterialNo { get; set; }

    }
}
