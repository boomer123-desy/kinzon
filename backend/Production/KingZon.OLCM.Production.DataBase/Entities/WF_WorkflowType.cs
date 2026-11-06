using KingZon.Core.DataBase;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KingZon.Mes.Workflow.Entity
{
    /// <summary>
    /// 数据映射Map
    /// </summary>
    public partial class WorkflowTypeMap : KingZonEntityTypeConfiguration<WF_WorkflowType>
    {
        public override void Configure(EntityTypeBuilder<WF_WorkflowType> builder)
        {
            
        }
    }
    /// <summary>
    /// 数据实体对象
    /// </summary>
    public partial class WF_WorkflowType : BaseEntity
    {
        [Column(Order = 1)]
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]

        public new long Id { get; set; }

        public string? Name { get; set; }
    }
}
