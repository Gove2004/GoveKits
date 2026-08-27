using System;
using System.Collections.Generic;
using GoveKits.Runtime.Util;
using UnityEngine;

namespace GoveKits.Runtime.UI
{
    /// <summary>
    /// UI 系统门面，统一管理界面注册/显示和 ViewModel 生命周期。
    /// 注册与显示以 View（界面）为单位，UIPanel/UIWidget 由 View 内部管理。
    /// </summary>
    public static class UICore
    {
        private static readonly Dictionary<Type, View> _views = new();
        private static readonly Dictionary<Type, ViewModel> _viewModels = new();

        /// <summary>
        /// 注册界面实例。界面必须在场景中存在并通过 AutoUIRegister 或其他方式注册。
        /// 同一类型重复注册会覆盖。
        /// </summary>
        public static void Register<T>(View view) where T : View => Register(typeof(T), view);
        public static void Register(Type type, View view)
        {
            if (!_views.ContainsKey(type))
            {
                _views[type] = view;
            }
        }

        /// <summary>
        /// 注销指定类型的界面。
        /// </summary>
        public static void Unregister<T>() where T : View => Unregister(typeof(T));
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
        public static void Show<T>(object param = null) where T : View => Show(typeof(T), param);
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
        public static void Hide<T>() where T : View => Hide(typeof(T));
        public static void Hide(Type type)
        {
            if (_views.TryGetValue(type, out var view))
            {
                view.OnHide();
                view.gameObject.SetActive(false);   // 触发 OnDisable → 泛型界面自动解绑 VM
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
        /// 关闭所有界面和 ViewModel，释放资源。
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
