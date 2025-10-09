using Accura_MES.Interfaces.Services;
using Accura_MES.Models;
using Accura_MES.Repositories;
using System.Data;

namespace Accura_MES.Service
{
    public enum API
    {
        Create,     // 對應 CreateFilters
        Create2,    // 對應 NonCreatableFields
        Read,
        Update,
        Delete,
        IsDelete
    }

    public class InputFilters
    {
        /// <summary>
        /// 基礎過濾條件 -> 特殊欄位
        /// </summary>
        public static readonly string[] CreateFilters = { "id", "isDelete", "objectType", "createdBy", "createdOn", "modifiedBy", "modifiedOn" };
        public static readonly string[] ReadFilters = Array.Empty<string>();
        /// <summary>
        /// 基礎過濾條件 -> 特殊欄位
        /// </summary>
        public static readonly string[] UpdateFilters = { "isDelete", "objectType", "createdBy", "createdOn", "modifiedBy", "modifiedOn" };
        public static readonly string[] DeleteFilters = Array.Empty<string>();
        public static readonly string[] IsDeleteFilters = Array.Empty<string>();

        /// <summary>
        /// 通用API過濾條件 -> 特殊資料表
        /// </summary>
        public static readonly string[] NoActionDatasheets = { "user", "department", "workstation" };
        /// <summary>
        /// UPDATE API建query時禁止的參數
        /// </summary>
        public static readonly string[] NonEditableFields = { "objectType", "createdBy", "createdOn", "modifiedOn" };
        /// <summary>
        /// CREATE API建query時禁止的參數
        /// </summary>
        public static readonly string[] NonCreatableFields = { "id", "objectType", "createdOn", "modifiedOn" };
    }

    /// <summary>
    /// 對給定輸入做處理的服務物件
    /// </summary>
    public class InputValidationService : IInputValidationService
    {
        private PropertyRepository _propertyRepository;
        private readonly API _apiType;
        private readonly string _connectionString;

        private InputValidationService(string connectionString, API apiType, PropertyRepository propertyRepository)
        {
            _apiType = apiType;
            _connectionString = connectionString;
            _propertyRepository = propertyRepository;
        }

        /// <summary>
        /// 靜態工廠方法
        /// </summary>
        /// <param name="connectionString"></param>
        /// <param name="dataSheet"></param>
        /// <param name="apiType"></param>
        /// <returns></returns>
        /// <exception cref="CustomErrorCodeException"></exception>
        public static InputValidationService CreateService(string connectionString, string? dataSheet, API apiType)
        {
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new CustomErrorCodeException(SelfErrorCode.NOT_FOUND_WITH_MSG, "SQL connectionString not exist");
            }

            var propertyRepository = PropertyRepository.CreateRepository(connectionString);

            // 返回此物件
            return new InputValidationService(connectionString, apiType, propertyRepository);
        }

        /// <summary>
        /// 根據 Filters Enum 返回對應的輸入過濾條件
        /// </summary>
        /// <param name="apiType"></param>
        /// <returns>Input filters</returns>
        public static string[] GetInputFilterByEnum(API apiType)
        {
            return apiType switch
            {
                API.Create => InputFilters.CreateFilters,
                API.Read => InputFilters.ReadFilters,
                API.Update => InputFilters.UpdateFilters,
                API.Delete => InputFilters.DeleteFilters,
                API.IsDelete => InputFilters.IsDeleteFilters,
                API.Create2 => InputFilters.NonCreatableFields,
                _ => throw new ArgumentOutOfRangeException(nameof(apiType), apiType, "無效的過濾條件"),
            };
        }

