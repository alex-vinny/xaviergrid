using MongoDB.Bson;
using MongoDB.Driver;
using System.Text.RegularExpressions;
using System.Text.Json;
using DynamicMongoAPI.Models;

namespace DynamicMongoAPI.Services
{
    public class QueryService : IQueryService
    {
        private readonly IJsonQueryLangMongoTranslator _translator;
        
        public QueryService(IJsonQueryLangMongoTranslator translator)
        {
            _translator = translator;
        }
        
        // Enhanced filter building with support for operators
        public FilterDefinition<BsonDocument> BuildFilterFromQuery(BsonDocument query)
        {
            var filter = Builders<BsonDocument>.Filter.Empty;
            
            foreach (var element in query)
            {
                var key = element.Name;
                var value = element.Value;
                
                // Handle operator-based queries
                if (value.IsBsonDocument)
                {
                    var operatorDoc = value.AsBsonDocument;
                    foreach (var op in operatorDoc)
                    {
                        switch (op.Name.ToLower())
                        {
                            case "$eq":
                                filter &= Builders<BsonDocument>.Filter.Eq(key, op.Value);
                                break;
                            case "$ne":
                                filter &= Builders<BsonDocument>.Filter.Ne(key, op.Value);
                                break;
                            case "$gt":
                                filter &= Builders<BsonDocument>.Filter.Gt(key, op.Value);
                                break;
                            case "$gte":
                                filter &= Builders<BsonDocument>.Filter.Gte(key, op.Value);
                                break;
                            case "$lt":
                                filter &= Builders<BsonDocument>.Filter.Lt(key, op.Value);
                                break;
                            case "$lte":
                                filter &= Builders<BsonDocument>.Filter.Lte(key, op.Value);
                                break;
                            case "$in":
                                filter &= Builders<BsonDocument>.Filter.In(key, op.Value.AsBsonArray);
                                break;
                            case "$nin":
                                filter &= Builders<BsonDocument>.Filter.Nin(key, op.Value.AsBsonArray);
                                break;
                            case "$regex":
                                filter &= Builders<BsonDocument>.Filter.Regex(key, new BsonRegularExpression(op.Value.AsString));
                                break;
                        }
                    }
                }
                else
                {
                    // Simple equality filter
                    filter &= Builders<BsonDocument>.Filter.Eq(key, value);
                }
            }
            
            return filter;
        }
        
        // Sanitize aggregation pipeline to prevent dangerous operations
        public BsonDocument[] SanitizeAggregationPipeline(BsonArray pipeline)
        {
            var allowedStages = new HashSet<string> { "$match", "$project", "$group", "$sort", "$limit", "$skip", "$lookup", "$unwind" };
            var sanitizedPipeline = new List<BsonDocument>();
            
            foreach (var stage in pipeline)
            {
                if (!stage.IsBsonDocument) continue;
                
                var stageDoc = stage.AsBsonDocument;
                var stageKey = stageDoc.ElementCount > 0 ? stageDoc.GetElement(0).Name : null;
                
                if (stageKey != null && allowedStages.Contains(stageKey))
                {
                    // Additional security for $lookup stages
                    if (stageKey == "$lookup")
                    {
                        // Ensure lookup only references allowed collections
                        // In a production environment, you might want to check against a whitelist
                        sanitizedPipeline.Add(stageDoc);
                    }
                    else
                    {
                        sanitizedPipeline.Add(stageDoc);
                    }
                }
            }
            
            return sanitizedPipeline.ToArray();
        }
        
        // New QueryAsync method using JSONQueryLang translator
        public async Task<IEnumerable<BsonDocument>> QueryAsync(string entity, JsonElement jsonQuery, MongoSchemaService schemaService)
        {
            var schema = await schemaService.GetSchemaAsync(entity);
            var db = schemaService.GetDatabase(schema);
            var collection = db.GetCollection<BsonDocument>(entity);
            
            var pipeline = _translator.Translate(jsonQuery);
            
            var result = await collection
                .Aggregate<BsonDocument>(pipeline)
                .ToListAsync();
            
            return result;
        }
    }
}