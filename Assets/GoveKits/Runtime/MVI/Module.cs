


namespace GoveKits.MVI
{
    // 模块接口
    public interface IModule
    {
        public virtual void Initialize() { }
        public virtual void Dispose() { }
    }


    // 模块基类，提供通用功能
    public abstract class Module : IModule
    {
        // 可在此添加通用的模块功能（如生命周期管理、依赖注入等）
    }
}