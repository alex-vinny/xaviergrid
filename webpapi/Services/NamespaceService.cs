using DynamicMongoAPI.Models;
using DynamicMongoAPI.Utils;
using MongoDB.Driver;

namespace DynamicMongoAPI.Services
{
    public class NamespaceService : BaseDataService, INamespaceService
    {
        private readonly ISchemaService _schemaService;
        private readonly IMongoCollection<NamespaceDefinition> _namespaces;
        private readonly IMongoCollection<EntityDefinition> _entities;

        public NamespaceService(IMongoClient client, IConfigurator config, ISchemaService schemaService)
            : base(client, config)
        {
            _schemaService = schemaService;

            var db = GetSchemaDatabase(); // get the schema database
            _namespaces = db.GetCollection<NamespaceDefinition>("namespaces");
            _entities = db.GetCollection<EntityDefinition>("entities");
        }


        public async Task<IList<string>> ListNamespacesAsync()
        {
            return await _namespaces
                .Find(_ => true)
                .Project(n => n.Name)
                .ToListAsync();
        }

        public async Task<IList<string>> ListEntitiesAsync(string namespaceName)
        {
            var ns = namespaceName.ToLowerInvariant();

            return await _entities
                .Find(e => e.Namespace == ns)
                .Project(e => e.Name)
                .ToListAsync();
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