using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace GoveKits.Runtime.UI
{
    /// <summary>
    /// UI 元素自动收集组件。
    /// 挂在任意 GameObject 上，自动扫描子物体中的 UI 组件与 UIItem 小组件，按名称索引供外部访问。
    ///
    /// 用法：
    /// 1. 在界面/面板上挂本组件（ViewPanel 已通过 RequireComponent 自动添加）
    /// 2. 在 Inspector 中勾选需要收集的组件类型开关（如 enableButtons）
    /// 3. 子物体按名称命名（如 "LoginBtn"），即可用 Elements.Buttons["LoginBtn"] 访问
    /// 4. 交互事件通过 Elements.ButtonClicked 等订阅
    ///
    /// 访问方式：
    /// - 组件：Elements.Buttons["名字"] / Elements.TMPTexts["名字"] ...
    /// - 小组件：Elements.GetItem&lt;T&gt;("名字")
    /// - 交互：Elements.ButtonClicked += 处理函数
    /// </summary>
    public class UIElements : MonoBehaviour
    {
        #region 收集开关（在 Inspector 中勾选需要收集的组件类型）

        /// <summary>勾选后收集子级 Button，点击时触发 ButtonClicked 事件。</summary>
        public bool enableButtons = false;

        /// <summary>勾选后收集子级 Toggle，状态变化时触发 ToggleChanged 事件。</summary>
        public bool enableToggles = false;

        /// <summary>勾选后收集子级 Slider，值变化时触发 SliderChanged 事件。</summary>
        public bool enableSliders = false;

        /// <summary>勾选后收集子级原生 Dropdown，选择变化时触发 DropdownChanged 事件。</summary>
        public bool enableDropdowns = false;

        /// <summary>勾选后收集子级 Image 组件。</summary>
        public bool enableImages = false;

        /// <summary>勾选后收集子级 RawImage 组件。</summary>
        public bool enableRawImages = false;

        /// <summary>勾选后收集子级原生 Text 组件。</summary>
        public bool enableTexts = false;

        /// <summary>勾选后收集子级原生 InputField，内容变化时触发 InputChanged 事件。</summary>
        public bool enableInputFields = false;

        /// <summary>勾选后收集子级 TMP 文本组件。</summary>
        public bool enableTMPTexts = false;

        /// <summary>勾选后收集子级 TMP 输入框，内容变化时触发 TMPInputChanged 事件。</summary>
        public bool enableTMPInputFields = false;

        /// <summary>勾选后收集子级 TMP 下拉框，选择变化时触发 TMPDropdownChanged 事件。</summary>
        public bool enableTMPDropdowns = false;

        #endregion

        #region 组件字典（按名称索引访问，懒初始化）

        /// <summary>按钮字典 - 按键名访问：Elements.Buttons["LoginBtn"]</summary>
        protected Dictionary<string, Button> Buttons => _buttons ??= new Dictionary<string, Button>();
        private Dictionary<string, Button> _buttons;

        /// <summary>开关字典 - 按键名访问：Elements.Toggles["SoundToggle"]</summary>
        protected Dictionary<string, Toggle> Toggles => _toggles ??= new Dictionary<string, Toggle>();
        private Dictionary<string, Toggle> _toggles;

        /// <summary>滑块字典 - 按键名访问：Elements.Sliders["VolumeSlider"]</summary>
        protected Dictionary<string, Slider> Sliders => _sliders ??= new Dictionary<string, Slider>();
        private Dictionary<string, Slider> _sliders;

        /// <summary>原生下拉框字典 - 按键名访问</summary>
        protected Dictionary<string, Dropdown> Dropdowns => _dropdowns ??= new Dictionary<string, Dropdown>();
        private Dictionary<string, Dropdown> _dropdowns;

        /// <summary>图片字典 - 按键名访问</summary>
        protected Dictionary<string, Image> Images => _images ??= new Dictionary<string, Image>();
        private Dictionary<string, Image> _images;

        /// <summary>原始图片字典 - 按键名访问</summary>
        protected Dictionary<string, RawImage> RawImages => _rawImages ??= new Dictionary<string, RawImage>();
        private Dictionary<string, RawImage> _rawImages;

        /// <summary>原生文本字典 - 按键名访问</summary>
        protected Dictionary<string, Text> Texts => _texts ??= new Dictionary<string, Text>();
        private Dictionary<string, Text> _texts;

        /// <summary>原生输入框字典 - 按键名访问</summary>
        protected Dictionary<string, InputField> InputFields => _inputFields ??= new Dictionary<string, InputField>();
        private Dictionary<string, InputField> _inputFields;

        /// <summary>TMP 文本字典 - 按键名访问</summary>
        protected Dictionary<string, TextMeshProUGUI> TMPTexts => _tmpTexts ??= new Dictionary<string, TextMeshProUGUI>();
        private Dictionary<string, TextMeshProUGUI> _tmpTexts;

        /// <summary>TMP 输入框字典 - 按键名访问</summary>
        protected Dictionary<string, TMP_InputField> TMPInputFields => _tmpInputFields ??= new Dictionary<string, TMP_InputField>();
        private Dictionary<string, TMP_InputField> _tmpInputFields;

        /// <summary>TMP 下拉框字典 - 按键名访问</summary>
        protected Dictionary<string, TMP_Dropdown> TMPDropdowns => _tmpDropdowns ??= new Dictionary<string, TMP_Dropdown>();
        private Dictionary<string, TMP_Dropdown> _tmpDropdowns;

        /// <summary>子级 Item（小组件）字典 - 按键名访问：Elements.Items["UserNameInput"]</summary>
        public Dictionary<string, UIItem> Items => _items ??= new Dictionary<string, UIItem>();
        private Dictionary<string, UIItem> _items;

        #endregion

        /// <summary>按名称获取 Item，未注册返回 null。</summary>
        public UIItem GetItem(string name)
            => Items.TryGetValue(name, out var item) ? item : null;

        /// <summary>按名称获取指定类型的 Item，未注册或类型不符返回 null。</summary>
        public T GetItem<T>(string name) where T : UIItem
            => GetItem(name) as T;

        /// <summary>
        /// 重新收集所有启用的 UI 元素。
        /// 运行时动态新增/删除 UI 组件或 Item 后调用，会先清除旧绑定再重新收集。
        /// </summary>
        public void Rebind()
        {
            ClearBindings();
            AutoBindUIElements();
        }

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
            _items?.Clear(); _items = null;

            ButtonClicked = null;
            ToggleChanged = null;
            SliderChanged = null;
            DropdownChanged = null;
            TMPDropdownChanged = null;
            InputChanged = null;
            TMPInputChanged = null;
        }

        protected virtual void Awake()
        {
            AutoBindUIElements();
        }

        protected virtual void OnDestroy()
        {
            ClearBindings();
        }

        private void AutoBindUIElements()
        {
            var uiBehaviours = GetComponentsInChildren<UnityEngine.EventSystems.UIBehaviour>(true);

            foreach (var behaviour in uiBehaviours)
            {
                // 跳过位于 UIItem 内部的组件（Item 内部组件由 Item 自身的 UIElements 收集）
                var itemParent = behaviour.GetComponentInParent<UIItem>();
                if (itemParent != null && itemParent.transform != transform) continue;

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

            // 收集子级 UIItem（小组件），Item 内部组件由 Item 自身的收集器管理
            foreach (var item in GetComponentsInChildren<UIItem>(true))
            {
                if (item.transform == transform) continue;
                TryCache(ref _items, item.name, item);
            }
        }

        /// <summary>按键名缓存组件；同名重复时输出警告并忽略，避免静默覆盖。</summary>
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

        #region 交互事件（订阅面板 UI 的交互回调）

        /// <summary>按钮点击事件 - 参数为被点击按钮的名称。</summary>
        public event Action<string> ButtonClicked;

        /// <summary>开关状态改变事件 - 参数为开关名称与新状态。</summary>
        public event Action<string, bool> ToggleChanged;

        /// <summary>滑块值改变事件 - 参数为滑块名称与新值。</summary>
        public event Action<string, float> SliderChanged;

        /// <summary>原生下拉框选择改变事件 - 参数为下拉框名称与选中索引。</summary>
        public event Action<string, int> DropdownChanged;

        /// <summary>TMP 下拉框选择改变事件 - 参数为下拉框名称与选中索引。</summary>
        public event Action<string, int> TMPDropdownChanged;

        /// <summary>原生输入框内容改变事件 - 参数为输入框名称与当前内容。</summary>
        public event Action<string, string> InputChanged;

        /// <summary>TMP 输入框内容改变事件 - 参数为输入框名称与当前内容。</summary>
        public event Action<string, string> TMPInputChanged;

        #endregion
    }
}
