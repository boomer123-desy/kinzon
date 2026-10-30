using KingZon.Core.DataBase;
using KingZon.Mes.Workflow.Enum;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace KingZon.Mes.Workflow.Entity
{
    /// <summary>
    /// 数据映射Map
    /// </summary>
    public partial class WorkflowMap : KingZonEntityTypeConfiguration<WF_Workflow>
    {
        public override void Configure(EntityTypeBuilder<WF_Workflow> mb)
        {
            mb.HasOne(o => o.WorkflowType)
                .WithMany()
                .HasForeignKey(o => o.WorkflowTypeId)
                .OnDelete(Microsoft.EntityFrameworkCore.DeleteBehavior.Restrict);

            mb.HasOne(o => o.WorkflowTemplate)
                .WithMany()
                .HasForeignKey(o => o.WorkflowTemplateId)
                .OnDelete(Microsoft.EntityFrameworkCore.DeleteBehavior.Restrict);
        }
    }
    /// <summary>
    /// 数据实体对象
    /// </summary>
    public partial class WF_Workflow
    : BaseEntity
    {
        public WF_Workflow()
        {
            this.Nodes = new HashSet<WF_Node>();
        }

        [Column(Order = 1)]
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]

        public new long Id { get; set; }
        /// <summary>
        /// 工作流状态
        /// </summary>
        public WorkflowState State { get; set; }
        /// <summary>
        /// 备注
        /// </summary>
        public string Note { get; set; }
        /// <summary>
        /// 创建时间
        /// </summary>
        public override System.DateTime CreateTime { get; set; }
        /// <summary>
        /// 对应的数据Id
        /// </summary>
        public long DataId { get; set; }
        /// <summary>
        /// 对应的工作流模板Id
        /// </summary>
        public long WorkflowTemplateId { get; set; }
        /// <summary>
        /// 对应的工作流模板
        /// </summary>
        public virtual WF_WorkflowTemplate WorkflowTemplate { get; set; }
        /// <summary>
        /// 工作流类型Id
        /// </summary>
        public long WorkflowTypeId { get; set; }
        /// <summary>
        /// 工作流类型
        /// </summary>
        public virtual WF_WorkflowType WorkflowType { get; set; }
        /// <summary>
        /// 工作流对应的节点
        /// </summary>
        public virtual ICollection<WF_Node> Nodes { get; set; }
    }
}
