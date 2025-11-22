using UnityEngine;
using UnityEngine.UI;

using UnityEngine.InputSystem;
using GoveKits.Config;

public class UILanguageText : MonoBehaviour
{
    public Text textComponent;
    public Button zhButton;
    public Button enButton;


    public void Start()
    {
        // 初始化文本
        UpdateText();

        // 绑定按钮事件
        zhButton.onClick.AddListener(() => {
            LanguageManager.Instance.SwitchLanguage(LanguageCode.ChineseCN);
            UpdateText();
        });

        enButton.onClick.AddListener(() => {
            LanguageManager.Instance.SwitchLanguage(LanguageCode.EnglishUS);
            UpdateText();
        });
    }

    private void UpdateText()
    {
        textComponent.text = LanguageManager.Instance.GetText("Hello");
    }


    public void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            var lang = ConfigManager.Instance.GetConfig<语言_语言Config>("Hello");
            Debug.Log(lang.ChineseCN);
            Debug.Log(lang.EnglishUS);
        }
    }
}
