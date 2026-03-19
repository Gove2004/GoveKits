using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using GoveKits.Runtime.Core.Singleton;
using UnityEngine;

namespace GoveKits.Runtime.Storage.Save
{
    /// <summary>
    /// 自动保存组件，挂载在场景中负责自动保存游戏。
    /// </summary>
    public class AutoSaveBehaviour : MonoSingleton<AutoSaveBehaviour>
    {
        [SerializeField] private bool autoSaveEnabled = true;
        [SerializeField] private float saveIntervalSeconds = 30f;

        private readonly Dictionary<object, System.Func<UniTask>> runtimeTargets = new();
        private float elapsedSeconds;
        private bool isSaving;

        private void Update()
        {
            if (!autoSaveEnabled || saveIntervalSeconds <= 0f)
            {
                return;
            }

            elapsedSeconds += Time.unscaledDeltaTime;
            if (elapsedSeconds >= saveIntervalSeconds)
            {
                elapsedSeconds = 0f;
                SaveAllAsync().Forget();
            }
        }

        /// <summary>
        /// 外部注册存档对象。
        /// </summary>
        public bool Register<T>(ISaveData<T> saveable)
        {
            if (saveable == null)
            {
                return false;
            }

            runtimeTargets[saveable.RelativePath] = () => SaveCore.SaveAsync(saveable);
            return true;
        }

        /// <summary>
        /// 外部注销存档对象。
        /// </summary>
        public bool Unregister<T>(ISaveData<T> saveable)
        {
            if (saveable == null)
            {
                return false;
            }

            return runtimeTargets.Remove(saveable.RelativePath);
        }

        /// <summary>
        /// 保存所有注册的存档对象。
         /// 由 Update 定时调用，也可由外部手动调用以实现特定时机的保存（如场景切换前）。
        /// </summary>
        public void SaveAll()
        {
            SaveAllAsync().Forget();
        }

        public async UniTask SaveAllAsync()
        {
            if (isSaving)
            {
                return;
            }

            isSaving = true;
            try
            {
                foreach (System.Func<UniTask> saveAction in runtimeTargets.Values)
                {
                    await saveAction.Invoke();
                }
            }
            finally
            {
                isSaving = false;
            }
        }
    }
}