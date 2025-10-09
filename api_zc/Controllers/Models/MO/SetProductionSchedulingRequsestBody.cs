using System.ComponentModel.DataAnnotations;

namespace Accura_MES.Controllers.Models.MO
{
    /// <summary>
    /// 排程設定的請求體模型
    /// </summary>
    public class SetProductionSchedulingRequsestBody : GetProductionSchedulingRequsestBody
    {
        /// <summary>
        /// 不能更改排程的製令，放的是製令 ID
        /// </summary>
        public List<long>? NsortMo { get; set; } = null;
        /// <summary>
        /// 排程製令陣列(前端要排除不能更改排程的製令)，放的是製令 ID
        /// </summary>
        public List<long>? SortMo { get; set; } = null;
    }
}
