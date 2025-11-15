using Accura_MES.Extensions;
using Accura_MES.Interfaces.Services;
using Accura_MES.Models;
using Accura_MES.Services;
using Microsoft.AspNetCore.Mvc;

namespace Accura_MES.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReceiptController : ControllerBase
    {
        private XML _xml = new();

        /// <summary>
        /// 建立收款單
        /// </summary>
        /// <param name="input">收款單資料列表</param>
        /// <remarks>
        /// 自動生成編號：根據 receiptDate 分組，格式 0001, 0002, 0003...
        /// </remarks>
        /// <returns>返回創建的收款單ID列表</returns>
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

                IReceiptService receiptService = ReceiptService.CreateService(connectionString);

                // 建立收款單
                ResponseObject responseObject = await receiptService.Create(user, input);

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
        /// 取得收款單清單
        /// </summary>
        /// <param name="request">查詢請求</param>
        /// <returns></returns>
        [HttpPost("GetReceiptList")]
        public async Task<IActionResult> GetReceiptList([FromBody] GetReceiptListRequest request)
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

                IReceiptService receiptService = ReceiptService.CreateService(connectionString);

                // 取得收款單清單
                ResponseObject responseObject = await receiptService.GetReceiptList(request);

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
        /// 刪除收款單
        /// </summary>
        /// <param name="request">刪除請求</param>
        /// <returns></returns>
        [HttpPost("DeleteReceipts")]
        public async Task<IActionResult> DeleteReceipts([FromBody] DeleteReceiptsRequest request)
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

                IReceiptService receiptService = ReceiptService.CreateService(connectionString);

                // 刪除收款單
                ResponseObject responseObject = await receiptService.DeleteReceipts(request, user);

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

