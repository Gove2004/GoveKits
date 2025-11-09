using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;


/*
使用 static class 获取效果
内置ComposeEffect 静态类，提供语义化命名的效果创建方法
也可以自定义静态类，封装特定领域的效果创建逻辑
例如，创建一个 FireEffect 类，专门用于火焰相关的效果
public static class FireEffect
{
    public static IEffect Burn(float damagePerSecond, float duration) { ... }
    public static IEffect Ignite(float chance) { ... }
}
并使用 EffectManager 来管理和执行这些效果
*/



namespace GoveKits.Unit
{
    /// <summary>
    /// 效果执行上下文
    /// </summary>
    public class EffectContext
    {
        public object Source { get; }
        public object Target { get; }
        public CancellationToken CancellationToken { get; }
        public Dictionary<string, object> Data { get; }

        public EffectContext(object source, object target, CancellationToken cancellationToken = default)
        {
            Source = source;
            Target = target;
            CancellationToken = cancellationToken;
            Data = new Dictionary<string, object>();
        }

        public T Get<T>(string key, T defaultValue = default) =>
            Data.TryGetValue(key, out var value) && value is T typedValue ? typedValue : defaultValue;

        public void Set<T>(string key, T value) => Data[key] = value;
    }


    /// <summary>
    /// 效果接口
    /// </summary>
    public interface IEffect
    {
        UniTask Execute(EffectContext context);
    }


    /// <summary>
    /// 效果组合器 - 语义化命名重构
    /// </summary>
    public static class ComposeEffect
    {
        // ==================== 基础原子效果 ====================

        /// <summary>立即执行效果 - 在当前帧立即执行指定操作</summary>
        public static IEffect Immediate(Action<EffectContext> action) => new ImmediateEffect(action);

        /// <summary>异步立即执行效果 - 立即开始异步操作</summary>
        public static IEffect ImmediateAsync(Func<EffectContext, UniTask> asyncAction) => new ImmediateAsyncEffect(asyncAction);

        /// <summary>时间延迟效果 - 等待指定的秒数</summary>
        public static IEffect Delay(float seconds) => new DelayEffect(seconds);

        /// <summary>帧延迟效果 - 等待指定的帧数</summary>
        public static IEffect DelayFrame(int frames = 1) => new DelayFrameEffect(frames);

        // ==================== 流程控制效果 ====================

        /// <summary>条件分支效果 - 根据条件选择执行路径</summary>
        public static IEffect If(Func<EffectContext, bool> condition, IEffect thenEffect, IEffect elseEffect = null) =>
            new IfEffect(condition, thenEffect, elseEffect);

        /// <summary>循环执行效果 - 当条件满足时重复执行</summary>
        public static IEffect While(Func<EffectContext, bool> condition, IEffect body) => new WhileEffect(condition, body);

        /// <summary>重复执行效果 - 固定次数重复执行</summary>
        public static IEffect Repeat(int count, IEffect effect) => new RepeatEffect(count, effect);

        /// <summary>等待条件满足效果 - 持续等待直到条件成立</summary>
        public static IEffect WaitUntil(Func<EffectContext, bool> condition) => new WaitUntilEffect(condition);

        /// <summary>等待条件结束效果 - 持续等待直到条件不再成立</summary>
        public static IEffect WaitWhile(Func<EffectContext, bool> condition) => new WaitWhileEffect(condition);

        /// <summary>事件触发效果 - 监听特定事件并在触发时执行</summary>
        /// <typeparam name="T">事件数据类型</typeparam>
        /// <param name="subscribe">订阅事件的函数，接受一个事件处理器并返回一个取消订阅的 IDisposable</param>
        /// <param name="onEvent">事件触发时执行的效果工厂函数，接受事件数据并返回对应的效果</param>
        public static IEffect OnEvent<T>(Func<Action<T>, IDisposable> subscribe, Func<T, IEffect> onEvent)
            => new EventEffect<T>(subscribe, onEvent);

        // ==================== 组合效果 ====================

