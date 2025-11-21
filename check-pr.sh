#!/bin/bash

echo "🔍 检查UTC时间使用（包括测试代码）..."
UTC_COUNT=$(grep -r "DateTime\.UtcNow\|DateTimeOffset\.UtcNow" --include="*.cs" . | grep -v "bin\|obj" | wc -l)
if [ $UTC_COUNT -gt 0 ]; then
    echo "❌ 发现 $UTC_COUNT 处使用UTC时间，必须修复！"
    grep -r "DateTime\.UtcNow\|DateTimeOffset\.UtcNow" --include="*.cs" . | grep -v "bin\|obj"
    exit 1
fi
echo "✅ 没有使用UTC时间"

echo "🏗️  构建项目..."
dotnet build
if [ $? -ne 0 ]; then
    echo "❌ 构建失败"
    exit 1
fi
echo "✅ 构建成功"

echo "🧪 运行测试..."
dotnet test --no-build
if [ $? -ne 0 ]; then
    echo "❌ 测试失败"
    exit 1
fi
echo "✅ 所有测试通过"

echo ""
echo "✅ 所有检查通过，可以提交PR"
