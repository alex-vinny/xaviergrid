using MongoDB.Bson;
using MongoDB.Driver;
using System.Text.Json;

namespace DynamicMongoAPI.Services
{
    public interface IQueryService
    {
        FilterDefinition<BsonDocument> BuildFilterFromQuery(BsonDocument query);
        BsonDocument[] SanitizeAggregationPipeline(BsonArray pipeline);
        Task<IEnumerable<BsonDocument>> QueryAsync(string entity, JsonElement jsonQuery, MongoSchemaService schemaService);
    }
}