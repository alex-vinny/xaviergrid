using DynamicMongoAPI.Models;

namespace DynamicMongoAPI.Services
{
    public interface IRuleWarningService
    {
        Task<IList<string>> EvaluateWarningsAsync(EntitySchema schema, DynamicEntity entity);
    }
}