using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

/*
使用 static class 获取效果
内置ComposeEffect 静态类，提供语义化命名的效果创建方法
也可以自定义静态类，封装特定领域的效果创建逻辑

// 容易扩展新效果
public static class CustomEffect
{
    public static IEffect MyCustomEffect(...) 
    => new CustomEffectImplementation(...);
}
并使用 EffectManager 来管理和执行这些效果

缺点：
按理说需要对象池优化才对
*/

namespace GoveKits.Units
{
    /// <summary>
    /// 效果接口
    /// 所有可执行效果的统一契约，支持异步执行
    /// </summary>
    public interface IEffect
    {
        /// <summary>
        /// 执行效果逻辑
        /// </summary>
        /// <param name="context">效果执行上下文</param>
        /// <returns>表示执行进度的异步任务</returns>
        UniTask Apply(UnitContext context);
    }

    /// <summary>
    /// 效果组合器 - 语义化命名重构
    /// 提供声明式API来组合和创建各种效果，提高代码可读性
    /// </summary>
    public static class BaseEffectBuilder
    {
        // ==================== 基础原子效果 ====================

        /// <summary>立即执行效果 - 在当前帧立即执行指定操作</summary>
        /// <param name="action">要执行的操作，接收效果上下文</param>
        /// <returns>立即执行的效果实例</returns>
        public static IEffect Immediate(Action<UnitContext> action) => new ImmediateEffect(action);

        /// <summary>异步立即执行效果 - 立即开始异步操作</summary>
        /// <param name="asyncAction">异步操作函数</param>
        /// <returns>异步立即执行的效果实例</returns>
        public static IEffect ImmediateAsync(Func<UnitContext, UniTask> asyncAction) => new ImmediateAsyncEffect(asyncAction);

        /// <summary>时间延迟效果 - 等待指定的秒数</summary>
        /// <param name="seconds">延迟时间（秒）</param>
        /// <returns>延迟效果实例</returns>
        public static IEffect After(float seconds) => new AfterEffect(seconds);

        /// <summary>帧延迟效果 - 等待指定的帧数</summary>
        /// <param name="frames">要延迟的帧数，默认为1帧</param>
        /// <returns>帧延迟效果实例</returns>
        public static IEffect AfterFrame(int frames = 1) => new AfterFrameEffect(frames);


        // ==================== 流程控制效果 ====================

        /// <summary>条件分支效果 - 根据条件执行不同的效果分支</summary>
        /// <param name="condition">条件判断函数</param>
        /// <param name="thenEffect">条件为true时执行的效果</param>
        /// <param name="elseEffect">条件为false时执行的效果，可选</param>
        /// <returns>条件分支效果实例</returns>
        public static IEffect If(Func<UnitContext, bool> condition, IEffect thenEffect, IEffect elseEffect = null)
            => new IfEffect(condition, thenEffect, elseEffect);

        /// <summary>循环执行效果 - 当条件满足时重复执行效果体</summary>
        /// <param name="condition">循环条件判断函数</param>
        /// <param name="body">循环体内要执行的效果</param>
        /// <returns>循环效果实例</returns>
        public static IEffect While(Func<UnitContext, bool> condition, IEffect body) => new WhileEffect(condition, body);

        /// <summary>重复执行效果 - 固定次数重复执行效果</summary>
        /// <param name="count">重复次数</param>
        /// <param name="effect">要重复执行的效果</param>
        /// <returns>重复执行效果实例</returns>
        public static IEffect Repeat(int count, IEffect effect) => new RepeatEffect(count, effect);

        /// <summary>等待条件满足效果 - 阻塞直到条件成立</summary>
        /// <param name="condition">等待的条件</param>
        /// <returns>等待条件效果实例</returns>
        public static IEffect WaitUntil(Func<UnitContext, bool> condition) => new WaitUntilEffect(condition);

        // ==================== 组合效果 ====================

