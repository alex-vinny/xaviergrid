using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
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
    public class EntityRulesController : ControllerBase
    {
        private readonly IMongoClient _client;
        private readonly MongoSchemaService _schemaService;
        private readonly string _masterSchemaDatabaseName;
        
        public EntityRulesController(IMongoClient client, MongoSchemaService schemaService, IConfiguration configuration)
        {
            _client = client;
            _schemaService = schemaService;
            _masterSchemaDatabaseName = configuration["MongoDB:MasterSchemaDatabase"] ?? AppConstants.MasterSchemaDatabaseName;
        }
        
        // GET /schemas/{entity}/rules - Get schema rules
        [HttpGet("{entity}/rules")]
        public async Task<IActionResult> GetSchemaRules(string entity)
        {
            try
            {
                var schema = await _schemaService.GetSchemaAsync(entity);
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
        
        // GET /schemas/{entity}/rules/{ruleId} - Get a specific rule from schema
        [HttpGet("{entity}/rules/{ruleId}")]
        public async Task<IActionResult> GetSchemaRule(string entity, int ruleId)
        {
            try
            {
                var schema = await _schemaService.GetSchemaAsync(entity);
                
                // Check if rules exist
                if (schema.Rules == null || schema.Rules.Count == 0)
                    return NotFound(new { error = "No rules found for this schema" });
                
                // Check if rule index is valid
                if (ruleId < 0 || ruleId >= schema.Rules.Count)
                    return NotFound(new { error = $"Rule with ID '{ruleId}' not found" });
                
                return Ok(schema.Rules[ruleId]);
            }
            catch (ArgumentException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"An error occurred while retrieving schema rule: {ex.Message}" });
            }
        }
        
        // POST /schemas/{entity}/rules - Add rules to schema
        [HttpPost("{entity}/rules")]
        public async Task<IActionResult> AddSchemaRules(string entity, [FromBody] List<RuleDefinition> rules)
        {
            try
            {
                var schema = await _schemaService.GetSchemaAsync(entity);
                
                var masterDb = _client.GetDatabase(_masterSchemaDatabaseName);
                var collection = masterDb.GetCollection<EntitySchema>("schemas");

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
        
        // PUT /schemas/{entity}/rules/{ruleId} - Change a rule from schema
        [HttpPut("{entity}/rules/{ruleId}")]
        public async Task<IActionResult> UpdateSchemaRule(string entity, int ruleId, [FromBody] RuleDefinition updatedRule)
        {
            try
            {
                var schema = await _schemaService.GetSchemaAsync(entity);
                
                var masterDb = _client.GetDatabase(_masterSchemaDatabaseName);
                var collection = masterDb.GetCollection<EntitySchema>("schemas");

                // Check if rules exist
                if (schema.Rules == null)
                    return NotFound(new { error = "No rules found for this schema" });
                
                // Check if rule index is valid
                if (ruleId < 0 || ruleId >= schema.Rules.Count)
                    return NotFound(new { error = $"Rule with ID '{ruleId}' not found" });
                
                // Update the rule
                schema.Rules[ruleId] = updatedRule;
                
                // Update the schema in database
                var filter = Builders<EntitySchema>.Filter.Eq(s => s.Id, schema.Id);
                await collection.ReplaceOneAsync(filter, schema);
                
                return Ok(new { message = "Rule updated successfully" });
            }
            catch (ArgumentException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"An error occurred while updating schema rule: {ex.Message}" });
            }
        }
        
        // DELETE /schemas/{entity}/rules - Delete all rules from schema
        [HttpDelete("{entity}/rules")]
        public async Task<IActionResult> DeleteSchemaRules(string entity)
        {
            try
            {
                var schema = await _schemaService.GetSchemaAsync(entity);
                
                var masterDb = _client.GetDatabase(_masterSchemaDatabaseName);
                var collection = masterDb.GetCollection<EntitySchema>("schemas");

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
        
        // DELETE /schemas/{entity}/rules/{ruleId} - Delete a rule from schema
        [HttpDelete("{entity}/rules/{ruleId}")]
        public async Task<IActionResult> DeleteSchemaRule(string entity, int ruleId)
        {
            try
            {
                var schema = await _schemaService.GetSchemaAsync(entity);
                
                var masterDb = _client.GetDatabase(_masterSchemaDatabaseName);
                var collection = masterDb.GetCollection<EntitySchema>("schemas");

                // Check if rules exist
                if (schema.Rules == null || schema.Rules.Count == 0)
                    return NotFound(new { error = "No rules found for this schema" });
                
                // Check if rule index is valid
                if (ruleId < 0 || ruleId >= schema.Rules.Count)
                    return NotFound(new { error = $"Rule with ID '{ruleId}' not found" });
                
                // Remove the rule at the specified index
                schema.Rules.RemoveAt(ruleId);
                
                // Update the schema in database
                var filter = Builders<EntitySchema>.Filter.Eq(s => s.Id, schema.Id);
                await collection.ReplaceOneAsync(filter, schema);
                
                return Ok(new { message = "Rule deleted successfully" });
            }
            catch (ArgumentException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"An error occurred while deleting schema rule: {ex.Message}" });
            }
        }
    }
}