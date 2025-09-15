
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using GoveKits.Manager;

public class AudioSettingsUI : MonoBehaviour
{
    [Header("Sliders")]
    [SerializeField] private Slider _masterSlider;
    [SerializeField] private Slider _BGMSlider;
    [SerializeField] private Slider _sfxSlider;
    [SerializeField] private Slider _uiSlider;
    [SerializeField] private Slider _voiceSlider;

    [Header("Texts")]
    [SerializeField] private Text _masterText;
    [SerializeField] private Text _BGMText;
    [SerializeField] private Text _sfxText;
    [SerializeField] private Text _uiText;
    [SerializeField] private Text _voiceText;

    [Header("测试音效")]
    [SerializeField] private AudioClip _testSFX1;
    [SerializeField] private AudioClip _testSFX2;
    [SerializeField] private AudioClip _testSFX3;
    [SerializeField] private AudioClip _testBGM1;
    [SerializeField] private AudioClip _testBGM2;

    private void Start()
    {
        // 初始化滑块值
        _masterSlider.value = AudioManager.Instance.MasterVolume;
        _BGMSlider.value = AudioManager.Instance.BGMVolume;
        _sfxSlider.value = AudioManager.Instance.SFXVolume;
        _uiSlider.value = AudioManager.Instance.UIVolume;
        _voiceSlider.value = AudioManager.Instance.VoiceVolume;

        // 更新文本
        UpdateVolumeTexts();

        // 添加监听
        _masterSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
        _BGMSlider.onValueChanged.AddListener(OnBGMVolumeChanged);
        _sfxSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
        _uiSlider.onValueChanged.AddListener(OnUIVolumeChanged);
        _voiceSlider.onValueChanged.AddListener(OnVoiceVolumeChanged);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.A))
        {
            AudioManager.Instance.PlayBGM(_testBGM1);
        }
        else if (Input.GetKeyDown(KeyCode.S))
        {
            AudioManager.Instance.PlayBGM(_testBGM2);
        }
        else if (Input.GetKeyDown(KeyCode.Z))
        {
            AudioManager.Instance.PlaySFX(_testSFX1);
        }
        else if (Input.GetKeyDown(KeyCode.X))
        {
            AudioManager.Instance.PlaySFX(_testSFX2);
        }
        else if (Input.GetKeyDown(KeyCode.C))
        {
            AudioManager.Instance.PlaySFX(_testSFX3);
        }
    }

    private void OnMasterVolumeChanged(float value)
    {
        AudioManager.Instance.SetVolume(AudioChannel.Master, value);
        _masterText.text = $"{value * 100:0}%";
    }

    private void OnBGMVolumeChanged(float value)
    {
        AudioManager.Instance.SetVolume(AudioChannel.BGM, value);
        _BGMText.text = $"{value * 100:0}%";
    }

    private void OnSFXVolumeChanged(float value)
    {
        AudioManager.Instance.SetVolume(AudioChannel.SFX, value);
        _sfxText.text = $"{value * 100:0}%";
    }

    private void OnUIVolumeChanged(float value)
    {
        AudioManager.Instance.SetVolume(AudioChannel.UI, value);
        _uiText.text = $"{value * 100:0}%";
    }

    private void OnVoiceVolumeChanged(float value)
    {
        AudioManager.Instance.SetVolume(AudioChannel.Voice, value);
        _voiceText.text = $"{value * 100:0}%";
    }

    private void UpdateVolumeTexts()
    {
        _masterText.text = $"{_masterSlider.value * 100:0}%";
        _BGMText.text = $"{_BGMSlider.value * 100:0}%";
        _sfxText.text = $"{_sfxSlider.value * 100:0}%";
        _uiText.text = $"{_uiSlider.value * 100:0}%";
        _voiceText.text = $"{_voiceSlider.value * 100:0}%";
    }
}

