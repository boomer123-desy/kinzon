using KingZon.Core.DataBase;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KingZon.Mes.Workflow.Entity
{
    /// <summary>
    /// 数据映射Map
    /// </summary>
    public partial class NodeLinkMap:KingZonEntityTypeConfiguration<WF_NodeLink>
    {
        public override void Configure(EntityTypeBuilder<WF_NodeLink> mb)
        {
            mb.HasKey(o => new { o.PreviousId, o.NextId });
            mb.HasOne(t => t.Previous)
                .WithMany(t => t.NextLines).
                HasForeignKey(d => d.PreviousId)
                .OnDelete(Microsoft.EntityFrameworkCore.DeleteBehavior.Cascade);

            mb.HasOne(t => t.Next)
                .WithMany(t => t.PreviousLines)
                .HasForeignKey(d => d.NextId)
                .OnDelete(Microsoft.EntityFrameworkCore.DeleteBehavior.Restrict);
            mb.HasOne(t => t.Workflow)
                .WithMany()
                .HasForeignKey(d => d.WorkflowId)
                .OnDelete(Microsoft.EntityFrameworkCore.DeleteBehavior.Cascade);
        }
    }
    /// <summary>
    /// 数据实体对象
    /// </summary>
    public partial class WF_NodeLink 
    {
        public long NextId { get; set; }

        public long PreviousId { get; set; }

        public virtual WF_Node Previous { get; set; } = null!;

        public virtual WF_Node Next { get; set; } = null!;
        public long WorkflowId { get; set; }
        public WF_Workflow Workflow { get; set; } = null!;
    }
}
