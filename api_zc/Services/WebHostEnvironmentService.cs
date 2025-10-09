using Accura_MES.Interfaces.Services;

namespace Accura_MES.Services
{
    public class WebHostEnvironmentService : IWebHostEnvironmentService
    {
        private readonly string _app_DataPath;          // 資料存放目錄
        private readonly string _initialDatasPath;      // 初始化資料庫相關設定檔
        private readonly string _AccuraConnectionPath;  // 資料庫連線相關設定檔

        public WebHostEnvironmentService(IWebHostEnvironment env)
        {
            _app_DataPath = Path.Combine(env.ContentRootPath, "App_Data");
            _initialDatasPath = Path.Combine(env.ContentRootPath, "App_Data", "InitialDatas");
            _AccuraConnectionPath = Path.Combine(env.ContentRootPath, "App_Data", "DataBase", "ACCURAConnection.xml");
        }

        public string GetFilePath()
        {
            return _app_DataPath;
        }

        #region InitialDatas
        public string GetFilePath_InitialData(string fileName)
        {
            return Path.Combine(_initialDatasPath, fileName);
        }

        public bool IsFileExists_InitialData(string fileName)
        {
            return File.Exists(GetFilePath_InitialData(fileName));
        }

        public string ReadFileContent_InitialData(string fileName)
        {
            string filePath = GetFilePath_InitialData(fileName);
            return File.Exists(filePath) ? File.ReadAllText(filePath) : "";
        }
        #endregion


        #region ACCURAConnection
        public string GetFilePath_AccuraConnection()
        {
            return _AccuraConnectionPath;
        }

        public bool IsFileExists_AccuraConnection()
        {
            return File.Exists(_AccuraConnectionPath);
        }

        public string ReadFileContent_AccuraConnection()
        {
            return File.Exists(_AccuraConnectionPath) ? File.ReadAllText(_AccuraConnectionPath) : "";
        }
        #endregion

    }
}
