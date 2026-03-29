using System;
using System.Collections.Generic;

namespace GoveKits.Runtime.Core
{
    /// <summary>
    /// 随机流实现类
    /// </summary>
    /// <remarks>
    /// 内部的流实现类，基于 System.Random 封装
    /// 加了细粒度锁，保证即使同一个独立流在多线程间被共享，也不会引发内部 Random 崩溃
    /// 实现 IRandomStream 接口，提供完整的随机数生成功能
    /// </remarks>
    internal class RandomStream : IRandomStream
    {
        /// <summary>
        /// 线程同步锁对象
        /// </summary>
        /// <remarks>
        /// System.Random 不是线程安全的
        /// 多线程并发访问时可能导致返回 0 或异常
        /// 使用 lock 确保线程安全
        /// </remarks>
        private readonly object _lock = new object();
        
        /// <summary>
        /// 底层随机数生成器
        /// </summary>
        /// <remarks>
        /// 使用 System.Random 作为随机算法实现
        /// 通过种子初始化，可复现相同随机序列
        /// </remarks>
        private Random _random;

        /// <summary>
        /// 当前种子值（只读）
        /// </summary>
        public int Seed { get; private set; }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="seed">初始种子值</param>
        /// <remarks>
        /// 创建时自动调用 Reseed 初始化随机数生成器
        /// </remarks>
        public RandomStream(int seed)
        {
            Reseed(seed);
        }

        /// <summary>
        /// 重新设置种子
        /// </summary>
        /// <param name="seed">新的种子值</param>
        /// <remarks>
        /// 加锁保护，确保线程安全
        /// 重置后会生成全新的随机序列
        /// 适用于需要重置随机状态的场景
        /// </remarks>
        public void Reseed(int seed)
        {
            lock (_lock)
            {
                Seed = seed;
                _random = new Random(seed);
            }
        }

        /// <summary>
        /// 生成随机整数（全范围）
        /// </summary>
        /// <returns>0 到 int.MaxValue 之间的随机整数</returns>
        /// <remarks>
        /// 加锁保护，确保线程安全
        /// </remarks>
        public int NextInt() 
        { 
            lock (_lock) return _random.Next(); 
        }
        
        /// <summary>
        /// 生成随机整数（指定上限）
        /// </summary>
        /// <param name="maxExclusive">上限（不包含）</param>
        /// <returns>0 到 maxExclusive-1 之间的随机整数</returns>
        /// <remarks>
        /// 参数验证：maxExclusive 必须大于 0
        /// 加锁保护，确保线程安全
        /// </remarks>
        public int NextInt(int maxExclusive) 
        { 
            if (maxExclusive <= 0) throw new ArgumentOutOfRangeException(nameof(maxExclusive));
            lock (_lock) return _random.Next(maxExclusive); 
        }

        /// <summary>
        /// 生成随机整数（指定范围）
        /// </summary>
        /// <param name="minInclusive">下限（包含）</param>
        /// <param name="maxExclusive">上限（不包含）</param>
        /// <returns>minInclusive 到 maxExclusive-1 之间的随机整数</returns>
        /// <remarks>
        /// 参数验证：maxExclusive 必须大于 minInclusive
        /// 加锁保护，确保线程安全
        /// </remarks>
        public int Range(int minInclusive, int maxExclusive) 
        {
            if (maxExclusive <= minInclusive) throw new ArgumentOutOfRangeException(nameof(maxExclusive));
            lock (_lock) return _random.Next(minInclusive, maxExclusive); 
        }

        /// <summary>
        /// 生成随机浮点数（0-1 范围）
        /// </summary>
        /// <returns>0.0 到 1.0 之间的随机浮点数</returns>
        /// <remarks>
        /// 基于 NextDouble() 转换为 float
        /// 加锁保护，确保线程安全
        /// </remarks>
        public float NextFloat() 
        { 
            lock (_lock) return (float)_random.NextDouble(); 
        }

        /// <summary>
        /// 生成随机浮点数（指定范围）
        /// </summary>
        /// <param name="minInclusive">下限（包含）</param>
        /// <param name="maxInclusive">上限（包含）</param>
        /// <returns>minInclusive 到 maxInclusive 之间的随机浮点数</returns>
        /// <remarks>
        /// 参数验证：maxInclusive 必须大于等于 minInclusive
        /// 使用线性插值计算：min + (max - min) * random
        /// 调用 NextFloat() 已加锁，此处无需重复加锁
        /// </remarks>
        public float Range(float minInclusive, float maxInclusive) 
        {
            if (maxInclusive < minInclusive) throw new ArgumentOutOfRangeException(nameof(maxInclusive));
            return minInclusive + (maxInclusive - minInclusive) * NextFloat(); 
        }

        /// <summary>
        /// 概率判定
        /// </summary>
        /// <param name="probability">成功概率（0.0-1.0）</param>
        /// <returns>成功返回 true，失败返回 false</returns>
        /// <remarks>
        /// 边界优化：
        /// - probability <= 0 时直接返回 false
        /// - probability >= 1 时直接返回 true
        /// - 其他情况与随机数比较
        /// 适用于掉落判定、暴击判定等场景
        /// </remarks>
        public bool Chance(float probability) 
        {
            if (probability <= 0f) return false;
            if (probability >= 1f) return true;
            return NextFloat() < probability;
        }

        /// <summary>
        /// 从列表中随机选择一个元素
        /// </summary>
        /// <typeparam name="T">元素类型</typeparam>
        /// <param name="list">源列表</param>
        /// <returns>随机选中的元素</returns>
        /// <remarks>
        /// 参数验证：列表不能为空
        /// 使用 NextInt(list.Count) 生成随机索引
        /// 适用于随机奖励、随机目标等场景
        /// </remarks>
        public T Pick<T>(IReadOnlyList<T> list) 
        {
            if (list == null || list.Count == 0) throw new ArgumentException("List empty");
            return list[NextInt(list.Count)];
        }

        /// <summary>
        /// 打乱列表顺序（Fisher-Yates 洗牌算法）
        /// </summary>
        /// <typeparam name="T">元素类型</typeparam>
        /// <param name="list">要洗牌的列表</param>
        /// <remarks>
        /// 参数验证：列表为空时直接返回
        /// 从后向前遍历，每次与前面的随机位置交换
        /// 加锁保护，确保线程安全
        /// 时间复杂度 O(n)，空间复杂度 O(1)
        /// </remarks>
        public void Shuffle<T>(IList<T> list) 
        {
            if (list == null) return;
            lock (_lock)
            {
                // Fisher-Yates 洗牌算法
                for (var i = list.Count - 1; i > 0; i--)
                {
                    // 生成 0 到 i 的随机索引
                    var j = _random.Next(i + 1);
                    // 交换元素
                    (list[i], list[j]) = (list[j], list[i]);
                }
            }
        }
    }
}