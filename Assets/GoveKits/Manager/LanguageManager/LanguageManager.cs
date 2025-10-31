using UnityEngine;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;


namespace GoveKits.Manager
{
    /// <summary>
    /// 语言管理器，负责多语言文本的加载、切换和字体适配
    /// 缓存所有语言文本数据，避免重复访问ConfigManager
    /// </summary>
    public class LanguageManager : MonoSingleton<LanguageManager>
    {
        [Header("基础配置")]
        [SerializeField] private string _configFileName = "language";

        [Header("字体配置")]
        [SerializeField] private List<LanguageFont> _fontSettings = new List<LanguageFont>();

        private LanguageCode[] _allLanguageCodes;
        private int _currentIndex = 0;
        private bool _isInitialized = false;

        /// <summary>语言文本缓存，key为语言代码，value为该语言的所有文本字典</summary>
        private readonly Dictionary<LanguageCode, Dictionary<string, string>> _languageTextCache =
            new Dictionary<LanguageCode, Dictionary<string, string>>();

        /// <summary>语言切换事件</summary>
        public event System.Action<LanguageCode> OnLanguageSwitched;

        [System.Serializable]
        public class LanguageFont
        {
            public LanguageCode languageCode;
            public TMP_FontAsset fontAsset;
        }

        protected void Awake()
        {
            ConfigManager.Instance.OnConfigAllLoaded += Initialize;
        }

        private void Initialize()
        {
            try
            {
                if (!ConfigManager.Instance.HasConfig(_configFileName))
                {
                    Debug.LogError($"[LanguageManager] 语言配置文件不存在: {_configFileName}");
                    SetDefaultState();
                    return;
                }

                LoadSupportedLanguages();
                LoadAllLanguageTexts();
                LoadSavedLanguageSettings();

                _isInitialized = true;
                Debug.Log($"[LanguageManager] 支持的语言: [{string.Join(", ", _allLanguageCodes.Select(x => x.ToLanguageString()))}] "
                        + $" 当前语言: {GetCurrentLanguageCode()}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[LanguageManager] 初始化失败: {e.Message}");
                SetDefaultState();
            }
        }

        private void SetDefaultState()
        {
            _allLanguageCodes = new LanguageCode[] { LanguageCode.ChineseCN };
            _currentIndex = 0;
            _isInitialized = true;
        }

        /// <summary>
        /// 从字体设置中加载支持的语言列表
        /// </summary>
        private void LoadSupportedLanguages()
        {
            if (_fontSettings != null && _fontSettings.Count > 0)
            {
                _allLanguageCodes = _fontSettings.Select(x => x.languageCode).Distinct().ToArray();
            }
            else
            {
                // 如果没有字体设置，使用默认的中文
                _allLanguageCodes = new LanguageCode[] { LanguageCode.ChineseCN };
                Debug.LogWarning("[LanguageManager] 没有字体设置，使用默认中文");
            }
        }

