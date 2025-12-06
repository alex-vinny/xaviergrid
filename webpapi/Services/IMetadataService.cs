using MongoDB.Driver;

namespace DynamicMongoAPI.Services
{
    public interface IMetadataService
    {
        Task<string> GetNamespaceForEntityAsync(string entityName);
        Task<IMongoDatabase> GetNamespaceDatabaseAsync(string namespaceName);
        Task<IMongoCollection<T>> GetNamespaceCollectionAsync<T>(string namespaceName, string collectionName);
    }
}