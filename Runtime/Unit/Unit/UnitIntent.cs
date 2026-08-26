using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using GoveKits.Runtime.Util;

namespace GoveKits.Runtime.Unit
{
    /// <summary>
    /// Intent 基类。表示对 Unit 施加的一个尚未落地的意图。
    /// Intent 进入 Unit 后由 Reaction 链处理，最终产出 Effect 落地到 Unit 状态。
    /// 工厂方法统一在 UnitCore 中（CreateIntent / CreateIntentRaw）。
    /// </summary>
    public abstract class UnitIntent : IPoolable
    {
        /// <summary>Intent 类型标签</summary>
        public abstract UnitTag Type { get; }

        /// <summary>发起此 Intent 的来源单位（可为 null）</summary>
        public IUnit Source { get; set; }

        /// <summary>此 Intent 的目标单位（由 ApplyIntent 设置）</summary>
        public IUnit Target { get; set; }

        /// <summary>清除扩展参数（回收时用）</summary>
        public virtual void OnRecycle()
        {
            Source = null;
            Target = null;
        }
    }
}
