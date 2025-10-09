namespace Accura_MES
{
    /// <summary>
    /// 可傳遞自訂SelfErrorCode，詳情請看ResponseObject.cs
    /// </summary>
    [Serializable]
    public class CustomErrorCodeException : Exception
    {
        public SelfErrorCode SelfErrorCode { get; }
        public object? ErrorData { get; } = new List<object>();

        public CustomErrorCodeException(SelfErrorCode errorCode, string message = "", object? errorData = null)
            : base(message)
        {
            ErrorData = errorData;
            SelfErrorCode = errorCode;
        }

        public CustomErrorCodeException(SelfErrorCode errorCode, Exception inner, string message = "", object? errorData = null)
            : base(message, inner)
        {
            ErrorData = errorData;
            SelfErrorCode = errorCode;
        }


        public CustomErrorCodeException(ResponseObject responseObject)
            : base(responseObject.Message + responseObject.StackTrace)
        {
            if (responseObject == null)
                throw new ArgumentNullException(nameof(responseObject));

            ErrorData = responseObject.ErrorData;
            SelfErrorCode = responseObject.GetSelfErrorCode();
        }
    }
}
