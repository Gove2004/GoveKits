using GoveKits.Runtime.Core;
using UnityEngine;
using UnityEngine.UI;
#if TMP_PRESENT
using TMPro;
#endif

namespace GoveKits.Runtime.Storage
{
    /// <summary>
    /// 本地化组件，挂载在 Text 或 TMP_Text 对象上，自动跟随当前语言更新显示文本和字体。
    /// </summary>
    public class LocalizationComponent : MonoBehaviour
    {
        [Tooltip("多语言 Key")]
        public string Key;

#if TMP_PRESENT
        private TMP_Text _tmpText;
#endif
        private Text _uiText;

        private void Awake()
        {
#if TMP_PRESENT
            _tmpText = GetComponent<TMP_Text>();
#endif
            _uiText = GetComponent<Text>();
        }

        private void Start()
        {
#if TMP_PRESENT
            if (_tmpText == null && _uiText == null)
#else
            if (_uiText == null)
#endif
                LogCore.Warning(nameof(LocalizationComponent), $"当前对象上没有 TMP_Text 或 UI.Text 组件: {name}");
            UpdateContent();
        }

        private void OnEnable()
        {
            LocalizationCore.OnLanguageChanged += UpdateContent;
        }

        private void OnDisable()
        {
            LocalizationCore.OnLanguageChanged -= UpdateContent;
        }

        /// <summary>
        /// 手动更新显示内容。通常在键名变更后调用。
        /// </summary>
        public void UpdateContent()
        {
            if (string.IsNullOrEmpty(Key)) return;

            string content = LocalizationCore.GetText(Key);

#if TMP_PRESENT
            if (_tmpText != null && _tmpText.text != content)
                _tmpText.text = content;
#endif

            if (_uiText != null && _uiText.text != content)
                _uiText.text = content;

#if TMP_PRESENT
            TMP_FontAsset font = LocalizationCore.GetCurrentFont();
            if (font != null && _tmpText != null && _tmpText.font != font)
                _tmpText.font = font;
#endif
        }

        /// <summary>
        /// 设置本地化键名并立即更新显示内容。
        /// </summary>
        /// <param name="newKey">新的本地化键名。</param>
        public void SetKey(string newKey)
        {
            Key = newKey;
            UpdateContent();
        }
    }
}
