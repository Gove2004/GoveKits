

using System;
using System.Collections.Generic;
using UnityEngine;

namespace GoveKits.Runtime.Network
{
    public static class SpawnCore
    {
        private static Dictionary<string, Func<ISpawnMsg, GameObject>> _prefab = new();

        public static void Register<T>(Func<T, GameObject> func) where T : ISpawnMsg
        {
            string typeName = typeof(T).FullName;
            if (_prefab.ContainsKey(typeName))
            {
                Debug.LogError($"SpawnCore 已经注册过 {typeName} 了，不能重复注册！");
                return;
            }

            _prefab[typeName] = (msg) => func((T)msg);
        }


        public static GameObject Spawn(ISpawnMsg msg)
        {
            string typeName = msg.GetType().FullName;
            if (_prefab.TryGetValue(typeName, out var func))
            {
                return func(msg);
            }
            else
            {
                Debug.LogError($"SpawnCore 没有注册过 {typeName} 的生成函数！");
                return null;
            }
        }
    }


    /// <summary>
    /// 根据消息内容生成一个GameObject
    /// </summary>
    public interface ISpawnMsg
    {
        
    }
}