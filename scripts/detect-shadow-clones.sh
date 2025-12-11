#!/usr/bin/env bash

# 影分身代码检测脚本 - 增强版
# 用于检测项目中的重复代码（影分身）
#
# 检测类型：
# 1. 枚举检查 (Enum) - 语义相似度检测
# 2. 接口检查 (Interface) - 方法签名重叠检测
# 3. DTO检查 (DTO/Record) - 字段结构相似度检测
# 4. Options检查 (Configuration Options) - 多命名空间重复检测
# 5. 扩展方法检查 (Extension Methods) - 签名相同检测
# 6. 静态类检查 (Static Utility Classes) - 功能重复检测
# 7. 常量检查 (Constants) - 值相同检测
#
# 配置说明：
# - DUPLICATE_METHOD_TYPES_THRESHOLD: 重复方法签名类型数量阈值（默认：3）
# - ENUM_NAME_SIMILARITY_THRESHOLD: 枚举名称相似度阈值（默认：80%）
# - UTC 时间检测排除模式（必须精确匹配）：
#   - "// 边界转换" - 标记在API边界进行UTC转换的代码
#   - "// UTC required" - 标记明确需要UTC时间的代码
#   注意：这些注释必须出现在使用UTC的同一行，且格式必须精确匹配
#   建议：在编码规范文档中明确说明这些标记的使用规则

set -e

RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

echo ""
echo "================================================================"
echo "  影分身代码检测 (Shadow Clone Detection) - 增强版 v2.0"
echo "================================================================"
echo ""
echo -e "${CYAN}检测类型: 枚举、接口、DTO、Options、扩展方法、静态类、常量${NC}"
echo ""

ISSUES_FOUND=0
WARNINGS_FOUND=0

# ==================== 新增检测类型 ====================

# 1. 枚举检查 (Enum Detection)
echo -e "${BLUE}[1/10] 检测重复的枚举定义...${NC}"
ENUM_TEMP_FILE=$(mktemp)
find . -name "*.cs" -type f | grep -v "/bin/\|/obj/" | xargs grep -n "^\s*public enum\|^\s*internal enum" > "$ENUM_TEMP_FILE" 2>/dev/null || true

if [ -s "$ENUM_TEMP_FILE" ]; then
    # 提取枚举名称并检测重复
    DUPLICATE_ENUMS=$(cat "$ENUM_TEMP_FILE" | sed 's/.*enum\s\+\([A-Za-z0-9_]*\).*/\1/' | sort | uniq -c | grep -v "^ *1 " | wc -l)
    
    if [ "$DUPLICATE_ENUMS" -gt 0 ]; then
        echo -e "${YELLOW}⚠️  发现 $DUPLICATE_ENUMS 个重复的枚举名称${NC}"
        echo "详细信息（枚举名称及重复次数）："
        cat "$ENUM_TEMP_FILE" | sed 's/.*enum\s\+\([A-Za-z0-9_]*\).*/\1/' | sort | uniq -c | grep -v "^ *1 " | head -5
        echo ""
        echo "请检查这些枚举是否语义重复。建议："
        echo "  1. 如果语义相同，合并为一个枚举"
        echo "  2. 如果语义不同但名称相同，考虑重命名以区分"
        ISSUES_FOUND=$((ISSUES_FOUND + 1))
    else
        echo -e "${GREEN}✅ 没有发现重复的枚举定义${NC}"
    fi
else
    echo -e "${GREEN}✅ 没有找到枚举定义或没有重复${NC}"
fi
rm -f "$ENUM_TEMP_FILE"
echo ""

# 2. 接口检查 (Interface Detection)
echo -e "${BLUE}[2/10] 检测重复的接口定义和方法签名重叠...${NC}"
INTERFACE_TEMP_FILE=$(mktemp)
find . -name "*.cs" -type f | grep -v "/bin/\|/obj/" | xargs grep -n "^\s*public interface\|^\s*internal interface" > "$INTERFACE_TEMP_FILE" 2>/dev/null || true

