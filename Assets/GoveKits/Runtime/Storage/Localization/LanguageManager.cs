using UnityEngine;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using GoveKits.Singleton;


namespace GoveKits.Config
{
    public class LanguageManager : MonoSingleton<LanguageManager>
    {
        [SerializeField] private TextAsset _languageJson;
        [SerializeField] private List<LanguageFont> _fontSettings = new List<LanguageFont>();

        // 运行时状态
        private LanguageCode _currentLanguage = LanguageCode.ChineseCN;
        private bool _isInitialized = false;

        // 原始数据: <Key, <LanguageName, Text>>
        // 这是一个通用结构，不需要定义具体的 DTO 类
        private Dictionary<string, Dictionary<string, string>> _rawData;

        public event System.Action<LanguageCode> OnLanguageSwitched;

        [System.Serializable]
        public class LanguageFont
        {
            public LanguageCode languageCode;
            public TMP_FontAsset fontAsset;
        }

        private void Awake()
        {
            Initialize();
        }

        public void Initialize()
        {
            if (_isInitialized) return;

            // 1. 加载默认设置
            LoadSavedLanguageSettings();

            // 2. 解析 JSON
            ParseJsonData();

            // 3. 构建缓存
            if (_rawData != null)
            {
                SwitchLanguage(_currentLanguage, true); // 强制刷新一次
                _isInitialized = true;
                Debug.Log($"[LanguageManager] 初始化完成. 当前语言: {_currentLanguage}");
            }
        }

        /// <summary>
        /// 解析 JSON 为通用字典结构
        /// </summary>
        private void ParseJsonData()
        {
            if (_languageJson == null)
            {
                Debug.LogError("[LanguageManager] 未配置 Language Json 文件！");
                return;
            }

            try
            {
                // 核心：直接反序列化为嵌套字典，完全不需要 DTO 类
                _rawData = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, string>>>(_languageJson.text);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[LanguageManager] JSON 解析失败: {e.Message}");
            }
        }

        /// <summary>
        /// 切换语言并重建缓存
        /// </summary>
        public void SwitchLanguage(LanguageCode targetCode, bool force = false)
        {
            if (!force && _currentLanguage == targetCode && _isInitialized) return;

            _currentLanguage = targetCode;

            SaveLanguageSettings();
            OnLanguageSwitched?.Invoke(_currentLanguage);
        }

        #region Public API (获取文本)

        public string GetText(string key)
        {
            if (!_isInitialized) return key;
            
            if (_rawData.TryGetValue(key, out Dictionary<string, string> langDict))
            {
                string langName = _currentLanguage.ToString();
                if (langDict.TryGetValue(langName, out string result))
                {
                    return result;
                }
            }
            return $"[#{key}]"; // 缺失 Key 的表现
        }

        #endregion


        #region Settings & Fonts

        private void LoadSavedLanguageSettings()
        {
            string saved = PlayerPrefs.GetString("CurrentLanguage", LanguageCode.ChineseCN.ToString());
            if (System.Enum.TryParse(saved, out LanguageCode code))
            {
                _currentLanguage = code;
            }
        }

        private void SaveLanguageSettings()
        {
            PlayerPrefs.SetString("CurrentLanguage", _currentLanguage.ToString());
            PlayerPrefs.Save();
        }

        public LanguageCode GetCurrentLanguage() => _currentLanguage;

        public TMP_FontAsset GetCurrentFont()
        {
            var setting = _fontSettings.Find(x => x.languageCode == _currentLanguage);
            return setting != null ? setting.fontAsset : null;
        }

        #endregion
    }

    public enum LanguageCode
    {
        ChineseCN,
        EnglishUS,
        // Spanish,
        // French,
        // German,
        // Japanese,
        // Korean,
        // Russian,
        // Portuguese,
        // Italian
    }
}   