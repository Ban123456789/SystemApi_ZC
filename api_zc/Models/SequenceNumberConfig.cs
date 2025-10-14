namespace Accura_MES.Models
{
    /// <summary>
    /// 序列号配置模型
    /// </summary>
    public class SequenceNumberConfig
    {
        /// <summary>
        /// 表名称
        /// </summary>
        public string TableName { get; set; } = string.Empty;

        /// <summary>
        /// 编号字段名称（例如：number）
        /// </summary>
        public string NumberFieldName { get; set; } = "number";

        /// <summary>
        /// 分组字段名称（例如：customerId, shippedDate）
        /// </summary>
        public string GroupByFieldName { get; set; } = string.Empty;

        /// <summary>
        /// 编号格式（例如："0000", "00"）
        /// </summary>
        public string NumberFormat { get; set; } = "0000";

        /// <summary>
        /// 如果需要从关联表获取分组键，指定关联表名称
        /// </summary>
        public string? JoinTableName { get; set; }

        /// <summary>
        /// 关联字段名称（例如：orderId）
        /// </summary>
        public string? JoinFieldName { get; set; }

        /// <summary>
        /// 从关联表中获取的分组字段名称（例如：shippedDate）
        /// </summary>
        public string? JoinGroupByFieldName { get; set; }
    }

    /// <summary>
    /// 序列号配置集合 - 定义所有表的序列号规则
    /// </summary>
    public static class SequenceNumberConfigs
    {
        /// <summary>
        /// 获取所有序列号配置
        /// </summary>
        public static Dictionary<string, SequenceNumberConfig> GetConfigs()
        {
            return new Dictionary<string, SequenceNumberConfig>(StringComparer.OrdinalIgnoreCase)
            {
                // Project 表：根据 customerId 分组编号
                ["Project"] = new SequenceNumberConfig
                {
                    TableName = "Project",
                    NumberFieldName = "number",
                    GroupByFieldName = "customerId",
                    NumberFormat = "0000"
                },

                // Order 表：根据 shippedDate 分组编号
                ["Order"] = new SequenceNumberConfig
                {
                    TableName = "Order",
                    NumberFieldName = "number",
                    GroupByFieldName = "shippedDate",
                    NumberFormat = "00"
                },

                // ShippingOrder 表：根据关联的 Order.shippedDate 分组编号
                ["ShippingOrder"] = new SequenceNumberConfig
                {
                    TableName = "ShippingOrder",
                    NumberFieldName = "number",
                    GroupByFieldName = "orderId",  // 本表的外键字段
                    NumberFormat = "0000",
                    JoinTableName = "Order",       // 关联表
                    JoinFieldName = "id",          // 关联表的主键
                    JoinGroupByFieldName = "shippedDate"  // 从关联表获取的分组字段
                }
            };
        }

        /// <summary>
        /// 获取指定表的配置
        /// </summary>
        public static SequenceNumberConfig? GetConfig(string tableName)
        {
            var configs = GetConfigs();
            return configs.TryGetValue(tableName, out var config) ? config : null;
        }

        /// <summary>
        /// 检查表是否需要自动编号
        /// </summary>
        public static bool HasAutoNumber(string tableName)
        {
            return GetConfigs().ContainsKey(tableName);
        }
    }
}

