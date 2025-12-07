using DynamicMongoAPI.Constants;

namespace DynamicMongoAPI.Utils
{
    public interface IConfigurator
    {
        string GetConnectionString();
        string GetDatabaseName();
    }

    public class Configurator : IConfigurator
    {
        private readonly IConfiguration _configuration;
        
        public Configurator(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        
        public string GetConnectionString()
        {
            return Environment.GetEnvironmentVariable("MONGODB_CONNECTION")
                ?? _configuration.GetConnectionString("MongoDB")
                ?? _configuration["MongoDB:ConnectionString"]
                ?? throw new InvalidOperationException("MongoDB connection string not found");
        }
        
        public string GetDatabaseName()
        {
            return Environment.GetEnvironmentVariable("MONGODB_DATABASE")
                ?? _configuration["MongoDB:DatabaseName"]
                ?? AppConstants.DatabaseName;
        }
    }
}