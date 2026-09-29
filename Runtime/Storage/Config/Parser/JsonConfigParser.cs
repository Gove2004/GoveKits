using System;
using System.Collections.Generic;
using System.Text;
using GoveKits.Runtime.Util;
using Newtonsoft.Json;

namespace GoveKits.Runtime.Storage
{
    /// <summary>
    /// JSON 配置解析器。支持 List&lt;T&gt;、Dictionary&lt;int,T&gt;、Dictionary&lt;string,T&gt;
    /// 以及单对象格式的自动降级解析。所有格式均失败时抛出包含各次失败原因的聚合异常。
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

            // 依次尝试各格式并记录失败原因，全部失败时抛出聚合异常，便于定位配置文件问题
            var errors = new List<Exception>();

            try
            {
                var list = JsonConvert.DeserializeObject<List<T>>(json);
                if (list != null) return list;
            }
            catch (Exception e)
            {
                errors.Add(new Exception($"按 List<{typeof(T).Name}> 解析失败: {e.Message}"));
            }

            try
            {
                var dictInt = JsonConvert.DeserializeObject<Dictionary<int, T>>(json);
                if (dictInt != null) return new List<T>(dictInt.Values);
            }
            catch (Exception e)
            {
                errors.Add(new Exception($"按 Dictionary<int, {typeof(T).Name}> 解析失败: {e.Message}"));
            }

            try
            {
                var dictString = JsonConvert.DeserializeObject<Dictionary<string, T>>(json);
                if (dictString != null) return new List<T>(dictString.Values);
            }
            catch (Exception e)
            {
                errors.Add(new Exception($"按 Dictionary<string, {typeof(T).Name}> 解析失败: {e.Message}"));
            }

            try
            {
                T one = JsonConvert.DeserializeObject<T>(json);
                return one == null ? new List<T>() : new List<T> { one };
            }
            catch (Exception e)
            {
                errors.Add(new Exception($"按单对象 {typeof(T).Name} 解析失败: {e.Message}"));
            }

            throw new AggregateException($"JSON 配置解析失败（类型: {typeof(T).Name}）", errors);
        }
    }
}
