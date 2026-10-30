using KingZon.OLCM.Base.Shared.DataBase.Enum;

namespace KingZon.OLCM.Production.WebApi.Dto
{
    public class CreateProductionScheduleDto
    {
        public int? DepartmentId { get; set; }
        public long CraftVersionId { get; set; }

        public decimal Quantity { get; set; }
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
        public bool ImmediateStart { get; set; }
        public bool Urgent { get; set; }
        public bool CirculationCard { get; set; }
        public string Remark { get; set; } = string.Empty;
        public List<ScheduleProcessDto> Processs { get; set; } = new();
    }

    public class ScheduleProcessDto
    {
        public string Code { get; set; } = string.Empty;
        public long NodeSettingTemplateId { get; set; }
        public int Order { get; set; }
        public string Name { get; set; } = string.Empty;
        public long CraftId { get; set; }
        public bool IsOutsource { get; set; }
        public bool IsInbound { get; set; }
        public bool IsReportWork { get; set; }
        public List<ProcessMaterialDto> Materials { get; set; } = new();
        public List<ProcessMachineRlDto> MachineRls { get; set; } = new();
    }

    public class ProcessMaterialDto
    {
        public int Order { get; set; }
        public long DataId { get; set; }
        public InventoryDataType DataType { get; set; }
        public decimal Quantity { get; set; }
        public long UnitId { get; set; }
        public long? UnitConversionId { get; set; }
        public required string ConvUnitInfo { get; set; }
    }

    public class ProcessMachineRlDto
    {
        public long MachineId { get; set; }
        public long UserId { get; set; }
        public decimal Quantity { get; set; }
        public decimal Hours { get; set; }
    }
}
