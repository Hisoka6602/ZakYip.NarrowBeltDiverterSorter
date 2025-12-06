#!/bin/bash

# 影分身代码检测脚本
# 用于检测项目中的重复代码（影分身）

set -e

RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

echo ""
echo "======================================"
echo "  影分身代码检测 (Shadow Clone Detection)"
echo "======================================"
echo ""

ISSUES_FOUND=0

# 1. 检测重复的类名
echo -e "${BLUE}[1/7] 检测重复的类名...${NC}"
DUPLICATE_CLASSES=$(find . -name "*.cs" -type f | grep -v "/bin/" | grep -v "/obj/" | \
    xargs grep -h "^public class\|^public interface\|^public record\|^internal class" | \
    sed 's/\s\+/ /g' | sort | uniq -c | grep -v "^ *1 " | wc -l)

if [ "$DUPLICATE_CLASSES" -gt 0 ]; then
    echo -e "${YELLOW}⚠️  发现 $DUPLICATE_CLASSES 个重复的类/接口/记录定义${NC}"
    echo "详细信息："
    find . -name "*.cs" -type f | grep -v "/bin/" | grep -v "/obj/" | \
        xargs grep -h "^public class\|^public interface\|^public record\|^internal class" | \
        sed 's/\s\+/ /g' | sort | uniq -c | grep -v "^ *1 " | head -10
    ISSUES_FOUND=$((ISSUES_FOUND + 1))
    echo ""
else
    echo -e "${GREEN}✅ 没有发现重复的类定义${NC}"
fi

# 2. 检测重复的事件参数类型
echo ""
echo -e "${BLUE}[2/7] 检测重复的事件参数类型...${NC}"
DUPLICATE_EVENTS=$(find . -name "*.cs" -type f | grep -v "/bin/" | grep -v "/obj/" | \
    xargs grep -h "public record.*EventArgs" | \
    sed 's/\s\+/ /g' | sort | uniq -c | grep -v "^ *1 " | wc -l)

if [ "$DUPLICATE_EVENTS" -gt 0 ]; then
    echo -e "${YELLOW}⚠️  发现 $DUPLICATE_EVENTS 个重复的事件参数定义${NC}"
    echo "详细信息："
    find . -name "*.cs" -type f | grep -v "/bin/" | grep -v "/obj/" | \
        xargs grep -h "public record.*EventArgs" | \
        sed 's/\s\+/ /g' | sort | uniq -c | grep -v "^ *1 " | head -10
    ISSUES_FOUND=$((ISSUES_FOUND + 1))
    echo ""
else
    echo -e "${GREEN}✅ 没有发现重复的事件参数定义${NC}"
fi

# 3. 检测重复的 DTO 类型
echo ""
echo -e "${BLUE}[3/7] 检测重复的 DTO 类型...${NC}"
DUPLICATE_DTOS=$(find . -name "*.cs" -type f | grep -v "/bin/" | grep -v "/obj/" | \
    xargs grep -h "public record.*Dto" | \
    sed 's/\s\+/ /g' | sort | uniq -c | grep -v "^ *1 " | wc -l)

if [ "$DUPLICATE_DTOS" -gt 0 ]; then
    echo -e "${YELLOW}⚠️  发现 $DUPLICATE_DTOS 个重复的 DTO 定义${NC}"
    echo "详细信息："
    find . -name "*.cs" -type f | grep -v "/bin/" | grep -v "/obj/" | \
        xargs grep -h "public record.*Dto" | \
        sed 's/\s\+/ /g' | sort | uniq -c | grep -v "^ *1 " | head -10
    ISSUES_FOUND=$((ISSUES_FOUND + 1))
    echo ""
else
    echo -e "${GREEN}✅ 没有发现重复的 DTO 定义${NC}"
fi

# 4. 检测技术债务标记
echo ""
echo -e "${BLUE}[4/7] 检测技术债务标记 (TODO/FIXME/HACK)...${NC}"
TODO_COUNT=$(grep -r "TODO" --include="*.cs" . 2>/dev/null | grep -v "/bin/\|/obj/\|技术债务.md" | wc -l)
FIXME_COUNT=$(grep -r "FIXME" --include="*.cs" . 2>/dev/null | grep -v "/bin/\|/obj/" | wc -l)
HACK_COUNT=$(grep -r "HACK" --include="*.cs" . 2>/dev/null | grep -v "/bin/\|/obj/" | wc -l)
TOTAL_DEBT=$((TODO_COUNT + FIXME_COUNT + HACK_COUNT))

if [ "$TOTAL_DEBT" -gt 0 ]; then
    echo -e "${YELLOW}⚠️  发现 $TOTAL_DEBT 个技术债务标记${NC}"
    echo "  - TODO: $TODO_COUNT"
    echo "  - FIXME: $FIXME_COUNT"
    echo "  - HACK: $HACK_COUNT"
    ISSUES_FOUND=$((ISSUES_FOUND + 1))
    echo ""
    echo "请确保这些技术债务已记录在 docs/Conventions/技术债务.md 中"
