using System;
using System.Collections.Generic;
using GoveKits.Runtime.Core;

namespace GoveKits.Runtime.Storage
{
    /// <summary>
    /// 多语言本地化核心。
    /// </summary>
    public static class LocalizationCore
    {
        private const string LanguagePrefKey = "Localization.Language";

        private static readonly Dictionary<string, ILocalizationConfigData> _rawRows = new();
        private static readonly Dictionary<string, string> _currentLangCache = new();
        private static readonly Dictionary<(Type t, string field), System.Reflection.FieldInfo> _fieldCache = new();

#if TMP_PRESENT
        private const string FontConfigResourcePath = "Config/LocalizationConfig";
        private static LocalizationConfig _fontConfig;
#endif
        private static LanguageCode _currentLanguage = LanguageCode.ChineseCN;

        public static event Action OnLanguageChanged;
        public static LanguageCode CurrentLanguage => _currentLanguage;

        /// <summary>
        /// 初始化多语言系统。加载配置数据、读取上次选择的语言设置并刷新缓存。
        /// </summary>
        public static void Setup()
        {
            try
            {
                LoadRowsFromConfig();
                LoadLanguageSettings();

#if TMP_PRESENT
                _fontConfig = Resources.Load<LocalizationConfig>(FontConfigResourcePath);
#endif
                RefreshCache();

                LogCore.Success(nameof(LocalizationCore), $"初始化成功: Language={_currentLanguage}, Keys={_currentLangCache.Count}");
            }
            catch (Exception e)
            {
                LogCore.Error(nameof(LocalizationCore), $"初始化失败: {e.Message}");
            }
        }

        /// <summary>
        /// 切换当前显示语言。切换后会刷新缓存并触发 OnLanguageChanged 事件。
        /// </summary>
        /// <param name="code">目标语言代码。</param>
        public static void SwitchLanguage(LanguageCode code)
        {
            if (_currentLanguage == code) return;

            _currentLanguage = code;
            PrefsCore.SetInt(LanguagePrefKey, (int)_currentLanguage);
            PrefsCore.Save();

            RefreshCache();
            OnLanguageChanged?.Invoke();
        }

        /// <summary>
        /// 根据键名获取本地化文本。若键不存在则返回 "#key#" 格式的占位符。
        /// </summary>
        /// <param name="key">本地化键名。</param>
        /// <returns>当前语言对应的文本内容。</returns>
        public static string GetText(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) return string.Empty;

            if (_currentLangCache.TryGetValue(key, out string result))
                return result;

            return $"#{key}#";
        }

#if TMP_PRESENT
        /// <summary>
        /// 获取当前语言对应的字体资源（仅在 TMP 可用时有效）。
        /// </summary>
        /// <returns>当前语言的 TMP 字体资源。</returns>
        public static TMPro.TMP_FontAsset GetCurrentFont()
        {
            if (_fontConfig == null) return null;
            return _fontConfig.GetFont(_currentLanguage);
        }
#endif

        private static void LoadRowsFromConfig()
        {
            _rawRows.Clear();
            var rows = ConfigCore.LoadAll<ILocalizationConfigData>();

            foreach (var row in rows)
            {
                if (row == null || string.IsNullOrWhiteSpace(row.Key)) continue;
                _rawRows[row.Key] = row;
            }
        }

        private static void LoadLanguageSettings()
        {
            int defaultCode = (int)LanguageCode.ChineseCN;
            int code = PrefsCore.GetInt(LanguagePrefKey, defaultCode);
            if (Enum.IsDefined(typeof(LanguageCode), code))
                _currentLanguage = (LanguageCode)code;
            else
                _currentLanguage = LanguageCode.ChineseCN;
        }

        private static void RefreshCache()
        {
            _currentLangCache.Clear();
            string langName = _currentLanguage.ToString();
            string fallbackName = LanguageCode.EnglishUS.ToString();

            foreach (var kvp in _rawRows)
            {
                string key = kvp.Key;
                ILocalizationConfigData row = kvp.Value;

                string content = ReadLanguageField(row, langName);
                if (string.IsNullOrEmpty(content))
                    content = ReadLanguageField(row, fallbackName);

                if (!string.IsNullOrEmpty(content))
                    _currentLangCache[key] = content;
            }
        }

        private static string ReadLanguageField(ILocalizationConfigData row, string fieldName)
        {
            var key = (row.GetType(), fieldName);
            System.Reflection.FieldInfo field;
            if (!_fieldCache.TryGetValue(key, out field))
            {
                field = row.GetType().GetField(fieldName);
                _fieldCache[key] = field;
            }
            if (field == null) return null;

            object value = field.GetValue(row);
            return value as string;
        }

        /// <summary>
        /// 关闭多语言系统，清空所有缓存数据。
        /// </summary>
        public static void Close()
        {
            _currentLangCache.Clear();
            _rawRows.Clear();
            OnLanguageChanged = null;
#if TMP_PRESENT
            _fontConfig = null;
#endif
        }
    }
}
