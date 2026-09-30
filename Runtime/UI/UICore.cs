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
    /// ViewPanel 销毁时会自动注销自身，无需手动 Unregister。
    /// </summary>
    public static class UICore
    {
        private static readonly Dictionary<Type, ViewPanel> _views = new();
        private static readonly Dictionary<Type, ViewModel> _viewModels = new();

        static UICore()
        {
            ResetForDomainReload();
        }

        // 关闭 Domain Reload 时清理静态注册表，避免跨 Play 会话残留
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetForDomainReload()
        {
            _views.Clear();
            _viewModels.Clear();
        }

        /// <summary>注册界面实例。同一类型重复注册会覆盖旧实例并输出警告。通常由 UIAutoRegister 自动调用。</summary>
        public static void Register<T>(ViewPanel view) where T : ViewPanel => Register(typeof(T), view);
        public static void Register(Type type, ViewPanel view)
        {
            // 键类型校验：键与实例类型不符时 Show<T>/Hide<T> 无法命中，注销路径也会残留
            if (view != null && !type.IsInstanceOfType(view))
            {
                LogCore.Error(nameof(UICore), $"注册失败：{view.GetType().Name} 与注册键 {type.Name} 类型不符。");
                return;
            }

            if (_views.TryGetValue(type, out var old) && old != null && old != view)
            {
                LogCore.Warning(nameof(UICore), $"{type.Name} 重复注册，旧实例 ({old.name}) 将被覆盖为新实例 ({view.name})。");
            }
            _views[type] = view;
        }

        /// <summary>注销指定类型的界面。传入 view 时仅当注册实例与之相同才注销（防止误销毁仍存活的注册）。</summary>
        public static void Unregister<T>() where T : ViewPanel => Unregister(typeof(T));
        public static void Unregister(Type type, ViewPanel view = null)
        {
            if (!_views.TryGetValue(type, out var registered)) return;
            if (view != null && registered != view) return;

            _views.Remove(type);
        }

        /// <summary>
        /// 显示界面。流程：OnReceiveShowParam(param) → 激活（自动绑定 VM + 全量刷新）→ OnShow()。
        /// 面板已处于激活状态时仅刷新参数（OnReceiveShowParam），不重复触发 OnShow。
        /// </summary>
        /// <param name="param">传给界面的参数，可在 OnReceiveShowParam 中接收</param>
        public static void Show<T>(object param = null) where T : ViewPanel => Show(typeof(T), param);
        public static void Show(Type type, object param = null)
        {
            if (!_views.TryGetValue(type, out var view) || view == null)
            {
                LogCore.Warning(nameof(UICore), $"Show 失败：{type.Name} 未注册或已销毁。请检查场景是否挂了 UIAutoRegister 或手动 Register。");
                return;
            }

            view.OnReceiveShowParam(param);

            // 用 activeInHierarchy 判断：activeSelf 在父级失活时仍为 true，
            // 会误判"已激活"而静默跳过显示流程（参数刷新了但面板不可见）
            if (view.gameObject.activeInHierarchy)
            {
                // 已激活面板仅刷新参数，不重复走激活流程
                return;
            }

            view.gameObject.SetActive(true);
            view.OnShow();
        }

        /// <summary>
        /// 隐藏界面。流程：OnHide() → 失活（自动解绑 VM）。
        /// </summary>
        public static void Hide<T>() where T : ViewPanel => Hide(typeof(T));
        public static void Hide(Type type)
        {
            if (!_views.TryGetValue(type, out var view) || view == null)
            {
                LogCore.Warning(nameof(UICore), $"Hide 失败：{type.Name} 未注册或已销毁。");
                return;
            }

            view.OnHide();
            view.gameObject.SetActive(false);
        }

        /// <summary>
        /// 获取指定类型的 ViewModel（首次访问时创建并调用 OnInit，与界面类型一一对应）。
        /// 界面内可直接用 ViewPanel&lt;TVM&gt;.VM，界面外（任意脚本）用本方法访问同一实例。
        /// </summary>
        public static TVM GetVM<TVM>() where TVM : ViewModel, new()
        {
            var type = typeof(TVM);
            if (!_viewModels.TryGetValue(type, out var vm))
            {
                vm = new TVM();
                vm.OnInit();
                _viewModels[type] = vm;
            }
            return (TVM)vm;
        }

        /// <summary>
        /// 关闭所有界面注册与 ViewModel，释放资源。通常由 GoveCore.Close 调用。
        /// 每个 VM 会先收到 OnDispose 回调（可清理事件订阅等资源），界面缓存的 VM 引用同步失效。
        /// </summary>
        public static void Close()
        {
            var views = new List<ViewPanel>(_views.Values);

            // 先失活仍显示的面板：走 OnDisable 正常解绑旧 VM。Close 后 OnEnable 不会重跑，
            // 若只重置缓存，之后访问 VM 会新建永远不绑定视图的实例，Notify 静默丢失
            foreach (var view in views)
            {
                if (view != null && view.gameObject.activeSelf)
                    view.gameObject.SetActive(false);
            }

            // 快照枚举：OnDispose 内可能调用 GetVM 重建 VM（写回字典）或 Register/Unregister，
            // 直接枚举 Values 会在回调中触发 InvalidOperationException
            var vms = new List<ViewModel>(_viewModels.Values);
            foreach (var vm in vms)
            {
                vm?.OnDispose();
            }

            foreach (var view in views)
            {
                if (view != null) view.ResetVMCache();
            }

            _views.Clear();
            _viewModels.Clear();
        }
    }
}
