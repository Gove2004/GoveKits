using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using GoveKits.Runtime.Core;

namespace GoveKits.Runtime.AI.FSM
{
    /// <summary>
    /// 通用有限状态机。
    /// <para>支持异步进入/退出状态，并提供 Update 与 FixedUpdate 驱动入口。</para>
    /// </summary>
    /// <typeparam name="TStateEnum">状态枚举类型。</typeparam>
    /// <typeparam name="TFSMObject">状态机持有者类型，需实现 <see cref="IFSMObject"/>。</typeparam>
    public class FSM<TStateEnum, TFSMObject> : IDisposable
        where TStateEnum : struct, Enum
        where TFSMObject : class, IFSMObject
    {
        /// <summary>
        /// 状态机所属对象。
        /// </summary>
        public TFSMObject Owner { get; private set; }

        /// <summary>
        /// 状态表，保存状态枚举到状态实例的映射。
        /// </summary>
        private readonly Dictionary<TStateEnum, BaseState<TStateEnum, TFSMObject>> _stateDict = new();
        
        /// <summary>
        /// 当前状态标签。
        /// </summary>
        public TStateEnum Current { get; private set; }

        /// <summary>
        /// 当前状态实例。
        /// </summary>
        public BaseState<TStateEnum, TFSMObject> CurrentState => _stateDict.TryGetValue(Current, out var state) ? state : null;
        
        /// <summary>
        /// 是否处于状态切换中。
        /// <para>切换期间会阻止新的切换和更新调用，避免重入。</para>
        /// </summary>
        private bool _isTransitioning;

        /// <summary>
        /// 创建一个状态机实例。
        /// </summary>
        /// <param name="owner">状态机所属对象。</param>
        public FSM(TFSMObject owner) => Owner = owner;

        /// <summary>
        /// 注册或替换一个状态。
        /// </summary>
        /// <param name="stateEnum">状态标签。</param>
        /// <param name="state">状态实例。</param>
        public void AddState(TStateEnum stateEnum, BaseState<TStateEnum, TFSMObject> state)
        {
            state.Machine = this;
            _stateDict[stateEnum] = state;
        }

        /// <summary>
        /// 切换到目标状态。
        /// <para>执行顺序：当前状态 OnExit -> 设置新状态 -> 新状态 OnEnter。</para>
        /// </summary>
        /// <param name="newState">目标状态标签。</param>
        public async UniTask ChangeState(TStateEnum newState)
        {
            if (_isTransitioning) return;
            if (!_stateDict.TryGetValue(newState, out var nextState)) return;
            if (CurrentState != null && Current.Equals(newState)) return;

            _isTransitioning = true;

            try
            {
                // 1. 退出
                if (CurrentState != null) await CurrentState.OnExit();

                // 2. 切换
                Current = newState;

                // 3. 进入
                await CurrentState.OnEnter();
            }
            catch (Exception ex)
            {
                LogCore.LogInfo(nameof(FSM<TStateEnum, TFSMObject>), $"状态切换异常: {ex.Message}", "FF0000");
            }
            finally
            {
                _isTransitioning = false;
            }
            
        }

        /// <summary>
        /// 启动状态机并进入初始状态。
        /// </summary>
        /// <param name="initialState">初始状态标签。</param>
        /// <exception cref="Exception">未注册初始状态时抛出。</exception>
        public void Start(TStateEnum initialState)
        {
            if (!_stateDict.TryGetValue(initialState, out var state))
            {
                LogCore.LogError(nameof(FSM<TStateEnum, TFSMObject>), $"FSM 启动失败: 未找到初始状态 {initialState}", "FF0000");
            }
            Current = initialState;
            CurrentState.OnEnter().Forget();
        }

        /// <summary>
        /// 每帧更新入口。
        /// </summary>
        public void Update()
        {
            if (_isTransitioning) return;
            CurrentState?.OnUpdate();
        }

        /// <summary>
        /// 固定帧更新入口。
        /// </summary>
        public void FixedUpdate()
        {
            if (_isTransitioning) return;
            CurrentState?.OnFixedUpdate();
        }

        /// <summary>
        /// 释放状态机资源。
        /// <para>会尝试退出当前状态，并释放全部状态实例。</para>
        /// </summary>
        public void Dispose()
        {
            if (_stateDict.TryGetValue(Current, out var currentState) && currentState != null)
            {
                currentState.OnExit().Forget();
            }

            foreach (var state in _stateDict.Values)
            {
                state?.Dispose();
            }

            _stateDict.Clear();
            _isTransitioning = false;
            Owner = null;
        }
    }
}