using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using GoveKits.Runtime.Util;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GoveKits.Runtime.UI
{
    /// <summary>
    /// UI 系统门面，统一管理面板注册/显示和 ViewModel 生命周期。
    /// </summary>
    public static class UICore
    {
        private static readonly Dictionary<Type, ViewPanel> _viewPanels = new();
        private static readonly Dictionary<Type, ViewModel> _viewModels = new();

        /// <summary>
        /// 注册面板实例。面板必须在场景中存在并通过 AutoUIRegister 或其他方式注册。
        /// 同一类型重复注册会覆盖。
        /// </summary>
        public static void Register<T>(ViewPanel panel) where T : ViewPanel => Register(typeof(T), panel);
        public static void Register(Type type, ViewPanel panel)
        {
            if (!_viewPanels.ContainsKey(type))
            {
                _viewPanels[type] = panel;
            }
        }

        /// <summary>
        /// 注销指定类型的面板。
        /// </summary>
        public static void Unregister<T>() where T : ViewPanel => Unregister(typeof(T));
        public static void Unregister(Type type)
        {
            if (_viewPanels.ContainsKey(type))
            {
                _viewPanels.Remove(type);
            }
        }

        /// <summary>
        /// 显示面板。触发 OnBindVM -> OnReceiveShowParam -> OnShow 生命周期。
        /// </summary>
        public static void Show<T>(object param = null) where T : ViewPanel => Show(typeof(T), param);
        public static void Show(Type type, object param = null)
        {
            if (_viewPanels.TryGetValue(type, out var panel))
            {
                panel.OnBindVM();
                panel.OnReceiveShowParam(param);
                panel.OnShow();
            }
        }

        /// <summary>
        /// 隐藏面板。触发 OnHide -> OnUnbindVM 生命周期。
        /// </summary>
        public static void Hide<T>() where T : ViewPanel => Hide(typeof(T));
        public static void Hide(Type type)
        {
            if (_viewPanels.TryGetValue(type, out var panel))
            {
                panel.OnHide();
                panel.OnUnbindVM();
            }
        }

        /// <summary>
        /// 获取 ViewModel 单例。首次调用时创建并调用 OnInit。
        /// </summary>
        public static TVM GetVM<TVM>() where TVM : ViewModel, new()
        {
            var type = typeof(TVM);
            if (!_viewModels.ContainsKey(type))
            {
                _viewModels[type] = new TVM();
                _viewModels[type].OnInit();
            }
            return _viewModels[type] as TVM;
        }

        /// <summary>
        /// 关闭所有面板和 ViewModel，释放资源。
        /// </summary>
        public static void Close()
        {
            foreach (var vm in _viewModels.Values)
            {
                vm.DetachAllViews();
            }
            _viewPanels.Clear();
            _viewModels.Clear();
        }
    }
}
