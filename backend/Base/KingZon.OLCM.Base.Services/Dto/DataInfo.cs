using KingZon.OLCM.Base.Shared.DataBase.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KingZon.OLCM.Base.Service.Dto
{
    public class DataInfo
    {
        public string? DataNo { get; set; }
        public string? DataName { get; set; }
        public string? DataSpecification { get; set; }
        public string? DataMaterialGrade { get; set; }
        public string? DataProductType { get; set; }
        public string? DataCategory { get; set; }
        public long? DataCategoryId { get; set; }
        public string? DataUnit { get; set; }
        public long? DataUnitId { get; set; }
        public string? DataTechnicalCode { get; set; }
        public string? DataCustomerMaterialNo { get; set; }
        public string? DataMaterialNoCustomerName { get; set; }
        public string? DataRemark { get; set; }

        public decimal? DataMinStock { get; set; }
        public decimal? DataMaxStock { get; set; }
        public decimal DataTotalStock { get; set; }
        /// <summary>
        /// 对应成品主单位
        /// </summary>
        public long? DataBaseUnitId { get; set; }
        public string? DataBaseUnitName { get; set; }
        public decimal? ToBaseUnitRatio { get; set; }
        public long? ToBaseUnitConversionId { get; set; }
        public long? DataProductId { get; set; }

        /// <summary>
        /// 最近一次采购单价
        /// </summary>
        public decimal? CurrentPurchasePrice { get; set; }
    }
}
