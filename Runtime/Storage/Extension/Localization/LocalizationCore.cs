using System;
using System.Collections.Generic;
using GoveKits.Runtime.Util;

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
        private static LanguageCode _fallbackLanguage = LanguageCode.EnglishUS;

        public static event Action OnLanguageChanged;
        public static LanguageCode CurrentLanguage => _currentLanguage;
        /// <summary>缺失翻译时的回退语言（可通过 Setup 配置）。</summary>
        public static LanguageCode FallbackLanguage => _fallbackLanguage;

        /// <summary>
        /// 初始化多语言系统。加载配置数据、读取上次选择的语言设置并刷新缓存。
        /// 完成后会补发一次 OnLanguageChanged 事件，让先于 Setup 启用的组件刷新。
        /// </summary>
        /// <param name="fallbackLanguage">缺失翻译时的回退语言，默认 EnglishUS。</param>
        public static void Setup(LanguageCode fallbackLanguage = LanguageCode.EnglishUS)
        {
            bool success = false;
            try
            {
                _fallbackLanguage = fallbackLanguage;
                LoadRowsFromConfig();
                LoadLanguageSettings();

#if TMP_PRESENT
                _fontConfig = UnityEngine.Resources.Load<LocalizationConfig>(FontConfigResourcePath);
#endif
                RefreshCache();

                LogCore.Success(nameof(LocalizationCore), $"初始化成功: Language={_currentLanguage}, Keys={_currentLangCache.Count}");
                success = true;
            }
            catch (Exception e)
            {
                LogCore.Error(nameof(LocalizationCore), $"初始化失败: {e.Message}");
            }

            // 仅成功时补发事件：失败时缓存为空，补发会让所有组件刷出满屏 #key#；
            // 保留旧显示（可能为上一会话缓存）等待下次 Setup 修复
            if (success)
                OnLanguageChanged?.Invoke();
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
            string fallbackName = _fallbackLanguage.ToString();

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
        /// 注意：不清空 OnLanguageChanged 订阅——已激活组件（LocalizationComponent）不会重新 OnEnable，
        /// 置空事件会让它们永久失联；重新 Setup 后补发事件即可让既有订阅者刷新。
        /// </summary>
        public static void Close()
        {
            _currentLangCache.Clear();
            _rawRows.Clear();
            _fieldCache.Clear();
#if TMP_PRESENT
            _fontConfig = null;
#endif
        }
    }
}
