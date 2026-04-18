using System.Collections.Generic;
using UnityEngine;
using PinYinSpell;
using System.Collections;
using GoveKits.Runtime.Core;

[System.Serializable]
public class AllSoundClips
{
    public AudioClip _b;
    public AudioClip _p;
    public AudioClip _m;
    public AudioClip _f;
    public AudioClip _d;
    public AudioClip _t;
    public AudioClip _n;
    public AudioClip _l;
    public AudioClip _g;
    public AudioClip _k;
    public AudioClip _h;
    public AudioClip _j;
    public AudioClip _q;
    public AudioClip _x;
    public AudioClip _zh;
    public AudioClip _ch;
    public AudioClip _sh;
    public AudioClip _r;
    public AudioClip _z;
    public AudioClip _c;
    public AudioClip _s;
    public AudioClip _y;
    public AudioClip _w;
    public AudioClip _a;
    public AudioClip _o;
    public AudioClip _e;
    public AudioClip _i;
    public AudioClip _u;
    public AudioClip _v;
    public AudioClip _ai;
    public AudioClip _ei;
    public AudioClip _ui;
    public AudioClip _ao;
    public AudioClip _ou;
    public AudioClip _iu;
    public AudioClip _ie;
    public AudioClip _ve;
    public AudioClip _er;
    public AudioClip _an;
    public AudioClip _en;
    public AudioClip _in;
    public AudioClip _un;
    public AudioClip _vn;
    public AudioClip _ang;
    public AudioClip _eng;
    public AudioClip _ing;
    public AudioClip _ong;
    public AudioClip __;

    public Dictionary<string, AudioClip> ClipsDict()
    {
        return new Dictionary<string, AudioClip> {
            { "b",_b }, { "p",_p }, { "m",_m }, { "f",_f }, { "d",_d }, { "t",_t },
            { "n",_n }, { "l",_l }, { "g",_g }, { "k",_k }, { "h",_h }, { "j",_j },
            { "q",_q }, { "x",_x }, { "zh",_zh }, { "ch",_ch }, { "sh",_sh }, { "r",_r },
            { "z",_z }, { "c",_c }, { "s",_s }, { "y",_y }, { "w",_w }, { "a",_a },
            { "o",_o }, { "e",_e }, { "i",_i }, { "u",_u }, { "v",_v }, { "ai",_ai },
            { "ei",_ei }, { "ui",_ui }, { "ao",_ao }, { "ou",_ou }, { "iu",_iu }, { "ie",_ie },
            { "ve",_ve }, { "er",_er }, { "an",_an }, { "en",_en }, { "in",_in }, { "un",_un },
            { "vn",_vn }, { "ang",_ang }, { "eng",_eng }, { "ing",_ing }, { "ong",_ong }, {"_", __}
        };
    }
}


[RequireComponent(typeof(AudioSource))]
public class PronounceCore : MonoSingleton<PronounceCore>
{
    public AudioSource audioSource;

    private Dictionary<string, AudioClip> clips;
    private Coroutine lastCo;

    [Tooltip("每个音节开头去掉一部分")]
    [Range(0, 0.5f)]
    public float trimBegin = 0;
    [Tooltip("每个音节结尾去掉一部分")]
    [Range(0.5f, 1f)]
    public float trimEnd = 1;

    public AllSoundClips allClips;

	protected override void Init()
    {
        audioSource = GetComponent<AudioSource>();
        clips = allClips != null ? allClips.ClipsDict() : new Dictionary<string, AudioClip>();
    }

    /// <summary>
    /// 将汉字转换为拼音
    /// </summary>
    /// <param name="chinese">输入的汉字字符串</param>
    /// <returns>转换后的拼音字符串，音节之间用空格分隔</returns>
    public string Convert(string chinese)
    {
        string s = Spell.PreProcess(chinese);
        string ret = Spell.MakePinYin(s, SpellOptions.AddSpace | SpellOptions.EnableUnicodeLetter);
        return ret;
    }

    /// <summary>
    /// 播放拼音对应的音频
    /// </summary>
    /// <param name="pinyin">输入的拼音字符串，音节之间用空格分隔</param>
    /// <remarks>如果拼音字符串中的某个音节没有对应的音频文件，则会跳过该音节并继续播放下一个音节。</remarks>
    public void Speak(string pinyin)
    {
        if (string.IsNullOrWhiteSpace(pinyin))
        {
            return;
        }

        pinyin = pinyin.Trim();
        string[] ss = pinyin.Split((char[])null, System.StringSplitOptions.RemoveEmptyEntries);
        if (lastCo != null)
        {
            StopCoroutine(lastCo);
        }
        lastCo = StartCoroutine(CoSpeak(ss));
    }

    /// <summary>
    /// 将汉字转换为拼音并播放
    /// </summary>
    /// <param name="chinese">输入的汉字字符串</param>
    public void ConvertAndSpeak(string chinese)
    {
        string pinyin = Convert(chinese);
        Speak(pinyin);
    }

    private IEnumerator CoSpeak(string[] source)
    {
        float begin = Mathf.Clamp01(trimBegin);
        float end = Mathf.Clamp01(Mathf.Max(trimEnd, begin));
        float safePitch = Mathf.Max(0.01f, audioSource.pitch);

        foreach (string raw in source)
        {
            string p = raw.Trim().ToLowerInvariant();

            if (!clips.TryGetValue(p, out AudioClip clip) || clip == null)
            {
                Debug.Log("跳过未知音节：" +  p + ";");
                continue;
            }

            audioSource.clip = clip;
            audioSource.time = clip.length * begin;
            audioSource.Play();

            float playDuration = clip.length * (end - begin) / safePitch;
            yield return new WaitForSeconds(Mathf.Max(0f, playDuration));
        }

        lastCo = null;
    }

    protected override void Uninit()
    {
        if (lastCo != null)
        {
            StopCoroutine(lastCo);
            lastCo = null;
        }

        if (audioSource != null)
        {
            audioSource.Stop();
        }
    }
}
