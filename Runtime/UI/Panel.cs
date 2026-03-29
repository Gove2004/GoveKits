using System.Threading.Tasks;
using UnityEngine;

namespace GoveKits.Runtime.UI
{
    public abstract class BasePanel : MonoBehaviour
    {
        [Tooltip("是否为入口面板，第一个显示的界面")]
        public bool isEntry = false;
        protected UIController Controller;
        public void SetUIController(UIController controller) => Controller = controller;

        public virtual void OnShow(object payload = null) => gameObject.SetActive(true);
        public virtual void OnHide() => gameObject.SetActive(false);
    }
}