# VSIX安装故障排除指南

## 🚨 如果所有安装方法都失败了

### 方案1：手动拖拽安装
1. 打开Visual Studio
2. 将VSIX文件直接拖拽到Visual Studio窗口中
3. 按照安装向导操作

### 方案2：使用Extensions菜单安装
1. 在Visual Studio中：`Extensions` → `Install Extension`
2. 浏览到VSIX文件位置
3. 选择并安装

### 方案3：检查Visual Studio版本兼容性
当前扩展支持：
- Visual Studio 2022 (17.0+)
- Community, Professional, Enterprise版本

如果使用其他版本，需要修改清单文件中的版本范围。

### 方案4：检查系统要求
- .NET Framework 4.8+
- Windows 10/11
- 足够的磁盘空间
- 管理员权限（某些情况下需要）

### 方案5：清理旧扩展
如果之前有安装失败的尝试：
1. 删除：`%LOCALAPPDATA%\Microsoft\VisualStudio\17.0\Extensions\SqlxExtension`
2. 重启Visual Studio
3. 重新安装

### 方案6：查看详细错误日志
1. 启用Visual Studio活动日志：
   ```
   devenv.exe /log
   ```
2. 查看日志文件：
   ```
   %APPDATA%\Microsoft\VisualStudio\17.0\ActivityLog.xml
   ```

### 方案7：替代测试方法
如果扩展安装始终失败，可以：
1. 直接在Visual Studio中测试DLL加载
2. 使用Visual Studio实验实例
3. 创建新的扩展项目模板

## 📞 获取帮助
如果以上方法都不行，请提供：
1. 具体的错误消息
2. Visual Studio版本信息
3. Windows版本信息
4. 安装日志文件内容

## 🔧 快速验证脚本
运行以下命令检查当前状态：
```powershell
powershell -ExecutionPolicy Bypass -File "tools\check-installed-extensions.ps1"
```

