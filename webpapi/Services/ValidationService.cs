using MongoDB.Bson;
using DynamicMongoAPI.Models;

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
    }
}