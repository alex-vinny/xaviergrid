using DynamicMongoAPI.Services;
using DynamicMongoAPI.Utils;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace DynamicMongoAPI.Controllers
{
    [ApiController]
    [Route("api/{entity}")]
    [ApiExplorerSettings(GroupName = "Crud Entity")]
    public class EntityCrudController : ControllerBase
    {
        private readonly IDynamicEntityService _dynamicEntityService;
        private readonly ILogger<EntityCrudController> _logger;

        public EntityCrudController(IDynamicEntityService dynamicEntityService, ILogger<EntityCrudController> logger)
        {
            _dynamicEntityService = dynamicEntityService;
            _logger = logger;
        }

        // ----------------------------
        // CREATE DOCUMENT
        // ----------------------------
        [HttpPost]
        public async Task<IActionResult> Create(string entity, [FromBody] JsonElement body)
        {
            if (BsonConverter.IsReservedRoute(entity))
                return NotFound(new { error = $"Entity '{entity}' is reserved and cannot be used" });

            try
            {
                var bsonDoc = BsonConverter.JsonToBson(body);
                var dynamicEntity = DynamicEntityExtensions.FromBsonDocument(bsonDoc);

                var created = await _dynamicEntityService.CreateAsync(entity, dynamicEntity);

                return Ok(created.ToDictionary());
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
        // GET ALL DOCUMENTS WITH PAGINATION
        // ----------------------------
        [HttpGet]
        public async Task<IActionResult> GetAll(string entity, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
        {
            try
            {
                var list = await _dynamicEntityService.ListAsync(entity, page, pageSize);
                var result = list.Select(d => d.ToDictionary()).ToList();
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"An error occurred while retrieving documents: {ex.Message}" });
            }
        }

        // ----------------------------
        // QUERY DOCUMENTS
        // ----------------------------
        [HttpPost("query")]
        public async Task<IActionResult> Query(string entity, [FromBody] JsonElement body)
        {
            try
            {
                var query = JsonNode.Parse(body.GetRawText())!.AsObject();
                var list = await _dynamicEntityService.QueryAsync(entity, query);
                var result = list.Select(d => d.ToDictionary()).ToList();
                return Ok(result);
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
                return StatusCode(500, new { error = $"An error occurred while querying documents: {ex.Message}" });
            }
        }

        // ----------------------------
        // GET BY ID + auto-join + virtual fields
        // ----------------------------
        [HttpGet("{id}")]
        public async Task<IActionResult> Get(string entity, string id)
        {
            if (BsonConverter.IsReservedRoute(entity))
                return NotFound(new { error = $"Entity '{entity}' is reserved and cannot be used" });

            try
            {
                var doc = await _dynamicEntityService.GetAsync(entity, id);
                if (doc == null)
                    return NotFound(new { error = $"Document '{id}' not found in '{entity}'" });

                return Ok(doc.ToDictionary());
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
        // UPDATE DOCUMENT
        // ----------------------------
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string entity, string id, [FromBody] JsonElement body)
        {
            try
            {
                var updateDoc = BsonConverter.JsonToBson(body);
                var dynamicEntity = DynamicEntityExtensions.FromBsonDocument(updateDoc);

                var updated = await _dynamicEntityService.UpdateAsync(entity, id, dynamicEntity);

                return Ok(updated.ToDictionary());
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
        // DELETE DOCUMENT (soft delete)
        // ----------------------------
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string entity, string id)
        {
            try
            {
                await _dynamicEntityService.DeleteAsync(entity, id);
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