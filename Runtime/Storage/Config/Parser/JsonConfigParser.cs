using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace GoveKits.Runtime.Storage
{
    /// <summary>
    /// JSON 配置解析器。支持 List&lt;T&gt;、Dictionary&lt;int,T&gt;、Dictionary&lt;string,T&gt;
    /// 以及单对象格式的自动降级解析。
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
                return new List<T>();

            try
            {
                var list = JsonConvert.DeserializeObject<List<T>>(json);
                if (list != null) return list;
            }
            catch
            {
                // 静默降级，不记录日志（解析器不应依赖外部 LogCore 实例）
            }

            try
            {
                var dictInt = JsonConvert.DeserializeObject<Dictionary<int, T>>(json);
                if (dictInt != null) return new List<T>(dictInt.Values);
            }
            catch
            {
                // 静默降级
            }

            try
            {
                var dictString = JsonConvert.DeserializeObject<Dictionary<string, T>>(json);
                if (dictString != null) return new List<T>(dictString.Values);
            }
            catch
            {
                // 静默降级
            }

            T one = JsonConvert.DeserializeObject<T>(json);
            return one == null ? new List<T>() : new List<T> { one };
        }
    }
}
