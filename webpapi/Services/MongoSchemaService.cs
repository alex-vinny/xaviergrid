using DynamicMongoAPI.Constants;
using DynamicMongoAPI.Models;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace DynamicMongoAPI.Services
{
    public sealed class SchemaException : Exception
    {
        SchemaException(string message)
        {
            Message = message;
        }
        
        public override string Message { get; }

        public static void NotDefined(string entityName)
        {
            throw new SchemaException($"Schema for entity '{entityName}' not defined");
        }

        public static void AlreadyExists(string entityName)
        {
            throw new SchemaException($"Schema for entity '{entityName}' already exists. Use PUT to update.");
        }

        public static void NotFound(string entityName)
        {
            throw new SchemaException($"Schema for entity '{entityName}' not found");
        }
    }

    public interface ISchemaService
    {
        Task<EntitySchema> GetAsync(string entityName);
        Task<IList<EntitySchema>> ListAsync();
        Task<EntitySchema[]> CreateAsync(params EntitySchema[] schemas);
        Task<EntitySchema> UpdateAsync(EntitySchema schema);
    }

    public abstract class BaseDataService
    {
        protected readonly IMongoClient _client;
        protected readonly IMongoDatabase _masterDb;

        public BaseDataService(IMongoClient client, IConfiguration configuration)
        {
            _client = client;
            var dbName = configuration["MongoDB:MasterSchemaDatabase"] ?? AppConstants.MasterSchemaDatabaseName;
            _masterDb = _client.GetDatabase(dbName);
        }

        protected IMongoDatabase GetDatabase(EntitySchema schema)
        {
            var databaseName = string.IsNullOrEmpty(schema.Namespace) ? "default" : schema.Namespace;
            return _client.GetDatabase(databaseName);
        }

        protected IMongoCollection<T> GetCollection<T>(string collectionName)
        {
            return _masterDb.GetCollection<T>(collectionName);
        }
    }


    public class MongoSchemaService : BaseDataService, ISchemaService
    {
        private readonly IMongoCollection<EntitySchema> schemasCollection;

        public MongoSchemaService(IMongoClient client, IConfiguration configuration)
            : base(client, configuration)
        {
            schemasCollection = GetCollection<EntitySchema>("schemas");
        }
        
        public async Task<EntitySchema> GetAsync(string entityName)
        {
            var schema = await schemasCollection.Find(s => s.EntityName == entityName).FirstOrDefaultAsync();
            
            if (schema == null)
                SchemaException.NotDefined(entityName);

            return schema;
        }

        public async Task<EntitySchema[]> CreateAsync(params EntitySchema[] schemas)
        {
            foreach (var schema in schemas)
            {

                // Validate the schema before saving it
                schema.Validate();

                // Check if schema already exists
                var existing = await schemasCollection.Find(s => s.EntityName == schema.EntityName).FirstOrDefaultAsync();
                if (existing != null)
                    SchemaException.AlreadyExists(schema.EntityName);

                // Ensure namespace exists
                var namespaceId = await EnsureNamespaceExists(schema.Namespace);

                await schemasCollection.InsertOneAsync(schema);

                // Update entity with schema ID
                var entity = await EnsureEntityExists(schema.Namespace, schema.EntityName);
                if (entity != null && !string.IsNullOrEmpty(schema.Id))
                {
                    await UpdateEntityWithSchemaId(entity["_id"].AsObjectId.ToString(), schema.Id);
                }
            }

            return schemas;
        }

        public async Task<EntitySchema> UpdateAsync(EntitySchema schema)
        {
            // Check if schema exists
            var existing = await schemasCollection.Find(s => s.EntityName == schema.EntityName).FirstOrDefaultAsync();
            if (existing == null)
                SchemaException.NotFound(schema.EntityName);

            // Keep the same ID
            schema.Id = existing.Id;

            // Ensure namespace exists if changed
            await EnsureNamespaceExists(schema.Namespace);

            // Replace the schema
            var filter = Builders<EntitySchema>.Filter.Eq(s => s.Id, existing.Id);
            await schemasCollection.ReplaceOneAsync(filter, schema);

            // Update entity with schema ID
            var entity = await EnsureEntityExists(schema.Namespace, schema.EntityName);
            if (entity != null && !string.IsNullOrEmpty(schema.Id))
            {
                await UpdateEntityWithSchemaId(entity["_id"].AsObjectId.ToString(), schema.Id);
            }

            return schema;
        }

        public async Task<IList<EntitySchema>> ListAsync()
        {
            return await schemasCollection.Find(_ => true).ToListAsync();
        }

        async Task<string> EnsureNamespaceExists(string namespaceName)
        {
            var namespacesCollection = _masterDb.GetCollection<BsonDocument>("namespaces");
            var filter = Builders<BsonDocument>.Filter.Eq("name", namespaceName.ToLower());
            var existing = await namespacesCollection.Find(filter).FirstOrDefaultAsync();
            
            if (existing != null)
            {
                return existing["_id"].AsObjectId.ToString();
            }
            
            var newNamespace = new BsonDocument
            {
                { "_id", ObjectId.GenerateNewId() },
                { "name", namespaceName.ToLower() }
            };
            
            await namespacesCollection.InsertOneAsync(newNamespace);
            return newNamespace["_id"].AsObjectId.ToString();
        }       
        
        async Task<BsonDocument> EnsureEntityExists(string namespaceName, string entityName)
        {
            var entitiesCollection = _masterDb.GetCollection<BsonDocument>("entities");
            var filter = Builders<BsonDocument>.Filter.Eq("name", entityName.ToLower()) &
                         Builders<BsonDocument>.Filter.Eq("namespace", namespaceName.ToLower());
            
            var existing = await entitiesCollection.Find(filter).FirstOrDefaultAsync();
            
            if (existing != null)
            {
                return existing;
            }
            
            // Get the namespace ID
            var namespacesCollection = _masterDb.GetCollection<BsonDocument>("namespaces");
            var namespaceFilter = Builders<BsonDocument>.Filter.Eq("name", namespaceName.ToLower());
            var namespaceDoc = await namespacesCollection.Find(namespaceFilter).FirstOrDefaultAsync();
            
            if (namespaceDoc != null)
            {
                var newEntity = new BsonDocument
                {
                    { "_id", ObjectId.GenerateNewId() },
                    { "name", entityName.ToLower() },
                    { "namespace", namespaceName.ToLower() },
                    { "id_namespace", namespaceDoc["_id"].AsObjectId }
                };
                
                await entitiesCollection.InsertOneAsync(newEntity);
                return newEntity;
            }
            
            return null;
        }
        
        public async Task UpdateEntityWithSchemaId(string entityId, string schemaId)
        {
            var entitiesCollection = _masterDb.GetCollection<BsonDocument>("entities");
            var filter = Builders<BsonDocument>.Filter.Eq("_id", ObjectId.Parse(entityId));
            var update = Builders<BsonDocument>.Update.Set("id_schema", ObjectId.Parse(schemaId));
            
            await entitiesCollection.UpdateOneAsync(filter, update);
        }
    }
}