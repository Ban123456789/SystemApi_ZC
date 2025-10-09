using Accura_MES.Extensions;
using Accura_MES.Interfaces.Repositories;
using Microsoft.Data.SqlClient;
using System.Diagnostics;

namespace Accura_MES.Repositories
{
    public class PreferencesRepository : IPreferencesRepository
    {
        private XML xml;

        private PreferencesRepository()
        {
            xml = new XML();
        }

        /// <summary>
        /// 靜態工廠方法
        /// </summary>
        /// <returns></returns>
        /// <exception cref="CustomErrorCodeException"></exception>
        public static PreferencesRepository CreateRepository()
        {
            // 返回此物件
            return new PreferencesRepository();
        }


        public async Task<Dictionary<string, Dictionary<string, string>>> GetSyncTaskForEachDataBase()
        {
            var syncTasksForEachDataBase = new Dictionary<string, Dictionary<string, string>>();

            try
            {
                var dataBases = xml.GetAllConnection();

                // 遍歷所有資料庫
                for (int i = 0; i < dataBases.Count; i++)
                {
                    // 記錄這個資料庫的所有任務資訊
                    var syncTasks = new Dictionary<string, string>();

                    string connectionString = xml.GetConnection(dataBases[i]);

                    string query = $"SELECT * FROM [preferences] WHERE [name] in (" +
                        $"'syncStockFromErp01'," +
                        $"'syncStockFromErp02'," +
                        $"'syncOtherFromErp') " +
                        $"AND enable = 1 AND isDelete = 0";

                    using (SqlConnection connection = new SqlConnection(connectionString))
                    {
                        // 打開連接
                        await connection.OpenAsync();
                        using (SqlCommand command = new SqlCommand(query, connection))
                        {
                            using (SqlDataReader reader = await command.ExecuteReaderAsync())
                            {
                                // 讀取結果
                                while (reader.Read())
                                {
                                    syncTasks[reader["name"]?.ToString()] = reader["value"]?.ToString();
                                }

                            }
                        }
                    }

                    syncTasksForEachDataBase[dataBases[i]] = syncTasks;
                }

                return syncTasksForEachDataBase;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());

                throw;
            }
        }

        public async Task<ResponseObject> GetCNCAutoShiftReportSettingEachDataBase()
        {
            ResponseObject responseObject = new ResponseObject().GenerateEntity(SelfErrorCode.INTERNAL_SERVER_WITH_MSG,
                null, null, "取得自動換班排程偏好設定發生未知錯誤");

            var result = new Dictionary<string, Dictionary<string, string>>();

            try
            {
                var dataBases = xml.GetAllConnection();

                // 測試用: 先不要用所有資料庫，只用 66 的 TC測試資料庫
                //dataBases = new List<string> { "TC_UNITEN_MES" };

                // 遍歷所有資料庫
                for (int i = 0; i < dataBases.Count; i++)
                {
                    // 記錄這個資料庫的所有任務資訊
                    var shiftTasks = new Dictionary<string, string>();

                    string connectionString = xml.GetConnection(dataBases[i]);

                    string query = $"SELECT * FROM [preferences] WHERE [name] in (" +
                        $"'CNCAutoShiftReport')" +
                        $"AND enable = 1 AND isDelete = 0";

                    using (SqlConnection connection = new SqlConnection(connectionString))
                    {
                        // 打開連接
                        await connection.OpenAsync();
                        using (SqlCommand command = new SqlCommand(query, connection))
                        {
                            using (SqlDataReader reader = await command.ExecuteReaderAsync())
                            {
                                // 讀取結果
                                while (reader.Read())
                                {
                                    shiftTasks[reader["name"]?.ToString() ?? ""] = reader["value"]?.ToString() ?? "";
                                }

                            }
                        }
                    }

                    result[dataBases[i]] = shiftTasks;
                }


                // 成功返回
                responseObject.SetErrorCode(SelfErrorCode.SUCCESS);
                responseObject.Data = result;
                return responseObject;
            }
            catch (Exception ex)
            {
                return await this.HandleExceptionAsync(ex);
            }
        }
    }
}
