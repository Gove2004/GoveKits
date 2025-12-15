using System;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks; // 引入 UniTask 命名空间

namespace GoveKits.UI
{
    /// <summary>
    /// UI 控制器，负责管理一组 UI 面板的生命周期和导航。
    /// 采用栈式管理，支持异步过渡动画。
    /// </summary>
    public class UIController : MonoBehaviour
    {
        [Tooltip("将此控制器管理的所有 UI 面板 Prefab 或场景中的实例拖到这里")]
        [SerializeField] private BasePanel[] uiPanelsArray;
        
        // 运行时存储面板实例的字典，用于快速查找
        private Dictionary<Type, BasePanel> uiPanels = new Dictionary<Type, BasePanel>();
        
        // 核心的UI导航栈
        private Stack<BasePanel> panelStack = new Stack<BasePanel>();


        private void Awake() => InitPanels();

        /// <summary>
        /// 初始化控制器，注册所有面板。
        /// </summary>
        private void InitPanels()
        {
            foreach (var panel in uiPanelsArray)
            {
                Type type = panel.GetType();
                if (!uiPanels.ContainsKey(type))
                {
                    uiPanels.Add(type, panel);
                    panel.SetUIController(this);
                    panel.gameObject.SetActive(false);
                }
            }
            // 自动启动被标记为入口的面板
            foreach (var panel in uiPanelsArray)
            {
                if (panel.isEntry)
                {
                    // 使用 .Forget() 来“发射后不管”，因为启动时我们不需要等待动画完成
                    Show(panel, null);
                    break;
                }
            }
        }

        /// <summary>
        /// 获取已注册的面板实例。
        /// </summary>
        public T GetPanel<T>() where T : BasePanel
        {
            if (uiPanels.TryGetValue(typeof(T), out BasePanel panel)) return panel as T;
            LogManager.LogWarning("UIController", $"未找到类型为 {typeof(T).Name} 的面板，请检查是否已在 Inspector 中配置。");
            return null;
        }

        #region 异步 Show / Hide

        /// <summary>
        /// 异步打开一个面板，并等待其过渡动画完成。
        /// </summary>
        /// <typeparam name="T">要打开的面板类型</typeparam>
        /// <param name="payload">要传递给面板的数据</param>
        public void Show<T>(object payload = null) where T : BasePanel
        {
            var nextPanel = GetPanel<T>();
            if (nextPanel != null)
                Show(nextPanel, payload);
        }

        private void Show(BasePanel nextPanel, object payload)
        {
            // 防止重复打开同一个面板
            if (panelStack.Count > 0 && panelStack.Peek() == nextPanel) return;

            // 如果栈中有面板，则需要先处理当前面板的“退场”
            if (panelStack.Count > 0)
            {
                BasePanel currentPanel = panelStack.Peek();
                IPanelLifeCycle currentLifeCycle = currentPanel;

                // 总是先暂停当前面板
                currentLifeCycle.OnPause();

                // 如果新面板不是弹窗，则需要彻底隐藏当前面板
                if (!nextPanel.isPopup)
                {
                    currentLifeCycle.OnStop();
                }
            }

            // 将新面板压入栈
            panelStack.Push(nextPanel);
            IPanelLifeCycle nextLifeCycle = nextPanel;

            // 按正确的生命周期顺序调用
            if (!nextPanel.IsCreated) nextLifeCycle.OnCreate();
            nextLifeCycle.OnStart(payload); // OnStart 负责 SetActive(true) 和数据初始化
            nextLifeCycle.OnResume(); // 等待新面板的进入动画播放完毕
        }

        /// <summary>
        /// 异步关闭当前最上层的面板，并等待过渡动画完成。
        /// </summary>
        public void Hide()
        {
            if (panelStack.Count == 0) return;

            BasePanel closingPanel = panelStack.Pop();
            IPanelLifeCycle closingLifeCycle = closingPanel;

            // 同时播放“即将关闭”面板的退场动画
            closingLifeCycle.OnPause();
            closingLifeCycle.OnStop();

            // 如果栈中还有其他面板，则需要恢复下一层的面板
            if (panelStack.Count > 0)
            {
                BasePanel resumingPanel = panelStack.Peek();
                IPanelLifeCycle resumingLifeCycle = resumingPanel;

                // 如果下层面板之前被 OnStop 隐藏了，需要先 OnStart 激活它
                if (!resumingPanel.gameObject.activeSelf)
                {
                    resumingLifeCycle.OnStart(null); // 恢复时通常不带参数
                }
                // 播放“即将恢复”面板的入场动画
                resumingLifeCycle.OnResume();
            }
        }



        public void FinishAll()
        {
            while (panelStack.Count > 0)
            {
                BasePanel closingPanel = panelStack.Pop();
                IPanelLifeCycle closingLifeCycle = closingPanel;

                // 播放关闭动画
                closingLifeCycle.OnPause();
                closingLifeCycle.OnStop();
            }
        }

        #endregion
    }
}