        public async Task<List<object>?> ValidateAndFilterAndSetDefaultInput(
            string? tableName,
            List<Dictionary<string, object?>>? rows,
            HashSet<string> requiredFields
            )
        {
            // 創建通用repository
            var genericRepository = GenericRepository.CreateRepository(_connectionString, tableName);

            // 開始驗證輸入
            var errorDetails = new List<object>();
            errorDetails = _apiType switch
            {
                API.Create => ValidateInput(rows, requiredFields),
                API.Read => ValidateInput(rows, requiredFields),
                API.Update => ValidateInput_UPDATE(rows, new HashSet<string>()),    // 關聯表的UPDATE，使用者不需要填"id"
                API.Delete => ValidateInput(rows, new HashSet<string>() { "id" }),
                API.IsDelete => ValidateInput(rows, new HashSet<string>() { "id", "isDelete" }),
                _ => new List<object>() // 其他情況不執行任何操作
            };


            // *驗證失敗
            if (errorDetails.Any())
            {
                return errorDetails;
            }

            // 如果有提供資料表名稱，才幫輸入填入預設值
            if (tableName is not null)
            {
                var propertyItems = await _propertyRepository.GetPropertiesAsync(tableName);

                // 設定預設值
                SetDefaultValues(rows, propertyItems);
            }

            // *驗證成功
            return null;
        }


        public async Task<List<object>?> ValidateAndFilterAndSetDefaultInput(
            string? tableName,
            List<Dictionary<string, object?>>? rows,
            HashSet<string>? requiredFields,
            HashSet<string>? excludeFields,
            bool isRelateTable
            )
        {
            // 創建通用repository
            var genericRepository = GenericRepository.CreateRepository(_connectionString, tableName);

            // 獲取部分property model
            var propertyItems = await _propertyRepository.GetPropertiesAsync(tableName);

            // 如果沒指定必填欄位，就從[property]分析
            if (requiredFields == null || !requiredFields.Any())
            {
                // 取得使用者必填欄位
                requiredFields = GetRequiredFields(propertyItems);
            }

            // 必填欄位濾掉本來就不應該出現的數據
            requiredFields = requiredFields.Except(GetInputFilterByEnum(_apiType)).ToHashSet();



            // 排除掉指定的欄位
            if (excludeFields != null && excludeFields.Any())
                requiredFields = requiredFields.Except(excludeFields).ToHashSet();

            // 開始驗證輸入
            var errorDetails = new List<object>();
            if (isRelateTable)
            {
                errorDetails = _apiType switch
                {
                    API.Create => ValidateInput(rows, requiredFields),
                    API.Read => ValidateInput(rows, requiredFields),
                    API.Update => ValidateInput_UPDATE(rows, new HashSet<string>()),    // 關聯表的UPDATE，使用者不需要填"id"
                    API.Delete => ValidateInput(rows, new HashSet<string>() { "id" }),
                    API.IsDelete => ValidateInput(rows, new HashSet<string>() { "id", "isDelete" }),
                    _ => new List<object>() // 其他情況不執行任何操作
                };
            }
            else
            {
                errorDetails = _apiType switch
                {
                    API.Create => ValidateInput(rows, requiredFields),
                    API.Read => ValidateInput(rows, requiredFields),
                    API.Update => ValidateInput_UPDATE(rows, new HashSet<string>() { "id" }),
                    API.Delete => ValidateInput(rows, new HashSet<string>() { "id" }),
                    API.IsDelete => ValidateInput(rows, new HashSet<string>() { "id", "isDelete" }),
                    _ => new List<object>() // 其他情況不執行任何操作
                };
            }

            // *驗證失敗
            if (errorDetails.Any())
            {
                return errorDetails;
            }

            // *驗證通過
            // 過濾欄位
            foreach (var dict in rows)
            {
                RemoveInValidFields(GetInputFilterByEnum(_apiType), dict);
            }

            // 設定預設值
            SetDefaultValues(rows, propertyItems);

            return null; // 返回 null 表示驗證成功，無需回應錯誤
        }

