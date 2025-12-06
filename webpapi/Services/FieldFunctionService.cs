using DynamicMongoAPI.Models;
using MongoDB.Bson;

namespace DynamicMongoAPI.Services
{
    public class FieldFunctionService : IFieldFunctionService
    {
        // Expose available functions publicly for controllers
        public static IReadOnlyDictionary<string, Func<BsonValue, BsonValue>> AvailableFunctions => Functions;

        private static readonly Dictionary<string, Func<BsonValue, BsonValue>> Functions =
            new(StringComparer.OrdinalIgnoreCase)
            {
            { "bcrypt", val => BCrypt.Net.BCrypt.HashPassword(val.AsString) },
            { "lowercase", val => val.AsString.ToLowerInvariant() },
            { "uppercase", val => val.AsString.ToUpperInvariant() },
            { "date", _ => DateTime.UtcNow },
            { "now", _ => DateTime.UtcNow }
            };

        public Task ApplyFieldFunctionsAsync(DynamicEntity entity, EntitySchema schema)
        {
            if (schema.Fields == null) return Task.CompletedTask;

            foreach (var field in schema.Fields)
            {
                if (string.IsNullOrEmpty(field.Function)) continue;
                if (!entity.DynamicFields.TryGetValue(field.Name, out var val)) continue;

                if (Functions.TryGetValue(field.Function, out var func))
                    entity.DynamicFields[field.Name] = func(val);
            }

            return Task.CompletedTask;
        }

        public Task EvaluateVirtualFieldsAsync(DynamicEntity entity, EntitySchema schema)
        {
            if (schema.VirtualFields == null) return Task.CompletedTask;

            foreach (var vf in schema.VirtualFields)
            {
                var expr = vf.Expression.ToLowerInvariant();
                if (expr.StartsWith("concat"))
                {
                    var parts = expr.Substring(7, expr.Length - 8)
                                    .Split(',')
                                    .Select(p => p.Trim().Trim('"')).ToArray();
                    var result = string.Join("", parts.Select(p => entity.DynamicFields.TryGetValue(p, out var v) ? v.AsString : ""));
                    entity.DynamicFields[vf.Name] = result;
                }
            }

            return Task.CompletedTask;
        }
    }
}