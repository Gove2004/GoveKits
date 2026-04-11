// using System;
// using System.Collections;
// using System.Collections.Generic;
// using System.IO;
// using System.Reflection;
// using System.Text;
// using System.Threading;
// using GoveKits.Runtime.Storage.Config;
// using UnityEditor;
// using UnityEngine;

// namespace GoveKits.Editor.Storage.Config
// {
//     /// <summary>
//     /// 配置管理与体检窗口。
//     /// </summary>
//     public sealed class ConfigManagerWindow : EditorWindow
//     {
//         private enum ViewMode
//         {
//             Tables,
//             Folders,
//         }

//         private sealed class BindingView
//         {
//             public Type ConfigType;
//             public string FilePath;
//             public ConfigSourceType SourceType;
//             public ConfigFileType FileType;
//             public bool Exists;
//             public string ResolvedPath;
//             public int LoadedRows = -1;
//         }

//         private readonly List<BindingView> bindings = new();
//         private Vector2 scroll;
//         private ViewMode viewMode;
//         private bool onlyShowMissing;
//         private string status = "Ready";

//         [MenuItem("GoveKits/Storage/Config Manager")]
//         public static void ShowWindow()
//         {
//             GetWindow<ConfigManagerWindow>("Config Manager");
//         }

//         private void OnEnable()
//         {
//             RefreshBindings();
//         }

//         private void OnGUI()
//         {
//             DrawToolbar();
//             EditorGUILayout.Space(6);
//             DrawSummary();
//             EditorGUILayout.Space(6);

//             viewMode = (ViewMode)GUILayout.Toolbar((int)viewMode, new[] { "Tables", "Folders" });
//             scroll = EditorGUILayout.BeginScrollView(scroll);
//             if (viewMode == ViewMode.Tables)
//             {
//                 DrawTables();
//             }
//             else
//             {
//                 DrawFolders();
//             }
//             EditorGUILayout.EndScrollView();

//             EditorGUILayout.Space(8);
//             EditorGUILayout.HelpBox(status, MessageType.None);
//         }

//         private void DrawToolbar()
//         {
//             EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
//             GUILayout.Label("Config Manager", EditorStyles.boldLabel);
//             GUILayout.FlexibleSpace();

//             if (GUILayout.Button("Scan", EditorStyles.toolbarButton))
//             {
//                 RefreshBindings();
//             }

//             if (GUILayout.Button("Init", EditorStyles.toolbarButton))
//             {
//                 InitConfigCore();
//             }

//             if (GUILayout.Button("Validate", EditorStyles.toolbarButton))
//             {
//                 ValidateAll();
//             }

//             onlyShowMissing = GUILayout.Toggle(onlyShowMissing, "Only Missing", EditorStyles.toolbarButton);

//             if (GUILayout.Button("Copy Report", EditorStyles.toolbarButton))
//             {
//                 CopyReport();
//             }

//             EditorGUILayout.EndHorizontal();
//         }

//         private void DrawSummary()
//         {
//             int missing = 0;
//             for (int i = 0; i < bindings.Count; i++)
//             {
//                 if (!bindings[i].Exists)
//                 {
//                     missing++;
//                 }
//             }

//             EditorGUILayout.LabelField("Initialized", ConfigCore.Initialized ? "Yes" : "No");
//             EditorGUILayout.LabelField("Binding Count", bindings.Count.ToString());
//             EditorGUILayout.LabelField("Missing Files", missing.ToString());
//         }

//         private void DrawTables()
//         {
//             if (bindings.Count == 0)
//             {
//                 EditorGUILayout.HelpBox("No config binding found.", MessageType.Info);
//                 return;
//             }

//             for (int i = 0; i < bindings.Count; i++)
//             {
//                 BindingView b = bindings[i];
//                 if (onlyShowMissing && b.Exists)
//                 {
//                     continue;
//                 }

//                 EditorGUILayout.BeginVertical("box");
//                 EditorGUILayout.LabelField("Type", b.ConfigType != null ? b.ConfigType.FullName : "<null>");
//                 EditorGUILayout.LabelField("Path", b.FilePath ?? string.Empty);
//                 EditorGUILayout.LabelField("Source", b.SourceType.ToString());
//                 EditorGUILayout.LabelField("Format", b.FileType.ToString());
//                 EditorGUILayout.LabelField("Exists", b.Exists ? "Yes" : "No");
//                 if (b.LoadedRows >= 0)
//                 {
//                     EditorGUILayout.LabelField("Loaded Rows", b.LoadedRows.ToString());
//                 }

//                 EditorGUILayout.BeginHorizontal();
//                 if (GUILayout.Button("Ping Asset", GUILayout.Width(100f)))
//                 {
//                     PingBindingAsset(b);
//                 }

//                 if (GUILayout.Button("Open Location", GUILayout.Width(120f)))
//                 {
//                     OpenBindingLocation(b);
//                 }