        /// <summary>顺序组合效果 - 按顺序依次执行</summary>
        public static IEffect Sequence(params IEffect[] effects) => new SequenceEffect(effects);

        /// <summary>并行组合效果 - 同时执行所有效果</summary>
        public static IEffect Parallel(params IEffect[] effects) => new ParallelEffect(effects);

        /// <summary>竞速组合效果 - 任一效果完成即返回</summary>
        public static IEffect Race(params IEffect[] effects) => new RaceEffect(effects);

        /// <summary>全部完成效果 - 等待所有效果完成（与Parallel类似但语义不同）</summary>
        public static IEffect WhenAll(params IEffect[] effects) => new WhenAllEffect(effects);

        // ==================== 便捷方法和特殊效果 ====================

        /// <summary>空效果 - 不执行任何操作</summary>
        public static IEffect Empty => Immediate(_ => { });

        /// <summary>回调效果 - 简化无参数回调创建</summary>
        public static IEffect Callback(Action action) => Immediate(_ => action());

        /// <summary>异步回调效果 - 简化异步回调创建</summary>
        public static IEffect CallbackAsync(Func<UniTask> asyncAction) => ImmediateAsync(async _ => await asyncAction());

        /// <summary>条件延迟效果 - 只有条件满足时才执行延迟</summary>
        public static IEffect DelayIf(float seconds, Func<EffectContext, bool> condition) =>
            If(condition, Delay(seconds), Empty);

        /// <summary>补间效果，为了简便起见，直接UniTask化</summary>
        /// <summary>渐变效果 - 数值随时间平滑变化</summary>
        public static IEffect Tween(float duration, Action<float> onUpdate, Func<float, float> easing = null)
            => new TweenEffect(duration, onUpdate, easing);
        
        // 更多便捷方法可以根据需要添加
    }

    #region 效果实现 - 语义化命名

    /// <summary>
    /// 立即执行效果实现
    /// 设计意图：在当前帧立即执行指定操作，不产生任何时间延迟
    /// 使用场景：瞬发技能、状态设置、即时伤害等
    /// </summary>
    internal class ImmediateEffect : IEffect
    {
        private readonly Action<EffectContext> _action;

        public ImmediateEffect(Action<EffectContext> action) => _action = action;

        public async UniTask Execute(EffectContext context)
        {
            await UniTask.SwitchToMainThread();
            _action(context);
        }
    }

    /// <summary>
    /// 异步立即执行效果实现
    /// 设计意图：立即开始异步操作，但操作本身可能需要时间完成
    /// 使用场景：异步资源加载、网络请求、异步动画等
    /// </summary>
    internal class ImmediateAsyncEffect : IEffect
    {
        private readonly Func<EffectContext, UniTask> _asyncAction;

        public ImmediateAsyncEffect(Func<EffectContext, UniTask> asyncAction) => _asyncAction = asyncAction;

        public UniTask Execute(EffectContext context) => _asyncAction(context);
    }

    /// <summary>
    /// 时间延迟效果实现
    /// 设计意图：基于真实时间系统的延迟等待
    /// 使用场景：技能冷却、定时触发、持续时间效果等
    /// </summary>
    internal class DelayEffect : IEffect
    {
        private readonly float _seconds;

        public DelayEffect(float seconds) => _seconds = seconds;

        public UniTask Execute(EffectContext context) =>
            UniTask.Delay(TimeSpan.FromSeconds(_seconds), cancellationToken: context.CancellationToken);
    }

    /// <summary>
    /// 帧延迟效果实现
    /// 设计意图：基于渲染帧的延迟等待，与帧率相关
    /// 使用场景：动画同步、渲染相关逻辑、帧精确控制等
    /// </summary>
    internal class DelayFrameEffect : IEffect
    {
        private readonly int _frames;

        public DelayFrameEffect(int frames) => _frames = frames;

        public UniTask Execute(EffectContext context) =>
            UniTask.DelayFrame(_frames, cancellationToken: context.CancellationToken);
    }

