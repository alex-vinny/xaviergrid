using DynamicMongoAPI.Models;
using MongoDB.Bson;
using MongoDB.Driver;

namespace DynamicMongoAPI.Services
{
    public class HistoryService : BaseDataService, IHistoryService
    {
        private readonly ISchemaService _schemaService;
        private readonly INamespaceService _namespaceService;

        public HistoryService(
            IMongoClient client,
            IConfiguration config,
            ISchemaService schemaService,
            INamespaceService namespaceService)
            : base(client, config)
        {
            _schemaService = schemaService;
            _namespaceService = namespaceService;
        }

        /// <summary>
        /// Get the history collection for the entity via MetadataService
        /// </summary>
        private async Task<IMongoCollection<EntityHistory>> GetCollectionAsync(string entityName)
        {
            var schema = await _schemaService.GetSchemaAsync(entityName);
            return await _namespaceService.GetNamespaceCollectionAsync<EntityHistory>(
                schema.Namespace,
                $"{schema.EntityName}_history"
            );
        }

        public async Task RecordVersionAsync(string entityName, DynamicEntity entity)
        {
            var col = await GetCollectionAsync(entityName);

            // Compute next version
            var version = await col.CountDocumentsAsync(h => h.DocumentId == entity.Id) + 1;

            // Store all fields including system fields
            var history = new EntityHistory
            {
                DocumentId = entity.Id,
                Version = (int)version,
                Timestamp = DateTime.UtcNow,
                Data = entity.ToBsonDictionary()
            };

            await col.InsertOneAsync(history);
        }

        public async Task<DynamicEntity?> RestoreAsync(string entityName, string id, int? version = null)
        {
            var col = await GetCollectionAsync(entityName);
            var oid = ObjectId.Parse(id);

            var filter = Builders<EntityHistory>.Filter.Eq(h => h.DocumentId, oid);
            if (version.HasValue)
                filter &= Builders<EntityHistory>.Filter.Eq(h => h.Version, version.Value);

            var hist = await col.Find(filter)
                                .SortByDescending(h => h.Version)
                                .FirstOrDefaultAsync();

            return hist != null ? DynamicEntityExtensions.FromBsonDictionary(hist.Data) : null;
        }

        public async Task<IList<EntityHistory>> HistoryAsync(string entityName, string id, int page = 1, int pageSize = 50)
        {
            var col = await GetCollectionAsync(entityName);
            var oid = ObjectId.Parse(id);

            return await col.Find(h => h.DocumentId == oid)
                            .SortByDescending(h => h.Version)
                            .Skip((page - 1) * pageSize)
                            .Limit(pageSize)
                            .ToListAsync();
        }

        public async Task<long> CountAsync(string entityName, string id)
        {
            var col = await GetCollectionAsync(entityName);
            var oid = ObjectId.Parse(id);
            return await col.CountDocumentsAsync(h => h.DocumentId == oid);
        }

        public async Task PurgeAsync(string entityName)
        {
            var col = await GetCollectionAsync(entityName);
            await col.DeleteManyAsync(FilterDefinition<EntityHistory>.Empty);
        }
    }
}