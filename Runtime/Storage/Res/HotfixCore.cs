using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Cysharp.Threading.Tasks;
using GoveKits.Runtime.Core;
using HybridCLR;
using UnityEngine;
using YooAsset;

namespace GoveKits.Runtime.Storage
{
    /// <summary>
    /// 热更新程序核心，封装 HybridCLR 热更加载流程，包括 AOT 泛型元数据加载、
    /// 热更程序集加载和入口方法调用。
    /// </summary>
    public static class HotfixCore
    {
        private static readonly Dictionary<string, Assembly> _hotfixAssemblies = new();

        /// <summary>
        /// 批量加载 AOT 泛型元数据。
        /// </summary>
        /// <param name="dllNames">需要加载元数据的 DLL 文件名列表。</param>
        /// <param name="packageName">可选的包裹名前缀，用于 ResCore 定位资源。</param>
        /// <returns>全部加载成功时返回 true。</returns>
        public static async UniTask<bool> LoadAotMetadataAsync(IReadOnlyList<string> dllNames, string packageName = "")
        {
            for (int i = 0; i < dllNames.Count; i++)
            {
                string location = string.IsNullOrEmpty(packageName)
                    ? dllNames[i]
                    : $"{packageName}:{dllNames[i]}";

                if (!await LoadAotMetadataInternal(location))
                {
                    LogCore.Error(nameof(HotfixCore), $"批量加载 AOT 中断，失败文件: {dllNames[i]}");
                    return false;
                }
            }
            return true;
        }

        private static async UniTask<bool> LoadAotMetadataInternal(string location)
        {
#if !UNITY_EDITOR
            var handle = ResCore.LoadAssetAsync<TextAsset>(location);
            await handle.Task;

            if (handle.Status != EOperationStatus.Succeed)
            {
                LogCore.Error(nameof(HotfixCore), $"AOT 元数据加载失败: {location}");
                ResCore.Release(handle);
                return false;
            }

            TextAsset textAsset = handle.AssetObject as TextAsset;
            byte[] dllBytes = textAsset.bytes;
            ResCore.Release(handle);

            LoadImageErrorCode err = RuntimeApi.LoadMetadataForAOTAssembly(dllBytes, HomologousImageMode.SuperSet);
            if (err != LoadImageErrorCode.OK)
            {
                LogCore.Error(nameof(HotfixCore), $"AOT 元数据补充失败: {location} 错误码: {err}");
                return false;
            }

            LogCore.Success(nameof(HotfixCore), $"AOT 元数据补充成功: {location}");
            return true;
#else
            await UniTask.CompletedTask;
            LogCore.Info(nameof(HotfixCore), $"编辑器模式跳过 AOT 元数据补充: {location}");
            return true;
#endif
        }

        /// <summary>
        /// 加载热更新程序集。
        /// </summary>
        /// <param name="location">程序集资源位置。</param>
        /// <returns>加载成功的程序集对象，失败时返回 null。</returns>
        public static async UniTask<Assembly> LoadHotfixAssemblyAsync(string location)
        {
            string assemblyName = Path.GetFileNameWithoutExtension(location);
            if (assemblyName.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                assemblyName = Path.GetFileNameWithoutExtension(assemblyName);

#if !UNITY_EDITOR
            var handle = ResCore.LoadAssetAsync<TextAsset>(location);
            await UniTask.WaitUntil(() => handle.IsDone);

            if (handle.Status != EOperationStatus.Succeed)
            {
                LogCore.Error(nameof(HotfixCore), $"热更程序集加载失败: {location}");
                ResCore.Release(handle);
                return null;
            }

            TextAsset textAsset = handle.AssetObject as TextAsset;
            byte[] dllBytes = textAsset.bytes;
            ResCore.Release(handle);

            try
            {
                Assembly ass = Assembly.Load(dllBytes);
                _hotfixAssemblies[ass.GetName().Name] = ass;
                LogCore.Success(nameof(HotfixCore), $"热更程序集真实加载成功: {ass.GetName().Name}");
                return ass;
            }
            catch (Exception ex)
            {
                LogCore.Error(nameof(HotfixCore), $"Assembly.Load 异常: {location}\n{ex.Message}");
                return null;
            }
#else
            await UniTask.CompletedTask;
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var ass in assemblies)
            {
                if (ass.GetName().Name == assemblyName)
                {
                    _hotfixAssemblies[assemblyName] = ass;
                    LogCore.Info(nameof(HotfixCore), $"编辑器模式映射热更程序集成功: {assemblyName}");
                    return ass;
                }
            }

            LogCore.Error(nameof(HotfixCore), $"编辑器下未找到名为 {assemblyName} 的程序集！请检查 Assembly Definition 配置。");
            return null;
#endif
        }

        /// <summary>
        /// 启动热更入口方法。通过反射查找并调用指定程序集中的静态方法。
        /// </summary>
        /// <param name="assemblyName">程序集名称。</param>
        /// <param name="className">类名。</param>
        /// <param name="methodName">静态方法名。</param>
        /// <param name="args">方法参数。</param>
        /// <returns>成功调用时返回 true。</returns>
        public static bool StartEntryMethod(string assemblyName, string className, string methodName, params object[] args)
        {
            if (!_hotfixAssemblies.TryGetValue(assemblyName, out Assembly ass))
            {
                LogCore.Error(nameof(HotfixCore), $"启动失败：未找到已加载的程序集 {assemblyName}");
                return false;
            }

            Type type = ass.GetType(className);
            if (type == null)
            {
                LogCore.Error(nameof(HotfixCore), $"启动失败：未在程序集 {assemblyName} 中找到类 {className}");
                return false;
            }

            MethodInfo method = type.GetMethod(methodName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (method == null)
            {
                LogCore.Error(nameof(HotfixCore), $"启动失败：未在类 {className} 中找到静态方法 {methodName}");
                return false;
            }

            try
            {
                method.Invoke(null, args);
                LogCore.Success(nameof(HotfixCore), $"成功拉起热更入口: {className}.{methodName}()");
                return true;
            }
            catch (Exception ex)
            {
                LogCore.Error(nameof(HotfixCore), $"热更入口执行异常: {className}.{methodName}()\n{ex}");
                return false;
            }
        }

        /// <summary>
        /// 获取已加载的热更程序集。
        /// </summary>
        /// <param name="assemblyName">程序集名称。</param>
        /// <returns>找到的程序集对象，未找到时返回 null。</returns>
        public static Assembly GetAssembly(string assemblyName)
        {
            if (!_hotfixAssemblies.TryGetValue(assemblyName, out Assembly ass))
            {
                LogCore.Error(nameof(HotfixCore), $"未找到已加载的程序集 {assemblyName}");
                return null;
            }
            return ass;
        }

        /// <summary>
        /// 关闭热更新系统，清空已加载的程序集缓存。
        /// </summary>
        public static void Close()
        {
            _hotfixAssemblies.Clear();
        }
    }
}
