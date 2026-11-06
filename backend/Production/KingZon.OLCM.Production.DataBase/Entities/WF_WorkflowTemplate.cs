using KingZon.Core.DataBase;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KingZon.Mes.Workflow.Entity
{

    /// <summary>
    /// 数据映射Map
    /// </summary>
    public partial class WorkflowTemplateMap:KingZonEntityTypeConfiguration<WF_WorkflowTemplate>
    {

        public override void Configure(EntityTypeBuilder<WF_WorkflowTemplate> builder)
        {
            builder.HasOne(o => o.WorkflowType)
                .WithMany()
                .HasForeignKey(o => o.WorkflowTypeId);
            builder.HasOne(o => o.StartNode)
                .WithMany()
                .HasForeignKey(o => o.StartNodeId)
                .OnDelete(Microsoft.EntityFrameworkCore.DeleteBehavior.Restrict);
            builder.HasOne(o => o.EndNode)
                .WithMany()
                .HasForeignKey(o => o.EndNodeId)
                .OnDelete(Microsoft.EntityFrameworkCore.DeleteBehavior.Restrict);
        }

    }
    /// <summary>
    /// 工作流模板
    /// </summary>
    public partial class WF_WorkflowTemplate : BaseEntity
    {
        [Column(Order = 1)]
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]

        public new long Id { get; set; }
        public string? Name { get; set; }
        public long WorkflowTypeId { get; set; }
        public virtual WF_WorkflowType WorkflowType { get; set; } = null!;
        public long? StartNodeId { get; set; }
        public WF_NodeTemplate? StartNode { get; set; } = null!;
        public long? EndNodeId { get; set; }
        public WF_NodeTemplate? EndNode { get; set; } = null!;
        public virtual ICollection<WF_NodeTemplate> NodeTemplates { get; set; } = null!;
        public virtual ICollection<WF_NodeTemplateLink> LineTemplates { get; set; } = null!;
    }
}
