# Events

事件系统（EventBus/EventManager）：支持多总线、优先级、阻断与对象池复用。

## 关键概念
- EventManager：全局门面，管理多条 EventBus，并提供 Main 总线快捷入口。
- EventBus：事件作用域（如主世界/副本/UI），支持订阅、取消订阅与发布。
- EventChannel：按事件类型划分的内部分发列表，负责排序和广播。
- EventListener：监听器基类与委托监听器实现，支持优先级。
- EventInfo：事件数据基类，配合对象池回收（OnRecycle）。

## 用法示例
```csharp
// 订阅
var dispose = EventManager.Subscribe<MyEvent>(e => Debug.Log(e.Value), priority: EventPriority.AboveNormal);

// 发布
EventManager.Publish<MyEvent>(e => e.Value = 123);

// 取消订阅
dispose();
```

更多细节请查看源码的 XML 注释。