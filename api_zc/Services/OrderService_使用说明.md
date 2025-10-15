# OrderService.Create 使用说明

## 📋 API 路径

```
POST /api/Order/Create
```

---

## 📦 请求格式

```json
[
  {
    "shippedDate": "2025-10-14",
    "productName": "产品A",
    "quantity": 100
    // 其他字段...
    // 不需要提供 number，系统会自动生成
  }
]
```

---

## 🔢 自动编号规则

- **字段**: `number`
- **格式**: `01`, `02`, `03`...`99`
- **分组规则**: 根据 `shippedDate` 分组
  - 同一天：编号累加
  - 不同天：从 `01` 重新开始

### 示例

```
2025-10-14:
  - 第1个订单: number = "01"
  - 第2个订单: number = "02"
  - 第3个订单: number = "03"

2025-10-15:
  - 第1个订单: number = "01"  ← 新的一天，重新开始
  - 第2个订单: number = "02"
```

---

## 📝 使用示例

### 示例 1: 创建单个订单

```csharp
using Accura_MES.Services;

// 创建服务
var orderService = OrderService.CreateService(connectionString);

// 准备数据
var orderData = new List<Dictionary<string, object?>>
{
    new Dictionary<string, object?>
    {
        { "shippedDate", DateTime.Parse("2025-10-14") },
        { "productName", "产品A" },
        { "quantity", 100 },
        { "customerId", 123 }
        // 不需要提供 number，系统会自动生成
    }
};

// 执行创建
var result = await orderService.Create(userId: 1, orderData);

if (result.Success)
{
    List<long> insertedIds = result.Data;
    Console.WriteLine($"创建成功！IDs: [{string.Join(", ", insertedIds)}]");
}
```

### 示例 2: 批量创建多个订单（同一天）

```csharp
var orderData = new List<Dictionary<string, object?>>
{
    // 2025-10-14 的第一个订单 -> number 自动生成 "01"
    new()
    {
        { "shippedDate", "2025-10-14" },
        { "productName", "产品A" }
    },
    
    // 2025-10-14 的第二个订单 -> number 自动生成 "02"
    new()
    {
        { "shippedDate", "2025-10-14" },
        { "productName", "产品B" }
    }
};

var result = await orderService.Create(1, orderData);
// 两个订单的 number 会是 "01" 和 "02"
```

### 示例 3: 批量创建多个订单（不同天）

```csharp
var orderData = new List<Dictionary<string, object?>>
{
    // 2025-10-14 -> number = "01"
    new()
    {
        { "shippedDate", "2025-10-14" },
        { "productName", "订单1" }
    },
    
    // 2025-10-15 -> number = "01" (新的一天)
    new()
    {
        { "shippedDate", "2025-10-15" },
        { "productName", "订单2" }
    },
    
    // 2025-10-14 -> number = "02" (同一天累加)
    new()
    {
        { "shippedDate", "2025-10-14" },
        { "productName", "订单3" }
    }
};

var result = await orderService.Create(1, orderData);
```

---

## 🔄 完整流程

```
1. 接收订单数据
    ↓
2. 为每个订单生成编号
   └─ 获取 shippedDate
   └─ 格式化为 yyyy-MM-dd
   └─ 调用 SequenceService 获取编号
   └─ 自动填充 number 字段（格式：01, 02...）
    ↓
3. 插入数据库（使用事务）
    ↓
4. 返回成功结果（ID列表）
```

---

## 📊 返回数据结构

```json
{
  "success": true,
  "code": "200-0-0",
  "message": "操作成功",
  "data": [101, 102, 103]
}
```

- `data`: 新创建的订单ID数组

---

## ⚠️ 错误处理

### 错误 1: 缺少 shippedDate

```json
{
  "success": false,
  "code": "400-xxx",
  "message": "shippedDate 不能为空"
}
```

**解决方案**: 确保每笔数据都包含 `shippedDate`

### 错误 2: shippedDate 格式不正确

```json
{
  "success": false,
  "code": "400-xxx",
  "message": "shippedDate 格式不正确: xxx"
}
```

**解决方案**: 使用正确的日期格式
- ✅ `"2025-10-14"`
- ✅ `"2025/10/14"`
- ✅ `DateTime.Parse("2025-10-14")`

---

## 🎯 最佳实践

### ✅ 推荐做法

1. **批量创建时，同一天的订单编号会是连续的**
```csharp
// 好：同一天的订单放在一起
var data = new List<Dictionary<string, object?>>
{
    new() { { "shippedDate", "2025-10-14" }, { "name", "订单1" } },  // 01
    new() { { "shippedDate", "2025-10-14" }, { "name", "订单2" } },  // 02
    new() { { "shippedDate", "2025-10-15" }, { "name", "订单A" } }   // 01 (新的一天)
};
```

2. **使用事务确保数据一致性**
   - 所有订单要么全部创建成功，要么全部失败
   - 编号生成和数据插入在同一事务中

3. **不要手动提供 number**
   - 系统会自动生成并覆盖

---

## 🔒 并发安全

- ✅ 支持多用户同时创建订单
- ✅ 使用数据库锁机制确保编号不重复
- ✅ 同一天的订单编号会排队生成，确保顺序

### 并发场景示例

```
用户 A: shippedDate=2025-10-14 → 等待获取锁 → 获得 number="01" → 释放锁
用户 B: shippedDate=2025-10-14 → 等待用户A完成 → 获得 number="02"
用户 C: shippedDate=2025-10-15 → 不冲突（不同日期） → 获得 number="01"
```

---

## 🆚 与 ProjectService 的区别

| 特性 | ProjectService | OrderService |
|------|---------------|--------------|
| 分组依据 | `customerId` | `shippedDate` |
| 编号格式 | `0001` (4位) | `01` (2位) |
| 分组说明 | 每个客户独立编号 | 每天独立编号 |
| 必填字段 | `name`, `customerId` | `shippedDate` |

---

## 🔗 API 调用示例

### 使用 Postman 或前端

```http
POST /api/Order/Create
Headers: 
  - Database: your-database-name
  - Authorization: Bearer your-token

Body (JSON):
[
  {
    "shippedDate": "2025-10-14",
    "productName": "产品A",
    "quantity": 100,
    "customerId": 123
  },
  {
    "shippedDate": "2025-10-14",
    "productName": "产品B",
    "quantity": 200,
    "customerId": 456
  }
]
```

**返回**：
```json
{
  "success": true,
  "code": "200-0-0",
  "message": "操作成功",
  "data": [101, 102]
}
```

第一个订单的 `number` = `"01"`  
第二个订单的 `number` = `"02"`

---

完成！ 🎉

