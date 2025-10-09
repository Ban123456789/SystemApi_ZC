
namespace Accura_MES.Utilities
{
    using Accura_MES.Services;
    using Serilog;
    using System.Collections.Generic;

    public class LogUtility
    {
        private readonly Dictionary<Enum, string> _actionDescriptions;
        private readonly ILogger _logger;
        private readonly string _mainAction;

        /// <summary>
        /// 通用的日誌記錄方法。
        /// log 格式 : [mainAction][action]logstring+format(datas)。
        /// </summary>
        /// <param name="Level"></param>
        /// <param name="action"></param>
        /// <param name="logstring"></param>
        /// <param name="datas"></param>
        /// <remarks>
        /// 如果輸入的 Action ，不存在於依賴注入的 _actionDescriptions 字典中，方法會將 actionDescription 設為 "未知動作"
        /// </remarks>
        public void WriteLog(LogLevel Level, Enum action, string logstring, params object?[] datas)
        {
            string actionDescription = _actionDescriptions.ContainsKey(action) ? _actionDescriptions[action] : "未知動作";

            string log = $"[{_mainAction}][{actionDescription}] {logstring}";

            switch (Level)
            {
                case LogLevel.Information:
                    _logger.Information(log, datas);
                    break;
                case LogLevel.Warning:
                    _logger.Warning(log, datas);
                    break;
                case LogLevel.Error:
                    _logger.Error(log, datas);
                    break;
                case LogLevel.Critical:
                    _logger.Fatal(log, datas);
                    break;
                default:
                    _logger.Debug(log, datas);
                    break;
            }
        }
    }


}
