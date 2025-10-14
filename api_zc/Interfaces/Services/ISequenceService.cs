using Microsoft.Data.SqlClient;

namespace Accura_MES.Interfaces.Services
{
    /// <summary>
    /// 序列号服务接口 - 用于生成自动编号
    /// </summary>
    public interface ISequenceService
    {
        /// <summary>
        /// 获取下一个序列号（自动创建连接和事务）
        /// </summary>
        /// <param name="tableName">表名称（如 "Project", "Order"）</param>
        /// <param name="groupKey">分组键（如 customerId, shippedDate）</param>
        /// <param name="numberFormat">编号格式（默认 "0000"，如 "00", "000000"）</param>
        /// <returns>格式化后的序列号</returns>
        Task<string> GetNextNumberAsync(string tableName, string groupKey, string numberFormat = "0000");

        /// <summary>
        /// 获取下一个序列号（使用现有连接和事务）
        /// </summary>
        /// <param name="connection">SQL 连接</param>
        /// <param name="transaction">SQL 事务（可为 null）</param>
        /// <param name="tableName">表名称</param>
        /// <param name="groupKey">分组键</param>
        /// <param name="numberFormat">编号格式</param>
        /// <returns>格式化后的序列号</returns>
        Task<string> GetNextNumberAsync(
            SqlConnection connection, 
            SqlTransaction? transaction,
            string tableName, 
            string groupKey, 
            string numberFormat = "0000");

        /// <summary>
        /// 重置指定分组的序列号（慎用！）
        /// </summary>
        /// <param name="tableName">表名称</param>
        /// <param name="groupKey">分组键</param>
        /// <param name="resetTo">重置到的数字（默认 0）</param>
        Task ResetSequenceAsync(string tableName, string groupKey, int resetTo = 0);

        /// <summary>
        /// 获取当前序列号（不递增）
        /// </summary>
        /// <param name="tableName">表名称</param>
        /// <param name="groupKey">分组键</param>
        /// <returns>当前序列号，如果不存在返回 0</returns>
        Task<int> GetCurrentNumberAsync(string tableName, string groupKey);
    }
}

