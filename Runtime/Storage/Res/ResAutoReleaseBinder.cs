using System;
using UnityEngine;

namespace GoveKits.Runtime.Storage
{
    /// <summary>
    /// 挂载在实例化的 GameObject 上，跟随物体销毁自动释放资源句柄
    /// </summary>
    [DisallowMultipleComponent]
    public class ResAutoReleaseBinder : MonoBehaviour
    {
        private Action _onDispose;

        public void Bind(Action onDispose)
        {
            // 支持多次绑定（例如一个特效预制体上绑了多个音效句柄）
            _onDispose += onDispose;
        }

        private void OnDestroy()
        {
            _onDispose?.Invoke();
            _onDispose = null;
        }
    }
}