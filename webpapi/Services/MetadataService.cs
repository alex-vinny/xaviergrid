using DynamicMongoAPI.Models;
using MongoDB.Driver;

namespace DynamicMongoAPI.Services
{
    public class MetadataService : IMetadataService
    {
        private readonly IMongoClient _client;
        private readonly ISchemaService _schemaService;
        private readonly string _schemaDbName;

        public MetadataService(IMongoClient client, ISchemaService schemaService, IConfiguration config)
        {
            _client = client;
            _schemaService = schemaService;
            _schemaDbName = config.GetValue<string>("Mongo:SchemaDatabase") ?? "schema_db";
        }

        public async Task<string> GetNamespaceForEntityAsync(string entityName)
        {
            var schema = await _schemaService.GetSchemaAsync(entityName);
            return schema.Namespace;
        }

        public Task<IMongoDatabase> GetNamespaceDatabaseAsync(string namespaceName)
        {
            // Namespace == Database name
            return Task.FromResult(_client.GetDatabase(namespaceName));
        }

        public async Task<IMongoCollection<T>> GetNamespaceCollectionAsync<T>(
            string namespaceName,
            string collectionName)
        {
            var db = await GetNamespaceDatabaseAsync(namespaceName);
            return db.GetCollection<T>(collectionName);
        }
    }   
}