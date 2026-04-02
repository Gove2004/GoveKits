using System.Collections.Generic;

namespace GoveKits.Runtime.AI
{
    /// <summary>
    /// 基于有限状态机 (FSM) 的思考者实现
    /// 
    /// 核心功能：
    /// 1. 管理多个 FSMState 状态
    /// 2. 处理状态跳转逻辑
    /// 3. 实现 IAITinker 接口，输出行为意图
    /// 
    /// 状态机流程：
    /// 1. 首次 Think 时进入初始状态
    /// 2. 每帧执行当前状态的 OnUpdate
    /// 3. 根据 OnUpdate 返回的 nextState 决定是否跳转
    /// 4. 返回状态的行为意图给 AIActor
    /// </summary>
    public class FSMTinker : IAITinker
    {
        /// <summary>状态注册表 - 按名称索引</summary>
        private readonly Dictionary<string, FSMState> _states = new();
        
        /// <summary>当前激活的状态</summary>
        private FSMState _currentState;
        
        /// <summary>初始状态名称 - 首次 Think 时进入</summary>
        private string _initialStateName;

        /// <summary>
        /// 初始化思考者
        /// 状态的初始进入将延迟到第一次 Think 时执行，因为需要 Memory 的上下文
        /// </summary>
        public void Init() { }

        /// <summary>
        /// 清理思考者资源
        /// 清空状态注册表和当前状态引用
        /// </summary>
        public void UnInit()
        {
            _states.Clear();
            _currentState = null;
        }

        /// <summary>
        /// 注册状态到状态机
        /// </summary>
        /// <param name="state">状态实例</param>
        public void AddState(FSMState state)
        {
            _states[state.StateName] = state;
        }

        /// <summary>
        /// 设置默认的初始状态
        /// 必须在 Think 之前调用
        /// </summary>
        /// <param name="stateName">状态名称</param>
        public void SetInitialState(string stateName)
        {
            _initialStateName = stateName;
        }

        /// <summary>
        /// 核心思考逻辑 - 由 AIActor 每帧调用
        /// 
        /// 执行流程：
        /// 1. 首次运行时进入初始状态
        /// 2. 执行当前状态的 OnUpdate
        /// 3. 处理状态跳转请求
        /// 4. 返回行为意图给 Actor
        /// </summary>
        /// <param name="memory">记忆系统引用</param>
        /// <returns>行为意图名称</returns>
        public string Think(IAIMemory memory)
        {
            // 1. 处理首次运行时的初始状态进入
            if (_currentState == null)
            {
                if (string.IsNullOrEmpty(_initialStateName)) return string.Empty;
                ChangeState(_initialStateName, memory);
                if (_currentState == null) return string.Empty;
            }

            // 2. 执行当前状态逻辑
            string intent = _currentState.OnUpdate(memory, out string nextStateName);

            // 3. 处理状态跳转要求
            if (!string.IsNullOrEmpty(nextStateName) && nextStateName != _currentState.StateName)
            {
                ChangeState(nextStateName, memory);
            }

            // 4. 返回行动意图给 Actor
            return intent;
        }

        /// <summary>
        /// 执行状态跳转
        /// 
        /// 跳转流程：
        /// 1. 查找目标状态
        /// 2. 调用当前状态的 OnExit
        /// 3. 切换到新状态
        /// 4. 调用新状态的 OnEnter
        /// </summary>
        /// <param name="nextStateName">目标状态名称</param>
        /// <param name="memory">记忆系统引用</param>
        private void ChangeState(string nextStateName, IAIMemory memory)
        {
            if (_states.TryGetValue(nextStateName, out var nextState))
            {
                _currentState?.OnExit(memory);
                _currentState = nextState;
                _currentState?.OnEnter(memory);
            }
        }
    }
}