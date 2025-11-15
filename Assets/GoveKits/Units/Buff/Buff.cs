// using System.Collections;



using System;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;

namespace GoveKits.Units
{
    /// <summary>
    /// Buff类，可堆叠的buffs，并且在Apply、Stack、Remove时触发额外逻辑
    /// </summary>
    public class Buff
    {
        public string Name { get; set; } = string.Empty;  // Buff名称
        public int MaxStack { get; set; } = 1; // 最大叠加层数，<=0表示无限
        public int CurrentStack { get; set; } = 1; // 当前叠加层数

        public Buff(string name, int currentStack = 1)
        {
            Name = name;
            CurrentStack = currentStack;
        }

        /// <summary>
        /// 应用Buff时调用
        /// </summary>
        public virtual void Apply()
        {
            // ...

            Duration().Forget();
        }

        /// <summary>
        /// Buff持续期间调用
        /// </summary>
        public virtual async UniTask Duration()
        {
            // Buff持续期间调用
            await UniTask.CompletedTask;
        }

        /// <summary>
        /// Buff堆叠时调用
        /// </summary>
        public virtual void Stack(int count = 1)
        {
            // Buff堆叠时调用
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
        /// 移除Buff时调用
        /// </summary>
        public virtual void Remove()
        {
        }
    }
}   
        
