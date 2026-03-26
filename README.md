# MAS.Communication

<p align="center">
  <img src="logo-RemoveBG.png" alt="Logo">
</p>

<p align="center">
  <a href="https://mas-copilot.github.io/mas.communication-docs/index.html">📖 Documentation</a>
</p>

<p align="center">
  <img src="https://img.shields.io/badge/.NET-8.0-blue" />
  <img src="https://img.shields.io/badge/license-MIT-green" />
  <a href="https://www.nuget.org/packages/MAS.Communication/">
    <img src="https://img.shields.io/nuget/v/MAS.Communication.svg" />
  </a>
  <img src="https://img.shields.io/badge/C%23-239120.svg?logo=csharp&logoColor=white" />
  <img src="https://img.shields.io/badge/IDE-Visual%20Studio-5C2D91?logo=visual-studio" />
</p>

## ✨ 项目特性

一个面向工业自动化场景的 **多协议通信管理框架**

- **多协议统一抽象**：Modbus TCP、MC Protocol、等协议采用一致的调用模型
- **多实例管理**：支持同协议多实例并存，适用于一机多 PLC / 多设备通信场景
- **统一生命周期控制**：统一创建、复用、查询与释放通信实例，避免资源失控
- **依赖注入集成**：基于 `Microsoft.Extensions.DependencyInjection`，便于工程化接入
- **易于扩展**：可按统一规范扩展新的通信协议，而无需改动上层业务逻辑
- **面向工业项目落地**：适用于 WPF、WinUI、后台服务等现代 .NET 应用

## 🎯 项目定位

`MAS.Communication` 是一个面向工业自动化的 .NET 通信框架，
用于在现代 .NET 应用中 **统一接入、管理和复用多种工业通信协议实例**

- 统一的通信抽象
- 多实例与多设备管理能力
- 一致的生命周期控制
- 面向依赖注入的工程化集成方式

适合构建中大型上位机系统中的通信基础设施层

## 🗺️ 发展路线

- [x] Modbus TCP
- [x] MC Protocol
- [x] S7 Protocol
- [ ] OPC UA
- [ ] EtherNet/IP
- [ ] MQTT
- [ ] CANopen
- [ ] PROFINET

## ⚠️ 使用前提

在使用本项目之前，请确认你的应用满足以下条件：

- 使用 **现代 .NET 项目**
- 目标框架为 **.NET 8.0 及以上**
- 使用 **Microsoft.Extensions.DependencyInjection** 进行服务注册与解析
- 不支持仍运行在 **.NET Framework** 上的项目
- 仅适合在 WPF / WinUI / Worker / ASP.NET Core 中集成

## 📦 NuGet

.NET CLI：

```bash
dotnet add package MAS.Communication
```

Package Manager：

```bash
Install-Package MAS.Communication
```

## 📂 目录结构

```bash
MAS.Communication:
├─docs                          # 用于生成的项目文档
├─libs                          # 项目依赖的动态链接库（DLL）
├─src                           # 源代码目录
│  └─MAS.Communication          # 通信类库，处理各种通信协议和数据传输
├─tests                         # 存放测试代码
│  └─MAS.CommunicationUnitTest  # 通信类库的单元测试
│  .editorconfig                # 编辑器配置文件
│  .gitignore                   # Git配置文件，指定了版本控制要忽略的文件和目录
│  .Directory.Build.props       # MSBuild配置文件，用于配置构建和编译选项
│  logo.ico                     # 应用程序的LOGO图标
│  README.md                    # 主文档
```

## 📖 项目文档

- 下载最新版本的 DocFX 可执行文件 [DocFX](https://github.com/dotnet/docfx/releases)
- 解压后，将`docfx.exe`文件所在的目录添加到系统的环境变量`PATH`中
- 使用`docfx --version`命令验证状态

**文档构建：**

```bash
# 进入文档目录
cd docs/

# 开始构建
docfx docfx.json

# 进入构建的目录
cd _site

# 启动服务器
docfx serve
```

每次向`main`分支推送代码时，仅当修改了`docs/`目录下的任意文件（或手动触发`docs-deploy`），才会通过`GitHub Actions`自动构建文档

## ✅ 测试

**导航到测试项目根目录，并运行以下命令：**

```bash
$iterations = 1000  # 迭代次数
$projectFile = Get-ChildItem -Recurse -Filter *.csproj | Select-Object -First 1
if ($projectFile -eq $null) {
    Write-Host "No .csproj file found in the current directory"
    exit
}
for ($i = 0; $i -lt $iterations; $i++) {
    Write-Host "Running iteration $($i + 1)"
    dotnet test $projectFile.FullName --logger "trx;LogFileName=test-$($i+1).trx"
}
```

## 😃 git commit emoji

| emoji | emoji代码       | commit 说明 |
| ----- | -------------- | ----------------------- |
| 🎉   | `:tada:`        | 初次提交                |
| ✨   | `:sparkles:`    | 新功能                  |
| ⚡️   | `:zap:`         | 性能改善                |
| 🐛   | `:bug:`         | 修复 Bug                |
| 🚑️   | `:ambulance:`   | 紧急修复 Bug            |
| 🎨   | `:art:`         | 改进代码结构/代码格式    |
| 🚚   | `:truck:`        | 移动或重命名文件、目录、命名空间等 |
| 💄   | `:lipstick:`    | 更新 UI 和样式文件      |
| 🔥   | `:fire:`        | 移除代码或文件           |
| 📝   | `:memo:`        | 撰写文档                |
| 🚀   | `:rocket:`      | 部署功能                |
| ✅   | `:white_check_mark:` | 添加或更新测试     |
| 🔒️   | `:lock:`        | 更新安全相关代码        |
| ⬆️   | `:arrow_up:`    | 升级依赖                |
| ⬇️   | `:arrow_down:`  | 降级依赖                |
| 🔀   | `:twisted_rightwards_arrows:` | 合并分支 |
| ⏪️   | `:rewind:`     | 回退到上一个版本         |
| 🔧   | `:wrench:`      | 修改配置文件            |
| 🗑️   | `:wastebasket:` | 删除不再需要的代码或文件 |
| ✏️   | `:pencil2:`     | 修正拼写或语法错误       |
| ♻️   | `:recycle:`     | 重构代码                |
| 💩   | `:poop:`        | 改进的(屎)坏(山)代码    |
| 👻   | `:ghost:`       | 添加或更新 GIF          |
| 👷   | `:construction_worker:` | 添加 或更新 CI 构建系统 |
| 🥚   | `:egg:`         | 添加或更新彩蛋          |
| 🏗️   | `:building_construction:` | 进行体系结构更改/重大重构 |
| 💡   | `:bulb:`        | 在源代码中添加或更新注释 |
