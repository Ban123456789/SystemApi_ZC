# 序列号自动编号系统 - 使用说明

## 📦 系统文件

### 核心代码
- `api_zc/Interfaces/Services/ISequenceService.cs` - 服务接口
- `api_zc/Services/SequenceService.cs` - **核心服务（SQL 逻辑在此）**
- `api_zc/Models/SequenceNumberConfig.cs` - **配置文件（需要修改）**
- `api_zc/Repositories/GenericRepositoryExtensions.cs` - 扩展方法

### 数据库脚本
- `api_zc/App_Data/InitialDatas/CreateSequenceNumbersTable.sql` - 表结构（如果已有则忽略）

---

## 🚀 使用步骤

### 步骤 1: 确认数据库表存在

如果 `sequenceNumbers` 表不存在，执行 SQL 脚本：
```sql
-- 文件: CreateSequenceNumbersTable.sql
```

表结构：
- `tableName` - 表名称
- `groupKey` - 分组键
- `currentNumber` - 当前编号
- `numberFormat` - 编号格式

---

### 步骤 2: 配置需要自动编号的表

编辑 `api_zc/Models/SequenceNumberConfig.cs`：

```csharp
public static Dictionary<string, SequenceNumberConfig> GetConfigs()
{
    return new Dictionary<string, SequenceNumberConfig>(StringComparer.OrdinalIgnoreCase)
    {
        // Project: 根据 customerId 分组，格式 0001
        ["Project"] = new SequenceNumberConfig
        {
            TableName = "Project",
            NumberFieldName = "number",
            GroupByFieldName = "customerId",
            NumberFormat = "0000"
        },

        // Order: 根据 shippedDate 分组，格式 01
        ["Order"] = new SequenceNumberConfig
        {
            TableName = "Order",
            NumberFieldName = "number",
            GroupByFieldName = "shippedDate",
            NumberFormat = "00"
        },

        // ShippingOrder: 从关联表 Order 获取 shippedDate
        ["ShippingOrder"] = new SequenceNumberConfig
        {
            TableName = "ShippingOrder",
            NumberFieldName = "number",
            GroupByFieldName = "orderId",
            NumberFormat = "0000",
            JoinTableName = "Order",
            JoinFieldName = "id",
            JoinGroupByFieldName = "shippedDate"
        }
    };
}
```

---

### 步骤 3: 使用方式

#### 方式 A: 自动集成到 GenericRepository（推荐）

在 `GenericRepository.cs` 的 `CreateDataGeneric` 方法中添加：

```csharp
public async Task<List<long>> CreateDataGeneric(SqlConnection connection, SqlTransaction? transaction,
    long user, string tableName, List<Dictionary<string, object?>> input)
{
    // 👇 添加这三行（在获取 propertyItems 之前）
    await GenericRepositoryExtensions.AutoGenerateNumbers(
        connection, transaction, tableName, input, _connectionString);

    var propertyItems = await _propertyRepository.GetPropertiesAsync(tableName);
    // ... 其他代码
}
```

#### 方式 B: 手动调用

```csharp
using Accura_MES.Services;

// 创建服务
ISequenceService sequenceService = SequenceService.CreateService(connectionString);

// 在事务中生成编号
using var connection = new SqlConnection(connectionString);
await connection.OpenAsync();
using var transaction = connection.BeginTransaction();

try
{
    string number = await sequenceService.GetNextNumberAsync(
        connection,
        transaction,
        "Project",      // 表名
        "123",          // 分组键（如 customerId）
        "0000"          // 格式
    );
    
    // 使用 number 进行后续操作
    
    await transaction.CommitAsync();
}
catch
{
    await transaction.RollbackAsync();
    throw;
}
```

---

## 🔧 核心原理

### SQL 取号逻辑（在 SequenceService.cs 中）

```csharp
// 1. 更新并获取新编号（使用锁防止并发）
UPDATE sequenceNumbers WITH (UPDLOCK, HOLDLOCK)
SET currentNumber = currentNumber + 1,
    modifiedOn = GETDATE()
OUTPUT INSERTED.currentNumber
WHERE tableName = @TableName AND groupKey = @GroupKey

// 2. 如果记录不存在，插入新记录
INSERT INTO sequenceNumbers (tableName, groupKey, currentNumber, numberFormat, createdOn, modifiedOn)
VALUES (@TableName, @GroupKey, 1, @NumberFormat, GETDATE(), GETDATE())

// 3. 格式化编号
string formattedNumber = currentNum.ToString().PadLeft(numberFormat.Length, '0');
```

---

## 📋 配置说明

| 参数 | 说明 | 示例 |
|------|------|------|
| `TableName` | 表名称 | "Project" |
| `NumberFieldName` | 编号字段名 | "number" |
| `GroupByFieldName` | 分组字段 | "customerId" |
| `NumberFormat` | 格式（位数） | "0000" = 4位 |
| `JoinTableName` | 关联表（可选） | "Order" |
| `JoinFieldName` | 关联表主键（可选） | "id" |
| `JoinGroupByFieldName` | 从关联表获取的分组字段（可选） | "shippedDate" |

---

## ✅ 示例场景

### 场景 1: Project 表
- **需求**: 每个客户的项目编号独立
- **分组**: `customerId`
- **编号**: 0001, 0002, 0003...

### 场景 2: Order 表
- **需求**: 每天的订单编号独立
- **分组**: `shippedDate`
- **编号**: 01, 02, 03...（每天重新开始）

### 场景 3: ShippingOrder 表
- **需求**: 根据关联订单的日期分组
- **分组**: 从 `Order` 表查询 `shippedDate`
- **编号**: 0001, 0002, 0003...（每天独立）

---

## 🛠️ 管理操作

### 查询当前编号
```sql
SELECT * FROM sequenceNumbers;
```

### 重置编号
```sql
UPDATE sequenceNumbers 
SET currentNumber = 0 
WHERE tableName = 'Project' AND groupKey = '123';
```

或使用代码：
```csharp
await sequenceService.ResetSequenceAsync("Project", "123", 0);
```

---

## ⚠️ 注意事项

1. **并发安全**: 使用 `UPDLOCK, HOLDLOCK` 锁机制，支持多用户并发
2. **跳号问题**: 事务回滚时编号不会回收（正常现象）
3. **字段配置**: 编号字段不要设为必填，系统会自动填充
4. **日期格式**: 日期类型会自动格式化为 `yyyy-MM-dd`

---

完成！ 🎉

