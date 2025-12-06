using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace DynamicMongoAPI.Models
{
    public class VirtualField
    {
        public string Name { get; set; } = string.Empty;

        public string Expression { get; set; } = string.Empty;
    }
}