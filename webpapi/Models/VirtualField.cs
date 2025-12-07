using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace DynamicMongoAPI.Models
{
    public class VirtualField
    {
        [BsonElement("name")]
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [BsonElement("expression")]
        [JsonPropertyName("expression")]
        public string Expression { get; set; } = string.Empty;
    }
}