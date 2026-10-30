using KingZon.Core.DataBase;
using KingZon.OLCM.Base.Shared.DataBase.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KingZon.OLCM.Technical.Shared.DataBase.Entities
{

    [Comment("标准工序单")]
    public class TechnicalCraftMap : KingZonEntityTypeConfiguration<Technical_CraftOrder>
    {
        public override void Configure(EntityTypeBuilder<Technical_CraftOrder> builder)
        {
            builder.ToTable("Technical_CraftOrder");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Code).HasMaxLength(50);
            builder.Property(x => x.Name).HasMaxLength(100);
            builder.Property(x => x.Description).HasMaxLength(500);
            builder.HasIndex(x => x.ProductId);

            builder.HasOne(o => o.Product)
              .WithMany()
              .OnDelete(DeleteBehavior.Cascade)
              .HasForeignKey(o => o.ProductId)
              .IsRequired(true);

            builder.HasOne(o => o.ProductSpecification)
              .WithMany()
              .OnDelete(DeleteBehavior.Cascade)
              .HasForeignKey(o => o.SpecificationId)
              .IsRequired(false);
        }
    }

    public class Technical_CraftOrder : BaseEntity, ISoftDeletedEntity
    {


        public new long Id { get; set; }

        /// <summary>
        /// 工艺单编码
        /// </summary>
        public string Code { get; set; } = string.Empty;

        /// <summary>
        /// 工艺单名称
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 产品 ID
        /// </summary>
        public long ProductId { get; set; }
        /// <summary>
        /// 规格ID
        /// </summary>
        public long? SpecificationId { get; set; }

        /// <summary>
        /// 工艺单说明
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// 删除标记
        /// </summary>
        public bool IsDeleted { get; set; }

        #region 导航属性

        public Base_Product Product { get; set; } = null!;
        public Base_ProductSpecification? ProductSpecification { get; set; }

        #endregion
    }
}
