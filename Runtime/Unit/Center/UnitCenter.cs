using System;
using UnityEngine;

namespace GoveKits.Runtime.Unit
{
	/// <summary>
	/// Unit 子系统总入口。
	/// </summary>
	/// <remarks>
	/// 负责统一初始化 Ability/Mark/Reaction 三类工厂中心。
	/// </remarks>
	public static class UnitCenter
	{
		private static bool _initialized;

		/// <summary>
		/// 手动初始化所有工厂中心（仅注解扫描）。
		/// </summary>
		public static void Initialize()
		{
			if (_initialized)
			{
				return;
			}

			AbilityCenter.ScanAndRegisterFactories();
			MarkCenter.ScanAndRegisterFactories();
			ReactionCenter.ScanAndRegisterFactories();

			_initialized = true;
		}

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
		private static void AutoInitialize()
		{
			Initialize();
		}
	}


    /// <summary>
    /// 标记类型可被扫描并自动注册到对应 Center。
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class AutoUnitAttribute : Attribute
    {
    }
}