        public async Task<List<object>?> ValidateAndFilterAndSetDefaultInput(
            string? tableName,
            List<Dictionary<string, object?>>? rows,
            HashSet<string>? requiredFields,
            HashSet<string>? excludeFields,
            HashSet<string> inValidFields,
            bool isRelateTable
            )
        {
            // 創建通用repository
            var genericRepository = GenericRepository.CreateRepository(_connectionString, tableName);

            // 獲取部分property model
            var propertyItems = await _propertyRepository.GetPropertiesAsync(tableName);

            // 如果沒指定必填欄位，就從[property]分析
            if (requiredFields == null || !requiredFields.Any())
            {
                // 取得使用者必填欄位
                requiredFields = GetRequiredFields(propertyItems);
            }

            // 必填欄位濾掉本來就不應該出現的數據
            requiredFields = requiredFields.Except(GetInputFilterByEnum(_apiType)).ToHashSet();

            // 排除掉指定的欄位
            if (excludeFields != null && excludeFields.Any())
                requiredFields = requiredFields.Except(excludeFields).ToHashSet();

            // 開始驗證輸入
            var errorDetails = new List<object>();
            if (isRelateTable)
            {
                errorDetails = _apiType switch
                {
                    API.Create => ValidateInput(rows, requiredFields, inValidFields),
                    API.Read => ValidateInput(rows, requiredFields, inValidFields),
                    API.Update => ValidateInput_UPDATE(rows, new HashSet<string>(), inValidFields),    // 關聯表的UPDATE，使用者不需要填"id"
                    API.Delete => ValidateInput(rows, new HashSet<string>() { "id" }, inValidFields),
                    API.IsDelete => ValidateInput(rows, new HashSet<string>() { "id", "isDelete" }, inValidFields),
                    _ => new List<object>() // 其他情況不執行任何操作
                };
            }
            else
            {
                errorDetails = _apiType switch
                {
                    API.Create => ValidateInput(rows, requiredFields, inValidFields),
                    API.Read => ValidateInput(rows, requiredFields, inValidFields),
                    API.Update => ValidateInput_UPDATE(rows, new HashSet<string>() { "id" }, inValidFields),
                    API.Delete => ValidateInput(rows, new HashSet<string>() { "id" }, inValidFields),
                    API.IsDelete => ValidateInput(rows, new HashSet<string>() { "id", "isDelete" }, inValidFields),
                    _ => new List<object>() // 其他情況不執行任何操作
                };
            }

            // *驗證失敗
            if (errorDetails.Any())
            {
                return errorDetails;
            }

            // *驗證通過
            // 過濾欄位
            foreach (var dict in rows)
            {
                RemoveInValidFields(GetInputFilterByEnum(_apiType), dict);
            }

            // 設定預設值
            SetDefaultValues(rows, propertyItems);

            return null; // 返回 null 表示驗證成功，無需回應錯誤
        }

        public List<object>? ValidateAndFilterAndSetDefaultInput(
            List<Dictionary<string, object?>>? rows,
            List<PropertyInputValidItem> propertyItems,
            HashSet<string>? requiredFields
            )
        {
            // 如果沒指定必填欄位，就從[property]分析
            if (requiredFields == null || !requiredFields.Any())
            {
                // 取得使用者必填欄位
                requiredFields = GetRequiredFields(propertyItems);
            }

            // 必填欄位濾掉本來就不應該出現的數據
            requiredFields = requiredFields.Except(GetInputFilterByEnum(_apiType)).ToHashSet();

            // 開始驗證輸入
            var errorDetails = _apiType switch
            {
                API.Create => ValidateInput(rows, requiredFields),
                API.Read => ValidateInput(rows, requiredFields),
                API.Update => ValidateInput_UPDATE(rows, new HashSet<string>() { "id" }),
                API.Delete => ValidateInput(rows, new HashSet<string>() { "id" }),
                API.IsDelete => ValidateInput(rows, new HashSet<string>() { "id", "isDelete" }),
                _ => new List<object>() // 其他情況不執行任何操作
            };

            // *驗證失敗
            if (errorDetails.Any())
            {
                return errorDetails;
            }

            // *驗證通過
            // 過濾欄位
            foreach (var dict in rows)
            {
                RemoveInValidFields(GetInputFilterByEnum(_apiType), dict);
            }

            // 設定預設值
            SetDefaultValues(rows, propertyItems);

            return null; // 返回 null 表示驗證成功，無需回應錯誤
        }

