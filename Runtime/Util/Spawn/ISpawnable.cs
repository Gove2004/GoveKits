namespace GoveKits.Runtime.Util
{
    /// <summary>
    /// 用于 Spawnable 初始化的数据接口。
    /// 派生类可携带生成实体时所需的各种配置参数。
    /// </summary>
    public interface ISpawnData { }

    /// <summary>
    /// 可被 SpawnCore 统一管理生命周期的实体接口。
    /// 每个实体拥有唯一的 ObjectId 和关联的 SpawnKey。
    /// </summary>
    public interface ISpawnable
    {
        /// <summary>实体注册时使用的键名，用于查找对应的工厂和销毁函数。</summary>
        string SpawnKey { get; }
        /// <summary>实体的唯一对象 ID，全局递增。</summary>
        uint ObjectId { get; }
    }
}