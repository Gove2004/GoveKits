namespace GoveKits.Runtime.Core
{
    /// <summary>
    /// 释放操作包装类
    /// </summary>
    /// <remarks>
    /// 实现 IDisposable 接口，将 Action 委托包装为可释放对象
    /// 用于订阅返回的取消订阅句柄
    /// 支持 using 语句自动取消订阅
    /// </remarks>
    public class DisposeAction : System.IDisposable
    {
        /// <summary>
        /// 释放时执行的回调方法
        /// </summary>
        private readonly System.Action _disposeAction;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="disposeAction">释放时执行的回调</param>
        public DisposeAction(System.Action disposeAction)
        {
            _disposeAction = disposeAction;
        }

        /// <summary>
        /// 释放方法
        /// </summary>
        /// <remarks>
        /// 调用内部存储的回调，执行取消订阅逻辑
        /// 支持 using 语句自动调用
        /// 使用 ?. 操作符避免空引用异常
        /// </remarks>
        public void Dispose()
        {
            _disposeAction?.Invoke();
        }
    }
}