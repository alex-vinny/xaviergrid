using MongoDB.Driver;

namespace DynamicMongoAPI.Services
{
    public abstract class BaseDataService
    {
        protected readonly IMongoClient _client;
        protected readonly string _dbName;

        protected BaseDataService(IMongoClient client, IConfiguration config)
        {
            _client = client;
            _dbName = config.GetValue<string>("Mongo:SchemaDatabase") ?? "schema_db";
        }

        protected IMongoDatabase GetSchemaDatabase() =>
            _client.GetDatabase(_dbName);
    }
}