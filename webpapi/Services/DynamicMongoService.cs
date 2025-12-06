using DynamicMongoAPI.Models;
using DynamicMongoAPI.Utils;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using System.Text.Json.Nodes;

namespace DynamicMongoAPI.Services
{
    public static class BsonValueExtensions
    {
        public static bool IsValidDateTime(this BsonValue value)
        {
            if (value == null || value.IsBsonNull) return false;
            return value.IsValidDateTime || value.IsString && DateTime.TryParse(value.AsString, out _);
        }

        public static DateTime ToUniversalTime(this BsonValue value)
        {
            if (value.IsValidDateTime) return value.ToUniversalTime();
            if (value.IsString && DateTime.TryParse(value.AsString, out var dt)) return dt.ToUniversalTime();
            return DateTime.UtcNow;
        }
    }

    public static class DynamicEntityExtensions
    {
        /// <summary>
        /// Convert a DynamicEntity to a BsonDocument, including system fields and dynamic fields.
        /// </summary>
        public static BsonDocument ToBsonDocument(this DynamicEntity entity)
        {
            var doc = new BsonDocument
            {
                ["_id"] = entity.Id != ObjectId.Empty ? entity.Id : ObjectId.GenerateNewId(),
                ["isDeleted"] = entity.IsDeleted,
                ["createdAt"] = entity.CreatedAt != default ? entity.CreatedAt : DateTime.UtcNow,
                ["updatedAt"] = entity.UpdatedAt ?? DateTime.UtcNow
            };

            if (entity.DynamicFields != null)
            {
                foreach (var kv in entity.DynamicFields)
                {
                    doc[kv.Key] = kv.Value ?? BsonNull.Value;
                }
            }

            return doc;
        }

        /// <summary>
        /// Create a DynamicEntity from a BsonDocument
        /// </summary>
        public static DynamicEntity FromBsonDocument(BsonDocument doc)
        {
            if (doc == null)
                throw new ArgumentNullException(nameof(doc));

            var entity = new DynamicEntity
            {
                Id = doc.GetValue("_id", ObjectId.GenerateNewId()).AsObjectId,
                IsDeleted = doc.GetValue("isDeleted", false).AsBoolean,
                CreatedAt = doc.Contains("createdAt") ? doc["createdAt"].ToUniversalTime() : DateTime.UtcNow,
                UpdatedAt = doc.Contains("updatedAt") ? doc["updatedAt"].ToUniversalTime() : (DateTime?)null,
                DynamicFields = new Dictionary<string, BsonValue>()
            };

            foreach (var element in doc.Elements)
            {
                if (element.Name == "_id" || element.Name == "isDeleted" || element.Name == "createdAt" || element.Name == "updatedAt")
                    continue;

                entity.DynamicFields[element.Name] = element.Value;
            }

            return entity;
        }

        /// <summary>
        /// Convert a DynamicEntity to a dictionary for JSON serialization (nested conversion handled)
        /// </summary>
        public static Dictionary<string, object> ToDictionary(this DynamicEntity entity)
        {
            var doc = entity.ToBsonDocument();
            return BsonConverter.BsonToDictionary(doc);
        }

        /// <summary>
        /// Create a DynamicEntity from a dictionary (inverse of ToDictionary)
        /// </summary>
        public static DynamicEntity FromDictionary(Dictionary<string, object> dict)
        {
            var doc = new BsonDocument();
            foreach (var kv in dict)
            {
                doc[kv.Key] = BsonValue.Create(kv.Value ?? BsonNull.Value);
            }

            return FromBsonDocument(doc);
        }

