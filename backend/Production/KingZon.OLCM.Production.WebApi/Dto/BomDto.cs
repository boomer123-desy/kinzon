using KingZon.OLCM.Base.Shared.DataBase.Enum;
using KingZon.OLCM.Production.Shared.DataBase.Enum;
using Kingzon.OLCM.Base.Services.JsonStructure;
using System.Collections.Generic;
using KingZon.OLCM.Base.Shared.DataBase.JsonStructure;
using KingZon.OLCM.Base.Shared.DataBase.Entities;
using KingZon.OLCM.Common.Service.Dto;

namespace KingZon.OLCM.Production.WebApi.Dto
{
    public class BomDto
    {
        public long? Id { get; set; }
        public long? ProductId { get; set; }
        public long? SpecificationId { get; set; }
        public string? ProductNo { get; set; }
        public string? ProductName { get; set; }
        public string? ProductTechnicalCode { get; set; }
        public string? ProductSpecification { get; set; }
        public string? ProductRemark { get; set; }
    }

    public class BomVersionDataDto
    {
        public long VersionId { get; set; }
        public BomVersionDto Version { get; set; } = new BomVersionDto();
        public List<BomDetailDto> DetailList { get; set; } = new List<BomDetailDto> { new BomDetailDto() };
        public int Index { get; set; }
    }

    public class BomVersionDto
    {
        public long? Id { get; set; }
        public string Version { get; set; } = "";
        public bool IsMain { get; set; }
        public BomStatus Status { get; set; }
        public decimal TotalQuantity { get; set; }
        public long UnitConversionId { get; set; }
        public string? Remark { get; set; }
        public List<CustomAttribute>? CustomAttributeList { get; set; }
        public List<InventoryUnitOutboundDto> UnitInfoList { get; set; } = new List<InventoryUnitOutboundDto>();
        public List<Base_FileStorage>? FileData { get; set; }
        public bool IsDeleted { get; set; }

    }

    public class BomDetailDto
    {
        public long? Id { get; set; }
        public long? VersionId { get; set; }
        public BomDetailType DetailType { get; set; }
        public long DetailId { get; set; }
        public int Order { get; set; } = 1;
        public decimal Quantity { get; set; }
        //底数单位
        public string? BaseUnitName { get; set; }
        public decimal Ratio { get; set; }
        public decimal UpperTolerance { get; set; }
        public decimal LowerTolerance { get; set; }
        public decimal CurQuantity { get; set; }
        public decimal CurRatio { get; set; }

        public List<BomDetailDto> ReplaceData { get; set; } = new List<BomDetailDto>(); // 替换料
        public long? DetailDataUnitId { get; set; }

        public List<BomDetailDto> Children { get; set; } = new List<BomDetailDto>(); // 子配方

        public List<InventoryUnitOutboundDto> UnitInfoList { get; set; } = new List<InventoryUnitOutboundDto>();

        public bool Editable { get; set; }
        /// <summary>
        /// 物料清单及明细 对应的文件
        /// </summary>
        public List<Base_FileStorage>? FileData { get; set; }
        /// <summary>
        /// 明细为子清单 子清单自身对应的文件
        /// </summary>
        public List<Base_FileStorage>? DetailFileData { get; set; }

        public decimal BaseQuantity { get; set; }
        public decimal CurBaseQuantity { get; set; }
        public string DetailDataNo { get; set; } = string.Empty;

        public string DetailDataName { get; set; } = string.Empty;

        public string DetailDataTechCode { get; set; } = string.Empty;

        public string DetailDataSpecification { get; set; } = string.Empty;

        public string DetailDataUnit { get; set; } = string.Empty;
        public string DetailDataVersion { get; set; } = string.Empty;

        public long? DetailFormulaId { get; set; }
        public long? DetailBomId { get; set; }

        public ProductType? DetailProductType { get; set; }

        public string DetailDataMaterialGrade { get; set; } = string.Empty;
        /// <summary>
        /// 物料类别
        /// </summary>
        public string DetailDataCategoryName { get; set; } = string.Empty;
        /// <summary>
        /// 占用库存
        /// </summary>
        public decimal OccupyInventory { get; set; }
        /// <summary>
        /// 正常库存
        /// </summary>
        public decimal InventoryQuantity { get; set; }
        /// <summary>
        /// 异常库存
        /// </summary>
        public decimal AbnormalQuantity { get; set; }
        /// <summary>
        /// 正常+异常
        /// </summary>
        public decimal TotalStockQuantity
        {
            get
            {
                return InventoryQuantity + AbnormalQuantity;
            }
        }
        public long? MainUnitId { get; set; }
        public string? MainUnitName { get; set; }

        /// <summary>
        /// 子项 对应的产品（或规格）信息
        /// </summary>
        public DetailWithProductDto? DetailWithProductDto { get; set; }
        /// <summary>
        /// 可选单位 前端使用
        /// </summary>
        public List<SelectionDto>? UnitData { get; set; }

        public long UnitConversionId { get; set; }
        public string Unit { get; set; } = "";
        /// <summary>
        /// 当明细是配方明细时 填入配方明细的Id
        /// </summary>
        public long? FormulaDetailId { get; set; }

        public long? MainFormulaDetailId { get; set; }
    }
    public class DetailWithProductDto
    {
        public long ProductId { get; set; }
        public long? SpecificationId { get; set; }
        public string ProductNo { get; set; } = string.Empty;

        public string ProductName { get; set; } = string.Empty;

        public string ProductTechCode { get; set; } = string.Empty;

        public string ProductSpecification { get; set; } = string.Empty;

        public string ProductUnit { get; set; } = string.Empty;
        public string ProductMaterialGrade { get; set; } = string.Empty;
        public string ProductCategoryName { get; set; } = string.Empty;
        public ProductType ProductType { get; set; }
    }

}
