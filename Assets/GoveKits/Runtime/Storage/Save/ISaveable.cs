


using GoveKits.Binary;

namespace GoveKits.Save
{
    /// <summary>
    /// 接口，表示一个可保存和加载状态的对象。
    /// 一般在其中调用 SaveManager 进行保存和加载操作。
    /// </summary>
    public interface ISaveable
    {
        void Save();
        void Load();
    }
}