# 窄带分拣机实时监控 SignalR 集成文档

## 概述

本系统提供了基于 SignalR 的实时监控接口，允许前端订阅并接收以下系统状态的实时更新：

- 主线速度状态
- 格口下方的小车
- 原点传感器处的小车
- 包裹创建和落格事件
- 在线包裹列表
- 设备状态
- 小车布局

## SignalR Hub 端点

```
ws://<host>:<port>/hubs/narrowbelt-live
```

例如本地开发环境：
```
ws://localhost:5000/hubs/narrowbelt-live
```

## 前端集成示例

### 1. 安装 SignalR 客户端库

```bash
npm install @microsoft/signalr
```

### 2. 建立连接

```typescript
import * as signalR from "@microsoft/signalr";

const connection = new signalR.HubConnectionBuilder()
    .withUrl("http://localhost:5000/hubs/narrowbelt-live")
    .withAutomaticReconnect()
    .configureLogging(signalR.LogLevel.Information)
    .build();

// 启动连接
async function start() {
    try {
        await connection.start();
        console.log("SignalR 已连接");
    } catch (err) {
        console.error("SignalR 连接失败:", err);
        setTimeout(start, 5000); // 5秒后重试
    }
}

// 断线重连
connection.onclose(async () => {
    await start();
});

start();
```

### 3. 订阅事件

#### 主线速度更新

```typescript
connection.on("LineSpeedUpdated", (data: LineSpeedDto) => {
    console.log("速度更新:", data);
    // data.ActualMmps: 实际速度 (mm/s)
    // data.TargetMmps: 目标速度 (mm/s)
    // data.Status: 速度状态 ("Unknown" | "Starting" | "Stable" | "Unstable")
    // data.LastUpdatedAt: 更新时间
});
```

#### 原点小车变更

```typescript
connection.on("OriginCartChanged", (data: OriginCartDto) => {
    console.log("原点小车:", data.CartId);
});
```

#### 格口小车变更

```typescript
connection.on("ChuteCartChanged", (data: ChuteCartDto) => {
    console.log(`格口 ${data.ChuteId} 的小车: ${data.CartId}`);
});

// 或订阅所有格口的更新
connection.on("ChuteCartsUpdated", (data: ChuteCartDto[]) => {
    console.log("所有格口小车映射:", data);
});
```

#### 包裹创建

```typescript
connection.on("LastCreatedParcelUpdated", (data: ParcelDto) => {
    console.log("新建包裹:", data);
    // data.ParcelId: 包裹ID
    // data.Barcode: 条码
    // data.WeightKg: 重量 (kg)
    // data.VolumeCubicMm: 体积 (立方毫米)
    // data.TargetChuteId: 目标格口
    // data.CreatedAt: 创建时间
});
```

#### 包裹落格

```typescript
connection.on("LastDivertedParcelUpdated", (data: ParcelDto) => {
    console.log("包裹落格:", data);
    // data.ActualChuteId: 实际落格格口
    // data.DivertedAt: 落格时间
});
```

#### 在线包裹列表

```typescript
connection.on("OnlineParcelsUpdated", (data: ParcelDto[]) => {
    console.log("当前在线包裹:", data.length);
    data.forEach(parcel => {
        console.log(`- ${parcel.Barcode} -> 格口 ${parcel.TargetChuteId}`);
    });
});
```

#### 设备状态

```typescript
connection.on("DeviceStatusUpdated", (data: DeviceStatusDto) => {
    console.log("设备状态:", data.Status);
    // data.Status: "Idle" | "Starting" | "Running" | "Stopped" | "Faulted" | "EmergencyStopped"
    // data.Message: 状态消息
});
```

#### 小车布局

```typescript
connection.on("CartLayoutUpdated", (data: CartLayoutDto) => {
    console.log("小车布局:", data.CartPositions.length);
    data.CartPositions.forEach(cart => {
        console.log(`小车 ${cart.CartId} (索引 ${cart.CartIndex}): 位置 ${cart.LinearPositionMm}mm, 格口 ${cart.CurrentChuteId}`);
    });
});
```

### 4. 高级功能：订阅特定格口

如果只想监控特定格口的小车变化，可以加入格口分组：

```typescript
// 加入格口 10 的分组
await connection.invoke("JoinChuteGroup", 10);

// 离开格口 10 的分组
await connection.invoke("LeaveChuteGroup", 10);
```

加入分组后，该格口的小车变更会单独推送给该客户端。

### 5. 获取当前状态快照

连接成功后，会自动收到所有状态的初始值。也可以手动请求：

```typescript
await connection.invoke("GetCurrentSnapshot");
```

## 数据结构定义

### LineSpeedDto
```typescript
interface LineSpeedDto {
    ActualMmps: number;        // 实际速度 (mm/s)
    TargetMmps: number;        // 目标速度 (mm/s)
    Status: string;            // 速度状态
    LastUpdatedAt: string;     // ISO 8601 时间戳
}
```

### ParcelDto
```typescript
interface ParcelDto {
    ParcelId: number;
    Barcode: string;
    WeightKg?: number;
    VolumeCubicMm?: number;
    TargetChuteId?: number;
    ActualChuteId?: number;
    CreatedAt: string;         // ISO 8601 时间戳
    DivertedAt?: string;       // ISO 8601 时间戳
}
```

### ChuteCartDto
```typescript
interface ChuteCartDto {
    ChuteId: number;
    CartId?: number;           // null 表示该格口下无小车
}
```

### OriginCartDto
```typescript
interface OriginCartDto {
    CartId?: number;           // null 表示原点处无小车
    LastUpdatedAt: string;
}
```

