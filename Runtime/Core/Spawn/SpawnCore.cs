using System;
using System.Collections.Generic;

namespace GoveKits.Runtime.Core
{
    public static class SpawnCore
    {
        // 核心字典：记录注册的工厂和销毁方法
        // 注意：Factory 增加了 uint 参数，让工厂在创建实体时就知道它的终身 ID 是多少
        private static readonly Dictionary<string, Func<uint, ISpawnData, ISpawnable>> _spawnFactories = new();
        private static readonly Dictionary<string, Action<ISpawnable>> _despawnActions = new();
        
        // 活着的实体花名册
        private static readonly Dictionary<uint, ISpawnable> _spawnedEntities = new();

        // 内部发号器（从 100 开始，预留前面的 ID 给特殊用途）
        private static uint _localIdCounter = 100;
        public static uint NextObjectId() => ++_localIdCounter;

        // 全局生命周期事件（极度好用！小地图、网络同步、成就系统可以直接监听）
        public static event Action<ISpawnable> OnEntitySpawned;
        public static event Action<ISpawnable> OnEntityDespawned;

        /// <summary>
        /// 注册生成器和销毁器
        /// </summary>
        public static void Register(string spawnKey, Func<uint, ISpawnData, ISpawnable> factoryFunc, Action<ISpawnable> despawnAction)
        {
            if (_spawnFactories.ContainsKey(spawnKey))
            {
                LogCore.Warning(nameof(SpawnCore), $"SpawnKey: [{spawnKey}] 已被注册，将被覆盖！");
            }
            _spawnFactories[spawnKey] = factoryFunc;
            _despawnActions[spawnKey] = despawnAction;
        }
        public static void UnRegister(string spawnKey)
        {
            _spawnFactories.Remove(spawnKey);
            _despawnActions.Remove(spawnKey);
        }

        /// <summary>
        /// 核心生成方法
        /// </summary>
        /// <param name="spawnKey">要生成的物体类型键值</param>
        /// <param name="data">初始化数据（可选）</param>
        /// <param name="predefinedId">预定义的ID。传0代表单机/服务器生成；传非0代表联机客机按服务器指令生成</param>
        public static ISpawnable Spawn(string spawnKey, ISpawnData data = null, uint predefinedId = 0)
        {
            if (!_spawnFactories.TryGetValue(spawnKey, out var factoryFunc))
            {
                LogCore.Error(nameof(SpawnCore), $"未找到 SpawnKey: [{spawnKey}] 的注册工厂！");
                return null;
            }

            // 1. 决定 ID 的归属权
            uint objectId = predefinedId > 0 ? predefinedId : NextObjectId();

            // 防止网络同步时传入了重复的 ID
            if (_spawnedEntities.ContainsKey(objectId))
            {
                LogCore.Warning(nameof(SpawnCore), $"ObjectId: [{objectId}] 已存在，放弃生成！");
                return _spawnedEntities[objectId];
            }

            try
            {
                // 2. 调用业务层工厂，并把 ID 强行塞给它
                ISpawnable entity = factoryFunc(objectId, data);
                if (entity != null)
                {
                    entity.ObjectId = objectId; // 由 SpawnCore 统一赋值和管理，业务代码不应该修改它
                    _spawnedEntities[objectId] = entity;
                    OnEntitySpawned?.Invoke(entity); // 触发全局事件
                    return entity;
                }
            }
            catch (Exception ex)
            {
                LogCore.Error(nameof(SpawnCore), $"生成 [{spawnKey}] 时发生异常: {ex}");
            }

            return null;
        }

        /// <summary>
        /// 泛型包装，方便直接获取强类型对象
        /// </summary>
        public static T Spawn<T>(string spawnKey, ISpawnData data = null, uint predefinedId = 0) where T : class, ISpawnable
        {
            return Spawn(spawnKey, data, predefinedId) as T;
        }

        /// <summary>
        /// 核心销毁方法
        /// </summary>
        public static void Despawn(uint objectId)
        {
            if (_spawnedEntities.TryGetValue(objectId, out var entity))
            {
                _spawnedEntities.Remove(objectId);

                if (_despawnActions.TryGetValue(entity.SpawnKey, out var despawnAction))
                {
                    try
                    {
                        despawnAction(entity);
                        OnEntityDespawned?.Invoke(entity); // 触发全局事件
                    }
                    catch (Exception ex)
                    {
                        LogCore.Error(nameof(SpawnCore), $"销毁 [{entity.SpawnKey}] 时发生异常: {ex}");
                    }
                }
                else
                {
                    LogCore.Error(nameof(SpawnCore), $"未找到 SpawnKey: [{entity.SpawnKey}] 的销毁器！");
                }
            }
            else
            {
                LogCore.Warning(nameof(SpawnCore), $"尝试销毁不存在的 ObjectId: [{objectId}]！");
            }
        }

        /// <summary>
        /// 获取当前存活的实体
        /// </summary>
        public static ISpawnable GetEntity(uint objectId)
        {
            return _spawnedEntities.GetValueOrDefault(objectId);
        }

        public static T GetEntity<T>(uint objectId) where T : class, ISpawnable
        {
            return GetEntity(objectId) as T;
        }

        public static List<T> GetAllEntitiesOfType<T>() where T : class, ISpawnable
        {
            List<T> list = new List<T>();
            foreach (var entity in _spawnedEntities.Values)
            {
                if (entity is T tEntity) list.Add(tEntity);
            }
            return list;
        }

        public static List<ISpawnable> GetAllEntities()
        {
            return new List<ISpawnable>(_spawnedEntities.Values);
        }

        /// <summary>
        /// 清理所有实体（切换场景或断线重连时必须调用）
        /// </summary>
        public static void ClearAll()
        {
            // 为了安全遍历并销毁，先拷贝一份 ID 列表
            List<uint> idsToDespawn = new List<uint>(_spawnedEntities.Keys);
            foreach (var id in idsToDespawn)
            {
                Despawn(id);
            }
            _spawnedEntities.Clear();
        }
    }
}