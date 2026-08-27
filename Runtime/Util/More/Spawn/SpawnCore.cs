using System;
using System.Collections.Generic;

namespace GoveKits.Runtime.Util
{
    /// <summary>
    /// 实体生成与销毁的统一管理器。
    /// 管理工厂注册、实体生命周期追踪和全局 ID 分配。
    /// </summary>
    public static class SpawnCore
    {
        /// <summary>实体工厂注册表，以 SpawnKey 为键，用于创建实体实例。</summary>
        private static Dictionary<string, Func<uint, ISpawnData, ISpawnable>> spawnFactories = new();
        /// <summary>实体销毁回调注册表，以 SpawnKey 为键，用于清理实体资源。</summary>
        private static Dictionary<string, Action<ISpawnable>> despawnActions = new();
        /// <summary>当前存活实体表，以 ObjectId 为键。</summary>
        private static Dictionary<uint, ISpawnable> spawnedEntities = new();
        /// <summary>全局对象 ID 计数器，从 100 开始递增。</summary>
        private static uint idCounter = 100;

        /// <summary>实体生成后触发，携带刚生成的实体引用。</summary>
        public static event Action<ISpawnable> OnEntitySpawned;
        /// <summary>实体销毁后触发，携带刚销毁的实体引用。</summary>
        public static event Action<ISpawnable> OnEntityDespawned;

        /// <summary>
        /// 注册实体的工厂方法和销毁回调。
        /// 同一 spawnKey 重复注册会覆盖之前的注册并输出警告。
        /// </summary>
        /// <param name="spawnKey">实体类型的唯一标识键</param>
        /// <param name="factoryFunc">工厂函数，用于创建实体实例</param>
        /// <param name="despawnAction">销毁回调，用于清理实体资源</param>
        public static void Register(string spawnKey, Func<uint, ISpawnData, ISpawnable> factoryFunc, Action<ISpawnable> despawnAction)
        {
            if (spawnFactories.ContainsKey(spawnKey))
            {
                LogCore.Warning(nameof(SpawnCore), $"SpawnKey: [{spawnKey}] 已被注册，将被覆盖！");
            }
            spawnFactories[spawnKey] = factoryFunc;
            despawnActions[spawnKey] = despawnAction;
        }

        /// <summary>
        /// 注销指定键名的实体工厂和销毁回调。
        /// </summary>
        /// <param name="spawnKey">要注销的实体类型键</param>
        public static void Unregister(string spawnKey)
        {
            spawnFactories.Remove(spawnKey);
            despawnActions.Remove(spawnKey);
        }

        /// <summary>
        /// 通过 spawnKey 生成实体实例。
        /// 若 predefinedId 非零则使用该 ID，否则自动分配下一个可用 ID。
        /// </summary>
        /// <param name="spawnKey">已注册的实体类型键</param>
        /// <param name="data">可选的初始化数据</param>
        /// <param name="predefinedId">可选的预定义 ID，零表示自动分配</param>
        /// <returns>生成的实体实例，注册不存在或已存在同 ID 时返回 null</returns>
        public static ISpawnable Spawn(string spawnKey, ISpawnData data = null, uint predefinedId = 0)
        {
            if (!spawnFactories.TryGetValue(spawnKey, out var factoryFunc))
            {
                LogCore.Error(nameof(SpawnCore), $"未找到 SpawnKey: [{spawnKey}] 的注册工厂！");
                return null;
            }

            uint objectId = predefinedId > 0 ? predefinedId : NextObjectId();

            if (spawnedEntities.ContainsKey(objectId))
            {
                LogCore.Warning(nameof(SpawnCore), $"ObjectId: [{objectId}] 已存在，放弃生成！");
                return spawnedEntities[objectId];
            }

            try
            {
                ISpawnable entity = factoryFunc(objectId, data);
                if (entity != null)
                {
                    spawnedEntities[objectId] = entity;
                    OnEntitySpawned?.Invoke(entity);
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
        /// 通过 ObjectId 销毁已生成的实体。
        /// 会调用对应的销毁回调并触发 OnEntityDespawned 事件。
        /// </summary>
        /// <param name="objectId">要销毁的实体 ID</param>
        public static void Despawn(uint objectId)
        {
            if (!spawnedEntities.TryGetValue(objectId, out var entity))
            {
                LogCore.Warning(nameof(SpawnCore), $"尝试销毁不存在的 ObjectId: [{objectId}]！");
                return;
            }

            spawnedEntities.Remove(objectId);

            if (despawnActions.TryGetValue(entity.SpawnKey, out var despawnAction))
            {
                try
                {
                    despawnAction(entity);
                    OnEntityDespawned?.Invoke(entity);
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

        /// <summary>
        /// 通过 ObjectId 获取当前存活的实体实例。
        /// </summary>
        /// <param name="objectId">实体的唯一 ID</param>
        /// <returns>实体实例，不存在时返回 null</returns>
        public static ISpawnable GetEntity(uint objectId)
        {
            return spawnedEntities.GetValueOrDefault(objectId);
        }

        /// <summary>
        /// 获取所有存活实体中指定类型的实例列表。
        /// </summary>
        /// <typeparam name="T">要筛选的实体类型</typeparam>
        /// <returns>匹配类型的实体列表</returns>
        public static List<T> GetAllEntitiesOfType<T>() where T : class, ISpawnable
        {
            var list = new List<T>();
            foreach (var entity in spawnedEntities.Values)
            {
                if (entity is T tEntity) list.Add(tEntity);
            }
            return list;
        }

        /// <summary>获取所有存活实体的列表副本。</summary>
        public static List<ISpawnable> GetAllEntities()
        {
            return new List<ISpawnable>(spawnedEntities.Values);
        }

        private static uint NextObjectId() => ++idCounter;

        /// <summary>
        /// 关闭并清理所有已生成的实体，触发各自的销毁回调。
        /// </summary>
        public static void Close()
        {
            var idsToDespawn = new List<uint>(spawnedEntities.Keys);
            foreach (var id in idsToDespawn)
                Despawn(id);
            spawnedEntities.Clear();
        }
    }
}
