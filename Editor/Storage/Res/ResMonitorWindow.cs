using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using GoveKits.Runtime.Storage.Res;
using UnityEditor;
using UnityEngine;

namespace GoveKits.Editor.Storage.Res
{
	/// <summary>
	/// 资源缓存监控窗口。
	/// </summary>
	public sealed class ResMonitorWindow : EditorWindow
	{
		private sealed class CacheView
		{
			public string Key;
			public string AssetName;
			public string AssetType;
			public string LoadType;
			public int RefCount;
			public bool IsAssetNull;
		}

		private readonly List<CacheView> items = new();
		private Vector2 scroll;
		private string status = "Ready";

		[MenuItem("GoveKits/Storage/Res Monitor")]
		public static void ShowWindow()
		{
			GetWindow<ResMonitorWindow>("Res Monitor");
		}

		private void OnEnable()
		{
			RefreshData();
		}

		private void OnGUI()
		{
			DrawToolbar();
			EditorGUILayout.Space(6);
			EditorGUILayout.LabelField("Cached Entries", items.Count.ToString());
			EditorGUILayout.Space(6);
			DrawCacheList();
			EditorGUILayout.Space(8);
			EditorGUILayout.HelpBox(status, MessageType.None);
		}

		private void DrawToolbar()
		{
			EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
			GUILayout.Label("Res Monitor", EditorStyles.boldLabel);
			GUILayout.FlexibleSpace();

			if (GUILayout.Button("Refresh", EditorStyles.toolbarButton))
			{
				RefreshData();
			}

			Color old = GUI.color;
			GUI.color = new Color(1f, 0.45f, 0.45f);
			if (GUILayout.Button("Release All", EditorStyles.toolbarButton))
			{
				ReleaseAllByReflection();
				RefreshData();
			}
			GUI.color = old;

			EditorGUILayout.EndHorizontal();
		}

		private void DrawCacheList()
		{
			if (items.Count == 0)
			{
				EditorGUILayout.HelpBox("No cached assets.", MessageType.Info);
				return;
			}

			scroll = EditorGUILayout.BeginScrollView(scroll);
			for (int i = 0; i < items.Count; i++)
			{
				CacheView s = items[i];
				EditorGUILayout.BeginVertical("box");
				EditorGUILayout.LabelField("Key", s.Key ?? string.Empty);
				EditorGUILayout.LabelField("Asset", s.AssetName ?? "<null>");
				EditorGUILayout.LabelField("Type", s.AssetType ?? "<null>");
				EditorGUILayout.LabelField("LoadType", s.LoadType ?? "<null>");
				EditorGUILayout.LabelField("RefCount", s.RefCount.ToString());
				EditorGUILayout.LabelField("IsNull", s.IsAssetNull ? "Yes" : "No");
				EditorGUILayout.EndVertical();
			}
			EditorGUILayout.EndScrollView();
		}

		private void RefreshData()
		{
			items.Clear();

			if (!TryGetRawCaches(out IDictionary rawCaches, out string err))
			{
				status = err;
				Repaint();
				return;
			}

			foreach (DictionaryEntry pair in rawCaches)
			{
				string key = pair.Key as string ?? string.Empty;
				object entry = pair.Value;
				if (entry == null)
				{
					continue;
				}

				Type entryType = entry.GetType();
				FieldInfo assetField = entryType.GetField("Asset", BindingFlags.Public | BindingFlags.Instance);
				FieldInfo loadTypeField = entryType.GetField("LoadType", BindingFlags.Public | BindingFlags.Instance);
				FieldInfo refField = entryType.BaseType?.GetField("RefCount", BindingFlags.Public | BindingFlags.Instance);

				UnityEngine.Object asset = assetField?.GetValue(entry) as UnityEngine.Object;
				object loadType = loadTypeField?.GetValue(entry);
				int refCount = 0;
				if (refField?.GetValue(entry) is int rc)
				{
					refCount = rc;
				}

				items.Add(new CacheView
				{
					Key = key,
					AssetName = asset != null ? asset.name : "<null>",
					AssetType = asset != null ? asset.GetType().Name : "<null>",
					LoadType = loadType != null ? loadType.ToString() : "<null>",
					RefCount = refCount,
					IsAssetNull = asset == null,
				});
			}

			status = $"Refreshed {items.Count} cache entrie(s).";
			Repaint();
		}

		private static bool TryGetRawCaches(out IDictionary rawCaches, out string error)
		{
			rawCaches = null;
			error = string.Empty;

			FieldInfo cacheField = typeof(ResCore).GetField("Cache", BindingFlags.NonPublic | BindingFlags.Static);
			if (cacheField == null)
			{
				error = "ResCore.Cache field not found.";
				return false;
			}

			object cacheObj = cacheField.GetValue(null);
			if (cacheObj == null)
			{
				error = "ResCore.Cache is null.";
				return false;
			}

			FieldInfo mapField = cacheObj.GetType().GetField("_caches", BindingFlags.NonPublic | BindingFlags.Instance);
			if (mapField == null)
			{
				error = "CacheContainer._caches field not found.";
				return false;
			}

			rawCaches = mapField.GetValue(cacheObj) as IDictionary;
			if (rawCaches == null)
			{
				error = "Cache map unavailable.";
				return false;
			}

			return true;
		}

		private void ReleaseAllByReflection()
		{
			if (!TryGetRawCaches(out IDictionary rawCaches, out string error))
			{
				status = error;
				return;
			}

			FieldInfo cacheField = typeof(ResCore).GetField("Cache", BindingFlags.NonPublic | BindingFlags.Static);
			object cacheObj = cacheField?.GetValue(null);
			MethodInfo tryRemove = cacheObj?.GetType().GetMethod("TryRemove", BindingFlags.Public | BindingFlags.Instance);
			if (tryRemove == null)
			{
				status = "TryRemove method not found.";
				return;
			}

			// 逐键循环释放，直到缓存字典清空或达到保护上限。
			int removedLoops = 0;
			const int maxLoop = 10000;
			while (rawCaches.Count > 0 && removedLoops < maxLoop)
			{
				string[] keys = new string[rawCaches.Count];
				rawCaches.Keys.CopyTo(keys, 0);

				for (int i = 0; i < keys.Length; i++)
				{
					object[] args = { keys[i], null };
					tryRemove.Invoke(cacheObj, args);
				}

				removedLoops++;
			}

			status = rawCaches.Count == 0
				? "Release all completed."
				: "Release all reached max loop guard.";
		}
	}
}
