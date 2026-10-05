using System;
using System.Collections;
using System.Collections.Generic;
using GoveKits.Runtime.Util;
using UnityEngine;

namespace GoveKits.Runtime.Storage
{
    /// <summary>
    /// 音频引擎核心，支持多通道音量管理、BGM 淡入淡出和动态音频源池。
    /// </summary>
    /// <remarks>
    /// <b>跨 Play 会话契约</b>（使用方关闭 Domain Reload 时）：本类全部状态是静态的，
    /// 会跨 Play 会话存活，而挂载 AudioSource 的 <c>[AudioCore]</c> 是 GameObject，
    /// 退出 Play 必被销毁。因此<b>所有触碰池的路径都必须判 Unity fake-null</b>
    /// （<see cref="IsDead"/>），并且 <see cref="Close"/> 不做提前返回 ——
    /// 否则池里会留下"已销毁的 AudioSource"，下一局每帧 MissingReferenceException。
    /// </remarks>
    public static class AudioCore
    {
        public const string AudioPrefPrefix = "Audio.Vol.";
        private static GameObject _root;
        private static AudioCoreDriver _driver;
        private static AudioSource _bgmSource;
        private static Coroutine _fadeCoroutine;

        private static readonly List<AudioNode> _audioPool = new();
        private static readonly Dictionary<AudioChannel, float> _volumes = new();
        private static bool _isFadingBGM;

        // 音量存盘节流：SetVolume（滑条拖动）每帧调用时不立即写盘，由 OnUpdate 延迟统一保存
        private static bool _volumeSaveDirty;
        private static float _volumeSaveDueTime;

        /// <summary>池化节点。必须是 class：池内以引用共享，保证 IsActive 等状态修改能反映到 _audioPool。</summary>
        private class AudioNode
        {
            public AudioSource Source;
            public AudioChannel Channel;
            public bool IsActive;

            /// <summary>
            /// 正被 <see cref="PauseAll"/> 暂停中。
            /// </summary>
            /// <remarks>
            /// <b>为什么自己要记这个标志</b>：Unity 的 <c>AudioSource</c> <b>没有</b> <c>isPaused</c> 属性
            /// （只有 <c>isPlaying</c>，而它在暂停时同样返回 <c>false</c>）——
            /// 光看 <c>Source</c> 分不出"播完了"和"暂停了"。所以暂停状态只能由
            /// <see cref="PauseAll"/> / <see cref="ResumeAll"/> 自己维护。
            /// </remarks>
            public bool Paused;
        }

        private class AudioCoreDriver : MonoBehaviour
        {
            public Action OnUpdate;
            private void Update() => OnUpdate?.Invoke();
        }

        /// <summary>
        /// 初始化音频引擎。创建根节点、BGM 音源和音频池。
        /// </summary>
        /// <param name="initialPoolSize">初始音频源池大小。</param>
        public static void Setup(int initialPoolSize = 16)
        {
            // 域重载关闭时静态池跨 Play 会话存活，先扫掉上一局可能留下的死节点（幂等无害）
            PurgeDeadNodes();

            if (_root != null) return;

            _root = new GameObject("[AudioCore]");
            UnityEngine.Object.DontDestroyOnLoad(_root);
            _driver = _root.AddComponent<AudioCoreDriver>();
            _driver.OnUpdate += OnUpdate;

            // BGM 专用音源
            _bgmSource = _root.AddComponent<AudioSource>();
            _bgmSource.loop = true;
            _bgmSource.playOnAwake = false;
            _bgmSource.priority = 0;

            // 初始化音量字典（从 PlayerPrefs 恢复）
            foreach (AudioChannel channel in Enum.GetValues(typeof(AudioChannel)))
            {
                _volumes[channel] = PrefsCore.GetFloat(AudioPrefPrefix + channel.ToString(), 1f);
            }

            // 预热对象池
            for (int i = 0; i < initialPoolSize; i++)
            {
                CreateNewAudioNode();
            }

            ApplyAllVolumes();
            LogCore.Success(nameof(AudioCore), $"音频引擎初始化完成，初始池大小: {initialPoolSize}");
        }

        #region 音量管理

        /// <summary>
        /// 获取指定通道的当前音量。
        /// </summary>
        /// <param name="channel">音频通道。</param>
        /// <returns>音量值（0~1）。</returns>
        public static float GetVolume(AudioChannel channel)
            => _volumes.TryGetValue(channel, out float vol) ? vol : 1f;

