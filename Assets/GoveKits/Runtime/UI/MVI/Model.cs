


// namespace GoveKits.MVI
// {
//     /// <summary>
//     /// 模型 - 管理状态和数据
//     /// </summary>
//     public abstract class Model<TState> : Module where TState : IState, new()
//     {
//         protected TState currentState = new TState();
//         private List<Action<TState>> stateListeners = new List<Action<TState>>();

//         // 获取当前状态
//         public TState CurrentState => currentState;

//         // 更新状态
//         protected void UpdateState(Action<TState> updater)
//         {
//             updater?.Invoke(currentState);
//             NotifyStateChanged();
//         }

//         // 设置新状态
//         protected void SetState(TState newState)
//         {
//             currentState = newState;
//             NotifyStateChanged();
//         }

//         // 通知状态变化
//         private void NotifyStateChanged()
//         {
//             foreach (var listener in stateListeners)
//             {
//                 listener?.Invoke(currentState);
//             }
//         }

//         // 添加状态监听
//         public void AddStateListener(Action<TState> listener)
//         {
//             if (listener != null && !stateListeners.Contains(listener))
//             {
//                 stateListeners.Add(listener);
//             }
//         }

//         // 移除状态监听
//         public void RemoveStateListener(Action<TState> listener)
//         {
//             stateListeners.Remove(listener);
//         }

//         public override void Dispose()
//         {
//             stateListeners.Clear();
//             base.Dispose();
//         }
//     }
// }