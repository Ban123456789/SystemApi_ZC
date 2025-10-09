using Accura_MES.Extensions;
using Accura_MES.Interfaces.Repositories;
using Accura_MES.Interfaces.Services;
using Accura_MES.Models;
using Accura_MES.Repositories;
using Accura_MES.Utilities;
using Microsoft.Data.SqlClient;
using System.Data;

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
    }
}