        /// <summary>
        /// Convert a DynamicEntity to a dictionary of BsonValues (keeps Bson types, safe for history)
        /// </summary>
        public static Dictionary<string, BsonValue> ToBsonDictionary(this DynamicEntity entity)
        {
            var dict = new Dictionary<string, BsonValue>
            {
                ["_id"] = entity.Id != ObjectId.Empty ? entity.Id : ObjectId.GenerateNewId(),
                ["isDeleted"] = entity.IsDeleted,
                ["createdAt"] = entity.CreatedAt != default ? entity.CreatedAt : DateTime.UtcNow,
                ["updatedAt"] = entity.UpdatedAt ?? DateTime.UtcNow
            };

            if (entity.DynamicFields != null)
            {
                foreach (var kv in entity.DynamicFields)
                {
                    dict[kv.Key] = kv.Value ?? BsonNull.Value;
                }
            }

            return dict;
        }

        /// <summary>
        /// Create a DynamicEntity from a dictionary of BsonValues (used by HistoryService)
        /// </summary>
        public static DynamicEntity FromBsonDictionary(Dictionary<string, BsonValue> dict)
        {
            var doc = new BsonDocument();

            foreach (var kv in dict)
            {
                doc[kv.Key] = kv.Value ?? BsonNull.Value;
            }

            return FromBsonDocument(doc);
        }
    }

    public static class BsonDocumentExtensions
    {
        public static bool DocumentMatches(this BsonDocument filter, BsonDocument doc)
        {
            // Simple equality-only matcher
            foreach (var elem in filter.Elements)
            {
                if (!doc.Contains(elem.Name)) return false;

                if (!doc[elem.Name].Equals(elem.Value)) return false;
            }
            return true;
        }
    }

    public interface IMetadataService
    {
        Task<string> GetNamespaceForEntityAsync(string entityName);
        Task<IMongoDatabase> GetNamespaceDatabaseAsync(string namespaceName);
        Task<IMongoCollection<T>> GetNamespaceCollectionAsync<T>(string namespaceName, string collectionName);
    }

    public interface IHistoryService
    {
        Task RecordVersionAsync(string entityName, DynamicEntity entity);
        Task<DynamicEntity?> RestoreAsync(string entityName, string id, int? version = null);
        Task<IList<EntityHistory>> HistoryAsync(string entityName, string id, int page = 1, int pageSize = 50);
        Task<long> CountAsync(string entityName, string id);
        Task PurgeAsync(string entityName);
    }

    public interface IRuleValidator
    {
        Task ValidateAsync(EntitySchema schema, DynamicEntity entity, RuleAction action);
    }

    public interface IRuleWarningService
    {
        Task<IList<string>> EvaluateWarningsAsync(EntitySchema schema, DynamicEntity entity);
    }

    public interface IFieldFunctionService
    {
        Task ApplyFieldFunctionsAsync(DynamicEntity entity, EntitySchema schema);
        Task EvaluateVirtualFieldsAsync(DynamicEntity entity, EntitySchema schema);
    }

    public interface IRelationService
    {
        Task<List<SchemaRelation>> GetRelationsAsync(string entityName);
        Task<SchemaRelation?> GetRelationAsync(string entityName, int relationId);
        Task AddRelationsAsync(string entityName, List<SchemaRelation> relations);
        Task UpdateRelationAsync(string entityName, int relationId, SchemaRelation relation);
        Task DeleteRelationsAsync(string entityName);
        Task DeleteRelationAsync(string entityName, int relationId);
        Task ApplyRelationsAsync(DynamicEntity entity, EntitySchema schema);
    }

    public interface ITranslator
    {
        FilterDefinition<DynamicEntity> Translate(JsonObject query);
    }

    public class MetadataService : IMetadataService
    {
        private readonly IMongoClient _client;
        private readonly ISchemaService _schemaService;
        private readonly string _schemaDbName;

        public MetadataService(IMongoClient client, ISchemaService schemaService, IConfiguration config)
        {
            _client = client;
            _schemaService = schemaService;
            _schemaDbName = config.GetValue<string>("Mongo:SchemaDatabase") ?? "schema_db";
        }

        public async Task<string> GetNamespaceForEntityAsync(string entityName)
        {
            var schema = await _schemaService.GetSchemaAsync(entityName);
            return schema.Namespace;
        }

        public Task<IMongoDatabase> GetNamespaceDatabaseAsync(string namespaceName)
        {
            // Namespace == Database name
            return Task.FromResult(_client.GetDatabase(namespaceName));
        }

