using System;
using System.Collections.Generic;
using System.Reflection;

namespace GoveKits.Runtime.Storage
{
    /// <summary>
    /// 标记配置类型对应的文件路径、来源和解析器。
    /// </summary>
    /// <example>
    /// [ConfigPath("Configs/Enemy")]
    /// </example>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class ConfigPathAttribute : Attribute
    {
        public ConfigPathAttribute(string filePath, string extension) 
        { 
            FilePath = filePath; 
            Extension = extension; 
        }

        /// <summary>
        /// 配置文件相对路径
        /// </summary>
        public string FilePath { get; }
        public string Extension { get; }
    }


    /// <summary>
    /// 配置类型绑定信息。
    /// </summary>
    internal readonly struct ConfigBinding
    {
        public ConfigBinding(Type configType, ConfigPathAttribute attribute)
        {
            ConfigType = configType;
            Attribute = attribute;
        }

        public Type ConfigType { get; }

        public ConfigPathAttribute Attribute { get; }
    }

    /// <summary>
    /// 负责扫描带 ConfigAttribute 的配置类型。
    /// </summary>
    internal static class ConfigBindingScanner
    {
        /// <summary>
        /// 扫描当前 AppDomain 中可用程序集，收集全部配置绑定。
        /// </summary>
        /// <returns>配置类型与注解信息列表。</returns>
        public static List<ConfigBinding> Scan()
        {
            var result = new List<ConfigBinding>();
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (int i = 0; i < assemblies.Length; i++)
            {
                Assembly assembly = assemblies[i];
                if (assembly == null || assembly.IsDynamic)
                {
                    continue;
                }

                Type[] types;
                try
                {
                    types = assembly.GetTypes();
                }
                catch (ReflectionTypeLoadException ex)
                {
                    types = ex.Types;
                }

                if (types == null)
                {
                    continue;
                }

                for (int j = 0; j < types.Length; j++)
                {
                    Type type = types[j];
                    if (type == null || type.IsInterface || type.IsAbstract)
                    {
                        continue;
                    }

                    if (!typeof(IConfigData).IsAssignableFrom(type))
                    {
                        continue;
                    }

                    ConfigPathAttribute attribute = type.GetCustomAttribute<ConfigPathAttribute>(false);
                    if (attribute == null || string.IsNullOrWhiteSpace(attribute.FilePath))
                    {
                        continue;
                    }

                    result.Add(new ConfigBinding(type, attribute));
                }
            }

            return result;
        }
    }
}