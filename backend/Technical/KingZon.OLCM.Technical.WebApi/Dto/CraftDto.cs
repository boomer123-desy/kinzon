using KingZon.Core.DataBase;
using KingZon.OLCM.Base.Service.Dto;
using KingZon.OLCM.Base.Shared.DataBase.Entities;
using KingZon.OLCM.Base.Shared.DataBase.Enum;
using KingZon.OLCM.Base.Shared.DataBase.JsonStructure;
using KingZon.OLCM.Technical.Shared.DataBase.Entities;
using KingZon.OLCM.Technical.Shared.DataBase.Enum;
using System;
using System.Collections.Generic;

namespace KingZon.OLCM.Technical.WebApi.Dto
{
    /// <summary>
    /// 工序单主数据的新增 / 修改入参。
    /// </summary>
    public class SaveCraftOrderDto
    {
        public long Id { get; set; }

        /// <summary>
        /// 空值时由服务端按工序单 Id 生成编码。
        /// </summary>
        public string? Code { get; set; }

        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public long ProductId { get; set; }

        public long? SpecificationId { get; set; }
    }

    /// <summary>已有工艺单仅允许修改名称。</summary>
    public class RenameCraftOrderDto
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    /// <summary>
    /// 工序资料库行。
    /// </summary>
    public class CraftResourceDto
    {
        public long DataId { get; set; }
        public InventoryDataType DataType { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public long UnitId { get; set; }
        public string UnitName { get; set; } = string.Empty;
        public string? TechCode { get; set; }
        public string? Specification { get; set; }
        public string? MaterialGrade { get; set; }
        public string? Version { get; set; }
        public string? Remark { get; set; }
    }

    /// <summary>
    /// 工序单主数据详情。
    /// </summary>
    public class CraftOrderInfoDto
    {
        public DataInfo? MaterialInfo { get; set; }
        public long Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public long ProductId { get; set; }
        public long? SpecificationId { get; set; }
        public string ProductNo { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string? SpecificationName { get; set; }
        public string? CreateUserName { get; set; }
        public DateTime CreateTime { get; set; }
        public string? UpdateUserName { get; set; }
        public DateTime? UpdateTime { get; set; }
    }

    /// <summary>
    /// 工序单版本 DTO（列表）
    /// </summary>
    public class CraftVersionDto
    {
        /// <summary>关联的制造 BOM 版本 Id（只读，后台维护）。</summary>
        public long? BomVersionId { get; set; }
        /// <summary>
        /// 版本 Id
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// 所属工序单 Id
        /// </summary>
        public long CraftOrderId { get; set; }

        /// <summary>
        /// 工序单编码
        /// </summary>
        public string CraftOrderCode { get; set; } = string.Empty;

        /// <summary>
        /// 工序单名称
        /// </summary>
        public string CraftOrderName { get; set; } = string.Empty;

        /// <summary>
        /// 数字版本号（1、2、3…），用于排序
        /// </summary>
        public int SequenceNo { get; set; }

        /// <summary>
        /// 展示版本号（如 V1、V2）
        /// </summary>
        public string Version { get; set; } = string.Empty;

        /// <summary>
        /// 是否为当前主版本
        /// </summary>
        public bool IsMain { get; set; }

        /// <summary>
        /// 版本状态
        /// </summary>
        public CraftOrderVersionStatus Status { get; set; }

        /// <summary>
        /// 版本备注
        /// </summary>
        public string? Remark { get; set; }

        /// <summary>
        /// 该版本下的工序明细数量
        /// </summary>
        public int NodeCount { get; set; }

        /// <summary>
        /// 创建人
        /// </summary>
        public string? CreateUserName { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreateTime { get; set; }
    }

    /// <summary>
    /// 以 craftVersion（Technical_CraftOrderVersion）为根的完整编辑模型：版本头 + 其下全部工序。
    /// </summary>
    public class CraftVersionDetailDto
    {
        /// <summary>关联的制造 BOM 版本 Id（只读，后台维护）。</summary>
        public long? BomVersionId { get; set; }
        /// <summary>
        /// 版本 Id
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// 所属工序单 Id
        /// </summary>
        public long CraftOrderId { get; set; }

        /// <summary>
        /// 工序单编码
        /// </summary>
        public string CraftOrderCode { get; set; } = string.Empty;

