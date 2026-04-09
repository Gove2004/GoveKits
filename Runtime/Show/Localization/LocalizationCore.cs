using System;
using System.Collections.Generic;
using UnityEngine;
using GoveKits.Runtime.Core;
using GoveKits.Runtime.Storage.Config;
using Cysharp.Threading.Tasks;

namespace GoveKits.Runtime.Util
{
    /// <summary>
    /// 本地化行数据。通过 ConfigCore 自动加载。
    /// </summary>
    [Config("Config/Localization", ConfigFileType.Json, ConfigSourceType.Resources)]
    public class LocalizationTextRow : IConfigData
    {
        public string Key;
        public string ChineseCN;
        public string EnglishUS;
        public string Japanese;
        public string Korean;
    }

    public static class LocalizationCore
    {
        private const string LanguagePrefKey = "Localization.Language";
        private const string FontConfigResourcePath = "Config/LocalizationConfig";

        private static readonly Dictionary<string, LocalizationTextRow> RawRows = new Dictionary<string, LocalizationTextRow>();
        private static readonly Dictionary<string, string> CurrentLangCache = new Dictionary<string, string>();

        private static LocalizationConfig _fontConfig;
        private static bool _isInitialized;
        private static LanguageCode _currentLanguage = LanguageCode.ChineseCN;

        public static event Action OnLanguageChanged;
        public static bool IsInitialized => _isInitialized;
        public static LanguageCode CurrentLanguage => _currentLanguage;

        /// <summary>
        /// 初始化本地化系统。
        /// </summary>
        public static void Initialize()
        {
            if (_isInitialized)
            {
                return;
            }

            try
            {
                EnsureConfigReady();
                LoadRowsFromConfig();
                LoadLanguageSettings();

                _fontConfig = Resources.Load<LocalizationConfig>(FontConfigResourcePath);
                RefreshCache();

                _isInitialized = true;
                LogCore.LogGreen("LocalizationCore", $"Initialized. Language={_currentLanguage}, Keys={CurrentLangCache.Count}");
            }
            catch (Exception e)
            {
                LogCore.LogError("LocalizationCore", $"Initialize failed: {e.Message}");
            }
        }

        /// <summary>
        /// 切换当前语言。
        /// </summary>
        public static void SwitchLanguage(LanguageCode code)
        {
            if (_isInitialized == false)
            {
                Initialize();
            }

            if (_currentLanguage == code)
            {
                return;
            }

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

            if (_isInitialized == false)
            {
                Initialize();
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

        private static void EnsureConfigReady()
        {
            if (ConfigCore.Initialized)
            {
                return;
            }

            ConfigCore.InitAsync().AsTask().GetAwaiter().GetResult();
        }

        private static void LoadRowsFromConfig()
        {
            RawRows.Clear();

            List<LocalizationTextRow> rows = ConfigCore.LoadAll<LocalizationTextRow>();
            for (int i = 0; i < rows.Count; i++)
            {
                LocalizationTextRow row = rows[i];
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
                LocalizationTextRow row = kvp.Value;

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

        private static string ReadLanguageField(LocalizationTextRow row, string fieldName)
        {
            var field = typeof(LocalizationTextRow).GetField(fieldName);
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
    }
}