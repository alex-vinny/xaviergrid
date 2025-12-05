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
            var functions = DynamicMongoService.AvailableFunctions.Values.Select(f => new {
                name = f.Name,
                description = f.Description,
                type = f.Type,
                parameters = f.Parameters,
                example = f.Example
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