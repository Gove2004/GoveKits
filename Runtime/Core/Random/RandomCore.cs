using System;
using System.Collections.Generic;

namespace GoveKits.Runtime.Core
{
    /// <summary>
    /// 随机系统静态入口
    /// </summary>
    public class RandomCore
    {
        private static IRNG rng;

        public static void Initialize(IRNG r) => rng = r;

        #region 代理方法

        /// <summary>
        /// 当前种子值（只读）
        /// </summary>
        public static int Seed => rng.Seed;
        
        /// <summary>
        /// 重新设置种子，重置随机序列
        /// </summary>
        public static void Reseed(int seed) => rng.Reseed(seed);
        
        /// <summary>
        /// 生成 0 到 int.MaxValue 之间的随机整数
        /// </summary>
        public static int NextInt() => rng.NextInt();
        
        /// <summary>
        /// 生成 0 到 maxExclusive-1 之间的随机整数
        /// </summary>
        public static int NextInt(int maxExclusive) => rng.NextInt(maxExclusive);
        
        /// <summary>
        /// 生成 minInclusive 到 maxExclusive-1 之间的随机整数
        /// </summary>
        public static int Range(int minInclusive, int maxExclusive) => rng.Range(minInclusive, maxExclusive);
        
        /// <summary>
        /// 生成 0.0 到 1.0 之间的随机浮点数
        /// </summary>
        public static float NextFloat() => rng.NextFloat();
        
        /// <summary>
        /// 生成 minInclusive 到 maxInclusive 之间的随机浮点数
        /// </summary>
        public static float Range(float minInclusive, float maxInclusive) => rng.Range(minInclusive, maxInclusive);
        
        /// <summary>
        /// 概率判定（传入 0.0~1.0 的概率，返回是否命中）
        /// </summary>
        public static bool Chance(float probability) => rng.Chance(probability);

        /// <summary>
        /// 生成随机布尔值（50% 为 true，50% 为 false）
        /// </summary>
        public static bool NextBool() => rng.NextBool();

        /// <summary>
        /// 生成随机符号（返回 1 或 -1）
        /// </summary>
        public static int NextSign() => rng.NextSign();

        /// <summary>
        /// 正态分布（高斯分布）随机数
        /// 常用于自然界的随机表现（如：子弹散布、角色身高、属性浮动）
        /// </summary>
        /// <param name="mean">均值（中心点）</param>
        /// <param name="stdDev">标准差（离散程度，值越大越分散）</param>
        public static float NextGaussian(float mean = 0f, float stdDev = 1f) => rng.NextGaussian(mean, stdDev);
        
        /// <summary>
        /// 从列表中随机等概率选择一个元素
        /// </summary>
        public static T Pick<T>(IReadOnlyList<T> list) => rng.Pick(list);

        /// <summary>
        /// 从列表中随机不重复地抽取指定数量的元素
        /// </summary>
        public static List<T> PickMultiple<T>(IEnumerable<T> source, int count) => rng.PickMultiple(source, count);

        /// <summary>
        /// 权重随机抽取（如：抽卡、物品掉落）
        /// </summary>
        /// <param name="items">候选列表</param>
        /// <param name="weightSelector">获取每个元素权重的方法</param>
        public static T PickWeighted<T>(IEnumerable<T> items, Func<T, float> weightSelector) => rng.PickWeighted(items, weightSelector);
        
        /// <summary>
        /// 打乱列表顺序（Fisher-Yates 洗牌算法）
        /// </summary>
        public static void Shuffle<T>(IList<T> list) => rng.Shuffle(list);

        #endregion

        #region 更多方法

        /// <summary>
        /// 创建指定类型的 RNG 实例
        /// </summary>
        /// <typeparam name="T">RNG 类型，必须实现 IRNG 接口并有接受 int 参数的构造函数</typeparam>
        /// <param name="seed">种子值</param>
        /// <returns>新创建的 RNG 实例</returns>
        public static T CreateRNG<T>(int seed) where T : IRNG
        {
            return (T)Activator.CreateInstance(typeof(T), seed);
        }

        #endregion

        public void Clear()
        {
            rng = null;
        }
    }
}