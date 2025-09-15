using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;


namespace GoveKits.UI
{
    /// <summary>
    /// 基础UI类，所有UI组件的基类
    /// </summary>
    public abstract class BaseUI : MonoBehaviour
    {
        private Dictionary<string, UIBehaviour> uiElements = new Dictionary<string, UIBehaviour>();

        public virtual void Awake()
        {
            CacheUIElements();
        }

        /// <summary>
        /// 缓存UI元素，方便后续访问
        /// </summary>
        private void CacheUIElements()
        {
            uiElements.Clear();
            UIBehaviour[] elements = GetComponentsInChildren<UIBehaviour>(true);
            foreach (var element in elements)
            {
                if (!uiElements.ContainsKey(element.name))
                {
                    uiElements[element.name] = element;
                }
                else
                {
                    Debug.LogWarning($"[BaseUI] Duplicate UI element name: {element.name} in {gameObject.name}");
                }
            }
        }

        /// <summary>
        /// 获取UI元素
        /// </summary>
        public T GetElement<T>(string name) where T : UIBehaviour
        {
            if (uiElements.TryGetValue(name, out UIBehaviour element))
            {
                return element as T;
            }
            Debug.LogWarning($"[BaseUI] UI element not found: {name} in {gameObject.name}");
            return null;
        }

        /// <summary>
        /// 注册UI事件
        /// </summary>
        private void RegisterUIEvents()
        {
            foreach (var kvp in uiElements)
            {
                string name = kvp.Key;
                UIBehaviour element = kvp.Value;

                if (element is Button button)
                {
                    button.onClick.AddListener(() => OnButtonClick(name));
                }
                else if (element is Toggle toggle)
                {
                    toggle.onValueChanged.AddListener((isOn) => OnToggleChanged(name, isOn));
                }
                else if (element is Slider slider)
                {
                    slider.onValueChanged.AddListener((value) => OnSliderChanged(name, value));
                }
                else if (element is InputField inputField)
                {
                    inputField.onValueChanged.AddListener((text) => OnInputFieldChanged(name, text));
                }
                // 可以继续添加其他UI组件的事件注册
            }
        }

        // UI事件回调接口
        public abstract void OnButtonClick(string elementName);
        public abstract void OnToggleChanged(string elementName, bool isOn);
        public abstract void OnSliderChanged(string elementName, float value);
        public abstract void OnInputFieldChanged(string elementName, string text);

        // 可以添加一些通用的UI功能，比如显示/隐藏动画等
        public abstract void Show();
        public abstract void Hide();

         // 实现本地化逻辑
        public abstract void Localize();
    }
}