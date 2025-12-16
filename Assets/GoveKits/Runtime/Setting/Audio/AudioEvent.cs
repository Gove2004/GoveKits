


using GoveKits.Events;

namespace GoveKits.Audio
{
    public class OnAudioPlayed : EventInfo
    {
        public AudioConfig audioConfig;

        public override void OnRecycle()
        {
            audioConfig = null;
        }
    }
}