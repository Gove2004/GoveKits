# Window 模块（Editor 调试工具）

GoveKits 提供 13 个 Editor 调试窗口，覆盖 Util、Storage、Network、Unit 等子系统的实时监控与管理。所有窗口通过 `GoveKits` 顶级菜单下的子菜单项打开，仅在 Editor 模式下可用。

## 窗口一览

| 菜单路径 | 窗口 | 说明 | 模块 |
|---------|------|------|------|
| GoveKits/Core | **Pool 监控** | 查看所有对象池的容量、使用率和命中率 | Util/Pool |
| GoveKits/Event | **Event 监控** | 实时查看事件总线上的活跃事件和监听器 | Util/Event |
| GoveKits/Time | **TimeWheel 监控** | 列出所有活跃定时器，支持暂停/恢复/取消 | Util/Time |
| GoveKits/Spawn | **Spawn 监控** | 查看存活实体列表和注册工厂，支持手动销毁 | Util/Spawn |
| GoveKits/Save | **Save 浏览器** | 物理存档文件浏览器，支持 UTF-8/Hex 预览 | Storage/Save |
| GoveKits/Config | **Config 管理** | 配置表映射、加载状态查看和数据结构预览 | Storage/Config |
| GoveKits/Prefs | **Prefs 编辑器** | PlayerPrefs 键值编辑、常用列表管理 | Storage/Prefs |
| GoveKits/Hotfix | **Hotfix 管理** | 热更新程序集状态检查和 AOT 引用分析 | Storage/Res |
| GoveKits/Network | **Network 监控** | Mirror 客户端/服务端连接状态、RTT、踢人操作 | Network |
| GoveKits/Localization | **Localization 管理** | 翻译表查看、缺失检测和语言切换 | Storage/Localization |
| GoveKits/Unit | **Unit 检查器** | Inspector 中增强显示 Unit 的属性和技能数据 | Unit |
| GoveKits/Android | **Android 配置** | Android 项目引导配置工具 | Editor |
| GoveKits/Project | **Project 工具** | 项目路径工具（绝对路径/相对路径转换） | Editor |

## 各窗口详细说明

### Pool 监控（PoolWindow）

实时查看所有 CSharp 对象池和 GameObject 对象池的状态。

- **CSharp 池列表**：显示类型名、当前数量、最大容量、填充比例
- **GameObject 池列表**：显示预制体名、已实例化数量、最大容量
- **一键清空**：清除当前标签页的所有池
- **自动刷新**：Play 模式下每 0.5 秒自动刷新

### Event 监控（EventWindow）

实时查看事件总线上的活跃事件和监听器分布。

- **活跃事件卡片**：显示事件类型、数据内容、监听器数量
- **监听器行**：显示优先级、过滤器状态
- **搜索**：按事件类型名过滤
- **清除当前 Bus**：清空当前活跃事件

### TimeWheel 监控（TimeWindow）

反射读取 TimeCore 内部的 TimeWheel，列出所有活跃定时器。

- **轮盘信息**：当前 Tick、槽位数、Tick 间隔
- **定时器列表**：显示 ID、目标 Tick、间隔、循环次数
- **状态颜色**：绿色=活跃、黄色=暂停、红色=已取消、灰色=已完成
- **操作按钮**：暂停 / 恢复 / 取消
- **统计汇总**：总计、活跃、暂停、已取消

### Spawn 监控（SpawnWindow）

反射读取 SpawnCore 内部的工厂注册表和存活实体列表。

- **存活实体 Tab**：显示 ID、类型、SpawnKey，支持按 ID/类型/键搜索
- **注册工厂 Tab**：显示所有已注册的 SpawnKey 和工厂类型
- **销毁按钮**：手动 Despawn 指定实体

### Save 浏览器（SaveWindow）

物理存档文件浏览器，以目录树形式展示存档文件，支持 UTF-8 / Hex 预览。

