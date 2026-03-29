namespace GoveKits.Runtime.Storage.Save
{
    /// <summary>
    /// 存档数据接口。
    /// <para>一个实例对应一个物理存档路径。</para>
    /// </summary>
    /// <typeparam name="T">存档结构类型。</typeparam>
    public interface ISaveData<T>
    {
        /// <summary>
        /// 存档相对路径（不含根目录）。
        /// </summary>
        string RelativePath { get; }

        /// <summary>
        /// 导出待写入的存档数据。
        /// </summary>
        T Save();

        /// <summary>
        /// 将加载到的存档数据应用到运行时对象。
        /// </summary>
        /// <param name="state">反序列化后的存档数据。</param>
        void Load(T state);
    }
}