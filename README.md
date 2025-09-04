# 飞书Unity集成插件 (FeiShu Unity Integration)

[![Unity Version](https://img.shields.io/badge/Unity-2019.4%2B-blue.svg)](https://unity3d.com/)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)
[![Version](https://img.shields.io/badge/Version-1.0.0-orange.svg)](https://github.com/U-NV/FeiShu-Unity-Integration/releases)

Unity编辑器插件，用于与飞书API集成，实现云端文件同步到本地。

## ✨ 主要功能

- 🔐 **OAuth2授权** - 安全的飞书账号授权登录
- 📄 **多格式支持** - 支持Excel (.xlsx)、PDF、Word (.docx)、CSV等格式
- 🔄 **批量同步** - 支持多个文件的批量导出和同步
- 💾 **本地保存** - 自动保存导出文件到指定目录
- 🎯 **进度显示** - 实时显示同步进度和状态
- ⚙️ **配置管理** - 灵活的配置文件管理

## 📋 系统要求

- Unity 2019.4 或更高版本
- .NET Framework 4.7.1 或更高版本
- 网络连接（用于访问飞书API）

## 🚀 快速开始

### 1. 安装插件

#### 方法一：Package Manager安装（推荐）
1. 在Unity编辑器中打开 `Window > Package Manager`
2. 点击左上角的 `+` 按钮，选择 `Add package from git URL`
3. 输入：`https://github.com/U-NV/FeiShu-Unity-Integration.git`
4. 点击 `Add` 完成安装

#### 方法二：手动安装
1. 下载最新版本的插件包
2. 解压到Unity项目的 `Assets/` 目录下
3. 重新打开Unity编辑器

> ⚠️ **注意**: 确保Unity版本为2019.4或更高版本

### 2. 飞书应用配置

#### 选项一：使用默认应用（推荐）

插件已内置默认的飞书应用配置，可以直接使用，无需创建自己的应用：

> 💡 **优势**: 开箱即用，无需额外配置

#### 选项二：创建自定义应用

如果您需要创建自己的飞书应用：

1. 访问 [飞书开放平台](https://open.feishu.cn/)
2. 登录您的飞书账号
3. 点击"创建应用" > "自建应用"
4. 填写应用基本信息：
   - **应用名称**: 例如 "Unity文档同步工具"
   - **应用描述**: 描述您的使用场景
   - **应用图标**: 上传一个图标（可选）

### 3. 应用权限说明

#### 默认应用权限
使用默认应用时，以下权限已预配置：

```
基础权限：
- offline_access (离线访问)

文档权限：
- sheets:spreadsheet:read (读取电子表格)
- docs:document:export (导出文档)
- drive:export:readonly (导出云盘文件)
- vc:export (导出多维表格)
```

#### 自定义应用权限配置
如果创建自定义应用，需要在应用管理页面配置上述权限。

### 4. 重定向URL说明

#### 默认应用
使用默认应用时，重定向URL已预配置为：
```
http://localhost:8080/callback
```

#### 自定义应用
如果创建自定义应用，需要在"安全设置"中添加上述重定向URL。

### 5. 应用凭证说明

#### 默认应用凭证
使用默认应用时，凭证已内置在插件中：
- **App ID**: `cli_a83cccd474fe100b`
- **App Secret**: `dDu3S5gfXdSZ6MirgHvuFeoj7bs5xUfm`

#### 自定义应用凭证
如果创建自定义应用，在"凭证与基础信息"页面记录：
- **App ID** (应用ID)
- **App Secret** (应用密钥)

> 📖 **详细文档**: [飞书开放平台文档](https://open.feishu.cn/document/server-docs/api-call-guide/calling-process/get-access-token)

### 6. 配置插件

1. 在Unity编辑器中，打开菜单 `工具 > 飞书`
2. 在"配置"标签页中：

#### 使用默认配置（推荐）
- 插件已预填默认的App ID和App Secret
- 直接点击"保存配置"按钮即可

#### 使用自定义配置
- 输入您自己的飞书应用信息：
  - **飞书应用ID** (App ID)
  - **飞书应用密钥** (App Secret)
- 点击"保存配置"按钮

> 📁 **配置文件位置**: 飞书相关配置会保存在 `Assets/Settings/` 文件夹中
> - `FeiShuConfig.asset` - 应用配置和同步任务配置
> - `FeiShuUserToken.asset` - 用户访问令牌（**不应被版本控制同步**）

### 7. 授权登录

1. 点击"重新授权"按钮
2. 系统会自动打开浏览器进行OAuth2授权
3. 在浏览器中完成飞书账号登录和授权
4. 授权成功后，访问令牌会自动保存到本地

> ⚠️ **注意**: 首次授权需要确保浏览器能够访问 `http://localhost:8080/callback`

### 8. 配置同步任务

在"配置"标签页中添加文件同步配置：

#### 基本配置项
- **文件类型**: 选择导出格式 (xlsx, pdf, docx, csv)
- **导出类型**: 选择内容类型 (sheet, doc, bitable)
- **Token**: 飞书文档/表格的token
- **本地路径**: 保存文件的本地路径

#### 高级配置项
- **子表ID** (仅表格): 指定要导出的具体工作表
- **文件名称**: 自定义导出文件的名称
- **覆盖模式**: 选择是否覆盖已存在的文件

### 9. 开始同步

1. 切换到"同步"标签页
2. 查看已配置的同步任务列表
3. 点击"开始同步数据"按钮
4. 实时查看同步进度和状态
5. 等待所有任务完成

> 💡 **提示**: 可以同时配置多个同步任务，系统会按顺序执行

## 📖 详细使用说明

### 飞书应用配置详解

#### 权限配置说明

在飞书开放平台创建应用时，需要配置以下权限：

```
基础权限：
- offline_access (离线访问) - 允许应用在用户离线时访问数据

文档权限：
- sheets:spreadsheet:read (读取电子表格) - 读取飞书表格内容
- drive:export:readonly (导出云盘文件) - 导出云盘中的文件
- docs:document:export (导出文档) - 导出飞书文档
- vc:export (导出多维表格) - 导出飞书多维表格
```

#### 回调地址配置

回调地址必须设置为：`http://localhost:8080/callback`

> 📖 **详细文档**: [飞书开放平台重定向URL配置](https://open.feishu.cn/document/develop-web-apps/configure-redirect-urls)

### 获取文档Token详解

#### 飞书表格Token获取

1. 打开飞书表格
2. 从URL中提取token，格式如：
   ```
   https://example.feishu.cn/sheets/FILE_TOKEN?sheet=SUB_ID
   ```
3. 将 `FILE_TOKEN` 部分作为token填入配置
4. 将 `SUB_ID` 部分作为子表ID填入配置（可选）

#### 飞书文档Token获取

1. 打开飞书文档
2. 从URL中提取token，格式如：
   ```
   https://example.feishu.cn/docs/FILE_TOKEN
   ```
3. 将 `FILE_TOKEN` 部分作为token填入配置

#### 飞书多维表格Token获取

1. 打开飞书多维表格
2. 从URL中提取token，格式如：
   ```
   https://example.feishu.cn/base/FILE_TOKEN
   ```
3. 将 `FILE_TOKEN` 部分作为token填入配置

### 路径配置说明

#### 支持的路径格式

- **Unity项目根目录相对路径**: 相对于Unity项目根目录的路径

#### 路径配置示例

```
# 保存到项目根目录下的Documents文件夹
Documents/

# 保存到Assets文件夹
Assets/Data/

# 保存到StreamingAssets文件夹
Assets/StreamingAssets/Config/

# 保存到Resources文件夹
Assets/Resources/GameData/
```

### 支持的文件格式

| 导出类型 | 支持格式 | 说明 |
|---------|---------|------|
| sheet | xlsx, csv | 飞书表格 |
| doc | pdf, docx | 飞书文档 |
| bitable | xlsx, csv | 飞书多维表格 |

## 🔧 高级配置

### 配置文件管理

#### 配置文件说明

插件会在 `Assets/Settings/` 文件夹中创建以下配置文件：

- **`FeiShuConfig.asset`**: 包含应用配置和同步任务配置
  - 飞书应用ID和密钥
  - 同步任务列表
  - 权限范围设置
  - ✅ **可以安全地提交到版本控制**

- **`FeiShuUserToken.asset`**: 包含用户访问令牌
  - 访问令牌 (Access Token)
  - 刷新令牌 (Refresh Token)
  - 令牌过期时间
  - ⚠️ **不应提交到版本控制**（包含敏感信息）

#### 版本控制建议

建议在 `.gitignore` 文件中添加以下规则：

```
# 飞书用户令牌文件（包含敏感信息）
Assets/Settings/FeiShuUserToken.asset
Assets/Settings/FeiShuUserToken.asset.meta
```

### 自定义保存路径

可以在配置中指定自定义的本地保存路径，支持以下路径格式：

- **Unity项目根目录相对路径**: 相对于Unity项目根目录的路径

### 批量配置

支持添加多个同步配置，系统会按顺序执行所有配置的同步任务。每个配置可以：

- 设置不同的导出格式
- 指定不同的保存路径
- 配置独立的文件命名规则

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

查看 [CHANGELOG.md](https://github.com/U-NV/FeiShu-Unity-Integration/blob/main/CHANGELOG.md) 了解版本更新详情。

## 🤝 贡献

欢迎提交Issue和Pull Request！

## 📄 许可证

本项目采用 MIT 许可证 - 查看 [LICENSE](LICENSE) 文件了解详情。

## 📞 支持

如果您遇到问题或有任何建议，请：

- 提交 [Issue](https://github.com/U-NV/FeiShu-Unity-Integration/issues)
- 
## 🙏 致谢

感谢飞书开放平台提供的API支持。

---

**注意**: 本插件仅用于Unity编辑器环境，不支持运行时使用。
