using System;

namespace GoveKits.Runtime.Network
{
    /// <summary>
    /// 可同步协议的接口。
    /// 实现此接口的实体可以通过帧同步系统进行网络同步。
    /// </summary>
    public interface ISyncable
    {
        /// <summary>
        /// 实体的网络唯一标识符。
        /// </summary>
        uint NetId { get; }

        /// <summary>
        /// 实体状态是否已变更（脏标记），true 表示需要同步到客户端。
        /// </summary>
        bool IsDirty { get; set; }

        /// <summary>
        /// 获取当前状态快照，返回数据类型和序列化后的字节负载。
        /// </summary>
        /// <returns>包含数据类型和字节负载的元组。</returns>
        (Type DataType, byte[] Payload) GetState();

        /// <summary>
        /// 应用来自服务器的状态快照，直接覆盖当前状态。
        /// </summary>
        /// <param name="payload">序列化后的状态字节数据。</param>
        void ApplySnap(byte[] payload);

        /// <summary>
        /// 在两个状态之间进行线性插值，用于客户端平滑过渡。
        /// </summary>
        /// <param name="fromPayload">起始状态的字节数据。</param>
        /// <param name="toPayload">目标状态的字节数据。</param>
        /// <param name="t">插值系数，范围 0~1。</param>
        void ApplyLerp(byte[] fromPayload, byte[] toPayload, float t);
    }
}
