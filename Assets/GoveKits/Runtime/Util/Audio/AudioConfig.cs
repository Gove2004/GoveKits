using UnityEngine;

namespace GoveKits.Runtime.Util
{
    [System.Serializable]
    public class AudioConfig : ScriptableObject
    {
        [SerializeField] private AudioChannel channel = AudioChannel.SFX;
        [SerializeField] private string clipPath;
        [SerializeField, Range(0f, 1f)] private float volume = 1f;
        [SerializeField, Range(0.1f, 3f)] private float pitch = 1f;
        [SerializeField, Range(0f, 1f)] private float pitchRandomRange;

        public AudioChannel Channel => channel;
        public string ClipPath => clipPath;
        public float Volume => volume;
        public float Pitch => pitch;
        public float PitchRandomRange => pitchRandomRange;
    }
}