---
name: govekits
description: GoveKits Unity 游戏框架（v3.0.0）的使用指南。当项目引用 GoveKits 包、需要用其门面类（GoveCore/EventCore/UICore/ResCore/SaveCore/UnitCore 等）写游戏逻辑时使用。包含启动流程、各模块正确用法、MVVM 纪律与池化规则。
---

# GoveKits 框架使用指南

可复用 Unity 游戏框架。五模块：Util（基础工具）、Network（Mirror + Http）、Storage（资源/配置/存档/音频/多语言）、UI（MVVM）、Unit（类 GAS 能力系统）。

## 硬性纪律（违反即错）

1. **门面模式**：一切通过静态 `*Core` 类调用，不 new 框架内部对象。启动调 `GoveCore.Setup()`，退出调 `GoveCore.Close()`。Setup 幂等（已内置 Domain Reload 防护）。
2. **命名空间**：业务代码 `using GoveKits.Runtime.{Util|Network|Storage|UI|Unit};` 即可。
3. **池化对象禁止手动 new**：
   - 事件：`EventCore.Pick<T>()` 取 → 填字段 → `EventCore.Publish(evt)`（发布后自动回收）。自定义事件必须继承 `EventData` 并在 `OnRecycle()` 中重置全部字段，否则复用时带脏数据。
   - 效果：`XxxEffect.Create().Set(...).Apply(target)` 即用即还。
4. **UI MVVM 纪律**：View 侧交互回调只能「取参 → 调一个 VM 方法」，导航与业务全在 ViewModel 里。一个 ViewPanel<TVM> 对应一个 VM，VM 由 UICore 按类型单例持有。
5. **Unit 事件链**：技能在 `ExecuteAsync` 里用 `EventCore.Publish` 发事件；被动继承 `UnitReaction<T>` 订阅同一事件。**不要引入 Intent 投递模式**（已废弃回退）。

## 启动模板

```csharp
public class GameEntry : MonoBehaviour
{
    async void Start()
    {
        GoveCore.Setup();                              // 日志/事件/时间轮/对象池
        ConfigCore.AddParser(new JsonConfigParser());  // 按需注册配置解析器
        ConfigCore.Setup();
        await ResCore.InitPackageAsync(new AutoOfflinePackageConfig("Main"));  // 或 AutoHostPackageConfig 热更
        AudioCore.Setup();
        LocalizationCore.Setup();
    }
    void OnDestroy() => GoveCore.Close();
}
```

## 模块速查

### Util（Event / Time / Pool / Log / Spawn / Singleton）

```csharp
EventCore.Publish(evt);                                          // 池化事件发布
var t1 = TimeCore.Once(2f, () => Debug.Log("ok"));               // 单次定时
var t2 = TimeCore.Loop(1f, () => Debug.Log("tick"));             // 循环定时（loopCount=-1 无限）
// 注意：时间轮无内置驱动，需在驱动处每帧调 TimeCore.Tick(Time.deltaTime)
var obj = PoolCore.Get<Bullet>(); PoolCore.Return(obj);          // C# 对象池（IPoolable）
var go = PoolCore.Get(prefab); PoolCore.Return(go);              // GameObject 池（跨场景存活）
LogCore.Info("Tag", "msg");                                      // 多后端日志
```

### UI（MVVM，详见 Runtime/UI/README.md）

```csharp
// ViewModel：数据 + 导航
public class LoginVM : ViewModel {
    public string UserName;
    public void Login(string pwd) { /* 业务 */ UICore.Hide<LoginPanel>(); UICore.Show<MainMenuPanel>(); }
    protected override void OnInit() { }
}
// View：只转发
loginButton.onClick.AddListener(() => VM.Login(pwdInput.text));
// 面板：ViewPanel<LoginVM> 挂 UI 根节点，OnEnable 自动绑定 VM；VM 变更用 Notify(key) 推送
UICore.Show<LoginPanel>(); UICore.Hide<LoginPanel>();            // Show 支持 param 传参 → OnReceiveShowParam
```

### Storage（Res / Config / Save / Audio / Localization，详见 README.md）

```csharp
var icon = await ResCore.LoadAssetAsync<Sprite>("Assets/UI/icon.png");  // 句柄必须 ResCore.Release(icon)
var go = await ResCore.InstantiateAsync("Assets/Prefabs/Enemy.prefab"); // 内部自动释放
var all = ConfigCore.LoadAll<ItemConfig>();                             // [ConfigPath] 标注的类型
SaveCore.Save("player.dat", data);  var d = SaveCore.Load<PlayerSave>("player.dat");  // 原子写入
AudioCore.PlayBGM(clip, 1f);  AudioCore.PlayDynamic(AudioChannel.SFX, clip, position: pos);
LocalizationCore.GetText("ui.tip");  LocalizationCore.SwitchLanguage(LanguageCode.ChineseCN);
```

### Network（Http / Mirror，详见 README.md）

```csharp
var resp = await HttpCore.GetAsync("https://api.example.com");   // 全方法支持 CancellationToken
if (resp.IsSuccess) Debug.Log(resp.Text);
// Mirror：场景挂 NetworkManager，网络脚本继承 NetworkBehaviour（SyncVar/Command/Rpc）
```

### Unit（类 GAS，详见 README.md，有完整「伤害+冷却+流血」示例）

```csharp
// 单位继承 UnitBehaviour（Awake 自建四容器）；世界容器用 Universe 单例
// 属性
unit.Attributes.Add("hp", 100);  float hp = unit.GetValue("hp");   // 取值用 IUnit 扩展
// 技能：继承 UnitAbility，ExecuteAsync 里 EventCore.Publish(DamageEvent)
hero.Abilities.AddAbility(UnitCore.CreateAbility<SlashAbility>());
await hero.Abilities.TryExecuteAsync("slash", new AbilityContext(hero, enemy));
// 被动：继承 UnitReaction<T>（OnFilter 过滤 / OnEvent 响应），挂上即监听
enemy.Reactions.AddReaction(UnitCore.CreateReaction<BleedReaction>());
// 效果（池化）
AttributeChangeEffect.Create().Set("hp", -10f).Apply(target);
// Buff：继承 TickMark，OnApply 里 base.OnApply() 必调
```

## 高频坑位（review 实测踩过）

| 坑 | 正确做法 |
|---|---|
| 事件对象复用脏数据 | `EventData.OnRecycle()` 里重置所有字段 |
| 资源句柄泄漏 | `LoadAssetAsync` 句柄用完 `ResCore.Release()`；实例化用 `InstantiateAsync`（自动） |
| `Attributes.GetValue("hp")` 编译不过 | 容器只有带 Func 的重载；单参用 `unit.GetValue("hp")`（IUnit 扩展） |
| TickMark 子类忘 base | `OnApply` 必须调 `base.OnApply()`（重置池化计时器） |
| SaveCore 未 Setup 就读写 | 先 `SaveCore.Setup(null)`（默认 JSON 序列化器） |
| 属性回滚失效 | `RemoveModifier(tag, new XxxSource())` 按类型移除；按引用需持有原 Source |

## 详细文档

本目录（GoveKitsDocs/）集中了全部模块文档，写业务前先读对应文件：`Util.md` / `UI.md` / `Storage.md` / `Network.md` / `Unit.md`（每份含一句话定位 / 架构 / 完整使用代码）。
