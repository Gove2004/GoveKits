using System.Collections.Generic;

namespace GoveKits.Runtime.Unit
{
    /// <summary>
    /// Unit 技能与效果的执行上下文。
    /// <para>提供 Source(施法者) / Target(受击者) 以及可扩展的运行时参数容器。</para>
    /// </summary>
    public class AbilityContext
    {
        /// <summary>触发方（施法者、攻击者等）</summary>
        public readonly IUnit Source;

        /// <summary>目标方（受击者等，可为 null）</summary>
        public readonly IUnit Target;

        // 无 GC 开销的浮点数参数字典（常用于传递伤害值、倍率等）
        private readonly Dictionary<string, float> _floatData = new();
        
        // 扩展对象参数表（常用于传递特定表现层的特效、复杂结构体）
        private readonly Dictionary<string, object> _data = new();

        public AbilityContext(IUnit source, IUnit target = null)
        {
            Source = source;
            Target = target;
        }

        #region 浮点数参数管理 (0 GC)

        public float GetFloat(string key, float defaultValue = 0f)
        {
            return _floatData.TryGetValue(key, out var value) ? value : defaultValue;
        }

        public AbilityContext SetFloat(string key, float value)
        {
            _floatData[key] = value;
            return this;
        }

        #endregion

        #region 扩展对象参数管理

        public AbilityContext SetData<T>(string key, T value)
        {
            _data[key] = value;
            return this;
        }

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

        public T GetData<T>(string key, T defaultValue = default)
        {
            return TryGetData<T>(key, out var value) ? value : defaultValue;
        }

        public bool RemoveData(string key) => _data.Remove(key);

        #endregion

        public void Clear()
        {
            _data.Clear();
            _floatData.Clear();
        }
    }
}