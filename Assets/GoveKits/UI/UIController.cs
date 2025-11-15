using System.Collections.Generic;
using UnityEngine;


namespace GoveKits.UI
{
    public class UIController : MonoBehaviour
    {
        [SerializeField] private BaseUI[] uiPanelsArray;
        private Dictionary<string, BaseUI> uiPanels = new Dictionary<string, BaseUI>();
        private Stack<string> panelStack = new Stack<string>();


        public void Awake()
        {
            foreach (var panel in uiPanelsArray)
            {
                uiPanels[panel.gameObject.name] = panel;
                panel.SetUIController(this);
                if (panel.isEntry)
                {
                    panel.Show();
                }
                else
                {
                    panel.Hide();
                }
            }
        }

        /// <summary>
        /// 显示指定的UI面板
        /// </summary>
        /// <param name="panelName"></param>
        public void ShowUI(string panelName)
        {
            uiPanels.TryGetValue(panelName, out BaseUI panel);
            panel?.Show();
        }

        /// <summary>
        /// 隐藏指定的UI面板
        /// </summary>
        /// <param name="panelName"></param>
        public void HideUI(string panelName)
        {
            uiPanels.TryGetValue(panelName, out BaseUI panel);
            panel?.Hide();
        }

        /// <summary>
        /// 入栈显示UI面板，隐藏当前面板
        /// </summary>
        /// <param name="panelName"></param>
        public void PushUI(string panelName)
        {
            if (panelStack.Count > 0)
            {
                var topPanelName = panelStack.Peek();
                HideUI(topPanelName);
            }
            panelStack.Push(panelName);
            ShowUI(panelName);
        }

        /// <summary>
        /// 出栈隐藏当前UI面板，显示上一个面板
        /// </summary>
        public void PopUI()
        {
            if (panelStack.Count == 0) return;

            var topPanelName = panelStack.Pop();
            HideUI(topPanelName);

            if (panelStack.Count > 0)
            {
                var nextPanelName = panelStack.Peek();
                ShowUI(nextPanelName);
            }
        }
    }
}