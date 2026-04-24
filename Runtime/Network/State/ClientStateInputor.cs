using System;

namespace GoveKits.Runtime.Network
{
    public interface IClientStateInputor
    {
        int PlayerId { get; }
        void Update(float deltaTime);
    }

    /// <summary>
    /// 客户端输入提交器基类
    /// </summary>
    public abstract class ClientStateInputor<T> : IClientStateInputor where T : IProtocolMessage, new()
    {
        public int PlayerId { get; private set; }
        public float SubmitInterval { get; set; } // 客户端发包频率
        
        private float _accumulateTime = 0f;
        protected T PendingInput = new T();

        public ClientStateInputor(int playerId, float submitInterval = 0.033f)
        {
            PlayerId = playerId;
            SubmitInterval = submitInterval;
        }

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
                StateCore.SendStateInput(PlayerId, ProtocolCore.Serialize(PendingInput));

                OnAfterSubmit(); // 触发清理逻辑
                
                _accumulateTime -= SubmitInterval;
            }
        }

        /// <summary>
        /// 子类实现：发包后的清理工作（如清除一次性开火标记）
        /// </summary>
        protected abstract void OnAfterSubmit();
    }
}