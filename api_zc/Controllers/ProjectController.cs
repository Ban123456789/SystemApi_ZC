using Accura_MES.Extensions;
using Accura_MES.Interfaces.Services;
using Accura_MES.Services;
using Microsoft.AspNetCore.Mvc;

namespace Accura_MES.Controllers
{

    [Route("api/[controller]")]
    [ApiController]

    public class ProjectController : ControllerBase
    {
        private XML _xml = new();

        /// <summary>
        /// 用輸入物件刷新 [tableColumnSetting]
        /// </summary>
        /// <param name="input">long id, long userId, string tableName, string columnName, int sequence, bool isShow</param>
        /// <remarks>
        /// 用 userId, tableName, columnName  比對新舊資料
        /// </remarks>
        /// <returns></returns>
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

                IProjectService projectService = ProjectService.CreateService(connectionString);

                // 建立 [project]
                ResponseObject responseObject = await projectService.Create(user, input);

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
