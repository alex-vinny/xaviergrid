using DynamicMongoAPI.Models;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Text.Json.Nodes;

namespace DynamicMongoAPI.Services
{
    public class DynamicEntityService : BaseDataService
    {
        private readonly ISchemaService _schemaService;
        private readonly IHistoryService _history;
        private readonly IRuleValidator _ruleValidator;
        private readonly IRuleWarningService _ruleWarnings;
        private readonly IFieldFunctionService _fieldFunctions;
        private readonly IRelationService _relations;
        private readonly ITranslator _translator;

        public DynamicEntityService(
            IMongoClient client,
            IConfiguration config,
            ISchemaService schemaService,
            IHistoryService history,
            IRuleValidator ruleValidator,
            IRuleWarningService ruleWarnings,
            IFieldFunctionService fieldFunctions,
            IRelationService relations,
            ITranslator translator,
            IMetadataService metadata)
            : base(client, config, metadata)
        {
            _schemaService = schemaService;
            _history = history;
            _ruleValidator = ruleValidator;
            _ruleWarnings = ruleWarnings;
            _fieldFunctions = fieldFunctions;
            _relations = relations;
            _translator = translator;
        }

        private async Task<IMongoCollection<DynamicEntity>> GetCollectionAsync(EntitySchema schema)
        {
            return await _metadataService.GetNamespaceCollectionAsync<DynamicEntity>(
                schema.Namespace,
                schema.EntityName
            );
        }

        private async Task ValidateEntityAsync(DynamicEntity entity, EntitySchema schema, RuleAction action)
        {
            // built-in validation
            if (schema.Fields != null)
            {
                foreach (var field in schema.Fields)
                {
                    if (!entity.DynamicFields.ContainsKey(field.Name) && field.Required)
                        throw new ArgumentException($"Field '{field.Name}' is required.");

                    if (!entity.DynamicFields.TryGetValue(field.Name, out var value))
                        continue;

                    switch (field.Type)
                    {
                        case FieldType.String:
                            if (!value.IsString) throw new ArgumentException($"Field '{field.Name}' must be string.");
                            break;

                        case FieldType.Number:
                            if (!value.IsInt32 && !value.IsInt64 && !value.IsDouble && !value.IsDecimal128)
                                throw new ArgumentException($"Field '{field.Name}' must be numeric.");
                            break;

                        case FieldType.Boolean:
                            if (!value.IsBoolean) throw new ArgumentException($"Field '{field.Name}' must be boolean.");
                            break;

                        case FieldType.Date:
                            if (!value.IsValidDateTime()) throw new ArgumentException($"Field '{field.Name}' must be a date.");
                            break;

                        case FieldType.Object:
                            if (!value.IsBsonDocument) throw new ArgumentException($"Field '{field.Name}' must be an object.");
                            break;

                        case FieldType.Array:
                            if (!value.IsBsonArray) throw new ArgumentException($"Field '{field.Name}' must be an array.");
                            break;
                    }

                    if (field.EnumValues != null && field.EnumValues.Count > 0)
                    {
                        if (!field.EnumValues.Contains(value.AsString))
                        {
                            var expected = string.Join(", ", field.EnumValues);
                            throw new ArgumentException($"Field '{field.Name}' must be one of: {expected}");
                        }
                    }
                }
            }

            await _ruleValidator.ValidateAsync(schema, entity, action);
        }

        // ---------------------------------------------------------------------

        public async Task<DynamicEntity> CreateAsync(string entityName, DynamicEntity entity)
        {
            var schema = await _schemaService.GetSchemaAsync(entityName);
            var col = await GetCollectionAsync(schema);

            // field functions BEFORE validation
            await _fieldFunctions.ApplyFieldFunctionsAsync(entity, schema);

            // Validate
            await ValidateEntityAsync(entity, schema, RuleAction.Create);

            entity.Id = ObjectId.GenerateNewId();
            entity.CreatedAt = DateTime.UtcNow;
            entity.UpdatedAt = DateTime.UtcNow;
            entity.IsDeleted = false;

            await col.InsertOneAsync(entity);

            await _history.RecordVersionAsync(entityName, entity);

            // Apply relations + virtual fields
            await _relations.ApplyRelationsAsync(entity, schema);
            await _fieldFunctions.EvaluateVirtualFieldsAsync(entity, schema);

            return entity;
        }

        // ---------------------------------------------------------------------

