using System;
using System.Collections.Generic;
using UnityEngine;

namespace GoveKits.FSM
{
    /// <summary>
    /// 状态机事件参数
    /// </summary>
    public class StateChangeEventArgs<TStateId> : EventArgs
    {
        public TStateId FromState { get; }
        public TStateId ToState { get; }
        public object TransitionData { get; }

        public StateChangeEventArgs(TStateId fromState, TStateId toState, object transitionData = null)
        {
            FromState = fromState;
            ToState = toState;
            TransitionData = transitionData;
        }
    }

    /// <summary>
    /// 状态接口
    /// </summary>
    public interface IState<TStateId>
    {
        TStateId StateId { get; }
        void OnEnter(object transitionData = null);
        void OnExit();
        void OnUpdate(float deltaTime);
        void OnFixedUpdate(float fixedDeltaTime);
        void OnLateUpdate();
    }

    /// <summary>
    /// 状态基类
    /// </summary>
    public abstract class StateBase<TStateId> : IState<TStateId>
    {
        public TStateId StateId { get; }
        protected StateMachine<TStateId> StateMachine { get; }
        protected float StateTime { get; private set; }

        protected StateBase(TStateId stateId, StateMachine<TStateId> stateMachine)
        {
            StateId = stateId;
            StateMachine = stateMachine;
        }

        public virtual void OnEnter(object transitionData = null)
        {
            StateTime = 0f;
        }

        public virtual void OnExit()
        {
            StateTime = 0f;
        }

        public virtual void OnUpdate(float deltaTime)
        {
            StateTime += deltaTime;
        }

        public virtual void OnFixedUpdate(float fixedDeltaTime) { }
        public virtual void OnLateUpdate() { }
    }

    /// <summary>
    /// 状态过渡条件
    /// </summary>
    public struct StateTransition<TStateId>
    {
        public TStateId FromState { get; }
        public TStateId ToState { get; }
        public Func<bool> Condition { get; }
        public Func<object> TransitionDataProvider { get; }

        public StateTransition(TStateId fromState, TStateId toState, Func<bool> condition, Func<object> transitionDataProvider = null)
        {
            FromState = fromState;
            ToState = toState;
            Condition = condition;
            TransitionDataProvider = transitionDataProvider;
        }
    }

    /// <summary>
    /// 增强型状态机
    /// </summary>
    public class StateMachine<TStateId> : IDisposable
    {
        #region 事件
        public event Action<StateChangeEventArgs<TStateId>> OnStateChangeStarted;    // 状态切换开始
        public event Action<StateChangeEventArgs<TStateId>> OnStateChangeCompleted;  // 状态切换完成
        public event Action<TStateId> OnStateUpdate;                                // 状态更新
        public event Action<TStateId, Exception> OnStateError;                      // 状态错误
        #endregion

        #region 内部状态
        private readonly Dictionary<TStateId, IState<TStateId>> _states = new Dictionary<TStateId, IState<TStateId>>();
        private readonly List<StateTransition<TStateId>> _transitions = new List<StateTransition<TStateId>>();

        private IState<TStateId> _currentState;
        private IState<TStateId> _previousState;
        private bool _isTransitioning = false;
        private bool _isEnabled = true;
        private float _stateTimer = 0f;
        #endregion

        #region 公共属性
        public TStateId CurrentStateId => _currentState != null ? _currentState.StateId : default;
        public TStateId PreviousStateId => _previousState != null ? _previousState.StateId : default;
        public IState<TStateId> CurrentState => _currentState;
        public IState<TStateId> PreviousState => _previousState;

        public float StateTime => _stateTimer;
        public bool IsTransitioning => _isTransitioning;
        public bool IsEnabled => _isEnabled;
        public int StateCount => _states.Count;
        public int TransitionCount => _transitions.Count;
        #endregion

