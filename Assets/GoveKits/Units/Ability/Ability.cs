using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;

namespace GoveKits.Units
{
    // 能力上下文
    public class AbilityContext
    {
        /// <summary>效果来源单位</summary>
        public Unit Source { get; }

        /// <summary>效果目标单位</summary>
        public Unit Target { get; }

        /// <summary>存储效果执行过程中需要的所有数据</summary>
        private Dictionary<string, object> data;

        /// <summary>
        /// 创建效果上下文
        /// </summary>
        /// <param name="source">效果来源对象（如施法者）</param>
        /// <param name="target">效果目标对象（如受击者）</param>
        public AbilityContext(Unit source,Unit target, Dictionary<string, object> parameters = null)
        {
            Source = source;
            Target = target;
            data = parameters ?? new Dictionary<string, object>();
        }

        /// <summary>
        /// 安全获取上下文数据，支持默认值
        /// </summary>
        /// <typeparam name="T">期望的数据类型</typeparam>
        /// <param name="key">数据键名</param>
        /// <param name="defaultValue">找不到数据时的默认值</param>
        /// <returns>类型转换后的数据或默认值</returns>
        public T Get<T>(string key, T defaultValue = default) =>
            data.TryGetValue(key, out var value) && value is T typedValue ? typedValue : defaultValue;

        /// <summary>
        /// 设置或更新上下文数据
        /// </summary>
        /// <typeparam name="T">数据类型</typeparam>
        /// <param name="key">数据键名</param>
        /// <param name="value">要存储的值</param>
        public void Set<T>(string key, T value) => data[key] = value;



    }


    /// <summary>
    /// 能力接口
    /// </summary>
    public interface IAbility
    {
        // 执行能力
        public bool Execute(AbilityContext context);
        // 检查能力施放条件
        public bool Condition(AbilityContext context);
        // 消耗能力资源
        public void Cost(AbilityContext context);
        // 取消能力效果
        public void Cancel(AbilityContext context);
    }







}