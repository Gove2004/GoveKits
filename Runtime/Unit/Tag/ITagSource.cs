using System;

namespace GoveKits.Runtime.Unit
{
    /// <summary>
    /// 标签查询源统一接口。
    /// Unit 的 MarkContainer、AttributeContainer、AbilityContainer、ReactionContainer 均实现此接口，
    /// 暴露给技能系统用于前置条件查询。
    /// </summary>
    public interface ITagSource
    {
        /// <summary>检查是否包含指定标签</summary>
        bool HasTag(UnitTag tag);
    }
}
