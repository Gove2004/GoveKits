# Plugins 开发手册

Plugins 目录用于管理第三方依赖来源与使用边界，目标是可追踪、可升级、可回滚。

## 设计理念

- 依赖来源明确，版本可追踪。
- 第三方代码尽量保持原样，业务封装放在 Runtime/Editor。
- 升级必须可验证、可回退。

## 架构介绍

当前常用依赖包括:

- Cysharp.Threading.Tasks (UniTask)
- Newtonsoft.Json
- Google.Protobuf

## 注意事项

- 不直接修改第三方源码。
- 升级依赖前先验证 asmdef 兼容性。
- 引入新依赖时补充用途与版本说明。

## 常见故障排查

- 现象: 编译提示命名空间找不到。
    - 排查: 检查对应依赖是否已导入，asmdef 是否声明引用。
- 现象: 升级后运行时报错。
    - 排查: 回查版本变更记录并在分支中做最小回归验证。
- 现象: 同一库在不同模块表现不一致。
    - 排查: 检查是否混用了不同版本 DLL 或重复引用路径。

## 相关跳转

- Root: [../../../README.md](../../../README.md)
- Runtime Core: [../Runtime/Core/README.md](../Runtime/Core/README.md)
- Runtime Unit: [../Runtime/Unit/README.md](../Runtime/Unit/README.md)
- 术语与命名规范: [../../../TERMINOLOGY.md](../../../TERMINOLOGY.md)



