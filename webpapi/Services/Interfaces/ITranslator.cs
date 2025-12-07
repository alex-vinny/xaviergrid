using DynamicMongoAPI.Models;
using MongoDB.Driver;
using System.Text.Json.Nodes;

namespace DynamicMongoAPI.Services
{
    public interface ITranslator
    {
        FilterDefinition<DynamicEntity> Translate(JsonObject query);
    }
}