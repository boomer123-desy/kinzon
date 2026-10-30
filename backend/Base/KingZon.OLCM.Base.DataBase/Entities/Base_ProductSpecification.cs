using KingZon.Core.DataBase;
using KingZon.OLCM.Base.Shared.DataBase.Enum;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel.DataAnnotations.Schema;

namespace KingZon.OLCM.Base.Shared.DataBase.Entities
{
    [Comment("成品规格型号表")]
    public class BaseProductSpecificationMap : KingZonEntityTypeConfiguration<Base_ProductSpecification>
    {
        public override void Configure(EntityTypeBuilder<Base_ProductSpecification> builder)
        {
            builder.HasKey(x => x.Id);
            builder.HasOne(builder => builder.Product)
                  .WithMany()
                  .OnDelete(DeleteBehavior.Cascade)
                  .HasForeignKey(builder => builder.ProductId)
                  .IsRequired(true);
            builder.HasOne(builder => builder.SpecificationUnit)
             .WithMany()
             .OnDelete(DeleteBehavior.SetNull)
             .HasForeignKey(builder => builder.SpecificationUnitId)
             .IsRequired(false);
            builder.HasOne(builder => builder.ContainerModel)
             .WithMany()
             .OnDelete(DeleteBehavior.SetNull)
             .HasForeignKey(builder => builder.ContainerModelId)
             .IsRequired(false);
            builder.HasOne(builder => builder.ProductCategory)
             .WithMany()
             .OnDelete(DeleteBehavior.SetNull)
             .HasForeignKey(builder => builder.ProductCategoryId)
             .IsRequired(false);
        }
    }
    /// <summary>
    /// 罐装规格
    /// </summary>
    public class Base_ProductSpecification : BaseEntity, ISoftDeletedEntity
    {
        public new long Id { get; set; }
        public long ProductId { get; set; }
        /// <summary>
        /// 规格编码
        /// </summary>
        public string? Code { get; set; }
        /// <summary>
        /// 规格名称
        /// </summary>
        public string? Name { get; set; }
        /// <summary>
        /// 规格类别
        /// </summary>
        public long? ProductCategoryId { get; set; }
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
        /// 最小安全库存
        /// </summary>
        [Column(TypeName = "decimal(12, 4)")]
        public decimal SafetyStock { get; set; }
        /// <summary>
        /// 最大安全库存
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
        /// 规格备注
        /// </summary>
        public string? Remark { get; set; }
        /// <summary>
        /// 规格单位
        /// </summary>
        public long? SpecificationUnitId { get; set; }
        /// <summary>
        /// 规格型号（310ml/支 250*250*1.7mm）
        /// </summary>
        public required string Specification { get; set; }

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
        /// 规格库存量【正常库存】
        /// 规格的库存记录 都是“已包装”状态
        /// </summary>
        [Column(TypeName = "decimal(12, 4)")]
        public decimal SpecificationInventoryQuantity { get; set; }
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

        /// <summary>
        /// 容器类型（支装、桶装等，也称包装方式，对应SystemParaType 包装类型140）
        /// </summary>
        public int? ContainerModelId { get; set; }
        /// <summary>
        /// 最近一次采购明细ID
        /// </summary>
        public long? PurchaseOrderDetailId { get; set; }
        /// <summary>
        /// 罐装加工参数（json格式，包含温度、压力等参数）？？？？
        /// </summary>

        #region 导航属性
        public Base_Product Product { get; set; } = null!;
        public Base_Unit? SpecificationUnit { get; set; }
        public Base_SystemPara? ContainerModel { get; set; }
        public Base_Category? ProductCategory { get; set; }

        #endregion
        public bool IsDeleted { get; set; }

    }
}
