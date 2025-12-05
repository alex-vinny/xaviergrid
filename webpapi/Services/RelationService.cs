using MongoDB.Bson;
using MongoDB.Driver;
using DynamicMongoAPI.Models;
using System.Text.RegularExpressions;

namespace DynamicMongoAPI.Services
{
    public class RelationService : IRelationService
    {
        private readonly MongoSchemaService _schemaService;
        
        public RelationService(MongoSchemaService schemaService)
        {
            _schemaService = schemaService;
        }
        
        public async Task ApplyRelations(BsonDocument doc, EntitySchema schema, IMongoDatabase db)
        {
            if (schema.Relations == null) return;
            
            foreach (var relation in schema.Relations)
            {
                var collectionName = relation.Collection;
                var foreignField = string.IsNullOrEmpty(relation.ForeignField) ? "_id" : relation.ForeignField;
                var asField = relation.As;
                
                if (string.IsNullOrEmpty(collectionName) || string.IsNullOrEmpty(asField)) continue;
                
                var childCollection = db.GetCollection<BsonDocument>(collectionName);
                var childFilter = Builders<BsonDocument>.Filter.Eq(foreignField, doc["_id"]) &
                                  Builders<BsonDocument>.Filter.Ne("isDeleted", true);
                
                var children = await childCollection.Find(childFilter).ToListAsync();
                doc[asField] = new BsonArray(children);
                
                // Recursively apply grandchild relations if they exist
                try
                {
                    var childSchema = await _schemaService.GetSchemaAsync(collectionName);
                    if (childSchema.Relations != null && childSchema.Relations.Count > 0)
                    {
                        foreach (var childDoc in children)
                        {
                            await ApplyRelations(childDoc, childSchema, db);
                        }
                    }
                }
                catch (ArgumentException)
                {
                    // If child schema doesn't exist, skip recursive relations
                    continue;
                }
            }
        }
    }
}