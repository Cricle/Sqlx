#!/bin/bash

# Sqlx NuGet包自动化发布脚本
# 用法: ./push-nuget.sh [-v version] [-k api-key] [-s source] [-d] [-t]
#   -v, --version    指定版本号
#   -k, --api-key    NuGet API Key
#   -s, --source     NuGet源 (默认: https://api.nuget.org/v3/index.json)
#   -d, --dry-run    模拟运行，不实际推送
#   -t, --skip-tests 跳过测试
#   -h, --help       显示帮助

set -e  # 遇到错误时退出

# 默认值
VERSION=""
API_KEY=""
SOURCE="https://api.nuget.org/v3/index.json"
DRY_RUN=false
SKIP_TESTS=false
PROJECT_FILE="Sqlx/Sqlx.csproj"

# 颜色输出
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

log_info() { echo -e "${CYAN}[INFO]${NC} $1"; }
log_success() { echo -e "${GREEN}[SUCCESS]${NC} $1"; }
log_warning() { echo -e "${YELLOW}[WARNING]${NC} $1"; }
log_error() { echo -e "${RED}[ERROR]${NC} $1"; }

# 显示帮助
show_help() {
    cat << EOF
Sqlx NuGet包自动化发布脚本

用法: $0 [选项]

选项:
    -v, --version VERSION     指定版本号 (如果未指定，将从项目文件或使用时间戳生成)
    -k, --api-key API_KEY     NuGet API Key (如果未指定，将提示输入)
    -s, --source SOURCE       NuGet源 (默认: https://api.nuget.org/v3/index.json)
    -d, --dry-run             模拟运行，不实际推送包
    -t, --skip-tests          跳过单元测试
    -h, --help                显示此帮助信息

示例:
    $0 -v "1.2.3" -k "oy2..." 
    $0 --version "1.2.3" --api-key "oy2..." --source "https://api.nuget.org/v3/index.json"
    $0 -d  # 模拟运行
    $0 -t  # 跳过测试

环境要求:
    - .NET SDK 6.0 或更高版本
    - dotnet 命令可用

EOF
}

# 解析命令行参数
parse_args() {
    while [[ $# -gt 0 ]]; do
        case $1 in
            -v|--version)
                VERSION="$2"
                shift 2
                ;;
            -k|--api-key)
                API_KEY="$2"
                shift 2
                ;;
            -s|--source)
                SOURCE="$2"
                shift 2
                ;;
            -d|--dry-run)
                DRY_RUN=true
                shift
                ;;
            -t|--skip-tests)
                SKIP_TESTS=true
                shift
                ;;
            -h|--help)
                show_help
                exit 0
                ;;
            *)
                log_error "未知参数: $1"
                show_help
                exit 1
                ;;
        esac
    done
}

# 检查dotnet命令
check_dotnet() {
    if ! command -v dotnet &> /dev/null; then
        log_error "未找到dotnet命令。请确保已安装.NET SDK。"
        exit 1
    fi
    
    log_info "dotnet版本: $(dotnet --version)"
}

# 获取项目版本
get_project_version() {
    if [[ -n "$VERSION" ]]; then
        echo "$VERSION"
        return
    fi
    
    if [[ -f "$PROJECT_FILE" ]]; then
        # 尝试从项目文件提取版本
        version=$(grep -oP '<Version>\K[^<]+' "$PROJECT_FILE" 2>/dev/null || true)
        if [[ -z "$version" ]]; then
            version=$(grep -oP '<PackageVersion>\K[^<]+' "$PROJECT_FILE" 2>/dev/null || true)
        fi
        
        if [[ -n "$version" ]]; then
            echo "$version"
            return
        fi
    fi
    
    # 使用时间戳生成版本
    current_date=$(date +"%Y.%m.%d")
    build_number=$(date +"%H%M")
    echo "${current_date}.${build_number}"
}

# 清理输出目录
clean_output() {
    log_info "清理输出目录..."
    
    find . -name "bin" -type d -exec rm -rf {} + 2>/dev/null || true
    find . -name "obj" -type d -exec rm -rf {} + 2>/dev/null || true
    find . -name "*.nupkg" -type f -delete 2>/dev/null || true
    
    log_success "清理完成"
}

