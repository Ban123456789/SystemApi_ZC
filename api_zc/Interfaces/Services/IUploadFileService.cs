namespace Accura_MES.Interfaces.Services
{
    public interface IUploadFileService : IService
    {
        /// <summary>
        /// 儲存檔案。
        /// </summary>
        /// <param name="files"></param>
        /// <param name="dataBaseName"></param>
        /// <remarks>
        /// 儲存路徑：{讀取設定檔的路徑}/{資料庫名稱}/{年}/{月}/{日}/{隨機碼}/{檔案名稱}
        /// </remarks>
        /// <returns>
        /// Data:字典:string, string
        /// (
        /// Key:
        /// "fileName": 檔名,
        /// "filePath": 實體檔案路徑,
        /// "type": image, pdf, other,
        /// "fileType": 副檔名,
        /// "parentPath": 父階資料夾完整路徑,
        /// "uniqueFolder": 父階資料夾名稱(亂碼)
        /// ) 
        /// <para>
        /// => List&lt;Dictionary&lt;string, string&gt;&gt;
        /// </para>
        /// </returns>
        Task<ResponseObject> SaveFiles(List<IFormFile> files, string dataBaseName);

        /// <summary>
        /// 批次建立附件資訊到資料庫
        /// </summary>
        /// <param name="user"></param>
        /// <param name="fileInfos">所有檔案資訊</param>
        /// <returns>list of created acctchment.id => List&lt;long&gt;</returns>
        Task<ResponseObject> CreateAttachment(long user, List<Dictionary<string, object?>> fileInfos);
    }
}
