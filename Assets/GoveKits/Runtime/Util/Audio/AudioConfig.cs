using UnityEngine;

namespace GoveKits.Audio
{
    [System.Serializable]
    public class AudioConfig
    {
        public string configName = "";
        
        [Tooltip("相对路径，不带扩展名。例如: 'Audio/BGM/BattleMusic'")]
        public string clipPath = "Audio/SFX/DefaultSound"; 
        
        [Range(0f, 1f)]
        public float volume = 1f;
        
        public AudioChannel audioChannel = AudioChannel.SFX;
    }
}