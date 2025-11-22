

namespace GoveKits.Pool
{
    /// <summary>
    /// 池化对象接口（用于重置对象状态）
    /// </summary>
    public interface IPoolable
    {
        void OnGetFromPool();
        void OnReturnToPool();
    }
}