using System;
using System.Collections.Generic;
using GoveKits.Runtime.Util;
using UnityEngine;

namespace GoveKits.Runtime.UI
{
    /// <summary>
    /// UI 系统门面，统一管理界面注册/显示与 ViewModel。
    /// 注册与显示以 ViewPanel（界面）为单位，UIItem 由 ViewPanel 内部管理。
    /// ViewModel 按类型由本门面持有单例（与界面类型一一对应），外部可通过 GetVM 随时访问。
    /// </summary>
    public static class UICore
    {
        private static readonly Dictionary<Type, ViewPanel> _views = new();
        private static readonly Dictionary<Type, ViewModel> _viewModels = new();

        /// <summary>
        /// 注册界面实例。界面必须在场景中存在并通过 UIAutoRegister 或其他方式注册。
        /// 同一类型重复注册会覆盖。
        /// </summary>
        public static void Register<T>(ViewPanel view) where T : ViewPanel => Register(typeof(T), view);
        public static void Register(Type type, ViewPanel view)
        {
            if (!_views.ContainsKey(type))
            {
                _views[type] = view;
            }
        }

        /// <summary>
        /// 注销指定类型的界面。
        /// </summary>
        public static void Unregister<T>() where T : ViewPanel => Unregister(typeof(T));
        public static void Unregister(Type type)
        {
            if (_views.ContainsKey(type))
            {
                _views.Remove(type);
            }
        }

        /// <summary>
        /// 显示界面。流程：OnReceiveShowParam(param) → 激活(自动绑定 VM + 全量刷新) → OnShow()。
        /// </summary>
        public static void Show<T>(object param = null) where T : ViewPanel => Show(typeof(T), param);
        public static void Show(Type type, object param = null)
        {
            if (_views.TryGetValue(type, out var view))
            {
                view.OnReceiveShowParam(param);
                view.gameObject.SetActive(true);    // 触发 OnEnable → 泛型界面自动绑定 VM 并全量刷新
                view.OnShow();
            }
        }

        /// <summary>
        /// 隐藏界面。流程：OnHide() → 失活(自动解绑 VM)。
        /// </summary>
        public static void Hide<T>() where T : ViewPanel => Hide(typeof(T));
        public static void Hide(Type type)
        {
            if (_views.TryGetValue(type, out var view))
            {
                view.OnHide();
                view.gameObject.SetActive(false);   // 触发 OnDisable → 泛型界面自动解绑 VM
            }
        }

        /// <summary>
        /// 获取指定类型的 ViewModel 单例（首次访问时创建并调用 OnInit）。
        /// 与界面类型一一对应：LoginView 绑定 LoginVM，外部逻辑也可直接访问同一实例。
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
        /// 关闭所有界面注册与 ViewModel，释放资源。
        /// </summary>
        public static void Close()
        {
            foreach (var vm in _viewModels.Values)
            {
                vm.DetachAllViews();
            }
            _views.Clear();
            _viewModels.Clear();
        }
    }
}
