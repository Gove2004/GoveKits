using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GoveKits.Runtime.Storage.Res;
using GoveKits.Runtime.Storage.Save;
using GoveKits.Runtime.Core;

namespace GoveKits.Runtime.Util
{
    public static class AudioCore
    {
        private sealed class AudioCoreDriver : MonoBehaviour {}

        private const int SfxPoolSize = 16;
        private const float MinVolume = 0f;
        private const float MaxVolume = 1f;
        private const float DefaultVolume = 1f;
        private const float MinPitch = 0.1f;
        private const float MaxPitch = 3f;
        private const float DefaultPitch = 1f;
        private const float MaxPitchRandomRange = 1f;
        private const float MinFadeSeconds = 0f;
        private const float DefaultBgmFadeSeconds = 0.5f;

        private const string AudioPreKey = "Audio.";
        private const string AudioMasterKey = AudioPreKey + "Master";
        private const string AudioBgmKey = AudioPreKey + "BGM";
        private const string AudioSfxKey = AudioPreKey + "SFX";
        private const string AudioUiKey = AudioPreKey + "UI";
        private const string AudioVoiceKey = AudioPreKey + "Voice";
        private static GameObject _root;
        private static AudioCoreDriver _driver;
        private static AudioSource _bgmSource;
        private static AudioSource _uiSource;
        private static readonly List<AudioSource> _sfxPool = new();
        private static Coroutine _fadeCoroutine;

        // 音量配置直接存入 AudioCore
        public static float MasterVol { get; private set; } = DefaultVolume;
        public static float BgmVol { get; private set; } = DefaultVolume;
        public static float SfxVol { get; private set; } = DefaultVolume;
        public static float UiVol { get; private set; } = DefaultVolume;
        public static float VoiceVol { get; private set; } = DefaultVolume;

        private static string _currentBgmPath;

        public static void Init()
        {
            if (_root != null) return;

            // 加载配置
            MasterVol = PrefsCore.GetFloat(AudioMasterKey, DefaultVolume);
            BgmVol = PrefsCore.GetFloat(AudioBgmKey, DefaultVolume);
            SfxVol = PrefsCore.GetFloat(AudioSfxKey, DefaultVolume);
            UiVol = PrefsCore.GetFloat(AudioUiKey, DefaultVolume);
            VoiceVol = PrefsCore.GetFloat(AudioVoiceKey, DefaultVolume);

            _root = new GameObject("[AudioCore]");
            UnityEngine.Object.DontDestroyOnLoad(_root);
            _driver = _root.AddComponent<AudioCoreDriver>();

            _bgmSource = _root.AddComponent<AudioSource>();
            _bgmSource.loop = true;
            _uiSource = _root.AddComponent<AudioSource>();

            for (int i = 0; i < SfxPoolSize; i++)
            {
                var src = _root.AddComponent<AudioSource>();
                src.playOnAwake = false;
                _sfxPool.Add(src);
            }
            ApplyVolume();
        }

        public static void SetVolume(AudioChannel channel, float vol)
        {
            Init();
            vol = ClampVolume(vol);

            switch (channel)
            {
                case AudioChannel.Master: MasterVol = vol; PrefsCore.SetFloat(AudioMasterKey, vol); break;
                case AudioChannel.BGM: BgmVol = vol; PrefsCore.SetFloat(AudioBgmKey, vol); break;
                case AudioChannel.SFX: SfxVol = vol; PrefsCore.SetFloat(AudioSfxKey, vol); break;
                case AudioChannel.UI: UiVol = vol; PrefsCore.SetFloat(AudioUiKey, vol); break;
                case AudioChannel.Voice: VoiceVol = vol; PrefsCore.SetFloat(AudioVoiceKey, vol); break;
            }
            ApplyVolume();
            PrefsCore.Save();
        }

        private static void ApplyVolume()
        {
            _bgmSource.volume = BgmVol * MasterVol;
            _uiSource.volume = UiVol * MasterVol;
        }

        public static void Play(AudioConfig config)
        {
            if (config == null)
            {
                LogCore.LogWarning(nameof(AudioCore), "Play ignored: config is null.");
                return;
            }

            if (string.IsNullOrWhiteSpace(config.ClipPath))
            {
                LogCore.LogWarning(nameof(AudioCore), "Play ignored: clip path is empty.");
                return;
            }

            float volume = Mathf.Clamp01(config.Volume);
            float pitch = BuildPitch(config.Pitch, config.PitchRandomRange);

            switch (config.Channel)
            {
                case AudioChannel.BGM: PlayBGM(config.ClipPath, pitch: pitch); break;
                case AudioChannel.SFX: PlaySFX(config.ClipPath, volume, pitch); break;
                case AudioChannel.UI: PlayUI(config.ClipPath, volume, pitch); break;
                case AudioChannel.Voice: PlayVoice(config.ClipPath, volume, pitch); break;
            }
        }

        public static void PlayBGM(string path, float fadeTime = DefaultBgmFadeSeconds, float pitch = DefaultPitch)
        {
            Init();
            if (string.IsNullOrWhiteSpace(path)) return;

            if (_currentBgmPath == path) return;
            var clip = ResCore.Load<AudioClip>(ResLoadType.Resources, path);
            if (clip == null) return;

            if (_fadeCoroutine != null) _driver.StopCoroutine(_fadeCoroutine);
            _fadeCoroutine = _driver.StartCoroutine(FadeBGM(clip, Mathf.Max(MinFadeSeconds, fadeTime), path, ClampPitch(pitch)));
        }

