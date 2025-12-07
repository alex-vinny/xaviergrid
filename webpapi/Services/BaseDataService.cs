using DynamicMongoAPI.Utils;
using MongoDB.Driver;

namespace DynamicMongoAPI.Services
{
    public abstract class BaseDataService
    {
        protected readonly IMongoClient _client;
        protected readonly string _dbName;

        protected BaseDataService(IMongoClient client, IConfigurator config)
        {
            _client = client;
            _dbName = config.GetDatabaseName();
        }

        protected IMongoDatabase GetSchemaDatabase() =>
            _client.GetDatabase(_dbName);
    }
}