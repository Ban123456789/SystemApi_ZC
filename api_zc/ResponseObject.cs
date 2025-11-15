using Microsoft.Data.SqlClient;
using System.Reflection;
namespace Accura_MES
{
    public class ResponseObject
    {
        /// <summary>
        /// 錯誤碼
        /// </summary>
        public string Code { get; set; }

        /// <summary>
        /// 成功時放的資源
        /// </summary>
        public object? Data { get; set; }

        /// <summary>
        /// 錯誤碼映射的名稱
        /// </summary>
        public string? ErrorCode { get; set; }

        /// <summary>
        /// 錯誤時放的資源
        /// </summary>
        public object? ErrorData { get; set; }

        /// <summary>
        /// 訊息
        /// </summary>
        public string? Message { get; set; }

        /// <summary>
        /// 成功與否
        /// </summary>
        public bool Success { get; set; }

        public string? StackTrace { get; set; }

        /// <summary>
        /// 接收 SelfErrorCode 來設定狀態碼與訊息
        /// </summary>
        /// <param name="errorCode"></param>
        /// <param name="messageArgs">填充字串</param>
        /// <returns></returns>
        public ResponseObject GenerateEntity(SelfErrorCode errorCode)
        {
            int code = int.Parse(errorCode.Code.Split("-")[0]);
            string message = errorCode.GetFormattedMessage();
            // 設定狀態碼與訊息
            Code = errorCode.Code;
            Message = message;
            Success = code >= 200 && code < 300; // 2xx 都是成功
            ResponseObject response = new ResponseObject
            {
                Code = Code,
                Message = Message,
                Success = Success,
                Data = "",
                ErrorCode = errorCode.GetErrorCodeName(),
                ErrorData = ""
            };
            return response;
        }

        /// <summary>
        /// 接收 SelfErrorCode 來設定狀態碼與訊息
        /// </summary>
        /// <param name="errorCode"></param>
        /// <param name="data"></param>
        /// <param name="errData"></param>
        /// <param name="messageArgs">填充字串</param>
        /// <returns></returns>
        public ResponseObject GenerateEntity(SelfErrorCode errorCode, object? data, object? errData)
        {
            int code = int.Parse(errorCode.Code.Split("-")[0]);
            string message = errorCode.GetFormattedMessage();
            // 設定狀態碼與訊息
            Code = errorCode.Code;
            Message = message;
            Success = code >= 200 && code < 300; // 2xx 都是成功
            ResponseObject response = new ResponseObject
            {
                Code = Code,
                Message = Message,
                Success = Success,
                Data = data,
                ErrorCode = errorCode.GetErrorCodeName(),
                ErrorData = errData
            };
            return response;
        }

        /// <summary>
        /// 接收 SelfErrorCode 來設定狀態碼與訊息
        /// </summary>
        /// <param name="errorCode"></param>
        /// <param name="data"></param>
        /// <param name="errData"></param>
        /// <param name="messageArgs">填充字串</param>
        /// <returns></returns>
        public ResponseObject GenerateEntity(SelfErrorCode errorCode, object? data, object? errData, params object[] messageArgs)
        {
            int code = int.Parse(errorCode.Code.Split("-")[0]);
            string message = errorCode.GetFormattedMessage(messageArgs);

            // 設定狀態碼與訊息
            Code = errorCode.Code;
            Message = message;
            Success = code >= 200 && code < 300; // 2xx 都是成功
            ResponseObject response = new ResponseObject
            {
                Code = Code,
                Message = Message,
                Success = Success,
                Data = data,
                ErrorCode = errorCode.GetErrorCodeName(),
                ErrorData = errData
            };


            return response;
        }

