using UnityEngine;

namespace GoveKits.Runtime.Storage
{
    [CreateAssetMenu(fileName = "AudioSO", menuName = "GoveKits/AudioSO")]
    public class AudioSO : ScriptableObject
    {
        [SerializeField] private AudioChannel channel = AudioChannel.SFX;
        [SerializeField] private AudioClip clip;
        [SerializeField, Range(0f, 1f)] private float volume = 1f;
        [SerializeField, Range(0.1f, 3f)] private float pitch = 1f;
        [SerializeField, Range(0f, 1f)] private float pitchRandomRange = 0f;
        [SerializeField] private bool loop = false; // 扩展：是否循环播放

        public AudioChannel Channel => channel;
        public AudioClip ClipPath => clip;
        public float Volume => volume;
        public float Pitch => pitch;
        public float PitchRandomRange => pitchRandomRange;
        public bool Loop => loop;
    }
}