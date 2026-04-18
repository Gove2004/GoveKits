using System;
using System.Collections.Generic;
using GoveKits.Runtime.Core;

namespace GoveKits.Runtime.Network
{
    internal class ServerFrameStoreger
    {
        private const int BATCH_SIZE = 200;
        private readonly List<FramePackage> _historyFrames = new List<FramePackage>(10000);
        public int LatestFrameId => _historyFrames.Count; // 当前最新的帧号

        public void AppendFrame(FramePackage frame)
        {
            _historyFrames.Add(frame);
        }

        /// <summary>
        /// 将历史帧切片下发给指定客户端（防止单包过大）
        /// </summary>
        public void SendHistoryToClient(int channelId, int clientLocalFrameId)
        {
            int startIndex = clientLocalFrameId; // 索引从0开始，如果客户端是0，就从索引0(第1帧)开始发
            int totalToSend = _historyFrames.Count - startIndex;

            if (totalToSend <= 0)
            {
                // 已经同步到最新了，发个空包告诉它结束了
                ServerCore.SendTo(channelId, new SyncFrameResponseMsg { IsEnd = true, HistoryFrames = new FramePackage[0] });
                return;
            }

            int sentCount = 0;

            while (sentCount < totalToSend)
            {
                int count = Math.Min(BATCH_SIZE, totalToSend - sentCount);
                var batch = _historyFrames.GetRange(startIndex + sentCount, count);
                
                sentCount += count;
                bool isEnd = (sentCount >= totalToSend);

                var msg = new SyncFrameResponseMsg
                {
                    IsEnd = isEnd,
                    HistoryFrames = batch.ToArray()
                };

                ServerCore.SendTo(channelId, msg);
            }
        }

        public void Clear() => _historyFrames.Clear();
    }
}