using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using MongoDB.Bson;
using DynamicMongoAPI.Services;

namespace DynamicMongoAPI.Controllers
{
    [ApiController]
    [Route("api/namespaces")]
    [ApiExplorerSettings(GroupName = "Namespaces")]
    public class NamespaceController : ControllerBase
    {
        private readonly IMongoClient _client;
        private readonly string _masterSchemaDatabaseName;
        
        public NamespaceController(IMongoClient client, IConfiguration configuration)
        {
            _client = client;
            _masterSchemaDatabaseName = configuration["MongoDB:MasterSchemaDatabase"] ?? "xgrid";
        }
        
        /// <summary>
        /// Get all namespaces
        /// </summary>
        /// <returns>List of namespace names</returns>
        [HttpGet]
        public async Task<IActionResult> GetNamespaces()
        {
            try
            {
                var masterDb = _client.GetDatabase(_masterSchemaDatabaseName);
                var namespacesCollection = masterDb.GetCollection<BsonDocument>("namespaces");
                
                var namespaces = await namespacesCollection
                    .Find(_ => true)
                    .ToListAsync();
                
                // Extract just the namespace names as string array
                var namespaceNames = namespaces
                    .Select(n => n.GetValue("name", "").AsString)
                    .Where(n => !string.IsNullOrEmpty(n))
                    .ToArray();
                
                return Ok(namespaceNames);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"An error occurred while retrieving namespaces: {ex.Message}" });
            }
        }
        
        /// <summary>
        /// Get all entities for a specific namespace
        /// </summary>
        /// <param name="ns">The namespace name</param>
        /// <returns>List of entity names</returns>
        [HttpGet("/api/{ns}/entities")]
        public async Task<IActionResult> GetEntitiesByNamespace(string ns)
        {
            try
            {
                var masterDb = _client.GetDatabase(_masterSchemaDatabaseName);
                var entitiesCollection = masterDb.GetCollection<BsonDocument>("entities");
                
                var filter = Builders<BsonDocument>.Filter.Eq("namespace", ns.ToLower());
                var entities = await entitiesCollection
                    .Find(filter)
                    .ToListAsync();
                
                // Extract just the entity names as string array
                var entityNames = entities
                    .Select(e => e.GetValue("name", "").AsString)
                    .Where(n => !string.IsNullOrEmpty(n))
                    .ToArray();
                
                return Ok(entityNames);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"An error occurred while retrieving entities: {ex.Message}" });
            }
        }
    }
}

