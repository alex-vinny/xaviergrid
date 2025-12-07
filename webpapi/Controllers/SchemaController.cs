using DynamicMongoAPI.Models;
using DynamicMongoAPI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace DynamicMongoAPI.Controllers
{
    [ApiController]
    [Route("schemas")]
    [ApiExplorerSettings(GroupName = "Schemas")]
    public class SchemaController : ControllerBase
    {
        private readonly ISchemaService _schemaService;
        private readonly JsonSerializerOptions _jsonOptions;
        
        public SchemaController(ISchemaService schemaService, IOptions<JsonOptions> jsonOptions)
        {
            _schemaService = schemaService;
            _jsonOptions = jsonOptions.Value.JsonSerializerOptions;
        }
        
        [HttpPost]
        public async Task<IActionResult> CreateSchemas([FromBody] JsonElement requestBody)
        {
            try
            {
                var schemas = new List<EntitySchema>();

                // Check if the request body is an array or a single object
                if (requestBody.ValueKind == JsonValueKind.Array)
                {
                    var entityNamesInRequest = new HashSet<string>();
                    foreach (var item in requestBody.EnumerateArray())
                    {
                        try
                        {
                            Console.WriteLine($"[CONTROLLER] Deserializing array item: {item.GetRawText()}");
                            var schema = JsonSerializer.Deserialize<EntitySchema>(item.GetRawText(), _jsonOptions);
                            // Diagnostic logging
                            Console.WriteLine($"[DIAGNOSTIC] Deserialized schema - EntityName: '{schema?.EntityName}', Raw JSON: {item.GetRawText()}");
                            if (schema != null)
                            {
                                // Validate the schema to ensure entity name is lowercase
                                schema.Validate();
                                
                                // Diagnostic logging after validation
                                Console.WriteLine($"[DIAGNOSTIC] After validation - EntityName: '{schema.EntityName}'");

                                // Check if this entity name is already in the current request (duplicate in array)
                                if (entityNamesInRequest.Contains(schema.EntityName))
                                    return Conflict(new { error = $"Duplicate schema for entity '{schema.EntityName}' in the same request." });

                                // Add entity name to the set for duplicate checking
                                entityNamesInRequest.Add(schema.EntityName);
                                
                                schemas.Add(schema);
                            }
                        }
                        catch (JsonException ex)
                        {
                            Console.WriteLine($"[CONTROLLER] JSON Exception: {ex}");
                            return BadRequest(new { error = $"Invalid JSON format: {ex.Message}" });
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[CONTROLLER] General Exception: {ex}");
                            throw;
                        }
                    }
                }
                else if (requestBody.ValueKind == JsonValueKind.Object)
                {
                    // Handle single schema
                    try
                    {
                        Console.WriteLine($"[CONTROLLER] Deserializing single schema: {requestBody.GetRawText()}");
                        var schema = JsonSerializer.Deserialize<EntitySchema>(requestBody.GetRawText(), _jsonOptions);
                        // Diagnostic logging
                        Console.WriteLine($"[DIAGNOSTIC] Deserialized single schema - EntityName: '{schema?.EntityName}', Raw JSON: {requestBody.GetRawText()}");
                        if (schema == null)
                            return BadRequest(new { error = "Invalid schema format" });

                        // Validate the schema before saving it
                        schema.Validate();
                        // Diagnostic logging after validation
                        Console.WriteLine($"[DIAGNOSTIC] After validation - EntityName: '{schema.EntityName}'");

                        schemas.Add(schema);
                    }
                    catch (JsonException ex)
                    {
                        Console.WriteLine($"[CONTROLLER] JSON Exception: {ex}");
                        return BadRequest(new { error = $"Invalid JSON format: {ex.Message}" });
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[CONTROLLER] General Exception: {ex}");
                        throw;
                    }
                }
                else
                {
                    return BadRequest(new { error = "Request body must be a schema object or an array of schemas" });
                }

                foreach (var schema in schemas)
                {
                    Console.WriteLine($"[DIAGNOSTIC] Creating schema with EntityName: '{schema.EntityName}'");
                    await _schemaService.CreateSchemaAsync(schema);
                }

                Console.WriteLine($"[DIAGNOSTIC] Returning response with schemas: [{string.Join(", ", schemas.Select(x => $"'{x.EntityName}'"))}]");
                return Ok(new
                {
                    schemas = schemas.Select(x => x.EntityName),
                    message = $"{schemas.Count} schema(s) created successfully"
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Exception in CreateSchemas: {ex}");
                return StatusCode(500, new { error = $"An error occurred while creating schemas: {ex.Message}" });
            }
        }

        [HttpPut("{entityName}")]
        [HttpPatch("{entityName}")]
        public async Task<IActionResult> UpdateSchema(string entityName, [FromBody] JsonElement requestBody)
        {
            try
            {
                var schema = JsonSerializer.Deserialize<EntitySchema>(requestBody.GetRawText());
                if (schema == null)
                    return BadRequest(new { error = "Invalid schema format" });

                if (string.IsNullOrEmpty(entityName) || schema.EntityName.Equals(entityName.ToLower()))
                    return BadRequest(new { error = "Mismatch entity enpoint for schema" });

                // Validate the schema before saving it
                schema.Validate();

                await _schemaService.UpdateSchemaAsync(schema);

                return Ok(new
                {
                    schema = schema.EntityName,
                    message = "Schema updated successfully"
                });
            }
            catch (JsonException ex)
            {
                return BadRequest(new { error = $"Invalid JSON format: {ex.Message}" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"An error occurred while updating schema: {ex.Message}" });
            }
        }
        
        [HttpGet]
        public async Task<IActionResult> GetSchemas()
        {
            try
            {
                var schemas = await _schemaService.ListSchemaAsync();
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
    }
}