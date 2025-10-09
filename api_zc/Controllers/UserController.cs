using Accura_MES.Extensions;
using Accura_MES.Interfaces.Services;
using Accura_MES.Models;
using Accura_MES.Service;
using Accura_MES.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Security.Cryptography;
using System.Text;

namespace Accura_MES.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly JwtService jwt;
        private readonly XML _xml = new XML();

        public UserController(JwtService jwt)
        {
            this.jwt = jwt;
        }

        /// <summary>
        /// 登入
        /// </summary>
        /// <param name="loginInfo"></param>
        /// <returns></returns>
        [HttpPost("Login")]
        public IActionResult Login([FromBody] LoginInfo loginInfo)
        {
            ResponseObject result = new ResponseObject();
            #region 檢查
            if (!Request.Headers.ContainsKey("Database"))
            {
                result = new ResponseObject().GenerateEntity(SelfErrorCode.MISSING_DATABASE, "", "");
                return StatusCode(int.Parse(result.Code.Split("-")[0]), result);
            }

            // 根據使用者ID動態取得sql連接字串
            string connectionString = new XML().GetConnection(Request.Headers["Database"].ToString());
            if (string.IsNullOrEmpty(connectionString))
            {
                result = new ResponseObject().GenerateEntity(SelfErrorCode.NOT_FOUND, "", "SQL not exist");
                return StatusCode(int.Parse(result.Code.Split("-")[0]), result);
            }
            if (loginInfo == null)
            {
                result = new ResponseObject().GenerateEntity(SelfErrorCode.BAD_REQUEST, "", "Invalid request message frame");
                return StatusCode(int.Parse(result.Code.Split("-")[0]), result);
            }
            #endregion
            try
            {
                var missingFields = new List<string>();
                if (loginInfo.Account == null || string.IsNullOrEmpty(loginInfo.Account))
                {
                    missingFields.Add("account");
                }
                if (loginInfo.Password == null || string.IsNullOrEmpty(loginInfo.Password))
                {
                    missingFields.Add("password");
                }
                if (missingFields.Any())
                {
                    var errorData = new List<object>
                    {
                        new
                        {
                            code = "400-7",
                            rowIndex = 1,
                            errorData = missingFields,
                            massage = $"第 1 筆資料缺失必要欄位: {string.Join(", ", missingFields)}"
                        }
                    };

                    throw new CustomErrorCodeException(SelfErrorCode.MISSING_PARAMETERS, "詳情請看errorData", errorData);
                }

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    // 構建 SQL 查詢
                    string query = @"
                        SELECT 
                            CASE 
                                WHEN u.isDelete = 1 THEN 'ACCOUNT_NOT_ALLOWED'
                                WHEN u.isEnable = 0 THEN 'ACCOUNT_DISABLED'
                                ELSE 'SUCCESS'
                            END AS LoginStatus,
                            u.id
                        FROM [user] u
                        WHERE u.account = @account AND u.password = @password;
                    ";

                    // 打開連接
                    connection.Open();
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {


                        command.Parameters.AddWithValue($"@account", loginInfo.Account);
                        //檢查密碼(先解密後加密)
                        command.Parameters.AddWithValue($"@password", EncryptAES(DecryptAES(loginInfo.Password)));

                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                string loginStatus = reader["LoginStatus"].ToString();
                                switch (loginStatus)
                                {
                                    case "ACCOUNT_NOT_ALLOWED":
                                        result = result.GenerateEntity(SelfErrorCode.ACCOUNT_NOT_ALLOWED, "", "");
                                        return StatusCode(int.Parse(result.Code.Split("-")[0]), result);
                                    case "ACCOUNT_DISABLED":
                                        result = result.GenerateEntity(SelfErrorCode.ACCOUNT_DISABLED, "", "");
                                        return StatusCode(int.Parse(result.Code.Split("-")[0]), result);
                                    case "SUCCESS":
                                        string userId = reader["id"].ToString();
                                        result = result.GenerateEntity(SelfErrorCode.SUCCESS, jwt.GenerateJwtToken(userId), "");
                                        return StatusCode(int.Parse(result.Code.Split("-")[0]), result);
                                }
                            }
                        }
                    }
                }
                result = result.GenerateEntity(SelfErrorCode.ACCOUNT_NOT_ALLOWED, "", "");
                return StatusCode(int.Parse(result.Code.Split("-")[0]), result);
            }
            catch (Exception ex)
            {
                return this.HandleAccuraException(null, ex);
            }
        }

        /// <summary>
        /// 建立使用者
        /// </summary>
        /// <param name="UserInfo"></param>
        /// <returns></returns>
        [HttpPost("Build")]
        public async Task<IActionResult> Build([FromBody] Dictionary<string, object> UserInfo)
        {
            // 檢查token
            ResponseObject result = CheckToken(Request);
            // 獲取 Authorization 標頭
            string connectionString = new XML().GetConnection(Request.Headers["Database"].ToString());
            string authorizationHeader = HttpContext.Request.Headers["Authorization"];
            var token = JwtService.AnalysisToken(authorizationHeader);

            InputValidationService.RemoveInValidFields(InputFilters.CreateFilters, UserInfo); //過濾輸入


            if (!result.Success) return StatusCode(int.Parse(result.Code.Split("-")[0]), result);

            try
            {
                IUserService userService = UserService.CreateService(connectionString);

                var insertedId = await userService.Create(UserInfo, long.Parse(token["sub"]));

                return this.SuccessAccuraResponse(insertedId);

            }
            catch (Exception ex)
            {
                return this.HandleAccuraException(null, ex);
            }
        }

        /// <summary>
        /// 編輯使用者
        /// </summary>
        /// <param name="UserInfo"></param>
        /// <returns></returns>
        [HttpPut("Edit")]
        public async Task<IActionResult> Edit([FromBody] Dictionary<string, object> UserInfo)
        {
            // 檢查token
            ResponseObject result = UserController.CheckToken(Request);
            // 獲取 Authorization 標頭
            string connectionString = new XML().GetConnection(Request.Headers["Database"].ToString());
            string authorizationHeader = HttpContext.Request.Headers["Authorization"];
            var token = JwtService.AnalysisToken(authorizationHeader);

            InputValidationService.RemoveInValidFields(InputFilters.UpdateFilters, UserInfo); //過濾輸入

            if (!result.Success) return StatusCode(int.Parse(result.Code.Split("-")[0]), result);

            try
            {
                IUserService userService = UserService.CreateService(connectionString);

                var isSuccess = await userService.Update(UserInfo, long.Parse(token["sub"]));

                if (isSuccess)
                    return this.SuccessAccuraResponse();
                else
                    return this.CustomAccuraResponse(SelfErrorCode.NO_DATA_AFFECTED);

            }
            catch (Exception ex)
            {
                return this.HandleAccuraException(null, ex);
            }
        }

        /// <summary>
        /// 變更密碼
        /// </summary>
        /// <param name="input">include "oldPassWord", "newPassWord"</param>
        /// <returns></returns>
        [HttpPut("ChangePassWord")]
        public async Task<IActionResult> ChangePassWord([FromBody] Dictionary<string, object> input)
        {
            try
            {
                // 取得connection
                string connectionString = _xml.GetConnection(Request.Headers["Database"].ToString());

                #region 檢查/解析輸入
                // 檢查Header
                ResponseObject result = UserController.CheckToken(Request);
                if (!result.Success)
                {
                    return StatusCode(int.Parse(result.Code.Split("-")[0]), result);
                }

                // 檢查Body
                var inputValidationService = InputValidationService.CreateService(connectionString, "user", API.Read);

                var validationResult = await inputValidationService.ValidateAndFilterAndSetDefaultInput(
                    "user",
                    new() { input },
                    new() { "oldPassWord", "newPassWord" },
                    null,
                    false);

                if (validationResult != null)
                {
                    // 驗證失敗，直接返回錯誤
                    return this.CustomAccuraResponse(SelfErrorCode.NESTED_STRUCTURE_ERROR, null, validationResult, "詳情請看error data");
                }

                // 獲取 token
                var token = JwtService.AnalysisToken(HttpContext.Request.Headers["Authorization"]);
                long user = long.Parse(token["sub"]);

                // analyze input
                string oldPassWord = input.GetValueThenTryParseOrThrow<string>("oldPassWord");
                string newPassWord = input.GetValueThenTryParseOrThrow<string>("newPassWord");
                #endregion
                IUserService userService = UserService.CreateService(connectionString);

                var responseObject = await userService.ChangePassWord(user, oldPassWord, newPassWord);

                if (!responseObject.Success)
                    return this.CustomAccuraResponse(responseObject);
                else
                    return this.SuccessAccuraResponse();

            }
            catch (Exception ex)
            {
                return this.HandleAccuraException(null, ex);
            }
        }

        /// <summary>
        /// 帳號啟用/停用
        /// </summary>
        /// <param name="UserInfo"></param>
        /// <returns></returns>
        [HttpPut("isEnabled")]
        public IActionResult isEnabled([FromBody] Dictionary<string, object> UserInfo)
        {
            //檢查token
            ResponseObject result = CheckToken(Request);
            // 獲取 Authorization 標頭
            string connectionString = new XML().GetConnection(Request.Headers["Database"].ToString());
            string authorizationHeader = HttpContext.Request.Headers["Authorization"];
            var token = JwtService.AnalysisToken(authorizationHeader);

            if (result.Success == true)
            {
                try
                {
                    using (SqlConnection connection = new SqlConnection(connectionString))
                    {
                        // 構建 SQL 更新語句
                        string query = "UPDATE [user] SET " +
                                       "IsEnable = @IsEnable, " +
                                       "modifiedBy = @modifiedBy " +
                                       "WHERE Id = @Id;";

                        // 打開連接
                        connection.Open();

                        try
                        {
                            // 確保傳遞的 UserInfo 包含 Id 和 isenable
                            if (UserInfo.ContainsKey("id") && UserInfo.ContainsKey("isEnable"))
                            {
                                using (SqlCommand command = new SqlCommand(query, connection))
                                {
                                    command.Parameters.AddWithValue("@Id", UserInfo["id"].ToString());
                                    command.Parameters.AddWithValue("@IsEnable", UserInfo["isEnable"].ToString());
                                    command.Parameters.AddWithValue("@modifiedBy", token["sub"]);
                                    command.ExecuteNonQuery();
                                }
                                // 返回成功結果
                                result = result.GenerateEntity(SelfErrorCode.SUCCESS, "", "");
                                return StatusCode(int.Parse(result.Code.Split("-")[0]), result);
                            }
                            else
                            {
                                result = new ResponseObject().GenerateEntity(SelfErrorCode.MISSING_ID, "", "");
                                return StatusCode(int.Parse(result.Code.Split("-")[0]), result);
                            }
                        }
                        catch (Exception ex)
                        {
                            result = result.GenerateEntity(SelfErrorCode.TRY_CATCH_ERROR, "", "", ex.Message);
                            return StatusCode(int.Parse(result.Code.Split("-")[0]), result);
                        }
                    }
                }
                catch (Exception ex)
                {
                    return this.HandleAccuraException(null, ex);
                }
            }
            else
            {
                return StatusCode(int.Parse(result.Code.Split("-")[0]), result);
            }
        }

        /// <summary>
        /// 更新token
        /// </summary>
        /// <returns></returns>
        [HttpPost("Refresh")]
        public IActionResult RefreshToken()
        {
            //檢查token
            ResponseObject result = CheckToken(Request);
            //// 獲取 Authorization 標頭
            string authorizationHeader = HttpContext.Request.Headers["Authorization"];
            var token = JwtService.AnalysisToken(authorizationHeader);

            if (result.Success == true)
            {
                result = result.GenerateEntity(SelfErrorCode.SUCCESS, jwt.GenerateJwtToken(token["sub"]), "");
                return StatusCode(int.Parse(result.Code.Split("-")[0]), result);
            }
            else
            {
                return StatusCode(int.Parse(result.Code.Split("-")[0]), result);
            }
        }

        /// <summary>
        /// 更新token
        /// </summary>
        /// <returns></returns>
        [HttpPost("TokenDecoded")]
        public IActionResult TokenDecoded()
        {
            //檢查token
            ResponseObject result = CheckToken(Request);
            //// 獲取 Authorization 標頭
            string authorizationHeader = HttpContext.Request.Headers["Authorization"];
            var token = JwtService.AnalysisToken(authorizationHeader);

            if (result.Success == true)
            {
                result = result.GenerateEntity(SelfErrorCode.SUCCESS, token, "");
                return StatusCode(int.Parse(result.Code.Split("-")[0]), result);
            }
            else
            {
                return StatusCode(int.Parse(result.Code.Split("-")[0]), result);
            }
        }

        /// <summary>
        /// 檢查token(非API)
        /// </summary>
        [ApiExplorerSettings(IgnoreApi = true)]
        public static ResponseObject CheckToken(HttpRequest request)
        {
            ResponseObject result = new ResponseObject();
            #region 檢查
            if (!request.Headers.ContainsKey("Database"))
            {
                result = new ResponseObject().GenerateEntity(SelfErrorCode.MISSING_DATABASE, "", "");
                return result;
            }

            // 根據使用者ID動態取得sql連接字串
            string connectionString = new XML().GetConnection(request.Headers["Database"].ToString());
            if (string.IsNullOrEmpty(connectionString))
            {
                result = new ResponseObject().GenerateEntity(SelfErrorCode.NOT_FOUND_WITH_MSG, "", "", "SQL not exist");
                return result;
            }
            #endregion
            // 獲取 Authorization 標頭
            string authorizationHeader = request.Headers["Authorization"];
            if (authorizationHeader != null)
            {
                var token = JwtService.AnalysisToken(authorizationHeader);
                if (token["sub"] == null)
                {
                    result = result.GenerateEntity(SelfErrorCode.TOKEN_NOT_ALLOWED);
                    return result;
                }

                // 檢查 token 時間
                //if (!JwtService.TokenTimeCheck(authorizationHeader))
                //{
                //    result = result.GenerateEntity(SelfErrorCode.TOKEN_EXPIRED);
                //    return result;
                //}

                //token符合標準後執行
                try
                {
                    using (SqlConnection connection = new SqlConnection(connectionString))
                    {
                        // 構建 SQL 查詢
                        string query = $"SELECT * FROM [user] WHERE id = @id AND isEnable = 1 AND isDelete = 0";

                        // 打開連接
                        connection.Open();
                        using (SqlCommand command = new SqlCommand(query, connection))
                        {
                            command.Parameters.AddWithValue($"@id", token["sub"]);
                            using (SqlDataReader reader = command.ExecuteReader())
                            {
                                var sqlresult = new List<object>();

                                // 讀取結果
                                while (reader.Read())
                                {
                                    //有
                                    result = result.GenerateEntity(SelfErrorCode.SUCCESS);
                                    return result;
                                }
                                result = result.GenerateEntity(SelfErrorCode.TOKEN_NOT_ALLOWED);
                                return result;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    // 記錄例外或返回詳細錯誤資訊
                    result = result.GenerateEntity(SelfErrorCode.TRY_CATCH_ERROR, "", "", ex.Message);
                    return result;
                }
            }
            result = result.GenerateEntity(SelfErrorCode.TOKEN_NOT_ALLOWED);
            return result;
        }

        /// <summary>
        /// AES 加密 api(測試用)
        /// </summary>
        /// <returns></returns>
        [HttpPost("Encrypt")]
        public IActionResult Encrypt_AES([FromBody] string password)
        {
            ResponseObject result = new ResponseObject();
            result = result.GenerateEntity(SelfErrorCode.SUCCESS, EncryptAES_test(password), "");
            return StatusCode(int.Parse(result.Code.Split("-")[0]), result);
        }

        /// <summary>
        /// AES 加密
        /// </summary>
        /// <param name="plainText"></param>
        /// <returns></returns>
        private static string EncryptAES(string plainText)
        {
            // 密鑰
            byte[] key = Encoding.UTF8.GetBytes("a1B2c3D4e5F6g7H8");
            // 獲取 IV
            byte[] iv = Encoding.UTF8.GetBytes("Y5z6A7b8C9d0E1f2");

            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = key;
                aesAlg.IV = iv; // 設置靜態 IV

                aesAlg.Mode = CipherMode.CBC;
                aesAlg.Padding = PaddingMode.PKCS7;

                using (MemoryStream msEncrypt = new MemoryStream())
                {
                    // 將 IV 寫入到密文流中
                    msEncrypt.Write(iv, 0, iv.Length);

                    using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, aesAlg.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                        {
                            swEncrypt.Write(plainText);
                        }
                    }

                    return Convert.ToBase64String(msEncrypt.ToArray()); // 返回包含 IV 的密文
                }
            }
        }

        /// <summary>
        /// AES 加密(測試用)
        /// </summary>
        /// <param name="plainText"></param>
        /// <returns></returns>
        private static string EncryptAES_test(string plainText)
        {
            // 密鑰
            byte[] key = Encoding.UTF8.GetBytes("=GC%'AmN/}2f9Q#u");

            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = key;
                aesAlg.GenerateIV(); // 自動生成 IV
                byte[] iv = aesAlg.IV; // 獲取 IV

                aesAlg.Mode = CipherMode.CBC;
                aesAlg.Padding = PaddingMode.PKCS7;

                using (MemoryStream msEncrypt = new MemoryStream())
                {
                    // 將 IV 寫入到密文流中
                    msEncrypt.Write(iv, 0, iv.Length);

                    using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, aesAlg.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                        {
                            swEncrypt.Write(plainText);
                        }
                    }

                    return Convert.ToBase64String(msEncrypt.ToArray()); // 返回包含 IV 的密文
                }
            }
        }

        /// <summary>
        /// AES 解密
        /// </summary>
        /// <param name="encryptedText"></param>
        /// <returns></returns>
        private static string DecryptAES(string encryptedText)
        {
            //密鑰
            byte[] key = Encoding.UTF8.GetBytes("=GC%'AmN/}2f9Q#u");

            // 從 Base64 編碼的加密字串轉換為 byte[]
            byte[] cipherTextCombined = Convert.FromBase64String(encryptedText);

            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = key;
                aesAlg.Mode = CipherMode.CBC; // 和前端保持一致
                aesAlg.Padding = PaddingMode.PKCS7;

                // AES IV 長度總是 16 字節，從密文中提取 IV
                byte[] iv = new byte[16];
                byte[] cipherText = new byte[cipherTextCombined.Length - iv.Length];

                Array.Copy(cipherTextCombined, iv, iv.Length);
                Array.Copy(cipherTextCombined, iv.Length, cipherText, 0, cipherText.Length);

                aesAlg.IV = iv;

                ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

                using (MemoryStream msDecrypt = new MemoryStream(cipherText))
                {
                    using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                    {
                        using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                        {
                            return srDecrypt.ReadToEnd();
                        }
                    }
                }
            }
        }
    }
}