        /// <summary>
        /// 设置指定通道的音量。音量会自动钳制到 0~1 范围，实际变化时才持久化到 PlayerPrefs。
        /// </summary>
        /// <param name="channel">音频通道。</param>
        /// <param name="vol">音量值（0~1）。</param>
        public static void SetVolume(AudioChannel channel, float vol)
        {
            EnsureSetup();
            vol = Mathf.Clamp01(vol);

            // 音量未实际变化时直接返回，避免重复写盘
            if (_volumes.TryGetValue(channel, out float old) && Mathf.Approximately(old, vol)) return;

            _volumes[channel] = vol;
            PrefsCore.SetFloat(AudioPrefPrefix + channel.ToString(), vol);

            // 延迟统一写盘：滑条拖动每帧触发 SetVolume，逐次 Save 会造成高频 IO
            _volumeSaveDirty = true;
            _volumeSaveDueTime = Time.unscaledTime + 1f;

            ApplyAllVolumes();
        }

        private static void ApplyAllVolumes()
        {
            // BGM 淡入淡出期间由协程按基础音量增量控制，避免互相覆盖
            if (!_isFadingBGM)
                _bgmSource.volume = GetVolume(AudioChannel.BGM) * GetVolume(AudioChannel.Master);

            foreach (var node in _audioPool)
            {
                if (IsDead(node)) continue;   // Unity fake-null：上一局退出 Play 留下的尸体
                if (node.IsActive)
                    node.Source.volume = GetBaseVolume(node.Channel);
            }
        }

        private static float GetBaseVolume(AudioChannel channel)
            => GetVolume(channel) * GetVolume(AudioChannel.Master);

        #endregion

        #region 播放

        /// <summary>
        /// 根据 AudioSO 配置播放音频。自动根据通道类型选择 BGM 或动态播放。
        /// </summary>
        /// <param name="audioSO">音频资源配置。</param>
        public static void Play(AudioSO audioSO)
        {
            switch (audioSO.Channel)
            {
                case AudioChannel.BGM:
                    PlayBGM(audioSO.Clip, audioSO.Pitch);
                    break;
                case AudioChannel.SFX:
                case AudioChannel.UI:
                case AudioChannel.Voice:
                case AudioChannel.Ambient:
                    PlayDynamic(audioSO.Channel, audioSO.Clip, audioSO.Volume, audioSO.Pitch, audioSO.Loop);
                    break;
                default:
                    LogCore.Warning(nameof(AudioCore), $"未知的音频通道类型: {audioSO.Channel}，已忽略播放请求");
                    break;
            }
        }

        /// <summary>
        /// 播放背景音乐，支持淡入淡出效果。引擎未初始化时会自动调用 Setup。
        /// </summary>
        /// <param name="clip">要播放的音频剪辑。</param>
        /// <param name="fadeTime">淡入淡出持续时间（秒）。</param>
        /// <param name="pitch">播放速率。</param>
        public static void PlayBGM(AudioClip clip, float fadeTime = 1f, float pitch = 1f)
        {
            EnsureSetup();
            if (_fadeCoroutine != null) _driver.StopCoroutine(_fadeCoroutine);
            _fadeCoroutine = _driver.StartCoroutine(FadeBGM(clip, fadeTime, pitch));
        }

        /// <summary>
        /// 动态播放音频（SFX/UI/Voice/Ambient 通道）。引擎未初始化时会自动调用 Setup。
        /// </summary>
        /// <param name="channel">音频通道。</param>
        /// <param name="clip">音频剪辑。</param>
        /// <param name="volScale">音量缩放系数。</param>
        /// <param name="pitch">播放速率。</param>
        /// <param name="loop">是否循环播放。</param>
        /// <param name="position">空间位置（为 null 时为 2D 音频）。</param>
        public static void PlayDynamic(AudioChannel channel, AudioClip clip, float volScale = 1f, float pitch = 1f, bool loop = false, Vector3? position = null)
        {
            EnsureSetup();
            var node = GetAvailableNode();
            node.IsActive = true;
            node.Paused = false;      // 复用来的节点必须清掉暂停态，否则它永远等不到回收
            node.Channel = channel;
            node.Source.clip = clip;
            node.Source.volume = GetBaseVolume(channel) * volScale;
            node.Source.pitch = Mathf.Clamp(pitch, 0f, 3f);
            node.Source.loop = loop;

            if (position.HasValue)
            {
                node.Source.transform.position = position.Value;
                node.Source.spatialBlend = 1f;
            }
            else
            {
                node.Source.transform.localPosition = Vector3.zero;
                node.Source.spatialBlend = 0f;
            }

            node.Source.Play();
        }

