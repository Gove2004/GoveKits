using System;
using System.Net;

namespace GoveKits.Runtime.Network
{
    public readonly struct FtpResponse
    {
        public readonly bool IsSuccess;
        public readonly FtpStatusCode StatusCode;
        public readonly string StatusDescription;
        public readonly string ErrorMsg;
        public readonly byte[] Data;
        public readonly string Text;

        private FtpResponse(bool success, FtpStatusCode code, string desc, string error, byte[] data, string text)
        {
            IsSuccess = success;
            StatusCode = code;
            StatusDescription = desc;
            ErrorMsg = error;
            Data = data;
            Text = text;
        }

        internal static FtpResponse Success(FtpWebResponse res, byte[] data, string text) 
            => new FtpResponse(true, res.StatusCode, res.StatusDescription, null, data, text);

        internal static FtpResponse Error(FtpWebResponse res, string errorMsg) 
            => new FtpResponse(false, res.StatusCode, res.StatusDescription, errorMsg, null, null);

        internal static FtpResponse FailException(Exception ex) 
            => new FtpResponse(false, FtpStatusCode.Undefined, null, ex.Message, null, null);
    }
}