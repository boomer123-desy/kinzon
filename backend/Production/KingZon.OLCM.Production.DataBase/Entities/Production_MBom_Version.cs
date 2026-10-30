using KingZon.Core.DataBase;
using KingZon.OLCM.Base.Shared.DataBase.Entities;
using KingZon.OLCM.Production.Shared.DataBase.Enum;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KingZon.OLCM.Production.Shared.DataBase.Entities
{
    public class ProductionMBomVersionMap : KingZonEntityTypeConfiguration<Production_MBom_Version>
    {
        public override void Configure(EntityTypeBuilder<Production_MBom_Version> builder)
        {
            builder.HasKey(x => x.Id);
            builder.HasOne(builder => builder.MBom)
               .WithMany(x => x.VersionList)
               .OnDelete(DeleteBehavior.Cascade)
               .HasForeignKey(builder => builder.MBomId)
               .IsRequired(true);
        }
    }
    public class Production_MBom_Version : BaseEntity, ISoftDeletedEntity
    {
        public new long Id { get; set; }
        public required string Version { get; set; }
        /// <summary>
        /// 是否是主版本
        /// </summary>
        public bool IsMain { get; set; }
        public long MBomId { get; set; }

        public BomStatus Status { get; set; } = BomStatus.待审核;
        /// <summary>
        /// BOM总量
        /// </summary>
        [Column(TypeName = "decimal(12, 4)")]
        public decimal TotalQuantity { get; set; }
        /// <summary>
        /// BOM单位（可选单位为 产品/规格对应的单位列表）
        /// </summary>
        public long UnitConversionId { get; set; }
        /// <summary>
        /// 单位转换
        /// 一定会有针对主单位的转换关系
        /// </summary>
        [Column(TypeName = "json")]
        public required string UnitInfo { get; set; }

        public string? Remark { get; set; }
        /// <summary>
        /// 拓展参数List<CustomAttribute>
        /// </summary>
        [Column(TypeName = "json")]
        public string? CustomAttribute { get; set; }
        /// <summary>
        /// 停用/禁用
        /// </summary>
        public bool IsDeleted { get; set; }

        #region 导航属性
        public Production_MBom MBom { get; set; } = null!;

        public List<Production_MBom_Detail> DetailList { get; set; } = new List<Production_MBom_Detail>();

        #endregion
    }
}
