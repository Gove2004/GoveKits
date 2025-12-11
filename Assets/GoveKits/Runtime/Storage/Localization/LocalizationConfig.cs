using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace GoveKits.Localization
{
    [CreateAssetMenu(fileName = "LocalizationConfig", menuName = "GoveKits/Localization Config")]
    public class LocalizationConfig : ScriptableObject
    {
        [Header("语言数据文件 (JSON)")]
        public TextAsset LanguageJson;

        [Header("字体设置")]
        public List<LanguageFont> FontSettings = new List<LanguageFont>();

        [Header("默认备用字体")]
        public TMP_FontAsset DefaultFont;

        public TMP_FontAsset GetFont(LanguageCode code)
        {
            var setting = FontSettings.Find(x => x.languageCode == code);
            return setting != null && setting.fontAsset != null ? setting.fontAsset : DefaultFont;
        }
    }

    [System.Serializable]
    public class LanguageFont
    {
        public LanguageCode languageCode;
        public TMP_FontAsset fontAsset;
    }
}