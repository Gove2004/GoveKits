using GoveKits.Runtime.Core;
using UnityEngine;

namespace GoveKits.Runtime.Storage
{
	/// <summary>
	/// 最简 PlayerPrefs 封装, 仅代理
	/// </summary>
	public class PrefsCore : ICore
	{
		/// <summary>
		/// 写入 int。
		/// </summary>
		public void SetInt(string key, int value)
            => PlayerPrefs.SetInt(key, value);

		/// <summary>
		/// 读取 int。
		/// </summary>
		public int GetInt(string key, int defaultValue = 0)
			=> PlayerPrefs.GetInt(key, defaultValue);

		/// <summary>
		/// 写入 float。
		/// </summary>
		public void SetFloat(string key, float value)
            => PlayerPrefs.SetFloat(key, value);
            
		/// <summary>
		/// 读取 float。
		/// </summary>
		public float GetFloat(string key, float defaultValue = 0f)
			=> PlayerPrefs.GetFloat(key, defaultValue);

		/// <summary>
		/// 写入 string。
		/// </summary>
		public void SetString(string key, string value)
            => PlayerPrefs.SetString(key, value ?? string.Empty);

		/// <summary>
		/// 读取 string。
		/// </summary>
		public string GetString(string key, string defaultValue = "")
			=> PlayerPrefs.GetString(key, defaultValue);

		/// <summary>
		/// 写入 bool（内部存为 1/0）。
		/// </summary>
		public void SetBool(string key, bool value)
			=> SetInt(key, value ? 1 : 0);

		/// <summary>
		/// 读取 bool（内部读取 1/0）。
		/// </summary>
		public bool GetBool(string key, bool defaultValue = false)
			=> GetInt(key, defaultValue ? 1 : 0) != 0;

		/// <summary>
		/// 判断 key 是否存在。
		/// </summary>
		public bool HasKey(string key)
			=> PlayerPrefs.HasKey(key);

		/// <summary>
		/// 删除单个 key。
		/// </summary>
		public void DeleteKey(string key)
            => PlayerPrefs.DeleteKey(key);

		/// <summary>
		/// 删除全部 PlayerPrefs 数据。
		/// </summary>
		public void DeleteAll()
            => PlayerPrefs.DeleteAll();

		/// <summary>
		/// 调用 PlayerPrefs.Save 强制落盘。
		/// </summary>
		public void Save()
			=> PlayerPrefs.Save();

        public void OnShutdown()
        {
            // Nothing to do.
        }
    }
}
