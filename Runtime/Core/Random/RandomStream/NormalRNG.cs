using System;
using System.Collections.Generic;
using System.Linq;

namespace GoveKits.Runtime.Core
{
    /// <summary>
    /// 标准随机数生成器（基于 System.Random，线程安全）
    /// </summary>
    public class NormalRNG : IRNG
    {
        private readonly object _lock = new object();
        private Random _random;

        public int Seed { get; private set; }

        public NormalRNG(int seed)
        {
            Reseed(seed);
        }

        public void Reseed(int seed)
        {
            lock (_lock)
            {
                Seed = seed;
                _random = new Random(seed);
            }
        }

        public int NextInt() 
        { 
            lock (_lock) return _random.Next(); 
        }
        
        public int NextInt(int maxExclusive) 
        { 
            if (maxExclusive <= 0) throw new ArgumentOutOfRangeException(nameof(maxExclusive));
            lock (_lock) return _random.Next(maxExclusive); 
        }

        public int Range(int minInclusive, int maxExclusive) 
        {
            if (maxExclusive <= minInclusive) throw new ArgumentOutOfRangeException(nameof(maxExclusive));
            lock (_lock) return _random.Next(minInclusive, maxExclusive); 
        }

        public float NextFloat() 
        { 
            lock (_lock) return (float)_random.NextDouble(); 
        }

        public float Range(float minInclusive, float maxInclusive) 
        {
            if (maxInclusive < minInclusive) throw new ArgumentOutOfRangeException(nameof(maxInclusive));
            return minInclusive + (maxInclusive - minInclusive) * NextFloat(); 
        }

        public bool Chance(float probability) 
        {
            if (probability <= 0f) return false;
            if (probability >= 1f) return true;
            return NextFloat() < probability;
        }

        public bool NextBool()
        {
            return NextFloat() < 0.5f;
        }

        public int NextSign()
        {
            return NextBool() ? 1 : -1;
        }

        /// <summary>
        /// 使用 Box-Muller 变换生成正态分布随机数
        /// </summary>
        public float NextGaussian(float mean = 0f, float stdDev = 1f)
        {
            // 使用 1.0 - NextFloat() 避免出现 0，因为 Math.Log(0) 会导致负无穷
            float u1 = 1.0f - NextFloat(); 
            float u2 = 1.0f - NextFloat();
            
            double randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
            return mean + stdDev * (float)randStdNormal;
        }

        public T Pick<T>(IReadOnlyList<T> list) 
        {
            if (list == null || list.Count == 0) throw new ArgumentException("List is null or empty");
            return list[NextInt(list.Count)];
        }

        /// <summary>
        /// 基于复制与洗牌的无放回抽取
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
        /// 轮盘赌算法实现权重抽取
        /// </summary>
        public T PickWeighted<T>(IEnumerable<T> items, Func<T, float> weightSelector)
        {
            if (items == null) throw new ArgumentNullException(nameof(items));
            if (weightSelector == null) throw new ArgumentNullException(nameof(weightSelector));

            float totalWeight = items.Sum(weightSelector);
            if (totalWeight <= 0) throw new ArgumentException("Total weight must be greater than 0");

            float randomVal = Range(0f, totalWeight);
            float currentWeight = 0f;

            foreach (var item in items)
            {
                currentWeight += weightSelector(item);
                if (randomVal <= currentWeight)
                {
                    return item;
                }
            }

            // 理论上不会走到这里，除非浮点数精度误差极其极端
            return items.Last();
        }

        public void Shuffle<T>(IList<T> list) 
        {
            if (list == null || list.Count < 2) return;
            
            lock (_lock)
            {
                // Fisher-Yates 核心算法
                for (var i = list.Count - 1; i > 0; i--)
                {
                    var j = _random.Next(i + 1);
                    (list[i], list[j]) = (list[j], list[i]);
                }
            }
        }
    }
}