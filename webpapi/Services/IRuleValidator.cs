using DynamicMongoAPI.Models;

namespace DynamicMongoAPI.Services
{
    public interface IRuleValidator
    {
        Task ValidateAsync(EntitySchema schema, DynamicEntity entity, RuleAction action);
    }
}