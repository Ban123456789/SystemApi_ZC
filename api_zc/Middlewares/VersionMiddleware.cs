using Accura_MES.Services;

namespace Accura_MES.Middlewares
{
    /// <summary>
    /// 版本中介軟體
    /// </summary>
    public class VersionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly string _version;

        /// <summary>
        /// 建構子
        /// </summary>
        /// <param name="next"></param>
        /// <param name="configuration"></param>
        public VersionMiddleware(RequestDelegate next, IConfiguration configuration)
        {
            _next = next;
            _version = configuration["Version"] ?? "0.0.0"; // 找不到時回傳預設版本
        }

        /// <summary>
        /// 執行中介軟體
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task InvokeAsync(HttpContext context)
        {
            // 添加版本號到回應標頭
            context.Response.OnStarting(() =>
            {
                context.Response.Headers["ACCURA_MES_API_Version"] = _version;
                return Task.CompletedTask;
            });

            await _next(context);
        }
    }

}
