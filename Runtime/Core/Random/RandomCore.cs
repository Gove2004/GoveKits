using System;
using System.Collections.Generic;

namespace GoveKits.Runtime.Core
{
    /// <summary>
    /// 随机系统静态入口
    /// </summary>
    /// <remarks>
    /// 默认代理一个主随机流 (Main Stream)
    /// 支持基于主种子派生创建独立的临时随机流，供地图生成、掉落等高频场景使用
    /// 采用静态类设计，全局可访问，无需实例化
    /// 提供统一的随机数生成接口
    /// </remarks>
    public static class RandomCore
    {
        /// <summary>
        /// 默认随机流实例
        /// </summary>
        /// <remarks>
        /// 所有快捷方法都代理到此流
        /// 通过 Init() 初始化，使用前必须调用
        /// </remarks>
        private static IRandomStream _defaultStream;
        
        /// <summary>
        /// 当前种子值（只读）
        /// </summary>
        /// <remarks>
        /// 获取默认随机流的种子
        /// 用于调试、回放、日志记录等
        /// </remarks>
        public static int Seed => _defaultStream.Seed;

        /// <summary>
        /// 初始化随机系统
        /// </summary>
        /// <param name="masterSeed">主种子值</param>
        /// <remarks>
        /// 必须在首次使用前调用
        /// 创建默认随机流实例
        /// 相同种子可复现相同的随机序列
        /// </remarks>
        public static void Init(int masterSeed)
        {
            _defaultStream = new RandomStream(masterSeed);
        }

        #region Factory: 创建独立随机流

        /// <summary>
        /// 创建独立随机流
        /// </summary>
        /// <param name="seed">随机流种子值</param>
        /// <returns>新的随机流实例</returns>
        /// <remarks>
        /// 直接基于一个指定的 Seed 创建一个独立随机流
        /// 适用于需要独立随机状态的场景：
        /// - 地图生成：使用独立种子，不影响主随机流
        /// - 掉落计算：可复现相同的掉落结果
        /// - 战斗模拟：可回放战斗过程
        /// 独立流之间互不影响
        /// </remarks>
        public static IRandomStream CreateStream(int seed)
        {
            return new RandomStream(seed);
        }

        #endregion

        #region Main Stream Proxy API (主流快捷代理)
        
        // ==================== 整数随机 ====================
        
        /// <summary>
        /// 生成随机整数（全范围）
        /// </summary>
        /// <returns>0 到 int.MaxValue 之间的随机整数</returns>
        /// <remarks>
        /// 全局通用方法，直接代理到底层的 _defaultStream，无任何额外开销
        /// </remarks>
        public static int NextInt() => _defaultStream.NextInt();
        
        /// <summary>
        /// 生成随机整数（指定上限）
        /// </summary>
        /// <param name="maxExclusive">上限（不包含）</param>
        /// <returns>0 到 maxExclusive-1 之间的随机整数</returns>
        public static int NextInt(int maxExclusive) => _defaultStream.NextInt(maxExclusive);
        
        /// <summary>
        /// 生成随机整数（指定范围）
        /// </summary>
        /// <param name="minInclusive">下限（包含）</param>
        /// <param name="maxExclusive">上限（不包含）</param>
        /// <returns>minInclusive 到 maxExclusive-1 之间的随机整数</returns>
        public static int Range(int minInclusive, int maxExclusive) => _defaultStream.Range(minInclusive, maxExclusive);

        // ==================== 浮点数随机 ====================
        
        /// <summary>
        /// 生成随机浮点数（0-1 范围）
        /// </summary>
        /// <returns>0.0 到 1.0 之间的随机浮点数</returns>
        public static float NextFloat() => _defaultStream.NextFloat();
        
        /// <summary>
        /// 生成随机浮点数（指定范围）
        /// </summary>
        /// <param name="minInclusive">下限（包含）</param>
        /// <param name="maxInclusive">上限（包含）</param>
        /// <returns>minInclusive 到 maxInclusive 之间的随机浮点数</returns>
        public static float Range(float minInclusive, float maxInclusive) => _defaultStream.Range(minInclusive, maxInclusive);

        // ==================== 概率判定 ====================
        
        /// <summary>
        /// 概率判定
        /// </summary>
        /// <param name="probability">成功概率（0.0-1.0）</param>
        /// <returns>成功返回 true，失败返回 false</returns>
        /// <remarks>
        /// 例如：Chance(0.3f) 表示 30% 概率返回 true
        /// 适用于掉落判定、暴击判定等场景
        /// </remarks>
        public static bool Chance(float probability) => _defaultStream.Chance(probability);

        // ==================== 列表操作 ====================
        
        /// <summary>
        /// 从列表中随机选择一个元素
        /// </summary>
        /// <typeparam name="T">元素类型</typeparam>
        /// <param name="list">源列表</param>
        /// <returns>随机选中的元素</returns>
        public static T Pick<T>(IReadOnlyList<T> list) => _defaultStream.Pick(list);
        
        /// <summary>
        /// 打乱列表顺序
        /// </summary>
        /// <typeparam name="T">元素类型</typeparam>
        /// <param name="list">要洗牌的列表</param>
        /// <remarks>
        /// 原地修改列表，使用 Fisher-Yates 算法
        /// </remarks>
        public static void Shuffle<T>(IList<T> list) => _defaultStream.Shuffle(list);

        #endregion
    }
}