


namespace GoveKits.MVI
{
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
}