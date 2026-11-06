using KingZon.Core.DataBase;
using KingZon.OLCM.Base.Shared.DataBase.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel.DataAnnotations.Schema;

namespace KingZon.OLCM.Production.Shared.DataBase.Entities
{
    [Comment("生产：工序节点")]
    public class WF_Node_MachineRLMap : KingZonEntityTypeConfiguration<WF_Node_ProductionExt_MachineRL>
    {
        public override void Configure(EntityTypeBuilder<WF_Node_ProductionExt_MachineRL> builder)
        {
            builder.HasKey(x => x.Id);
            builder.HasOne(builder => builder.Node)
               .WithMany()
               .OnDelete(DeleteBehavior.Restrict)
               .HasForeignKey(x => x.NodeId)
               .IsRequired(true);
            builder.HasOne(builder => builder.Machine)
               .WithMany()
               .OnDelete(DeleteBehavior.Restrict)
               .HasForeignKey(x => x.MachineId)
               .IsRequired(true);
            builder.HasOne(builder => builder.User)
                .WithMany()
                .HasForeignKey(o=>o.UserId)
                .IsRequired(false);

            builder.Property(x => x.DoingQuantity).HasComputedColumnSql("IF(ReciveQuantity > CompleteQuantity,ReciveQuantity - CompleteQuantity,0)", stored: false);
        }
    }
    public class WF_Node_ProductionExt_MachineRL : BaseEntity
    {
        public new long Id { get; set; }
        public long NodeId { get; set; }
        public long MachineId { get; set; }

        #region 数量：派工单位

        /// <summary>
        /// 派工数量
        /// </summary>
        [Column(TypeName = "decimal(12,4)")]
        public decimal DispatchQuantity { get; set; }
        /// <summary>
        /// 接单数量
        /// </summary>
        [Column(TypeName = "decimal(12,4)")]
        public decimal ReciveQuantity { get; set; }
        /// <summary>
        /// 完工出单数量
        /// </summary>
        [Column(TypeName = "decimal(12,4)")]
        public decimal CompleteQuantity { get; set; }
        /// <summary>
        /// 生产中数量
        /// </summary>
        [Column(TypeName = "decimal(12,4)")]
        public decimal DoingQuantity { get; set; }
        /// <summary>
        /// 人员Id
        /// </summary>
        public int? UserId { get; set; }
        /// <summary>
        /// 预计工时，以小时为单位
        /// </summary>
        /// 
        [Column(TypeName = "decimal(12,4)")]
        public decimal PlanWorkHours { get; set; }
        /// <summary>
        /// 实际工时，以小时为单位
        /// </summary>
        /// 
        [Column(TypeName = "decimal(12,4)")]
        public decimal ActualWorkHours { get; set; }

        #endregion

        #region 导航属性
        public WF_Node_ProductionExt Node { get; set; } = null!;
        public Base_Machine Machine { get; set; } = null!;
        public Base_User? User { get; set; }

        #endregion
    }
}
