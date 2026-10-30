using KingZon.Core.DataBase;
using KingZon.OLCM.Base.DataBase.Enum;
using KingZon.OLCM.Base.Shared.DataBase.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel.DataAnnotations.Schema;

namespace KingZon.OLCM.Production.Shared.DataBase.Entities
{
    [Comment("生产：派工报工操作关联记录")]
    public class Production_Dispatch_OperationRecordRLMap : KingZonEntityTypeConfiguration<Production_Dispatch_OperationRecordRL>
    {
        public override void Configure(EntityTypeBuilder<Production_Dispatch_OperationRecordRL> builder)
        {
            builder.HasKey(x => new { x.SourceOperationId, x.TargetOperationId });
            builder.HasOne(builder => builder.SourceOperation)
               .WithMany()
               .OnDelete(DeleteBehavior.Cascade)
               .HasForeignKey(x => x.SourceOperationId)
               .IsRequired(true);
            builder.HasOne(builder => builder.TargetOperation)
               .WithMany()
               .OnDelete(DeleteBehavior.Cascade)
               .HasForeignKey(x => x.TargetOperationId)
               .IsRequired(true);
        }
    }
    public class Production_Dispatch_OperationRecordRL
    {
        public long SourceOperationId { get; set; }
        public long TargetOperationId { get; set; }

        /// <summary>
        /// 报工数量
        /// </summary>
        [Column(TypeName = "decimal(12, 4)")]
        public decimal OperateQuantity { get; set; }
        /// <summary>
        /// 主单位数量
        /// </summary>
        [Column(TypeName = "decimal(12, 4)")]
        public decimal MainQuantity { get; set; }
        /// <summary>
        /// 产品单位数量
        /// </summary>
        [Column(TypeName = "decimal(12, 4)")]
        public decimal BaseQuantity { get; set; }
        /// <summary>
        /// 收入单位数量
        /// </summary>
        [Column(TypeName = "decimal(12, 4)")]
        public decimal IncomeQuantity { get; set; }

        #region 导航属性
        public Production_Dispatch_OperationRecord SourceOperation { get; set; } = null!;
        public Production_Dispatch_OperationRecord TargetOperation { get; set; } = null!;

        #endregion

    }
}
