using KingZon.Core.DataBase;
using KingZon.Mes.Workflow.Entity;
using KingZon.OLCM.Base.Shared.DataBase.Entities;
using KingZon.OLCM.Production.Shared.DataBase.Enum;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel.DataAnnotations.Schema;

namespace KingZon.OLCM.Production.Shared.DataBase.Entities
{
    [Comment("生产：派工单")]
    public class Production_Dispatch_OrderMap : KingZonEntityTypeConfiguration<Production_Dispatch_Order>
    {
        public override void Configure(EntityTypeBuilder<Production_Dispatch_Order> builder)
        {
            builder.HasKey(x => x.Id);
            builder.HasOne(builder => builder.Product)
               .WithMany()
               .OnDelete(DeleteBehavior.Restrict)
               .HasForeignKey(x => x.ProductId)
               .IsRequired(true);
            builder.HasOne(builder => builder.ProductSpecification)
               .WithMany()
               .OnDelete(DeleteBehavior.Restrict)
               .HasForeignKey(x => x.SpecificationId)
               .IsRequired(false);
            builder.HasOne(builder => builder.ProductPackage)
                .WithMany()
                .OnDelete(DeleteBehavior.Restrict)
                .HasForeignKey(builder => builder.PackageId)
                .IsRequired(false);
            builder.HasOne(builder => builder.Department)
               .WithMany()
               .OnDelete(DeleteBehavior.Restrict)
               .HasForeignKey(x => x.DepartmentId)
               .IsRequired(true);
            builder.Property(x => x.PendingInboundQuantity).HasComputedColumnSql("CompleteQuantity - TotalInboundQuantity", stored: false);
        }
    }
    public class Production_Dispatch_Order : BaseEntity, ISoftDeletedEntity
    {
        public new long Id { get; set; }
        /// <summary>
        /// 派工单号：JYPG+年月日+3位流水
        /// </summary>
        public required string OrderNo { get; set; }
        /// <summary>
        /// 派工日期
        /// </summary>
        public DateTime DispatchDate { get; set; }
        /// <summary>
        /// 预计完工日期
        /// </summary>
        public DateTime? PlanCompleteDate { get; set; }
        /// <summary>
        /// 实际完工日期
        /// </summary>
        public DateTime? CompleteDate { get; set; }
        /// <summary>
        /// 派工产品
        /// </summary>
        public long ProductId { get; set; }
        /// <summary>
        /// 派工产品规格
        /// </summary>
        public long? SpecificationId { get; set; }
        /// <summary>
        /// 产品包装规格
        /// </summary>
        public long? PackageId { get; set; }
        /// <summary>
        /// 派工部门
        /// </summary>
        public int? DepartmentId { get; set; }
        /// <summary>
        /// 工作流
        /// </summary>
        public long? WorkFlowId { get; set; }
        /// <summary>
        /// 录入派工数量（派工单位）
        /// </summary>
        [Column(TypeName = "decimal(12, 4)")]
        public decimal DispatchQuantity { get; set; }
        /// <summary>
        /// 已完工数量（派工单位）
        /// </summary>
        [Column(TypeName = "decimal(12, 4)")]
        public decimal CompleteQuantity { get; set; }
        /// <summary>
        /// 已入库数量（派工单位）
        /// </summary>
        [Column(TypeName = "decimal(12, 4)")]
        public decimal TotalInboundQuantity { get; set; }
        /// <summary>
        /// 待入库数量 （计算属性）
        /// </summary>
        [Column(TypeName = "decimal(12,4)")]
        public decimal PendingInboundQuantity {  get; set; }
        /// <summary>
        /// 转换的产品单位数量
        /// </summary>
        [Column(TypeName = "decimal(12, 4)")]
        public decimal BaseQuantity { get; set; }
        [Column(TypeName = "decimal(12, 4)")]
        public decimal BaseCompleteQuantity { get; set; }
        /// <summary>
        /// 转换的主单位数量
        /// </summary>
        [Column(TypeName = "decimal(12, 4)")]
        public decimal MainQuantity { get; set; }
        [Column(TypeName = "decimal(12, 4)")]
        public decimal MainCompleteQuantity { get; set; }
        /// <summary>
        /// 收入单位数量
        /// </summary>
        [Column(TypeName = "decimal(12, 4)")]
        public decimal IncomeQuantity { get; set; }
        [Column(TypeName = "decimal(12, 4)")]
        public decimal IncomeCompleteQuantity { get; set; }
        /// <summary>
        /// 收入单价
        /// </summary>
        [Column(TypeName = "decimal(12, 4)")]
        public decimal Price { get; set; }
        /// <summary>
        /// 派工单位转换信息UnitInfo
        /// </summary>
        [Column(TypeName = "json")]
        public required string DispatchUnit { get; set; }
        /// <summary>
        /// 备注
        /// </summary>
        public string? Remark { get; set; }
        /// <summary>
        /// 状态
        /// </summary>
        public DispatchOrderStatus Status { get; set; }
        public bool IsDeleted { get; set; }
        public DispatchBasisType BasisType { get; set; }
        /// <summary>
        /// 来源版本Id
        /// </summary>
        public long? BasisId { get; set; }
        /// 配方单位信息FormulaUnitInfo
        /// </summary>
        [Column(TypeName = "json")]
        public string? FormulaUnitInfo { get; set; }
        /// <summary>
        /// 是否是加急单
        /// </summary>
        public bool IsUrgent { get; set; }

        /// <summary>
        /// 生产扩展参数
        /// </summary>
        [Column(TypeName = "json")]
        public string? CustomAttribute { get; set; }

        #region 导航属性
        public Base_Product Product { get; set; } = null!;
        public Base_ProductSpecification? ProductSpecification { get; set; }
        public Base_ProductPackage? ProductPackage { get; set; }
        public Base_Department Department { get; set; } = null!;
        public WF_Workflow? Workflow { get; set; }
        #endregion

    }
}
