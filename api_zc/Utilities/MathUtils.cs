using System.Diagnostics;

namespace Accura_MES.Utilities
{
    /// <summary>
    /// 集中管理算數
    /// </summary>
    public class MathUtils
    {
        /// <summary>
        /// 公用除法工具
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns>0 if <paramref name="b"/> is zero; (<paramref name="a"/> / <paramref name="b"/>) if otherwise</returns>
        /// <exception cref="ArgumentException"></exception>
        public static decimal Divide<T>(T a, T b)
        {
            try
            {
                var decimalA = Convert.ToDecimal(a);
                var decimalB = Convert.ToDecimal(b);

                if (decimalB == 0)
                {
                    throw new DivideByZeroException("除數不能為零。");
                }

                return decimalA / decimalB;
            }
            catch (DivideByZeroException ex)
            {
                Debug.WriteLine(ex);

                // 返回 0，不要返回例外
                return 0;
            }
            catch (InvalidCastException)
            {
                throw new ArgumentException("輸入的值必須是可轉換為數字的類型。");
            }
            catch (FormatException)
            {
                throw new ArgumentException("輸入的格式無法轉換為數字。");
            }
        }
    }
}
