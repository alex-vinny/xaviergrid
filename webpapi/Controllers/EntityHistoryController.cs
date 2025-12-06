using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using DynamicMongoAPI.Models;
using DynamicMongoAPI.Services;
using System.Text.Json;
using System.Linq;
using DynamicMongoAPI.Utils;

namespace DynamicMongoAPI.Controllers
{
    [ApiController]
    [Route("api/{entity}")]
    [ApiExplorerSettings(GroupName = "Entity History")]
    public class EntityHistoryController : ControllerBase
    {
        private readonly MongoSchemaService _schemaService;
        private readonly DynamicMongoService _dynamicMongoService;
        
        public EntityHistoryController(MongoSchemaService schemaService, DynamicMongoService dynamicMongoService)
        {
            _schemaService = schemaService;
            _dynamicMongoService = dynamicMongoService;
        }
        
        // ----------------------------
        // RESTORE
        // ----------------------------
        [HttpPost("{id}/restore")]
        public async Task<IActionResult> Restore(string entity, string id)
        {
            try
            {
                var schema = await _schemaService.GetSchemaAsync(entity);
                var db = _schemaService.GetDatabase(schema);
                var collection = db.GetCollection<BsonDocument>(entity);
                
                var filter = Builders<BsonDocument>.Filter.Eq("_id", _dynamicMongoService.ToObjectId(id));
                
                var update = Builders<BsonDocument>.Update
                    .Set("isDeleted", false)
                    .Unset("deletedAt")
                    .Set("updatedAt", DateTime.UtcNow);
                
                var result = await collection.UpdateOneAsync(filter, update);
                
                if (result.ModifiedCount == 0)
                {
                    return NotFound(new { error = "Document not found or already restored" });
                }
                
                return Ok(new { message = "Document restored successfully" });
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
                var schema = await _schemaService.GetSchemaAsync(entity);
                var db = _schemaService.GetDatabase(schema);
                var collection = db.GetCollection<BsonDocument>(entity);
                await collection.DeleteManyAsync(FilterDefinition<BsonDocument>.Empty);
                
                var histCollection = db.GetCollection<BsonDocument>(entity + "_history");
                await histCollection.DeleteManyAsync(FilterDefinition<BsonDocument>.Empty);
                
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
                var schema = await _schemaService.GetSchemaAsync(entity);
                var db = _schemaService.GetDatabase(schema);
                var histCollection = db.GetCollection<BsonDocument>(entity + "_history");
                
                var filter = Builders<BsonDocument>.Filter.Eq("documentId", _dynamicMongoService.ToObjectId(id));
                var total = await histCollection.CountDocumentsAsync(filter);
                
                var docs = await histCollection.Find(filter)
                                         .Sort(Builders<BsonDocument>.Sort.Descending("timestamp"))
                                         .Skip((page - 1) * pageSize)
                                         .Limit(pageSize)
                                         .ToListAsync();
                
                // Convert BsonDocuments to Dictionary objects for proper JSON serialization
                var resultData = docs.Select(BsonConverter.BsonToDictionary).ToList();
                
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