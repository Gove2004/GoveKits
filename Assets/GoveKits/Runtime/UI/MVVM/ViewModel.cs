using System;
using System.Collections.Generic;

namespace GoveKits.Runtime.UI.MVVM
{
	/// <summary>
	/// 可执行命令接口。
	/// </summary>
	public interface ICommand
	{
		bool CanExecute();

		void Execute();

		event Action CanExecuteChanged;
	}

	/// <summary>
	/// 无参命令实现。
	/// </summary>
	public sealed class RelayCommand : ICommand
	{
		private readonly Action _execute;
		private readonly Func<bool> _canExecute;

		public event Action CanExecuteChanged;

		public RelayCommand(Action execute, Func<bool> canExecute = null)
		{
			_execute = execute ?? throw new ArgumentNullException(nameof(execute));
			_canExecute = canExecute;
		}

		public bool CanExecute()
		{
			return _canExecute == null || _canExecute();
		}

		public void Execute()
		{
			if (!CanExecute())
			{
				return;
			}

			_execute();
		}

		public void RaiseCanExecuteChanged()
		{
			CanExecuteChanged?.Invoke();
		}
	}

	/// <summary>
	/// 基础 ViewModel。
	/// </summary>
	public abstract class ViewModel : IDisposable
	{
		private bool _disposed;

		/// <summary>
		/// 属性变更通知。参数为属性名。
		/// </summary>
		public event Action<string> PropertyChanged;

		/// <summary>
		/// View 绑定时调用。
		/// </summary>
		public virtual void OnBind()
		{
		}

		/// <summary>
		/// View 解绑时调用。
		/// </summary>
		public virtual void OnUnbind()
		{
		}

		/// <summary>
		/// 主动通知属性变更。
		/// </summary>
		protected void RaisePropertyChanged(string propertyName)
		{
			if (string.IsNullOrEmpty(propertyName))
			{
				return;
			}

			PropertyChanged?.Invoke(propertyName);
		}

		/// <summary>
		/// 设置字段并触发属性通知。
		/// </summary>
		protected bool SetProperty<T>(ref T field, T value, string propertyName)
		{
			if (EqualityComparer<T>.Default.Equals(field, value))
			{
				return false;
			}

			field = value;
			RaisePropertyChanged(propertyName);
			return true;
		}

		public void Dispose()
		{
			if (_disposed)
			{
				return;
			}

			_disposed = true;
			OnDispose();
		}

		protected virtual void OnDispose()
		{
		}
	}

	/// <summary>
	/// 带 Model 的 ViewModel。
	/// </summary>
	public abstract class ViewModel<TModel> : ViewModel where TModel : Model
	{
		protected TModel Model { get; private set; }

		protected ViewModel(TModel model)
		{
			Model = model;
		}

		protected override void OnDispose()
		{
			base.OnDispose();
			if (Model != null)
			{
				Model.Dispose();
				Model = null;
			}
		}
	}
}
