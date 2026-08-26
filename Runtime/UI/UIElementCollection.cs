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
    /// 1. 按开关扫描子对象中的指定 UI 组件
    /// 2. 按名称索引存储到字典中，便于代码访问
    /// 3. 自动绑定事件监听，路由到虚方法供子类重写
    /// 4. 支持原生 UI 和 TextMeshPro 组件
    ///
    /// 性能优化：
    /// 通过 enableXXX 开关控制是否需要扫描某类组件，避免不必要的字典和监听器开销。
    /// 在编辑器中勾选需要的组件类型即可。
    ///
    /// 使用方式：
    /// 继承此类后，在编辑器中勾选需要的 UI 组件类型，子类可直接通过 Buttons["BtnName"] 等方式访问组件。
    /// 重写 OnButtonClicked 等虚方法处理交互逻辑。
    /// </summary>
    public abstract class UIElementCollection : MonoBehaviour
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
        /// 自动绑定所有启用的 UI 元素。
        /// 单次遍历统一提取，根据开关决定是否缓存和绑定事件。
        /// </summary>
        private void AutoBindUIElements()
        {
            // 单次遍历所有 UIBehaviour，按开关分发到对应字典
            var uiBehaviours = GetComponentsInChildren<UnityEngine.EventSystems.UIBehaviour>(true);

            foreach (var behaviour in uiBehaviours)
            {
                string compName = behaviour.name;

                switch (behaviour)
                {
                    case Button btn:
                        if (enableButtons && TryCache(ref _buttons, compName, btn))
                            btn.onClick.AddListener(() => OnButtonClicked(compName));
                        break;

                    case Toggle tog:
                        if (enableToggles && TryCache(ref _toggles, compName, tog))
                            tog.onValueChanged.AddListener(val => OnToggleChanged(compName, val));
                        break;

                    case Slider slider:
                        if (enableSliders && TryCache(ref _sliders, compName, slider))
                            slider.onValueChanged.AddListener(val => OnSliderChanged(compName, val));
                        break;

                    case Dropdown dp:
                        if (enableDropdowns && TryCache(ref _dropdowns, compName, dp))
                            dp.onValueChanged.AddListener(val => OnDropdownChanged(compName, val));
                        break;

                    case TMP_Dropdown tmpDp:
                        if (enableTMPDropdowns && TryCache(ref _tmpDropdowns, compName, tmpDp))
                            tmpDp.onValueChanged.AddListener(val => OnTMPDropdownChanged(compName, val));
                        break;

                    case InputField input:
                        if (enableInputFields && TryCache(ref _inputFields, compName, input))
                            input.onValueChanged.AddListener(val => OnInputChanged(compName, val));
                        break;

                    case TMP_InputField tmpInput:
                        if (enableTMPInputFields && TryCache(ref _tmpInputFields, compName, tmpInput))
                            tmpInput.onValueChanged.AddListener(val => OnTMPInputChanged(compName, val));
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

            Debug.LogWarning($"[UIElementCollection] <{gameObject.name}> 存在同名的同类 UI 组件: {name} ({typeof(T).Name})。可能会导致事件路由和获取混乱，请检查层级！");
            return false;
        }

        protected virtual void OnDestroy()
        {
            // 清理已启用的组件字典，移除所有监听器
            if (enableButtons && _buttons != null) { foreach (var b in _buttons.Values) { if (b) b.onClick.RemoveAllListeners(); } _buttons.Clear(); _buttons = null; }
            if (enableToggles && _toggles != null) { foreach (var t in _toggles.Values) { if (t) t.onValueChanged.RemoveAllListeners(); } _toggles.Clear(); _toggles = null; }
            if (enableSliders && _sliders != null) { foreach (var s in _sliders.Values) { if (s) s.onValueChanged.RemoveAllListeners(); } _sliders.Clear(); _sliders = null; }
            if (enableDropdowns && _dropdowns != null) { foreach (var d in _dropdowns.Values) { if (d) d.onValueChanged.RemoveAllListeners(); } _dropdowns.Clear(); _dropdowns = null; }
            if (enableTMPDropdowns && _tmpDropdowns != null) { foreach (var d in _tmpDropdowns.Values) { if (d) d.onValueChanged.RemoveAllListeners(); } _tmpDropdowns.Clear(); _tmpDropdowns = null; }
            if (enableInputFields && _inputFields != null) { foreach (var i in _inputFields.Values) { if (i) i.onValueChanged.RemoveAllListeners(); } _inputFields.Clear(); _inputFields = null; }
            if (enableTMPInputFields && _tmpInputFields != null) { foreach (var i in _tmpInputFields.Values) { if (i) i.onValueChanged.RemoveAllListeners(); } _tmpInputFields.Clear(); _tmpInputFields = null; }

            _texts?.Clear(); _texts = null;
            _tmpTexts?.Clear(); _tmpTexts = null;
            _images?.Clear(); _images = null;
            _rawImages?.Clear(); _rawImages = null;
        }

        #region 供子类重写的交互回调

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

        #endregion
    }
}
