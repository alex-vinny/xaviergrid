using MongoDB.Bson;
using DynamicMongoAPI.Models;
using BCrypt.Net;

namespace DynamicMongoAPI.Services
{
    public class FieldFunctionService : IFieldFunctionService
    {
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
    }
}