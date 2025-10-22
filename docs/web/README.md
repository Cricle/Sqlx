# Sqlx GitHub Pages

这是 Sqlx 项目的 GitHub Pages 网站源代码。

## 🌐 访问网站

访问 [https://your-username.github.io/Sqlx/](https://your-username.github.io/Sqlx/) 查看在线文档。

## 📁 文件结构

```
web/
├── index.html      # 网站主页
├── .nojekyll       # 禁用 Jekyll
└── README.md       # 本文件
```

## 🚀 部署到 GitHub Pages

### 方法1：通过 GitHub 设置

1. 进入仓库的 **Settings** 页面
2. 找到 **Pages** 选项
3. 在 **Source** 中选择：
   - Branch: `main` (或 `master`)
   - Folder: `/docs/web`
4. 点击 **Save**
5. 等待几分钟，网站将在 `https://your-username.github.io/Sqlx/` 可用

### 方法2：使用 GitHub Actions

创建 `.github/workflows/pages.yml`:

```yaml
name: Deploy to GitHub Pages

on:
  push:
    branches: [ main ]
  workflow_dispatch:

permissions:
  contents: read
  pages: write
  id-token: write

jobs:
  deploy:
    environment:
      name: github-pages
      url: ${{ steps.deployment.outputs.page_url }}
    runs-on: ubuntu-latest
    steps:
      - name: Checkout
        uses: actions/checkout@v3

      - name: Setup Pages
        uses: actions/configure-pages@v3

      - name: Upload artifact
        uses: actions/upload-pages-artifact@v2
        with:
          path: 'docs/web'

      - name: Deploy to GitHub Pages
        id: deployment
        uses: actions/deploy-pages@v2
```

## 🎨 自定义网站

编辑 `index.html` 来自定义网站内容：

- 修改标题、描述
- 更新 GitHub 和 NuGet 链接
- 添加新的文档链接
- 调整样式和布局

## 📝 注意事项

1. **链接更新**：记得将 `index.html` 中的示例链接替换为实际的链接
2. **相对路径**：文档链接使用相对路径 `../` 指向 docs 文件夹
3. **响应式设计**：网站已优化移动端显示
4. **浏览器兼容**：支持现代浏览器（Chrome, Firefox, Safari, Edge）

## 🔧 本地测试

使用任何 HTTP 服务器在本地预览：

```bash
# 使用 Python
cd docs/web
python -m http.server 8000

# 使用 Node.js (需要安装 http-server)
cd docs/web
npx http-server

# 使用 .NET
cd docs/web
dotnet serve
```

然后访问 http://localhost:8000

---

**提示**：首次部署后，可能需要几分钟才能看到网站上线。


