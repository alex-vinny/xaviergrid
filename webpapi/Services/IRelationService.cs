using MongoDB.Bson;
using MongoDB.Driver;
using DynamicMongoAPI.Models;

namespace DynamicMongoAPI.Services
{
    public interface IRelationService
    {
        Task ApplyRelations(BsonDocument doc, EntitySchema schema, IMongoDatabase db);
    }
}