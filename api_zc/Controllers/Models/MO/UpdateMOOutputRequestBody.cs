using System.ComponentModel.DataAnnotations;

namespace Accura_MES.Controllers.Models.MO
{
    /// <summary>
    /// 編輯輸出料件的模型
    /// </summary>
    public class UpdateMOOutputRequestBody
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
        /// 基數
        /// </summary>
        public decimal? BaseQuantity { get; set; } = null;
        /// <summary>
        /// 是否廢棄物
        /// </summary>
        public bool? Waste { get; set; } = null;
        /// <summary>
        /// 定容量
        /// </summary>
        public decimal? FixedCapacity { get; set; } = null;
    }
}
