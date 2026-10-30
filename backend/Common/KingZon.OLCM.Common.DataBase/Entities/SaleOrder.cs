using KingZon.Core.DataBase;
using KingZon.OLCM.Common.DataBase.Entities;
using KingZon.OLCM.Common.DataBase.Enum;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace KingZon.OLCM.Common.DataBase.Entities
{
    public class SaleOrderMap : KingZonEntityTypeConfiguration<SaleOrder>
    {
        public override void Configure(EntityTypeBuilder<SaleOrder> builder)
        {
            builder.HasKey(x => x.Id);
            builder.HasOne(builder => builder.ReceiveUser)
                .WithMany()
                .OnDelete(DeleteBehavior.Restrict)
                .HasForeignKey(x => x.ReceiveUserId)
                .IsRequired(true);
            builder.HasOne(builder => builder.Customer)
                .WithMany()
                .OnDelete(DeleteBehavior.Restrict)
                .HasForeignKey(x => x.CustomerId)
                .IsRequired(true);
        }
    }

    public partial class SaleOrder : BaseEntity
    {
        /// <summary>
        /// 订单编号
        /// </summary>
        public string OrderNo { get; set; }
        /// <summary>
        /// 客户地址 {Contact:联系人,Phone:联系电话,Address:收货地址,IsDefault:是否为默认地址,Remark:备注,LogisticsCycle:物流周期（小时）}
        /// </summary>
        public string CustomerAddress { get; set; }
        /// <summary>
        /// 订单不含税总价
        /// </summary>
        [Column(TypeName = "decimal(12, 4)")]
        public decimal OrderTotalPrice { get; set; }
        /// <summary>
        /// 订单备注
        /// </summary>
        public string? Remark { get; set; }
        /// <summary>
        /// 订单状态
        /// </summary>
        public SaleOrderStatus Status { get; set; }
        /// <summary>
        /// 接受时间
        /// </summary>
        public DateTime ReceiveOrderTime { get; set; }
        /// <summary>
        /// 接收人
        /// </summary>
        public int ReceiveUserId { get; set; }
        /// <summary>
        /// 客户
        /// </summary>
        public int CustomerId { get; set; }
        /// <summary>
        /// 客户订单号
        /// </summary>
        public string? CustomerOrderNo { get; set; }
        /// <summary>
        /// 是否审核
        /// </summary>
        public SaleOrderAuditStatus AuditStatus { get; set; } = SaleOrderAuditStatus.审核通过;
        /// <summary>
        /// 订单含税总价
        /// </summary>
        [Column(TypeName = "decimal(12, 4)")]
        public decimal OrderTotalPriceHaveTax { get; set; }

        /// <summary>
        /// 导航属性
        /// </summary>
        public User ReceiveUser { get; set; } = null!;
        public Customer Customer { get; set; } = null!;
        public ICollection<SaleOrderDetail> SaleOrderDetails { get; set; } = new List<SaleOrderDetail>();
        /// <summary>
        /// 订单变更记录
        /// </summary>
        public ICollection<SaleOrderChange> saleOrderChanges { get; set; } = new List<SaleOrderChange>();

        public SaleOrder(string orderNo, string customerAddress)
        {
            OrderNo = orderNo;
            CustomerAddress = customerAddress;
        }
        //#region 从标品表映射的字段
        ///// <summary>
        ///// 评审状态（评审是下订单的固定流程）
        ///// </summary>
        //public ReviewStatus ReviewStatus { get; set; } = ReviewStatus.待提交;
        ///// <summary>
        ///// 订单类型
        ///// </summary>
        //public SaleOrderType OrderType { get; set; } = SaleOrderType.客户订单;
        ///// <summary>
        ///// 客户下单显示的拓展参数
        ///// </summary>
        //[Column(TypeName = "json")]
        //public string? CustomSaleAttribute { get; set; }
        //public bool IsDeleted { get; set; } = false;
        //#endregion
    }
}
