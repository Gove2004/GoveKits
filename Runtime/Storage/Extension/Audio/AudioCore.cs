using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GoveKits.Runtime.Core;
using Random = UnityEngine.Random;

namespace GoveKits.Runtime.Storage
{
    public static class AudioCore
    {
        // ---------------- 内部类：音频播放节点 ----------------
        private class AudioNode
        {
            public AudioSource Source;
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

        private static GameObject _root;
        private static AudioCoreDriver _driver;
        
        // BGM 专属音轨
        private static AudioSource _bgmSource;
        private static Coroutine _fadeCoroutine;
 
        // 动态音轨池 (SFX, UI, Voice 等通用)
        private static readonly List<AudioNode> _audioPool = new();

        // 字典化音量管理，彻底消灭 Switch case，实现高扩展
        private static readonly Dictionary<AudioChannel, float> _volumes = new();

        public static void Initialize(int initialPoolSize = 16)
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
            _bgmSource.priority = 0;  // 避免BGM被满载裁切

            // 2. 初始化音量字典
            foreach (AudioChannel channel in Enum.GetValues(typeof(AudioChannel)))
            {
                float savedVol = PrefsCore.GetFloat(AudioPreKey + channel.ToString(), 1f);
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
        
        public static float GetVolume(AudioChannel channel)
        {
            return _volumes.TryGetValue(channel, out float vol) ? vol : 1f;
        }

        public static void SetVolume(AudioChannel channel, float vol)
        {
            vol = Mathf.Clamp01(vol);
            _volumes[channel] = vol;
            PrefsCore.SetFloat(AudioPreKey + channel.ToString(), vol);
            PrefsCore.Save();

            ApplyAllVolumes();
        }

        private static void ApplyAllVolumes()
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

        private static float GetBaseVolume(AudioChannel channel)
        {
            return GetVolume(channel) * GetVolume(AudioChannel.Master);
        }

        public static void Play(AudioSO audioSO)
        {
            switch (audioSO.Channel)
            {
                case AudioChannel.BGM:
                    PlayBGM(audioSO.ClipPath, audioSO.Pitch, audioSO.PitchRandomRange);
                    break;
                case AudioChannel.SFX:
                case AudioChannel.UI:
                case AudioChannel.Voice:
                case AudioChannel.Ambient:
                    PlayDynamic(audioSO.Channel, audioSO.ClipPath, audioSO.Volume, audioSO.Pitch, audioSO.Loop);
                    break;
            }
        }

        public static void PlayBGM(AudioClip clip, float fadeTime = 1f, float pitch = 1f)
        {
            if (_fadeCoroutine != null) _driver.StopCoroutine(_fadeCoroutine);
            _fadeCoroutine = _driver.StartCoroutine(FadeBGM(clip, fadeTime, pitch));
        }

        // 通用播放方法
        public static void PlayDynamic(AudioChannel channel, AudioClip clip, float volScale = 1f, float pitch = 1f, bool loop = false, Vector3? position = null)
        {
            AudioNode node = GetAvailableNode();

            node.IsActive = true;
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
        private static void OnUpdate()
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

        public static void StopAllChannel(AudioChannel channel)
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

        public static void PauseAll()
        {
            if (_root == null) return;
            _bgmSource.Pause();
            foreach (var node in _audioPool) if (node.IsActive) node.Source.Pause();
        }

        public static void ResumeAll()
        {
            if (_root == null) return;
            _bgmSource.UnPause();
            foreach (var node in _audioPool) if (node.IsActive) node.Source.UnPause();
        }

        public static void StopBGM()
        {
            if (_root == null) return;

            if (_fadeCoroutine != null)
            {
                _driver.StopCoroutine(_fadeCoroutine);
                _fadeCoroutine = null;
            }

            _bgmSource.Stop();
            _bgmSource.clip = null;
        }

        public static void OnShutdown()
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

        private static AudioNode GetAvailableNode()
        {
            foreach (var node in _audioPool)
            {
                if (!node.IsActive) return node;
            }
            // 动态扩容：池子满了自动增加，保证多语音/多UI同时播放不被强行切断
            return CreateNewAudioNode();
        }

        private static AudioNode CreateNewAudioNode()
        {
            var src = _root.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.priority = 128; 
            var node = new AudioNode { Source = src, IsActive = false };
            _audioPool.Add(node);
            return node;
        }

        private static void RecycleNode(AudioNode node)
        {
            node.IsActive = false;
            node.Source.clip = null;
        }

        private static IEnumerator FadeBGM(AudioClip newClip, float duration, float pitch)
        {
            float startVol = _bgmSource.volume;
            // 淡出
            for (float t = 0; t < duration / 2; t += Time.deltaTime)
            {
                _bgmSource.volume = Mathf.Lerp(startVol, 0, t / (duration / 2));
                yield return null;
            }

            _bgmSource.clip = newClip;
            _bgmSource.pitch = pitch;
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


        public static void Clear()
        {
            _audioPool.Clear();
            _volumes.Clear();
            _root = null;
            _driver = null;
            _bgmSource = null;
            _fadeCoroutine = null;
        }
    }
}