# GetAccountingByCustomer API 使用說明

## API 端點
- **URL**: `POST /api/CustomerPrice/GetAccountingByCustomer`
- **功能**: 取得客戶別簡表（會計用）

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
  "data": [
    {
      "shippingOrderId": 1,
      "outputMeters": 100.5,
      "remaining": 50.0,
      "price": 1500.00,
      "customerId": 1,
      "customerNickName": "客戶A",
      "orderId": 101,
      "shippedDate": "2025-10-10",
      "projectId": 1,
      "projectNumber": "PRJ001",
      "projectName": "專案A",
      "productId": 1,
      "productNumber": "PRD001"
    }
  ]
}
```

### 錯誤響應
```json
{
  "success": false,
  "code": "400-1",
  "message": "查詢請求不能為空",
  "data": null
}
```

## 使用範例

### 1. 查詢所有客戶的資料
```json
{
  "startShippedDate": null,
  "endShippedDate": null,
  "customerIds": []
}
```

### 2. 查詢特定日期範圍的資料
```json
{
  "startShippedDate": "2025-10-01T00:00:00Z",
  "endShippedDate": "2025-10-31T23:59:59Z",
  "customerIds": []
}
```

### 3. 查詢特定客戶的資料
```json
{
  "startShippedDate": "2025-10-01T00:00:00Z",
  "endShippedDate": "2025-10-31T23:59:59Z",
  "customerIds": [1, 2, 3]
}
```

## 注意事項
1. 所有日期參數都是可選的，如果為 null 則不會加入該條件
2. customerIds 為空陣列時會查詢所有客戶
3. 結果會按照出貨日期降序、客戶暱稱、專案編號排序
4. 只會回傳 isDelete = 0 的資料
