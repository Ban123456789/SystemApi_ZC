# RollbackReceivable API 使用說明

## API 端點
- **URL**: `POST /api/CustomerPrice/RollbackReceivable`
- **功能**: 應收帳款退回

## 請求格式

### Headers
```
Authorization: Bearer {token}
Database: {database_name}
Content-Type: application/json
```

### Request Body
```json
{
  "startShippedDate": "2025-10-10T00:00:00Z",  // 可選，開始出貨日期
  "endShippedDate": "2025-10-10T23:59:59Z",    // 可選，結束出貨日期
  "customerIds": [1, 2, 3]                     // 可選，客戶 ID 陣列，空陣列表示查詢所有客戶
}
```

### 參數說明
- `startShippedDate`: 開始出貨日期，可為 null
- `endShippedDate`: 結束出貨日期，可為 null  
- `customerIds`: 客戶 ID 陣列，可為空陣列

## 響應格式

### 成功響應 (200)
```json
{
  "success": true,
  "code": "200-0",
  "message": "成功",
  "data": {
    "totalProcessed": 10,
    "updatedCount": 10,
    "message": "退回完成：共處理 10 筆，成功退回 10 筆"
  }
}
```

### 錯誤響應
```json
{
  "success": false,
  "code": "400-1",
  "message": "退回請求不能為空",
  "data": null
}
```

## 業務邏輯說明

### 1. 查詢需要處理的出貨單
系統會根據以下條件查詢出貨單：
- `shippingOrder.isDelete = 0` (未刪除)
- 根據請求參數動態添加日期和客戶條件

### 2. 退回應收帳款
針對每個查詢到的出貨單：
- 將 `shippingOrder.price` 設為 `0`
- 更新 `modifiedOn` 為當前時間

## 使用範例

### 1. 退回所有客戶的資料
```json
{
  "startShippedDate": null,
  "endShippedDate": null,
  "customerIds": []
}
```

### 2. 退回特定日期範圍的資料
```json
{
  "startShippedDate": "2025-10-01T00:00:00Z",
  "endShippedDate": "2025-10-31T23:59:59Z",
  "customerIds": []
}
```

### 3. 退回特定客戶的資料
```json
{
  "startShippedDate": "2025-10-01T00:00:00Z",
  "endShippedDate": "2025-10-31T23:59:59Z",
  "customerIds": [1, 2, 3]
}
```

## 注意事項
1. 所有日期參數都是可選的，如果為 null 則不會加入該條件
2. customerIds 為空陣列時會處理所有客戶
3. 只會處理 `isDelete = 0` 的出貨單
4. 所有符合條件的出貨單價格都會被設為 0
5. 操作會在事務中執行，確保資料一致性
6. 此操作會將出貨單的價格重置為 0，請謹慎使用
