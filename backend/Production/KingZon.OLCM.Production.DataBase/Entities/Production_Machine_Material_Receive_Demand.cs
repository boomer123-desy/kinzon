using KingZon.Core.DataBase;
using KingZon.OLCM.Base.Shared.DataBase.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel.DataAnnotations.Schema;

namespace KingZon.OLCM.Production.Shared.DataBase.Entities
{
    [Comment("生产：设备领用物料需求")]
    public class Production_Machine_Material_Receive_DemandMap : KingZonEntityTypeConfiguration<Production_Machine_Material_Receive_Demand>
    {
        public override void Configure(EntityTypeBuilder<Production_Machine_Material_Receive_Demand> builder)
        {
            builder.HasKey(x => x.Id);
            builder.HasOne(builder => builder.Machine)
               .WithMany()
               .OnDelete(DeleteBehavior.Restrict)
               .HasForeignKey(x => x.MachineId)
               .IsRequired(true);
            builder.HasOne(builder => builder.MaterialReceiveDemand)
                .WithMany()
                .OnDelete(DeleteBehavior.Cascade)
                .HasForeignKey(builder => builder.MaterialReceiveDemandId)
                .IsRequired(true);
            builder.HasOne(builder => builder.NodeSettingMachine)
                .WithMany()
                .OnDelete(DeleteBehavior.Restrict)
                .HasForeignKey(builder => builder.NodeSettingMachineId)
                .IsRequired(true);

            builder.Property(x => x.BaseRemainQuantity).HasComputedColumnSql("IF(BaseDemandQuantity + BaseReturnQuantity > BaseReceiveQuantity,BaseDemandQuantity + BaseReturnQuantity - BaseReceiveQuantity,0)", stored: false);

        }
    }
    public class Production_Machine_Material_Receive_Demand : BaseEntity, ISoftDeletedEntity
    {
        public new long Id { get; set; }
        /// <summary>
        /// 派工设备
        /// </summary>
        public long MachineId { get; set; }
        public long NodeSettingMachineId { get; set; }
        /// <summary>
        /// 领料需求
        /// </summary>
        public long MaterialReceiveDemandId { get; set; }
        /// <summary>
        /// 需求数量
        /// </summary>
        [Column(TypeName = "decimal(18, 6)")]
        public decimal DemandQuantity { get; set; }
        public bool IsDeleted { get; set; }
        /// <summary>
        /// 备注
        /// </summary>
        public string? Remark { get; set; }

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
        /// 发料剩余量
        /// </summary>
        [Column(TypeName = "decimal(18, 6)")]
        public decimal BaseRemainQuantity { get; set; }

        #endregion

        #region 导航属性
        public Production_Material_Receive_Demand MaterialReceiveDemand { get; set; } = null!;
        public Base_Machine Machine { get; set; } = null!;
        public WF_Node_ProductionExt_MachineRL NodeSettingMachine { get; set; } = null!;

        #endregion

    }
}
