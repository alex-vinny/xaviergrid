using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using DynamicMongoAPI.Models;
using DynamicMongoAPI.Services;
using System.Text.Json;
using System.Linq;

namespace DynamicMongoAPI.Controllers
{
    [ApiController]
    [Route("api/{entity}")]
    [ApiExplorerSettings(GroupName = "Crud Entity")]
    public class DynamicMongoController : ControllerBase
    {
        private readonly IMongoClient _client;
        private readonly MongoSchemaService _schemaService;
        private readonly DynamicMongoService _dynamicMongoService;
        
        public DynamicMongoController(IMongoClient client, MongoSchemaService schemaService, DynamicMongoService dynamicMongoService)
        {
            _client = client;
            _schemaService = schemaService;
            _dynamicMongoService = dynamicMongoService;
        }

        // Helper method to convert JsonElement to BsonDocument
        private static BsonDocument JsonToBson(JsonElement json)
        {
            var jsonString = json.GetRawText();
            return BsonSerializer.Deserialize<BsonDocument>(jsonString);
        }

        // Helper method to convert JsonElement array to BsonArray
        private static BsonArray JsonToBsonArray(JsonElement json)
        {
            var jsonString = json.GetRawText();
            return BsonSerializer.Deserialize<BsonArray>(jsonString);
        }

        // Helper method to convert BsonDocument to Dictionary for proper JSON serialization
        private static Dictionary<string, object> BsonToDictionary(BsonDocument doc)
        {
            var dict = new Dictionary<string, object>();
            foreach (var element in doc)
            {
                dict[element.Name] = BsonValueToNative(element.Value);
            }
            return dict;
        }

        // Helper method to convert BsonValue to native types
        private static object BsonValueToNative(BsonValue value)
        {
            switch (value.BsonType)
            {
                case BsonType.ObjectId:
                    return value.AsObjectId.ToString();
                case BsonType.String:
                    return value.AsString;
                case BsonType.Boolean:
                    return value.AsBoolean;
                case BsonType.DateTime:
                    return value.ToUniversalTime();
                case BsonType.Double:
                    return value.AsDouble;
                case BsonType.Int32:
                    return value.AsInt32;
                case BsonType.Int64:
                    return value.AsInt64;
                case BsonType.Decimal128:
                    return value.AsDecimal;
                case BsonType.Array:
                    var array = value.AsBsonArray;
                    var list = new List<object>();
                    foreach (var item in array)
                    {
                        list.Add(BsonValueToNative(item));
                    }
                    return list;
                case BsonType.Document:
                    return BsonToDictionary(value.AsBsonDocument);
                default:
                    return value.ToString();
            }
        }

        // Check if the entity name is a reserved route
        private bool IsReservedRoute(string entity)
        {
            var reservedRoutes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "swagger",
                "system",
                "health",
                "api"
            };
            
            return reservedRoutes.Contains(entity);
        }
        
