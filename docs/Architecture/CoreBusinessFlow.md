# 核心业务流程 (Core Business Flow)

## 概述

本文档详细描述窄带分拣系统从启动到完成分拣的完整业务流程，包括状态转换、关键节点和时序逻辑。

---

## 完整分拣流程概览

```mermaid
stateDiagram-v2
    [*] --> Idle: 系统启动
    
    Idle --> Running: 面板启动按钮触发
    
    Running --> CountingCarts: 原点IO监听
    CountingCarts --> CartIdentified: 双IO识别首车
    CartIdentified --> CountingCarts: 单IO计数普通车
    
    CountingCarts --> ParcelArrival: 入口传感器触发
    ParcelArrival --> ParcelBinding: 查询路由规则
    ParcelBinding --> CartTracking: 绑定目标车号
    
    CartTracking --> WindowCalculation: 小车持续运行
    WindowCalculation --> ChuteTriggering: 车号匹配窗口
    ChuteTriggering --> ParcelDropped: DO输出
    
    ParcelDropped --> ResultReporting: 格口传感器确认
    ResultReporting --> CountingCarts: 继续处理下一包裹
    
    Running --> Idle: 面板停止按钮
    Running --> Emergency: 急停触发
    Emergency --> Idle: 恢复
    
    note right of CartIdentified
        首车识别后
        环形数组基准确立
    end note
    
    note right of ParcelBinding
        包裹ID + 车号
        建立绑定关系
    end note
    
    note right of ChuteTriggering
        精确窗口控制
        通常100ms
    end note
```

---

## 阶段一：系统启动与初始化

### 启动流程

```mermaid
sequenceDiagram
    participant Admin as 管理员
    participant API as Host API
    participant Config as 配置存储
    participant Exec as Execution层
    participant IO as IO监听器
    participant Drive as 主线驱动
    
    Admin->>API: 1. 配置电柜面板IO映射
    API->>Config: 2. 持久化配置
    Config-->>Exec: 3. 配置变更通知
    
    Admin->>API: 4. 触发启动按钮IO
    API->>Exec: 5. 启动指令
    
    Exec->>Drive: 6. 启动主线驱动
    Drive-->>Exec: 7. 主线就绪
    
    Exec->>IO: 8. 启用IO监听
    IO-->>Exec: 9. IO监听就绪
    
    Exec->>Exec: 10. 进入Running状态
    
    Note over Exec: 系统进入运行模式<br/>等待包裹和小车事件
```

### 关键配置项
- **面板启动按钮**: DI通道号
- **面板停止按钮**: DI通道号
- **急停按钮**: DI通道号
- **运行指示灯**: DO通道号

---

## 阶段二：小车识别与位置追踪

### 双IO识别算法

```mermaid
flowchart TD
    Start[原点IO触发] --> CheckBoth{双IO同时触发?}
    
    CheckBoth -->|是| ZeroCar[识别为0号车]
    CheckBoth -->|否| RegularCar[识别为普通车]
    
    ZeroCar --> ResetCounter[计数器归零]
    RegularCar --> IncrementCounter[计数器+1]
    
    ResetCounter --> UpdateHead[更新首车位置 = 1]
    IncrementCounter --> CalcHead[计算首车位置<br/>= (当前位置 + 1) mod 总车数]
    
    UpdateHead --> NotifyExec[通知Execution层]
    CalcHead --> NotifyExec
    
    NotifyExec --> UpdateBindings[更新所有格口当前车号]
    UpdateBindings --> End[继续监听]
    
    style ZeroCar fill:#90EE90
    style RegularCar fill:#87CEEB
```

### 环形数组维护

**数据结构**:
```
环形数组: [1, 2, 3, ..., 100, 1, 2, ...] (循环)
首车索引: CurrentOriginCartIndex (0-based)
首车编号: HeadCartNumber = CurrentOriginCartIndex + 1
```

**更新公式**:
```
当检测到普通车:
  CurrentOriginCartIndex = (CurrentOriginCartIndex + 1) mod TotalCartCount

当检测到0号车:
  CurrentOriginCartIndex = 0
```

---

## 阶段三：包裹上料与绑定

### 包裹创建流程

