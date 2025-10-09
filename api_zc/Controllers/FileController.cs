using Accura_MES.Extensions;
using Accura_MES.Interfaces.Repositories;
using Accura_MES.Interfaces.Services;
using Accura_MES.Models;
using Accura_MES.Repositories;
using Accura_MES.Services;
using Microsoft.AspNetCore.Mvc;
using System.Data.SqlTypes;
using System.Diagnostics;

namespace Accura_MES.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FileController : ControllerBase
    {
        private XML _xml;
        private readonly IWebHostEnvironmentService _webHostEnvironmentService;

        public FileController(IWebHostEnvironmentService webHostService)
        {
            _xml = new XML();
            _webHostEnvironmentService = webHostService;
        }

        /// <summary>
        /// 將接收的檔案傳送到指定位置，並將檔案資訊加入[attachment]
        /// </summary>
        /// <param name="files"></param>
        /// <returns></returns>
        [HttpPost("UploadFile")]
        public async Task<IActionResult> PostFormData(List<IFormFile> files)
        {
            #region 檢查
            // 檢查Header
            ResponseObject result = UserController.CheckToken(Request);
            if (!result.Success)
            {
                return StatusCode(int.Parse(result.Code.Split("-")[0]), result);
            }
            // 檢查Body
            if (files == null || files.Count == 0)
            {
                return this.CustomAccuraResponse(SelfErrorCode.MISSING_FILE);
            }
            #endregion

            // 獲取connection, token
            string connectionString = _xml.GetConnection(Request.Headers["Database"].ToString());
            string? authorizationHeader = HttpContext.Request.Headers["Authorization"];
            var token = JwtService.AnalysisToken(authorizationHeader);
            long user = long.Parse(token["sub"]);

            IFileService uploadFileService = FileService.CreateService(connectionString, _webHostEnvironmentService);

            string url = _xml.GetFileURL();

            try
            {
                // 儲存檔案
                var responseObject = await uploadFileService.SaveFiles(files, Request.Headers["Database"].ToString());
                if (!responseObject.Success)
                {
                    // 儲存檔案失敗
                    return this.CustomAccuraResponse(responseObject);
                }

                // 讀取回傳物件
                var fileInfos = responseObject.Data as List<Dictionary<string, string>>;

                // 轉換為 List<Dictionary<string, object>>
                List<Dictionary<string, object>> fileInfosObj = fileInfos
                    .Select(dict => dict.ToDictionary(kv => kv.Key, kv => (object)kv.Value))
                    .ToList();

                // 建立附件
                var responseObject_CreateAttachment = await uploadFileService.CreateAttachment(user, fileInfosObj);
                if (!responseObject_CreateAttachment.Success)
                {
                    // 失敗返回
                    return this.CustomAccuraResponse(responseObject_CreateAttachment);
                }

                // 讀取回傳物件
                List<long>? insertedId = responseObject_CreateAttachment.Data as List<long>;

                return this.SuccessAccuraResponse(insertedId);
            }
            catch (Exception ex)
            {
                // 使用統一方法處理例外
                return await this.HandleAccuraExceptionAsync(null, ex);
            }
        }
    }
}