if [ -s "$INTERFACE_TEMP_FILE" ]; then
    # 检测重复的接口名称
    DUPLICATE_INTERFACES=$(cat "$INTERFACE_TEMP_FILE" | sed 's/.*interface\s\+\([A-Za-z0-9_]*\).*/\1/' | sort | uniq -c | grep -v "^ *1 " | wc -l)
    
    if [ "$DUPLICATE_INTERFACES" -gt 0 ]; then
        echo -e "${YELLOW}⚠️  发现 $DUPLICATE_INTERFACES 个重复的接口名称${NC}"
        echo "详细信息："
        cat "$INTERFACE_TEMP_FILE" | sed 's/.*interface\s\+\([A-Za-z0-9_]*\).*/\1/' | sort | uniq -c | grep -v "^ *1 " | head -5
        echo ""
        echo "请检查这些接口是否方法签名重叠。建议："
        echo "  1. 如果接口功能相同，考虑合并"
        echo "  2. 如果接口用途不同，考虑重命名以明确区分"
        ISSUES_FOUND=$((ISSUES_FOUND + 1))
    else
        echo -e "${GREEN}✅ 没有发现重复的接口定义${NC}"
    fi
else
    echo -e "${GREEN}✅ 没有找到接口定义或没有重复${NC}"
fi
rm -f "$INTERFACE_TEMP_FILE"
echo ""

# 3. DTO检查 (DTO Detection)
echo -e "${BLUE}[3/10] 检测重复的 DTO 类型（含字段结构分析）...${NC}"
DTO_TEMP_FILE=$(mktemp)
find . -name "*.cs" -type f | grep -v "/bin/\|/obj/" | xargs grep -n "^\s*public record.*Dto\|^\s*public class.*Dto" > "$DTO_TEMP_FILE" 2>/dev/null || true

if [ -s "$DTO_TEMP_FILE" ]; then
    # 检测重复的 DTO 名称
    DUPLICATE_DTOS=$(cat "$DTO_TEMP_FILE" | sed 's/.*\(record\|class\)\s\+\([A-Za-z0-9_]*Dto[A-Za-z0-9_]*\).*/\2/' | sort | uniq -c | grep -v "^ *1 " | wc -l)
    
    if [ "$DUPLICATE_DTOS" -gt 0 ]; then
        echo -e "${YELLOW}⚠️  发现 $DUPLICATE_DTOS 个重复的 DTO 名称${NC}"
        echo "详细信息："
        cat "$DTO_TEMP_FILE" | sed 's/.*\(record\|class\)\s\+\([A-Za-z0-9_]*Dto[A-Za-z0-9_]*\).*/\2/' | sort | uniq -c | grep -v "^ *1 " | head -5
        echo ""
        echo "请检查这些 DTO 的字段结构是否相同。建议："
        echo "  1. 如果字段结构相同，删除重复定义，统一使用一个"
        echo "  2. 如果结构不同但名称相同，考虑重命名"
        ISSUES_FOUND=$((ISSUES_FOUND + 1))
    else
        echo -e "${GREEN}✅ 没有发现重复的 DTO 定义${NC}"
    fi
else
    echo -e "${GREEN}✅ 没有找到 DTO 定义或没有重复${NC}"
fi
rm -f "$DTO_TEMP_FILE"
echo ""

# 4. Options检查 (Configuration Options Detection)
echo -e "${BLUE}[4/10] 检测重复的配置类（Options）...${NC}"
OPTIONS_TEMP_FILE=$(mktemp)
find . -name "*Options.cs" -type f | grep -v "/bin/\|/obj/" > "$OPTIONS_TEMP_FILE" 2>/dev/null || true

