
namespace GoveKits.Runtime.Util
{
    /// <summary>
    /// 释放操作包装类
    /// </summary>
    public class DisposeAction : System.IDisposable
    {
        private readonly System.Action _disposeAction;

        public DisposeAction(System.Action disposeAction)
        {
            _disposeAction = disposeAction;
        }

        public void Dispose()
        {
            _disposeAction?.Invoke();
        }
    }
}