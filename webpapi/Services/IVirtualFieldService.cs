using MongoDB.Bson;

namespace DynamicMongoAPI.Services
{
    public interface IVirtualFieldService
    {
        string EvaluateVirtualField(string expression, BsonDocument doc);
    }
}