using DynamicMongoAPI.Constants;
using DynamicMongoAPI.Models;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace DynamicMongoAPI.Services
{
    public sealed class SchemaException : Exception
    {
        private SchemaException(string message) : base(message) { }

        public static void NotDefined(string entityName)
            => throw new SchemaException($"Schema for entity '{entityName}' is not defined.");

        public static void AlreadyExists(string entityName)
            => throw new SchemaException($"Schema for entity '{entityName}' already exists. Use PUT to update.");

        public static void NotFound(string entityName)
            => throw new SchemaException($"Schema for entity '{entityName}' not found.");
    }

    public interface INamespaceManagementService
    {
        Task<NamespaceDefinition> EnsureNamespaceExistsAsync(string namespaceName);
        Task<EntityDefinition> EnsureEntityExistsAsync(string namespaceName, string entityName);
        Task UpdateEntitySchemaIdAsync(ObjectId entityId, ObjectId schemaId);
    }

    public interface INamespaceService
    {
        Task<IList<string>> ListNamespacesAsync();
        Task<IList<string>> ListEntitiesAsync(string namespaceName);
    }

    public interface ISchemaService
    {
        Task<EntitySchema> GetSchemaAsync(string entityName, string namespaceName = "default");
        Task<IList<EntitySchema>> ListSchemaAsync(string? namespaceName = null);
        Task<EntitySchema[]> CreateSchemaAsync(params EntitySchema[] schemas);
        Task<EntitySchema> UpdateSchemaAsync(EntitySchema schema);
    }

    public abstract class BaseDataService
    {
        protected readonly IMongoClient _client;
        protected readonly IMetadataService _metadataService;
        protected readonly string _schemaDbName;

        protected BaseDataService(IMongoClient client, IConfiguration config, IMetadataService metadataService)
        {
            _client = client;
            _metadataService = metadataService;
            _schemaDbName = config.GetValue<string>("Mongo:SchemaDatabase") ?? "schema_db";
        }

        protected IMongoDatabase GetSchemaDatabase() =>
            _client.GetDatabase(_schemaDbName);

        protected Task<IMongoDatabase> GetNamespaceDatabase(string namespaceName) =>
            _metadataService.GetNamespaceDatabaseAsync(namespaceName);
    }

    public class NamespaceManagementService : BaseDataService, INamespaceManagementService
    {
        private readonly IMongoCollection<NamespaceDefinition> _namespaces;
        private readonly IMongoCollection<EntityDefinition> _entities;

        public NamespaceManagementService(IMongoClient client, IConfiguration config, IMetadataService metadataService)
            : base(client, config, metadataService)
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