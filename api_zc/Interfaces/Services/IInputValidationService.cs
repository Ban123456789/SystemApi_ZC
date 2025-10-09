using Accura_MES.Models;

namespace Accura_MES.Interfaces.Services
{
    public interface IInputValidationService : IService
    {
        /// <summary>
        /// 依使用者提供的必填欄位檢查輸入。
        /// 如果有提供資料表名稱，才幫輸入填入預設值
        /// </summary>
        /// <param name="tableName">資料表名</param>
        /// <param name="rows">輸入</param>
        /// <param name="requiredFields">使用者提供的必填欄位</param>
        /// <returns>null if input is valid, error data if not valid</returns>
        Task<List<object>?> ValidateAndFilterAndSetDefaultInput(
            string? tableName,
            List<Dictionary<string, object?>>? rows,
            HashSet<string>? requiredFields
        );

        /// <summary>
        /// 檢查與過濾使用者輸入，並幫使用者輸入設定預設值。
        /// 只處理單表。
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="rows"></param>
        /// <param name="requiredFields">指定的必填，如果沒有指定，則會以propery的值判斷哪些必填</param>
        /// <param name="excludeFields">另外排除掉的必填</param>
        /// <param name="isRelateTable">這張表是不是關聯表</param>
        /// <returns>null if input is valid, error data if not valid</returns>
        Task<List<object>?> ValidateAndFilterAndSetDefaultInput(
            string? tableName,
            List<Dictionary<string, object?>>? rows,
            HashSet<string>? requiredFields,
            HashSet<string>? excludeFields,
            bool isRelateTable
        );

        /// <summary>
        /// 檢查與過濾使用者輸入，並幫使用者輸入設定預設值。
        /// 只處理單表。
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="rows"></param>
        /// <param name="requiredFields">指定的必填，如果沒有指定，則會以propery的值判斷哪些必填</param>
        /// <param name="excludeFields">另外排除掉的必填</param>
        /// <param name="inValidFields">不允許的欄位</param>
        /// <param name="isRelateTable">這張表是不是關聯表</param>
        /// <returns>null if input is valid, error data if not valid</returns>
        Task<List<object>?> ValidateAndFilterAndSetDefaultInput(
            string? tableName,
            List<Dictionary<string, object?>>? rows,
            HashSet<string>? requiredFields,
            HashSet<string>? excludeFields,
            HashSet<string> inValidFields,
            bool isRelateTable
        );

        /// <summary>
        /// 檢查與過濾使用者輸入，並幫使用者輸入設定預設值。
        /// 只處理單表。
        /// </summary>
        /// <param name="rows">使用者輸入</param>
        /// <param name="requiredFields">主表指定必填欄位</param>
        /// <param name="propertyItems">主表在[property]部分映射</param>
        /// <returns>null if input is valid, error data if not valid</returns>
        List<object>? ValidateAndFilterAndSetDefaultInput(
            List<Dictionary<string, object?>>? rows,
            List<PropertyInputValidItem> propertyItems,
            HashSet<string>? requiredFields
        );

        /// <summary>
        /// 檢查與過濾使用者輸入，並幫使用者輸入設定預設值。
        /// 關聯表也一起處理。
        /// </summary>
        /// <param name="tableName">主表名</param>
        /// <param name="rows">使用者輸入的欄位資訊</param>
        /// <param name="requiredFields">主表指定必填欄位</param>
        /// <param name="foreignTables">關聯表的外鍵 => Dict:關聯表名, HashSet:這張關聯表的所有外鍵</param>
        /// <returns>null if input is valid, error data if not valid</returns>
        Task<List<object>?> ValidateAndFilterAndSetDefaultInput(
            string tableName,
            List<Dictionary<string, object?>> rows,
            HashSet<string>? requiredFields,
            Dictionary<string, HashSet<string>> foreignTables
        );

    }
}
