# Synchron

<div align="center">

![Synchron Logo](https://img.shields.io/badge/Synchron-File%20Sync%20Tool-blue?style=for-the-badge)

**High-Performance File Synchronization Tool**

[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?style=flat-square&logo=dotnet)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/badge/License-MIT-green?style=flat-square)](LICENSE)
[![Build](https://img.shields.io/badge/Build-Passing-brightgreen?style=flat-square)]()
[![Tests](https://img.shields.io/badge/Tests-83%20Passed-success?style=flat-square)]()

**English** | [中文文档](README_CN.md)

</div>

---

## Table of Contents

- [Overview](#overview)
- [Requirements](#requirements)
- [Installation](#installation)
- [Usage](#usage)
- [Development Guide](#development-guide)
- [Troubleshooting](#troubleshooting)
- [Contributing](#contributing)

---

## Overview

**Synchron** is a high-performance file synchronization tool designed for Windows, featuring a modular architecture inspired by FastCopy. The core processing logic is encapsulated in an independent C# library, with a console application shell handling user interaction.

### Key Features

| Feature | Description |
|---------|-------------|
| 🚀 **High Performance** | Async I/O and buffer optimization for efficient large file and bulk small file transfers |
| 🔄 **Multiple Sync Modes** | Supports Diff (incremental), Sync, Move, and Mirror modes |
| 👀 **Real-time Monitoring** | Directory change monitoring with automatic sync based on FileSystemWatcher |
| 🎯 **Smart Filtering** | Wildcard and regex support for file inclusion/exclusion rules |
| 📊 **Progress Reporting** | Real-time sync progress with file count, transfer speed statistics |
| ⚙️ **Flexible Configuration** | JSON config files and command-line parameters |
| 📝 **Detailed Logging** | Multi-level logging with console color output and file logging |
| 🔒 **Safe & Reliable** | File verification, retry mechanism, and error handling |

### Comparison with Similar Tools

| Feature | Synchron | FastCopy | Robocopy | rsync |
|---------|----------|----------|----------|-------|
| Open Source | ✅ | ✅ | ✅ | ✅ |
| Cross-platform | ❌ (Windows) | ❌ (Windows) | ❌ (Windows) | ✅ |
| Real-time Monitoring | ✅ | ❌ | ❌ | ❌ |
| GitIgnore Integration | ✅ | ❌ | ❌ | ❌ |
| Config File | ✅ JSON | ❌ | ❌ | ✅ |
| Modular Design | ✅ | ❌ | ❌ | ❌ |
| .NET Native | ✅ | ❌ | ❌ | ❌ |
| CLI Interface | ✅ | ✅ | ✅ | ✅ |
| Interactive Menu | ✅ | ❌ | ❌ | ❌ |

---

## Requirements

### System Requirements

| Item | Minimum | Recommended |
|------|---------|-------------|
| OS | Windows 10 (1809+) | Windows 11 |
| Runtime | .NET 8.0 Runtime | .NET 8.0 SDK |
| Memory | 512 MB | 2 GB+ |
| Disk Space | 50 MB | 100 MB+ |

### Dependencies

- **.NET 8.0 Runtime** - [Download](https://dotnet.microsoft.com/download/dotnet/8.0)

---

## Installation

### Option 1: Install from NuGet (Recommended)

```powershell
# Install as global tool
dotnet tool install --global Synchron

# Usage
synchron --help
synchron --version
```

### Option 2: Download Pre-built Binary

```powershell
# 1. Download from Releases page
# https://github.com/hope-phenom/synchron/releases

# 2. Extract to target directory
Expand-Archive -Path synchron-v1.2.2.zip -DestinationPath C:\Tools\Synchron

# 3. Add to system PATH (optional)
$env:PATH += ";C:\Tools\Synchron"

# 4. Verify installation
.\Synchron.exe --version
```

### Option 3: Build from Source

```powershell
# 1. Clone repository
git clone https://github.com/hope-phenom/synchron.git
cd synchron

# 2. Restore dependencies
dotnet restore

# 3. Build project
dotnet build --configuration Release

# 4. Run tests
dotnet test

# 5. Pack as global tool
dotnet pack src/Synchron.Console/Synchron.Console.csproj -c Release -o nupkg
dotnet tool install --global --add-source ./nupkg Synchron
```

---

## Usage

### Quick Start

```bash
# Show help
synchron --help

# Show version
synchron --version

# Basic sync
synchron C:\Source D:\Backup

# Preview mode (dry run)
synchron C:\Source D:\Backup --dry-run
```

### Command Line Options

```
Usage:
  synchron <source> <target> [options]    Single sync operation
  synchron task <tasks.json> [options]    Execute task list
  synchron task-init                      Create sample task list

Arguments:
  <source>       Source directory path
  <target>       Target directory path

Options:
  -m, --mode <mode>       Sync mode: diff, sync, move, mirror (default: diff)
  -f, --filter <pattern>  Include file pattern (e.g., *.txt, **/*.cs)
  -e, --exclude <pattern> Exclude file pattern
  -w, --watch             Enable real-time monitoring mode
  -l, --log <level>       Log level: debug, info, warn, error (default: info)
  -c, --config <file>     Configuration file path
      --dry-run           Preview mode, no actual execution
      --verify            Use hash verification
      --verbose           Verbose output
      --no-subdirs        Exclude subdirectories
      --conflict <mode>   Conflict resolution: overwrite, newer, skip, rename
      --buffer <size>     Buffer size in bytes (default: 1MB)
      --logfile <path>    Log file path
  -v, --version           Show version
  -h, --help              Show help

GitIgnore Options:
      --no-gitignore      Disable GitIgnore auto-detection
      --gitignore <file>  Use external .gitignore file
      --force-gitignore   Force use specified GitIgnore (skip auto-detection)

Task List Options:
  synchron task <tasks.json>              Execute all enabled tasks
  synchron task <tasks.json> --list       List all tasks
  synchron task <tasks.json> -t <name>    Execute specific task
  synchron task <tasks.json> --dry-run    Preview all tasks
  synchron task-init                      Create sample task list file
```

### Sync Modes

#### 1. Diff Mode (Incremental Sync)

Only copies new and changed files, preserving other files in target directory.

```bash
synchron C:\Projects D:\Backup -m diff
```

```
Source:           Target:
+-- file1.txt     +-- file1.txt (exists, skipped)
+-- file2.txt     +-- file2.txt (exists, skipped)
+-- file3.txt     +-- old.txt   (preserved)
                  | After sync
                  +-- file1.txt
                  +-- file2.txt
                  +-- file3.txt (new)
                  +-- old.txt   (preserved)
```

#### 2. Sync Mode (Standard Sync)

Similar to Diff, but updates all files that exist in source.

```bash
synchron C:\Projects D:\Backup -m sync
```

#### 3. Move Mode

Moves files from source to target, source files are deleted.

```bash
synchron C:\Temp\Inbox C:\Archive -m move
```

#### 4. Mirror Mode

Makes target directory exactly mirror source, deleting extra files in target.

```bash
synchron C:\Source D:\Mirror -m mirror
```

### GitIgnore Integration

Synchron has built-in GitIgnore support, automatically detecting Git repositories and applying `.gitignore` rules.

```bash
# Default: auto-detect and apply .gitignore rules
synchron C:\MyProject D:\Backup

# Disable GitIgnore auto-detection
synchron C:\Source D:\Backup --no-gitignore

# Use external .gitignore file
synchron C:\Source D:\Backup --gitignore C:\rules\.gitignore
```

### Task List Feature

Execute multiple sync tasks in batch via task list configuration.

```bash
# Create sample task list config file
synchron task-init

# Execute all enabled tasks
synchron task tasks.json

# List all tasks
synchron task tasks.json --list

# Execute specific task
synchron task tasks.json -t "Documents Backup"

# Preview mode
synchron task tasks.json --dry-run
```

#### Task List Example

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

### Real-time Monitoring Mode

```bash
# Start monitoring mode
synchron C:\Source D:\Backup -w

# Monitoring with verbose logging
synchron C:\Source D:\Backup -w -l debug
```

### Configuration File

Create `synchron.json` configuration file:

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

Use configuration file:

```bash
synchron -c synchron.json
```

### Interactive Menu

Run Synchron without arguments to enter interactive menu:

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

## Development Guide

### Project Structure

```
Synchron/
+-- src/
|   +-- Synchron.Core/                 # Core library
|   |   +-- Interfaces/                # Interface definitions
|   |   +-- Models/                    # Data models
|   |   +-- GitSupport/                # GitIgnore support
|   |   +-- Logger.cs                  # Logger implementation
|   |   +-- SyncEngine.cs              # Sync engine
|   |   +-- TaskListExecutor.cs        # Task list executor
|   |   +-- TaskListManager.cs         # Task list manager
|   |
|   +-- Synchron.Console/              # Console application
|       +-- Program.cs                 # Main entry
|       +-- CommandLineParser.cs       # Command line parser
|       +-- InteractiveMenu.cs         # Interactive menu
|
+-- tests/
|   +-- Synchron.Core.Tests/           # Unit tests
|
+-- Synchron.slnx                      # Solution file
+-- README.md                          # English documentation
+-- README_CN.md                       # Chinese documentation
```

### Development Setup

```powershell
# 1. Install .NET SDK
winget install Microsoft.DotNet.SDK.8

# 2. Clone project
git clone https://github.com/hope-phenom/synchron.git
cd synchron

# 3. Restore dependencies
dotnet restore

# 4. Build project
dotnet build

# 5. Run tests
dotnet test --verbosity normal

# 6. Run application
dotnet run --project src/Synchron.Console
```

### Commit Message Convention

```
feat: New feature
fix: Bug fix
docs: Documentation update
style: Code formatting
refactor: Code refactoring
test: Test related
chore: Build/tool related

Examples:
feat: add hash verification for file comparison
fix: handle file lock exception during sync
docs: update installation guide
```

---

## Troubleshooting

### Issue 1: File Locked

```
Error: The process cannot access the file because it is being used by another process.
```

**Solutions:**
1. Close the program using the file
2. Adjust buffer size with `--buffer` parameter
3. Check if antivirus is locking the file

### Issue 2: Access Denied

```
Error: Access to the path 'xxx' is denied.
```

**Solutions:**
1. Run as administrator
2. Check folder permissions
3. Ensure target directory is writable

### Issue 3: Path Too Long

```
Error: The specified path, file name, or both are too long.
```

**Solution:** Enable Windows long path support:
```powershell
New-ItemProperty -Path "HKLM:\SYSTEM\CurrentControlSet\Control\FileSystem" `
  -Name "LongPathsEnabled" -Value 1 -PropertyType DWORD -Force
```

### Performance Tips

```bash
# Small files: reduce buffer size
--buffer 65536

# Large files: increase buffer size
--buffer 4194304

# Use precise filter rules to reduce scan scope
-f "*.cs" -e "bin/*" -e "obj/*"
```

---

## Contributing

We welcome all forms of contribution!

### Ways to Contribute

1. **Report Issues** - Submit bug reports or feature requests
2. **Submit Code** - Fix bugs or implement new features
3. **Improve Documentation** - Improve docs or translations
4. **Share Experience** - Share use cases and best practices

### PR Checklist

- [ ] Code passes all tests `dotnet test`
- [ ] Code follows coding standards
- [ ] Added necessary unit tests
- [ ] Updated relevant documentation
- [ ] Commit messages follow convention

---

## License

This project is licensed under the [MIT License](LICENSE).

---

## Contact

- **Issues**: [GitHub Issues](https://github.com/hope-phenom/synchron/issues)
- **Discussions**: [GitHub Discussions](https://github.com/hope-phenom/synchron/discussions)

---

<div align="center">

**If this project helps you, please give it a Star!**

Made with love by Synchron Team

</div>
