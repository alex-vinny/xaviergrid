using DynamicMongoAPI.Models;
using DynamicMongoAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace DynamicMongoAPI.Controllers
{
    [ApiController]
    [Route("schemas")]
    [ApiExplorerSettings(GroupName = "Entity Rules")]
    public class EntityRelationsController : ControllerBase
    {
        private readonly IRelationService _relationService;

        public EntityRelationsController(IRelationService relationService)
        {
            _relationService = relationService;
        }

        [HttpGet("{entity}/relations")]
        public async Task<IActionResult> GetRelations(string entity)
        {
            try
            {
                var relations = await _relationService.GetRelationsAsync(entity);
                return Ok(relations);
            }
            catch (ArgumentException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"An error occurred while retrieving schema relations: {ex.Message}" });
            }
        }

        [HttpGet("{entity}/relations/{relationId}")]
        public async Task<IActionResult> GetRelationById(string entity, int relationId)
        {
            try
            {
                var relation = await _relationService.GetRelationAsync(entity, relationId);
                return Ok(relation);
            }
            catch (ArgumentException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"An error occurred while retrieving schema relation: {ex.Message}" });
            }
        }

        [HttpPost("{entity}/relations")]
        public async Task<IActionResult> AddRelations(string entity, [FromBody] List<SchemaRelation> relations)
        {
            try
            {
                await _relationService.AddRelationsAsync(entity, relations);
                return Ok(new { message = $"{relations.Count} relations added successfully" });
            }
            catch (ArgumentException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"An error occurred while adding schema relations: {ex.Message}" });
            }
        }

        [HttpPut("{entity}/relations/{relationId}")]
        public async Task<IActionResult> UpdateRelation(string entity, int relationId, [FromBody] SchemaRelation updatedRelation)
        {
            try
            {
                await _relationService.UpdateRelationAsync(entity, relationId, updatedRelation);
                return Ok(new { message = "Relation updated successfully" });
            }
            catch (ArgumentException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"An error occurred while updating schema relation: {ex.Message}" });
            }
        }

        [HttpDelete("{entity}/relations")]
        public async Task<IActionResult> DeleteRelations(string entity)
        {
            try
            {
                await _relationService.DeleteRelationsAsync(entity);
                return Ok(new { message = "All relations deleted successfully" });
            }
            catch (ArgumentException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"An error occurred while deleting schema relations: {ex.Message}" });
            }
        }

        [HttpDelete("{entity}/relations/{relationId}")]
        public async Task<IActionResult> DeleteRelation(string entity, int relationId)
        {
            try
            {
                await _relationService.DeleteRelationAsync(entity, relationId);
                return Ok(new { message = "Relation deleted successfully" });
            }
            catch (ArgumentException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"An error occurred while deleting schema relation: {ex.Message}" });
            }
        }
    }
}