using Accura_MES.Utilities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace Accura_MES.Extensions
{
    public static class ControllerExtensions
    {
        /// <summary>
        /// 通用的處理例外方法
        /// </summary>
        /// <param name="controller"></param>
        /// <param name="transaction"></param>
        /// <param name="ex"></param>
        /// <returns></returns>
        [Obsolete("use ExceptionHandler.HandleException() instead")]
        public static IActionResult HandleAccuraException(this ControllerBase controller, SqlTransaction? transaction, Exception ex)
        {
            // 回滾交易
            if (transaction != null)
            {
                transaction.Rollback();
                transaction.Dispose();
            }

            // 自訂Exception
            if (ex is CustomErrorCodeException customErrCodeEx)
            {
                // 該error code須以特殊格式返回前端
                if (customErrCodeEx.SelfErrorCode == SelfErrorCode.NESTED_STRUCTURE_ERROR)
                {
                    var errorDetails = new List<object>
                    {
                        new
                        {
                            code = $"??",
                            rowIndex = 0,
                            errorData = new List<string>() { },
                            message = $"未處理之巢狀錯誤，這個error code不允許直接throw，否則會發生這個狀況，"
                        }
                    };

                    return controller.CustomAccuraResponse(customErrCodeEx.SelfErrorCode, null, errorDetails, "建議:請修改成呼叫CustomAccuraResponse(...)回傳");
                }

                // 回傳自訂errorCode、errorData、message
                return controller.CustomAccuraResponse(customErrCodeEx.SelfErrorCode, null, customErrCodeEx.ErrorData, customErrCodeEx.ToString());
            }

            else if (ex is SqlException sqlEx)
            {
                // 將原始 SQL 錯誤翻譯為用戶友好的訊息
                string friendlyMessage = SqlErrorTranslator.Translate(sqlEx);

                var result = new ResponseObject().GenerateEntity(SelfErrorCode.SQL_ERROR, new List<string>(), new List<string>(), sqlEx, friendlyMessage);
                return controller.StatusCode(int.Parse(result.Code.Split("-")[0]), result);
            }

            else
            {
                return controller.CustomAccuraResponse(SelfErrorCode.TRY_CATCH_ERROR, null, null, ex.ToString());
            }
        }

        /// <summary>
        /// 通用的處理例外方法
        /// </summary>
        /// <param name="controller"></param>
        /// <param name="transaction"></param>
        /// <param name="ex"></param>
        /// <returns></returns>
        [Obsolete("use ExceptionHandler.HandleExceptionAsync() instead")]
        public static async Task<IActionResult> HandleAccuraExceptionAsync(this ControllerBase controller, SqlTransaction? transaction, Exception ex)
        {
            // 回滾交易
            if (transaction != null)
            {
                await transaction.RollbackAsync();
                transaction.Dispose();
            }

            // 自訂Exception
            if (ex is CustomErrorCodeException customErrCodeEx)
            {
                // 該error code須以特殊格式返回前端
                if (customErrCodeEx.SelfErrorCode == SelfErrorCode.NESTED_STRUCTURE_ERROR)
                {
                    var errorDetails = new List<object>
                    {
                        new
                        {
                            code = $"??",
                            rowIndex = 0,
                            errorData = new List<string>() { },
                            message = $"未處理之巢狀錯誤，這個error code不允許直接throw，否則會發生這個狀況."
                        }
                    };

                    return controller.CustomAccuraResponse(customErrCodeEx.SelfErrorCode, null, errorDetails, "建議:請修改成呼叫CustomAccuraResponse(...)回傳");
                }

                // 回傳自訂errorCode、errorData、message
                return controller.CustomAccuraResponse(customErrCodeEx.SelfErrorCode, null, customErrCodeEx.ErrorData, customErrCodeEx.ToString());
            }

            else if (ex is SqlException sqlEx)
            {
                // 將原始 SQL 錯誤翻譯為用戶友好的訊息
                string friendlyMessage = SqlErrorTranslator.Translate(sqlEx);

                var result = new ResponseObject().GenerateEntity(SelfErrorCode.SQL_ERROR, new List<string>(), new List<string>(), sqlEx, friendlyMessage);
                return controller.StatusCode(int.Parse(result.Code.Split("-")[0]), result);
            }

            else
            {
                return controller.CustomAccuraResponse(SelfErrorCode.TRY_CATCH_ERROR, null, null, ex.ToString());
            }
        }

        /// <summary>
        /// 自訂error code的返回方法
        /// </summary>
        /// <param name="controller"></param>
        /// <param name="data"></param>
        /// <param name="errData"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public static IActionResult CustomAccuraResponse(this ControllerBase controller, SelfErrorCode selfErrorCode, object? data = null, object? errData = null, string message = "")
        {
            // 回傳預設格式
            data ??= new List<object>();
            errData ??= new List<object>();

            ResponseObject result = new ResponseObject().GenerateEntity(selfErrorCode, data, errData, message);
            return controller.StatusCode(int.Parse(result.Code.Split("-")[0]), result);
        }

        /// <summary>
        /// 接收 ResponseObject 的返回方法
        /// </summary>
        /// <param name="controller"></param>
        /// <param name="responseObject"></param>
        /// <returns></returns>
        public static IActionResult CustomAccuraResponse(this ControllerBase controller, ResponseObject responseObject)
        {
            return controller.StatusCode(int.Parse(responseObject.Code.Split("-")[0]), responseObject);
        }

        /// <summary>
        /// 接收 ResponseObject 的返回方法
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="controller"></param>
        /// <param name="responseObject"></param>
        /// <returns></returns>
        public static ActionResult<T> CustomAccuraResponse<T>(this ControllerBase controller, ResponseObject responseObject)
        {
            return controller.StatusCode(int.Parse(responseObject.Code.Split("-")[0]), responseObject);
        }

        /// <summary>
        /// 通用的成功返回方法
        /// </summary>
        public static IActionResult SuccessAccuraResponse(this ControllerBase controller, object? data = null, object? errData = null, string message = "")
        {
            // 回傳預設格式
            data ??= new List<object>();
            errData ??= new List<object>();

            ResponseObject result = new ResponseObject().GenerateEntity(SelfErrorCode.SUCCESS, data, errData, message);
            return controller.StatusCode(int.Parse(result.Code.Split("-")[0]), result);
        }



    } // end of class ControllerExtensions
}
