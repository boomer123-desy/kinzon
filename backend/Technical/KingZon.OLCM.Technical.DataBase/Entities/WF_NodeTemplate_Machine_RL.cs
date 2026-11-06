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

namespace KingZon.OLCM.Technical.Shared.DataBase.Entities
{

    public class WF_NodeTemplate_Machine_RLMap : KingZonEntityTypeConfiguration<WF_NodeTemplate_Machine_RL>
    {
        public override void Configure(EntityTypeBuilder<WF_NodeTemplate_Machine_RL> builder)
        {
            builder.ToTable("WF_NodeTemplate_Machine_RL");
            builder.HasKey(x => x.Id);
            builder.HasOne(x => x.NodeSettingTemplate)
                .WithMany()
                .OnDelete(DeleteBehavior.Cascade)
                .HasForeignKey(x => x.NodeSettingTemplateId)
                .IsRequired(true);
            builder.HasOne(x => x.Machine)
                .WithMany()
                .OnDelete(DeleteBehavior.Cascade)
                .HasForeignKey(x => x.MachineId)
                .IsRequired(true);
        }
    }

    public class WF_NodeTemplate_Machine_RL : BaseEntity, ISoftDeletedEntity
    {
        public new long Id { get; set; }

        public long MachineId { get; set; }

        public long NodeSettingTemplateId { get; set; }

        public bool IsDeleted { get; set; }
        /// <summary>
        /// 加工工时，单位小时
        /// </summary>

        [Column(TypeName = "decimal(18, 6)")]
        public decimal WorkHours { get; set; } = 0m;

        #region 导航属性

        public Base_Machine Machine { get; set; } = null!;

        public WF_NodeTemplate_ProductionExt NodeSettingTemplate { get; set; } = null!;

        #endregion
    }
}
