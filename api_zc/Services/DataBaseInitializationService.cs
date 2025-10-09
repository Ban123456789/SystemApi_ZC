using Accura_MES.Extensions;
using Accura_MES.Interfaces.Repositories;
using Accura_MES.Interfaces.Services;
using Accura_MES.Models;
using Accura_MES.Repositories;
using Accura_MES.Utilities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Transactions;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;


namespace Accura_MES.Services
{
    public class DataBaseInitializationService : IDataBaseInitializationService
    {
        private readonly string _connectionString;
        private readonly IWebHostEnvironmentService _webHostService;
        
        private DataBaseInitializationService(string connectionString, IWebHostEnvironmentService webHostService)
        {
            _connectionString = connectionString;
            _webHostService = webHostService;
        }

        /// <summary>
        /// 靜態工廠方法，用於創建 DataBaseInitializationService 實例
        /// </summary>
        /// <param name="connectionString"></param>
        /// <returns>DataBaseInitializationService 實例</returns>
        /// <exception cref="CustomErrorCodeException"></exception>
        public static DataBaseInitializationService CreateService(string connectionString, IWebHostEnvironmentService webHostService)
        {
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new CustomErrorCodeException(SelfErrorCode.NOT_FOUND_WITH_MSG, "SQL connection string not exist");
            }

            return new DataBaseInitializationService(connectionString, webHostService);
        }

        public async Task<bool> UpdateDataSource()
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                IPropertyRepository propertyRepository = PropertyRepository.CreateRepository(_connectionString);

                // 設定 [property].datasource
                // 獲取指定json檔案路徑
                DirectoryInfo directoryInfo = new DirectoryInfo(Directory.GetCurrentDirectory());
                string filePath = System.IO.Path.Combine(directoryInfo.Parent != null ? directoryInfo.Parent.FullName : string.Empty, "propertyAndMenuList.json");

                // 讀取json
                List<MappingItem_propertyAndMenuList> mappingItems = FileUtils.ReadJsonFile<List<MappingItem_propertyAndMenuList>>(filePath);

                await propertyRepository.UpdateDataSource(mappingItems);