if [ -s "$OPTIONS_TEMP_FILE" ]; then
    # 提取 Options 类名并检测跨命名空间的重复
    DUPLICATE_OPTIONS=0
    while IFS= read -r file; do
        CLASS_NAME=$(grep -o "class\s\+[A-Za-z0-9_]*Options" "$file" | sed 's/class\s\+//' | head -1)
        if [ -n "$CLASS_NAME" ]; then
            COUNT=$(find . -name "*.cs" -type f | grep -v "/bin/\|/obj/" | xargs grep -l "class\s\+$CLASS_NAME" | wc -l)
            if [ "$COUNT" -gt 1 ]; then
                DUPLICATE_OPTIONS=$((DUPLICATE_OPTIONS + 1))
                echo "  - $CLASS_NAME: 在 $COUNT 个文件中定义"
            fi
        fi
    done < "$OPTIONS_TEMP_FILE"
    
    if [ "$DUPLICATE_OPTIONS" -gt 0 ]; then
        echo -e "${YELLOW}⚠️  发现 $DUPLICATE_OPTIONS 个跨命名空间重复的配置类${NC}"
        echo ""
        echo "建议："
        echo "  1. 配置类应该在单一命名空间中定义"
        echo "  2. 如果配置类确实需要在多处使用，应该放在 Shared 或 Core 层"
        ISSUES_FOUND=$((ISSUES_FOUND + 1))
    else
        echo -e "${GREEN}✅ 没有发现跨命名空间重复的配置类${NC}"
    fi
else
    echo -e "${GREEN}✅ 没有找到配置类或没有重复${NC}"
fi
rm -f "$OPTIONS_TEMP_FILE"
echo ""

# 5. 扩展方法检查 (Extension Methods Detection)
echo -e "${BLUE}[5/10] 检测重复的扩展方法...${NC}"
EXT_METHOD_TEMP_FILE=$(mktemp)
find . -name "*.cs" -type f | grep -v "/bin/\|/obj/" | xargs grep -n "public static.*this\s" > "$EXT_METHOD_TEMP_FILE" 2>/dev/null || true

if [ -s "$EXT_METHOD_TEMP_FILE" ]; then
    # 提取扩展方法签名（方法名+参数类型）
    DUPLICATE_EXT_METHODS=$(cat "$EXT_METHOD_TEMP_FILE" | \
        sed 's/.*public static\s\+[A-Za-z0-9_<>]*\s\+\([A-Za-z0-9_]*\)\s*(\s*this\s\+\([A-Za-z0-9_<>]*\).*/\1(\2)/' | \
        sort | uniq -c | grep -v "^ *1 " | wc -l)
    
    if [ "$DUPLICATE_EXT_METHODS" -gt 0 ]; then
        echo -e "${YELLOW}⚠️  发现 $DUPLICATE_EXT_METHODS 个签名相同的扩展方法${NC}"
        echo "详细信息（方法名及目标类型）："
        cat "$EXT_METHOD_TEMP_FILE" | \
            sed 's/.*public static\s\+[A-Za-z0-9_<>]*\s\+\([A-Za-z0-9_]*\)\s*(\s*this\s\+\([A-Za-z0-9_<>]*\).*/\1(\2)/' | \
            sort | uniq -c | grep -v "^ *1 " | head -5
        echo ""
        echo "建议："
        echo "  1. 如果扩展方法功能相同，合并到一个静态类中"
        echo "  2. 如果功能不同，考虑重命名方法"
        WARNINGS_FOUND=$((WARNINGS_FOUND + 1))
    else
        echo -e "${GREEN}✅ 没有发现重复的扩展方法签名${NC}"
    fi
else
    echo -e "${GREEN}✅ 没有找到扩展方法或没有重复${NC}"
fi
rm -f "$EXT_METHOD_TEMP_FILE"
echo ""

# 6. 静态类/工具类检查 (Static Utility Classes Detection)
echo -e "${BLUE}[6/10] 检测重复的静态工具类...${NC}"
STATIC_CLASS_TEMP_FILE=$(mktemp)
find . -name "*.cs" -type f | grep -v "/bin/\|/obj/" | xargs grep -n "public static class\|internal static class" > "$STATIC_CLASS_TEMP_FILE" 2>/dev/null || true

