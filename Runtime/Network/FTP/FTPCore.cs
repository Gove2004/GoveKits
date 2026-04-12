
using System.Net;
using GoveKits.Runtime.Core;

namespace GoveKits.Runtime.Network
{
    public static class FTPCore
    {
        public static FtpRequestBuilder Request(
            string url, 
            NetworkCredential credential
        )
             => new FtpRequestBuilder(url, credential);
    }
}