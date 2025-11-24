// using System.Collections;



using System;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;

namespace GoveKits.Units
{
    /// <summary>
    /// Mark类，可堆叠的Marks，并且在Apply、Stack、Remove时触发额外逻辑
    /// </summary>
    public class Mark
    {
        public string Name { get; set; } = string.Empty;  // Mark名称
        public int MaxStack { get; set; } = 1; // 最大叠加层数，<=0表示无限
        public int CurrentStack { get; set; } = 1; // 当前叠加层数

        public Mark(string name, int currentStack = 1)
        {
            Name = name;
            CurrentStack = currentStack;
        }

        /// <summary>
        /// 应用Mark时调用
        /// </summary>
        public virtual void Apply()
        {
            // ...

            Duration().Forget();
        }

        /// <summary>
        /// Mark持续期间调用
        /// </summary>
        public virtual async UniTask Duration()
        {
            // Mark持续期间调用
            await UniTask.CompletedTask;
        }

        /// <summary>
        /// Mark堆叠时调用
        /// </summary>
        public virtual void Stack(int count = 1)
        {
            // Mark堆叠时调用
            if (MaxStack <= 0)
            {
                CurrentStack += count;
            }
            else
            {
                CurrentStack = Math.Min(CurrentStack + count, MaxStack);
            }
        }

        /// <summary>
        /// 移除Mark时调用
        /// </summary>
        public virtual void Remove()
        {
        }
    }
}   
        