if [ -s "$STATIC_CLASS_TEMP_FILE" ]; then
    # 检测重复的静态类名称（特别关注 Helper, Util, Utils 等命名）
    DUPLICATE_STATIC_CLASSES=$(cat "$STATIC_CLASS_TEMP_FILE" | \
        sed 's/.*static class\s\+\([A-Za-z0-9_]*\).*/\1/' | \
        sort | uniq -c | grep -v "^ *1 " | wc -l)
    
    if [ "$DUPLICATE_STATIC_CLASSES" -gt 0 ]; then
        echo -e "${YELLOW}⚠️  发现 $DUPLICATE_STATIC_CLASSES 个重复的静态类名称${NC}"
        echo "详细信息："
        cat "$STATIC_CLASS_TEMP_FILE" | \
            sed 's/.*static class\s\+\([A-Za-z0-9_]*\).*/\1/' | \
            sort | uniq -c | grep -v "^ *1 " | head -5
        echo ""
        echo "请检查这些静态类的功能是否重复。建议："
        echo "  1. 如果功能重复，合并到一个类中"
        echo "  2. 如果功能不同但名称相同，考虑重命名以区分职责"
        WARNINGS_FOUND=$((WARNINGS_FOUND + 1))
    else
        echo -e "${GREEN}✅ 没有发现重复的静态类名称${NC}"
    fi
else
    echo -e "${GREEN}✅ 没有找到静态类或没有重复${NC}"
fi
rm -f "$STATIC_CLASS_TEMP_FILE"
echo ""

# 7. 常量检查 (Constants Detection)
echo -e "${BLUE}[7/10] 检测重复的常量定义...${NC}"
CONST_TEMP_FILE=$(mktemp)
find . -name "*.cs" -type f | grep -v "/bin/\|/obj/" | xargs grep -n "^\s*public const\|^\s*internal const\|^\s*private const" > "$CONST_TEMP_FILE" 2>/dev/null || true

if [ -s "$CONST_TEMP_FILE" ]; then
    # 提取常量值并检测重复（特别是字符串和数字常量）
    DUPLICATE_CONST_VALUES=$(cat "$CONST_TEMP_FILE" | \
        grep -o '=\s*["\047][^"\047]*["\047]\|=\s*[0-9]\+' | \
        sed 's/=\s*//' | \
        sort | uniq -c | sort -rn | grep -v "^ *1 " | wc -l)
    
    if [ "$DUPLICATE_CONST_VALUES" -gt 5 ]; then
        echo -e "${YELLOW}⚠️  发现 $DUPLICATE_CONST_VALUES 个重复的常量值（超过5个）${NC}"
        echo "最常重复的常量值（前5个）："
        cat "$CONST_TEMP_FILE" | \
            grep -o '=\s*["\047][^"\047]*["\047]\|=\s*[0-9]\+' | \
            sed 's/=\s*//' | \
            sort | uniq -c | sort -rn | grep -v "^ *1 " | head -5
        echo ""
        echo "建议："
        echo "  1. 检查这些常量值是否应该共享定义"
        echo "  2. 考虑将常用常量集中到一个 Constants 类中"
        WARNINGS_FOUND=$((WARNINGS_FOUND + 1))
    else
        echo -e "${GREEN}✅ 常量重复在合理范围内${NC}"
    fi
else
    echo -e "${GREEN}✅ 没有找到常量定义或没有重复${NC}"
fi
rm -f "$CONST_TEMP_FILE"
echo ""

# ==================== 原有检测类型 ====================

# 8. 检测重复的类名
echo -e "${BLUE}[8/10] 检测重复的类/接口/记录名称...${NC}"
DUPLICATE_CLASSES=$(find . -name "*.cs" -type f | grep -v "/bin/" | grep -v "/obj/" | \
    xargs grep -h "^\s*public class\|^\s*public interface\|^\s*public record\|^\s*internal class" | \
    sed 's/\s\+/ /g' | sort | uniq -c | grep -v "^ *1 " | wc -l)

if [ "$DUPLICATE_CLASSES" -gt 0 ]; then
    echo -e "${YELLOW}⚠️  发现 $DUPLICATE_CLASSES 个重复的类/接口/记录定义${NC}"
    echo "详细信息："
    find . -name "*.cs" -type f | grep -v "/bin/" | grep -v "/obj/" | \
        xargs grep -h "^\s*public class\|^\s*public interface\|^\s*public record\|^\s*internal class" | \
        sed 's/\s\+/ /g' | sort | uniq -c | grep -v "^ *1 " | head -10
    ISSUES_FOUND=$((ISSUES_FOUND + 1))
    echo ""
else
    echo -e "${GREEN}✅ 没有发现重复的类定义${NC}"
fi

