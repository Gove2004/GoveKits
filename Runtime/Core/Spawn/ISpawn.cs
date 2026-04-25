using System;

namespace GoveKits.Runtime.Core
{
    /// <summary>
    /// 用于 ISpawnable 初始化的数据接口。
    /// （在网络层中，这可以是被反序列化出来的盲盒数据；在单机层中，它可以是任何类）
    /// </summary>
    public interface ISpawnData { }

    /// <summary>
    /// 表示可以被 SpawnCore 统一生命周期管理的对象。
    /// 可以挂载在 MonoBehaviour 上，也可以是纯 C# 类。
    /// </summary>
    public interface ISpawnable
    {
        string SpawnKey { get; }
        
        // 建议接口只留 get，具体赋值由业务类的初始化方法（或属性本身）完成
        uint ObjectId { get; set; } 
    }
}