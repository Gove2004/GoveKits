namespace GoveKits.Runtime.Storage
{
    /// <summary>
    /// 本地化行数据基类，通过 ConfigCore 加载配置表数据。
    /// 每个子类代表一行多语言翻译数据，Key 字段为本地化键名。
    /// </summary>
    public abstract class ILocalizationConfigData : IConfigData
    {
        public string Key;
    }
}
