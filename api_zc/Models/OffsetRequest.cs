namespace Accura_MES.Models
{
    /// <summary>
    /// 沖帳請求模型
    /// </summary>
    public class OffsetRequest
    {
        /// <summary>
        /// 沖帳記錄
        /// </summary>
        public OffsetRecordModel offsetRecord { get; set; }

        /// <summary>
        /// 沖帳收款單列表
        /// </summary>
        public List<OffsetRecordReceiptModel> offsetRecordReceipts { get; set; } = new List<OffsetRecordReceiptModel>();

        /// <summary>
        /// 沖帳出貨單列表
        /// </summary>
        public List<OffsetRecordShippingOrderModel> offsetRecordShippingOrders { get; set; } = new List<OffsetRecordShippingOrderModel>();
    }

    /// <summary>
    /// 沖帳記錄模型
    /// </summary>
    public class OffsetRecordModel
    {
        /// <summary>
        /// 客戶 ID
        /// </summary>
        public long customerId { get; set; }

        /// <summary>
        /// 編號（系統自行產生）
        /// </summary>
        public string? number { get; set; }

        /// <summary>
        /// 沖帳日期
        /// </summary>
        public string offsetDate { get; set; }

        /// <summary>
        /// 備註
        /// </summary>
        public string? note { get; set; }

        /// <summary>
        /// 折扣
        /// </summary>
        public decimal discount { get; set; }

        /// <summary>
        /// 預付金額
        /// </summary>
        public decimal prePayMoney { get; set; }

        /// <summary>
        /// 總沖帳金額
        /// </summary>
        public decimal totalOffsetMoney { get; set; }

        /// <summary>
        /// 應沖帳金額
        /// </summary>
        public decimal shouldBeOffsetMoney { get; set; }

        /// <summary>
        /// 客戶編號（僅用於顯示，不存入資料庫）
        /// </summary>
        public string? customerNumber { get; set; }

        /// <summary>
        /// 客戶名稱（僅用於顯示，不存入資料庫）
        /// </summary>
        public string? customerName { get; set; }

        /// <summary>
        /// 客戶暱稱（僅用於顯示，不存入資料庫）
        /// </summary>
        public string? customerNickName { get; set; }

        /// <summary>
        /// 沖帳結果（僅用於顯示，不存入資料庫）
        /// </summary>
        public decimal? offsetResult { get; set; }
    }

    /// <summary>
    /// 沖帳收款單模型
    /// </summary>
    public class OffsetRecordReceiptModel
    {
        /// <summary>
        /// 收款單 ID
        /// </summary>
        public long receiptId { get; set; }

        /// <summary>
        /// 金額
        /// </summary>
        public decimal price { get; set; }
    }

    /// <summary>
    /// 沖帳出貨單模型
    /// </summary>
    public class OffsetRecordShippingOrderModel
    {
        /// <summary>
        /// 出貨單 ID
        /// </summary>
        public long shippingOrderId { get; set; }

        /// <summary>
        /// 價格
        /// </summary>
        public decimal price { get; set; }

        /// <summary>
        /// 數量
        /// </summary>
        public decimal quantity { get; set; }

        /// <summary>
        /// 沖帳金額
        /// </summary>
        public decimal offsetMoney { get; set; }
    }
}