        private static IEnumerator FadeBGM(AudioClip newClip, float duration, string newPath, float pitch)
        {
            float startVol = _bgmSource.volume;
            // 淡出
            for (float t = 0; t < duration / 2; t += Time.deltaTime)
            {
                _bgmSource.volume = Mathf.Lerp(startVol, 0, t / (duration / 2));
                yield return null;
            }

            // 切换
            if (!string.IsNullOrEmpty(_currentBgmPath)) ResCore.Release<AudioClip>(ResLoadType.Resources, _currentBgmPath);
            _bgmSource.clip = newClip;
            _bgmSource.pitch = pitch;
            _currentBgmPath = newPath;
            _bgmSource.Play();

            // 淡入
            float targetVol = BgmVol * MasterVol;
            for (float t = 0; t < duration / 2; t += Time.deltaTime)
            {
                _bgmSource.volume = Mathf.Lerp(0, targetVol, t / (duration / 2));
                yield return null;
            }
            _bgmSource.volume = targetVol;
        }

        public static void PlaySFX(string path, float volScale = DefaultVolume, float pitch = DefaultPitch)
        {
            Init();
            if (string.IsNullOrWhiteSpace(path)) return;

            var clip = ResCore.Load<AudioClip>(ResLoadType.Resources, path);
            if (clip == null) return;

            var src = GetAvailableSource();
            src.clip = clip;
            src.volume = SfxVol * MasterVol * volScale;
            src.pitch = ClampPitch(pitch);
            src.Play();
            
            // 简单延时释放
            _driver.StartCoroutine(Release(path, clip.length));
        }

        public static void PlaySFXAtPoint(string path, Vector3 position, float volScale = DefaultVolume)
        {
            Init();
            var clip = ResCore.Load<AudioClip>(ResLoadType.Resources, path);
            if (clip == null) return;

            AudioSource.PlayClipAtPoint(clip, position, SfxVol * MasterVol * volScale);
            
            // 简单延时释放
            _driver.StartCoroutine(Release(path, clip.length));
        }

        public static void PlayUI(string path, float volScale = DefaultVolume, float pitch = DefaultPitch)
        {
            Init();
            if (string.IsNullOrWhiteSpace(path)) return;

            var clip = ResCore.Load<AudioClip>(ResLoadType.Resources, path);
            if (clip == null) return;

            _uiSource.pitch = ClampPitch(pitch);
            _uiSource.PlayOneShot(clip, UiVol * MasterVol * volScale);
            
            // 简单延时释放
            _driver.StartCoroutine(Release(path, clip.length));
        }

        public static void PlayVoice(string path, float volScale = DefaultVolume, float pitch = DefaultPitch)
        {
            Init();
            if (string.IsNullOrWhiteSpace(path)) return;

            var clip = ResCore.Load<AudioClip>(ResLoadType.Resources, path);
            if (clip == null) return;

            var src = GetAvailableSource();
            src.clip = clip;
            src.volume = VoiceVol * MasterVol * volScale;
            src.pitch = ClampPitch(pitch);
            src.Play();
            
            // 简单延时释放
            _driver.StartCoroutine(Release(path, clip.length));
        }

        public static void PlayVoiceAtPoint(string path, Vector3 position, float volScale = DefaultVolume)
        {
            Init();
            var clip = ResCore.Load<AudioClip>(ResLoadType.Resources, path);
            if (clip == null) return;

            AudioSource.PlayClipAtPoint(clip, position, VoiceVol * MasterVol * volScale);
            
            // 简单延时释放
            _driver.StartCoroutine(Release(path, clip.length));
        }

        private static IEnumerator Release(string path, float delay)
        {
            yield return new WaitForSeconds(delay);
            ResCore.Release<AudioClip>(ResLoadType.Resources, path);
        }

        public static void StopBGM(bool releaseClip = true)
        {
            if (_root == null) return;

            if (_fadeCoroutine != null)
            {
                _driver.StopCoroutine(_fadeCoroutine);
                _fadeCoroutine = null;
            }

            _bgmSource.Stop();
            _bgmSource.clip = null;

            if (releaseClip && !string.IsNullOrEmpty(_currentBgmPath))
            {
                ResCore.Release<AudioClip>(ResLoadType.Resources, _currentBgmPath);
            }

            _currentBgmPath = null;
        }

        public static void PauseAll()
        {
            if (_root == null) return;

            _bgmSource.Pause();
            _uiSource.Pause();
            for (int i = 0; i < _sfxPool.Count; i++)
            {
                _sfxPool[i].Pause();
            }
        }

        public static void ResumeAll()
        {
            if (_root == null) return;

            _bgmSource.UnPause();
            _uiSource.UnPause();
            for (int i = 0; i < _sfxPool.Count; i++)
            {
                _sfxPool[i].UnPause();
            }
        }

        private static float BuildPitch(float basePitch, float randomRange)
        {
            float clampedBase = ClampPitch(basePitch);
            float clampedRange = Mathf.Clamp(randomRange, MinVolume, MaxPitchRandomRange);
            if (clampedRange <= 0f)
            {
                return clampedBase;
            }

            return ClampPitch(clampedBase + Random.Range(-clampedRange, clampedRange));
        }

        private static float ClampVolume(float value)
        {
            return Mathf.Clamp(value, MinVolume, MaxVolume);
        }

        private static float ClampPitch(float value)
        {
            return Mathf.Clamp(value, MinPitch, MaxPitch);
        }

        private static AudioSource GetAvailableSource()
        {
            foreach (var s in _sfxPool) if (!s.isPlaying) return s;
            return _sfxPool[0];
        }
    }
}