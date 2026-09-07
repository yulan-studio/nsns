# Railway 部署说明（暂不使用持久化存储）

## 部署前提

- Railway 会自动读取仓库根目录的 `Dockerfile`。
- 服务监听 Railway 提供的 `PORT`，并绑定 `0.0.0.0`。
- Python 和 `pypdf` 会在镜像构建时安装。
- `DATA_DIR=/tmp/research-assistant`，因此检索记录和上传 PDF 都是临时文件。

## 使用 GitHub 部署

1. 将本目录提交并推送到一个 GitHub 仓库。不要提交 `work/` 中的 PDF。
2. 登录 Railway，选择 **New Project** → **Deploy from GitHub repo**。
3. 选择该仓库。如果本项目位于仓库子目录，在服务 Settings 中把 **Root Directory** 设为该目录。
4. 等待构建和健康检查通过。Railway 应显示使用了根目录的 Dockerfile。
5. 进入服务 **Settings** → **Networking** → **Generate Domain**。
6. 打开生成的 `https://...up.railway.app` 地址。

本阶段不需要设置 `PORT`、`HOST`、`PYTHON_BIN` 或 `DATA_DIR`；Dockerfile 和 Railway 会提供默认值。

## 使用 Railway CLI 部署

在本目录登录并部署：

```powershell
railway login
railway init
railway up
```

然后在 Railway 服务的 Networking 中生成公开域名。

## 临时存储的重要限制

- 上传的 PDF 和服务器保存的检索 JSON 位于临时文件系统。
- 重新部署、容器替换或服务重启后，这些文件可能消失。
- 浏览器 localStorage 中的研究项目状态可能仍在，但已上传 PDF 的链接会失效，需要重新上传。
- “下载完整项目记录”不包含 PDF 本体。请在自己的电脑上另行保存原始 PDF。
- 不要上传可识别儿童身份的资料。

正式使用前建议增加登录保护；当前公开域名上的任何访问者都能使用网页和上传接口。
