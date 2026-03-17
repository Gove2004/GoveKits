


namespace GoveKits.Runtime.AI.FSM
{
    /// <summary>
    /// FSM 持有者标记接口。
    /// <para>用于约束可作为 <see cref="FSM{TStateEnum, TFSMObject}"/> Owner 的对象类型。</para>
    /// </summary>
    public interface IFSMObject
    {
        /// <summary>
        /// FSM 初始化接口。
        /// <para>通常在 <see cref="FSM{TStateEnum, TFSMObject}.Start"/> 首次调用前触发，用于注册状态与搭建结构。</para>
        /// </summary>
        void InitFSM() { }
    }
}