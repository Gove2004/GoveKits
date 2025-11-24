using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Audio;


namespace GoveKits.Audio
{
    /// <summary>
    /// 音频资源类
    /// </summary>
    public enum AudioChannel
    {
        Master,
        BGM,
        SFX,
        UI,
        Voice
    }

    /// <summary>
    /// 音频管理器
    /// </summary>
    public class AudioManager : MonoSingleton<AudioManager>
    {
        [Header("Mixer Groups")]
        [SerializeField] private AudioMixer _audioMixer;

        [Header("Audio Sources")]
        private AudioSource _BGMSource;
        private AudioSource _uiSource;
        [SerializeField] private int _sfxPoolSize = 10;
        private List<AudioSource> _sfxSources = new List<AudioSource>();
        private Coroutine _BGMFadeCoroutine;

        private Dictionary<string, AudioClip> _audioClips = new Dictionary<string, AudioClip>();

        // 音量属性
        public float MasterVolume { get; private set; } = 1f;
        public float BGMVolume { get; private set; } = 1f;
        public float SFXVolume { get; private set; } = 1f;
        public float UIVolume { get; private set; } = 1f;
        public float VoiceVolume { get; private set; } = 1f;

        private void Awake()
        {
            Initialize();
        }

        private void Initialize()
        {
            // 创建BGM音频源
            GameObject bgmSourceObj = new GameObject("BGM_Source");
            bgmSourceObj.transform.SetParent(transform);
            _BGMSource = bgmSourceObj.AddComponent<AudioSource>();
            _BGMSource.outputAudioMixerGroup = _audioMixer.FindMatchingGroups("BGM")[0];
            // 创建UI音频源
            GameObject uiSourceObj = new GameObject("UI_Source");
            uiSourceObj.transform.SetParent(transform);
            _uiSource = uiSourceObj.AddComponent<AudioSource>();
            _uiSource.outputAudioMixerGroup = _audioMixer.FindMatchingGroups("UI")[0];
            // 创建SFX音频源池
            for (int i = 0; i < _sfxPoolSize; i++)
            {
                GameObject sfxSourceObj = new GameObject($"SFX_Source_{i}");
                sfxSourceObj.transform.SetParent(transform);
                AudioSource source = sfxSourceObj.AddComponent<AudioSource>();
                source.outputAudioMixerGroup = _audioMixer.FindMatchingGroups("SFX")[0];
                _sfxSources.Add(source);
            }

            // 加载音量设置
            LoadVolumeSettings();
        }

        #region 持久化
        private void SaveVolumeSettings()
        {
            // PlayerPrefs.SetFloat("Master", MasterVolume);
            // PlayerPrefs.SetFloat("BGM", BGMVolume);
            // PlayerPrefs.SetFloat("SFX", SFXVolume);
            // PlayerPrefs.SetFloat("UI", UIVolume);
            // PlayerPrefs.SetFloat("Voice", VoiceVolume);
            // PlayerPrefs.Save();
        }

        private void LoadVolumeSettings()
        {
            // SetVolume(AudioChannel.Master, PlayerPrefs.GetFloat("Master", 1f));
            // SetVolume(AudioChannel.BGM, PlayerPrefs.GetFloat("BGM", 1f));
            // SetVolume(AudioChannel.SFX, PlayerPrefs.GetFloat("SFX", 1f));
            // SetVolume(AudioChannel.UI, PlayerPrefs.GetFloat("UI", 1f));
            // SetVolume(AudioChannel.Voice, PlayerPrefs.GetFloat("Voice", 1f));
        }
        #endregion

        #region 背景音乐控制
        public void PlayBGM(AudioClip clip, bool loop = true, float fadeDuration = 1f)
        {
            if (_BGMSource.clip == clip && _BGMSource.isPlaying)
                return;

            if (_BGMFadeCoroutine != null)
                StopCoroutine(_BGMFadeCoroutine);

            _BGMFadeCoroutine = StartCoroutine(FadeBGM(clip, loop, fadeDuration));
        }

        public void StopBGM(float fadeDuration = 1f)
        {
            if (_BGMFadeCoroutine != null)
                StopCoroutine(_BGMFadeCoroutine);

            _BGMFadeCoroutine = StartCoroutine(FadeOutBGM(fadeDuration));
        }

        public void PauseBGM()
        {
            _BGMSource.Pause();
        }

        public void ResumeBGM()
        {
            _BGMSource.UnPause();
        }