        public async Task<IMongoCollection<T>> GetNamespaceCollectionAsync<T>(
            string namespaceName,
            string collectionName)
        {
            var db = await GetNamespaceDatabaseAsync(namespaceName);
            return db.GetCollection<T>(collectionName);
        }
    }   

    public class HistoryService : BaseDataService, IHistoryService
    {
        private readonly ISchemaService _schemaService;

        public HistoryService(
            IMongoClient client,
            IConfiguration config,
            ISchemaService schemaService,
            IMetadataService metadata)
            : base(client, config, metadata)
        {
            _schemaService = schemaService;
        }

        /// <summary>
        /// Get the history collection for the entity via MetadataService
        /// </summary>
        private async Task<IMongoCollection<EntityHistory>> GetCollectionAsync(string entityName)
        {
            var schema = await _schemaService.GetSchemaAsync(entityName);
            return await _metadataService.GetNamespaceCollectionAsync<EntityHistory>(
                schema.Namespace,
                $"{schema.EntityName}_history"
            );
        }

        public async Task RecordVersionAsync(string entityName, DynamicEntity entity)
        {
            var col = await GetCollectionAsync(entityName);

            // Compute next version
            var version = await col.CountDocumentsAsync(h => h.DocumentId == entity.Id) + 1;

            // Store all fields including system fields
            var history = new EntityHistory
            {
                DocumentId = entity.Id,
                Version = (int)version,
                Timestamp = DateTime.UtcNow,
                Data = entity.ToBsonDictionary()
            };

            await col.InsertOneAsync(history);
        }

        public async Task<DynamicEntity?> RestoreAsync(string entityName, string id, int? version = null)
        {
            var col = await GetCollectionAsync(entityName);
            var oid = ObjectId.Parse(id);

            var filter = Builders<EntityHistory>.Filter.Eq(h => h.DocumentId, oid);
            if (version.HasValue)
                filter &= Builders<EntityHistory>.Filter.Eq(h => h.Version, version.Value);

            var hist = await col.Find(filter)
                                .SortByDescending(h => h.Version)
                                .FirstOrDefaultAsync();

            return hist != null ? DynamicEntityExtensions.FromBsonDictionary(hist.Data) : null;
        }

        public async Task<IList<EntityHistory>> HistoryAsync(string entityName, string id, int page = 1, int pageSize = 50)
        {
            var col = await GetCollectionAsync(entityName);
            var oid = ObjectId.Parse(id);

            return await col.Find(h => h.DocumentId == oid)
                            .SortByDescending(h => h.Version)
                            .Skip((page - 1) * pageSize)
                            .Limit(pageSize)
                            .ToListAsync();
        }

        public async Task<long> CountAsync(string entityName, string id)
        {
            var col = await GetCollectionAsync(entityName);
            var oid = ObjectId.Parse(id);
            return await col.CountDocumentsAsync(h => h.DocumentId == oid);
        }

        public async Task PurgeAsync(string entityName)
        {
            var col = await GetCollectionAsync(entityName);
            await col.DeleteManyAsync(FilterDefinition<EntityHistory>.Empty);
        }
    }

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

    public class RelationService : BaseDataService, IRelationService
    {
        private readonly ISchemaService _schemaService;

        public RelationService(
            IMongoClient client,
            IConfiguration config,
            ISchemaService schemaService,
            IMetadataService metadata)
            : base(client, config, metadata)
        {
            _schemaService = schemaService;
        }

        public async Task<List<SchemaRelation>> GetRelationsAsync(string entityName)
        {
            var schema = await _schemaService.GetSchemaAsync(entityName);
            return schema.Relations ?? new List<SchemaRelation>();
        }

        public async Task<SchemaRelation?> GetRelationAsync(string entityName, int relationId)
        {
            var relations = await GetRelationsAsync(entityName);
            if (relationId < 0 || relationId >= relations.Count)
                throw new ArgumentException("Relation not found.");

            return relations[relationId];
        }

