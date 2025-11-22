using UnityEngine;


namespace GoveKits.Audio
{
    // 音频组件
    public class AudioComponent : MonoBehaviour
    {
        public AudioConfig[] audioConfigs;

        /// <summary>
        /// 播放音频
        /// </summary>
        /// <param name="soundName"></param>
        public void Play(string soundName)
        {
            foreach (var config in audioConfigs)
            {
                if (config.configName == soundName && config.audioClip != null)
                {
                    if (config.isBGMLoop)  // 播放背景音乐
                    {
                        AudioManager.Instance.PlayBGM(config.audioClip);
                    }
                    else  // 播放音效
                    {
                        AudioManager.Instance.PlaySFX(config.audioClip, config.volume);
                    }
                }
            }
        }


        public void Play(AudioClip clip, float volume = 1f)
        {
            AudioManager.Instance.PlaySFX(clip, volume);
        }
    }






    // 音频配置
    [System.Serializable]
    public class AudioConfig
    {
        public string configName = "";
        public AudioClip audioClip = null;
        public float volume = 1f;
        public bool isBGMLoop = false;
    }
}