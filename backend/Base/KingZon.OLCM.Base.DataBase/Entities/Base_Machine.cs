using KingZon.Core.DataBase;
using KingZon.OLCM.Base.Shared.DataBase.Enum;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel.DataAnnotations.Schema;

namespace KingZon.OLCM.Base.Shared.DataBase.Entities
{
    [Comment("设备表")]
    public class BaseMachineMap : KingZonEntityTypeConfiguration<Base_Machine>

    {
        public override void Configure(EntityTypeBuilder<Base_Machine> builder)
        {
            builder.ToTable("Base_Machine");

            builder.HasKey(x => x.Id);
            builder.HasOne(x => x.Category)
                .WithMany()
                .HasForeignKey(x => x.CategoryId)
                .IsRequired(true)
                .OnDelete(DeleteBehavior.Restrict);

        }
    }
    public class Base_Machine : BaseEntity
        , ISoftDeletedEntity
    {
        public new long Id { get; set; }

        public long CategoryId { get; set; } // 类别ID

        public required string Code { get; set; }

        public required string Name { get; set; }
        /// <summary>
        /// 设备型号
        /// </summary>
        public required string Model { get; set; }
        [Column(TypeName = "decimal(12,4)")]
        //默认为0
        public decimal Capacity { get; set; } = 0;
        /// <summary>
        /// 设备类型，生产设备/实验设备
        /// </summary>
        public MachineType MachineType { get; set; }

        public string? Remark { get; set; }
        //默认是启用的
        public bool IsStop { get; set; } = false;

        //软删
        public bool IsDeleted { get; set; } = false;




        #region 导航属性
        public List<Base_Machine_User_RL> UserRLs { get; set; } = [];
        public List<Base_Machine_Craft_RL> CraftRLs { get; set; } = [];
        public Base_Category Category { get; set; } = null!;
        #endregion
    }
}
