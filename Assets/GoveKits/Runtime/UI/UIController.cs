using System;
using System.Collections.Generic;
using UnityEngine;


namespace GoveKits.UI
{
    public class UIController : MonoBehaviour
    {
        [SerializeField] private PanelUI[] uiPanelsArray;
        private Dictionary<Type, PanelUI> uiPanels = new Dictionary<Type, PanelUI>();
        private Stack<PanelUI> panelStack = new Stack<PanelUI>();

        private void Awake() => InitPanels();

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
            // 启动入口 (入口一般没参数)
            foreach (var panel in uiPanelsArray)
            {
                if (panel.isEntry) 
                {
                    Show(panel, null);
                    break;
                }
            }
        }

        public T GetPanel<T>() where T : PanelUI
        {
            if (uiPanels.TryGetValue(typeof(T), out PanelUI panel)) return panel as T;
            return null;
        }

        /// <summary>
        /// 打开面板，可传参
        /// </summary>
        /// <param name="payload">参数对象 (可以是 string, int, 或自定义类)</param>
        public void Show<T>(object payload = null) where T : PanelUI
        {
            var nextPanel = GetPanel<T>();
            if (nextPanel != null) Show(nextPanel, payload);
        }

        private void Show(PanelUI nextPanel, object payload)
        {
            // 将 PanelUI 转为接口，这样才能调用隐藏的生命周期方法
            IUILifeCycle nextLifeCycle = nextPanel;
            
            if (panelStack.Count > 0 && panelStack.Peek() == nextPanel) return;

            if (panelStack.Count > 0)
            {
                PanelUI currentPanel = panelStack.Peek();
                IUILifeCycle currentLifeCycle = currentPanel;

                currentLifeCycle.OnPause();

                if (!nextPanel.isPopup)
                {
                    currentLifeCycle.OnStop();
                }
            }

            panelStack.Push(nextPanel);

            if (!nextPanel.IsCreated) nextLifeCycle.OnCreate();
            
            // 将参数传进去
            nextLifeCycle.OnStart(payload); 
            nextLifeCycle.OnResume();
        }


        public void Hide()
        {
            if (panelStack.Count == 0) return;

            PanelUI closingPanel = panelStack.Pop();
            IUILifeCycle closingLifeCycle = closingPanel;

            closingLifeCycle.OnPause();
            closingLifeCycle.OnStop();

            if (panelStack.Count > 0)
            {
                PanelUI resumingPanel = panelStack.Peek();
                IUILifeCycle resumingLifeCycle = resumingPanel;

                if (!resumingPanel.gameObject.activeSelf)
                {
                    // 恢复时通常不需要传参，或者传 null
                    resumingLifeCycle.OnStart(null); 
                }
                resumingLifeCycle.OnResume();
            }
        }
    }
}