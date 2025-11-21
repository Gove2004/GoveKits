using System;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;


namespace GoveKits.Units
{
    /// <summary>
    /// 异步能力接口
    /// </summary>
    public interface IAbility
    {
        /// <summary>
        /// 执行能力
        /// </summary>
        UniTask Execute(UnitContext context);

        /// <summary>
        /// 能力冷却处理
        /// </summary>
        UniTask Cooldown(UnitContext context);

        /// <summary>
        /// 检查能力施放条件
        /// </summary>
        UniTask<bool> Condition(UnitContext context);

        /// <summary>
        /// 消耗能力资源
        /// </summary>
        UniTask<bool> Cost(UnitContext context);

        /// <summary>
        /// 取消能力效果
        /// </summary>
        UniTask Cancel(UnitContext context);

        /// <summary>
        /// 完成能力效果
        /// </summary>
        UniTask Complete(UnitContext context);

        /// <summary>
        /// 处理能力错误
        /// </summary>
        public UniTask Error(UnitContext context);

        /// <summary>
        /// 尝试执行能力，包含完整的生命周期
        /// </summary>
        UniTask<bool> Try(UnitContext context);
    }



    // 能力基类
    public abstract class BaseAbility : IAbility
    {
        public string Name { get; set; } = string.Empty;  // 能力名称
        public int Level { get; set; } = 0;  // 能力等级
        public float CooldownTime { get; set; } = -1f;  // 冷却时间，负数表示无冷却
        private float _cooldownEndTime; // 冷却结束的时间点
        public float CurrentCooldown => Mathf.Max(0f, _cooldownEndTime - Time.time);
        public bool IsCooldownReady => Time.time >= _cooldownEndTime;


        // 构造函数
        public BaseAbility(string name, int level = 1)
        {
            Name = name;
            Level = level;
        }

        

        public virtual async UniTask Cooldown(UnitContext context)
        {
            if (CooldownTime <= 0f) return;
            // 记录结束时间点 (Unity Time)
            _cooldownEndTime = Time.time + CooldownTime;
            // 只是单纯等待，不进行每帧计算
            await UniTask.Delay(TimeSpan.FromSeconds(CooldownTime));
        }


        public virtual async UniTask<bool> Cost(UnitContext context)
        {
            await UniTask.Yield();
            // 声明资源
            // ...
            // 检查资源
            // if (false) return false;
            // 扣除资源
            // ...
            return true;
        }

        public virtual async UniTask<bool> Condition(UnitContext context)
        {
            await UniTask.Yield();
            if (!IsCooldownReady) return false;
            if (await Cost(context) == false) return false;
            // 其他条件检查
            // ...
            return true;
        }

        public abstract UniTask Execute(UnitContext context);

        public virtual async UniTask Complete(UnitContext context)
        {
            await UniTask.Yield();
        }

        public virtual async UniTask Cancel(UnitContext context)
        {
            await UniTask.Yield();
        }

        public virtual async UniTask Error(UnitContext context)
        {
            await UniTask.Yield();
        }


        /// <summary>
        /// 尝试执行能力，包含完整的生命周期
        /// </summary>
        public virtual async UniTask<bool> Try(UnitContext context)
        {
            // 检查条件
            if (!await Condition(context))
                return false;

            try
            {
                // 支付消耗
                await Cost(context);

                // 执行能力
                await Execute(context);

                // 启动冷却
                _ =  Cooldown(context);

                // 完成能力
                await Complete(context);

                return true;
            }
            catch (OperationCanceledException)
            {
                // 取消能力
                await Cancel(context);
                return false;
            }
            catch (Exception ex)
            {
                // 处理错误
                await Error(context);
                throw new Exception($"[Ability] 执行失败 {ex.Message}", ex);
            }
        }
    }
}