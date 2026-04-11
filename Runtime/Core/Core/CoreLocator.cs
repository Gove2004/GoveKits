using System;
using System.Collections.Generic;
using GoveKits.Runtime.Network;
using GoveKits.Runtime.Procedure;
using GoveKits.Runtime.Storage;

namespace GoveKits.Runtime.Core
{
    public static class CoreLocator
    {
        private static readonly Dictionary<Type, ICore> _cores = new Dictionary<Type, ICore>();

        /// <summary>
        /// 注入核心
        /// </summary>
        public static void InfuseCore<T>(T core) where T : class, ICore
        {
            _cores[typeof(T)] = core;
        }

        /// <summary>
        /// 检查核心是否存在
        /// </summary>
        public static bool HasCore<T>() where T : class, ICore
        {
            return _cores.ContainsKey(typeof(T));
        }

        /// <summary>
        /// 获取指定核心
        /// </summary>
        public static T GetCore<T>() where T : class, ICore
        {
            return _cores[typeof(T)] as T;
        }

        public static void Clear()
        {
            foreach (var core in _cores.Values)
            {
                core.OnShutdown();
            }
            _cores.Clear();
        }

        #region 内置外观 快速访问

        // Core 外观
        public static RandomCore Random => GetCore<RandomCore>();
        public static LogCore Log => GetCore<LogCore>();
        public static PoolCore Pool => GetCore<PoolCore>();
        public static EventCore Event => GetCore<EventCore>();

        // Network 外观
        public static HttpCore Http => GetCore<HttpCore>();
        public static FTPCore FTP => GetCore<FTPCore>();
        public static ProtocolCore Protocol => GetCore<ProtocolCore>();
        public static DispatcherCore Dispatcher => GetCore<DispatcherCore>();
        public static ClientCore Client => GetCore<ClientCore>();
        public static ServerCore Server => GetCore<ServerCore>();

        // Procedure 外观
        public static TimeCore Time => GetCore<TimeCore>();
        public static SceneCore Scene => GetCore<SceneCore>();

        // Storage 外观
        public static SaveCore Save => GetCore<SaveCore>();
        public static ResCore Res => GetCore<ResCore>();
        public static ConfigCore Config => GetCore<ConfigCore>();

        public static AudioCore Audio => GetCore<AudioCore>();
        public static LocalizationCore Localization => GetCore<LocalizationCore>();
        

        #endregion
    }
}