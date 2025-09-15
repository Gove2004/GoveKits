// using System;
// using System.Collections;
// using System.Collections.Generic;
// using System.IO;
// using System.Text;
// using UnityEngine;
// using UnityEngine.Networking;

// namespace GoveKits.Manager
// {
//     /// <summary>
//     /// 网络请求管理器
//     /// </summary>
//     public class NetworkManager : MonoSingleton<NetworkManager>
//     {
//         #region 配置参数
//         [Header("网络设置")]
//         [Tooltip("基础API地址")]
//         public string baseApiUrl = "https://api.example.com/";
        
//         [Tooltip("默认超时时间(秒)")]
//         public float defaultTimeout = 10f;
        
//         [Tooltip("最大并发请求数")]
//         public int maxConcurrentRequests = 5;
        
//         [Header("重试策略")]
//         [Tooltip("最大重试次数")]
//         public int maxRetryCount = 3;
        
//         [Tooltip("重试间隔(秒)")]
//         public float retryInterval = 1f;
        
//         [Header("缓存设置")]
//         [Tooltip("启用响应缓存")]
//         public bool enableCaching = true;
        
//         [Tooltip("缓存时间(秒)")]
//         public float cacheDuration = 300f;
//         #endregion

//         #region 内部状态
//         private Dictionary<string, CacheEntry> responseCache = new Dictionary<string, CacheEntry>();
//         private Queue<NetworkRequest> requestQueue = new Queue<NetworkRequest>();
//         private int activeRequests = 0;
//         #endregion

//         #region 数据结构
//         /// <summary>
//         /// 网络请求配置
//         /// </summary>
//         public class RequestConfig
//         {
//             public string endpoint;
//             public HttpMethod method = HttpMethod.GET;
//             public object body;
//             public Dictionary<string, string> headers;
//             public float timeout;
//             public int retryCount;
//             public bool useCache;
//         }

//         /// <summary>
//         /// 网络响应
//         /// </summary>
//         public class NetworkResponse
//         {
//             public bool success;
//             public long statusCode;
//             public string error;
//             public string text;
//             public byte[] bytes;
//             public Texture2D texture;
//             public AssetBundle assetBundle;
//             public Dictionary<string, string> headers;
//         }

//         /// <summary>
//         /// HTTP方法枚举
//         /// </summary>
//         public enum HttpMethod
//         {
//             GET,
//             POST,
//             PUT,
//             DELETE,
//             PATCH
//         }

//         /// <summary>
//         /// 缓存条目
//         /// </summary>
//         private class CacheEntry
//         {
//             public string response;
//             public DateTime expiration;
//         }

//         /// <summary>
//         /// 请求队列项
//         /// </summary>
//         private class NetworkRequest
//         {
//             public RequestConfig config;
//             public Action<NetworkResponse> callback;
//         }
//         #endregion

//         #region 公共API
//         /// <summary>
//         /// 发送GET请求
//         /// </summary>
//         public void Get(string endpoint, Action<NetworkResponse> callback, Dictionary<string, string> headers = null)
//         {
//             SendRequest(new RequestConfig
//             {
//                 endpoint = endpoint,
//                 method = HttpMethod.GET,
//                 headers = headers
//             }, callback);
//         }

//         /// <summary>
//         /// 发送POST请求
//         /// </summary>
//         public void Post(string endpoint, object body, Action<NetworkResponse> callback, Dictionary<string, string> headers = null)
//         {
//             SendRequest(new RequestConfig
//             {
//                 endpoint = endpoint,
//                 method = HttpMethod.POST,
//                 body = body,
//                 headers = headers
//             }, callback);
//         }

//         /// <summary>
//         /// 下载文件
//         /// </summary>
//         public void DownloadFile(string url, string savePath, Action<bool, string> callback)
//         {
//             StartCoroutine(DownloadFileCoroutine(url, savePath, callback));
//         }

//         /// <summary>
//         /// 上传文件
//         /// </summary>
//         public void UploadFile(string endpoint, string filePath, string fieldName, Action<NetworkResponse> callback, Dictionary<string, string> formData = null)
//         {
//             StartCoroutine(UploadFileCoroutine(endpoint, filePath, fieldName, callback, formData));
//         }

