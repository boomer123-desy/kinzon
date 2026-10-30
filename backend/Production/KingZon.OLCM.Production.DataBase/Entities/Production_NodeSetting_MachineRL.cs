using KingZon.Core.DataBase;
using KingZon.OLCM.Base.Shared.DataBase.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace KingZon.OLCM.Production.Shared.DataBase.Entities
{
    [Comment("生产：工序节点")]
    public class Production_NodeSetting_MachineRLMap : KingZonEntityTypeConfiguration<Production_NodeSetting_MachineRL>
    {
        public override void Configure(EntityTypeBuilder<Production_NodeSetting_MachineRL> builder)
        {
            builder.HasKey(x => x.Id);
            builder.HasAlternateKey(x => new { x.Id, x.NodeSettingId });
            builder.HasOne(x => x.NodeSetting).WithMany()
                .OnDelete(DeleteBehavior.Cascade).HasForeignKey(x => x.NodeSettingId).IsRequired(true);
            builder.HasOne(x => x.Machine).WithMany()
                .OnDelete(DeleteBehavior.Cascade).HasForeignKey(x => x.MachineId).IsRequired(true);
            builder.Property(x => x.DoingQuantity).HasComputedColumnSql("IF(ReciveQuantity > CompleteQuantity,ReciveQuantity - CompleteQuantity,0)", stored: false);
            builder.HasIndex(x => new { x.MachineId, x.PlanStartDate, x.PlanCompleteDate });
        }
    }

    public class Production_NodeSetting_MachineRL : BaseEntity
    {
        public new long Id { get; set; }
        public long NodeSettingId { get; set; }
        public long MachineId { get; set; }

        #region 数量：派工单位
        [Column(TypeName = "decimal(12,4)")]
        public decimal DispatchQuantity { get; set; }
        [Column(TypeName = "decimal(12,4)")]
        public decimal ReciveQuantity { get; set; }
        [Column(TypeName = "decimal(12,4)")]
        public decimal CompleteQuantity { get; set; }
        [Column(TypeName = "decimal(12,4)")]
        public decimal DoingQuantity { get; set; }
        #endregion

        /// <summary>设备实际排产时间段，供甘特图与资源冲突校验使用。</summary>
        public DateTime? PlanStartDate { get; set; }
        public DateTime? PlanCompleteDate { get; set; }

        public Production_NodeSetting NodeSetting { get; set; } = null!;
        public Base_Machine Machine { get; set; } = null!;
    }
}
