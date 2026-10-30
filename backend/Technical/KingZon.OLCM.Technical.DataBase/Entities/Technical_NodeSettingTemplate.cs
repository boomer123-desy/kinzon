using KingZon.Core.DataBase;
using KingZon.Mes.Workflow.Entity;
using KingZon.OLCM.Base.Shared.DataBase.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KingZon.OLCM.Technical.Shared.DataBase.Entities
{
    [Comment("标准工序")]
    public class TechnicalNodeSettingTemplateMap : KingZonEntityTypeConfiguration<Technical_NodeSettingTemplate>
    {
        public override void Configure(EntityTypeBuilder<Technical_NodeSettingTemplate> builder)
        {
            builder.ToTable("Technical_NodeSettingTemplate");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Code).HasMaxLength(50);
            builder.Property(x => x.Name).HasMaxLength(100);
            builder.Property(x => x.Description).HasMaxLength(500);
            builder.Property(x => x.TheoreticalWorkHours).HasPrecision(12, 4).HasDefaultValue(0m);
            builder.Property(x => x.IsReportWork).HasDefaultValue(false);
            builder.HasIndex(x => new { x.CraftOrderVersionId, x.Sequence });

            builder.HasOne(o => o.CraftOrderVersion)
              .WithMany()
              .OnDelete(DeleteBehavior.Cascade)
              .HasForeignKey(o => o.CraftOrderVersionId)
              .IsRequired(true);

            builder.HasOne(o => o.Craft)
              .WithMany()
              .OnDelete(DeleteBehavior.Cascade)
              .HasForeignKey(o => o.CraftId)
              .IsRequired(false);
        }
    }

    public class Technical_NodeSettingTemplate : BaseEntity, ISoftDeletedEntity
    {
        public new long Id { get; set; }

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
        /// 工序名称
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 是否外协
        /// </summary>
        public bool IsOutsource { get; set; }

        /// <summary>
        /// 是否入库
        /// </summary>
        public bool IsInbound { get; set; }

        /// <summary>理论加工工时，单位：小时。</summary>
        public decimal TheoreticalWorkHours { get; set; }

        /// <summary>是否报工。</summary>
        public bool IsReportWork { get; set; }


        /// <summary>
        /// 工序说明
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// 是否删除
        /// </summary>
        public bool IsDeleted { get; set; }

        /// <summary>
        /// 工序模板ID
        /// </summary>
        public long NodeTemplateId { get; set; }

        /// <summary>
        /// 工艺Id
        /// </summary>
        public long? CraftId { get; set; }

        #region 导航属性

        /// <summary>
        /// 工序模板
        /// </summary>
        public WF_NodeTemplate NodeTemplate { get; set; } = null!;

        /// <summary>
        /// 所属工艺版本
        /// </summary>
        public Technical_CraftOrderVersion CraftOrderVersion { get; set; } = null!;

        public Base_Craft? Craft { get; set; }

        #endregion

        
    }
}
