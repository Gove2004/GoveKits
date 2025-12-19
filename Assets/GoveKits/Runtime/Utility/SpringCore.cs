using System.Collections.Generic;

/// <summary>
/// 轻量级 Bean 容器：存储和检索命名的字符串值对。
/// 适用于全局配置、轻量级依赖注入等场景。
/// </summary>
public class SpringCore
{
    private static Dictionary<string, string> _beans = new Dictionary<string, string>();

    /// <summary>
    /// 注册一个 Bean（键值对）。若键已存在则覆盖。
    /// </summary>
    /// <param name="name">Bean 名称（键）。</param>
    /// <param name="value">Bean 值。</param>
    public static void RegisterBean(string name, string value)
    {
        _beans[name] = value;
    }
    
    /// <summary>
    /// 获取指定名称的 Bean。
    /// </summary>
    /// <param name="name">Bean 名称（键）。</param>
    /// <returns>Bean 值，若不存在则返回 null。</returns>
    public static string GetBean(string name)
    {
        return _beans.TryGetValue(name, out var value) ? value : null;
    }
}