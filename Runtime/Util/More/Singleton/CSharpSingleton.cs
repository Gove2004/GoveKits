using System;
using System.Collections.Generic;
using UnityEngine;

namespace GoveKits.Runtime.Util
{
    /// <summary>
    /// CSharpSingleton 静态状态重置注册表（内部使用）。
    /// [RuntimeInitializeOnLoadMethod] 不能用于泛型类，各封闭单例在静态构造时注册重置动作，
    /// 由本类在每次 Play 开始前（关闭 Domain Reload 时）统一执行。
    /// </summary>
    internal static class CSharpSingletonResetRegistry
    {
        private static readonly List<Action> _resets = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetAll()
        {
            foreach (var reset in _resets)
            {
                reset();
            }
        }

        public static void Register(Action reset) => _resets.Add(reset);
    }

    /// <summary>
    /// 纯 C# 单例基类。首次访问 Instance 时创建，DestroyInstance 时销毁。
    /// 适用于不依赖 MonoBehaviour 的纯逻辑单例。
    /// </summary>
    public abstract class CSharpSingleton<T> where T : CSharpSingleton<T>, new()
    {
        private static T _instance;
        private static bool _initialized;

        static CSharpSingleton()
        {
            // 注册静态状态重置（由非泛型注册表代为挂 RuntimeInitializeOnLoadMethod）
            CSharpSingletonResetRegistry.Register(() =>
            {
                _instance = null;
                _initialized = false;
            });
        }

        /// <summary>单例实例，首次访问时自动创建并调用 Init。Init 抛出异常时实例回滚，下次访问将重试。</summary>
        public static T Instance
        {
            get
            {
                if (_instance != null)
                    return _instance;

                _instance = new T();
                try
                {
                    _instance.Init();
                }
                catch
                {
                    _instance = null;
                    throw;
                }
                _initialized = true;
                return _instance;
            }
        }

        /// <summary>销毁单例实例，销毁前自动调用 Uninit。</summary>
        public static void DestroyInstance()
        {
            if (_instance == null)
                return;

            if (_initialized)
            {
                _instance.Uninit();
                _initialized = false;
            }

            _instance = null;
        }

        /// <summary>首次创建后调用一次，子类可重写。</summary>
        protected virtual void Init() { }

        /// <summary>销毁前调用一次，子类可重写。</summary>
        protected virtual void Uninit() { }
    }
}