        public async Task<List<object>?> ValidateAndFilterAndSetDefaultInput(
            string tableName,
            List<Dictionary<string, object?>> rows,
            HashSet<string>? requiredFields,
            Dictionary<string, HashSet<string>> foreignTables
            )
        {
            // 檢查結果
            var errorDetails = new List<object>();

            // 檢查輸入筆數
            if (rows.Count - 1 != foreignTables.Count)
            {
                errorDetails.Add(new
                {
                    code = $"400-7",
                    rowIndex = 0,
                    missingFields = new List<string>(),
                    message = $"關聯表數量與輸入筆數不一致，關聯表數應為輸入筆數 - 1。輸入筆數:{rows.Count} 關聯表數量:{foreignTables.Count}"
                });

                // 驗證失敗，直接返回錯誤
                return errorDetails;
            }

            // 創建通用repository
            var genericRepository = GenericRepository.CreateRepository(_connectionString, tableName);

            // 獲取部分property model
            var propertyItems = await _propertyRepository.GetPropertiesAsync(tableName);

            // 如果沒指定必填欄位，就從[property]分析
            if (requiredFields == null || !requiredFields.Any())
            {
                // 取得使用者必填欄位
                requiredFields = GetRequiredFields(propertyItems);
            }

            // 必填欄位濾掉本來就不應該出現的數據
            requiredFields = requiredFields.Except(GetInputFilterByEnum(_apiType)).ToHashSet();

            // 開始驗證主表輸入
            var mainTableRow = new List<Dictionary<string, object?>>(new[] { rows[0] }); // 第一筆為主表
            errorDetails = _apiType switch
            {
                API.Create => ValidateInput(mainTableRow, requiredFields),
                API.Read => ValidateInput(mainTableRow, requiredFields),
                API.Update => ValidateInput_UPDATE(mainTableRow, new HashSet<string>() { "id" }),
                API.Delete => ValidateInput(mainTableRow, new HashSet<string>() { "id" }),
                API.IsDelete => ValidateInput(mainTableRow, new HashSet<string>() { "id", "isDelete" }),
                _ => new List<object>() // 其他情況不執行任何操作
            };

            // 開始處理關聯表
            var foreignErrorDetails = new List<object>();
            if (foreignTables.Any())
            {
                // 計算使用者輸入的筆數，第一筆開始為關聯表
                int rowIndex = 1;
                foreach (var foreignTable in foreignTables)
                {
                    string foreignTableName = foreignTable.Key;
                    HashSet<string> foreignKeys = foreignTable.Value;
                    foreignKeys.Add("id"); // 關聯表不需要填id

                    // 創建輸入處理service
                    var inputValidationService = CreateService(_connectionString, foreignTableName, _apiType);

                    // 開始分析關聯表必填欄位
                    var foreignPropertyItems = await _propertyRepository.GetPropertiesAsync(foreignTableName);
                    var foreignRequiredFields = GetRequiredFields(foreignPropertyItems);
                    foreignRequiredFields = foreignRequiredFields.Except(foreignKeys).ToHashSet();  // 排除關聯表不需要的必填欄位

                    // 照輸入順序驗證關聯表
                    var foreignErrorDetail = await inputValidationService.ValidateAndFilterAndSetDefaultInput(
                        foreignTableName,
                        new List<Dictionary<string, object?>>(new[] { rows[rowIndex] }),
                        foreignRequiredFields,
                        null,
                        true
                    );

                    if (foreignErrorDetail != null)
                    {
                        foreignErrorDetails.AddRange(foreignErrorDetail);
                    }

                    rowIndex++;
                }
            }
            if (foreignErrorDetails.Any())
            {
                errorDetails.AddRange(foreignErrorDetails);
            }
            // 驗證流程完畢


            // *驗證失敗
            if (errorDetails.Any())
            {
                return errorDetails;
            }

            // *驗證通過
            // 過濾欄位
            foreach (var dict in rows)
            {
                RemoveInValidFields(GetInputFilterByEnum(_apiType), dict);
            }

            // 設定預設值
            SetDefaultValues(rows, propertyItems);

            return null; // 返回 null 表示驗證成功，無需回應錯誤
        }

