using System;
using System.Collections.Generic;
using GoveKits.Runtime.Util;
using UnityEngine;

namespace GoveKits.Runtime.UI
{
    /// <summary>
    /// 界面基类（UIView 层）。
    /// 一个界面 = 一个完整的业务界面（如登录主界面），绑定一个专属 UIViewModel，包含多个 UIPanel。
    ///
    /// 层级：UIView（界面，绑 VM）→ UIPanel（面板，挂 UIElements）→ UIWidget（小组件）
    ///
    /// 生命周期（由 UICore.Show/Hide 驱动）：
    ///   Show: OnReceiveShowParam(param) → 激活(自动绑定 VM + 全量刷新) → OnShow()
    ///   Hide: OnHide() → 失活(自动解绑 VM)
    ///
    /// 面板管理：Awake 自动收集子级 UIPanel，通过 ShowPanel / HidePanel / SwitchPanel 切换。
    /// VM 通知：VM.Notify(key) → UIView.OnNotify(key) → 转发给所有已激活的面板。
    /// </summary>
    public abstract class UIView : MonoBehaviour
    {
        /// <summary>子级面板字典 - 按类型索引（Awake 自动收集）。</summary>
        protected readonly Dictionary<Type, UIPanel> _panels = new();

        /// <summary>本界面绑定的 ViewModel（由 UICore 按类型持有，与界面一一对应），未绑定返回 null。</summary>
        public abstract UIViewModel GetVM();

        protected virtual void Awake()
        {
            foreach (var panel in GetComponentsInChildren<UIPanel>(true))
            {
                _panels.TryAdd(panel.GetType(), panel);
            }
        }

        #region 面板管理

        /// <summary>获取指定类型的面板，未注册返回 null。</summary>
        protected UIPanel GetPanel<T>() where T : UIPanel
            => _panels.TryGetValue(typeof(T), out var p) ? p : null;

        /// <summary>显示面板：OnReceiveShowParam → 激活 → OnShow。</summary>
        protected void ShowPanel<T>(object param = null) where T : UIPanel
        {
            var p = GetPanel<T>();
            if (p == null) return;
            p.OnReceiveShowParam(param);
            p.gameObject.SetActive(true);
            p.OnShow();
        }

        /// <summary>隐藏面板：OnHide → 失活。</summary>
        protected void HidePanel<T>() where T : UIPanel
        {
            var p = GetPanel<T>();
            if (p == null) return;
            p.OnHide();
            p.gameObject.SetActive(false);
        }

        /// <summary>切换面板：先隐藏所有已激活面板，再显示指定面板。</summary>
        protected void SwitchPanel<T>(object param = null) where T : UIPanel
        {
            foreach (var p in _panels.Values)
            {
                if (p.gameObject.activeSelf)
                {
                    p.OnHide();
                    p.gameObject.SetActive(false);
                }
            }
            ShowPanel<T>(param);
        }

        #endregion

        /// <summary>
        /// VM 通知入口，转发给所有已激活的面板。
        /// key 为 null 表示全量刷新（绑定 VM 后自动触发）。
        /// </summary>
        public virtual void OnNotify(string key)
        {
            foreach (var panel in _panels.Values)
            {
                if (panel.gameObject.activeSelf)
                {
                    panel.OnNotify(key);
                }
            }
        }

        /// <summary>界面显示时接收外部参数，默认空实现，子类可重写。</summary>
        public virtual void OnReceiveShowParam(object param) { }

        /// <summary>界面显示完成回调（已激活、VM 已绑定），子类可重写播放动画等。</summary>
        public virtual void OnShow() { }

        /// <summary>界面隐藏回调（失活前），子类可重写播放淡出等。</summary>
        public virtual void OnHide() { }
    }

    /// <summary>
    /// 泛型界面基类，与指定类型的 UIViewModel 一一对应。
    /// VM 实例由 UICore 按类型持有（外部也可通过 UICore.GetVM 访问），
    /// 本界面激活时自动绑定并全量刷新，失活时自动解绑。
    /// </summary>
    /// <typeparam name="TVM">ViewModel 类型</typeparam>
    public abstract class UIView<TVM> : UIView where TVM : UIViewModel, new()
    {
        private TVM _vm;

        /// <summary>本界面绑定的 ViewModel 实例（由 UICore 持有，与界面类型一一对应）。</summary>
        public TVM VM => _vm ??= UICore.GetVM<TVM>();

        public override UIViewModel GetVM() => VM;

        protected virtual void OnEnable()
        {
            VM.AttachView(this);    // 绑定即全量刷新（触发 OnNotify(null)）
        }

        protected virtual void OnDisable()
        {
            VM?.DetachView(this);
        }
    }
}
