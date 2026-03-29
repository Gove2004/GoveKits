using System.Collections.Generic;

namespace GoveKits.Runtime.Core
{
    /// <summary>
    /// 随机流接口
    /// </summary>
    /// <remarks>
    /// 定义随机数生成器的标准规范
    /// 支持种子管理、多种随机数生成方法
    /// 便于实现不同的随机算法或 Mock 测试
    /// </remarks>
    public interface IRandomStream
    {
        /// <summary>
        /// 当前种子值（只读）
        /// </summary>
        /// <remarks>
        /// 用于追踪当前随机流的状态
        /// 相同种子可复现相同的随机序列
        /// 适用于回放、调试、确定性模拟等场景
        /// </remarks>
        int Seed { get; }
        
        /// <summary>
        /// 重新设置种子
        /// </summary>
        /// <param name="seed">新的种子值</param>
        /// <remarks>
        /// 重置后随机序列将从新种子重新开始
        /// 适用于需要重置随机状态的场景
        /// </remarks>
        void Reseed(int seed);
        
        /// <summary>
        /// 生成随机整数（全范围）
        /// </summary>
        /// <returns>0 到 int.MaxValue 之间的随机整数</returns>
        int NextInt();
        
        /// <summary>
        /// 生成随机整数（指定上限）
        /// </summary>
        /// <param name="maxExclusive">上限（不包含）</param>
        /// <returns>0 到 maxExclusive-1 之间的随机整数</returns>
        int NextInt(int maxExclusive);
        
        /// <summary>
        /// 生成随机整数（指定范围）
        /// </summary>
        /// <param name="minInclusive">下限（包含）</param>
        /// <param name="maxExclusive">上限（不包含）</param>
        /// <returns>minInclusive 到 maxExclusive-1 之间的随机整数</returns>
        int Range(int minInclusive, int maxExclusive);
        
        /// <summary>
        /// 生成随机浮点数（0-1 范围）
        /// </summary>
        /// <returns>0.0 到 1.0 之间的随机浮点数</returns>
        float NextFloat();
        
        /// <summary>
        /// 生成随机浮点数（指定范围）
        /// </summary>
        /// <param name="minInclusive">下限（包含）</param>
        /// <param name="maxInclusive">上限（包含）</param>
        /// <returns>minInclusive 到 maxInclusive 之间的随机浮点数</returns>
        float Range(float minInclusive, float maxInclusive);
        
        /// <summary>
        /// 概率判定
        /// </summary>
        /// <param name="probability">成功概率（0.0-1.0）</param>
        /// <returns>成功返回 true，失败返回 false</returns>
        /// <remarks>
        /// 例如：Chance(0.3f) 表示 30% 概率返回 true
        /// 适用于掉落判定、暴击判定等场景
        /// </remarks>
        bool Chance(float probability);
        
        /// <summary>
        /// 从列表中随机选择一个元素
        /// </summary>
        /// <typeparam name="T">元素类型</typeparam>
        /// <param name="list">源列表</param>
        /// <returns>随机选中的元素</returns>
        /// <remarks>
        /// 列表不能为空，否则抛出异常
        /// 适用于随机奖励、随机目标等场景
        /// </remarks>
        T Pick<T>(IReadOnlyList<T> list);
        
        /// <summary>
        /// 打乱列表顺序（Fisher-Yates 洗牌算法）
        /// </summary>
        /// <typeparam name="T">元素类型</typeparam>
        /// <param name="list">要洗牌的列表</param>
        /// <remarks>
        /// 原地修改列表，不创建新列表
        /// 适用于随机顺序、随机排列等场景
        /// </remarks>
        void Shuffle<T>(IList<T> list);
    }
}