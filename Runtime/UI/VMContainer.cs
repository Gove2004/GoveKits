using System;
using System.Collections.Generic;

namespace GoveKits.Runtime.UI
{
    /// <summary>
    /// ViewModel 容器
    /// 采用 Spring 风格的单例模式，
    /// 确保每个 ViewModel 类型在整个应用生命周期中唯一
    /// </summary>
    public static class VMContainer
    {
        /// <summary>
        /// 全局共享的 ViewModel 容器 (Spring 风格单例)
        /// 按类型存储，确保同一 ViewModel 在整个应用生命周期中唯一
        /// </summary>
        private static readonly Dictionary<Type, ViewModel> ViewModels = new();

        /// <summary>
        /// 获取或创建指定类型的 ViewModel 单例
        /// 采用懒加载模式，首次访问时创建实例
        /// </summary>
        /// <typeparam name="T">ViewModel 类型</typeparam>
        /// <returns>ViewModel 实例</returns>
        public static T Get<T>() where T : ViewModel, new()
        {
            Type type = typeof(T);
            if (!ViewModels.TryGetValue(type, out var vm))
            {
                vm = new T();
                ViewModels[type] = vm;
            }
            return vm as T;
        }

        /// <summary>
        /// 移除指定类型的 ViewModel 实例（可选）
        /// 用于在特定场景下清理资源或重置状态
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public static void Remove<T>() where T : ViewModel
        {
            Type type = typeof(T);
            if (ViewModels.TryGetValue(type, out var vm))
            {
                vm.OnUninit();
                ViewModels.Remove(type);
            }
        }
    }
}