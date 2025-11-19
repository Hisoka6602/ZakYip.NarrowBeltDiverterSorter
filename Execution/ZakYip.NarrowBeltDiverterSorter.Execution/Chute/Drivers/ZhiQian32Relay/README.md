# 智嵌32路网络继电器驱动

## 概述

本驱动实现了智嵌（ZhiQian）32路网络继电器的TCP控制协议，用于格口IO控制。

## 文件结构

```
ZhiQian32Relay/
├── README.md                        # 本文档
├── ZhiQian32RelayClient.cs         # TCP协议封装层
├── ZhiQian32RelayEndpoint.cs       # IChuteIoEndpoint实现
└── ZhiQian32RelayChuteIoService.cs # IChuteIoService实现
```

## 协议说明

### 连接参数

- **协议**: TCP Socket
- **模式**: ASCII命令
- **端口**: 可配置（通常为8080）

### 命令格式

#### 单个继电器控制

打开继电器：
```
OPEN CH:xx\r\n
```

关闭继电器：
```
CLOSE CH:xx\r\n
```

其中 `xx` 为通道号的两位数字表示（01-32）。

#### 批量控制

打开所有继电器：
```
OPEN ALL\r\n
```

关闭所有继电器：
```
CLOSE ALL\r\n
```

## 配置示例

在 `appsettings.json` 或 `appsettings.ZhiQian32Relay.json` 中配置：

```json
{
  "ChuteIo": {
    "IsHardwareEnabled": true,
    "Mode": "ZhiQian32Relay",
    "Nodes": [
      {
        "NodeKey": "zhiqian-node-1",
        "Brand": "ZhiQian32Relay",
        "IpAddress": "192.168.1.100",
        "Port": 8080,
        "MaxChannelCount": 32,
        "Channels": [
          {
            "ChuteId": 1,
            "ChannelIndex": 1
          },
          {
            "ChuteId": 2,
            "ChannelIndex": 2
          }
        ]
      }
    ]
  }
}
```

## 使用方法

### 启动应用

使用ZhiQian32Relay配置启动：

```bash
dotnet run --configuration ZhiQian32Relay
```

或在命令行中指定配置文件：

```bash
dotnet run -- --environment ZhiQian32Relay
```

### 查看日志

启动时会显示：

```
格口 IO 实现: 智嵌32路网络继电器
  节点数量: 2
  - zhiqian-node-1: 192.168.1.100:8080, 8 个通道绑定
  - zhiqian-node-2: 192.168.1.101:8080, 4 个通道绑定
```

运行时日志示例：

```
[智嵌继电器端点] 端点 zhiqian-node-1 设置通道 1 状态为 开
[智嵌继电器客户端] 正在连接到 192.168.1.100:8080
[智嵌继电器客户端] 成功连接到 192.168.1.100:8080
[智嵌继电器客户端] 发送命令: OPEN CH:01
```

## 特性

### 自动重连

客户端在连接失败或断开后会在下次操作时自动尝试重新连接。

### 异常处理

所有网络异常都会被捕获并记录到日志，不会导致Host进程崩溃。

### 通道范围检查

驱动会验证通道索引范围（1-32），超出范围的请求会被拒绝并记录错误日志。

### 多端点支持

支持同时管理多个智嵌继电器设备，每个设备独立配置IP和端口。

## 扩展其他品牌

要添加其他品牌的继电器驱动（如新捷、科维等），请按照以下步骤：

1. 在 `Chutes/Drivers/` 下创建新的品牌文件夹，如 `Xinjie/` 或 `Kovi/`
2. 实现 `IChuteIoEndpoint` 接口
3. 创建对应的 TCP/UDP 客户端封装协议
4. 在 `Program.cs` 的 Host 注册处添加品牌路由逻辑
5. 创建对应的配置文件和单元测试

## 故障排查

### 连接失败

检查：
- IP地址和端口号是否正确
- 网络连接是否正常
- 继电器设备是否开机
- 防火墙设置是否阻止了连接

查看日志中的错误信息：
```
[智嵌继电器客户端] 连接失败 192.168.1.100:8080
```

### 通道索引超出范围

检查配置文件中的 `ChannelIndex` 是否在 1-32 范围内：

```
[智嵌继电器端点] 端点 zhiqian-node-1 通道索引 33 超出范围 (1..32)，拒绝发送
```

### 格口未配置映射

检查配置文件中是否为该格口配置了通道绑定：

```
[智嵌继电器格口IO服务] 格口 999 未配置映射关系
```

## 技术参考

- [格口IO架构文档](../../../../docs/ChuteIoArchitecture.md)
- [IChuteIoEndpoint接口定义](../../IChuteIoEndpoint.cs)
- [IChuteIoService接口定义](../../../../Core/Abstractions/IChuteIoService.cs)
