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
            var collection = _masterDb.GetCollection<EntitySchema>("entitySchemas");
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
        
        public async Task EnsureNamespaceExists(string namespaceName)
        {
            var namespacesCollection = _masterDb.GetCollection<BsonDocument>("namespaces");
            var filter = Builders<BsonDocument>.Filter.Eq("name", namespaceName.ToLower());
            var existing = await namespacesCollection.Find(filter).FirstOrDefaultAsync();
            
            if (existing == null)
            {
                await namespacesCollection.InsertOneAsync(new BsonDocument
                {
                    { "name", namespaceName.ToLower() }
                });
            }
        }
        
        public async Task EnsureEntityExists(string namespaceName, string entityName)
        {
            var entitiesCollection = _masterDb.GetCollection<BsonDocument>("entities");
            var filter = Builders<BsonDocument>.Filter.Eq("name", entityName.ToLower()) &
                         Builders<BsonDocument>.Filter.Eq("namespace", namespaceName.ToLower());
            
            var existing = await entitiesCollection.Find(filter).FirstOrDefaultAsync();
            
            if (existing == null)
            {
                await entitiesCollection.InsertOneAsync(new BsonDocument
                {
                    { "name", entityName.ToLower() },
                    { "namespace", namespaceName.ToLower() }
                });
            }
        }
    }
}