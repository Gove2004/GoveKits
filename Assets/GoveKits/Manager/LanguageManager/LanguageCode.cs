using System.Collections.Generic;
using System.Linq;


namespace GoveKits.Manager
{
    /// <summary>
    /// 语言代码枚举
    /// </summary>
    public enum LanguageCode
    {
        ChineseCN,  // zh-cn
        English,    // en
        Japanese,   // ja
        Korean,     // ko
        ChineseTW,  // zh-tw
        French,     // fr
        German,     // de
        Spanish,    // es
        Russian,    // ru
        Portuguese, // pt
    }


    /// <summary>
    /// 语言代码扩展方法
    /// </summary>
    public static class LanguageCodeExtensions
    {
        private static readonly Dictionary<LanguageCode, string> LanguageCodeMap = new Dictionary<LanguageCode, string>
        {
            { LanguageCode.ChineseCN, "zh-cn" },
            { LanguageCode.English, "en" },
            { LanguageCode.Japanese, "ja" },
            { LanguageCode.Korean, "ko" },
            { LanguageCode.ChineseTW, "zh-tw" },
            { LanguageCode.French, "fr" },
            { LanguageCode.German, "de" },
            { LanguageCode.Spanish, "es" },
            { LanguageCode.Russian, "ru" },
            { LanguageCode.Portuguese, "pt" },
        };

        private static readonly Dictionary<string, LanguageCode> StringToLanguageCodeMap =
            LanguageCodeMap.ToDictionary(kvp => kvp.Value, kvp => kvp.Key);

        /// <summary>
        /// 将枚举转换为语言代码字符串
        /// </summary>
        public static string ToLanguageString(this LanguageCode languageCode)
        {
            return LanguageCodeMap.TryGetValue(languageCode, out string result) ? result : "zh-cn";
        }

        /// <summary>
        /// 将语言代码字符串转换为枚举
        /// </summary>
        public static LanguageCode ToLanguageCode(this string languageString)
        {
            return StringToLanguageCodeMap.TryGetValue(languageString, out LanguageCode result) ? result : LanguageCode.ChineseCN;
        }

        /// <summary>
        /// 获取所有支持的语言代码字符串
        /// </summary>
        public static string[] GetAllLanguageStrings()
        {
            return LanguageCodeMap.Values.ToArray();
        }
    }
}

