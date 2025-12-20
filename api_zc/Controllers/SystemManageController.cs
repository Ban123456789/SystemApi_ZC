using Accura_MES.Extensions;
using Accura_MES.Interfaces.Services;
using Accura_MES.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace Accura_MES.Controllers
{

    [Route("api/[controller]")]
    [ApiController]

    public class SystemManageController : ControllerBase
    {
        private XML _xml = new();
        private readonly IConfiguration _configuration;

        public SystemManageController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        /// <summary>
        /// 用輸入物件刷新 [tableColumnSetting]
        /// </summary>
        /// <param name="input">long id, long userId, string tableName, string columnName, int sequence, bool isShow</param>
        /// <remarks>
        /// 用 userId, tableName, columnName  比對新舊資料
        /// </remarks>
        /// <returns></returns>
        [HttpPut("TableSetting")]
        public async Task<IActionResult> TableSetting([FromBody] List<Dictionary<string, object?>> input)
        {
            try
            {
                // 取得 connection
                string connectionString = _xml.GetConnection(Request.Headers["Database"].ToString());

                #region 檢查
                // 檢查Header
                ResponseObject result = UserController.CheckToken(Request);
                if (!result.Success)
                {
                    return StatusCode(int.Parse(result.Code.Split("-")[0]), result);
                }

                #endregion

                // 獲取 token
                var token = JwtService.AnalysisToken(HttpContext.Request.Headers["Authorization"]);
                long user = long.Parse(token["sub"]);

                ISystemManageService systemManageService = SystemManageService.CreateService(connectionString);

                // 刷新 [tableColumnSetting]
                ResponseObject responseObject = await systemManageService.TableSetting(user, input);

                // 直接返回
                return this.CustomAccuraResponse(responseObject);
            }
            catch (Exception ex)
            {
                // 使用統一方法處理例外
                return this.HandleAccuraException(null, ex);
            }
        }

        /// <summary>
        /// 備份資料庫
        /// </summary>
        /// <returns>備份文件下載</returns>
        [HttpGet("BackupSQL")]
        public async Task<IActionResult> BackupSQL()
        {
            string? backupFilePath = null;
            try
            {
                // 取得 connection
                string? databaseHeader = Request.Headers["Database"].ToString();
                if (string.IsNullOrEmpty(databaseHeader))
                {
                    return this.CustomAccuraResponse(
                        SelfErrorCode.MISSING_PARAMETERS,
                        null,
                        null,
                        "缺少 Database Header"
                    );
                }

                string connectionString = _xml.GetConnection(databaseHeader);
                if (string.IsNullOrEmpty(connectionString))
                {
                    return this.CustomAccuraResponse(
                        SelfErrorCode.NOT_FOUND_WITH_MSG,
                        null,
                        null,
                        $"找不到資料庫連線設定: {databaseHeader}"
                    );
                }

                #region 檢查
                // 檢查Header
                ResponseObject result = UserController.CheckToken(Request);
                if (!result.Success)
                {
                    return StatusCode(int.Parse(result.Code.Split("-")[0]), result);
                }
                #endregion

                ISystemManageService systemManageService = SystemManageService.CreateService(connectionString);

                // 從配置中讀取備份設置
                var backupPath = _configuration["BackupSettings:BackupPath"] ?? "App_Data/Backups";
                var commandTimeout = int.Parse(_configuration["BackupSettings:CommandTimeout"] ?? "300");
                var retentionCount = int.Parse(_configuration["BackupSettings:RetentionCount"] ?? "7");

                System.Diagnostics.Debug.WriteLine($"[SystemManageController] 開始備份，路徑: {backupPath}, 超時: {commandTimeout}秒, 保留份數: {retentionCount}");

                // 執行備份
                backupFilePath = await systemManageService.BackupSQL(backupPath, commandTimeout, retentionCount);

                if (string.IsNullOrEmpty(backupFilePath))
                {
                    return this.CustomAccuraResponse(
                        SelfErrorCode.INTERNAL_SERVER_ERROR,
                        null,
                        null,
                        "備份服務返回空路徑"
                    );
                }

                if (!System.IO.File.Exists(backupFilePath))
                {
                    return this.CustomAccuraResponse(
                        SelfErrorCode.INTERNAL_SERVER_ERROR,
                        null,
                        null,
                        $"備份文件不存在: {backupFilePath}"
                    );
                }

                System.Diagnostics.Debug.WriteLine($"[SystemManageController] 備份文件創建成功: {backupFilePath}");

                // 讀取備份文件並返回給用戶下載
                var fileBytes = await System.IO.File.ReadAllBytesAsync(backupFilePath);
                string fileName = System.IO.Path.GetFileName(backupFilePath);

                System.Diagnostics.Debug.WriteLine($"[SystemManageController] 文件讀取完成，大小: {fileBytes.Length} bytes, 文件名: {fileName}");

                // 返回文件下載（使用 ControllerBase.File 方法）
                System.Diagnostics.Debug.WriteLine($"[SystemManageController] 返回文件下載: {fileName}");
                return File(fileBytes, "application/octet-stream", fileName);
            }
            catch (CustomErrorCodeException customEx)
            {
                // 自定義異常，直接返回錯誤信息
                System.Diagnostics.Debug.WriteLine($"[SystemManageController] 自定義異常: {customEx.Message}");
                
                // 如果發生錯誤，嘗試清理備份文件
                if (!string.IsNullOrEmpty(backupFilePath) && System.IO.File.Exists(backupFilePath))
                {
                    try
                    {
                        System.IO.File.Delete(backupFilePath);
                    }
                    catch
                    {
                        // 忽略刪除錯誤
                    }
                }

                return this.CustomAccuraResponse(
                    customEx.SelfErrorCode,
                    null,
                    customEx.ErrorData,
                    customEx.Message
                );
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SystemManageController] 異常: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[SystemManageController] 堆疊: {ex.StackTrace}");

                // 如果發生錯誤，嘗試清理備份文件
                if (!string.IsNullOrEmpty(backupFilePath) && System.IO.File.Exists(backupFilePath))
                {
                    try
                    {
                        System.IO.File.Delete(backupFilePath);
                    }
                    catch
                    {
                        // 忽略刪除錯誤
                    }
                }

                // 使用統一方法處理例外
                return this.HandleAccuraException(null, ex);
            }
        }
    }
}
