using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using UnityEngine;
using GoveKits.Runtime.Core;

namespace GoveKits.Runtime.Storage
{
    /// <summary>
    /// Json 配置解析器。
    /// </summary>
    public sealed class JsonConfigParser : IConfigParser
    {
        private static readonly string[] ParserExtensions = { "json" };

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
                LogCore.Warning(nameof(JsonConfigParser), "解析为 List<T> 失败，尝试其他格式");
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
                LogCore.Warning(nameof(JsonConfigParser), "解析为 Dictionary<int, T> 失败，尝试其他格式");
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
                LogCore.Warning(nameof(JsonConfigParser), "解析为 Dictionary<string, T> 失败，尝试其他格式");
            }

            T one = JsonConvert.DeserializeObject<T>(json);
            return one == null ? new List<T>() : new List<T> { one };
        }
    }
}