        /// <summary>
        /// 分析必填欄位
        /// </summary>
        /// <returns></returns>
        public static HashSet<string> GetRequiredFields(List<PropertyInputValidItem> propertyItems)
        {
            // 取得使用者必填欄位
            var requiredFields = new HashSet<string>();
            foreach (var item in propertyItems)
            {
                // IsRequired is true and DefaultValue is empty，則使用者必須輸入該欄位的值
                if (item.IsRequired && string.IsNullOrEmpty(item.DefaultValue.ToString()))
                {
                    requiredFields.Add(item.Name);
                }
            }

            return requiredFields;
        }

        /// <summary>
        /// 幫使用者輸入設定預設值
        /// </summary>
        /// <param name="rows"></param>
        private static void SetDefaultValues(List<Dictionary<string, object?>>? rows, List<PropertyInputValidItem> propertyItems)
        {
            // convert list to dictionaty
            var defaultValues = new Dictionary<string, object>();
            for (int i = 0; i < propertyItems.Count; i++)
            {
                if (!string.IsNullOrEmpty(propertyItems[i].DefaultValue.ToString()))
                    defaultValues.Add(propertyItems[i].Name, propertyItems[i].DefaultValue);
            }

            if (rows == null || !defaultValues.Any())
            {
                // "rows or defaultValues cannot be null."
                return;
            }

            foreach (var row in rows)
            {
                foreach (var kvp in defaultValues)
                {
                    // 如果 row 中不存在該 key，或該 key 的值為 null，則設置預設值
                    if (!row.ContainsKey(kvp.Key) || row[kvp.Key] == null)
                    {
                        row[kvp.Key] = kvp.Value;
                    }
                }
            }
        }

        /// <summary>
        /// 過濾 field，移除不符合的欄位（忽略大小寫）。
        /// </summary>
        /// <param name="filter">要過濾的參數</param>
        /// <param name="item">要處理的字典</param>
        public static void RemoveInValidFields(string[] filter, Dictionary<string, object> item)
        {
            // 使用忽略大小寫的比較器
            var comparer = StringComparer.OrdinalIgnoreCase;

            // 將 item.Keys 使用忽略大小寫的方式比對
            var keysToRemove = item.Keys
                .Where(key => filter.Contains(key, comparer))
                .ToList(); // ToList 防止在迭代時修改集合

            // 移除匹配的鍵
            foreach (var key in keysToRemove)
            {
                item.Remove(key);
            }
        }

        /// <summary>
        /// 過濾 field，移除不符合的欄位（忽略大小寫）。
        /// </summary>
        /// <param name="filter">要過濾的參數</param>
        public static void RemoveInValidFields(string[] filter, HashSet<string> inputColumns)
        {
            // 使用忽略大小寫的比較器
            var comparer = StringComparer.OrdinalIgnoreCase;

            // 將 item.Keys 使用忽略大小寫的方式比對
            var keysToRemove = inputColumns
                .Where(key => filter.Contains(key, comparer))
                .ToList(); // ToList 防止在迭代時修改集合

            // 移除匹配的鍵
            foreach (var key in keysToRemove)
            {
                inputColumns.Remove(key);
            }
        }

