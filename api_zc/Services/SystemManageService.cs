using Accura_MES.Extensions;
using Accura_MES.Interfaces.Repositories;
using Accura_MES.Interfaces.Services;
using Accura_MES.Models;
using Accura_MES.Repositories;
using Accura_MES.Utilities;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Diagnostics;

namespace Accura_MES.Services
{
    public class SystemManageService : ISystemManageService
    {
        private string _connectionString;
        private IGenericRepository _genericRepository;

        private SystemManageService(string connectionString, IGenericRepository genericRepository)
        {
            _connectionString = connectionString;
            _genericRepository = genericRepository;
        }

        /// <summary>
        /// 靜態工廠方法，用於創建 SystemManageService 實例，並接收外部資料庫連線
        /// </summary>
        /// <param name="connection"></param>
        /// <returns>SystemManageService 實例</returns>
        /// <exception cref="CustomErrorCodeException"></exception>
        public static SystemManageService CreateService(string connectionString)
        {
            // 建立通用 repository
            IGenericRepository genericRepository = GenericRepository.CreateRepository(connectionString, null);

            // 接收外部資料庫連線
            return new SystemManageService(connectionString, genericRepository);
        }

        public async Task<ResponseObject> TableSetting(long user, List<Dictionary<string, object?>> new_tableColumnSettingObject)
        {
            ResponseObject responseObject = new ResponseObject().GenerateEntity(
                SelfErrorCode.INTERNAL_SERVER_WITH_MSG, null, null, "刷新tableColumnSetting發生未知錯誤");

            SqlTransaction? transaction = null;

            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                // 取得第一筆 tableColumnSettingObject 的 userId, tableName
                var firstItem = new_tableColumnSettingObject.FirstOrDefault();
                if (firstItem == null)
                {
                    // 如果沒有資料，則直接返回成功
                    responseObject.SetErrorCode(SelfErrorCode.SUCCESS);
                    return responseObject;
                }
                var userId = firstItem["userId"];
                var tableName = firstItem["tableName"];

                // 用 userId, tableName 取得舊資料
                List<Dictionary<string, object>> old_tableColumnSettingObject = _genericRepository.GenericAdvancedGetNotDelete(
                    connection,
                    transaction,
                    new AdvancedSearch
                    {
                        Datasheet = "tableColumnSetting",
                        And = new List<QueryObject>
                        {
                            new QueryObject
                            {
                                Field = "userId",
                                Operate = "=",
                                Value = userId.ToString()
                            },
                            new QueryObject
                            {
                                Field = "tableName",
                                Operate = "=",
                                Value = tableName.ToString()
                            }
                        }
                    });

                // 用 columnName 比對新舊資料: 執行 UPSERT + DELETE
                List<Dictionary<string, object?>> updateObjs = new();
                List<Dictionary<string, object?>> insertObjs = new();
                List<long> deleteObjs = new();

                foreach (var newItem in new_tableColumnSettingObject)
                {
                    var columnName = newItem["columnName"];
                    var sequence = newItem["sequence"];
                    var isShow = newItem["isShow"];
                    // 找到舊資料
                    var oldItem = old_tableColumnSettingObject.FirstOrDefault(x => x["columnName"].ToString() == columnName.ToString());
                    if (oldItem != null)
                    {
                        // 更新舊資料
                        newItem["id"] = oldItem["id"];  // 設定 id
                        updateObjs.Add(newItem);
                    }
                    else
                    {
                        // 新增新資料
                        //Dictionary<string, object?> insertObj = new()
                        //{
                        //    { "userId", userId },
                        //    { "tableName", tableName },
                        //    { "columnName", columnName },
                        //    { "sequence", sequence },
                        //    { "isShow", isShow }
                        //};
                        insertObjs.Add(newItem);
                    }
                }

                // 找到舊資料中不在新資料中的項目，標記為刪除
                foreach (var oldItem in old_tableColumnSettingObject)
                {
                    var columnName = oldItem["columnName"];
                    var newItem = new_tableColumnSettingObject.FirstOrDefault(x => x["columnName"].ToString() == columnName.ToString());
                    if (newItem == null)
                    {
                        // 標記為刪除
                        deleteObjs.Add(Convert.ToInt64(oldItem["id"]));
                    }
                }

                // 執行更新、插入和刪除操作
                transaction = connection.BeginTransaction();    // 開啟交易

                if (updateObjs.Count > 0)
                {
                    await _genericRepository.GenericUpdate(
                        user,
                        new TableDatas
                        {
                            Datasheet = "tableColumnSetting",
                            DataStructure = updateObjs
                        },
                        connection,
                        transaction);
                }
                if (insertObjs.Count > 0)
                {
                    await _genericRepository.CreateDataGeneric(
                        connection,
                        transaction,
                        user,
                        "tableColumnSetting",
                        insertObjs.First().Select(x => x.Key).ToHashSet(),
                        insertObjs);
                }
                if (deleteObjs.Count > 0)
                {
                    await _genericRepository.Delete(
                        connection,
                        transaction,
                        user,
                        "tableColumnSetting",
                        deleteObjs);
                }


                // commit transaction
                await transaction.CommitAsync();

                // 成功返回
                responseObject.SetErrorCode(SelfErrorCode.SUCCESS);
                return responseObject;
            }
            catch (Exception ex)
            {
                return await this.HandleExceptionAsync(ex, transaction);
            }
            finally
            {
                SqlHelper.RollbackAndDisposeTransaction(transaction);
            }
        }

