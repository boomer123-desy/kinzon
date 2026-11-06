namespace KingZon.Mes.Workflow.Entity
{
    using KingZon.Core.DataBase;
    using KingZon.Mes.Workflow.Enum;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Metadata.Builders;
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    /// <summary>
    /// 数据映射Map
    /// </summary>
    public partial class NodeMap : KingZonEntityTypeConfiguration<WF_Node>
    {
        public override void Configure(EntityTypeBuilder<WF_Node> mb)
        {
            //多表继承配置
            mb.HasKey(o => o.Id);
            mb.UseTptMappingStrategy();

            mb.HasOne(t => t.NodeTemplate)
                .WithMany()
                .HasForeignKey(d => d.NodeTemplateId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired(false);

            mb.HasOne(o => o.NodeType)
                .WithMany()
                .HasForeignKey(o => o.NodeTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            mb.HasOne(o => o.Workflow)
                .WithMany(o => o.Nodes)
                .HasForeignKey(o => o.WorkflowId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
    /// <summary>
    /// 数据实体对象
    /// </summary>
    public partial class WF_Node: BaseEntity
    {
        [Column(Order = 1)]
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]

        public new long Id { get; set; }

        public NodeState State { get; set; }


        public string? Remark { get; set; }


        public long? HandlerId { get; set; }

        public Nullable<System.DateTime> HandleDate { get; set; }


        public string? Name { get; set; }


        public string? Type { get; set; }

        public decimal Left { get; set; }

        public decimal Top { get; set; }

        public decimal Width { get; set; }

        public decimal Height { get; set; }

        public long DataId { get; set; }

        public bool IsRedo { get; set; }
        /// <summary>
        /// 节点对应的节点模板Id
        /// </summary>
        public long? NodeTemplateId { get; set; }
        /// <summary>
        /// 节点对应的节点模板
        /// </summary>
        public virtual WF_NodeTemplate? NodeTemplate { get; set; }
        /// <summary>
        /// 对应的工作流Id
        /// </summary>
        public long WorkflowId { get; set; }
        /// <summary>
        /// 对应的工作流
        /// </summary>
        public virtual WF_Workflow Workflow { get; set; } = null!;
        /// <summary>
        /// 对应的节点类型Id
        /// </summary>
        public long NodeTypeId { get; set; }
        /// <summary>
        /// 对应的节点类型
        /// </summary>
        public virtual WF_NodeType NodeType { get; set; } = null!;

        public virtual ICollection<WF_Operation> Operator { get; set; } = null!;

        public virtual ICollection<WF_NodeLink> PreviousLines { get; set; } = null!;

        public virtual ICollection<WF_NodeLink> NextLines { get; set; } = null!;
    }
}
