# Project 工具

用于初始化项目目录结构、创建 `.gitignore`，并附带清除 `PlayerPrefs` 的快捷入口。

## 功能
- 目录结构初始化：读取模板文件并批量创建目录。
- `.gitignore` 生成：根据模板写入到项目根目录。
- 清除 PlayerPrefs：一键清理本地偏好。

## 使用步骤
1. 打开窗口：GoveKits/Project。
2. 配置“目录模板文件”与“.gitignore 模板文件”。
3. 点击对应按钮执行操作，完成后查看 Console 提示。

## 模板说明
- 目录模板：每行一个相对路径，空行与 `#` 注释会被忽略。
- `.gitignore` 模板：常规 Git 忽略规则文本。
