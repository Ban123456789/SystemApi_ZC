using Accura_MES.Utilities;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Accura_MES.Controllers.Models.Track
{
    /// <summary>
    /// 完工製令回報的模型
    /// </summary>
    public class FinishRequestBody
    {
        /// <summary>
        /// 製令 ID
        /// </summary>
        [Required]
        public long Id { get; set; }
        /// <summary>
        /// 回報時間
        /// </summary>
        [Required]
        [JsonConverter(typeof(CustomDateTimeConverter))]
        public DateTime ReportTime { get; set; }
        /// <summary>
        /// 不良數量
        /// </summary>
        public decimal FaultyQuantity { get; set; } = 0;
        /// <summary>
        /// 不良原因
        /// </summary>
        public string? FaultyReason { get; set; }
        /// <summary>
        /// 回報原因
        /// </summary>
        public string? ReportReason { get; set; }
        /// <summary>
        /// 穴數
        /// </summary>
        public int HoleQuantity { get; set; } = 0;
        /// <summary>
        /// 回報數量
        /// </summary>
        public decimal ReportQuantity { get; set; } = 0;
        /// <summary>
        /// 生產工時
        /// </summary>
        public decimal TotalProductionTime { get; set; } = 0;
        /// <summary>
        /// 單次平均生產週期時間
        /// </summary>
        public double AverageProductionTime { get; set; } = 0;
        /// <summary>
        /// 機器次數
        /// </summary>
        public string? PartCount { get; set; }
        /// <summary>
        /// 單顆生產時間
        /// </summary>
        public decimal? SingleTime { get; set; } = 0;
        /// <summary>
        /// 備註
        /// </summary>
        public string? Remark { get; set; } = string.Empty;

    }
}
