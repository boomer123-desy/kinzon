using KingZon.Core.DataBase;
using KingZon.OLCM.Base.Shared.DataBase.Enum;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel.DataAnnotations.Schema;
using InspectionConclusion = KingZon.OLCM.Base.Shared.DataBase.Enum.InspectionConclusion;

namespace KingZon.OLCM.Inventory.Shared.DataBase.Entities
{
    public class Inventory_ArrivalRecordMap : KingZonEntityTypeConfiguration<Inventory_ArrivalRecord>
    {
        public override void Configure(EntityTypeBuilder<Inventory_ArrivalRecord> builder)
        {
            builder.ToTable("Inventory_ArrivalRecord");
            builder.HasKey(x => x.Id);
            builder.HasDiscriminator(x => x.Inventory_ArrivalRecordType)
                    .HasValue<PurchaseArrivalRecord>("Purchase")
                    .HasValue<OutSourceArrivalRecord>("OutSource")
                    .HasValue<SaleReturnArrivalRecord>("SaleReturn");

            //var purchaseOrderDetail = Type.GetType("KingZon.OLCM.Purchase.DataBase.Entities.Purchase_Order_Detail,KingZon.OLCM.Purchase");
            //builder.HasOne(purchaseOrderDetail, null)
            //    .WithMany()
            //    .HasForeignKey(PurchaseArrivalRecord.PurchaseOrderDetailId)
            //    .OnDelete(DeleteBehavior.Restrict);

            //builder.HasOne("KingZon.OLCM.Purchase.DataBase.Entities.Purchase_Order_Detail", null)
            //       .WithMany()
            //       .HasForeignKey("PurchaseOrderDetailId")
            //       .IsRequired(true)
            //       .OnDelete(DeleteBehavior.Restrict);
        }

    }
    public class Inventory_ArrivalRecord : BaseEntity
    {
        public new long Id { get; set; }
        /// <summary>
        /// 到货记录编号
        /// </summary>
        public required string ArrivalRecordNo { get; set; }
        /// <summary>
        /// 到货量：到货单位
        /// </summary>
        [Column(TypeName = "decimal(18,6)")]
        public decimal ArrivalQuantity { get; set; }
        /// <summary>
        /// 到货数量：物料主单位
        /// </summary>
        [Column(TypeName = "decimal(18,6)")]
        public decimal BaseArrivalQuantity { get; set; }
        /// <summary>
        /// 入库数量：物料主单位
        /// </summary>
        [Column(TypeName = "decimal(18, 6)")]
        public decimal InventoryQuantity { get; set; }
        /// <summary>
        /// 到货时间
        /// </summary>
        public DateTime ArrivalTime { get; set; }
        /// <summary>
        /// 到货入库是否完成
        /// </summary>
        public bool IsCompleted { get; set; } = false;
        /// <summary>
        /// 备注
        /// </summary>
        public string? Remark { get; set; }
        /// <summary>
        /// 是否已检验
        /// </summary>
        public bool IsInspection { get; set; } = false;
        /// <summary>
        /// 拓展参数,CustomAttribute
        /// </summary>
        [Column(TypeName = "json")]
        public string? CustomAttribute { get; set; }
        /// <summary>
        /// 检验结论
        /// </summary>
        public InspectionConclusion InspectionConclusion { get; set; } = InspectionConclusion.合格入库;
        /// <summary>
        /// 库位库区Id
        /// </summary>
        public long PositionId { get; set; }
        public string? Inventory_ArrivalRecordType {  get; set; }
        /// <summary>
        /// 到货单位
        /// </summary>
        public string ArrivalUnit { get; set; } = string.Empty;
        /// <summary>
        /// 到货单位Id
        /// </summary>
        public long ArrivalUnitId { get; set; }
        /// <summary>
        /// -1表示主单位
        /// </summary>
        public long ArrivalUnitConversionId { get; set; }
        /// <summary>
        /// 和主单位的转化率
        /// </summary>
        [Column(TypeName = "decimal(12, 4)")]
        public decimal Ratio { get; set; }
        /// <summary>
        /// 单位转化json,[{UnitId:,UnitName:,PlaceOrderQuantity:,Ratio:}
        /// </summary>
        [Column(TypeName = "json")]
        public string? UnitConversionList { get; set; } = string.Empty;


    }

    public class PurchaseArrivalRecord : Inventory_ArrivalRecord{
        /// <summary>
        ///  采购订单详情Id
        /// </summary>
        public long? PurchaseOrderDetailId { get; set; }
        /// <summary>
        /// 物料状态
        /// </summary>
        public InventoryDataStatus DataStatus { get; set; }
    }
    public class OutSourceArrivalRecord : Inventory_ArrivalRecord
    {
        /// <summary>
        /// 外协详情Id
        /// </summary>
        public long? OutSourceOrderDetailId { get; set; }
        /// <summary>
        /// 到货返修数量
        /// </summary>
        [Column(TypeName = "decimal(16, 3)")]
        public decimal ArrivalReworkQuantity { get; set; }
        /// <summary>
        /// 到货返修重量
        /// </summary>
        [Column(TypeName = "decimal(16, 3)")]
        public decimal ArrivalReworkWeight { get; set; }
        public bool IsReturnComplete { get; set; } = false;
    }
    public class SaleReturnArrivalRecord : Inventory_ArrivalRecord
    {
        /// <summary>
        ///  销售退货单详情Id
        /// </summary>
        public long? SaleReturnOrderDetailId { get; set; }
        /// <summary>
        /// 物料状态
        /// </summary>
        public InventoryDataStatus DataStatus { get; set; }
    }
}
