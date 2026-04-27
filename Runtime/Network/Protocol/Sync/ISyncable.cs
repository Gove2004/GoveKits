

using System;

namespace GoveKits.Runtime.Network
{
    /// <summary>
    /// 可同步协议的接口，标记了这个接口的协议会被自动记录和同步
    /// </summary>
    public interface ISyncable
    {
        uint NetId { get; }
        bool IsDirty { get; set; }

        (Type, byte[]) GetState();
        void ApplySnap(byte[] payload);
        void ApplyLerp(byte[] fromPayload, byte[] toPayload, float t);
    }
}