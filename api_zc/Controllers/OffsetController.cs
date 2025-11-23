using Accura_MES.Extensions;
using Accura_MES.Interfaces.Services;
using Accura_MES.Models;
using Accura_MES.Services;
using Accura_MES.Utilities;
using Microsoft.AspNetCore.Mvc;

namespace Accura_MES.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OffsetController : ControllerBase
    {
        private XML _xml = new();

        /// <summary>
        /// 取得未沖帳清單
        /// </summary>
        /// <param name="request">查詢請求</param>
        /// <returns></returns>
        [HttpPost("GetUnOffsetList")]
        public async Task<IActionResult> GetUnOffsetList([FromBody] GetUnOffsetListRequest request)
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

                // 檢查 Body
                if (request == null)
                {
                    return this.CustomAccuraResponse(SelfErrorCode.MISSING_PARAMETERS, null, null, "查詢請求不能為空");
                }
                #endregion

                IOffsetService offsetService = OffsetService.CreateService(connectionString);

                // 取得未沖帳清單
                ResponseObject responseObject = await offsetService.GetUnOffsetList(request);

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
        /// 沖帳
        /// </summary>
        /// <param name="requests">沖帳請求列表</param>
        /// <returns></returns>
        [HttpPost("Offset")]
        public async Task<IActionResult> Offset([FromBody] List<OffsetRequest> requests)
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

                // 檢查 Body
                if (requests == null || !requests.Any())
                {
                    return this.CustomAccuraResponse(SelfErrorCode.MISSING_PARAMETERS, null, null, "沖帳請求不能為空");
                }
                #endregion

                // 獲取 token
                var token = JwtService.AnalysisToken(HttpContext.Request.Headers["Authorization"]);
                long user = long.Parse(token["sub"]);

                IOffsetService offsetService = OffsetService.CreateService(connectionString);

                // 執行沖帳
                ResponseObject responseObject = await offsetService.Offset(user, requests);

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
        /// 取得沖帳紀錄
        /// </summary>
        /// <param name="request">查詢請求</param>
        /// <returns></returns>
        [HttpPost("GetOffsetRecords")]
        public async Task<IActionResult> GetOffsetRecords([FromBody] GetOffsetRecordsRequest request)
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

                // 檢查 Body
                if (request == null)
                {
                    return this.CustomAccuraResponse(SelfErrorCode.MISSING_PARAMETERS, null, null, "查詢請求不能為空");
                }
                #endregion

                IOffsetService offsetService = OffsetService.CreateService(connectionString);

                // 取得沖帳紀錄
                ResponseObject responseObject = await offsetService.GetOffsetRecords(request);

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
        /// 反沖銷
        /// </summary>
        /// <param name="request">反沖銷請求</param>
        /// <returns></returns>
        [HttpPost("RollbackOffset")]
        public async Task<IActionResult> RollbackOffset([FromBody] RollbackOffsetRequest request)
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

                // 檢查 Body
                if (request == null)
                {
                    return this.CustomAccuraResponse(SelfErrorCode.MISSING_PARAMETERS, null, null, "反沖銷請求不能為空");
                }
                #endregion

                // 獲取 token
                var token = JwtService.AnalysisToken(HttpContext.Request.Headers["Authorization"]);
                long user = long.Parse(token["sub"]);

                IOffsetService offsetService = OffsetService.CreateService(connectionString);

                // 執行反沖銷
                ResponseObject responseObject = await offsetService.RollbackOffset(user, request);

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
        /// 取得客戶結餘
        /// </summary>
        /// <param name="request">查詢請求</param>
        /// <returns></returns>
        [HttpPost("GetCustomerBalance")]
        public async Task<IActionResult> GetCustomerBalance([FromBody] GetCustomerBalanceRequest request)
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

                // 檢查 Body
                if (request == null)
                {
                    return this.CustomAccuraResponse(SelfErrorCode.MISSING_PARAMETERS, null, null, "查詢請求不能為空");
                }
                #endregion

                IOffsetService offsetService = OffsetService.CreateService(connectionString);

                // 取得客戶結餘
                ResponseObject responseObject = await offsetService.GetCustomerBalance(request);

                // 直接返回
                return this.CustomAccuraResponse(responseObject);
            }
            catch (Exception ex)
            {
                // 使用統一方法處理例外
                return this.HandleAccuraException(null, ex);
            }
        }
    }
}

