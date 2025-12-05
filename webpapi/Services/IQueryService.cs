using MongoDB.Bson;
using MongoDB.Driver;

namespace DynamicMongoAPI.Services
{
    public interface IQueryService
    {
        FilterDefinition<BsonDocument> BuildFilterFromQuery(BsonDocument query);
        BsonDocument[] SanitizeAggregationPipeline(BsonArray pipeline);
    }
}