        /// <summary>
        /// 備份資料庫
        /// </summary>
        /// <param name="backupPath">備份文件存儲路徑（相對路徑）</param>
        /// <param name="commandTimeout">SQL 命令超時時間（秒）</param>
        /// <param name="retentionCount">保留備份檔案數量（<=0 表示不清理）</param>
        /// <returns>備份文件的路徑</returns>
        public async Task<string> BackupSQL(string backupPath, int commandTimeout, int retentionCount)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                // 從連接字符串中提取數據庫名稱
                var builder = new SqlConnectionStringBuilder(_connectionString);
                string databaseName = builder.InitialCatalog;
                
                if (string.IsNullOrEmpty(databaseName))
                {
                    throw new CustomErrorCodeException(
                        SelfErrorCode.BAD_REQUEST,
                        "無法從連接字符串中獲取數據庫名稱"
                    );
                }

                // 創建備份目錄（支持相對路徑和絕對路徑）
                string backupDirectory;
                if (Path.IsPathRooted(backupPath))
                {
                    // 絕對路徑
                    backupDirectory = backupPath;
                }
                else
                {
                    // 相對路徑，相對於應用程序根目錄
                    backupDirectory = Path.Combine(Directory.GetCurrentDirectory(), backupPath);
                }

                if (!Directory.Exists(backupDirectory))
                {
                    Directory.CreateDirectory(backupDirectory);
                }

                // 先做備份檔案數量控管（超過就刪最舊的）
                CleanupOldBackups(backupDirectory, retentionCount);

                // 生成備份文件名（格式：數據庫名_日期時間.bak）
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string backupFileName = $"{databaseName}_{timestamp}.bak";
                string backupFilePath = Path.Combine(backupDirectory, backupFileName);

                // 執行備份命令
                // 轉義路徑中的單引號（SQL Server 需要）
                string escapedPath = backupFilePath.Replace("'", "''");
                string backupSql = $@"
                    BACKUP DATABASE [{databaseName}]
                    TO DISK = '{escapedPath}'
                    WITH FORMAT, INIT, NAME = 'Full Backup of {databaseName}', 
                    SKIP, NOREWIND, NOUNLOAD, STATS = 10";

                Debug.WriteLine($"[SystemManageService] 開始備份數據庫: {databaseName}");
                Debug.WriteLine($"[SystemManageService] 備份路徑: {backupFilePath}");
                Debug.WriteLine($"[SystemManageService] SQL 命令: {backupSql}");

