# Plugins Module

Plugins 目录用于记录第三方依赖来源与接入方式。

## 设计理念

- 依赖可追溯: 每个第三方库都记录来源地址与接入方式。
- 升级可控: 降低版本升级风险与人员交接成本。
- 边界清晰: 第三方代码尽量不直接改写，业务逻辑在 Runtime/Editor 完成。

## 架构介绍

当前常见依赖:

- Newtonsoft.Json
- UniTask
- ExcelDataReader
- Google.Protobuf

## 快速开始

1. 先确认依赖是否已内置（build in）。
2. 对需要外部拉取的库按文档安装。
3. 使用前检查 asmdef 依赖与命名空间引用。

## 依赖来源

- Newtonsoft.Json: https://github.com/JamesNK/Newtonsoft.Json
- UniTask: https://github.com/Cysharp/UniTask
- ExcelDataReader: https://github.com/ExcelDataReader/ExcelDataReader
- Google.Protobuf: https://github.com/protocolbuffers/protobuf

## 相关跳转

- Root: [../../../README.md](../../../README.md)
- Runtime Core: [../Runtime/Core/README.md](../Runtime/Core/README.md)
- Runtime Unit: [../Runtime/Unit/README.md](../Runtime/Unit/README.md)
