using KingZon.Core.DataBase;
using KingZon.Mes.Workflow.Entity;
using KingZon.Mes.Workflow.Model;
using KingZon.OLCM.Technical.Shared.DataBase.Enum;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel.DataAnnotations.Schema;

namespace KingZon.OLCM.Technical.Shared.DataBase.Entities
{

    [Comment("工艺单版本")]
    public class TechnicalCraftOrderVersionMap : KingZonEntityTypeConfiguration<Technical_CraftOrderVersion>
    {
        public override void Configure(EntityTypeBuilder<Technical_CraftOrderVersion> builder)
        {
            builder.ToTable("Technical_CraftOrderVersion", t => t
                .HasCheckConstraint("CK_Technical_CraftVersion_Time", "[InvalidTime] IS NULL OR [EffectiveTime] IS NULL OR [InvalidTime] > [EffectiveTime]"));
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Version).HasMaxLength(20);
            builder.Property(x => x.Remark).HasMaxLength(500);
            builder.HasIndex(x => new { x.CraftOrderId, x.SequenceNo }).IsUnique();
            builder.HasIndex(x => x.BomVersionId).IsUnique();
            builder.HasIndex(x => new { x.CraftOrderId, x.IsMain }).HasFilter("[IsMain] = 1").IsUnique();

            builder.HasOne(o => o.CraftOrder)
              .WithMany()
              .OnDelete(DeleteBehavior.Restrict)
              .HasForeignKey(o => o.CraftOrderId)
              .IsRequired(true);

            builder.HasOne(o => o.WorkflowTemplate)
              .WithMany()
              .OnDelete(DeleteBehavior.Restrict)
              .HasForeignKey(o => o.WorkflowTemplateId)
              .IsRequired(true);

        }
    }

    public class Technical_CraftOrderVersion : BaseEntity, ISoftDeletedEntity
    {
        public new long Id { get; set; }

        /// <summary>
        /// 所属工艺单ID
        /// </summary>
        public long CraftOrderId { get; set; }

        /// <summary>该工艺版本对应的制造 BOM 版本，由后台保存时维护。</summary>
        public long? BomVersionId { get; set; }

        /// <summary>
        /// 用于排序及生成新版本的数字版本号，例如 3
        /// </summary>
        public int SequenceNo { get; set; }

        /// <summary>
        /// 展示版本号，例如 V3
        /// </summary>
        public string Version { get; set; } = string.Empty;

        /// <summary>
        /// 是否为当前主版本
        /// </summary>
        public bool IsMain { get; set; }

        /// <summary>
        /// 版本状态
        /// </summary>
        public CraftOrderVersionStatus Status { get; set; } = CraftOrderVersionStatus.暂存;

        /// <summary>
        /// 版本备注
        /// </summary>
        public string? Remark { get; set; }

        /// <summary>
        /// 软删除标记
        /// </summary>
        public bool IsDeleted { get; set; }

        /// <summary>
        /// 工作流模板 ID
        /// </summary>
        public long WorkflowTemplateId { get; set; }

        #region 导航属性

        public WF_WorkflowTemplate WorkflowTemplate { get; set; } = null!;

        /// <summary>
        /// 所属工艺单
        /// </summary>
        public Technical_CraftOrder CraftOrder { get; set; } = null!;

        #endregion
    }
}
