


namespace GoveKits.MVI
{
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
}