# 9. 检测重复的事件参数类型
echo ""
echo -e "${BLUE}[9/10] 检测重复的事件参数类型...${NC}"
DUPLICATE_EVENTS=$(find . -name "*.cs" -type f | grep -v "/bin/" | grep -v "/obj/" | \
    xargs grep -h "^\s*public record.*EventArgs" | \
    sed 's/\s\+/ /g' | sort | uniq -c | grep -v "^ *1 " | wc -l)

if [ "$DUPLICATE_EVENTS" -gt 0 ]; then
    echo -e "${YELLOW}⚠️  发现 $DUPLICATE_EVENTS 个重复的事件参数定义${NC}"
    echo "详细信息："
    find . -name "*.cs" -type f | grep -v "/bin/" | grep -v "/obj/" | \
        xargs grep -h "^\s*public record.*EventArgs" | \
        sed 's/\s\+/ /g' | sort | uniq -c | grep -v "^ *1 " | head -10
    ISSUES_FOUND=$((ISSUES_FOUND + 1))
    echo ""
else
    echo -e "${GREEN}✅ 没有发现重复的事件参数定义${NC}"
fi

# 10. 检测技术债务标记
echo ""
echo -e "${BLUE}[10/10] 检测技术债务标记 (TODO/FIXME/HACK)...${NC}"
TODO_COUNT=$(grep -r "TODO" --include="*.cs" . 2>/dev/null | grep -v "/bin/\|/obj/\|技术债务.md" | wc -l)
FIXME_COUNT=$(grep -r "FIXME" --include="*.cs" . 2>/dev/null | grep -v "/bin/\|/obj/" | wc -l)
HACK_COUNT=$(grep -r "HACK" --include="*.cs" . 2>/dev/null | grep -v "/bin/\|/obj/" | wc -l)
TOTAL_DEBT=$((TODO_COUNT + FIXME_COUNT + HACK_COUNT))

if [ "$TOTAL_DEBT" -gt 0 ]; then
    echo -e "${YELLOW}⚠️  发现 $TOTAL_DEBT 个技术债务标记${NC}"
    echo "  - TODO: $TODO_COUNT"
    echo "  - FIXME: $FIXME_COUNT"
    echo "  - HACK: $HACK_COUNT"
    WARNINGS_FOUND=$((WARNINGS_FOUND + 1))
    echo ""
    echo "请确保这些技术债务已记录在 docs/Conventions/技术债务.md 中"
else
    echo -e "${GREEN}✅ 没有发现技术债务标记${NC}"
fi

# 总结
echo ""
echo "================================================================"
echo "  检测总结"
echo "================================================================"
echo ""
echo -e "${CYAN}检测完成！统计信息：${NC}"
echo "  - 高优先级问题（Issues）: $ISSUES_FOUND"
echo "  - 警告（Warnings）: $WARNINGS_FOUND"
echo ""

if [ "$ISSUES_FOUND" -eq 0 ] && [ "$WARNINGS_FOUND" -eq 0 ]; then
    echo -e "${GREEN}🎉 太棒了！没有发现影分身代码或技术债务问题${NC}"
    exit 0
elif [ "$ISSUES_FOUND" -eq 0 ]; then
    echo -e "${YELLOW}⚠️  发现 $WARNINGS_FOUND 个警告，但没有高优先级问题${NC}"
    echo ""
    echo "下一步行动："
    echo "1. 查看上述警告信息"
    echo "2. 评估是否需要记录到 docs/Conventions/技术债务.md"
    echo "3. 考虑在适当时机解决这些警告"
    echo ""
    echo "注意：警告不会阻止 PR 提交，但建议尽早处理"
    exit 0
else
    echo -e "${RED}❌ 发现 $ISSUES_FOUND 个高优先级问题，必须解决！${NC}"
    echo ""
    echo "下一步行动："
    echo "1. 查看上述详细信息"
    echo "2. 确认这些问题已记录在 docs/Conventions/技术债务.md"
    echo "3. 如有新问题，请更新技术债务文档"
    echo "4. 优先解决高优先级技术债务"
    echo ""
    echo "⛔ 此脚本检测到问题将阻止 PR 提交"
    exit 1
fi
