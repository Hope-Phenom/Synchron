# Synchron

<div align="center">

![Synchron Logo](https://img.shields.io/badge/Synchron-File%20Sync%20Tool-blue?style=for-the-badge)

**高性能文件同步工具**

[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?style=flat-square&logo=dotnet)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/badge/License-MIT-green?style=flat-square)](LICENSE)
[![Build](https://img.shields.io/badge/Build-Passing-brightgreen?style=flat-square)]()
[![Tests](https://img.shields.io/badge/Tests-83%20Passed-success?style=flat-square)]()

[English](README.md) | **中文文档**

</div>

---

## 目录

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
+-------------------------------------------------------------+
|                     Synchron 架构设计                        |
+-------------------------------------------------------------+
|                                                             |
|  +------------------+       +-----------------------------+ |
|  |  Console Shell   |       |       Synchron.Core         | |
|  |  (用户交互层)     |------>|       (核心处理层)           | |
|  +------------------+       +-----------------------------+ |
|         |                              |                    |
|         |                              |                    |
|         V                              V                    |
|  +------------------+       +-----------------------------+ |
|  | Command Parser   |       |  +-------+  +-----------+   | |
|  | Interactive Menu |       |  |Logger |  |FileFilter |   | |
|  +------------------+       |  +-------+  +-----------+   | |
|                             |  +-----------+ +----------+  | |
|                             |  |SyncEngine | |FileWatch |  | |
|                             |  +-----------+ +----------+  | |
|                             |  +-----------------------+   | |
|                             |  |    ConfigManager      |   | |
|                             |  +-----------------------+   | |
|                             +-----------------------------+ |
|                                                             |
+-------------------------------------------------------------+
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

---

## 安装步骤

### 方式一：从 NuGet 安装（推荐）

```powershell
# 安装为全局工具
dotnet tool install --global Synchron

# 使用
synchron --help
synchron --version
```

### 方式二：下载预编译版本

```powershell
# 1. 从 Releases 页面下载最新版本
# https://github.com/hope-phenom/synchron/releases

# 2. 解压到目标目录
Expand-Archive -Path synchron-v1.2.2.zip -DestinationPath C:\Tools\Synchron

# 3. 添加到系统 PATH（可选）
$env:PATH += ";C:\Tools\Synchron"

# 4. 验证安装
.\Synchron.exe --version
```

### 方式三：从源码编译

```powershell
# 1. 克隆仓库
git clone https://github.com/hope-phenom/synchron.git
cd synchron

# 2. 还原依赖
dotnet restore

# 3. 编译项目
dotnet build --configuration Release

# 4. 运行测试
dotnet test

# 5. 打包为全局工具
dotnet pack src/Synchron.Console/Synchron.Console.csproj -c Release -o nupkg
dotnet tool install --global --add-source ./nupkg Synchron
```

---

## 使用方法

### 快速开始

```bash
# 显示帮助信息
synchron --help

# 显示版本
synchron --version

# 基本同步
synchron C:\Source D:\Backup

# 预览模式（不实际执行）
synchron C:\Source D:\Backup --dry-run
```

### 命令行参数

```
Usage:
  synchron <source> <target> [options]    单次同步操作
  synchron task <tasks.json> [options]    执行任务列表
  synchron task-init                      创建示例任务列表

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

任务列表选项:
  synchron task <tasks.json>              执行所有启用的任务
  synchron task <tasks.json> --list       列出所有任务
  synchron task <tasks.json> -t <name>    执行指定任务
  synchron task <tasks.json> --dry-run    预览所有任务
  synchron task-init                      创建示例任务列表文件
```

### 同步模式详解

#### 1. Diff 模式（增量同步）

仅复制新增和已更改的文件，保留目标目录中的其他文件。

```bash
synchron C:\Projects D:\Backup -m diff
```

```
源目录:          目标目录:
+-- file1.txt    +-- file1.txt (已存在，跳过)
+-- file2.txt    +-- file2.txt (已存在，跳过)
+-- file3.txt    +-- old.txt   (保留)
                 | 同步后
                 +-- file1.txt
                 +-- file2.txt
                 +-- file3.txt (新增)
                 +-- old.txt   (保留)
```

#### 2. Sync 模式（标准同步）

与 Diff 类似，但会更新所有源目录中存在的文件。

```bash
synchron C:\Projects D:\Backup -m sync
```

#### 3. Move 模式（移动）

将文件从源目录移动到目标目录，源文件会被删除。

```bash
synchron C:\Temp\Inbox C:\Archive -m move
```

#### 4. Mirror 模式（镜像）

使目标目录完全镜像源目录，删除目标目录中多余的文件。

```bash
synchron C:\Source D:\Mirror -m mirror
```

```
源目录:          目标目录:
+-- file1.txt    +-- file1.txt
+-- file2.txt    +-- file2.txt
                 +-- extra.txt (将被删除)
                 | 同步后
                 +-- file1.txt
                 +-- file2.txt
```

### GitIgnore 集成

Synchron 内置 GitIgnore 支持，可自动检测 Git 仓库并应用 `.gitignore` 规则进行文件过滤。

```bash
# 默认行为：自动检测并应用 .gitignore 规则
synchron C:\MyProject D:\Backup

# 禁用 GitIgnore 自动检测
synchron C:\Source D:\Backup --no-gitignore

# 使用外部 .gitignore 文件
synchron C:\Source D:\Backup --gitignore C:\rules\.gitignore
```

### 任务列表功能

Synchron 支持通过任务列表配置文件批量执行多个同步任务。

```bash
# 创建示例任务列表配置文件
synchron task-init

# 执行所有启用的任务
synchron task tasks.json

# 列出所有任务
synchron task tasks.json --list

# 执行特定任务
synchron task tasks.json -t "Documents Backup"

# 预览模式
synchron task tasks.json --dry-run
```

#### 任务列表示例

```json
{
  "name": "Sample Task List",
  "stopOnError": false,
  "maxParallelTasks": 1,
  "tasks": [
    {
      "name": "Documents Backup",
      "description": "Sync documents to backup folder",
      "enabled": true,
      "options": {
        "sourcePath": "C:\\Users\\User\\Documents",
        "targetPath": "D:\\Backup\\Documents",
        "mode": "Sync",
        "includeSubdirectories": true,
        "gitIgnore": {
          "enabled": true,
          "autoDetect": true
        }
      }
    }
  ]
}
```

### 实时监控模式

```bash
# 启动监控模式
synchron C:\Source D:\Backup -w

# 监控模式 + 详细日志
synchron C:\Source D:\Backup -w -l debug
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
  "gitIgnore": {
    "enabled": true,
    "autoDetect": true
  }
}
```

使用配置文件：

```bash
synchron -c synchron.json
```

### 交互式菜单

不带参数运行 Synchron 将进入交互式菜单：

```bash
synchron
```

```
===========================================
         Synchron - File Sync Tool         
===========================================

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

---

## 开发指南

### 项目结构

```
Synchron/
+-- src/
|   +-- Synchron.Core/                 # 核心类库
|   |   +-- Interfaces/                # 接口定义
|   |   +-- Models/                    # 数据模型
|   |   +-- GitSupport/                # GitIgnore 支持
|   |   +-- Logger.cs                  # 日志实现
|   |   +-- SyncEngine.cs              # 同步引擎
|   |   +-- TaskListExecutor.cs        # 任务列表执行器
|   |   +-- TaskListManager.cs         # 任务列表管理器
|   |
|   +-- Synchron.Console/              # 控制台应用
|       +-- Program.cs                 # 主入口
|       +-- CommandLineParser.cs       # 命令行解析
|       +-- InteractiveMenu.cs         # 交互菜单
|
+-- tests/
|   +-- Synchron.Core.Tests/           # 单元测试
|
+-- Synchron.slnx                      # 解决方案文件
+-- README.md                          # 英文文档
+-- README_CN.md                       # 中文文档
```

### 开发环境搭建

```powershell
# 1. 安装 .NET SDK
winget install Microsoft.DotNet.SDK.8

# 2. 克隆项目
git clone https://github.com/hope-phenom/synchron.git
cd synchron

# 3. 还原依赖
dotnet restore

# 4. 构建项目
dotnet build

# 5. 运行测试
dotnet test --verbosity normal

# 6. 运行应用
dotnet run --project src/Synchron.Console
```

### 提交信息规范

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
启用 Windows 长路径支持：
```powershell
New-ItemProperty -Path "HKLM:\SYSTEM\CurrentControlSet\Control\FileSystem" `
  -Name "LongPathsEnabled" -Value 1 -PropertyType DWORD -Force
```

### 性能优化建议

```bash
# 小文件：减小缓冲区
--buffer 65536

# 大文件：增大缓冲区
--buffer 4194304

# 使用精确的过滤规则减少扫描范围
-f "*.cs" -e "bin/*" -e "obj/*"
```

---

## 贡献说明

我们欢迎所有形式的贡献！

### 贡献方式

1. **报告问题** - 提交 Bug 报告或功能建议
2. **提交代码** - 修复 Bug 或实现新功能
3. **完善文档** - 改进文档或翻译
4. **分享经验** - 分享使用案例和最佳实践

### PR 检查清单

- [ ] 代码通过所有测试 `dotnet test`
- [ ] 代码符合编码规范
- [ ] 添加必要的单元测试
- [ ] 更新相关文档
- [ ] 提交信息符合规范

---

## 许可证

本项目采用 [MIT License](LICENSE) 开源协议。

---

## 联系方式

- **问题反馈**: [GitHub Issues](https://github.com/hope-phenom/synchron/issues)
- **功能建议**: [GitHub Discussions](https://github.com/hope-phenom/synchron/discussions)

---

<div align="center">

**如果这个项目对你有帮助，请给一个 Star！**

Made with love by Synchron Team

</div>
