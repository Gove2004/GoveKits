


using GoveKits.Binary;

namespace GoveKits.Audio
{
    [GenBinaryData("Assets/GoveKits/Runtime/Audio")]
    public partial class AudioSetting
    {
        [BinaryMember(1)] public float MasterVolume = 1f;
        [BinaryMember(2)] public float BGMVolume = 1f;
        [BinaryMember(3)] public float SFXVolume = 1f;
        [BinaryMember(4)] public float UIVolume = 1f;
        [BinaryMember(5)] public float VoiceVolume = 1f;
    }
}