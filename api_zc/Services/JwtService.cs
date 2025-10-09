using Microsoft.IdentityModel.Tokens;
using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Accura_MES.Services
{

    public class JwtService
    {
        private readonly IConfiguration _configuration;

        public JwtService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string GenerateJwtToken(string userId)
        {
            var jwtSettings = _configuration.GetSection("Jwt");
            var claims = new[]
            {
                new Claim("Lifetime", $"{DateTime.Now.AddMinutes(Convert.ToDouble(jwtSettings["ExpireMinutes"])).ToString("s")}"),
                new Claim(JwtRegisteredClaimNames.Sub, userId),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var expiration = DateTime.Now.AddMinutes(Convert.ToDouble(jwtSettings["ExpireMinutes"]));

            var token = new JwtSecurityToken(
                //issuer: jwtSettings["Issuer"],
                //audience: jwtSettings["Audience"],
                claims: claims,
                expires: expiration,
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        /// <summary>
        /// 解析token內容
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        public static IDictionary<string, string> AnalysisToken(string? token)
        {
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);

            // 取得所有 claims，將它們儲存為字典
            var claims = jwtToken.Claims.ToDictionary(claim => claim.Type, claim => claim.Value);

            return claims;
        }

        /// <summary>
        /// 檢查token時間
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        public static bool TokenTimeCheck(string token)
        {
            var claims = AnalysisToken(token);
            if (claims.ContainsKey("Lifetime"))
            {
                if (DateTime.TryParse(claims["Lifetime"], out DateTime dateTime))
                {
                    Debug.WriteLine(dateTime);
                    TimeSpan difference = DateTime.Now - dateTime;
                    if (difference.TotalHours < 1)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
