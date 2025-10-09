using Accura_MES.Extensions;
using Accura_MES.Interfaces.Services;
using Accura_MES.Models;
using Accura_MES.Repositories;
using Accura_MES.Service;
using Accura_MES.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Diagnostics;
using System.Text.Json;
// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Accura_MES.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GenericController : ControllerBase
    {

        private readonly JwtService jwt;
        public GenericController(JwtService jwt)
        {
            this.jwt = jwt;
        }

        private XML xml = new();




        /// <summary>
        /// query in 搜尋
        /// </summary>
        /// <param name="innerSearch"></param>
        /// <returns></returns>
        [HttpPost("Get")]
        public async Task<IActionResult> Get([FromBody] InnerSearch innerSearch)
        {
            try
            {
                #region 檢查
                // check Headers
                ResponseObject result = UserController.CheckToken(Request);
                if (!result.Success)
                {
                    return StatusCode(int.Parse(result.Code.Split("-")[0]), result);
                }

                // check Body
                var requiredProperties = new[] { "Datasheet", "Dataname", "Datas" };
                var missingFields = new List<string>();
                var validationResult = new List<object>();

                foreach (var requiredProperty in requiredProperties)
                {
                    var value = innerSearch.GetType().GetProperty(requiredProperty)?.GetValue(innerSearch);
                    if (value == null || (value is string str && string.IsNullOrEmpty(str)))
                    {
                        missingFields.Add(requiredProperty);
                    }
                }
                if (missingFields.Any())
                {
                    validationResult.Add(new
                    {
                        code = $"400-7-0",
                        rowIndex = 0,
                        missingFields,
                        message = $"缺失必要欄位: {string.Join(", ", missingFields)}"
                    });

                    // 驗證失敗，直接返回錯誤
                    return this.CustomAccuraResponse(SelfErrorCode.NESTED_STRUCTURE_ERROR, null, validationResult, "詳情請看error data");
                }
                #endregion

                string connectionString = xml.GetConnection(Request.Headers["Database"].ToString());

                var genericRepository = GenericRepository.CreateRepository(connectionString, innerSearch.Datasheet);

                var data = genericRepository.GenericGet(innerSearch);

                return this.SuccessAccuraResponse(data);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
                return await this.HandleAccuraExceptionAsync(null, ex);
            }
        }

        /// <summary>
        /// 針對輸入的資料表和搜尋條件作Query查詢，只處理單表
        /// </summary>
        /// <param name="advancedSearch"></param>
        /// <returns></returns>
        [HttpPost("SimpleSearch")]
        public async Task<IActionResult> SimpleSearch([FromBody] AdvancedSearch advancedSearch)
        {
            try
            {
                #region 檢查
                // check Headers
                var result = UserController.CheckToken(Request);
                if (!result.Success)
                {
                    return StatusCode(int.Parse(result.Code.Split("-")[0]), result);
                }

                // check Body
                var requiredProperties = new[] { "Datasheet" };
                var missingFields = new List<string>();
                var validationResult = new List<object>();

                foreach (var requiredProperty in requiredProperties)
                {
                    var value = advancedSearch.GetType().GetProperty(requiredProperty)?.GetValue(advancedSearch);
                    if (value == null || (value is string str && string.IsNullOrEmpty(str)))
                    {
                        missingFields.Add(requiredProperty);
                    }
                }
                if (missingFields.Any())
                {
                    validationResult.Add(new
                    {
                        code = $"400-7-0",
                        rowIndex = 0,
                        missingFields,
                        message = $"缺失必要欄位: {string.Join(", ", missingFields)}"
                    });

                    // 驗證失敗，直接返回錯誤
                    return this.CustomAccuraResponse(SelfErrorCode.NESTED_STRUCTURE_ERROR, null, validationResult, "詳情請看error data");
                }
                #endregion


                string connectionString = xml.GetConnection(Request.Headers["Database"].ToString());

                var genericRepository = GenericRepository.CreateRepository(connectionString, advancedSearch.Datasheet);

                var data = genericRepository.GenericAdvancedGet(advancedSearch);

                return this.SuccessAccuraResponse(data);
            }
            catch (Exception ex)
            {
                return await this.HandleAccuraExceptionAsync(null, ex);
            }
        }

        /// <summary>
        /// 針對輸入的資料表和搜尋條件作Query查詢，只處理單表
        /// </summary>
        /// <param name="advancedSearch"></param>
        /// <returns>第一筆資料</returns>
        [HttpPost("SimpleSearchTop")]
        public async Task<IActionResult> SimpleSearchTop([FromBody] AdvancedSearch advancedSearch)
        {
            try
            {
                #region 檢查
                // check Headers
                var result = UserController.CheckToken(Request);
                if (!result.Success)
                {
                    return StatusCode(int.Parse(result.Code.Split("-")[0]), result);
                }

                // check Body
                var requiredProperties = new[] { "Datasheet" };
                var missingFields = new List<string>();
                var validationResult = new List<object>();

                foreach (var requiredProperty in requiredProperties)
                {
                    var value = advancedSearch.GetType().GetProperty(requiredProperty)?.GetValue(advancedSearch);
                    if (value == null || (value is string str && string.IsNullOrEmpty(str)))
                    {
                        missingFields.Add(requiredProperty);
                    }
                }
                if (missingFields.Any())
                {
                    validationResult.Add(new
                    {
                        code = $"400-7-0",
                        rowIndex = 0,
                        missingFields,
                        message = $"缺失必要欄位: {string.Join(", ", missingFields)}"
                    });

                    // 驗證失敗，直接返回錯誤
                    return this.CustomAccuraResponse(SelfErrorCode.NESTED_STRUCTURE_ERROR, null, validationResult, "詳情請看error data");
                }
                #endregion


                string connectionString = xml.GetConnection(Request.Headers["Database"].ToString());
                using var connection = new SqlConnection(connectionString);
                connection.Open();

                var genericRepository = GenericRepository.CreateRepository(connectionString, advancedSearch.Datasheet);

                var data = genericRepository.GenericAdvancedGetTop(connection, null, advancedSearch);

                return this.SuccessAccuraResponse(data);
            }
            catch (Exception ex)
            {
                return await this.HandleAccuraExceptionAsync(null, ex);
            }
        }

        /// <summary>
        /// 巢狀結構資料查詢模板
        /// </summary>
        /// <param name="advancedSearch"></param>
        /// <returns></returns>
        [HttpPost("GetNestedStructure")]
        public async Task<IActionResult> NestedStructureDataQueryTemplate([FromBody] AdvancedSearch advancedSearch)
        {
            try
            {
                string connectionString = xml.GetConnection(Request.Headers["Database"].ToString());

                #region 檢查
                // check Headers
                ResponseObject result = UserController.CheckToken(Request);
                if (!result.Success)
                {
                    return StatusCode(int.Parse(result.Code.Split("-")[0]), result);
                }

                // check Body
                var validationResult = new List<object>();
                if (string.IsNullOrEmpty(advancedSearch.Datasheet))
                {
                    validationResult.Add(new
                    {
                        code = $"400-7-0",
                        rowIndex = 0,
                        missingFields = new List<string>(),
                        message = "缺失欄位資訊，未提供資料表名稱"
                    });

                    // 驗證失敗，直接返回錯誤
                    return this.CustomAccuraResponse(SelfErrorCode.NESTED_STRUCTURE_ERROR, null, validationResult, "詳情請看error data");
                }
                #endregion

                var genericRepository = GenericRepository.CreateRepository(connectionString, advancedSearch.Datasheet);

                // 取得巢狀資料
                var nestData = genericRepository.GetNestedStructureData(advancedSearch, Request.Headers["i18n"].ToString());

                return this.SuccessAccuraResponse(nestData);

            }

            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
                return await this.HandleAccuraExceptionAsync(null, ex);
            }

        }

        /// <summary>
        /// 巢狀結構資料查詢模板
        /// </summary>
        /// <param name="advancedSearch"></param>
        /// <returns></returns>
        /// <remarks>
        /// 用 object type value 來接收資料
        /// </remarks>
        [HttpPost("GetNestedStructure2")]
        public async Task<IActionResult> NestedStructureDataQueryTemplate([FromBody] AdvancedSearchObj advancedSearch)
        {
            try
            {
                string connectionString = xml.GetConnection(Request.Headers["Database"].ToString());

                #region 檢查
                // check Headers
                ResponseObject result = UserController.CheckToken(Request);
                if (!result.Success)
                {
                    return StatusCode(int.Parse(result.Code.Split("-")[0]), result);
                }

                // check Body
                var validationResult = new List<object>();
                if (string.IsNullOrEmpty(advancedSearch.Datasheet))
                {
                    validationResult.Add(new
                    {
                        code = $"400-7-0",
                        rowIndex = 0,
                        missingFields = new List<string>(),
                        message = "缺失欄位資訊，未提供資料表名稱"
                    });

                    // 驗證失敗，直接返回錯誤
                    return this.CustomAccuraResponse(SelfErrorCode.NESTED_STRUCTURE_ERROR, null, validationResult, "詳情請看error data");
                }
                #endregion

                var genericRepository = GenericRepository.CreateRepository(connectionString, advancedSearch.Datasheet);

                // 取得巢狀資料
                var nestData = genericRepository.GetNestedStructureData(advancedSearch, Request.Headers["i18n"].ToString());

                return this.SuccessAccuraResponse(nestData);

            }

            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
                return await this.HandleAccuraExceptionAsync(null, ex);
            }

        }

        [HttpPut("Upsert")]
        public async Task<IActionResult> Upsert([FromBody] Dictionary<string, object?> input)
        {
            try
            {
                string connectionString = xml.GetConnection(Request.Headers["Database"].ToString());
                string authorizationHeader = HttpContext.Request.Headers["Authorization"];
                var token = JwtService.AnalysisToken(authorizationHeader);

                #region 檢查
                // check Headers
                ResponseObject result = UserController.CheckToken(Request);
                if (!result.Success)
                {
                    return StatusCode(int.Parse(result.Code.Split("-")[0]), result);
                }

                // check Body
                // 創建輸入處理service
                IInputValidationService inputValidationService = InputValidationService.CreateService(connectionString, null, API.Create);

                // 檢查必填欄位
                var validationResult1 = await inputValidationService.ValidateAndFilterAndSetDefaultInput(
                    null,
                    new List<Dictionary<string, object>>()
                    {
                        input
                    },
                    new HashSet<string>()
                    {
                        "tableName", "primaryKeys", "values"
                    }
                );
                if (validationResult1 != null)
                {
                    // 驗證失敗，直接返回錯誤
                    return this.CustomAccuraResponse(SelfErrorCode.NESTED_STRUCTURE_ERROR, null, validationResult1, "詳情請看error data");
                }

                string? tableName = input.GetValueOrDefault("tableName")?.ToString();

                // 手動解析 primaryKeys 為 List<string>
                var primaryKeysJsonElement = input.GetValueOrDefault("primaryKeys") as JsonElement?;
                var primaryKeys = primaryKeysJsonElement?.EnumerateArray()
                                                                  .Select(element => element.GetString()!)
                                                                  .ToHashSet();

                // 手動解析 values 為 Dictionary<string, object?>
                var valuesJsonElement = input.GetValueOrDefault("values") as JsonElement?;
                Dictionary<string, object?>? values = valuesJsonElement?.Deserialize<Dictionary<string, object?>>();


                #endregion

                var genericRepository = GenericRepository.CreateRepository(connectionString, null);

                // 取得巢狀資料
                var connection = new SqlConnection(connectionString);
                connection.Open();

                long user = long.Parse(token["sub"].ToString());

                var nestData = await genericRepository.Upsert(tableName, primaryKeys, values, user, connection, null);

                return this.SuccessAccuraResponse(nestData);

            }

            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
                return await this.HandleAccuraExceptionAsync(null, ex);
            }

        }

        public class AlterTableRequest
        {
            public Dictionary<string, string> AlterData { get; set; }
        }

        /// <summary>
        /// 添加欄位API(未完成)
        /// </summary>
        /// <param name="alterData"></param>
        /// <returns></returns>
        [HttpPut]
        public IActionResult AddColumn([FromBody] AlterTableRequest alterData)
        {
            #region 檢查
            if (!Request.Headers.ContainsKey("Database"))
            {
                return BadRequest("缺失Request Header屬性Database");
            }

            //根據使用者ID動態取得連接字串
            string connectionString = xml.GetConnection(Request.Headers["Database"].ToString());
            if (string.IsNullOrEmpty(connectionString))
            {
                return StatusCode(500, "Internal server error: sql not exist");
            }
            #endregion
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    string sql = $"ALTER TABLE {alterData.AlterData["table_name"]} ADD {alterData.AlterData["column_name"]} {alterData.AlterData["datatype"]};";
                    //打開連接
                    connection.Open();
                    using (SqlCommand command = new SqlCommand(sql, connection))
                    {
                        command.ExecuteNonQuery();
                    }
                }
                ResponseObject result = new ResponseObject().GenerateEntity(SelfErrorCode.SUCCESS, "", "");
                return StatusCode(int.Parse(result.Code.Split("-")[0]), result);
            }
            catch (Exception ex)
            {
                //記錄例外或返回詳細錯誤資訊
                return StatusCode(500, "Internal server error: " + ex.Message);
            }
        }


        /// <summary>
        /// 通用 多資料建立
        /// </summary>
        /// <param name="shareinfo"></param>
        /// <returns></returns>
        [HttpPost("Create")]
        public async Task<IActionResult> Create([FromBody] TableDatas shareinfo)
        {
            try
            {
                string connectionString = new XML().GetConnection(Request.Headers["Database"].ToString());
                string authorizationHeader = HttpContext.Request.Headers["Authorization"];
                var token = JwtService.AnalysisToken(authorizationHeader);
                var results = new List<object>();

                #region 檢查
                // check Headers
                ResponseObject result = UserController.CheckToken(Request);
                if (!result.Success) return StatusCode(int.Parse(result.Code.Split("-")[0]), result);

                // 不允許在特殊資料表使用通用建立
                foreach (var datasheet in InputFilters.NoActionDatasheets)
                {
                    if (shareinfo.Datasheet == datasheet)
                    {
                        result = new ResponseObject().GenerateEntity(SelfErrorCode.DATASHEET_NOT_ALLOW, "", "");
                        return StatusCode(int.Parse(result.Code.Split("-")[0]), result);
                    }
                }

                // check Body
                // 創建輸入處理service
                var inputValidationService = InputValidationService.CreateService(connectionString, shareinfo.Datasheet, API.Create);

                // 檢查必填欄位、過濾不允許的欄位、設定預設值
                var validationResult = await inputValidationService.ValidateAndFilterAndSetDefaultInput(
                    shareinfo.Datasheet,
                    shareinfo.DataStructure,
                    null,
                    null,
                    false
                );

                if (validationResult != null)
                {
                    // 驗證失敗，直接返回錯誤
                    return this.CustomAccuraResponse(SelfErrorCode.NESTED_STRUCTURE_ERROR, null, validationResult, "詳情請看error data");
                }
                #endregion

                // 創建通用repository
                var genericRepository = GenericRepository.CreateRepository(connectionString, shareinfo.Datasheet);

                // 插入資料
                var insertId = await genericRepository.CreateDataGeneric(long.Parse(token["sub"]), shareinfo.Datasheet, shareinfo.DataStructure);

                return this.SuccessAccuraResponse(insertId);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                return await this.HandleAccuraExceptionAsync(null, ex);
            }
        }


        /// <summary>
        /// 通用 多資料編輯
        /// </summary>
        /// <param name="shareinfo"></param>
        /// <returns></returns>
        [HttpPut("Update")]
        public async Task<IActionResult> Update([FromBody] TableDatas shareinfo)
        {
            string connectionString = new XML().GetConnection(Request.Headers["Database"].ToString());
            string authorizationHeader = HttpContext.Request.Headers["Authorization"];
            var token = JwtService.AnalysisToken(authorizationHeader);


            try
            {
                #region 檢查
                // check Headers
                ResponseObject result = UserController.CheckToken(Request);
                if (!result.Success) return StatusCode(int.Parse(result.Code.Split("-")[0]), result);

                foreach (var datasheet in InputFilters.NoActionDatasheets)
                {
                    if (shareinfo.Datasheet == datasheet)
                    {
                        result = new ResponseObject().GenerateEntity(SelfErrorCode.DATASHEET_NOT_ALLOW, "", "");
                        return StatusCode(int.Parse(result.Code.Split("-")[0]), result);
                    }
                }

                // check Body
                // 創建輸入處理service
                var inputValidationService = InputValidationService.CreateService(connectionString, shareinfo.Datasheet, API.Update);

                // 檢查必填欄位、過濾不允許的欄位、設定預設值
                var validationResult = await inputValidationService.ValidateAndFilterAndSetDefaultInput(
                    shareinfo.Datasheet,
                    shareinfo.DataStructure,
                    null,
                    null,
                    false
                );

                if (validationResult != null)
                {
                    // 驗證失敗，直接返回錯誤
                    return this.CustomAccuraResponse(SelfErrorCode.NESTED_STRUCTURE_ERROR, null, validationResult, "詳情請看error data");
                }
                #endregion


                int affectedNumber = 0;
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    using (SqlTransaction transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            var queries = new List<string>();
                            var allParameters = new Dictionary<string, object>();

                            int rowIndex = 0;
                            foreach (var row in shareinfo.DataStructure)
                            {
                                var setClauses = new List<string>();
                                var parameters = new Dictionary<string, object>();

                                if (!row.ContainsKey("id"))
                                {
                                    result = new ResponseObject().GenerateEntity(SelfErrorCode.MISSING_ID, "", "");
                                    return StatusCode(int.Parse(result.Code.Split("-")[0]), result);
                                }

                                row["modifiedBy"] = token["sub"];

                                foreach (var kvp in row)
                                {
                                    if (!InputFilters.NonEditableFields.Contains(kvp.Key))
                                    {
                                        string paramKey = $"@{kvp.Key}_{rowIndex}";

                                        if (kvp.Key != "id")
                                        {
                                            setClauses.Add($"{kvp.Key} = {paramKey}");
                                        }

                                        if (kvp.Value != null) parameters[paramKey] = kvp.Value.ToString();
                                        else parameters[paramKey] = DBNull.Value;
                                    }
                                }

                                string query = $"UPDATE [{shareinfo.Datasheet}] SET {string.Join(", ", setClauses)} WHERE Id = @Id_{rowIndex}";
                                queries.Add(query);

                                foreach (var param in parameters)
                                {
                                    allParameters[param.Key] = param.Value;
                                }

                                rowIndex++;
                            }


                            string combinedQuery = string.Join("; ", queries);
                            Debug.WriteLine(combinedQuery);
                            using (SqlCommand command = new SqlCommand(combinedQuery, connection, transaction))
                            {
                                foreach (var param in allParameters)
                                {
                                    command.Parameters.AddWithValue(param.Key, param.Value);
                                }

                                affectedNumber += await command.ExecuteNonQueryAsync();
                            }

                            await transaction.CommitAsync();

                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine(ex);
                            return this.HandleAccuraException(transaction, ex);
                        }
                    }
                }

                if (affectedNumber == 0)
                {
                    List<object> ids = shareinfo.DataStructure
                        .Where(dict => dict.ContainsKey("id"))  // 篩選出包含 "id" 鍵的字典
                        .Select(dict => dict["id"])             // 取出 "id" 的值
                        .ToList();

                    return this.CustomAccuraResponse(SelfErrorCode.NO_DATA_AFFECTED, "", "", $"ids : {string.Join(", ", ids)}");
                }
                else
                {
                    return this.SuccessAccuraResponse();
                }
            }
            catch (Exception ex)
            {
                return this.HandleAccuraException(null, ex);
            }
        }


        /// <summary>
        /// 通用 多資料刪除(更改isdelete)
        /// </summary>
        /// <param name="shareinfo"></param>
        /// <returns></returns>
        [HttpDelete("isDelete")]
        public async Task<IActionResult> IsDelete([FromBody] TableDatas shareinfo)
        {
            string connectionString = new XML().GetConnection(Request.Headers["Database"].ToString());
            string authorizationHeader = HttpContext.Request.Headers["Authorization"];
            var token = JwtService.AnalysisToken(authorizationHeader);


            try
            {
                #region 檢查
                // check Headers
                ResponseObject result = UserController.CheckToken(Request);
                if (!result.Success) return StatusCode(int.Parse(result.Code.Split("-")[0]), result);

                // check Body
                // 創建輸入處理service
                var inputValidationService = InputValidationService.CreateService(connectionString, shareinfo.Datasheet, API.IsDelete);

                // 檢查必填欄位、過濾不允許的欄位、設定預設值
                var validationResult = await inputValidationService.ValidateAndFilterAndSetDefaultInput(
                    shareinfo.Datasheet,
                    shareinfo.DataStructure,
                    null,
                    null,
                    false
                );

                if (validationResult != null)
                {
                    // 驗證失敗，直接返回錯誤
                    return this.CustomAccuraResponse(SelfErrorCode.NESTED_STRUCTURE_ERROR, null, validationResult, "詳情請看error data");
                }
                #endregion

                IGenericService geneticService = GenericService.CreateService(connectionString);

                long user = long.Parse(token["sub"]);

                int affectedNumber = await geneticService.IsDelete(user, shareinfo);

                if (affectedNumber == 0)
                {
                    List<object> ids = shareinfo.DataStructure
                        .Where(dict => dict.ContainsKey("id"))  // 篩選出包含 "id" 鍵的字典
                        .Select(dict => dict["id"])             // 取出 "id" 的值
                        .ToList();

                    return this.CustomAccuraResponse(SelfErrorCode.NO_DATA_AFFECTED, "", "", $"ids : {string.Join(", ", ids)}");
                }
                else
                {
                    return this.SuccessAccuraResponse();
                }
            }
            catch (Exception ex)
            {
                return this.HandleAccuraException(null, ex);
            }
        }


        /// <summary>
        /// 通用 多資料刪除(真刪除)
        /// </summary>
        /// <param name="shareinfo"></param>
        /// <returns></returns>
        [HttpDelete("Delete")]
        public async Task<IActionResult> Delete([FromBody] TableDatas shareinfo)
        {
            string connectionString = new XML().GetConnection(Request.Headers["Database"].ToString());
            string authorizationHeader = HttpContext.Request.Headers["Authorization"];
            var token = JwtService.AnalysisToken(authorizationHeader);

            try
            {
                #region 檢查
                // check Headers
                ResponseObject result = UserController.CheckToken(Request);
                if (!result.Success) return StatusCode(int.Parse(result.Code.Split("-")[0]), result);

                // check Body
                // 創建輸入處理service
                var inputValidationService = InputValidationService.CreateService(connectionString, shareinfo.Datasheet, API.Delete);

                // 檢查必填欄位、過濾不允許的欄位、設定預設值
                var validationResult = await inputValidationService.ValidateAndFilterAndSetDefaultInput(
                    shareinfo.Datasheet,
                    shareinfo.DataStructure,
                    null,
                    null,
                    false
                );

                if (validationResult != null)
                {
                    // 驗證失敗，直接返回錯誤
                    return this.CustomAccuraResponse(SelfErrorCode.NESTED_STRUCTURE_ERROR, null, validationResult, "詳情請看error data");
                }
                #endregion

                int affectedNumber = 0;
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    using (SqlTransaction transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            var idList = new List<string>();
                            foreach (var row in shareinfo.DataStructure)
                            {
                                // 確認資料結構中有 'id' 欄位
                                if (!row.ContainsKey("id"))
                                {
                                    result = new ResponseObject().GenerateEntity(SelfErrorCode.MISSING_ID, "", "");
                                    return StatusCode(int.Parse(result.Code.Split("-")[0]), result);
                                }
                                idList.Add(row["id"].ToString());
                            }

                            // 構建單一的批量刪除 SQL 語句
                            string query = $"DELETE FROM [{shareinfo.Datasheet}] WHERE id IN ({string.Join(", ", idList.Select((id, index) => $"@Id_{index}"))})";

                            using (SqlCommand command = new SqlCommand(query, connection, transaction))
                            {
                                // 為每個 `id` 添加參數
                                for (int i = 0; i < idList.Count; i++)
                                {
                                    command.Parameters.AddWithValue($"@Id_{i}", idList[i].ToString());
                                }

                                // 執行批量刪除
                                affectedNumber += await command.ExecuteNonQueryAsync();
                            }

                            // 提交交易
                            await transaction.CommitAsync();

                        }
                        catch (Exception ex)
                        {
                            return this.HandleAccuraException(transaction, ex);
                        }
                    }
                }

                if (affectedNumber == 0)
                {
                    List<object> ids = shareinfo.DataStructure
                        .Where(dict => dict.ContainsKey("id"))  // 篩選出包含 "id" 鍵的字典
                        .Select(dict => dict["id"])             // 取出 "id" 的值
                        .ToList();

                    return this.CustomAccuraResponse(SelfErrorCode.NO_DATA_AFFECTED, "", "", $"ids : {string.Join(", ", ids)}");
                }
                else
                {
                    return this.SuccessAccuraResponse();
                }
            }
            catch (Exception ex)
            {
                return this.HandleAccuraException(null, ex);
            }
        }
    }
}
