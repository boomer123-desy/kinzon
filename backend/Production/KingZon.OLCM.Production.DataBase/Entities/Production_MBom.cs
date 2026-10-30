using KingZon.Core.DataBase;
using KingZon.OLCM.Base.Shared.DataBase.Entities;
using KingZon.OLCM.Production.Shared.DataBase.Enum;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KingZon.OLCM.Production.Shared.DataBase.Entities
{
    [Comment("制造BOM")]
    public class ProductionMBomMap : KingZonEntityTypeConfiguration<Production_MBom>
    {
        public override void Configure(EntityTypeBuilder<Production_MBom> builder)
        {
            builder.HasKey(x => x.Id);
            builder.HasOne(builder => builder.Product)
             .WithMany()
             .OnDelete(DeleteBehavior.Restrict)
             .HasForeignKey(x => x.ProductId)
             .IsRequired(false);
            builder.HasOne(builder => builder.ProductSpecification)
             .WithMany()
             .OnDelete(DeleteBehavior.Restrict)
             .HasForeignKey(x => x.ProductSpecificationId)
             .IsRequired(false);
        }
    }

    public class Production_MBom : BaseEntity, ISoftDeletedEntity
    {
        public new long Id { get; set; }

        public long? ProductId { get; set; }
        public long? ProductSpecificationId { get; set; }
        public BomStatus Status { get; set; } = BomStatus.待审核;

        /// <summary>
        /// 停用/禁用
        /// </summary>
        public bool IsDeleted { get; set; }

        #region 导航属性
        public Base_Product? Product { get; set; }
        public Base_ProductSpecification? ProductSpecification { get; set; }

        public List<Production_MBom_Version> VersionList { get; set; } = new List<Production_MBom_Version>();
        #endregion
    }
}
