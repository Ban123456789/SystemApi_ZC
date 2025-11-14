using Accura_MES.Extensions;
using Accura_MES.Interfaces.Services;
using Accura_MES.Models;
using Accura_MES.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace Accura_MES.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CustomerPriceController : ControllerBase
    {
        private XML _xml = new();

        /// <summary>
        /// 建立客戶價格資料
        /// </summary>
        /// <param name="customerPriceData">客戶價格資料</param>
        /// <returns></returns>
        [HttpPost("Create")]
        public async Task<IActionResult> Create([FromBody] List<Dictionary<string, object?>> customerPriceData)
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

                // check Body
                if (customerPriceData == null || !customerPriceData.Any())
                {
                    return this.CustomAccuraResponse(SelfErrorCode.MISSING_PARAMETERS, null, null, "客戶價格資料不能為空");
                }
                #endregion

                // 獲取 token
                var token = JwtService.AnalysisToken(HttpContext.Request.Headers["Authorization"]);
                long user = long.Parse(token["sub"]);

                ICustomerPriceService customerPriceService = CustomerPriceService.CreateService(connectionString);

                // 建立客戶價格資料
                ResponseObject responseObject = await customerPriceService.Create(user, customerPriceData);

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
        /// 取得客戶別簡表
        /// </summary>
        /// <param name="request">查詢請求</param>
        /// <returns></returns>
        [HttpPost("GetAccountingByCustomer")]
        public async Task<IActionResult> GetAccountingByCustomer([FromBody] GetAccountingByCustomerRequest request)
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

                ICustomerPriceService customerPriceService = CustomerPriceService.CreateService(connectionString);

                // 取得客戶別簡表
                ResponseObject responseObject = await customerPriceService.GetAccountingByCustomer(request);

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
        /// 出貨單轉應收帳款
        /// </summary>
        /// <param name="request">轉換請求</param>
        /// <returns></returns>
        [HttpPost("ConverToReceivable")]
        public async Task<IActionResult> ConverToReceivable([FromBody] ConverToReceivableRequest request)
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
                    return this.CustomAccuraResponse(SelfErrorCode.MISSING_PARAMETERS, null, null, "轉換請求不能為空");
                }
                #endregion

                // 獲取 token
                var token = JwtService.AnalysisToken(HttpContext.Request.Headers["Authorization"]);
                long user = long.Parse(token["sub"]);

                ICustomerPriceService customerPriceService = CustomerPriceService.CreateService(connectionString);

                // 出貨單轉應收帳款
                ResponseObject responseObject = await customerPriceService.ConverToReceivable(request, user);

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
        /// 應收帳款退回
        /// </summary>
        /// <param name="request">退回請求</param>
        /// <returns></returns>
        [HttpPost("RollbackReceivable")]
        public async Task<IActionResult> RollbackReceivable([FromBody] RollbackReceivableRequest request)
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
                    return this.CustomAccuraResponse(SelfErrorCode.MISSING_PARAMETERS, null, null, "退回請求不能為空");
                }
                #endregion

                // 獲取 token
                var token = JwtService.AnalysisToken(HttpContext.Request.Headers["Authorization"]);
                long user = long.Parse(token["sub"]);

                ICustomerPriceService customerPriceService = CustomerPriceService.CreateService(connectionString);

                // 應收帳款退回
                ResponseObject responseObject = await customerPriceService.RollbackReceivable(request, user);

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
        /// 建立應收帳款
        /// </summary>
        /// <param name="request">建立應收帳款請求</param>
        /// <returns></returns>
        [HttpPost("CreateReceivable")]
        public async Task<IActionResult> CreateReceivable([FromBody] CreateReceivableRequest request)
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
                    return this.CustomAccuraResponse(SelfErrorCode.MISSING_PARAMETERS, null, null, "建立應收帳款請求不能為空");
                }
                #endregion

                // 獲取 token
                var token = JwtService.AnalysisToken(HttpContext.Request.Headers["Authorization"]);
                long user = long.Parse(token["sub"]);

                ICustomerPriceService customerPriceService = CustomerPriceService.CreateService(connectionString);

                // 建立應收帳款
                ResponseObject responseObject = await customerPriceService.CreateReceivable(request, user);

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
        /// 取得應收帳款清單
        /// </summary>
        /// <param name="request">查詢請求</param>
        /// <returns></returns>
        [HttpPost("GetReceivableList")]
        public async Task<IActionResult> GetReceivableList([FromBody] GetReceivableListRequest request)
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

                ICustomerPriceService customerPriceService = CustomerPriceService.CreateService(connectionString);

                // 取得應收帳款清單
                ResponseObject responseObject = await customerPriceService.GetReceivableList(request);

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
        /// 刪除應收帳款
        /// </summary>
        /// <param name="request">刪除請求</param>
        /// <returns></returns>
        [HttpPost("DeleteReceivable")]
        public async Task<IActionResult> DeleteReceivable([FromBody] DeleteReceivableRequest request)
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
                    return this.CustomAccuraResponse(SelfErrorCode.MISSING_PARAMETERS, null, null, "刪除請求不能為空");
                }
                #endregion

                // 獲取 token
                var token = JwtService.AnalysisToken(HttpContext.Request.Headers["Authorization"]);
                long user = long.Parse(token["sub"]);

                ICustomerPriceService customerPriceService = CustomerPriceService.CreateService(connectionString);

                // 刪除應收帳款
                ResponseObject responseObject = await customerPriceService.DeleteReceivable(request, user);

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
