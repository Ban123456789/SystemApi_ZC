# OrderController.GetList API 使用说明

## 📋 API 信息

```
POST /api/Order/GetList
```

---

## 📦 请求参数

### Headers
- `Database`: 数据库名称
- `Authorization`: Bearer token

### Body (JSON)
```json
{
  "ids": [1, 2, 3],           // 可选：订单ID数组
  "shippedDate": "2025-10-15" // 可选：出货日期
}
```

---

## 🎯 参数说明

| 参数 | 类型 | 必填 | 说明 |
|------|------|------|------|
| `ids` | array | ❌ | 订单ID数组，会用 `IN` 查询过滤 |
| `shippedDate` | string/date | ❌ | 出货日期，会用 `=` 查询过滤 |

**注意**：
- 两个参数都是可选的
- 如果都提供，则使用 `AND` 连接
- 如果都不提供，返回所有订单

---

## 📝 使用示例

### 示例 1: 根据 IDs 查询

**请求**：
```json
POST /api/Order/GetList

{
  "ids": [1, 2, 3]
}
```

**生成的 SQL**：
```sql
WHERE [order].id IN (@id0, @id1, @id2)
```

**返回**：
```json
{
  "success": true,
  "code": "200-0-0",
  "message": "操作成功",
  "data": [
    {
      "id": 1,
      "number": "01",
      "shippedDate": "2025-10-14T00:00:00",
      "customerId": 123,
      "customerNumber": "C001",
      "customerNickName": "客户A",
      "productId": 5,
      "productNumber": "P001",
      "pounds": 50,
      "projectId": 10,
      "projectName": "项目A",
      "collapse": 10,
      "printCollapse": 10,
      "ratio": "1:2",
      "businessId": 2,
      "buisnessName": "业务员王",
      "totalshippingOrder": 5,
      "totalOutputMeters": 1000,
      "totalRemaining": 950
    },
    // ... 更多订单
  ]
}
```

---

### 示例 2: 根据出货日期查询

**请求**：
```json
POST /api/Order/GetList

{
  "shippedDate": "2025-10-15"
}
```

**生成的 SQL**：
```sql
WHERE [order].shippedDate = @shippedDate
```

**返回**：所有 2025-10-15 的订单列表

---

### 示例 3: 组合查询（IDs + 日期）

**请求**：
```json
POST /api/Order/GetList

{
  "ids": [1, 2, 3, 4, 5],
  "shippedDate": "2025-10-15"
}
```

**生成的 SQL**：
```sql
WHERE [order].id IN (@id0, @id1, @id2, @id3, @id4) 
  AND [order].shippedDate = @shippedDate
```

**返回**：ID 在 [1,2,3,4,5] 且出货日期为 2025-10-15 的订单

---

### 示例 4: 查询所有订单

**请求**：
```json
POST /api/Order/GetList

{}
```

**生成的 SQL**：
```sql
-- 无 WHERE 条件，返回所有订单
```

---

## 📊 返回字段说明

### 基本字段
| 字段 | 类型 | 说明 |
|------|------|------|
| `id` | long | 订单ID |
| `number` | string | 订单编号 |
| `shippedDate` | datetime | 出货日期 |
| `customerId` | long | 客户ID |
| `customerNumber` | string | 客户编号 |
| `customerNickName` | string | 客户昵称 |

### 产品字段
| 字段 | 类型 | 说明 |
|------|------|------|
| `productId` | long | 产品ID |
| `productNumber` | string | 产品编号 |
| `pounds` | decimal | 磅数 |

### 项目字段
| 字段 | 类型 | 说明 |
|------|------|------|
| `projectId` | long | 项目ID |
| `projectName` | string | 项目名称 |

### 订单详情字段
| 字段 | 类型 | 说明 |
|------|------|------|
| `collapse` | int | 坍度 |
| `printCollapse` | int | 打印坍度 |
| `ratio` | string | 配比 |

### 业务字段
| 字段 | 类型 | 说明 |
|------|------|------|
| `businessId` | long | 业务员ID |
| `buisnessName` | string | 业务员名称 |

### 统计字段（来自 shippingOrder）
| 字段 | 类型 | 说明 |
|------|------|------|
| `totalshippingOrder` | int | 出货单总笔数 |
| `totalOutputMeters` | decimal | 总出货米数 |
| `totalRemaining` | decimal | 总剩余米数（出货米数 - 退货米数）|

---

## 🔍 统计字段计算逻辑

### totalshippingOrder（出货单总笔数）
```sql
COUNT(*) -- 统计该订单的所有出货记录
```

### totalOutputMeters（总出货米数）
```sql
SUM(ISNULL(outputMeters, 0)) -- 将 null 当 0 处理后求和
```

### totalRemaining（总剩余米数）
```sql
SUM(ISNULL(outputMeters, 0)) - SUM(ISNULL(returnMeters, 0))
-- 出货米数总和 - 退货米数总和
```

**示例**：
如果某订单有以下出货记录：

| orderId | outputMeters | returnMeters |
|---------|--------------|--------------|
| 1       | 100          | 10           |
| 1       | 200          | 20           |
| 1       | NULL         | 5            |

结果：
- `totalshippingOrder` = 3
- `totalOutputMeters` = 300 (100 + 200 + 0)
- `totalRemaining` = 265 (300 - 35)

---

## 💡 使用建议

### ✅ 推荐做法

1. **精确查询**：优先使用 `ids` 参数
```json
{ "ids": [1, 2, 3] }
```

2. **日期范围查询**：查询特定日期的订单
```json
{ "shippedDate": "2025-10-15" }
```

3. **组合查询**：两个条件都满足
```json
{
  "ids": [1, 2, 3],
  "shippedDate": "2025-10-15"
}
```

### ⚠️ 注意事项

1. **日期格式**：支持多种格式
   - ✅ `"2025-10-15"`
   - ✅ `"2025/10/15"`
   - ✅ `"2025-10-15T00:00:00"`

2. **IDs 类型**：支持数字和字符串
   - ✅ `[1, 2, 3]`
   - ✅ `["1", "2", "3"]`

3. **空参数**：返回所有订单（慎用）
   - ⚠️ `{}` - 可能返回大量数据

---

## 🚀 完整请求示例

### 使用 Postman

```http
POST /api/Order/GetList
Headers:
  Database: your-database-name
  Authorization: Bearer eyJhbGc...

Body (raw JSON):
{
  "ids": [1, 2, 3],
  "shippedDate": "2025-10-15"
}
```

### 使用 C# HttpClient

```csharp
var client = new HttpClient();
client.DefaultRequestHeaders.Add("Database", "your-database");
client.DefaultRequestHeaders.Add("Authorization", "Bearer your-token");

var request = new
{
    ids = new[] { 1, 2, 3 },
    shippedDate = "2025-10-15"
};

var json = JsonSerializer.Serialize(request);
var content = new StringContent(json, Encoding.UTF8, "application/json");

var response = await client.PostAsync(
    "https://your-api/api/Order/GetList", 
    content
);

var result = await response.Content.ReadAsStringAsync();
```

### 使用 JavaScript/Fetch

```javascript
fetch('/api/Order/GetList', {
  method: 'POST',
  headers: {
    'Content-Type': 'application/json',
    'Database': 'your-database',
    'Authorization': 'Bearer your-token'
  },
  body: JSON.stringify({
    ids: [1, 2, 3],
    shippedDate: '2025-10-15'
  })
})
.then(response => response.json())
.then(data => {
  console.log('订单列表:', data.data);
});
```

---

完成！ 🎉

