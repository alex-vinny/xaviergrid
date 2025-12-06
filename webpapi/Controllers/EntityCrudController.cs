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
    [ApiExplorerSettings(GroupName = "Crud Entity")]
    public class EntityCrudController : ControllerBase
    {
        private readonly MongoSchemaService _schemaService;
        private readonly DynamicMongoService _dynamicMongoService;
        
        public EntityCrudController(MongoSchemaService schemaService, DynamicMongoService dynamicMongoService)
        {
            _schemaService = schemaService;
            _dynamicMongoService = dynamicMongoService;
        }
        
        // ----------------------------
        // CREATE DOCUMENT
        // ----------------------------
        [HttpPost]
        [ApiExplorerSettings(GroupName = "Crud Entity")]
        public async Task<IActionResult> Create(string entity, [FromBody] JsonElement body)
        {
            var doc = BsonConverter.JsonToBson(body);
            // Ignore reserved routes
            if (BsonConverter.IsReservedRoute(entity))
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
                
                return Ok(BsonConverter.BsonToDictionary(doc));
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
            if (BsonConverter.IsReservedRoute(entity))
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
                
                return Ok(BsonConverter.BsonToDictionary(doc));
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
            var updateDoc = BsonConverter.JsonToBson(body);
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
        // DELETE (soft delete)
        // ----------------------------
        [HttpDelete("{id}")]
        [ApiExplorerSettings(GroupName = "Crud Entity")]
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
    }
}