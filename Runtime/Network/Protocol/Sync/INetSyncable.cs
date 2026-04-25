

namespace GoveKits.Runtime.Network
{
    /// <summary>
    /// 可同步协议的接口，标记了这个接口的协议会被自动记录和同步
    /// </summary>
    public interface INetSyncable : IProtocolMessage
    {
        // 目前不需要额外成员，纯标记接口
    }
}