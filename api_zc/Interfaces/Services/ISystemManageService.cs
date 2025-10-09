namespace Accura_MES.Interfaces.Services
{
    public interface ISystemManageService : IService
    {
        /// <summary>
        /// 將 [tableColumnSetting] 的資料新增、編輯、(真)刪除成輸入物件的樣子
        /// </summary>
        ///<param name="user"></param>
        /// <param name="tableColumnSettingObject">long id, long userId, string tableName, string columnName, int sequence, bool isShow</param>
        /// <remarks>
        /// 用 userId, tableName, columnName  比對新舊資料
        /// </remarks>
        /// <returns></returns>
        Task<ResponseObject> TableSetting(long user, List<Dictionary<string, object?>> tableColumnSettingObject);
    }
}
