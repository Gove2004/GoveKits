using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GoveKits.Runtime.Storage.Res;
using GoveKits.Runtime.Storage.Save;

namespace GoveKits.Runtime.Util
{
    public static class AudioCore
    {
        private const string AudioPreKey = "Audio.";
        private const string AudioMasterKey = AudioPreKey + "Master";
        private const string AudioBgmKey = AudioPreKey + "BGM";
        private const string AudioSfxKey = AudioPreKey + "SFX";
        private const string AudioUiKey = AudioPreKey + "UI";
        private const string AudioVoiceKey = AudioPreKey + "Voice";
        private static GameObject _root;
        private static AudioSource _bgmSource;
        private static AudioSource _uiSource;
        private static readonly List<AudioSource> _sfxPool = new();
        private static Coroutine _fadeCoroutine;

        // 音量配置直接存入 AudioCore
        public static float MasterVol { get; private set; } = 1f;
        public static float BgmVol { get; private set; } = 1f;
        public static float SfxVol { get; private set; } = 1f;
        public static float UiVol { get; private set; } = 1f;
        public static float VoiceVol { get; private set; } = 1f;

        private static string _currentBgmPath;

        public static void Init()
        {
            if (_root != null) return;

            // 加载配置
            MasterVol = PrefsCore.GetFloat(AudioMasterKey, 1f);
            BgmVol = PrefsCore.GetFloat(AudioBgmKey, 1f);
            SfxVol = PrefsCore.GetFloat(AudioSfxKey, 1f);
            UiVol = PrefsCore.GetFloat(AudioUiKey, 1f);
            VoiceVol = PrefsCore.GetFloat(AudioVoiceKey, 1f);

            _root = new GameObject("[AudioCore]");
            UnityEngine.Object.DontDestroyOnLoad(_root);

            _bgmSource = _root.AddComponent<AudioSource>();
            _bgmSource.loop = true;
            _uiSource = _root.AddComponent<AudioSource>();

            for (int i = 0; i < 16; i++)
            {
                var src = _root.AddComponent<AudioSource>();
                src.playOnAwake = false;
                _sfxPool.Add(src);
            }
            ApplyVolume();
        }

        public static void SetVolume(AudioChannel channel, float vol)
        {
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

        public static void PlayBGM(string path, float fadeTime = 0.5f)
        {
            if (_currentBgmPath == path) return;
            var clip = ResCore.Load<AudioClip>(ResLoadType.Resources, path);
            if (clip == null) return;

            if (_fadeCoroutine != null) _root.GetComponent<MonoBehaviour>().StopCoroutine(_fadeCoroutine);
            _fadeCoroutine = _root.GetComponent<MonoBehaviour>().StartCoroutine(FadeBGM(clip, fadeTime, path));
        }

        private static IEnumerator FadeBGM(AudioClip newClip, float duration, string newPath)
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

        public static void PlaySFX(string path, float volScale = 1f)
        {
            var clip = ResCore.Load<AudioClip>(ResLoadType.Resources, path);
            if (clip == null) return;

            var src = GetAvailableSource();
            src.clip = clip;
            src.volume = SfxVol * MasterVol * volScale;
            src.Play();
            
            // 简单延时释放
            _root.GetComponent<MonoBehaviour>().StartCoroutine(Release(path, clip.length));
        }

        public static void PlaySFXAtPoint(string path, Vector3 position, float volScale = 1f)
        {
            var clip = ResCore.Load<AudioClip>(ResLoadType.Resources, path);
            if (clip == null) return;

            AudioSource.PlayClipAtPoint(clip, position, SfxVol * MasterVol * volScale);
            
            // 简单延时释放
            _root.GetComponent<MonoBehaviour>().StartCoroutine(Release(path, clip.length));
        }

        public static void PlayUI(string path, float volScale = 1f)
        {
            var clip = ResCore.Load<AudioClip>(ResLoadType.Resources, path);
            if (clip == null) return;

            _uiSource.PlayOneShot(clip, UiVol * MasterVol * volScale);
            
            // 简单延时释放
            _root.GetComponent<MonoBehaviour>().StartCoroutine(Release(path, clip.length));
        }

        public static void PlayVoice(string path, float volScale = 1f)
        {
            var clip = ResCore.Load<AudioClip>(ResLoadType.Resources, path);
            if (clip == null) return;

            var src = GetAvailableSource();
            src.clip = clip;
            src.volume = VoiceVol * MasterVol * volScale;
            src.Play();
            
            // 简单延时释放
            _root.GetComponent<MonoBehaviour>().StartCoroutine(Release(path, clip.length));
        }

        public static void PlayVoiceAtPoint(string path, Vector3 position, float volScale = 1f)
        {
            var clip = ResCore.Load<AudioClip>(ResLoadType.Resources, path);
            if (clip == null) return;

            AudioSource.PlayClipAtPoint(clip, position, VoiceVol * MasterVol * volScale);
            
            // 简单延时释放
            _root.GetComponent<MonoBehaviour>().StartCoroutine(Release(path, clip.length));
        }

        private static IEnumerator Release(string path, float delay)
        {
            yield return new WaitForSeconds(delay);
            ResCore.Release<AudioClip>(ResLoadType.Resources, path);
        }

        private static AudioSource GetAvailableSource()
        {
            foreach (var s in _sfxPool) if (!s.isPlaying) return s;
            return _sfxPool[0];
        }
    }
}