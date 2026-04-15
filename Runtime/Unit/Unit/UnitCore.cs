using System;
using System.Collections.Generic;
using GoveKits.Runtime.Core;

namespace GoveKits.Runtime.Unit
{
    /// <summary>
    /// Unit 组件全局注册与工厂中心。
    /// 职责：将 UnitTag 映射到具体的 C# 类型，实现纯数据驱动实例化。
    /// </summary>
    public static class UnitCore
    {
        private static readonly Dictionary<UnitTag, Type> _abilityMap = new();
        private static readonly Dictionary<UnitTag, Type> _markMap = new();
        private static readonly Dictionary<UnitTag, Type> _reactionMap = new();

        #region 注册中心

        /// <summary>注册技能类型（要求无参构造）</summary>
        public static void RegisterAbility<T>(UnitTag tag) where T : UnitAbility, new() 
            => _abilityMap[tag] = typeof(T);

        /// <summary>注册标记类型（要求无参构造）</summary>
        public static void RegisterMark<T>(UnitTag tag) where T : UnitMark, new() 
            => _markMap[tag] = typeof(T);

        /// <summary>注册反应类型（要求无参构造）</summary>
        public static void RegisterReaction<T>(UnitTag tag) where T : UnitReaction, new() 
            => _reactionMap[tag] = typeof(T);

        #endregion

        #region 工厂方法

        public static UnitAbility CreateAbility(UnitTag tag)
        {
            if (_abilityMap.TryGetValue(tag, out var type))
                return Activator.CreateInstance(type) as UnitAbility;
                
            LogCore.Error("UnitCore", $"工厂创建失败，未找到技能配置: {tag}");
            return null;
        }

        public static UnitMark CreateMark(UnitTag tag, int stack = 1, float duration = -1f)
        {
            if (_markMap.TryGetValue(tag, out var type))
            {
                var mark = Activator.CreateInstance(type) as UnitMark;
                // 利用流式接口设置初始数据
                return mark?.SetStack(stack).SetDuration(duration);
            }
            
            LogCore.Error("UnitCore", $"工厂创建失败，未找到标记配置: {tag}");
            return null;
        }

        public static UnitReaction CreateReaction(UnitTag tag)
        {
            if (_reactionMap.TryGetValue(tag, out var type))
                return Activator.CreateInstance(type) as UnitReaction;
                
            LogCore.Error("UnitCore", $"工厂创建失败，未找到反应配置: {tag}");
            return null;
        }

        #endregion
        
        public static void ClearAllRegistries()
        {
            _abilityMap.Clear();
            _markMap.Clear();
            _reactionMap.Clear();
        }
    }
}