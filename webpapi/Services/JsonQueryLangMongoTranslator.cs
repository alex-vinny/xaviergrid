using MongoDB.Bson;
using MongoDB.Driver;
using System.Text.Json;

namespace DynamicMongoAPI.Services
{
    public class JsonQueryLangMongoTranslator : IJsonQueryLangMongoTranslator
    {
        public List<BsonDocument> Translate(JsonElement query)
        {
            var pipeline = new List<BsonDocument>();

            if (query.TryGetProperty("filter", out JsonElement filterElement))
            {
                pipeline.Add(new BsonDocument("$match",
                    BsonDocument.Parse(filterElement.GetRawText())));
            }

            if (query.TryGetProperty("sort", out JsonElement sortElement))
            {
                var sortDoc = new BsonDocument();
                
                foreach (var prop in sortElement.EnumerateObject())
                {
                    sortDoc[prop.Name] = prop.Value.GetString() == "desc" ? -1 : 1;
                }

                pipeline.Add(new BsonDocument("$sort", sortDoc));
            }

            if (query.TryGetProperty("map", out JsonElement mapElement))
            {
                var proj = new BsonDocument();

                foreach (var prop in mapElement.EnumerateObject())
                {
                    proj[prop.Name] = $"${prop.Value}";
                }

                pipeline.Add(new BsonDocument("$project", proj));
            }

            if (query.TryGetProperty("join", out JsonElement joinElement))
            {
                var lookup = new BsonDocument
                {
                    { "from", joinElement.GetProperty("from").GetString() },
                    { "localField", joinElement.GetProperty("localField").GetString() },
                    { "foreignField", joinElement.GetProperty("foreignField").GetString() },
                    { "as", joinElement.GetProperty("as").GetString() }
                };

                pipeline.Add(new BsonDocument("$lookup", lookup));
            }

            if (query.TryGetProperty("aggregate", out JsonElement aggregateElement))
            {
                var stages = JsonDocument.Parse(aggregateElement.GetRawText()).RootElement.EnumerateArray();

                foreach (var stage in stages)
                {
                    var doc = BsonDocument.Parse(stage.GetRawText());
                    pipeline.Add(doc);
                }
            }

            if (query.TryGetProperty("skip", out JsonElement skipElement))
            {
                pipeline.Add(new BsonDocument("$skip", skipElement.GetInt32()));
            }

            if (query.TryGetProperty("limit", out JsonElement limitElement))
            {
                pipeline.Add(new BsonDocument("$limit", limitElement.GetInt32()));
            }

            return pipeline;
        }

    }
}