        /// <summary>
        /// sql exception時呼叫。
        /// error code格式設定成400-500-{0}，會把 SqlException number 填入 Code
        /// </summary>
        /// <param name="errorCode"></param>
        /// <param name="data"></param>
        /// <param name="errData"></param>
        /// <param name="sqlEx"></param>
        /// <param name="messageArgs"></param>
        /// <returns></returns>
        public ResponseObject GenerateEntity(SelfErrorCode errorCode, object? data, object? errData, SqlException sqlEx, params object[] messageArgs)
        {
            int code = int.Parse(errorCode.Code.Split("-")[0]);
            string message = errorCode.GetFormattedMessage(messageArgs);

            // 設定狀態碼與訊息
            Code = errorCode.GetFormattedSqlCode(sqlEx.Number);    // 寫入 sql 例外編號
            Message = message;
            Success = code >= 200 && code < 300; // 2xx 都是成功
            ResponseObject response = new ResponseObject
            {
                Code = Code,
                Message = Message,
                Success = Success,
                Data = data,
                ErrorCode = errorCode.GetErrorCodeName(),
                ErrorData = errData
            };


            return response;
        }


        /// <summary>
        /// 設定錯誤碼、成功狀態、訊息
        /// </summary>
        /// <param name="errorCode">錯誤碼</param>
        /// <param name="message">訊息</param>
        public void SetErrorCode(SelfErrorCode errorCode, params string[] message)
        {
            int code = int.Parse(errorCode.Code.Split("-")[0]);

            this.Code = errorCode.Code;
            this.ErrorCode = errorCode.GetErrorCodeName();
            this.Success = code >= 200 && code < 300; // 2xx 都是成功
            this.Message = errorCode.GetFormattedMessage(message);
        }

        /// <summary>
        /// 設定錯誤碼、成功狀態、訊息
        /// <para></para>
        /// 可以將 SQL 編號寫入 Code
        /// </summary>
        /// <param name="errorCode">錯誤碼</param>
        /// <param name="message">訊息</param>
        public void SetErrorCodeWithStackTrace(SelfErrorCode errorCode, string stackTrace, SqlException? sqlEx = null, params string[] message)
        {
            int code = int.Parse(errorCode.Code.Split("-")[0]);

            if (sqlEx is not null)
                this.Code = errorCode.GetFormattedSqlCode(sqlEx.Number);    // 寫入 sql 例外編號
            else
                this.Code = errorCode.Code;

            this.ErrorCode = errorCode.GetErrorCodeName();
            this.Success = code >= 200 && code < 300; // 2xx 都是成功
            this.Message = errorCode.GetFormattedMessage(message);
            this.StackTrace = stackTrace;
        }

        /// <summary>
        /// 設定錯誤碼、成功狀態、訊息
        /// <para></para>
        /// 會將 SQL 編號寫入 Code
        /// </summary>
        /// <param name="errorCode"></param>
        /// <param name="sqlEx"></param>
        /// <param name="message"></param>
        public void SetErrorCode(SelfErrorCode errorCode, SqlException sqlEx, params string[] message)
        {
            int code = int.Parse(errorCode.Code.Split("-")[0]);

            this.Code = errorCode.GetFormattedSqlCode(sqlEx.Number);    // 寫入 sql 例外編號
            this.ErrorCode = errorCode.GetErrorCodeName();
            this.Success = code >= 200 && code < 300; // 2xx 都是成功
            this.Message = errorCode.GetFormattedMessage(message);
        }

        /// <summary>
        /// 根據錯誤碼取得對應的 selfErrorCode
        /// </summary>
        public SelfErrorCode GetSelfErrorCode()
        {
            // 使用反射來找出 SelfErrorCode 的名稱
            var fields = typeof(SelfErrorCode).GetFields(BindingFlags.Public | BindingFlags.Static);

            // 遍歷所有靜態欄位，尋找與當前錯誤碼名稱相符的 SelfErrorCode
            foreach (var field in fields)
            {
                var fieldValue = field.GetValue(null) as SelfErrorCode;
                if (fieldValue != null && fieldValue.GetErrorCodeName() == this.ErrorCode)
                {
                    return fieldValue; // 找到對應的錯誤碼，返回
                }
            }

            return SelfErrorCode.INTERNAL_SERVER_WITH_MSG; // 如果找不到對應的錯誤碼，返回預設的錯誤碼
        }
    }


    /// <summary>
    /// 回應物件，繼承自 ResponseObject，並指定資料類型 TData
    /// </summary>
    /// <typeparam name="TData"></typeparam>
    public class ResponseObject<TData> : ResponseObject
    {
        /// <summary>
        /// 資料部分，繼承自 ResponseObject，並指定 TData 類型
        /// </summary>
        public new TData? Data
        {
            get
            {
                if (base.Data == null) return default;
                if (base.Data is TData value) return value;
                throw new InvalidCastException($"Stored Data is not of type {typeof(TData).Name}.");
            }

            set => base.Data = value;
        }
    }

