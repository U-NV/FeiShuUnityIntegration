# 飞书文件同步插件 (FeiShu File Sync)

[![Unity Version](https://img.shields.io/badge/Unity-2019.4%2B-blue.svg)](https://unity3d.com/)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)
[![Version](https://img.shields.io/badge/Version-1.0.0-orange.svg)](https://github.com/u0ugames/feishu-file-sync-unity/releases)

一个强大的Unity编辑器插件，用于与飞书API集成，实现文档、表格等文件的自动同步和导出功能。

## ✨ 主要功能

- 🔐 **OAuth2授权** - 安全的飞书账号授权登录
- 📄 **多格式支持** - 支持Excel (.xlsx)、PDF、Word (.docx)、CSV等格式
- 🔄 **批量同步** - 支持多个文件的批量导出和同步
- 📊 **表格数据** - 自动获取飞书表格的sheet信息
- 💾 **本地保存** - 自动保存导出文件到指定目录
- 🎯 **进度显示** - 实时显示同步进度和状态
- ⚙️ **配置管理** - 灵活的配置文件管理

## 📋 系统要求

- Unity 2019.4 或更高版本
- .NET Framework 4.7.1 或更高版本
- 网络连接（用于访问飞书API）

## 🚀 快速开始

### 1. 安装插件

将插件文件夹复制到Unity项目的 `Assets/` 目录下，或通过Package Manager安装。

### 2. 配置飞书应用

1. 在Unity编辑器中，打开菜单 `工具 > 飞书`
2. 在"配置"标签页中，输入您的飞书应用信息：
   - **飞书应用ID** (App ID)
   - **飞书应用密钥** (App Secret)

### 3. 授权登录

1. 点击"重新授权"按钮
2. 系统会自动打开浏览器进行OAuth2授权
3. 完成授权后，访问令牌会自动保存

### 4. 配置同步任务

1. 在"配置"标签页中添加文件同步配置：
   - **文件类型**: 选择导出格式 (xlsx, pdf, docx, csv)
   - **导出类型**: 选择内容类型 (sheet, doc, bitable)
   - **Token**: 飞书文档/表格的token
   - **本地路径**: 保存文件的本地路径

### 5. 开始同步

1. 切换到"同步"标签页
2. 点击"开始同步数据"按钮
3. 等待同步完成

## 📖 详细使用说明

### 飞书应用配置

在飞书开放平台创建应用时，需要配置以下权限：

```
offline_access
sheets:spreadsheet:read
drive:export:readonly
docs:document:export
vc:export
```

回调地址设置为：`http://localhost:8080/callback`

### 获取文档Token

1. 打开飞书文档或表格
2. 从URL中提取token，格式如：`https://example.feishu.cn/sheets/shtxxxxxxxxxxxxx`
3. 将 `shtxxxxxxxxxxxxx` 部分作为token填入配置

### 支持的文件格式

| 导出类型 | 支持格式 | 说明 |
|---------|---------|------|
| sheet | xlsx, csv | 飞书表格 |
| doc | pdf, docx | 飞书文档 |
| bitable | xlsx, csv | 飞书多维表格 |

## 🔧 高级配置

### 自定义保存路径

可以在配置中指定自定义的本地保存路径，支持相对路径和绝对路径。

### 批量配置

支持添加多个同步配置，系统会按顺序执行所有配置的同步任务。

## 🐛 故障排除

### 常见问题

**Q: 授权失败怎么办？**
A: 检查飞书应用配置是否正确，确保回调地址设置为 `http://localhost:8080/callback`

**Q: 文件下载失败？**
A: 检查网络连接，确保访问令牌未过期，验证文档token是否正确

**Q: 权限不足？**
A: 确保飞书应用已申请必要的API权限

### 调试信息

插件会在Unity Console中输出详细的调试信息，包括：
- API请求和响应
- 文件下载进度
- 错误详情

## 📝 更新日志

查看 [CHANGELOG.md](CHANGELOG.md) 了解版本更新详情。

## 🤝 贡献

欢迎提交Issue和Pull Request！

1. Fork 本仓库
2. 创建特性分支 (`git checkout -b feature/AmazingFeature`)
3. 提交更改 (`git commit -m 'Add some AmazingFeature'`)
4. 推送到分支 (`git push origin feature/AmazingFeature`)
5. 打开Pull Request

## 📄 许可证

本项目采用 MIT 许可证 - 查看 [LICENSE](LICENSE) 文件了解详情。

## 📞 支持

如果您遇到问题或有任何建议，请：

- 提交 [Issue](https://github.com/u0ugames/feishu-file-sync-unity/issues)
- 发送邮件至 support@u0ugames.com

## 🙏 致谢

感谢飞书开放平台提供的API支持。

---

**注意**: 本插件仅用于Unity编辑器环境，不支持运行时使用。