else
    echo -e "${GREEN}✅ 没有发现技术债务标记${NC}"
fi

# 5. 检测配置硬编码
echo ""
echo -e "${BLUE}[5/7] 检测配置硬编码问题...${NC}"
HARDCODED_CONFIG=$(grep -rn "TODO.*配置\|TODO.*LiteDB\|硬编码" --include="*.cs" . 2>/dev/null | \
    grep -v "/bin/\|/obj/\|技术债务.md" | wc -l)

if [ "$HARDCODED_CONFIG" -gt 0 ]; then
    echo -e "${YELLOW}⚠️  发现 $HARDCODED_CONFIG 处配置硬编码或待迁移配置${NC}"
    echo "详细信息："
    grep -rn "TODO.*配置\|TODO.*LiteDB\|硬编码" --include="*.cs" . 2>/dev/null | \
        grep -v "/bin/\|/obj/\|技术债务.md" | head -5
    ISSUES_FOUND=$((ISSUES_FOUND + 1))
    echo ""
else
    echo -e "${GREEN}✅ 没有发现配置硬编码问题${NC}"
fi

# 6. 检测重复的方法签名
echo ""
echo -e "${BLUE}[6/7] 检测重复的方法签名...${NC}"

# 阈值说明：
# - 出现 1-3 次被认为是合理的（接口实现、测试方法等）
# - 超过 3 次（即 4 次或更多）表示可能存在过度重复，需要考虑提取公共基类或模式
# - 如果发现超过 3 种不同的方法签名出现 4+ 次，则报告警告
DUPLICATE_METHOD_TYPES_THRESHOLD=3

DUPLICATE_METHODS=$(find . -name "*.cs" -type f | grep -v "/bin/" | grep -v "/obj/" | \
    xargs grep -h "public.*Task.*Async\|public.*void\|public.*bool" | \
    sed 's/\s\+/ /g' | sort | uniq -c | grep -v "^ *1 \|^ *2 \|^ *3 " | wc -l)

if [ "$DUPLICATE_METHODS" -gt "$DUPLICATE_METHOD_TYPES_THRESHOLD" ]; then
    echo -e "${YELLOW}⚠️  发现 $DUPLICATE_METHODS 个高度重复的方法签名（出现4次或更多）${NC}"
    echo "详细信息（前5个）："
    find . -name "*.cs" -type f | grep -v "/bin/" | grep -v "/obj/" | \
        xargs grep -h "public.*Task.*Async\|public.*void\|public.*bool" | \
        sed 's/\s\+/ /g' | sort | uniq -c | grep -v "^ *1 \|^ *2 \|^ *3 " | head -5
    ISSUES_FOUND=$((ISSUES_FOUND + 1))
    echo ""
else
    echo -e "${GREEN}✅ 方法签名重复在合理范围内${NC}"
fi

# 7. 检测未使用 UTC 时间的情况（应该使用本地时间）
echo ""
echo -e "${BLUE}[7/7] 检测 UTC 时间使用（应使用本地时间）...${NC}"
UTC_USAGE=$(grep -rn "DateTime\.UtcNow\|DateTimeOffset\.UtcNow\|ToUniversalTime" --include="*.cs" . 2>/dev/null | \
    grep -v "/bin/\|/obj/\|Tests\|// 边界转换\|// UTC required" | wc -l)

if [ "$UTC_USAGE" -gt 0 ]; then
    echo -e "${RED}❌ 发现 $UTC_USAGE 处使用 UTC 时间（违反项目规范）${NC}"
    echo "详细信息："
    grep -rn "DateTime\.UtcNow\|DateTimeOffset\.UtcNow\|ToUniversalTime" --include="*.cs" . 2>/dev/null | \
        grep -v "/bin/\|/obj/\|Tests\|// 边界转换\|// UTC required" | head -5
    ISSUES_FOUND=$((ISSUES_FOUND + 1))
    echo ""
else
    echo -e "${GREEN}✅ 没有违规使用 UTC 时间${NC}"
fi

# 总结
echo ""
echo "======================================"
echo "  检测总结"
echo "======================================"

if [ "$ISSUES_FOUND" -eq 0 ]; then
    echo -e "${GREEN}🎉 太棒了！没有发现影分身代码或技术债务问题${NC}"
    exit 0
else
    echo -e "${YELLOW}⚠️  发现 $ISSUES_FOUND 类问题需要关注${NC}"
    echo ""
    echo "下一步行动："
    echo "1. 查看上述详细信息"
    echo "2. 确认这些问题已记录在 docs/Conventions/技术债务.md"
    echo "3. 如有新问题，请更新技术债务文档"
    echo "4. 优先解决高优先级技术债务"
    echo ""
    echo "注意：此脚本仅用于检测，不会阻止 PR 提交"
    exit 0  # 不阻止 PR，仅警告
fi