```mermaid
sequenceDiagram
    participant Sensor as 入口传感器
    participant Ingress as Ingress层
    participant Config as 配置存储
    participant Exec as Execution层
    participant Tracker as 小车追踪器
    
    Sensor->>Ingress: 1. DI触发（上升沿）
    Ingress->>Ingress: 2. 创建包裹实体
    
    Note over Ingress: PackageId = 唯一ID<br/>Timestamp = 当前时间
    
    Ingress->>Config: 3. 查询路由规则
    Config-->>Ingress: 4. 返回目标格口ID
    
    Ingress->>Exec: 5. 包裹到达事件<br/>(PackageId, ChuteId)
    
    Exec->>Tracker: 6. 查询当前首车位置
    Tracker-->>Exec: 7. HeadCartNumber
    
    Exec->>Exec: 8. 计算格口当前车号
    
    Note over Exec: CartNumber = f(HeadCartNumber,<br/>ChuteBaseCart, TotalCarts)
    
    Exec->>Exec: 9. 建立绑定关系<br/>(PackageId → CartNumber)
    
    Note over Exec: 绑定记录持久化<br/>用于后续窗口匹配
```

### 格口车号计算公式

给定：
- `TotalCartCount`: 总小车数量（如100）
- `HeadCartNumber`: 当前首车编号（1-based）
- `CartNumberWhenHeadAtOrigin`: 格口基准车号（1-based）

计算格口当前车号：
```
zeroBasedHead = HeadCartNumber - 1
zeroBasedChuteBase = CartNumberWhenHeadAtOrigin - 1
zeroBasedResult = (zeroBasedChuteBase + zeroBasedHead) mod TotalCartCount
CartNumber = zeroBasedResult + 1
```

**示例**:
- TotalCartCount = 100
- HeadCartNumber = 5（当前5号车在原点）
- 格口1的 CartNumberWhenHeadAtOrigin = 90

则格口1当前车号 = ((90-1) + (5-1)) mod 100 + 1 = 94

---

## 阶段四：小车运行与窗口计算

### 实时位置更新

```mermaid
sequenceDiagram
    participant Cart as 小车环
    participant Sensor as 原点传感器
    participant Tracker as 位置追踪器
    participant Exec as 执行引擎
    participant Resolver as 格口解析器
    
    loop 小车持续运行
        Cart->>Sensor: 小车经过原点
        Sensor->>Tracker: IO触发
        Tracker->>Tracker: 更新首车位置
        
        Tracker->>Resolver: 位置变更通知
        Resolver->>Resolver: 重新计算所有格口车号
        
        Resolver->>Exec: 格口车号更新
        
        Exec->>Exec: 检查待分拣包裹列表
        
        alt 车号匹配包裹绑定
            Exec->>Exec: 进入发信窗口准备
            Note over Exec: 窗口倒计时开始
        else 无匹配
            Exec->>Exec: 继续监听
        end
    end
```

### 窗口精确控制

**窗口计算**:
1. 包裹绑定车号 = N
2. 格口当前车号 = M
3. 当 M == N 时，进入发信窗口
4. 窗口持续时间：通常100ms

**时间容错**:
- 允许±10ms的时间误差
- 通过PID控制主线速度，减小速度波动
- 窗口时间可通过API配置

---

## 阶段五：格口触发与落格确认

### DO触发时序

```mermaid
sequenceDiagram
    participant Exec as 执行引擎
    participant Chute as 格口发信器
    participant PLC as IO板卡
    participant Hardware as 格口硬件
    participant Sensor as 格口传感器
    
    Exec->>Exec: 1. 检测到车号匹配
    
    Note over Exec: PackageId: 12345<br/>BoundCart: 90<br/>CurrentCart: 90<br/>TargetChute: 1
    
    Exec->>Chute: 2. OpenWindowAsync(ChuteId=1, 100ms)
    
    Chute->>PLC: 3. 设置DO=High
    
    Note over PLC: DO通道立即输出高电平
    
    PLC->>Hardware: 4. 触发信号
    
    Note over Hardware: 小车旋转<br/>包裹滑落
    
    par 异步并发
        Chute->>Chute: 5a. 等待100ms
        Hardware->>Sensor: 5b. 包裹落入格口
    end
    
    Chute->>PLC: 6. 设置DO=Low
    
    Note over PLC: DO通道恢复低电平
    
    Sensor->>Exec: 7. 格口传感器确认
    
    Exec->>Exec: 8. 标记包裹状态=已分拣
    
    Exec->>Exec: 9. 从待分拣列表移除
```

### 落格确认逻辑

**正常流程**:
1. DO触发后，等待格口传感器确认
2. 超时时间：500ms（可配置）
3. 确认后，标记包裹为"已完成"

**异常情况**:
- **超时无确认**: 记录告警，但不阻塞后续包裹
- **DO失败**: 使用SafetyIsolator捕获，记录日志
- **传感器故障**: 降级为"基于DO时间推断"模式

