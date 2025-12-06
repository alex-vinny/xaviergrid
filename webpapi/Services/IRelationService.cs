using DynamicMongoAPI.Models;

namespace DynamicMongoAPI.Services
{
    public interface IRelationService
    {
        Task<List<SchemaRelation>> GetRelationsAsync(string entityName);
        Task<SchemaRelation?> GetRelationAsync(string entityName, int relationId);
        Task AddRelationsAsync(string entityName, List<SchemaRelation> relations);
        Task UpdateRelationAsync(string entityName, int relationId, SchemaRelation relation);
        Task DeleteRelationsAsync(string entityName);
        Task DeleteRelationAsync(string entityName, int relationId);
        Task ApplyRelationsAsync(DynamicEntity entity, EntitySchema schema);
    }
}