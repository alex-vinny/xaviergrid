using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using MongoDB.Bson;
using DynamicMongoAPI.Models;
using DynamicMongoAPI.Services;
using System.Text.Json;
using DynamicMongoAPI.Utils;

namespace DynamicMongoAPI.Controllers
{
    [ApiController]
    [Route("schemas")]
    [ApiExplorerSettings(GroupName = "Schemas")]
    public class SchemaController : ControllerBase
    {
        private readonly IMongoClient _client;
        private readonly MongoSchemaService _schemaService;
        private readonly string _masterSchemaDatabaseName;
        
        public SchemaController(IMongoClient client, MongoSchemaService schemaService, IConfiguration configuration)
        {
            _client = client;
            _schemaService = schemaService;
            _masterSchemaDatabaseName = configuration["MongoDB:MasterSchemaDatabase"] ?? "masterSchemas";
        }
        
        [HttpPost]
        public async Task<IActionResult> CreateSchemas([FromBody] JsonElement requestBody)
        {
            try
            {
                var masterDb = _client.GetDatabase(_masterSchemaDatabaseName);
                var schemasCollection = masterDb.GetCollection<EntitySchema>("schemas");
                
                // Check if the request body is an array or a single object
                if (requestBody.ValueKind == JsonValueKind.Array)
                {
                    // Handle array of schemas
                    var schemas = new List<EntitySchema>();
                    foreach (var item in requestBody.EnumerateArray())
                    {
                        try
                        {
                            var schema = System.Text.Json.JsonSerializer.Deserialize<EntitySchema>(item.GetRawText());
                            if (schema != null)
                            {
                                // Validate the schema before adding it
                                schema.Validate();
                                
                                // Ensure namespace exists
                                await _schemaService.EnsureNamespaceExists(schema.Namespace);
                                
                                // Ensure entity exists
                                await _schemaService.EnsureEntityExists(schema.Namespace, schema.EntityName);
                                
                                schemas.Add(schema);
                            }
                        }
                        catch (JsonException ex)
                        {
                            return BadRequest(new { error = $"Invalid JSON format: {ex.Message}" });
                        }
                    }
                    
                    await schemasCollection.InsertManyAsync(schemas);
                    return Ok(new { message = $"{schemas.Count} schemas created successfully" });
                }
                else if (requestBody.ValueKind == JsonValueKind.Object)
                {
                    // Handle single schema
                    try
                    {
                        var schema = System.Text.Json.JsonSerializer.Deserialize<EntitySchema>(requestBody.GetRawText());
                        if (schema == null)
                            return BadRequest(new { error = "Invalid schema format" });
                        
                        // Validate the schema before saving it
                        schema.Validate();
                        
                        // Ensure namespace exists
                        await _schemaService.EnsureNamespaceExists(schema.Namespace);
                        
                        // Ensure entity exists
                        await _schemaService.EnsureEntityExists(schema.Namespace, schema.EntityName);
                        
                        await schemasCollection.InsertOneAsync(schema);
                        return Ok(new { message = "Schema created successfully" });
                    }
                    catch (JsonException ex)
                    {
                        return BadRequest(new { error = $"Invalid JSON format: {ex.Message}" });
                    }
                }
                else
                {
                    return BadRequest(new { error = "Request body must be a schema object or an array of schemas" });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"An error occurred while creating schemas: {ex.Message}" });
            }
        }
        
