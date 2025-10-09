namespace Accura_MES.Models
{
    public class PropertyModel
    {
        public long Id { get; set; }                  // 對應 [id]
        public string Name { get; set; }              // 對應 [name]
        public string? Label { get; set; }            // 對應 [label]
        public string? DataType { get; set; }         // 對應 [dataType]
        public string? DataSource { get; set; }       // 對應 [dataSource]
        public int? StoredLength { get; set; }        // 對應 [storedLength]
        public int? Scale { get; set; }               // 對應 [scale]
        public bool IsDelete { get; set; }            // 對應 [isDelete]
        public bool IsOnly { get; set; }              // 對應 [isOnly]
        public bool IsRequired { get; set; }          // 對應 [isRequired]
        public int? ColumnWidth { get; set; }         // 對應 [columnWidth]
        public int? ColumnIndex { get; set; }         // 對應 [columnIndex]
        public int? RowIndex { get; set; }            // 對應 [rowIndex]
        public int SortIndex { get; set; }            // 對應 [sortIndex]
        public string? DefaultValue { get; set; }     // 對應 [defaultValue]
        public long ItemTypeId { get; set; }          // 對應 [itemTypeId]
        public string PropertyType { get; set; }      // 對應 [propertyType]
        public long CreatedBy { get; set; }           // 對應 [createdBy]
        public DateTime CreatedOn { get; set; }       // 對應 [createdOn]
        public long ModifiedBy { get; set; }          // 對應 [modifiedBy]
        public DateTime ModifiedOn { get; set; }      // 對應 [modifiedOn]
    }

    /// <summary>
    /// [property]的部分資料，驗證使用者輸入用
    /// </summary>
    public class PropertyInputValidItem
    {
        public string Name { get; set; }          // 欄位名
        public bool IsRequired { get; set; }
        public object? DefaultValue { get; set; }
    }
}
