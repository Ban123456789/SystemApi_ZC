using Accura_MES.Extensions;
using Accura_MES.Interfaces.Repositories;
using Accura_MES.Interfaces.Services;
using Accura_MES.Repositories;
using Accura_MES.Utilities;
using Microsoft.Data.SqlClient;

namespace Accura_MES.Services
{
    public class UploadFileService : IUploadFileService
    {

        private readonly string _connectionString;
        private readonly XML _xml;
        private IGenericRepository _genericRepository;

        public UploadFileService(string connectionString, IGenericRepository genericRepository)
        {
            _connectionString = connectionString;
            _xml = new XML();
            _genericRepository = genericRepository;
        }

        /// <summary>
        /// 靜態工廠方法，並初始化資料庫 Service
        /// </summary>
        /// <param name="connectionString"></param>
        /// <param name="user"></param>
        /// <returns></returns>
        /// <exception cref="CustomErrorCodeException"></exception>
        public static UploadFileService CreateService(string connectionString)
        {
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new CustomErrorCodeException(SelfErrorCode.NOT_FOUND_WITH_MSG, "SQL connection string not exist");
            }

            IGenericRepository genericRepository;
            try
            {
                genericRepository = GenericRepository.CreateRepository(connectionString, null);
            }
            catch
            {
                throw;
            }

            return new UploadFileService(connectionString, genericRepository);
        }

        public async Task<ResponseObject> SaveFiles(List<IFormFile> files, string dataBaseName)
        {
            ResponseObject responseObject = new ResponseObject().GenerateEntity(
                SelfErrorCode.INTERNAL_SERVER_WITH_MSG, null, null, "嘗試儲存檔案失敗");

            try
            {
                var fileInfos = new List<Dictionary<string, string>>();

                foreach (var file in files)
                {
                    // 紀錄檔案資訊
                    var fileInfo = new Dictionary<string, string>
                    {
                        { "fileName", "" },     // 檔名
                        { "filePath", "" },     // 實體檔案路徑
                        { "type", "" },         // image, pdf, other
                        { "fileType", "" },     // 副檔名
                        { "parentPath", "" },   // 父階資料夾完整路徑
                        { "uniqueFolder", "" }  // 父階資料夾名稱(亂碼)
                    };

                    // 取得原始檔案名稱
                    string fileName = Path.GetFileName(file.FileName);

                    // 生成隨機碼
                    string randomCode = Guid.NewGuid().ToString("N");

                    // 取得當前日期
                    string year = DateTime.Now.Year.ToString();
                    string month = DateTime.Now.Month.ToString("D2");
                    string day = DateTime.Now.Day.ToString("D2");

                    // 設定要傳到資料庫的資料
                    string dbName = dataBaseName;
                    string basePath = _xml.GetFilepath();   // 讀取設定檔的路徑
                    string parentPath = Path.Combine(dbName, year, month, day).Replace("\\", "/"); // 替換成正斜線
                    string uniqueFolder = Path.Combine(randomCode);

                    // 設定儲存路徑：{基底路徑}/{資料庫名稱}/{年}/{月}/{日}/{隨機碼}/{檔案名稱}
                    string filePath = Path.Combine(basePath, dbName, year, month, day, randomCode, fileName);

                    // 建立儲存目錄（如果不存在）
                    var directory = Path.GetDirectoryName(filePath);
                    if (!Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }

                    // 儲存檔案
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }

                    // 記錄檔案資訊
                    fileInfo["fileName"] = fileName;
                    fileInfo["filePath"] = filePath;
                    fileInfo["type"] = FileUtils.GetFileType(file);
                    fileInfo["fileType"] = System.IO.Path.GetExtension(filePath);
                    fileInfo["parentPath"] = parentPath;
                    fileInfo["uniqueFolder"] = uniqueFolder;
                    fileInfos.Add(fileInfo);
                } // end of for(files)


                responseObject.SetErrorCode(SelfErrorCode.SUCCESS);
                responseObject.Data = fileInfos;
                return responseObject;
            }
            catch (Exception ex)
            {
                return await this.HandleExceptionAsync(ex);
            }
        }


        public async Task<ResponseObject> CreateAttachment(long user, List<Dictionary<string, object?>> fileInfos)
        {
            ResponseObject responseObject = new ResponseObject().GenerateEntity(
                SelfErrorCode.INTERNAL_SERVER_WITH_MSG, null, null, "建立 Attachment 發生未知錯誤");

            SqlTransaction? transaction = null;

            List<long> attachmentIds = new();   // 儲存建立的附件 id

            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();
                transaction = connection.BeginTransaction();

                // 取得設定檔 FILEURL
                string fileURL = _xml.GetFileURL().TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar); // 去掉結尾斜線

                // 製作附件的物件
                List<Dictionary<string, object?>> valuesOfAttachments = new();
                foreach (var fileInfo in fileInfos)
                {
                    Dictionary<string, object?> valuesOfAttachment = new()
                    {
                        { "name", fileInfo.GetValueOrThrow<string>("fileName") },
                        { "type", fileInfo.GetValueOrThrow<string>("type") },
                        { "filetype", fileInfo.GetValueOrThrow<string>("fileType") },
                        { "location", fileInfo.GetValueOrThrow<string>("parentPath") },
                        { "url", fileURL },
                        { "uniqueCode", fileInfo.GetValueOrThrow<string>("uniqueFolder") }
                    };

                    valuesOfAttachments.Add(valuesOfAttachment);
                }

                // 建立附件
                attachmentIds = await _genericRepository.CreateDataGeneric(connection, transaction, user, "attachment", valuesOfAttachments);

                await transaction.CommitAsync();

                responseObject.SetErrorCode(SelfErrorCode.SUCCESS);
                responseObject.Data = attachmentIds;
                return responseObject;
            }
            catch (Exception ex)
            {
                return await this.HandleExceptionAsync(ex, transaction);
            }
        }
    }
}