        [HttpGet]
        public async Task<IActionResult> GetSchemas()
        {
            try
            {
                var masterDb = _client.GetDatabase(_masterSchemaDatabaseName);
                var collection = masterDb.GetCollection<EntitySchema>("entitySchemas");
                var schemas = await collection.Find(_ => true).ToListAsync();
                return Ok(schemas);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"An error occurred while retrieving schemas: {ex.Message}" });
            }
        }
        
        [HttpGet("{entityName}")]
        public async Task<IActionResult> GetSchema(string entityName)
        {
            try
            {
                var schema = await _schemaService.GetSchemaAsync(entityName);
                return Ok(schema);
            }
            catch (ArgumentException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"An error occurred while retrieving the schema: {ex.Message}" });
            }
        }
        
        // Get rules for a schema
        [HttpGet("{entityName}/rules")]
        [ApiExplorerSettings(GroupName = "Rules and Relations")]
        public async Task<IActionResult> GetSchemaRules(string entityName)
        {
            try
            {
                var schema = await _schemaService.GetSchemaAsync(entityName);
                return Ok(schema.Rules ?? new List<RuleDefinition>());
            }
            catch (ArgumentException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"An error occurred while retrieving schema rules: {ex.Message}" });
            }
        }
        
        // Get relations for a schema
        [HttpGet("{entityName}/relations")]
        [ApiExplorerSettings(GroupName = "Rules and Relations")]
        public async Task<IActionResult> GetSchemaRelations(string entityName)
        {
            try
            {
                var schema = await _schemaService.GetSchemaAsync(entityName);
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
        
        // Add rules to a schema
        [HttpPost("{entityName}/rules")]
        [ApiExplorerSettings(GroupName = "Rules and Relations")]
        public async Task<IActionResult> AddSchemaRules(string entityName, [FromBody] List<RuleDefinition> rules)
        {
            try
            {
                var schema = await _schemaService.GetSchemaAsync(entityName);
                
                var masterDb = _client.GetDatabase(_masterSchemaDatabaseName);
                var collection = masterDb.GetCollection<EntitySchema>("entitySchemas");
                
                // Add new rules to existing ones
                if (schema.Rules == null)
                    schema.Rules = new List<RuleDefinition>();
                
                schema.Rules.AddRange(rules);
                
                // Update the schema in database
                var filter = Builders<EntitySchema>.Filter.Eq(s => s.Id, schema.Id);
                await collection.ReplaceOneAsync(filter, schema);
                
                return Ok(new { message = $"{rules.Count} rules added successfully" });
            }
            catch (ArgumentException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"An error occurred while adding schema rules: {ex.Message}" });
            }
        }
        
        // Add relations to a schema
        [ApiExplorerSettings(GroupName = "Rules and Relations")]
        [HttpPost("{entityName}/relations")]
        public async Task<IActionResult> AddSchemaRelations(string entityName, [FromBody] List<SchemaRelation> relations)
        {
            try
            {
                var schema = await _schemaService.GetSchemaAsync(entityName);
                
                var masterDb = _client.GetDatabase(_masterSchemaDatabaseName);
                var collection = masterDb.GetCollection<EntitySchema>("entitySchemas");
                
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
        
        [ApiExplorerSettings(GroupName = "Rules and Relations")]
        // Delete rules from a schema
        [HttpDelete("{entityName}/rules")]
        public async Task<IActionResult> DeleteSchemaRules(string entityName)
        {
            try
            {
                var schema = await _schemaService.GetSchemaAsync(entityName);
                
                var masterDb = _client.GetDatabase(_masterSchemaDatabaseName);
                var collection = masterDb.GetCollection<EntitySchema>("entitySchemas");
                
                // Clear all rules
                schema.Rules = new List<RuleDefinition>();
                
                // Update the schema in database
                var filter = Builders<EntitySchema>.Filter.Eq(s => s.Id, schema.Id);
                await collection.ReplaceOneAsync(filter, schema);
                
                return Ok(new { message = "All rules deleted successfully" });
            }
            catch (ArgumentException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"An error occurred while deleting schema rules: {ex.Message}" });
            }
        }
        
        // Delete relations from a schema
        [ApiExplorerSettings(GroupName = "Rules and Relations")]
        [HttpDelete("{entityName}/relations")]
        public async Task<IActionResult> DeleteSchemaRelations(string entityName)
        {
            try
            {
                var schema = await _schemaService.GetSchemaAsync(entityName);
                
                var masterDb = _client.GetDatabase(_masterSchemaDatabaseName);
                var collection = masterDb.GetCollection<EntitySchema>("entitySchemas");
                
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
    }
}