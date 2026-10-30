using KingZon.Core.DataBase;
using KingZon.OLCM.Base.Shared.DataBase.Entities;
using KingZon.OLCM.Sale.Shared.DataBase.Enum;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KingZon.OLCM.Sale.Shared.DataBase.Entities
{
    public class SaleReturnOrderMap : KingZonEntityTypeConfiguration<Sale_ReturnOrder>
    {
        public override void Configure(EntityTypeBuilder<Sale_ReturnOrder> builder)
        {
            builder.HasKey(x => x.Id);
            builder.HasOne(builder => builder.Customer)
                .WithMany()
                .OnDelete(Microsoft.EntityFrameworkCore.DeleteBehavior.Restrict)
                .HasForeignKey(builder => builder.CustomerId)
                .IsRequired(true);
        }
    }

    public class Sale_ReturnOrder : BaseEntity
    {
        public new long Id { get; set; }
        /// <summary>
        /// 退货单号
        /// </summary>
        public string OrderNo { get; set; }
        /// <summary>
        /// 退货总价
        /// </summary>
        [Column(TypeName = "decimal(12,4)")]
        public decimal OrderTotalPrice { get; set; }
        /// <summary>
        /// 退货日期
        /// </summary>
        public DateTime ReturnTime { get; set; }
        /// <summary>
        /// 退货单检验状态
        /// </summary>
        public SaleRetrunOrderStatus Status { get; set; }
        /// <summary>
        /// 是否完成
        /// </summary>
        public bool IsComplete { get; set; } = false;
        /// <summary>
        /// 备注
        /// </summary>
        public string? Remark { get; set; }
        /// <summary>
        /// 客户ID
        /// </summary>
        public long CustomerId { get; set; }
        /// <summary>
        /// 导航属性
        /// </summary>
        public Base_Partner Customer { get; set; } = null!;

        public Sale_ReturnOrder(string orderNo)
        {
            OrderNo = orderNo;
        }
    }
}
