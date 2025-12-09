


namespace GoveKits.Save
{
    public interface ISaveable
    {
        object OnSave();
        void OnLoad(object state);
    }
}