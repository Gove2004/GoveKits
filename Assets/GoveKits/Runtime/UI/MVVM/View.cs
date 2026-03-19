using UnityEngine;

namespace GoveKits.Runtime.UI.MVVM
{
	/// <summary>
	/// MVVM 中的 View 基类。
	/// 负责 UI 展示与交互转发，不承载业务逻辑。
	/// </summary>
	public abstract class View : MonoBehaviour
	{
	}

	/// <summary>
	/// 强类型 View 基类。
	/// 提供 ViewModel 生命周期绑定、解绑与属性通知监听。
	/// </summary>
	public abstract class View<TViewModel> : View where TViewModel : ViewModel
	{
		protected TViewModel ViewModel { get; private set; }

		/// <summary>
		/// 设置并绑定 ViewModel。
		/// </summary>
		public void SetViewModel(TViewModel viewModel)
		{
			if (ReferenceEquals(ViewModel, viewModel))
			{
				return;
			}

			UnbindViewModel();
			ViewModel = viewModel;
			BindViewModel();
		}

		/// <summary>
		/// 绑定 ViewModel 事件并触发首帧刷新。
		/// </summary>
		protected virtual void BindViewModel()
		{
			if (ViewModel == null)
			{
				return;
			}

			ViewModel.PropertyChanged += OnViewModelPropertyChanged;
			ViewModel.OnBind();
			RefreshAll();
		}

		/// <summary>
		/// 解绑 ViewModel 事件。
		/// </summary>
		protected virtual void UnbindViewModel()
		{
			if (ViewModel == null)
			{
				return;
			}

			ViewModel.PropertyChanged -= OnViewModelPropertyChanged;
			ViewModel.OnUnbind();
		}

		/// <summary>
		/// 子类根据属性名刷新局部 UI。
		/// </summary>
		protected abstract void OnViewModelPropertyChanged(string propertyName);

		/// <summary>
		/// 子类实现完整 UI 刷新（首次绑定常用）。
		/// </summary>
		protected abstract void RefreshAll();

		protected virtual void OnDestroy()
		{
			UnbindViewModel();
		}
	}
}
