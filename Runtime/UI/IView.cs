

namespace GoveKits.Runtime.UI
{
    public interface IView
    {
        IViewModel ViewModel { get; set; }
        void BindViewModel(IViewModel viewModel);
        void UnbindViewModel();
    }
}