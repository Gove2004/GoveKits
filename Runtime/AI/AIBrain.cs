using System.Collections.Generic;

namespace GoveKits.Runtime.AI
{
    /// <summary>
    /// AI 记忆接口
    /// 
    /// 核心功能：
    /// 1. 存储 AI 感知到的世界数据
    /// 2. 提供键值对形式的读写接口
    /// 3. 作为 Observer 和 Tinker 之间的数据桥梁
    /// 
    /// 设计模式：接口隔离，支持多种记忆实现（如 KVMemory、黑板模式等）
    /// </summary>
    public interface IAIMemory
    {
        /// <summary>初始化记忆系统</summary>
        void Init();
        
        /// <summary>清理记忆系统资源</summary>
        void UnInit();
        
        /// <summary>
        /// 写入记忆数据
        /// </summary>
        /// <typeparam name="T">数据类型</typeparam>
        /// <param name="key">记忆键名</param>
        /// <param name="value">记忆值</param>
        void Set<T>(string key, T value);
        
        /// <summary>
        /// 读取记忆数据
        /// </summary>
        /// <typeparam name="T">数据类型</typeparam>
        /// <param name="key">记忆键名</param>
        /// <returns>记忆值，不存在则返回 default</returns>
        T Get<T>(string key);
    } 

    /// <summary>
    /// AI 感知器接口
    /// 
    /// 核心功能：
    /// 1. 观察游戏世界（敌人、道具、环境等）
    /// 2. 将观察结果写入记忆系统
    /// 3. 每帧被 AIActor 调用，更新记忆数据
    /// 
    /// 设计模式：观察者模式，支持多种感知器组合（视觉、听觉、触觉等）
    /// </summary>
    public interface IAIObserver
    {
        /// <summary>初始化感知器</summary>
        void Init();
        
        /// <summary>清理感知器资源</summary>
        void UnInit();
        
        /// <summary>
        /// 执行感知逻辑，将结果写入记忆
        /// </summary>
        /// <param name="memory">记忆系统引用</param>
        void Observe(IAIMemory memory);
    }

    /// <summary>
    /// AI 思考者接口
    /// 
    /// 核心功能：
    /// 1. 读取记忆系统中的数据
    /// 2. 根据记忆做出决策
    /// 3. 输出行为意图（ActionName）给 AIActor 执行
    /// 
    /// 设计模式：策略模式，支持多种思考实现（FSM、行为树、GOAP 等）
    /// </summary>
    public interface IAITinker
    {
        /// <summary>初始化思考者</summary>
        void Init();
        
        /// <summary>清理思考者资源</summary>
        void UnInit();
        
        /// <summary>
        /// 执行思考逻辑，输出行为意图
        /// </summary>
        /// <param name="memory">记忆系统引用</param>
        /// <returns>行为意图名称（如 "Attack"、"Patrol"、"Flee"）</returns>
        string Think(IAIMemory memory);
    }
}