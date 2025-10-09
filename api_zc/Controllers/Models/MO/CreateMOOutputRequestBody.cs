using System.ComponentModel.DataAnnotations;

namespace Accura_MES.Controllers.Models.MO
{
    /// <summary>
    /// 建立輸出料件的模型
    /// </summary>
    public class CreateMOOutputRequestBody
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
        /// 基數
        /// </summary>
        public decimal? BaseQuantity { get; set; } = 0;
        /// <summary>
        /// 產出數量
        /// </summary>
        public decimal? OutPutQuantity { get; set; } = 0;
        /// <summary>
        /// 廢棄物
        /// </summary>
        public bool? Waste { get; set; } = false;
        /// <summary>
        /// 定容量
        /// </summary>
        public decimal? FixedCapacity { get; set; } = 0;
    }
}
