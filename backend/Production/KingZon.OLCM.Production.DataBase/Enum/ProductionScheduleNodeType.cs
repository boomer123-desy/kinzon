namespace KingZon.OLCM.Production.Shared.DataBase.Enum
{
    /// <summary>排产业务节点类型，对应 WF_NodeType.Id，不是工作流结构枚举 NodeType。</summary>
    public enum ProductionScheduleNodeType
    {
        生产 = 1,
        外协 = 2,
        入库 = 3,
    }
}
