using System;
using GoveKits.Runtime.Core;

namespace GoveKits.Runtime.Storage
{
    /// <summary>
    /// 字符串国际化扩展方法集合，为 string 类型提供便捷的本地化查询 API。
    /// </summary>
    public static class StringExtension
    {
        /// <summary>
        /// 将字符串作为本地化键名，查询当前语言对应的翻译文本。
        /// </summary>
        /// <param name="key">本地化键名。</param>
        /// <returns>当前语言对应的翻译文本，键不存在时返回 "#key#" 占位符。</returns>
        public static string I18n(this string key)
        {
            return LocalizationCore.GetText(key);
        }
    }
}
