using DynamicMongoAPI.Models;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace DynamicMongoAPI.Services
{
    public class RuleWarningService : IRuleWarningService
    {
        private readonly ITranslator _translator;

        public RuleWarningService(ITranslator translator)
        {
            _translator = translator;
        }

        public async Task<IList<string>> EvaluateWarningsAsync(EntitySchema schema, DynamicEntity entity)
        {
            var warnings = new List<string>();

            if (schema.Rules == null || schema.Rules.Count == 0)
                return warnings;

            var bson = entity.DynamicFields.ToDictionary(
                kv => kv.Key,
                kv => kv.Value
            );

            bson["_id"] = entity.Id;
            bson["isDeleted"] = entity.IsDeleted;
            bson["createdAt"] = entity.CreatedAt;
            bson["updatedAt"] = entity.UpdatedAt == null ? BsonNull.Value
                                                        : (BsonValue)entity.UpdatedAt.Value;

            var doc = new BsonDocument(bson);

            foreach (var rule in schema.Rules)
            {
                if (rule.Rule?.Jql == null)
                    continue;

                try
                {
                    var filter = _translator.Translate(rule.Rule.Jql.AsObject()); // FilterDefinition<DynamicEntity>

                    var filterDoc = filter.Render(
                        BsonSerializer.SerializerRegistry.GetSerializer<DynamicEntity>(),
                        BsonSerializer.SerializerRegistry
                    );

                    if (!filterDoc.DocumentMatches(doc))
                    {
                        warnings.Add(rule.Message ?? "Rule violation detected");
                    }
                }
                catch
                {
                    warnings.Add($"Rule could not be evaluated: {rule.Message}");
                }
            }


            return warnings;
        }
    }
}