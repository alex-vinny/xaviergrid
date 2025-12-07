using DynamicMongoAPI.Models;
using System.Text.Json.Nodes;

namespace DynamicMongoAPI.Services
{
    public interface IDynamicEntityService
    {
        Task<DynamicEntity?> GetAsync(string entityName, string id);
        Task<DynamicEntity> CreateAsync(string entityName, DynamicEntity entity);
        Task<IList<DynamicEntity>> ListAsync(string entityName, int page = 1, int pageSize = 50);
        Task<long> CountAsync(string entityName);
        Task<DynamicEntity> UpdateAsync(string entityName, string id, DynamicEntity updated);
        Task DeleteAsync(string entityName, string id);
        Task<IList<DynamicEntity>> QueryAsync(string entityName, JsonObject query);
    }
}
