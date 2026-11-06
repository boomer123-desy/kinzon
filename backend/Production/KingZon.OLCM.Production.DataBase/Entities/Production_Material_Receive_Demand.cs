using KingZon.Core.DataBase;
using KingZon.OLCM.Base.Shared.DataBase.Entities;
using KingZon.OLCM.Base.Shared.DataBase.Enum;
using KingZon.OLCM.Production.Shared.DataBase.Enum;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel.DataAnnotations.Schema;

namespace KingZon.OLCM.Production.Shared.DataBase.Entities
{
    [Comment("生产：领用物料需求")]
    public class Production_Material_Receive_DemandMap : KingZonEntityTypeConfiguration<Production_Material_Receive_Demand>
    {
        public override void Configure(EntityTypeBuilder<Production_Material_Receive_Demand> builder)
        {
            builder.HasKey(x => x.Id);
            builder.HasOne(builder => builder.DispatchOrder)
               .WithMany()
               .OnDelete(DeleteBehavior.Cascade)
               .HasForeignKey(x => x.DispatchOrderId)
               .IsRequired(true);
            builder.HasOne(builder => builder.Unit)
                .WithMany()
                .OnDelete(DeleteBehavior.Restrict)
                .HasForeignKey(builder => builder.UnitId)
                .IsRequired(true);
            builder.HasOne(builder => builder.UnitConversion)
               .WithMany()
               .OnDelete(DeleteBehavior.Restrict)
               .HasForeignKey(x => x.UnitConversionId)
               .IsRequired(false);
            builder.HasOne(builder => builder.Node)
              .WithMany()
              .OnDelete(DeleteBehavior.Restrict)
              .HasForeignKey(x => x.NodeId)
              .IsRequired(false);
            builder.HasOne(builder => builder.Supplier)
              .WithMany()
              .OnDelete(DeleteBehavior.Restrict)
              .HasForeignKey(x => x.SupplierId)
              .IsRequired(false);

            builder.Property(x => x.BaseRemainQuantity).HasComputedColumnSql("IF(BaseDemandQuantity + BaseReturnQuantity > BaseReceiveQuantity,BaseDemandQuantity + BaseReturnQuantity - BaseReceiveQuantity,0)", stored: false);


        }
    }
    public class Production_Material_Receive_Demand : BaseEntity, ISoftDeletedEntity
    {
        public new long Id { get; set; }
        /// <summary>
        /// 领用物料Id
        /// </summary>
        public long DataId { get; set; }
        /// <summary>
        /// 物料类型
        /// </summary>
        public InventoryDataType DataType { get; set; }
        /// <summary>
        /// 物料状态
        /// </summary>
        public InventoryDataStatus DataStatus { get; set; } = InventoryDataStatus.None;
        /// <summary>
        /// 派工单Id
        /// </summary>
        public long DispatchOrderId { get; set; }
        /// <summary>
        /// 工序
        /// </summary>
        public long? NodeId { get; set; }
        /// <summary>
        /// 需求数量
        /// </summary>
        [Column(TypeName = "decimal(18, 6)")]
        public decimal DemandQuantity { get; set; }
        /// <summary>
        /// 生产单位用量 = 需求数量 / 派工数量（生产单位）
        /// </summary>
        [Column(TypeName = "decimal(18, 6)")]
        public decimal UnitQuantity { get; set; }
        /// <summary>
        /// 单位Id
        /// </summary>
        public long UnitId { get; set; }
        /// <summary>
        /// 单位转换Id
        /// </summary>
        public long? UnitConversionId { get; set; }
        public long? SupplierId { get; set; }

        /// <summary>
        /// 返修物料 指定领用的库存 结构List<ReworkMaterialInventory>
        /// </summary>
        [Column(TypeName = "json")]
        public string? Inventorys { get; set; }
        /// <summary>
        /// 备注
        /// </summary>
        public string? Remark { get; set; }

        public bool IsDeleted { get; set; }

        /// <summary>
        /// 配方单位信息FormulaUnitInfo
        /// </summary>
        [Column(TypeName = "json")]
        public string? FormulaUnitInfo { get; set; }

        /// <summary>
        /// 物料单位转换信息List<ConvUnitInfo>(Base项目)
        /// </summary>
        [Column(TypeName = "json")]
        public required string ConvUnitInfo { get; set; }
        /// <summary>
        /// 需求发料顺序
        /// </summary>
        public int Order { get; set; }

        #region 汇总字段:物料主单位

        /// <summary>
        /// 主单位需求数量
        /// </summary>
        [Column(TypeName = "decimal(18, 6)")]
        public decimal BaseDemandQuantity { get; set; }
        /// <summary>
        /// 领用数量
        /// </summary>
        [Column(TypeName = "decimal(18, 6)")]
        public decimal BaseReceiveQuantity { get; set; }
        /// <summary>
        /// 退料数量
        /// </summary>
        [Column(TypeName = "decimal(18, 6)")]
        public decimal BaseReturnQuantity { get; set; }
        /// <summary>
        /// 待发料需求数量 = BaseDemandQuantity - BaseReceiveQuantity + BaseReturnQuantity
        /// </summary>
        [Column(TypeName = "decimal(18, 6)")]
        public decimal BaseRemainQuantity { get; set; }

        /// <summary>
        /// 需求类型
        /// </summary>
        public MaterialDemandType MaterialDemandType { get; set; } = MaterialDemandType.领料需求;

        #endregion


        #region 导航属性
        public Production_Dispatch_Order DispatchOrder { get; set; } = null!;
        public Base_Unit Unit { get; set; } = null!;
        public Base_UnitConversion? UnitConversion { get; set; }
        public WF_Node_ProductionExt? Node { get; set; }
        public Base_Partner? Supplier { get; set; }

        #endregion

    }
}
