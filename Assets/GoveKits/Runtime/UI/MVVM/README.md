# MVVM 模块文档

## 文档速览
- 目标: 提供 Unity UI 下可复用的 MVVM 基本范式。
- 核心类: `Model`、`ViewModel`、`View<TViewModel>`。
- 示例: 本文内嵌最小 Login 示例，展示 Model-ViewModel-View 的完整连接。

## 阅读路径
1. 先看 `Model.cs`，理解数据层职责。
2. 再看 `ViewModel.cs`，理解属性通知与命令。
3. 接着看 `View.cs`，理解绑定与解绑生命周期。
4. 最后看本文“最小可运行 Login 示例”代码段，直接照着接 UI 即可运行。

## 设计理念
- 低耦合: Model 不依赖 UI，View 不承载业务逻辑。
- 可观测: ViewModel 通过 `PropertyChanged` 驱动局部刷新。
- 可执行命令: 用 `ICommand/RelayCommand` 承载交互动作。
- 生命周期明确: Bind/Unbind/Dispose 保证事件不泄漏。

## 架构介绍
- `Model`: 领域数据与规则校验。
- `ViewModel`: 状态汇聚、命令封装、通知派发。
- `View<TViewModel>`: 负责 UI 控件映射和事件转发。
- README 内嵌登录示例: 最小登录面板示例，包含输入、按钮、状态文案。

## 快速开始
### 1) 创建你的 ViewModel 属性
```csharp
public sealed class ProfileViewModel : ViewModel
{
    private string _nickName;

    public string NickName
    {
        get => _nickName;
        set => SetProperty(ref _nickName, value, nameof(NickName));
    }
}
```

### 2) 在 View 中监听属性并刷新 UI
```csharp
public sealed class ProfileView : View<ProfileViewModel>
{
    [SerializeField] private Text nickNameText;

    protected override void OnViewModelPropertyChanged(string propertyName)
    {
        if (propertyName == nameof(ProfileViewModel.NickName))
        {
            nickNameText.text = ViewModel.NickName;
        }
    }

    protected override void RefreshAll()
    {
        nickNameText.text = ViewModel.NickName;
    }
}
```

### 3) 最小可运行 Login 示例（直接拷贝）
```csharp
using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace GoveKits.Runtime.UI.MVVM
{
    public sealed class LoginModel : Model
    {
        public bool Validate(string userName, string password)
        {
            return !string.IsNullOrWhiteSpace(userName)
                && !string.IsNullOrWhiteSpace(password)
                && password.Length >= 6;
        }
    }

    public sealed class LoginViewModel : ViewModel<LoginModel>
    {
        private readonly RelayCommand _loginCommand;
        private string _userName = string.Empty;
        private string _password = string.Empty;
        private string _status = "Please input account.";
        private bool _isSubmitting;

        public LoginViewModel(LoginModel model) : base(model)
        {
            _loginCommand = new RelayCommand(ExecuteLogin, CanLogin);
        }

        public string UserName
        {
            get => _userName;
            set
            {
                if (SetProperty(ref _userName, value, nameof(UserName)))
                {
                    _loginCommand.RaiseCanExecuteChanged();
                }
            }
        }

        public string Password
        {
            get => _password;
            set
            {
                if (SetProperty(ref _password, value, nameof(Password)))
                {
                    _loginCommand.RaiseCanExecuteChanged();
                }
            }
        }

        public string Status
        {
            get => _status;
            private set => SetProperty(ref _status, value, nameof(Status));
        }

        public ICommand LoginCommand => _loginCommand;

        private bool CanLogin()
        {
            return !_isSubmitting && Model.Validate(UserName, Password);
        }

        private async void ExecuteLogin()
        {
            if (!CanLogin())
            {
                Status = "Invalid account or password.";
                return;
            }

            _isSubmitting = true;
            _loginCommand.RaiseCanExecuteChanged();
            Status = "Logging in...";

            try
            {
                await Task.Delay(500);
                Status = $"Login success, welcome {UserName}.";
            }
            catch (Exception e)
            {
                Status = $"Login failed: {e.Message}";
            }
            finally
            {
                _isSubmitting = false;
                _loginCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public sealed class LoginView : View<LoginViewModel>
    {
        [SerializeField] private InputField userNameInput;
        [SerializeField] private InputField passwordInput;
        [SerializeField] private Button loginButton;
        [SerializeField] private Text statusText;

        private LoginViewModel _vm;

        private void Awake()
        {
            _vm = new LoginViewModel(new LoginModel());
            SetViewModel(_vm);

            userNameInput.onValueChanged.AddListener(v => ViewModel.UserName = v);
            passwordInput.onValueChanged.AddListener(v => ViewModel.Password = v);
            loginButton.onClick.AddListener(() => ViewModel.LoginCommand.Execute());
            ViewModel.LoginCommand.CanExecuteChanged += RefreshButton;

            RefreshAll();
        }

        protected override void OnViewModelPropertyChanged(string propertyName)
        {
            if (propertyName == nameof(LoginViewModel.Status))
            {
                statusText.text = ViewModel.Status;
            }
        }

        protected override void RefreshAll()
        {
            statusText.text = ViewModel.Status;
            RefreshButton();
        }

        protected override void OnDestroy()
        {
            if (ViewModel?.LoginCommand != null)
            {
                ViewModel.LoginCommand.CanExecuteChanged -= RefreshButton;
            }

            _vm?.Dispose();
            base.OnDestroy();
        }

        private void RefreshButton()
        {
            loginButton.interactable = ViewModel.LoginCommand.CanExecute();
        }
    }
}
```

## 注意事项
- `View.SetViewModel` 后会自动调用 `BindViewModel`，并触发 `RefreshAll`。
- `View` 销毁时应解绑 UI 事件并释放 ViewModel。
- `SetProperty` 只有值变化时才触发通知，避免无效刷新。
- `RelayCommand` 的 `CanExecute` 变化后记得 `RaiseCanExecuteChanged`。
- 示例使用 `UnityEngine.UI` 组件，需在场景内正确绑定引用。

## 相关跳转
- `Model.cs`
- `ViewModel.cs`
- `View.cs`
- `README.md`（本文内嵌登录示例）
