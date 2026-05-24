


namespace GoveKits.Runtime.Storage
{
    /// <summary>
    /// 本地化行数据。通过 ConfigCore 自动加载。
    /// </summary>
    public class ILocalizationConfigData : IConfigData
    {
        public string Key;
    }

    // [ConfigPath("Localization")]
    // public class MyLocalizationConfigData : ILocalizationConfigData
    // {
    //     public string ChineseCN;
    //     public string EnglishUS;
    //     public string JapaneseJP;
    // }
}