using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GoveKits.Runtime.Core;
using Random = UnityEngine.Random;

namespace GoveKits.Runtime.Storage
{
    public class AudioCore : ICore
    {
        // ---------------- 内部类：音频播放节点 ----------------
        private class AudioNode
        {
            public AudioSource Source;
            public string AssetPath;
            public AudioChannel Channel;
            public bool IsActive;
        }

        // ---------------- 内部类：驱动器 ----------------
        private sealed class AudioCoreDriver : MonoBehaviour 
        {
            public Action OnUpdate;
            private void Update() => OnUpdate?.Invoke();
        }

        private const string AudioPreKey = "Audio.Vol.";

        private GameObject _root;
        private AudioCoreDriver _driver;
        
        // BGM 专属音轨
        private AudioSource _bgmSource;
        private string _currentBgmPath;
        private Coroutine _fadeCoroutine;

        // 动态音轨池 (SFX, UI, Voice 等通用)
        private readonly List<AudioNode> _audioPool = new();

        // 字典化音量管理，彻底消灭 Switch case，实现高扩展
        private readonly Dictionary<AudioChannel, float> _volumes = new();

        private PrefsCore prefsCore => CoreLocator.GetCore<PrefsCore>();
        private ResCore resCore => CoreLocator.GetCore<ResCore>();

        public AudioCore(int initialPoolSize = 10)
        {
            if (_root != null) return;

            _root = new GameObject("[AudioCore]");
            UnityEngine.Object.DontDestroyOnLoad(_root);
            _driver = _root.AddComponent<AudioCoreDriver>();
            _driver.OnUpdate += OnUpdate; // 注册轮询

            // 1. 初始化 BGM
            _bgmSource = _root.AddComponent<AudioSource>();
            _bgmSource.loop = true;
            _bgmSource.playOnAwake = false;

            // 2. 初始化音量字典
            foreach (AudioChannel channel in Enum.GetValues(typeof(AudioChannel)))
            {
                float savedVol = prefsCore.GetFloat(AudioPreKey + channel.ToString(), 1f);
                _volumes[channel] = savedVol;
            }

            // 3. 预热对象池
            for (int i = 0; i < initialPoolSize; i++)
            {
                CreateNewAudioNode();
            }

            ApplyAllVolumes();
        }

        // ======================== 音量管理 ========================
        
        public float GetVolume(AudioChannel channel)
        {
            return _volumes.TryGetValue(channel, out float vol) ? vol : 1f;
        }

        public void SetVolume(AudioChannel channel, float vol)
        {
            vol = Mathf.Clamp01(vol);
            _volumes[channel] = vol;
            prefsCore.SetFloat(AudioPreKey + channel.ToString(), vol);
            prefsCore.Save();

            ApplyAllVolumes();
        }

        private void ApplyAllVolumes()
        {
            // 刷新 BGM 音量
            _bgmSource.volume = GetVolume(AudioChannel.BGM) * GetVolume(AudioChannel.Master);

            // 刷新池中所有正在播放的音轨音量
            foreach (var node in _audioPool)
            {
                if (node.IsActive)
                {
                    node.Source.volume = GetBaseVolume(node.Channel);
                }
            }
        }

        private float GetBaseVolume(AudioChannel channel)
        {
            return GetVolume(channel) * GetVolume(AudioChannel.Master);
        }

        // ======================== 播放 API ========================

        public void Play(AudioSO config, Vector3? position = null)
        {
            if (config == null || string.IsNullOrWhiteSpace(config.ClipPath))
            {
                CoreLocator.Log.Warn(nameof(AudioCore), "Play ignored: Config or Path is invalid.");
                return;
            }

            float pitch = BuildPitch(config.Pitch, config.PitchRandomRange);
            float volScale = Mathf.Clamp01(config.Volume);

            if (config.Channel == AudioChannel.BGM)
            {
                PlayBGM(config.ClipPath, 1f, pitch);
            }
            else
            {
                // 通用播放：支持任意频道（UI, Voice, SFX等并发播放）
                PlayDynamic(config.Channel, config.ClipPath, volScale, pitch, config.Loop, position);
            }
        }

        public void PlayBGM(string path, float fadeTime = 1f, float pitch = 1f)
        {
            if (string.IsNullOrWhiteSpace(path) || _currentBgmPath == path) return;

            AudioClip clip = resCore.Load<AudioClip>(path);
            if (_fadeCoroutine != null) _driver.StopCoroutine(_fadeCoroutine);
            _fadeCoroutine = _driver.StartCoroutine(FadeBGM(clip, fadeTime, path, Mathf.Clamp(pitch, 0f, 3f)));
        }

