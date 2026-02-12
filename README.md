# Synchron

<div align="center">

![Synchron Logo](https://img.shields.io/badge/Synchron-File%20Sync%20Tool-blue?style=for-the-badge)

**高性能文件同步工具**

[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?style=flat-square&logo=dotnet)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/badge/License-MIT-green?style=flat-square)](LICENSE)
[![Build](https://img.shields.io/badge/Build-Passing-brightgreen?style=flat-square)]()
[![Tests](https://img.shields.io/badge/Tests-62%20Passed-success?style=flat-square)]()

[English](#english) | [中文文档](#中文文档)

</div>

---

## 中文文档

### 目录

- [项目介绍](#项目介绍)
- [环境要求](#环境要求)
- [安装步骤](#安装步骤)
- [使用方法](#使用方法)
- [开发指南](#开发指南)
- [维护指南](#维护指南)
- [贡献说明](#贡献说明)

---

## 项目介绍

### 核心功能

**Synchron** 是一款专为 Windows 平台设计的高性能文件同步工具，采用模块化架构设计，灵感来源于 FastCopy 和 Windows 文件记录功能。项目将核心处理逻辑封装为独立的 C# 动态库，由控制台程序外壳负责调用和用户交互。

#### 主要特性

| 特性 | 描述 |
|------|------|
| 🚀 **高性能同步** | 采用异步 I/O 和缓冲优化，支持大文件和大量小文件的高效传输 |
| 🔄 **多种同步模式** | 支持 Diff（增量）、Sync（同步）、Move（移动）、Mirror（镜像）四种模式 |
| 👀 **实时监控** | 基于 FileSystemWatcher 的目录变化实时监控与自动同步 |
| 🎯 **智能过滤** | 支持通配符和正则表达式的文件包含/排除规则 |
| 📊 **进度报告** | 实时同步进度反馈，包括文件数量、传输速度等统计信息 |
| ⚙️ **灵活配置** | 支持 JSON 配置文件和命令行参数双重配置方式 |
| 📝 **详细日志** | 多级别日志系统，支持控制台彩色输出和文件日志 |
| 🔒 **安全可靠** | 支持文件校验、重试机制和错误处理 |

### 设计理念

```
┌─────────────────────────────────────────────────────────────┐
│                     Synchron 架构设计                        │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  ┌─────────────────┐       ┌─────────────────────────────┐ │
│  │  Console Shell  │       │       Synchron.Core         │ │
│  │  (用户交互层)    │──────▶│       (核心处理层)           │ │
│  └─────────────────┘       └─────────────────────────────┘ │
│         │                              │                    │
│         │                              │                    │
│         ▼                              ▼                    │
│  ┌─────────────────┐       ┌─────────────────────────────┐ │
│  │ Command Parser  │       │  ┌───────┐  ┌───────────┐   │ │
│  │ Interactive Menu│       │  │Logger │  │FileFilter │   │ │
│  └─────────────────┘       │  └───────┘  └───────────┘   │ │
│                            │  ┌───────────┐ ┌──────────┐  │ │
│                            │  │SyncEngine │ │FileWatch │  │ │
│                            │  └───────────┘ └──────────┘  │ │
│                            │  ┌───────────────────────┐   │ │
│                            │  │    ConfigManager      │   │ │
│                            │  └───────────────────────┘   │ │
│                            └─────────────────────────────┘ │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

### 目标用户群体

- **开发人员**：代码备份、项目同步、构建产物分发
- **系统管理员**：服务器文件同步、日志归档、配置分发
- **普通用户**：文件夹备份、数据迁移、文档同步

### 与同类工具对比

| 功能特性 | Synchron | FastCopy | Robocopy | rsync |
|---------|----------|----------|----------|-------|
| 开源免费 | ✅ | ✅ | ✅ | ✅ |
| 跨平台 | ❌ (Windows) | ❌ (Windows) | ❌ (Windows) | ✅ |
| 实时监控 | ✅ | ❌ | ❌ | ❌ |
| GitIgnore 集成 | ✅ | ❌ | ❌ | ❌ |
| 配置文件 | ✅ JSON | ❌ | ❌ | ✅ |
| 模块化设计 | ✅ | ❌ | ❌ | ❌ |
| .NET 原生 | ✅ | ❌ | ❌ | ❌ |
| 命令行界面 | ✅ | ✅ | ✅ | ✅ |
| 交互式菜单 | ✅ | ❌ | ❌ | ❌ |

---

## 环境要求

### 系统要求

| 项目 | 最低要求 | 推荐配置 |
|------|---------|---------|
| 操作系统 | Windows 10 (1809+) | Windows 11 |
| 运行时 | .NET 8.0 Runtime | .NET 8.0 SDK |
| 内存 | 512 MB | 2 GB+ |
| 磁盘空间 | 50 MB | 100 MB+ |

### 软件依赖

#### 运行时依赖

- **.NET 8.0 Runtime** - [下载地址](https://dotnet.microsoft.com/download/dotnet/8.0)

#### 开发依赖（仅开发时需要）

- **.NET 8.0 SDK** - 包含编译器和开发工具
- **Visual Studio 2022** (17.8+) 或 **VS Code** - 推荐IDE
- **Git** - 版本控制

### 版本兼容性

```
.NET 8.0  ──────────────────────────────────────▶
          │
          ├── Synchron 1.0.x (当前版本)
          │
          └── 支持所有 .NET 8.0 兼容平台
```

---

## 安装步骤

### 方式一：下载预编译版本

```powershell
# 1. 从 Releases 页面下载最新版本
# https://github.com/your-repo/synchron/releases

# 2. 解压到目标目录
Expand-Archive -Path synchron-v1.0.0.zip -DestinationPath C:\Tools\Synchron

# 3. 添加到系统 PATH（可选）
$env:PATH += ";C:\Tools\Synchron"

# 4. 验证安装
.\Synchron.exe --version
```

### 方式二：从源码编译

```powershell
# 1. 克隆仓库
git clone https://github.com/your-repo/synchron.git
cd synchron

# 2. 还原依赖
dotnet restore

# 3. 编译项目
dotnet build --configuration Release

# 4. 运行测试
dotnet test

# 5. 发布 AOT 版本（可选）
dotnet publish src/Synchron.Console -c Release -r win-x64 --self-contained

# 编译产物位置
# src/Synchron.Console/bin/Release/net8.0/win-x64/publish/Synchron.exe
```

### 方式三：全局安装（开发模式）

```powershell
# 安装为全局工具
dotnet pack src/Synchron.Core -c Release
dotnet tool install --global --add-source ./nupkg Synchron

# 使用
synchron --help
```

### 配置文件位置

```
Windows:
├── %APPDATA%\Synchron\
│   └── synchron.json          # 默认配置文件
│
└── %LOCALAPPDATA%\Synchron\
    └── logs\                  # 日志文件目录
```

---

## 使用方法

### 快速开始

```bash
# 显示帮助信息
Synchron --help

# 显示版本
Synchron --version

# 基本同步
Synchron C:\Source D:\Backup

# 预览模式（不实际执行）
Synchron C:\Source D:\Backup --dry-run
```

### 命令行参数

```
Synchron <source> <target> [options]

参数:
  <source>       源目录路径
  <target>       目标目录路径

选项:
  -m, --mode <mode>       同步模式: diff, sync, move, mirror (默认: diff)
  -f, --filter <pattern>  包含文件模式 (如: *.txt, **/*.cs)
  -e, --exclude <pattern> 排除文件模式
  -w, --watch             启用实时监控模式
  -l, --log <level>       日志级别: debug, info, warn, error (默认: info)
  -c, --config <file>     配置文件路径
      --dry-run           预览模式，不实际执行
      --verify            使用哈希校验文件
      --verbose           详细输出
      --no-subdirs        不包含子目录
      --conflict <mode>   冲突处理: overwrite, newer, skip, rename
      --buffer <size>     缓冲区大小(字节) (默认: 1MB)
      --logfile <path>    日志文件路径
  -v, --version           显示版本信息
  -h, --help              显示帮助信息

GitIgnore 选项:
      --no-gitignore      禁用 GitIgnore 自动检测
      --gitignore <file>  使用外部 .gitignore 文件
      --force-gitignore   强制使用指定的 GitIgnore (跳过自动检测)
```

### 同步模式详解

#### 1. Diff 模式（增量同步）

仅复制新增和已更改的文件，保留目标目录中的其他文件。

```bash
Synchron C:\Projects D:\Backup -m diff
```

```
源目录:          目标目录:
├── file1.txt    ├── file1.txt (已存在，跳过)
├── file2.txt    ├── file2.txt (已存在，跳过)
└── file3.txt    └── old.txt   (保留)
                 ↓ 同步后
                 ├── file1.txt
                 ├── file2.txt
                 ├── file3.txt (新增)
                 └── old.txt   (保留)
```

#### 2. Sync 模式（标准同步）

与 Diff 类似，但会更新所有源目录中存在的文件。

```bash
Synchron C:\Projects D:\Backup -m sync
```

#### 3. Move 模式（移动）

将文件从源目录移动到目标目录，源文件会被删除。

```bash
Synchron C:\Temp\Inbox C:\Archive -m move
```

#### 4. Mirror 模式（镜像）

使目标目录完全镜像源目录，删除目标目录中多余的文件。

```bash
Synchron C:\Source D:\Mirror -m mirror
```

```
源目录:          目标目录:
├── file1.txt    ├── file1.txt
└── file2.txt    ├── file2.txt
                 └── extra.txt (将被删除)
                 ↓ 同步后
                 ├── file1.txt
                 └── file2.txt
```

### 文件过滤

#### 通配符过滤

```bash
# 仅同步文本文件
Synchron C:\Source D:\Backup -f "*.txt"

# 同步所有代码文件
Synchron C:\Source D:\Backup -f "*.cs" -f "*.js" -f "*.py"

# 排除临时文件
Synchron C:\Source D:\Backup -e "*.tmp" -e "*.log" -e "*.bak"

# 组合使用
Synchron C:\Source D:\Backup -f "*.txt" -e "*_test.txt"
```

#### 过滤模式语法

| 模式 | 说明 | 示例 |
|------|------|------|
| `*` | 匹配任意字符（不含路径分隔符） | `*.txt` |
| `**` | 匹配任意字符（含路径分隔符） | `**/*.cs` |
| `?` | 匹配单个字符 | `file?.txt` |

### GitIgnore 集成

Synchron 内置 GitIgnore 支持，可自动检测 Git 仓库并应用 `.gitignore` 规则进行文件过滤。

#### 自动检测机制

```
源目录扫描流程:
┌─────────────────────────────────────────────────────────────┐
│  1. 向上扫描目录树，检测 .git 目录                           │
│  2. 检测同目录及父目录中的 .gitignore 文件                   │
│  3. 解析 .gitignore 规则并缓存                               │
│  4. 应用规则过滤同步文件                                     │
└─────────────────────────────────────────────────────────────┘
```

#### GitIgnore 命令行选项

```bash
# 默认行为：自动检测并应用 .gitignore 规则
Synchron C:\MyProject D:\Backup

# 禁用 GitIgnore 自动检测
Synchron C:\Source D:\Backup --no-gitignore

# 使用外部 .gitignore 文件
Synchron C:\Source D:\Backup --gitignore C:\rules\.gitignore

# 强制使用指定的 GitIgnore（跳过自动检测）
Synchron C:\Source D:\Backup --gitignore .\my-rules.txt --force-gitignore
```

#### 配置文件中的 GitIgnore 设置

```json
{
  "sourcePath": "C:\\Projects",
  "targetPath": "D:\\Backup",
  "gitIgnore": {
    "enabled": true,
    "autoDetect": true,
    "externalGitIgnorePath": null,
    "overrideAutoDetect": false
  }
}
```

#### GitIgnore 配置选项

| 选项 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `enabled` | bool | `true` | 是否启用 GitIgnore 过滤 |
| `autoDetect` | bool | `true` | 是否自动检测 Git 环境 |
| `externalGitIgnorePath` | string | `null` | 外部 .gitignore 文件路径 |
| `overrideAutoDetect` | bool | `false` | 是否覆盖自动检测 |

#### 支持的 GitIgnore 语法

Synchron 支持标准 `.gitignore` 语法：

| 语法 | 说明 | 示例 |
|------|------|------|
| `*` | 匹配任意字符（不含 `/`） | `*.log` |
| `**` | 匹配任意目录 | `**/temp/` |
| `?` | 匹配单个字符 | `file?.txt` |
| `[]` | 匹配字符范围 | `[abc].txt` |
| `!` | 否定模式 | `!important.log` |
| `/` | 目录分隔符 | `build/` |
| `#` | 注释 | `# This is a comment` |

#### GitIgnore 示例

```gitignore
# Build outputs
bin/
obj/
*.dll
*.exe

# IDE settings
.vs/
.idea/
*.user
*.suo

# Logs
*.log
logs/

# Exceptions (negation)
!important.dll
!important.log
```

#### 配置优先级

```
优先级（从高到低）:
1. 命令行参数 (--no-gitignore, --gitignore, --force-gitignore)
2. 配置文件 (synchron.json 中的 gitIgnore 配置)
3. 自动检测的 Git 环境
4. 默认行为
```

#### 注意事项

- GitIgnore 规则会被缓存以提高性能
- 修改 `.gitignore` 文件后，缓存会自动更新
- 外部 GitIgnore 文件支持相对路径和绝对路径
- 使用 `--force-gitignore` 时会跳过自动检测，仅使用指定文件

### 实时监控模式

```bash
# 启动监控模式
Synchron C:\Source D:\Backup -w

# 监控模式 + 详细日志
Synchron C:\Source D:\Backup -w -l debug

# 监控模式 + 过滤
Synchron C:\Source D:\Backup -w -f "*.txt"
```

监控模式下，Synchron 会持续监视源目录的变化，并在检测到文件变化时自动同步：

```
[2026-02-12 10:00:00] [INFO] File watcher started on: C:\Source
[2026-02-12 10:00:05] [INFO] File changed: newfile.txt (Created)
[2026-02-12 10:00:05] [INFO] Auto-sync completed: 1 copied, 0 deleted
```

### 配置文件

创建 `synchron.json` 配置文件：

```json
{
  "sourcePath": "C:\\Projects",
  "targetPath": "D:\\Backup",
  "mode": "Diff",
  "includeSubdirectories": true,
  "includePatterns": ["*.cs", "*.js", "*.json"],
  "excludePatterns": ["bin/*", "obj/*", "*.tmp"],
  "compareMethod": "TimestampAndSize",
  "conflictResolution": "OverwriteIfNewer",
  "bufferSize": 1048576,
  "maxRetries": 3,
  "retryDelayMs": 1000,
  "logLevel": "Info",
  "preserveTimestamps": true,
  "preserveAttributes": true,
  "watchDebounceMs": 500,
  "gitIgnore": {
    "enabled": true,
    "autoDetect": true,
    "externalGitIgnorePath": null,
    "overrideAutoDetect": false
  }
}
```

使用配置文件：

```bash
Synchron -c synchron.json

# 配置文件 + 命令行参数覆盖
Synchron -c synchron.json -m mirror --dry-run
```

### 交互式菜单

不带参数运行 Synchron 将进入交互式菜单：

```bash
Synchron
```

```
═══════════════════════════════════════════
         Synchron - File Sync Tool         
═══════════════════════════════════════════

  Source: (not set)
  Target: (not set)
  Mode:   Diff

  [1] Execute Sync
  [2] Preview Sync (Dry Run)
  [3] Configure Settings
  [4] Start Watch Mode
  [5] Stop Watch Mode
  [6] Show Current Configuration
  [7] Save Configuration to File
  [0] Exit

  Select option: _
```

### 使用示例

#### 场景一：项目代码备份

```bash
# 备份项目，排除编译产物
Synchron C:\MyProject D:\Backups\MyProject `
  -e "bin/*" -e "obj/*" -e ".vs/*" `
  -e "*.user" -e "*.suo"

# 使用配置文件
Synchron -c project-backup.json
```

#### 场景二：日志文件归档

```bash
# 移动30天前的日志到归档目录
Synchron C:\Logs D:\Archive\Logs -m move -f "*.log"
```

#### 场景三：实时同步开发目录

```bash
# 实时同步到网络共享
Synchron C:\DevProjects \\NAS\DevBackup -w -l debug --logfile sync.log
```

#### 场景四：镜像网站目录

```bash
# 完整镜像网站目录
Synchron C:\Website\Staging D:\Website\Production -m mirror --verify
```

---

## 开发指南

### 项目结构

```
Synchron/
├── src/
│   ├── Synchron.Core/                 # 核心类库
│   │   ├── Interfaces/                # 接口定义
│   │   │   ├── ILogger.cs             #   日志接口
│   │   │   ├── ISyncEngine.cs         #   同步引擎接口
│   │   │   ├── IFileWatcher.cs        #   文件监控接口
│   │   │   └── IConfigManager.cs      #   配置管理接口
│   │   ├── Models/                    # 数据模型
│   │   │   ├── SyncOptions.cs         #   同步选项
│   │   │   ├── SyncResult.cs          #   同步结果
│   │   │   └── FileItem.cs            #   文件项
│   │   ├── Logger.cs                  # 日志实现
│   │   ├── ConfigManager.cs           # 配置管理
│   │   ├── FileFilter.cs              # 文件过滤
│   │   ├── SyncEngine.cs              # 同步引擎
│   │   ├── FileWatcher.cs             # 文件监控
│   │   └── Synchron.Core.csproj
│   │
│   └── Synchron.Console/              # 控制台应用
│       ├── Program.cs                 # 主入口
│       ├── CommandLineParser.cs       # 命令行解析
│       ├── CommandLineOptions.cs      # 命令行选项
│       ├── InteractiveMenu.cs         # 交互菜单
│       └── Synchron.Console.csproj
│
├── tests/
│   └── Synchron.Core.Tests/           # 单元测试
│       ├── LoggerTests.cs
│       ├── FileFilterTests.cs
│       ├── ConfigManagerTests.cs
│       ├── SyncEngineTests.cs
│       └── Synchron.Core.Tests.csproj
│
├── Synchron.slnx                      # 解决方案文件
└── README.md
```

### 核心模块说明

#### SyncEngine（同步引擎）

```csharp
public interface ISyncEngine
{
    Task<SyncResult> SyncAsync(SyncOptions options, CancellationToken cancellationToken = default);
    Task<SyncPreview> PreviewAsync(SyncOptions options, CancellationToken cancellationToken = default);
    event EventHandler<SyncProgressEventArgs>? ProgressChanged;
}
```

#### FileWatcher（文件监控）

```csharp
public interface IFileWatcher
{
    void Start();
    void Stop();
    bool IsRunning { get; }
    string WatchPath { get; }
    event EventHandler<FileChangedEventArgs>? FileChanged;
}
```

### 编码规范

#### 命名约定

```csharp
// 接口：I + PascalCase
public interface ILogger { }
public interface ISyncEngine { }

// 类：PascalCase
public class SyncEngine { }
public class FileFilter { }

// 方法：PascalCase
public void StartWatch() { }
public async Task<SyncResult> SyncAsync() { }

// 参数：camelCase
public void CopyFile(string sourcePath, string targetPath) { }

// 私有字段：_ + camelCase
private readonly ILogger _logger;
private readonly object _lock = new();
```

#### 异步编程

```csharp
// 使用 async/await
public async Task<SyncResult> SyncAsync(SyncOptions options, CancellationToken ct = default)
{
    await Task.Run(() => { /* ... */ }, ct);
    return result;
}

// 使用 ValueTask 优化高频调用
public ValueTask<bool> FileExistsAsync(string path)
{
    return File.Exists(path) 
        ? new ValueTask<bool>(true) 
        : new ValueTask<bool>(CheckExistsAsync(path));
}
```

### 开发环境搭建

```powershell
# 1. 安装 .NET SDK
winget install Microsoft.DotNet.SDK.8

# 2. 安装 VS Code 扩展
code --install-extension ms-dotnettools.csharp
code --install-extension ms-dotnettools.vscode-dotnet-runtime

# 3. 克隆项目
git clone https://github.com/your-repo/synchron.git
cd synchron

# 4. 还原依赖
dotnet restore

# 5. 构建项目
dotnet build

# 6. 运行测试
dotnet test --verbosity normal

# 7. 运行应用
dotnet run --project src/Synchron.Console
```

### 提交代码流程

```bash
# 1. 创建功能分支
git checkout -b feature/your-feature-name

# 2. 进行修改并测试
dotnet build
dotnet test

# 3. 提交更改
git add .
git commit -m "feat: add your feature description"

# 4. 推送分支
git push origin feature/your-feature-name

# 5. 创建 Pull Request
```

#### 提交信息规范

```
feat: 新功能
fix: 修复 bug
docs: 文档更新
style: 代码格式调整
refactor: 重构
test: 测试相关
chore: 构建/工具相关

示例:
feat: add hash verification for file comparison
fix: handle file lock exception during sync
docs: update installation guide
```

---

## 维护指南

### 常见问题排查

#### 问题 1：文件被占用无法同步

```
错误信息: The process cannot access the file because it is being used by another process.
```

**解决方案：**
1. 关闭占用文件的程序
2. 使用 `--buffer` 参数调整缓冲区大小
3. 检查杀毒软件是否锁定文件

#### 问题 2：权限不足

```
错误信息: Access to the path 'xxx' is denied.
```

**解决方案：**
1. 以管理员身份运行
2. 检查文件夹权限设置
3. 确认目标目录可写

#### 问题 3：路径过长

```
错误信息: The specified path, file name, or both are too long.
```

**解决方案：**
1. 启用 Windows 长路径支持：
```powershell
# 注册表设置
New-ItemProperty -Path "HKLM:\SYSTEM\CurrentControlSet\Control\FileSystem" `
  -Name "LongPathsEnabled" -Value 1 -PropertyType DWORD -Force
```

#### 问题 4：监控模式丢失事件

**解决方案：**
1. 增加 `watchDebounceMs` 值
2. 减少监控目录的文件数量
3. 使用多个监控实例分担负载

### 日志查看

#### 控制台日志

```bash
# 详细日志
Synchron C:\Source D:\Backup -l debug

# 仅错误日志
Synchron C:\Source D:\Backup -l error
```

#### 文件日志

```bash
# 输出到文件
Synchron C:\Source D:\Backup --logfile sync.log -l debug
```

#### 日志格式

```
[2026-02-12 15:30:45.123] [   INFO] Starting sync: C:\Source -> D:\Backup
[2026-02-12 15:30:45.234] [  DEBUG] Scanning source directory...
[2026-02-12 15:30:45.345] [  DEBUG] Found 150 files to process
[2026-02-12 15:30:46.456] [   INFO] Sync completed: 10 copied, 140 skipped
[2026-02-12 15:30:47.567] [WARNING] File locked, retrying: data.db
[2026-02-12 15:30:48.678] [  ERROR] Failed to copy: permission denied
```

### 性能优化建议

#### 1. 缓冲区优化

```bash
# 小文件：减小缓冲区
--buffer 65536

# 大文件：增大缓冲区
--buffer 4194304
```

#### 2. 并行处理

```csharp
// 在 SyncOptions 中配置
options.MaxDegreeOfParallelism = Environment.ProcessorCount;
```

#### 3. 过滤优化

```bash
# 使用精确的过滤规则减少扫描范围
-f "*.cs" -e "bin/*" -e "obj/*"
```

### 版本更新策略

```
版本号格式: MAJOR.MINOR.PATCH

MAJOR: 不兼容的 API 变更
MINOR: 向后兼容的功能新增
PATCH: 向后兼容的问题修复

示例:
1.0.0 -> 1.0.1 (修复 bug)
1.0.1 -> 1.1.0 (新增功能)
1.1.0 -> 2.0.0 (重大变更)
```

---

## 贡献说明

### 参与贡献

我们欢迎所有形式的贡献！

#### 贡献方式

1. **报告问题** - 提交 Bug 报告或功能建议
2. **提交代码** - 修复 Bug 或实现新功能
3. **完善文档** - 改进文档或翻译
4. **分享经验** - 分享使用案例和最佳实践

### 提交 Issue

#### Bug 报告模板

```markdown
## Bug 描述
简要描述遇到的问题

## 复现步骤
1. 执行命令 `Synchron ...`
2. 观察到错误信息

## 期望行为
描述期望的正常行为

## 实际行为
描述实际的错误行为

## 环境信息
- OS: Windows 11
- .NET Version: 8.0.100
- Synchron Version: 1.0.0

## 日志输出
```
粘贴相关日志
```

## 截图
如有必要，添加截图
```

#### 功能建议模板

```markdown
## 功能描述
描述你希望添加的功能

## 使用场景
描述该功能解决什么问题

## 建议实现
如有想法，描述可能的实现方式

## 替代方案
描述你考虑过的替代方案
```

### 提交 Pull Request

#### PR 检查清单

- [ ] 代码通过所有测试 `dotnet test`
- [ ] 代码符合编码规范
- [ ] 添加必要的单元测试
- [ ] 更新相关文档
- [ ] 提交信息符合规范

#### PR 流程

```
1. Fork 项目
2. 创建分支: git checkout -b feature/amazing-feature
3. 提交更改: git commit -m 'feat: add amazing feature'
4. 推送分支: git push origin feature/amazing-feature
5. 创建 Pull Request
6. 等待代码审查
7. 合并到主分支
```

### 行为准则

- 尊重所有贡献者
- 接受建设性批评
- 关注对社区最有利的事情
- 对其他社区成员保持同理心

---

## 许可证

本项目采用 [MIT License](LICENSE) 开源协议。

---

## 联系方式

- **问题反馈**: [GitHub Issues](https://github.com/your-repo/synchron/issues)
- **功能建议**: [GitHub Discussions](https://github.com/your-repo/synchron/discussions)

---

<div align="center">

**⭐ 如果这个项目对你有帮助，请给一个 Star！⭐**

Made with ❤️ by Synchron Team

</div>

---

## English

### Overview

**Synchron** is a high-performance file synchronization tool for Windows, featuring:

- 🚀 High-speed async file operations
- 🔄 Multiple sync modes (Diff, Sync, Move, Mirror)
- 👀 Real-time directory monitoring
- 🎯 Flexible file filtering
- 📊 Progress reporting and statistics
- ⚙️ JSON configuration support

### Quick Start

```bash
# Build
dotnet build

# Run tests
dotnet test

# Basic sync
Synchron C:\Source D:\Backup

# Mirror mode with preview
Synchron C:\Source D:\Backup -m mirror --dry-run

# Watch mode
Synchron C:\Source D:\Backup -w
```

### Requirements

- Windows 10 (1809+) or Windows 11
- .NET 8.0 Runtime

### License

MIT License - see [LICENSE](LICENSE) for details.
