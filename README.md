# MES Upload System

一个用于MES（制造执行系统）集成的C# WinForms应用程序，支持PLC通信和数据上传功能。

## 🎯 项目概述

MES Upload System是一个专业的制造业数据采集和上传系统，通过多种通信协议与PLC设备进行数据交互，并将采集的数据上传到MES系统。

## ✨ 主要特性

- **多协议PLC通信支持**
  - TCP/IP通信
  - UDP通信
  - 串口通信 (Serial Port)
  - 实时数据采集

- **MES系统集成**
  - 批量物料数据上传
  - 实时数据同步
  - 配置化管理

- **用户友好界面**
  - WinForms图形界面
  - 实时状态监控
  - 配置管理界面

## 🏗️ 技术架构

- **开发平台**: .NET Framework 4.7.2
- **UI框架**: Windows Forms (WinForms)
- **开发语言**: C#
- **通信协议**: TCP, UDP, Serial Port

## 📁 项目结构

```
MESUploadSystem/
├── Controls/                 # 自定义控件
│   ├── BatchMaterialControl.cs
│   ├── SerialPortControl.cs
│   ├── TcpControl.cs
│   └── UdpControl.cs
├── Forms/                   # 窗口表单
│   ├── MainForm.cs         # 主窗口
│   ├── MesSettingsForm.cs  # MES设置
│   ├── PlcSettingsForm.cs  # PLC设置
│   └── SettingsForm.cs     # 系统设置
├── Models/                  # 数据模型
│   ├── AppConfig.cs        # 应用配置
│   ├── BatchMaterial.cs    # 批量物料
│   ├── MesConfig.cs        # MES配置
│   ├── PlcSignalConfig.cs  # PLC信号配置
│   ├── SerialPortConfig.cs # 串口配置
│   ├── TcpConfig.cs        # TCP配置
│   └── UdpConfig.cs        # UDP配置
├── App.config              # 应用配置文件
└── MESUploadSystem.csproj  # 项目文件
```

## 🚀 快速开始

### 环境要求

- Windows 10/11
- .NET Framework 4.7.2 或更高版本
- Visual Studio 2019/2022 (推荐)

### 安装步骤

1. **克隆项目**
   ```bash
   git clone https://github.com/luluzsp/MESUploadSystem.git
   cd MESUploadSystem
   ```

2. **打开项目**
   - 双击 `MESUploadSystem.sln` 文件
   或使用 Visual Studio 打开解决方案

3. **构建项目**
   - 在 Visual Studio 中选择 `Build > Build Solution`
   - 或按 `Ctrl + Shift + B`

4. **运行应用程序**
   - 按 `F5` 启动调试
   或双击生成的可执行文件

## ⚙️ 配置说明

### PLC通信配置

1. **TCP通信**
   - 设置PLC的IP地址和端口号
   - 配置数据读取间隔
   - 设置超时参数

2. **UDP通信**
   - 配置本地和远程端口
   - 设置广播地址
   - 配置数据包格式

3. **串口通信**
   - 选择正确的COM端口
   - 设置波特率、数据位、停止位
   - 配置奇偶校验

### MES系统配置

1. **服务器设置**
   - MES服务器地址
   - API接口路径
   - 认证信息

2. **数据上传**
   - 批量物料数据格式
   - 上传频率设置
   - 错误重试机制

## 🎯 使用指南

### 主界面操作

1. **连接PLC**
   - 选择通信协议类型
   - 配置连接参数
   - 点击"连接"按钮

2. **数据采集**
   - 实时显示PLC数据
   - 数据变化自动检测
   - 历史数据记录

3. **数据上传**
   - 配置MES系统参数
   - 设置上传规则
   - 手动或自动上传

### 故障排查

- **连接失败**: 检查网络配置和PLC设置
- **数据异常**: 验证数据格式和通信协议
- **上传失败**: 检查MES服务器状态和认证信息

## 🛠️ 开发指南

### 添加新功能

1. **新增通信协议**
   - 创建新的Control类
   - 实现通信接口
   - 添加到主界面

2. **扩展数据模型**
   - 在Models目录添加新类
   - 更新配置文件
   - 实现数据序列化

### 调试技巧

- 使用Visual Studio的调试工具
- 查看实时日志输出
- 使用断点调试通信过程

## 📊 性能优化

- **内存管理**: 及时释放资源
- **网络优化**: 合理设置缓冲区大小
- **UI响应**: 使用异步操作避免界面卡顿

## 🔒 安全考虑

- **数据加密**: 敏感数据传输加密
- **访问控制**: 配置访问权限
- **日志审计**: 记录操作日志

## 📋 更新日志

### v1.0.0 (2026-01-04)
- 初始版本发布
- 支持TCP/UDP/串口通信
- 实现MES数据上传功能
- 提供完整的用户界面

## 🤝 贡献指南

欢迎贡献代码！请遵循以下步骤：

1. Fork 项目
2. 创建功能分支 (`git checkout -b feature/AmazingFeature`)
3. 提交更改 (`git commit -m 'Add some AmazingFeature'`)
4. 推送到分支 (`git push origin feature/AmazingFeature`)
5. 创建 Pull Request

## 📄 许可证

本项目采用 MIT 许可证 - 查看 [LICENSE](LICENSE) 文件了解详情。

## 📞 联系方式

- 项目地址: [https://github.com/luluzsp/MESUploadSystem](https://github.com/luluzsp/MESUploadSystem)
- 问题反馈: [Issues](https://github.com/luluzsp/MESUploadSystem/issues)

## 🙏 致谢

- 感谢所有贡献者
- 感谢开源社区的支持