        // 通用播放方法 (替代原来的 PlaySFX, PlayUI, PlayVoice)
        public void PlayDynamic(AudioChannel channel, string path, float volScale = 1f, float pitch = 1f, bool loop = false, Vector3? position = null)
        {
            if (string.IsNullOrWhiteSpace(path)) return;
            if (channel == AudioChannel.Master || channel == AudioChannel.BGM) return; // 拦截非法调用

            AudioClip clip = resCore.Load<AudioClip>(path);
            AudioNode node = GetAvailableNode();

            node.IsActive = true;
            node.AssetPath = path;
            node.Channel = channel;
            node.Source.clip = clip;
            node.Source.volume = GetBaseVolume(channel) * volScale;
            node.Source.pitch = Mathf.Clamp(pitch, 0f, 3f);
            node.Source.loop = loop;

            // 3D/2D 音效处理
            if (position.HasValue)
            {
                node.Source.transform.position = position.Value;
                node.Source.spatialBlend = 1f; // 开启 3D
            }
            else
            {
                node.Source.transform.localPosition = Vector3.zero;
                node.Source.spatialBlend = 0f; // 2D (UI等)
            }

            node.Source.Play();
        }

        // ======================== 控制与生命周期 ========================

        // 核心亮点：不再使用不可靠的延时携程，而是使用 Update 状态轮询
        private void OnUpdate()
        {
            for (int i = 0; i < _audioPool.Count; i++)
            {
                var node = _audioPool[i];
                // 如果节点被标记为激活，但声音已经停止播放，则回收资源
                if (node.IsActive && !node.Source.isPlaying)
                {
                    RecycleNode(node);
                }
            }
        }

        public void StopAllChannel(AudioChannel channel)
        {
            foreach (var node in _audioPool)
            {
                if (node.IsActive && node.Channel == channel)
                {
                    node.Source.Stop();
                    RecycleNode(node);
                }
            }
        }

        public void PauseAll()
        {
            if (_root == null) return;
            _bgmSource.Pause();
            foreach (var node in _audioPool) if (node.IsActive) node.Source.Pause();
        }

        public void ResumeAll()
        {
            if (_root == null) return;
            _bgmSource.UnPause();
            foreach (var node in _audioPool) if (node.IsActive) node.Source.UnPause();
        }

        public void StopBGM(bool releaseClip = true)
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
                resCore.ReleaseHandle(_currentBgmPath);
            }
            _currentBgmPath = null;
        }

        public void OnShutdown()
        {
            StopBGM();
            _driver.OnUpdate -= OnUpdate;
            foreach (var node in _audioPool)
            {
                if (node.IsActive) RecycleNode(node);
            }
            GameObject.Destroy(_root);
        }

        // ======================== 内部方法 ========================

        private AudioNode GetAvailableNode()
        {
            foreach (var node in _audioPool)
            {
                if (!node.IsActive) return node;
            }
            // 动态扩容：池子满了自动增加，保证多语音/多UI同时播放不被强行切断
            return CreateNewAudioNode();
        }

        private AudioNode CreateNewAudioNode()
        {
            var src = _root.AddComponent<AudioSource>();
            src.playOnAwake = false;
            var node = new AudioNode { Source = src, IsActive = false };
            _audioPool.Add(node);
            return node;
        }

        private void RecycleNode(AudioNode node)
        {
            node.IsActive = false;
            node.Source.clip = null;
            if (!string.IsNullOrEmpty(node.AssetPath))
            {
                resCore.ReleaseHandle(node.AssetPath);
                node.AssetPath = null;
            }
        }

        private IEnumerator FadeBGM(AudioClip newClip, float duration, string newPath, float pitch)
        {
            float startVol = _bgmSource.volume;
            // 淡出
            for (float t = 0; t < duration / 2; t += Time.deltaTime)
            {
                _bgmSource.volume = Mathf.Lerp(startVol, 0, t / (duration / 2));
                yield return null;
            }

            if (!string.IsNullOrEmpty(_currentBgmPath)) resCore.ReleaseHandle(_currentBgmPath);
            _bgmSource.clip = newClip;
            _bgmSource.pitch = pitch;
            _currentBgmPath = newPath;
            _bgmSource.Play();

            // 淡入
            float targetVol = GetBaseVolume(AudioChannel.BGM);
            for (float t = 0; t < duration / 2; t += Time.deltaTime)
            {
                _bgmSource.volume = Mathf.Lerp(0, targetVol, t / (duration / 2));
                yield return null;
            }
            _bgmSource.volume = targetVol;
        }

        private float BuildPitch(float basePitch, float randomRange)
        {
            return Mathf.Clamp(basePitch + Random.Range(-randomRange, +randomRange), 0f, 3f);
        }
    }
}