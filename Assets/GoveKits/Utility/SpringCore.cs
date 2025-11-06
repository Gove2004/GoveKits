using System.Collections.Generic;


public class SpringCore
{
    private static Dictionary<string, string> _beans = new Dictionary<string, string>();

    public static void RegisterBean(string name, string value)
    {
        _beans[name] = value;
    }
    
    public static string GetBean(string name)
    {
        return _beans.TryGetValue(name, out var value) ? value : null;
    }
}