        #endregion

        #region 控制

        private static void OnUpdate()
        {
            // 音量存盘节流：脏标记超过 1 秒未再变动时统一落盘
            if (_volumeSaveDirty && Time.unscaledTime >= _volumeSaveDueTime)
            {
                _volumeSaveDirty = false;
                PrefsCore.Save();
            }

            for (int i = _audioPool.Count - 1; i >= 0; i--)
            {
                var node = _audioPool[i];
                if (IsDead(node))
                {
                    _audioPool.RemoveAt(i);   // Unity fake-null：上一局退出 Play 留下的尸体
                    continue;
                }

                // 暂停中的节点不能回收：PauseAll 之后 Source.isPlaying 是 false，
                // 不判它的话暂停中的音效会被当"播完了"清掉，ResumeAll 再也唤不醒。
                // ⚠ 判的是 AudioNode.Paused 而不是什么 Source.isPaused ——
                //   Unity 的 AudioSource 没有那个属性（编译期就会炸）。
                if (node.IsActive && !node.Paused && !node.Source.isPlaying)
                    RecycleNode(node);
            }
        }

        /// <summary>
        /// 停止指定通道的全部音频。
        /// </summary>
        /// <param name="channel">要停止的音频通道。</param>
        public static void StopAllChannel(AudioChannel channel)
        {
            foreach (var node in _audioPool)
            {
                if (IsDead(node)) continue;
                if (node.IsActive && node.Channel == channel)
                {
                    node.Source.Stop();
                    RecycleNode(node);
                }
            }
        }

        /// <summary>
        /// 暂停所有音频通道的播放（包括 BGM 和动态音频）。
        /// </summary>
        /// <remarks>
        /// 暂停状态记在 <see cref="AudioNode.Paused"/> 上：<c>AudioSource</c> 不暴露"是否暂停"，
        /// 光靠 <c>isPlaying</c> 会让回放循环把暂停中的音效误判成"播完了"而回收掉。
        /// </remarks>
        public static void PauseAll()
        {
            if (_root == null) return;
            _bgmSource.Pause();

            foreach (var node in _audioPool)
            {
                if (IsDead(node) || !node.IsActive) continue;
                node.Source.Pause();
                node.Paused = true;
            }
        }

        /// <summary>
        /// 恢复所有音频通道的播放（包括 BGM 和动态音频）。
        /// </summary>
        public static void ResumeAll()
        {
            if (_root == null) return;
            _bgmSource.UnPause();

            foreach (var node in _audioPool)
            {
                if (IsDead(node) || !node.IsActive) continue;
                node.Source.UnPause();
                node.Paused = false;
            }
        }

        /// <summary>
        /// 停止背景音乐播放并清理相关协程。
        /// </summary>
        public static void StopBGM()
        {
            // ⚠ 淡入淡出标志必须在任何早退之前复位：退出 Play 时 _root 已 fake-null，
            //   老写法在下面那行守卫直接返回，_isFadingBGM 可能带着 true 跨局存活
            //   （静态字段 + 域重载关闭）—— 下一局 ApplyAllVolumes 会一直以为"淡入淡出中"，
            //   不敢写 BGM 音量（SetVolume 对 BGM 静默失效，直到下一次 PlayBGM 走完整个淡入）。
            _isFadingBGM = false;

            if (_fadeCoroutine != null)
            {
                if (_driver != null) _driver.StopCoroutine(_fadeCoroutine);
                _fadeCoroutine = null;
            }

            // 引擎已死（fake-null）：上面该清的状态已清完，这里没得停也不必停
            if (_root == null) return;

            _bgmSource.Stop();
            _bgmSource.clip = null;
        }

        #endregion

        #region 内部方法

