using UnityEngine;

namespace GoveKits.Runtime.Storage
{
    /// <summary>
    /// 音频资源配置项，用于在编辑器中定义音频参数。
    /// </summary>
    [CreateAssetMenu(fileName = "AudioSO", menuName = "GoveKits/AudioSO")]
    public class AudioSO : ScriptableObject
    {
        /// <summary>音频通道（私有序列化字段）。</summary>
        [SerializeField] private AudioChannel _channel = AudioChannel.SFX;

        /// <summary>音频剪辑（私有序列化字段）。</summary>
        [SerializeField] private AudioClip _clip;

        /// <summary>音量（私有序列化字段，范围 0~1）。</summary>
        [SerializeField, Range(0f, 1f)] private float _volume = 1f;

        /// <summary>音调（私有序列化字段，范围 0.1~3）。</summary>
        [SerializeField, Range(0.1f, 3f)] private float _pitch = 1f;

        /// <summary>是否循环播放（私有序列化字段）。</summary>
        [SerializeField] private bool _loop = false;

        /// <summary>
        /// 获取音频通道。
        /// </summary>
        public AudioChannel Channel => _channel;

        /// <summary>
        /// 获取音频剪辑。
        /// </summary>
        public AudioClip Clip => _clip;

        /// <summary>
        /// 获取音量值（范围 0~1）。
        /// </summary>
        public float Volume => _volume;

        /// <summary>
        /// 获取音调值（范围 0.1~3）。
        /// </summary>
        public float Pitch => _pitch;

        /// <summary>
        /// 获取是否循环播放。
        /// </summary>
        public bool Loop => _loop;
    }
}
