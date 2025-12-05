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
        
        // New services for separated concerns
        private readonly IFieldFunctionService _fieldFunctionService;
        private readonly IValidationService _validationService;
        private readonly IRelationService _relationService;
        private readonly IVirtualFieldService _virtualFieldService;
        private readonly IQueryService _queryService;
        
        public DynamicMongoService(
            IMongoClient client, 
            MongoSchemaService schemaService, 
            IConfiguration configuration,
            IFieldFunctionService fieldFunctionService,
            IValidationService validationService,
            IRelationService relationService,
            IVirtualFieldService virtualFieldService,
            IQueryService queryService)
        {
            _client = client;
            _schemaService = schemaService;
            _masterSchemaDatabaseName = configuration["MongoDB:MasterSchemaDatabase"] ?? AppConstants.MasterSchemaDatabaseName;
            _fieldFunctionService = fieldFunctionService;
            _validationService = validationService;
            _relationService = relationService;
            _virtualFieldService = virtualFieldService;
            _queryService = queryService;
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
            _fieldFunctionService.ApplyFieldFunctions(doc, schema);
        }
        
        public void ValidateEnums(BsonDocument doc, EntitySchema schema)
        {
            _validationService.ValidateEnums(doc, schema);
        }
        
        public void CheckRules(List<RuleDefinition>? rules, string action, BsonDocument doc)
        {
            _validationService.CheckRules(rules, action, doc);
        }
        
        public async Task ApplyRelations(BsonDocument doc, EntitySchema schema, IMongoDatabase db)
        {
            await _relationService.ApplyRelations(doc, schema, db);
        }
        
        // Improved virtual field evaluation with better expression parsing
        public string EvaluateVirtualField(string expression, BsonDocument doc)
        {
            return _virtualFieldService.EvaluateVirtualField(expression, doc);
        }
        
        // Enhanced filter building with support for operators
        public FilterDefinition<BsonDocument> BuildFilterFromQuery(BsonDocument query)
        {
            return _queryService.BuildFilterFromQuery(query);
        }
        
        // Sanitize aggregation pipeline to prevent dangerous operations
        public BsonDocument[] SanitizeAggregationPipeline(BsonArray pipeline)
        {
            return _queryService.SanitizeAggregationPipeline(pipeline);
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