using System.Collections.Generic;
using UnityEngine;


namespace GoveKits.UI
{
    public class UIController : MonoBehaviour
    {
        [SerializeField] private BaseUI[] uiPanelsArray;
        private Dictionary<string, BaseUI> uiPanels = new Dictionary<string, BaseUI>();

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


        public void ShowUI(string panelName)
        {
            uiPanels.TryGetValue(panelName, out BaseUI panel);
            panel?.Show();
        }


        public void HideUI(string panelName)
        {
            uiPanels.TryGetValue(panelName, out BaseUI panel);
            panel?.Hide();
        }
    }
}