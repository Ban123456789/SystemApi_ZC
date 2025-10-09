namespace Accura_MES.Interfaces.Services
{
    public interface IUserService : IService
    {
        /// <summary>
        /// 建立使用者
        /// </summary>
        /// <param name="UserInfo"></param>
        /// <param name="user"></param>
        /// <returns></returns>
        Task<long> Create(Dictionary<string, object?> UserInfo, long? user);


        /// <summary>
        /// 編輯使用者
        /// </summary>
        /// <param name="UserInfo"></param>
        /// <param name="user"></param>
        /// <returns></returns>
        Task<bool> Update(Dictionary<string, object?> UserInfo, long? user);

        /// <summary>
        /// change password
        /// </summary>
        /// <param name="user"></param>
        /// <param name="oldPassword"></param>
        /// <param name="newPassword"></param>
        /// <returns></returns>
        Task<ResponseObject> ChangePassWord(long user, string oldPassword, string newPassword);


        /// <summary>
        /// 編輯 createdBy/modifiedBy
        /// </summary>
        /// <param name="UserInfo"></param>
        /// <returns></returns>
        Task<bool> UpdateCreatedByAndModifiedBy(Dictionary<string, object?> UserInfo);
    }
}
