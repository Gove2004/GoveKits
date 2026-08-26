using System;
using System.Collections.Generic;
using GoveKits.Runtime.Util;

namespace GoveKits.Runtime.Unit
{
    /// <summary>
    /// Unit 组件全局注册与工厂中心。
    /// 将 UnitTag 映射到具体的 C# 类型，实现纯数据驱动的实例化流程。
    /// 所有技能、标记、反应的类型注册均通过此类完成。
    /// Intent 和 Effect 的统一工厂也在此处，对外屏蔽对象池细节。
    /// </summary>
    public static class UnitCore
    {
        #region 注册表

        /// <summary>技能 Tag → 类型注册表</summary>
        private static readonly Dictionary<UnitTag, Type> _abilityMap = new Dictionary<UnitTag, Type>();
        /// <summary>标记 Tag → 类型注册表</summary>
        private static readonly Dictionary<UnitTag, Type> _markMap = new Dictionary<UnitTag, Type>();
        /// <summary>反应 Tag → 类型注册表</summary>
        private static readonly Dictionary<UnitTag, Type> _reactionMap = new Dictionary<UnitTag, Type>();

        #endregion

        #region 注册（string 自动隐式转为 UnitTag）

        /// <summary>注册技能类型（要求无参构造）。</summary>
        public static void RegisterAbility<T>(UnitTag tag) where T : UnitAbility, new()
            => _abilityMap[tag] = typeof(T);

        /// <summary>注册标记类型（要求无参构造）。</summary>
        public static void RegisterMark<T>(UnitTag tag) where T : UnitMark, new()
            => _markMap[tag] = typeof(T);

        /// <summary>注册反应类型（要求无参构造）。</summary>
        public static void RegisterReaction<T>(UnitTag tag) where T : UnitReaction, new()
            => _reactionMap[tag] = typeof(T);

        #endregion

        #region 字符串工厂（查注册表反射创建）

        /// <summary>根据标签创建技能实例（查注册表，反射实例化）。</summary>
        public static UnitAbility CreateAbility(UnitTag tag)
        {
            if (_abilityMap.TryGetValue(tag, out var type))
            {
                try
                {
                    return Activator.CreateInstance(type) as UnitAbility;
                }
                catch (Exception e)
                {
                    LogCore.Error(nameof(UnitCore), $"工厂创建技能失败: {tag} {e.Message}");
                    return null;
                }
            }
            LogCore.Error(nameof(UnitCore), $"未找到技能配置: {tag}");
            return null;
        }

        /// <summary>根据标签创建标记实例（查注册表，反射实例化）。</summary>
        public static UnitMark CreateMark(UnitTag tag)
        {
            if (_markMap.TryGetValue(tag, out var type))
            {
                try
                {
                    return Activator.CreateInstance(type) as UnitMark;
                }
                catch (Exception e)
                {
                    LogCore.Error(nameof(UnitCore), $"工厂创建标记失败: {tag} {e.Message}");
                    return null;
                }
            }
            LogCore.Error(nameof(UnitCore), $"未找到标记配置: {tag}");
            return null;
        }

        /// <summary>根据标签创建反应实例（查注册表，反射实例化）。</summary>
        public static UnitReaction CreateReaction(UnitTag tag)
        {
            if (_reactionMap.TryGetValue(tag, out var type))
            {
                try
                {
                    return Activator.CreateInstance(type) as UnitReaction;
                }
                catch (Exception e)
                {
                    LogCore.Error(nameof(UnitCore), $"工厂创建反应失败: {tag} {e.Message}");
                    return null;
                }
            }
            LogCore.Error(nameof(UnitCore), $"未找到反应配置: {tag}");
            return null;
        }

        #endregion

        #region 类型工厂（直接 new T，fluent API）

        /// <summary>直接创建技能实例（不走注册表，适用于代码硬编码）。</summary>
        public static T CreateAbility<T>() where T : UnitAbility, new()
            => new T();

        /// <summary>直接创建标记实例（不走注册表，适用于代码硬编码）。</summary>
        public static T CreateMark<T>() where T : UnitMark, new()
            => new T();

        /// <summary>直接创建反应实例（不走注册表，适用于代码硬编码）。</summary>
        public static T CreateReaction<T>() where T : UnitReaction, new()
            => new T();

        #endregion

        #region Intent / Effect 统一工厂

        /// <summary>从对象池获取一个 Intent 实例并设置 Source/Target。</summary>
        public static T CreateIntent<T>(IUnit source = null, IUnit target = null) where T : UnitIntent, new()
        {
            var intent = PoolCore.Get<T>();
            intent.Source = source;
            intent.Target = target;
            return intent;
        }

        /// <summary>直接 new 一个 Intent 实例（不从对象池获取）。</summary>
        public static T CreateIntentRaw<T>() where T : UnitIntent, new()
            => new T();

        /// <summary>从对象池获取一个 Effect 实例。</summary>
        public static T CreateEffect<T>() where T : UnitEffect<T>, new()
            => PoolCore.Get<T>();

        /// <summary>直接 new 一个 Effect 实例（不从对象池获取）。</summary>
        public static T CreateEffectRaw<T>() where T : UnitEffect<T>, new()
            => new T();

        #endregion

        #region 关闭

        /// <summary>关闭所有注册表，释放内存</summary>
        public static void Close()
        {
            _abilityMap.Clear();
            _markMap.Clear();
            _reactionMap.Clear();
        }

        #endregion
    }
}
