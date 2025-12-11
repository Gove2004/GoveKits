using System;

namespace GoveKits.Binary
{
    // 基础接口
    public interface IBinaryData
    {
        /// <summary>
        /// 获取序列化后的总字节长度（包含自身内容的长度，不含 Tag/WireType）
        /// </summary>
        int Length();

        /// <summary>
        /// 写入数据到 Buffer
        /// </summary>
        void Writing(byte[] buffer, ref int index);

        /// <summary>
        /// 从 Buffer 读取数据
        /// </summary>
        /// <param name="buffer">字节数组</param>
        /// <param name="index">当前索引</param>
        /// <param name="endPos">当前对象的结束索引（用于版本兼容跳过未知字段）</param>
        void Reading(byte[] buffer, ref int index, int endPos);
    }
}