        // ----------------------------
        // CREATE DOCUMENT
        // ----------------------------
        [HttpPost]
        [ApiExplorerSettings(GroupName = "Crud Entity")]
        public async Task<IActionResult> Create(string entity, [FromBody] JsonElement body)
        {
            var doc = JsonToBson(body);
            // Ignore reserved routes
            if (IsReservedRoute(entity))
            {
                return NotFound(new { error = $"Entity '{entity}' is reserved and cannot be used" });
            }
            try
            {
                var schema = await _schemaService.GetSchemaAsync(entity);
                var rules = schema.Rules;
                var db = _schemaService.GetDatabase(schema);
                
                _dynamicMongoService.ApplyFieldFunctions(doc, schema);
                _dynamicMongoService.ValidateEnums(doc, schema);
                _dynamicMongoService.CheckRules(rules, "create", doc);
                
                // Set default values
                doc["_id"] = doc.Contains("_id") ? doc["_id"] : ObjectId.GenerateNewId();
                doc["isDeleted"] = false;
                doc["createdAt"] = DateTime.UtcNow;
                doc["updatedAt"] = DateTime.UtcNow;
                
                var collection = db.GetCollection<BsonDocument>(entity);
                await collection.InsertOneAsync(doc);
                
                // Add to history
                var histCollection = db.GetCollection<BsonDocument>(entity + "_history");
                var histDoc = new BsonDocument
                {
                    ["_id"] = ObjectId.GenerateNewId(),
                    ["documentId"] = doc["_id"],
                    ["version"] = 1,
                    ["data"] = doc,
                    ["timestamp"] = DateTime.UtcNow,
                    ["action"] = "create"
                };
                await histCollection.InsertOneAsync(histDoc);
                
                return Ok(BsonToDictionary(doc));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"An error occurred while creating the document: {ex.Message}" });
            }
        }
        
        // ----------------------------
        // GET BY ID + auto-join + virtual fields
        // ----------------------------
        [ApiExplorerSettings(GroupName = "Crud Entity")]
        [HttpGet("{id}")]
        public async Task<IActionResult> Get(string entity, string id)
        {
            // Ignore reserved routes
            if (IsReservedRoute(entity))
            {
                return NotFound(new { error = $"Entity '{entity}' is reserved and cannot be used" });
            }
            try
            {
                var schema = await _schemaService.GetSchemaAsync(entity);
                var db = _schemaService.GetDatabase(schema);
                var collection = db.GetCollection<BsonDocument>(entity);
                
                var filter = Builders<BsonDocument>.Filter.Eq("_id", _dynamicMongoService.ToObjectId(id)) &
                             Builders<BsonDocument>.Filter.Ne("isDeleted", true);
                
                var doc = await collection.Find(filter).FirstOrDefaultAsync();
                if (doc == null) return NotFound(new { error = "Document not found" });
                
                // Multi-level joins
                await _dynamicMongoService.ApplyRelations(doc, schema, db);
                
                // Virtual fields
                if (schema.VirtualFields != null)
                {
                    foreach (var vf in schema.VirtualFields)
                    {
                        var name = vf.Name;
                        var expr = vf.Expression;
                        doc[name] = _dynamicMongoService.EvaluateVirtualField(expr, doc);
                    }
                }
                
                return Ok(BsonToDictionary(doc));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"An error occurred while retrieving the document: {ex.Message}" });
            }
        }
        
        // ----------------------------
        // UPDATE
        // ----------------------------
        [ApiExplorerSettings(GroupName = "Crud Entity")]
        [HttpPatch("{id}")]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string entity, string id, [FromBody] JsonElement body)
        {
            var updateDoc = JsonToBson(body);
            try
            {
                var schema = await _schemaService.GetSchemaAsync(entity);
                var rules = schema.Rules;
                var db = _schemaService.GetDatabase(schema);
                
                _dynamicMongoService.ApplyFieldFunctions(updateDoc, schema);
                
                var collection = db.GetCollection<BsonDocument>(entity);
                var filter = Builders<BsonDocument>.Filter.Eq("_id", _dynamicMongoService.ToObjectId(id)) &
                             Builders<BsonDocument>.Filter.Ne("isDeleted", true);
                
                var existing = await collection.Find(filter).FirstOrDefaultAsync();
                if (existing == null) return NotFound(new { error = "Document not found" });
                
                // Merge for validation
                var merged = existing.DeepClone().AsBsonDocument;
                foreach (var elem in updateDoc) merged[elem.Name] = elem.Value;
                
                _dynamicMongoService.ValidateEnums(merged, schema);
                _dynamicMongoService.CheckRules(rules, "update", merged);
                
                // Add timestamp
                updateDoc["updatedAt"] = DateTime.UtcNow;
                
                var update = new BsonDocument("$set", updateDoc);
                var result = await collection.UpdateOneAsync(filter, update);
                
                if (result.ModifiedCount == 0)
                {
                    return Ok(new { message = "No changes were made to the document" });
                }
                
                // Add to history
                var histCollection = db.GetCollection<BsonDocument>(entity + "_history");
                var version = await histCollection.CountDocumentsAsync(Builders<BsonDocument>.Filter.Eq("documentId", existing["_id"])) + 1;
                
                var histDoc = new BsonDocument
                {
                    ["_id"] = ObjectId.GenerateNewId(),
                    ["documentId"] = existing["_id"],
                    ["version"] = version,
                    ["data"] = merged,
                    ["timestamp"] = DateTime.UtcNow,
                    ["action"] = "update"
                };
                await histCollection.InsertOneAsync(histDoc);
                
                return Ok(new { message = "Document updated successfully" });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"An error occurred while updating the document: {ex.Message}" });
            }
        }
        
        // ----------------------------
        [ApiExplorerSettings(GroupName = "Crud Entity")]
        // DELETE (soft delete)
        // ----------------------------
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string entity, string id)
        {
            try
            {
                var schema = await _schemaService.GetSchemaAsync(entity);
                var rules = schema.Rules;
                var db = _schemaService.GetDatabase(schema);
                
                var collection = db.GetCollection<BsonDocument>(entity);
                var filter = Builders<BsonDocument>.Filter.Eq("_id", _dynamicMongoService.ToObjectId(id)) &
                             Builders<BsonDocument>.Filter.Ne("isDeleted", true);
                
                var doc = await collection.Find(filter).FirstOrDefaultAsync();
                if (doc == null) return NotFound(new { error = "Document not found" });
                
                _dynamicMongoService.CheckRules(rules, "delete", doc);
                
                var update = Builders<BsonDocument>.Update
                    .Set("isDeleted", true)
                    .Set("deletedAt", DateTime.UtcNow)
                    .Set("updatedAt", DateTime.UtcNow);
                
                await collection.UpdateOneAsync(filter, update);
                
                // Add to history
                var histCollection = db.GetCollection<BsonDocument>(entity + "_history");
                var histDoc = new BsonDocument
                {
                    ["_id"] = ObjectId.GenerateNewId(),
                    ["documentId"] = doc["_id"],
                    ["version"] = await histCollection.CountDocumentsAsync(Builders<BsonDocument>.Filter.Eq("documentId", doc["_id"])) + 1,
                    ["data"] = doc,
                    ["timestamp"] = DateTime.UtcNow,
                    ["action"] = "delete"
                };
                await histCollection.InsertOneAsync(histDoc);
                
                return Ok(new { message = "Document deleted successfully" });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"An error occurred while deleting the document: {ex.Message}" });
            }
        }
        
        // ----------------------------
        // RESTORE
        // ----------------------------
        [HttpPost("{id}/restore")]
        [ApiExplorerSettings(GroupName = "Entity History")]
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
        [ApiExplorerSettings(GroupName = "Entity History")]
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
        [ApiExplorerSettings(GroupName = "Entity History")]
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
                
                return Ok(new
                {
                    total,
                    page,
                    pageSize,
                    data = docs
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"An error occurred while retrieving history: {ex.Message}" });
            }
        }
        
        // ----------------------------
        [ApiExplorerSettings(GroupName = "Entity Advanced")]
        // DYNAMIC SEARCH
        // ----------------------------
        [HttpPost("search")]
        public async Task<IActionResult> Search(string entity, [FromBody] JsonElement body)
        {
            var query = JsonToBson(body);
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
                var result = docs.Select(BsonToDictionary).ToList();
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
        
        
        [ApiExplorerSettings(GroupName = "Entity Advanced")]
        // ----------------------------
        // DYNAMIC AGGREGATE
        // ----------------------------
        [HttpPost("aggregate")]
        public async Task<IActionResult> Aggregate(string entity, [FromBody] JsonElement body)
        {
            var pipeline = JsonToBsonArray(body);
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
                var ret = docs.Select(BsonToDictionary).ToList();
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
        
    }
}