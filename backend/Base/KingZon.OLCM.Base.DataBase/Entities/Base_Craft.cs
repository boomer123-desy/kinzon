using KingZon.Core.DataBase;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KingZon.OLCM.Base.Shared.DataBase.Entities
{
    [Comment("加工类别表")]
    public class BaseCraftMap : KingZonEntityTypeConfiguration<Base_Craft>
    {
        public override void Configure(EntityTypeBuilder<Base_Craft> builder)
        {
            builder.ToTable("Base_Craft");

            builder.HasKey(x => x.Id);
        }
    }

    public class Base_Craft : BaseEntity
    {
        public new long Id {  get; set; }
        public required string Name { get; set; }
    }
}
