using KingZon.OLCM.Base.Shared.DataBase.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KingZon.OLCM.Base.Service.Dto
{
    public class ProductIdFieldDto
    {
        /// <summary>
        /// ProductId_SpecificationId_GoodsId
        /// </summary>
        public required string IdField { get; set; }
        public long ProductId { get; set; }
        public long? SpecificationId { get; set; }
    }

    public class ProductIdDto : ProductIdFieldDto
    {
        public List<Base_ProductPackage>? PackageList { get; set; }
        public List<GetProductUnitDto>? UnitList { get; set; }
        /// <summary>
        /// 是否过滤已存在客户产品的包装规格
        /// </summary>
        public bool? IsFilterGoods { get; set; }
    }

     public class GetProductUnitDto
 {
     /// <summary>
     /// 选项选中值
     /// </summary>
     public string? Value { get; set; }
     /// <summary>
     /// 选项编码
     /// </summary>
     public string? Code { get; set; }
     /// <summary>
     /// 选择显示值
     /// </summary>
     public string? Text { get; set; }
     public string? Extension { get; set; }
     public long? PackageId { get; set; }
     /// <summary>
     /// 单位ID
     /// </summary>
     public long? UnitId { get; set; }
     public bool IsMainUnit { get; set; } = false;
 }
}
