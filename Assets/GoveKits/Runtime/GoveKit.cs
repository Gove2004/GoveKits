using GoveKits.Audio;
using UnityEngine;

namespace GoveKits
{
    public static class GoveKit
    {
        public static AudioManager AudioManagerInstance { get; private set; }







        
        public static void Initialize()
        {
            Debug.Log("GoveKit Initialized");

            GameObject gameObject = new GameObject("AudioManager");
            AudioManagerInstance = gameObject.AddComponent<AudioManager>();
            Object.DontDestroyOnLoad(gameObject);

            // 在这里添加其他全局初始化逻辑
        }
    }
}