//         /// <summary>
//         /// 获取JSON数据并解析为对象
//         /// </summary>
//         public void GetJson<T>(string endpoint, Action<bool, T> callback, Dictionary<string, string> headers = null)
//         {
//             Get(endpoint, response =>
//             {
//                 if (response.success)
//                 {
//                     try
//                     {
//                         T data = JsonUtility.FromJson<T>(response.text);
//                         callback(true, data);
//                     }
//                     catch (Exception ex)
//                     {
//                         Debug.LogError($"[NetManager] JSON解析失败: {ex.Message}");
//                         callback(false, default);
//                     }
//                 }
//                 else
//                 {
//                     callback(false, default);
//                 }
//             }, headers);
//         }

//         /// <summary>
//         /// 发送JSON数据
//         /// </summary>
//         public void PostJson<T>(string endpoint, T data, Action<NetworkResponse> callback, Dictionary<string, string> headers = null)
//         {
//             string json = JsonUtility.ToJson(data);
//             var requestHeaders = headers ?? new Dictionary<string, string>();
//             requestHeaders["Content-Type"] = "application/json";
            
//             Post(endpoint, json, callback, requestHeaders);
//         }
//         #endregion

//         #region 核心请求处理
//         /// <summary>
//         /// 发送网络请求
//         /// </summary>
//         private void SendRequest(RequestConfig config, Action<NetworkResponse> callback)
//         {
//             // 检查缓存
//             if (config.useCache && enableCaching && TryGetCachedResponse(config, out string cachedResponse))
//             {
//                 callback(new NetworkResponse
//                 {
//                     success = true,
//                     statusCode = 200,
//                     text = cachedResponse
//                 });
//                 return;
//             }

//             var request = new NetworkRequest
//             {
//                 config = config,
//                 callback = callback
//             };

//             requestQueue.Enqueue(request);
//             ProcessQueue();
//         }

//         /// <summary>
//         /// 处理请求队列
//         /// </summary>
//         private void ProcessQueue()
//         {
//             while (activeRequests < maxConcurrentRequests && requestQueue.Count > 0)
//             {
//                 NetworkRequest request = requestQueue.Dequeue();
//                 activeRequests++;
//                 StartCoroutine(SendRequestCoroutine(request));
//             }
//         }

//         /// <summary>
//         /// 发送请求协程
//         /// </summary>
//         private IEnumerator SendRequestCoroutine(NetworkRequest request)
//         {
//             RequestConfig config = request.config;
//             string url = CombineUrl(baseApiUrl, config.endpoint);
//             HttpMethod method = config.method;
            
//             UnityWebRequest webRequest = CreateWebRequest(url, method, config.body, config.headers);
//             webRequest.timeout = config.timeout > 0 ? config.timeout : defaultTimeout;
            
//             // 设置重试次数
//             int retryCount = config.retryCount > 0 ? config.retryCount : maxRetryCount;
//             int attempts = 0;
//             bool success = false;
            
//             while (attempts <= retryCount && !success)
//             {
//                 if (attempts > 0)
//                 {
//                     Debug.Log($"[NetManager] 请求重试 ({attempts}/{retryCount}): {url}");
//                     yield return new WaitForSeconds(retryInterval);
//                 }
                
//                 yield return webRequest.SendWebRequest();
                
//                 // 检查请求结果
//                 if (IsRequestSuccess(webRequest))
//                 {
//                     success = true;
//                 }
//                 else
//                 {
//                     attempts++;
//                     if (attempts > retryCount)
//                     {
//                         break;
//                     }
                    
//                     // 重置请求以重试
//                     webRequest.Dispose();
//                     webRequest = CreateWebRequest(url, method, config.body, config.headers);
//                     webRequest.timeout = defaultTimeout;
//                 }
//             }
            
//             // 处理响应
//             NetworkResponse response = CreateResponse(webRequest);
            
