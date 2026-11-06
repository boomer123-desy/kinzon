using KingZon.Core.DataBase;
using KingZon.Mes.Workflow.Entity;
using KingZon.OLCM.Base.Shared.DataBase.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel.DataAnnotations.Schema;

namespace KingZon.OLCM.Technical.Shared.DataBase.Entities
{
    [Comment("标准工序")]
    public class WF_NodeTemplate_ProductionMap : KingZonEntityTypeConfiguration<WF_NodeTemplate_ProductionExt>
    {
        public override void Configure(EntityTypeBuilder<WF_NodeTemplate_ProductionExt> builder)
        {
            builder.ToTable("WF_NodeTemplate_ProductionExt");

            builder.HasIndex(x => new { x.CraftOrderVersionId, x.Sequence });

            builder.HasOne(o => o.CraftOrderVersion)
              .WithMany()
              .OnDelete(DeleteBehavior.Restrict)
              .HasForeignKey(o => o.CraftOrderVersionId)
              .HasConstraintName("FK_WF_NodeTemplate_ProductionExt_CraftOrderVersion_CraftOrderVersionId")
              .IsRequired(true);

            builder.HasOne(o => o.Craft)
              .WithMany()
              .OnDelete(DeleteBehavior.Restrict)
              .HasForeignKey(o => o.CraftId)
              .IsRequired(false);
        }
    }

    public class WF_NodeTemplate_ProductionExt : WF_NodeTemplate
    {
        /// <summary>
        /// 所属工艺版本 ID
        /// </summary>
        public long CraftOrderVersionId { get; set; }

        /// <summary>
        /// 工序顺序，按 10、20、30 递增
        /// </summary>
        public int Sequence { get; set; }

        /// <summary>
        /// 工序编码
        /// </summary>
        public string Code { get; set; } = string.Empty;

        /// <summary>
        /// 是否外协
        /// </summary>
        public bool IsOutsource { get; set; }

        /// <summary>
        /// 是否入库
        /// </summary>
        public bool IsInbound { get; set; }

        /// <summary>
        /// 工序说明
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// 是否删除
        /// </summary>
        public bool IsDeleted { get; set; }

        /// <summary>
        /// 工艺Id
        /// </summary>
        public long? CraftId { get; set; }
        /// <summary>
        /// 是否报工，默认true，表示该工序需要报工；如果为false，则表示该工序不需要报工。
        /// </summary>
        public bool IsReport { get; set; } = true;
        /// <summary>
        /// 加工工时，单位小时
        /// </summary>

        [Column(TypeName = "decimal(18, 6)")]
        public decimal WorkHours { get; set; } = 0m;

        #region 导航属性

        /// <summary>
        /// 所属工艺版本
        /// </summary>
        public Technical_CraftOrderVersion CraftOrderVersion { get; set; } = null!;

        public Base_Craft? Craft { get; set; }

        #endregion

        
    }
}
