namespace GoveKits.Runtime.AI
{
    /// <summary>
    /// FSM 状态基类
    /// 
    /// 核心功能：
    /// 1. 定义状态的生命周期方法（OnEnter/OnUpdate/OnExit）
    /// 2. 提供状态跳转机制（通过 nextState 参数）
    /// 3. 返回行为意图给 AIActor 执行
    /// 
    /// 状态生命周期：
    /// 进入状态 → OnEnter → 每帧 OnUpdate → 退出状态 → OnExit
    /// </summary>
    public abstract class FSMState
    {
        /// <summary>
        /// 状态的唯一名称标识
        /// 用于状态注册和跳转时的索引
        /// 建议在构造函数中赋值
        /// </summary>
        public string StateName { get; protected set; }

        /// <summary>
        /// 进入状态时调用
        /// 用于初始化状态相关数据
        /// </summary>
        /// <param name="memory">实体的记忆系统</param>
        public virtual void OnEnter(IAIMemory memory) { }
        
        /// <summary>
        /// 退出状态时调用
        /// 用于清理状态相关数据
        /// </summary>
        /// <param name="memory">实体的记忆系统</param>
        public virtual void OnExit(IAIMemory memory) { }

        /// <summary>
        /// 状态逻辑更新 - 每帧调用
        /// 
        /// 核心职责：
        /// 1. 执行状态逻辑
        /// 2. 决定是否跳转状态（通过 nextState 参数）
        /// 3. 返回行为意图给 Actor 执行
        /// </summary>
        /// <param name="memory">实体的记忆系统</param>
        /// <param name="nextState">如果需要跳转状态，对其赋值；否则保持 null</param>
        /// <returns>返回希望 Actor 执行的动作意图 (ActionName)</returns>
        public abstract string OnUpdate(IAIMemory memory, out string nextState);
    }
}