using Accura_MES.Extensions;
using Accura_MES.Interfaces.Repositories;
using Accura_MES.Interfaces.Services;
using Accura_MES.Repositories;
using Accura_MES.Services;
using Accura_MES.Utilities;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace Accura_MES.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DataBaseInitializeController : ControllerBase
    {
        private XML xml = new();
        private readonly IWebHostEnvironmentService _webHostService;
        public DataBaseInitializeController(IWebHostEnvironmentService webHostService)
        {
            _webHostService = webHostService;
        }

        /// <summary>
        /// 初始化資料表[itemType]
        /// </summary>
        /// <returns></returns>
        [HttpPost("InitializeItemType")]
        public async Task<IActionResult> InitializeItemType()
        {
            try
            {
                #region 檢查
                if (!Request.Headers.ContainsKey("Database"))
                {
                    throw new CustomErrorCodeException(SelfErrorCode.MISSING_DATABASE);
                }

                string connectionString = new XML().GetConnection(Request.Headers["Database"].ToString());
                if (string.IsNullOrEmpty(connectionString))
                {
                    throw new CustomErrorCodeException(SelfErrorCode.NOT_FOUND_WITH_MSG, "SQL not exist");
                }
                #endregion

                IPropertyRepository propertyRepository = PropertyRepository.CreateRepository(connectionString);

                // 取得系統資料
                Dictionary<string, List<Dictionary<string, object>>> systemInfo = propertyRepository.GetDataBaseSystemInfo();

                // 初始化[itemType]
                await propertyRepository.InitializeItemType(systemInfo, 1);

                return this.SuccessAccuraResponse();
            }
            catch (Exception ex)
            {
                // 使用統一方法處理例外
                return await this.HandleAccuraExceptionAsync(null, ex);
            }
        }

        /// <summary>
        /// 初始化資料表[property]
        /// </summary>
        /// <returns></returns>
        [HttpPost("InitializeProperty")]
        public async Task<IActionResult> InitializeProperty()
        {
            try
            {
                #region 檢查
                if (!Request.Headers.ContainsKey("Database"))
                {
                    throw new CustomErrorCodeException(SelfErrorCode.MISSING_DATABASE);
                }

                string connectionString = new XML().GetConnection(Request.Headers["Database"].ToString());
                if (string.IsNullOrEmpty(connectionString))
                {
                    throw new CustomErrorCodeException(SelfErrorCode.NOT_FOUND_WITH_MSG, "SQL not exist");
                }
                #endregion

                IPropertyRepository propertyRepository = PropertyRepository.CreateRepository(connectionString);

                // 取得系統資料
                Dictionary<string, List<Dictionary<string, object>>> systemInfo = propertyRepository.GetDataBaseSystemInfo();

                await propertyRepository.InitializeProperty(systemInfo);

                return this.SuccessAccuraResponse();
            }
            catch (Exception ex)
            {
                // 使用統一方法處理例外
                return await this.HandleAccuraExceptionAsync(null, ex);
            }
        }

        /// <summary>
        /// 兩張表一起初始化，同時更新 property.datasource
        /// </summary>
        /// <returns></returns>
        [HttpPost("InitializeItemTypeAndProperty")]
        public async Task<IActionResult> InitializeItemTypeAndProperty()
        {
            try
            {

                #region 檢查
                if (!Request.Headers.ContainsKey("Database"))
                {
                    throw new CustomErrorCodeException(SelfErrorCode.MISSING_DATABASE);
                }

                string connectionString = new XML().GetConnection(Request.Headers["Database"].ToString());
                if (string.IsNullOrEmpty(connectionString))
                {
                    throw new CustomErrorCodeException(SelfErrorCode.NOT_FOUND_WITH_MSG, "SQL not exist");
                }
                #endregion

                IDataBaseInitializationService dataBaseInitializationService =
                    DataBaseInitializationService.CreateService(connectionString, _webHostService);

                return await dataBaseInitializationService.InitializeItemTypeAndPropertyAndDataSourceAsync(this);
            }
            catch (Exception ex)
            {
                return this.HandleAccuraException(null, ex);
            }
        }


        /// <summary>
        /// 依照propertyAndMenuList.json 更新[property].[dataSource]
        /// </summary>
        /// <returns></returns>
        [HttpPut("InitializeDataSource")]
        public async Task<IActionResult> UpdateDataSource()
        {
            // 獲取更新資訊
            try
            {
                #region 檢查
                if (!Request.Headers.ContainsKey("Database"))
                {
                    throw new CustomErrorCodeException(SelfErrorCode.MISSING_DATABASE);
                }

                string connectionString = new XML().GetConnection(Request.Headers["Database"].ToString());
                if (string.IsNullOrEmpty(connectionString))
                {
                    throw new CustomErrorCodeException(SelfErrorCode.NOT_FOUND_WITH_MSG, "SQL not exist");
                }
                #endregion

                IDataBaseInitializationService dataBaseInitializationService =
                    DataBaseInitializationService.CreateService(connectionString, _webHostService);

                await dataBaseInitializationService.UpdateDataSource();

                return this.SuccessAccuraResponse();
            }
            catch (Exception ex)
            {
                // 使用統一方法處理例外
                return await this.HandleAccuraExceptionAsync(null, ex);
            }
        }


        /// <summary>
        /// MenuListAndDropList.json 初始化[menulist] and [droplist]
        /// </summary>
        /// <returns></returns>
        /// <exception cref="CustomErrorCodeException"></exception>
        [HttpPut("InitializeMenuListAndDropList")]
        public async Task<IActionResult> InitializeMenuListAndDropList()
        {
            try
            {
                #region 檢查
                // 檢查Headers
                var result = UserController.CheckToken(Request);
                if (!result.Success)
                {
                    return StatusCode(int.Parse(result.Code.Split("-")[0]), result);
                }
                #endregion

                // 獲取 token
                var token = JwtService.AnalysisToken(HttpContext.Request.Headers["Authorization"]);

                // 取得connection
                string connectionString = xml.GetConnection(Request.Headers["Database"].ToString());

                IDataBaseInitializationService dataBaseInitializationService =
                    DataBaseInitializationService.CreateService(connectionString, _webHostService);

                var isSuccess = await dataBaseInitializationService.InitializeMenuListAndDropList();

                if (isSuccess)
                    return this.SuccessAccuraResponse();
                else
                    return this.CustomAccuraResponse(SelfErrorCode.INTERNAL_SERVER_ERROR, "發生未知錯誤，請後端工程師跨賣欸");
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
                return await this.HandleAccuraExceptionAsync(null, ex);
            }
        }

        /// <summary>
        /// MenuListAndDropList.json 初始化[menulist] and [droplist]
        /// </summary>
        /// <returns></returns>
        /// <exception cref="CustomErrorCodeException"></exception>
        /// <remarks>
        /// 使用 SqlBulkCopy 
        /// </remarks>
        [HttpPut("InitializeMenuListAndDropList2")]
        public async Task<IActionResult> InitializeMenuListAndDropList2()
        {
            try
            {
                // 取得connection
                string connectionString = xml.GetConnection(Request.Headers["Database"].ToString());

                IDataBaseInitializationService dataBaseInitializationService =
                    DataBaseInitializationService.CreateService(connectionString, _webHostService);

                var isSuccess = await dataBaseInitializationService.InitializeMenuListAndDropList_userTempTable();

                if (isSuccess)
                    return this.SuccessAccuraResponse();
                else
                    return this.CustomAccuraResponse(SelfErrorCode.INTERNAL_SERVER_ERROR, "發生未知錯誤，請後端工程師跨賣欸");
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
                return await this.HandleAccuraExceptionAsync(null, ex);
            }
        }

        [HttpPost("CreateDataBase")]
        public async Task<IActionResult> CreateDataBase()
        {
            try
            {
                #region 檢查
                if (!Request.Headers.ContainsKey("Database"))
                {
                    throw new CustomErrorCodeException(SelfErrorCode.MISSING_DATABASE);
                }

                string connectionString = new XML().GetConnection(Request.Headers["Database"].ToString());
                if (string.IsNullOrEmpty(connectionString))
                {
                    throw new CustomErrorCodeException(SelfErrorCode.NOT_FOUND_WITH_MSG, "SQL not exist");
                }
                #endregion

                IDataBaseInitializationService dataBaseInitializationService =
                    DataBaseInitializationService.CreateService(connectionString, _webHostService);

                string dataBaseName = "Accura_MESTEST";
                string filePath = $@"E:\SQL Server\MSSQL16.SQLEXPRESS\MSSQL\DATA\{dataBaseName}.mdf";
                string logFilePath = $@"E:\SQL Server\MSSQL16.SQLEXPRESS\MSSQL\DATA\{dataBaseName}.ldf";

                bool success = await dataBaseInitializationService.CreateDataBase(dataBaseName, filePath, logFilePath);

                if (success)
                {
                    return this.SuccessAccuraResponse();
                }
                else
                {
                    return this.CustomAccuraResponse(SelfErrorCode.INTERNAL_SERVER_ERROR);
                }
            }
            catch (Exception ex)
            {
                return this.HandleAccuraException(null, ex);
            }
        }


        [HttpPost("InitializeDataBase")]
        public async Task<IActionResult> InitializeDataBase()
        {
            //return this.CustomAccuraResponse(SelfErrorCode.INTERNAL_SERVER_ERROR,
            //    "確定要初始化資料庫? 此訊息是為了防止誤點初始化API，導致資料庫被多次初始化，確定要的話請把這個例外加上註解: 在 DataBaseInitializeController 就能找到了");

            try
            {
                #region 檢查
                if (!Request.Headers.ContainsKey("Database"))
                {
                    throw new CustomErrorCodeException(SelfErrorCode.MISSING_DATABASE);
                }

                string connectionString = new XML().GetConnection(Request.Headers["Database"].ToString());
                if (string.IsNullOrEmpty(connectionString))
                {
                    throw new CustomErrorCodeException(SelfErrorCode.NOT_FOUND_WITH_MSG, "SQL connection string not exist");
                }
                #endregion

                IDataBaseInitializationService dataBaseInitializationService =
                    DataBaseInitializationService.CreateService(connectionString, _webHostService);

                bool success = await dataBaseInitializationService.InitializeDataBase();

                if (success)
                {
                    return this.SuccessAccuraResponse();
                }
                else
                {
                    return this.CustomAccuraResponse(SelfErrorCode.INTERNAL_SERVER_ERROR, "發生未知錯誤，請後端工程師跨賣欸");
                }
            }
            catch (Exception ex)
            {
                return this.HandleAccuraException(null, ex);
            }
        }
    }
}