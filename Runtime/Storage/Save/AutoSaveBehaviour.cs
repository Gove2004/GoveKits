using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using GoveKits.Runtime.Core;
using UnityEngine;

namespace GoveKits.Runtime.Storage
{
    /// <summary>
    /// 自动保存组件，挂载在场景中负责自动保存游戏。
    /// </summary>
    public class AutoSaveBehaviour : MonoSingleton<AutoSaveBehaviour>
    {
        [SerializeField] private float intervalSeconds = 60f;
        
        private readonly Dictionary<string, Func<object>> _registrations = new();
        private readonly Dictionary<string, string> _paths = new();
        private float _timer;


        private void Update()
        {
            _timer += Time.deltaTime;
            if (_timer < intervalSeconds)
            {
                return;
            }
            SaveAll();
            _timer = 0;
        }

        /// <summary>
        /// 注册自动保存对象
        /// </summary>
        /// <param name="key">唯一标识（用于日志）</param>
        /// <param name="path">存档路径</param>
        /// <param name="getData">获取当前数据的委托</param>
        public void Register<T>(string key, string path, Func<T> getData)
        {
            _registrations[key] = () => getData();
            _paths[key] = path;
        }

        public void Unregister(string key)
        {
            _registrations.Remove(key);
            _paths.Remove(key);
        }

        /// <summary>
        /// 保存所有注册的存档对象。
         /// 由 Update 定时调用，也可由外部手动调用以实现特定时机的保存（如场景切换前）。
        /// </summary>
        public void SaveAll()
        {
            SaveAllAsync().Forget();
        }

        private async UniTask SaveAllAsync()
        {
            foreach (var kvp in _registrations)
            {
                try
                {
                    var data = kvp.Value.Invoke();
                    var path = _paths[kvp.Key];
                    await SaveCore.SaveAsync(path, data);  // 直接调用新API
                }
                catch (Exception ex)
                {
                    LogCore.Error(nameof(AutoSaveBehaviour), $"AutoSave failed [{kvp.Key}]: {ex}");
                }
            }
        }
    }
}