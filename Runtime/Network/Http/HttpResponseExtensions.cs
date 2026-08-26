using System;
using GoveKits.Runtime.Core;
using Newtonsoft.Json;

namespace GoveKits.Runtime.Network
{
    /// <summary>
    /// HttpResponse 的扩展方法集合，主要为 JSON 反序列化提供便捷支持。
    /// </summary>
    public static class HttpResponseExtensions
    {
        /// <summary>
        /// 将 HttpResponse 中的 Text 字段通过 JsonConvert 反序列化为指定类型。
        /// 反序列化过程中发生异常时，通过 LogCore 记录错误并返回 default(T)。
        /// </summary>
        /// <typeparam name="T">目标反序列化类型。</typeparam>
        /// <param name="response">HTTP 响应实例。</param>
        /// <returns>反序列化后的对象，失败时返回 default(T)。</returns>
        public static T GetJson<T>(this HttpResponse response)
        {
            try
            {
                return JsonConvert.DeserializeObject<T>(response.Text);
            }
            catch (Exception ex)
            {
                LogCore.Error(nameof(HttpResponse), $"JSON 解析失败: {ex.Message}");
                return default;
            }
        }
    }
}
