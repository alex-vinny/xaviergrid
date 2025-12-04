using Microsoft.AspNetCore.Mvc;

namespace DynamicMongoAPI.Controllers
{
    [ApiController]
    [Route("")]
    public class SwaggerController : ControllerBase
    {
        [HttpGet]
        public IActionResult Get()
        {
            // Redirect to Swagger UI
            return Redirect("/swagger");
        }
    }
}