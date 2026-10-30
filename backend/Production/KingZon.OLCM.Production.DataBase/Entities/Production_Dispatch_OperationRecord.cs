using KingZon.Core.DataBase;
using KingZon.OLCM.Base.DataBase.Enum;
using KingZon.OLCM.Base.Shared.DataBase.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel.DataAnnotations.Schema;

namespace KingZon.OLCM.Production.Shared.DataBase.Entities
{
    [Comment("生产：派工报工操作记录")]
    public class Production_Dispatch_OperationRecordMap : KingZonEntityTypeConfiguration<Production_Dispatch_OperationRecord>
    {
        public override void Configure(EntityTypeBuilder<Production_Dispatch_OperationRecord> builder)
        {
            builder.HasKey(x => x.Id);
            builder.HasOne(x => x.NodeSettingMachine).WithMany()
                .HasForeignKey(x => new { x.NodeSettingMachineId, x.NodeSettingId })
                .HasPrincipalKey(x => new { x.Id, x.NodeSettingId })
                .OnDelete(DeleteBehavior.Restrict).IsRequired(false);
            builder.HasOne(x => x.NodeSetting).WithMany()
                .HasForeignKey(x => new { x.NodeSettingId, x.DispatchOrderId })
                .HasPrincipalKey(x => new { x.Id, x.DispatchOrderId })
                .OnDelete(DeleteBehavior.Restrict).IsRequired(false);
            builder.HasOne(builder => builder.DispatchOrder)
               .WithMany()
               .OnDelete(DeleteBehavior.Restrict)
               .HasForeignKey(x => x.DispatchOrderId)
               .IsRequired(true);
            builder.HasOne(builder => builder.OperateUser)
               .WithMany()
               .OnDelete(DeleteBehavior.Restrict)
               .HasForeignKey(x => x.OperateUserId)
               .IsRequired(true);
            builder.HasOne(builder => builder.Department)
               .WithMany()
               .OnDelete(DeleteBehavior.Restrict)
               .HasForeignKey(x => x.DepartmentId)
               .IsRequired(true);
            builder.Property(x => x.TotalPrice).HasComputedColumnSql("`Price` * `IncomeQuantity`", stored: true);
        }
    }
    public class Production_Dispatch_OperationRecord : BaseEntity
    {
        public new long Id { get; set; }
        /// <summary>
        /// 报工记录编号BG
        /// </summary>
        public required string RecordNo { get; set; }
        /// <summary>
        /// 派工单
        /// </summary>
        public long DispatchOrderId { get; set; }
        /// <summary>实际派工工序 ID；历史整单报工为空，工序派工报工时必须指定。</summary>
        public long? NodeSettingId { get; set; }
        /// <summary>设备报工对应的工序设备分配行；填写时必须同时指定 NodeSettingId。</summary>
        public long? NodeSettingMachineId { get; set; }
        /// <summary>
        /// 收入部门
        /// </summary>
        public int DepartmentId { get; set; }
        /// <summary>
        /// 报工人
        /// </summary>
        public int OperateUserId { get; set; }
        /// <summary>
        /// 报工数量
        /// </summary>
        [Column(TypeName = "decimal(12, 4)")]
        public decimal OperateQuantity { get; set; }
        /// <summary>
        /// 完工数量
        /// </summary>
        [Column(TypeName = "decimal(12, 4)")]
        public decimal CompleteQuantity { get; set; }
        /// <summary>
        /// 转换的主单位数量
        /// </summary>
        [Column(TypeName = "decimal(12, 4)")]
        public decimal MainQuantity { get; set; }
        [Column(TypeName = "decimal(12, 4)")]
        public decimal MainCompleteQuantity { get; set; }
        /// <summary>
        /// 产品单位数量
        /// </summary>
        [Column(TypeName = "decimal(12, 4)")]
        public decimal BaseQuantity { get; set; }
        [Column(TypeName = "decimal(12, 4)")]
        public decimal BaseCompleteQuantity { get; set; }
        /// <summary>
        /// 收入单位数量
        /// </summary>
        [Column(TypeName = "decimal(12, 4)")]
        public decimal IncomeQuantity { get; set; }
        [Column(TypeName = "decimal(12, 4)")]
        public decimal IncomeCompleteQuantity { get; set; }
        /// <summary>
        /// 报工收入单价
        /// </summary>
        [Column(TypeName = "decimal(12, 4)")]
        public decimal Price { get; set; }
        /// <summary>
        /// 合计收入
        /// </summary>
        [Column(TypeName = "decimal(12, 4)")]
        public decimal TotalPrice { get; set; }
        /// <summary>
        /// 备注
        /// </summary>
        public string? Remark { get; set; }
        public OperateType OperateType { get; set; }
        /// <summary>
        /// 阿米巴收入Income.Id
        /// </summary>
        public long? IncomeId { get; set; }
        

        #region 导航属性
        public Production_Dispatch_Order DispatchOrder { get; set; } = null!;
        public Production_NodeSetting? NodeSetting { get; set; }
        public Production_NodeSetting_MachineRL? NodeSettingMachine { get; set; }
        public Base_User OperateUser { get; set; } = null!;
        public Base_Department Department { get; set; } = null!;

        #endregion

    }
}
