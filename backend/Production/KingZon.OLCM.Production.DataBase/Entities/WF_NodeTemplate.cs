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
    public partial class NodeTemplateMap: KingZonEntityTypeConfiguration<WF_NodeTemplate>
    {
        public override void Configure(EntityTypeBuilder<WF_NodeTemplate> builder)
        {
            builder.HasOne(o => o.WorkflowTemplate)
                .WithMany(o=>o.NodeTemplates)
                .HasForeignKey(o => o.WorkflowTemplateId);
        }
    }
    /// <summary>
    /// 节点模板
    /// </summary>
    public partial class WF_NodeTemplate
    : BaseEntity


    {
        public WF_NodeTemplate()
        {
        }

        [Column(Order = 1)]
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]

        public new long Id { get; set; }


        public string Name { get; set; }


        public string Type { get; set; }

        public decimal Left { get; set; }

        public decimal Top { get; set; }

        public decimal Width { get; set; }

        public decimal Height { get; set; }

        public long NodeTypeId { get; set; }
        public virtual WF_NodeType NodeType { get; set; }

        public long DataId { get; set; }
        public long WorkflowTemplateId { get; set; }
        /// <summary>
        /// 对应的流程模板
        /// </summary>
        public virtual WF_WorkflowTemplate WorkflowTemplate { get; set; }

        public virtual ICollection<WF_NodeTemplateLink> PreviousLineTemplates { get; set; } = new HashSet<WF_NodeTemplateLink>();

        public virtual ICollection<WF_NodeTemplateLink> NextLineTemplates { get; set; } = new HashSet<WF_NodeTemplateLink>();

        public virtual ICollection<WF_NodePermissionTemplate> NodePermissionTemplates { get; set; } = new HashSet<WF_NodePermissionTemplate>();
    }
}
