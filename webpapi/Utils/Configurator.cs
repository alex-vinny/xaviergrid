using MongoDB.Driver;
using DynamicMongoAPI.Constants;

namespace DynamicMongoAPI.Utils
{
    public class Configurator
    {
        private readonly IConfiguration _configuration;
        private readonly IMongoClient _mongoClient;
        
        public Configurator(IConfiguration configuration, IMongoClient mongoClient)
        {
            _configuration = configuration;
            _mongoClient = mongoClient;
        }
        
        public string GetMongoConnectionString()
        {
            return Environment.GetEnvironmentVariable("MONGODB_CONNECTION")
                ?? _configuration.GetConnectionString("MongoDB")
                ?? _configuration["MongoDB:ConnectionString"]
                ?? throw new InvalidOperationException("MongoDB connection string not found");
        }
        
        public string GetMasterSchemaDatabaseName()
        {
            return Environment.GetEnvironmentVariable("MONGODB_MASTER_DATABASE")
                ?? _configuration["MongoDB:MasterSchemaDatabase"]
                ?? AppConstants.MasterSchemaDatabaseName;
        }
        
        public IMongoClient GetMongoClient()
        {
            return _mongoClient;
        }
        
        public IMongoDatabase GetMasterDatabase()
        {
            return _mongoClient.GetDatabase(GetMasterSchemaDatabaseName());
        }
    }
}