# UI 模块

完整的 UI 系统实现：包括栈式面板导航框架（Panel）和响应式架构参考（MVI）。

## 模块结构

### [Panel](Panel/README.md)

**栈式面板导航系统**：基于 Android Activity 生命周期设计，支持异步过渡动画与弹窗管理。

**核心组件**：
- **UIController**：管理面板栈、驱动生命周期、处理导航
- **BasePanel**：面板基类，提供完整的生命周期钩子
- **IPanelLifeCycle**：面板生命周期接口（内部接口）
- **PanelEvent**：面板事件，用于全局监听面板状态变化
- **UIItem**：UI 项目基类，用于组织面板内的元素

**特性**：
- 栈式管理：支持 Push/Pop 操作与返回导航
- 生命周期完整：OnCreate/OnStart/OnResume/OnPause/OnStop/OnFinish
- 弹窗支持：特殊面板可不隐藏下方内容
- 异步动画：支持淡入/淡出等过渡效果
- 参数传递：Show 时传递数据给新面板

**适用场景**：主菜单、游戏界面、设置面板、弹窗、加载界面等

---

### [MVI](MVI/README.md)

**MVI 架构参考实现**：Model-View-Intent 模式的完整架构骨架（大部分为注释状态，提供模板）。

**核心概念**：
- 单向数据流：Intent → Model → State → View
- 响应式更新：状态变化自动触发 UI 更新
- 关注点分离：Model（数据）、View（UI）、System（逻辑）独立

**核心组件**：
- **Module / IModule**：模块生命周期基类
- **IState**：状态基类
- **IIntent**：意图接口
- **Model**（注释）：状态管理与业务逻辑
- **View**（注释）：UI 渲染与输入响应
- **System**（注释）：协调 Model/View 的系统
- **App**（注释）：全局应用容器

**特性**：
- 架构清晰：单向数据流，易于调试与测试
- 模块化：Model 独立于 UI，便于单元测试
- 可扩展：支持注册多个子系统

**适用场景**：复杂 UI 逻辑、可测试架构、响应式 UI 系统

---

## 使用指南

### Panel 系统快速开始

1. **创建面板**：继承 `BasePanel` 并重写生命周期方法
2. **配置 UIController**：在 Canvas 下创建 UIController 脚本
3. **注册面板**：在 Inspector 中将所有面板拖入 uiPanelsArray
4. **导航**：使用 `uiController.Show<T>(payload)` 打开面板

### MVI 系统使用（可选）

1. **参考示例**：查看 Example.cs 中的完整架构示例
2. **定义组件**：创建 State、Intent、Model、View、System 子类
3. **集成应用**：注册系统到全局 App 并启动

---

## 架构图

```
UIController (管理栈)
  ├─ BasePanel A (OnCreate → OnStart → OnResume → OnPause → OnStop)
  ├─ BasePanel B
  └─ BasePanel C (isPopup=true)
      └─ MVI System (内部状态管理)
          ├─ Model (状态)
          ├─ View (UI)
          └─ System (逻辑)
```

---

## 最佳实践

1. **面板设计**：单一职责，一个面板对应一个功能模块
2. **生命周期**：充分利用各阶段的特性（创建、激活、暂停、销毁）
3. **数据传递**：通过 Show 参数或事件系统传递跨模块数据
4. **性能优化**：
   - OnCreate 中做一次性初始化
   - OnStop 中释放重资源
   - 使用对象池管理频繁创建的 UI 元素
5. **调试**：订阅 PanelEvent 进行全局监听与埋点

---

## 相关文档

- [Utility/Events 事件系统](../../Utility/Events/README.md)
- [Utility/Pools 对象池](../../Utility/Pools/README.md)
