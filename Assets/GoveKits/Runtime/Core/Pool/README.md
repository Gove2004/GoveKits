# Pool

## 概述

这是一套统一的对象池系统，分为两类：

- `CSharpPool<T>`: 管理纯 C# 对象。
- `GameObjectPool`: 管理 Unity `GameObject` 实例。

对外统一通过 `PoolCore` 调用。

适用场景：

- 高频创建和回收的临时数据对象
- 子弹、特效、敌人等可复用的场景对象
- 希望减少 `new` / `Instantiate` / `Destroy` 带来的运行时开销

## 核心接口

### IPoolable

所有池化对象都需要实现 `IPoolable`：

```csharp
public interface IPoolable
{
	void OnRecycle();
}
```

调用时机：

- `OnRecycle()`: 对象归还到池中时调用

推荐用途：

- 在 `OnRecycle()` 中清理引用和运行时脏数据

## 纯 C# 对象池

### 适合什么

适合不依赖 Unity 生命周期的对象，例如：

- 战斗结算数据
- 路径节点
- 技能上下文对象
- 临时计算对象

### 使用方式

```csharp
using GoveKits.Runtime.Core.Pool;

public class EnemyData : IPoolable
{
	public int Id;
	public float Hp;

	public void OnRecycle()
	{
		Id = 0;
		Hp = 0f;
	}
}
```

```csharp
PoolCore.Create<EnemyData>(count: 8, maxSize: 64);

EnemyData enemy = PoolCore.Get<EnemyData>();
enemy.Id = 1;
enemy.Hp = 100f;

PoolCore.Return(enemy);
```

### 要求

- 必须是引用类型
- 必须实现 `IPoolable`
- 必须有无参构造函数

也就是泛型约束：

```csharp
where T : class, IPoolable, new()
```

## GameObject 对象池

### 适合什么

适合会频繁生成和回收的场景对象，例如：

- 子弹
- 特效
- 敌人
- 掉落物

### prefab 要求

传入 `PoolCore.Create(prefab)` 或 `PoolCore.Get(prefab)` 的 prefab，至少要满足：

- prefab 本体上必须挂有一个实现了 `IPoolable` 的组件

例如：

```csharp
using GoveKits.Runtime.Core.Pool;
using UnityEngine;

public class Bullet : MonoBehaviour, IPoolable
{
	public void OnRecycle()
	{
	}
}
```

### 使用方式

```csharp
using GoveKits.Runtime.Core.Pool;
using UnityEngine;

public class BulletShooter : MonoBehaviour
{
	[SerializeField] private GameObject bulletPrefab;

	private void Start()
	{
		PoolCore.Create(bulletPrefab, count: 16, maxSize: 64);
	}

	private void Update()
	{
		if (Input.GetKeyDown(KeyCode.Space))
		{
			GameObject bullet = PoolCore.Get(bulletPrefab);
			bullet.transform.position = transform.position;
		}
	}
}
```

归还时：

```csharp
PoolCore.Return(bulletGameObject);
```

或：

```csharp
bulletGameObject.ReturnToPool();
```

### 内部行为

`GameObjectPool` 在内部会：

- 池空时根据 prefab `Instantiate` 新对象
- 自动给实例添加 `PoolRecord`
- 取出对象时激活 `GameObject`
- 归还对象时调用所有 `IPoolable` 组件的 `OnRecycle()`
- 然后将对象设为失活状态

## 常用 API

### 创建或获取池

```csharp
PoolCore.Create<MyData>(count: 8, maxSize: 64);
PoolCore.Create(prefab, count: 8, maxSize: 64);
```

### 取对象

```csharp
MyData data = PoolCore.Get<MyData>();
GameObject obj = PoolCore.Get(prefab);
```

### 归还对象

```csharp
PoolCore.Return(data);
PoolCore.Return(obj);
```

### 清空指定池

```csharp
PoolCore.Clear<MyData>();
PoolCore.Clear(prefab);
```

### 清空全部池

```csharp
PoolCore.ClearAll();
```

## 扩展方法

为了方便使用，系统提供了 `ReturnToPool()`：

```csharp
data.ReturnToPool();
gameObject.ReturnToPool();
```

## 重要说明

### 1. Create 参数只在首次创建时生效

如果某个类型或 prefab 的池已经创建过，再次调用 `Create` 不会重新应用新的 `count` 和 `maxSize`。

如果你想改配置，建议：

```csharp
PoolCore.Clear<MyData>();
PoolCore.Create<MyData>(count: 32, maxSize: 128);
```

或：

```csharp
PoolCore.Clear(prefab);
PoolCore.Create(prefab, count: 32, maxSize: 128);
```

### 2. 非池对象不要调用 Return

`PoolCore.Return(GameObject obj)` 依赖实例上的 `PoolRecord` 找回来源池。

这意味着：

- 从池中拿出来的对象可以正常归还
- 不是由池创建出来的普通对象，不应该调用 `Return`

### 3. GameObject prefab 必须挂 IPoolable 组件

当前实现里，`CheckPrefab` 会检查对象上是否存在 `IPoolable` 组件。

如果没有，会抛出异常。

### 4. C# 池对象必须可 `new()`

纯 C# 池在池空时会直接 `new T()`，所以不能用于没有无参构造函数的类型。

## 推荐实践

### 纯 C# 对象

- 把可复用但生命周期很短的对象放进 `CSharpPool<T>`
- 在 `OnRecycle()` 里清空引用类型字段
- 不要在池对象里持有长期外部引用

### GameObject 对象

- 让 prefab 上至少有一个主要脚本实现 `IPoolable`
- 在业务初始化代码里设置位置、速度、计时器、动画状态
- 在 `OnRecycle()` 中停止粒子、协程、Tween、事件监听并清理状态
- 只归还由池生成的对象实例

## 一个完整的 Bullet 示例

```csharp
using GoveKits.Runtime.Core.Pool;
using UnityEngine;

public class Bullet : MonoBehaviour, IPoolable
{
	[SerializeField] private float speed = 10f;
	[SerializeField] private float lifeTime = 2f;

	private float _timer;
	private Vector3 _direction;

	public void Fire(Vector3 position, Vector3 direction)
	{
		transform.position = position;
		_direction = direction.normalized;
		_timer = lifeTime;
	}

	private void Update()
	{
		transform.position += _direction * speed * Time.deltaTime;
		_timer -= Time.deltaTime;

		if (_timer <= 0f)
		{
			gameObject.ReturnToPool();
		}
	}

	public void OnRecycle()
	{
		_timer = 0f;
		_direction = Vector3.zero;
	}
}
```

## 总结

如果你只记住 3 件事：

1. 纯数据对象用 `PoolCore.Get<T>() / Return(T)`
2. prefab 对象用 `PoolCore.Get(prefab) / Return(GameObject)`
3. 所有池化对象都要实现 `IPoolable`
