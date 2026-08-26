using System;
using System.Collections.Generic;
using GoveKits.Runtime.Core;

namespace GoveKits.Runtime.Network
{
    /// <summary>
    /// 输入数据清理接口。实现此接口的输入数据可以在提交后被自动清除状态。
    /// </summary>
    public interface IInputData
    {
        /// <summary>
        /// 清除输入数据的状态，通常在提交后调用以复用对象。
        /// </summary>
        void Clear();
    }

    /// <summary>
    /// 客户端输入收集器，定时将输入数据提交到网络。
    /// </summary>
    internal class ClientInputor
    {
        private readonly float _submitInterval;
        private float _timer = 0f;

        public event Action<Type, object> OnSubmit;

        private object _pendingInput;
        private Type _inputType;

        public ClientInputor(float submitInterval = 0.05f)
        {
            _submitInterval = submitInterval;
        }

        public void Update(float deltaTime)
        {
            if (_pendingInput == null) return;

            _timer += deltaTime;
            if (_timer >= _submitInterval)
            {
                OnSubmit?.Invoke(_inputType, _pendingInput);
                (_pendingInput as IInputData)?.Clear();
                _timer -= _submitInterval;
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

    /// <summary>
    /// 服务端输入收集器，按 Tick 收集所有玩家的输入。
    /// </summary>
    internal class ServerCollector
    {
        private readonly Dictionary<int, byte[]> _currentInputs = new();

        public void OnReceiveInput(PlayerInputPackage msg)
        {
            _currentInputs[msg.PlayerId] = msg.Payload;
        }

        public AllInputPackage ExtractTickData(int currentTick)
        {
            var tickData = new AllInputPackage { Tick = currentTick };
            foreach (var kvp in _currentInputs)
            {
                tickData.Inputs[kvp.Key] = kvp.Value;
            }
            _currentInputs.Clear();
            return tickData;
        }
    }

    /// <summary>
    /// 服务端固定帧模拟器。
    /// </summary>
    internal class ServerSimulator
    {
        public float TickInterval { get; set; }
        private float _accumulateTime = 0f;
        private int _currentTick = 0;

        private readonly ServerCollector _collector;

        public event Action<AllInputPackage> OnLogicTick;
        public event Action<int> OnBroadcastTick;

        public ServerSimulator(ServerCollector collector, float tickInterval)
        {
            _collector = collector;
            TickInterval = tickInterval;
        }

        public void Update(float deltaTime)
        {
            _accumulateTime += deltaTime;
            while (_accumulateTime >= TickInterval)
            {
                _currentTick++;
                var tickData = _collector.ExtractTickData(_currentTick);
                OnLogicTick?.Invoke(tickData);
                OnBroadcastTick?.Invoke(_currentTick);
                _accumulateTime -= TickInterval;
            }
        }
    }

    /// <summary>
    /// 客户端帧同步插值器。
    /// </summary>
    internal class ClientLerper
    {
        public const int MaxSnapshots = 10;
        private readonly List<WorldPackage> _snapshots = new();

        public float InterpolationDelay { get; set; } = 0.1f;
        private float _renderTime = 0f;
        private readonly float _serverTickInterval;

        public ClientLerper(float serverTickInterval)
        {
            _serverTickInterval = serverTickInterval;
        }

        public void EnqueueSnapshot(WorldPackage snapshot)
        {
            _snapshots.Add(snapshot);
            if (_snapshots.Count > MaxSnapshots) _snapshots.RemoveAt(0);

            if (_snapshots.Count == 1)
                _renderTime = (snapshot.Tick * _serverTickInterval) - InterpolationDelay;
        }

        public void Update(float deltaTime)
        {
            if (_snapshots.Count < 2) return;

            _renderTime += deltaTime;

            WorldPackage fromSnap = null;
            WorldPackage toSnap = null;

            for (int i = 0; i < _snapshots.Count - 1; i++)
            {
                float snapTimeFrom = _snapshots[i].Tick * _serverTickInterval;
                float snapTimeTo = _snapshots[i + 1].Tick * _serverTickInterval;

                if (snapTimeFrom <= _renderTime && snapTimeTo >= _renderTime)
                {
                    fromSnap = _snapshots[i];
                    toSnap = _snapshots[i + 1];
                    break;
                }
            }

            if (fromSnap != null && toSnap != null)
            {
                float t = (_renderTime - (fromSnap.Tick * _serverTickInterval)) / _serverTickInterval;
                ApplyInterpolation(fromSnap, toSnap, t);
            }
        }

        private void ApplyInterpolation(WorldPackage fromSnap, WorldPackage toSnap, float t)
        {
            foreach (var toState in toSnap.Entities)
            {
                var entity = SpawnCore.GetEntity(toState.NetId) as ISyncable;
                if (entity == null) continue;

                var fromState = GetStateFromSnap(fromSnap, toState.NetId);

                if (fromState != null)
                {
                    entity.ApplyLerp(fromState.StatePayload, toState.StatePayload, t);
                }
                else
                {
                    entity.ApplySnap(toState.StatePayload);
                }
            }
        }

        private EntityState GetStateFromSnap(WorldPackage snap, uint netId)
        {
            if (snap.Entities == null) return null;
            foreach (var state in snap.Entities)
                if (state.NetId == netId) return state;
            return null;
        }
    }
}
