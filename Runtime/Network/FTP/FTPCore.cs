
using System.Net;
using GoveKits.Runtime.Core;

namespace GoveKits.Runtime.Network
{
    public class FTPCore : ICore
    {
        public FtpRequestBuilder Request(
            string url, 
            NetworkCredential credential
        )
             => new FtpRequestBuilder(this, url, credential);

        public void OnShutdown()
        {
            // FTP暂无需要持久清理的内部资源
        }
    }
}