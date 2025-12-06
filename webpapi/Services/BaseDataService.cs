using DynamicMongoAPI.Constants;
using DynamicMongoAPI.Models;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace DynamicMongoAPI.Services
{
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
}