        #region 状态管理
        /// <summary>
        /// 添加状态
        /// </summary>
        public void AddState(IState<TStateId> state)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));

            if (_states.ContainsKey(state.StateId))
                throw new InvalidOperationException($"状态已存在: {state.StateId}");

            _states[state.StateId] = state;
        }

        /// <summary>
                /// 添加状态（泛型便捷方法）
                /// </summary>
                public void AddState<TState>(TStateId stateId) where TState : IState<TStateId>
                {
                    var state = (TState)Activator.CreateInstance(typeof(TState), stateId, this);
                    if (state == null)
                        throw new InvalidOperationException($"无法创建状态实例: {typeof(TState).FullName}");
                    AddState(state);
                }

        /// <summary>
        /// 移除状态
        /// </summary>
        public bool RemoveState(TStateId stateId)
        {
            // 不能移除当前状态
            if (EqualityComparer<TStateId>.Default.Equals(CurrentStateId, stateId))
                return false;

            return _states.Remove(stateId);
        }

        /// <summary>
        /// 检查状态是否存在
        /// </summary>
        public bool HasState(TStateId stateId) => _states.ContainsKey(stateId);

        /// <summary>
        /// 获取状态
        /// </summary>
        public IState<TStateId> GetState(TStateId stateId)
        {
            _states.TryGetValue(stateId, out var state);
            return state;
        }
        #endregion

        #region 过渡管理
        /// <summary>
        /// 添加状态过渡条件
        /// </summary>
        public void AddTransition(TStateId fromState, TStateId toState, Func<bool> condition, Func<object> transitionDataProvider = null)
        {
            var transition = new StateTransition<TStateId>(fromState, toState, condition, transitionDataProvider);
            _transitions.Add(transition);
        }

        /// <summary>
        /// 添加任意状态到目标状态的过渡
        /// </summary>
        public void AddTransitionFromAny(TStateId toState, Func<bool> condition, Func<object> transitionDataProvider = null)
        {
            // 使用 default 表示任意状态
            AddTransition(default, toState, condition, transitionDataProvider);
        }

        /// <summary>
        /// 移除过渡条件
        /// </summary>
        public bool RemoveTransition(TStateId fromState, TStateId toState)
        {
            return _transitions.RemoveAll(t =>
                EqualityComparer<TStateId>.Default.Equals(t.FromState, fromState) &&
                EqualityComparer<TStateId>.Default.Equals(t.ToState, toState)) > 0;
        }
        #endregion

        #region 状态控制
        /// <summary>
        /// 启动状态机
        /// </summary>
        public void Start(TStateId initialStateId, object transitionData = null)
        {
            if (!_states.ContainsKey(initialStateId))
                throw new InvalidOperationException($"初始状态不存在: {initialStateId}");

            _isEnabled = true;
            ChangeStateInternal(initialStateId, transitionData, isInitialState: true);
        }

        /// <summary>
        /// 切换状态
        /// </summary>
        public bool ChangeState(TStateId newStateId, object transitionData = null)
        {
            if (!_isEnabled || _isTransitioning)
                return false;

            if (!_states.ContainsKey(newStateId))
            {
                OnStateError?.Invoke(CurrentStateId, new InvalidOperationException($"目标状态不存在: {newStateId}"));
                return false;
            }

            if (EqualityComparer<TStateId>.Default.Equals(CurrentStateId, newStateId))
                return false; // 已经是目标状态

            return ChangeStateInternal(newStateId, transitionData);
        }

        /// <summary>
        /// 强制切换状态（忽略过渡条件）
        /// </summary>
        public bool ForceChangeState(TStateId newStateId, object transitionData = null)
        {
            if (!_isEnabled || _isTransitioning)
                return false;

            if (!_states.ContainsKey(newStateId))
            {
                OnStateError?.Invoke(CurrentStateId, new InvalidOperationException($"目标状态不存在: {newStateId}"));
                return false;
            }

            return ChangeStateInternal(newStateId, transitionData);
        }

        /// <summary>
        /// 返回到上一个状态
        /// </summary>
        public bool RevertToPreviousState(object transitionData = null)
        {
            if (_previousState == null)
                return false;

            return ChangeState(_previousState.StateId, transitionData);
        }

        /// <summary>
        /// 内部状态切换实现
        /// </summary>
        private bool ChangeStateInternal(TStateId newStateId, object transitionData, bool isInitialState = false)
        {
            TStateId fromState = _currentState != null ? _currentState.StateId : default(TStateId);
            var toState = newStateId;

            _isTransitioning = true;

            try
            {
                // 触发状态切换开始事件
                if (!isInitialState)
                {
                    OnStateChangeStarted?.Invoke(new StateChangeEventArgs<TStateId>(fromState, toState, transitionData));
                }

                // 退出当前状态
                if (_currentState != null)
                {
                    SafeExecute(() => _currentState.OnExit());
                }

                // 更新状态引用
                _previousState = _currentState;
                _currentState = _states[newStateId];
                _stateTimer = 0f;

                // 进入新状态
                SafeExecute(() => _currentState.OnEnter(transitionData));

                // 触发状态切换完成事件
                if (!isInitialState)
                {
                    OnStateChangeCompleted?.Invoke(new StateChangeEventArgs<TStateId>(fromState, toState, transitionData));
                }

                return true;
            }
            catch (Exception ex)
            {
                OnStateError?.Invoke(toState, ex);
                return false;
            }
            finally
            {
                _isTransitioning = false;
            }
        }

        /// <summary>
        /// 安全执行状态方法
        /// </summary>
        private void SafeExecute(Action action)
        {
            try
            {
                action?.Invoke();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[StateMachine] 状态执行错误: {ex}");
                OnStateError?.Invoke(CurrentStateId, ex);
            }
        }
        #endregion

        #region 状态机更新
        /// <summary>
        /// 更新状态机（在Update中调用）
        /// </summary>
        public void Update(float deltaTime)
        {
            if (!_isEnabled || _currentState == null || _isTransitioning)
                return;

            // 检查自动过渡
            CheckAutomaticTransitions();

            // 更新当前状态
            SafeExecute(() =>
            {
                _currentState.OnUpdate(deltaTime);
                _stateTimer += deltaTime;
            });

            OnStateUpdate?.Invoke(CurrentStateId);
        }

        /// <summary>
        /// 固定更新（在FixedUpdate中调用）
        /// </summary>
        public void FixedUpdate(float fixedDeltaTime)
        {
            if (!_isEnabled || _currentState == null || _isTransitioning)
                return;

            SafeExecute(() => _currentState.OnFixedUpdate(fixedDeltaTime));
        }

        /// <summary>
        /// 后期更新（在LateUpdate中调用）
        /// </summary>
        public void LateUpdate()
        {
            if (!_isEnabled || _currentState == null || _isTransitioning)
                return;

            SafeExecute(() => _currentState.OnLateUpdate());
        }

        /// <summary>
        /// 检查自动状态过渡
        /// </summary>
        private void CheckAutomaticTransitions()
        {
            foreach (var transition in _transitions)
            {
                // 检查过渡条件是否匹配
                bool fromStateMatches = EqualityComparer<TStateId>.Default.Equals(transition.FromState, default) ||
                                      EqualityComparer<TStateId>.Default.Equals(transition.FromState, CurrentStateId);

                if (fromStateMatches && transition.Condition != null && transition.Condition())
                {
                    var transitionData = transition.TransitionDataProvider?.Invoke();
                    if (ChangeState(transition.ToState, transitionData))
                    {
                        break; // 一次只执行一个过渡
                    }
                }
            }
        }
        #endregion

        #region 控制方法
        /// <summary>
        /// 启用状态机
        /// </summary>
        public void Enable()
        {
            _isEnabled = true;
        }

        /// <summary>
        /// 禁用状态机
        /// </summary>
        public void Disable()
        {
            _isEnabled = false;
        }

        /// <summary>
        /// 重置状态机
        /// </summary>
        public void Reset()
        {
            _currentState?.OnExit();
            _currentState = null;
            _previousState = null;
            _isTransitioning = false;
            _stateTimer = 0f;
        }

        /// <summary>
        /// 清理资源
        /// </summary>
        public void Dispose()
        {
            Reset();
            _states.Clear();
            _transitions.Clear();

            // 清空事件订阅
            OnStateChangeStarted = null;
            OnStateChangeCompleted = null;
            OnStateUpdate = null;
            OnStateError = null;
        }
        #endregion

        #region 调试信息
        /// <summary>
        /// 获取状态机调试信息
        /// </summary>
        public string GetDebugInfo()
        {
            return $"StateMachine<{typeof(TStateId).Name}>: " +
                   $"Current: {CurrentStateId}, " +
                   $"Previous: {PreviousStateId}, " +
                   $"Time: {StateTime:F2}s, " +
                   $"Enabled: {IsEnabled}, " +
                   $"Transitioning: {IsTransitioning}";
        }

        /// <summary>
        /// 获取所有状态ID
        /// </summary>
        public IEnumerable<TStateId> GetAllStateIds() => _states.Keys;

        /// <summary>
        /// 获取所有过渡条件
        /// </summary>
        public IEnumerable<StateTransition<TStateId>> GetAllTransitions() => _transitions;
        #endregion
    }

    #region Unity集成扩展
    /// <summary>
    /// 用于Unity MonoBehaviour的状态机组件
    /// </summary>
    public abstract class StateMachineBehaviour<TStateId> : MonoBehaviour
    {
        protected StateMachine<TStateId> StateMachine { get; private set; }

        protected virtual void Awake()
        {
            StateMachine = new StateMachine<TStateId>();
            ConfigureStateMachine();
        }

        protected virtual void Update()
        {
            StateMachine?.Update(Time.deltaTime);
        }

        protected virtual void FixedUpdate()
        {
            StateMachine?.FixedUpdate(Time.fixedDeltaTime);
        }

        protected virtual void LateUpdate()
        {
            StateMachine?.LateUpdate();
        }

        protected virtual void OnDestroy()
        {
            StateMachine?.Dispose();
        }

        /// <summary>
        /// 配置状态机（子类实现）
        /// </summary>
        protected abstract void ConfigureStateMachine();
    }

    /// <summary>
    /// Unity状态基类
    /// </summary>
    public abstract class UnityStateBase<TStateId> : StateBase<TStateId>
    {
        public GameObject Owner { get; }

        protected UnityStateBase(TStateId stateId, StateMachine<TStateId> stateMachine, GameObject owner)
            : base(stateId, stateMachine)
        {
            Owner = owner;
        }
    }
    #endregion
}











