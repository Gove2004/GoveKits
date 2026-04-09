using System;
using System.Collections.Generic;

namespace GoveKits.Runtime.Core
{
    /// <summary>
    /// 核心随机数生成器接口
    /// </summary>
    public interface IRNG
    {
        /// <summary>
        /// 当前种子值（只读）
        /// </summary>
        int Seed { get; }
        
        /// <summary>
        /// 重新设置种子，重置随机序列
        /// </summary>
        void Reseed(int seed);
        
        /// <summary>
        /// 生成 0 到 int.MaxValue 之间的随机整数
        /// </summary>
        int NextInt();
        
        /// <summary>
        /// 生成 0 到 maxExclusive-1 之间的随机整数
        /// </summary>
        int NextInt(int maxExclusive);
        
        /// <summary>
        /// 生成 minInclusive 到 maxExclusive-1 之间的随机整数
        /// </summary>
        int Range(int minInclusive, int maxExclusive);
        
        /// <summary>
        /// 生成 0.0 到 1.0 之间的随机浮点数
        /// </summary>
        float NextFloat();
        
        /// <summary>
        /// 生成 minInclusive 到 maxInclusive 之间的随机浮点数
        /// </summary>
        float Range(float minInclusive, float maxInclusive);
        
        /// <summary>
        /// 概率判定（传入 0.0~1.0 的概率，返回是否命中）
        /// </summary>
        bool Chance(float probability);

        /// <summary>
        /// 生成随机布尔值（50% 为 true，50% 为 false）
        /// </summary>
        bool NextBool();

        /// <summary>
        /// 生成随机符号（返回 1 或 -1）
        /// </summary>
        int NextSign();

        /// <summary>
        /// 正态分布（高斯分布）随机数
        /// 常用于自然界的随机表现（如：子弹散布、角色身高、属性浮动）
        /// </summary>
        /// <param name="mean">均值（中心点）</param>
        /// <param name="stdDev">标准差（离散程度，值越大越分散）</param>
        float NextGaussian(float mean = 0f, float stdDev = 1f);
        
        /// <summary>
        /// 从列表中随机等概率选择一个元素
        /// </summary>
        T Pick<T>(IReadOnlyList<T> list);

        /// <summary>
        /// 从列表中随机不重复地抽取指定数量的元素
        /// </summary>
        List<T> PickMultiple<T>(IEnumerable<T> source, int count);

        /// <summary>
        /// 权重随机抽取（如：抽卡、物品掉落）
        /// </summary>
        /// <param name="items">候选列表</param>
        /// <param name="weightSelector">获取每个元素权重的方法</param>
        T PickWeighted<T>(IEnumerable<T> items, Func<T, float> weightSelector);
        
        /// <summary>
        /// 打乱列表顺序（Fisher-Yates 洗牌算法）
        /// </summary>
        void Shuffle<T>(IList<T> list);
    }
}