using DynamicMongoAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace DynamicMongoAPI.Controllers
{
    [ApiController]
    [Route("api/namespaces")]
    [ApiExplorerSettings(GroupName = "Namespaces")]
    public class NamespaceController : ControllerBase
    {
        private readonly INamespaceService _namespaceService;

        public NamespaceController(INamespaceService namespaceService)
        {
            _namespaceService = namespaceService;
        }

        [HttpGet]
        public async Task<IActionResult> GetNamespaces()
        {
            try
            {
                var namespaces = await _namespaceService.ListNamespacesAsync();
                return Ok(namespaces);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("{ns}/entities")]
        public async Task<IActionResult> GetEntitiesByNamespace(string ns)
        {
            try
            {
                var entities = await _namespaceService.ListEntitiesAsync(ns);
                return Ok(entities);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}