    /// <summary>
    /// 条件分支效果实现
    /// 设计意图：提供动态行为选择，根据运行时条件决定执行路径
    /// 使用场景：AI决策、技能条件释放、状态依赖行为等
    /// </summary>
    internal class IfEffect : IEffect
    {
        private readonly Func<EffectContext, bool> _condition;
        private readonly IEffect _thenEffect;
        private readonly IEffect _elseEffect;

        public IfEffect(Func<EffectContext, bool> condition, IEffect thenEffect, IEffect elseEffect)
        {
            _condition = condition;
            _thenEffect = thenEffect;
            _elseEffect = elseEffect ?? ComposeEffect.Empty;
        }

        public async UniTask Execute(EffectContext context)
        {
            var effect = _condition(context) ? _thenEffect : _elseEffect;
            await effect.Execute(context);
        }
    }

    /// <summary>
    /// 循环执行效果实现
    /// 设计意图：提供while循环语义，支持基于条件的重复执行
    /// 使用场景：持续施法、状态持续检查、循环攻击等
    /// </summary>
    internal class WhileEffect : IEffect
    {
        private readonly Func<EffectContext, bool> _condition;
        private readonly IEffect _body;

        public WhileEffect(Func<EffectContext, bool> condition, IEffect body)
        {
            _condition = condition;
            _body = body;
        }

        public async UniTask Execute(EffectContext context)
        {
            while (_condition(context) && !context.CancellationToken.IsCancellationRequested)
            {
                await _body.Execute(context);
            }
        }
    }

    /// <summary>
    /// 重复执行效果实现
    /// 设计意图：提供for循环语义，固定次数的重复执行
    /// 使用场景：连击技能、多次攻击、重复动作等
    /// </summary>
    internal class RepeatEffect : IEffect
    {
        private readonly int _count;
        private readonly IEffect _effect;

        public RepeatEffect(int count, IEffect effect)
        {
            _count = count;
            _effect = effect;
        }

        public async UniTask Execute(EffectContext context)
        {
            for (int i = 0; i < _count && !context.CancellationToken.IsCancellationRequested; i++)
            {
                await _effect.Execute(context);
            }
        }
    }

    /// <summary>
    /// 顺序组合效果实现
    /// 设计意图：严格的顺序执行，前一个效果完成后才开始下一个
    /// 使用场景：技能连招、剧情序列、步骤化流程等
    /// </summary>
    internal class SequenceEffect : IEffect
    {
        private readonly IEffect[] _effects;

        public SequenceEffect(IEffect[] effects) => _effects = effects;

        public async UniTask Execute(EffectContext context)
        {
            foreach (var effect in _effects)
            {
                if (context.CancellationToken.IsCancellationRequested) break;
                await effect.Execute(context);
            }
        }
    }

    /// <summary>
    /// 并行组合效果实现
    /// 设计意图：真正的并行执行，所有效果同时开始
    /// 使用场景：同时播放多个动画、并行状态更新、复合效果等
    /// </summary>
    internal class ParallelEffect : IEffect
    {
        private readonly IEffect[] _effects;

        public ParallelEffect(IEffect[] effects) => _effects = effects;

        public async UniTask Execute(EffectContext context)
        {
            var tasks = new List<UniTask>();
            foreach (var effect in _effects)
            {
                tasks.Add(effect.Execute(context));
            }
            await UniTask.WhenAll(tasks);
        }
    }

    /// <summary>
    /// 竞速组合效果实现
    /// 设计意图：竞争性执行，第一个完成的效果胜出
    /// 使用场景：超时控制、快速响应、优先级执行等
    /// </summary>
    internal class RaceEffect : IEffect
    {
        private readonly IEffect[] _effects;

        public RaceEffect(IEffect[] effects) => _effects = effects;

        public async UniTask Execute(EffectContext context)
        {
            var tasks = new List<UniTask>();
            foreach (var effect in _effects)
            {
                tasks.Add(effect.Execute(context));
            }
            await UniTask.WhenAny(tasks);
        }
    }

