using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace GoveKits.MVI
{
    // ==================== 核心接口 ====================
    
    /// <summary>
    /// 状态接口 - 所有状态都必须实现
    /// </summary>
    public interface IState
    {
        // 状态标识符
        string StateId { get; }
    }

    /// <summary>
    /// 意图接口 - 用户操作或事件
    /// </summary>
    public interface IIntent
    {
        // 意图类型标识
        string IntentType { get; }
    }

    /// <summary>
    /// 模块基类 - 所有模块的基类
    /// </summary>
    public abstract class Module
    {
        public abstract string ModuleId { get; }
        public virtual void Initialize() { }
        public virtual void Dispose() { }
    }

    // ==================== MVI 核心组件 ====================

    /// <summary>
    /// 模型 - 管理状态和数据
    /// </summary>
    public abstract class Model<TState> : Module where TState : IState, new()
    {
        protected TState currentState = new TState();
        private List<Action<TState>> stateListeners = new List<Action<TState>>();

        // 获取当前状态
        public TState CurrentState => currentState;

        // 更新状态
        protected void UpdateState(Action<TState> updater)
        {
            updater?.Invoke(currentState);
            NotifyStateChanged();
        }

        // 设置新状态
        protected void SetState(TState newState)
        {
            currentState = newState;
            NotifyStateChanged();
        }

        // 通知状态变化
        private void NotifyStateChanged()
        {
            foreach (var listener in stateListeners)
            {
                listener?.Invoke(currentState);
            }
        }

        // 添加状态监听
        public void AddStateListener(Action<TState> listener)
        {
            if (listener != null && !stateListeners.Contains(listener))
            {
                stateListeners.Add(listener);
            }
        }

        // 移除状态监听
        public void RemoveStateListener(Action<TState> listener)
        {
            stateListeners.Remove(listener);
        }

        public override void Dispose()
        {
            stateListeners.Clear();
            base.Dispose();
        }
    }

    /// <summary>
    /// 视图 - 渲染UI和接收用户输入
    /// </summary>
    public abstract class View<TState> : Module where TState : IState
    {
        protected Model<TState> boundModel;

        // 绑定模型
        public void BindModel(Model<TState> model)
        {
            if (boundModel != null)
            {
                boundModel.RemoveStateListener(OnStateChanged);
            }

            boundModel = model;
            if (boundModel != null)
            {
                boundModel.AddStateListener(OnStateChanged);
                OnStateChanged(boundModel.CurrentState);
            }
        }

        // 状态变化回调
        protected abstract void OnStateChanged(TState state);

        // 发送意图到系统
        protected void SendIntent(IIntent intent)
        {
            App.Instance?.GetSystem<System>()?.ProcessIntent(intent);
        }

        public override void Dispose()
        {
            if (boundModel != null)
            {
                boundModel.RemoveStateListener(OnStateChanged);
            }
            base.Dispose();
        }
    }

    /// <summary>
    /// 系统 - 协调 Model 和 View，处理业务逻辑
    /// </summary>
    public abstract class System : Module
    {
        protected Dictionary<Type, Model<IState>> models = new Dictionary<Type, Model<IState>>();
        protected Dictionary<Type, View<IState>> views = new Dictionary<Type, View<IState>>();

        // 注册模型
        public void RegisterModel<TModel>(TModel model) where TModel : Model<IState>
        {
            models[typeof(TModel)] = model;
        }

        // 注册视图
        public void RegisterView<TView>(TView view) where TView : View<IState>
        {
            views[typeof(TView)] = view;
        }

        // 处理意图
        public abstract void ProcessIntent(IIntent intent);

        // 获取模型
        public TModel GetModel<TModel>() where TModel : Model<IState>
        {
            if (models.TryGetValue(typeof(TModel), out var model))
            {
                return (TModel)model;
            }
            return null;
        }

        // 获取视图
        public TView GetView<TView>() where TView : View<IState>
        {
            if (views.TryGetValue(typeof(TView), out var view))
            {
                return (TView)view;
            }
            return null;
        }

        public override void Dispose()
        {
            foreach (var model in models.Values)
            {
                model.Dispose();
            }
            foreach (var view in views.Values)
            {
                view.Dispose();
            }
            models.Clear();
            views.Clear();
            base.Dispose();
        }
    }

    // ==================== 应用管理 ====================

    /// <summary>
    /// 应用 - 全局单例，管理系统
    /// </summary>
    public class App : MonoBehaviour
    {
        private static App instance;
        public static App Instance => instance;

        private Dictionary<string, System> systems = new Dictionary<string, System>();

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
                Initialize();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Initialize()
        {
            // 初始化所有系统
            RegisterSystem(new UserSystem());
            RegisterSystem(new GameSystem());
            // 添加更多系统...

            foreach (var system in systems.Values)
            {
                system.Initialize();
            }
        }

        // 注册系统
        public void RegisterSystem(System system)
        {
            if (system != null)
            {
                systems[system.ModuleId] = system;
            }
        }

        // 获取系统
        public TSystem GetSystem<TSystem>() where TSystem : System
        {
            foreach (var system in systems.Values)
            {
                if (system is TSystem targetSystem)
                {
                    return targetSystem;
                }
            }
            return null;
        }

        // 处理全局意图
        public void ProcessIntent(IIntent intent)
        {
            foreach (var system in systems.Values)
            {
                system.ProcessIntent(intent);
            }
        }

        private void OnDestroy()
        {
            foreach (var system in systems.Values)
            {
                system.Dispose();
            }
            systems.Clear();

            if (instance == this)
            {
                instance = null;
            }
        }
    }

    // ==================== 示例实现 ====================

    // 示例：用户系统
    public class UserState : IState
    {
        public string StateId => "UserState";
        public string UserName { get; set; } = "Guest";
        public int Level { get; set; } = 1;
        public bool IsLoggedIn { get; set; } = false;
    }

    public class UserIntent : IIntent
    {
        public string IntentType { get; }

        public UserIntent(string type)
        {
            IntentType = type;
        }

        // 预定义意图类型
        public static readonly string LOGIN = "USER_LOGIN";
        public static readonly string LOGOUT = "USER_LOGOUT";
        public static readonly string UPDATE_PROFILE = "USER_UPDATE_PROFILE";
    }

    public class UserModel : Model<UserState>
    {
        public override string ModuleId => "UserModel";

        // 登录业务逻辑
        public void Login(string username)
        {
            UpdateState(state =>
            {
                state.UserName = username;
                state.IsLoggedIn = true;
                state.Level = 1;
            });
        }

        // 登出业务逻辑
        public void Logout()
        {
            UpdateState(state =>
            {
                state.UserName = "Guest";
                state.IsLoggedIn = false;
                state.Level = 0;
            });
        }
    }

    public class UserView : View<UserState>
    {
        public override string ModuleId => "UserView";

        protected override void OnStateChanged(UserState state)
        {
            // 更新UI显示
            Debug.Log($"UserView: {state.UserName}, Level: {state.Level}, LoggedIn: {state.IsLoggedIn}");
            
            // 这里可以更新Unity的UI组件
            // UpdateUIElements(state);
        }

        // UI按钮点击事件
        public void OnLoginButtonClicked()
        {
            SendIntent(new UserIntent(UserIntent.LOGIN));
        }

        public void OnLogoutButtonClicked()
        {
            SendIntent(new UserIntent(UserIntent.LOGOUT));
        }
    }

    public class UserSystem : System
    {
        private UserModel userModel;
        private UserView userView;

        public override string ModuleId => "UserSystem";

        public override void Initialize()
        {
            base.Initialize();

            userModel = new UserModel();
            userView = new UserView();

            RegisterModel(userModel);
            RegisterView(userView);

            // 绑定模型和视图
            userView.BindModel(userModel);

            userModel.Initialize();
            userView.Initialize();
        }

        public override void ProcessIntent(IIntent intent)
        {
            if (intent is UserIntent userIntent)
            {
                switch (userIntent.IntentType)
                {
                    case UserIntent.LOGIN:
                        userModel.Login("Player123");
                        break;
                    case UserIntent.LOGOUT:
                        userModel.Logout();
                        break;
                }
            }
        }
    }

    // 示例：游戏系统
    public class GameSystem : System
    {
        public override string ModuleId => "GameSystem";

        public override void ProcessIntent(IIntent intent)
        {
            // 处理游戏相关意图
        }
    }
}



// 在Unity中初始化
public class GameInitializer : MonoBehaviour
{
    private void Start()
    {
        // 获取用户系统
        var userSystem = App.Instance.GetSystem<UserSystem>();
        
        // 模拟用户操作
        userSystem.ProcessIntent(new UserIntent(UserIntent.LOGIN));
        
        // 获取用户模型数据
        var userModel = userSystem.GetModel<UserModel>();
        Debug.Log($"Current user: {userModel.CurrentState.UserName}");
    }
}