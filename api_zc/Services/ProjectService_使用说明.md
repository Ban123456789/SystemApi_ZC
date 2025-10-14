# ProjectService.Create 使用说明

## 📋 方法签名

```csharp
public async Task<ResponseObject> Create(long userId, List<Dictionary<string, object?>> projectObject)
```

---

## 📦 字段说明

| 字段名 | 类型 | 长度限制 | 必填 | 说明 |
|--------|------|---------|------|------|
| `number` | string | 100 | ❌ | **工程代碼（自动生成）** |
| `name` | string | 200 | ✅ | 工程名稱 |
| `note` | text | - | ❌ | 工程備註 |
| `customerId` | - | - | ✅ | 客戶代碼（item->customer） |
| `unit` | - | - | ❌ | 使用單位（list->ProjectUnit） |
| `businessId` | - | - | ❌ | 業務人員（item->business） |
| `areaCode` | - | - | ❌ | 區域代碼（list->AreaCode） |
| `address` | string | 200 | ❌ | 工程地址 |
| `complateStatus` | - | - | ❌ | 是否結案（list->ComplateStatus） |

---

## 🔢 自動編號規則

- **字段**: `number`
- **格式**: `0001`, `0002`, `0003`...
- **分組規則**: 根據 `customerId` 分組
  - 同一客戶：編號累加
  - 不同客戶：從 `0001` 重新開始

### 示例

```
客戶 A (customerId=123):
  - 第1個項目: number = "0001"
  - 第2個項目: number = "0002"
  - 第3個項目: number = "0003"

客戶 B (customerId=456):
  - 第1個項目: number = "0001"  ← 獨立編號
  - 第2個項目: number = "0002"
```

---

## 📝 使用示例

### 示例 1: 創建單個項目

```csharp
using Accura_MES.Services;

// 創建服務
var projectService = ProjectService.CreateService(connectionString);

// 準備數據
var projectData = new List<Dictionary<string, object?>>
{
    new Dictionary<string, object?>
    {
        { "customerId", 123 },
        { "name", "新建大樓工程" },
        { "address", "台北市信義區信義路100號" },
        { "note", "預計工期6個月" },
        { "unit", "平方米" },
        { "businessId", 5 },
        { "areaCode", "TPE" },
        { "complateStatus", 0 }
        // 不需要提供 number，系統會自動生成
    }
};

// 執行創建
var result = await projectService.Create(userId: 1, projectData);

if (result.Success)
{
    Console.WriteLine($"創建成功！");
    // result.Data 包含 insertedIds
}
```

### 示例 2: 批量創建多個項目（同一客戶）

```csharp
var projectData = new List<Dictionary<string, object?>>
{
    // 客戶 123 的第一個項目 -> number 自动生成 "0001"
    new()
    {
        { "customerId", 123 },
        { "name", "A棟建築" },
        { "address", "台北市大安區" }
    },
    
    // 客戶 123 的第二個項目 -> number 自动生成 "0002"
    new()
    {
        { "customerId", 123 },
        { "name", "B棟建築" },
        { "address", "台北市中山區" }
    }
};

var result = await projectService.Create(1, projectData);
// 兩個項目的 number 會是 "0001" 和 "0002"
```

### 示例 3: 批量創建多個項目（不同客戶）

```csharp
var projectData = new List<Dictionary<string, object?>>
{
    // 客戶 123 -> number = "0001" (假設是該客戶的第一個項目)
    new()
    {
        { "customerId", 123 },
        { "name", "客戶A的項目" }
    },
    
    // 客戶 456 -> number = "0001" (客戶456的第一個項目)
    new()
    {
        { "customerId", 456 },
        { "name", "客戶B的項目" }
    },
    
    // 客戶 123 -> number = "0002" (客戶123的第二個項目)
    new()
    {
        { "customerId", 123 },
        { "name", "客戶A的另一個項目" }
    }
};

var result = await projectService.Create(1, projectData);
```

---

## 🔄 完整流程

```
1. 接收項目數據
    ↓
2. 驗證必填字段（name, customerId）
    ↓
3. 驗證字段長度（name ≤ 200, address ≤ 200）
    ↓
4. 為每個項目生成編號
   └─ 根據 customerId 分組
   └─ 調用 SequenceService 獲取編號
   └─ 自動填充 number 字段
    ↓
5. 插入數據庫（使用事務）
    ↓
6. 返回成功結果（包含插入的ID列表）
```

---

## ⚠️ 錯誤處理

### 錯誤 1: 缺少必填字段

```json
{
  "success": false,
  "code": "400-xxx",
  "message": "第 1 筆數據缺少必填字段: name"
}
```

**解決方案**: 確保每筆數據都包含 `name` 和 `customerId`

### 錯誤 2: 字段長度超過限制

```json
{
  "success": false,
  "code": "400-xxx",
  "message": "第 1 筆數據的 name 長度超過限制 (200 字符)"
}
```

**解決方案**: 縮短字段內容

### 錯誤 3: customerId 為空

```json
{
  "success": false,
  "code": "400-xxx",
  "message": "customerId 不能為空"
}
```

**解決方案**: 確保提供有效的 customerId

---

## 🎯 最佳實踐

### ✅ 推薦做法

1. **批量創建時，同一客戶的項目編號會是連續的**
```csharp
// 好：同一客戶的項目放在一起
var data = new List<Dictionary<string, object?>>
{
    new() { { "customerId", 123 }, { "name", "項目1" } },  // 0001
    new() { { "customerId", 123 }, { "name", "項目2" } },  // 0002
    new() { { "customerId", 456 }, { "name", "項目A" } }   // 0001
};
```

2. **使用事務確保數據一致性**
   - 所有項目要么全部創建成功，要么全部失敗
   - 編號生成和數據插入在同一事務中

3. **不要手動提供 number**
   - 系統會自動生成並覆蓋

---

## 🔒 並發安全

- ✅ 支持多用戶同時創建項目
- ✅ 使用數據庫鎖機制確保編號不重複
- ✅ 同一客戶的編號會排隊生成，確保順序

---

## 📊 返回數據結構

```json
{
  "success": true,
  "code": "200-0-0",
  "message": "操作成功",
  "data": [101, 102, 103]
}
```

- `data`: 新創建的項目ID數組

---

完成！ 🎉

