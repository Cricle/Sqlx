# Changelog

All notable changes to the **Sqlx IntelliSense** extension will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.0] - 2025-10-01

### 🎉 Initial Release

#### Added
- **SQL Hover Tooltips**: Instantly view complete SQL queries when hovering over methods with Sqlx attributes
- **Dedicated Tool Window**: Browse and explore all Sqlx methods in your solution with smart search and filtering
- **Smart Code Navigation**: F12 support and double-click navigation to jump directly to method definitions
- **Real-time Code Analysis**: Roslyn-based analysis engine with intelligent caching for optimal performance
- **Multi-Attribute Support**: Full support for `[Sqlx]`, `[SqlTemplate]`, and `[ExpressionToSql]` attributes
- **Formatted SQL Display**: Syntax highlighting and proper formatting for better SQL readability
- **Method Explorer Features**:
  - Real-time search across method names, class names, SQL content, and namespaces
  - Method categorization by class and namespace
  - Detailed method information panel with parameters and signatures
  - Refresh functionality to sync with code changes
- **Keyboard Shortcuts**: 
  - `Ctrl+Shift+S, Q` to open Sqlx Methods tool window
  - `F12` for navigation (standard VS behavior extended for Sqlx methods)
- **Performance Optimizations**:
  - Intelligent caching to avoid redundant code analysis
  - Background processing to maintain UI responsiveness
  - Incremental updates on workspace changes

#### Technical Features
- **Visual Studio 2022 Support**: Full compatibility with Community, Professional, and Enterprise editions
- **Roslyn Integration**: Modern code analysis using Microsoft's Roslyn compiler platform
- **WPF Tool Window**: Native Visual Studio tool window with proper theming support
- **MEF Composition**: Standard Visual Studio extension architecture
- **Thread-Safe Operations**: Proper async/await patterns and UI thread marshaling

#### Developer Experience
- **Zero Configuration**: Works out of the box with any Sqlx ORM project
- **Non-Intrusive**: Doesn't interfere with existing Visual Studio functionality
- **Memory Efficient**: Optimized for low memory footprint and fast startup
- **Error Resilient**: Graceful error handling to maintain IDE stability

### 📋 System Requirements
- Visual Studio 2022 (17.0 or later)
- .NET Framework 4.7.2 or higher
- Windows 10/11 (x64)
- Projects using Sqlx ORM (any version)

### 🔗 Resources
- Extension Icon: Custom Sqlx logo (32x32 PNG)
- Documentation: Comprehensive README with usage examples
- License: MIT License
- Support: GitHub Issues for bug reports and feature requests

---

## [Planned] - Future Versions

### 🚧 Roadmap Items
- **SQL Syntax Validation**: Real-time SQL syntax checking and error highlighting
- **Database Schema Integration**: IntelliSense for table and column names
- **SQL Formatting Options**: Customizable SQL display formatting preferences
- **Performance Metrics**: Method execution time estimates and performance tips
- **Multi-Database Support**: Enhanced support for different database dialects
- **Code Snippets**: Pre-built Sqlx code snippets and templates
- **Refactoring Tools**: Rename SQL methods and update references automatically

### 💡 Community Requests
*This section will be updated based on user feedback and feature requests*

---

**Note**: This extension is specifically designed for **Sqlx ORM** users. While it may work with other ORM frameworks that use similar attribute patterns, optimal functionality is achieved when used with Sqlx ORM projects.