                using var command = new SqlCommand(backupSql, connection);
                command.CommandTimeout = commandTimeout; // 使用配置的超時時間

                try
                {
                    await command.ExecuteNonQueryAsync();
                    Debug.WriteLine($"[SystemManageService] SQL 備份命令執行完成");
                }
                catch (SqlException sqlEx)
                {
                    Debug.WriteLine($"[SystemManageService] SQL 備份命令執行失敗: {sqlEx.Message}");
                    Debug.WriteLine($"[SystemManageService] SQL 錯誤編號: {sqlEx.Number}");
                    throw new CustomErrorCodeException(
                        SelfErrorCode.INTERNAL_SERVER_ERROR,
                        $"SQL Server 備份失敗: {sqlEx.Message} (錯誤編號: {sqlEx.Number})",
                        sqlEx
                    );
                }

                // 等待一小段時間，確保文件系統已更新
                await Task.Delay(1000);

                // 檢查備份文件是否存在
                if (!System.IO.File.Exists(backupFilePath))
                {
                    throw new CustomErrorCodeException(
                        SelfErrorCode.INTERNAL_SERVER_ERROR,
                        $"備份文件創建失敗，文件路徑: {backupFilePath}"
                    );
                }

                // 檢查文件大小，確保不是空文件
                var fileInfo = new System.IO.FileInfo(backupFilePath);
                if (fileInfo.Length == 0)
                {
                    throw new CustomErrorCodeException(
                        SelfErrorCode.INTERNAL_SERVER_ERROR,
                        $"備份文件為空，文件路徑: {backupFilePath}"
                    );
                }

                Debug.WriteLine($"[SystemManageService] 備份文件驗證成功，文件大小: {fileInfo.Length} bytes");

                return backupFilePath;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SystemManageService] 數據庫備份失敗: {ex.Message}");
                Debug.WriteLine($"[SystemManageService] 異常堆疊: {ex.StackTrace}");
                
                // 如果是自定義異常，直接拋出
                if (ex is CustomErrorCodeException)
                {
                    throw;
                }
                
                // 其他異常包裝為自定義異常，確保錯誤信息能傳遞到前端
                throw new CustomErrorCodeException(
                    SelfErrorCode.INTERNAL_SERVER_ERROR,
                    $"數據庫備份失敗: {ex.Message}",
                    ex
                );
            }
        }

        /// <summary>
        /// 控管備份檔案數量：若超過 retentionCount，依建立時間優先刪除最早的
        /// </summary>
        private void CleanupOldBackups(string backupDirectory, int retentionCount)
        {
            // retentionCount <= 0 表示不啟用清理
            if (retentionCount <= 0)
            {
                return;
            }

            try
            {
                var dirInfo = new DirectoryInfo(backupDirectory);
                if (!dirInfo.Exists)
                {
                    return;
                }

                // 只針對 .bak 檔案
                var files = dirInfo.GetFiles("*.bak", SearchOption.TopDirectoryOnly)
                    // 依建立時間（CreationTimeUtc）由舊到新排序
                    .OrderBy(f => f.CreationTimeUtc)
                    .ToList();

                // 若已經是 retentionCount 個(含)以上，刪到剩 retentionCount - 1 個（讓本次新增後不超過 retentionCount）
                while (files.Count >= retentionCount)
                {
                    var toDelete = files[0];
                    files.RemoveAt(0);

                    try
                    {
                        Debug.WriteLine($"[SystemManageService] 超過保留數({retentionCount})，刪除最早備份: {toDelete.FullName}");
                        toDelete.Delete();
                    }
                    catch (Exception deleteEx)
                    {
                        // 刪除失敗就中止，避免無限迴圈；備份仍可繼續嘗試
                        Debug.WriteLine($"[SystemManageService] 刪除舊備份失敗: {deleteEx.Message}，檔案: {toDelete.FullName}");
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                // 清理失敗不影響備份主流程
                Debug.WriteLine($"[SystemManageService] 清理舊備份失敗: {ex.Message}");
            }
        }
    }
}
