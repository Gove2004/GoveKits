
using System;
using GoveKits.Runtime.Core;
using Newtonsoft.Json;
using UnityEngine.Networking;

namespace GoveKits.Runtime.Network
{
    public readonly struct HttpResponse
    {
        public readonly bool IsSuccess;
        public readonly long StatusCode;
        public readonly string ErrorMsg;
        public readonly string Text;

        private HttpResponse(bool success, long code, string error, string text)
        {
            IsSuccess = success;
            StatusCode = code;
            ErrorMsg = error;
            Text = text;
        }

        public T GetJson<T>()
        {
            try 
            { 
                return JsonConvert.DeserializeObject<T>(Text); 
            }
            catch (Exception ex)
            {
                LogCore.Error(nameof(HttpResponse), $"JSON Parsing Error: {ex.Message}");
                return default;
            }
        }

        internal static HttpResponse Success(UnityWebRequest uwr) => new HttpResponse(true, uwr.responseCode, null, uwr.downloadHandler?.text);
        internal static HttpResponse Error(UnityWebRequest uwr) => new HttpResponse(false, uwr.responseCode, uwr.error, uwr.downloadHandler?.text);
        internal static HttpResponse Cached(string text) => new HttpResponse(true, 200, null, text);
        internal static HttpResponse FailException(Exception ex) => new HttpResponse(false, 0, ex.Message, null);
    }
}