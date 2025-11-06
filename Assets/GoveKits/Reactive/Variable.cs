using System;
using System.Collections.Generic;

namespace GoveKits.Reactive
{
    /// <summary>
    /// 可变变量类
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class Variable<T>
    {
        private T _value;

        /// <summary>
        /// 变量值
        /// </summary>
        public T Value
        {
            get => _value;
            set
            {
                if (!EqualityComparer<T>.Default.Equals(_value, value))
                {
                    _value = value;
                    OnValueChanged?.Invoke(_value);
                }
            }
        }

        /// <summary>
        /// 值变化事件
        /// </summary>
        public event Action<T> OnValueChanged;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="initialValue">初始值</param>
        public Variable(T initialValue = default)
        {
            _value = initialValue;
        }
    }
}