using Microsoft.AspNetCore.Mvc;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Accura_MES.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DbSelecter : ControllerBase
    {
        private XML xml = new();
        // GET: api/<ValuesController>
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public IActionResult Get()
        {
            ResponseObject result = new ResponseObject();

            result = result.GenerateEntity(SelfErrorCode.SUCCESS, xml.GetAllConnectionObj(), "");
            return StatusCode(int.Parse(result.Code.Split("-")[0]), result);
        }
    }
}
