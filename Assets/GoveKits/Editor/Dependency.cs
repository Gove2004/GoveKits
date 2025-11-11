// 把以下代码保存为 DependencyHelper.cs 放到你的工作集中
#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class DependencyHelper
{
    // ==== 在这里添加你的依赖 ====
    private static readonly (string name, string url, string testType)[] Dependencies = 
    {
        ("UniTask", "https://github.com/Cysharp/UniTask.git", "Cysharp.Threading.Tasks.UniTask, UniTask"),
        ("DoTween", "com.demigiant.dotween", "DG.Tweening.DOTween, DG.Tweening"),
        ("Newtonsoft Json", "com.unity.nuget.newtonsoft-json", "Newtonsoft.Json.JsonConvert, Newtonsoft.Json"),
        // 添加新依赖：复制一行，修改三个参数即可
        // ("依赖名", "包地址", "检测用的类型全名")
    };
    // ============================
    
    [MenuItem("GoveKits/Dependency/Install Dependencies")]
    public static void InstallAll()
    {
        foreach (var dep in Dependencies)
        {
            if (!IsInstalled(dep.testType))
            {
                Debug.Log($"安装 {dep.name}...");
                UnityEditor.PackageManager.Client.Add(dep.url);
            }
        }
        EditorUtility.DisplayDialog("完成", "依赖安装完成！请等待包管理器刷新", "确定");
    }
    
    [MenuItem("GoveKits/Dependency/Check Dependencies")]
    public static void CheckAll()
    {
        var missing = new List<string>();
        
        foreach (var dep in Dependencies)
        {
            if (!IsInstalled(dep.testType))
                missing.Add(dep.name);
        }
        
        if (missing.Count == 0)
            EditorUtility.DisplayDialog("依赖检查", "✅ 所有依赖已安装", "确定");
        else
            EditorUtility.DisplayDialog("依赖检查", $"❌ 缺少: {string.Join(", ", missing)}", "确定");
    }
    
    private static bool IsInstalled(string typeName) => System.Type.GetType(typeName) != null;
}
#endif