        /// <summary>顺序组合效果 - 按顺序依次执行多个效果</summary>
        /// <param name="effects">要顺序执行的效果数组</param>
        /// <returns>顺序组合效果实例</returns>
        public static IEffect Sequence(params IEffect[] effects) => new SequenceEffect(effects);

        /// <summary>并行组合效果 - 同时执行多个效果，等待所有完成</summary>
        /// <param name="effects">要并行执行的效果数组</param>
        /// <returns>并行组合效果实例</returns>
        public static IEffect Parallel(params IEffect[] effects) => new ParallelEffect(effects);

        // ==================== 便捷方法 ====================

        /// <summary>空效果 - 不执行任何操作的效果</summary>
        public static IEffect Empty => Immediate(_ => { });

        /// <summary>回调效果 - 将普通Action包装为效果</summary>
        /// <param name="action">要执行的回调</param>
        /// <returns>回调效果实例</returns>
        public static IEffect Callback(Action action) => Immediate(_ => action());

        /// <summary>
        /// 延时回调效果 - 在指定秒数后执行回调
        /// </summary>
        /// <param name="seconds"></param>
        /// <param name="action"></param>
        /// <returns></returns>
        public static IEffect Delay(float seconds, Action<UnitContext> action) => Sequence(
            After(seconds),
            Immediate(action)
        );

        /// <summary>异步回调效果 - 将异步函数包装为效果</summary>
        /// <param name="asyncAction">异步回调函数</param>
        /// <returns>异步回调效果实例</returns>
        public static IEffect CallbackAsync(Func<UniTask> asyncAction) => ImmediateAsync(async _ => await asyncAction());

        /// <summary>
        /// 不可取消的事件监听效果 - 永久监听带参数的事件并在触发时执行子效果
        /// </summary>
        /// <param name="eventName"></param>
        /// <param name="action"></param>
        /// <returns></returns>
        public static IEffect OnEvent<T>(Action<Action<T>> subscribe, IEffect effect)
        {
            return new EventEffect<T>(subscribe, effect);
        }

        
    }

    #region 基础原子效果实现

    /// <summary>
    /// 立即执行效果实现
    /// 在当前帧同步执行指定操作，适用于瞬时效果
    /// </summary>
    internal class ImmediateEffect : IEffect
    {
        private readonly Action<UnitContext> _action;

        public ImmediateEffect(Action<UnitContext> action) => _action = action;

        public UniTask Apply(UnitContext context)
        {
            _action(context);  // 同步执行操作
            return UniTask.CompletedTask;  // 立即返回已完成任务
        }
    }

    /// <summary>
    /// 异步立即执行效果实现
    /// 立即开始异步操作，适用于需要等待的异步任务
    /// </summary>
    internal class ImmediateAsyncEffect : IEffect
    {
        private readonly Func<UnitContext, UniTask> _asyncAction;

        public ImmediateAsyncEffect(Func<UnitContext, UniTask> asyncAction) => _asyncAction = asyncAction;

        public UniTask Apply(UnitContext context) => _asyncAction(context);  // 返回异步任务
    }

    /// <summary>
    /// 时间延迟效果实现
    /// 等待指定的时间间隔，基于秒的单位
    /// </summary>
    internal class AfterEffect : IEffect
    {
        private readonly float _seconds;

        public AfterEffect(float seconds) => _seconds = seconds;
        public UniTask Apply(UnitContext context) => UniTask.Delay(TimeSpan.FromSeconds(_seconds));
    }

    /// <summary>
    /// 帧延迟效果实现
    /// 等待指定的帧数，适用于与渲染帧率相关的延迟
    /// </summary>
    internal class AfterFrameEffect : IEffect
    {
        private readonly int _frames;

        public AfterFrameEffect(int frames) => _frames = frames;
        public UniTask Apply(UnitContext context) => UniTask.DelayFrame(_frames);
    }


    #endregion

    #region 流程控制效果实现

    /// <summary>
    /// 条件分支效果实现
    /// 根据运行时条件决定执行哪个效果分支
    /// </summary>
    internal class IfEffect : IEffect
    {
        private readonly Func<UnitContext, bool> _condition;
        private readonly IEffect _thenEffect;
        private readonly IEffect _elseEffect;

