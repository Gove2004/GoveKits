namespace GoveKits.Runtime.Network
{
    /// <summary>
    /// HTTP 请求方法枚举，对应标准 HTTP 动词。
    /// </summary>
    public enum HttpMethod
    {
        /// <summary>GET 请求：从服务器检索资源。</summary>
        GET,
        /// <summary>POST 请求：向服务器提交数据以创建或更新资源。</summary>
        POST,
        /// <summary>PUT 请求：用请求体中的数据替换目标资源。</summary>
        PUT,
        /// <summary>DELETE 请求：从服务器删除指定资源。</summary>
        DELETE
    }
}
