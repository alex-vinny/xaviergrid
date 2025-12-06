using MongoDB.Bson;
using System.Text.Json;

namespace DynamicMongoAPI.Services
{
    public interface IJsonQueryLangMongoTranslator
    {
        List<BsonDocument> Translate(JsonElement query);
    }
}