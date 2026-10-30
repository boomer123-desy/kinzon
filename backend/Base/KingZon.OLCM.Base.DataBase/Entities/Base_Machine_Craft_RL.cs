using KingZon.Core.DataBase;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;


namespace KingZon.OLCM.Base.Shared.DataBase.Entities
{
    [Comment("设备与加工类别关联表")]
    public class BaseMachineCraftMap : KingZonEntityTypeConfiguration<Base_Machine_Craft_RL>
    {
        public override void Configure(EntityTypeBuilder<Base_Machine_Craft_RL> builder)
        {
            builder.HasKey(e => new {e.MachineId,e.CraftId});

            builder.HasOne(e => e.Machine)
                .WithMany(e=>e.CraftRLs)
                .HasForeignKey(e => e.MachineId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired(true);

            builder.HasOne(e => e.Craft)
                .WithMany()
                .HasForeignKey(e => e.CraftId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired(true);
            
        }
    }
    public class Base_Machine_Craft_RL
    {
        public long MachineId { get; set; }
        public long CraftId { get; set; }

        #region 导航属性
        public Base_Craft Craft { get; set; } = null!;
        public Base_Machine Machine { get; set; }= null!;
        #endregion
    }
}
