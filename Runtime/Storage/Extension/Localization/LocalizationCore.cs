using System;
using System.Collections.Generic;
using UnityEngine;
using GoveKits.Runtime.Core;
using Cysharp.Threading.Tasks;
using TMPro;

namespace GoveKits.Runtime.Storage
{
    public static class LocalizationCore
    {
        private const string LanguagePrefKey = "Localization.Language";
        private const string FontConfigResourcePath = "Config/LocalizationConfig";

        private static Dictionary<string, ILocalizationConfigData> RawRows = new Dictionary<string, ILocalizationConfigData>();
        private static Dictionary<string, string> CurrentLangCache = new Dictionary<string, string>();

        private static LocalizationConfig _fontConfig;
        private static LanguageCode _currentLanguage = LanguageCode.ChineseCN;

        public static event Action OnLanguageChanged;
        public static LanguageCode CurrentLanguage => _currentLanguage;


        public static void Initialize()
        {
            try
            {
                LoadRowsFromConfig();
                LoadLanguageSettings();

                _fontConfig = Resources.Load<LocalizationConfig>(FontConfigResourcePath);
                RefreshCache();

                LogCore.Success(nameof(LocalizationCore), $"初始化成功: Language={_currentLanguage}, Keys={CurrentLangCache.Count}");
            }
            catch (Exception e)
            {
                LogCore.Error(nameof(LocalizationCore), $"初始化失败: {e.Message}");
            }
        }



        /// <summary>
        /// 切换当前语言。
        /// </summary>
        public static void SwitchLanguage(LanguageCode code)
        {
            if (_currentLanguage == code) return;

            _currentLanguage = code;
            SaveLanguageSettings();

            RefreshCache();
            OnLanguageChanged?.Invoke();
        }

        /// <summary>
        /// 获取本地化文本。命中缓存时为 O(1)。
        /// </summary>
        public static string GetText(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return string.Empty;
            }

            if (CurrentLangCache.TryGetValue(key, out string result))
            {
                return result;
            }

            return $"#{key}#";
        }

#if TMP_PRESENT
        /// <summary>
        /// 获取当前语言字体。若未配置则返回 null。
        /// </summary>
        public static TMP_FontAsset GetCurrentFont()
        {
            if (_fontConfig == null)
            {
                return null;
            }

            return _fontConfig.GetFont(_currentLanguage);
        }
#endif

        private static void LoadRowsFromConfig()
        {
            RawRows.Clear();

            List<ILocalizationConfigData> rows = ConfigCore.LoadAll<ILocalizationConfigData>();
            for (int i = 0; i < rows.Count; i++)
            {
                ILocalizationConfigData row = rows[i];
                if (row == null || string.IsNullOrWhiteSpace(row.Key))
                {
                    continue;
                }

                RawRows[row.Key] = row;
            }
        }

        private static void RefreshCache()
        {
            CurrentLangCache.Clear();
            string langName = _currentLanguage.ToString();
            string fallbackName = LanguageCode.EnglishUS.ToString();

            foreach (var kvp in RawRows)
            {
                string key = kvp.Key;
                ILocalizationConfigData row = kvp.Value;

                string content = ReadLanguageField(row, langName);
                if (string.IsNullOrEmpty(content))
                {
                    content = ReadLanguageField(row, fallbackName);
                }

                if (string.IsNullOrEmpty(content) == false)
                {
                    CurrentLangCache[key] = content;
                }
            }
        }

        private static string ReadLanguageField(ILocalizationConfigData row, string fieldName)
        {
            var field = typeof(ILocalizationConfigData).GetField(fieldName);
            if (field == null)
            {
                return null;
            }

            object value = field.GetValue(row);
            return value as string;
        }

        private static void SaveLanguageSettings()
        {
            PlayerPrefs.SetInt(LanguagePrefKey, (int)_currentLanguage);
            PlayerPrefs.Save();
        }

        private static void LoadLanguageSettings()
        {
            int defaultCode = (int)LanguageCode.ChineseCN;
            int code = PlayerPrefs.GetInt(LanguagePrefKey, defaultCode);
            if (Enum.IsDefined(typeof(LanguageCode), code))
            {
                _currentLanguage = (LanguageCode)code;
            }
            else
            {
                _currentLanguage = LanguageCode.ChineseCN;
            }
        }

        public static void Clear()
        {
            SaveLanguageSettings();
        }
    }
}