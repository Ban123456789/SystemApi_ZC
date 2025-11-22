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

    public class OrderController : ControllerBase
    {
        private XML _xml = new();

        /// <summary>
        /// 創建訂單
        /// </summary>
        /// <param name="input">訂單資料列表</param>
        /// <remarks>
        /// 自動生成編號：根據 shippedDate 分組，格式 01, 02, 03...
        /// </remarks>
        /// <returns>返回創建的訂單ID列表</returns>
        [HttpPost("Create")]
        public async Task<IActionResult> Create([FromBody] List<Dictionary<string, object?>> input)
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

                IOrderService orderService = OrderService.CreateService(connectionString);

                // 建立 [order]
                ResponseObject responseObject = await orderService.Create(user, input);

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
        /// 查詢訂單列表
        /// </summary>
        /// <param name="searchParams">查詢參數</param>
        /// <remarks>
        /// 查詢參數：
        /// - ids: 訂單ID數組 [1, 2, 3]
        /// - shippedDate: 出貨日期 "2025-10-15"
        /// </remarks>
        /// <returns>返回訂單列表</returns>
        [HttpPost("GetList")]
        public async Task<IActionResult> GetList([FromBody] Dictionary<string, object?> searchParams)
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

                IOrderService orderService = OrderService.CreateService(connectionString);

                // 查詢訂單列表
                ResponseObject responseObject = await orderService.GetList(searchParams);

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
        /// 刪除訂單
        /// </summary>
        /// <param name="request">刪除請求</param>
        /// <returns></returns>
        [HttpPost("DeleteOrders")]
        public async Task<IActionResult> DeleteOrders([FromBody] DeleteOrdersRequest request)
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

                if (request.orderIds == null || !request.orderIds.Any())
                {
                    return this.CustomAccuraResponse(SelfErrorCode.MISSING_PARAMETERS, null, null, "要刪除的訂單 ID 列表不能為空");
                }
                #endregion

                // 獲取 token
                var token = JwtService.AnalysisToken(HttpContext.Request.Headers["Authorization"]);
                long user = long.Parse(token["sub"]);

                IOrderService orderService = OrderService.CreateService(connectionString);

                // 刪除訂單
                ResponseObject responseObject = await orderService.DeleteOrders(user, request);

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
        /// 編輯訂單
        /// </summary>
        /// <param name="input">訂單資料（需包含 id）</param>
        /// <returns></returns>
        [HttpPost("UpdateOrder")]
        public async Task<IActionResult> UpdateOrder([FromBody] Dictionary<string, object?> input)
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
                if (input == null)
                {
                    return this.CustomAccuraResponse(SelfErrorCode.MISSING_PARAMETERS, null, null, "訂單資料不能為空");
                }

                if (!input.ContainsKey("id") || input["id"] == null)
                {
                    return this.CustomAccuraResponse(SelfErrorCode.MISSING_PARAMETERS, null, null, "訂單資料必須包含 id");
                }
                #endregion

                // 獲取 token
                var token = JwtService.AnalysisToken(HttpContext.Request.Headers["Authorization"]);
                long user = long.Parse(token["sub"]);

                IOrderService orderService = OrderService.CreateService(connectionString);

                // 編輯訂單
                ResponseObject responseObject = await orderService.UpdateOrder(user, input);

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