    /// <summary>
    /// Provides factory methods for creating instances of <see cref="ResponseObject{TData}"/>  with predefined success
    /// or failure states.
    /// </summary>
    /// <remarks>This class simplifies the creation of <see cref="ResponseObject{TData}"/> instances by 
    /// encapsulating common patterns for success and failure responses. It supports setting  error codes, messages, and
    /// optional data or error data.</remarks>
    public static class ResponseObjectFactory
    {
        /// <summary>
        /// 建立一個成功的回應物件，並設定資料
        /// </summary>
        /// <param name="data"></param>
        /// <remarks>
        /// Code 為 200 SUCCESS，表示操作成功。
        /// </remarks>
        /// <returns></returns>
        public static ResponseObject<TData> CreateSuccess<TData>(TData? data = default)
        {
            var response = new ResponseObject<TData>();

            response.SetErrorCode(SelfErrorCode.SUCCESS);
            response.Data = data;

            return response;
        }

        /// <summary>
        /// 建立一個失敗的回應物件，並設定錯誤資料與錯誤訊息
        /// </summary>
        /// <param name="errorData"></param>
        /// <param name="message"></param>
        /// <remarks>
        /// Code 為 500-6 INTERNAL_SERVER_WITH_MSG，表示內部伺服器錯誤，並且會帶有自訂訊息。
        /// </remarks>
        /// <returns></returns>
        public static ResponseObject<TData> CreateFail<TData>(object? errorData = default, params string[] message)
        {
            var response = new ResponseObject<TData>();

            response.SetErrorCode(SelfErrorCode.INTERNAL_SERVER_WITH_MSG, message);
            response.ErrorData = errorData;

            return response;
        }

        /// <summary>
        /// 建立一個動態的回應物件，並設定資料與錯誤訊息
        /// </summary>
        /// <param name="errorCode"></param>
        /// <param name="message"></param>
        /// <param name="data"></param>
        /// <param name="stackTrace"></param>
        /// <param name="errorData"></param>
        /// <remarks>
        /// Code 為指定的錯誤碼，並且可以帶有自訂訊息。
        /// </remarks>
        /// <returns></returns>
        public static ResponseObject<TData> Create<TData>(
            SelfErrorCode errorCode,
            TData? data = default,
            object? errorData = default,
            string? stackTrace = null, params string[] message)
        {
            var response = new ResponseObject<TData>();

            response.SetErrorCode(errorCode, message);
            response.Data = data;
            response.ErrorData = errorData;
            response.StackTrace = stackTrace;

            return response;
        }

        /// <summary>
        /// 將 Base ResponseObject 轉換為指定資料類型的 ResponseObject
        /// </summary>
        /// <typeparam name="TData"></typeparam>
        /// <param name="responseObject"></param>
        /// <returns></returns>
        public static ResponseObject<TData> TransformError<TData>(ResponseObject responseObject)
        {
            var response = Create<TData>(
                responseObject.GetSelfErrorCode(), default, responseObject.ErrorData, responseObject.StackTrace, responseObject.Message);

            return response;
        }
    }

    /// <summary>
    /// Represents a standardized error code and message used for API responses and error handling.
    /// </summary>
    /// <remarks>
    /// The <see cref="SelfErrorCode"/> class provides a collection of predefined error codes and
    /// messages that can be used to represent various error conditions in an application.
    /// Each error code is associated with a unique string identifier and a descriptive message.
    /// </remarks>
    public class SelfErrorCode
    {
        public string Code { get; }
        public string Message { get; }

        private SelfErrorCode(string code, string message)
        {
            Code = code;
            Message = message;
        }

        // 靜態的錯誤碼定義(有-號代表自訂義)
        public static readonly SelfErrorCode SUCCESS = new SelfErrorCode("200", "Success");
        public static readonly SelfErrorCode BAD_REQUEST = new SelfErrorCode("400", "Bad Request");
        public static readonly SelfErrorCode NOT_FOUND = new SelfErrorCode("404", "Not Found");
        public static readonly SelfErrorCode METHOD_NOT_ALLOWED = new SelfErrorCode("405", "Method Not Allowed");
        public static readonly SelfErrorCode INTERNAL_SERVER_ERROR = new SelfErrorCode("500", "Internal server error");


