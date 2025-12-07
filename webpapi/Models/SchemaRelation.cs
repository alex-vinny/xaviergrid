using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace DynamicMongoAPI.Models
{
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

        [BsonElement("type")]
        [JsonPropertyName("type")]
        public RelationType Type { get; set; } = RelationType.ManyToOne;
        
        [BsonElement("isNullable")]
        [JsonPropertyName("isNullable")]
        public bool IsNullable { get; set; } = true;
        
        [BsonElement("defaultValue")]
        [JsonPropertyName("defaultValue")]
        public object? DefaultValue { get; set; } = null;
    }
}