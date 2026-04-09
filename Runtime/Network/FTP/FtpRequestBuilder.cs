


using System.Net;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace GoveKits.Runtime.Network
{
        public class FtpRequestBuilder
    {
        private readonly FTPCore _core;

        internal string Url { get; }
        internal NetworkCredential Credentials { get; private set; }
        internal float Timeout { get; private set; }
        internal int RetryCount { get; private set; }
        internal bool UsePassiveMode { get; private set; } = true;

        public FtpRequestBuilder(FTPCore core, string url, NetworkCredential credential)
        {
            _core = core;
            Url = url;
        }

        #region Fluent Setters
        public FtpRequestBuilder SetCredentials(string username, string password)
        {
            Credentials = new NetworkCredential(username, password);
            return this;
        }
        public FtpRequestBuilder SetTimeout(float seconds) { Timeout = seconds; return this; }
        public FtpRequestBuilder SetRetry(int count) { RetryCount = count; return this; }
        public FtpRequestBuilder UseActiveMode() { UsePassiveMode = false; return this; }
        #endregion

        #region Terminal Methods
        public UniTask<FtpResponse> DownloadAsync(CancellationToken ct = default)
            => FtpEngine.ExecuteAsync(_core, this, WebRequestMethods.Ftp.DownloadFile, null, null, ct);

        public UniTask<FtpResponse> DownloadToFileAsync(string savePath, CancellationToken ct = default)
            => FtpEngine.ExecuteAsync(_core, this, WebRequestMethods.Ftp.DownloadFile, null, savePath, ct);

        public UniTask<FtpResponse> UploadAsync(byte[] fileData, CancellationToken ct = default)
            => FtpEngine.ExecuteAsync(_core, this, WebRequestMethods.Ftp.UploadFile, fileData, null, ct);

        public UniTask<FtpResponse> UploadTextAsync(string text, CancellationToken ct = default)
            => UploadAsync(Encoding.UTF8.GetBytes(text), ct);

        public UniTask<FtpResponse> DeleteFileAsync(CancellationToken ct = default)
            => FtpEngine.ExecuteAsync(_core, this, WebRequestMethods.Ftp.DeleteFile, null, null, ct);

        public UniTask<FtpResponse> ListDirectoryDetailsAsync(CancellationToken ct = default)
            => FtpEngine.ExecuteAsync(_core, this, WebRequestMethods.Ftp.ListDirectoryDetails, null, null, ct);

        public UniTask<FtpResponse> MakeDirectoryAsync(CancellationToken ct = default)
            => FtpEngine.ExecuteAsync(_core, this, WebRequestMethods.Ftp.MakeDirectory, null, null, ct);
        #endregion
    }
}