---

## 阶段六：结果上报

### 上报时序

```mermaid
sequenceDiagram
    participant Exec as 执行引擎
    participant Event as 事件总线
    participant Comm as 通信层
    participant WCS as 上游WCS
    
    Exec->>Event: 1. 发布分拣完成事件
    
    Note over Event: SortingCompletedEventArgs<br/>PackageId, ChuteId,<br/>Timestamp, Status
    
    Event->>Comm: 2. 事件订阅触发
    
    Comm->>Comm: 3. 构建上报消息
    
    Note over Comm: SortingResultReportDto<br/>包含包裹ID、格口、时间
    
    Comm->>WCS: 4. TCP发送结果
    
    alt 发送成功
        WCS-->>Comm: 5a. ACK确认
        Comm->>Comm: 6a. 记录Info日志
    else 发送失败
        Comm->>Comm: 5b. 记录Error日志
        Note over Comm: 不重试，等待WCS查询
    end
```

### 上报数据结构

```json
{
  "packageId": "PKG-12345",
  "targetChuteId": 1,
  "actualChuteId": 1,
  "sortedAt": "2025-11-21T08:30:15.123Z",
  "status": "Success",
  "cartNumber": 90,
  "duration": 125
}
```

---

## 关键性能指标

### 时间节点
1. **包裹上料 → 绑定**: &lt; 10ms
2. **绑定 → 窗口匹配**: 取决于小车环运行时间
3. **窗口匹配 → DO触发**: &lt; 5ms
4. **DO触发 → 落格确认**: 100ms（窗口时间） + 50ms（传感器延迟）
5. **落格确认 → 上报WCS**: &lt; 100ms

### 吞吐量
- **理论上限**: 受小车间距和速度限制
- **实测**: 100个小车环，速度3000mm/s，约60包裹/分钟
- **瓶颈**: 上料间隔和格口发信器恢复时间

---

## 异常流程处理

### 关键异常点

```mermaid
flowchart TD
    Start[包裹到达] --> Bind{绑定成功?}
    
    Bind -->|失败| LogBindError[记录绑定错误]
    LogBindError --> SkipPackage[跳过该包裹]
    
    Bind -->|成功| Track[追踪小车]
    Track --> Match{车号匹配?}
    
    Match -->|超时| LogTimeout[记录超时告警]
    LogTimeout --> SkipPackage
    
    Match -->|匹配| Trigger{DO触发成功?}
    
    Trigger -->|失败| LogTriggerError[记录DO错误]
    LogTriggerError --> SkipPackage
    
    Trigger -->|成功| Wait{传感器确认?}
    
    Wait -->|超时| LogNoConfirm[记录无确认]
    LogNoConfirm --> AssumeSuccess[推断为成功]
    
    Wait -->|确认| Success[分拣成功]
    AssumeSuccess --> Report[上报WCS]
    Success --> Report
    
    SkipPackage --> NextPackage[处理下一包裹]
    Report --> NextPackage
    
    style LogBindError fill:#FF6B6B
    style LogTimeout fill:#FF6B6B
    style LogTriggerError fill:#FF6B6B
    style LogNoConfirm fill:#FFD93D
    style Success fill:#90EE90
```

详细异常处理策略请参考 [异常处理流程文档](./ExceptionHandlingFlow.md)。

---

## 仿真验证

### 1000包裹全链路测试

测试覆盖完整流程：
1. ✅ 面板启动按钮配置（通过API）
2. ✅ 小车IO识别（双IO算法）
3. ✅ 包裹创建与绑定（1000个包裹）
4. ✅ 小车环运行（环形数组维护）
5. ✅ 窗口匹配与DO触发（精确时序）
6. ✅ 落格确认（传感器验证）

测试断言：
- 每个包裹绑定车号正确
- 落格格口与目标格口一致
- 无漏落格、无误触发
- 车号与格口对应关系正确

详细测试说明请参考 [仿真测试文档](../Simulation/SimulationTesting.md)。

---

## 参考文档

- [系统拓扑图](./SystemTopology.md)
- [异常处理流程](./ExceptionHandlingFlow.md)
- [分层架构说明](./LayeredArchitecture.md)
- [小车编号与格口绑定](../NarrowBelt/CartNumberingAndChutes.md)
- [窄带分拣机设计](./NarrowBeltDesign.md)

---

**版本**: v1.0  
**最后更新**: 2025-11-21  
**维护者**: ZakYip Team
