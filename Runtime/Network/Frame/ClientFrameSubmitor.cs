using System;

namespace GoveKits.Runtime.Network
{
    // 供 FrameCore 统一调用的非泛型接口
    public interface IClientSubmitor
    {
        int PlayerId { get; }
        void Update(float deltaTime);
    }

    /// <summary>
    /// 客户端帧提交器基类
    /// </summary>
    public abstract class ClientFrameSubmitor<T> : IClientSubmitor where T : IProtocolMessage, new()
    {
        public int PlayerId => ClientCore.PlayerId;
        public float SubmitInterval { get; set; }
        
        private float _accumulateTime = 0f;
        protected T PendingInput = new T(); // 待发送的大包裹

        public ClientFrameSubmitor(float submitInterval = 0.033f)
        {
            SubmitInterval = submitInterval;
        }

        /// <summary>
        /// 代理方法：供分布式系统局部修改大包裹的数据
        /// </summary>
        public void ModifyInput(Action<T> modifier)
        {
            modifier?.Invoke(PendingInput);
        }

        public void Update(float deltaTime)
        {
            _accumulateTime += deltaTime;
            if (_accumulateTime >= SubmitInterval)
            {
                // 序列化当前包裹并发送
                FrameCore.SendInput(PlayerId, ProtocolCore.Serialize(PendingInput));
                
                OnAfterSubmit(); // 触发清理逻辑
                
                _accumulateTime -= SubmitInterval;
            }
        }

        /// <summary>
        /// 子类实现：发包后的清理工作（如清除开火标记）
        /// </summary>
        protected abstract void OnAfterSubmit();
    }
}