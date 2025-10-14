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
    public class ProjectService : IProjectService
    {
        private string _connectionString;
        private IGenericRepository _genericRepository;
        private ISequenceService _sequenceService;

        private ProjectService(string connectionString, IGenericRepository genericRepository, ISequenceService sequenceService)
        {
            _connectionString = connectionString;
            _genericRepository = genericRepository;
            _sequenceService = sequenceService;
        }

        /// <summary>
        /// 靜態工廠方法，用於創建 ProjectService 實例，並接收外部資料庫連線
        /// </summary>
        /// <param name="connectionString">資料庫連線字串</param>
        /// <returns>ProjectService 實例</returns>
        /// <exception cref="CustomErrorCodeException"></exception>
        public static ProjectService CreateService(string connectionString)
        {
            // 建立通用 repository
            IGenericRepository genericRepository = GenericRepository.CreateRepository(connectionString, null);
            
            // 建立序列號服務
            ISequenceService sequenceService = SequenceService.CreateService(connectionString);

            // 接收外部資料庫連線
            return new ProjectService(connectionString, genericRepository, sequenceService);
        }

        /// <summary>
        /// 創建工程項目
        /// </summary>
        /// <param name="userId">用戶ID</param>
        /// <param name="projectObject">工程項目資料列表</param>
        /// <returns>響應對象，包含創建的項目ID列表</returns>
        public async Task<ResponseObject> Create(long userId, List<Dictionary<string, object?>> projectObject)
        {
            ResponseObject responseObject = new ResponseObject().GenerateEntity(SelfErrorCode.SUCCESS);
            SqlTransaction? transaction = null;

            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();
                transaction = connection.BeginTransaction();

                // 為每個項目生成編號
                await GenerateProjectNumbers(connection, transaction, projectObject);

                // 插入項目數據
                var insertedIds = await _genericRepository.CreateDataGeneric(
                    connection, 
                    transaction, 
                    userId, 
                    "Project", 
                    projectObject
                );

                // 提交事務
                await transaction.CommitAsync();

                Debug.WriteLine($"[ProjectService] 成功創建 {insertedIds.Count} 個項目");

                // 成功返回，直接返回ID列表
                responseObject.SetErrorCode(SelfErrorCode.SUCCESS);
                responseObject.Data = insertedIds;
                return responseObject;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ProjectService] 創建項目失敗: {ex.Message}");
                return await this.HandleExceptionAsync(ex, transaction);
            }
            finally
            {
                SqlHelper.RollbackAndDisposeTransaction(transaction);
            }
        }

        /// <summary>
        /// 為項目生成編號
        /// 規則：根據 customerId 分組，格式 0001
        /// </summary>
        private async Task GenerateProjectNumbers(
            SqlConnection connection, 
            SqlTransaction transaction, 
            List<Dictionary<string, object?>> projectObject)
        {
            foreach (var project in projectObject)
            {
                // 獲取客戶ID作為分組鍵
                var customerId = project["customerId"]?.ToString();
                
                if (string.IsNullOrEmpty(customerId))
                {
                    throw new CustomErrorCodeException(
                        SelfErrorCode.BAD_REQUEST, 
                        "customerId 不能為空"
                    );
                }

                // 生成工程代碼
                string projectNumber = await _sequenceService.GetNextNumberAsync(
                    connection,
                    transaction,
                    tableName: "project",
                    groupKey: customerId,
                    numberFormat: "0000"
                );

                // 將生成的編號寫入數據
                project["number"] = projectNumber;

                Debug.WriteLine($"[ProjectService] 為客戶 {customerId} 生成工程代碼: {projectNumber}");
            }
        }
    }
}
