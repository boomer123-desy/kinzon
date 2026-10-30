using System.Collections.Generic;
using KingZon.OLCM.Base.Shared.DataBase.Enum;

namespace KingZon.OLCM.Technical.WebApi.Dto
{
    public class ProductionScheduleQuantityQueryDto
    {
        public long CraftVersionId { get; set; }

        /// <summary>成品生产数量，使用所选工序单的产品/规格默认单位。</summary>
        public decimal Quantity { get; set; }
        public string? Keyword { get; set; }
        public bool ShortageOnly { get; set; }
    }

    public class ScheduleMaterialInfoDto
    {
        public InventoryDataType DataType { get; set; }
        public long DataId { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Specification { get; set; } = string.Empty;

        /// <summary>物料数量统一使用产品基本单位，规格库存不与产品库存合并。</summary>
        public long UnitId { get; set; }
        public string Unit { get; set; } = string.Empty;
    }

    public class ScheduleMaterialQuantityDto : ScheduleMaterialInfoDto
    {
        public decimal Gross { get; set; }
        public decimal Stock { get; set; }
        public decimal Occupied { get; set; }
        public decimal SafetyStock { get; set; }

        /// <summary>本需求分配到的可用库存，跨分支只分配一次。</summary>
        public decimal Available { get; set; }

        /// <summary>库存口径缺量；当前查询未抵扣在途、在制和排产供应。</summary>
        public decimal Net { get; set; }
        // 当前范围不查询排产/到货单据；null 表示未查询，不能解释为实际数量为零。
        public decimal? Transit { get; set; }
        public decimal? InProduction { get; set; }
        public decimal? ScheduledTotal { get; set; }
    }

    public class ScheduleMaterialNodeDto : ScheduleMaterialQuantityDto
    {
        /// <summary>完整工艺引用路径组成的需求实例 ID，同物料不同分支不会共用节点 ID。</summary>
        public string Id { get; set; } = string.Empty;
        public string ParentId { get; set; } = string.Empty;
        public int Level { get; set; }
        public string Kind { get; set; } = "material";
        public string Status { get; set; } = string.Empty;
        public long SourceCraftVersionId { get; set; }

        /// <summary>汇总来源的工序物料行Id和工序Id，供排产关联。</summary>
        public List<long> MaterialRlIds { get; set; } = new();
        public List<long> NodeSettingTemplateIds { get; set; } = new();

        /// <summary>领用半成品所引用的工艺版本Id。</summary>
        public long? CraftVersionId { get; set; }
        public List<ScheduleMaterialNodeDto> Children { get; set; } = new();
        public List<ScheduleMaterialSubstituteDto> Substitutes { get; set; } = new();
    }

    public class ScheduleMaterialSubstituteDto : ScheduleMaterialInfoDto
    {
        public List<long> MaterialRlIds { get; set; } = new();
        public List<long> NodeSettingTemplateIds { get; set; } = new();
        public long? CraftVersionId { get; set; }

        /// <summary>每基本单位父项产出对应的替料标准用量；不自动抵扣主料需求。</summary>
        public decimal QuantityPerParentUnit { get; set; }
    }

    public class ScheduleMaterialSummaryDto : ScheduleMaterialQuantityDto
    {
        public int OccurrenceCount { get; set; }
    }

    public class ProductionScheduleQuantityResultDto
    {
        public long CraftOrderId { get; set; }
        public string CraftOrderCode { get; set; } = string.Empty;
        public string CraftOrderName { get; set; } = string.Empty;
        public long CraftVersionId { get; set; }
        public string CraftVersion { get; set; } = string.Empty;
        public ScheduleMaterialInfoDto Product { get; set; } = new();
        public decimal Quantity { get; set; }
        public long QuantityUnitId { get; set; }
        public string QuantityUnit { get; set; } = string.Empty;
        public string QuantityBasis { get; set; } = "inventory";

        /// <summary>过滤之前的需求节点数。</summary>
        public int NodeCount { get; set; }
        public List<ScheduleMaterialNodeDto> Nodes { get; set; } = new();

        /// <summary>过滤之前按 DataType + DataId 汇总，同物料库存和占用不重复累加。</summary>
        public List<ScheduleMaterialSummaryDto> Materials { get; set; } = new();
    }
}
