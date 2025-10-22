# CustomerPriceController 使用說明

## API 概述
CustomerPriceController 提供客戶價格資料的建立功能，包含重複檢查機制。

## API 端點
- **URL**: `POST /api/CustomerPrice/Create`
- **功能**: 建立客戶價格資料
- **驗證**: 需要有效的 JWT Token 和 Database Header

## 請求格式

### Headers
```
Authorization: Bearer <JWT_TOKEN>
Database: <DATABASE_NAME>
Content-Type: application/json
```

### Request Body
```json
[
  {
    "objectType": "customerPrice",
    "id": "lejdnijB404Jj7NL",
    "customerId": 1,
    "taxType": "內含",
    "projectId": 9,
    "productId": 1,
    "collapse": 0,
    "price": 2900,
    "isDelete": false,
    "printCollapse": 0
  }
]
```

### 欄位說明
| 欄位名稱 | 類型 | 必填 | 說明 |
|---------|------|------|------|
| objectType | string | 否 | 物件類型，預設為 "customerPrice" |
| id | string | 否 | 唯一識別碼，如未提供會自動生成 GUID |
| customerId | long | 是 | 客戶代號 |
| taxType | string | 否 | 稅別，預設為 "內含" |
| projectId | long | 是 | 工程代號 |
| productId | long | 是 | 成品代號 |
| collapse | int | 否 | 摺疊狀態，預設為 0 |
| price | decimal | 是 | 價格 |
| isDelete | bool | 否 | 是否刪除，預設為 false |
| createdBy | long | 否 | 建立者，會自動設定為當前使用者（系統自動填入） |
| createdOn | DateTime | 否 | 建立時間，會自動設定為當前時間（系統自動填入） |
| modifiedBy | long | 否 | 修改者，會自動設定為當前使用者（系統自動填入） |
| modifiedOn | DateTime | 否 | 修改時間，會自動設定為當前時間（系統自動填入） |
| printCollapse | int | 否 | 列印摺疊狀態，預設為 0 |

## 回應格式

### 成功回應 (200)
```json
{
  "code": "200",
  "data": [
    "lejdnijB404Jj7NL",
    "abc123def456ghi789"
  ],
  "errorCode": "SUCCESS",
  "errorData": [],
  "message": "Success",
  "success": true
}
```

### 錯誤回應

#### 1. 重複資料錯誤 (400-15)
```json
{
  "code": "400-100",
  "data": "",
  "errorCode": "NESTED_STRUCTURE_ERROR",
  "errorData": [
    {
      "code": "400-15",
      "rowIndex": 0,
      "errorData": ["customerId", "projectId", "productId"],
      "message": "第 1 筆資料已存在相同的工程代號 + 客戶代號 + 成品代號"
    }
  ],
  "message": "詳情請看error data",
  "success": false
}
```

#### 2. 必填欄位缺失 (400-7)
```json
{
  "code": "400-100",
  "data": "",
  "errorCode": "NESTED_STRUCTURE_ERROR",
  "errorData": [
    {
      "code": "400-7",
      "rowIndex": 0,
      "missingFields": ["customerId", "projectId", "productId"],
      "message": "第 1 筆資料缺失必要欄位: customerId, projectId, productId"
    }
  ],
  "message": "詳情請看error data",
  "success": false
}
```

#### 3. 資料為空 (400-7)
```json
{
  "code": "400-7",
  "data": "",
  "errorCode": "MISSING_PARAMETERS",
  "errorData": [],
  "message": "客戶價格資料不能為空",
  "success": false
}
```

## 業務邏輯

### 重複檢查
API 會檢查是否存在相同的 `customerId + projectId + productId` 組合，且 `isDelete = false` 的記錄。如果存在重複，會回傳錯誤碼 `400-15`。

### 交易處理
所有資料建立都在同一個資料庫交易中進行，如果任何一筆資料發生錯誤，整個交易會回滾。

### 自動欄位設定
- `createdBy` 和 `modifiedBy` 會自動設定為當前登入使用者的 ID
- `createdOn` 和 `modifiedOn` 會自動設定為當前時間
- 如果 `id` 未提供，會自動生成 GUID

## 資料庫需求

### CustomerPrice 表結構
```sql
CREATE TABLE CustomerPrice (
    ObjectType NVARCHAR(50),
    Id NVARCHAR(50) PRIMARY KEY,
    CustomerId BIGINT NOT NULL,
    TaxType NVARCHAR(50),
    ProjectId BIGINT NOT NULL,
    ProductId BIGINT NOT NULL,
    Collapse INT DEFAULT 0,
    Price DECIMAL(18,2) NOT NULL,
    IsDelete BIT DEFAULT 0,
    CreatedBy BIGINT,
    CreatedOn DATETIME2,
    ModifiedBy BIGINT,
    ModifiedOn DATETIME2,
    PrintCollapse INT DEFAULT 0
);

-- 建議建立複合索引以提升查詢效能
CREATE INDEX IX_CustomerPrice_CustomerId_ProjectId_ProductId 
ON CustomerPrice (CustomerId, ProjectId, ProductId) 
WHERE IsDelete = 0;
```

## 使用範例

### cURL 範例
```bash
curl -X POST "https://your-api-url/api/CustomerPrice/Create" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Database: YOUR_DATABASE_NAME" \
  -H "Content-Type: application/json" \
  -d '[
    {
      "objectType": "customerPrice",
      "id": "lejdnijB404Jj7NL",
      "customerId": 1,
      "taxType": "內含",
      "projectId": 9,
      "productId": 1,
      "collapse": 0,
      "price": 2900,
      "isDelete": false,
      "printCollapse": 0
    }
  ]'
```

### JavaScript 範例
```javascript
const response = await fetch('/api/CustomerPrice/Create', {
  method: 'POST',
  headers: {
    'Authorization': 'Bearer ' + token,
    'Database': databaseName,
    'Content-Type': 'application/json'
  },
  body: JSON.stringify([
    {
      objectType: "customerPrice",
      id: "lejdnijB404Jj7NL",
      customerId: 1,
      taxType: "內含",
      projectId: 9,
      productId: 1,
      collapse: 0,
      price: 2900,
      isDelete: false,
      printCollapse: 0
    }
  ])
});

const result = await response.json();
console.log(result);
```

## 架構說明

### 分層架構
本 API 遵循項目的標準分層架構：

1. **Controller 層** (`CustomerPriceController`)
   - 處理 HTTP 請求和回應
   - 驗證 Token 和基本參數
   - 調用 Service 層處理業務邏輯

2. **Service 層** (`CustomerPriceService`)
   - 實現業務邏輯
   - 處理資料驗證和重複檢查
   - 管理資料庫交易

3. **Repository 層** (`GenericRepository`)
   - 處理資料庫操作
   - 提供通用的 CRUD 功能

4. **Model 層** (`CustomerPriceModel`)
   - 定義資料結構
   - 提供型別安全

### 設計模式
- **工廠模式**: Service 使用靜態工廠方法創建實例
- **依賴注入**: Controller 依賴 Service 介面
- **交易模式**: 使用資料庫交易確保資料一致性
