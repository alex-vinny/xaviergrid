using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using DynamicMongoAPI.Models;
using DynamicMongoAPI.Services;
using System.Text.Json;
using DynamicMongoAPI.Constants;
using System.Linq;

namespace DynamicMongoAPI.Controllers
{
    [ApiController]
    [Route("schemas")]
    [ApiExplorerSettings(GroupName = "Rules and Relations")]
    public class EntityRelationsController : ControllerBase
    {
        private readonly MongoSchemaService _schemaService;
        private readonly string _masterSchemaDatabaseName;
        
        public EntityRelationsController(MongoSchemaService schemaService, IConfiguration configuration)
        {
            _schemaService = schemaService;
            _masterSchemaDatabaseName = configuration["MongoDB:MasterSchemaDatabase"] ?? AppConstants.MasterSchemaDatabaseName;
        }
        
        // GET /schemas/{entity}/relations - Get schema relations
        [HttpGet("{entity}/relations")]
        public async Task<IActionResult> GetSchemaRelations(string entity)
        {
            try
            {
                var schema = await _schemaService.GetSchemaAsync(entity);
                return Ok(schema.Relations ?? new List<SchemaRelation>());
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
        
        // GET /schemas/{entity}/relations/{relationId} - Get a specific relation from schema
        [HttpGet("{entity}/relations/{relationId}")]
        public async Task<IActionResult> GetSchemaRelation(string entity, int relationId)
        {
            try
            {
                var schema = await _schemaService.GetSchemaAsync(entity);
                
                // Check if relations exist
                if (schema.Relations == null || schema.Relations.Count == 0)
                    return NotFound(new { error = "No relations found for this schema" });
                
                // Check if relation index is valid
                if (relationId < 0 || relationId >= schema.Relations.Count)
                    return NotFound(new { error = $"Relation with ID '{relationId}' not found" });
                
                return Ok(schema.Relations[relationId]);
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
        
        // POST /schemas/{entity}/relations - Add relations to schema
        [HttpPost("{entity}/relations")]
        public async Task<IActionResult> AddSchemaRelations(string entity, [FromBody] List<SchemaRelation> relations)
        {
            try
            {
                var schema = await _schemaService.GetSchemaAsync(entity);
                
                var collection = _schemaService.GetMasterCollection<EntitySchema>("schemas");

                // Add new relations to existing ones
                if (schema.Relations == null)
                    schema.Relations = new List<SchemaRelation>();
                
                schema.Relations.AddRange(relations);
                
                // Update the schema in database
                var filter = Builders<EntitySchema>.Filter.Eq(s => s.Id, schema.Id);
                await collection.ReplaceOneAsync(filter, schema);
                
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
        
        // PUT /schemas/{entity}/relations/{relationId} - Change a relation from schema
        [HttpPut("{entity}/relations/{relationId}")]
        public async Task<IActionResult> UpdateSchemaRelation(string entity, int relationId, [FromBody] SchemaRelation updatedRelation)
        {
            try
            {
                var schema = await _schemaService.GetSchemaAsync(entity);
                
                var collection = _schemaService.GetMasterCollection<EntitySchema>("schemas");

                // Check if relations exist
                if (schema.Relations == null)
                    return NotFound(new { error = "No relations found for this schema" });
                
                // Check if relation index is valid
                if (relationId < 0 || relationId >= schema.Relations.Count)
                    return NotFound(new { error = $"Relation with ID '{relationId}' not found" });
                
                // Update the relation
                schema.Relations[relationId] = updatedRelation;
                
                // Update the schema in database
                var filter = Builders<EntitySchema>.Filter.Eq(s => s.Id, schema.Id);
                await collection.ReplaceOneAsync(filter, schema);
                
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
        
        // DELETE /schemas/{entity}/relations - Delete all relations from schema
        [HttpDelete("{entity}/relations")]
        public async Task<IActionResult> DeleteSchemaRelations(string entity)
        {
            try
            {
                var schema = await _schemaService.GetSchemaAsync(entity);
                
                var collection = _schemaService.GetMasterCollection<EntitySchema>("schemas");

                // Clear all relations
                schema.Relations = new List<SchemaRelation>();
                
                // Update the schema in database
                var filter = Builders<EntitySchema>.Filter.Eq(s => s.Id, schema.Id);
                await collection.ReplaceOneAsync(filter, schema);
                
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
        
        // DELETE /schemas/{entity}/relations/{relationId} - Delete a relation from schema
        [HttpDelete("{entity}/relations/{relationId}")]
        public async Task<IActionResult> DeleteSchemaRelation(string entity, int relationId)
        {
            try
            {
                var schema = await _schemaService.GetSchemaAsync(entity);
                
                var collection = _schemaService.GetMasterCollection<EntitySchema>("schemas");

                // Check if relations exist
                if (schema.Relations == null || schema.Relations.Count == 0)
                    return NotFound(new { error = "No relations found for this schema" });
                
                // Check if relation index is valid
                if (relationId < 0 || relationId >= schema.Relations.Count)
                    return NotFound(new { error = $"Relation with ID '{relationId}' not found" });
                
                // Remove the relation at the specified index
                schema.Relations.RemoveAt(relationId);
                
                // Update the schema in database
                var filter = Builders<EntitySchema>.Filter.Eq(s => s.Id, schema.Id);
                await collection.ReplaceOneAsync(filter, schema);
                
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