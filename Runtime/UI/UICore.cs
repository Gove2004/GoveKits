using System;
using System.Collections.Generic;
using GoveKits.Runtime.Util;
using UnityEngine;

namespace GoveKits.Runtime.UI
{
    /// <summary>
    /// UI 系统门面。统一管理界面注册/显示与 ViewModel。
    ///
    /// 用法：
    /// - 打开/关闭界面：UICore.Show&lt;LoginPanel&gt;() / UICore.Hide&lt;LoginPanel&gt;()
    /// - 界面外访问数据：UICore.GetVM&lt;LoginVM&gt;()（与界面内 ViewPanel&lt;TVM&gt;.VM 是同一实例）
    ///
    /// 界面需先注册（场景挂 UIAutoRegister 自动注册，或手动 Register）。
    /// </summary>
    public static class UICore
    {
        private static readonly Dictionary<Type, ViewPanel> _views = new();
        private static readonly Dictionary<Type, ViewModel> _viewModels = new();

        /// <summary>注册界面实例。同一类型重复注册会覆盖。通常由 UIAutoRegister 自动调用。</summary>
        public static void Register<T>(ViewPanel view) where T : ViewPanel => Register(typeof(T), view);
        public static void Register(Type type, ViewPanel view)
        {
            if (!_views.ContainsKey(type))
            {
                _views[type] = view;
            }
        }

        /// <summary>注销指定类型的界面。通常由 UIAutoRegister 自动调用。</summary>
        public static void Unregister<T>() where T : ViewPanel => Unregister(typeof(T));
        public static void Unregister(Type type)
        {
            if (_views.ContainsKey(type))
            {
                _views.Remove(type);
            }
        }

        /// <summary>
        /// 显示界面。流程：OnReceiveShowParam(param) → 激活（自动绑定 VM + 全量刷新）→ OnShow()。
        /// </summary>
        /// <param name="param">传给界面的参数，可在 OnReceiveShowParam 中接收</param>
        public static void Show<T>(object param = null) where T : ViewPanel => Show(typeof(T), param);
        public static void Show(Type type, object param = null)
        {
            if (_views.TryGetValue(type, out var view))
            {
                view.OnReceiveShowParam(param);
                view.gameObject.SetActive(true);
                view.OnShow();
            }
        }

        /// <summary>
        /// 隐藏界面。流程：OnHide() → 失活（自动解绑 VM）。
        /// </summary>
        public static void Hide<T>() where T : ViewPanel => Hide(typeof(T));
        public static void Hide(Type type)
        {
            if (_views.TryGetValue(type, out var view))
            {
                view.OnHide();
                view.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// 获取指定类型的 ViewModel（首次访问时创建并调用 OnInit，与界面类型一一对应）。
        /// 界面内可直接用 ViewPanel&lt;TVM&gt;.VM，界面外（任意脚本）用本方法访问同一实例。
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

        /// <summary>关闭所有界面注册与 ViewModel，释放资源。通常由 GoveCore.Close 调用。</summary>
        public static void Close()
        {
            _views.Clear();
            _viewModels.Clear();
        }
    }
}
