using Microsoft.Data.SqlClient;

namespace Accura_MES.Utilities
{
    /// <summary>
    /// 通用例外處理公用工具
    /// </summary>
    public static class ExceptionHandler
    {
        /// <summary>
        /// 通用的處理例外方法，並可幫忙回滾交易
        /// <para></para>
        /// 將例外包裝成 ResponseObject
        /// </summary>
        /// <param name="exception"></param>
        /// <param name="transaction"></param>
        /// <remarks>
        /// 1.自訂例外, 2.SQL例外, 3.其餘例外
        /// </remarks>
        /// <returns></returns>
        public static async Task<ResponseObject> HandleExceptionAsync(
            Exception exception, SqlTransaction? transaction = null)
        {
            var responseObject = new ResponseObject().GenerateEntity(
                SelfErrorCode.INTERNAL_SERVER_WITH_MSG, new List<object>(), new List<object>(),
                "[通用例外處理方法] 發生未知例外");

            try
            {
                // 需要的話回滾交易
                if (transaction?.Connection != null)
                {
                    await transaction.RollbackAsync();
                    await transaction.DisposeAsync();
                }

                // 處理例外
                ProcessException(exception, responseObject);

                // 處理完返回
                return responseObject;
            }
            catch (Exception ex)
            {
                responseObject.SetErrorCodeWithStackTrace(
                    SelfErrorCode.INTERNAL_SERVER_WITH_MSG, ex.ToString(), null, $"[通用例外處理方法] 發生系統例外");

                return responseObject;
            }
        }


        /// <summary>
        /// 通用的處理例外方法，並可幫忙回滾交易
        /// <para></para>
        /// 將例外包裝成 ResponseObject
        /// </summary>
        /// <param name="exception"></param>
        /// <param name="transaction"></param>
        /// <remarks>
        /// 1.自訂例外, 2.SQL例外, 3.其餘例外
        /// </remarks>
        /// <returns></returns>
        public static ResponseObject HandleException(
            Exception exception, SqlTransaction? transaction = null)
        {
            var responseObject = new ResponseObject().GenerateEntity(
                SelfErrorCode.INTERNAL_SERVER_WITH_MSG, new List<object>(), new List<object>(),
                "[通用例外處理方法] 發生未知例外");

            try
            {
                // 需要的話回滾交易
                if (transaction?.Connection != null)
                {
                    transaction.Rollback();
                    transaction.Dispose();
                }

                // 處理例外
                ProcessException(exception, responseObject);

                // 處理完返回
                return responseObject;
            }
            catch (Exception ex)
            {
                responseObject.SetErrorCodeWithStackTrace(
                    SelfErrorCode.INTERNAL_SERVER_WITH_MSG, ex.ToString(), null, $"[通用例外處理方法] 發生系統例外");

                return responseObject;
            }
        }

        /// <summary>
        /// Handles an exception asynchronously, optionally rolling back a SQL transaction, and returns a failure
        /// response object.
        /// </summary>
        /// <remarks>This method provides a centralized mechanism for handling exceptions, including
        /// optional transaction rollback and error response creation. If a secondary exception occurs during the
        /// handling process, it will be captured in the returned response object with additional error
        /// details.</remarks>
        /// <typeparam name="TData">The type of data contained in the response object.</typeparam>
        /// <param name="exception">The exception to handle. Cannot be <see langword="null"/>.</param>
        /// <param name="transaction">An optional <see cref="SqlTransaction"/> to roll back if it is active. If provided and connected, the
        /// transaction will be rolled back and disposed.</param>
        /// <returns>A <see cref="ResponseObject{TData}"/> representing a failure response. The response includes error details
        /// based on the handled exception.</returns>
        public static async Task<ResponseObject<TData>> HandleExceptionAsync<TData>(
            Exception exception, SqlTransaction? transaction = null)
        {

            var responseObject = ResponseObjectFactory.CreateFail<TData>(default, "[通用例外處理方法] 發生未知例外");
            try
            {
                // 需要的話回滾交易
                if (transaction?.Connection != null)
                {
                    await transaction.RollbackAsync();
                    await transaction.DisposeAsync();
                }

                // 處理例外
                ProcessException(exception, responseObject);

                // 處理完返回
                return responseObject;
            }
            catch (Exception ex)
            {
                responseObject.SetErrorCodeWithStackTrace(
                    SelfErrorCode.INTERNAL_SERVER_WITH_MSG, ex.ToString(), null, $"[通用例外處理方法] 發生系統例外");

                return responseObject;
            }
        }

        /// <summary>
        /// 處理例外，並製作 ResponseObject
        /// </summary>
        /// <param name="exception"></param>
        /// <param name="responseObject"></param>
        /// <remarks>
        /// Helper of 
        /// <see cref="HandleExceptionAsync(Exception, SqlTransaction?)"/>
        /// and <see cref="HandleException(Exception, SqlTransaction?)"/>
        /// </remarks>
        private static void ProcessException(Exception exception, ResponseObject responseObject)
        {
            switch (exception)
            {
                // 自訂例外
                case CustomErrorCodeException customErrorCodeException:
                    // 防呆: 該error code須以特殊格式返回前端
                    // 因為前端會讀取 ResponseObject.ErrorData 裡面的內容
                    if (customErrorCodeException.SelfErrorCode == SelfErrorCode.NESTED_STRUCTURE_ERROR)
                    {
                        var errorDetails = new List<object>
                            {
                                new
                                {
                                    code = $"??",
                                    rowIndex = 0,
                                    errorData = new List<string>() { },
                                    message = $"未處理之巢狀錯誤，這個 error code 不允許直接 throw，否則會發生這個狀況，" +
                                    $"{customErrorCodeException.ToString()}"
                                }
                            };

                        responseObject.ErrorData = errorDetails;

                        responseObject.SetErrorCodeWithStackTrace(
                        customErrorCodeException.SelfErrorCode,
                        $"建議:請將錯誤包裝成 ResponseObject，並將錯誤資訊寫入 ErrorData，最後直接回傳");

                    }
                    // 其餘自訂例外
                    else
                    {
                        responseObject.SetErrorCodeWithStackTrace(
                            customErrorCodeException.SelfErrorCode,
                            customErrorCodeException.ToString(),
                            null,
                            $"發生自訂例外: {customErrorCodeException.Message}");

                        responseObject.ErrorData = customErrorCodeException.ErrorData ?? new List<object>();  // 自訂例外可能會有 error data
                    }
                    break;

                // SQL例外
                case SqlException sqlException:

                    // 將原始 SQL 錯誤翻譯為用戶友好的訊息
                    string friendlyMessage = SqlErrorTranslator.Translate(sqlException);

                    responseObject.SetErrorCodeWithStackTrace(
                        SelfErrorCode.SQL_ERROR,
                        sqlException.ToString(),
                        sqlException,
                        $"發生SQL例外: 友好訊息:[{friendlyMessage}]");
                    break;

                // 其餘例外
                default:
                    responseObject.SetErrorCodeWithStackTrace(
                        SelfErrorCode.TRY_CATCH_ERROR,
                        exception.ToString(),
                        null,
                        $"發生系統例外: {exception.Message}");
                    break;
            }
        }
    }

}