//                 if (GUILayout.Button("Count Rows", GUILayout.Width(100f)))
//                 {
//                     b.LoadedRows = QueryLoadedRows(b.ConfigType);
//                     status = b.LoadedRows >= 0
//                         ? $"{b.ConfigType.Name}: {b.LoadedRows} row(s)."
//                         : $"{b.ConfigType.Name}: not initialized.";
//                 }
//                 EditorGUILayout.EndHorizontal();

//                 EditorGUILayout.EndVertical();
//             }
//         }

//         private void DrawFolders()
//         {
//             if (bindings.Count == 0)
//             {
//                 EditorGUILayout.HelpBox("No config binding found.", MessageType.Info);
//                 return;
//             }

//             Dictionary<string, List<BindingView>> groups = new(StringComparer.OrdinalIgnoreCase);
//             for (int i = 0; i < bindings.Count; i++)
//             {
//                 BindingView b = bindings[i];
//                 if (onlyShowMissing && b.Exists)
//                 {
//                     continue;
//                 }

//                 string folder = Path.GetDirectoryName((b.FilePath ?? string.Empty).Replace('\\', '/')) ?? "<root>";
//                 if (!groups.TryGetValue(folder, out List<BindingView> list))
//                 {
//                     list = new List<BindingView>();
//                     groups[folder] = list;
//                 }

//                 list.Add(b);
//             }

//             foreach (var pair in groups)
//             {
//                 EditorGUILayout.BeginVertical("box");
//                 EditorGUILayout.LabelField(pair.Key, EditorStyles.boldLabel);
//                 for (int i = 0; i < pair.Value.Count; i++)
//                 {
//                     BindingView b = pair.Value[i];
//                     string state = b.Exists ? "OK" : "Missing";
//                     EditorGUILayout.LabelField($"- {b.ConfigType.Name} [{state}]");
//                 }
//                 EditorGUILayout.EndVertical();
//             }
//         }

//         private void RefreshBindings()
//         {
//             bindings.Clear();

//             List<(Type type, ConfigAttribute attr)> scanned = ScanBindings();
//             for (int i = 0; i < scanned.Count; i++)
//             {
//                 var item = scanned[i];
//                 ResolveBindingLocation(item.attr, out bool exists, out string resolvedPath);
//                 bindings.Add(new BindingView
//                 {
//                     ConfigType = item.type,
//                     FilePath = item.attr.FilePath,
//                     SourceType = item.attr.SourceType,
//                     FileType = item.attr.ParseType,
//                     Exists = exists,
//                     ResolvedPath = resolvedPath,
//                 });
//             }

//             status = $"Scanned {bindings.Count} binding(s).";
//             Repaint();
//         }

//         private void ValidateAll()
//         {
//             int missing = 0;
//             for (int i = 0; i < bindings.Count; i++)
//             {
//                 BindingView b = bindings[i];
//                 ResolveBindingLocation(new ConfigAttribute(b.FilePath, b.FileType, b.SourceType), out bool exists, out string resolvedPath);
//                 b.Exists = exists;
//                 b.ResolvedPath = resolvedPath;
//                 if (!exists)
//                 {
//                     missing++;
//                 }
//             }

//             status = missing == 0 ? "Validation passed." : $"Validation done. Missing {missing} file(s).";
//         }

//         private void CopyReport()
//         {
//             StringBuilder sb = new StringBuilder(512);
//             sb.AppendLine("[Config Manager Report]");
//             sb.Append("Initialized: ").AppendLine(ConfigCore.Initialized ? "Yes" : "No");
//             sb.Append("Binding Count: ").AppendLine(bindings.Count.ToString());

//             int missing = 0;
//             for (int i = 0; i < bindings.Count; i++)
//             {
//                 if (!bindings[i].Exists)
//                 {
//                     missing++;
//                 }
//             }

//             sb.Append("Missing: ").AppendLine(missing.ToString());
//             sb.AppendLine("Details:");

//             for (int i = 0; i < bindings.Count; i++)
//             {
//                 BindingView b = bindings[i];
//                 sb.Append("- ")
//                     .Append(b.ConfigType != null ? b.ConfigType.FullName : "<null>")
//                     .Append(" | ")
//                     .Append(b.SourceType)
//                     .Append(" | ")
//                     .Append(b.FileType)
//                     .Append(" | ")
//                     .Append(b.Exists ? "OK" : "Missing")
//                     .Append(" | ")
//                     .AppendLine(b.FilePath ?? string.Empty);
//             }

//             EditorGUIUtility.systemCopyBuffer = sb.ToString();
//             status = "Report copied to clipboard.";
//         }

//         private static List<(Type type, ConfigAttribute attr)> ScanBindings()
//         {
//             var result = new List<(Type type, ConfigAttribute attr)>();

//             Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
//             for (int i = 0; i < assemblies.Length; i++)
//             {
//                 Assembly asm = assemblies[i];
//                 if (asm == null || asm.IsDynamic)
//                 {
//                     continue;
//                 }

