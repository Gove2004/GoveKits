


namespace GoveKits.MVI
{
    /// <summary>
    /// 模块接口：定义 MVI 架构中所有模块的生命周期。
    /// <para>支持初始化（Initialize）和释放（Dispose）两个关键阶段。</para>
    /// </summary>
    public interface IModule
    {
        /// <summary>模块初始化。</summary>
        public virtual void Initialize() { }
        /// <summary>模块资源释放。</summary>
        public virtual void Dispose() { }
    }


    /// <summary>
    /// 模块基类：为 MVI 架构提供通用的模块功能（生命周期管理、依赖注入等）。
    /// <para>所有 Model/View/System 等组件应继承此类。</para>
    /// </summary>
    public abstract class Module : IModule
    {
        
    }
}