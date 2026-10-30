using KingZon.Core.DataBase;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel.DataAnnotations.Schema;

namespace KingZon.OLCM.Base.Shared.DataBase.Entities
{
    [Comment("产品表")]
    public class BaseGoodsMap : KingZonEntityTypeConfiguration<Base_Goods>
    {
        public override void Configure(EntityTypeBuilder<Base_Goods> builder)
        {
            builder.ToTable("Base_Goods");

            builder.HasKey(x => x.Id);
            builder.HasOne(builder => builder.Product)
                  .WithMany()
                  .OnDelete(DeleteBehavior.Restrict)
                  .HasForeignKey(builder => builder.ProductId)
                  .IsRequired(true);
            builder.HasOne(builder => builder.SalesUnit)
               .WithMany()
               .OnDelete(DeleteBehavior.SetNull)
               .HasForeignKey(builder => builder.SalesUnitId)
               .IsRequired(false);
            builder.HasOne(builder => builder.Customer)
                 .WithMany()
                 .OnDelete(DeleteBehavior.Restrict)
                 .HasForeignKey(builder => builder.CustomerId)
                 .IsRequired(true);
            builder.HasOne(builder => builder.ProductSpecification)
                 .WithMany()
                 .OnDelete(DeleteBehavior.Restrict)
                 .HasForeignKey(builder => builder.SpecificationId)
                 .IsRequired(false);
            builder.HasOne(builder => builder.ProductPackage)
                .WithMany()
                .OnDelete(DeleteBehavior.SetNull)
                .HasForeignKey(builder => builder.PackageId)
                .IsRequired(false);
        }
    }
    /// <summary>
    /// 外部产品表
    /// </summary>
    public class Base_Goods : BaseEntity, ISoftDeletedEntity
    {
        public new long Id { get; set; }
        public long ProductId { get; set; }
        public long CustomerId { get; set; }
        /// <summary>
        /// 销售单价
        /// </summary>
        [Column(TypeName = "decimal(12, 4)")]
        public decimal SalesPrice { get; set; }
        /// <summary>
        /// 售卖单位
        /// </summary>
        public long? SalesUnitId { get; set; }
        /// <summary>
        /// 客户产品图号
        /// </summary>
        public string? CustomerMaterialNo { get; set; }
        /// <summary>
        /// 客户产品名称
        /// </summary>
        public string? CustomerMaterialName { get; set; }
        /// <summary>
        /// 产品规格型号
        /// </summary>
        public long? SpecificationId { get; set; }
        /// <summary>
        /// 产品包装规格
        /// </summary>
        public long? PackageId { get; set; }
        /// <summary>
        /// 售卖单位转换率（不建立外键）
        /// </summary>
        public long? SalesUnitConversionId { get; set; }

        #region 导航属性
        public Base_Product Product { get; set; } = null!;

        public Base_Unit? SalesUnit { get; set; }

        public Base_Partner Customer { get; set; } = null!;

        public Base_ProductSpecification? ProductSpecification { get; set; }
        public Base_ProductPackage? ProductPackage { get; set; }
        #endregion

        public bool IsDeleted { get; set; } = false;
    }
}
