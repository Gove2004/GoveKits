using System;
using UnityEngine;



namespace GoveKits.UI
{
    public abstract class BaseUI : MonoBehaviour
    {
        public bool isEntry = false;
        protected UIController uiController;
        public void SetUIController(UIController controller) => uiController = controller;

        public virtual void Show()
        {
            this.gameObject.SetActive(true);
        }

        public virtual void Hide()
        {
            this.gameObject.SetActive(false);
        }
    }
}