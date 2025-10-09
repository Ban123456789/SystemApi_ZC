using Accura_MES.Extensions;
using Accura_MES.Interfaces.Services;
using Accura_MES.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Accura_MES.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReportController : ControllerBase
    {
        private XML _xml = new();
        private readonly IWebHostEnvironmentService _webHostEnvironmentService;

        public ReportController(IWebHostEnvironmentService webHostService)
        {
            _xml = new XML();
            _webHostEnvironmentService = webHostService;
        }

        /// <summary>
        /// 生產日報表
        /// </summary>
        /// <param name="input">
        /// <para>{</para>
        /// <para>    startDate:              開始日期              </para>
        /// <para>    endDate:                結束日期              </para>
        /// <para>    poNumbers:              [工單編號陣列]        </para>
        /// <para>    moIds:                  [製令 ID 陣列]        </para>
        /// <para>    productionUnitNumbers:  [機台/線程編號陣列]   </para>
        /// <para>    formats:                [型號編號陣列]        </para>
        /// <para>    processTemplateIds:     [工序 ID 陣列]        </para>
        /// <para>}</para>
        /// </param>
        // [HttpPost("DailyReport")]
        // public async Task<IActionResult> DailyReport([FromBody] Dictionary<string, object?> input)
        // {
        //     try
        //     {
        //         // 獲取 token
        //         var token = JwtService.AnalysisToken(HttpContext.Request.Headers["Authorization"]);
        //         long user = long.Parse(token["sub"]);
        //         string connectionString = _xml.GetConnection(Request.Headers["Database"].ToString());
        //         #region 檢查
        //         // 檢查token
        //         ResponseObject result = UserController.CheckToken(Request);
        //         if (!result.Success)
        //         {
        //             return StatusCode(int.Parse(result.Code.Split("-")[0]), result);
        //         }

        //         string[] keys = { "startDate", "endDate", "poNumbers", "moIds", "productionUnitNumbers", "formats", "processTemplateIds", "exportExcel" };
        //         foreach (string key in keys)
        //         {
        //             if (!input.ContainsKey(key))
        //             {
        //                 return this.CustomAccuraResponse(SelfErrorCode.MISSING_PARAMETERS, null, null, key);
        //             }
        //         }
        //         #endregion

        //         input.Add("userId", user);
        //         input.Add("database", Request.Headers["Database"].ToString());

        //         ReportService reportService = ReportService.CreateService(connectionString, _webHostEnvironmentService);

        //         // 取得所有資料表系統資訊
        //         result = await reportService.DailyReport(Request.Headers["i18n"].ToString(), input);
        //         return StatusCode(int.Parse(result.Code.Split("-")[0]), result);
        //     }
        //     catch (Exception ex)
        //     {
        //         // 使用統一方法處理例外
        //         return  this.HandleAccuraException(null, ex);
        //     }
        // }
    }
}
