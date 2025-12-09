


using UnityEngine;

namespace GoveKits.Audio
{
        // AudioConfig 类保持不变
    [System.Serializable]
    public class AudioConfig
    {
        public string configName = "";
        public AudioClip audioClip = null;
        public float volume = 1f;
        public bool isBGMLoop = false;
    }
}