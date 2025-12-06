using DynamicMongoAPI.Constants;
using DynamicMongoAPI.Models;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace DynamicMongoAPI.Services
{
    public interface ISchemaService
    {
        Task<EntitySchema> GetSchemaAsync(string entityName, string namespaceName = "default");
        Task<IList<EntitySchema>> ListSchemaAsync(string? namespaceName = null);
        Task<EntitySchema[]> CreateSchemaAsync(params EntitySchema[] schemas);
        Task<EntitySchema> UpdateSchemaAsync(EntitySchema schema);
    }
}