using System;
using System.Collections.Generic;
using UnityEngine;
using GoveKits.Runtime.Core;

namespace GoveKits.Runtime.Network
{
    public static class SpawnCore
    {
        // 核心变更：Key 从“C#类名”变成了“PrefabId(字符串)”
        private static readonly Dictionary<string, Func<SpawnEntityMsg, INetObject>> _factories = new();
        private static readonly Dictionary<string, Action<INetObject>> _despawnActions = new();
        private static readonly Dictionary<int, INetObject> _activeEntities = new();


        // --- 注册字典 ---
        public static void Register(string prefabId, Func<SpawnEntityMsg, INetObject> factoryFunc, Action<INetObject> despawnAction)
        {
            if (_factories.ContainsKey(prefabId))
            {
                LogCore.Warning(nameof(SpawnCore), $"已经注册过 {prefabId} 的生成器了！");
                return;
            }
            _factories[prefabId] = factoryFunc;
            _despawnActions[prefabId] = despawnAction;
        }

        // --- 生成与销毁 ---
        public static INetObject Spawn(SpawnEntityMsg msg)
        {
            if (_activeEntities.ContainsKey(msg.NetId)) return _activeEntities[msg.NetId];

            if (_factories.TryGetValue(msg.PrefabId, out var factoryFunc))
            {
                // 调用业务层注册的工厂函数
                INetObject netObj = factoryFunc(msg);
                if (netObj != null)
                {
                    _activeEntities.Add(msg.NetId, netObj);
                }
                return netObj;
            }
            else
            {
                LogCore.Error(nameof(SpawnCore), $"未找到 PrefabId: [{msg.PrefabId}] 的注册器！");
                return null;
            }
        }

        public static void Despawn(int netId)
        {
            if (_activeEntities.TryGetValue(netId, out var netObj))
            {
                _activeEntities.Remove(netId);
                if (_despawnActions.TryGetValue(netObj.PrefabId, out var despawnAction))
                {
                    despawnAction(netObj);
                }
                else
                {
                    LogCore.Error(nameof(SpawnCore), $"未找到 PrefabId: [{netObj.PrefabId}] 的销毁器！");
                }
            }
            else
            {
                LogCore.Warning(nameof(SpawnCore), $"尝试销毁不存在的 NetId: {netId}");
            }
        }

        public static void ClearAll()
        {
            foreach (var netObj in _activeEntities.Values)
                if (netObj != null) Despawn(netObj.NetId);
            _activeEntities.Clear();
        }

        public static INetObject GetEntity(int netId)
        {
            _activeEntities.TryGetValue(netId, out var netObj);
            return netObj;
        }

        public static IEnumerable<KeyValuePair<int, INetObject>> GetAllEntities() => _activeEntities;
    }
}