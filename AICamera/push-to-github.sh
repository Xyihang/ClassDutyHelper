#!/bin/bash

# AI相机项目 - GitHub推送脚本
# 使用方法: ./push-to-github.sh YOUR_GITHUB_USERNAME

set -e

USERNAME=$1

if [ -z "$USERNAME" ]; then
    echo "❌ 请提供GitHub用户名"
    echo "用法: ./push-to-github.sh YOUR_GITHUB_USERNAME"
    echo ""
    echo "示例:"
    echo "  ./push-to-github.sh johndoe"
    exit 1
fi

REPO_NAME="AICamera"
REMOTE_URL="https://github.com/$USERNAME/$REPO_NAME.git"

echo "========================================="
echo "  AI相机项目 - GitHub推送"
echo "========================================="
echo ""
echo "GitHub用户名: $USERNAME"
echo "仓库地址: $REMOTE_URL"
echo ""

# 检查远程仓库是否已存在
if git remote | grep -q "origin"; then
    echo "⚠️ 远程仓库已存在，更新为新的地址..."
    git remote set-url origin "$REMOTE_URL"
else
    echo "➕ 添加远程仓库..."
    git remote add origin "$REMOTE_URL"
fi

# 确保分支名为 main
echo "🔄 切换分支名为 main..."
git branch -M main

echo ""
echo "📤 推送到GitHub..."
echo ""

# 尝试推送
if git push -u origin main; then
    echo ""
    echo "✅ 推送成功！"
    echo ""
    echo "📎 仓库地址: https://github.com/$USERNAME/$REPO_NAME"
    echo ""
else
    echo ""
    echo "❌ 推送失败"
    echo ""
    echo "可能的原因："
    echo "  1. GitHub仓库不存在 - 请先访问 https://github.com/new 创建仓库"
    echo "  2. 未登录GitHub - 请检查git凭证或SSH密钥配置"
    echo "  3. 网络问题 - 请检查网络连接"
    echo ""
    echo "手动创建仓库步骤："
    echo "  1. 打开 https://github.com/new"
    echo "  2. 仓库名填写: $REPO_NAME"
    echo "  3. 不要勾选 'Initialize this repository with a README'"
    echo "  4. 点击 Create repository"
    echo "  5. 重新运行此脚本"
    exit 1
fi