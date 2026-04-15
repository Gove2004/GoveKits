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
    public static class HotfixCore
    {
        private static readonly Dictionary<string, Assembly> _hotfixAssemblies = new();

        /// <summary>
        /// 1. 加载并补充 AOT 泛型元数据
        /// </summary>
        public static async UniTask<bool> LoadAotMetadataAsync(IReadOnlyList<string> dllNames, string packageName = "")
        {
            for (int i = 0; i < dllNames.Count; i++)
            {
                // 如果 packageName 为空，依赖 ResCore 默认包语法
                string location = string.IsNullOrEmpty(packageName) 
                    ? $"{dllNames[i]}" 
                    : $"{packageName}:{dllNames[i]}";

                if (!await LoadAotMetadataAsync(location))
                {
                    LogCore.Error(nameof(HotfixCore), $"批量加载 AOT 中断，失败文件: {dllNames[i]}");
                    return false;
                }
            }
            return true;
        }
        public static async UniTask<bool> LoadAotMetadataAsync(string location)
        {

#if !UNITY_EDITOR
            // ================== 真机 IL2CPP 模式 ==================
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

            // 补充元数据 (HomologousImageMode.SuperSet 是官方推荐模式)
            LoadImageErrorCode err = RuntimeApi.LoadMetadataForAOTAssembly(dllBytes, HomologousImageMode.SuperSet);
            if (err != LoadImageErrorCode.OK)
            {
                LogCore.Error(nameof(HotfixCore), $"AOT 元数据补充失败: {location} 错误码: {err}");
                return false;
            }

            LogCore.Success(nameof(HotfixCore), $"AOT 元数据补充成功: {location}");
            return true;
#else
            // ================== 编辑器模式 ==================
            // 编辑器下基于 Mono 运行，不存在 AOT 泛型裁剪问题，直接跳过即可！
            await UniTask.CompletedTask;
            LogCore.Info(nameof(HotfixCore), $"编辑器模式跳过 AOT 元数据补充: {location}");
            return true;
#endif
        }

        /// <summary>
        /// 2. 加载热更程序集 (Hotfix Assembly)
        /// </summary>
        public static async UniTask<Assembly> LoadHotfixAssemblyAsync(string location)
        {
            // 解析我们要加载的程序集名字，比如从 "Default:Hotfix.dll.bytes" 中提取出 "Hotfix"
            string assemblyName = Path.GetFileNameWithoutExtension(location);
            if (assemblyName.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
            {
                assemblyName = Path.GetFileNameWithoutExtension(assemblyName);
            }

#if !UNITY_EDITOR
            // ================== 真机 IL2CPP 模式 ==================
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
            // ================== 编辑器模式 ==================
            // ⚠️ 核心操作：编辑器下，代码已经被 Unity 编译并加载到当前 AppDomain 了！
            // 所以我们绝对不能去读 bytes，而是直接在内存里把它找出来！
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
        /// 3. 进入热更逻辑主入口
        /// </summary>
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
        /// 获取已加载的程序集
        /// </summary>
        public static Assembly GetAssembly(string assemblyName)
        {
            if (!_hotfixAssemblies.TryGetValue(assemblyName, out Assembly ass))
            {
                LogCore.Error(nameof(HotfixCore), $"未找到已加载的程序集 {assemblyName}");
                return null;
            }

            return ass;
        }

        public static void Clear()
        {
            _hotfixAssemblies.Clear();
        }
    }
}