//                 Type[] types;
//                 try
//                 {
//                     types = asm.GetTypes();
//                 }
//                 catch (ReflectionTypeLoadException ex)
//                 {
//                     types = ex.Types;
//                 }

//                 if (types == null)
//                 {
//                     continue;
//                 }

//                 for (int j = 0; j < types.Length; j++)
//                 {
//                     Type type = types[j];
//                     if (type == null || type.IsAbstract || type.IsInterface)
//                     {
//                         continue;
//                     }

//                     if (!typeof(IConfigData).IsAssignableFrom(type))
//                     {
//                         continue;
//                     }

//                     ConfigAttribute attr = type.GetCustomAttribute<ConfigAttribute>(false);
//                     if (attr == null || string.IsNullOrWhiteSpace(attr.FilePath))
//                     {
//                         continue;
//                     }

//                     result.Add((type, attr));
//                 }
//             }

//             return result;
//         }

//         private static void ResolveBindingLocation(ConfigAttribute attr, out bool exists, out string resolvedPath)
//         {
//             exists = false;
//             resolvedPath = string.Empty;

//             if (attr.SourceType == ConfigSourceType.Resources)
//             {
//                 string resourcePath = NormalizeResourcePath(attr.FilePath);
//                 TextAsset asset = Resources.Load<TextAsset>(resourcePath);
//                 if (asset != null)
//                 {
//                     exists = true;
//                     resolvedPath = AssetDatabase.GetAssetPath(asset);
//                 }
//                 else
//                 {
//                     resolvedPath = $"Resources:{resourcePath}";
//                 }

//                 return;
//             }

//             string fullPath = Path.Combine(Application.streamingAssetsPath, attr.FilePath.Replace('\\', '/').TrimStart('/'));
//             exists = File.Exists(fullPath);
//             resolvedPath = fullPath;
//         }

//         private static void PingBindingAsset(BindingView b)
//         {
//             if (string.IsNullOrEmpty(b.ResolvedPath))
//             {
//                 return;
//             }

//             UnityEngine.Object obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(b.ResolvedPath);
//             if (obj != null)
//             {
//                 EditorGUIUtility.PingObject(obj);
//             }
//         }

//         private static void OpenBindingLocation(BindingView b)
//         {
//             if (string.IsNullOrEmpty(b.ResolvedPath))
//             {
//                 return;
//             }

//             if (File.Exists(b.ResolvedPath))
//             {
//                 EditorUtility.RevealInFinder(b.ResolvedPath);
//                 return;
//             }

//             UnityEngine.Object obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(b.ResolvedPath);
//             if (obj != null)
//             {
//                 string path = AssetDatabase.GetAssetPath(obj);
//                 if (!string.IsNullOrEmpty(path))
//                 {
//                     EditorUtility.RevealInFinder(path);
//                 }
//             }
//         }

//         private static int QueryLoadedRows(Type configType)
//         {
//             if (!ConfigCore.Initialized)
//             {
//                 return -1;
//             }

//             MethodInfo loadAllMethod = typeof(ConfigCore).GetMethod(nameof(ConfigCore.LoadAll), BindingFlags.Public | BindingFlags.Static);
//             if (loadAllMethod == null)
//             {
//                 return -1;
//             }

//             MethodInfo generic = loadAllMethod.MakeGenericMethod(configType);
//             object value = generic.Invoke(null, null);
//             if (value is IList list)
//             {
//                 return list.Count;
//             }

//             return -1;
//         }

//         private void InitConfigCore()
//         {
//             try
//             {
//                 MethodInfo initMethod = typeof(ConfigCore).GetMethod(nameof(ConfigCore.InitAsync), new[] { typeof(CancellationToken) });
//                 if (initMethod == null)
//                 {
//                     status = "InitAsync not found.";
//                     return;
//                 }

//                 object uniTask = initMethod.Invoke(null, new object[] { CancellationToken.None });
//                 if (uniTask != null)
//                 {
//                     MethodInfo asTask = uniTask.GetType().GetMethod("AsTask", Type.EmptyTypes);
//                     if (asTask != null)
//                     {
//                         var task = asTask.Invoke(uniTask, null) as System.Threading.Tasks.Task;
//                         task?.GetAwaiter().GetResult();
//                     }
//                 }

//                 status = "Config init completed.";
//                 Repaint();
//             }
//             catch (Exception ex)
//             {
//                 status = $"Config init failed: {ex.Message}";
//             }
//         }

//         private static string NormalizeResourcePath(string path)
//         {
//             string normalized = (path ?? string.Empty).Replace('\\', '/').TrimStart('/');
//             string ext = Path.GetExtension(normalized);
//             if (!string.IsNullOrEmpty(ext))
//             {
//                 normalized = normalized.Substring(0, normalized.Length - ext.Length);
//             }

//             return normalized;
//         }
//     }
// }
