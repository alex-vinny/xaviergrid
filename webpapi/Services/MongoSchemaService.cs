using DynamicMongoAPI.Models;
using MongoDB.Driver;
using MongoDB.Bson;
using System.Threading.Tasks;

namespace DynamicMongoAPI.Services
{
    public class MongoSchemaService
    {
        private readonly IMongoClient _client;
        private readonly string _masterSchemaDatabaseName;
        private readonly IMongoDatabase _masterDb;
        
        public MongoSchemaService(IMongoClient client, string masterSchemaDatabaseName)
        {
            _client = client;
            _masterSchemaDatabaseName = masterSchemaDatabaseName;
            _masterDb = _client.GetDatabase(_masterSchemaDatabaseName);
        }
        
        public async Task<EntitySchema> GetSchemaAsync(string entityName)
        {
            var collection = _masterDb.GetCollection<EntitySchema>("schemas");
            var schema = await collection.Find(s => s.EntityName == entityName).FirstOrDefaultAsync();
            
            if (schema == null)
                throw new ArgumentException($"Schema for entity '{entityName}' not defined");
                
            return schema;
        }
        
        public IMongoDatabase GetDatabase(EntitySchema schema)
        {
            var databaseName = string.IsNullOrEmpty(schema.Namespace) ? "default" : schema.Namespace;
            return _client.GetDatabase(databaseName);
        }
        
        public async Task<string> EnsureNamespaceExists(string namespaceName)
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
        
        public async Task<BsonDocument> EnsureEntityExists(string namespaceName, string entityName)
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