    /// <summary>
    /// 全部完成效果实现
    /// 设计意图：等待所有效果完成，但不要求同时开始
    /// 使用场景：批量操作完成检查、资源加载完成等待等
    /// </summary>
    internal class WhenAllEffect : IEffect
    {
        private readonly IEffect[] _effects;

        public WhenAllEffect(IEffect[] effects) => _effects = effects;

        public async UniTask Execute(EffectContext context)
        {
            // 顺序执行但等待所有完成 - 与Parallel的语义区别
            foreach (var effect in _effects)
            {
                if (context.CancellationToken.IsCancellationRequested) break;
                await effect.Execute(context);
            }
        }
    }

    /// <summary>
    /// 等待条件满足效果实现
    /// 设计意图：被动等待，直到外部条件满足
    /// 使用场景：等待玩家输入、等待资源就绪、等待状态变化等
    /// </summary>
    internal class WaitUntilEffect : IEffect
    {
        private readonly Func<EffectContext, bool> _condition;

        public WaitUntilEffect(Func<EffectContext, bool> condition) => _condition = condition;

        public async UniTask Execute(EffectContext context)
        {
            await UniTask.WaitUntil(() => _condition(context), cancellationToken: context.CancellationToken);
        }
    }

    /// <summary>
    /// 等待条件结束效果实现
    /// 设计意图：被动等待，直到外部条件结束
    /// 使用场景：等待无敌状态结束、等待冷却完成、等待负面效果消失等
    /// </summary>
    internal class WaitWhileEffect : IEffect
    {
        private readonly Func<EffectContext, bool> _condition;

        public WaitWhileEffect(Func<EffectContext, bool> condition) => _condition = condition;

        public async UniTask Execute(EffectContext context)
        {
            await UniTask.WaitWhile(() => _condition(context), cancellationToken: context.CancellationToken);
        }
    }



    /// <summary>
    /// 事件触发效果实现
    /// 设计意图：监听特定事件并在触发时执行对应效果
    /// 使用场景：响应游戏事件、触发连锁反应、动态效果应用等
    /// </summary>
    internal class EventEffect<T> : IEffect
    {
        private readonly Func<Action<T>, IDisposable> _subscribe;
        private readonly Func<T, IEffect> _effectFactory;

        public EventEffect(Func<Action<T>, IDisposable> subscribe, Func<T, IEffect> effectFactory)
        {
            _subscribe = subscribe;
            _effectFactory = effectFactory;
        }

        public async UniTask Execute(EffectContext context)
        {
            var tcs = new UniTaskCompletionSource();
            IDisposable subscription = null;

            void Handler(T eventData)
            {
                if (context.CancellationToken.IsCancellationRequested) return;
                
                // 事件触发时执行效果
                var effect = _effectFactory(eventData);
                effect.Execute(context).Forget();
            }

            subscription = _subscribe(Handler);
            
            // 注册取消回调
            context.CancellationToken.Register(() =>
            {
                subscription?.Dispose();
                tcs.TrySetResult();
            });

            // 等待取消
            await tcs.Task;
        }
    }

    /// <summary>
    /// 补间效果实现
    /// 设计意图：数值随时间平滑变化，支持自定义缓动函数
    /// 使用场景：属性渐变、动画过渡、视觉效果等
    /// </summary>
    internal class TweenEffect : IEffect
    {
        private readonly float _duration;
        private readonly Action<float> _onUpdate;
        private readonly Func<float, float> _easing;

        public TweenEffect(float duration, Action<float> onUpdate, Func<float, float> easing)
        {
            _duration = duration;
            _onUpdate = onUpdate;
            _easing = easing ?? ((t) => t); // 默认线性缓动
        }

        public async UniTask Execute(EffectContext context)
        {
            float elapsed = 0f;
            
            while (elapsed < _duration && !context.CancellationToken.IsCancellationRequested)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / _duration);
                float easedProgress = _easing(progress);
                
                _onUpdate(easedProgress);
                await UniTask.Yield(context.CancellationToken);
            }

            // 确保最终状态
            if (!context.CancellationToken.IsCancellationRequested)
            {
                _onUpdate(1f);
            }
        }
    }

    #endregion
}