#!/usr/bin/env pwsh
<#
.SYNOPSIS
    一键构建和测试脚本 - ZakYip.NarrowBeltDiverterSorter
.DESCRIPTION
    执行完整的构建流程：
    1. 还原 NuGet 包
    2. 编译解决方案（Release 模式，警告视为错误）
    3. 运行所有测试（包括单元测试和 E2E 测试）
#>

$ErrorActionPreference = "Stop"
$SolutionFile = "ZakYip.NarrowBeltDiverterSorter.sln"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "ZakYip.NarrowBeltDiverterSorter 一键构建" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# 1. 还原 NuGet 包
Write-Host "[1/3] 开始还原 NuGet 包..." -ForegroundColor Yellow
dotnet restore $SolutionFile
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ NuGet 包还原失败！" -ForegroundColor Red
    exit 1
}
Write-Host "✅ NuGet 包还原成功" -ForegroundColor Green
Write-Host ""

# 2. 编译解决方案（Release 模式，警告视为错误）
Write-Host "[2/3] 开始编译解决方案（Release 模式，启用警告视为错误）..." -ForegroundColor Yellow
dotnet build $SolutionFile -c Release --no-restore
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ 编译失败！" -ForegroundColor Red
    exit 1
}
Write-Host "✅ 编译成功" -ForegroundColor Green
Write-Host ""

# 3. 运行所有测试
Write-Host "[3/3] 开始执行全部测试（含单元测试和 E2E 测试）..." -ForegroundColor Yellow
dotnet test $SolutionFile -c Release --no-build --verbosity normal
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ 测试失败！" -ForegroundColor Red
    exit 1
}
Write-Host "✅ 所有测试通过" -ForegroundColor Green
Write-Host ""

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "✅ 构建和测试全部完成！" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
