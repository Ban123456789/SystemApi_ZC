namespace Accura_MES.Interfaces.Services
{
    public interface IWebHostEnvironmentService
    {

        /// <summary>
        /// 取得根目錄路徑
        /// </summary>
        /// <returns></returns>
        string GetFilePath();

        #region InitialDatas
        /// <summary>
        /// 取得 InitialDatas 資料夾底下檔案的完整路徑
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns>文件完整路徑</returns>
        string GetFilePath_InitialData(string fileName);

        /// <summary>
        /// 檔案存不存在
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        bool IsFileExists_InitialData(string fileName);

        /// <summary>
        /// 讀取檔案
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        string ReadFileContent_InitialData(string fileName);
        #endregion

        #region ACCURAConnection
        /// <summary>
        /// 取得 ACCURAConnection 檔案的完整路徑
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        string GetFilePath_AccuraConnection();

        /// <summary>
        /// 檔案存不存在
        /// </summary>
        /// <returns></returns>
        bool IsFileExists_AccuraConnection();

        /// <summary>
        /// 讀取檔案
        /// </summary>
        /// <returns></returns>
        string ReadFileContent_AccuraConnection();
        #endregion
    }
}
