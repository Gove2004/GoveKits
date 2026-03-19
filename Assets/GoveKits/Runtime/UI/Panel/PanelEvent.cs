


using GoveKits.Runtime.Core.Event;

namespace GoveKits.Runtime.UI.Panel
{
    /// <summary>
    /// 面板生命周期事件：在面板状态转换时发布，用于监听面板行为。
    /// <para>包含生命周期类型（OnCreate/OnStart/OnResume 等）和触发的面板引用。</para>
    /// </summary>
    public class PanelEvent : EventInfo
    {
        /// <summary>面板生命周期类型。</summary>
        public PanelLifeType LifeType = PanelLifeType.None;
        /// <summary>触发该事件的面板实例。</summary>
        public BasePanel Panel = null;


        /// <summary>
        /// 设置事件数据。
        /// </summary>
        /// <param name="lifeType">生命周期类型。</param>
        /// <param name="panel">触发事件的面板。</param>
        public void SetData(PanelLifeType lifeType, BasePanel panel)
        {
            LifeType = lifeType;
            Panel = panel;
        }


        public override void OnRecycle()
        {
            LifeType = PanelLifeType.None;
            Panel = null;
        }
    }
}