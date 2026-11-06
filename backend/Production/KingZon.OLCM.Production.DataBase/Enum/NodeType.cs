using System.ComponentModel;

namespace KingZon.Mes.Workflow.Enum
{
    //不要轻易修改，除非更换ui插件
    public enum NodeType
    {
        [Description("开始")]
        start = 1,
        [Description("普通")]
        node = 10,
        [Description("复杂")]
        complex = 20,
        [Description("结束")]
        end = 99
    }
}
