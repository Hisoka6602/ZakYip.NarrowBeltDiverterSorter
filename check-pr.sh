#!/bin/bash

# PR 提交前检查脚本
# 检查代码规范、构建、测试、技术债务等

set -e

echo "======================================"
echo "  PR 提交前检查"
echo "======================================"
echo ""

# 1. 检查技术债务文档
echo "📋 检查技术债务文档..."
if [ ! -f "docs/Conventions/技术债务.md" ]; then
    echo "❌ 技术债务文档不存在！"
    echo "   请创建 docs/Conventions/技术债务.md"
    exit 1
fi
echo "✅ 技术债务文档存在"
echo ""

# 2. 运行影分身检测
echo "🔍 运行影分身代码检测..."
if [ -f "scripts/detect-shadow-clones.sh" ]; then
    ./scripts/detect-shadow-clones.sh
else
    echo "⚠️  影分身检测脚本不存在，跳过此检查"
fi
echo ""

# 3. 检查UTC时间使用（包括测试代码）
echo "🔍 检查UTC时间使用（包括测试代码）..."
UTC_COUNT=$(grep -r "DateTime\.UtcNow\|DateTimeOffset\.UtcNow" --include="*.cs" . | grep -v "bin\|obj\|// 边界转换\|// UTC required" | wc -l)
if [ $UTC_COUNT -gt 0 ]; then
    echo "❌ 发现 $UTC_COUNT 处使用UTC时间，必须修复！"
    grep -r "DateTime\.UtcNow\|DateTimeOffset\.UtcNow" --include="*.cs" . | grep -v "bin\|obj\|// 边界转换\|// UTC required"
    exit 1
fi
echo "✅ 没有使用UTC时间"
echo ""

# 4. 检查是否有未记录的技术债务
echo "📝 检查未记录的技术债务标记..."
TODO_COUNT=$(grep -r "TODO\|FIXME\|HACK" --include="*.cs" . 2>/dev/null | grep -v "/bin/\|/obj/\|技术债务.md" | wc -l)
if [ $TODO_COUNT -gt 27 ]; then
    echo "⚠️  发现 $TODO_COUNT 个技术债务标记（上次记录：27）"
    echo "   请确保新增的技术债务已记录在 docs/Conventions/技术债务.md"
    echo ""
    echo "新增的技术债务标记："
    grep -rn "TODO\|FIXME\|HACK" --include="*.cs" . 2>/dev/null | grep -v "/bin/\|/obj/\|技术债务.md" | tail -5
    echo ""
    echo "⚠️  警告：请在 PR 中说明这些新增技术债务"
fi
echo ""

# 5. 构建项目
echo "🏗️  构建项目..."
dotnet build
if [ $? -ne 0 ]; then
    echo "❌ 构建失败"
    exit 1
fi
echo "✅ 构建成功"
echo ""

# 6. 运行测试
echo "🧪 运行测试..."
dotnet test --no-build
if [ $? -ne 0 ]; then
    echo "❌ 测试失败"
    exit 1
fi
echo "✅ 所有测试通过"
echo ""

# 总结
echo "======================================"
echo "  检查完成"
echo "======================================"
echo ""
echo "✅ 所有强制性检查通过"
echo ""
echo "📋 PR 提交检查清单："
echo "  ✅ 技术债务文档存在"
echo "  ✅ 没有违规使用 UTC 时间"
echo "  ✅ 构建成功"
echo "  ✅ 所有测试通过"
echo ""
echo "请确认以下内容："
echo "  - [ ] 已通读 docs/Conventions/技术债务.md"
echo "  - [ ] 新增技术债务已记录到文档"
echo "  - [ ] 高优先级技术债务已解决或有说明"
echo "  - [ ] PR 描述中包含技术债务变更说明"
echo ""
echo "✅ 可以提交 PR"