- **目录树**：递归展示存档目录结构，支持折叠/展开
- **文件预览**：
  - Auto 模式：自动检测文本/二进制文件
  - UTF-8 模式：以文本形式展示
  - Hex 模式：以十六进制展示
- **文件操作**：定位到 Finder / 删除存档
- **容量限制**：预览最大 256KB，防止大文件卡死编辑器

### Config 管理（ConfigWindow）

配置表映射管理窗口，展示文件路径、加载状态和数据结构。

- **两种视图**：列表视图（按类型名排序）和文件夹视图（按目录分组）
- **状态指示**：绿色"正常" / 红色"缺失"
- **数据结构预览**：展开查看配置类的字段类型和名称
- **内存行数**：Play 模式下显示已加载的行数
- **搜索**：按配置类名过滤
- **复制报告**：一键复制所有配置表状态到剪贴板

### Prefs 编辑器（PrefsWindow）

PlayerPrefs 键值编辑器，支持类型安全的读写操作。

- **键值编辑器**：输入键名、选择类型（int/float/string/bool）、输入值、执行操作
- **常用键列表**：可添加/移除常用键，快速切换
- **操作按钮**：设置 / 读取 / 检查 / 删除
- **状态栏**：显示操作结果（成功/失败/键不存在）

### Hotfix 管理（HotfixWindow）

HybridCLR 热更新管理窗口，检查热更程序集和 AOT 引用。

- **配置区**：显示热更程序集名称、AOT 泛型引用文件
- **热更状态**：显示已加载的热更程序集及其类型数
- **AOT 状态**：显示 AOT 泛型元数据加载情况，缺失标红
- **一键复制**：复制配置报告到剪贴板

### Network 监控（NetworkWindow）

基于内置 Mirror 的网络监控窗口，同时展示 Client 和 Server 的连接状态。

- **客户端 Tab**：客户端激活状态、连接状态、RTT（颜色指示：<50ms 绿 / <150ms 黄 / 以上红）
- **服务端 Tab**：服务端激活状态、在线连接数、每个连接的地址与就绪状态
- **踢人按钮**：从编辑器踢出指定客户端
- **自动刷新**：Play 模式下每 0.5 秒自动刷新

### Localization 管理（LocalizationWindow）

多语言翻译表查看和管理窗口。

- **翻译表**：显示 Key 和各语言的翻译内容
- **搜索**：按 Key 或翻译内容过滤
- **缺失检测**：仅显示缺少翻译的条目
- **语言过滤**：按指定语言代码过滤
- **一键设置**：修改当前语言的翻译字段

### Unit 检查器（UnitWindow）

在 Unity Inspector 中增强显示 Unit 组件的详细数据。

- **核心数据监控**：显示属性、标记、技能、反应的统计摘要
- **清除按钮**：清除 Inspector 中的缓存数据
- **属性面板**：显示名称、当前值、基础值、修饰器列表
- **标记面板**：显示 Tag、堆叠数、剩余时间
- **技能面板**：显示名称、状态（执行中/空闲）、优先级
- **反应面板**：显示 Tag、优先级、处理条件

### Android 配置（AndroidWindow）

Android 隐私弹窗模板部署工具。从源目录复制模板文件到 Android Plugins 目录，支持文件夹浏览、路径验证、覆盖确认。

### Project 工具（ProjectWindows）

项目路径工具，提供绝对路径和相对路径的相互转换，以及 `.gitignore` 模板部署。

## 使用建议

1. **开发阶段**：开启所有窗口的自动刷新，实时监控各子系统状态
2. **调试阶段**：使用 Save 浏览器和 Config 管理检查数据文件，使用 Network 监控排查连接问题
3. **性能分析**：通过 Pool 监控观察对象池命中率，通过 Time 监控检查定时器数量
4. **热更排查**：使用 Hotfix 窗口检查 AOT 泛型引用是否完整
