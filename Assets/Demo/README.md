GoveKits 演示

这个小型的运行时演示程序用于演练 Assets/GoveKits/Units 目录下的 Units 框架。

演示内容
•   DemoController — 通过编程方式创建两个单位 GameObject，设置属性，添加能力和效果，并运行一个简单的回合制战斗循环，同时将日志输出到控制台。

•   DemoAbilities — 基于 BaseAbility 构建的简单 DamageAbility 和 HealAbility 实现。

•   DemoMarks — AttributeMark 演示，该效果在应用/移除时会修改属性。

如何运行
1.  在 Unity 编辑器中打开项目。
2.  创建一个空场景（或使用任何现有场景）。将 DemoController 组件添加到场景中的一个空 GameObject 上。
3.  进入播放模式。查看控制台以了解演示运行情况（回合、HP 变化、效果、能力结果）。

说明
•   演示程序在运行时动态创建单位（无需修改场景或预制体）。

•   使用 Cysharp.Threading.Tasks (UniTask) 来驱动异步能力生命周期。

如果您需要，我也可以创建一个示例场景文件，用于预先放置 DemoController GameObject。