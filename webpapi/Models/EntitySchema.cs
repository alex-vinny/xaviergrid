using System.Text.Json.Serialization;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.RegularExpressions;

namespace DynamicMongoAPI.Models
{
    public class EntitySchema
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        [JsonPropertyName("id")]
        public string? Id { get; set; }
        
        [BsonElement("entity")]
        [JsonPropertyName("entity")]
        public string EntityName { get; set; } = string.Empty;
        
        [BsonElement("namespace")]
        [JsonPropertyName("namespace")]
        public string Namespace { get; set; } = "default";
        
        [BsonElement("fields")]
        [JsonPropertyName("fields")]
        public List<SchemaField>? Fields { get; set; }
        
        [BsonElement("relations")]
        [JsonPropertyName("relations")]
        public List<SchemaRelation>? Relations { get; set; }
        
        [BsonElement("virtualFields")]
        [JsonPropertyName("virtualFields")]
        public List<VirtualField>? VirtualFields { get; set; }
        
        [BsonElement("rules")]
        [JsonPropertyName("rules")]
        public List<RuleDefinition>? Rules { get; set; }
        
        [BsonIgnore]
        [JsonIgnore]
        public ObjectId InternalId { get; set; }
        
        // Validate the schema before saving
        public void Validate()
        {
            // Convert entity name to lowercase
            EntityName = EntityName.ToLower();
            
            // Convert namespace to lowercase
            Namespace = Namespace.ToLower();
            
            // Validate fields
            if (Fields != null)
            {
                // Check for reserved field names
                var reservedNames = new HashSet<string> { "id", "_id", "isdeleted", "createdat", "updatedat", "deletedat" };
                
                foreach (var field in Fields)
                {
                    // Convert field name to lowercase
                    field.Name = field.Name.ToLower();
                    
                    // Check if field name is reserved
                    if (reservedNames.Contains(field.Name))
                    {
                        throw new ArgumentException($"Field name '{field.Name}' is reserved and cannot be used");
                    }
                    
                    // Validate field name format (only lowercase letters, numbers, and underscores)
                    if (!Regex.IsMatch(field.Name, @"^[a-z0-9_]+$"))
                    {
                        throw new ArgumentException($"Field name '{field.Name}' must contain only lowercase letters, numbers, and underscores");
                    }
                }
            }
            
            // Validate relations
            if (Relations != null)
            {
                foreach (var relation in Relations)
                {
                    // Convert collection name to lowercase
                    relation.Collection = relation.Collection.ToLower();
                    
                    // Convert foreign field to lowercase
                    relation.ForeignField = relation.ForeignField.ToLower();
                    
                    // Convert 'as' field to lowercase
                    relation.As = relation.As.ToLower();
                }
            }
            
            // Validate virtual fields
            if (VirtualFields != null)
            {
                foreach (var virtualField in VirtualFields)
                {
                    // Convert virtual field name to lowercase
                    virtualField.Name = virtualField.Name.ToLower();
                }
            }
        }
    }
    
    public class SchemaField
    {
        [BsonElement("name")]
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
        
        [BsonElement("type")]
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;
        
        [BsonElement("required")]
        [JsonPropertyName("required")]
        public bool Required { get; set; }
        
        [BsonElement("enum")]
        [JsonPropertyName("enum")]
        public List<string>? EnumValues { get; set; }
        
        [BsonElement("function")]
        [JsonPropertyName("function")]
        public string? Function { get; set; }
    }
    
    public class SchemaRelation
    {
        [BsonElement("collection")]
        [JsonPropertyName("collection")]
        public string Collection { get; set; } = string.Empty;
        
        [BsonElement("foreignField")]
        [JsonPropertyName("foreignField")]
        public string ForeignField { get; set; } = "_id";
        
        [BsonElement("as")]
        [JsonPropertyName("as")]
        public string As { get; set; } = string.Empty;
    }
    
    public class VirtualField
    {
        [BsonElement("name")]
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
        
        [BsonElement("expression")]
        [JsonPropertyName("expression")]
        public string Expression { get; set; } = string.Empty;
    }
    
    public class RuleDefinition
    {
        [BsonElement("action")]
        [JsonPropertyName("action")]
        public string Action { get; set; } = string.Empty; // create, update, delete
        
        [BsonElement("field")]
        [JsonPropertyName("field")]
        public string? Field { get; set; }
        
        [BsonElement("allowedValues")]
        [JsonPropertyName("allowedValues")]
        public List<string>? AllowedValues { get; set; }
        
        [BsonElement("message")]
        [JsonPropertyName("message")]
        public string Message { get; set; } = "Business rule violated";
    }
}