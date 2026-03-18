using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace GoveKits.Runtime.Unit
{
	/// <summary>
	/// Mark 工厂中心。
	/// </summary>
	/// <remarks>
	/// 启动时可调用 <see cref="ScanAndRegisterFactories"/> 自动扫描并注册所有 UnitMark 派生类。
	/// 创建实例时约定构造函数第一个参数为 IUnit owner，其余参数通过 params 传入。
	/// </remarks>
	public static class MarkCenter
	{
		private static readonly Dictionary<Type, Func<IUnit, object[], UnitMark>> _factories = new();
		private static bool _scanned;

		/// <summary>
		/// 扫描当前 AppDomain 内所有 UnitMark 派生类并注册工厂。
		/// </summary>
		public static void ScanAndRegisterFactories()
		{
			if (_scanned)
			{
				return;
			}

			var markBaseType = typeof(UnitMark);
			var assemblies = AppDomain.CurrentDomain.GetAssemblies();

			for (int i = 0; i < assemblies.Length; i++)
			{
				var assembly = assemblies[i];
				Type[] types;

				try
				{
					types = assembly.GetTypes();
				}
				catch (ReflectionTypeLoadException ex)
				{
					types = ex.Types.Where(t => t != null).ToArray();
				}

				for (int j = 0; j < types.Length; j++)
				{
					var type = types[j];
					if (type == null || type.IsAbstract || !markBaseType.IsAssignableFrom(type))
					{
						continue;
					}

					RegisterFactory(type);
				}
			}

			_scanned = true;
		}

		/// <summary>
		/// 手动注册指定 Mark 类型工厂。
		/// </summary>
		/// <param name="markType">Mark 类型。</param>
		public static void RegisterFactory(Type markType)
		{
			if (markType == null)
			{
				throw new ArgumentNullException(nameof(markType));
			}

			if (!typeof(UnitMark).IsAssignableFrom(markType) || markType.IsAbstract)
			{
				throw new ArgumentException($"Type {markType.FullName} is not a valid UnitMark.", nameof(markType));
			}

			_factories[markType] = BuildFactory(markType);
		}

		/// <summary>
		/// 手动注册泛型 Mark 工厂。
		/// </summary>
		public static void RegisterFactory<TMark>() where TMark : UnitMark
		{
			RegisterFactory(typeof(TMark));
		}

		/// <summary>
		/// 判断某个 Mark 类型是否已注册。
		/// </summary>
		public static bool IsRegistered<TMark>() where TMark : UnitMark
		{
			return _factories.ContainsKey(typeof(TMark));
		}

		/// <summary>
		/// 按类型创建 Mark。
		/// </summary>
		/// <param name="markType">Mark 类型。</param>
		/// <param name="owner">归属 Unit。</param>
		/// <param name="args">除 owner 外的构造参数。</param>
		public static UnitMark Create(Type markType, IUnit owner, params object[] args)
		{
			if (markType == null)
			{
				throw new ArgumentNullException(nameof(markType));
			}

			EnsureScanned();

			if (!_factories.TryGetValue(markType, out var factory))
			{
				RegisterFactory(markType);
				factory = _factories[markType];
			}

			return factory(owner, args ?? Array.Empty<object>());
		}

		/// <summary>
		/// 按泛型类型创建 Mark。
		/// </summary>
		public static TMark Create<TMark>(IUnit owner, params object[] args) where TMark : UnitMark
		{
			return (TMark)Create(typeof(TMark), owner, args);
		}

		private static void EnsureScanned()
		{
			if (!_scanned)
			{
				ScanAndRegisterFactories();
			}
		}

		private static Func<IUnit, object[], UnitMark> BuildFactory(Type markType)
		{
			var constructors = markType.GetConstructors(BindingFlags.Public | BindingFlags.Instance);
			if (constructors.Length == 0)
			{
				throw new InvalidOperationException($"Mark type {markType.FullName} has no public constructor.");
			}

			return (owner, args) =>
			{
				var callArgs = args ?? Array.Empty<object>();
				var match = FindBestConstructor(constructors, owner, callArgs);
				if (match == null)
				{
					throw new InvalidOperationException(
						$"No matching constructor found for {markType.FullName}. " +
						"Expected first parameter to be IUnit and remaining parameters to match provided args.");
				}

				var invokeArgs = new object[callArgs.Length + 1];
				invokeArgs[0] = owner;
				Array.Copy(callArgs, 0, invokeArgs, 1, callArgs.Length);

				return (UnitMark)match.Invoke(invokeArgs);
			};
		}

		private static ConstructorInfo FindBestConstructor(ConstructorInfo[] constructors, IUnit owner, object[] args)
		{
			for (int i = 0; i < constructors.Length; i++)
			{
				var ctor = constructors[i];
				var ps = ctor.GetParameters();

				if (ps.Length != args.Length + 1)
				{
					continue;
				}

				if (!ps[0].ParameterType.IsAssignableFrom(owner?.GetType() ?? typeof(IUnit)))
				{
					if (owner != null || !ps[0].ParameterType.IsAssignableFrom(typeof(IUnit)))
					{
						continue;
					}
				}

				var ok = true;
				for (int j = 0; j < args.Length; j++)
				{
					var arg = args[j];
					var pType = ps[j + 1].ParameterType;

					if (arg == null)
					{
						if (pType.IsValueType && Nullable.GetUnderlyingType(pType) == null)
						{
							ok = false;
							break;
						}

						continue;
					}

					if (!pType.IsInstanceOfType(arg))
					{
						ok = false;
						break;
					}
				}

				if (ok)
				{
					return ctor;
				}
			}

			return null;
		}
	}
}
