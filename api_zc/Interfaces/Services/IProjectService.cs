namespace Accura_MES.Interfaces.Services
{
    public interface IProjectService : IService
    {
        /// <summary>
        /// 建立 [project]
        /// </summary>
        /// <param name="userId">使用者 ID</param>
        /// <param name="projectObject">Project Object</param>
        /// <returns></returns>
        Task<ResponseObject> Create(long userId, List<Dictionary<string, object?>> projectObject);
    }
}
