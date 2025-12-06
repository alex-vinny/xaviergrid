using DynamicMongoAPI.Models;

namespace DynamicMongoAPI.Services
{
    public interface IFieldFunctionService
    {
        Task ApplyFieldFunctionsAsync(DynamicEntity entity, EntitySchema schema);
        Task EvaluateVirtualFieldsAsync(DynamicEntity entity, EntitySchema schema);
    }
}