        private IEnumerator FadeBGM(AudioClip clip, bool loop, float fadeDuration)
        {
            // 淡出当前音乐
            if (_BGMSource.isPlaying)
            {
                float startVolume = _BGMSource.volume;
                for (float t = 0; t < fadeDuration; t += Time.deltaTime)
                {
                    _BGMSource.volume = Mathf.Lerp(startVolume, 0, t / fadeDuration);
                    yield return null;
                }
                _BGMSource.Stop();
            }

            // 设置新音乐并淡入
            _BGMSource.clip = clip;
            _BGMSource.loop = loop;
            _BGMSource.Play();

            float targetVolume = BGMVolume;
            for (float t = 0; t < fadeDuration; t += Time.deltaTime)
            {
                _BGMSource.volume = Mathf.Lerp(0, targetVolume, t / fadeDuration);
                yield return null;
            }

            _BGMSource.volume = targetVolume;
            _BGMFadeCoroutine = null;
        }

        private IEnumerator FadeOutBGM(float fadeDuration)
        {
            float startVolume = _BGMSource.volume;
            for (float t = 0; t < fadeDuration; t += Time.deltaTime)
            {
                _BGMSource.volume = Mathf.Lerp(startVolume, 0, t / fadeDuration);
                yield return null;
            }
            _BGMSource.Stop();
            _BGMSource.volume = startVolume;
            _BGMFadeCoroutine = null;
        }
        #endregion

        #region 音效控制
        public void PlaySFX(AudioClip clip, float volumeScale = 1f, bool loop = false)
        {
            AudioSource source = GetAvailableSFXSource();
            if (source == null) return;

            source.clip = clip;
            source.volume = SFXVolume * volumeScale;
            source.loop = loop;
            source.Play();
        }

        public void PlaySFXAtPosition(AudioClip clip, Vector3 position, float volumeScale = 1f)
        {
            AudioSource.PlayClipAtPoint(clip, position, SFXVolume * volumeScale);
        }

        public void PlayUISound(AudioClip clip, float volumeScale = 1f)
        {
            _uiSource.PlayOneShot(clip, UIVolume * volumeScale);
        }

        public void StopAllSFX()
        {
            foreach (AudioSource source in _sfxSources)
            {
                source.Stop();
            }
        }

        private AudioSource GetAvailableSFXSource()
        {
            foreach (AudioSource source in _sfxSources)
            {
                if (!source.isPlaying)
                    return source;
            }

            // 如果没有可用源，使用最早播放的源
            return _sfxSources[0];
        }
        #endregion

        #region 音量控制
        public void SetVolume(AudioChannel channel, float volume)
        {
            volume = Mathf.Clamp01(volume);

            switch (channel)
            {
                case AudioChannel.Master:
                    MasterVolume = volume;
                    _audioMixer.SetFloat("Master", VolumeToDB(volume));
                    break;
                case AudioChannel.BGM:
                    BGMVolume = volume;
                    _audioMixer.SetFloat("BGM", VolumeToDB(volume));
                    _BGMSource.volume = volume;
                    break;
                case AudioChannel.SFX:
                    SFXVolume = volume;
                    _audioMixer.SetFloat("SFX", VolumeToDB(volume));
                    break;
                case AudioChannel.UI:
                    UIVolume = volume;
                    _audioMixer.SetFloat("UI", VolumeToDB(volume));
                    break;
                case AudioChannel.Voice:
                    VoiceVolume = volume;
                    _audioMixer.SetFloat("Voice", VolumeToDB(volume));
                    break;
            }

            SaveVolumeSettings();
        }

        private float VolumeToDB(float volume)
        {
            // 将0-1线性音量转换为分贝值
            return volume <= 0 ? -80f : Mathf.Log10(volume) * 20;
        }
        #endregion

        // #region 资源管理
        // public void PreloadAudio(string clipName)
        // {
        //     if (_audioClips.ContainsKey(clipName)) return;

        //     AudioClip clip = Resources.Load<AudioClip>($"Audio/{clipName}");
        //     if (clip != null)
        //     {
        //         _audioClips.Add(clipName, clip);
        //     }
        // }

        // public AudioClip GetAudioClip(string clipName)
        // {
        //     if (_audioClips.TryGetValue(clipName, out AudioClip clip))
        //     {
        //         return clip;
        //     }

        //     // 如果没有预加载，尝试直接加载
        //     clip = Resources.Load<AudioClip>($"Audio/{clipName}");
        //     if (clip != null)
        //     {
        //         _audioClips.Add(clipName, clip);
        //     }

        //     return clip;
        // }

        // public void UnloadAudio(string clipName)
        // {
        //     if (_audioClips.ContainsKey(clipName))
        //     {
        //         Resources.UnloadAsset(_audioClips[clipName]);
        //         _audioClips.Remove(clipName);
        //     }
        // }

        // public void UnloadAllAudio()
        // {
        //     foreach (var clip in _audioClips.Values)
        //     {
        //         Resources.UnloadAsset(clip);
        //     }
        //     _audioClips.Clear();
        // }
        // #endregion
    }


}