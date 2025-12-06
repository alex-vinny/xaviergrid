using DynamicMongoAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace DynamicMongoAPI.Controllers
{
    [ApiController]
    [Route("api/{entity}")]
    [ApiExplorerSettings(GroupName = "Entity History")]
    public class EntityHistoryController : ControllerBase
    {
        private readonly IHistoryService _historyService;
        
        public EntityHistoryController(IHistoryService historyService)
        {
            _historyService = historyService;
        }
        
        // ----------------------------
        // RESTORE
        // ----------------------------
        [HttpPost("{id}/restore")]
        public async Task<IActionResult> Restore(string entity, string id)
        {
            try
            {                
                var result = await _historyService.RestoreAsync(entity, id);
                
                if (result == null)
                {
                    return NotFound(new { error = "Document not found or already restored" });
                }
                
                return Ok(new 
                { 
                    message = "Document restored successfully",
                    id = result.Id
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"An error occurred while restoring the document: {ex.Message}" });
            }
        }
        
        // ----------------------------
        // PURGE
        // ----------------------------
        [HttpPost("purge")]
        public async Task<IActionResult> Purge(string entity)
        {
            try
            {
                await _historyService.PurgeAsync(entity);
                
                return Ok(new { message = $"All documents purged for entity '{entity}'" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"An error occurred while purging documents: {ex.Message}" });
            }
        }
        
        // ----------------------------
        // HISTORY (paginated)
        // ----------------------------
        [HttpGet("{id}/history")]
        public async Task<IActionResult> History(string entity, string id, int page = 1, int pageSize = 20)
        {
            try
            {
                var total = await _historyService.CountAsync(entity, id);
                var resultData = await _historyService.HistoryAsync(entity, id, page, pageSize);
                
                return Ok(new
                {
                    total,
                    page,
                    pageSize,
                    data = resultData
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"An error occurred while retrieving history: {ex.Message}" });
            }
        }
    }
}