using Accura_MES.Extensions;
using Accura_MES.Interfaces.Services;
using Accura_MES.Models;
using Accura_MES.Services;
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
    }
}

