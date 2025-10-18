using Accura_MES.Extensions;
using Accura_MES.Interfaces.Services;
using Accura_MES.Services;
using Microsoft.AspNetCore.Mvc;

namespace Accura_MES.Controllers
{

    [Route("api/[controller]")]
    [ApiController]

    public class ShippingOrderController : ControllerBase
    {
        private XML _xml = new();

        /// <summary>
        /// 創建出貨單
        /// </summary>
        /// <param name="input">出貨單資料列表</param>
        /// <remarks>
        /// 自動生成編號：根據 orderId -> order.shippedDate 分組，格式 0001, 0002, 0003...
        /// </remarks>
        /// <returns>返回創建的出貨單ID列表</returns>
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

                IShippingOrderService shippingOrderService = ShippingOrderService.CreateService(connectionString);

                // 建立 [shippingOrder]
                ResponseObject responseObject = await shippingOrderService.Create(user, input);

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
        /// todo 查詢出貨單列表
        /// </summary>
        /// <param name="searchParams">查詢參數</param>
        /// <remarks>
        /// 查詢參數：
        /// - ids: 出貨單ID數組 [1, 2, 3]
        /// - orderId: 訂單ID
        /// </remarks>
        /// <returns>返回出貨單列表</returns>
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

                IShippingOrderService shippingOrderService = ShippingOrderService.CreateService(connectionString);

                // 查詢出貨單列表
                ResponseObject responseObject = await shippingOrderService.GetList(searchParams);

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

