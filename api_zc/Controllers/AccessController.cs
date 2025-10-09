using Accura_MES.Extensions;
using Accura_MES.Interfaces.Repositories;
using Accura_MES.Interfaces.Services;
using Accura_MES.Repositories;
using Accura_MES.Service;
using Accura_MES.Services;
using Microsoft.AspNetCore.Mvc;

namespace Accura_MES.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccessController : Controller
    {
        private readonly XML xml = new();

        /// <summary>
        /// 根據輸入建立或刪除資料
        /// </summary>
        /// <param name="access"></param>
        /// <returns>建立或刪除的資料id list</returns>
        [HttpPost("CreateOrDeleteAccess")]
        public async Task<IActionResult> CreateOrDeleteAccess([FromBody] List<Dictionary<string, object>> access)
        {
            try
            {
                // get Headers
                string connectionString = xml.GetConnection(Request.Headers["Database"].ToString());
                string authorizationHeader = HttpContext.Request.Headers["Authorization"];
                var token = JwtService.AnalysisToken(HttpContext.Request.Headers["Authorization"]);

                #region Verify
                // Verify Headers
                ResponseObject result = UserController.CheckToken(Request);
                if (!result.Success)
                {
                    // 驗證失敗，直接返回錯誤
                    return StatusCode(int.Parse(result.Code.Split("-")[0]), result);
                }

                // Verify Body
                // 創建輸入處理service
                IInputValidationService inputValidationService = InputValidationService.CreateService(connectionString, "access", API.Create);

                // 檢查"Calendar"必填欄位、過濾不允許的欄位、設定預設值
                var validationResult = await inputValidationService.ValidateAndFilterAndSetDefaultInput("access", access, null, null, false);

                if (validationResult != null)
                {
                    // 驗證失敗，直接返回錯誤
                    return this.CustomAccuraResponse(SelfErrorCode.NESTED_STRUCTURE_ERROR, null, validationResult, "詳情請看error data");
                }
                #endregion

                IAccessRepository accessRepository = AccessRepository.CreateRepository(connectionString);

                await accessRepository.CreateOrDeleteAccess(token["sub"], access);

                return this.SuccessAccuraResponse();
            }
            catch (Exception ex)
            {
                return this.HandleAccuraException(null, ex);
            }
        }
    }
}
