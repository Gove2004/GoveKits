using UnityEngine;
using TMPro;

namespace GoveKits.Localization
{
    [RequireComponent(typeof(TMP_Text))]
    public class LocalizationComponent : MonoBehaviour
    {
        [Tooltip("多语言 Key")]
        public string Key;

        private TMP_Text _tmpText;

        private void Awake()
        {
            _tmpText = GetComponent<TMP_Text>();
        }

        private void Start()
        {
            UpdateContent();
        }

        private void OnEnable()
        {
            LanguageManager.OnLanguageChanged += UpdateContent;
            // 每次激活时重新刷新，防止字体丢失或文本未更新
            UpdateContent();
        }

        private void OnDisable()
        {
            LanguageManager.OnLanguageChanged -= UpdateContent;
        }

        /// <summary>
        /// 核心更新逻辑
        /// </summary>
        public void UpdateContent()
        {
            if (string.IsNullOrEmpty(Key) || _tmpText == null) return;

            // 1. 更新文本
            string content = LanguageManager.GetText(Key);
            // 只有当文本真正变化时才赋值，避免触发网格重建
            if (_tmpText.text != content) 
            {
                _tmpText.text = content;
            }

            // 2. 更新字体
            // 不同的语言可能需要不同的字体 (如中文需要含中文字库的字体)
            TMP_FontAsset font = LanguageManager.GetCurrentFont();
            if (font != null && _tmpText.font != font)
            {
                _tmpText.font = font;
            }
        }

        // 方便代码动态修改 Key
        public void SetKey(string newKey)
        {
            Key = newKey;
            UpdateContent();
        }
    }
}