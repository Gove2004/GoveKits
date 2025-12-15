using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;       // 依赖 DOTween
using GoveKits.Save;     // 依赖 SaveManager
using GoveKits.Res; // 依赖 ResManager

namespace GoveKits.Audio
{
    /// <summary>
    /// 静态音频管理器 (无 Mixer 版，集成自动资源管理)
    /// </summary>
    public static class AudioManager
    {
        // --- 常量与配置 ---
        public const string SavePath = "AudioSetting";
        private const int SFX_POOL_SIZE = 15;

        // --- 运行时引用 ---
        private static GameObject _rootObj;
        private static AudioSource _bgmSource;
        private static AudioSource _uiSource;
        private static List<AudioSource> _sfxSources = new List<AudioSource>();
        
        // --- 状态与缓存控制 ---
        private static Tweener _bgmTweener;
        private static bool _isInitialized = false;

        // BGM 路径记录 (用于单曲引用的释放)
        private static string _currentBGMPath = "";
        
        // SFX/UI 路径集合 (用于关卡结束时的批量释放)
        // HashSet 自动去重，防止同一个音效播放多次导致记录膨胀，但 ResManager 内部引用计数会正确处理多次 Load
        private static readonly HashSet<string> _loadedAudioPaths = new HashSet<string>();

        // --- 数据 ---
        public static AudioSetting Setting { get; private set; } = new AudioSetting();

        /// <summary>
        /// 初始化音频系统
        /// </summary>
        public static void Initialize()
        {
            if (_isInitialized) return;

            // 1. 加载音量设置
            LoadSettings();

            // 2. 创建承载对象 (DontDestroyOnLoad)
            _rootObj = new GameObject("[AudioManager_Runtime]");
            Object.DontDestroyOnLoad(_rootObj);

            // 3. 初始化 BGM Source
            _bgmSource = _rootObj.AddComponent<AudioSource>();
            _bgmSource.playOnAwake = false;
            _bgmSource.loop = true;

            // 4. 初始化 UI Source
            _uiSource = _rootObj.AddComponent<AudioSource>();
            _uiSource.playOnAwake = false;

            // 5. 初始化 SFX 对象池
            for (int i = 0; i < SFX_POOL_SIZE; i++)
            {
                var source = _rootObj.AddComponent<AudioSource>();
                source.playOnAwake = false;
                _sfxSources.Add(source);
            }

            // 6. 应用初始音量
            RefreshAllVolumes();

            _isInitialized = true;
            LogManager.LogGreen("AudioManager", "Initialized.");
        }

        #region BGM 控制

        /// <summary>
        /// 播放背景音乐 (自动管理资源引用)
        /// </summary>
        public static void PlayBGM(string path, bool loop = true, float fadeDuration = 1f)
        {
            // 0. 如果路径没变，直接返回（避免重复加载）
            if (_currentBGMPath == path) return;

            // 1. 加载新 BGM (ResManager 引用计数 +1)
            // 使用泛型 Load，支持 Res/AB/AA 策略切换
            AudioClip clip = ResManager.Load<AudioClip>(path);

            if (clip != null)
            {
                // 2. 释放旧 BGM (ResManager 引用计数 -1)
                // 必须在 Load 之后 Release，确保如果是同一资源，引用计数不会归零导致卸载
                if (!string.IsNullOrEmpty(_currentBGMPath))
                {
                    ResManager.Release(_currentBGMPath);
                }

                // 3. 更新路径记录
                _currentBGMPath = path;

                // 4. 执行播放逻辑
                PlayBGM(clip, loop, fadeDuration);
            }
            else
            {
                LogManager.LogError("AudioManager", $"BGM Load Failed: {path}");
            }
        }

        public static void PlayBGM(AudioClip clip, bool loop = true, float fadeDuration = 1f)
        {
            CheckInit();
            if (_bgmSource.clip == clip && _bgmSource.isPlaying) return;

            _bgmTweener?.Kill();

            float targetVol = Setting.BGMVolume * Setting.MasterVolume;

            // 淡入淡出逻辑
            if (_bgmSource.isPlaying && _bgmSource.volume > 0.01f)
            {
                // 先淡出旧的
                _bgmTweener = _bgmSource.DOFade(0, fadeDuration * 0.5f)
                    .SetEase(Ease.Linear)
                    .OnComplete(() =>
                    {
                        _bgmSource.clip = clip;
                        _bgmSource.loop = loop;
                        _bgmSource.Play();
                        // 再淡入新的
                        _bgmTweener = _bgmSource.DOFade(targetVol, fadeDuration * 0.5f).SetEase(Ease.Linear);
                    });
            }
            else
            {
                // 直接播放并淡入
                _bgmSource.clip = clip;
                _bgmSource.loop = loop;
                _bgmSource.volume = 0;
                _bgmSource.Play();
                _bgmTweener = _bgmSource.DOFade(targetVol, fadeDuration).SetEase(Ease.Linear);
            }
        }

        public static void StopBGM(float fadeDuration = 1f)
        {
            CheckInit();
            _bgmTweener?.Kill();

            if (fadeDuration <= 0)
            {
                _bgmSource.Stop();
                _bgmSource.volume = 0;
            }
            else
            {
                _bgmTweener = _bgmSource.DOFade(0, fadeDuration)
                    .SetEase(Ease.Linear)
                    .OnComplete(() => _bgmSource.Stop());
            }
        }

        public static void PauseBGM() { CheckInit(); _bgmSource.Pause(); }
        public static void ResumeBGM() { CheckInit(); _bgmSource.UnPause(); }

        #endregion

        #region SFX / UI 控制