        #region ==400==
        /// <summary>
        /// Header缺失Database屬性
        /// </summary>
        public static readonly SelfErrorCode MISSING_DATABASE = new SelfErrorCode("400-1", "Missing 'Database' property in the request header");
        /// <summary>
        /// use all eidt 返回id是必填未輸入
        /// </summary>
        public static readonly SelfErrorCode MISSING_ID = new SelfErrorCode("400-3", "Missing id field");
        /// <summary>
        /// use all isDelete 返回id、isDelete是必填未輸入
        /// </summary>
        public static readonly SelfErrorCode MISSING_ISDELETE = new SelfErrorCode("400-4", "Missing id or isDelete field");
        /// <summary>
        /// use user datasheep 返回密碼錯誤
        /// </summary>
        public static readonly SelfErrorCode WRONG_PASSWORD = new SelfErrorCode("400-5", "Password is error");
        /// <summary>
        ///   檢查用:當API沒有接收到任何檔案
        /// </summary>
        public static readonly SelfErrorCode MISSING_FILE = new SelfErrorCode("400-6", "No file uploaded");
        // =====特殊回傳=====
        // 特殊回傳綜合 : 回傳格式如範例
        /*
           此狀態須以特定格式回傳給前端:
           舉例:
            {
                "code": "400-100",
                "data": "",
                "errorCode": "MISSING_PARAMETERS",
                "errorData": [
                    {
                        "code":400-7,
                        "rowIndex": 1,
                        "errorData": [
                            "phoneNumber"
                        ],
                        "message": "第 1 筆資料缺失必要欄位: phoneNumber"
                    },
                    {
                        "code":400-8,
                        "rowIndex": 2,
                        "errorData": [
                            "sourceId"
                        ],
                        "message": "第 2 筆資料錯誤，該欄位不允許建立: sourceId""
                    }
                ],
                "message": "詳情請看error data",
                "success": false
            }
        */
        public static readonly SelfErrorCode NESTED_STRUCTURE_ERROR = new SelfErrorCode("400-100", "{0}");
        /// <summary>
        ///   檢查用:當API沒有接收到必須參數 : {0}
        /// </summary>
        public static readonly SelfErrorCode MISSING_PARAMETERS = new SelfErrorCode("400-7", "Missing: {0}");
        /// <summary>
        ///   檢查用:當API接收到不符合規則的參數 : {0} 
        /// </summary>
        public static readonly SelfErrorCode INVALID_PARAMETERS = new SelfErrorCode("400-8", "Invalid: {0}");
        /// <summary>
        /// use all eidt 返回isrequired是必填未輸入
        /// </summary>
        public static readonly SelfErrorCode MISSING_ISREQUIRED = new SelfErrorCode("400-9", "Missing isrequired field");
        /// <summary>
        /// use user datasheep 返回帳號停用
        /// </summary>
        public static readonly SelfErrorCode ACCOUNT_DISABLED = new SelfErrorCode("400-10", "Account is disabled");
        #endregion

        #region ==404==
        /// <summary>
        /// Token 驗證錯誤
        /// </summary>
        public static readonly SelfErrorCode TOKEN_NOT_ALLOWED = new SelfErrorCode("404-1", "The token not allowed");
        /// <summary>
        /// Token 過期
        /// </summary>
        public static readonly SelfErrorCode TOKEN_EXPIRED = new SelfErrorCode("404-2", "The token has expired");
        /// <summary>
        /// 帳號錯誤
        /// </summary>
        public static readonly SelfErrorCode ACCOUNT_NOT_ALLOWED = new SelfErrorCode("404-3", "The account not allowed");
        /// <summary>
        /// 資料庫指定錯誤
        /// </summary>
        public static readonly SelfErrorCode DATASHEET_NOT_ALLOW = new SelfErrorCode("404-5", "Datasheet Not Allowed");
        /// <summary>
        /// INPUT內容錯誤 
        /// </summary>
        public static readonly SelfErrorCode DATA_ERROR = new SelfErrorCode("404-7", "The input data not allow.");
        /// <summary>
        /// 當API沒有影響到任何資料庫之資料 : {0} = 查詢條件
        /// </summary>
        public static readonly SelfErrorCode NO_DATA_AFFECTED = new SelfErrorCode("404-8", "No record found with {0}");
        /// <summary>
        /// 帶有自訂回傳訊息的 404 錯誤
        /// </summary>
        public static readonly SelfErrorCode NOT_FOUND_WITH_MSG = new SelfErrorCode("404-9", "{0}");
        #endregion