                return true;
            }
            catch
            {
                throw;
            }
        }

        public async Task<IActionResult> InitializeItemTypeAndPropertyAndDataSourceAsync(ControllerBase controller)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                connection.Open();
                IPropertyRepository propertyRepository = PropertyRepository.CreateRepository(_connectionString);


                // 取得系統資料
                Dictionary<string, List<Dictionary<string, object>>> systemInfo = propertyRepository.GetDataBaseSystemInfo();


                bool initialized = false;
                try
                {

                    IUserRepository userRepository = UserRepository.CreateRepository(_connectionString);

                    // 取得 system id
                    long sysId = await userRepository.GetSystemId(connection, null);

                    // 初始化[temType]
                    initialized = await propertyRepository.InitializeItemType(systemInfo, sysId);
                    if (!initialized)
                    {
                        return controller.CustomAccuraResponse(SelfErrorCode.INITIALIZATION_FAILED, "", "", "[itemtype]");
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex);
                    throw new CustomErrorCodeException(SelfErrorCode.INITIALIZATION_FAILED, "[itemtype]");
                }

                try
                {
                    // 初始化[property]
                    await propertyRepository.InitializeProperty(systemInfo);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex);
                    throw new CustomErrorCodeException(SelfErrorCode.INITIALIZATION_FAILED, "[property]");
                }

                // 設定 [property].datasource
                // 獲取指定json檔案路徑
                DirectoryInfo directoryInfo = new DirectoryInfo(Directory.GetCurrentDirectory());
                string filePath = System.IO.Path.Combine(directoryInfo.Parent != null ? directoryInfo.Parent.FullName : string.Empty, "propertyAndMenuList.json");

                // 讀取json
                List<MappingItem_propertyAndMenuList> mappingItems = FileUtils.ReadJsonFile<List<MappingItem_propertyAndMenuList>>(filePath);

                await propertyRepository.UpdateDataSource(mappingItems);

                return controller.SuccessAccuraResponse();
            }
            catch
            {
                throw;
            }
        }

        public async Task<bool> InitializeMenuListAndDropList()
        {
            try
            {
                var connection = new SqlConnection(_connectionString);
                connection.Open();
                var transaction = connection.BeginTransaction();

                IPropertyRepository propertyRepository = PropertyRepository.CreateRepository(_connectionString);

                bool isSuccess = false;
                try
                {
                    IUserRepository userRepository = UserRepository.CreateRepository(_connectionString);

                    // 取得 system id
                    long sysId = await userRepository.GetSystemId(connection, transaction);


                    var mappingItems = GetMenuListAndDropListContentFromFiles();


                    isSuccess = await propertyRepository.InitializeMenuListAndDropList(connection, transaction, mappingItems, sysId);
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }

                transaction.Commit();

                return isSuccess;
            }
            catch
            {
                throw;
            }
        }

        public async Task<bool> InitializeMenuListAndDropList_userTempTable()
        {
            try
            {
                var connection = new SqlConnection(_connectionString);
                connection.Open();
                var transaction = connection.BeginTransaction();

                IPropertyRepository propertyRepository = PropertyRepository.CreateRepository(_connectionString);
                IUserRepository userRepository = UserRepository.CreateRepository(_connectionString);

                // 取得 system id
                long sysId = await userRepository.GetSystemId(connection, transaction);

                bool isSuccess = false;
                try
                {
                    var mappingItems = GetMenuListAndDropListContentFromFiles();

                    var systemInfo = GetSystemInfoForInitializationAndDropListContent(propertyRepository);

                    isSuccess = await propertyRepository.InitializeMenuListAndDropList_useTempTable(connection, transaction, systemInfo["droplist"], mappingItems, sysId);
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }

                transaction.Commit();

                return isSuccess;
            }
            catch
            {
                throw;
            }
        }



        public async Task<bool> CreateDataBase(
            string dataBaseName,
            string fileName,
            string logFileName)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                IPropertyRepository propertyRepository = PropertyRepository.CreateRepository(_connectionString);

                return await propertyRepository.CreateDataBase(dataBaseName, fileName, logFileName);
            }
            catch
            {
                throw;
            }
        }


        public async Task<bool> InitializeDataBase()
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                IPropertyRepository propertyRepository = PropertyRepository.CreateRepository(_connectionString);
                IGenericRepository genericRepository = GenericRepository.CreateRepository(_connectionString, null);
                IUserRepository userRepository = UserRepository.CreateRepository(_connectionString);

                // 開始交易
                return
                await SqlHelper.ExecuteTransactionAsync(connection, async (transaction, command) =>
                {
                    // 1.建立預設 User
                    var userInfo1 = new Dictionary<string, object?>()
                    {
                        { "number", "000" },
                        { "name", "system" },
                        { "account", "system" },
                        { "password", EncryptAES_test("system") },
                        { "isEnable", "true" }
                    };
                    var userInfo2 = new Dictionary<string, object?>()
                    {
                        { "number", "000" },
                        { "name", "admin" },
                        { "account", "admin" },
                        { "password", EncryptAES_test("admin") },
                        { "isEnable", "true" }
                    };

                    // 建立 system
                    var systemUserId = await userRepository.Create(connection, transaction, userInfo1, null);

                    // 更新 system.createdBy/modifiedBy
                    var userInfo3 = new Dictionary<string, object?>()
                    {
                        { "id", systemUserId },
                        { "createdBy", systemUserId },
                        { "modifiedBy", systemUserId }
                    };

                    var isUpdateSuccess = await userRepository.Update(connection, transaction, userInfo3, systemUserId);
                    if (!isUpdateSuccess)
                        throw new CustomErrorCodeException(SelfErrorCode.NO_DATA_AFFECTED,
                            $"發生錯誤:對 [User].name = system 的欄位 {string.Join(", ", userInfo3.Select(dict => dict.Key).ToList())} 更新失敗");

                    // 建立 admin
                    var adminUserId = await userRepository.Create(connection, transaction, userInfo2, systemUserId); // 將 system 的 user.id 給 admin

                    Debug.WriteLine("建立預設 User 成功完成");

                    // 2.建立預設 Identity
                    var inputData = new List<Dictionary<string, object?>>()
                    {
                        new ()
                        {
                            { "number", "000" },
                            { "name", "Admin" },
                            { "label", "{\"zh-TW\":\"系統管理員\"}" },
                            { "isSystem", true }
                        }
                    };

                    HashSet<string> columnNames = new()
                    {
                        "number", "name", "label", "isSystem"
                    };

                    var adminIdentityIds = await genericRepository.CreateDataGeneric(connection, transaction,
                        systemUserId, "identity", columnNames, inputData);

                    var adminIdentityId = adminIdentityIds.First();


                    Debug.WriteLine("建立預設 identity 成功完成");

                    // 3.建立預設 user_identity
                    inputData = new List<Dictionary<string, object?>>()
                    {
                        new ()
                        {
                            { "userId", systemUserId },
                            { "identityId", adminIdentityId }
                        },
                        new ()
                        {
                            { "userId", adminUserId },
                            { "identityId", adminIdentityId }
                        }
                    };
                    columnNames = new()
                    {
                        "userId", "identityId"
                    };

                    await genericRepository.CreateDataGeneric(
                        connection, transaction, systemUserId, "user_identity", columnNames, inputData);

                    Debug.WriteLine("建立預設 user_identity 成功完成");

                    // 5.建立預設 itemtype
                    Dictionary<string, List<Dictionary<string, object>>> systemInfo = GetSystemInfoForInitializationAndDropListContent(propertyRepository);

                    var itemtypeInfo = await propertyRepository.InitializeItemType(connection, transaction, systemInfo, systemUserId);

                    Debug.WriteLine("建立預設 itemtype 成功完成");

                    // 6.建立預設 property
                    await propertyRepository.InitializeProperty(connection, transaction, systemInfo, itemtypeInfo, systemUserId);

                    Debug.WriteLine("建立預設 property 成功完成");

                    // 7.建立預設 menulist/droplist
                    var mappingItems2 = GetMenuListAndDropListContentFromFiles();
                    await propertyRepository.InitializeMenuListAndDropList_useTempTable(connection, transaction, systemInfo["droplist"], mappingItems2, systemUserId);

                    Debug.WriteLine("建立預設 menulist/droplist 成功完成");

                    // 8.建立預設 preferences
                    // inputData = new List<Dictionary<string, object?>>()
                    // {
                    //     new ()
                    //     {
                    //         // 同步第1次/1天
                    //         { "name", "syncStockFromErp01" },
                    //         { "description", "同步 ERP 庫存結餘01" },
                    //         { "value", "00,00,00,10" },
                    //         { "enable", true }
                    //     }
                    // };

                    columnNames = inputData.First().Keys.ToHashSet();

                    await genericRepository.CreateDataGeneric(
                        connection, transaction, systemUserId, "preferences", columnNames, inputData);

                    Debug.WriteLine("建立預設 preferences 成功完成");

                    return true;
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                throw;
            }
        }

        /// <summary>
        /// 讀取資料庫系統資訊並設定下拉選單資料
        /// </summary>
        /// <param name="propertyRepository"></param>
        /// <returns>根據 json 檔案內容更新 reference_table_name 後的資料庫系統資訊</returns>
        private static Dictionary<string, List<Dictionary<string, object>>> GetSystemInfoForInitializationAndDropListContent(IPropertyRepository propertyRepository)
        {
            Dictionary<string, List<Dictionary<string, object>>> systemInfo = propertyRepository.GetDataBaseSystemInfo(); // 取得系統資料

            // 獲取下拉選單關聯 json 檔案路徑
            DirectoryInfo directoryInfo = new DirectoryInfo(Directory.GetCurrentDirectory());
            string filePath = System.IO.Path.Combine(directoryInfo.Parent != null ? directoryInfo.Parent.FullName : string.Empty, "propertyAndMenuList.json");

            // 讀取json
            List<MappingItem_propertyAndMenuList> mappingItems1 = FileUtils.ReadJsonFile<List<MappingItem_propertyAndMenuList>>(filePath);

            // 更新 systemInfo.datasource
            foreach (var mappingItem in mappingItems1)
            {
                string itemTypeName = mappingItem.ItemTypeName;
                string propertyName = mappingItem.PropertyName;
                string newValue = mappingItem.Name;

                if (systemInfo.TryGetValue(itemTypeName, out var innerList))
                {
                    bool foundColumn = false; // 檢查用

                    // 遍歷所有欄位
                    foreach (var innerDict in innerList)
                    {
                        // 找到指定欄位
                        if (innerDict.ContainsValue(propertyName))
                        {
                            Debug.WriteLine($"更新值 : [{itemTypeName}].[{innerDict["COLUMN_NAME"]}] -> [{innerDict["Referenced_Table_Name"]}] to {newValue}");

                            innerDict["Referenced_Table_Name"] = newValue; // 更新值

                            foundColumn = true;
                        }
                    }

                    // 沒找到指定欄位
                    if (!foundColumn)
                    {
                        Debug.WriteLine($"[{itemTypeName}] 這張表找不到指定欄位 : [{propertyName}]，請檢查更新資料來源");
                    }
                }
            }

            return systemInfo;
        }

        /// <summary>
        /// 從檔案讀取下拉選單的資料
        /// </summary>
        /// <returns></returns>
        private static List<Mapping_MenuListAndDropList> GetMenuListAndDropListContentFromFiles()
        {
            // 獲取指定 json 檔案路徑
            DirectoryInfo directoryInfo = new DirectoryInfo(Directory.GetCurrentDirectory());
            string jsonFilePath = System.IO.Path.Combine(directoryInfo.Parent != null ? directoryInfo.Parent.FullName : string.Empty, "MenuListAndDropList.json");

            // 獲取指定 excel 檔案路徑
            string excelFilePath1 = System.IO.Path.Combine(directoryInfo.Parent != null ? directoryInfo.Parent.FullName : string.Empty, "料件分群碼.xls");
            string excelFilePath2 = System.IO.Path.Combine(directoryInfo.Parent != null ? directoryInfo.Parent.FullName : string.Empty, "檢驗不良原因.xls");
            string excelFilePath3 = System.IO.Path.Combine(directoryInfo.Parent != null ? directoryInfo.Parent.FullName : string.Empty, "製程停工代號.xlsx");

            // 讀取 json
            var mappingItems = FileUtils.ReadJsonFile<List<Mapping_MenuListAndDropList>>(jsonFilePath);

            // 讀取 excel
            var excelContentList1 = FileUtils.ReadExcel(excelFilePath1);
            var excelContentList2 = FileUtils.ReadExcel(excelFilePath2);
            var excelContentList3 = FileUtils.ReadExcel(excelFilePath3);

            // 將 excel 的內容寫進 mappingItems 的對應 droplist 中
            var excelDropListItem_PartGroupCode = new List<DropListItem>();
            var excelDropListItem_RejectReason = new List<DropListItem>();
            var excelDropListItem_StopReason = new List<DropListItem>();
            var excelDropListItem_faultyReason = new List<DropListItem>();

            // 第一個 excel 料件分群碼
            var sheet = excelContentList1["SQL Results"];
            sheet.RemoveAt(0); // 第一行不是下拉選單資料，刪除第一行

            // 遍歷 工作表 每行
            foreach (var col in sheet)
            {
                if (string.IsNullOrEmpty(col[0])) continue;

                var row1 = col[0];
                var row2 = col[1];

                var dropListItem = new DropListItem()
                {
                    I18n = new()
                    {
                        ZhTW = row2 is null ? row1 : row2,    // 如果中文值是空的，則設為代號
                        En = ""
                    },
                    Value = col[0],
                    Descripetion = "",
                    Type = "system",
                    IsDelete = false,
                };

                excelDropListItem_PartGroupCode.Add(dropListItem);
            }

            // 第二個 excel 檢驗不良原因
            sheet = excelContentList2["SQL Results"];
            sheet.RemoveAt(0); // 第一行不是下拉選單資料，刪除第一行

            foreach (var col in sheet)
            {
                if (string.IsNullOrEmpty(col[0])) continue;

                var row1 = col[0];
                var row2 = col[1];

                var dropListItem = new DropListItem()
                {
                    I18n = new()
                    {
                        ZhTW = row2 is null ? row1 : row2,    // 如果中文值是空的，則設為代號
                        En = ""
                    },
                    Value = col[0],
                    Descripetion = "",
                    Type = "system",
                    IsDelete = false,
                };

                excelDropListItem_RejectReason.Add(dropListItem);
            }

            // 第三個 excel 軋型停工代號 & 產出不良
            sheet = excelContentList3["停機原因"];
            sheet.RemoveAt(0); // 第一行不是下拉選單資料
            sheet.RemoveAt(0); // 第二行不是下拉選單資料

            foreach (var col in sheet)
            {
                if (string.IsNullOrEmpty(col[0])) continue;

                var row1 = col[0];
                var row2 = col[1];
                var dropListItem = new DropListItem()
                {
                    I18n = new()
                    {
                        ZhTW = row2 is null ? row1 : row2,    // 如果中文值是空的，則設為代號
                        En = ""
                    },
                    Value = col[0],
                    Descripetion = "",
                    Type = "normal",
                    IsDelete = false,
                };
                excelDropListItem_StopReason.Add(dropListItem);
            }

            sheet = excelContentList3["產出不良"];
            sheet.RemoveAt(0); // 第一行不是下拉選單資料
            sheet.RemoveAt(0); // 第二行不是下拉選單資料

            foreach (var col in sheet)
            {
                if (string.IsNullOrEmpty(col[0])) continue;

                var row1 = col[0];
                var row2 = col[1];
                var dropListItem = new DropListItem()
                {
                    I18n = new()
                    {
                        ZhTW = row2 is null ? row1 : row2,    // 如果中文值是空的，則設為代號
                        En = "" 
                    },
                    Value = col[0],
                    Descripetion = "",
                    Type = "normal",
                    IsDelete = false,
                };
                excelDropListItem_faultyReason.Add(dropListItem);
            }

            // 加進 mappingItems
            foreach (var mappingItem in mappingItems)
            {
                switch (mappingItem.Name)
                {
                    case "Part_Group_Code":
                        mappingItem.DropList.AddRange(excelDropListItem_PartGroupCode);
                        break;
                    case "Inspection_RejectReason":
                        mappingItem.DropList.AddRange(excelDropListItem_RejectReason);
                        break;
                    case "MoReporthistory_StopReason":
                        mappingItem.DropList.AddRange(excelDropListItem_StopReason);
                        break;
                    case "MoReporthistory_FaultyReason":
                        mappingItem.DropList.AddRange(excelDropListItem_faultyReason);
                        break;
                }
            }

            return mappingItems;
        }

    
        /// <summary>
        /// AES 加密(測試用)
        /// </summary>
        /// <param name="plainText"></param>
        /// <returns></returns>
        private static string EncryptAES_test(string plainText)
        {
            // 密鑰
            byte[] key = Encoding.UTF8.GetBytes("=GC%'AmN/}2f9Q#u");

            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = key;
                aesAlg.GenerateIV(); // 自動生成 IV
                byte[] iv = aesAlg.IV; // 獲取 IV

                aesAlg.Mode = CipherMode.CBC;
                aesAlg.Padding = PaddingMode.PKCS7;

                using (MemoryStream msEncrypt = new MemoryStream())
                {
                    // 將 IV 寫入到密文流中
                    msEncrypt.Write(iv, 0, iv.Length);

                    using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, aesAlg.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                        {
                            swEncrypt.Write(plainText);
                        }
                    }

                    return Convert.ToBase64String(msEncrypt.ToArray()); // 返回包含 IV 的密文
                }
            }
        }
    }
}
