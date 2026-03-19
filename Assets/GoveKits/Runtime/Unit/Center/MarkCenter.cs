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
	/// 启动时可调用 <see cref="ScanAndRegisterFactories"/> 自动扫描并注册带
	/// <see cref="AutoUnitAttribute"/> 的 UnitMark 派生类。
	/// 创建实例时约定构造函数第一个参数为 IUnit owner，其余参数通过 params 传入。
	/// </remarks>
	public static class MarkCenter
	{
		private static readonly Dictionary<Type, Func<IUnit, object[], UnitMark>> _factories = new();
		private static bool _scanned;

		/// <summary>
		/// 扫描当前 AppDomain 并注册带 AutoUnitAttribute 的 UnitMark 派生类工厂。
		/// </summary>
		public static void ScanAndRegisterFactories()
		{
			if (_scanned)
			{
				return;
			}

			_factories.Clear();

			var markBaseType = typeof(UnitMark);
			foreach (var type in GetAllLoadableTypes())
			{
				if (type == null || type.IsAbstract || !markBaseType.IsAssignableFrom(type))
				{
					continue;
				}

				if (!Attribute.IsDefined(type, typeof(AutoUnitAttribute), false))
				{
					continue;
				}

				_factories[type] = BuildFactory(type);
			}

			_scanned = true;
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
				throw new InvalidOperationException(
					$"Mark type {markType.FullName} is not registered. Add [AutoUnitAttribute].");
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

				if (!IsOwnerParameterCompatible(ps[0].ParameterType, owner))
				{
					continue;
				}

				var ok = true;
				for (int j = 0; j < args.Length; j++)
				{
					if (!IsArgumentCompatible(ps[j + 1].ParameterType, args[j]))
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

		private static bool IsOwnerParameterCompatible(Type parameterType, IUnit owner)
		{
			if (owner == null)
			{
				return !parameterType.IsValueType;
			}

			return parameterType.IsInstanceOfType(owner);
		}

		private static bool IsArgumentCompatible(Type parameterType, object value)
		{
			if (value == null)
			{
				return !parameterType.IsValueType || Nullable.GetUnderlyingType(parameterType) != null;
			}

			return parameterType.IsInstanceOfType(value);
		}

		private static IEnumerable<Type> GetAllLoadableTypes()
		{
			var assemblies = AppDomain.CurrentDomain.GetAssemblies();
			for (int i = 0; i < assemblies.Length; i++)
			{
				Type[] types;
				try
				{
					types = assemblies[i].GetTypes();
				}
				catch (ReflectionTypeLoadException ex)
				{
					types = ex.Types.Where(t => t != null).ToArray();
				}

				for (int j = 0; j < types.Length; j++)
				{
					var type = types[j];
					if (type != null)
					{
						yield return type;
					}
				}
			}
		}
	}
}