        public async Task AddRelationsAsync(string entityName, List<SchemaRelation> relations)
        {
            var schema = await _schemaService.GetSchemaAsync(entityName);
            schema.Relations ??= new List<SchemaRelation>();

            // Validate that all target collections exist in the namespace
            var db = await _metadataService.GetNamespaceDatabaseAsync(schema.Namespace);
            var existingCollections = await db.ListCollectionNames().ToListAsync();

            foreach (var rel in relations)
            {
                if (!existingCollections.Contains(rel.Collection))
                    throw new InvalidOperationException(
                        $"Cannot add relation '{rel.As}' to collection '{rel.Collection}' outside namespace '{schema.Namespace}'.");
            }

            schema.Relations.AddRange(relations);
            schema.Validate();
            await _schemaService.UpdateSchemaAsync(schema);
        }

        public async Task UpdateRelationAsync(string entityName, int relationId, SchemaRelation relation)
        {
            var schema = await _schemaService.GetSchemaAsync(entityName);

            if (schema.Relations == null || relationId < 0 || relationId >= schema.Relations.Count)
                throw new ArgumentException("Relation not found.");

            var db = await _metadataService.GetNamespaceDatabaseAsync(schema.Namespace);
            var existingCollections = await db.ListCollectionNames().ToListAsync();

            if (!existingCollections.Contains(relation.Collection))
                throw new InvalidOperationException(
                    $"Cannot update relation '{relation.As}' to collection '{relation.Collection}' outside namespace '{schema.Namespace}'.");

            schema.Relations[relationId] = relation;
            schema.Validate();
            await _schemaService.UpdateSchemaAsync(schema);
        }

        public async Task DeleteRelationsAsync(string entityName)
        {
            var schema = await _schemaService.GetSchemaAsync(entityName);
            schema.Relations = new List<SchemaRelation>();
            await _schemaService.UpdateSchemaAsync(schema);
        }

        public async Task DeleteRelationAsync(string entityName, int relationId)
        {
            var schema = await _schemaService.GetSchemaAsync(entityName);

            if (schema.Relations == null || relationId < 0 || relationId >= schema.Relations.Count)
                throw new ArgumentException("Relation not found.");

            schema.Relations.RemoveAt(relationId);
            await _schemaService.UpdateSchemaAsync(schema);
        }

        public async Task ApplyRelationsAsync(DynamicEntity entity, EntitySchema schema)
        {
            if (schema.Relations == null || schema.Relations.Count == 0)
                return;

            var db = await _metadataService.GetNamespaceDatabaseAsync(schema.Namespace);

            foreach (var rel in schema.Relations)
            {
                var col = db.GetCollection<DynamicEntity>(rel.Collection);

                if (!entity.DynamicFields.TryGetValue(rel.As, out var raw) || raw.IsBsonNull)
                {
                    if (!rel.IsNullable && rel.DefaultValue != null)
                        entity.DynamicFields[rel.As] = BsonValue.Create(rel.DefaultValue);
                    continue;
                }

                if (rel.Type == RelationType.OneToOne || rel.Type == RelationType.ManyToOne)
                {
                    var filter = Builders<DynamicEntity>.Filter.Eq("_id", raw.AsObjectId);
                    var related = await col.Find(filter).FirstOrDefaultAsync();
                    entity.DynamicFields[rel.As] = related != null
                        ? BsonValue.Create(related.ToBsonDocument())
                        : BsonNull.Value;
                }
                else if (rel.Type == RelationType.ManyToMany)
                {
                    if (!raw.IsBsonArray) continue;

                    var ids = raw.AsBsonArray.Select(v => v.AsObjectId).ToList();
                    if (!ids.Any()) continue;

                    var filter = Builders<DynamicEntity>.Filter.In("_id", ids);
                    var relatedDocs = await col.Find(filter).ToListAsync();

                    entity.DynamicFields[rel.As] = new BsonArray(relatedDocs.Select(d => d.ToBsonDocument()));
                }
            }
        }
    }

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