        /// <summary>
        /// 播放音效 (自动记录引用)
        /// </summary>
        public static void PlaySFX(string path, float volumeScale = 1f)
        {
            // 1. 加载资源 (Ref +1)
            AudioClip clip = ResManager.Load<AudioClip>(path);
            
            if (clip != null)
            {
                // 2. 记录路径，用于后续统一释放
                _loadedAudioPaths.Add(path);

                // 3. 播放
                PlaySFX(clip, volumeScale);
            }
        }

        public static void PlaySFX(AudioClip clip, float volumeScale = 1f)
        {
            CheckInit();
            AudioSource source = GetAvailableSFXSource();
            
            // 最终音量 = 基础配置 * 主音量 * 本次缩放
            float finalVol = Setting.SFXVolume * Setting.MasterVolume * volumeScale;
            
            source.clip = clip;
            source.volume = finalVol;
            source.loop = false;
            source.Play();
        }

        /// <summary>
        /// 播放 UI 音效 (自动记录引用)
        /// </summary>
        public static void PlayUISound(string path, float volumeScale = 1f)
        {
            AudioClip clip = ResManager.Load<AudioClip>(path);
            if(clip != null) 
            {
                _loadedAudioPaths.Add(path);
                PlayUISound(clip, volumeScale);
            }
        }

        public static void PlayUISound(AudioClip clip, float volumeScale = 1f)
        {
            CheckInit();
            float finalVol = Setting.UIVolume * Setting.MasterVolume * volumeScale;
            _uiSource.PlayOneShot(clip, finalVol);
        }

        public static void StopAllSFX()
        {
            if (!_isInitialized) return;
            foreach (var s in _sfxSources) s.Stop();
        }

        private static AudioSource GetAvailableSFXSource()
        {
            // 简单策略：找空闲的，没有则打断最早的(index 0)
            foreach (var s in _sfxSources)
            {
                if (!s.isPlaying) return s;
            }
            return _sfxSources[0];
        }

        #endregion

        #region 资源清理 (关键)

        /// <summary>
        /// 清理关卡音频缓存
        /// <para>请在【切换场景】或【关卡卸载】时调用此方法</para>
        /// </summary>
        public static void ClearLevelAudioCache()
        {
            // 释放所有通过 PlaySFX / PlayUISound 加载的资源引用
            foreach (var path in _loadedAudioPaths)
            {
                ResManager.Release(path);
            }
            _loadedAudioPaths.Clear();

            LogManager.LogGreen("AudioManager", "Level Audio Cache Cleared.");
        }

        /// <summary>
        /// 完全重置 (包括 BGM)
        /// </summary>
        public static void ReleaseAll()
        {
            // 1. 停止并释放 BGM
            StopBGM(0);
            if (!string.IsNullOrEmpty(_currentBGMPath))
            {
                ResManager.Release(_currentBGMPath);
                _currentBGMPath = "";
            }

            // 2. 释放所有 SFX
            ClearLevelAudioCache();
        }

        #endregion

        #region 音量控制

        public static void SetVolume(AudioChannel channel, float volume, bool saveImmediately = true)
        {
            volume = Mathf.Clamp01(volume);

            switch (channel)
            {
                case AudioChannel.Master:
                    Setting.MasterVolume = volume;
                    RefreshAllVolumes(); // Master 影响所有
                    break;
                case AudioChannel.BGM:
                    Setting.BGMVolume = volume;
                    UpdateBGMVolume();
                    break;
                case AudioChannel.SFX:
                    Setting.SFXVolume = volume;
                    break;
                case AudioChannel.UI:
                    Setting.UIVolume = volume;
                    _uiSource.volume = Setting.UIVolume * Setting.MasterVolume;
                    break;
                case AudioChannel.Voice:
                    Setting.VoiceVolume = volume;
                    break;
            }

            if (saveImmediately)
                SaveSettings();
        }

        private static void RefreshAllVolumes()
        {
            if (!_isInitialized) return;

            UpdateBGMVolume();
            _uiSource.volume = Setting.UIVolume * Setting.MasterVolume;
            // 注意：正在播放的 SFX 此处未实时更新，只影响新播放的
        }

        private static void UpdateBGMVolume()
        {
            if (_bgmSource == null) return;
            // 避免打断正在进行的 Fade 动画
            if (_bgmTweener != null && _bgmTweener.IsActive()) return;
            
            _bgmSource.volume = Setting.BGMVolume * Setting.MasterVolume;
        }

        public static float GetVolume(AudioChannel channel)
        {
            switch (channel)
            {
                case AudioChannel.Master: return Setting.MasterVolume;
                case AudioChannel.BGM: return Setting.BGMVolume;
                case AudioChannel.SFX: return Setting.SFXVolume;
                case AudioChannel.UI: return Setting.UIVolume;
                case AudioChannel.Voice: return Setting.VoiceVolume;
                default: return 1f;
            }
        }

        #endregion

        #region 持久化

        private static void SaveSettings()
        {
            // 假设 SaveManager.SaveData 已适配
            SaveManager.SaveData(Setting, SavePath);
        }

        private static void LoadSettings()
        {
            // 假设 SaveManager.TryLoad 已适配泛型
            if (SaveManager.TryLoadData<AudioSetting>(SavePath, out AudioSetting loadedData))
            {
                Setting = loadedData;
            }
            else
            {
                Setting = new AudioSetting();
            }
        }

        #endregion

        private static void CheckInit()
        {
            if (!_isInitialized)
            {
                LogManager.LogError("AudioManager", "Not Initialized! Call AudioManager.Initialize() first.");
                Initialize(); // 自动补救
            }
        }
    }
}