//             // 缓存成功响应
//             if (success && config.useCache && enableCaching)
//             {
//                 CacheResponse(config, response.text);
//             }
            
//             // 调用回调
//             request.callback?.Invoke(response);
            
//             // 清理资源
//             webRequest.Dispose();
//             activeRequests--;
//             ProcessQueue();
//         }
//         #endregion

//         #region 辅助方法
//         /// <summary>
//         /// 创建Web请求
//         /// </summary>
//         private UnityWebRequest CreateWebRequest(string url, HttpMethod method, object body, Dictionary<string, string> headers)
//         {
//             UnityWebRequest webRequest;
            
//             switch (method)
//             {
//                 case HttpMethod.POST:
//                     webRequest = UnityWebRequest.PostWwwForm(url, "");
//                     if (body != null)
//                     {
//                         if (body is string json)
//                         {
//                             byte[] jsonBytes = Encoding.UTF8.GetBytes(json);
//                             webRequest.uploadHandler = new UploadHandlerRaw(jsonBytes);
//                         }
//                         else if (body is byte[] bytes)
//                         {
//                             webRequest.uploadHandler = new UploadHandlerRaw(bytes);
//                         }
//                         else if (body is WWWForm form)
//                         {
//                             webRequest = UnityWebRequest.Post(url, form);
//                         }
//                     }
//                     break;
                    
//                 case HttpMethod.PUT:
//                     webRequest = UnityWebRequest.Put(url, body is string str ? str : "");
//                     break;
                    
//                 case HttpMethod.DELETE:
//                     webRequest = UnityWebRequest.Delete(url);
//                     break;
                    
//                 case HttpMethod.PATCH:
//                     webRequest = new UnityWebRequest(url, "PATCH");
//                     if (body != null)
//                     {
//                         byte[] bodyBytes = Encoding.UTF8.GetBytes(body.ToString());
//                         webRequest.uploadHandler = new UploadHandlerRaw(bodyBytes);
//                     }
//                     break;
                    
//                 default: // GET
//                     webRequest = UnityWebRequest.Get(url);
//                     break;
//             }
            
//             // 设置下载处理器
//             webRequest.downloadHandler = new DownloadHandlerBuffer();
            
//             // 添加请求头
//             if (headers != null)
//             {
//                 foreach (var header in headers)
//                 {
//                     webRequest.SetRequestHeader(header.Key, header.Value);
//                 }
//             }
            
//             return webRequest;
//         }

//         /// <summary>
//         /// 创建响应对象
//         /// </summary>
//         private NetworkResponse CreateResponse(UnityWebRequest webRequest)
//         {
//             var response = new NetworkResponse
//             {
//                 statusCode = webRequest.responseCode,
//                 headers = ParseResponseHeaders(webRequest),
//                 bytes = webRequest.downloadHandler?.data,
//                 text = webRequest.downloadHandler?.text
//             };
            
//             if (IsRequestSuccess(webRequest))
//             {
//                 response.success = true;
//             }
//             else
//             {
//                 response.success = false;
//                 response.error = webRequest.error;
                
//                 if (string.IsNullOrEmpty(response.error))
//                 {
//                     response.error = $"[NetManager] HTTP错误: {webRequest.responseCode}";
//                 }
//             }
            
//             return response;
//         }

//         /// <summary>
//         /// 检查请求是否成功
//         /// </summary>
//         private bool IsRequestSuccess(UnityWebRequest webRequest)
//         {
//             if (webRequest.result == UnityWebRequest.Result.ConnectionError || 
//                 webRequest.result == UnityWebRequest.Result.ProtocolError)
//             {
//                 return false;
//             }
            
//             // 检查HTTP状态码
//             return webRequest.responseCode >= 200 && webRequest.responseCode < 300;
//         }

//         /// <summary>
//         /// 解析响应头
//         /// </summary>
//         private Dictionary<string, string> ParseResponseHeaders(UnityWebRequest webRequest)
//         {
//             var headers = new Dictionary<string, string>();
            
