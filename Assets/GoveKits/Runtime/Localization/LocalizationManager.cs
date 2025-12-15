using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Newtonsoft.Json;
using GoveKits;
using GoveKits.Res;
using GoveKits.Save; // 引用之前的资源管理器

namespace GoveKits.Localization
{
    public class LanguageManager
    {
        // 配置路径 (需放在 Resources/Config 或 AB/Config 下)
        private const string CONFIG_PATH = "Config/LocalizationConfig";
        private const string SavePath = "LocalizationSetting";

        // 运行时状态
        private static LocalizationSetting _setting = new LocalizationSetting();
        private static LocalizationConfig _config;
        private static bool _isInitialized = false;

        // 原始数据: Key -> (LangCodeString -> Text)
        private static Dictionary<string, Dictionary<string, string>> _rawData;
        
        // 优化缓存: Key -> CurrentText (只存当前语言的文本，提升查找速度)
        private static Dictionary<string, string> _currentLangCache = new Dictionary<string, string>();

        // 事件
        public static event Action OnLanguageChanged;

        /// <summary>
        /// 初始化 (建议在游戏启动时调用)
        /// </summary>
        public static void Initialize()
        {
            if (_isInitialized) return;

            // 1. 加载配置 SO
            _config = ResManager.Load<LocalizationConfig>(CONFIG_PATH);
            if (_config == null)
            {
                LogManager.LogError("LanguageManager", $"找不到配置: {CONFIG_PATH}");
                return;
            }

            // 2. 解析 JSON
            ParseJson();

            // 3. 加载上次保存的语言
            LoadSettings();

            // 4. 构建当前语言缓存
            RefreshCache();

            _isInitialized = true;
            LogManager.Log("LanguageManager", $"初始化完成. 当前语言: {_setting.CurrentLanguage}");
        }

        /// <summary>
        /// 切换语言
        /// </summary>
        public static void SwitchLanguage(LanguageCode code)
        {
            if ((LanguageCode)_setting.CurrentLanguage == code && _isInitialized) return;

            _setting.CurrentLanguage = (int)code;
            
            // 保存设置
            SaveSettings();

            // 重建缓存并通知 UI 更新
            RefreshCache();
            OnLanguageChanged?.Invoke();
        }

        /// <summary>
        /// 获取当前语言的文本 (O(1) 复杂度)
        /// </summary>
        public static string GetText(string key)
        {
            if (!_isInitialized) return key;
            
            if (_currentLangCache.TryGetValue(key, out string result))
            {
                return result;
            }
            
            return $"#{key}#"; // 缺失提示
        }

        /// <summary>
        /// 获取当前语言的字体
        /// </summary>
        public static TMP_FontAsset GetCurrentFont()
        {
            if (_config == null) return null;
            return _config.GetFont((LanguageCode)_setting.CurrentLanguage);
        }

        // --- 内部逻辑 ---

        private static void ParseJson()
        {
            if (_config.LanguageJson == null) return;
            try
            {
                _rawData = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, string>>>(_config.LanguageJson.text);
            }
            catch (Exception e)
            {
                LogManager.LogError("LanguageManager", $"JSON 解析失败: {e.Message}");
            }
        }

        private static void RefreshCache()
        {
            if (_rawData == null) return;

            _currentLangCache.Clear();
            string langKey = _setting.CurrentLanguage.ToString();

            // 扁平化处理：将嵌套字典转为单层字典
            foreach (var kvp in _rawData)
            {
                string key = kvp.Key;
                var dict = kvp.Value;

                if (dict.TryGetValue(langKey, out string content))
                {
                    _currentLangCache[key] = content;
                }
                else
                {
                    // 如果当前语言缺失该Key，尝试回退到英文 (可选逻辑)
                    if (dict.TryGetValue(LanguageCode.EnglishUS.ToString(), out string fallback))
                    {
                        _currentLangCache[key] = fallback;
                    }
                }
            }
        }

        public static void SaveSettings()
        {
            SaveManager.SaveData(_setting, SavePath);
        }

        public static void LoadSettings()
        {
            if (SaveManager.TryLoadData<LocalizationSetting>(SavePath, out var data))
            {
                _setting = data;


            }
        }
    }
}