        /// <summary>
        /// 工序单名称
        /// </summary>
        public string CraftOrderName { get; set; } = string.Empty;

        /// <summary>
        /// 数字版本号（1、2、3…），用于排序
        /// </summary>
        public int SequenceNo { get; set; }

        /// <summary>
        /// 展示版本号（如 V1、V2）
        /// </summary>
        public string Version { get; set; } = string.Empty;

        /// <summary>
        /// 是否为当前主版本
        /// </summary>
        public bool IsMain { get; set; }

        /// <summary>
        /// 版本状态
        /// </summary>
        public CraftOrderVersionStatus Status { get; set; }

        /// <summary>
        /// 版本备注
        /// </summary>
        public string? Remark { get; set; }

        /// <summary>
        /// 该版本下的工序集合
        /// </summary>
        public List<NodeSettingTemplateDto> NodeSettings { get; set; } = new List<NodeSettingTemplateDto>();
    }

    /// <summary>
    /// 保存 craftVersion 入参（编辑保存统一入口，以版本为根提交其下全部工序）
    /// </summary>
    public class SaveCraftVersionDto
    {
        /// <summary>
        /// 版本 Id
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// 所属工序单 Id（用于校验归属）
        /// </summary>
        public long CraftOrderId { get; set; }

        /// <summary>
        /// 版本备注
        /// </summary>
        public string? Remark { get; set; }

        /// <summary>
        /// 是否设为主版本：true 时会把该版本设为主版本（自动清除同工序单其它主版本标记）
        /// </summary>
        public bool IsMain { get; set; }

        /// <summary>
        /// 该版本下的工序集合
        /// </summary>
        public List<NodeSettingTemplateDto> NodeSettings { get; set; } = new List<NodeSettingTemplateDto>();
    }

    /// <summary>
    /// 工序（对应实体 Technical_NodeSettingTemplate），新增 / 修改 / 加载共用。
    /// Id == 0 表示新增；IsDeleted == true 表示软删。
    /// </summary>
    public class NodeSettingTemplateDto
    {
        /// <summary>
        /// 工序 Id（0 表示新增）
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// 工序顺序，按 10、20、30 递增
        /// </summary>
        public int Sequence { get; set; }

        /// <summary>
        /// 工序编码
        /// </summary>
        public string Code { get; set; } = string.Empty;

        /// <summary>
        /// 工序名称
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 是否外协
        /// </summary>
        public bool IsOutsource { get; set; }

        /// <summary>
        /// 是否入库
        /// </summary>
        public bool IsInbound { get; set; }

        /// <summary>理论加工工时，单位：小时。</summary>
        public decimal TheoreticalWorkHours { get; set; }

        /// <summary>是否报工。</summary>
        public bool IsReportWork { get; set; }


        /// <summary>
        /// 工序说明
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// 工序模板 Id
        /// </summary>
        public long NodeTemplateId { get; set; }

        /// <summary>
        /// 工艺 Id
        /// </summary>
        public long? CraftId { get; set; }

        public string? CraftName { get; set; } = string.Empty;

        /// <summary>
        /// 设备 Id 集合（对应 Technical_NodeSettingTemplate_Machine_RL，保存时整体替换）
        /// </summary>
        public List<long> MachineIds { get; set; } = new List<long>();

        public List<NodeMachineSettingDto> MachineSettings { get; set; } = new();

        public List<NodeMaterialRLDto> Materials { get; set; } = new();

        /// <summary>
        /// 软删除标记（仅保存入参使用）
        /// </summary>
        public bool IsDeleted { get; set; }
    }

    public class NodeMachineSettingDto
    {
        public long MachineId { get; set; }
        public decimal TheoreticalWorkHours { get; set; }
    }

    public class NodeMaterialRLDto
    {
        public long Id { get; set; }

        /// <summary>产品 / 物料 Id（保存必填）。</summary>
        public long DataId { get; set; }
        /// <summary>
        /// 物料类型
        /// </summary>
        public InventoryDataType DataType { get; set; }

        public string MaterialNo { get; set; } = string.Empty;

        public string MaterialName { get; set; } = string.Empty;

        public string MaterialDescription { get; set; } = string.Empty;

        public string MaterialType { get; set; } = string.Empty;

