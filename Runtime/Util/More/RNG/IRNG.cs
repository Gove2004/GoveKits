using System;
using System.Collections.Generic;
using System.Linq;

namespace GoveKits.Runtime.Util
{
    /// <summary>
    /// 核心随机数生成器接口。
    /// 定义统一的随机操作契约，NormalRNG 为默认实现。
    /// </summary>
    public interface IRNG
    {
        /// <summary>当前 RNG 的种子值。</summary>
        int Seed { get; }
        /// <summary>重新设置种子，重置随机序列。</summary>
        /// <param name="seed">新的种子值</param>
        void Reseed(int seed);
        /// <summary>生成 0 到 int.MaxValue 之间的随机整数。</summary>
        int NextInt();
        /// <summary>生成 0 到 maxExclusive-1 之间的随机整数。</summary>
        int NextInt(int maxExclusive);
        /// <summary>生成 minInclusive 到 maxExclusive-1 之间的随机整数。</summary>
        int Range(int minInclusive, int maxExclusive);
        /// <summary>生成 0.0 到 1.0 之间的随机浮点数。</summary>
        float NextFloat();
        /// <summary>生成 minInclusive 到 maxInclusive 之间的随机浮点数。</summary>
        float Range(float minInclusive, float maxInclusive);
        /// <summary>概率判定，传入 0.0~1.0 的概率值，返回是否命中。</summary>
        bool Chance(float probability);
        /// <summary>生成随机布尔值，约 50% 概率返回 true。</summary>
        bool NextBool();
        /// <summary>生成随机符号，返回 1 或 -1。</summary>
        int NextSign();
        /// <summary>生成正态分布（高斯分布）随机数。</summary>
        float NextGaussian(float mean = 0f, float stdDev = 1f);
        /// <summary>从列表中随机等概率选择一个元素。</summary>
        T Pick<T>(IReadOnlyList<T> list);
        /// <summary>从列表中随机不重复地抽取指定数量的元素。</summary>
        List<T> PickMultiple<T>(IEnumerable<T> source, int count);
        /// <summary>按权重随机抽取一个元素（轮盘赌算法）。</summary>
        T PickWeighted<T>(IEnumerable<T> items, Func<T, float> weightSelector);
        /// <summary>使用 Fisher-Yates 算法原地打乱列表顺序。</summary>
        void Shuffle<T>(IList<T> list);
    }
}