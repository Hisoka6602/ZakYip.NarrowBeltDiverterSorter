#!/bin/bash
#
# 一键构建和测试脚本 - ZakYip.NarrowBeltDiverterSorter
#
# 执行完整的构建流程：
# 1. 还原 NuGet 包
# 2. 编译解决方案（Release 模式，警告视为错误）
# 3. 运行所有测试（包括单元测试和 E2E 测试）
#

set -e

SOLUTION_FILE="ZakYip.NarrowBeltDiverterSorter.sln"

# 颜色定义
CYAN='\033[0;36m'
YELLOW='\033[1;33m'
GREEN='\033[0;32m'
RED='\033[0;31m'
NC='\033[0m' # No Color

echo -e "${CYAN}========================================"
echo -e "ZakYip.NarrowBeltDiverterSorter 一键构建"
echo -e "========================================${NC}"
echo ""

# 1. 还原 NuGet 包
echo -e "${YELLOW}[1/3] 开始还原 NuGet 包...${NC}"
if ! dotnet restore "$SOLUTION_FILE"; then
    echo -e "${RED}❌ NuGet 包还原失败！${NC}"
    exit 1
fi
echo -e "${GREEN}✅ NuGet 包还原成功${NC}"
echo ""

# 2. 编译解决方案（Release 模式，警告视为错误）
echo -e "${YELLOW}[2/3] 开始编译解决方案（Release 模式，启用警告视为错误）...${NC}"
if ! dotnet build "$SOLUTION_FILE" -c Release --no-restore; then
    echo -e "${RED}❌ 编译失败！${NC}"
    exit 1
fi
echo -e "${GREEN}✅ 编译成功${NC}"
echo ""

# 3. 运行所有测试
echo -e "${YELLOW}[3/3] 开始执行全部测试（含单元测试和 E2E 测试）...${NC}"
if ! dotnet test "$SOLUTION_FILE" -c Release --no-build --verbosity normal; then
    echo -e "${RED}❌ 测试失败！${NC}"
    exit 1
fi
echo -e "${GREEN}✅ 所有测试通过${NC}"
echo ""

echo -e "${CYAN}========================================"
echo -e "${GREEN}✅ 构建和测试全部完成！${NC}"
echo -e "${CYAN}========================================${NC}"
