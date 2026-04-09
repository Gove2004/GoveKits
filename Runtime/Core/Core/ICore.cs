


namespace GoveKits.Runtime.Core
{
    /// <summary>
    /// 每个模块核心都继承自这里
    /// </summary>
    public interface ICore
    {
        /// <summary>
        /// 核心销毁时清理资源
        /// </summary>
        void OnShutdown();
    }
}