// using UnityEngine;
// using GoveKits.FSM;

// // 状态ID枚举
// public enum PlayerState
// {
//     Idle,
//     Move,
//     Jump,
//     Attack,
//     Hurt,
//     Die
// }

// // 玩家状态机
// public class PlayerStateMachine : StateMachineBehaviour<PlayerState>
// {
//     [SerializeField] private float moveSpeed = 5f;
//     [SerializeField] private float jumpForce = 10f;
    
//     private Rigidbody _rigidbody;
//     private Animator _animator;

//     protected override void Awake()
//     {
//         _rigidbody = GetComponent<Rigidbody>();
//         _animator = GetComponent<Animator>();
//         base.Awake();
//     }

//     protected override void ConfigureStateMachine()
//     {
//         // 添加状态
//         StateMachine.AddState(new IdleState(PlayerState.Idle, StateMachine, gameObject));
//         StateMachine.AddState(new MoveState(PlayerState.Move, StateMachine, gameObject));
//         StateMachine.AddState(new JumpState(PlayerState.Jump, StateMachine, gameObject));
//         StateMachine.AddState(new AttackState(PlayerState.Attack, StateMachine, gameObject));

//         // 添加过渡条件
//         StateMachine.AddTransition(PlayerState.Idle, PlayerState.Move, () => Input.GetAxis("Horizontal") != 0);
//         StateMachine.AddTransition(PlayerState.Move, PlayerState.Idle, () => Input.GetAxis("Horizontal") == 0);
//         StateMachine.AddTransitionFromAny(PlayerState.Jump, () => Input.GetKeyDown(KeyCode.Space));
//         StateMachine.AddTransition(PlayerState.Jump, PlayerState.Idle, () => _rigidbody.velocity.y == 0);