        public long NodeSettingTemplateId { get; set; }

        /// <summary>
        /// 物料类型
        /// </summary>
        public NodeIOType IOType { get; set; }

        /// <summary>
        /// 用量或产出数量
        /// </summary>
        public decimal Quantity { get; set; }

        /// <summary>单位 Id。</summary>
        public long UnitId { get; set; }

        /// <summary>
        /// 单位
        /// </summary>
        public string UnitName { get; set; } = string.Empty ;

        /// <summary>
        /// 多单位转换 ID
        /// </summary>
        public long UnitConversionId { get; set; }

        /// <summary>
        /// 多单位转换Json 对应 InboundUnitDto
        /// </summary>
        public List<InventoryUnitInboundDto> UnitList { get; set; } = new ();

        /// <summary>
        /// 最大损耗率
        /// </summary>
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
        /// 外部半成品引用的工艺版本名称
        /// </summary>
        public string RefCraftOrderVersionName { get; set; } = string.Empty;

        /// <summary>
        /// 备注
        /// </summary>
        public string? Remark { get; set; }

        public long? InsteadSourceId { get; set; }

        public List<NodeMaterialRLDto> Substitutes { get; set; } = new List<NodeMaterialRLDto>();
    }

    /// <summary>
    /// 新建 craftVersion 入参
    /// </summary>
    public class CreateCraftVersionDto
    {
        /// <summary>
        /// 所属工序单 Id
        /// </summary>
        public long CraftOrderId { get; set; }

        /// <summary>
        /// 版本备注
        /// </summary>
        public string? Remark { get; set; }
    }

    /// <summary>将所选工艺单版本复制为当前工艺单的新暂存版本。</summary>
    public class CopyCraftVersionDto
    {
        /// <summary>目标工艺单 Id。</summary>
        public long CraftOrderId { get; set; }

        /// <summary>来源工艺单 Id，用于校验版本归属。</summary>
        public long SourceCraftOrderId { get; set; }

        /// <summary>来源工艺单版本 Id。</summary>
        public long SourceVersionId { get; set; }
    }

    /// <summary>
    /// 发布 craftVersion 入参
    /// </summary>
    public class PublishCraftVersionDto
    {
        /// <summary>
        /// 版本 Id
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// 是否把旧主版本置为已废弃（默认 true）
        /// </summary>
        public bool ObsoleteOldMain { get; set; } = true;
    }

    /// <summary>
    /// 工艺单列表查询入参（分页由 KingZonControllerBase.PageModel 承载）
    /// </summary>
    public class GetCraftOrderListDto
    {
        /// <summary>
        /// 工艺单过滤（编码/名称等，来源表 craftOrder）
        /// </summary>
        public List<Filter> CraftOrderFilterList { get; set; } = new List<Filter>();

        /// <summary>
        /// 产品过滤（编码/名称等，来源表 product）
        /// </summary>
        public List<Filter> ProductFilterList { get; set; } = new List<Filter>();
    }

    /// <summary>
    /// 工艺单列表行（含产品信息与版本汇总）
    /// </summary>
    public class CraftOrderListItemDto
    {
        public long Id { get; set; }

        /// <summary>
        /// 工艺单编码
        /// </summary>
        public string Code { get; set; } = string.Empty;

        /// <summary>
        /// 工艺单名称
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 工艺单说明
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// 产品 Id
        /// </summary>
        public long ProductId { get; set; }

        /// <summary>
        /// 规格 Id
        /// </summary>
        public long? SpecificationId { get; set; }

        /// <summary>
        /// 产品编码
        /// </summary>
        public string ProductNo { get; set; } = string.Empty;

        /// <summary>
        /// 产品名称
        /// </summary>
        public string ProductName { get; set; } = string.Empty;

        /// <summary>
        /// 规格名称
        /// </summary>
        public string? SpecificationName { get; set; }

        /// <summary>
        /// 该工艺单下的版本数量（分页后批量回填）
        /// </summary>
        public int VersionCount { get; set; }

        /// <summary>
        /// 当前主版本号，如 V2（分页后批量回填）
        /// </summary>
        public string? MainVersion { get; set; }

        /// <summary>
        /// 创建人
        /// </summary>
        public string? CreateUserName { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreateTime { get; set; }
    }
}