        public async Task<DynamicEntity?> GetAsync(string entityName, string id)
        {
            var schema = await _schemaService.GetSchemaAsync(entityName);
            var col = await GetCollectionAsync(schema);

            var filter = Builders<DynamicEntity>.Filter.Eq("_id", ObjectId.Parse(id));
            var entity = await col.Find(filter).FirstOrDefaultAsync();

            if (entity == null)
                return null;

            await _relations.ApplyRelationsAsync(entity, schema);
            await _fieldFunctions.EvaluateVirtualFieldsAsync(entity, schema);

            // Inject warnings
            var warnings = await _ruleWarnings.EvaluateWarningsAsync(schema, entity);
            entity.DynamicFields["warnings"] = new BsonArray(warnings);

            return entity;
        }

        // ---------------------------------------------------------------------

        public async Task<IList<DynamicEntity>> ListAsync(string entityName, int page = 1, int pageSize = 50)
        {
            var schema = await _schemaService.GetSchemaAsync(entityName);
            var col = await GetCollectionAsync(schema);

            var results = await col.Find(_ => true)
                                   .Skip((page - 1) * pageSize)
                                   .Limit(pageSize)
                                   .ToListAsync();

            foreach (var entity in results)
            {
                await _relations.ApplyRelationsAsync(entity, schema);
                await _fieldFunctions.EvaluateVirtualFieldsAsync(entity, schema);

                var warnings = await _ruleWarnings.EvaluateWarningsAsync(schema, entity);
                entity.DynamicFields["warnings"] = new BsonArray(warnings);
            }

            return results;
        }

        // ---------------------------------------------------------------------

        public async Task<long> CountAsync(string entityName)
        {
            var schema = await _schemaService.GetSchemaAsync(entityName);
            var col = await GetCollectionAsync(schema);
            return await col.CountDocumentsAsync(_ => true);
        }

        // ---------------------------------------------------------------------

        public async Task<DynamicEntity> UpdateAsync(string entityName, string id, DynamicEntity updated)
        {
            var schema = await _schemaService.GetSchemaAsync(entityName);
            var col = await GetCollectionAsync(schema);

            var oid = ObjectId.Parse(id);
            var filter = Builders<DynamicEntity>.Filter.Eq("_id", oid);

            var existing = await col.Find(filter).FirstOrDefaultAsync();
            if (existing == null)
                throw new KeyNotFoundException("Document not found.");

            // Apply field functions first
            await _fieldFunctions.ApplyFieldFunctionsAsync(updated, schema);

            // Merge dynamic fields
            foreach (var kv in updated.DynamicFields)
                existing.DynamicFields[kv.Key] = kv.Value;

            existing.UpdatedAt = DateTime.UtcNow;

            await ValidateEntityAsync(existing, schema, RuleAction.Update);
            await _history.RecordVersionAsync(entityName, existing);

            await col.ReplaceOneAsync(filter, existing);

            await _relations.ApplyRelationsAsync(existing, schema);
            await _fieldFunctions.EvaluateVirtualFieldsAsync(existing, schema);

            return existing;
        }

        // ---------------------------------------------------------------------

        public async Task DeleteAsync(string entityName, string id)
        {
            var schema = await _schemaService.GetSchemaAsync(entityName);
            var col = await GetCollectionAsync(schema);

            var oid = ObjectId.Parse(id);
            var filter = Builders<DynamicEntity>.Filter.Eq("_id", oid);

            var existing = await col.Find(filter).FirstOrDefaultAsync();
            if (existing == null) return;

            await _ruleValidator.ValidateAsync(schema, existing, RuleAction.Delete);

            await _history.RecordVersionAsync(entityName, existing);

            var update = Builders<DynamicEntity>.Update
                .Set(x => x.IsDeleted, true)
                .Set(x => x.UpdatedAt, DateTime.UtcNow);

            await col.UpdateOneAsync(filter, update);
        }

        // ---------------------------------------------------------------------

        public async Task<IList<DynamicEntity>> QueryAsync(string entityName, JsonObject query)
        {
            var schema = await _schemaService.GetSchemaAsync(entityName);
            var col = await GetCollectionAsync(schema);

            var filter = _translator.Translate(query);

            var skip = query["skip"]?.GetValue<int>() ?? 0;
            var limit = query["limit"]?.GetValue<int>() ?? 50;

            var results = await col.Find(filter)
                                   .Skip(skip)
                                   .Limit(limit)
                                   .ToListAsync();

            foreach (var e in results)
            {
                await _relations.ApplyRelationsAsync(e, schema);
                await _fieldFunctions.EvaluateVirtualFieldsAsync(e, schema);

                var warnings = await _ruleWarnings.EvaluateWarningsAsync(schema, e);
                e.DynamicFields["warnings"] = new BsonArray(warnings);
            }

            return results;
        }
    }
}