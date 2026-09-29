using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using GoveKits.Runtime.Util;
using UnityEngine;

namespace GoveKits.Runtime.Storage
{
    /// <summary>
    /// 自动保存组件，挂载在场景中负责定时保存游戏。
    /// </summary>
    public class AutoSaveBehaviour : MonoBehaviour
    {
        [SerializeField] private float intervalSeconds = 60f;

        private readonly Dictionary<string, string> _paths = new();
        private readonly Dictionary<string, System.Func<object>> _getData = new();
        private float _timer;
        private bool _isSaving;

        private void Update()
        {
            if (_isSaving) return; // 上一次保存未完成时跳过本轮，避免重入

            _timer += Time.deltaTime;
            if (_timer < intervalSeconds) return;

            SaveAll();
            _timer = 0f;
        }

        /// <summary>
        /// 注册自动保存对象。
        /// </summary>
        /// <param name="key">唯一标识（用于日志）。</param>
        /// <param name="path">存档路径。</param>
        /// <param name="getData">获取当前数据的委托。</param>
        public void Register<T>(string key, string path, System.Func<T> getData)
        {
            _getData[key] = () => getData();
            _paths[key] = path;
        }

        /// <summary>
        /// 取消注册指定键名的自动保存对象。
        /// </summary>
        /// <param name="key">要取消注册的键名。</param>
        public void Unregister(string key)
        {
            _getData.Remove(key);
            _paths.Remove(key);
        }

        /// <summary>
        /// 立即保存所有已注册的存档对象。
        /// </summary>
        public void SaveAll()
        {
            SaveAllAsync().Forget();
        }

        private async UniTask SaveAllAsync()
        {
            if (_isSaving) return;
            _isSaving = true;
            try
            {
                // await 期间 Register/Unregister 可能修改字典，迭代前做快照
                var snapshot = _getData.ToArray();
                foreach (var kvp in snapshot)
                {
                    try
                    {
                        var data = kvp.Value.Invoke();
                        _paths.TryGetValue(kvp.Key, out string path);
                        await SaveCore.SaveAsync(path, data);
                    }
                    catch (System.Exception ex)
                    {
                        LogCore.Error(nameof(AutoSaveBehaviour), $"AutoSave failed [{kvp.Key}]: {ex}");
                    }
                }
            }
            finally
            {
                _isSaving = false;
            }
        }
    }
}
