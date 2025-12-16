namespace GoveKits.Save
{
    /// <summary>
    /// 接口，表示一个可保存和加载状态的对象。
    /// </summary>
    public interface ISaveable
    {
        void Save();
        void Load();
    }
}