# Plugins 开发文档

Plugins 记录第三方依赖来源与接入方式。

## 设计理念

- 依赖来源可追踪。
- 第三方代码尽量保持原样。
- 业务封装写在 Runtime/Editor 层。

## 架构介绍

当前依赖:

- Newtonsoft.Json
- UniTask
- ExcelDataReader
- Google.Protobuf

## 快速开始

1. 先确认依赖是否已内置。
2. 若需外部安装，按仓库地址与版本说明拉取。
3. 通过 asmdef 声明依赖后再使用。

```csharp
// UniTask 示例
using Cysharp.Threading.Tasks;

public async UniTask DemoDelayAsync()
{
	await UniTask.Delay(100);
}
```

## 注意事项

- 不建议直接修改第三方源码。
- 升级版本前先在分支验证兼容性。

## 相关跳转

- Root: [../../../README.md](../../../README.md)
- Runtime Core: [../Runtime/Core/README.md](../Runtime/Core/README.md)
- Runtime Unit: [../Runtime/Unit/README.md](../Runtime/Unit/README.md)
