using GoveKits.Runtime.Storage;


namespace GoveKits.Runtime.Util
{
    public static class StringExtension
    {
        /// <summary>
        /// 国际化文本获取
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public static string I18n(this string key)
        {
            return LocalizationCore.GetText(key);
        }
    }
}