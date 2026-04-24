using System;
using System.Collections.Generic;
using GoveKits.Runtime.Core;

namespace GoveKits.Runtime.Network
{
    internal class ClientFrameExecutor
    {
        public const int MaxCatchUpFrames = 100; // 每次 Update 最多处理多少帧，防止卡死
        public float TickInterval { get; set; }
        public Action<FramePackage> OnFrameExecuted;
        public bool IsCatchingUp { get; private set; } = true; // 刚连上时默认处于追帧模式
        public int ExpectedFrameId { get; private set; } = 1; // 从第1帧开始期待

        // 队列变字典
        private readonly Dictionary<int, FramePackage> _frameDict = new Dictionary<int, FramePackage>();
        private float _accumulateTime = 0f;

        public void EnqueueFrame(FramePackage frame)
        {
            // 收到新帧，不管是历史补发还是最新广播，统统塞进字典
            if (frame.FrameId >= ExpectedFrameId && !_frameDict.ContainsKey(frame.FrameId))
            {
                _frameDict[frame.FrameId] = frame;
            }
        }

        public void StartCatchUpPhase()
        {
            IsCatchingUp = true;
            LogCore.Warning(nameof(ClientFrameExecutor), "进入追帧模式，正在努力追赶中...");
        }

        // 收到服务端的结束标记
        public void EndCatchUpPhase()
        {
            // 注意：只是收到标记，我们还要等字典里的帧消化到正常水平，才真正解除 CatchUp
            LogCore.Warning(nameof(ClientFrameExecutor), "收到追帧结束标记，等待消化剩余帧后进入正常模式...");
        }

        public void Update(float deltaTime)
        {
            // 智能状态切换：如果字典里积压的数据极少，说明追赶完毕，恢复正常节拍
            if (IsCatchingUp && _frameDict.Count <= 2)
            {
                IsCatchingUp = false;
                _accumulateTime = 0; // 重置节拍器
                LogCore.Success(nameof(ClientFrameExecutor), "追帧完毕，进入正常同步模式！");
            }

            if (IsCatchingUp)
            {
                // ==== 追帧模式 ====
                // 抛弃 TickInterval！最大马力疯狂运算！
                // 限流：每次 Update 最多只跑 500 帧，防止 Unity 主线程卡死！
                int runCount = 0;
                while (_frameDict.ContainsKey(ExpectedFrameId) && runCount < MaxCatchUpFrames)
                {
                    var frame = _frameDict[ExpectedFrameId];
                    _frameDict.Remove(ExpectedFrameId);
                    
                    OnFrameExecuted?.Invoke(frame);
                    
                    ExpectedFrameId++;
                    runCount++;
                }
            }
            else
            {
                // ==== 正常模式 ====
                // 严格按照 TickInterval 走
                _accumulateTime += deltaTime;
                while (_accumulateTime >= TickInterval && _frameDict.ContainsKey(ExpectedFrameId))
                {
                    var frame = _frameDict[ExpectedFrameId];
                    _frameDict.Remove(ExpectedFrameId);
                    
                    OnFrameExecuted?.Invoke(frame);
                    
                    ExpectedFrameId++;
                    _accumulateTime -= TickInterval;
                }
            }
        }
    }
}