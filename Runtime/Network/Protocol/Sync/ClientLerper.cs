using System;
using System.Collections.Generic;
using GoveKits.Runtime.Core;

namespace GoveKits.Runtime.Network
{
    internal class ClientLerper
    {
        public const int MaxSnapshots = 10;
        private readonly List<WorldPackage> _snapshots = new List<WorldPackage>();

        public float InterpolationDelay { get; set; } = 0.1f; 
        private float _renderTime = 0f;
        private float _serverTickInterval;

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

            // 寻找包含 renderTime 的前后快照
            for (int i = 0; i < _snapshots.Count - 1; i++)
            {
                float snapTimeFrom = _snapshots[i].Tick * _serverTickInterval;
                float snapTimeTo = _snapshots[i+1].Tick * _serverTickInterval;

                if (snapTimeFrom <= _renderTime && snapTimeTo >= _renderTime)
                {
                    fromSnap = _snapshots[i];
                    toSnap = _snapshots[i+1];
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
            // 遍历目标快照中所有发生改变的实体
            foreach (var toState in toSnap.Entities)
            {
                var entity = SpawnCore.GetEntity(toState.NetId) as ISyncable;
                if (entity != null)
                {
                    var fromState = GetStateFromSnap(fromSnap, toState.NetId);
                    
                    if (fromState != null)
                    {
                        // 正常情况：前后帧都有数据，平滑插值
                        entity.ApplyLerp(fromState.StatePayload, toState.StatePayload, t);
                    }
                    else
                    {
                        // 如果上一帧这个物体没动（增量同步中没发），或者刚生成，直接硬贴
                        entity.ApplySnap(toState.StatePayload);
                    }
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