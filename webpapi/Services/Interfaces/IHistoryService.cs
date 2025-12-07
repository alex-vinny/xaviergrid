using DynamicMongoAPI.Models;

namespace DynamicMongoAPI.Services
{
    public interface IHistoryService
    {
        Task RecordVersionAsync(string entityName, DynamicEntity entity);
        Task<DynamicEntity?> RestoreAsync(string entityName, string id, int? version = null);
        Task<IList<EntityHistory>> HistoryAsync(string entityName, string id, int page = 1, int pageSize = 50);
        Task<long> CountAsync(string entityName, string id);
        Task PurgeAsync(string entityName);
    }
}