        /// <summary>
        /// 基礎檢查API輸入。
        /// 驗證輸入數據是否包含所有必填欄位
        /// </summary>
        /// <param name="input"></param>
        /// <param name="requiredFields"></param>
        /// <returns>詳細錯誤資訊</returns>
        /// <remarks>
        /// Helper of <see cref="ValidateAndFilterAndSetDefaultInput"/>
        /// </remarks>
        private static List<object> ValidateInput(List<Dictionary<string, object?>>? input, HashSet<string> requiredFields)
        {
            // 檢查結果
            var errorDetails = new List<object>();

            if (input == null || !input.Any())
            {
                errorDetails.Add(new
                {
                    code = $"400-7",
                    rowIndex = 0,
                    errorData = requiredFields,
                    message = "缺失欄位資訊，請至少輸入一筆欄位"
                });
                return errorDetails;
            }

            // 遍歷每筆資料
            int rowIndex = 0;
            foreach (var row in input)
            {
                var missingFields = requiredFields
                    .Where(field => !row.ContainsKey(field) || string.IsNullOrEmpty(row[field]?.ToString()))
                    .ToList();

                if (missingFields.Any())
                {
                    errorDetails.Add(new
                    {
                        code = $"400-7",
                        rowIndex = rowIndex + 1,
                        errorData = missingFields,
                        message = $"第 {rowIndex + 1} 筆資料缺失必要欄位: {string.Join(", ", missingFields)}"
                    });
                }
                rowIndex++;
            }

            return errorDetails;

        }

        /// <summary>
        /// 基礎檢查API輸入。
        /// 驗證輸入數據是否包含所有必填欄位，並且不包含不允許的欄位
        /// </summary>
        /// <param name="input"></param>
        /// <param name="requiredFields"></param>
        /// <param name="inValidFields">不允許的欄位</param>
        /// <returns>詳細錯誤資訊</returns>
        /// <remarks>
        /// Helper of <see cref="ValidateAndFilterAndSetDefaultInput"/>
        /// </remarks>
        private static List<object> ValidateInput(List<Dictionary<string, object?>>? input, HashSet<string> requiredFields, HashSet<string> inValidFields)
        {
            // 檢查結果
            var errorDetails = new List<object>();

            if (input == null || !input.Any())
            {
                errorDetails.Add(new
                {
                    code = $"400-7",
                    rowIndex = 0,
                    errorData = requiredFields,
                    message = "缺失欄位資訊，請至少輸入一筆欄位"
                });
                return errorDetails;
            }

            // 遍歷每筆資料
            int rowIndex = 0;
            foreach (var row in input)
            {
                // 檢查缺失的必填欄位
                var missingFields = requiredFields
                    .Where(field => !row.ContainsKey(field) || string.IsNullOrEmpty(row[field]?.ToString()))
                    .ToList();

                // 檢查是否包含不允許的欄位
                var invalidFields = inValidFields
                    .Where(field => row.ContainsKey(field))
                    .ToList();

                // 先判斷 如果缺失必填欄位，新增到錯誤資訊
                if (missingFields.Any())
                {
                    errorDetails.Add(new
                    {
                        code = $"400-7",
                        rowIndex = rowIndex + 1,
                        errorData = missingFields,
                        message = $"第 {rowIndex + 1} 筆資料缺失必要欄位: {string.Join(", ", missingFields)}"
                    });
                }

                // 如果包含不允許的欄位，新增到錯誤資訊
                else if (invalidFields.Any())
                {
                    errorDetails.Add(new
                    {
                        code = $"400-8",
                        rowIndex = rowIndex + 1,
                        errorData = invalidFields,
                        message = $"第 {rowIndex + 1} 筆資料包含不允許的欄位: {string.Join(", ", invalidFields)}"
                    });
                }

                rowIndex++;
            }

            return errorDetails;
        }


