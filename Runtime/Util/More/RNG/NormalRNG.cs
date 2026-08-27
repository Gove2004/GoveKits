using System;
using System.Collections.Generic;
using System.Linq;

namespace GoveKits.Runtime.Util
{
    /// <summary>
    /// 标准随机数生成器，System.Random 实现。
    /// 适用于单线程（主线程）环境，多线程场景请各线程独立实例。
    /// </summary>
    public class NormalRNG : IRNG
    {
        private Random _random;

        /// <summary>当前种子值。</summary>
        public int Seed { get; private set; }

        /// <summary>
        /// 使用指定种子创建实例。同一种子产生相同随机序列，可用于结果复现。
        /// </summary>
        /// <param name="seed">随机种子</param>
        public NormalRNG(int seed)
        {
            Reseed(seed);
        }

        /// <summary>
        /// 重新设置种子，重置随机序列。
        /// </summary>
        /// <param name="seed">新的种子值</param>
        public void Reseed(int seed)
        {
            Seed = seed;
            _random = new Random(seed);
        }

        /// <summary>生成 0 到 int.MaxValue 之间的随机整数。</summary>
        public int NextInt()
        {
            return _random.Next();
        }

        /// <summary>生成 0 到 maxExclusive-1 之间的随机整数。</summary>
        public int NextInt(int maxExclusive)
        {
            if (maxExclusive <= 0) throw new ArgumentOutOfRangeException(nameof(maxExclusive));
            return _random.Next(maxExclusive);
        }

        /// <summary>生成 minInclusive 到 maxExclusive-1 之间的随机整数。</summary>
        public int Range(int minInclusive, int maxExclusive)
        {
            if (maxExclusive <= minInclusive) throw new ArgumentOutOfRangeException(nameof(maxExclusive));
            return _random.Next(minInclusive, maxExclusive);
        }

        /// <summary>生成 0.0 到 1.0 之间的随机浮点数。</summary>
        public float NextFloat()
        {
            return (float)_random.NextDouble();
        }

        /// <summary>生成 minInclusive 到 maxInclusive 之间的随机浮点数。</summary>
        public float Range(float minInclusive, float maxInclusive)
        {
            if (maxInclusive < minInclusive) throw new ArgumentOutOfRangeException(nameof(maxInclusive));
            return minInclusive + (maxInclusive - minInclusive) * NextFloat();
        }

        /// <summary>概率判定，传入 0.0~1.0 的概率值，返回是否命中。</summary>
        public bool Chance(float probability)
        {
            if (probability <= 0f) return false;
            if (probability >= 1f) return true;
            return NextFloat() < probability;
        }

        /// <summary>生成随机布尔值，约 50% 概率返回 true。</summary>
        public bool NextBool()
        {
            return NextFloat() < 0.5f;
        }

        /// <summary>生成随机符号，返回 1 或 -1。</summary>
        public int NextSign()
        {
            return NextBool() ? 1 : -1;
        }

        /// <summary>
        /// 使用 Box-Muller 变换生成正态分布（高斯分布）随机数。
        /// </summary>
        public float NextGaussian(float mean = 0f, float stdDev = 1f)
        {
            float u1 = 1.0f - NextFloat();
            float u2 = 1.0f - NextFloat();
            double randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
            return mean + stdDev * (float)randStdNormal;
        }

        /// <summary>从列表中随机等概率选择一个元素。</summary>
        public T Pick<T>(IReadOnlyList<T> list)
        {
            if (list == null || list.Count == 0) throw new ArgumentException("列表不能为 null 或空。");
            return list[NextInt(list.Count)];
        }

        /// <summary>
        /// 基于复制与洗牌的无放回随机抽取。
        /// </summary>
        public List<T> PickMultiple<T>(IEnumerable<T> source, int count)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (count <= 0) return new List<T>();

            var list = source.ToList();
            if (count >= list.Count) return list;

            Shuffle(list);
            return list.GetRange(0, count);
        }

        /// <summary>
        /// 轮盘赌权重抽取，按各元素的权重比例随机选择。
        /// </summary>
        public T PickWeighted<T>(IEnumerable<T> items, Func<T, float> weightSelector)
        {
            if (items == null) throw new ArgumentNullException(nameof(items));
            if (weightSelector == null) throw new ArgumentNullException(nameof(weightSelector));

            float totalWeight = items.Sum(weightSelector);
            if (totalWeight <= 0) throw new ArgumentException("总权重必须大于 0。");

            float randomVal = Range(0f, totalWeight);
            float currentWeight = 0f;

            foreach (var item in items)
            {
                currentWeight += weightSelector(item);
                if (randomVal <= currentWeight)
                    return item;
            }

            return items.Last();
        }

        /// <summary>使用 Fisher-Yates 算法原地打乱列表顺序。</summary>
        public void Shuffle<T>(IList<T> list)
        {
            if (list == null || list.Count < 2) return;

            for (var i = list.Count - 1; i > 0; i--)
            {
                var j = _random.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}