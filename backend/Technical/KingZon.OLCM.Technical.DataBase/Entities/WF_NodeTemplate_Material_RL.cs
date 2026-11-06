using KingZon.Core.DataBase;
using KingZon.OLCM.Base.Shared.DataBase.Entities;
using KingZon.OLCM.Base.Shared.DataBase.Enum;
using KingZon.OLCM.Technical.Shared.DataBase.Enum;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KingZon.OLCM.Technical.Shared.DataBase.Entities
{

    [Comment("标准工序")]
    public class WF_NodeTemplate_Material_RLMap : KingZonEntityTypeConfiguration<WF_NodeTemplate_Material_RL>
    {
        public override void Configure(EntityTypeBuilder<WF_NodeTemplate_Material_RL> builder)
        {
            builder.ToTable("WF_NodeTemplate_Material_RL");

            builder.HasOne(o => o.RefCraftOrderVersion)
              .WithMany()
              .OnDelete(DeleteBehavior.Restrict)
              .HasForeignKey(o => o.RefCraftOrderVersionId)
              .HasConstraintName("FK_WF_NodeTemplate_Material_RL_RefCraftOrderVersion")
              .IsRequired(false);

            builder.HasOne(o => o.NodeSettingTemplate)
              .WithMany()
              .OnDelete(DeleteBehavior.Restrict)
              .HasForeignKey(o => o.NodeSettingTemplateId)
              .HasConstraintName("FK_WF_NodeTemplate_Material_RL_NodeSettingTemplate")
              .IsRequired(true);

            builder.HasOne(o => o.Unit)
              .WithMany()
              .OnDelete(DeleteBehavior.Restrict)
              .HasForeignKey(o => o.UnitId)
              .IsRequired(true);

            builder.HasOne(o=> o.InsteadSource)
                .WithMany(o => o.Substitutes)
                .OnDelete(DeleteBehavior.Restrict)
                .HasForeignKey(o => o.InsteadSourceId)
                .HasConstraintName("FK_WF_NodeTemplate_Material_RL_InsteadSource")
                .IsRequired(false);
        }
    }

    public class WF_NodeTemplate_Material_RL : BaseEntity
    {
        public new long Id { get; set; }

        /// <summary>
        /// 所属工序 ID
        /// </summary>
        public long NodeSettingTemplateId { get; set; }

        /// <summary>
        /// 物料类型
        /// </summary>
        public InventoryDataType DataType { get; set; }

        /// <summary>
        /// 物料ID
        /// </summary>
        public long DataId { get; set; }

        /// <summary>
        /// 物料类型
        /// </summary>
        public NodeIOType IOType { get; set; }

        /// <summary>
        /// 替换料的 ID
        /// </summary>
        public long? InsteadSourceId { get; set; }

        /// <summary>
        /// 用量或产出数量
        /// </summary>
        [Column(TypeName = "decimal(18, 6)")]
        public decimal Quantity { get; set; }

        /// <summary>
        /// 单位 ID
        /// </summary>
        public long UnitId { get; set; }

        /// <summary>
        /// 多单位转换 ID
        /// </summary>
        public long UnitConversionId { get; set; }

        /// <summary>
        /// 多单位转换Json 对应 InboundUnitDto
        /// </summary>
        public string UnitListInfo { get; set; } = string.Empty;

        /// <summary>
        /// 损耗率
        /// <summary>
        [Column(TypeName = "decimal(9, 6)")]
        public decimal LossRate { get; set; }

        /// <summary>
        /// 显示顺序
        /// </summary>
        public int SortNo { get; set; }

        /// <summary>
        /// 外部半成品引用的工艺版本 ID
        /// </summary>
        public long? RefCraftOrderVersionId { get; set; }

        /// <summary>
        /// 备注
        /// </summary>
        public string? Remark { get; set; }

        #region 导航属性

        /// <summary>
        /// 所属工序
        /// </summary>
        public WF_NodeTemplate_ProductionExt NodeSettingTemplate { get; set; } = null!;

        /// <summary>
        /// 外部半成品引用的工艺版本
        /// </summary>
        public Technical_CraftOrderVersion? RefCraftOrderVersion { get; set; }

        public Base_Unit Unit { get; set; } = null!;

        /// <summary>
        /// 替换料来源（本表自引用）：本行是某物料的替换料时，指向被它替换的原物料行
        /// </summary>
        public WF_NodeTemplate_Material_RL? InsteadSource { get; set; }

        /// <summary>
        /// 反向导航：以本行为原物料的所有替换料行
        /// </summary>
        public ICollection<WF_NodeTemplate_Material_RL> Substitutes { get; set; } = new List<WF_NodeTemplate_Material_RL>();

        #endregion
    }
}
