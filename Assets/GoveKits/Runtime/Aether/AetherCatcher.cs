


namespace GoveKits.Aether
{
    // ==========================================================
    // 3. AetherCatcher: 捕获器 (逻辑节点)
    // ==========================================================
    /// <summary>
    /// 非泛型基类，用于管道存储
    /// </summary>
    public abstract class AetherCatcher
    {
        public virtual int Priority => 0; // 越小越靠近上游
        internal abstract void OnFlowIn(AetherInfo aether);
    }

    /// <summary>
    /// 泛型捕获器，用户继承此类编写逻辑
    /// </summary>
    public abstract class AetherCatcher<T> : AetherCatcher where T : AetherInfo
    {
        internal override void OnFlowIn(AetherInfo aether)
        {
            // 强类型转换，由管线保证类型安全
            OnCapture((T)aether);
        }

        protected abstract void OnCapture(T aether);
    }

}