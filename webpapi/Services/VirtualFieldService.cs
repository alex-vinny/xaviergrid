using MongoDB.Bson;
using System.Text.RegularExpressions;

namespace DynamicMongoAPI.Services
{
    public class VirtualFieldService : IVirtualFieldService
    {
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
                var value = doc[field]?.ToString() ?? string.Empty;
                // Escape special regex characters in the value
                var escapedValue = Regex.Escape(value);
                result = Regex.Replace(result, $@"\b{field}\b", value);
            }
            
            // Handle string concatenation
            result = result.Replace(" + ", "");
            result = result.Replace("'", "");
            
            return result.Trim();
        }
    }
}