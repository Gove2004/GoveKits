using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GoveKits.Runtime.Core;
using UnityEngine;

namespace GoveKits.Runtime.UI
{
    public class UIController : MonoBehaviour
    {
        [Tooltip("将此控制器管理的所有 UI 面板 Prefab 或场景中的实例拖到这里")]
        [SerializeField] private BasePanel[] PanelsArray;

         // 运行时存储面板实例的字典，用于快速查找
        private Dictionary<Type, BasePanel> Panels = new Dictionary<Type, BasePanel>();
        
        // 核心的UI导航栈
        private Stack<BasePanel> panelStack = new Stack<BasePanel>();

        private void Awake() => InitPanels();

        /// <summary>
        /// 初始化控制器，注册所有面板。
        /// </summary>
        private void InitPanels()
        {
            foreach (var panel in PanelsArray)
            {
                Type type = panel.GetType();
                if (!Panels.ContainsKey(type))
                {
                    Panels.Add(type, panel);
                    panel.SetUIController(this);
                    panel.gameObject.SetActive(false); // 默认隐藏所有面板
                }

                if (panel.isEntry)
                {
                    Show(type); // 显示入口面板，假设只有一个入口面板
                }
            }
        }

        /// <summary>
        /// 获取已注册的面板实例。
        /// </summary>
        public TPanel GetPanel<TPanel>() where TPanel : BasePanel => (TPanel)GetPanel(typeof(TPanel));
        public BasePanel GetPanel(Type panelType)
        {
            if (Panels.TryGetValue(panelType, out BasePanel panel)) return panel;
            LogCore.LogWarning("UIController", $"未找到类型为 {panelType.Name} 的面板，请检查是否已在 Inspector 中配置。");
            return null;
        }

        #region 异步 Show / Hide

        /// <summary>
        /// 异步打开一个面板，并等待其过渡动画完成。
        /// </summary>
        /// <typeparam name="T">要打开的面板类型</typeparam>
        /// <param name="payload">要传递给面板的数据</param>
        public void Show<T>(object payload = null) where T : BasePanel => Show(typeof(T), payload);
        public void Show(Type panelType, object payload = null)
        {
            var nextPanel = GetPanel(panelType);

            // 防止重复打开同一个面板
            if (panelStack.Count > 0 && panelStack.Peek() == nextPanel)
                return;

            // 如果栈中有面板，则需要先处理当前面板的“退场”
            if (panelStack.Count > 0)
            {
                BasePanel currentPanel = panelStack.Peek();
                currentPanel.OnHide();
            }

            // 将新面板压入栈
            panelStack.Push(nextPanel);
            nextPanel.OnShow(payload);
        }
        
        /// <summary>
        /// 异步关闭当前最上层的面板，并等待过渡动画完成。
        /// </summary>
        public void Hide()
        {
            if (panelStack.Count == 0) return;

            BasePanel closingPanel = panelStack.Pop();

            // 同时播放“即将关闭”面板的退场动画
            closingPanel.OnHide();

            // 如果栈中还有其他面板，则需要恢复下一层的面板
            if (panelStack.Count > 0)
            {
                BasePanel resumingPanel = panelStack.Peek();
                resumingPanel.OnShow();
            }
        }
        
        public void FinishAll()
        {
            while (panelStack.Count > 0)
            {
                BasePanel closingPanel = panelStack.Pop();

                // 播放关闭动画
                closingPanel.OnHide();
            }
        }

        #endregion
    }
}