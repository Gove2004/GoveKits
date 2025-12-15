


namespace GoveKits.UI
{
    
    public class PanelEvent : Events.EventInfo
    {
        public PanelLifeType LifeType = PanelLifeType.None;
        public BasePanel Panel = null;


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