using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace GoveKits.Runtime.UI
{
    /// <summary>
    /// UI 元素自动收集与路由基类
    /// 
    /// 核心功能：
    /// 1. 自动扫描子对象中的所有 UI 组件
    /// 2. 按名称索引存储到字典中，便于代码访问
    /// 3. 自动绑定事件监听，路由到虚方法供子类重写
    /// 4. 支持原生 UI 和 TextMeshPro 组件
    /// 
    /// 使用方式：
    /// 继承此类后，子类可直接通过 Buttons["BtnName"] 等方式访问组件
    /// 重写 OnButtonClicked 等虚方法处理交互逻辑
    /// </summary>
    public abstract class UIElementCollection : MonoBehaviour
    {
        // --- 核心字典容器 ---
        /// <summary>按钮组件字典 - 按名称索引</summary>
        protected Dictionary<string, Button> Buttons = new();
        /// <summary>开关组件字典 - 按名称索引</summary>
        protected Dictionary<string, Toggle> Toggles = new();
        /// <summary>滑块组件字典 - 按名称索引</summary>
        protected Dictionary<string, Slider> Sliders = new();
        /// <summary>下拉框组件字典 - 按名称索引</summary>
        protected Dictionary<string, Dropdown> Dropdowns = new();
        /// <summary>图片组件字典 - 按名称索引</summary>
        protected Dictionary<string, Image> Images = new();
        /// <summary>原始图片组件字典 - 按名称索引</summary>
        protected Dictionary<string, RawImage> RawImages = new();
        
        // --- 文本与输入 (原生) ---
        /// <summary>原生文本组件字典 - 按名称索引</summary>
        protected Dictionary<string, Text> Texts = new();
        /// <summary>原生输入框字典 - 按名称索引</summary>
        protected Dictionary<string, InputField> InputFields = new();
        
        // --- TextMeshPro ---
        /// <summary>TMP 文本组件字典 - 按名称索引</summary>
        protected Dictionary<string, TextMeshProUGUI> TMPTexts = new();
        /// <summary>TMP 输入框字典 - 按名称索引</summary>
        protected Dictionary<string, TMP_InputField> TMPInputFields = new();
        /// <summary>TMP 下拉框字典 - 按名称索引</summary>
        protected Dictionary<string, TMP_Dropdown> TMPDropdowns = new();

        protected virtual void Awake()
        {
            AutoBindUIElements();
        }

        /// <summary>
        /// 自动绑定所有 UI 元素
        /// 遍历子对象，收集组件并注册事件监听
        /// </summary>
        private void AutoBindUIElements()
        {
            // 1. Button 点击事件 - 自动路由到 OnButtonClicked
            foreach (var btn in GetComponentsInChildren<Button>(true))
            {
                Buttons[btn.name] = btn;
                string btnName = btn.name;
                btn.onClick.AddListener(() => OnButtonClicked(btnName));
            }

            // 2. Toggle 状态改变 - 自动路由到 OnToggleChanged
            foreach (var tog in GetComponentsInChildren<Toggle>(true))
            {
                Toggles[tog.name] = tog;
                string togName = tog.name;
                tog.onValueChanged.AddListener(val => OnToggleChanged(togName, val));
            }

            // 3. Slider 滑动 - 自动路由到 OnSliderChanged
            foreach (var slider in GetComponentsInChildren<Slider>(true))
            {
                Sliders[slider.name] = slider;
                string sName = slider.name;
                slider.onValueChanged.AddListener(val => OnSliderChanged(sName, val));
            }

            // 4. Dropdown (原生 & TMP) - 分别路由
            foreach (var dp in GetComponentsInChildren<Dropdown>(true))
            {
                Dropdowns[dp.name] = dp;
                string dName = dp.name;
                dp.onValueChanged.AddListener(val => OnDropdownChanged(dName, val));
            }
            foreach (var dp in GetComponentsInChildren<TMP_Dropdown>(true))
            {
                TMPDropdowns[dp.name] = dp;
                string dName = dp.name;
                dp.onValueChanged.AddListener(val => OnTMPDropdownChanged(dName, val));
            }

            // 5. InputField (原生 & TMP) - 分别路由
            foreach (var input in GetComponentsInChildren<InputField>(true))
            {
                InputFields[input.name] = input;
                string iName = input.name;
                input.onValueChanged.AddListener(val => OnInputChanged(iName, val));
            }
            foreach (var input in GetComponentsInChildren<TMP_InputField>(true))
            {
                TMPInputFields[input.name] = input;
                string iName = input.name;
                input.onValueChanged.AddListener(val => OnTMPInputChanged(iName, val));
            }

            // 6. 纯显示组件 (Text, TMPText, Image, RawImage) - 仅收集，无事件
            foreach (var txt in GetComponentsInChildren<Text>(true)) Texts[txt.name] = txt;
            foreach (var txt in GetComponentsInChildren<TextMeshProUGUI>(true)) TMPTexts[txt.name] = txt;
            foreach (var img in GetComponentsInChildren<Image>(true)) Images[img.name] = img;
            foreach (var img in GetComponentsInChildren<RawImage>(true)) RawImages[img.name] = img;
        }

        // --- 供子类重写的交互回调 ---
        /// <summary>按钮点击回调 - 子类重写处理具体逻辑</summary>
        protected virtual void OnButtonClicked(string btnName) { }
        /// <summary>开关状态改变回调</summary>
        protected virtual void OnToggleChanged(string togName, bool val) { }
        /// <summary>滑块值改变回调</summary>
        protected virtual void OnSliderChanged(string sName, float val) { }
        /// <summary>原生下拉框选择改变回调</summary>
        protected virtual void OnDropdownChanged(string dName, int val) { }
        /// <summary>TMP 下拉框选择改变回调</summary>
        protected virtual void OnTMPDropdownChanged(string dName, int val) { }
        /// <summary>原生输入框内容改变回调</summary>
        protected virtual void OnInputChanged(string iName, string val) { }
        /// <summary>TMP 输入框内容改变回调</summary>
        protected virtual void OnTMPInputChanged(string iName, string val) { }
    }
}