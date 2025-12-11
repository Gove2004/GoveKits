using UnityEngine;

namespace GoveKits.Audio
{
    public class AudioComponent : MonoBehaviour
    {
        public AudioConfig[] audioConfigs;

        public void Play(string soundName)
        {
            foreach (var config in audioConfigs)
            {
                if (config.configName == soundName)
                {
                    if (string.IsNullOrEmpty(config.clipPath)) return;

                    switch (config.audioChannel)
                    {
                        case AudioChannel.BGM:
                            AudioManager.PlayBGM(config.clipPath);
                            break;
                        case AudioChannel.SFX:
                            AudioManager.PlaySFX(config.clipPath, config.volume);
                            break;
                        case AudioChannel.UI:
                            AudioManager.PlayUISound(config.clipPath, config.volume);
                            break;
                        case AudioChannel.Voice:
                            AudioManager.PlaySFX(config.clipPath, config.volume);
                            break;
                    }
                    return;
                }
            }
        }
    }
}