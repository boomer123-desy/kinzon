using KingZon.Core.DataBase;
using KingZon.OLCM.Base.Shared.DataBase.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KingZon.OLCM.Production.Shared.DataBase.Entities
{
    public class ProductionDispatchSaleRLMap : KingZonEntityTypeConfiguration<Production_Dispatch_SaleRL>
    {
        public override void Configure(EntityTypeBuilder<Production_Dispatch_SaleRL> builder)
        {
            builder.HasKey(x => x.Id);
            builder.HasOne(x => x.DispatchOrder)
              .WithMany()
              .OnDelete(DeleteBehavior.Cascade)
              .HasForeignKey(x => x.DispatchOrderId)
              .IsRequired(true);
            builder.HasOne(x => x.Customer)
                .WithMany()
                .OnDelete(DeleteBehavior.Cascade)
                .HasForeignKey(x => x.CustomerId)
                .IsRequired(true);
            var sale = Type.GetType("KingZon.OLCM.Sale.Shared.DataBase.Entities.Sale_OrderDetail,KingZon.OLCM.Sale.Shared");
            builder.HasOne(sale, null)
                .WithMany()
                .HasForeignKey(nameof(Production_Dispatch_SaleRL.SaleOrderDetailId))
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired(true);
        }
    }
    public class Production_Dispatch_SaleRL: BaseEntity
    {
        /// <summary>
        /// 工单
        /// </summary>
        public long DispatchOrderId { get; set; }
        /// <summary>
        /// 销售单及客户
        /// </summary>
        public long SaleOrderDetailId { get; set; }
        public long CustomerId { get; set; }

        /// <summary>
        /// 针对销售单的派工量及派工单位
        /// </summary>
        [Column(TypeName = "decimal(12, 4)")]
        public decimal DispatchQuantity { get; set; }
        /// <summary>
        /// 分配的单位 不建立外键
        /// </summary>
        public long? DispatchUnitConversionId { get; set; }

        /// <summary>
        /// 派工量（主单位）
        /// </summary>
        [Column(TypeName = "decimal(12, 4)")]
        public decimal Quantity { get; set; }
        /// <summary>
        /// 入库量（主单位）
        /// </summary>
        [Column(TypeName = "decimal(12, 4)")]
        public decimal InboundQuantity { get; set; }

        /// <summary>
        /// 工单的多单位 
        /// 销售单的多单位 
        /// </summary>
        [Column(TypeName = "json")]
        public string? UnitConversionInfo { get; set; }

        #region  导航属性
        public Production_Dispatch_Order DispatchOrder { get; set; } = null!;
        public Base_Partner Customer { get; set; } = null!;
        #endregion
    }
}