        /// <summary>
        /// 檢查UPDATE API輸入。
        /// 1.檢查每個字典是否包含必填欄位 id。
        /// 2.檢查每個字典中是否至少有兩個欄位（key-value pairs）。
        /// </summary>
        /// <param name="input"></param>
        /// <param name="requiredFields">
        /// <returns>詳細錯誤資訊</returns>
        /// <remarks>
        /// Helper of <see cref="ValidateAndFilterAndSetDefaultInput"/>
        /// </remarks>
        private static List<object> ValidateInput_UPDATE(List<Dictionary<string, object?>>? input, HashSet<string> requiredFields)
        {
            // 檢查結果
            var errorDetails = new List<object>();

            if (input == null || !input.Any())
            {
                errorDetails.Add(new
                {
                    code = $"400-7",
                    rowIndex = 0,
                    errorData = requiredFields,
                    message = "缺失欄位資訊，請至少輸入一筆欄位"
                });
                return errorDetails;
            }

            // 遍歷每筆資料
            int rowIndex = 0;
            foreach (var row in input)
            {
                var missingFields = requiredFields
                    .Where(field => !row.ContainsKey(field) || string.IsNullOrEmpty(row[field]?.ToString()))
                    .ToList();

                if (missingFields.Any())
                {
                    errorDetails.Add(new
                    {
                        code = $"400-7",
                        rowIndex = rowIndex + 1,
                        errorData = missingFields,
                        message = $"第 {rowIndex + 1} 筆資料缺失必要欄位: {string.Join(", ", missingFields)}"
                    });
                }
                else
                {
                    if (row.Count < 2)
                    {
                        errorDetails.Add(new
                        {
                            code = $"400-7",
                            rowIndex = rowIndex + 1,
                            errorData = missingFields,
                            message = $"第 {rowIndex + 1} 筆資料必須包含至少兩筆欄位. 目前欄位數: {row.Count}."
                        });
                    }
                }

                rowIndex++;
            }

            return errorDetails;
        }

        /// <summary>
        /// 檢查UPDATE API輸入。
        /// 1.檢查每個字典是否包含必填欄位 id，並且不包含不允許的欄位
        /// 2.檢查每個字典中是否至少有兩個欄位（key-value pairs）。
        /// </summary>
        /// <param name="input"></param>
        /// <param name="requiredFields">
        /// <returns>詳細錯誤資訊</returns>
        /// <remarks>
        /// Helper of <see cref="ValidateAndFilterAndSetDefaultInput"/>
        /// </remarks>
        private static List<object> ValidateInput_UPDATE(List<Dictionary<string, object?>>? input, HashSet<string> requiredFields, HashSet<string> inValidFields)
        {
            // 檢查結果
            var errorDetails = new List<object>();

            if (input == null || !input.Any())
            {
                errorDetails.Add(new
                {
                    code = $"400-7",
                    rowIndex = 0,
                    missingFields = new List<string>(),
                    message = "缺失欄位資訊"
                });
                return errorDetails;
            }

            // 遍歷每筆資料
            int rowIndex = 0;
            foreach (var row in input)
            {
                var missingFields = requiredFields
                    .Where(field => !row.ContainsKey(field) || string.IsNullOrEmpty(row[field]?.ToString()))
                    .ToList();

                // 檢查是否包含不允許的欄位
                var invalidFields = inValidFields
                    .Where(field => row.ContainsKey(field))
                    .ToList();

                // 先判斷必填
                if (missingFields.Any())
                {
                    errorDetails.Add(new
                    {
                        code = $"400-7",
                        rowIndex = rowIndex + 1,
                        errorData = missingFields,
                        message = $"第 {rowIndex + 1} 筆資料缺失必要欄位: {string.Join(", ", missingFields)}"
                    });
                }
                else
                {
                    if (row.Count < 2)
                    {
                        errorDetails.Add(new
                        {
                            code = $"400-7",
                            rowIndex = rowIndex + 1,
                            errorData = missingFields,
                            message = $"第 {rowIndex + 1} 筆資料必須包含至少兩筆欄位. 目前欄位數: {row.Count}."
                        });
                    }
                }

                // 如果包含不允許的欄位，新增到錯誤資訊
                if (!missingFields.Any() && invalidFields.Any())
                {
                    errorDetails.Add(new
                    {
                        code = $"400-8",
                        rowIndex = rowIndex + 1,
                        errorData = invalidFields,
                        message = $"第 {rowIndex + 1} 筆資料包含不允許的欄位: {string.Join(", ", invalidFields)}"
                    });
                }

                rowIndex++;
            }

            return errorDetails;
        }


    }
}