using System;

namespace GoveKits.Runtime.UI.MVVM
{
	/// <summary>
	/// MVVM 中的 Model 基类。
	/// 负责数据与领域逻辑，不关心 UI 渲染细节。
	/// </summary>
	public abstract class Model : IDisposable
	{
		private bool _disposed;

		/// <summary>
		/// 释放 Model 持有的资源（如订阅、句柄、临时缓存）。
		/// </summary>
		public void Dispose()
		{
			if (_disposed)
			{
				return;
			}

			_disposed = true;
			OnDispose();
		}

		/// <summary>
		/// 子类资源清理入口。
		/// </summary>
		protected virtual void OnDispose()
		{
		}
	}
}
