using MongoDB.Bson;
using DynamicMongoAPI.Models;

namespace DynamicMongoAPI.Services
{
    public interface IFieldFunctionService
    {
        void ApplyFieldFunctions(BsonDocument doc, EntitySchema schema);
    }
}