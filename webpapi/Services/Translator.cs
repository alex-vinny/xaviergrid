using DynamicMongoAPI.Models;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Text.Json.Nodes;

namespace DynamicMongoAPI.Services
{
    public class Translator : ITranslator
    {
        public FilterDefinition<DynamicEntity> Translate(JsonObject query)
        {
            var builder = Builders<DynamicEntity>.Filter;
            FilterDefinition<DynamicEntity> filter = builder.Empty;

            if (query.TryGetPropertyValue("filters", out var filtersNode) && filtersNode is JsonArray filters)
            {
                foreach (var f in filters.OfType<JsonObject>())
                {
                    filter &= BuildFilter(f, builder);
                }
            }

            return filter;
        }

        private FilterDefinition<DynamicEntity> BuildFilter(JsonObject filterObj, FilterDefinitionBuilder<DynamicEntity> builder)
        {
            if (filterObj.Count != 1)
                throw new ArgumentException("Invalid filter object");

            var op = filterObj.First().Key;
            var val = filterObj.First().Value;

            return op switch
            {
                "eq" => BuildComparison(val, builder, (field, v) => builder.Eq($"DynamicFields.{field}", BsonValue.Create(v))),
                "neq" => BuildComparison(val, builder, (field, v) => builder.Ne($"DynamicFields.{field}", BsonValue.Create(v))),
                "lt" => BuildComparison(val, builder, (field, v) => builder.Lt($"DynamicFields.{field}", BsonValue.Create(v))),
                "lte" => BuildComparison(val, builder, (field, v) => builder.Lte($"DynamicFields.{field}", BsonValue.Create(v))),
                "gt" => BuildComparison(val, builder, (field, v) => builder.Gt($"DynamicFields.{field}", BsonValue.Create(v))),
                "gte" => BuildComparison(val, builder, (field, v) => builder.Gte($"DynamicFields.{field}", BsonValue.Create(v))),
                "in" => BuildComparison(val, builder, (field, v) =>
                {
                    if (v is JsonArray arr)
                        return builder.In($"DynamicFields.{field}", arr.Select(x => BsonValue.Create(x?.ToString())));
                    throw new ArgumentException("Invalid 'in' filter format");
                }),
                "and" => LogicalOperator(val, builder, true),
                "or" => LogicalOperator(val, builder, false),
                _ => throw new NotSupportedException($"Unsupported operator '{op}'")
            };
        }

        private FilterDefinition<DynamicEntity> BuildComparison(JsonNode val, FilterDefinitionBuilder<DynamicEntity> builder,
            Func<string, object?, FilterDefinition<DynamicEntity>> comparator)
        {
            if (val is JsonArray arr && arr.Count == 2)
            {
                var field = arr[0]?.ToString();
                var value = arr[1];
                if (field == null) throw new ArgumentException("Invalid filter field");
                return comparator(field, value?.ToString());
            }
            throw new ArgumentException("Invalid comparison filter format");
        }

        private FilterDefinition<DynamicEntity> LogicalOperator(JsonNode val, FilterDefinitionBuilder<DynamicEntity> builder, bool isAnd)
        {
            if (val is not JsonArray arr || arr.Count == 0)
                return builder.Empty;

            var filters = arr.OfType<JsonObject>().Select(f => BuildFilter(f, builder)).ToList();
            return isAnd ? builder.And(filters) : builder.Or(filters);
        }
    }
}