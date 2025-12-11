


using GoveKits.Binary;

namespace GoveKits.Localization
{
    [GenBinaryData("Assets/GoveKits/Runtime/Storage/Localization")]
    public partial class LocalizationSetting
    {
        [BinaryMember(1)] public int CurrentLanguage = (int)LanguageCode.ChineseCN;
    }
}