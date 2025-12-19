# Excel2Json 工具

将 Excel 配置导出为 C# DTO 代码和 JSON 数据，支持分步或一键生成。

## 使用步骤
1. 打开窗口：GoveKits/Excel2Json。
2. 依次设置：Excel 目录、DTO 代码目录、JSON 目录、命名空间。
3. 可按需先“清空旧文件”，再选择“仅生成代码/仅生成 JSON/一键全部生成”。

## 目录建议
- Excel 源：`Assets/Config/Excel`
- DTO 输出：`Assets/Config/DTO`
- JSON 输出：`Assets/Resources/Config/Json`

## 注意事项
- 忽略临时文件（形如 `~$xxx.xlsx`）。
- 导出后 Unity 会刷新资源，等待编译完成再继续作业。
