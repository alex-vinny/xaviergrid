using DynamicMongoAPI.Models;
using DynamicMongoAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace DynamicMongoAPI.Controllers
{
    [ApiController]
    [Route("schemas")]
    [ApiExplorerSettings(GroupName = "Entity Relations")]
    public class EntityRulesController : ControllerBase
    {
        private readonly ISchemaService _schemaService;

        public EntityRulesController(ISchemaService schemaService)
        {
            _schemaService = schemaService;
        }

        // ------------------------------------------
        // GET /schemas/{entity}/rules
        // ------------------------------------------
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

        // ------------------------------------------
        // GET /schemas/{entity}/rules/{ruleId}
        // ------------------------------------------
        [HttpGet("{entity}/rules/{ruleId}")]
        public async Task<IActionResult> GetSchemaRule(string entity, int ruleId)
        {
            try
            {
                var schema = await _schemaService.GetSchemaAsync(entity);

                if (schema.Rules == null || ruleId < 0 || ruleId >= schema.Rules.Count)
                    return NotFound(new { error = "Rule not found" });

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

        // ------------------------------------------
        // POST /schemas/{entity}/rules
        // Add new rule(s)
        // ------------------------------------------
        [HttpPost("{entity}/rules")]
        public async Task<IActionResult> AddSchemaRules(string entity, [FromBody] List<RuleDefinition> rules)
        {
            try
            {
                var schema = await _schemaService.GetSchemaAsync(entity);

                schema.Rules ??= new List<RuleDefinition>();
                schema.Rules.AddRange(rules);

                schema.Validate(); // revalidate entire schema

                await _schemaService.UpdateSchemaAsync(schema);

                return Ok(new { message = $"{rules.Count} rule(s) added successfully" });
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

        // ------------------------------------------
        // PUT /schemas/{entity}/rules/{ruleId}
        // Update an existing rule
        // ------------------------------------------
        [HttpPut("{entity}/rules/{ruleId}")]
        public async Task<IActionResult> UpdateSchemaRule(string entity, int ruleId, [FromBody] RuleDefinition updatedRule)
        {
            try
            {
                var schema = await _schemaService.GetSchemaAsync(entity);

                if (schema.Rules == null || ruleId < 0 || ruleId >= schema.Rules.Count)
                    return NotFound(new { error = "Rule not found" });

                schema.Rules[ruleId] = updatedRule;
                schema.Validate();

                await _schemaService.UpdateSchemaAsync(schema);

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

        // ------------------------------------------
        // DELETE /schemas/{entity}/rules
        // Delete ALL rules
        // ------------------------------------------
        [HttpDelete("{entity}/rules")]
        public async Task<IActionResult> DeleteSchemaRules(string entity)
        {
            try
            {
                var schema = await _schemaService.GetSchemaAsync(entity);

                schema.Rules = new List<RuleDefinition>();
                await _schemaService.UpdateSchemaAsync(schema);

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

        // ------------------------------------------
        // DELETE /schemas/{entity}/rules/{ruleId}
        // ------------------------------------------
        [HttpDelete("{entity}/rules/{ruleId}")]
        public async Task<IActionResult> DeleteSchemaRule(string entity, int ruleId)
        {
            try
            {
                var schema = await _schemaService.GetSchemaAsync(entity);

                if (schema.Rules == null || ruleId < 0 || ruleId >= schema.Rules.Count)
                    return NotFound(new { error = "Rule not found" });

                schema.Rules.RemoveAt(ruleId);
                await _schemaService.UpdateSchemaAsync(schema);

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