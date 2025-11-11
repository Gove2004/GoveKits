using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEditor.PackageManager;

namespace GoveKits.Units
{
    // 能力上下文
    public class AbilityContext
    {
        /// <summary>效果来源单位</summary>
        public IUnit Source { get; }

        /// <summary>效果目标单位</summary>
        public IUnit Target { get; }

        /// <summary>取消令牌，用于异步操作的中断</summary>
        public CancellationToken CancellationToken { get; }

        /// <summary>存储效果执行过程中需要的所有数据</summary>
        private Dictionary<string, object> data;

        /// <summary>
        /// 创建效果上下文
        /// </summary>
        public AbilityContext(IUnit source, IUnit target, CancellationToken cancellationToken = default, Dictionary<string, object> parameters = null)
        {
            Source = source;
            Target = target;
            CancellationToken = cancellationToken;
            data = parameters ?? new Dictionary<string, object>();
        }

        public T Get<T>(string key, T defaultValue = default) =>
            data.TryGetValue(key, out var value) && value is T typedValue ? typedValue : defaultValue;

        public void Set<T>(string key, T value) => data[key] = value;

        /// <summary>
        /// 检查是否已取消
        /// </summary>
        public bool IsCancelled => CancellationToken.IsCancellationRequested;
    }

    /// <summary>
    /// 异步能力接口
    /// </summary>
    public interface IAbility
    {
        /// <summary>
        /// 执行能力（异步）
        /// </summary>
        UniTask Execute(AbilityContext context);

        /// <summary>
        /// 检查能力施放条件（异步）
        /// </summary>
        UniTask<bool> Condition(AbilityContext context);

        /// <summary>
        /// 消耗能力资源（异步）
        /// </summary>
        UniTask Cost(AbilityContext context);

        /// <summary>
        /// 取消能力效果（异步）
        /// </summary>
        UniTask Cancel(AbilityContext context);

        /// <summary>
        /// 完成能力效果（异步）
        /// </summary>
        UniTask Complete(AbilityContext context);

        /// <summary>
        /// 处理能力错误（异步）
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public UniTask Error(AbilityContext context);

        /// <summary>
        /// 尝试执行能力，包含完整的生命周期（异步）
        /// </summary>
        UniTask<bool> Try(AbilityContext context);
    }

    // 能力基类，提供默认异步实现
    public abstract class BaseAbility : IAbility
    {
        public virtual async UniTask<bool> Condition(AbilityContext context)
        {
            // 基础条件检查
            if (context.IsCancelled)
                return false;

            await UniTask.Yield(); // 保持异步性
            return true;
        }

        public virtual async UniTask Cost(AbilityContext context)
        {
            // 基础实现：简单的资源消耗
            await UniTask.Yield();
        }

        public abstract UniTask Execute(AbilityContext context);

        public virtual async UniTask Cancel(AbilityContext context)
        {
            // 基础的取消逻辑
            await UniTask.Yield();
        }

        public virtual async UniTask Complete(AbilityContext context)
        {
            // 基础的完成逻辑
            await UniTask.Yield();
        }

        public virtual async UniTask Error(AbilityContext context)
        {
            // 基础的错误处理逻辑
            await UniTask.Yield();
        }

        public virtual async UniTask<bool> Try(AbilityContext context)
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

                // 完成能力
                await Complete(context);

                return true;
            }
            catch (OperationCanceledException)
            {
                await Cancel(context);
                return false;
            }
            catch (Exception ex)
            {
                await Error(context);
                throw new Exception($"[Ability] 执行失败 {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 工具方法：等待指定时间（可被取消）
        /// </summary>
        protected async UniTask WaitForSeconds(float seconds, AbilityContext context)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(seconds), cancellationToken: context.CancellationToken);
        }
    }
}