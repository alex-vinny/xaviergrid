using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.RegularExpressions;

namespace DynamicMongoAPI.Models
{
    public class EntitySchema
    {
        [BsonId]
        public ObjectId Id { get; set; }
        
        [BsonElement("entity")]
        public string EntityName { get; set; } = string.Empty;
        
        [BsonElement("namespace")]
        public string Namespace { get; set; } = "default";
        
        [BsonElement("fields")]
        public List<SchemaField>? Fields { get; set; }
        
        [BsonElement("relations")]
        public List<SchemaRelation>? Relations { get; set; }
        
        [BsonElement("virtualFields")]
        public List<VirtualField>? VirtualFields { get; set; }
        
        [BsonElement("rules")]
        public List<RuleDefinition>? Rules { get; set; }
        
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
        public string Name { get; set; } = string.Empty;
        
        [BsonElement("type")]
        public string Type { get; set; } = string.Empty;
        
        [BsonElement("required")]
        public bool Required { get; set; }
        
        [BsonElement("enum")]
        public List<string>? EnumValues { get; set; }
        
        [BsonElement("function")]
        public string? Function { get; set; }
    }
    
    public class SchemaRelation
    {
        [BsonElement("collection")]
        public string Collection { get; set; } = string.Empty;
        
        [BsonElement("foreignField")]
        public string ForeignField { get; set; } = "_id";
        
        [BsonElement("as")]
        public string As { get; set; } = string.Empty;
    }
    
    public class VirtualField
    {
        [BsonElement("name")]
        public string Name { get; set; } = string.Empty;
        
        [BsonElement("expression")]
        public string Expression { get; set; } = string.Empty;
    }
    
    public class RuleDefinition
    {
        [BsonElement("action")]
        public string Action { get; set; } = string.Empty; // create, update, delete
        
        [BsonElement("field")]
        public string? Field { get; set; }
        
        [BsonElement("allowedValues")]
        public List<string>? AllowedValues { get; set; }
        
        [BsonElement("message")]
        public string Message { get; set; } = "Business rule violated";
    }
}