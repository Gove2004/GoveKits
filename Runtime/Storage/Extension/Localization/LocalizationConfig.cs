using System;
using System.Collections.Generic;
using GoveKits.Runtime.Util;
using UnityEngine;
#if TMP_PRESENT
using TMPro;
#endif

namespace GoveKits.Runtime.Storage
{
    /// <summary>
    /// 多语言字体配置 ScriptableObject，用于管理不同语言的字体映射关系。
    /// 通过 LocalizationCore 加载，为每种语言指定对应的 TMP 字体资源。
    /// </summary>
    [CreateAssetMenu(fileName = "LocalizationConfig", menuName = "GoveKits/Localization Config")]
    public class LocalizationConfig : ScriptableObject
    {
#if TMP_PRESENT
        /// <summary>
        /// 各语言对应的字体设置列表。
        /// </summary>
        public List<LanguageFont> FontSettings = new List<LanguageFont>();

        /// <summary>
        /// 默认备用字体，当指定语言没有配置字体时使用。
        /// </summary>
        public TMP_FontAsset DefaultFont;

        /// <summary>
        /// 根据语言代码获取对应的字体资源。
        /// </summary>
        /// <param name="code">语言代码。</param>
        /// <returns>匹配的字体资源，未找到或配置为空时返回默认字体。</returns>
        public TMP_FontAsset GetFont(LanguageCode code)
        {
            var setting = FontSettings.Find(x => x.languageCode == code);
            return setting != null && setting.fontAsset != null ? setting.fontAsset : DefaultFont;
        }
#endif
    }

    /// <summary>
    /// 单一语言的字体映射配置。
    /// </summary>
    [Serializable]
    public class LanguageFont
    {
        /// <summary>语言代码。</summary>
        public LanguageCode languageCode;
#if TMP_PRESENT
        /// <summary>对应的 TextMesh Pro 字体资源。</summary>
        public TMP_FontAsset fontAsset;
#endif
    }
}