# 运行测试
run_tests() {
    if [[ "$SKIP_TESTS" == true ]]; then
        log_warning "跳过测试 (使用了 --skip-tests 参数)"
        return 0
    fi
    
    log_info "运行单元测试..."
    
    test_projects=$(find . -name "*.Tests.csproj" -type f)
    if [[ -z "$test_projects" ]]; then
        log_warning "未找到测试项目"
        return 0
    fi
    
    for test_project in $test_projects; do
        log_info "运行测试: $test_project"
        if ! dotnet test "$test_project" --configuration Release --verbosity minimal; then
            log_error "测试失败: $test_project"
            return 1
        fi
    done
    
    log_success "所有测试通过!"
    return 0
}

# 构建项目
build_project() {
    local project_path="$1"
    local package_version="$2"
    
    log_info "构建项目: $project_path (版本: $package_version)"
    
    if ! dotnet build "$project_path" \
        --configuration Release \
        --verbosity minimal \
        "/p:PackageVersion=$package_version" \
        "/p:Version=$package_version"; then
        log_error "构建失败: $project_path"
        return 1
    fi
    
    log_success "构建成功: $project_path"
    return 0
}

# 打包项目
pack_project() {
    local project_path="$1"
    local package_version="$2"
    
    log_info "打包项目: $project_path"
    
    if ! dotnet pack "$project_path" \
        --configuration Release \
        --output . \
        --verbosity minimal \
        "/p:PackageVersion=$package_version" \
        "/p:Version=$package_version" \
        --no-build; then
        log_error "打包失败: $project_path"
        return 1
    fi
    
    log_success "打包成功: $project_path"
    return 0
}

# 推送到NuGet
push_to_nuget() {
    local package_path="$1"
    local nuget_api_key="$2"
    local nuget_source="$3"
    
    if [[ "$DRY_RUN" == true ]]; then
        log_warning "模拟运行: 将推送 $package_path 到 $nuget_source"
        return 0
    fi
    
    if [[ -z "$nuget_api_key" ]]; then
        echo -n "请输入NuGet API Key: "
        read -s nuget_api_key
        echo
    fi
    
    log_info "推送包到NuGet: $package_path"
    
    if ! dotnet nuget push "$package_path" \
        --api-key "$nuget_api_key" \
        --source "$nuget_source" \
        --verbosity minimal; then
        log_error "推送失败: $package_path"
        return 1
    fi
    
    log_success "推送成功: $package_path"
    return 0
}

# 主函数
main() {
    log_info "=== Sqlx NuGet包发布脚本 ==="
    log_info "源码目录: $(pwd)"
    log_info "目标源: $SOURCE"
    
    # 检查环境
    check_dotnet
    
    # 检查项目文件
    if [[ ! -f "$PROJECT_FILE" ]]; then
        log_error "未找到项目文件: $PROJECT_FILE"
        exit 1
    fi
    
    # 获取版本号
    package_version=$(get_project_version)
    log_info "包版本: $package_version"
    
    # 执行构建流程
    clean_output
    
    if ! run_tests; then
        exit 1
    fi
    
    if ! build_project "$PROJECT_FILE" "$package_version"; then
        exit 1
    fi
    
    if ! pack_project "$PROJECT_FILE" "$package_version"; then
        exit 1
    fi
    
    # 查找生成的包
    package_path="Sqlx.${package_version}.nupkg"
    if [[ ! -f "$package_path" ]]; then
        log_error "未找到生成的包: $package_path"
        exit 1
    fi
    
    # 推送到NuGet
    if ! push_to_nuget "$package_path" "$API_KEY" "$SOURCE"; then
        exit 1
    fi
    
    log_success "✅ NuGet包发布成功!"
    log_info "包名: Sqlx"
    log_info "版本: $package_version"
    log_info "源: $SOURCE"
}

# 解析参数并运行
parse_args "$@"
main



