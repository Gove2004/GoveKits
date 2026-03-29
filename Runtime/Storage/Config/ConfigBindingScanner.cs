using System;
using System.Collections.Generic;
using System.Reflection;

namespace GoveKits.Runtime.Storage.Config
{
    /// <summary>
    /// 标记配置类型对应的文件路径、来源和解析器。
    /// </summary>
    /// <example>
    /// [Config("Configs/Enemy", ConfigFileType.Json, ConfigSourceType.Resources)]
    /// </example>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class ConfigAttribute : Attribute
    {
        public ConfigAttribute(string filePath, ConfigFileType parseType, ConfigSourceType sourceType = ConfigSourceType.Resources)
        {
            FilePath = filePath;
            ParseType = parseType;
            SourceType = sourceType;
        }

        /// <summary>
        /// 配置文件相对路径。
        /// Resources 来源可写带扩展名或不带扩展名。
        /// StreamingAssets 来源建议带扩展名。
        /// </summary>
        public string FilePath { get; }

        /// <summary>
        /// 解析类型。
        /// </summary>
        public ConfigFileType ParseType { get; }

        /// <summary>
        /// 配置来源。
        /// </summary>
        public ConfigSourceType SourceType { get; }
    }


    /// <summary>
    /// 配置类型绑定信息。
    /// </summary>
    internal readonly struct ConfigBinding
    {
        public ConfigBinding(Type configType, ConfigAttribute attribute)
        {
            ConfigType = configType;
            Attribute = attribute;
        }

        public Type ConfigType { get; }

        public ConfigAttribute Attribute { get; }
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

                    ConfigAttribute attribute = type.GetCustomAttribute<ConfigAttribute>(false);
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