using DynamicMongoAPI.Models;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace DynamicMongoAPI.Services
{
    public class NamespaceManagementService : BaseDataService, INamespaceManagementService
    {
        private readonly IMongoCollection<NamespaceDefinition> _namespaces;
        private readonly IMongoCollection<EntityDefinition> _entities;

        public NamespaceManagementService(IMongoClient client, IConfiguration config)
            : base(client, config)
        {
            var db = GetSchemaDatabase();
            _namespaces = db.GetCollection<NamespaceDefinition>("namespaces");
            _entities = db.GetCollection<EntityDefinition>("entities");
        }
        
        public async Task<NamespaceDefinition> EnsureNamespaceExistsAsync(string namespaceName)
        {
            var ns = namespaceName.ToLowerInvariant();

            var existing = await _namespaces.Find(x => x.Name == ns).FirstOrDefaultAsync();
            if (existing != null) return existing;

            var newNs = new NamespaceDefinition
            {
                Id = ObjectId.GenerateNewId(),
                Name = ns
            };

            await _namespaces.InsertOneAsync(newNs);
            return newNs;
        }

        public async Task<EntityDefinition> EnsureEntityExistsAsync(string namespaceName, string entityName)
        {
            var ns = namespaceName.ToLowerInvariant();
            var en = entityName.ToLowerInvariant();

            var existing = await _entities
                .Find(e => e.Name == en && e.Namespace == ns)
                .FirstOrDefaultAsync();

            if (existing != null) return existing;

            var nsDef = await EnsureNamespaceExistsAsync(ns);

            var newEntity = new EntityDefinition
            {
                Id = ObjectId.GenerateNewId(),
                Name = en,
                Namespace = ns,
                NamespaceId = nsDef.Id
            };

            await _entities.InsertOneAsync(newEntity);
            return newEntity;
        }

        public async Task UpdateEntitySchemaIdAsync(ObjectId entityId, ObjectId schemaId)
        {
            var update = Builders<EntityDefinition>.Update.Set(e => e.SchemaId, schemaId);
            await _entities.UpdateOneAsync(e => e.Id == entityId, update);
        }
    }
}