using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace GoveKits.Runtime.UI
{
    /// <summary>
    /// MVVM 模式中的 ViewModel 基类
    /// 
    /// 核心功能：
    /// 1. 实现 INotifyPropertyChanged 接口，支持数据绑定通知
    /// 2. 提供 OnPropertyChanged 方法触发属性变更事件
    /// 3. 提供 SetProperty 辅助方法简化属性实现
    /// </summary>
    public abstract class ViewModel : INotifyPropertyChanged
    {
        /// <summary>
        /// 属性变更事件
        /// 当属性值改变时触发，通知 UI 更新
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// 触发属性变更通知
        /// 使用 CallerMemberName 特性自动获取调用者属性名
        /// </summary>
        /// <param name="propertyName">变更的属性名（自动填充）</param>
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// 辅助方法：简化属性写法
        /// 自动比较新旧值，仅在值改变时触发通知
        /// </summary>
        /// <typeparam name="T">属性类型</typeparam>
        /// <param name="storage">属性存储字段引用</param>
        /// <param name="value">新值</param>
        /// <param name="propertyName">属性名（自动填充）</param>
        /// <returns>值是否发生改变</returns>
        protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(storage, value)) return false;
            storage = value;
            OnPropertyChanged(propertyName);
            return true;
        }


        /// <summary>
        /// 当 ViewModel 被销毁时触发。
        /// 子类可重写此方法，用于注销全局事件监听。
        /// </summary>
        public virtual void OnUninit() { }
    }
}