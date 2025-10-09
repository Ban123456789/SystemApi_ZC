using System.ComponentModel.DataAnnotations;

namespace Accura_MES.Controllers.Models.MO
{
    /// <summary>
    /// 排程頁面的請求體模型
    /// </summary>
    public class GetProductionSchedulingRequsestBody
    {
        /// <summary>
        /// 要排程的機台[machine]/線程[thread]
        /// </summary>
        [Required]
        public string? SourceType { get; set; } = null;
        /// <summary>
        /// 要排程的機台[machine]/線程[thread] 編號
        /// </summary>
        [Required]
        public string? SourceNumber { get; set; } = null;
        /// <summary>
        /// 要排程的日期
        /// </summary>
        [Required]
        public DateTime? Date { get; set; } = null;
    }
}
