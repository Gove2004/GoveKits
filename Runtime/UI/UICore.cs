using System;
using System.Collections.Generic;
using GoveKits.Runtime.Util;
using UnityEngine;

namespace GoveKits.Runtime.UI
{
    /// <summary>
    /// UI 系统门面，统一管理界面注册/显示。
    /// 注册与显示以 UIView（界面）为单位，UIPanel/UIWidget 由 UIView 内部管理。
    /// ViewModel 与界面一一对应，由 UIView 自身持有管理，本门面不介入。
    /// </summary>
    public static class UICore
    {
        private static readonly Dictionary<Type, UIView> _views = new();

        /// <summary>
        /// 注册界面实例。界面必须在场景中存在并通过 UIAutoRegister 或其他方式注册。
        /// 同一类型重复注册会覆盖。
        /// </summary>
        public static void Register<T>(UIView view) where T : UIView => Register(typeof(T), view);
        public static void Register(Type type, UIView view)
        {
            if (!_views.ContainsKey(type))
            {
                _views[type] = view;
            }
        }

        /// <summary>
        /// 注销指定类型的界面。
        /// </summary>
        public static void Unregister<T>() where T : UIView => Unregister(typeof(T));
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
        public static void Show<T>(object param = null) where T : UIView => Show(typeof(T), param);
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
        public static void Hide<T>() where T : UIView => Hide(typeof(T));
        public static void Hide(Type type)
        {
            if (_views.TryGetValue(type, out var view))
            {
                view.OnHide();
                view.gameObject.SetActive(false);   // 触发 OnDisable → 泛型界面自动解绑 VM
            }
        }

        /// <summary>
        /// 关闭所有界面注册，释放资源。
        /// </summary>
        public static void Close()
        {
            _views.Clear();
        }
    }
}