//             if (webRequest.GetResponseHeaders() != null)
//             {
//                 foreach (var header in webRequest.GetResponseHeaders())
//                 {
//                     headers[header.Key] = header.Value;
//                 }
//             }
            
//             return headers;
//         }

//         /// <summary>
//         /// 组合URL
//         /// </summary>
//         private string CombineUrl(string baseUrl, string endpoint)
//         {
//             if (string.IsNullOrEmpty(endpoint)) return baseUrl;
            
//             if (baseUrl.EndsWith("/") && endpoint.StartsWith("/"))
//             {
//                 return baseUrl + endpoint.Substring(1);
//             }
//             else if (!baseUrl.EndsWith("/") && !endpoint.StartsWith("/"))
//             {
//                 return baseUrl + "/" + endpoint;
//             }
//             else
//             {
//                 return baseUrl + endpoint;
//             }
//         }
//         #endregion

//         #region 文件传输
//         /// <summary>
//         /// 下载文件协程
//         /// </summary>
//         private IEnumerator DownloadFileCoroutine(string url, string savePath, Action<bool, string> callback)
//         {
//             using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
//             {
//                 webRequest.timeout = defaultTimeout;
//                 yield return webRequest.SendWebRequest();
                
//                 if (IsRequestSuccess(webRequest))
//                 {
//                     try
//                     {
//                         string directory = Path.GetDirectoryName(savePath);
//                         if (!Directory.Exists(directory))
//                         {
//                             Directory.CreateDirectory(directory);
//                         }
                        
//                         File.WriteAllBytes(savePath, webRequest.downloadHandler.data);
//                         callback(true, savePath);
//                     }
//                     catch (Exception ex)
//                     {
//                         Debug.LogError($"[NetManager] 文件保存失败: {ex.Message}");
//                         callback(false, $"[NetManager] 文件保存失败: {ex.Message}");
//                     }
//                 }
//                 else
//                 {
//                     callback(false, $"[NetManager] 下载失败: {webRequest.error}");
//                 }
//             }
//         }

//         /// <summary>
//         /// 上传文件协程
//         /// </summary>
//         private IEnumerator UploadFileCoroutine(string endpoint, string filePath, string fieldName, Action<NetworkResponse> callback, Dictionary<string, string> formData = null)
//         {
//             if (!File.Exists(filePath))
//             {
//                 callback(new NetworkResponse
//                 {
//                     success = false,
//                     error = $"[NetManager] 文件不存在: {filePath}"
//                 });
//                 yield break;
//             }
            
//             string url = CombineUrl(baseApiUrl, endpoint);
//             WWWForm form = new WWWForm();
            
//             // 添加文件
//             byte[] fileData = File.ReadAllBytes(filePath);
//             string fileName = Path.GetFileName(filePath);
//             form.AddBinaryData(fieldName, fileData, fileName);
            
//             // 添加其他表单数据
//             if (formData != null)
//             {
//                 foreach (var entry in formData)
//                 {
//                     form.AddField(entry.Key, entry.Value);
//                 }
//             }
            
//             using (UnityWebRequest webRequest = UnityWebRequest.Post(url, form))
//             {
//                 webRequest.timeout = defaultTimeout;
//                 yield return webRequest.SendWebRequest();
                
//                 callback(CreateResponse(webRequest));
//             }
//         }
//         #endregion

//         #region 缓存管理
//         /// <summary>
//         /// 缓存响应
//         /// </summary>
//         private void CacheResponse(RequestConfig config, string response)
//         {
//             if (string.IsNullOrEmpty(response)) return;
            
//             string cacheKey = GenerateCacheKey(config);
//             responseCache[cacheKey] = new CacheEntry
//             {
//                 response = response,
//                 expiration = DateTime.Now.AddSeconds(cacheDuration)
//             };
//         }

//         /// <summary>
//         /// 尝试获取缓存响应
//         /// </summary>
//         private bool TryGetCachedResponse(RequestConfig config, out string response)
//         {
//             response = null;
//             string cacheKey = GenerateCacheKey(config);
            
