using MongoDB.Driver;

namespace DynamicMongoAPI.Services
{
    public interface INamespaceService
    {
        Task<IList<string>> ListNamespacesAsync();
        Task<IList<string>> ListEntitiesAsync(string namespaceName);
        Task<string> GetNamespaceForEntityAsync(string entityName);
        Task<IMongoDatabase> GetNamespaceDatabaseAsync(string namespaceName);
        Task<IMongoCollection<T>> GetNamespaceCollectionAsync<T>(string namespaceName, string collectionName);
    }
}