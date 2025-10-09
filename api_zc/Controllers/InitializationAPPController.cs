using Accura_MES.Extensions;
using Accura_MES.Interfaces.Services;
using Accura_MES.Services;
using Accura_MES.Utilities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace Accura_MES.Controllers
{
    /// <summary>
    /// 這個控制器主要用於
    /// 1.初始化應用程式的相關操作
    /// 2.跟測試用的 API
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class InitializationAPPController : Controller
    {
        private readonly XML _xml = new();
        private readonly Serilog.ILogger _logger;
        private readonly IWebHostEnvironmentService _webHostEnvironmentService;

        private readonly List<Timer> _timers = new();  // 儲存動態排程的 Timer

        public InitializationAPPController(IWebHostEnvironmentService webHostService)
        {
            _webHostEnvironmentService = webHostService;
        }

        /// <summary>
        /// 測試用
        /// </summary>
        /// <returns></returns>
        [HttpGet("APPStart")]
        public IActionResult APPSTart()
        {
            ResponseObject responseObject = new();
            _logger.Information("這是一條來自 InitializationAPPController 的日誌。");

            try
            {
                Dictionary<string, object> dict = new()
                {
                    { "innerDict", new Dictionary<string, object>() { { "321", 123 } } }
                };

                var innerDict = dict.GetValueThenTryParseOrThrow<Dictionary<string, object>>("innerDict");

                responseObject.SetErrorCode(SelfErrorCode.SUCCESS);
                responseObject.Data = innerDict;
                return this.CustomAccuraResponse(responseObject);
            }
            catch (Exception ex)
            {
                return this.HandleAccuraException(null, ex);
            }

            // 嘗試取得IIS server 中的初始化資料夾中的文件
            if (_webHostEnvironmentService.IsFileExists_InitialData("testJson.json"))
            {
                _logger.Information("找到了 testJson.json 文件。");

                // 讀取文件內容
                string jsonContent = _webHostEnvironmentService.ReadFileContent_InitialData("testJson.json");

                // 反序列化 JSON 字符串
                var json = JsonHelper.Deserialize<Dictionary<string, string>>(jsonContent);

                // 將反序列化後的 JSON 字典輸出到日誌
                _logger.Information("testJson.json 文件內容：{json}", json);
            }
            else
            {
                // 未找到文件
                // log
                _logger.Information("未找到 testJson.json 文件。");
            }

            // 嘗試取得IIS server 中的 ACCURAConnection 文件
            if (_webHostEnvironmentService.IsFileExists_AccuraConnection())
            {
                _logger.Information("找到了 ACCURAConnection 文件。");

                // 讀取文件內容
                string accuraConnectionContent = _webHostEnvironmentService.ReadFileContent_AccuraConnection();

                // 將 ACCURAConnection 文件內容輸出到日誌
                _logger.Information("ACCURAConnection 文件內容：{accuraConnectionContent}", accuraConnectionContent);
            }
            else
            {
                // 未找到文件
                // log
                _logger.Information("未找到 ACCURAConnection 文件。");
            }

            return this.SuccessAccuraResponse();
        }

        /// <summary>
        /// 用來保持後端 APP 運行用的 API
        /// </summary>
        /// <returns></returns>
        [HttpGet("APPWakeUp")]
        public IActionResult APPWakeUp()
        {
            // log
            _logger.Information("已接收到 APPWakeUp 請求。");

            return this.SuccessAccuraResponse();
        }
    }
}
