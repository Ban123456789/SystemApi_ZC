using System.ComponentModel.DataAnnotations;

namespace Accura_MES.Controllers.Models.MO
{
    /// <summary>
    /// 編輯輸入料件的模型
    /// </summary>
    public class UpdateMOInputRequestBody
    {
        /// <summary>
        /// 輸入料件 ID
        /// </summary>
        [Required]
        public long? Id { get; set; }
        /// <summary>
        /// 製令 ID
        /// </summary>
        [Required]
        public long? MOId { get; set; }
        /// <summary>
        /// 料件 ID
        /// </summary>
        public long? PartId { get; set; } = null;
        /// <summary>
        /// 總數量
        /// </summary>
        public decimal? Quantity { get; set; } = null;
        /// <summary>
        /// 物料狀況
        /// </summary>
        public string? FieldStatus { get; set; } = null;
    }
}
