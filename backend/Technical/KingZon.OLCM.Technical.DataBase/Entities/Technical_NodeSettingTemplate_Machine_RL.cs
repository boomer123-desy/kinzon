using KingZon.Core.DataBase;
using KingZon.OLCM.Base.Shared.DataBase.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KingZon.OLCM.Technical.Shared.DataBase.Entities
{

    public class Technical_NodeTemplateSettingMachineRLMap : KingZonEntityTypeConfiguration<Technical_NodeSettingTemplate_Machine_RL>
    {
        public override void Configure(EntityTypeBuilder<Technical_NodeSettingTemplate_Machine_RL> builder)
        {
            builder.HasKey(x => x.Id);
            builder.Property(x => x.TheoreticalWorkHours).HasPrecision(12, 4).HasDefaultValue(0m);
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

    public class Technical_NodeSettingTemplate_Machine_RL : BaseEntity, ISoftDeletedEntity
    {
        public new long Id { get; set; }

        public long MachineId { get; set; }

        /// <summary>本工序在此设备上的理论加工工时（小时）。</summary>
        public decimal TheoreticalWorkHours { get; set; }

        public long NodeSettingTemplateId { get; set; }

        public bool IsDeleted { get; set; }

        #region 导航属性

        public Base_Machine Machine { get; set; } = null!;

        public Technical_NodeSettingTemplate NodeSettingTemplate { get; set; } = null!;

        #endregion
    }
}
