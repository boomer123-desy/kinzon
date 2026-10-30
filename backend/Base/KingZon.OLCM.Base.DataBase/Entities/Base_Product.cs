using KingZon.Core.DataBase;
using KingZon.OLCM.Base.Shared.DataBase.Enum;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel.DataAnnotations.Schema;

namespace KingZon.OLCM.Base.Shared.DataBase.Entities
{
    [Comment("成品表")]
    public class BaseProductMap : KingZonEntityTypeConfiguration<Base_Product>
    {
        public override void Configure(EntityTypeBuilder<Base_Product> builder)
        {
            builder.ToTable("Base_Product");

            builder.HasKey(x => x.Id);
            builder.HasOne(builder => builder.BaseUnit)
              .WithMany()
              .OnDelete(DeleteBehavior.Restrict)
              .HasForeignKey(builder => builder.BaseUnitId)
              .IsRequired(true);
            builder.HasOne(builder => builder.ProductCategory)
            .WithMany()
            .OnDelete(DeleteBehavior.Restrict)
            .HasForeignKey(builder => builder.ProductCategoryId)
            .IsRequired(true);
        }
    }
    /// <summary>
    /// 内部成品表
    /// </summary>
    public class Base_Product : BaseEntity, ISoftDeletedEntity
    {
        public new long Id { get; set; }
        /// <summary>
        /// 产品编码
        /// </summary>
        public required string ProductNo { get; set; }
        /// <summary>
        /// 产品名称
        /// </summary>
        public required string ProductName { get; set; }
        /// <summary>
        /// 技术型号（内部型号）
        /// </summary>
        public string? TechnicalCode { get; set; }
        /// <summary>
        /// 基础计量单位
        /// </summary>
        public  long BaseUnitId { get; set; }
        /// <summary>
        /// 产品备注
        /// </summary>
        public string? Remark { get; set; }
        /// <summary>
        /// 产品类别
        /// </summary>
        public long ProductCategoryId { get; set; }
        /// <summary>
        /// 评审状态
        /// </summary>
        public ReviewStatus ReviewStatus { get; set; }
        /// <summary>
        /// 产品类型
        /// </summary>
        public ProductType ProductType { get; set; }
        /// <summary>
        /// 材质牌号(原材料)
        /// </summary>
        public string? MaterialGrade { get; set; }
        /// <summary>
        /// 规格型号(原材料)
        /// </summary>
        public string? MaterialSpecification { get; set; } 
        /// <summary>
        /// 编码后缀（流水号）
        /// </summary>
        public required string NoSuffix { get; set; }
        /// <summary>
        /// 类别参数List<CustomAttribute>
        /// </summary>
        [Column(TypeName = "json")]
        public string? CategoryAttribute { get; set; }
        /// <summary>
        /// 拓展参数List<CustomAttribute>
        /// </summary>
        [Column(TypeName = "json")]
        public string? CustomAttribute { get; set; }
        /// <summary>
        /// 新品/成熟产品
        /// </summary>
        public ProductProperty ProductProperty { get; set; } = ProductProperty.成熟产品;
        /// <summary>
        /// 新品转正时间
        /// </summary>
        public DateTime? MatureTime { get; set; }
        /// <summary>
        /// 安全库存(最低库存)
        /// </summary>
        [Column(TypeName = "decimal(12, 4)")]
        public decimal SafetyStock { get; set; }
        /// <summary>
        /// 最大库存
        /// </summary>
        [Column(TypeName = "decimal(12, 4)")]
        public decimal MaxStock { get; set; }
        /// <summary>
        /// 保质期
        /// </summary>
        public int? ShelfLifeValue { get; set; }
        public TimeUnit? ShelfLifeUnit { get; set; }
        /// <summary>
        /// 临期阈值
        /// </summary>
        public int? NearExpiryThreshold { get; set; }
        public TimeUnit? NearExpiryUnit { get; set; }
        /// <summary>
        /// 最近一次采购明细ID
        /// </summary>
        public long? PurchaseOrderDetailId { get; set; }

        #region 默认单位设置
        /// <summary>
        /// 销售默认单位
        /// </summary>
        public long? DefSaleUnitConversionId { get; set; }
        /// <summary>
        /// 采购默认单位
        /// </summary>
        public long? DefPurchaseUnitConversionId { get; set; }
        #endregion

        #region 统计数量信息 对应BaseUnit
        /// <summary>
        /// 成品总库存量 = 未包装 + 已包装【正常库存】
        /// 半成品总库存
        /// 物料总库存
        /// </summary>
        [Column(TypeName = "decimal(12, 4)")]
        public decimal InventoryQuantity { get; set; }
        /// <summary>
        /// 成品未包装库存（库存的 DataStatus = 待包装）【正常库存】
        /// </summary>
        [Column(TypeName = "decimal(12, 4)")]
        public decimal UnPackageQuantity { get; set; }
        /// <summary>
        /// 成品已包装库存（库存的 DataStatus = 已包装）【正常库存】
        /// </summary>
        [Column(TypeName = "decimal(12, 4)")]
        public decimal PackageQuantity { get; set; }
        /// <summary>
        /// 占用库存
        /// </summary>
        [Column(TypeName = "decimal(12, 4)")]
        public decimal OccupyInventory { get; set; }
        /// <summary>
        /// 异常总库存
        /// </summary>
        [Column(TypeName = "decimal(12, 4)")]
        public decimal AbnormalQuantity { get; set; }
        #endregion

        #region 导航属性
        public Base_Unit BaseUnit { get; set; } = null!;
        public Base_Category ProductCategory { get; set; } = null!;
        #endregion

        public bool IsDeleted { get; set; }
    }
}
