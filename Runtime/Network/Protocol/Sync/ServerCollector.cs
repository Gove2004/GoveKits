using System.Collections.Generic;

namespace GoveKits.Runtime.Network
{
    internal class ServerCollector
    {
        private readonly Dictionary<int, byte[]> _currentInputs = new();

        public void OnReceiveInput(PlayerInputPackage msg)
        {
            _currentInputs[msg.PlayerId] = msg.Payload;
        }

        /// <summary>
        /// 提取当前节拍的所有输入，并清空收集器准备下一帧
        /// </summary>
        public AllInputPackage ExtractTickData(int currentTick)
        {
            var tickData = new AllInputPackage { Tick = currentTick };
            
            // 拷贝一份当前输入发给业务层
            foreach (var kvp in _currentInputs)
            {
                tickData.Inputs[kvp.Key] = kvp.Value;
            }

            _currentInputs.Clear(); // 清理，防止不按键的玩家依然无限移动
            return tickData;
        }
    }
}