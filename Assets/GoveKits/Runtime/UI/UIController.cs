using System;
using System.Collections.Generic;
using UnityEngine;

namespace GoveKits.UI
{
    public class UIController : MonoBehaviour
    {
        [Tooltip("注册的所有UI面板, 请在Inspector中配置, 确保唯一性, 启动时会自动初始化注册")]
        [SerializeField] private PanelUI[] uiPanelsArray;
        
        private Dictionary<Type, PanelUI> uiPanels = new Dictionary<Type, PanelUI>();
        private Stack<PanelUI> panelStack = new Stack<PanelUI>();

        private void Awake()
        {
            InitPanels();
        }

        private void InitPanels()
        {
            foreach (var panel in uiPanelsArray)
            {
                Type type = panel.GetType();
                if (!uiPanels.ContainsKey(type))
                {
                    uiPanels.Add(type, panel);
                    panel.SetUIController(this);
                    
                    // 默认全部隐藏，不触发任何生命周期
                    panel.gameObject.SetActive(false); 
                }
            }

            // 处理入口界面
            foreach (var panel in uiPanelsArray)
            {
                if (panel.isEntry)
                {
                    ShowInternal(panel, null);
                    break; 
                }
            }
        }

        public T GetPanel<T>() where T : PanelUI
        {
            if (uiPanels.TryGetValue(typeof(T), out PanelUI panel))
                return panel as T;
            return null;
        }


        /// <summary>
        /// 打开界面
        /// </summary>
        public void Show<T>(object parameters = null) where T : PanelUI
        {
            var nextPanel = GetPanel<T>();
            if (nextPanel == null)
            {
                Debug.LogError($"UI Panel not found: {typeof(T).Name}");
                return;
            }
            
            ShowInternal(nextPanel, parameters);
        }

        /// <summary>
        /// 内部显示方法，统一处理生命周期
        /// </summary>
        private void ShowInternal(PanelUI nextPanel, object parameters)
        {
            // 如果已经在栈顶，不重复打开
            if (panelStack.Count > 0 && panelStack.Peek() == nextPanel) return;

            // 1. 处理当前栈顶界面
            if (panelStack.Count > 0)
            {
                var currentPanel = panelStack.Peek();
                
                // 失去焦点
                currentPanel.InternalOnPause();

                // 如果是全屏界面，需要隐藏底层界面
                if (!nextPanel.isPopup)
                {
                    currentPanel.InternalOnStop();
                }
            }

            // 2. 压栈
            panelStack.Push(nextPanel);

            // 3. 处理新界面
            if (!nextPanel.IsCreated)
            {
                nextPanel.InternalOnCreate();
            }
            
            // 传递参数并启动
            nextPanel.InternalOnStart(parameters);
            nextPanel.InternalOnResume();
        }

        /// <summary>
        /// 关闭当前界面
        /// </summary>
        public void Hide()
        {
            if (panelStack.Count == 0) return;

            // 1. 移除当前界面
            var closingPanel = panelStack.Pop();

            closingPanel.InternalOnPause();
            closingPanel.InternalOnStop();

            // 2. 恢复上一个界面
            if (panelStack.Count > 0)
            {
                var resumingPanel = panelStack.Peek();

                // 如果上一个界面被Stop了，需要重新Start
                if (!resumingPanel.gameObject.activeSelf)
                {
                    resumingPanel.InternalOnStart(null); // 恢复时不传递新参数
                }

                resumingPanel.InternalOnResume();
            }
        }

        /// <summary>
        /// 获取当前栈顶界面
        /// </summary>
        public PanelUI GetCurrentPanel()
        {
            return panelStack.Count > 0 ? panelStack.Peek() : null;
        }

        /// <summary>
        /// 清空所有界面（返回主界面等场景使用）
        /// </summary>
        public void ClearStack()
        {
            while (panelStack.Count > 0)
            {
                var panel = panelStack.Pop();
                panel.InternalOnPause();
                panel.InternalOnStop();
            }
        }
    }
}