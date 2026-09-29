---
name: govekits
description: GoveKits Unity 游戏框架（v3.0.0）使用指南。当项目引用 GoveKits 包，需要用其门面类（GoveCore/EventCore/UICore/ResCore/SaveCore/UnitCore 等）编写游戏逻辑、搭建界面/存档/资源/网络/技能系统时使用。提供启动流程、硬性纪律、高频坑位对照，以及按需加载的各模块详细文档（references/）。
agent_created: true
---

# GoveKits 框架使用指南

GoveKits 是可复用 Unity 游戏框架，含五个模块：Util（事件/时间轮/对象池/日志/生成）、Storage（YooAsset 资源、配置表、存档、音频、多语言）、UI（MVVM）、Network（Mirror + HTTP）、Unit（类 GAS 能力系统）。

本技能供 AI 在 GoveKits 项目中编写业务代码时使用：先按下方「硬性纪律」约束实现方式，再按「按需加载文档」读取对应模块的 references 文档获取完整用法。

## 硬性纪律（违反即错）

1. **门面模式**：一切通过静态 `*Core` 类调用，不 new 框架内部对象。启动调 `GoveCore.Setup()`，退出调 `GoveCore.Close()`。Setup 幂等（已内置 Domain Reload 防护）。
2. **命名空间**：业务代码 `using GoveKits.Runtime.{Util|Network|Storage|UI|Unit};`。
3. **池化对象禁止手动 new**：
   - 事件：`EventCore.Pick<T>()` 取 → 填字段 → `EventCore.Publish(evt)`（发布后自动回收）。自定义事件继承 `EventData`，必须在 `OnRecycle()` 重置全部字段，否则池化复用带脏数据。
   - 效果：`XxxEffect.Create().Set(...).Apply(target)` 即用即还。
4. **UI MVVM 纪律**：View 侧交互回调只允许「取参 → 调一个 VM 方法」；导航与业务全在 ViewModel。一个 `ViewPanel<TVM>` 对应一个 VM，VM 由 UICore 按类型单例持有。
5. **Unit 事件链**：技能在 `ExecuteAsync` 内用 `EventCore.Publish` 发事件；被动继承 `UnitReaction<T>` 订阅同一事件。禁止引入 Intent 投递模式（已废弃回退）。
6. **时间轮需手动驱动**：每帧调 `TimeCore.Tick(Time.deltaTime)`（框架无内置驱动器）。

## 启动模板

```csharp
public class GameEntry : MonoBehaviour
{
    async void Start()
    {
        GoveCore.Setup();                              // 日志/事件/时间轮/对象池
        ConfigCore.AddParser(new JsonConfigParser());  // 按需注册配置解析器
        ConfigCore.Setup();
        await ResCore.InitPackageAsync(new AutoOfflinePackageConfig("Main"));  // 热更用 AutoHostPackageConfig
        AudioCore.Setup();
        LocalizationCore.Setup();
    }
    void OnDestroy() => GoveCore.Close();
}
```

## 按需加载文档

编写某模块业务前，先读取对应 references 文档（含架构说明与完整使用代码）：

| 任务 | 加载 |
|---|---|
| 定时器 / 对象池 / 事件 / 日志 / 实体生成 | `references/Util.md` |
| 界面、面板、ViewModel、UIItem | `references/UI.md` |
| 资源加载 / 配置表 / 存档 / 音频 / 多语言 | `references/Storage.md` |
| HTTP 请求 / Mirror 联机 | `references/Network.md` |
| 属性 / 技能 / Buff / 反应 / 效果 / 单位存档 | `references/Unit.md`（含完整「伤害+冷却+流血」示例） |

## 高频坑位（review 实测踩过）

| 坑 | 正确做法 |
|---|---|
| 事件对象复用脏数据 | `EventData.OnRecycle()` 里重置所有字段 |
| 资源句柄泄漏 | `LoadAssetAsync` 句柄用完 `ResCore.Release()`；实例化用 `InstantiateAsync`（自动释放） |
| `Attributes.GetValue("hp")` 编译不过 | 容器只有带 Func 的重载；单参用 `unit.GetValue("hp")`（IUnit 扩展） |
| TickMark 子类忘调 base | `OnApply` 必须调 `base.OnApply()`（重置池化计时器） |
| SaveCore 未 Setup 就读写 | 先 `SaveCore.Setup(null)`（默认 JSON 序列化器） |
| 属性回滚失效 | `RemoveModifier(tag, new XxxSource())` 按类型移除；按引用需持有原 Source |
| 旧存档读档失败 | `UnitArchiveData` 已改为存 BaseValue+Modifiers，旧格式不兼容 |
