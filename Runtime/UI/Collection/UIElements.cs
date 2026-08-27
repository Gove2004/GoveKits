using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace GoveKits.Runtime.UI
{
    /// <summary>
    /// UI 元素自动收集组件。
    /// 独立挂载在任意 GameObject 上，扫描子物体中的指定 UI 组件并按名称索引，供其他逻辑访问。
    ///
    /// 核心功能：
    /// 1. 按开关扫描子对象中的指定 UI 组件
    /// 2. 按名称索引存储到字典中，便于代码访问
    /// 3. 自动绑定事件监听，通过 C# 事件对外暴露（ButtonClicked 等）
    /// 4. 支持原生 UI 和 TextMeshPro 组件
    ///
    /// 性能优化：
    /// 通过 enableXXX 开关控制是否需要扫描某类组件，避免不必要的字典和监听器开销。
    /// 在编辑器中勾选需要的组件类型即可。
    ///
    /// 使用方式：
    /// 将本组件挂到面板 GameObject 上，在编辑器中勾选需要的 UI 组件类型，
    /// 外部通过 Elements.Buttons["BtnName"] 等方式访问组件，通过 Elements.ButtonClicked 订阅交互事件。
    /// </summary>
    public class UIElements : MonoBehaviour
    {
        #region 收集开关（在编辑器中勾选需要的组件类型）

        /// <summary>是否需要收集 Button 组件。未勾选则跳过扫描和事件绑定。</summary>
        public bool enableButtons = false;

        /// <summary>是否需要收集 Toggle 组件。</summary>
        public bool enableToggles = false;

        /// <summary>是否需要收集 Slider 组件。</summary>
        public bool enableSliders = false;

        /// <summary>是否需要收集 Dropdown 组件。</summary>
        public bool enableDropdowns = false;

        /// <summary>是否需要收集 Image 组件。</summary>
        public bool enableImages = false;

        /// <summary>是否需要收集 RawImage 组件。</summary>
        public bool enableRawImages = false;

        /// <summary>是否需要收集原生 Text 组件。</summary>
        public bool enableTexts = false;

        /// <summary>是否需要收集原生 InputField 组件。</summary>
        public bool enableInputFields = false;

        /// <summary>是否需要收集 TMP Text 组件。</summary>
        public bool enableTMPTexts = false;

        /// <summary>是否需要收集 TMP InputField 组件。</summary>
        public bool enableTMPInputFields = false;

        /// <summary>是否需要收集 TMP Dropdown 组件。</summary>
        public bool enableTMPDropdowns = false;

        #endregion

        #region 组件字典（懒初始化，按需分配）

        private Dictionary<string, Button> _buttons;
        /// <summary>按钮组件字典 - 按名称索引</summary>
        protected Dictionary<string, Button> Buttons => _buttons ??= new Dictionary<string, Button>();

        private Dictionary<string, Toggle> _toggles;
        /// <summary>开关组件字典 - 按名称索引</summary>
        protected Dictionary<string, Toggle> Toggles => _toggles ??= new Dictionary<string, Toggle>();

        private Dictionary<string, Slider> _sliders;
        /// <summary>滑块组件字典 - 按名称索引</summary>
        protected Dictionary<string, Slider> Sliders => _sliders ??= new Dictionary<string, Slider>();

        private Dictionary<string, Dropdown> _dropdowns;
        /// <summary>原生下拉框组件字典 - 按名称索引</summary>
        protected Dictionary<string, Dropdown> Dropdowns => _dropdowns ??= new Dictionary<string, Dropdown>();

        private Dictionary<string, Image> _images;
        /// <summary>图片组件字典 - 按名称索引</summary>
        protected Dictionary<string, Image> Images => _images ??= new Dictionary<string, Image>();

        private Dictionary<string, RawImage> _rawImages;
        /// <summary>原始图片组件字典 - 按名称索引</summary>
        protected Dictionary<string, RawImage> RawImages => _rawImages ??= new Dictionary<string, RawImage>();

        private Dictionary<string, Text> _texts;
        /// <summary>原生文本组件字典 - 按名称索引</summary>
        protected Dictionary<string, Text> Texts => _texts ??= new Dictionary<string, Text>();

        private Dictionary<string, InputField> _inputFields;
        /// <summary>原生输入框字典 - 按名称索引</summary>
        protected Dictionary<string, InputField> InputFields => _inputFields ??= new Dictionary<string, InputField>();

        private Dictionary<string, TextMeshProUGUI> _tmpTexts;
        /// <summary>TMP 文本组件字典 - 按名称索引</summary>
        protected Dictionary<string, TextMeshProUGUI> TMPTexts => _tmpTexts ??= new Dictionary<string, TextMeshProUGUI>();

        private Dictionary<string, TMP_InputField> _tmpInputFields;
        /// <summary>TMP 输入框字典 - 按名称索引</summary>
        protected Dictionary<string, TMP_InputField> TMPInputFields => _tmpInputFields ??= new Dictionary<string, TMP_InputField>();

        private Dictionary<string, TMP_Dropdown> _tmpDropdowns;
        /// <summary>TMP 下拉框字典 - 按名称索引</summary>
        protected Dictionary<string, TMP_Dropdown> TMPDropdowns => _tmpDropdowns ??= new Dictionary<string, TMP_Dropdown>();

        #endregion

        protected virtual void Awake()
        {
            AutoBindUIElements();
        }

        /// <summary>
        /// 重新收集并绑定所有启用的 UI 元素。
        /// 运行时动态新增/删除 UI 组件后调用，可重复调用（自动先清除旧绑定）。
        /// </summary>
        public void Rebind()
        {
            ClearBindings();
            AutoBindUIElements();
        }

        /// <summary>
        /// 清除所有已收集组件与监听器，并置空对外事件引用。
        /// 由 Rebind 与 OnDestroy 复用。
        /// </summary>
        private void ClearBindings()
        {
            if (_buttons != null) { foreach (var b in _buttons.Values) { if (b) b.onClick.RemoveAllListeners(); } _buttons.Clear(); _buttons = null; }
            if (_toggles != null) { foreach (var t in _toggles.Values) { if (t) t.onValueChanged.RemoveAllListeners(); } _toggles.Clear(); _toggles = null; }
            if (_sliders != null) { foreach (var s in _sliders.Values) { if (s) s.onValueChanged.RemoveAllListeners(); } _sliders.Clear(); _sliders = null; }
            if (_dropdowns != null) { foreach (var d in _dropdowns.Values) { if (d) d.onValueChanged.RemoveAllListeners(); } _dropdowns.Clear(); _dropdowns = null; }
            if (_tmpDropdowns != null) { foreach (var d in _tmpDropdowns.Values) { if (d) d.onValueChanged.RemoveAllListeners(); } _tmpDropdowns.Clear(); _tmpDropdowns = null; }
            if (_inputFields != null) { foreach (var i in _inputFields.Values) { if (i) i.onValueChanged.RemoveAllListeners(); } _inputFields.Clear(); _inputFields = null; }
            if (_tmpInputFields != null) { foreach (var i in _tmpInputFields.Values) { if (i) i.onValueChanged.RemoveAllListeners(); } _tmpInputFields.Clear(); _tmpInputFields = null; }

            _texts?.Clear(); _texts = null;
            _tmpTexts?.Clear(); _tmpTexts = null;
            _images?.Clear(); _images = null;
            _rawImages?.Clear(); _rawImages = null;

            ButtonClicked = null;
            ToggleChanged = null;
            SliderChanged = null;
            DropdownChanged = null;
            TMPDropdownChanged = null;
            InputChanged = null;
            TMPInputChanged = null;
        }

        protected virtual void OnDestroy()
        {
            ClearBindings();
        }

        /// <summary>
        /// 自动绑定所有启用的 UI 元素。
        /// 单次遍历统一提取，根据开关决定是否缓存和绑定事件。
        /// </summary>
        private void AutoBindUIElements()
        {
            // 单次遍历所有 UIBehaviour，按开关分发到对应字典
            var uiBehaviours = GetComponentsInChildren<UnityEngine.EventSystems.UIBehaviour>(true);

            foreach (var behaviour in uiBehaviours)
            {
                // 跳过位于 UIWidget 内部的组件（Widget 内部由 Widget 自身管理，不纳入本收集器）
                // 本收集器自身挂在 Widget 上时不跳过（widgetParent.transform == transform）
                var widgetParent = behaviour.GetComponentInParent<UIWidget>();
                if (widgetParent != null && widgetParent.transform != transform) continue;

                string compName = behaviour.name;

                switch (behaviour)
                {
                    case Button btn:
                        if (enableButtons && TryCache(ref _buttons, compName, btn))
                            btn.onClick.AddListener(() => ButtonClicked?.Invoke(compName));
                        break;

                    case Toggle tog:
                        if (enableToggles && TryCache(ref _toggles, compName, tog))
                            tog.onValueChanged.AddListener(val => ToggleChanged?.Invoke(compName, val));
                        break;

                    case Slider slider:
                        if (enableSliders && TryCache(ref _sliders, compName, slider))
                            slider.onValueChanged.AddListener(val => SliderChanged?.Invoke(compName, val));
                        break;

                    case Dropdown dp:
                        if (enableDropdowns && TryCache(ref _dropdowns, compName, dp))
                            dp.onValueChanged.AddListener(val => DropdownChanged?.Invoke(compName, val));
                        break;

                    case TMP_Dropdown tmpDp:
                        if (enableTMPDropdowns && TryCache(ref _tmpDropdowns, compName, tmpDp))
                            tmpDp.onValueChanged.AddListener(val => TMPDropdownChanged?.Invoke(compName, val));
                        break;

                    case InputField input:
                        if (enableInputFields && TryCache(ref _inputFields, compName, input))
                            input.onValueChanged.AddListener(val => InputChanged?.Invoke(compName, val));
                        break;

                    case TMP_InputField tmpInput:
                        if (enableTMPInputFields && TryCache(ref _tmpInputFields, compName, tmpInput))
                            tmpInput.onValueChanged.AddListener(val => TMPInputChanged?.Invoke(compName, val));
                        break;

                    case Text txt:
                        if (enableTexts)
                            TryCache(ref _texts, compName, txt);
                        break;

                    case TextMeshProUGUI tmpTxt:
                        if (enableTMPTexts)
                            TryCache(ref _tmpTexts, compName, tmpTxt);
                        break;

                    case Image img:
                        if (enableImages)
                            TryCache(ref _images, compName, img);
                        break;

                    case RawImage rawImg:
                        if (enableRawImages)
                            TryCache(ref _rawImages, compName, rawImg);
                        break;
                }
            }
        }

        /// <summary>
        /// 辅助缓存方法，同时避免同名组件被静默覆盖。
        /// </summary>
        private bool TryCache<T>(ref Dictionary<string, T> dict, string name, T component) where T : Component
        {
            dict ??= new Dictionary<string, T>();

            if (!dict.ContainsKey(name))
            {
                dict.Add(name, component);
                return true;
            }

            Debug.LogWarning($"[UIElements] <{gameObject.name}> 存在同名的同类 UI 组件: {name} ({typeof(T).Name})。可能会导致事件路由和获取混乱，请检查层级！");
            return false;
        }

        #region 对外交互事件（任意逻辑可订阅）

        /// <summary>按钮点击事件 - 参数为按钮名称</summary>
        public event Action<string> ButtonClicked;

        /// <summary>开关状态改变事件 - 参数为开关名称与状态</summary>
        public event Action<string, bool> ToggleChanged;

        /// <summary>滑块值改变事件 - 参数为滑块名称与值</summary>
        public event Action<string, float> SliderChanged;

        /// <summary>原生下拉框选择改变事件 - 参数为下拉框名称与选项索引</summary>
        public event Action<string, int> DropdownChanged;

        /// <summary>TMP 下拉框选择改变事件 - 参数为下拉框名称与选项索引</summary>
        public event Action<string, int> TMPDropdownChanged;

        /// <summary>原生输入框内容改变事件 - 参数为输入框名称与内容</summary>
        public event Action<string, string> InputChanged;

        /// <summary>TMP 输入框内容改变事件 - 参数为输入框名称与内容</summary>
        public event Action<string, string> TMPInputChanged;

        #endregion
    }
}
