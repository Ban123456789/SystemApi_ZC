namespace Accura_MES.Models
{
    /// <summary>
    /// 客戶價格模型
    /// </summary>
    public class CustomerPriceModel
    {
        public string? ObjectType { get; set; }
        public string? Id { get; set; }
        public long CustomerId { get; set; }
        public string? TaxType { get; set; }
        public long ProjectId { get; set; }
        public long ProductId { get; set; }
        public int Collapse { get; set; }
        public decimal Price { get; set; }
        public bool IsDelete { get; set; }
        public long? CreatedBy { get; set; }
        public DateTime? CreatedOn { get; set; }
        public long? ModifiedBy { get; set; }
        public DateTime? ModifiedOn { get; set; }
    }
}
