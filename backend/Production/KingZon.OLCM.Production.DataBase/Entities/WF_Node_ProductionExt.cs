using KingZon.Core.DataBase;
using KingZon.Mes.Workflow.Entity;
using KingZon.OLCM.Base.Shared.DataBase.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel.DataAnnotations.Schema;

namespace KingZon.OLCM.Production.Shared.DataBase.Entities
{
    [Comment("生产：工序节点")]
    public class WF_Node_ProductionExtMap : KingZonEntityTypeConfiguration<WF_Node_ProductionExt>
    {
        public override void Configure(EntityTypeBuilder<WF_Node_ProductionExt> builder)
        {
            builder.ToTable("WF_Node_ProductionExt");

            builder.HasOne(builder => builder.DispatchOrder)
               .WithMany()
               .OnDelete(DeleteBehavior.Cascade)
               .HasForeignKey(x => x.DispatchOrderId)
               .IsRequired(true);

            builder.HasOne(builder => builder.Craft)
               .WithMany()
               .OnDelete(DeleteBehavior.Restrict)
               .HasForeignKey(x => x.CraftId)
               .IsRequired(true);

            builder.Property(x => x.DoingQuantity).HasComputedColumnSql("IF(ReciveQuantity > CompleteQuantity,ReciveQuantity - CompleteQuantity,0)", stored: false);
        }
    }
    public class WF_Node_ProductionExt : WF_Node, ISoftDeletedEntity
    {
        public int Order { get; set; }
        /// <summary>
        /// 派工单
        /// </summary>
        public  long DispatchOrderId { get; set; }
        /// <summary>
        /// 工艺/加工类别
        /// </summary>
        public long CraftId { get; set; }
        public long? NodeTemplateSettingId { get; set; }
        public bool IsDeleted { get; set; }


        #region 数量：派工单位
        /// <summary>
        /// 接单数量
        /// </summary>
        [Column(TypeName = "decimal(12,4)")]
        public decimal ReciveQuantity { get; set; }
        /// <summary>
        /// 完工出单数量
        /// </summary>
        [Column(TypeName = "decimal(12,4)")]
        public decimal CompleteQuantity { get; set; }
        /// <summary>
        /// 生产中数量
        /// </summary>
        [Column(TypeName = "decimal(12,4)")]
        public decimal DoingQuantity { get; set; }
        /// <summary>
        /// 预计工时
        /// </summary>
        [Column(TypeName = "decimal(12,4)")]
        public decimal PlanWorkHours { get; set; }
        /// <summary>
        /// 实际工时
        /// </summary>
        [Column(TypeName = "decimal(12,4)")]
        public decimal ActualWorkHours { get; set; }
        #endregion

        /// <summary>
        /// 工序价格（不含物料成本）
        /// </summary>
        [Column(TypeName = "decimal(12,4)")]
        public decimal ProcessPrice { get; set; }

        #region 导航属性

        public Production_Dispatch_Order DispatchOrder { get; set; } = null!;
        public Base_Craft Craft { get; set; } = null!;
        #endregion
    }
}
