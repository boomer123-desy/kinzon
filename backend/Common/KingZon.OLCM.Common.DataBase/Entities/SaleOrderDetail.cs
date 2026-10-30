using KingZon.Core.DataBase;
using KingZon.OLCM.Common.DataBase.Entities;
using KingZon.OLCM.Common.DataBase.Enum;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KingZon.OLCM.Common.DataBase.Entities
{
    public class SaleOrderDetailMap : KingZonEntityTypeConfiguration<SaleOrderDetail>
    {
        public override void Configure(EntityTypeBuilder<SaleOrderDetail> builder)
        {
            builder.HasKey(x => x.Id);
            builder.HasOne(x => x.SaleOrder)
                .WithMany(x => x.SaleOrderDetails)
                .OnDelete(DeleteBehavior.Cascade)
                .HasForeignKey(x => x.SaleOrderId)
                .IsRequired(true);
            builder.HasOne(x=> x.Product)
                .WithMany()
                .OnDelete(DeleteBehavior.Restrict)
                .HasForeignKey(x => x.ProductId)
                .IsRequired(false);
            builder.HasOne(x=> x.HistoryProduct)
                .WithMany()
                .OnDelete(DeleteBehavior.Restrict)
                .HasForeignKey(x => x.HistoryProductId)
                .IsRequired(false);
        }
    }
    public class SaleOrderDetail : BaseEntity
    {
        /// <summary>
        /// 下单数量
        /// </summary>
        public int PlaceOrderQuantity { get; set; }
        /// <summary>
        /// 拣货数量
        /// </summary>
        public int PickQuantity { get; set; }
        /// <summary>
        /// 拣货重量
        /// </summary>
        [Column(TypeName = "decimal(16, 3)")]
        public decimal PickWight { get; set; }
        /// <summary>
        /// 包装数量
        /// </summary>
        public int PackageQuantity { get; set; }
        /// <summary>
        /// 送货数量
        /// </summary>
        public int DeliveryQuantity { get; set; }
        /// <summary>
        /// 送货通知数量
        /// </summary>
        public int DeliveryNoticeQuantity { get; set; }
        /// <summary>
        /// 不含税单价
        /// </summary>
        [Column(TypeName = "decimal(12, 4)")]
        public decimal Price { get; set; }
        /// <summary>
        /// 模具费
        /// </summary>
        [Column(TypeName = "decimal(12, 4)")]
        public decimal MoldCost { get; set; }
        /// <summary>
        /// 不含税总价
        /// </summary>
        public decimal TotalPrice { get; set; }
        /// <summary>
        /// 是否加急
        /// </summary>
        public bool IsUrgent { get; set; }
        /// <summary>
        /// 批量(false)/样品(true)
        /// </summary>
        public bool IsModel { get; set; } = false;
        /// <summary>
        /// 是否评审
        /// </summary>
        public bool IsReview { get; set; }
        /// <summary>
        /// 状态
        /// </summary>
        public SaleOrderStatus Status { get; set; }
        /// <summary>
        /// 客户交期
        /// </summary>
        public DateTime DeliveryTime { get; set; }
        /// <summary>
        /// 备注
        /// </summary>
        public string? Remark { get; set; }
        /// <summary>
        /// 历史产品ID
        /// </summary>
        public int? HistoryProductId { get; set; }
        /// <summary>
        /// 基础产品ID
        /// </summary>
        public int? ProductId { get; set; }
        /// <summary>
        /// 销售订单ID
        /// </summary>
        public int SaleOrderId { get; set; }

        /// <summary>
        /// 模具费是否对账
        /// </summary>
        public bool IsMoldAccount { get; set; }
        /// <summary>
        /// 客户产品图号
        /// </summary>
        public string? CustomerMaterialNo { get; set; }
        /// <summary>
        /// 拣选异常重量
        /// </summary>
        [Column(TypeName = "decimal(16, 3)")]
        public decimal PickAbnormalWeight { get; set; }
        /// <summary>
        /// 导航属性
        /// </summary>
        public SaleOrder SaleOrder { get; set; } = null!;
        public Product? Product { get; set; }
        public HistoryProduct? HistoryProduct { get; set; }
        public int StockPlanQuantity { get; set; } = 0;
        /// <summary>
        /// 含税单价
        /// </summary>
        [Column(TypeName = "decimal(12, 4)")]
        public decimal PriceHaveTax { get; set; }
        /// <summary>
        /// 含税总价
        /// </summary>
        public decimal TotalPriceHaveTax { get; set; }
        /// <summary>
        /// 税率
        /// </summary>
        public decimal TaxRate { get; set; }


        //#region 从标品表映射的字段
        ///// <summary>
        ///// 下单产品（外部产品 下单钧业备货单时 此字段为空）
        ///// </summary>
        //public long? GoodsId { get; set; }
        //public long? SpecificationId { get; set; }
        ///// <summary>
        ///// 计划生产数量（销售单位）
        ///// </summary>
        //[Column(TypeName = "decimal(12, 4)")]
        //public decimal PlanProductionQuantity { get; set; } = 0m;
        ///// <summary>
        ///// 计划外购数量（销售单位）
        ///// </summary>
        //[Column(TypeName = "decimal(12, 4)")]
        //public decimal PlanPurchaseQuantity { get; set; } = 0m;
        ///// <summary>
        ///// 计划直接发货数量 （销售单位）
        ///// </summary>
        //[Column(TypeName = "decimal(12, 4)")]
        //public decimal PlanDirectDliveryQuantity { get; set; } = 0m;
        ///// <summary>
        ///// 订单明细扩展参数
        ///// </summary>
        //[Column(TypeName = "json")]
        //public string? SaleExtendedInfo { get; set; }
        ///// <summary>
        ///// 评审状态
        ///// </summary>
        //public ReviewStatus ReviewStatus { get; set; } = ReviewStatus.待提交;
        //public bool IsDeleted { get; set; } = false;
        ///// <summary>
        ///// 订单已排产数量(商品基础主单位)
        ///// </summary>
        //[Column(TypeName = "decimal(12, 4)")]
        //public decimal ProductionQuantity { get; set; } = 0m;
        ///// <summary>
        ///// 排产已入库数量(商品基础主单位)
        ///// </summary>
        //[Column(TypeName = "decimal(12, 4)")]
        //public decimal ProductionInventoryQuantity { get; set; } = 0m;
        ///// <summary>
        ///// 外购数量(商品基础主单位)
        ///// </summary>
        //[Column(TypeName = "decimal(12, 4)")]
        //public decimal PurchaseQuantity { get; set; } = 0m;
        ///// <summary>
        ///// 外购已入库数量(商品基础主单位)
        ///// </summary>
        //[Column(TypeName = "decimal(12, 4)")]
        //public decimal PurchaseInventoryQuantity { get; set; } = 0m;
        ///// <summary>
        ///// 售卖单位 不建立外键
        ///// </summary>
        //public long? SalesUnitId { get; set; }
        ///// <summary>
        ///// 售卖单位
        ///// </summary>
        //public  string? SalesUnit { get; set; }
        ///// <summary>
        ///// 不建立外键
        ///// </summary>
        //public long? SalesUnitConversionId { get; set; }
        ///// <summary>
        ///// 售卖单位和基础单位、包装单位换算信息[{UnitId:,UnitName:,PlaceOrderQuantity:,Ratio:}]
        ///// 对应DTO SaleUnitConversionInfo
        ///// </summary>
        //[Column(TypeName = "json")]
        //public string? UnitConversionInfo { get; set; }
        ///// <summary>
        ///// 外包装 不建立外键
        ///// </summary>
        //public long? PackageId { get; set; }
        ///// <summary>
        ///// 包装规格
        ///// </summary>
        //public string? PackageSpecification { get; set; }
        ///// <summary>
        ///// 产品编码（成品编码 或者 规格编码）
        ///// </summary>
        //public string? GoodsNo { get; set; }
        ///// <summary>
        ///// 产品名称（成品名称 或者 规格名称）
        ///// </summary>
        //public string? GoodsName { get; set; }
        ///// <summary>
        ///// 产品单位（成品单位 或者 规格单位）
        ///// </summary>
        //public string? GoodsUnit { get; set; }
        ///// <summary>
        ///// 规格型号
        ///// </summary>
        //public string? Specification { get; set; }
        ///// <summary>
        ///// 技术型号（内部型号）
        ///// </summary>
        //public string? TechnicalCode { get; set; }
        ///// <summary>
        ///// 产品类别（成品类别 或者 规格类别）
        ///// </summary>
        //public string? GoodsCategory { get; set; }
        //public string? GoodsRemark { get; set; }
        //public string? GoodsPackageWay { get; set; }
        ///// <summary>
        ///// 客户产品名称
        ///// </summary>
        //public string? CustomerMaterialName { get; set; }
        ///// <summary>
        ///// 产品扩展参数
        ///// </summary>
        //[Column(TypeName = "json")]
        //public string? GoodsExtendedInfo { get; set; }
        //[Column(TypeName = "json")]
        //public string? InputProductionInfo { get; set; }

        //[Column(TypeName = "json")]
        //public string? InputDeliveryInfo { get; set; }
        ///// <summary>
        ///// 手动输入的生产总量（销售单位）
        ///// </summary>
        //[Column(TypeName = "decimal(12, 4)")]
        //public decimal? TotalInputProductionQty { get; set; }
        //[Column(TypeName = "decimal(12, 4)")]
        //public decimal? TotalInputDeliveryQty { get; set; }
        //#endregion

    }
}