//             if (responseCache.TryGetValue(cacheKey, out CacheEntry entry))
//             {
//                 if (DateTime.Now < entry.expiration)
//                 {
//                     response = entry.response;
//                     return true;
//                 }
//                 else
//                 {
//                     // 移除过期缓存
//                     responseCache.Remove(cacheKey);
//                 }
//             }
            
//             return false;
//         }

//         /// <summary>
//         /// 生成缓存键
//         /// </summary>
//         private string GenerateCacheKey(RequestConfig config)
//         {
//             // 使用URL和方法作为缓存键
//             return $"{config.method}:{CombineUrl(baseApiUrl, config.endpoint)}";
//         }

//         /// <summary>
//         /// 清除所有缓存
//         /// </summary>
//         public void ClearCache()
//         {
//             responseCache.Clear();
//         }
//         #endregion

//         #region WebSocket支持
//         /// <summary>
//         /// WebSocket连接
//         /// </summary>
//         public class WebSocketConnection
//         {
//             private UnityWebRequest webRequest;
//             private System.Threading.CancellationTokenSource cancellationToken;
            
//             public event Action OnConnected;
//             public event Action<string> OnMessageReceived;
//             public event Action<string> OnError;
//             public event Action OnClosed;
            
//             public bool IsConnected { get; private set; }
            
//             public WebSocketConnection(string url)
//             {
//                 webRequest = UnityWebRequest.Get(url);
//                 webRequest.SetRequestHeader("Upgrade", "websocket");
//                 webRequest.SetRequestHeader("Connection", "Upgrade");
//                 webRequest.SetRequestHeader("Sec-WebSocket-Version", "13");
//                 webRequest.SetRequestHeader("Sec-WebSocket-Key", GenerateWebSocketKey());
//             }
            
//             public void Connect()
//             {
//                 if (IsConnected) return;
                
//                 cancellationToken = new System.Threading.CancellationTokenSource();
//                 ConnectAsync(cancellationToken.Token);
//             }
            
//             private async void ConnectAsync(System.Threading.CancellationToken token)
//             {
//                 try
//                 {
//                     webRequest.SendWebRequest();
                    
//                     while (!webRequest.isDone)
//                     {
//                         if (token.IsCancellationRequested)
//                         {
//                             webRequest.Abort();
//                             return;
//                         }
//                         await System.Threading.Tasks.Task.Delay(100);
//                     }
                    
//                     if (webRequest.result == UnityWebRequest.Result.Success)
//                     {
//                         IsConnected = true;
//                         OnConnected?.Invoke();
//                         StartListening();
//                     }
//                     else
//                     {
//                         OnError?.Invoke(webRequest.error);
//                     }
//                 }
//                 catch (Exception ex)
//                 {
//                     OnError?.Invoke(ex.Message);
//                 }
//             }
            
//             private void StartListening()
//             {
//                 // 实际项目中需要实现WebSocket协议解析
//                 // 这里仅作为示例
//                 Debug.LogWarning("[NetManager] WebSocket监听未实现，需要完整WebSocket客户端实现");
//             }
            
//             public void Send(string message)
//             {
//                 if (!IsConnected) return;
                
//                 // 实际项目中需要实现WebSocket数据帧发送
//                 Debug.LogWarning("[NetManager] WebSocket发送未实现");
//             }
            
//             public void Close()
//             {
//                 if (!IsConnected) return;
                
//                 cancellationToken?.Cancel();
//                 IsConnected = false;
//                 OnClosed?.Invoke();
//                 webRequest.Dispose();
//             }
            
//             private string GenerateWebSocketKey()
//             {
//                 byte[] key = new byte[16];
//                 new System.Random().NextBytes(key);
//                 return Convert.ToBase64String(key);
//             }
//         }
        
//         /// <summary>
//         /// 创建WebSocket连接
//         /// </summary>
//         public WebSocketConnection CreateWebSocket(string endpoint)
//         {
//             string url = CombineUrl(baseApiUrl, endpoint).Replace("https://", "wss://").Replace("http://", "ws://");
//             return new WebSocketConnection(url);
//         }
//         #endregion
//     }
// }