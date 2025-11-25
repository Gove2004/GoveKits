
using System;
using System.Text;


namespace GoveKits.Network
{
    // RPC消息
    [Message(Protocol.RpcID)]
    public class RPCMessage : Message
    {
        public int NetID;          // 目标对象的网络ID
        public string MethodName;     // 方法哈希值
        public object[] Parameters;  // 方法参数
        

        // 构造函数
        public RPCMessage() { }
        public RPCMessage(int netID, string methodName, object[] parameters)
        {
            NetID = netID;
            MethodName = methodName;
            Parameters = parameters;
        }



        protected override int BodyLength()
        {
            int length = 4 + 4 + Encoding.UTF8.GetByteCount(MethodName) + 1; // NetID + MethodName + Args Count (byte)
            // 计算参数长度
            if (Parameters != null)
            {
                foreach (var param in Parameters)
                {
                    length += ArgExtensions.GetArgLength(param);
                }
            }
            return length;
        }
        protected override void BodyWriting(byte[] buffer, ref int index)
        {
            WriteInt(buffer, NetID, ref index);        // 先写 NetID
            WriteString(buffer, MethodName, ref index); // 再写 MethodHash
            // 写入参数
            byte argCount = (byte)(Parameters?.Length ?? 0);
            WriteByte(buffer, argCount, ref index);     // 写入参数数量
            if (Parameters != null)
            {
                foreach (var param in Parameters)
                {
                    ArgExtensions.WriteArg(buffer, param, ref index);
                }
            }
        }
        protected override void BodyReading(byte[] buffer, ref int index)
        {
            NetID = ReadInt(buffer, ref index);        // 先读 NetID
            MethodName = ReadString(buffer, ref index); // 再读 MethodHash
            // 读取参数
            byte argCount = ReadByte(buffer, ref index);
            Parameters = new object[argCount];
            for (int i = 0; i < argCount; i++)
            {
                Parameters[i] = ArgExtensions.ReadArg(buffer, ref index);
            }
        }
    }


    

    // 标记哪些方法可以被 RPC 调用
    [AttributeUsage(AttributeTargets.Method)]
    public class RpcAttribute : Attribute { }


    // 配合使用的枚举
    public enum RpcTarget { Server, All, Others }
}