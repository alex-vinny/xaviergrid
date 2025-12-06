using DynamicMongoAPI.Constants;
using DynamicMongoAPI.Models;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace DynamicMongoAPI.Services
{
    public class NamespaceService : BaseDataService, INamespaceService
    {
        private readonly IMongoCollection<NamespaceDefinition> _namespaces;
        private readonly IMongoCollection<EntityDefinition> _entities;

        public NamespaceService(IMongoClient client, IConfiguration config, IMetadataService metadataService)
            : base(client, config, metadataService)
        {
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
    }
}