# Protobuf 代码生成器

通过 `protoc` 将 `.proto` 生成 C# 代码。

## 使用步骤
1. 打开窗口：GoveKits/Protobuf Generator。
2. 配置 `protoc.exe` 路径、`.proto` 文件路径、输出目录。
3. 点击“生成 C# 代码”。成功后 Unity 将自动刷新工程。

## 路径建议
- `protoc.exe` 可放置于项目 `Assets/Plugins/`。
- 输出目录建议位于 `Assets/Scripts/Generated/` 等受版本管理目录。

## 常见问题
- 若 `.proto` 内使用 import，请确保 `--proto_path` 指向其父目录。
- 生成失败请查看 Console 中的详细错误输出。
