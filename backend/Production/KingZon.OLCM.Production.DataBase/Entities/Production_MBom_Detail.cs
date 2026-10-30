using KingZon.Core.DataBase;
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
    public class ProductionMBomDetailMap : KingZonEntityTypeConfiguration<Production_MBom_Detail>
    {
        public override void Configure(EntityTypeBuilder<Production_MBom_Detail> builder)
        {
            builder.HasKey(x => x.Id);
            builder.HasOne(x => x.MBom)
                .WithMany()
                .OnDelete(DeleteBehavior.Cascade)
                .HasForeignKey(builder => builder.MBomId)
                .IsRequired(true);
            builder.HasOne(x => x.BomVersion)
                .WithMany(x => x.DetailList)
                .OnDelete(DeleteBehavior.Cascade)
                .HasForeignKey(builder => builder.VersionId)
                .IsRequired(true);
        }
    }
    public class Production_MBom_Detail : BaseEntity
    {
        public new long Id { get; set; }
        public BomDetailType DetailType { get; set; }
        public long DetailId { get; set; }
        /// <summary>
        /// 当DetailType为配方时，存储配方的树形结构及用量信息
        /// 当配方发生变化时，通过此字段可以提示用户配方的变化情况
        /// </summary>
        [Column(TypeName = "json")]
        public string? FormulaDetailInfo { get; set; }

        /// <summary>
        /// 用量 
        /// </summary>
        [Column(TypeName = "decimal(12, 4)")]
        public decimal Quantity { get; set; }
        public long UnitConversionId { get; set; }
        /// <summary>
        /// 单位转换
        /// 一定会有针对主单位的转换关系
        /// </summary>
        [Column(TypeName = "json")]
        public required string UnitInfo { get; set; }

        /// <summary>
        /// 用量比例(小数)
        /// </summary>
        [Column(TypeName = "decimal(12, 4)")]
        public decimal Ratio { get; set; }
        /// <summary>
        /// 上偏差(小数)
        /// </summary>
        [Column(TypeName = "decimal(12, 4)")]
        public decimal UpperTolerance { get; set; }
        /// <summary>
        /// 下偏差(小数)
        /// </summary>
        [Column(TypeName = "decimal(12, 4)")]
        public decimal LowerTolerance { get; set; }
        /// <summary>
        /// 针对主版本上的同物料的差值（替换料此字段为空）
        /// </summary>
        [Column(TypeName = "decimal(12, 4)")]
        public decimal? QuantityDiff2MainVersion { get; set; }
        [Column(TypeName = "decimal(12, 4)")]
        public decimal? RatioDiff2MainVersion { get; set; }

        /// <summary>
        /// 是否为替换料
        /// </summary>
        public bool IsSubstitute { get; set; } = false;
        /// <summary>
        /// 替换料对应的主料Id（如果是替换料，则此字段不为空）
        /// </summary>
        public long? MainDetailId { get; set; }

        public int Order { get; set; }

        public long VersionId { get; set; }
        public long MBomId { get; set; }


        #region 导航属性
        public Production_MBom MBom { get; set; } = null!;
        public Production_MBom_Version BomVersion { get; set; } = null!;
        #endregion
    }
}
