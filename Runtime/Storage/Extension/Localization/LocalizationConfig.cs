using System.Collections.Generic;
using UnityEngine;
#if TMP_PRESENT
using TMPro;
#endif

namespace GoveKits.Runtime.Storage
{
    [CreateAssetMenu(fileName = "LocalizationConfig", menuName = "GoveKits/Localization Config")]
    public class LocalizationConfig : ScriptableObject
    {
#if TMP_PRESENT
        [Header("字体设置")]
        public List<LanguageFont> FontSettings = new List<LanguageFont>();

        [Header("默认备用字体")]
        public TMP_FontAsset DefaultFont;

        public TMP_FontAsset GetFont(LanguageCode code)
        {
            var setting = FontSettings.Find(x => x.languageCode == code);
            return setting != null && setting.fontAsset != null ? setting.fontAsset : DefaultFont;
        }
#endif
    }

    [System.Serializable]
    public class LanguageFont
    {
        public LanguageCode languageCode;
#if TMP_PRESENT
        public TMP_FontAsset fontAsset;
#endif
    }
}