//         // 订阅事件
//         StateMachine.OnStateChangeCompleted += OnStateChanged;

//         // 启动状态机
//         StateMachine.Start(PlayerState.Idle);
//     }

//     private void OnStateChanged(StateChangeEventArgs<PlayerState> e)
//     {
//         Debug.Log($"状态切换: {e.FromState} -> {e.ToState}");
//     }
// }

// // 空闲状态实现
// public class IdleState : UnityStateBase<PlayerState>
// {
//     public IdleState(PlayerState stateId, StateMachine<PlayerState> stateMachine, GameObject owner) 
//         : base(stateId, stateMachine, owner) { }

//     public override void OnEnter(object transitionData = null)
//     {
//         base.OnEnter();
//         // 播放空闲动画
//         var animator = Owner.GetComponent<Animator>();
//         if (animator != null)
//             animator.Play("Idle");
//     }

//     public override void OnUpdate(float deltaTime)
//     {
//         base.OnUpdate(deltaTime);
        
//         // 状态逻辑
//         if (StateTime > 5f)
//         {
//             Debug.Log("站得太久了...");
//         }
//     }
// }

// // 移动状态实现
// public class MoveState : UnityStateBase<PlayerState>
// {
//     private Rigidbody _rigidbody;
//     private float _moveSpeed;

//     public MoveState(PlayerState stateId, StateMachine<PlayerState> stateMachine, GameObject owner) 
//         : base(stateId, stateMachine, owner)
//     {
//         _rigidbody = owner.GetComponent<Rigidbody>();
//         _moveSpeed = owner.GetComponent<PlayerStateMachine>().MoveSpeed;
//     }

//     public override void OnEnter(object transitionData = null)
//     {
//         base.OnEnter();
        
//         var animator = Owner.GetComponent<Animator>();
//         if (animator != null)
//             animator.Play("Move");
//     }

//     public override void OnUpdate(float deltaTime)
//     {
//         base.OnUpdate(deltaTime);
        
//         float horizontal = Input.GetAxis("Horizontal");
//         Vector3 movement = new Vector3(horizontal, 0, 0) * _moveSpeed;
//         _rigidbody.velocity = new Vector3(movement.x, _rigidbody.velocity.y, 0);
//     }
// }