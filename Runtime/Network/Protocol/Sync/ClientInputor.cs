using System;


namespace GoveKits.Runtime.Network
{
    public interface IInputData
    {
        void Clear(); // 自动清除瞬时状态
    }


    internal class ClientInputor
    {
        private float SubmitInterval;
        private float _timer = 0f;

        public event Action<Type, object> OnSubmit; // 供外部订阅输入提交事件
        private object _pendingInput; // 待提交的输入数据，可以根据需要定义具体类型
        private Type _inputType; // 输入数据的类型

        public ClientInputor(float submitInterval = 0.05f)
        {
            SubmitInterval = submitInterval;
        }

        public void Update(float deltaTime)
        {
            if (_pendingInput == null) return;

            _timer += deltaTime;
            if (_timer >= SubmitInterval)
            {
                OnSubmit?.Invoke(_inputType, _pendingInput);
                
                (_pendingInput as IInputData)?.Clear();

                _timer -= SubmitInterval;
            }
        }

        public T ModifyInput<T>(Action<T> modifier) where T : new()
        {
            if (_pendingInput == null)
            {
                _pendingInput = new T();
                _inputType = typeof(T);
            }

            modifier?.Invoke((T)_pendingInput);
            return (T)_pendingInput;
        }
    }
}
