namespace Accura_MES.Models
{
    public class GenericModel
    {
    }

    #region 進階查詢架構
    public class QueryObject
    {
        public string? Field { get; set; }
        /// <summary>
        /// =、!=、like、not like、null、not null、&lt;、&lt;=、&gt;、&gt;=
        /// </summary>
        public string? Operate { get; set; }
        public string? Value { get; set; }
    }

    /// <summary>
    /// object type value
    /// </summary>
    public class QueryObjectObj
    {
        public string? Field { get; set; }
        /// <summary>
        /// =、!=、like、not like、null、not null、&lt;、&lt;=、&gt;、&gt;=
        /// </summary>
        public string? Operate { get; set; }
        public object? Value { get; set; }
    }

    public class InnerSearch
    {
        public string? Datasheet { get; set; }      // 資料表名稱
        public string? Dataname { get; set; }       // 資料欄位名稱
        public List<string>? Datas { get; set; }    // 欄位value
    }

    public class AdvancedSearch
    {
        public string? Datasheet { get; set; }
        /// <summary>
        /// 多國語言
        /// </summary>
        public bool Localization { get; set; }
        public List<QueryObject>? And { get; set; }
        public List<QueryObject>? Or { get; set; }
        public List<QueryObject>? Order { get; set; }
        /// <summary>
        /// "SQL":"sql query string"，讓Ben直接把WHERE條件值入查詢，這個條件式目前只開放給"exceptiondays"查詢用
        /// </summary>
        public string? SQL { get; set; }

        /// <summary>
        /// 任何查詢可用: 指定第一層資料的查詢欄位。
        /// </summary>
        /// <remarks>
        /// 如果是空的，那就是照舊的查詢方式，所有欄位都查詢。
        /// </remarks>
        public System.Collections.Generic.HashSet<string> SelectPrimaryColumns { get; set; } = new();

        /// <summary>
        /// 巢狀查詢專用: 指定第二層資料的查詢欄位。
        /// </summary>
        /// <remarks>
        /// Key: 第一層的欄位名稱, Value: 第二層的要顯示的欄位名稱。
        /// 如果是空的，那就是照舊的查詢方式，所有欄位都查詢。
        /// </remarks>
        public Dictionary<string, System.Collections.Generic.HashSet<string>> SelectForeignColumns { get; set; } = new();
    }

    /// <summary>
    /// object type value
    /// </summary>
    public class AdvancedSearchObj : AdvancedSearch
    {
        // 這裡的屬性是為了讓呼叫者能夠使用 object 的方式來傳遞資料

        public new List<QueryObjectObj>? And { get; set; }
        public new List<QueryObjectObj>? Or { get; set; }
        public new List<QueryObjectObj>? Order { get; set; }
    }


    #region 巢狀條件測試
    public class Condition
    {
        public string Field { get; set; }
        public string Operator { get; set; }
        public object Value { get; set; }
        public string? Logic { get; set; }
        public List<Condition>? NestedConditions { get; set; }
    }

    public class ConditionGroup
    {
        public List<Condition> Conditions { get; set; } = new();
    }
    #endregion
    #endregion

    /// <summary>
    /// 紀錄一筆時間範圍
    /// </summary>
    public class TimeRange
    {
        public DateTime StartTime { get; set; } // 開始時間
        public DateTime EndTime { get; set; }   // 結束時間

        public override string ToString()
        {
            return $"StartTime: {StartTime}, EndTime: {EndTime}";
        }
    }
}
