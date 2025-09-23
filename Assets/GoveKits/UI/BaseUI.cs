using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;


namespace GoveKits.UI
{
    /// <summary>
    /// 基础UI类，所有UI组件的基类
    /// </summary>
    public abstract class BaseUI : MonoBehaviour
    {
        // 二级字典缓存UI元素，方便按类型和名称访问
        private Dictionary<Type, Dictionary<string, UIBehaviour>> cachedUIElements = new();

        public virtual void Awake()
        {
            CacheUIElements<Button>();
            CacheUIElements<Toggle>();
            CacheUIElements<Slider>();
            CacheUIElements<InputField>();
            CacheUIElements<Dropdown>();
            CacheUIElements<Text>();
            CacheUIElements<TextMeshPro>();
            CacheUIElements<Image>();
            // 可以继续缓存其他UI组件类型
            RegisterUIEvents();
        }

        /// <summary>
        /// 缓存UI元素，方便后续访问
        /// </summary>
        private void CacheUIElements<T>() where T : UIBehaviour
        {
            if (!cachedUIElements.TryGetValue(typeof(T), out var uiElements))
            {
                uiElements = new Dictionary<string, UIBehaviour>();
                cachedUIElements[typeof(T)] = uiElements;
            }
            uiElements.Clear();
            T[] elements = GetComponentsInChildren<T>(true);
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
            if (!cachedUIElements.TryGetValue(typeof(T), out var uiElements))
            {
                Debug.LogWarning($"[BaseUI] UI element type not cached: {typeof(T).Name} in {gameObject.name}");
                return null;
            }
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
            foreach (var pair in cachedUIElements)
            {
                Type type = pair.Key;
                Dictionary<string, UIBehaviour> uiElements = pair.Value;
                foreach (var kvp in uiElements)
                {
                    string name = kvp.Key;
                    if (type == typeof(Button) && kvp.Value is Button button)
                    {
                        button.onClick.AddListener(() => OnButtonClick(name));
                    }
                    else if (type == typeof(Toggle) && kvp.Value is Toggle toggle)
                    {
                        toggle.onValueChanged.AddListener((isOn) => OnToggleChanged(name, isOn));
                    }
                    else if (type == typeof(Slider) && kvp.Value is Slider slider)
                    {
                        slider.onValueChanged.AddListener((value) => OnSliderChanged(name, value));
                    }
                    else if (type == typeof(InputField) && kvp.Value is InputField inputField)
                    {
                        inputField.onValueChanged.AddListener((text) => OnInputFieldChanged(name, text));
                    }
                    // 可以继续添加其他UI组件的事件注册
                }
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