using UnityEngine;

namespace GoveKits.Runtime.Core
{
    /// <summary>
    /// 可被池系统管理的对象接口
    /// </summary>
    public interface IPoolable
    {
        void OnRecycle();
    }
}