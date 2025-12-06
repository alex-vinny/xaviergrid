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
    [Route("api")]
    [ApiExplorerSettings(GroupName = "Entity Advanced")]
    public class EntityQueryController : ControllerBase
    {
        private readonly MongoSchemaService _schemaService;
        private readonly DynamicMongoService _dynamicMongoService;
        
        public EntityQueryController(MongoSchemaService schemaService, DynamicMongoService dynamicMongoService)
        {
            _schemaService = schemaService;
            _dynamicMongoService = dynamicMongoService;
        }
        
        // ----------------------------
        // GET ALL DOCUMENTS (with pagination)
        // ----------------------------
        [HttpGet("{entity}")]
        public async Task<IActionResult> GetAll(string entity, int skip = 0, int limit = 20)
        {
            try
            {
                var schema = await _schemaService.GetSchemaAsync(entity);
                var db = _schemaService.GetDatabase(schema);
                var collection = db.GetCollection<BsonDocument>(entity);
                
                // Build filter for non-deleted documents
                var filter = Builders<BsonDocument>.Filter.Ne("isDeleted", true);
                
                // Get total count
                var total = await collection.CountDocumentsAsync(filter);
                
                // Get paginated documents
                var docs = await collection.Find(filter)
                                         .Skip(skip)
                                         .Limit(limit)
                                         .ToListAsync();
                
                // Apply relations & virtual fields
                foreach (var doc in docs)
                {
                    await _dynamicMongoService.ApplyRelations(doc, schema, db);
                    
                    if (schema.VirtualFields != null)
                    {
                        foreach (var vf in schema.VirtualFields)
                        {
                            var name = vf.Name;
                            var expr = vf.Expression;
                            doc[name] = _dynamicMongoService.EvaluateVirtualField(expr, doc);
                        }
                    }
                }
                
                // Convert list of BsonDocuments to list of Dictionaries
                var result = docs.Select(BsonConverter.BsonToDictionary).ToList();
                
                return Ok(new
                {
                    total,
                    skip,
                    limit,
                    data = result
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"An error occurred while retrieving documents: {ex.Message}" });
            }
        }
        
        // ----------------------------
        // DYNAMIC SEARCH
        // ----------------------------
        [HttpPost("{entity}/search")]
        public async Task<IActionResult> Search(string entity, [FromBody] JsonElement body)
        {
            var query = BsonConverter.JsonToBson(body);
            try
            {
                var schema = await _schemaService.GetSchemaAsync(entity);
                var db = _schemaService.GetDatabase(schema);
                var collection = db.GetCollection<BsonDocument>(entity);
                
                // Build filter from query
                var filter = _dynamicMongoService.BuildFilterFromQuery(query);
                
                // Add soft delete filter
                filter &= Builders<BsonDocument>.Filter.Ne("isDeleted", true);
                
                var docs = await collection.Find(filter).ToListAsync();
                
                // Apply relations & virtual fields
                foreach (var doc in docs)
                {
                    await _dynamicMongoService.ApplyRelations(doc, schema, db);
                    
                    if (schema.VirtualFields != null)
                    {
                        foreach (var vf in schema.VirtualFields)
                        {
                            var name = vf.Name;
                            var expr = vf.Expression;
                            doc[name] = _dynamicMongoService.EvaluateVirtualField(expr, doc);
                        }
                    }
                }
                
                // Convert list of BsonDocuments to list of Dictionaries
                var result = docs.Select(BsonConverter.BsonToDictionary).ToList();
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"An error occurred during search: {ex.Message}" });
            }
        }
        
        
        // ----------------------------
        // DYNAMIC AGGREGATE
        // ----------------------------
        [HttpPost("{entity}/aggregate")]
        public async Task<IActionResult> Aggregate(string entity, [FromBody] JsonElement body)
        {
            var pipeline = BsonConverter.JsonToBsonArray(body);
            try
            {
                var schema = await _schemaService.GetSchemaAsync(entity);
                var db = _schemaService.GetDatabase(schema);
                var collection = db.GetCollection<BsonDocument>(entity);
                
                // Security: Limit pipeline stages to prevent malicious operations
                var sanitizedPipeline = _dynamicMongoService.SanitizeAggregationPipeline(pipeline);
                
                var result = await collection.AggregateAsync<BsonDocument>(sanitizedPipeline);
                var docs = await result.ToListAsync();
                
                // Convert list of BsonDocuments to list of Dictionaries
                var ret = docs.Select(BsonConverter.BsonToDictionary).ToList();
                return Ok(ret);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"An error occurred during aggregation: {ex.Message}" });
            }
        }
        
        // ----------------------------
        // JSONQueryLang QUERY
        // ----------------------------
        [HttpPost("query/{entity}")]
        public async Task<IActionResult> Query(string entity, [FromBody] JsonElement body)
        {
            try
            {
                // Ignore reserved routes
                if (BsonConverter.IsReservedRoute(entity))
                {
                    return NotFound(new { error = $"Entity '{entity}' is reserved and cannot be used" });
                }
                
                var result = await _dynamicMongoService.QueryAsync(entity, body);
                
                // Convert list of BsonDocuments to list of Dictionaries
                var ret = result.Select(BsonConverter.BsonToDictionary).ToList();
                return Ok(ret);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"An error occurred during query: {ex.Message}" });
            }
        }
    }
}