using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GoveKits.Runtime.Core
{
    public static class SceneCore
    {
        /// <summary>
        /// 获取当前活动场景的名称。
        /// </summary>
        public static string ActiveSceneName => SceneManager.GetActiveScene().name;

        /// <summary>
        /// 判断指定场景是否为当前活动场景。
        /// </summary>
        public static bool IsSceneActive(string sceneName)
        {
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                return false;
            }

            return string.Equals(ActiveSceneName, sceneName, StringComparison.Ordinal);
        }

        /// <summary>
        /// 判断场景是否已被加载到内存。
        /// </summary>
        public static bool IsSceneLoaded(string sceneName)
        {
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                return false;
            }

            Scene scene = SceneManager.GetSceneByName(sceneName);
            return scene.IsValid() && scene.isLoaded;
        }

        /// <summary>
        /// 同步加载场景。
        /// </summary>
        public static void Load(string sceneName, LoadSceneMode mode = LoadSceneMode.Single)
        {
            SceneManager.LoadScene(sceneName, mode);
        }

        /// <summary>
        /// 异步加载场景。
        /// </summary>
        public static async UniTask LoadAsync(string sceneName, LoadSceneMode mode = LoadSceneMode.Single)
        {
            AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName, mode);
            await operation;
        }

        /// <summary>
        /// 卸载已加载的附加场景。
        /// </summary>
        public static async UniTask UnloadAsync(string sceneName)
        {
            Scene scene = SceneManager.GetSceneByName(sceneName);
            if (scene.IsValid() == false || scene.isLoaded == false)
            {
                return;
            }

            AsyncOperation operation = SceneManager.UnloadSceneAsync(scene);
            if (operation == null)
            {
                return;
            }

            await operation;
        }

        /// <summary>
        /// 重新加载当前活动场景。
        /// </summary>
        public static UniTask ReloadActiveAsync()
        {
            return LoadAsync(ActiveSceneName, LoadSceneMode.Single);
        }
    }
}