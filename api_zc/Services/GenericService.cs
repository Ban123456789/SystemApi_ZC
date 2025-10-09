using Accura_MES.Extensions;
using Accura_MES.Interfaces.Repositories;
using Accura_MES.Interfaces.Services;
using Accura_MES.Models;
using Accura_MES.Repositories;
using Microsoft.Data.SqlClient;
using System.Data;

namespace Accura_MES.Services
{
    public class GenericService : IGenericService
    {
        private readonly string _connectionString;
        private readonly IGenericRepository _genericRepository;

        private GenericService(string connectionString)
        {
            _connectionString = connectionString;
            _genericRepository = GenericRepository.CreateRepository(_connectionString, null);
        }

        /// <summary>
        /// 靜態工廠方法，用於創建 GeneticService 實例，並初始化資料庫連線
        /// </summary>
        /// <param name="connectionString"></param>
        /// <returns>GeneticService 實例</returns>                                                  
        /// <exception cref="CustomErrorCodeException"></exception>
        public static GenericService CreateService(string connectionString)
        {
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new CustomErrorCodeException(SelfErrorCode.NOT_FOUND_WITH_MSG, "SQL connection string not exist");
            }

            return new GenericService(connectionString);
        }



        public async Task<int> IsDelete(long user, TableDatas shareInfo)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                return await _genericRepository.IsDelete(connection, null, user, shareInfo);
            }
            catch
            {
                throw;
            }
        }

        public async Task<Dictionary<string, object?>> GetOrCreate(SqlConnection connection, SqlTransaction? transaction,
            string tableName, HashSet<string> primaryKeys, Dictionary<string, object?> input, long user)
        {
            Dictionary<string, object?> result = new Dictionary<string, object?>();
            try
            {
                // create AdvancedSearch object
                var advancedSearch = new AdvancedSearch()
                {
                    Datasheet = tableName,
                    Localization = false,
                    And = null
                };

                // 填入查詢條件與值
                foreach (var key in primaryKeys)
                {
                    var and = new QueryObject()
                    {
                        Field = key,
                        Operate = "=",
                        Value = input[key]?.ToString() ?? throw new ArgumentException($"primaryKeys : [{key}] 的值不得為空")
                    };


                    if (advancedSearch.And is null)
                    {
                        advancedSearch.And = new() { and };
                    }
                    else
                    {
                        advancedSearch.And.Add(and);
                    }
                }

                // 查詢資料
                var genericCatcher1 = _genericRepository.GenericAdvancedGetNotDelete(connection, transaction, advancedSearch);

                // 查到資料
                if (genericCatcher1 is not null && genericCatcher1.Any())
                {
                    result = genericCatcher1.First();
                }

                // 若查無資料，建立資料
                else
                {
                    var genericCatcher2 = await _genericRepository.CreateDataGeneric(connection, transaction, user, tableName, new() { input });

                    // 查詢資料
                    var genericCatcher3 = _genericRepository.GenericAdvancedGetNotDelete(connection, transaction, advancedSearch);

                    result = genericCatcher3?.FirstOrDefault() ?? new ();

                }

                return result;
            }
            catch
            {
                throw;
            }
        }

        public async Task<ResponseObject> Upsert_Delete_SqlBulkCopy(
            string tableName,
            DataTable dataTable,
            HashSet<string> primaryKeys)
        {
            ResponseObject responseObject = new ResponseObject().GenerateEntity(SelfErrorCode.INTERNAL_SERVER_WITH_MSG,
                null, null, "通用批次建立&編輯&刪除發生未知錯誤");

            SqlTransaction? transaction = null;

            IGenericRepository genericRepository = GenericRepository.CreateRepository(_connectionString, null);

            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                await genericRepository.Upsert_Delete_SqlBulkCopy(
                    tableName,
                    dataTable,
                    primaryKeys,
                    connection,
                    transaction);

                // 成功返回
                responseObject.SetErrorCode(SelfErrorCode.SUCCESS);
                return responseObject;
            }
            catch (Exception ex)
            {
                return await this.HandleExceptionAsync(ex, transaction);
            }
        }
    }
}
