using KingZon.Core.DataBase;
using KingZon.Mes.Workflow.Enum;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KingZon.Mes.Workflow.Entity
{

    /// <summary>
    /// 数据映射Map
    /// </summary>
    public partial class NodeTypeMap : KingZonEntityTypeConfiguration<WF_NodeType>
    {
        public override void Configure(EntityTypeBuilder<WF_NodeType> builder)
        {
        }
    }
    /// <summary>
    /// 数据实体对象
    /// </summary>
    public partial class WF_NodeType
    : BaseEntity
    {
       
        [Column(Order = 1)]
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]

        public new long Id { get; set; }


        public string? Name { get; set; }


        public string? Area { get; set; }


        public string? Action { get; set; }


        public string? Controller { get; set; }

        public NodeType Type { get; set; }

        /// <summary>
        /// 对应的工作流类型Id
        /// </summary>
        public long WorkflowTypeId { get; set; }
        /// <summary>
        /// 对应的工作流类型
        /// </summary>
        public virtual WF_WorkflowType WorkflowType { get; set; } = null!;
    }
}
