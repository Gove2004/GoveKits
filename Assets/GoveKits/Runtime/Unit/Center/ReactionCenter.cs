using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace GoveKits.Runtime.Unit
{
	/// <summary>
	/// Reaction 工厂中心。
	/// </summary>
	/// <remarks>
	/// 仅扫描并注册带 <see cref="AutoUnitAttribute"/> 的 UnitReaction 派生类。
	/// </remarks>
	public static class ReactionCenter
	{
		private static readonly Dictionary<Type, Func<IUnit, object[], UnitReaction>> _factories = new();
		private static bool _scanned;

		/// <summary>
		/// 扫描并注册 Reaction 工厂。
		/// </summary>
		public static void ScanAndRegisterFactories()
		{
			if (_scanned)
			{
				return;
			}

			_factories.Clear();
			var reactionBaseType = typeof(UnitReaction);
			foreach (var type in GetAllLoadableTypes())
			{
				if (type == null || type.IsAbstract || !reactionBaseType.IsAssignableFrom(type))
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
		/// 按类型创建 Reaction。
		/// </summary>
		public static UnitReaction Create(Type reactionType, IUnit owner, params object[] args)
		{
			if (reactionType == null)
			{
				throw new ArgumentNullException(nameof(reactionType));
			}

			EnsureScanned();
			if (!_factories.TryGetValue(reactionType, out var factory))
			{
				throw new InvalidOperationException(
					$"Reaction type {reactionType.FullName} is not registered. Add [FactoryAutoRegister].");
			}

			return factory(owner, args ?? Array.Empty<object>());
		}

		/// <summary>
		/// 按泛型类型创建 Reaction。
		/// </summary>
		public static TReaction Create<TReaction>(IUnit owner, params object[] args) where TReaction : UnitReaction
		{
			return (TReaction)Create(typeof(TReaction), owner, args);
		}

		private static void EnsureScanned()
		{
			if (!_scanned)
			{
				ScanAndRegisterFactories();
			}
		}

		private static Func<IUnit, object[], UnitReaction> BuildFactory(Type reactionType)
		{
			var constructors = reactionType.GetConstructors(BindingFlags.Public | BindingFlags.Instance);
			if (constructors.Length == 0)
			{
				throw new InvalidOperationException($"Reaction type {reactionType.FullName} has no public constructor.");
			}

			return (owner, args) =>
			{
				var callArgs = args ?? Array.Empty<object>();
				var match = FindBestConstructor(constructors, owner, callArgs);
				if (match == null)
				{
					throw new InvalidOperationException(
						$"No matching constructor found for {reactionType.FullName}. " +
						"Expected first parameter to be IUnit owner and remaining parameters to match provided args.");
				}

				var invokeArgs = new object[callArgs.Length + 1];
				invokeArgs[0] = owner;
				Array.Copy(callArgs, 0, invokeArgs, 1, callArgs.Length);
				return (UnitReaction)match.Invoke(invokeArgs);
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
