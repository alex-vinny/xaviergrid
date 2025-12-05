using MongoDB.Bson;
using DynamicMongoAPI.Models;

namespace DynamicMongoAPI.Services
{
    public interface IValidationService
    {
        void ValidateEnums(BsonDocument doc, EntitySchema schema);
        void CheckRules(List<RuleDefinition>? rules, string action, BsonDocument doc);
    }
}