        /// <summary>确保引擎已初始化。Setup 仅创建内部 GameObject，不依赖外部资源，可安全自动调用。</summary>
        private static void EnsureSetup()
        {
            if (_root == null)
            {
                LogCore.Warning(nameof(AudioCore), "音频引擎尚未初始化，已自动调用 Setup()");
                Setup();
            }
        }

        private static AudioNode GetAvailableNode()
        {
            PurgeDeadNodes();
            foreach (var node in _audioPool)
            {
                if (!node.IsActive) return node;
            }
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

        /// <summary>
        /// 节点是否已死（Unity fake-null）。Unity 重载了 <c>==</c>，销毁对象 <c>== null</c> 为真。
        /// </summary>
        /// <remarks>
        /// 域重载关闭时静态池跨 Play 会话存活；上一局退出 Play 时 <c>[AudioCore]</c>（DontDestroyOnLoad）
        /// 被销毁，而 <see cref="Close"/> 的老守卫（判 <c>_driver</c>）可能先被跳过 ——
        /// 池里会留下「已销毁的 AudioSource + IsActive = true」的尸体。
        /// 不判它的话，<see cref="OnUpdate"/> 遍历到它就每帧抛 MissingReferenceException，
        /// Console 的 Error Pause 还会因此自动暂停编辑器（表现为"演出卡死 / 输入无响应"）。
        /// </remarks>
        private static bool IsDead(AudioNode node) => node == null || node.Source == null;

        /// <summary>把池里 Source 已销毁的节点剔掉（倒序移除，幂等）。</summary>
        private static void PurgeDeadNodes()
        {
            for (int i = _audioPool.Count - 1; i >= 0; i--)
                if (IsDead(_audioPool[i])) _audioPool.RemoveAt(i);
        }

        private static void RecycleNode(AudioNode node)
        {
            node.IsActive = false;
            node.Paused = false;      // 回收的节点不能带着"暂停中"的残留状态
            node.Source.clip = null;
        }

        private static IEnumerator FadeBGM(AudioClip newClip, float duration, float pitch)
        {
            _isFadingBGM = true;
            float half = Mathf.Max(duration / 2f, 0.01f);

            // 淡出：按当前基础音量的比例递减，SetVolume 期间调整仍会平滑生效
            for (float t = 0; t < half; t += Time.deltaTime)
            {
                _bgmSource.volume = GetBaseVolume(AudioChannel.BGM) * (1f - t / half);
                yield return null;
            }
            _bgmSource.volume = 0f;

            _bgmSource.clip = newClip;
            _bgmSource.pitch = pitch;
            _bgmSource.Play();

            // 淡入：目标为当前基础音量而非固定值，避免与 SetVolume 相互跳变
            for (float t = 0; t < half; t += Time.deltaTime)
            {
                _bgmSource.volume = GetBaseVolume(AudioChannel.BGM) * (t / half);
                yield return null;
            }

            _isFadingBGM = false;
            _bgmSource.volume = GetBaseVolume(AudioChannel.BGM);
        }

        #endregion

        /// <summary>
        /// 关闭音频引擎，停止所有音频并释放资源。
        /// </summary>
        /// <remarks>
        /// <b>全程不早退</b>：退出 Play 时 Unity 会先销毁 <c>[AudioCore]</c>（<c>_root/_driver/_bgmSource</c>
        /// 变 fake-null），老的 <c>if (_driver == null) return;</c> 守卫在这一步就提前返回，
        /// 池与音量字典没清 —— 而它们是静态的，域重载关闭时会跨 Play 会话存活，
        /// 下一局就在尸体上跑（见 <see cref="IsDead"/> 的说明）。
        /// 各步都自带判空，所以这里对"从未初始化"与"重复关闭"都是安全的幂等空操作。
        /// </remarks>
        public static void Close()
        {
            // 关闭前若有待写盘的音量变更，立即落盘
            if (_volumeSaveDirty)
            {
                _volumeSaveDirty = false;
                PrefsCore.Save();
            }

            StopBGM();
            if (_driver != null) _driver.OnUpdate -= OnUpdate;

            foreach (var node in _audioPool)
            {
                if (!IsDead(node) && node.IsActive) RecycleNode(node);
            }

            _audioPool.Clear();
            _volumes.Clear();

            if (_root != null)
            {
                UnityEngine.Object.Destroy(_root);
            }
            _root = null;
            _driver = null;
            _bgmSource = null;
            _fadeCoroutine = null;
        }
    }
}
