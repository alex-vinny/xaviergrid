using Microsoft.AspNetCore.Mvc;
using DynamicMongoAPI.Services;

namespace DynamicMongoAPI.Controllers
{
    [ApiController]
    [Route("system")]
    [ApiExplorerSettings(GroupName = "General")]
    public class SystemController : ControllerBase
    {
        /// <summary>
        /// Get all available functions that can be used in schemas
        /// </summary>
        [HttpGet("functions")]
        public IActionResult GetAvailableFunctions()
        {
            var functions = FieldFunctionService.AvailableFunctions.Select(f => new {
                name = f.Key,
                description = f.Key, // you can add a proper description if needed
                type = "BsonValue => BsonValue",
                parameters = new[] { "value" },
                example = f.Key switch
                {
                    "bcrypt" => "\"password\"",
                    "lowercase" => "\"TEXT\"",
                    "uppercase" => "\"text\"",
                    "date" => "null",
                    "now" => "null",
                    _ => ""
                }
            }).ToArray();

            return Ok(functions);
        }

        /// <summary>
        /// Get API version information
        /// </summary>
        [HttpGet("version")]
        public IActionResult GetVersion()
        {
            return Ok(new
            {
                version = "1.0.0",
                name = "Dynamic MongoDB API",
                description = "A modern .NET 8+ Web API with dynamic MongoDB operations"
            });
        }
    }
}