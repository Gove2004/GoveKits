using System.Collections.Generic;



public class Param : Dictionary<string, object>
{
    public void Put(string key, object value)
    {
        this[key] = value;
    }

    public T Get<T>(string key, T defaultValue = default)
    {
        if (TryGetValue(key, out object value) && value is T typedValue)
        {
            return typedValue;
        }
        return defaultValue;
    }

    public static byte[] ToBytes(string str)
    {
        return System.Text.Encoding.UTF8.GetBytes(str);
    }

    public static string FromBytes(byte[] bytes)
    {
        return System.Text.Encoding.UTF8.GetString(bytes);
    }
}