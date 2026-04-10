using System;
using System.Collections.Generic;
using UnityEngine;
using GoveKits.Runtime.Core;
using Cysharp.Threading.Tasks;

namespace GoveKits.Runtime.Storage
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

    public class LocalizationCore : ICore
    {
        private const string LanguagePrefKey = "Localization.Language";
        private const string FontConfigResourcePath = "Config/LocalizationConfig";

        private readonly Dictionary<string, LocalizationTextRow> RawRows = new Dictionary<string, LocalizationTextRow>();
        private readonly Dictionary<string, string> CurrentLangCache = new Dictionary<string, string>();

        private LocalizationConfig _fontConfig;
        private bool _isInitialized;
        private LanguageCode _currentLanguage = LanguageCode.ChineseCN;

        public event Action OnLanguageChanged;
        public bool IsInitialized => _isInitialized;
        public LanguageCode CurrentLanguage => _currentLanguage;

        private ConfigCore configCore => CoreLocator.GetCore<ConfigCore>();

        /// <summary>
        /// 初始化本地化系统。
        /// </summary>
        public void Initialize()
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
                CoreLocator.Log.Success(nameof(LocalizationCore), $"Initialized. Language={_currentLanguage}, Keys={CurrentLangCache.Count}");
            }
            catch (Exception e)
            {
                CoreLocator.Log.Success(nameof(LocalizationCore), $"Initialize failed: {e.Message}");
            }
        }

        /// <summary>
        /// 切换当前语言。
        /// </summary>
        public void SwitchLanguage(LanguageCode code)
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
        public string GetText(string key)
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
        public TMP_FontAsset GetCurrentFont()
        {
            if (_fontConfig == null)
            {
                return null;
            }

            return _fontConfig.GetFont(_currentLanguage);
        }
#endif

        private void EnsureConfigReady()
        {
            if (configCore.Initialized)
            {
                return;
            }

            configCore.InitAsync().AsTask().GetAwaiter().GetResult();
        }

        private void LoadRowsFromConfig()
        {
            RawRows.Clear();

            List<LocalizationTextRow> rows = configCore.LoadAll<LocalizationTextRow>();
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

        private void RefreshCache()
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

        private string ReadLanguageField(LocalizationTextRow row, string fieldName)
        {
            var field = typeof(LocalizationTextRow).GetField(fieldName);
            if (field == null)
            {
                return null;
            }

            object value = field.GetValue(row);
            return value as string;
        }

        private void SaveLanguageSettings()
        {
            PlayerPrefs.SetInt(LanguagePrefKey, (int)_currentLanguage);
            PlayerPrefs.Save();
        }

        private void LoadLanguageSettings()
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

        public void OnShutdown()
        {
            SaveLanguageSettings();
        }
    }
}