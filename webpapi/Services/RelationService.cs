using DynamicMongoAPI.Models;
using DynamicMongoAPI.Utils;
using MongoDB.Bson;
using MongoDB.Driver;

namespace DynamicMongoAPI.Services
{
    public class RelationService : BaseDataService, IRelationService
    {
        private readonly ISchemaService _schemaService;
        private readonly INamespaceService _namespaceService;

        public RelationService(
            IMongoClient client,
            IConfigurator config,
            ISchemaService schemaService,
            INamespaceService namespaceService)
            : base(client, config)
        {
            _schemaService = schemaService;
            _namespaceService = namespaceService;
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
            var db = await _namespaceService.GetNamespaceDatabaseAsync(schema.Namespace);
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

            var db = await _namespaceService.GetNamespaceDatabaseAsync(schema.Namespace);
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

            var db = await _namespaceService.GetNamespaceDatabaseAsync(schema.Namespace);

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
}