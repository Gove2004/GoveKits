using System;
using System.Collections.Generic;
using GoveKits.Runtime.Core;
using UnityEngine;

namespace GoveKits.Runtime.UI
{
    /// <summary>
    /// UI 控制器 - 核心管理类
    /// 负责管理所有 UI 面板的生命周期、导航栈以及 ViewModel 的单例共享
    /// </summary>
    public class UIController : MonoBehaviour
    {
        [Header("UI 配置")]
        /// <summary>
        /// 在 Inspector 中配置的所有面板模板数组
        /// 支持 Prefab 和场景节点两种形式
        /// </summary>
        [SerializeField] private ViewPanel[] panelsArray;

        /// <summary>
        /// 面板实例缓存 - 按类型索引
        /// 用于快速查找和复用已初始化的面板
        /// </summary>
        private readonly Dictionary<Type, ViewPanel> _panels = new();
        
        /// <summary>
        /// 面板导航栈 - 管理全屏界面的切换历史
        /// 栈底为入口界面，栈顶为当前显示界面
        /// </summary>
        private readonly Stack<ViewPanel> _panelStack = new();

        private void Awake() => Init();

        /// <summary>
        /// 初始化所有配置的面板
        /// 1. 遍历面板数组，区分 Prefab 和场景节点
        /// 2. 实例化或复用面板，设置控制器引用
        /// 3. 标记入口面板自动显示
        /// </summary>
        protected void Init()
        {
            foreach (var template in panelsArray)
            {
                if (template == null) continue;
                
                Type type = template.GetType();
                
                // 判断：场景 rootCount 为 0 说明是 Prefab，否则是场景节点
                // Prefab 需要实例化，场景节点直接使用
                ViewPanel instance = template.gameObject.scene.rootCount == 0 
                    ? Instantiate(template, transform) 
                    : template;

                instance.SetUIController(this);
                instance.gameObject.SetActive(false);
                _panels[type] = instance;

                // 入口面板自动显示
                if (instance.isEntry) Show(type);
            }
        }

        #region 导航接口
        
        /// <summary>
        /// 泛型版本 - 显示指定类型的面板
        /// </summary>
        /// <typeparam name="T">面板类型</typeparam>
        /// <param name="payload">传递的数据参数</param>
        public void Show<T>(object payload = null) where T : ViewPanel => Show(typeof(T), payload);

        /// <summary>
        /// 显示指定类型的面板
        /// 处理全屏界面切换逻辑和弹窗显示
        /// </summary>
        /// <param name="type">面板类型</param>
        /// <param name="payload">传递的数据参数</param>
        public void Show(Type type, object payload = null)
        {
            if (!_panels.TryGetValue(type, out var nextPanel))
            {
                CoreLocator.Log.Error(nameof(UIController), $"未注册 Panel: {type.Name}");
                return;
            }

            // 处理全屏界面切换（非弹窗）
            if (!nextPanel.isPopup)
            {
                // 隐藏当前栈顶界面（如果存在且不是同一个）
                if (_panelStack.Count > 0)
                {
                    var current = _panelStack.Peek();
                    if (current != nextPanel) current.OnHide();
                }

                // 将新界面压入栈（避免重复）
                if (!_panelStack.Contains(nextPanel))
                    _panelStack.Push(nextPanel);
            }

            // 设置层级为最上层并显示
            nextPanel.transform.SetAsLastSibling();
            nextPanel.OnShow(payload);
        }

        /// <summary>
        /// 返回上一级界面
        /// 弹出当前界面，恢复栈顶界面
        /// </summary>
        public void Back()
        {
            // 至少保留一个界面（入口界面）
            if (_panelStack.Count <= 1) return;

            var current = _panelStack.Pop();
            current.OnHide();

            var previous = _panelStack.Peek();
            previous.OnShow();
        }

        /// <summary>
        /// 隐藏指定类型的弹窗
        /// 弹窗不进入导航栈，独立管理
        /// </summary>
        /// <typeparam name="T">弹窗面板类型</typeparam>
        public void HidePopup<T>() where T : ViewPanel
        {
            if (_panels.TryGetValue(typeof(T), out var panel)) panel.OnHide();
        }

        #endregion
    }
}