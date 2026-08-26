using System;
using System.Collections.Generic;
using System.Reflection;

namespace GoveKits.Runtime.Storage
{
    /// <summary>
    /// 配置类型与路径特性的绑定信息，用于关联配置类和其对应的资源文件路径。
    /// </summary>
    internal readonly struct ConfigBinding
    {
        /// <summary>
        /// 创建配置绑定信息。
        /// </summary>
        /// <param name="configType">配置数据类型。</param>
        /// <param name="attribute">该类型上的 [ConfigPath] 特性。</param>
        public ConfigBinding(Type configType, ConfigPathAttribute attribute)
        {
            ConfigType = configType;
            Attribute = attribute;
        }

        /// <summary>
        /// 配置类类型。
        /// </summary>
        public Type ConfigType { get; }
        /// <summary>
        /// 配置路径特性，包含资源文件和扩展名信息。
        /// </summary>
        public ConfigPathAttribute Attribute { get; }
    }

    /// <summary>
    /// 负责在当前域的所有程序集中扫描标注了 [ConfigPath] 的配置类型，并生成绑定关系列表。
    /// </summary>
    internal static class ConfigBindingScanner
    {
        /// <summary>
        /// 扫描所有程序集，收集带有 [ConfigPath] 特性的配置类型。
        /// </summary>
        /// <returns>配置类型与路径特性的绑定列表。</returns>
        public static List<ConfigBinding> Scan()
        {
            var result = new List<ConfigBinding>();
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();

            for (int i = 0; i < assemblies.Length; i++)
            {
                Assembly assembly = assemblies[i];
                if (assembly == null || assembly.IsDynamic) continue;

                Type[] types;
                try { types = assembly.GetTypes(); }
                catch (ReflectionTypeLoadException ex) { types = ex.Types; continue; }

                if (types == null) continue;

                for (int j = 0; j < types.Length; j++)
                {
                    Type type = types[j];
                    if (type == null || type.IsInterface || type.IsAbstract) continue;
                    if (!typeof(IConfigData).IsAssignableFrom(type)) continue;

                    ConfigPathAttribute attribute = type.GetCustomAttribute<ConfigPathAttribute>(false);
                    if (attribute == null || string.IsNullOrWhiteSpace(attribute.FilePath)) continue;

                    result.Add(new ConfigBinding(type, attribute));
                }
            }

            return result;
        }
    }
}
