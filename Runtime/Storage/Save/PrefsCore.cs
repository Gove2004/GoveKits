using System;
using GoveKits.Runtime.Util;
using UnityEngine;

namespace GoveKits.Runtime.Storage
{
    /// <summary>
    /// 本地偏好设置核心，封装 PlayerPrefs 操作，提供类型安全的读写 API。
    /// </summary>
    public static class PrefsCore
    {
        /// <summary>
        /// 设置整数类型的偏好设置。
        /// </summary>
        /// <param name="key">键名。</param>
        /// <param name="value">要存储的值。</param>
        public static void SetInt(string key, int value)
            => PlayerPrefs.SetInt(key, value);

        /// <summary>
        /// 读取整数类型的偏好设置。
        /// </summary>
        /// <param name="key">键名。</param>
        /// <param name="defaultValue">键不存在时的默认值。</param>
        /// <returns>存储的整数值。</returns>
        public static int GetInt(string key, int defaultValue = 0)
            => PlayerPrefs.GetInt(key, defaultValue);

        /// <summary>
        /// 设置浮点数类型的偏好设置。
        /// </summary>
        /// <param name="key">键名。</param>
        /// <param name="value">要存储的值。</param>
        public static void SetFloat(string key, float value)
            => PlayerPrefs.SetFloat(key, value);

        /// <summary>
        /// 读取浮点数类型的偏好设置。
        /// </summary>
        /// <param name="key">键名。</param>
        /// <param name="defaultValue">键不存在时的默认值。</param>
        /// <returns>存储的浮点数值。</returns>
        public static float GetFloat(string key, float defaultValue = 0f)
            => PlayerPrefs.GetFloat(key, defaultValue);

        /// <summary>
        /// 设置字符串类型的偏好设置。null 值会被转为空字符串。
        /// </summary>
        /// <param name="key">键名。</param>
        /// <param name="value">要存储的值。</param>
        public static void SetString(string key, string value)
            => PlayerPrefs.SetString(key, value ?? string.Empty);

        /// <summary>
        /// 读取字符串类型的偏好设置。
        /// </summary>
        /// <param name="key">键名。</param>
        /// <param name="defaultValue">键不存在时的默认值。</param>
        /// <returns>存储的字符串值。</returns>
        public static string GetString(string key, string defaultValue = "")
            => PlayerPrefs.GetString(key, defaultValue);

        /// <summary>
        /// 设置布尔类型的偏好设置（底层以整数存储）。
        /// </summary>
        /// <param name="key">键名。</param>
        /// <param name="value">要存储的值。</param>
        public static void SetBool(string key, bool value)
            => SetInt(key, value ? 1 : 0);

        /// <summary>
        /// 读取布尔类型的偏好设置（底层以整数存储）。
        /// </summary>
        /// <param name="key">键名。</param>
        /// <param name="defaultValue">键不存在时的默认值。</param>
        /// <returns>存储的布尔值。</returns>
        public static bool GetBool(string key, bool defaultValue = false)
            => GetInt(key, defaultValue ? 1 : 0) != 0;

        /// <summary>
        /// 检查指定键是否存在。
        /// </summary>
        /// <param name="key">键名。</param>
        /// <returns>存在时返回 true。</returns>
        public static bool HasKey(string key)
            => PlayerPrefs.HasKey(key);

        /// <summary>
        /// 删除指定键的偏好设置。
        /// </summary>
        /// <param name="key">要删除的键名。</param>
        public static void DeleteKey(string key)
            => PlayerPrefs.DeleteKey(key);

        /// <summary>
        /// 删除所有偏好设置。
        /// </summary>
        public static void DeleteAll()
            => PlayerPrefs.DeleteAll();

        /// <summary>
        /// 将所有未保存的偏好设置写入持久化存储。
        /// </summary>
        public static void Save()
            => PlayerPrefs.Save();

        /// <summary>
        /// 关闭偏好设置系统，先执行一次 Save 确保数据持久化。
        /// </summary>
        public static void Close()
        {
            Save();
        }
    }
}
