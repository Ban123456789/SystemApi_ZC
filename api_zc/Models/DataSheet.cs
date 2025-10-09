using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Accura_MES.Models
{
    public class LoginInfo
    {
        public string Account { get; set; }
        public string Password { get; set; }
    }

    /// <summary>
    /// 資料表與其資料
    /// </summary>
    public class TableDatas
    {
        /// <summary>
        /// 資料表名稱
        /// </summary>
        public string? Datasheet { get; set; }

        /// <summary>
        /// Key 欄位名稱，例如：id
        /// <para></para>
        /// Value 欄位值
        /// </summary>
        public List<Dictionary<string, object>>? DataStructure { get; set; }
    }

    public class UserInfo
    {
        [Key]
        public long id { get; set; }
        public string number { get; set; }
        public string surname { get; set; }
        public string name { get; set; }
        [Key]
        public string account { get; set; }
        public string password { get; set; }
        public string email { get; set; }
        public long phone { get; set; }
        public string firstPage { get; set; }
        public string firstPageForPhone { get; set; }
        public float salary { get; set; }
        public string salaryUnit { get; set; }
        public bool isenable { get; set; }
        public bool isdelete { get; set; }
        public long createdBy { get; set; }
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public DateTime createdOn { get; set; }
        public long modifiedBy { get; set; }
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public DateTime modifiedOn { get; set; }

    }

    public class Calendar
    {
        //[DatabaseGenerated(DatabaseGeneratedOption.Computed)]: 資料庫自動處理
        //[Required]: 設定必填欄位  [Required(ErrorMessage = "Number is required")]
        //[MaxLength]: 設定最大字串長度  [MaxLength(100, ErrorMessage = "Name can't be longer than 100 characters")]
        //[Column]: 自定欄位名稱或資料庫型別  [Column("Calendar_Number", TypeName = "nvarchar(50)")]
        //[DatabaseGenerated]: 控制資料庫生成行為 
        //[DatabaseGenerated(DatabaseGeneratedOption.Identity)]用於告訴資料庫這個欄位的值是由資料庫自動生成的，通常用於主鍵的自增值。


        public long id { get; set; }
        public string number { get; set; }
        public string name { get; set; }
        public long createdBy { get; set; }
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public DateTime? createdOn { get; set; }
        public long modifiedBy { get; set; }
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public DateTime? modifiedOn { get; set; }
    }

    public class Property
    {
        public long id { get; set; }
        public string name { get; set; }
        public string label { get; set; }
        public string dataType { get; set; }
        public string dataSource { get; set; }
        public int sroredLength { get; set; }
        public int scale { get; set; }
        public bool isOnly { get; set; }
        public bool isRequired { get; set; }
        public string columnWidth { get; set; }
        public string columnIndex { get; set; }
        public string rowIndex { get; set; }
        public string? defaultValue { get; set; }
        public long createdBy { get; set; }
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public DateTime? createdOn { get; set; }
        public long modifiedBy { get; set; }
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public DateTime? modifiedOn { get; set; }
    }


}