        /// <summary>
        /// 加载所有语言的文本数据并缓存
        /// </summary>
        private void LoadAllLanguageTexts()
        {
            _languageTextCache.Clear();

            foreach (LanguageCode langCode in _allLanguageCodes)
            {
                try
                {
                    string langString = langCode.ToLanguageString();
                    string path = $"{_configFileName}.{langString}";

                    JObject langData = ConfigManager.Instance.GetConfig<JObject>(path);

                    if (langData != null)
                    {
                        var textDict = new Dictionary<string, string>();
                        CacheLanguageTexts(langData, textDict, "");
                        _languageTextCache[langCode] = textDict;
                    }
                    else
                    {
                        Debug.LogWarning($"[LanguageManager] 语言 {langString} 的配置数据为空");
                        _languageTextCache[langCode] = new Dictionary<string, string>();
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[LanguageManager] 加载语言 {langCode.ToLanguageString()} 失败: {e.Message}");
                    _languageTextCache[langCode] = new Dictionary<string, string>();
                }
            }
        }

        /// <summary>
        /// 递归缓存语言文本数据
        /// </summary>
        /// <param name="jObject">JSON对象</param>
        /// <param name="textDict">文本字典</param>
        /// <param name="prefix">前缀路径</param>
        private void CacheLanguageTexts(JObject jObject, Dictionary<string, string> textDict, string prefix)
        {
            foreach (var property in jObject.Properties())
            {
                string key = string.IsNullOrEmpty(prefix) ? property.Name : $"{prefix}.{property.Name}";

                if (property.Value.Type == JTokenType.Object)
                {
                    // 递归处理嵌套对象
                    CacheLanguageTexts((JObject)property.Value, textDict, key);
                }
                else if (property.Value.Type == JTokenType.Array)
                {
                    // 处理数组
                    JArray array = (JArray)property.Value;
                    for (int i = 0; i < array.Count; i++)
                    {
                        string arrayKey = $"{key}[{i}]";
                        if (array[i].Type == JTokenType.String)
                        {
                            textDict[arrayKey] = array[i].ToString();
                        }
                        else if (array[i].Type == JTokenType.Object)
                        {
                            CacheLanguageTexts((JObject)array[i], textDict, arrayKey);
                        }
                    }
                }
                else
                {
                    // 直接存储字符串值
                    textDict[key] = property.Value.ToString();
                }
            }
        }

        private void LoadSavedLanguageSettings()
        {
            if (_allLanguageCodes == null || _allLanguageCodes.Length == 0)
            {
                _currentIndex = 0;
                return;
            }

            string savedLanguageString = PlayerPrefs.GetString("CurrentLanguage", _allLanguageCodes[0].ToLanguageString());
            LanguageCode savedLanguage = savedLanguageString.ToLanguageCode();
            int savedIndex = System.Array.IndexOf(_allLanguageCodes, savedLanguage);

            if (savedIndex >= 0)
            {
                _currentIndex = savedIndex;
            }
            else
            {
                _currentIndex = 0; // 使用第一个语言
            }
        }

        /// <summary>
        /// 获取指定key的多语言文本
        /// </summary>
        /// <param name="key">文本key</param>
        /// <returns>对应文本内容</returns>
        public string GetText(string key)
        {
            if (!_isInitialized)
            {
                return $"[NOREADY:{key}]";
            }

            if (string.IsNullOrEmpty(key))
            {
                return $"[NOKEY:{key}]";
            }

            if (_allLanguageCodes == null || _allLanguageCodes.Length == 0)
            {
                return $"[NOCODE:{key}]";
            }

            try
            {
                LanguageCode currentLang = _allLanguageCodes[_currentIndex];

                // 从缓存中获取当前语言的文本
                if (_languageTextCache.TryGetValue(currentLang, out Dictionary<string, string> currentLangTexts))
                {
                    if (currentLangTexts.TryGetValue(key, out string text))
                    {
                        return text;
                    }
                }

                Debug.LogWarning($"[LanguageManager] 未找到当前语言 '{currentLang.ToLanguageString()}' 的文本: {key}");

                // 尝试第一个语言作为回退
                if (_currentIndex != 0 && _allLanguageCodes.Length > 0)
                {
                    LanguageCode firstLang = _allLanguageCodes[0];
                    if (_languageTextCache.TryGetValue(firstLang, out Dictionary<string, string> firstLangTexts))
                    {
                        if (firstLangTexts.TryGetValue(key, out string fallbackText))
                        {
                            return fallbackText;
                        }
                    }
                }

                return $"[MISS:{key}]";
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[LanguageManager] 获取文本失败 '{key}': {e.Message}");
                return $"[ERROR:{key}]";
            }
        }

        /// <summary>
        /// 获取当前语言对应的字体
        /// </summary>
        public TMP_FontAsset GetCurrentLanguageFont()
        {
            if (!_isInitialized || _allLanguageCodes == null || _allLanguageCodes.Length == 0)
            {
                return null;
            }

            LanguageCode currentLang = _allLanguageCodes[_currentIndex];
            var setting = _fontSettings?.Find(x => x.languageCode == currentLang);

            return setting?.fontAsset;
        }

        /// <summary>
        /// 切换语言
        /// </summary>
        /// <param name="languageCode">目标语言代码，为null时循环切换</param>
        /// <returns>是否切换成功</returns>
        public bool SwitchLanguage(LanguageCode? languageCode = null)
        {
            if (!_isInitialized)
            {
                Debug.LogWarning("[LanguageManager] 尚未初始化完成，无法切换语言");
                return false;
            }

            if (_allLanguageCodes == null || _allLanguageCodes.Length == 0)
            {
                Debug.LogWarning("[LanguageManager] 没有可用的语言");
                return false;
            }

            int oldIndex = _currentIndex;

            if (languageCode == null)
            {
                _currentIndex = (_currentIndex + 1) % _allLanguageCodes.Length;
            }
            else
            {
                int index = System.Array.IndexOf(_allLanguageCodes, languageCode.Value);
                if (index >= 0)
                {
                    _currentIndex = index;
                }
                else
                {
                    Debug.LogWarning($"[LanguageManager] 不支持的语言代码: '{languageCode.Value.ToLanguageString()}'");
                    return false;
                }
            }

            SaveLanguageSettings();

            if (oldIndex != _currentIndex)
            {
                OnLanguageSwitched?.Invoke(_allLanguageCodes[_currentIndex]);
                Debug.Log($"[LanguageManager] 切换语言: {_allLanguageCodes[oldIndex].ToLanguageString()} => {_allLanguageCodes[_currentIndex].ToLanguageString()}");
                return true;
            }

            return false;
        }

        /// <summary>
        /// 切换语言（字符串重载，向后兼容）
        /// </summary>
        public bool SwitchLanguage(string languageCode)
        {
            if (string.IsNullOrEmpty(languageCode))
            {
                return SwitchLanguage((LanguageCode?)null);
            }

            LanguageCode enumCode = languageCode.ToLanguageCode();
            return SwitchLanguage(enumCode);
        }

        private void SaveLanguageSettings()
        {
            if (_allLanguageCodes != null && _currentIndex >= 0 && _currentIndex < _allLanguageCodes.Length)
            {
                PlayerPrefs.SetString("CurrentLanguage", _allLanguageCodes[_currentIndex].ToLanguageString());
                PlayerPrefs.Save();
            }
        }

        /// <summary>获取当前语言代码</summary>
        public LanguageCode GetCurrentLanguage()
        {
            return _isInitialized && _allLanguageCodes != null && _allLanguageCodes.Length > 0
                ? _allLanguageCodes[_currentIndex]
                : LanguageCode.ChineseCN;
        }

        /// <summary>获取当前语言代码字符串</summary>
        public string GetCurrentLanguageCode()
        {
            return GetCurrentLanguage().ToLanguageString();
        }

        /// <summary>获取所有支持的语言代码</summary>
        public LanguageCode[] GetAllLanguages()
        {
            return _allLanguageCodes ?? new LanguageCode[] { LanguageCode.ChineseCN };
        }

        /// <summary>获取所有支持的语言代码字符串</summary>
        public string[] GetAllLanguageCodes()
        {
            return GetAllLanguages().Select(x => x.ToLanguageString()).ToArray();
        }

        /// <summary>是否已初始化完成</summary>
        public bool IsInitialized()
        {
            return _isInitialized;
        }

        /// <summary>
        /// 获取格式化文本
        /// </summary>
        public string GetTextFormatted(string key, params object[] args)
        {
            string text = GetText(key);

            if (args != null && args.Length > 0)
            {
                try
                {
                    return string.Format(text, args);
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[LanguageManager] 格式化文本失败 '{key}': {e.Message}");
                    return text;
                }
            }

            return text;
        }

        /// <summary>
        /// 批量获取文本
        /// </summary>
        public Dictionary<string, string> GetTexts(params string[] keys)
        {
            var result = new Dictionary<string, string>();

            if (keys != null)
            {
                foreach (string key in keys)
                {
                    result[key] = GetText(key);
                }
            }

            return result;
        }

        /// <summary>
        /// 重新加载语言配置
        /// </summary>
        public void ReloadLanguageConfig()
        {
            try
            {
                // 重新加载配置
                ConfigManager.Instance.ReloadConfig(_configFileName);

                // 清空缓存并重新初始化
                _languageTextCache.Clear();
                _isInitialized = false;
                Initialize();

                OnLanguageSwitched?.Invoke(GetCurrentLanguage());
                Debug.Log("[LanguageManager] 语言配置重新加载完成");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[LanguageManager] 重新加载配置失败: {e.Message}");
            }
        }

        /// <summary>
        /// 获取缓存统计信息
        /// </summary>
        public string GetCacheInfo()
        {
            if (!_isInitialized)
                return "未初始化";

            int totalTexts = _languageTextCache.Values.Sum(dict => dict.Count);
            return $"已缓存 {_languageTextCache.Count} 种语言，共 {totalTexts} 条文本";
        }
    }
}