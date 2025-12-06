using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using DynamicMongoAPI.Models;
using System.Text.Json;

namespace DynamicMongoAPI.Services
{
    public class ValidationService : IValidationService
    {
        public void ValidateEnums(BsonDocument doc, EntitySchema schema)
        {
            if (schema.Fields == null) return;
            
            foreach (var field in schema.Fields)
            {
                if (!doc.Contains(field.Name)) continue;
                if (field.EnumValues == null || field.EnumValues.Count == 0) continue;
                
                var value = doc[field.Name].ToString();
                if (field.EnumValues != null && !field.EnumValues.Contains(value))
                {
                    throw new ArgumentException($"Field '{field.Name}' must be one of: {string.Join(", ", field.EnumValues)}");
                }
            }
        }
        
        public void CheckRules(List<RuleDefinition>? rules, string action, BsonDocument doc)
        {
            if (rules == null || rules.Count == 0) return;
            
            var ruleValidator = new RuleValidator();
            
            foreach (var rule in rules)
            {
                if (!string.Equals(rule.Action, action, StringComparison.OrdinalIgnoreCase)) continue;
                
                // Handle new JSONQueryLang format
                if (rule.Rule != null && rule.Rule.Filter.Any())
                {
                    var docJson = JsonSerializer.Serialize(doc);
                    var filterJson = JsonSerializer.Serialize(rule.Rule.Filter);
                    
                    var docJsonDoc = JsonDocument.Parse(docJson);
                    var filterJsonDoc = JsonDocument.Parse(filterJson);
                    
                    if (!ruleValidator.Validate(docJsonDoc, filterJsonDoc))
                    {
                        throw new InvalidOperationException(rule.Message ?? "Business rule violated");
                    }
                }
            }
        }
    }
}