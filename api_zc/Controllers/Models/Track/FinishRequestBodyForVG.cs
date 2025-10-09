using System.ComponentModel.DataAnnotations;

namespace Accura_MES.Controllers.Models.Track
{
    /// <summary>
    /// 完工製令回報的模型
    /// </summary>
    /// <remarks>
    /// 立墩版
    /// </remarks>
    public class FinishRequestBodyForVG
    {
        /// <summary>
        /// 製令 ID
        /// </summary>
        [Required]
        public long MOId { get; set; }
    }
}
