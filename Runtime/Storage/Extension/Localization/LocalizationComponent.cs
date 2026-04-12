using GoveKits.Runtime.Core;
using UnityEngine;
#if TMP_PRESENT
using TMPro;
#endif
using UnityEngine.UI;

namespace GoveKits.Runtime.Storage
{
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
            UpdateContent();
        }

        private void OnEnable()
        {
            LocalizationCore.OnLanguageChanged += UpdateContent;
            // 每次激活时重新刷新，防止字体丢失或文本未更新
            UpdateContent();
        }

        private void OnDisable()
        {
            LocalizationCore.OnLanguageChanged -= UpdateContent;
        }

        /// <summary>
        /// 核心更新逻辑
        /// </summary>
        public void UpdateContent()
        {
            if (string.IsNullOrEmpty(Key))
            {
                return;
            }

            // 1. 更新文本
            string content = LocalizationCore.GetText(Key);
            // 只有当文本真正变化时才赋值，避免触发网格重建
#if TMP_PRESENT
            if (_tmpText != null && _tmpText.text != content)
            {
                _tmpText.text = content;
            }
#endif

            if (_uiText != null && _uiText.text != content)
            {
                _uiText.text = content;
            }

            // 2. 更新字体
            // 不同的语言可能需要不同的字体 (如中文需要含中文字库的字体)
#if TMP_PRESENT
            TMP_FontAsset font = LocalizationCore.GetCurrentFont();
            if (font != null && _tmpText.font != font)
            {
                _tmpText.font = font;
            }
#endif
        }

        // 方便代码动态修改 Key
        public void SetKey(string newKey)
        {
            Key = newKey;
            UpdateContent();
        }
    }
}