using DynamicMongoAPI.Models;
using MongoDB.Driver;
using MongoDB.Bson;

namespace DynamicMongoAPI.Services
{
    public class MongoSchemaService
    {
        private readonly IMongoClient _client;
        private readonly string _masterSchemaDatabaseName;
        
        public MongoSchemaService(IMongoClient client, string masterSchemaDatabaseName)
        {
            _client = client;
            _masterSchemaDatabaseName = masterSchemaDatabaseName;
        }
        
        public async Task<EntitySchema> GetSchemaAsync(string entityName)
        {
            var masterDb = _client.GetDatabase(_masterSchemaDatabaseName);
            var collection = masterDb.GetCollection<EntitySchema>("entitySchemas");
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
    }
}