using DynamicMongoAPI.Models;
using MongoDB.Bson;

namespace DynamicMongoAPI.Services
{
    public interface INamespaceManagementService
    {
        Task<NamespaceDefinition> EnsureNamespaceExistsAsync(string namespaceName);
        Task<EntityDefinition> EnsureEntityExistsAsync(string namespaceName, string entityName);
        Task UpdateEntitySchemaIdAsync(ObjectId entityId, ObjectId schemaId);
    }
}