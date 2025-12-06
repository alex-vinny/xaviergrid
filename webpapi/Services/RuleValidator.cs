using DynamicMongoAPI.Models;
using MongoDB.Bson;
using System.Text.Json.Nodes;

namespace DynamicMongoAPI.Services
{
    public class RuleValidator : IRuleValidator
    {
        public Task ValidateAsync(EntitySchema schema, DynamicEntity entity, RuleAction action)
        {
            if (schema.Rules == null)
                return Task.CompletedTask;

            foreach (var rule in schema.Rules.Where(r => r.Action == action))
            {
                if (rule.Rule?.Jql == null)
                    continue;

                if (!EvaluateJql(rule.Rule.Jql, entity))
                    throw new InvalidOperationException(rule.Message ?? "Rule violation detected");
            }

            return Task.CompletedTask;
        }

        private bool EvaluateJql(JsonNode jql, DynamicEntity entity)
        {
            if (jql is JsonObject obj && obj.Count == 1)
            {
                var op = obj.First().Key;
                var val = obj.First().Value;

                return op switch
                {
                    "eq" => Compare(val, entity, (a, b) => a?.Equals(b) ?? false),
                    "neq" => Compare(val, entity, (a, b) => !(a?.Equals(b) ?? false)),
                    "lt" => Compare(val, entity, (a, b) => CompareNumeric(a, b) < 0),
                    "lte" => Compare(val, entity, (a, b) => CompareNumeric(a, b) <= 0),
                    "gt" => Compare(val, entity, (a, b) => CompareNumeric(a, b) > 0),
                    "gte" => Compare(val, entity, (a, b) => CompareNumeric(a, b) >= 0),
                    "in" => InOperator(val, entity),
                    "and" => LogicalArray(val, entity, true),
                    "or" => LogicalArray(val, entity, false),
                    _ => throw new NotSupportedException($"Unsupported JQL operator '{op}'")
                };
            }

            throw new ArgumentException("Invalid JQL format");
        }

        private bool Compare(JsonNode val, DynamicEntity entity, Func<object?, object?, bool> comparer)
        {
            if (val is JsonArray arr && arr.Count == 2)
            {
                var fieldName = arr[0]?.ToString();
                var targetValue = arr[1]?.ToString();
                if (fieldName == null) return false;

                var entityValue = entity.DynamicFields.TryGetValue(fieldName, out var bv)
                    ? BsonToComparable(bv)
                    : null;

                return comparer(entityValue, targetValue);
            }
            return false;
        }

        private bool InOperator(JsonNode val, DynamicEntity entity)
        {
            if (val is JsonArray arr && arr.Count == 2)
            {
                var fieldName = arr[0]?.ToString();
                var valuesNode = arr[1] as JsonArray;
                if (fieldName == null || valuesNode == null) return false;

                var entityValue = entity.DynamicFields.TryGetValue(fieldName, out var bv)
                    ? BsonToComparable(bv)?.ToString()
                    : null;

                var values = valuesNode.Select(x => x?.ToString()).ToList();
                return entityValue != null && values.Contains(entityValue);
            }
            return false;
        }

        private bool LogicalArray(JsonNode val, DynamicEntity entity, bool isAnd)
        {
            if (val is JsonArray arr)
            {
                foreach (var item in arr)
                {
                    var result = EvaluateJql(item!, entity);
                    if (isAnd && !result) return false;
                    if (!isAnd && result) return true;
                }
                return isAnd;
            }
            return isAnd;
        }

        private object? BsonToComparable(BsonValue bv) => bv switch
        {
            BsonString s => s.Value,
            BsonInt32 i => i.Value,
            BsonInt64 l => l.Value,
            BsonDouble d => d.Value,
            BsonBoolean b => b.Value,
            BsonDateTime dt => dt.ToUniversalTime(),
            _ => bv.ToString()
        };

        private int CompareNumeric(object? a, object? b)
        {
            if (a == null || b == null) return -1;

            if (double.TryParse(a.ToString(), out var x) && double.TryParse(b.ToString(), out var y))
                return x.CompareTo(y);

            return string.Compare(a.ToString(), b.ToString(), StringComparison.Ordinal);
        }
    }
}