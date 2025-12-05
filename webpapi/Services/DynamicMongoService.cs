using MongoDB.Bson;
using MongoDB.Driver;
using DynamicMongoAPI.Models;
using BCrypt.Net;
using System.Text.RegularExpressions;
using DynamicMongoAPI.Constants;

namespace DynamicMongoAPI.Services
{
    public class DynamicMongoService
    {
        private readonly IMongoClient _client;
        private readonly MongoSchemaService _schemaService;
        private readonly string _masterSchemaDatabaseName;
        
        public DynamicMongoService(IMongoClient client, MongoSchemaService schemaService, IConfiguration configuration)
        {
            _client = client;
            _schemaService = schemaService;
            _masterSchemaDatabaseName = configuration["MongoDB:MasterSchemaDatabase"] ?? AppConstants.MasterSchemaDatabaseName;
        }
        
        // Function registry - centralized place to define all available functions
        public static readonly Dictionary<string, FunctionInfo> AvailableFunctions = new Dictionary<string, FunctionInfo>(StringComparer.OrdinalIgnoreCase)
        {
            {
                "bcrypt", new FunctionInfo
                {
                    Name = "bcrypt",
                    Description = "Hashes a string value using bcrypt algorithm",
                    Type = "string",
                    Parameters = new object[0],
                    Example = new { function = "bcrypt" }
                }
            },
            {
                "lowercase", new FunctionInfo
                {
                    Name = "lowercase",
                    Description = "Converts a string value to lowercase",
                    Type = "string",
                    Parameters = new object[0],
                    Example = new { function = "lowercase" }
                }
            },
            {
                "uppercase", new FunctionInfo
                {
                    Name = "uppercase",
                    Description = "Converts a string value to uppercase",
                    Type = "string",
                    Parameters = new object[0],
                    Example = new { function = "uppercase" }
                }
            },
            {
                "date", new FunctionInfo
                {
                    Name = "date",
                    Description = "Sets the field to the current UTC date and time",
                    Type = "date",
                    Parameters = new object[0],
                    Example = new { function = "date" }
                }
            },
            {
                "now", new FunctionInfo
                {
                    Name = "now",
                    Description = "Sets the field to the current UTC date and time (alias for date)",
                    Type = "date",
                    Parameters = new object[0],
                    Example = new { function = "now" }
                }
            }
        };
        
        public ObjectId ToObjectId(string id) =>
            ObjectId.TryParse(id, out var oid) ? oid : ObjectId.Empty;
            
        public void ApplyFieldFunctions(BsonDocument doc, EntitySchema schema)
        {
            if (schema.Fields == null) return;
            
            foreach (var field in schema.Fields)
            {
                // Handle date functions
                if (field.Function?.ToLower() == "date" || field.Function?.ToLower() == "now")
                {
                    doc[field.Name] = DateTime.UtcNow;
                    continue;
                }
                
                if (!doc.Contains(field.Name)) continue;
                if (string.IsNullOrEmpty(field.Function)) continue;
                
                switch (field.Function.ToLower())
                {
                    case "bcrypt":
                        doc[field.Name] = BCrypt.Net.BCrypt.HashPassword(doc[field.Name].AsString);
                        break;
                    case "lowercase":
                        doc[field.Name] = doc[field.Name].AsString.ToLower();
                        break;
                    case "uppercase":
                        doc[field.Name] = doc[field.Name].AsString.ToUpper();
                        break;
                }
            }
        }
        
        public void ValidateEnums(BsonDocument doc, EntitySchema schema)
        {
            if (schema.Fields == null) return;
            
            foreach (var field in schema.Fields)
            {
                if (!doc.Contains(field.Name)) continue;
                if (field.EnumValues == null || field.EnumValues.Count == 0) continue;
                
                var value = doc[field.Name].ToString();
                if (!field.EnumValues.Contains(value))
                {
                    throw new ArgumentException($"Field '{field.Name}' must be one of: {string.Join(", ", field.EnumValues)}");
                }
            }
        }
        
        public void CheckRules(List<RuleDefinition>? rules, string action, BsonDocument doc)
        {
            if (rules == null || rules.Count == 0) return;
            
            foreach (var rule in rules)
            {
                if (!string.Equals(rule.Action, action, StringComparison.OrdinalIgnoreCase)) continue;
                
                bool violates = false;
                
                if (!string.IsNullOrEmpty(rule.Field) && rule.AllowedValues != null && rule.AllowedValues.Count > 0)
                {
                    var field = rule.Field;
                    var allowed = rule.AllowedValues;
                    
                    if (doc.Contains(field) && !allowed.Contains(doc[field].ToString()))
                    {
                        violates = true;
                    }
                }
                
                if (violates)
                {
                    throw new InvalidOperationException(rule.Message ?? "Business rule violated");
                }
            }
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
        
        // Improved virtual field evaluation with better expression parsing
        public string EvaluateVirtualField(string expression, BsonDocument doc)
        {
            // Handle simple concatenation expressions like "FirstName + ' ' + LastName"
            var result = expression;
            
            // Find all field references in the expression (assuming they are valid field names)
            var fieldRefs = Regex.Matches(expression, @"\b[a-zA-Z_][a-zA-Z0-9_]*\b")
                                .Cast<Match>()
                                .Select(m => m.Value)
                                .Where(f => doc.Contains(f))
                                .Distinct();
            
            // Replace field references with their values
            foreach (var field in fieldRefs)
            {
                var value = doc[field].ToString();
                // Escape special regex characters in the value
                var escapedValue = Regex.Escape(value);
                result = Regex.Replace(result, $@"\b{field}\b", value);
            }
            
            // Handle string concatenation
            result = result.Replace(" + ", "");
            result = result.Replace("'", "");
            
            return result.Trim();
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
    }
    
    public class FunctionInfo
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public object[] Parameters { get; set; } = Array.Empty<object>();
        public object Example { get; set; } = new object();
    }
}