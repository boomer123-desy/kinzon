using KingZon.Core.DataBase;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KingZon.OLCM.Base.Shared.DataBase.Entities
{
    [Comment("设备与用户关联表")]
    public class BaseMachineUserMap : KingZonEntityTypeConfiguration<Base_Machine_User_RL>
    {
        public override void Configure(EntityTypeBuilder<Base_Machine_User_RL> builder)
        {
            builder.HasKey(e => new {e.MachineId,e.UserId});

            builder.HasOne(e => e.Machine)
                .WithMany(e=>e.UserRLs)
                .HasForeignKey(e => e.MachineId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired(true);

            builder.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired(true);
        }
    }
    public class Base_Machine_User_RL
    {
        public long MachineId { get; set; }
        public int UserId { get; set; }

        #region 导航属性
        public Base_Machine Machine { get; set; } = null!;
        public Base_User User { get; set; } = null!;
        #endregion
    }
}
