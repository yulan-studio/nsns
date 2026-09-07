# Railway 部署与 Cloudflare R2 PDF 存储

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

不需要设置 `PORT`、`HOST`、`PYTHON_BIN` 或 `DATA_DIR`；Dockerfile 和 Railway 会提供默认值。

## 配置登录

在Railway服务的 **Variables** 中设置以下变量，然后重新部署：

```text
AUTH_USERNAME=nsns
AUTH_PASSWORD=请设置为约定的密码
```

密码只保存在Railway环境变量中，不要提交到GitHub。生产环境未配置这两个变量时，网站不会允许登录。会话保存在当前服务进程中，有效期12小时；Railway重启或重新部署后需要重新登录。

## 使用 Cloudflare R2 永久保存 PDF

先在 Cloudflare R2 创建一个私有 Bucket，并创建仅允许该 Bucket **Object Read & Write** 的 R2 S3 API Token。然后在 Railway 服务的 **Variables** 中添加：

```text
R2_ACCOUNT_ID=Cloudflare Account ID
R2_ACCESS_KEY_ID=R2 Token 的 Access Key ID
R2_SECRET_ACCESS_KEY=R2 Token 的 Secret Access Key
R2_BUCKET_NAME=Bucket 名称
```

四项必须同时存在。保存变量后 Railway 会重新部署。打开：

```text
https://你的域名/health
```

看到 `"pdf_storage": "cloudflare-r2"` 表示连接方式已启用。之后上传的 PDF 会先在 Railway 临时目录解析，再写入私有 R2；网页仍通过 `/api/uploads/...` 打开文件，不会暴露 R2 密钥或公开 Bucket。

旧的本地 PDF 不会自动迁移到 R2，需要重新上传。请勿上传包含可识别儿童身份的数据。

## 使用 Railway CLI 部署

在本目录登录并部署：

```powershell
railway login
railway init
railway up
```

然后在 Railway 服务的 Networking 中生成公开域名。

## 未配置 R2 时的临时存储限制

- 上传的 PDF 和服务器保存的检索 JSON 位于临时文件系统。
- 重新部署、容器替换或服务重启后，这些文件可能消失。
- 浏览器 localStorage 中的研究项目状态可能仍在，但已上传 PDF 的链接会失效，需要重新上传。
- “下载完整项目记录”不包含 PDF 本体。请在自己的电脑上另行保存原始 PDF。
- 不要上传可识别儿童身份的资料。

正式使用前建议增加登录保护；当前公开域名上的任何访问者都能使用网页和上传接口。
