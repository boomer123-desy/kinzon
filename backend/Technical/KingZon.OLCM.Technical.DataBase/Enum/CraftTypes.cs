using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KingZon.OLCM.Technical.Shared.DataBase.Enum
{
    public enum CraftOrderVersionStatus
    {
        暂存 = 0,
        待审核 = 1,
        已发布 = 2,
        已废弃 = 3
    }

    public enum NodeIOType
    {
        输入 = 1,
        输出 = 2,
        替料 = 3
        
    }
    /// <summary>
    /// 标准工序单版本使用的工作流类型。
    /// 数值与工作流节点类型基础数据中的工序单流程类型保持一致。
    /// </summary>
    public enum CraftOrderWorkflowType
    {
        工序单版本 = 1,
    }

    /// <summary>
    /// 工序单工作流的节点类型。
    /// 对应节点类型基础数据：流程开始=1、节点=10、流程结束=99。
    /// </summary>
    public enum CraftOrderNodeType
    {
        流程开始 = 1,
        节点 = 2,
        流程结束 = 3,
    }
}
