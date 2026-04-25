using System;


namespace GoveKits.Runtime.Network
{
    public interface IInputSubmitor
    {
        int PlayerId { get; }
        public event Action<int, ushort, byte[]> OnSubmit; // playerId, protocolID, payload
        void Update(float deltaTime);
    }

    
    public abstract class InputSubmitor<T> : IInputSubmitor where T : IProtocolMessage, new()
    {
        public int PlayerId => ClientCore.PlayerId;
        public float SubmitInterval { get; private set; }

        private float _accumulateTime = 0f;
        protected T PendingInput = new T(); // 待发送的大包裹

        public event Action<int, ushort, byte[]> OnSubmit;

        public InputSubmitor(float submitInterval = 0.033f)
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
            if (PlayerId == 0) return; // 还未连接服务器或者本地玩家还未准备好，不提交输入

            _accumulateTime += deltaTime;
            if (_accumulateTime >= SubmitInterval)
            {
                // 序列化当前包裹并发送
                OnSubmit?.Invoke(PlayerId, ProtocolCore.GetId<T>(), ProtocolCore.Serialize(PendingInput));
                
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
