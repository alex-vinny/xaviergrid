using DynamicMongoAPI.Constants;
using DynamicMongoAPI.Models;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace DynamicMongoAPI.Services
{
    public class SchemaService : BaseDataService, ISchemaService
    {
        private readonly IMongoCollection<EntitySchema> _schemas;
        private readonly INamespaceManagementService _nsAdmin;

        public SchemaService(
            IMongoClient client,
            IConfiguration config,
            IMetadataService metadataService,
            INamespaceManagementService nsAdmin)
            : base(client, config, metadataService)
        {
            var db = GetSchemaDatabase(); // get the schema database
            _schemas = db.GetCollection<EntitySchema>("schemas");
            _nsAdmin = nsAdmin;
        }

        public async Task<EntitySchema> GetSchemaAsync(string entityName, string namespaceName = "default")
        {
            var lowerEntity = entityName.ToLowerInvariant();
            var lowerNamespace = namespaceName.ToLowerInvariant();

            var schema = await _schemas
                .Find(s => s.EntityName == lowerEntity && s.Namespace == lowerNamespace)
                .FirstOrDefaultAsync();

            if (schema == null)
                SchemaException.NotDefined($"{lowerNamespace}.{lowerEntity}");

            return schema!;
        }

        public async Task<IList<EntitySchema>> ListSchemaAsync(string? namespaceName = null)
        {
            if (string.IsNullOrWhiteSpace(namespaceName))
                return await _schemas.Find(_ => true).ToListAsync();

            var ns = namespaceName.ToLowerInvariant();
            return await _schemas.Find(s => s.Namespace == ns).ToListAsync();
        }

        public async Task<EntitySchema[]> CreateSchemaAsync(params EntitySchema[] schemas)
        {
            foreach (var schema in schemas)
            {
                schema.Validate();
                schema.Namespace = schema.Namespace.ToLowerInvariant();
                schema.EntityName = schema.EntityName.ToLowerInvariant();

                bool exists = await _schemas
                    .Find(s => s.EntityName == schema.EntityName && s.Namespace == schema.Namespace)
                    .AnyAsync();

                if (exists)
                    SchemaException.AlreadyExists($"{schema.Namespace}.{schema.EntityName}");

                // delegate namespace/entity creation
                var entityRecord = await _nsAdmin.EnsureEntityExistsAsync(schema.Namespace, schema.EntityName);

                await _schemas.InsertOneAsync(schema);

                await _nsAdmin.UpdateEntitySchemaIdAsync(entityRecord.Id, schema.Id);
            }

            return schemas;
        }

        public async Task<EntitySchema> UpdateSchemaAsync(EntitySchema schema)
        {
            schema.Validate();
            schema.Namespace = schema.Namespace.ToLowerInvariant();
            schema.EntityName = schema.EntityName.ToLowerInvariant();

            var existing = await _schemas
                .Find(s => s.EntityName == schema.EntityName && s.Namespace == schema.Namespace)
                .FirstOrDefaultAsync();

            if (existing == null)
                SchemaException.NotFound($"{schema.Namespace}.{schema.EntityName}");

            schema.Id = existing.Id;

            // Ensure metadata exists
            var entityRecord = await _nsAdmin.EnsureEntityExistsAsync(schema.Namespace, schema.EntityName);

            // Update schema
            var filter = Builders<EntitySchema>.Filter.Eq(s => s.Id, schema.Id);
            await _schemas.ReplaceOneAsync(filter, schema);

            await _nsAdmin.UpdateEntitySchemaIdAsync(entityRecord.Id, schema.Id);

            return schema;
        }
    }
}