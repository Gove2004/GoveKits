


namespace GoveKits.Runtime.Storage
{
    /// <summary>
    /// 本地化行数据。通过 ConfigCore 自动加载。
    /// </summary>
    [ConfigPath("Config/Localization.csv")]
    public class LocalizationConfigData : IConfigData
    {
        public string Key;
        public string ChineseCN;
        public string EnglishUS;
    }
}