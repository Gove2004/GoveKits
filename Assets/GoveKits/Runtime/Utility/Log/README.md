# Log

日志管理工具：提供带标签、多色彩、条件编译与上下文支持的控制台输出。

## 特点
- 编辑器专属：信息/彩色日志仅在编辑器输出（条件编译 `UNITY_EDITOR`）。
- 警告/错误无条件输出（即使发布版也显示）。
- 标签显示为粗体，易于识别日志来源。
- 支持上下文定位：点击日志可跳转到指定物体。
- 多色快捷方法：绿/红/黄/青/洋红/蓝。

## 快速用法
```csharp
LogManager.Log("MyTag", "普通日志");
LogManager.LogGreen("MyTag", "成功消息");
LogManager.LogRed("MyTag", "错误消息", someGameObject);  // 带上下文
LogManager.LogWarning("MyTag", "警告信息");
LogManager.LogError("MyTag", "错误信息");  // 无条件输出
```

更多 API 详见源码 XML 注释。