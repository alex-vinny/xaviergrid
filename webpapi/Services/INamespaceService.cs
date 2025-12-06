using DynamicMongoAPI.Constants;
using DynamicMongoAPI.Models;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace DynamicMongoAPI.Services
{
    public interface INamespaceService
    {
        Task<IList<string>> ListNamespacesAsync();
        Task<IList<string>> ListEntitiesAsync(string namespaceName);
    }
}