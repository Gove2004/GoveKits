namespace GoveKits.Save
{
    /// <summary>
    /// 可保存接口：定义对象的存档和加载行为。
    /// <para>实现者应在 Save() 中序列化状态，在 Load() 中反序列化并恢复状态。</para>
    /// <para>通常与 SaveManager + Protobuf 配合使用。</para>
    /// </summary>
    public interface ISaveable
    {
        /// <summary>
        /// 保存对象状态。
        /// </summary>
        void Save();
        
        /// <summary>
        /// 加载对象状态。
        /// </summary>
        void Load();
    }
}