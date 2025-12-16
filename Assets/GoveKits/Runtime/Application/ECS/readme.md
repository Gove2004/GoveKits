

```C#
using UnityEngine;
using GoveKits.ECS;

// 1. 定义数据 (不需要继承任何类)
public class Position { public Vector3 Value; }
public class Velocity { public Vector3 Value; }

// 2. 定义逻辑
public class MovementSystem : GoveKits.ECS.System
{
    private Filter _filter;

    public override void OnInitialize()
    {
        // 获取所有同时拥有 Position 和 Velocity 的实体
        _filter = World.GetFilter(new[] { typeof(Position), typeof(Velocity) });
    }

    public override void OnUpdate(float dt)
    {
        foreach (var entity in _filter.Entities)
        {
            var pos = World.GetComponent<Position>(entity);
            var vel = World.GetComponent<Velocity>(entity);
            
            pos.Value += vel.Value * dt;
            
            // 在 Unity 中可以通过这种方式简单的Debug
            // Debug.Log($"Entity {entity.ID} moved to {pos.Value}");
        }
    }
}

// 3. Unity 入口
public class GameBootstrap : MonoBehaviour
{
    private World _world;
    private SystemGroup _systems;

    void Start()
    {
        _world = new World();
        _systems = new SystemGroup();

        // 注册系统
        _systems.Add(new MovementSystem(), _world);

        // 创建 100 个实体
        for (int i = 0; i < 100; i++)
        {
            var e = _world.CreateEntity();
            _world.AddComponent(e, new Position { Value = Vector3.zero });
            _world.AddComponent(e, new Velocity { Value = new Vector3(0, 1, 0) });
        }
    }

    void Update()
    {
        _systems.Update(Time.deltaTime);
    }

    void OnDestroy()
    {
        _systems.Destroy();
    }
}
```