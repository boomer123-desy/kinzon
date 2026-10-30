using KingZon.Core.DataBase;
using KingZon.Mes.Workflow.Entity;
using KingZon.OLCM.Base.Shared.DataBase.Entities;
using KingZon.OLCM.Production.Shared.DataBase.Enum;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel.DataAnnotations.Schema;

namespace KingZon.OLCM.Production.Shared.DataBase.Entities
{
    [Comment("生产：工序节点")]
    public class Production_NodeSettingMap : KingZonEntityTypeConfiguration<Production_NodeSetting>
    {
        public override void Configure(EntityTypeBuilder<Production_NodeSetting> builder)
        {
            builder.HasKey(x => x.Id);

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
    public class Production_NodeSetting : BaseEntity, ISoftDeletedEntity
    {
        public new long Id { get; set; }
        public int Order { get; set; }
        /// <summary>
        /// 工序类型
        /// </summary>
        public NodeType NodeType { get; set; }
        /// <summary>
        /// 工序状态
        /// </summary>
        public NodeState State { get; set; }
        /// <summary>
        /// 工序名称
        /// </summary>
        public required string NodeName { get; set; }
        /// <summary>
        /// 工作流节点
        /// </summary>
        public long? NodeId {  get; set; }
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
        #endregion

        #region 导航属性

        public Production_Dispatch_Order DispatchOrder { get; set; } = null!;
        public Base_Craft Craft { get; set; } = null!;
        public WF_Node? WFNode { get; set; }
        #endregion
    }
}
