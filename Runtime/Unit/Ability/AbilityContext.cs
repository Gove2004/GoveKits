using System.Collections.Generic;


namespace GoveKits.Runtime.Unit
{
    /// <summary>
    /// Unit 执行上下文。
    /// <para>提供 Source/Target 以及可扩展的运行时参数容器。</para>
    /// </summary>
    public class AbilityContext
    {
        /// <summary>
        /// 触发方（施法者、攻击者等）。
        /// </summary>
        public readonly IUnit Source;

        /// <summary>
        /// 目标方。
        /// </summary>
        public readonly IUnit Target;

        // 浮点数数据字典，用于存储各种浮点数值参数
        private readonly Dictionary<string, float> _floatData = new();

        /// <summary>
        /// 获取指定键的浮点数值，如果不存在则返回默认值。
        /// </summary>
        /// <param name="key">参数键。</param>
        /// <param name="defaultValue">不存在时的默认值。</param>
        /// <returns>找到的值或默认值。</returns>
        public float GetFloat(string key, float defaultValue = 0f)
        {
            return _floatData.TryGetValue(key, out var value) ? value : defaultValue;
        }

        /// <summary>
        /// 设置指定键的浮点数值。
        /// </summary>
        /// <param name="key">参数键。</param>
        /// <param name="value">参数值。</param>
        public void SetFloat(string key, float value)
        {
            _floatData[key] = value;
        }

        /// <summary>
        /// 扩展参数表，用于承载技能计算中的临时上下文。
        /// </summary>
        private readonly Dictionary<string, object> _data = new();

        /// <summary>
        /// 创建执行上下文实例。
        /// </summary>
        /// <param name="source">触发方单位。</param>
        /// <param name="target">目标单位，可为空。</param>
        public AbilityContext(IUnit source, IUnit target = null)
        {
            Source = source;
            Target = target;
        }

        /// <summary>
        /// 写入或覆盖一个上下文参数。
        /// </summary>
        /// <param name="key">参数键。</param>
        /// <param name="value">参数值。</param>
        /// <returns>当前上下文实例，便于链式调用。</returns>
        public AbilityContext SetData<T>(string key, T value)
        {
            _data[key] = value;
            return this;
        }

        /// <summary>
        /// 尝试读取一个强类型上下文参数。
        /// </summary>
        /// <typeparam name="T">目标类型。</typeparam>
        /// <param name="key">参数键。</param>
        /// <param name="value">读取到的值。</param>
        /// <returns>存在且类型匹配返回 true，否则返回 false。</returns>
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

        /// <summary>
        /// 读取一个强类型上下文参数，读取失败时返回默认值。
        /// </summary>
        /// <typeparam name="T">目标类型。</typeparam>
        /// <param name="key">参数键。</param>
        /// <param name="defaultValue">读取失败时的默认值。</param>
        /// <returns>找到的值或默认值。</returns>
        public T GetData<T>(string key, T defaultValue = default)
        {
            return TryGetData<T>(key, out var value) ? value : defaultValue;
        }

        /// <summary>
        /// 删除指定上下文参数。
        /// </summary>
        /// <param name="key">参数键。</param>
        /// <returns>删除成功返回 true，否则返回 false。</returns>
        public bool RemoveData(string key)
        {
            return _data.Remove(key);
        }

        /// <summary>
        /// 清空全部扩展参数。
        /// </summary>
        public void ClearData()
        {
            _data.Clear();
            _floatData.Clear();
        }
    }
}