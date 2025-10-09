using System.ComponentModel.DataAnnotations;

namespace Accura_MES.Controllers.Models.MO
{
    /// <summary>
    /// 建立輸入料件的模型
    /// </summary>
    public class CreateMOInputRequestBody
    {
        /// <summary>
        /// 製令 ID
        /// </summary>
        [Required]
        public long? MOId { get; set; }
        /// <summary>
        /// 料件 ID
        /// </summary>
        [Required]
        public long? PartId { get; set; }
        /// <summary>
        /// 總數量
        /// </summary>
        public decimal? Quantity { get; set; } = 0;
        /// <summary>
        /// 物料狀況
        /// </summary>
        public string? FieldStatus { get; set; } = string.Empty;
    }
}