        #region ==500==
        /// <summary>
        /// 例外錯誤
        /// </summary>
        public static readonly SelfErrorCode TRY_CATCH_ERROR = new SelfErrorCode("500-1", "Exception:{0}");
        /// <summary>
        /// 當初始化失敗
        /// </summary>
        public static readonly SelfErrorCode INITIALIZATION_FAILED = new SelfErrorCode("500-2", "{0} initialization failed");
        /// <summary>
        /// 同步作業用: 執行中
        /// </summary>
        public static readonly SelfErrorCode SYNC_EXE = new SelfErrorCode("500-3", "{0}");
        /// <summary>
        /// 同步作業用: 失敗
        /// </summary>
        public static readonly SelfErrorCode SYNC_FAIL = new SelfErrorCode("500-4", "{0}");
        /// <summary>
        /// 同步作業用: 部分成功
        /// </summary>
        public static readonly SelfErrorCode SYNC_PARTIALSUCCESS = new SelfErrorCode("500-5", "{0}");
        /// <summary>
        /// 帶有自訂回傳訊息的 500 錯誤
        /// </summary>
        public static readonly SelfErrorCode INTERNAL_SERVER_WITH_MSG = new SelfErrorCode("500-6", "{0}");
        /// <summary>
        /// 連線逾時
        /// </summary>
        public static readonly SelfErrorCode CONNECTION_TIME_OUT = new SelfErrorCode("500-14", "SQL connection time out: {0}");
        /// <summary>
        /// 已存在相同的工程代號 + 客戶代號 + 成品代號
        /// </summary>
        public static readonly SelfErrorCode DUPLICATE_CUSTOMER_PRICE = new SelfErrorCode("400-15", "已存在相同的工程代號 + 客戶代號 + 成品代號");
        /// <summary>
        /// 已沖帳的收款單不能刪除
        /// </summary>
        public static readonly SelfErrorCode RECEIPT_ALREADY_OFFSET = new SelfErrorCode("400-16", "已沖帳的收款單不能刪除");
        /// <summary>
        /// 已沖帳的收款單不能編輯
        /// </summary>
        public static readonly SelfErrorCode RECEIPT_ALREADY_OFFSET_CANNOT_EDIT = new SelfErrorCode("400-17", "已沖帳的收款單不能編輯");
        #endregion

        /// <summary>
        /// 當API執行Sql query發生錯誤時 : 後端返回的error code格式設定成400-500-{0} : {0} = sql exception number
        /// </summary>
        public static readonly SelfErrorCode SQL_ERROR = new SelfErrorCode("400-500-{0}", "{0}");


        /// <summary>
        /// 將字串傳入訊息格式字串中
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public string GetFormattedMessage(params object[] args)
        {
            return (args.Length > 0) ? string.Format(Message, args) : Message;
        }

        /// <summary>
        /// 將sql exception number填入Code
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public string GetFormattedSqlCode(params object[] args)
        {
            return (args.Length > 0) ? string.Format(Code, args) : Code;
        }

        /// <summary>
        /// 使用反射來找出 SelfErrorCode 的名稱
        /// </summary>
        /// <returns></returns>
        public string? GetErrorCodeName()
        {
            var fields = typeof(SelfErrorCode).GetFields(BindingFlags.Public | BindingFlags.Static);
            foreach (var field in fields)
            {
                var fieldValue = field.GetValue(null) as SelfErrorCode;
                if (fieldValue != null && fieldValue.Code == Code)
                {
                    return field.Name;  // 返回欄位名稱，如 "SUCCESS"
                }
            }
            return null;
        }
    }
}
