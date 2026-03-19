using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using UnityEngine;
using GoveKits.Runtime.Core;

namespace GoveKits.Runtime.Storage.Config
{
    /// <summary>
    /// Json 配置解析器。
    /// </summary>
    /// <remarks>
    /// 支持以下 JSON 结构:
    /// 1) List&lt;T&gt;
    /// 2) Dictionary&lt;int, T&gt;
    /// 3) Dictionary&lt;string, T&gt;
    /// 4) 单对象 T
    /// </remarks>
    public sealed class JsonConfigParser : IConfigParser
    {
        private static readonly string[] ParserExtensions = { ".json" };

        public IReadOnlyList<string> Extensions => ParserExtensions;

        public List<T> Parse<T>(byte[] bytes, string text) where T : class, IConfigData, new()
        {
            string json = string.IsNullOrEmpty(text)
                ? Encoding.UTF8.GetString(bytes)
                : text;

            if (string.IsNullOrWhiteSpace(json))
            {
                return new List<T>();
            }

            try
            {
                var list = JsonConvert.DeserializeObject<List<T>>(json);
                if (list != null)
                {
                    return list;
                }
            }
            catch
            {
                GoveKitsCore.Log("JsonConfigParser", "Failed to parse as List<T>, trying other formats.", logType: LogType.Warning);
            }

            try
            {
                var dictInt = JsonConvert.DeserializeObject<Dictionary<int, T>>(json);
                if (dictInt != null)
                {
                    return new List<T>(dictInt.Values);
                }
            }
            catch
            {
                GoveKitsCore.Log("JsonConfigParser", "Failed to parse as Dictionary<int, T>, trying other formats.", logType: LogType.Warning);
            }

            try
            {
                var dictString = JsonConvert.DeserializeObject<Dictionary<string, T>>(json);
                if (dictString != null)
                {
                    return new List<T>(dictString.Values);
                }
            }
            catch
            {
                GoveKitsCore.Log("JsonConfigParser", "Failed to parse as Dictionary<string, T>, trying other formats.", logType: LogType.Warning);
            }

            T one = JsonConvert.DeserializeObject<T>(json);
            return one == null ? new List<T>() : new List<T> { one };
        }
    }
}