        public IfEffect(Func<UnitContext, bool> condition, IEffect thenEffect, IEffect elseEffect)
        {
            _condition = condition;
            _thenEffect = thenEffect;
            _elseEffect = elseEffect ?? BaseEffectBuilder.Empty;  // 默认使用空效果
        }

        public async UniTask Apply(UnitContext context)
        {
            // 根据条件选择要执行的效果分支
            var effect = _condition(context) ? _thenEffect : _elseEffect;
            await effect.Apply(context);
        }
    }

    /// <summary>
    /// 循环执行效果实现
    /// 当条件满足时重复执行效果体，适用于不确定次数的循环
    /// </summary>
    internal class WhileEffect : IEffect
    {
        private readonly Func<UnitContext, bool> _condition;
        private readonly IEffect _body;

        public WhileEffect(Func<UnitContext, bool> condition, IEffect body)
        {
            _condition = condition;
            _body = body;
        }

        public async UniTask Apply(UnitContext context)
        {
            // 只要条件满足就继续循环
            while (_condition(context))
            {
                await _body.Apply(context);
            }
        }
    }

    /// <summary>
    /// 重复执行效果实现
    /// 固定次数重复执行效果，适用于确定次数的循环
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

        public async UniTask Apply(UnitContext context)
        {
            // 固定次数循环
            for (int i = 0; i < _count; i++)
            {
                await _effect.Apply(context);
            }
        }
    }

    /// <summary>
    /// 等待条件满足效果实现
    /// 阻塞当前效果流程，直到指定条件成立
    /// </summary>
    internal class WaitUntilEffect : IEffect
    {
        private readonly Func<UnitContext, bool> _condition;

        public WaitUntilEffect(Func<UnitContext, bool> condition) => _condition = condition;

        public async UniTask Apply(UnitContext context)
        {
            // 使用UniTask等待条件成立
            await UniTask.WaitUntil(() => _condition(context));
        }
    }

    #endregion

    #region 组合效果实现

    /// <summary>
    /// 顺序组合效果实现
    /// 按顺序依次执行多个效果，前一个效果完成后才开始下一个
    /// </summary>
    internal class SequenceEffect : IEffect
    {
        private readonly IEffect[] _effects;

        public SequenceEffect(IEffect[] effects) => _effects = effects;

        public async UniTask Apply(UnitContext context)
        {
            // 顺序遍历执行所有效果
            foreach (var effect in _effects)
            {
                await effect.Apply(context);  // 等待当前效果完成
            }
        }
    }

    /// <summary>
    /// 并行组合效果实现
    /// 同时启动所有效果，等待所有效果完成后继续
    /// </summary>
    internal class ParallelEffect : IEffect
    {
        private readonly IEffect[] _effects;

        public ParallelEffect(IEffect[] effects) => _effects = effects;

        public async UniTask Apply(UnitContext context)
        {
            var tasks = new List<UniTask>();

            // 启动所有效果任务
            foreach (var effect in _effects)
            {
                tasks.Add(effect.Apply(context));
            }

            // 等待所有效果任务完成
            await UniTask.WhenAll(tasks);
        }
    }
    

    /// <summary>
    /// 带参数的永久事件监听效果实现 - 不可取消, 存在内存泄漏风险
    /// </summary>
    internal class EventEffect<T> : IEffect
    {
        private readonly Action<Action<T>> _subscribe;
        private readonly IEffect _effect;

        public EventEffect(Action<Action<T>> subscribe, IEffect effect)
        {
            _subscribe = subscribe;
            _effect = effect;
        }

        public UniTask Apply(UnitContext context)
        {
            // 直接订阅事件
            _subscribe(async (eventData) =>
            {
                try
                {
                    await _effect.Apply(context);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"PermanentOnEvent<T> effect failed: {ex}");
                }
            });

            // 立即返回完成的任务，但监听器会永久存在
            return UniTask.CompletedTask;
        }
    }

    #endregion
}