using KingZon.Core.DataBase;
using KingZon.OLCM.Base.Shared.DataBase.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace KingZon.OLCM.Production.Shared.DataBase.Entities
{
    public class ProductionNodeSettingUserRLMap : KingZonEntityTypeConfiguration<Production_NodeSetting_UserRL>
    {
        public override void Configure(EntityTypeBuilder<Production_NodeSetting_UserRL> builder)
        {
            builder.HasKey(x => x.Id);
            builder.HasOne(x => x.NodeSetting).WithMany()
                .HasForeignKey(x => x.NodeSettingId).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(x => x.User).WithMany()
                .HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
            builder.HasIndex(x => new { x.NodeSettingId, x.UserId }).IsUnique();
            builder.HasIndex(x => new { x.UserId, x.PlanStartDate, x.PlanCompleteDate });
        }
    }

    [Comment("生产：派工工序人员分配")]
    public class Production_NodeSetting_UserRL : BaseEntity, ISoftDeletedEntity
    {
        public new long Id { get; set; }
        public long NodeSettingId { get; set; }
        public int UserId { get; set; }
        /// <summary>人员分配数量，使用所属派工单的派工单位；不与设备分配量重复累计。</summary>
        [Column(TypeName = "decimal(12,4)")]
        public decimal Quantity { get; set; }
        public DateTime? PlanStartDate { get; set; }
        public DateTime? PlanCompleteDate { get; set; }
        public bool IsDeleted { get; set; }

        public Production_NodeSetting NodeSetting { get; set; } = null!;
        public Base_User User { get; set; } = null!;
    }
}