### DeviceStatusDto
```typescript
interface DeviceStatusDto {
    Status: string;            // 设备状态
    Message?: string;          // 状态消息
    LastUpdatedAt: string;
}
```

### CartPositionDto
```typescript
interface CartPositionDto {
    CartId: number;
    CartIndex: number;
    LinearPositionMm?: number;
    CurrentChuteId?: number;
}
```

### CartLayoutDto
```typescript
interface CartLayoutDto {
    CartPositions: CartPositionDto[];
    LastUpdatedAt: string;
}
```

## 推送频率配置

系统支持配置不同类型事件的推送频率，避免推送过于频繁导致前端无法处理。

在 `appsettings.json` 中配置：

```json
{
  "LiveViewPush": {
    "LineSpeedPushIntervalMs": 200,        // 主线速度推送间隔
    "ChuteCartPushIntervalMs": 100,        // 格口小车推送间隔
    "OriginCartPushIntervalMs": 100,       // 原点小车推送间隔
    "ParcelCreatedPushIntervalMs": 50,     // 包裹创建推送间隔
    "ParcelDivertedPushIntervalMs": 50,    // 包裹落格推送间隔
    "DeviceStatusPushIntervalMs": 500,     // 设备状态推送间隔
    "CartLayoutPushIntervalMs": 500,       // 小车布局推送间隔
    "OnlineParcelsPushPeriodMs": 1000,     // 在线包裹列表推送周期
    "EnableOnlineParcelsPush": true        // 是否启用在线包裹列表推送
  }
}
```

**说明：**
- 所有 `*IntervalMs` 配置表示该类型事件的最小推送间隔（毫秒）
- 当事件触发频率高于配置的间隔时，部分事件会被节流（throttle）
- `OnlineParcelsPushPeriodMs` 是定时推送的周期，而非事件驱动

## 完整示例：Vue 3 组件

```vue
<template>
  <div class="realtime-monitor">
    <div class="speed-panel">
      <h3>主线速度</h3>
      <p>实际: {{ lineSpeed.ActualMmps }} mm/s</p>
      <p>目标: {{ lineSpeed.TargetMmps }} mm/s</p>
      <p>状态: {{ lineSpeed.Status }}</p>
    </div>

    <div class="parcels-panel">
      <h3>在线包裹 ({{ onlineParcels.length }})</h3>
      <ul>
        <li v-for="parcel in onlineParcels" :key="parcel.ParcelId">
          {{ parcel.Barcode }} → 格口 {{ parcel.TargetChuteId }}
        </li>
      </ul>
    </div>

    <div class="device-panel">
      <h3>设备状态</h3>
      <p :class="deviceStatus.Status">{{ deviceStatus.Status }}</p>
      <p>{{ deviceStatus.Message }}</p>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, onUnmounted } from 'vue';
import * as signalR from '@microsoft/signalr';

const lineSpeed = ref({
  ActualMmps: 0,
  TargetMmps: 0,
  Status: 'Unknown',
  LastUpdatedAt: ''
});

const onlineParcels = ref<any[]>([]);

const deviceStatus = ref({
  Status: 'Idle',
  Message: '',
  LastUpdatedAt: ''
});

let connection: signalR.HubConnection;

onMounted(async () => {
  connection = new signalR.HubConnectionBuilder()
    .withUrl('http://localhost:5000/hubs/narrowbelt-live')
    .withAutomaticReconnect()
    .build();

  // 订阅事件
  connection.on('LineSpeedUpdated', (data) => {
    lineSpeed.value = data;
  });

  connection.on('OnlineParcelsUpdated', (data) => {
    onlineParcels.value = data;
  });

  connection.on('DeviceStatusUpdated', (data) => {
    deviceStatus.value = data;
  });

  // 启动连接
  await connection.start();
  console.log('SignalR connected');
});

onUnmounted(() => {
  if (connection) {
    connection.stop();
  }
});
</script>

<style scoped>
.realtime-monitor {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: 1rem;
  padding: 1rem;
}

.Running { color: green; }
.Faulted { color: red; }
.Starting { color: orange; }
</style>
```

## 性能建议

1. **使用自动重连**：建议使用 `.withAutomaticReconnect()` 以在连接断开时自动重连
2. **批量处理**：对于高频事件（如速度更新），考虑在前端进行防抖或节流处理
3. **按需订阅**：如果只关心特定格口，使用 `JoinChuteGroup` 加入格口分组
4. **分页显示**：在线包裹列表可能很长，建议前端进行分页或虚拟滚动
5. **合理配置推送间隔**：根据实际需求调整 `appsettings.json` 中的推送间隔

## 故障排查

### 连接失败

1. 检查后端服务是否已启动
2. 检查端口是否正确
3. 检查防火墙设置
4. 查看浏览器控制台错误信息

### 收不到更新

1. 确认事件名称拼写正确（区分大小写）
2. 检查后端是否发布了相应的事件
3. 查看后端日志，确认推送是否被节流
4. 使用浏览器开发工具的网络面板查看 WebSocket 流量

### 性能问题

1. 增加推送间隔配置
2. 禁用不需要的推送（如 `EnableOnlineParcelsPush`）
3. 使用格口分组减少不必要的消息
4. 在前端实现额外的节流逻辑

## 相关资源

- [SignalR 官方文档](https://learn.microsoft.com/en-us/aspnet/core/signalr/)
- [@microsoft/signalr NPM 包](https://www.npmjs.com/package/@microsoft/signalr)
- 系统架构文档：`SORTING_SYSTEM.md`
