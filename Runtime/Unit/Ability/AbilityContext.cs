using System.Collections.Generic;

namespace GoveKits.Runtime.Unit
{
    /// <summary>
    /// Unit 技能与效果的执行上下文。
    /// 提供 Source（施法者）/ Target（受击者）以及可扩展的运行时参数容器。
    /// </summary>
    public class AbilityContext
    {
        /// <summary>触发方（施法者、攻击者等）</summary>
        public readonly IUnit Source;

        /// <summary>目标方（受击者等，可为 null）</summary>
        public IUnit Target;

        private readonly Dictionary<string, float> _floatData = new();
        private readonly Dictionary<string, object> _data = new();

        /// <summary>
        /// 创建技能执行上下文。
        /// </summary>
        public AbilityContext(IUnit source, IUnit target = null)
        {
            Source = source;
            Target = target;
        }

        #region 浮点数参数管理

        /// <summary>获取浮点型参数，不存在则返回默认值</summary>
        public float GetFloat(string key, float defaultValue = 0f)
            => _floatData.TryGetValue(key, out var value) ? value : defaultValue;

        /// <summary>设置浮点型参数</summary>
        public AbilityContext SetFloat(string key, float value)
        {
            _floatData[key] = value;
            return this;
        }

        #endregion

        #region 扩展对象参数管理

        /// <summary>设置扩展对象参数</summary>
        public AbilityContext SetData<T>(string key, T value)
        {
            _data[key] = value;
            return this;
        }

        /// <summary>尝试获取指定类型的扩展参数</summary>
        public bool TryGetData<T>(string key, out T value)
        {
            if (_data.TryGetValue(key, out var raw) && raw is T typed)
            {
                value = typed;
                return true;
            }
            value = default;
            return false;
        }

        /// <summary>获取扩展参数，不存在则返回默认值</summary>
        public T GetData<T>(string key, T defaultValue = default)
            => TryGetData<T>(key, out var value) ? value : defaultValue;

        /// <summary>移除指定键的扩展参数</summary>
        public bool RemoveData(string key) => _data.Remove(key);

        #endregion

        /// <summary>清空所有运行时参数（回收时用）</summary>
        public void Clear()
        {
            _data.Clear();
            _floatData.Clear();
        }
    }
}
