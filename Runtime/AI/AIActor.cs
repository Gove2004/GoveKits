using System.Collections.Generic;
using UnityEngine;

namespace GoveKits.Runtime.AI
{
    /// <summary>
    /// AI 行动者实体基类 - AI 系统的最终承载者
    /// 
    /// 核心功能：
    /// 1. 统筹 感知 (Observer) -> 记忆 (Memory) -> 思考 (Tinker) -> 执行 (Act) 的完整闭环
    /// 2. 管理 AI 组件的生命周期（Init/UnInit）
    /// 3. 每帧驱动 AI 心跳（TickAI）
    /// 
    /// 架构设计：
    /// - 组件化：Memory、Tinker、Observers 可自由组合
    /// - 扩展性：子类通过 SetupAI 装配自己的 AI 零件
    /// - 生命周期：与 MonoBehaviour 生命周期绑定
    /// </summary>
    public abstract class AIActor : MonoBehaviour
    {
        /// <summary>初始化状态标记 - 避免重复初始化</summary>
        private bool _isInitialized = false;

        /// <summary>记忆系统 - 存储感知数据</summary>
        protected IAIMemory Memory { get; private set; }
        
        /// <summary>思考系统 - 做出决策</summary>
        protected IAITinker Tinker { get; private set; }
        
        /// <summary>感知器列表 - 观察世界</summary>
        protected List<IAIObserver> Observers { get; private set; } = new();

        /// <summary>
        /// 子类必须实现此方法，组装自己的 Memory、Tinker 和 Observers
        /// 这是 AI 系统的装配入口，决定 AI 的行为能力
        /// </summary>
        /// <param name="memory">记忆系统实例</param>
        /// <param name="tinker">思考系统实例</param>
        /// <param name="observers">感知器列表</param>
        protected abstract void SetupAI(out IAIMemory memory, out IAITinker tinker, out List<IAIObserver> observers);

        /// <summary>
        /// 初始化 AI 系统
        /// 1. 调用 SetupAI 装配零件
        /// 2. 级联初始化所有组件
        /// </summary>
        public virtual void Init()
        {
            if (_isInitialized) return;

            // 1. 让子类装配零件
            SetupAI(out var mem, out var tin, out var obs);
            Memory = mem;
            Tinker = tin;
            if (obs != null) Observers.AddRange(obs);

            // 2. 级联初始化
            Memory?.Init();
            Tinker?.Init();
            foreach (var observer in Observers)
            {
                observer.Init();
            }

            _isInitialized = true;
        }

        /// <summary>
        /// 清理 AI 系统资源
        /// 1. 逆序清理所有组件
        /// 2. 清空引用，避免内存泄漏
        /// </summary>
        public virtual void UnInit()
        {
            if (!_isInitialized) return;

            // 逆序清理：Observers -> Tinker -> Memory
            foreach (var observer in Observers)
            {
                observer.UnInit();
            }
            Observers.Clear();
            
            Tinker?.UnInit();
            Tinker = null;
            Memory?.UnInit();
            Memory = null;

            _isInitialized = false;
        }

        // ===== Unity 生命周期钩子 =====
        
        /// <summary>启动时自动初始化</summary>
        protected virtual void Start() => Init();
        
        /// <summary>销毁时自动清理</summary>
        protected virtual void OnDestroy() => UnInit();
        
        /// <summary>每帧驱动 AI 心跳</summary>
        protected virtual void Update() => TickAI();

        /// <summary>
        /// 驱动 AI 心跳：感知 -> 思考 -> 执行
        /// 这是 AI 系统的核心循环，每帧调用一次
        /// 
        /// 执行流程：
        /// 1. 感知：所有 Observers 观察世界并写入 Memory
        /// 2. 思考：Tinker 读取 Memory 并输出行为意图
        /// 3. 执行：Actor 根据意图执行具体动作
        /// </summary>
        protected void TickAI()
        {
            if (!_isInitialized || Memory == null || Tinker == null) return;

            // 1. 感知：所有感官观察世界并写入记忆
            for (int i = 0; i < Observers.Count; i++)
            {
                Observers[i].Observe(Memory);
            }

            // 2. 思考：大脑根据记忆做出决定
            string intent = Tinker.Think(Memory);

            // 3. 执行：实体自身根据决定采取行动
            if (!string.IsNullOrEmpty(intent))
            {
                Act(intent);
            }
        }

        /// <summary>
        /// 实体执行动作的具体表现
        /// 子类必须实现，将抽象的行为意图转化为具体的游戏行为
        /// （如动画播放、位移、攻击判定等）
        /// </summary>
        /// <param name="intendedAction">行为意图名称（由 Tinker 输出）</param>
        protected abstract void Act(string intendedAction);
    }
}